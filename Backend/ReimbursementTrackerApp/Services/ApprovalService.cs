using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models;
using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;
using ReimbursementTrackerApp.Models.Enums;
using ReimbursementTrackerApp.Models.helper;

namespace ReimbursementTrackerApp.Services
{
    /// <summary>
    /// Implements the multi-stage approval workflow:
    ///
    ///   Employee  → TeamLead → Manager → Finance
    ///   TeamLead  → Manager  → Finance
    ///   Manager   → Finance  (direct, no TeamLead stage)
    ///   Finance   → Finance  (self-submitted, Finance approves internally — or Admin)
    /// </summary>
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
        // TEAM LEAD APPROVAL
        // Handles: Employee → TeamLead stage
        // After approval → moves to PendingManager
        // ======================================================
        public async Task<CreateApprovalResponseDto?> TeamLeadApproval(CreateApprovalRequestDto request)
        {
            var (expense, users) = await LoadExpenseAndUsers(request.ExpenseId);

            if (expense.Status != ExpenseStatus.PendingTeamLead)
                throw new InvalidOperationException(
                    $"Expense is not awaiting Team Lead approval. Current status: {expense.Status}.");

            var approver = users.FirstOrDefault(u => u.UserId == request.ManagerId)
                ?? throw new KeyNotFoundException($"Approver {request.ManagerId} not found.");

            if (approver.Role != UserRole.TeamLead)
                throw new InvalidOperationException("Only a Team Lead can approve at this stage.");

            if (expense.UserId == request.ManagerId)
                throw new InvalidOperationException("You cannot approve your own expense.");

            // Enforce same-department: TeamLead must be in same dept as expense owner
            var expenseOwner = users.FirstOrDefault(u => u.UserId == expense.UserId);
            if (expenseOwner?.Department != null && approver.Department != expenseOwner.Department)
                throw new InvalidOperationException(
                    $"You can only approve expenses from your own department ({approver.Department}).");

            var approval = CreateApprovalRecord(request, ApprovalStage.TeamLead, "TeamLead");
            bool isApproved = ParseStatus(request.Status);
            approval.Status = isApproved ? ApprovalStatus.Approved : ApprovalStatus.Rejected;

            if (isApproved)
            {
                expense.Status = ExpenseStatus.PendingManager;
                // Notify the same-department Manager
                var deptManager = users.FirstOrDefault(u =>
                    u.Role == UserRole.Manager && u.Department == approver.Department);
                if (deptManager != null)
                    await Notify(deptManager.UserId,
                        $"Expense '{expense.ExpenseId}' approved by Team Lead {approver.UserName}, awaiting your review.",
                        $"Amount: ₹{expense.Amount:N2} | Category: {expense.CategoryName} | Employee: {expenseOwner?.UserName}");
            }
            else
            {
                expense.Status = ExpenseStatus.Rejected;
            }

            await SaveApprovalAndExpense(approval, expense);
            await NotifySubmitter(expense, users, approval.Status, "Team Lead", request.Comments);
            await LogAction($"TeamLead {approval.Status} expense {expense.ExpenseId}", expense);

            return BuildResponse(approval, expense, approver.UserName, users);
        }

        // ======================================================
        // MANAGER APPROVAL
        // Handles:
        //   - Employee flow: PendingManager → PendingFinance
        //   - TeamLead flow: PendingManager → PendingFinance
        //   - Manager self-submitted: handled by AdminApproval
        // ======================================================
        public async Task<CreateApprovalResponseDto?> ManagerApproval(CreateApprovalRequestDto request)
        {
            var (expense, users) = await LoadExpenseAndUsers(request.ExpenseId);

            if (expense.Status != ExpenseStatus.PendingManager)
                throw new InvalidOperationException(
                    $"Expense is not awaiting Manager approval. Current status: {expense.Status}.");

            var expenseOwner = users.FirstOrDefault(u => u.UserId == expense.UserId);
            if (expenseOwner != null &&
                (expenseOwner.Role == UserRole.Manager || expenseOwner.Role == UserRole.Finance))
                throw new InvalidOperationException(
                    "Expenses by Manager or Finance must be approved by Admin.");

            if (expense.UserId == request.ManagerId)
                throw new InvalidOperationException("You cannot approve your own expense.");

            var approver = users.FirstOrDefault(u => u.UserId == request.ManagerId)
                ?? throw new KeyNotFoundException($"Approver {request.ManagerId} not found.");

            if (approver.Role != UserRole.Manager)
                throw new InvalidOperationException("Only a Manager can approve at this stage.");

            // Enforce same-department
            if (expenseOwner?.Department != null && approver.Department != expenseOwner.Department)
                throw new InvalidOperationException(
                    $"You can only approve expenses from your own department ({approver.Department}).");

            var approval = CreateApprovalRecord(request, ApprovalStage.Manager, "Manager");
            bool isApproved = ParseStatus(request.Status);
            approval.Status = isApproved ? ApprovalStatus.Approved : ApprovalStatus.Rejected;

            if (isApproved)
            {
                expense.Status = ExpenseStatus.PendingFinance;
                var finance = users.FirstOrDefault(u => u.Role == UserRole.Finance);
                if (finance != null)
                    await Notify(finance.UserId,
                        $"Expense '{expense.ExpenseId}' (₹{expense.Amount:N2}) approved by Manager {approver.UserName}, ready for Finance review.",
                        $"Category: {expense.CategoryName} | Employee: {expenseOwner?.UserName} | Dept: {approver.Department}");
            }
            else
            {
                expense.Status = ExpenseStatus.Rejected;
            }

            await SaveApprovalAndExpense(approval, expense);
            await NotifySubmitter(expense, users, approval.Status, "Manager", request.Comments);
            await LogAction($"Manager {approval.Status} expense {expense.ExpenseId}", expense);

            return BuildResponse(approval, expense, approver.UserName, users);
        }

        // ======================================================
        // FINANCE APPROVAL / MARK AS PAID
        // Handles: PendingFinance → Approved (ready for payment)
        // ======================================================
        public async Task<CreateApprovalResponseDto?> FinanceApproval(CreateApprovalRequestDto request)
        {
            var (expense, users) = await LoadExpenseAndUsers(request.ExpenseId);

            if (expense.Status != ExpenseStatus.PendingFinance)
                throw new InvalidOperationException(
                    $"Expense is not awaiting Finance approval. Current status: {expense.Status}.");

            var approver = users.FirstOrDefault(u => u.UserId == request.ManagerId)
                ?? throw new KeyNotFoundException($"Approver {request.ManagerId} not found.");

            if (approver.Role != UserRole.Finance)
                throw new InvalidOperationException("Only Finance team can approve at this stage.");

            var approval = CreateApprovalRecord(request, ApprovalStage.Finance, "Finance");
            bool isApproved = ParseStatus(request.Status);
            approval.Status = isApproved ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
            expense.Status = isApproved ? ExpenseStatus.Approved : ExpenseStatus.Rejected;

            await SaveApprovalAndExpense(approval, expense);
            await NotifySubmitter(expense, users, approval.Status, "Finance", request.Comments);
            await LogAction($"Finance {approval.Status} expense {expense.ExpenseId}", expense);

            return BuildResponse(approval, expense, approver.UserName, users);
        }

        // ======================================================
        // ADMIN APPROVAL — for Manager/Finance-submitted expenses
        // ======================================================
        public async Task<CreateApprovalResponseDto?> AdminApproval(CreateApprovalRequestDto request)
        {
            var (expense, users) = await LoadExpenseAndUsers(request.ExpenseId);

            if (expense.Status != ExpenseStatus.PendingAdmin)
                throw new InvalidOperationException(
                    $"Expense is not awaiting Admin approval. Current status: {expense.Status}.");

            var expenseOwner = users.FirstOrDefault(u => u.UserId == expense.UserId);
            if (expenseOwner == null ||
                (expenseOwner.Role != UserRole.Manager && expenseOwner.Role != UserRole.Finance))
                throw new InvalidOperationException(
                    "Admin approval is only for expenses created by Manager or Finance roles.");

            var approval = CreateApprovalRecord(request, ApprovalStage.Manager, "Admin");
            bool isApproved = ParseStatus(request.Status);
            approval.Status = isApproved ? ApprovalStatus.Approved : ApprovalStatus.Rejected;

            if (isApproved)
            {
                expense.Status = ExpenseStatus.PendingFinance;
                var finance = users.FirstOrDefault(u => u.Role == UserRole.Finance);
                if (finance != null)
                    await Notify(finance.UserId,
                        $"Expense '{expense.ExpenseId}' (₹{expense.Amount:N2}) approved by Admin, ready for payment.",
                        $"Category: {expense.CategoryName} | Submitted by: {expenseOwner?.UserName} ({expenseOwner?.Role})");
            }
            else
            {
                expense.Status = ExpenseStatus.Rejected;
            }

            await SaveApprovalAndExpense(approval, expense);
            var adminUser = users.FirstOrDefault(u => u.UserId == request.ManagerId);
            await NotifySubmitter(expense, users, approval.Status, "Admin", request.Comments);
            await LogAction($"Admin {approval.Status} expense {expense.ExpenseId}", expense);

            return BuildResponse(approval, expense, adminUser?.UserName ?? "Admin", users);
        }

        // ======================================================
        // GET ALL APPROVALS (paginated)
        // ======================================================
        public async Task<PagedResponse<CreateApprovalResponseDto>> GetAllApprovals(PaginationParams paginationParams)
        {
            var approvals = (await _approvalRepo.GetAllAsync()) ?? new List<Approval>();
            var users = (await _userRepo.GetAllAsync()) ?? new List<User>();
            var expenses = (await _expenseRepo.GetAllAsync()) ?? new List<Expense>();

            var userMap = users.GroupBy(u => u.UserId).ToDictionary(g => g.Key, g => g.First().UserName);
            var expenseMap = expenses.GroupBy(e => e.ExpenseId).ToDictionary(g => g.Key, g => g.First());

            var query = approvals.Select(a =>
            {
                expenseMap.TryGetValue(a.ExpenseId, out var exp);
                userMap.TryGetValue(a.ManagerId, out var approverName);
                var employeeName = exp != null && userMap.TryGetValue(exp.UserId, out var en) ? en : "";
                return new { approval = a, exp, approverName = approverName ?? "", employeeName };
            }).AsEnumerable();

            if (!string.IsNullOrWhiteSpace(paginationParams.UserName))
                query = query.Where(x =>
                    x.employeeName.Contains(paginationParams.UserName, StringComparison.OrdinalIgnoreCase) ||
                    x.approverName.Contains(paginationParams.UserName, StringComparison.OrdinalIgnoreCase));

            var filteredCount = query.Count();

            var paged = query
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
                    ApproverName = x.approverName,
                    EmployeeName = x.employeeName,
                    DocumentUrls = x.exp?.DocumentUrls ?? new List<string>(),
                    ExpenseAmount = x.exp?.Amount ?? 0,
                    AmountInRupees = x.exp != null ? CurrencyHelper.FormatRupees(x.exp.Amount) : string.Empty
                })
                .ToList();

            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
            {
                Action = "Viewed approvals list",
                Date = DateTime.UtcNow
            });

            return new PagedResponse<CreateApprovalResponseDto>(
                paged, filteredCount, paginationParams.PageNumber, paginationParams.PageSize);
        }

        // ======================================================
        // GET MY APPROVAL HISTORY
        // Returns all approvals where ManagerId = approverId
        // ======================================================
        public async Task<List<CreateApprovalResponseDto>> GetMyApprovalHistory(string approverId)
        {
            var approvals = (await _approvalRepo.GetAllAsync()) ?? new List<Approval>();
            var users     = (await _userRepo.GetAllAsync())?.ToList() ?? new List<User>();
            var expenses  = (await _expenseRepo.GetAllAsync()) ?? new List<Expense>();

            var userMap    = users.GroupBy(u => u.UserId).ToDictionary(g => g.Key, g => g.First().UserName);
            var expenseMap = expenses.GroupBy(e => e.ExpenseId).ToDictionary(g => g.Key, g => g.First());

            return approvals
                .Where(a => a.ManagerId == approverId)
                .OrderByDescending(a => a.ApprovedAt)
                .Select(a =>
                {
                    expenseMap.TryGetValue(a.ExpenseId, out var exp);
                    var employeeName = exp != null && userMap.TryGetValue(exp.UserId, out var en) ? en : "";
                    userMap.TryGetValue(a.ManagerId, out var approverName);
                    return new CreateApprovalResponseDto
                    {
                        ApprovalId    = a.ApprovalId,
                        ExpenseId     = a.ExpenseId,
                        Status        = a.Status.ToString(),
                        Comments      = a.Comments,
                        Level         = a.Level,
                        ApprovedAt    = a.ApprovedAt,
                        ApproverName  = approverName ?? "",
                        EmployeeName  = employeeName,
                        DocumentUrls  = exp?.DocumentUrls ?? new List<string>(),
                        ExpenseAmount = exp?.Amount ?? 0,
                        AmountInRupees = exp != null ? CurrencyHelper.FormatRupees(exp.Amount) : string.Empty
                    };
                })
                .ToList();
        }
        public async Task<List<CreateApprovalResponseDto>> GetPendingApprovalsForMe(string approverId)
        {
            var users = (await _userRepo.GetAllAsync())?.ToList() ?? new List<User>();
            var approver = users.FirstOrDefault(u => u.UserId == approverId)
                ?? throw new KeyNotFoundException("Approver not found.");

            var expenses = (await _expenseRepo.GetAllAsync()) ?? new List<Expense>();
            var userMap = users.GroupBy(u => u.UserId).ToDictionary(g => g.Key, g => g.First());

            // Same-dept user IDs for TeamLead and Manager scoping
            var deptUserIds = users
                .Where(u => u.Department == approver.Department)
                .Select(u => u.UserId)
                .ToHashSet();

            var pendingStatuses = approver.Role switch
            {
                UserRole.TeamLead => new[] { ExpenseStatus.PendingTeamLead },
                UserRole.Manager  => new[] { ExpenseStatus.PendingManager },
                UserRole.Finance  => new[] { ExpenseStatus.PendingFinance },
                UserRole.Admin    => new[] { ExpenseStatus.PendingAdmin },
                _                 => Array.Empty<ExpenseStatus>()
            };

            return expenses
                .Where(e =>
                    pendingStatuses.Contains(e.Status) &&
                    e.UserId != approverId &&
                    // TeamLead and Manager only see their own department; Finance and Admin see all
                    (approver.Role == UserRole.Finance || approver.Role == UserRole.Admin ||
                     deptUserIds.Contains(e.UserId)))
                .Select(e =>
                {
                    userMap.TryGetValue(e.UserId, out var owner);
                    return new CreateApprovalResponseDto
                    {
                        ExpenseId = e.ExpenseId,
                        Status = e.Status.ToString(),
                        EmployeeName = owner?.UserName ?? "",
                        DocumentUrls = e.DocumentUrls ?? new List<string>(),
                        ExpenseAmount = e.Amount,
                        AmountInRupees = CurrencyHelper.FormatRupees(e.Amount),
                        Level = approver.Role.ToString()
                    };
                })
                .ToList();
        }

        // ======================================================
        // PRIVATE HELPERS
        // ======================================================

        private async Task<(Expense expense, List<User> users)> LoadExpenseAndUsers(string expenseId)
        {
            var expenses = await _expenseRepo.GetAllAsync();
            var expense = expenses?.FirstOrDefault(e => e.ExpenseId == expenseId)
                ?? throw new KeyNotFoundException($"Expense {expenseId} not found.");
            var users = (await _userRepo.GetAllAsync())?.ToList() ?? new List<User>();
            return (expense, users);
        }

        private static Approval CreateApprovalRecord(
            CreateApprovalRequestDto request, ApprovalStage stage, string level) => new()
        {
            ApprovalId = Guid.NewGuid().ToString(),
            ExpenseId = request.ExpenseId,
            ManagerId = request.ManagerId,
            Stage = stage,
            Level = level,
            Comments = request.Comments,
            ApprovedAt = DateTime.Now
        };

        private static bool ParseStatus(string status)
        {
            if (status.Equals("approved", StringComparison.OrdinalIgnoreCase)) return true;
            if (status.Equals("rejected", StringComparison.OrdinalIgnoreCase)) return false;
            throw new ArgumentException("Invalid status. Must be 'approved' or 'rejected'.");
        }

        private async Task SaveApprovalAndExpense(Approval approval, Expense expense)
        {
            await _approvalRepo.AddAsync(approval);
            await _expenseRepo.UpdateAsync(expense.ExpenseId, expense);
        }

        private async Task Notify(string userId, string message, string description)
        {
            try
            {
                await _notificationService.CreateNotification(new CreateNotificationRequestDto
                {
                    UserId = userId,
                    Message = message,
                    Description = description,
                    SenderRole = "System"
                });
            }
            catch { /* notification failure must not block approval */ }
        }

        private async Task NotifySubmitter(
            Expense expense, List<User> users, ApprovalStatus status, string approverRole, string comments)
        {
            var verb = status == ApprovalStatus.Approved ? "APPROVED" : "REJECTED";
            var msg = $"Your expense {expense.ExpenseId} has been {verb} by {approverRole}.";
            var desc = string.IsNullOrWhiteSpace(comments) ? string.Empty : $"{approverRole} comments: {comments}";
            await Notify(expense.UserId, msg, desc);
        }

        private async Task LogAction(string action, Expense expense)
        {
            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
            {
                Action = action,
                ExpenseId = expense.ExpenseId,
                Amount = expense.Amount,
                Date = DateTime.UtcNow
            });
        }

        private static CreateApprovalResponseDto BuildResponse(
            Approval approval, Expense expense, string approverName, List<User> users)
        {
            var owner = users.FirstOrDefault(u => u.UserId == expense.UserId);
            return new CreateApprovalResponseDto
            {
                ApprovalId = approval.ApprovalId,
                ExpenseId = expense.ExpenseId,
                Status = approval.Status.ToString(),
                Comments = approval.Comments,
                Level = approval.Level,
                ApprovedAt = approval.ApprovedAt,
                ApproverName = approverName,
                EmployeeName = owner?.UserName ?? "",
                DocumentUrls = expense.DocumentUrls ?? new List<string>(),
                ExpenseAmount = expense.Amount,
                AmountInRupees = CurrencyHelper.FormatRupees(expense.Amount)
            };
        }
    }
}
