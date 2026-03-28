

// FILE: Services/ApprovalService.cs — FULL FILE
// CHANGE: ManagerApproval and GetAllApprovals now map expense.DocumentUrls
//         and expense.Amount into the response DTO so the frontend can
//         display the uploaded image on the approval screen.
//         No new DTOs. No new models. Only mapping updated.

using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models;
using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;
using ReimbursementTrackerApp.Models.Enums;
using ReimbursementTrackerApp.Models.helper;

namespace ReimbursementTrackerApp.Services
{
    public class ApprovalService : IApprovalService
    {
        private readonly IRepository<string, Expense> _expenseRepo;
        private readonly IRepository<string, Approval> _approvalRepo;
        private readonly IRepository<string, User> _userRepo;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;

        public ApprovalService(
            IRepository<string, Expense> expenseRepo,
            IRepository<string, Approval> approvalRepo,
            IRepository<string, User> userRepo,
            INotificationService notificationService,
            IAuditLogService auditLogService)
        {
            _expenseRepo = expenseRepo;
            _approvalRepo = approvalRepo;
            _userRepo = userRepo;
            _notificationService = notificationService;
            _auditLogService = auditLogService;
        }

        // ======================================================
        // MANAGER APPROVAL / REJECTION
        // ✅ CHANGE: response now includes DocumentUrls + amount
        //            from the expense so the frontend shows the image.
        // ======================================================
        public async Task<CreateApprovalResponseDto?> ManagerApproval(CreateApprovalRequestDto request)
        {
            var expenses = await _expenseRepo.GetAllAsync();
            var expense = expenses?.FirstOrDefault(e => e.ExpenseId == request.ExpenseId);

            if (expense == null)
                throw new KeyNotFoundException($"Expense {request.ExpenseId} not found.");

            if (expense.Status != ExpenseStatus.Submitted)
                throw new InvalidOperationException("Expense must be in Submitted state.");

            // ✅ Self-approval restriction: a manager cannot approve their own expense
            if (expense.UserId == request.ManagerId)
                throw new InvalidOperationException("You cannot approve your own expense. Another manager must review it.");

            var approval = new Approval
            {
                ApprovalId = Guid.NewGuid().ToString(),
                ExpenseId = expense.ExpenseId,
                ManagerId = request.ManagerId,
                Level = "Manager",
                Comments = request.Comments,
                ApprovedAt = DateTime.Now
            };

            string notificationMessage;

            if (request.Status.Equals("approved", StringComparison.OrdinalIgnoreCase))
            {
                approval.Status = ApprovalStatus.Approved;
                expense.Status = ExpenseStatus.Approved;
                notificationMessage = $"Your expense {expense.ExpenseId} has been APPROVED by Manager.";
            }
            else if (request.Status.Equals("rejected", StringComparison.OrdinalIgnoreCase))
            {
                approval.Status = ApprovalStatus.Rejected;
                expense.Status = ExpenseStatus.Rejected;
                notificationMessage = $"Your expense {expense.ExpenseId} has been REJECTED by Manager.";
            }
            else
            {
                throw new ArgumentException("Invalid status. Must be 'approved' or 'rejected'.");
            }

            await _approvalRepo.AddAsync(approval);
            await _expenseRepo.UpdateAsync(expense.ExpenseId, expense);

            var users = await _userRepo.GetAllAsync();
            var managerUser = users?.FirstOrDefault(u => u.UserId == request.ManagerId);

            await _notificationService.CreateNotification(new CreateNotificationRequestDto
            {
                UserId = expense.UserId,
                Message = notificationMessage,
                Description = string.IsNullOrWhiteSpace(request.Comments)
                                  ? string.Empty
                                  : $"Manager comments: {request.Comments}",
                SenderRole = "System"
            });

            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
            {
                Action = $"Manager {approval.Status} expense {expense.ExpenseId}",
                ExpenseId = expense.ExpenseId,
                Amount = expense.Amount,
                Date = DateTime.UtcNow
            });

            return new CreateApprovalResponseDto
            {
                ApprovalId = approval.ApprovalId,
                ExpenseId = expense.ExpenseId,
                Status = approval.Status.ToString(),
                Comments = approval.Comments,
                Level = approval.Level,
                ApprovedAt = approval.ApprovedAt,
                ApproverName = managerUser?.UserName ?? "",

                // ✅ MAP image paths from expense — same files, no re-upload
                DocumentUrls = expense.DocumentUrls ?? new List<string>(),

                // ✅ MAP amount for display on approval screen
                ExpenseAmount = expense.Amount,
                AmountInRupees = CurrencyHelper.FormatRupees(expense.Amount)
            };
        }

        // ======================================================
        // GET ALL APPROVALS (Admin)
        // ✅ CHANGE: joins expense to get DocumentUrls + Amount
        //            so admin view also has image access.
        // ======================================================
        public async Task<PagedResponse<CreateApprovalResponseDto>> GetAllApprovals(PaginationParams paginationParams)
        {
            var approvals = await _approvalRepo.GetAllAsync();
            var users = await _userRepo.GetAllAsync();
            var expenses = await _expenseRepo.GetAllAsync();  // ✅ fetch expenses for image paths

            var approvalsList = approvals ?? new List<Approval>();
            var usersList = users ?? new List<User>();
            var expensesList = expenses ?? new List<Expense>();

            // Build fast lookup dictionaries (safe against duplicates)
            var userMap    = usersList.GroupBy(u => u.UserId).ToDictionary(g => g.Key, g => g.First().UserName);
            var expenseMap = expensesList.GroupBy(e => e.ExpenseId).ToDictionary(g => g.Key, g => g.First());

            // Map each approval to its expense and employee name
            var mapped = approvalsList
                .Join(usersList,
                    a => a.ManagerId,
                    u => u.UserId,
                    (a, u) => new { approval = a, ApproverName = u.UserName })
                .Select(x =>
                {
                    expenseMap.TryGetValue(x.approval.ExpenseId, out var expense);
                    var employeeName = expense != null && userMap.TryGetValue(expense.UserId, out var ename) ? ename : "";
                    return new
                    {
                        x.approval,
                        x.ApproverName,
                        expense,
                        EmployeeName = employeeName
                    };
                })
                .ToList();

            var query = mapped.AsEnumerable();

            var totalRecords = query.Count();

            // Filter by employee name (expense submitter) or approver name
            if (!string.IsNullOrWhiteSpace(paginationParams.UserName))
                query = query.Where(x =>
                    x.EmployeeName.Contains(paginationParams.UserName, StringComparison.OrdinalIgnoreCase) ||
                    x.ApproverName.Contains(paginationParams.UserName, StringComparison.OrdinalIgnoreCase));

            var filteredCount = query.Count();

            var pagedApprovals = query
                .OrderBy(x => x.approval.ApprovalId)
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .Select(x => new CreateApprovalResponseDto
                {
                    ApprovalId = x.approval.ApprovalId,
                    ExpenseId = x.approval.ExpenseId,
                    Status = x.approval.Status.ToString(),
                    Comments = x.approval.Comments,
                    Level = x.approval.Level,
                    ApprovedAt = x.approval.ApprovedAt,
                    ApproverName = x.ApproverName,
                    EmployeeName = x.EmployeeName,
                    DocumentUrls = x.expense?.DocumentUrls ?? new List<string>(),
                    ExpenseAmount = x.expense?.Amount ?? 0,
                    AmountInRupees = x.expense != null
                                     ? CurrencyHelper.FormatRupees(x.expense.Amount)
                                     : string.Empty
                })
                .ToList();

            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
            {
                Action = "Admin viewed approvals list",
                Date = DateTime.UtcNow
            });

            return new PagedResponse<CreateApprovalResponseDto>(
                pagedApprovals,
                filteredCount,
                paginationParams.PageNumber,
                paginationParams.PageSize
            );
        }
    }
}
