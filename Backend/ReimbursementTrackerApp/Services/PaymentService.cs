
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using ReimbursementTrackerApp.Interfaces;
//using ReimbursementTrackerApp.Models;
//using ReimbursementTrackerApp.Models.Common;
//using ReimbursementTrackerApp.Models.DTOs;
//using ReimbursementTrackerApp.Models.Enums;
//using System.Security.Claims;


//namespace ReimbursementTrackerApp.Services
//{
//    public class PaymentService : IPaymentService
//    {
//        private readonly IRepository<string, Payment> _paymentRepo;
//        private readonly IRepository<string, Expense> _expenseRepo;
//        private readonly IRepository<string, User> _userRepo;
//        private readonly INotificationService _notificationService;
//        private readonly IHttpContextAccessor _httpContextAccessor;
//        private readonly ILogger<PaymentService> _logger; // Added ILogger

//        public PaymentService(
//            IRepository<string, Payment> paymentRepo,
//            IRepository<string, Expense> expenseRepo,
//            IRepository<string, User> userRepo,
//            INotificationService notificationService,
//            IHttpContextAccessor httpContextAccessor,
//            ILogger<PaymentService> logger) // Inject ILogger
//        {
//            _paymentRepo = paymentRepo;
//            _expenseRepo = expenseRepo;
//            _userRepo = userRepo;
//            _notificationService = notificationService;
//            _httpContextAccessor = httpContextAccessor;
//            _logger = logger;
//        }

//        // =====================================================
//        // 🔹 Complete Payment (Finance Only)
//        // =====================================================
//        public async Task<CreatePaymentResponseDto?> CompletePayment(
//            string expenseId,
//            string referenceNo,
//            string paymentMode)
//        {
//            var (userId, userName, role) = GetUserFromToken();
//            _logger.LogInformation("User {UserId} ({Role}) attempting to complete payment for Expense {ExpenseId}", userId, role, expenseId);

//            if (role != UserRole.Finance)
//            {
//                _logger.LogWarning("Unauthorized access attempt by User {UserId} to complete payment.", userId);
//                throw new UnauthorizedAccessException("Only Finance users can complete payments.");
//            }

//            // 1️⃣ Fetch expense
//            var expense = await _expenseRepo.GetByIdAsync(expenseId);
//            if (expense == null)
//            {
//                _logger.LogWarning("Expense {ExpenseId} not found.", expenseId);
//                throw new KeyNotFoundException("Expense not found.");
//            }

//            // 2️⃣ Check existing payment
//            var payment = expense.Payments?.FirstOrDefault();
//            if (payment != null && payment.PaymentStatus == PaymentStatusEnum.Paid)
//            {
//                _logger.LogWarning("Payment already completed for Expense {ExpenseId}.", expenseId);
//                throw new InvalidOperationException("Payment has already been completed for this expense.");
//            }

//            // 3️⃣ Check if manager approved
//            if (expense.Status != ExpenseStatus.Approved)
//            {
//                _logger.LogWarning("Expense {ExpenseId} not approved by manager.", expenseId);
//                throw new InvalidOperationException("Expense must be approved by manager before payment.");
//            }

//            // 4️⃣ Create payment if none exists
//            if (payment == null)
//            {
//                payment = new Payment
//                {
//                    PaymentId = Guid.NewGuid().ToString(),
//                    ExpenseId = expense.ExpenseId,
//                    UserId = expense.UserId,
//                    AmountPaid = expense.Amount,
//                    PaymentStatus = PaymentStatusEnum.Pending
//                };
//                await _paymentRepo.AddAsync(payment);
//                _logger.LogInformation("Created new payment {PaymentId} for Expense {ExpenseId}.", payment.PaymentId, expenseId);
//            }

//            // 5️⃣ Complete payment
//            payment.PaymentStatus = PaymentStatusEnum.Paid;
//            payment.PaymentDate = DateTime.Now;
//            payment.PaymentMode = paymentMode;
//            payment.ReferenceNo = referenceNo;

//            if (payment.User == null && payment.UserId != null)
//                payment.User = await _userRepo.GetByIdAsync(payment.UserId);

//            await _paymentRepo.UpdateAsync(payment.PaymentId, payment);
//            _logger.LogInformation("Payment {PaymentId} for Expense {ExpenseId} marked as Paid.", payment.PaymentId, expenseId);

//            // 6️⃣ Update expense status
//            expense.Status = ExpenseStatus.Paid;
//            await _expenseRepo.UpdateAsync(expense.ExpenseId, expense);
//            _logger.LogInformation("Expense {ExpenseId} status updated to Paid.", expenseId);

//            // 7️⃣ Notify employee
//            await _notificationService.CreateNotification(new CreateNotificationRequestDto
//            {
//                UserId = expense.UserId,
//                Message = $"Your expense '{expense.ExpenseId}' has been PAID successfully. Amount: ₹{expense.Amount:N2}",
//                Description = $"Reference No: {referenceNo} | Mode: {paymentMode}",
//                SenderRole = "System"
//            });

//            _logger.LogInformation("Notification sent to User {UserId} for Payment {PaymentId}.", expense.UserId, payment.PaymentId);

//            return MapToDto(payment);
//        }

//        // =====================================================
//        // 🔹 Get All Payments with Pagination
//        // =====================================================
//        public async Task<PagedResponse<CreatePaymentResponseDto>> GetAllPayments(PaginationParams paginationParams)
//        {
//            _logger.LogInformation("Fetching all payments. Page {Page}, Size {Size}", paginationParams.PageNumber, paginationParams.PageSize);

//            var allPayments = await _paymentRepo.GetAllAsync() ?? new List<Payment>();

//            foreach (var p in allPayments)
//            {
//                if (p.User == null && p.UserId != null)
//                    p.User = await _userRepo.GetByIdAsync(p.UserId);
//            }

//            var paymentsPage = allPayments
//                .OrderByDescending(p => p.PaymentDate)
//                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
//                .Take(paginationParams.PageSize)
//                .ToList();

//            var paymentDtos = paymentsPage
//                .Select(p => MapToDto(p))
//                .ToList();

//            _logger.LogInformation("Fetched {Count} payments out of {Total}", paymentDtos.Count, allPayments.Count());

//            return new PagedResponse<CreatePaymentResponseDto>(
//                paymentDtos,
//                allPayments.Count(),
//                paginationParams.PageNumber,
//                paginationParams.PageSize
//            );
//        }

//        // =====================================================
//        // 🔹 Get Payment by ExpenseId
//        // =====================================================
//        public async Task<CreatePaymentResponseDto?> GetPaymentByExpenseId(string expenseId, string userId, string role)
//        {
//            _logger.LogInformation("Fetching payment for Expense {ExpenseId} requested by User {UserId} ({Role})", expenseId, userId, role);

//            var allPayments = await _paymentRepo.GetAllAsync() ?? new List<Payment>();

//            var payment = allPayments
//                .Where(p => p.ExpenseId == expenseId)
//                .OrderByDescending(p => p.PaymentDate)
//                .FirstOrDefault();

//            if (payment == null)
//            {
//                _logger.LogWarning("No payment found for Expense {ExpenseId}", expenseId);
//                return null;
//            }

//            role = role?.Trim();

//            if ((role.Equals("Employee", StringComparison.OrdinalIgnoreCase) ||
//                 role.Equals("Manager", StringComparison.OrdinalIgnoreCase)) &&
//                payment.UserId != userId)
//            {
//                _logger.LogWarning("User {UserId} ({Role}) attempted to access payment {PaymentId} without permission", userId, role, payment.PaymentId);
//                return null;
//            }

//            if (payment.User == null && payment.UserId != null)
//                payment.User = await _userRepo.GetByIdAsync(payment.UserId);

//            return MapToDto(payment);
//        }

//        // =====================================================
//        // 🔹 Map Payment to DTO
//        // =====================================================
//        private CreatePaymentResponseDto MapToDto(Payment payment)
//        {
//            return new CreatePaymentResponseDto
//            {
//                PaymentId = payment.PaymentId,
//                UserId = payment.UserId ?? "",
//                UserName = payment.User?.UserName ?? "",
//                AmountPaid = payment.AmountPaid,
//                PaymentStatus = payment.PaymentStatus.ToString(),
//                PaymentMode = payment.PaymentMode ?? "",
//                ReferenceNo = payment.ReferenceNo ?? "",
//                PaymentDate = payment.PaymentDate ?? DateTime.Now,
//                AmountInRupees = payment.AmountPaid.ToString("C"),
//                ExpenseId = payment.ExpenseId
//            };
//        }

//        // =====================================================
//        // 🔹 Get User from Token
//        // =====================================================
//        private (string userId, string userName, UserRole role) GetUserFromToken()
//        {
//            var user = _httpContextAccessor.HttpContext?.User;

//            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
//                return ("Anonymous", "Anonymous", UserRole.Employee);

//            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
//            var userName = user.FindFirstValue(ClaimTypes.Name) ?? "Unknown";
//            var roleStr = user.FindFirstValue(ClaimTypes.Role) ?? "Employee";

//            Enum.TryParse<UserRole>(roleStr, out var role);

//            return (userId, userName, role);
//        }
//    }
//}

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
            var allExpenses = await _expenseRepo.GetAllAsync() ?? new List<Expense>(); // ✅ load expenses

            // Load users if not already included via navigation property
            foreach (var p in allPayments)
            {
                if (p.User == null && p.UserId != null)
                    p.User = await _userRepo.GetByIdAsync(p.UserId);
            }

            // Apply date, amount, and status filters
            var filtered = allPayments.AsEnumerable();

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
