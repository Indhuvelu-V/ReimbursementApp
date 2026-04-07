

<<<<<<< HEAD
// FILE: Services/ApprovalService.cs — FULL FILE
// CHANGE: ManagerApproval and GetAllApprovals now map expense.DocumentUrls
//         and expense.Amount into the response DTO so the frontend can
//         display the uploaded image on the approval screen.
//         No new DTOs. No new models. Only mapping updated.
=======
//using Microsoft.Extensions.Logging;
//using ReimbursementTrackerApp.Interfaces;
//using ReimbursementTrackerApp.Models;
//using ReimbursementTrackerApp.Models.Common;
//using ReimbursementTrackerApp.Models.DTOs;
//using ReimbursementTrackerApp.Models.Enums;

//namespace ReimbursementTrackerApp.Services
//{
//    public class ApprovalService : IApprovalService
//    {
//        private readonly IRepository<string, Expense> _expenseRepo;
//        private readonly IRepository<string, Approval> _approvalRepo;
//        private readonly IRepository<string, User> _userRepo;
//        private readonly INotificationService _notificationService;
//        private readonly IAuditLogService _auditLogService;
//        private readonly ILogger<ApprovalService> _logger;

//        public ApprovalService(
//            IRepository<string, Expense> expenseRepo,
//            IRepository<string, Approval> approvalRepo,
//            IRepository<string, User> userRepo,
//            INotificationService notificationService,
//            IAuditLogService auditLogService,
//            ILogger<ApprovalService> logger)
//        {
//            _expenseRepo = expenseRepo;
//            _approvalRepo = approvalRepo;
//            _userRepo = userRepo;
//            _notificationService = notificationService;
//            _auditLogService = auditLogService;
//            _logger = logger;
//        }

//        // ======================================================
//        // 1️⃣ MANAGER APPROVAL / REJECTION
//        // ======================================================
//        public async Task<CreateApprovalResponseDto?> ManagerApproval(CreateApprovalRequestDto request)
//        {
//            try
//            {
//                _logger.LogInformation("Manager {ManagerId} attempting {Status} for Expense {ExpenseId}",
//                    request.ManagerId, request.Status, request.ExpenseId);

//                var expenses = await _expenseRepo.GetAllAsync();
//                var expense = expenses?.FirstOrDefault(e => e.ExpenseId == request.ExpenseId);

//                if (expense == null)
//                {
//                    _logger.LogWarning("Expense {ExpenseId} not found for approval.", request.ExpenseId);
//                    throw new KeyNotFoundException($"Expense {request.ExpenseId} not found.");
//                }

//                if (expense.Status != ExpenseStatus.Submitted)
//                {
//                    _logger.LogWarning("Expense {ExpenseId} is not in Submitted state.", request.ExpenseId);
//                    throw new InvalidOperationException("Expense must be in Submitted state.");
//                }

//                var approval = new Approval
//                {
//                    ApprovalId = Guid.NewGuid().ToString(),
//                    ExpenseId = expense.ExpenseId,
//                    ManagerId = request.ManagerId,
//                    Level = "Manager",
//                    Comments = request.Comments,
//                    ApprovedAt = DateTime.Now
//                };

//                string notificationMessage;

//                if (request.Status.Equals("approved", StringComparison.OrdinalIgnoreCase))
//                {
//                    approval.Status = ApprovalStatus.Approved;
//                    expense.Status = ExpenseStatus.Approved;
//                    notificationMessage = $"Your expense {expense.ExpenseId} has been APPROVED by Manager.";
//                }
//                else if (request.Status.Equals("rejected", StringComparison.OrdinalIgnoreCase))
//                {
//                    approval.Status = ApprovalStatus.Rejected;
//                    expense.Status = ExpenseStatus.Rejected;
//                    notificationMessage = $"Your expense {expense.ExpenseId} has been REJECTED by Manager.";
//                }
//                else
//                {
//                    _logger.LogError("Invalid approval status {Status} for Expense {ExpenseId}", request.Status, request.ExpenseId);
//                    throw new ArgumentException("Invalid status. Must be 'approved' or 'rejected'.");
//                }

//                await _approvalRepo.AddAsync(approval);
//                await _expenseRepo.UpdateAsync(expense.ExpenseId, expense);

//                var users = await _userRepo.GetAllAsync();
//                var managerUser = users?.FirstOrDefault(u => u.UserId == request.ManagerId);

//                await _notificationService.CreateNotification(new CreateNotificationRequestDto
//                {
//                    UserId = expense.UserId,
//                    Message = notificationMessage,
//                    Description = string.IsNullOrWhiteSpace(request.Comments)
//                        ? string.Empty
//                        : $"Manager comments: {request.Comments}",
//                    SenderRole = "System"
//                });

//                await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
//                {
//                    Action = $"Manager {approval.Status} expense {expense.ExpenseId}",
//                    ExpenseId = expense.ExpenseId,
//                    Amount = expense.Amount,
//                    Date = DateTime.UtcNow
//                });

//                _logger.LogInformation("Manager {ManagerId} successfully {Status} expense {ExpenseId}",
//                    request.ManagerId, approval.Status, expense.ExpenseId);

//                return new CreateApprovalResponseDto
//                {
//                    ApprovalId = approval.ApprovalId,
//                    ExpenseId = expense.ExpenseId,
//                    Status = approval.Status.ToString(),
//                    Comments = approval.Comments,
//                    Level = approval.Level,
//                    ApprovedAt = approval.ApprovedAt,
//                    ApproverName = managerUser?.UserName ?? ""
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in ManagerApproval for Expense {ExpenseId}", request.ExpenseId);
//                throw;
//            }
//        }

//        // ======================================================
//        // 2️⃣ ADMIN VIEW ALL APPROVALS
//        // ======================================================
//        public async Task<PagedResponse<CreateApprovalResponseDto>> GetAllApprovals(PaginationParams paginationParams)
//        {
//            try
//            {
//                _logger.LogInformation("Admin fetching all approvals, Page {PageNumber}, Size {PageSize}",
//                    paginationParams.PageNumber, paginationParams.PageSize);

//                var approvals = await _approvalRepo.GetAllAsync();
//                var users = await _userRepo.GetAllAsync();

//                var approvalsList = approvals ?? new List<Approval>();
//                var usersList = users ?? new List<User>();

//                var query = approvalsList.Join(
//                    usersList,
//                    approval => approval.ManagerId,
//                    user => user.UserId,
//                    (approval, user) => new { approval, ApproverName = user.UserName }
//                );

//                var totalRecords = query.Count();

//                var pagedApprovals = query
//                    .OrderBy(x => x.approval.ApprovalId)
//                    .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
//                    .Take(paginationParams.PageSize)
//                    .Select(x => new CreateApprovalResponseDto
//                    {
//                        ApprovalId = x.approval.ApprovalId,
//                        ExpenseId = x.approval.ExpenseId,
//                        Status = x.approval.Status.ToString(),
//                        Comments = x.approval.Comments,
//                        Level = x.approval.Level,
//                        ApprovedAt = x.approval.ApprovedAt,
//                        ApproverName = x.ApproverName
//                    })
//                    .ToList();

//                await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
//                {
//                    Action = "Admin viewed approvals list",
//                    Date = DateTime.UtcNow
//                });

//                _logger.LogInformation("Admin fetched {Count} approvals successfully", pagedApprovals.Count);

//                return new PagedResponse<CreateApprovalResponseDto>(
//                    pagedApprovals,
//                    totalRecords,
//                    paginationParams.PageNumber,
//                    paginationParams.PageSize
//                );
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error fetching approvals list for Admin");
//                throw;
//            }
//        }
//    }
//}

// FILE: Services/ApprovalService.cs
// Implements 3-level approval hierarchy:
//   Employee/TeamLead → TeamLead (Level1) → Manager (Level2) → Finance (payment)
// Manager can also submit their own expenses → Manager approval skips Level1.
>>>>>>> eba5464 (Feature added)

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
        // TEAM LEAD APPROVAL (Level1)
        // Processes expenses submitted by Employee or TeamLead.
        // On approval → expense moves to Pending (awaiting Manager Level2).
        // On rejection → expense moves to Rejected.
        // ======================================================
        public async Task<CreateApprovalResponseDto?> TeamLeadApproval(CreateApprovalRequestDto request)
        {
            var expenses = await _expenseRepo.GetAllAsync();
            var expense = expenses?.FirstOrDefault(e => e.ExpenseId == request.ExpenseId);

            if (expense == null)
                throw new KeyNotFoundException($"Expense {request.ExpenseId} not found.");

            // TeamLead can only act on Submitted expenses
            if (expense.Status != ExpenseStatus.Submitted)
                throw new InvalidOperationException(
                    "Expense must be in Submitted state for Team Lead review.");

            // Verify the approver is actually a TeamLead
            var teamLead = await _userRepo.GetByIdAsync(request.ManagerId);
            if (teamLead == null || teamLead.Role != UserRole.TeamLead)
                throw new InvalidOperationException("Approver must be a Team Lead.");

            var approval = new Approval
            {
                ApprovalId = Guid.NewGuid().ToString(),
                ExpenseId  = expense.ExpenseId,
                ManagerId  = request.ManagerId,
                Level      = "Level1",
                Comments   = request.Comments,
                ApprovedAt = DateTime.Now
            };

            string notificationMessage;

            if (request.Status.Equals("approved", StringComparison.OrdinalIgnoreCase))
            {
                approval.Status = ApprovalStatus.Approved;
                // Move to Pending = awaiting Manager (Level2) approval
                expense.Status = ExpenseStatus.Pending;
                notificationMessage = $"Your expense {expense.ExpenseId} has been approved by Team Lead and is now pending Manager review.";
            }
            else if (request.Status.Equals("rejected", StringComparison.OrdinalIgnoreCase))
            {
                approval.Status = ApprovalStatus.Rejected;
                expense.Status  = ExpenseStatus.Rejected;
                notificationMessage = $"Your expense {expense.ExpenseId} has been REJECTED by Team Lead.";
            }
            else
            {
                throw new ArgumentException("Invalid status. Must be 'approved' or 'rejected'.");
            }

            await _approvalRepo.AddAsync(approval);
            await _expenseRepo.UpdateAsync(expense.ExpenseId, expense);

            await _notificationService.CreateNotification(new CreateNotificationRequestDto
            {
                UserId      = expense.UserId,
                Message     = notificationMessage,
                Description = string.IsNullOrWhiteSpace(request.Comments) ? string.Empty : $"Team Lead comments: {request.Comments}",
                SenderRole  = "TeamLead"
            });

            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
            {
                Action    = $"TeamLead {approval.Status} expense {expense.ExpenseId}",
                ExpenseId = expense.ExpenseId,
                Amount    = expense.Amount,
                Date      = DateTime.UtcNow
            });

            return BuildResponse(approval, teamLead, expense);
        }

        // ======================================================
        // MANAGER APPROVAL (Level2)
        // Processes expenses that are either:
        //   a) In Pending state (approved by TeamLead at Level1), OR
        //   b) In Submitted state (Manager's own expense — skips Level1)
        // On approval → ExpenseStatus.Approved (ready for Finance payment).
        // On rejection → ExpenseStatus.Rejected.
        // ======================================================
        public async Task<CreateApprovalResponseDto?> ManagerApproval(CreateApprovalRequestDto request)
        {
            var expenses = await _expenseRepo.GetAllAsync();
            var expense = expenses?.FirstOrDefault(e => e.ExpenseId == request.ExpenseId);

            if (expense == null)
                throw new KeyNotFoundException($"Expense {request.ExpenseId} not found.");

            // Manager can act on:
            //   Pending  = passed TeamLead review, awaiting Manager
            //   Submitted = Manager's own expense (no TeamLead step)
            if (expense.Status != ExpenseStatus.Pending && expense.Status != ExpenseStatus.Submitted)
                throw new InvalidOperationException(
                    "Expense must be in Pending (post-TeamLead) or Submitted (Manager's own) state for Manager review.");

            // Verify the approver is actually a Manager
            var manager = await _userRepo.GetByIdAsync(request.ManagerId);
            if (manager == null || manager.Role != UserRole.Manager)
                throw new InvalidOperationException("Approver must be a Manager.");

            // ✅ Block: Manager cannot approve expenses created by Manager or Finance — Admin must handle those
            var expenseOwner = (await _userRepo.GetAllAsync())?.FirstOrDefault(u => u.UserId == expense.UserId);
            if (expenseOwner != null &&
                (expenseOwner.Role == UserRole.Manager || expenseOwner.Role == UserRole.Finance))
                throw new InvalidOperationException(
                    "Expenses created by Manager or Finance roles must be approved by Admin, not a Manager.");

            // ✅ Self-approval restriction: a manager cannot approve their own expense
            if (expense.UserId == request.ManagerId)
                throw new InvalidOperationException("You cannot approve your own expense. Another manager must review it.");

            var approval = new Approval
            {
                ApprovalId = Guid.NewGuid().ToString(),
                ExpenseId  = expense.ExpenseId,
                ManagerId  = request.ManagerId,
                Level      = "Level2",
                Comments   = request.Comments,
                ApprovedAt = DateTime.Now
            };

            string notificationMessage;

            if (request.Status.Equals("approved", StringComparison.OrdinalIgnoreCase))
            {
                approval.Status = ApprovalStatus.Approved;
                expense.Status  = ExpenseStatus.Approved; // ready for Finance payment
                notificationMessage = $"Your expense {expense.ExpenseId} has been APPROVED by Manager and is pending payment.";
            }
            else if (request.Status.Equals("rejected", StringComparison.OrdinalIgnoreCase))
            {
                approval.Status = ApprovalStatus.Rejected;
                expense.Status  = ExpenseStatus.Rejected;
                notificationMessage = $"Your expense {expense.ExpenseId} has been REJECTED by Manager.";
            }
            else
            {
                throw new ArgumentException("Invalid status. Must be 'approved' or 'rejected'.");
            }

            await _approvalRepo.AddAsync(approval);
            await _expenseRepo.UpdateAsync(expense.ExpenseId, expense);

            await _notificationService.CreateNotification(new CreateNotificationRequestDto
            {
                UserId      = expense.UserId,
                Message     = notificationMessage,
                Description = string.IsNullOrWhiteSpace(request.Comments) ? string.Empty : $"Manager comments: {request.Comments}",
                SenderRole  = "Manager"
            });

            // 🔹 If approved, notify Finance for payment processing
            if (approval.Status == ApprovalStatus.Approved)
            {
                var financeUser = users?.FirstOrDefault(u => u.Role == UserRole.Finance);
                if (financeUser != null)
                {
                    await _notificationService.CreateNotification(new CreateNotificationRequestDto
                    {
                        UserId = financeUser.UserId,
                        Message = $"Expense '{expense.ExpenseId}' (₹{expense.Amount:N2}) by {expense.User?.UserName ?? expense.UserId} has been approved and is ready for payment.",
                        Description = $"Category: {expense.CategoryName} | Approved by: {managerUser?.UserName ?? "Manager"}",
                        SenderRole = "System"
                    });
                }
            }

            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
            {
                Action    = $"Manager {approval.Status} expense {expense.ExpenseId}",
                ExpenseId = expense.ExpenseId,
                Amount    = expense.Amount,
                Date      = DateTime.UtcNow
            });

            return BuildResponse(approval, manager, expense);
        }

        // ======================================================
        // ADMIN APPROVAL — for Manager/Finance expenses
        // ======================================================
        public async Task<CreateApprovalResponseDto?> AdminApproval(CreateApprovalRequestDto request)
        {
            var expenses = await _expenseRepo.GetAllAsync();
            var expense = expenses?.FirstOrDefault(e => e.ExpenseId == request.ExpenseId);

            if (expense == null)
                throw new KeyNotFoundException($"Expense {request.ExpenseId} not found.");

            if (expense.Status != ExpenseStatus.Submitted)
                throw new InvalidOperationException("Expense must be in Submitted state.");

            var users = await _userRepo.GetAllAsync();
            var expenseOwner = users?.FirstOrDefault(u => u.UserId == expense.UserId);

            // Only allow admin to approve Manager/Finance expenses
            if (expenseOwner == null ||
                (expenseOwner.Role != UserRole.Manager && expenseOwner.Role != UserRole.Finance))
                throw new InvalidOperationException(
                    "Admin approval is only for expenses created by Manager or Finance roles.");

            var approval = new Approval
            {
                ApprovalId = Guid.NewGuid().ToString(),
                ExpenseId = expense.ExpenseId,
                ManagerId = request.ManagerId, // Admin's userId
                Level = "Admin",
                Comments = request.Comments,
                ApprovedAt = DateTime.Now
            };

            string notificationMessage;

            if (request.Status.Equals("approved", StringComparison.OrdinalIgnoreCase))
            {
                approval.Status = ApprovalStatus.Approved;
                expense.Status = ExpenseStatus.Approved;
                notificationMessage = $"Your expense {expense.ExpenseId} has been APPROVED by Admin.";
            }
            else if (request.Status.Equals("rejected", StringComparison.OrdinalIgnoreCase))
            {
                approval.Status = ApprovalStatus.Rejected;
                expense.Status = ExpenseStatus.Rejected;
                notificationMessage = $"Your expense {expense.ExpenseId} has been REJECTED by Admin.";
            }
            else
            {
                throw new ArgumentException("Invalid status. Must be 'approved' or 'rejected'.");
            }

            await _approvalRepo.AddAsync(approval);
            await _expenseRepo.UpdateAsync(expense.ExpenseId, expense);

            var adminUser = users?.FirstOrDefault(u => u.UserId == request.ManagerId);

            await _notificationService.CreateNotification(new CreateNotificationRequestDto
            {
                UserId = expense.UserId,
                Message = notificationMessage,
                Description = string.IsNullOrWhiteSpace(request.Comments)
                                  ? string.Empty
                                  : $"Admin comments: {request.Comments}",
                SenderRole = "System"
            });

            // 🔹 If approved, notify Finance for payment processing
            if (approval.Status == ApprovalStatus.Approved)
            {
                var allUsers = await _userRepo.GetAllAsync() ?? new List<User>();
                var financeUser = allUsers.FirstOrDefault(u => u.Role == UserRole.Finance);
                if (financeUser != null)
                {
                    await _notificationService.CreateNotification(new CreateNotificationRequestDto
                    {
                        UserId = financeUser.UserId,
                        Message = $"Expense '{expense.ExpenseId}' (₹{expense.Amount:N2}) by {adminUser?.UserName ?? expense.UserId} has been approved by Admin and is ready for payment.",
                        Description = $"Category: {expense.CategoryName} | Approved by: {adminUser?.UserName ?? "Admin"}",
                        SenderRole = "System"
                    });
                }
            }

            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
            {
                Action = $"Admin {approval.Status} expense {expense.ExpenseId}",
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
                ApproverName = adminUser?.UserName ?? "",
                DocumentUrls = expense.DocumentUrls ?? new List<string>(),
                ExpenseAmount = expense.Amount,
                AmountInRupees = CurrencyHelper.FormatRupees(expense.Amount)
            };
        }

        // ======================================================
        // GET ALL APPROVALS (Admin)
        // ======================================================
        public async Task<PagedResponse<CreateApprovalResponseDto>> GetAllApprovals(PaginationParams paginationParams)
        {
            var approvals = await _approvalRepo.GetAllAsync();
            var users     = await _userRepo.GetAllAsync();
            var expenses  = await _expenseRepo.GetAllAsync();

            var approvalsList = approvals ?? new List<Approval>();
            var usersList     = users     ?? new List<User>();
            var expensesList  = expenses  ?? new List<Expense>();

<<<<<<< HEAD
            // Build fast lookup dictionaries (safe against duplicates)
            var userMap = usersList.GroupBy(u => u.UserId).ToDictionary(g => g.Key, g => g.First().UserName);
            var expenseMap = expensesList.GroupBy(e => e.ExpenseId).ToDictionary(g => g.Key, g => g.First());

            // Map each approval to its expense and employee name
            var mapped = approvalsList
=======
            var query = approvalsList
>>>>>>> eba5464 (Feature added)
                .Join(usersList,
                    a => a.ManagerId,
                    u => u.UserId,
                    (a, u) => new { approval = a, ApproverName = u.UserName })
<<<<<<< HEAD
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
=======
                .GroupJoin(expensesList,
                    au => au.approval.ExpenseId,
                    e  => e.ExpenseId,
                    (au, expGroup) => new
                    {
                        au.approval,
                        au.ApproverName,
                        expense = expGroup.FirstOrDefault()
                    });
>>>>>>> eba5464 (Feature added)

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
<<<<<<< HEAD
                    ApprovalId = x.approval.ApprovalId,
                    ExpenseId = x.approval.ExpenseId,
                    Status = x.approval.Status.ToString(),
                    Comments = x.approval.Comments,
                    Level = x.approval.Level,
                    ApprovedAt = x.approval.ApprovedAt,
                    ApproverName = x.ApproverName,
                    EmployeeName = x.EmployeeName,
                    DocumentUrls = x.expense?.DocumentUrls ?? new List<string>(),
=======
                    ApprovalId    = x.approval.ApprovalId,
                    ExpenseId     = x.approval.ExpenseId,
                    Status        = x.approval.Status.ToString(),
                    Comments      = x.approval.Comments,
                    Level         = x.approval.Level,
                    ApprovedAt    = x.approval.ApprovedAt,
                    ApproverName  = x.ApproverName,
                    DocumentUrls  = x.expense?.DocumentUrls ?? new List<string>(),
>>>>>>> eba5464 (Feature added)
                    ExpenseAmount = x.expense?.Amount ?? 0,
                    AmountInRupees = x.expense != null
                                     ? CurrencyHelper.FormatRupees(x.expense.Amount)
                                     : string.Empty
                })
                .ToList();

            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
            {
                Action = "Admin viewed approvals list",
                Date   = DateTime.UtcNow
            });

            return new PagedResponse<CreateApprovalResponseDto>(
<<<<<<< HEAD
                pagedApprovals,
                filteredCount,
                paginationParams.PageNumber,
                paginationParams.PageSize
            );
=======
                pagedApprovals, totalRecords,
                paginationParams.PageNumber, paginationParams.PageSize);
>>>>>>> eba5464 (Feature added)
        }

        // ======================================================
        // GET EXPENSES PENDING TEAM LEAD APPROVAL
        // Returns Submitted expenses from Employees or TeamLeads
        // ======================================================
        public async Task<List<CreateExpenseResponseDto>> GetExpensesPendingTeamLeadApproval()
        {
            var expenses = await _expenseRepo.GetAllAsync() ?? new List<Expense>();
            var users    = await _userRepo.GetAllAsync()    ?? new List<User>();

            // Submitted expenses from Employee or TeamLead roles
            var submitterIds = users
                .Where(u => u.Role == UserRole.Employee || u.Role == UserRole.TeamLead)
                .Select(u => u.UserId)
                .ToHashSet();

            return expenses
                .Where(e => e.Status == ExpenseStatus.Submitted && submitterIds.Contains(e.UserId))
                .Select(e => MapExpenseToDto(e))
                .ToList();
        }

        // ======================================================
        // GET EXPENSES PENDING MANAGER APPROVAL
        // Returns Pending (post-TeamLead) + Submitted (Manager's own)
        // ======================================================
        public async Task<List<CreateExpenseResponseDto>> GetExpensesPendingManagerApproval()
        {
            var expenses = await _expenseRepo.GetAllAsync() ?? new List<Expense>();
            var users    = await _userRepo.GetAllAsync()    ?? new List<User>();

            var managerIds = users
                .Where(u => u.Role == UserRole.Manager)
                .Select(u => u.UserId)
                .ToHashSet();

            return expenses
                .Where(e =>
                    e.Status == ExpenseStatus.Pending ||                                    // passed TeamLead
                    (e.Status == ExpenseStatus.Submitted && managerIds.Contains(e.UserId))) // Manager's own
                .Select(e => MapExpenseToDto(e))
                .ToList();
        }

        private static CreateExpenseResponseDto MapExpenseToDto(Expense e) =>
            new CreateExpenseResponseDto
            {
                ExpenseId        = e.ExpenseId,
                CategoryId       = e.CategoryId,
                CategoryName     = e.CategoryName,
                Amount           = e.Amount,
                AmountInRupees   = CurrencyHelper.FormatRupees(e.Amount),
                ExpenseDate      = e.ExpenseDate.ToString("dd-MM-yyyy"),
                Status           = e.Status.ToString(),
                DocumentUrls     = e.DocumentUrls ?? new List<string>(),
                IsAdvanceRequest = e.IsAdvanceRequest,
                CanEdit          = false
            };

        // ── Helper ────────────────────────────────────────────────────────────
        private static CreateApprovalResponseDto BuildResponse(Approval approval, User approver, Expense expense) =>
            new CreateApprovalResponseDto
            {
                ApprovalId     = approval.ApprovalId,
                ExpenseId      = expense.ExpenseId,
                Status         = approval.Status.ToString(),
                Comments       = approval.Comments,
                Level          = approval.Level,
                ApprovedAt     = approval.ApprovedAt,
                ApproverName   = approver.UserName,
                DocumentUrls   = expense.DocumentUrls ?? new List<string>(),
                ExpenseAmount  = expense.Amount,
                AmountInRupees = CurrencyHelper.FormatRupees(expense.Amount)
            };
    }
}
