using FluentAssertions;
using Moq;
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models;
using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;
using ReimbursementTrackerApp.Models.Enums;
using ReimbursementTrackerApp.Services;
using Xunit;

namespace ReimbursementTrackerApp.Tests.Services
{
    public class ApprovalServiceTests
    {
        private readonly Mock<IRepository<string, Expense>>  _expenseRepo  = new();
        private readonly Mock<IRepository<string, Approval>> _approvalRepo = new();
        private readonly Mock<IRepository<string, User>>     _userRepo     = new();
        private readonly Mock<INotificationService>          _notifService = new();
        private readonly Mock<IAuditLogService>              _auditService = new();

        private ApprovalService CreateService() =>
            new(_expenseRepo.Object, _approvalRepo.Object, _userRepo.Object,
                _notifService.Object, _auditService.Object);

        private void SetupAudit() =>
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

        private void SetupNotif() =>
            _notifService.Setup(n => n.CreateNotification(It.IsAny<CreateNotificationRequestDto>()))
                .ReturnsAsync(new CreateNotificationResponseDto());

        private Expense MakeSubmittedExpense(string id = "E1", string userId = "EMP1") =>
            new() { ExpenseId = id, UserId = userId, Status = ExpenseStatus.Submitted, Amount = 500 };

        // ── ManagerApproval ───────────────────────────────────────────────────

        [Fact]
        public async Task ManagerApproval_Approve_SetsExpenseApproved()
        {
            var expense = MakeSubmittedExpense();
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.AddAsync(It.IsAny<Approval>())).ReturnsAsync((Approval a) => a);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { new() { UserId = "MGR1", UserName = "Manager One" } });
            SetupAudit(); SetupNotif();

            var result = await CreateService().ManagerApproval(new CreateApprovalRequestDto
            {
                ExpenseId = "E1", ManagerId = "MGR1", Status = "approved"
            });

            result!.Status.Should().Be("Approved");
            result.ApproverName.Should().Be("Manager One");
            expense.Status.Should().Be(ExpenseStatus.Approved);
        }

        [Fact]
        public async Task ManagerApproval_Reject_SetsExpenseRejected()
        {
            var expense = MakeSubmittedExpense("E2");
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.AddAsync(It.IsAny<Approval>())).ReturnsAsync((Approval a) => a);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { new() { UserId = "MGR1", UserName = "Manager One" } });
            SetupAudit(); SetupNotif();

            var result = await CreateService().ManagerApproval(new CreateApprovalRequestDto
            {
                ExpenseId = "E2", ManagerId = "MGR1", Status = "rejected", Comments = "Not valid"
            });

            result!.Status.Should().Be("Rejected");
            expense.Status.Should().Be(ExpenseStatus.Rejected);
        }

        [Fact]
        public async Task ManagerApproval_SelfApproval_ThrowsInvalidOperation()
        {
            var expense = MakeSubmittedExpense("E3", "MGR1");
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService().ManagerApproval(new CreateApprovalRequestDto
                {
                    ExpenseId = "E3", ManagerId = "MGR1", Status = "approved"
                }));
        }

        [Fact]
        public async Task ManagerApproval_ExpenseNotFound_ThrowsKeyNotFound()
        {
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                CreateService().ManagerApproval(new CreateApprovalRequestDto
                {
                    ExpenseId = "MISSING", ManagerId = "MGR1", Status = "approved"
                }));
        }

        [Fact]
        public async Task ManagerApproval_NotSubmittedStatus_ThrowsInvalidOperation()
        {
            var expense = new Expense { ExpenseId = "E4", UserId = "EMP1", Status = ExpenseStatus.Draft };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService().ManagerApproval(new CreateApprovalRequestDto
                {
                    ExpenseId = "E4", ManagerId = "MGR1", Status = "approved"
                }));
        }

        [Fact]
        public async Task ManagerApproval_InvalidStatus_ThrowsArgumentException()
        {
            var expense = MakeSubmittedExpense("E5");
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });

            await Assert.ThrowsAsync<ArgumentException>(() =>
                CreateService().ManagerApproval(new CreateApprovalRequestDto
                {
                    ExpenseId = "E5", ManagerId = "MGR1", Status = "pending"
                }));
        }

        [Fact]
        public async Task ManagerApproval_WithComments_IncludesCommentsInResponse()
        {
            var expense = MakeSubmittedExpense("E6");
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.AddAsync(It.IsAny<Approval>())).ReturnsAsync((Approval a) => a);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { new() { UserId = "MGR1", UserName = "Mgr" } });
            SetupAudit(); SetupNotif();

            var result = await CreateService().ManagerApproval(new CreateApprovalRequestDto
            {
                ExpenseId = "E6", ManagerId = "MGR1", Status = "approved", Comments = "Looks good"
            });

            result!.Comments.Should().Be("Looks good");
        }

        // ── GetAllApprovals ───────────────────────────────────────────────────

        [Fact]
        public async Task GetAllApprovals_ReturnsPagedResult()
        {
            var approvals = new List<Approval>
            {
                new() { ApprovalId = "A1", ExpenseId = "E1", ManagerId = "MGR1", Status = ApprovalStatus.Approved },
                new() { ApprovalId = "A2", ExpenseId = "E2", ManagerId = "MGR1", Status = ApprovalStatus.Rejected }
            };
            _approvalRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(approvals);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { new() { UserId = "MGR1", UserName = "Manager One" } });
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>
            {
                new() { ExpenseId = "E1", UserId = "EMP1", Amount = 100 },
                new() { ExpenseId = "E2", UserId = "EMP1", Amount = 200 }
            });
            SetupAudit();

            var result = await CreateService().GetAllApprovals(new PaginationParams { PageNumber = 1, PageSize = 10 });
            result.Data.Should().HaveCount(2);
            result.TotalRecords.Should().Be(2);
        }

        [Fact]
        public async Task GetAllApprovals_UserNameFilter_ReturnsMatchingApprovals()
        {
            var approvals = new List<Approval>
            {
                new() { ApprovalId = "A1", ExpenseId = "E1", ManagerId = "MGR1", Status = ApprovalStatus.Approved },
                new() { ApprovalId = "A2", ExpenseId = "E2", ManagerId = "MGR2", Status = ApprovalStatus.Approved }
            };
            _approvalRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(approvals);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "MGR1", UserName = "Alice Manager" },
                new() { UserId = "MGR2", UserName = "Bob Manager" }
            });
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>
            {
                new() { ExpenseId = "E1", UserId = "EMP1", Amount = 100 },
                new() { ExpenseId = "E2", UserId = "EMP2", Amount = 200 }
            });
            SetupAudit();

            var result = await CreateService().GetAllApprovals(new PaginationParams
            {
                PageNumber = 1, PageSize = 10, UserName = "Alice"
            });
            result.Data.Should().HaveCount(1);
            result.Data.First().ApprovalId.Should().Be("A1");
        }

        [Fact]
        public async Task GetAllApprovals_Pagination_ReturnsCorrectPage()
        {
            var approvals = Enumerable.Range(1, 8)
                .Select(i => new Approval { ApprovalId = $"A{i}", ExpenseId = $"E{i}", ManagerId = "MGR1", Status = ApprovalStatus.Approved })
                .ToList();
            var expenses = Enumerable.Range(1, 8)
                .Select(i => new Expense { ExpenseId = $"E{i}", UserId = "EMP1", Amount = 100 })
                .ToList();
            _approvalRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(approvals);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { new() { UserId = "MGR1", UserName = "Mgr" } });
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expenses);
            SetupAudit();

            var result = await CreateService().GetAllApprovals(new PaginationParams { PageNumber = 2, PageSize = 3 });
            result.Data.Should().HaveCount(3);
        }

        // ── Manager/Finance expense restriction ───────────────────────────

        [Fact]
        public async Task ManagerApproval_ManagerExpense_ThrowsInvalidOperation()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "MGR2", Status = ExpenseStatus.Submitted };
            var expenseOwner = new User { UserId = "MGR2", UserName = "OtherMgr", Role = UserRole.Manager };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { expenseOwner });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService().ManagerApproval(new CreateApprovalRequestDto
                { ExpenseId = "E1", ManagerId = "MGR1", Status = "approved" }));
        }

        [Fact]
        public async Task ManagerApproval_FinanceExpense_ThrowsInvalidOperation()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "FIN1", Status = ExpenseStatus.Submitted };
            var expenseOwner = new User { UserId = "FIN1", UserName = "FinUser", Role = UserRole.Finance };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { expenseOwner });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService().ManagerApproval(new CreateApprovalRequestDto
                { ExpenseId = "E1", ManagerId = "MGR1", Status = "approved" }));
        }
    }

    // ── Additional branch coverage ────────────────────────────────────────────

    public class ApprovalServiceBranchTests
    {
        private readonly Mock<IRepository<string, Expense>>  _expenseRepo  = new();
        private readonly Mock<IRepository<string, Approval>> _approvalRepo = new();
        private readonly Mock<IRepository<string, User>>     _userRepo     = new();
        private readonly Mock<INotificationService>          _notifService = new();
        private readonly Mock<IAuditLogService>              _auditService = new();

        private ApprovalService Svc() =>
            new(_expenseRepo.Object, _approvalRepo.Object, _userRepo.Object,
                _notifService.Object, _auditService.Object);

        private void SetupAudit() =>
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

        private void SetupNotif() =>
            _notifService.Setup(n => n.CreateNotification(It.IsAny<CreateNotificationRequestDto>()))
                .ReturnsAsync(new CreateNotificationResponseDto());

        // Branch: ManagerApproval — managerUser not found → ApproverName = ""
        [Fact]
        public async Task ManagerApproval_ManagerNotInUserList_ApproverNameEmpty()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Submitted, Amount = 500 };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.AddAsync(It.IsAny<Approval>())).ReturnsAsync((Approval a) => a);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>()); // no users
            SetupAudit(); SetupNotif();

            var result = await Svc().ManagerApproval(new CreateApprovalRequestDto
            { ExpenseId = "E1", ManagerId = "MGR1", Status = "approved" });

            result!.ApproverName.Should().Be("");
        }

        // Branch: ManagerApproval — with comments → notification includes comments
        [Fact]
        public async Task ManagerApproval_WithComments_NotificationIncludesComments()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Submitted, Amount = 500 };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.AddAsync(It.IsAny<Approval>())).ReturnsAsync((Approval a) => a);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { new() { UserId = "MGR1", UserName = "Mgr" } });
            SetupAudit();

            CreateNotificationRequestDto? captured = null;
            _notifService.Setup(n => n.CreateNotification(It.IsAny<CreateNotificationRequestDto>()))
                .Callback<CreateNotificationRequestDto>(r => captured = r)
                .ReturnsAsync(new CreateNotificationResponseDto());

            await Svc().ManagerApproval(new CreateApprovalRequestDto
            { ExpenseId = "E1", ManagerId = "MGR1", Status = "rejected", Comments = "Not valid" });

            captured!.Description.Should().Contain("Not valid");
        }

        // Branch: ManagerApproval — empty comments → description is empty
        [Fact]
        public async Task ManagerApproval_EmptyComments_DescriptionEmpty()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Submitted, Amount = 500 };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.AddAsync(It.IsAny<Approval>())).ReturnsAsync((Approval a) => a);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { new() { UserId = "MGR1", UserName = "Mgr" } });
            SetupAudit();

            CreateNotificationRequestDto? captured = null;
            _notifService.Setup(n => n.CreateNotification(It.IsAny<CreateNotificationRequestDto>()))
                .Callback<CreateNotificationRequestDto>(r => captured = r)
                .ReturnsAsync(new CreateNotificationResponseDto());

            await Svc().ManagerApproval(new CreateApprovalRequestDto
            { ExpenseId = "E1", ManagerId = "MGR1", Status = "approved", Comments = "" });

            captured!.Description.Should().Be(string.Empty);
        }

        // Branch: GetAllApprovals — expense not in map → DocumentUrls empty, Amount 0
        [Fact]
        public async Task GetAllApprovals_ExpenseNotFound_DefaultsApplied()
        {
            var approvals = new List<Approval>
            {
                new() { ApprovalId = "A1", ExpenseId = "MISSING", ManagerId = "MGR1", Status = ApprovalStatus.Approved }
            };
            _approvalRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(approvals);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { new() { UserId = "MGR1", UserName = "Mgr" } });
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            SetupAudit();

            var result = await Svc().GetAllApprovals(new PaginationParams { PageNumber = 1, PageSize = 10 });

            result.Data.First().ExpenseAmount.Should().Be(0);
            result.Data.First().DocumentUrls.Should().BeEmpty();
        }

        // Branch: GetAllApprovals — employee name not in userMap → EmployeeName = ""
        [Fact]
        public async Task GetAllApprovals_EmployeeNotInUserMap_EmployeeNameEmpty()
        {
            var approvals = new List<Approval>
            {
                new() { ApprovalId = "A1", ExpenseId = "E1", ManagerId = "MGR1", Status = ApprovalStatus.Approved }
            };
            _approvalRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(approvals);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { new() { UserId = "MGR1", UserName = "Mgr" } });
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>
            {
                new() { ExpenseId = "E1", UserId = "EMP_UNKNOWN", Amount = 100 }
            });
            SetupAudit();

            var result = await Svc().GetAllApprovals(new PaginationParams { PageNumber = 1, PageSize = 10 });
            result.Data.First().EmployeeName.Should().Be("");
        }

        // Branch: GetAllApprovals — empty approvals list
        [Fact]
        public async Task GetAllApprovals_EmptyApprovals_ReturnsEmpty()
        {
            _approvalRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Approval>());
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            SetupAudit();

            var result = await Svc().GetAllApprovals(new PaginationParams { PageNumber = 1, PageSize = 10 });
            result.Data.Should().BeEmpty();
        }
    }
}

namespace ReimbursementTrackerApp.Tests.Services
{
    public class AdminApprovalTests
    {
        private readonly Mock<IRepository<string, Expense>>  _expenseRepo  = new();
        private readonly Mock<IRepository<string, Approval>> _approvalRepo = new();
        private readonly Mock<IRepository<string, User>>     _userRepo     = new();
        private readonly Mock<INotificationService>          _notifService = new();
        private readonly Mock<IAuditLogService>              _auditService = new();

        private ApprovalService Svc() =>
            new(_expenseRepo.Object, _approvalRepo.Object, _userRepo.Object,
                _notifService.Object, _auditService.Object);

        private void SetupAudit() =>
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

        private void SetupNotif() =>
            _notifService.Setup(n => n.CreateNotification(It.IsAny<CreateNotificationRequestDto>()))
                .ReturnsAsync(new CreateNotificationResponseDto());

        private Expense ManagerExpense(string id = "E1", string userId = "MGR1") =>
            new() { ExpenseId = id, UserId = userId, Status = ExpenseStatus.Submitted, Amount = 800, CategoryName = "Travel" };

        // ── AdminApproval: approve ────────────────────────────────────────────

        [Fact]
        public async Task AdminApproval_Approve_SetsExpenseApproved()
        {
            var expense = ManagerExpense();
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.AddAsync(It.IsAny<Approval>())).ReturnsAsync((Approval a) => a);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "MGR1",   UserName = "Manager One", Role = UserRole.Manager },
                new() { UserId = "ADMIN1", UserName = "Admin",       Role = UserRole.Admin }
            });
            SetupAudit(); SetupNotif();

            var result = await Svc().AdminApproval(new CreateApprovalRequestDto
            { ExpenseId = "E1", ManagerId = "ADMIN1", Status = "approved" });

            result!.Status.Should().Be("Approved");
            result.Level.Should().Be("Admin");
            expense.Status.Should().Be(ExpenseStatus.Approved);
        }

        [Fact]
        public async Task AdminApproval_Reject_SetsExpenseRejected()
        {
            var expense = ManagerExpense("E2");
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.AddAsync(It.IsAny<Approval>())).ReturnsAsync((Approval a) => a);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "MGR1",   UserName = "Manager One", Role = UserRole.Manager },
                new() { UserId = "ADMIN1", UserName = "Admin",       Role = UserRole.Admin }
            });
            SetupAudit(); SetupNotif();

            var result = await Svc().AdminApproval(new CreateApprovalRequestDto
            { ExpenseId = "E2", ManagerId = "ADMIN1", Status = "rejected", Comments = "Not valid" });

            result!.Status.Should().Be("Rejected");
            expense.Status.Should().Be(ExpenseStatus.Rejected);
        }

        [Fact]
        public async Task AdminApproval_ExpenseNotFound_ThrowsKeyNotFound()
        {
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                Svc().AdminApproval(new CreateApprovalRequestDto
                { ExpenseId = "MISSING", ManagerId = "ADMIN1", Status = "approved" }));
        }

        [Fact]
        public async Task AdminApproval_NotSubmittedStatus_ThrowsInvalidOperation()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "MGR1", Status = ExpenseStatus.Draft };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "MGR1", Role = UserRole.Manager }
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Svc().AdminApproval(new CreateApprovalRequestDto
                { ExpenseId = "E1", ManagerId = "ADMIN1", Status = "approved" }));
        }

        [Fact]
        public async Task AdminApproval_EmployeeExpense_ThrowsInvalidOperation()
        {
            // Admin approval is only for Manager/Finance expenses
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Submitted };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "EMP1", Role = UserRole.Employee }
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Svc().AdminApproval(new CreateApprovalRequestDto
                { ExpenseId = "E1", ManagerId = "ADMIN1", Status = "approved" }));
        }

        [Fact]
        public async Task AdminApproval_FinanceExpense_Approve_Succeeds()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "FIN1", Status = ExpenseStatus.Submitted, Amount = 600, CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.AddAsync(It.IsAny<Approval>())).ReturnsAsync((Approval a) => a);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "FIN1",   UserName = "Finance User", Role = UserRole.Finance },
                new() { UserId = "ADMIN1", UserName = "Admin",        Role = UserRole.Admin }
            });
            SetupAudit(); SetupNotif();

            var result = await Svc().AdminApproval(new CreateApprovalRequestDto
            { ExpenseId = "E1", ManagerId = "ADMIN1", Status = "approved" });

            result!.Status.Should().Be("Approved");
        }

        [Fact]
        public async Task AdminApproval_InvalidStatus_ThrowsArgumentException()
        {
            var expense = ManagerExpense();
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "MGR1", Role = UserRole.Manager }
            });

            await Assert.ThrowsAsync<ArgumentException>(() =>
                Svc().AdminApproval(new CreateApprovalRequestDto
                { ExpenseId = "E1", ManagerId = "ADMIN1", Status = "pending" }));
        }

        [Fact]
        public async Task AdminApproval_Approve_NotifiesFinance()
        {
            var expense = ManagerExpense();
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.AddAsync(It.IsAny<Approval>())).ReturnsAsync((Approval a) => a);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "MGR1",   UserName = "Manager One", Role = UserRole.Manager },
                new() { UserId = "ADMIN1", UserName = "Admin",       Role = UserRole.Admin },
                new() { UserId = "FIN1",   UserName = "Finance",     Role = UserRole.Finance }
            });
            SetupAudit(); SetupNotif();

            await Svc().AdminApproval(new CreateApprovalRequestDto
            { ExpenseId = "E1", ManagerId = "ADMIN1", Status = "approved" });

            // Should notify expense owner + finance (2 notifications)
            _notifService.Verify(n => n.CreateNotification(It.IsAny<CreateNotificationRequestDto>()), Times.Exactly(2));
        }

        [Fact]
        public async Task AdminApproval_Reject_OnlyNotifiesOwner()
        {
            var expense = ManagerExpense();
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.AddAsync(It.IsAny<Approval>())).ReturnsAsync((Approval a) => a);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "MGR1",   UserName = "Manager One", Role = UserRole.Manager },
                new() { UserId = "ADMIN1", UserName = "Admin",       Role = UserRole.Admin },
                new() { UserId = "FIN1",   UserName = "Finance",     Role = UserRole.Finance }
            });
            SetupAudit(); SetupNotif();

            await Svc().AdminApproval(new CreateApprovalRequestDto
            { ExpenseId = "E1", ManagerId = "ADMIN1", Status = "rejected" });

            // Only owner notified on rejection
            _notifService.Verify(n => n.CreateNotification(It.IsAny<CreateNotificationRequestDto>()), Times.Once);
        }

        [Fact]
        public async Task AdminApproval_OwnerNotFound_ThrowsInvalidOperation()
        {
            var expense = ManagerExpense();
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>()); // no users

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Svc().AdminApproval(new CreateApprovalRequestDto
                { ExpenseId = "E1", ManagerId = "ADMIN1", Status = "approved" }));
        }

        [Fact]
        public async Task AdminApproval_MapsDocumentUrlsAndAmount()
        {
            var expense = new Expense
            {
                ExpenseId = "E1", UserId = "MGR1", Status = ExpenseStatus.Submitted,
                Amount = 800, CategoryName = "Travel",
                DocumentUrlsJson = System.Text.Json.JsonSerializer.Serialize(new List<string> { "/uploads/doc.pdf" })
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.AddAsync(It.IsAny<Approval>())).ReturnsAsync((Approval a) => a);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "MGR1",   UserName = "Manager One", Role = UserRole.Manager },
                new() { UserId = "ADMIN1", UserName = "Admin",       Role = UserRole.Admin }
            });
            SetupAudit(); SetupNotif();

            var result = await Svc().AdminApproval(new CreateApprovalRequestDto
            { ExpenseId = "E1", ManagerId = "ADMIN1", Status = "approved" });

            result!.ExpenseAmount.Should().Be(800);
        }
    }
}

namespace ReimbursementTrackerApp.Tests.Services
{
    public class ApprovalServiceCoverageTests
    {
        private readonly Mock<IRepository<string, Expense>>  _expenseRepo  = new();
        private readonly Mock<IRepository<string, Approval>> _approvalRepo = new();
        private readonly Mock<IRepository<string, User>>     _userRepo     = new();
        private readonly Mock<INotificationService>          _notifService = new();
        private readonly Mock<IAuditLogService>              _auditService = new();

        private ApprovalService Svc() =>
            new(_expenseRepo.Object, _approvalRepo.Object, _userRepo.Object,
                _notifService.Object, _auditService.Object);

        private void SetupAudit() =>
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

        private void SetupNotif() =>
            _notifService.Setup(n => n.CreateNotification(It.IsAny<CreateNotificationRequestDto>()))
                .ReturnsAsync(new CreateNotificationResponseDto());

        // ── ManagerApproval: approved → notifies Finance ──────────────────────
        [Fact]
        public async Task ManagerApproval_Approve_NotifiesFinance()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Submitted, Amount = 500, CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.AddAsync(It.IsAny<Approval>())).ReturnsAsync((Approval a) => a);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "EMP1", UserName = "Employee", Role = UserRole.Employee },
                new() { UserId = "MGR1", UserName = "Manager",  Role = UserRole.Manager },
                new() { UserId = "FIN1", UserName = "Finance",  Role = UserRole.Finance }
            });
            SetupAudit(); SetupNotif();

            await Svc().ManagerApproval(new CreateApprovalRequestDto
            { ExpenseId = "E1", ManagerId = "MGR1", Status = "approved" });

            // Employee notified + Finance notified = 2 calls
            _notifService.Verify(n => n.CreateNotification(It.IsAny<CreateNotificationRequestDto>()), Times.Exactly(2));
        }

        // ── ManagerApproval: rejected → only employee notified ────────────────
        [Fact]
        public async Task ManagerApproval_Reject_OnlyNotifiesEmployee()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Submitted, Amount = 500, CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.AddAsync(It.IsAny<Approval>())).ReturnsAsync((Approval a) => a);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "EMP1", UserName = "Employee", Role = UserRole.Employee },
                new() { UserId = "MGR1", UserName = "Manager",  Role = UserRole.Manager },
                new() { UserId = "FIN1", UserName = "Finance",  Role = UserRole.Finance }
            });
            SetupAudit(); SetupNotif();

            await Svc().ManagerApproval(new CreateApprovalRequestDto
            { ExpenseId = "E1", ManagerId = "MGR1", Status = "rejected" });

            _notifService.Verify(n => n.CreateNotification(It.IsAny<CreateNotificationRequestDto>()), Times.Once);
        }

        // ── ManagerApproval: approved, no Finance user → no second notif ──────
        [Fact]
        public async Task ManagerApproval_Approve_NoFinanceUser_OnlyOneNotif()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Submitted, Amount = 500, CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.AddAsync(It.IsAny<Approval>())).ReturnsAsync((Approval a) => a);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "EMP1", UserName = "Employee", Role = UserRole.Employee },
                new() { UserId = "MGR1", UserName = "Manager",  Role = UserRole.Manager }
                // no Finance user
            });
            SetupAudit(); SetupNotif();

            await Svc().ManagerApproval(new CreateApprovalRequestDto
            { ExpenseId = "E1", ManagerId = "MGR1", Status = "approved" });

            _notifService.Verify(n => n.CreateNotification(It.IsAny<CreateNotificationRequestDto>()), Times.Once);
        }

        // ── ManagerApproval: maps DocumentUrls and Amount ─────────────────────
        [Fact]
        public async Task ManagerApproval_MapsDocumentUrlsAndAmount()
        {
            var expense = new Expense
            {
                ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Submitted,
                Amount = 750, CategoryName = "Travel",
                DocumentUrlsJson = System.Text.Json.JsonSerializer.Serialize(new List<string> { "/uploads/receipt.pdf" })
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.AddAsync(It.IsAny<Approval>())).ReturnsAsync((Approval a) => a);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "EMP1", UserName = "Employee", Role = UserRole.Employee },
                new() { UserId = "MGR1", UserName = "Manager",  Role = UserRole.Manager }
            });
            SetupAudit(); SetupNotif();

            var result = await Svc().ManagerApproval(new CreateApprovalRequestDto
            { ExpenseId = "E1", ManagerId = "MGR1", Status = "approved" });

            result!.ExpenseAmount.Should().Be(750);
            result.DocumentUrls.Should().Contain("/uploads/receipt.pdf");
        }

        // ── GetAllApprovals: UserName filter matches employee name ────────────
        [Fact]
        public async Task GetAllApprovals_UserNameFilter_MatchesEmployeeName()
        {
            var approvals = new List<Approval>
            {
                new() { ApprovalId = "A1", ExpenseId = "E1", ManagerId = "MGR1", Status = ApprovalStatus.Approved },
                new() { ApprovalId = "A2", ExpenseId = "E2", ManagerId = "MGR1", Status = ApprovalStatus.Approved }
            };
            _approvalRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(approvals);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "MGR1", UserName = "Manager One" },
                new() { UserId = "EMP1", UserName = "Alice Employee" },
                new() { UserId = "EMP2", UserName = "Bob Employee" }
            });
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>
            {
                new() { ExpenseId = "E1", UserId = "EMP1", Amount = 100 },
                new() { ExpenseId = "E2", UserId = "EMP2", Amount = 200 }
            });
            SetupAudit();

            var result = await Svc().GetAllApprovals(new PaginationParams
            { PageNumber = 1, PageSize = 10, UserName = "Alice" });

            result.Data.Should().HaveCount(1);
            result.Data.First().EmployeeName.Should().Be("Alice Employee");
        }

        // ── GetAllApprovals: null approvals → empty ───────────────────────────
        [Fact]
        public async Task GetAllApprovals_NullApprovals_ReturnsEmpty()
        {
            _approvalRepo.Setup(r => r.GetAllAsync()).ReturnsAsync((IEnumerable<Approval>?)null);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            SetupAudit();

            var result = await Svc().GetAllApprovals(new PaginationParams { PageNumber = 1, PageSize = 10 });
            result.Data.Should().BeEmpty();
        }

        // ── GetAllApprovals: null users → empty (join produces nothing) ───────
        [Fact]
        public async Task GetAllApprovals_NullUsers_ReturnsEmpty()
        {
            var approvals = new List<Approval>
            {
                new() { ApprovalId = "A1", ExpenseId = "E1", ManagerId = "MGR1", Status = ApprovalStatus.Approved }
            };
            _approvalRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(approvals);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync((IEnumerable<User>?)null);
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            SetupAudit();

            var result = await Svc().GetAllApprovals(new PaginationParams { PageNumber = 1, PageSize = 10 });
            result.Data.Should().BeEmpty();
        }

        // ── GetAllApprovals: pagination ───────────────────────────────────────
        [Fact]
        public async Task GetAllApprovals_Pagination_SecondPage()
        {
            var approvals = Enumerable.Range(1, 10)
                .Select(i => new Approval { ApprovalId = $"A{i}", ExpenseId = $"E{i}", ManagerId = "MGR1", Status = ApprovalStatus.Approved })
                .ToList();
            var expenses = Enumerable.Range(1, 10)
                .Select(i => new Expense { ExpenseId = $"E{i}", UserId = "EMP1", Amount = 100 })
                .ToList();
            _approvalRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(approvals);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { new() { UserId = "MGR1", UserName = "Mgr" } });
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expenses);
            SetupAudit();

            var result = await Svc().GetAllApprovals(new PaginationParams { PageNumber = 2, PageSize = 4 });
            result.Data.Should().HaveCount(4);
            result.TotalRecords.Should().Be(10);
        }

        // ── AdminApproval: no Finance user → only owner notified ─────────────
        [Fact]
        public async Task AdminApproval_Approve_NoFinanceUser_OnlyOwnerNotified()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "MGR1", Status = ExpenseStatus.Submitted, Amount = 800, CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.AddAsync(It.IsAny<Approval>())).ReturnsAsync((Approval a) => a);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "MGR1",   UserName = "Manager One", Role = UserRole.Manager },
                new() { UserId = "ADMIN1", UserName = "Admin",       Role = UserRole.Admin }
                // no Finance
            });
            SetupAudit(); SetupNotif();

            await Svc().AdminApproval(new CreateApprovalRequestDto
            { ExpenseId = "E1", ManagerId = "ADMIN1", Status = "approved" });

            _notifService.Verify(n => n.CreateNotification(It.IsAny<CreateNotificationRequestDto>()), Times.Once);
        }

        // ── AdminApproval: admin user not found → ApproverName empty ─────────
        [Fact]
        public async Task AdminApproval_AdminNotInUserList_ApproverNameEmpty()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "MGR1", Status = ExpenseStatus.Submitted, Amount = 800, CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.AddAsync(It.IsAny<Approval>())).ReturnsAsync((Approval a) => a);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "MGR1", UserName = "Manager One", Role = UserRole.Manager }
                // ADMIN1 not in list
            });
            SetupAudit(); SetupNotif();

            var result = await Svc().AdminApproval(new CreateApprovalRequestDto
            { ExpenseId = "E1", ManagerId = "ADMIN1", Status = "approved" });

            result!.ApproverName.Should().Be("");
        }
    }
}
