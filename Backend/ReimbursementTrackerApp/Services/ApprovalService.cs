

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

            // Join approvals → users (approver name) + expenses (image paths)
            var query = approvalsList
                .Join(usersList,
                    a => a.ManagerId,
                    u => u.UserId,
                    (a, u) => new { approval = a, ApproverName = u.UserName })
                .GroupJoin(expensesList,
                    au => au.approval.ExpenseId,
                    e => e.ExpenseId,
                    (au, expGroup) => new
                    {
                        au.approval,
                        au.ApproverName,
                        expense = expGroup.FirstOrDefault()  // ✅ matching expense for image
                    });

            var totalRecords = query.Count();

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

                    // ✅ image paths from matched expense
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
                totalRecords,
                paginationParams.PageNumber,
                paginationParams.PageSize
            );
        }
    }
}
