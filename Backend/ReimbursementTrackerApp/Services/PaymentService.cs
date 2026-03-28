

// FILE: Services/PaymentService.cs — FULL FILE
// CHANGE: CompletePayment, GetAllPayments, GetPaymentByExpenseId
//         now carry DocumentUrls from the linked expense into the
//         response DTO. No re-upload. Same stored path reused.

using Microsoft.EntityFrameworkCore;
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models;
using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;
using ReimbursementTrackerApp.Models.Enums;
using ReimbursementTrackerApp.Models.helper;
using System.Security.Claims;

namespace ReimbursementTrackerApp.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IRepository<string, Payment> _paymentRepo;
        private readonly IRepository<string, Expense> _expenseRepo;
        private readonly IRepository<string, User> _userRepo;
        private readonly INotificationService _notificationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PaymentService(
            IRepository<string, Payment> paymentRepo,
            IRepository<string, Expense> expenseRepo,
            IRepository<string, User> userRepo,
            INotificationService notificationService,
            IHttpContextAccessor httpContextAccessor)
        {
            _paymentRepo = paymentRepo;
            _expenseRepo = expenseRepo;
            _userRepo = userRepo;
            _notificationService = notificationService;
            _httpContextAccessor = httpContextAccessor;
        }

        // =====================================================
        // COMPLETE PAYMENT (Finance Only)
        // ✅ CHANGE: MapToDto now receives the expense too,
        //            so DocumentUrls flow into the response.
        // =====================================================
        public async Task<CreatePaymentResponseDto?> CompletePayment(
            string expenseId,
            string referenceNo,
            string paymentMode)
        {
            var (userId, userName, role) = GetUserFromToken();

            if (role != UserRole.Finance)
                throw new UnauthorizedAccessException("Only Finance users can complete payments.");

            var expense = await _expenseRepo.GetByIdAsync(expenseId);
            if (expense == null)
                throw new KeyNotFoundException("Expense not found.");

            var payment = expense.Payments?.FirstOrDefault();
            if (payment != null && payment.PaymentStatus == PaymentStatusEnum.Paid)
                throw new InvalidOperationException("Payment has already been completed for this expense.");

            if (expense.Status != ExpenseStatus.Approved)
                throw new InvalidOperationException("Expense must be approved by manager before payment.");

            if (payment == null)
            {
                payment = new Payment
                {
                    PaymentId = Guid.NewGuid().ToString(),
                    ExpenseId = expense.ExpenseId,
                    UserId = expense.UserId,
                    AmountPaid = expense.Amount,
                    PaymentStatus = PaymentStatusEnum.Pending
                };
                await _paymentRepo.AddAsync(payment);
            }

            payment.PaymentStatus = PaymentStatusEnum.Paid;
            payment.PaymentDate = DateTime.Now;
            payment.PaymentMode = paymentMode;
            payment.ReferenceNo = referenceNo;
            payment.ProcessedByUserId = userId;
            payment.ProcessedByName = userName;

            // Ensure User is loaded for UserName in response
            if (payment.User == null && payment.UserId != null)
                payment.User = await _userRepo.GetByIdAsync(payment.UserId);

            await _paymentRepo.UpdateAsync(payment.PaymentId, payment);

            expense.Status = ExpenseStatus.Paid;
            await _expenseRepo.UpdateAsync(expense.ExpenseId, expense);

            // Notify employee
            await _notificationService.CreateNotification(new CreateNotificationRequestDto
            {
                UserId = expense.UserId,
                Message = $"Your expense '{expense.ExpenseId}' has been PAID successfully. Amount: ₹{expense.Amount:N2}",
                Description = $"Reference No: {referenceNo} | Mode: {paymentMode}",
                SenderRole = "System"
            });

            // ✅ Pass expense to MapToDto so DocumentUrls are included
            return MapToDto(payment, expense);
        }

        // =====================================================
        // GET ALL PAYMENTS (Finance/Admin paged)
        // ✅ CHANGE: loads matching expense for each payment
        //            so DocumentUrls are in every row.
        // =====================================================
        public async Task<PagedResponse<CreatePaymentResponseDto>> GetAllPayments(PaginationParams paginationParams)
        {
            var allPayments = await _paymentRepo.GetAllAsync() ?? new List<Payment>();
            var allExpenses = await _expenseRepo.GetAllAsync() ?? new List<Expense>();
            var allUsers    = await _userRepo.GetAllAsync() ?? new List<User>();

            // Build a user lookup dictionary for safe O(1) access
            var userMap = allUsers.ToDictionary(u => u.UserId, u => u);

            // Attach user to each payment using the dictionary (no throws)
            foreach (var p in allPayments)
            {
                if (p.User == null && p.UserId != null && userMap.TryGetValue(p.UserId, out var u))
                    p.User = u;
            }

            // Apply date, amount, status, and username filters
            var filtered = allPayments.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(paginationParams.UserName))
                filtered = filtered.Where(p => (p.User?.UserName ?? "").Contains(paginationParams.UserName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(paginationParams.Status))
                filtered = filtered.Where(p => p.PaymentStatus.ToString().Equals(paginationParams.Status, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(paginationParams.FromDate) &&
                DateTime.TryParse(paginationParams.FromDate, out var fromDate))
                filtered = filtered.Where(p => p.PaymentDate.HasValue && p.PaymentDate.Value.Date >= fromDate.Date);

            if (!string.IsNullOrWhiteSpace(paginationParams.ToDate) &&
                DateTime.TryParse(paginationParams.ToDate, out var toDate))
                filtered = filtered.Where(p => p.PaymentDate.HasValue && p.PaymentDate.Value.Date <= toDate.Date);

            if (paginationParams.MinAmount.HasValue)
                filtered = filtered.Where(p => p.AmountPaid >= paginationParams.MinAmount.Value);

            if (paginationParams.MaxAmount.HasValue)
                filtered = filtered.Where(p => p.AmountPaid <= paginationParams.MaxAmount.Value);

            var filteredList = filtered.OrderByDescending(p => p.PaymentDate).ToList();
            var totalCount = filteredList.Count;

            var paymentsPage = filteredList
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToList();

            var paymentDtos = paymentsPage
                .Select(p =>
                {
                    // ✅ Find matching expense to get DocumentUrls
                    var expense = allExpenses.FirstOrDefault(e => e.ExpenseId == p.ExpenseId);
                    return MapToDto(p, expense);
                })
                .ToList();

            return new PagedResponse<CreatePaymentResponseDto>(
                paymentDtos,
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize
            );
        }

        // =====================================================
        // GET PAYMENT BY EXPENSE ID (role-based)
        // ✅ CHANGE: loads expense to include DocumentUrls
        // =====================================================
        public async Task<CreatePaymentResponseDto?> GetPaymentByExpenseId(
            string expenseId, string userId, string role)
        {
            var allPayments = await _paymentRepo.GetAllAsync() ?? new List<Payment>();

            var payment = allPayments
                .Where(p => p.ExpenseId == expenseId)
                .OrderByDescending(p => p.PaymentDate)
                .FirstOrDefault();

            if (payment == null) return null;

            role = role?.Trim();

            // Employee & Manager → only own payments
            if ((role.Equals("Employee", StringComparison.OrdinalIgnoreCase) ||
                 role.Equals("Manager", StringComparison.OrdinalIgnoreCase)) &&
                payment.UserId != userId)
            {
                return null;
            }

            // Finance & Admin → full access

            if (payment.User == null && payment.UserId != null)
                payment.User = await _userRepo.GetByIdAsync(payment.UserId);

            // ✅ Load expense to get DocumentUrls for this payment
            Expense? expense = null;
            try { expense = await _expenseRepo.GetByIdAsync(expenseId); } catch { }

            return MapToDto(payment, expense);
        }

        // =====================================================
        // PRIVATE MAP TO DTO
        // ✅ CHANGE: now accepts optional expense parameter
        //            and maps DocumentUrls from it.
        //            If expense is null (edge case), DocumentUrls = empty list.
        // =====================================================
        private CreatePaymentResponseDto MapToDto(Payment payment, Expense? expense = null)
        {
            return new CreatePaymentResponseDto
            {
                PaymentId = payment.PaymentId,
                UserId = payment.UserId ?? "",
                UserName = payment.User?.UserName ?? "",
                ProcessedByName = payment.ProcessedByName ?? "",
                AmountPaid = payment.AmountPaid,
                PaymentStatus = payment.PaymentStatus.ToString(),
                PaymentMode = payment.PaymentMode ?? "",
                ReferenceNo = payment.ReferenceNo ?? "",
                PaymentDate = payment.PaymentDate ?? DateTime.Now,
                AmountInRupees = CurrencyHelper.FormatRupees(payment.AmountPaid),
                ExpenseId = payment.ExpenseId,

                // ✅ Reuse stored paths — NO re-upload, same /uploads/xyz.jpg from DB
                DocumentUrls = expense?.DocumentUrls ?? new List<string>()
            };
        }

        // =====================================================
        // GET USER FROM TOKEN
        // =====================================================
        private (string userId, string userName, UserRole role) GetUserFromToken()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
                return ("Anonymous", "Anonymous", UserRole.Employee);

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
            var userName = user.FindFirstValue(ClaimTypes.Name) ?? "Unknown";
            var roleStr = user.FindFirstValue(ClaimTypes.Role) ?? "Employee";

            Enum.TryParse<UserRole>(roleStr, out var role);

            return (userId, userName, role);
        }
    }
}
