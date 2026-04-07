

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
        // Employee  : max 2 bills, ₹5,000 total, no advance
        // TeamLead  : max 5 bills, ₹10,000 total, no advance
        // Manager   : unlimited bills, ₹20,000 total, can advance
        // Finance   : no limit enforced here (Finance approves, not submits)
        // =====================================================
        private static (int? maxBills, decimal maxAmount, bool canAdvance) GetRoleLimits(UserRole role) =>
            role switch
            {
                UserRole.Employee => (2, 5000m, false),
                UserRole.TeamLead => (5, 10000m, false),
                UserRole.Manager  => (null, 20000m, true),
                _                 => (null, decimal.MaxValue, true)
            };

        // =====================================================
        // CREATE EXPENSE
        // Enforces role-based monthly bill count + amount limits.
        // Advance requests restricted to Manager only.
        // Manager can submit future-dated bills (post-advance).
        // =====================================================
        public async Task<CreateExpenseResponseDto?> CreateExpense(CreateExpenseRequestDto request)
        {
            var (userId, userName, role) = GetUserFromToken();

            var today = DateTime.UtcNow;
            var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

            var (maxBills, maxAmount, canAdvance) = GetRoleLimits(role);

            // ── Advance request restriction ───────────────────────────────────
            if (request.IsAdvanceRequest && !canAdvance)
                throw new InvalidOperationException(
                    $"Role '{role}' is not allowed to create advance requests. Only Managers can request advances.");

            // ── Date rule: Employee/TeamLead → current month only
            //              Manager → can submit future-dated bills (post-advance usage)
            if (!canAdvance && (request.ExpenseDate < monthStart || request.ExpenseDate > monthEnd))
                throw new InvalidOperationException(
                    $"Expense date must be within the current month " +
                    $"({monthStart:dd MMM yyyy} – {monthEnd:dd MMM yyyy}).");

<<<<<<< HEAD
            // ── Handle file uploads ──────────────────────────────────────────
            // Files are saved by the controller via FileUploadService before calling this method.
            // DocumentUrls already contains the saved paths — no re-upload needed here.
=======
            // ── Handle file uploads ───────────────────────────────────────────
>>>>>>> eba5464 (Feature added)
            var documentUrls = request.DocumentUrls ?? new List<string>();

            // ── Fetch category ────────────────────────────────────────────────
            ExpenseCategory category;
            try { category = await _categoryRepo.GetByIdAsync(request.CategoryId) ?? throw new KeyNotFoundException("Expense category not found."); }
            catch (KeyNotFoundException) { throw new KeyNotFoundException("Expense category not found."); }

            if (request.Amount > category.MaxLimit)
                throw new InvalidOperationException($"Amount exceeds the category limit for {category.CategoryName}.");

<<<<<<< HEAD
            // ── RULE 2 + 3: check for existing expenses this month ────────────
            // ── RULE 2 + 3: check for existing expense this month ─────────────
=======
            // ── Monthly aggregates for this user ──────────────────────────────
>>>>>>> eba5464 (Feature added)
            var allExpenses = await _expenseRepo.GetAllAsync() ?? new List<Expense>();

            var thisMonthExpenses = allExpenses
                .Where(e => e.UserId == userId &&
                            e.ExpenseDate >= monthStart &&
                            e.ExpenseDate <= monthEnd &&
                            e.Status != ExpenseStatus.Rejected)
                .ToList();

            // ── Bill count limit ──────────────────────────────────────────────
            if (maxBills.HasValue && thisMonthExpenses.Count >= maxBills.Value)
                throw new InvalidOperationException(
                    $"Monthly bill limit reached. Role '{role}' can submit at most {maxBills} bills per month.");

            // ── Amount limit ──────────────────────────────────────────────────
            var totalThisMonth = thisMonthExpenses.Sum(e => e.Amount);
            if (totalThisMonth + request.Amount > maxAmount)
                throw new InvalidOperationException(
                    $"Monthly reimbursement limit exceeded. Role '{role}' can claim at most ₹{maxAmount:N0} per month. " +
                    $"Already claimed: ₹{totalThisMonth:N0}, Requested: ₹{request.Amount:N0}.");

            // ── Rejected expense reuse (same month) ───────────────────────────
            var rejectedThisMonth = allExpenses.FirstOrDefault(e =>
                e.UserId == userId &&
                e.ExpenseDate >= monthStart &&
                e.ExpenseDate <= monthEnd &&
                e.Status == ExpenseStatus.Rejected);

            if (rejectedThisMonth != null)
            {
                var oldAmount = rejectedThisMonth.Amount;
                var oldDocs   = rejectedThisMonth.DocumentUrls ?? new List<string>();

                rejectedThisMonth.CategoryId      = category.CategoryId;
                rejectedThisMonth.CategoryName    = category.CategoryName.ToString();
                rejectedThisMonth.Amount          = request.Amount;
                rejectedThisMonth.ExpenseDate     = request.ExpenseDate;
                rejectedThisMonth.DocumentUrls    = documentUrls;
                rejectedThisMonth.IsAdvanceRequest = request.IsAdvanceRequest;
                rejectedThisMonth.Status          = ExpenseStatus.Draft;

                await _expenseRepo.UpdateAsync(rejectedThisMonth.ExpenseId, rejectedThisMonth);

                await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
                {
<<<<<<< HEAD
                    // ✅ RULE 3 — Rejected expense exists → UPDATE it in-place
                    var oldAmount = existingThisMonth.Amount;
                    var oldDocs = existingThisMonth.DocumentUrls ?? new List<string>();

                    existingThisMonth.CategoryId = category.CategoryId;
                    existingThisMonth.CategoryName = category.CategoryName.ToString();
                    existingThisMonth.Amount = request.Amount;
                    existingThisMonth.ExpenseDate = request.ExpenseDate;
                    existingThisMonth.DocumentUrls = documentUrls;
                    existingThisMonth.Status = ExpenseStatus.Draft;

                    await _expenseRepo.UpdateAsync(existingThisMonth.ExpenseId, existingThisMonth);

                    await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
                    {
                        Action = $"Updated Rejected Expense {existingThisMonth.ExpenseId} (re-edit after rejection)",
                        ExpenseId = existingThisMonth.ExpenseId,
                        Amount = existingThisMonth.Amount,
                        OldAmount = oldAmount,
                        DocumentUrls = existingThisMonth.DocumentUrls,
                        OldDocumentUrls = oldDocs,
                        Date = DateTime.UtcNow
                    });

                    return MapToDto(existingThisMonth);
                }
                else
                {
                    // ✅ RULE 2 — Active (non-rejected) expense already exists
                    await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
                    {
                        Action = $"Blocked Expense creation — already submitted an expense for {today:MMMM yyyy}",
                        Date = DateTime.UtcNow
                    });
                    throw new InvalidOperationException(
                        $"You have already submitted an expense for " +
                        $"{today:MMMM yyyy}. Only one expense is allowed per month.");
                }
=======
                    Action = $"Re-edited rejected expense {rejectedThisMonth.ExpenseId}",
                    ExpenseId = rejectedThisMonth.ExpenseId,
                    Amount = rejectedThisMonth.Amount,
                    OldAmount = oldAmount,
                    DocumentUrls = rejectedThisMonth.DocumentUrls,
                    OldDocumentUrls = oldDocs,
                    Date = DateTime.UtcNow
                });

                return MapToDto(rejectedThisMonth);
>>>>>>> eba5464 (Feature added)
            }

            // ── Create new expense ────────────────────────────────────────────
            var expense = new Expense
            {
                ExpenseId        = Guid.NewGuid().ToString(),
                UserId           = userId,
                CategoryId       = category.CategoryId,
                CategoryName     = category.CategoryName.ToString(),
                Amount           = request.Amount,
                ExpenseDate      = request.ExpenseDate,
                DocumentUrls     = documentUrls,
                IsAdvanceRequest = request.IsAdvanceRequest,
                Status           = ExpenseStatus.Draft
            };

            await _expenseRepo.AddAsync(expense);

            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
            {
                Action = $"Created expense {expense.ExpenseId}" + (expense.IsAdvanceRequest ? " [ADVANCE]" : ""),
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
                return (false, "Cannot delete after manager approved", null);

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
            else if (role == UserRole.Manager)
            {
                // Manager sees only expenses from employees in their own department
                var managerUser = users.FirstOrDefault(u => u.UserId == userId);
                if (managerUser != null)
                {
                    var deptEmployeeIds = users
                        .Where(u => u.Department == managerUser.Department && u.UserId != userId)
                        .Select(u => u.UserId)
                        .ToHashSet();
                    query = query.Where(e => deptEmployeeIds.Contains(e.UserId));
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
        // GET MY EXPENSES — includes approval comments
        // =====================================================
        public async Task<List<CreateExpenseResponseDto>> GetMyExpenses()
        {
            var (userId, userName, role) = GetUserFromToken();

            var expenses = await _expenseRepo.GetAllAsync() ?? new List<Expense>();
            var approvals = await _approvalRepo.GetAllAsync() ?? new List<Approval>();
            var users = await _userRepo.GetAllAsync() ?? new List<User>();

            // Build approval lookup: expenseId → latest approval
            var approvalMap = approvals
                .GroupBy(a => a.ExpenseId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.ApprovedAt).First());

            var userMap = users.GroupBy(u => u.UserId).ToDictionary(g => g.Key, g => g.First().UserName);

            return expenses
                .Where(e => e.UserId == userId)
                .Select(e =>
                {
                    var dto = MapToDto(e, userName);
                    if (approvalMap.TryGetValue(e.ExpenseId, out var approval))
                    {
                        dto.ApprovalComment = approval.Comments ?? string.Empty;
                        userMap.TryGetValue(approval.ManagerId ?? "", out var approverName);
                        dto.ApproverName = approverName ?? string.Empty;
                    }
                    return dto;
                })
                .ToList();
        }

        // =====================================================
        // SUBMIT EXPENSE — auto-approves Admin expenses
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

            if (submitter?.Role == UserRole.Admin)
            {
                // Admin expense → skip approval, go directly to Approved
                expense.Status = ExpenseStatus.Approved;
                await _expenseRepo.UpdateAsync(expenseId, expense);

                // Notify Finance for payment
                var financeUser = users.FirstOrDefault(u => u.Role == UserRole.Finance);
                if (financeUser != null)
                {
                    try
                    {
                        await _notificationService.CreateNotification(new CreateNotificationRequestDto
                        {
                            UserId = financeUser.UserId,
                            Message = $"Admin expense '{expense.ExpenseId}' (₹{expense.Amount:N2}) is ready for payment.",
                            Description = $"Category: {expense.CategoryName} | Submitted by Admin",
                            SenderRole = "System"
                        });
                    }
                    catch { }
                }
            }
            else
            {
                // Non-admin → normal submit flow
                expense.Status = ExpenseStatus.Submitted;
                await _expenseRepo.UpdateAsync(expenseId, expense);
                await NotifyApproverOnSubmit(expense);
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

            expense.Status = ExpenseStatus.Submitted;
            await _expenseRepo.UpdateAsync(expenseId, expense);

            // 🔹 Notify the approver on resubmit
            await NotifyApproverOnSubmit(expense);

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
        // Employee → notify their reporting manager
        // Manager/Finance → notify Admin
        // =====================================================
        private async Task NotifyApproverOnSubmit(Expense expense)
        {
            try
            {
                var users = await _userRepo.GetAllAsync() ?? new List<User>();
                var submitter = users.FirstOrDefault(u => u.UserId == expense.UserId);
                if (submitter == null) return;

                string? approverId = null;
                string approverRole = "Manager";

                if (submitter.Role == UserRole.Manager || submitter.Role == UserRole.Finance)
                {
                    // Notify Admin
                    var admin = users.FirstOrDefault(u => u.Role == UserRole.Admin);
                    approverId = admin?.UserId;
                    approverRole = "Admin";
                }
                else
                {
                    // Notify reporting manager
                    approverId = submitter.ManagerId;
                }

                if (string.IsNullOrEmpty(approverId)) return;

                await _notificationService.CreateNotification(new CreateNotificationRequestDto
                {
                    UserId = approverId,
                    Message = $"Expense '{expense.ExpenseId}' (₹{expense.Amount:N2}) submitted by {submitter.UserName} is awaiting your approval.",
                    Description = $"Category: {expense.CategoryName} | Date: {expense.ExpenseDate:dd MMM yyyy}",
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
<<<<<<< HEAD
=======
                IsAdvanceRequest = e.IsAdvanceRequest,
>>>>>>> eba5464 (Feature added)
                CanEdit = (e.UserId == userId || role == UserRole.Admin) &&
                                 (e.Status == ExpenseStatus.Draft ||
                                  e.Status == ExpenseStatus.Submitted ||
                                  e.Status == ExpenseStatus.Rejected)
            };
        }
    }
}
