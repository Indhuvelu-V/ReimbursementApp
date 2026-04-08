

using Microsoft.AspNetCore.Http;
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models;
using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;
using ReimbursementTrackerApp.Models.Enums;
using ReimbursementTrackerApp.Models.helper;
using System.Security.Claims;

namespace ReimbursementTrackerApp.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly IRepository<string, Expense> _expenseRepo;
        private readonly IRepository<string, ExpenseCategory> _categoryRepo;
        private readonly IRepository<string, User> _userRepo;
        private readonly IRepository<string, Approval> _approvalRepo;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFileUploadService _fileUploadService;
        private readonly INotificationService _notificationService;

        public ExpenseService(
            IRepository<string, Expense> expenseRepo,
            IRepository<string, ExpenseCategory> categoryRepo,
            IRepository<string, User> userRepo,
            IRepository<string, Approval> approvalRepo,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            IFileUploadService fileUploadService,
            INotificationService notificationService)
        {
            _approvalRepo = approvalRepo;
            _expenseRepo = expenseRepo;
            _categoryRepo = categoryRepo;
            _userRepo = userRepo;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
            _fileUploadService = fileUploadService;
            _notificationService = notificationService;
        }

        // =====================================================
        // GET USER FROM TOKEN — unchanged
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

        // =====================================================
        // ROLE-BASED MONTHLY LIMITS
        //   Employee  : max 2 requests, max ₹5,000 total/month
        //   TeamLead  : max 5 requests, max ₹10,000 total/month
        //   Manager   : unlimited requests, max ₹20,000 total/month
        //               + may submit advance (future-dated) requests
        //   Finance/Admin: no monthly count/amount restrictions
        // =====================================================
        private static (int? maxRequests, decimal? maxAmount) GetRoleLimits(UserRole role) => role switch
        {
            UserRole.Employee => (2, 5000m),
            UserRole.TeamLead => (5, 10000m),
            UserRole.Manager  => (null, 20000m),   // null = unlimited count
            UserRole.Finance  => (null, 30000m),   // unlimited count, ₹30,000/month
            UserRole.Admin    => (null, 30000m),   // unlimited count, ₹30,000/month
            _                 => (null, null)
        };

        // =====================================================
        // CREATE EXPENSE
        //
        // ✅ Role-based monthly request count + amount limits
        // ✅ Managers may submit future-dated (advance) requests
        // ✅ Rejected expense for same month → UPDATE in-place
        // ✅ FILE UPLOAD — paths already saved by controller
        // =====================================================
        public async Task<CreateExpenseResponseDto?> CreateExpense(CreateExpenseRequestDto request)
        {
            var (userId, userName, role) = GetUserFromToken();

            var today = DateTime.UtcNow;
            var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd   = monthStart.AddMonths(1).AddTicks(-1);

            // ── Advance request: only Managers may submit future-dated expenses ──
            bool isAdvance = request.ExpenseDate > monthEnd;
            if (isAdvance && role != UserRole.Manager)
                throw new InvalidOperationException(
                    "Only Managers are allowed to submit advance (future-dated) reimbursement requests.");

            // ── Non-advance: date must be within current month ────────────────
            if (!isAdvance && (request.ExpenseDate < monthStart || request.ExpenseDate > monthEnd))
                throw new InvalidOperationException(
                    $"Expense date must be within the current month " +
                    $"({monthStart:dd MMM yyyy} – {monthEnd:dd MMM yyyy}). " +
                    $"Only Managers may submit advance requests.");

            var documentUrls = request.DocumentUrls ?? new List<string>();

            // ── Fetch category ────────────────────────────────────────────────
            ExpenseCategory category;
            try { category = await _categoryRepo.GetByIdAsync(request.CategoryId) ?? throw new KeyNotFoundException("Expense category not found."); }
            catch (KeyNotFoundException) { throw new KeyNotFoundException("Expense category not found."); }

            if (request.Amount > category.MaxLimit)
                throw new InvalidOperationException($"Amount exceeds the category limit of ₹{category.MaxLimit:N2} for {category.CategoryName}.");

            // ── Role-based monthly limits ─────────────────────────────────────
            var (maxRequests, maxAmount) = GetRoleLimits(role);
            var allExpenses = await _expenseRepo.GetAllAsync() ?? new List<Expense>();

            // Count active requests this month — exclude Draft and Rejected
            // (Draft = not yet submitted, Rejected = doesn't count against limit)
            var monthlyExpenses = allExpenses
                .Where(e => e.UserId == userId &&
                            e.ExpenseDate >= monthStart &&
                            e.ExpenseDate <= monthEnd &&
                            e.Status != ExpenseStatus.Rejected &&
                            e.Status != ExpenseStatus.Draft)
                .ToList();

            if (maxRequests.HasValue && monthlyExpenses.Count >= maxRequests.Value)
                throw new InvalidOperationException(
                    $"You have reached the monthly request limit of {maxRequests.Value} for your role ({role}).");

            if (maxAmount.HasValue)
            {
                var monthlyTotal = monthlyExpenses.Sum(e => e.Amount);
                if (monthlyTotal + request.Amount > maxAmount.Value)
                    throw new InvalidOperationException(
                        $"This request would exceed your monthly reimbursement limit of ₹{maxAmount.Value:N2} for role {role}. " +
                        $"Current total: ₹{monthlyTotal:N2}, Requested: ₹{request.Amount:N2}.");
            }

            // ── RULE: If a REJECTED expense exists for this month → UPDATE it ─
            var rejectedThisMonth = allExpenses.FirstOrDefault(e =>
                e.UserId == userId &&
                e.ExpenseDate >= monthStart &&
                e.ExpenseDate <= monthEnd &&
                e.Status == ExpenseStatus.Rejected);

            if (rejectedThisMonth != null)
            {
                var oldAmount = rejectedThisMonth.Amount;
                var oldDocs   = rejectedThisMonth.DocumentUrls ?? new List<string>();

                rejectedThisMonth.CategoryId    = category.CategoryId;
                rejectedThisMonth.CategoryName  = category.CategoryName.ToString();
                rejectedThisMonth.Amount        = request.Amount;
                rejectedThisMonth.ExpenseDate   = request.ExpenseDate;
                rejectedThisMonth.DocumentUrls  = documentUrls;
                rejectedThisMonth.Status        = ExpenseStatus.Draft;
                rejectedThisMonth.IsAdvanceRequest = isAdvance;

                await _expenseRepo.UpdateAsync(rejectedThisMonth.ExpenseId, rejectedThisMonth);
                await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
                {
                    Action = $"Re-edited rejected expense {rejectedThisMonth.ExpenseId}",
                    ExpenseId = rejectedThisMonth.ExpenseId,
                    Amount = rejectedThisMonth.Amount,
                    OldAmount = oldAmount,
                    DocumentUrls = rejectedThisMonth.DocumentUrls,
                    OldDocumentUrls = oldDocs,
                    Date = DateTime.UtcNow
                });
                return MapToDto(rejectedThisMonth);
            }

            // ── Create new expense ────────────────────────────────────────────
            var expense = new Expense
            {
                ExpenseId  = Guid.NewGuid().ToString(),
                UserId     = userId,
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName.ToString(),
                Amount     = request.Amount,
                ExpenseDate = request.ExpenseDate,
                DocumentUrls = documentUrls,
                Status     = ExpenseStatus.Draft,
                IsAdvanceRequest = isAdvance
            };

            await _expenseRepo.AddAsync(expense);
            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
            {
                Action = $"Created expense {expense.ExpenseId}" + (isAdvance ? " [ADVANCE]" : ""),
                ExpenseId = expense.ExpenseId,
                Amount = expense.Amount,
                DocumentUrls = expense.DocumentUrls,
                Date = DateTime.UtcNow
            });

            return MapToDto(expense);
        }

        // =====================================================
        // UPDATE EXPENSE
        //
        // ✅ CHANGED: Now also allows editing Rejected expenses
        //    (previously only Draft/Submitted were allowed).
        //    Rejected expenses can be edited before resubmission.
        // ✅ FILE UPLOAD: new files are appended to existing ones.
        // =====================================================
        public async Task<(bool, string, CreateExpenseResponseDto?)> UpdateExpenseSafe(
            string expenseId, CreateExpenseRequestDto dto)
        {
            var (userId, userName, role) = GetUserFromToken();

            Expense? existingExpense;
            try { existingExpense = await _expenseRepo.GetByIdAsync(expenseId); }
            catch (KeyNotFoundException) { return (false, "Expense not found.", null); }
            if (existingExpense == null) return (false, "Expense not found.", null);

            if (existingExpense.UserId != userId && role != UserRole.Admin)
                return (false, "Not authorized.", null);

            // ✅ Rejected is now allowed (user edits after rejection)
            if (existingExpense.Status != ExpenseStatus.Draft &&
                existingExpense.Status != ExpenseStatus.Submitted &&
                existingExpense.Status != ExpenseStatus.Rejected)
                return (false, "Only Draft, Submitted, or Rejected expenses can be edited.", null);

            var oldAmount = existingExpense.Amount;
            var oldDocs = existingExpense.DocumentUrls ?? new List<string>();

            // ── Build final document list ─────────────────────────────────────
            // Frontend sends:
            //   DocumentUrls = kept existing files (or "__EMPTY__" if all deleted)
            //   Documents    = new IFormFile uploads (controller saves them and appends paths)
            //
            // Strip the sentinel, then use whatever remains as the final list.
            var rawSent = dto.DocumentUrls?.ToList();

            List<string> documentUrls;
            if (rawSent == null)
            {
                // Frontend sent nothing at all — keep old docs unchanged
                documentUrls = new List<string>(oldDocs);
            }
            else
            {
                // Remove sentinel, keep real paths only
                documentUrls = rawSent
                    .Where(u => u != "__EMPTY__")
                    .Distinct()
                    .ToList();
            }

            Console.WriteLine($"[UpdateExpense] rawSent=[{string.Join(", ", rawSent ?? new List<string>())}]");
            Console.WriteLine($"[UpdateExpense] oldDocs=[{string.Join(", ", oldDocs)}]");
            Console.WriteLine($"[UpdateExpense] finalDocs=[{string.Join(", ", documentUrls)}]");

            // ── Resolve category name ─────────────────────────────────────────
            string resolvedCategoryName = dto.CategoryName;
            if (string.IsNullOrWhiteSpace(resolvedCategoryName))
            {
                try
                {
                    var allCategories = (await _categoryRepo.GetAllAsync())?.ToList() ?? new List<ExpenseCategory>();
                    var matchedCategory = allCategories.FirstOrDefault(c => c.CategoryId == dto.CategoryId);
                    resolvedCategoryName = matchedCategory?.CategoryName.ToString()
                        ?? existingExpense.CategoryName
                        ?? dto.CategoryId;
                }
                catch { resolvedCategoryName = existingExpense.CategoryName ?? dto.CategoryId; }
            }
            if (string.IsNullOrWhiteSpace(resolvedCategoryName))
                resolvedCategoryName = dto.CategoryId;

            existingExpense.Amount = dto.Amount;
            existingExpense.CategoryId = dto.CategoryId;
            existingExpense.CategoryName = resolvedCategoryName;
            existingExpense.ExpenseDate = dto.ExpenseDate;
            existingExpense.DocumentUrlsJson = System.Text.Json.JsonSerializer.Serialize(documentUrls);

            await _expenseRepo.UpdateAsync(expenseId, existingExpense);

            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
            {
                Action = $"Updated Expense {expenseId}",
                ExpenseId = expenseId,
                Amount = existingExpense.Amount,
                OldAmount = oldAmount,
                DocumentUrls = documentUrls,
                OldDocumentUrls = oldDocs,
                Date = DateTime.UtcNow
            });

            return (true, "Updated successfully", MapToDto(existingExpense));
        }

        // =====================================================
        // DELETE EXPENSE — unchanged
        // =====================================================
        public async Task<(bool, string, CreateExpenseResponseDto?)> DeleteExpenseSafe(string expenseId)
        {
            var (userId, userName, role) = GetUserFromToken();

            Expense? expense;
            try { expense = await _expenseRepo.GetByIdAsync(expenseId); }
            catch (KeyNotFoundException) { return (false, "Not found", null); }
            if (expense == null) return (false, "Not found", null);

            if (expense.UserId != userId && role != UserRole.Admin)
                return (false, "Not authorized", null);

            if (expense.Status != ExpenseStatus.Draft &&
                expense.Status != ExpenseStatus.Submitted)
                return (false, "Cannot delete after manager approved or rejected", null);

            var response = MapToDto(expense);

            await _expenseRepo.DeleteAsync(expenseId);

            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
            {
                Action = $"Deleted Expense {expenseId}",
                ExpenseId = expenseId,
                Amount = expense.Amount,
                DocumentUrls = expense.DocumentUrls,
                Date = DateTime.UtcNow
            });

            return (true, "Deleted successfully", response);
        }

        // =====================================================
        // GET BY ID — unchanged
        // =====================================================
        public async Task<CreateExpenseResponseDto?> GetExpenseById(string expenseId)
        {
            var (userId, userName, role) = GetUserFromToken();

            Expense? expense;
            try { expense = await _expenseRepo.GetByIdAsync(expenseId); }
            catch (KeyNotFoundException) { return null; }
            if (expense == null) return null;

            if (role == UserRole.Employee && expense.UserId != userId)
                return null;

            return MapToDto(expense);
        }

        // =====================================================
        // GET ALL — unchanged
        // =====================================================
        public async Task<PagedResponse<CreateExpenseResponseDto>> GetAllExpenses(PaginationParams paginationParams)
        {
            var (userId, userName, role) = GetUserFromToken();

            var expenses = await _expenseRepo.GetAllAsync() ?? new List<Expense>();
            var users = await _userRepo.GetAllAsync() ?? new List<User>();

            // Build user name lookup
            var userMap = users.GroupBy(u => u.UserId).ToDictionary(g => g.Key, g => g.First().UserName);
            var userRoleMap = users.GroupBy(u => u.UserId).ToDictionary(g => g.Key, g => g.First().Role.ToString());

            var query = expenses.AsEnumerable();

            if (role == UserRole.Employee)
                query = query.Where(e => e.UserId == userId);
            else if (role == UserRole.TeamLead)
            {
                // TeamLead sees same-dept employees' expenses + their own
                var tlUser = users.FirstOrDefault(u => u.UserId == userId);
                if (tlUser != null)
                {
                    var deptUserIds = users
                        .Where(u => u.Department == tlUser.Department)
                        .Select(u => u.UserId)
                        .ToHashSet();
                    query = query.Where(e => deptUserIds.Contains(e.UserId));
                }
            }
            else if (role == UserRole.Manager)
            {
                // Manager sees expenses from same-department users (not their own)
                var managerUser = users.FirstOrDefault(u => u.UserId == userId);
                if (managerUser != null)
                {
                    var deptUserIds = users
                        .Where(u => u.Department == managerUser.Department && u.UserId != userId)
                        .Select(u => u.UserId)
                        .ToHashSet();
                    query = query.Where(e => deptUserIds.Contains(e.UserId));
                }
            }

            // Username filter
            if (!string.IsNullOrWhiteSpace(paginationParams.UserName))
            {
                var q = paginationParams.UserName.ToLower();
                query = query.Where(e =>
                    (userMap.TryGetValue(e.UserId, out var uname) && uname.ToLower().Contains(q)));
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(paginationParams.Status))
                query = query.Where(e => e.Status.ToString().Equals(paginationParams.Status, StringComparison.OrdinalIgnoreCase));

            // Date filters
            if (!string.IsNullOrWhiteSpace(paginationParams.FromDate) &&
                DateTime.TryParse(paginationParams.FromDate, out var fromDate))
                query = query.Where(e => e.ExpenseDate.Date >= fromDate.Date);

            if (!string.IsNullOrWhiteSpace(paginationParams.ToDate) &&
                DateTime.TryParse(paginationParams.ToDate, out var toDate))
                query = query.Where(e => e.ExpenseDate.Date <= toDate.Date);

            // Amount filters
            if (paginationParams.MinAmount.HasValue)
                query = query.Where(e => e.Amount >= paginationParams.MinAmount.Value);

            if (paginationParams.MaxAmount.HasValue)
                query = query.Where(e => e.Amount <= paginationParams.MaxAmount.Value);

            var filtered = query.OrderByDescending(e => e.ExpenseDate).ToList();
            var total = filtered.Count;

            var data = filtered
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .Select(e =>
                {
                    userMap.TryGetValue(e.UserId, out var ownerName);
                    userRoleMap.TryGetValue(e.UserId, out var ownerRole);
                    return MapToDto(e, ownerName, ownerRole);
                })
                .ToList();

            return new PagedResponse<CreateExpenseResponseDto>(
                data, total, paginationParams.PageNumber, paginationParams.PageSize);
        }

        // =====================================================
        // GET MY EXPENSES — includes ALL stage approval comments
        // =====================================================
        public async Task<List<CreateExpenseResponseDto>> GetMyExpenses()
        {
            var (userId, userName, role) = GetUserFromToken();

            var expenses = await _expenseRepo.GetAllAsync() ?? new List<Expense>();
            var approvals = await _approvalRepo.GetAllAsync() ?? new List<Approval>();
            var users = await _userRepo.GetAllAsync() ?? new List<User>();

            var userMap = users.GroupBy(u => u.UserId).ToDictionary(g => g.Key, g => g.First().UserName);

            // Group all approvals per expense, ordered by stage
            var approvalsByExpense = approvals
                .GroupBy(a => a.ExpenseId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.ApprovedAt).ToList());

            return expenses
                .Where(e => e.UserId == userId)
                .Select(e =>
                {
                    var dto = MapToDto(e, userName);

                    if (approvalsByExpense.TryGetValue(e.ExpenseId, out var expApprovals) && expApprovals.Any())
                    {
                        // Build a combined comment from all stages that have comments
                        var commentParts = expApprovals
                            .Where(a => !string.IsNullOrWhiteSpace(a.Comments))
                            .Select(a =>
                            {
                                userMap.TryGetValue(a.ManagerId ?? "", out var approverName);
                                var name = approverName ?? a.Level;
                                return $"{name} ({a.Level}): {a.Comments}";
                            })
                            .ToList();

                        dto.ApprovalComment = commentParts.Any()
                            ? string.Join(" | ", commentParts)
                            : string.Empty;

                        // ApproverName = most recent approver
                        var latest = expApprovals.First();
                        userMap.TryGetValue(latest.ManagerId ?? "", out var latestApproverName);
                        dto.ApproverName = latestApproverName ?? string.Empty;
                    }

                    return dto;
                })
                .ToList();
        }

        // =====================================================
        // VALIDATE MONTHLY LIMITS — shared helper
        // Called at both CreateExpense and SubmitExpense so
        // limits are enforced at the point of entering the
        // approval workflow, not just at draft creation.
        // =====================================================
        private async Task ValidateMonthlyLimits(string userId, UserRole role, decimal expenseAmount, string expenseId)
        {
            var (maxRequests, maxAmount) = GetRoleLimits(role);
            if (!maxRequests.HasValue && !maxAmount.HasValue) return; // Finance/Admin — no limits

            var today      = DateTime.UtcNow;
            var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd   = monthStart.AddMonths(1).AddTicks(-1);

            var allExpenses = await _expenseRepo.GetAllAsync() ?? new List<Expense>();

            // Count submitted/active expenses this month — exclude the expense being submitted itself,
            // Draft, and Rejected (they don't count until submitted)
            var monthlyActive = allExpenses
                .Where(e => e.UserId == userId &&
                            e.ExpenseDate >= monthStart &&
                            e.ExpenseDate <= monthEnd &&
                            e.ExpenseId != expenseId &&          // exclude self
                            e.Status != ExpenseStatus.Rejected &&
                            e.Status != ExpenseStatus.Draft)
                .ToList();

            if (maxRequests.HasValue && monthlyActive.Count >= maxRequests.Value)
                throw new InvalidOperationException(
                    $"Monthly request limit of {maxRequests.Value} reached for role {role}. " +
                    $"You cannot submit more expenses this month.");

            if (maxAmount.HasValue)
            {
                var monthlyTotal = monthlyActive.Sum(e => e.Amount);
                if (monthlyTotal + expenseAmount > maxAmount.Value)
                    throw new InvalidOperationException(
                        $"Submitting this expense would exceed your monthly limit of ₹{maxAmount.Value:N2} for role {role}. " +
                        $"Already submitted: ₹{monthlyTotal:N2}, This expense: ₹{expenseAmount:N2}. " +
                        $"Remaining budget: ₹{Math.Max(0, maxAmount.Value - monthlyTotal):N2}.");
            }
        }

        // =====================================================
        // SUBMIT EXPENSE
        // Routes to the correct first approval stage based on role.
        // Validates monthly limits before entering approval flow.
        // =====================================================
        public async Task<CreateExpenseResponseDto?> SubmitExpense(string expenseId)
        {
            Expense expense;
            try { expense = await _expenseRepo.GetByIdAsync(expenseId) ?? throw new KeyNotFoundException("Expense not found."); }
            catch (KeyNotFoundException) { throw new KeyNotFoundException("Expense not found."); }

            if (expense.Status != ExpenseStatus.Draft)
                throw new InvalidOperationException("Only Draft expenses can be submitted.");

            var users = await _userRepo.GetAllAsync() ?? new List<User>();
            var submitter = users.FirstOrDefault(u => u.UserId == expense.UserId);

            // ── Enforce monthly limits before entering approval flow ───────────
            if (submitter != null && submitter.Role != UserRole.Admin)
                await ValidateMonthlyLimits(expense.UserId, submitter.Role, expense.Amount, expense.ExpenseId);

            if (submitter?.Role == UserRole.Admin)
            {
                expense.Status = ExpenseStatus.Approved;
                await _expenseRepo.UpdateAsync(expenseId, expense);
                var financeUser = users.FirstOrDefault(u => u.Role == UserRole.Finance);
                if (financeUser != null)
                    await _notificationService.CreateNotification(new CreateNotificationRequestDto
                    {
                        UserId = financeUser.UserId,
                        Message = $"Admin expense '{expense.ExpenseId}' (₹{expense.Amount:N2}) is ready for payment.",
                        Description = $"Category: {expense.CategoryName} | Submitted by Admin",
                        SenderRole = "System"
                    });
            }
            else
            {
                expense.Status = submitter?.Role switch
                {
                    UserRole.Employee => ExpenseStatus.PendingTeamLead,
                    UserRole.TeamLead => ExpenseStatus.PendingManager,
                    UserRole.Manager  => ExpenseStatus.PendingAdmin,   // Admin must approve
                    UserRole.Finance  => ExpenseStatus.PendingAdmin,   // Admin must approve
                    _                 => ExpenseStatus.Submitted
                };
                await _expenseRepo.UpdateAsync(expenseId, expense);
                await NotifyApproverOnSubmit(expense, submitter);
            }

            return MapToDto(expense);
        }

        // =====================================================
        // RESUBMIT EXPENSE  ✅ NEW
        //
        // Allows a user to resubmit a Rejected or Draft expense
        // back to Submitted status.
        //
        // Flow:
        //   Rejected → (user edits via UpdateExpenseSafe) → Draft
        //   Draft    → ResubmitExpense                   → Submitted
        //
        // Also allows direct resubmit from Rejected state
        // (skips the edit step if no changes are needed).
        // =====================================================
        public async Task<CreateExpenseResponseDto?> ResubmitExpense(string expenseId)
        {
            var (userId, userName, role) = GetUserFromToken();

            Expense expense;
            try { expense = await _expenseRepo.GetByIdAsync(expenseId) ?? throw new KeyNotFoundException("Expense not found."); }
            catch (KeyNotFoundException) { throw new KeyNotFoundException("Expense not found."); }

            // Only the owner (or Admin) can resubmit
            if (expense.UserId != userId && role != UserRole.Admin)
                throw new UnauthorizedAccessException("You are not authorized to resubmit this expense.");

            // Only Rejected or Draft can be resubmitted
            if (expense.Status != ExpenseStatus.Rejected &&
                expense.Status != ExpenseStatus.Draft)
                throw new InvalidOperationException(
                    $"Only Rejected or Draft expenses can be resubmitted. " +
                    $"Current status: {expense.Status}.");

            var users = await _userRepo.GetAllAsync() ?? new List<User>();
            var submitter = users.FirstOrDefault(u => u.UserId == expense.UserId);

            // ── Enforce monthly limits before re-entering approval flow ────────
            if (submitter != null && submitter.Role != UserRole.Admin)
                await ValidateMonthlyLimits(expense.UserId, submitter.Role, expense.Amount, expense.ExpenseId);

            // Route to correct first stage again
            expense.Status = submitter?.Role switch
            {
                UserRole.Employee => ExpenseStatus.PendingTeamLead,
                UserRole.TeamLead => ExpenseStatus.PendingManager,
                UserRole.Manager  => ExpenseStatus.PendingAdmin,
                UserRole.Finance  => ExpenseStatus.PendingAdmin,
                _                 => ExpenseStatus.Submitted
            };
            await _expenseRepo.UpdateAsync(expenseId, expense);
            await NotifyApproverOnSubmit(expense, submitter);

            // ✅ Audit log for resubmission
            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
            {
                Action = $"Resubmitted Expense {expenseId} (was {expense.Status})",
                ExpenseId = expenseId,
                Amount = expense.Amount,
                Date = DateTime.UtcNow
            });

            return MapToDto(expense);
        }

        // =====================================================
        // NOTIFY APPROVER ON SUBMIT
        // Routes notification to the correct first approver:
        //   Employee  → their assigned TeamLead (ManagerId)
        //   TeamLead  → their assigned Manager (ManagerId)
        //   Manager/Finance → Admin
        // =====================================================
        private async Task NotifyApproverOnSubmit(Expense expense, User? submitter = null)
        {
            try
            {
                if (submitter == null)
                {
                    var users = await _userRepo.GetAllAsync() ?? new List<User>();
                    submitter = users.FirstOrDefault(u => u.UserId == expense.UserId);
                }
                if (submitter == null) return;

                string? approverId = null;

                if (submitter.Role == UserRole.Manager || submitter.Role == UserRole.Finance)
                {
                    var allUsers = await _userRepo.GetAllAsync() ?? new List<User>();
                    var admin = allUsers.FirstOrDefault(u => u.Role == UserRole.Admin);
                    approverId = admin?.UserId;
                }
                else
                {
                    // Employee → TeamLead, TeamLead → Manager (both stored in ManagerId)
                    approverId = submitter.ManagerId;
                }

                if (string.IsNullOrEmpty(approverId)) return;

                await _notificationService.CreateNotification(new CreateNotificationRequestDto
                {
                    UserId = approverId,
                    Message = $"Expense '{expense.ExpenseId}' (₹{expense.Amount:N2}) submitted by {submitter.UserName} is awaiting your approval.",
                    Description = $"Category: {expense.CategoryName} | Date: {expense.ExpenseDate:dd MMM yyyy}" +
                                  (expense.IsAdvanceRequest ? " [ADVANCE REQUEST]" : ""),
                    SenderRole = "System"
                });
            }
            catch { /* notification failure must not block submit */ }
        }

        // =====================================================
        // MAP TO DTO — unchanged
        // ✅ CanEdit now also includes Rejected (user can edit
        //    after rejection before resubmitting)
        // =====================================================
        private CreateExpenseResponseDto MapToDto(Expense e, string? expenseOwnerName = null, string? expenseOwnerRole = null)
        {
            var (userId, userName, role) = GetUserFromToken();

            return new CreateExpenseResponseDto
            {
                ExpenseId = e.ExpenseId ?? "",
                UserId = e.UserId ?? "",
                UserName = expenseOwnerName ?? "",
                UserRole = expenseOwnerRole ?? "",
                CategoryId = e.CategoryId ?? "",
                CategoryName = string.IsNullOrWhiteSpace(e.CategoryName)
                    ? (e.Category?.CategoryName.ToString() ?? "")
                    : e.CategoryName,
                Amount = e.Amount,
                AmountInRupees = CurrencyHelper.FormatRupees(e.Amount),
                ExpenseDate = e.ExpenseDate.ToString("dd-MM-yyyy"),
                Status = e.Status.ToString(),
                DocumentUrls = e.DocumentUrls ?? new List<string>(),
                IsAdvanceRequest = e.IsAdvanceRequest,
                CurrentApprovalStage = e.Status switch
                {
                    ExpenseStatus.PendingTeamLead => "TeamLead",
                    ExpenseStatus.PendingManager  => "Manager",
                    ExpenseStatus.PendingAdmin    => "Admin",
                    ExpenseStatus.PendingFinance  => "Finance",
                    _ => null
                },
                CanEdit = (e.UserId == userId || role == UserRole.Admin) &&
                                 (e.Status == ExpenseStatus.Draft ||
                                  e.Status == ExpenseStatus.Submitted ||
                                  e.Status == ExpenseStatus.Rejected)
            };
        }
    }
}
