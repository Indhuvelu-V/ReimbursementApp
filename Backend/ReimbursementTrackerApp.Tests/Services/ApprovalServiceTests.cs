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
    }
}
