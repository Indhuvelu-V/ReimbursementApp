
//using Microsoft.AspNetCore.Http;
//using ReimbursementTrackerApp.Interfaces;
//using ReimbursementTrackerApp.Models;
//using ReimbursementTrackerApp.Models.Common;
//using ReimbursementTrackerApp.Models.DTOs;
//using ReimbursementTrackerApp.Models.Enums;
//using ReimbursementTrackerApp.Models.helper;
//using System.Security.Claims;

//namespace ReimbursementTrackerApp.Services
//{
//    public class ExpenseService : IExpenseService
//    {
//        private readonly IRepository<string, Expense> _expenseRepo;
//        private readonly IRepository<string, ExpenseCategory> _categoryRepo;
//        private readonly IAuditLogService _auditLogService;
//        private readonly IHttpContextAccessor _httpContextAccessor;

//        public ExpenseService(
//            IRepository<string, Expense> expenseRepo,
//            IRepository<string, ExpenseCategory> categoryRepo,
//            IAuditLogService auditLogService,
//            IHttpContextAccessor httpContextAccessor)
//        {
//            _expenseRepo = expenseRepo;
//            _categoryRepo = categoryRepo;
//            _auditLogService = auditLogService;
//            _httpContextAccessor = httpContextAccessor;
//        }

//        // =====================================================
//        // GET USER FROM TOKEN
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

//        // =====================================================
//        // CREATE EXPENSE

//        // =====================================================
//        public async Task<CreateExpenseResponseDto?> CreateExpense(CreateExpenseRequestDto request)
//        {
//            var (userId, userName, role) = GetUserFromToken();

//            var today = DateTime.UtcNow;
//            var monthStart = new DateTime(today.Year, today.Month, 1);
//            var monthEnd = monthStart.AddMonths(1).AddTicks(-1); // last tick of the month

//            // ── RULE 1: current-month date only ──────────────────────────────
//            if (request.ExpenseDate < monthStart || request.ExpenseDate > monthEnd)
//                throw new InvalidOperationException(
//                    $"Expense date must be within the current month " +
//                    $"({monthStart:dd MMM yyyy} – {monthEnd:dd MMM yyyy}). " +
//                    $"You cannot create expenses for past or future months.");
//            // ─────────────────────────────────────────────────────────────────

//            // ── RULE 2: one active expense per current month ──────────────────
//            var allExpenses = await _expenseRepo.GetAllAsync() ?? new List<Expense>();

//            var alreadyExists = allExpenses.Any(e =>
//                e.UserId == userId &&
//                e.ExpenseDate >= monthStart &&
//                e.ExpenseDate <= monthEnd &&
//                e.Status != ExpenseStatus.Rejected); // Rejected → allowed to recreate

//            if (alreadyExists)
//                throw new InvalidOperationException(
//                    $"You have already submitted an expense for " +
//                    $"{today:MMMM yyyy}. Only one expense is allowed per month.");
//            // ─────────────────────────────────────────────────────────────────

//            var category = await _categoryRepo.GetByIdAsync(request.CategoryId);
//            if (category == null)
//                throw new KeyNotFoundException("Expense category not found.");

//            if (request.Amount > category.MaxLimit)
//                throw new InvalidOperationException($"Amount exceeds limit for {category.CategoryName}");

//            var expense = new Expense
//            {
//                ExpenseId = Guid.NewGuid().ToString(),
//                UserId = userId,
//                CategoryId = category.CategoryId,
//                CategoryName = category.CategoryName.ToString(),
//                Amount = request.Amount,
//                ExpenseDate = request.ExpenseDate,
//                DocumentUrls = request.DocumentUrls ?? new List<string>(),
//                Status = ExpenseStatus.Draft
//            };

//            await _expenseRepo.AddAsync(expense);

//            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
//            {
//                Action = $"Created Expense {expense.ExpenseId}",
//                ExpenseId = expense.ExpenseId,
//                Amount = expense.Amount,
//                DocumentUrls = expense.DocumentUrls,
//                Date = DateTime.UtcNow
//            });

//            return MapToDto(expense);
//        }

//        // =====================================================
//        // UPDATE EXPENSE — unchanged
//        // =====================================================
//        public async Task<(bool, string, CreateExpenseResponseDto?)> UpdateExpenseSafe(
//            string expenseId, CreateExpenseRequestDto dto)
//        {
//            var (userId, userName, role) = GetUserFromToken();

//            var existingExpense = await _expenseRepo.GetByIdAsync(expenseId);
//            if (existingExpense == null)
//                return (false, "Expense not found.", null);

//            if (existingExpense.UserId != userId && role != UserRole.Admin)
//                return (false, "Not authorized.", null);

//            if (existingExpense.Status != ExpenseStatus.Draft &&
//                existingExpense.Status != ExpenseStatus.Submitted)
//                return (false, "Only Draft/Submitted allowed.", null);

//            var oldAmount = existingExpense.Amount;
//            var oldDocs = existingExpense.DocumentUrls ?? new List<string>();

//            existingExpense.Amount = dto.Amount;
//            existingExpense.CategoryId = dto.CategoryId;
//            existingExpense.CategoryName = dto.CategoryName;
//            existingExpense.ExpenseDate = dto.ExpenseDate;
//            existingExpense.DocumentUrls = dto.DocumentUrls?.ToList() ?? new List<string>();

//            await _expenseRepo.UpdateAsync(expenseId, existingExpense);

//            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
//            {
//                Action = $"Updated Expense {expenseId}",
//                ExpenseId = expenseId,
//                Amount = existingExpense.Amount,
//                OldAmount = oldAmount,
//                DocumentUrls = existingExpense.DocumentUrls,
//                OldDocumentUrls = oldDocs,
//                Date = DateTime.UtcNow
//            });

//            return (true, "Updated successfully", MapToDto(existingExpense));
//        }

//        // =====================================================
//        // DELETE EXPENSE — unchanged
//        // =====================================================
//        public async Task<(bool, string, CreateExpenseResponseDto?)> DeleteExpenseSafe(string expenseId)
//        {
//            var (userId, userName, role) = GetUserFromToken();

//            var expense = await _expenseRepo.GetByIdAsync(expenseId);
//            if (expense == null)
//                return (false, "Not found", null);

//            if (expense.UserId != userId && role != UserRole.Admin)
//                return (false, "Not authorized", null);

//            if (expense.Status != ExpenseStatus.Draft &&
//                expense.Status != ExpenseStatus.Submitted)
//                return (false, "Cannot delete after manager approved", null);

//            var response = MapToDto(expense);

//            await _expenseRepo.DeleteAsync(expenseId);

//            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
//            {
//                Action = $"Deleted Expense {expenseId}",
//                ExpenseId = expenseId,
//                Amount = expense.Amount,
//                DocumentUrls = expense.DocumentUrls,
//                Date = DateTime.UtcNow
//            });

//            return (true, "Deleted successfully", response);
//        }

//        // =====================================================
//        // GET BY ID — unchanged
//        // =====================================================
//        public async Task<CreateExpenseResponseDto?> GetExpenseById(string expenseId)
//        {
//            var (userId, userName, role) = GetUserFromToken();

//            var expense = await _expenseRepo.GetByIdAsync(expenseId);
//            if (expense == null) return null;

//            if (role == UserRole.Employee && expense.UserId != userId)
//                return null;

//            return MapToDto(expense);
//        }

//        // =====================================================
//        // GET ALL — unchanged
//        // =====================================================
//        public async Task<PagedResponse<CreateExpenseResponseDto>> GetAllExpenses(PaginationParams paginationParams)
//        {
//            var (userId, userName, role) = GetUserFromToken();

//            var expenses = await _expenseRepo.GetAllAsync() ?? new List<Expense>();

//            var query = expenses.AsQueryable();

//            if (role == UserRole.Employee)
//                query = query.Where(e => e.UserId == userId);

//            var total = query.Count();

//            var data = query
//                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
//                .Take(paginationParams.PageSize)
//                .Select(MapToDto)
//                .ToList();

//            return new PagedResponse<CreateExpenseResponseDto>(
//                data, total, paginationParams.PageNumber, paginationParams.PageSize);
//        }

//        // =====================================================
//        // GET MY EXPENSES — unchanged
//        // =====================================================
//        public async Task<List<CreateExpenseResponseDto>> GetMyExpenses()
//        {
//            var (userId, userName, role) = GetUserFromToken();

//            var expenses = await _expenseRepo.GetAllAsync() ?? new List<Expense>();

//            return expenses
//                .Where(e => e.UserId == userId)
//                .Select(MapToDto)
//                .ToList();
//        }

//        // =====================================================
//        // SUBMIT — unchanged
//        // =====================================================
//        public async Task<CreateExpenseResponseDto?> SubmitExpense(string expenseId)
//        {
//            var expense = await _expenseRepo.GetByIdAsync(expenseId);

//            if (expense == null)
//                throw new KeyNotFoundException("Expense not found.");

//            if (expense.Status != ExpenseStatus.Draft)
//                throw new InvalidOperationException("Only Draft allowed");

//            expense.Status = ExpenseStatus.Submitted;

//            await _expenseRepo.UpdateAsync(expenseId, expense);

//            return MapToDto(expense);
//        }

//        // =====================================================
//        // MAP — unchanged
//        // =====================================================
//        private CreateExpenseResponseDto MapToDto(Expense e)
//        {
//            var (userId, _, role) = GetUserFromToken();

//            return new CreateExpenseResponseDto
//            {
//                ExpenseId = e.ExpenseId ?? "",
//                CategoryId = e.CategoryId ?? "",
//                CategoryName = e.Category?.CategoryName.ToString() ?? e.CategoryName,
//                Amount = e.Amount,
//                AmountInRupees = CurrencyHelper.FormatRupees(e.Amount),
//                ExpenseDate = e.ExpenseDate.ToString("dd-MM-yyyy"),
//                Status = e.Status.ToString(),
//                DocumentUrls = e.DocumentUrls ?? new List<string>(),
//                CanEdit = (e.UserId == userId || role == UserRole.Admin) &&
//                                 (e.Status == ExpenseStatus.Draft || e.Status == ExpenseStatus.Submitted)
//            };
//        }
//    }
//}

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
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFileUploadService _fileUploadService;  // ✅ NEW

        public ExpenseService(
            IRepository<string, Expense> expenseRepo,
            IRepository<string, ExpenseCategory> categoryRepo,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            IFileUploadService fileUploadService)  // ✅ NEW
        {
            _expenseRepo = expenseRepo;
            _categoryRepo = categoryRepo;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
            _fileUploadService = fileUploadService;
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
        // CREATE EXPENSE
        //
        // ✅ RULE 1 — Current month only
        // ✅ RULE 2 — One expense per current month
        // ✅ RULE 3 — If a REJECTED expense exists for this month
        //             → UPDATE it instead of creating a new one.
        //             This replaces the old "block on rejected" logic:
        //             the user now edits and resubmits the same record.
        // ✅ FILE UPLOAD — Documents attached via IFormFile are
        //             saved to wwwroot/uploads via FileUploadService.
        // =====================================================
        public async Task<CreateExpenseResponseDto?> CreateExpense(CreateExpenseRequestDto request)
        {
            var (userId, userName, role) = GetUserFromToken();

            // ── Current-month boundary ────────────────────────────────────────
            var today = DateTime.UtcNow;
            var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

            // ── RULE 1: date must be within current month ─────────────────────
            if (request.ExpenseDate < monthStart || request.ExpenseDate > monthEnd)
                throw new InvalidOperationException(
                    $"Expense date must be within the current month " +
                    $"({monthStart:dd MMM yyyy} – {monthEnd:dd MMM yyyy}). " +
                    $"You cannot create expenses for past or future months.");

            // ── Handle file uploads ──────────────────────────────────────────
            var documentUrls = request.DocumentUrls ?? new List<string>();
            if (request.Documents != null && request.Documents.Count > 0)
            {
                var uploadedUrls = await _fileUploadService.SaveFilesAsync(request.Documents);
                documentUrls.AddRange(uploadedUrls);
            }

            // ── Fetch category ────────────────────────────────────────────────
            var category = await _categoryRepo.GetByIdAsync(request.CategoryId);
            if (category == null)
                throw new KeyNotFoundException("Expense category not found.");

            if (request.Amount > category.MaxLimit)
                throw new InvalidOperationException($"Amount exceeds limit for {category.CategoryName}");

            // ── RULE 2 + 3: check for existing expense this month ─────────────
            var allExpenses = await _expenseRepo.GetAllAsync() ?? new List<Expense>();

            var existingThisMonth = allExpenses.FirstOrDefault(e =>
                e.UserId == userId &&
                e.ExpenseDate >= monthStart &&
                e.ExpenseDate <= monthEnd);

            if (existingThisMonth != null)
            {
                if (existingThisMonth.Status == ExpenseStatus.Rejected)
                {
                    // ✅ RULE 3 — Rejected expense exists → UPDATE it in-place
                    //    instead of creating a new one. User edits the same record.
                    var oldAmount = existingThisMonth.Amount;
                    var oldDocs = existingThisMonth.DocumentUrls ?? new List<string>();

                    existingThisMonth.CategoryId = category.CategoryId;
                    existingThisMonth.CategoryName = category.CategoryName.ToString();
                    existingThisMonth.Amount = request.Amount;
                    existingThisMonth.ExpenseDate = request.ExpenseDate;
                    existingThisMonth.DocumentUrls = documentUrls;
                    existingThisMonth.Status = ExpenseStatus.Draft; // back to Draft after edit

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
                    throw new InvalidOperationException(
                        $"You have already submitted an expense for " +
                        $"{today:MMMM yyyy}. Only one expense is allowed per month.");
                }
            }

            // ── Create new expense ────────────────────────────────────────────
            var expense = new Expense
            {
                ExpenseId = Guid.NewGuid().ToString(),
                UserId = userId,
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName.ToString(),
                Amount = request.Amount,
                ExpenseDate = request.ExpenseDate,
                DocumentUrls = documentUrls,
                Status = ExpenseStatus.Draft
            };

            await _expenseRepo.AddAsync(expense);

            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
            {
                Action = $"Created Expense {expense.ExpenseId}",
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

            var existingExpense = await _expenseRepo.GetByIdAsync(expenseId);
            if (existingExpense == null)
                return (false, "Expense not found.", null);

            if (existingExpense.UserId != userId && role != UserRole.Admin)
                return (false, "Not authorized.", null);

            // ✅ Rejected is now allowed (user edits after rejection)
            if (existingExpense.Status != ExpenseStatus.Draft &&
                existingExpense.Status != ExpenseStatus.Submitted &&
                existingExpense.Status != ExpenseStatus.Rejected)
                return (false, "Only Draft, Submitted, or Rejected expenses can be edited.", null);

            var oldAmount = existingExpense.Amount;
            var oldDocs = existingExpense.DocumentUrls ?? new List<string>();

            // ── Handle new file uploads, appending to existing URLs ───────────
            var documentUrls = dto.DocumentUrls?.ToList() ?? new List<string>(oldDocs);
            if (dto.Documents != null && dto.Documents.Count > 0)
            {
                var uploadedUrls = await _fileUploadService.SaveFilesAsync(dto.Documents);
                documentUrls.AddRange(uploadedUrls);
            }

            existingExpense.Amount = dto.Amount;
            existingExpense.CategoryId = dto.CategoryId;
            existingExpense.CategoryName = dto.CategoryName;
            existingExpense.ExpenseDate = dto.ExpenseDate;
            existingExpense.DocumentUrls = documentUrls;

            await _expenseRepo.UpdateAsync(expenseId, existingExpense);

            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
            {
                Action = $"Updated Expense {expenseId}",
                ExpenseId = expenseId,
                Amount = existingExpense.Amount,
                OldAmount = oldAmount,
                DocumentUrls = existingExpense.DocumentUrls,
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

            var expense = await _expenseRepo.GetByIdAsync(expenseId);
            if (expense == null)
                return (false, "Not found", null);

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

            var expense = await _expenseRepo.GetByIdAsync(expenseId);
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

            var query = expenses.AsQueryable();

            if (role == UserRole.Employee)
                query = query.Where(e => e.UserId == userId);

            var total = query.Count();

            var data = query
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .Select(MapToDto)
                .ToList();

            return new PagedResponse<CreateExpenseResponseDto>(
                data, total, paginationParams.PageNumber, paginationParams.PageSize);
        }

        // =====================================================
        // GET MY EXPENSES — unchanged
        // =====================================================
        public async Task<List<CreateExpenseResponseDto>> GetMyExpenses()
        {
            var (userId, userName, role) = GetUserFromToken();

            var expenses = await _expenseRepo.GetAllAsync() ?? new List<Expense>();

            return expenses
                .Where(e => e.UserId == userId)
                .Select(MapToDto)
                .ToList();
        }

        // =====================================================
        // SUBMIT EXPENSE — unchanged (Draft → Submitted)
        // =====================================================
        public async Task<CreateExpenseResponseDto?> SubmitExpense(string expenseId)
        {
            var expense = await _expenseRepo.GetByIdAsync(expenseId);

            if (expense == null)
                throw new KeyNotFoundException("Expense not found.");

            if (expense.Status != ExpenseStatus.Draft)
                throw new InvalidOperationException("Only Draft expenses can be submitted.");

            expense.Status = ExpenseStatus.Submitted;

            await _expenseRepo.UpdateAsync(expenseId, expense);

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

            var expense = await _expenseRepo.GetByIdAsync(expenseId);

            if (expense == null)
                throw new KeyNotFoundException("Expense not found.");

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
        // MAP TO DTO — unchanged
        // ✅ CanEdit now also includes Rejected (user can edit
        //    after rejection before resubmitting)
        // =====================================================
        private CreateExpenseResponseDto MapToDto(Expense e)
        {
            var (userId, _, role) = GetUserFromToken();

            return new CreateExpenseResponseDto
            {
                ExpenseId = e.ExpenseId ?? "",
                CategoryId = e.CategoryId ?? "",
                CategoryName = e.Category?.CategoryName.ToString() ?? e.CategoryName,
                Amount = e.Amount,
                AmountInRupees = CurrencyHelper.FormatRupees(e.Amount),
                ExpenseDate = e.ExpenseDate.ToString("dd-MM-yyyy"),
                Status = e.Status.ToString(),
                DocumentUrls = e.DocumentUrls ?? new List<string>(),
                // ✅ Rejected is now editable so user can fix and resubmit
                CanEdit = (e.UserId == userId || role == UserRole.Admin) &&
                                 (e.Status == ExpenseStatus.Draft ||
                                  e.Status == ExpenseStatus.Submitted ||
                                  e.Status == ExpenseStatus.Rejected)
            };
        }
    }
}
