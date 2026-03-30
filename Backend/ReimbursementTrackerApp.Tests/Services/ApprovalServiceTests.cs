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

        // ── ManagerApproval ───────────────────────────────────────────────────

        [Fact]
        public async Task ManagerApproval_ValidApprove_ReturnsApprovedDto()
        {
            var expense = new Expense
            {
                ExpenseId = "E1", UserId = "EMP1",
                Status = ExpenseStatus.Submitted, Amount = 500
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.AddAsync(It.IsAny<Approval>())).ReturnsAsync((Approval a) => a);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new User { UserId = "MGR1", UserName = "Manager One" }
            });
            SetupAudit(); SetupNotif();

            var svc = CreateService();
            var result = await svc.ManagerApproval(new CreateApprovalRequestDto
            {
                ExpenseId = "E1", ManagerId = "MGR1", Status = "approved"
            });

            result.Should().NotBeNull();
            result!.Status.Should().Be("Approved");
            result.ApproverName.Should().Be("Manager One");
            expense.Status.Should().Be(ExpenseStatus.Approved);
        }

        [Fact]
        public async Task ManagerApproval_ValidReject_ReturnsRejectedDto()
        {
            var expense = new Expense
            {
                ExpenseId = "E2", UserId = "EMP1",
                Status = ExpenseStatus.Submitted, Amount = 200
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.AddAsync(It.IsAny<Approval>())).ReturnsAsync((Approval a) => a);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new User { UserId = "MGR1", UserName = "Manager One" }
            });
            SetupAudit(); SetupNotif();

            var svc = CreateService();
            var result = await svc.ManagerApproval(new CreateApprovalRequestDto
            {
                ExpenseId = "E2", ManagerId = "MGR1", Status = "rejected"
            });

            result!.Status.Should().Be("Rejected");
            expense.Status.Should().Be(ExpenseStatus.Rejected);
        }

        [Fact]
        public async Task ManagerApproval_SelfApproval_ThrowsInvalidOperation()
        {
            var expense = new Expense
            {
                ExpenseId = "E3", UserId = "MGR1",
                Status = ExpenseStatus.Submitted
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });

            var svc = CreateService();
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.ManagerApproval(new CreateApprovalRequestDto
                {
                    ExpenseId = "E3", ManagerId = "MGR1", Status = "approved"
                }));
        }

        [Fact]
        public async Task ManagerApproval_ExpenseNotFound_ThrowsKeyNotFound()
        {
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());

            var svc = CreateService();
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.ManagerApproval(new CreateApprovalRequestDto
                {
                    ExpenseId = "MISSING", ManagerId = "MGR1", Status = "approved"
                }));
        }

        [Fact]
        public async Task ManagerApproval_NotSubmitted_ThrowsInvalidOperation()
        {
            var expense = new Expense
            {
                ExpenseId = "E4", UserId = "EMP1",
                Status = ExpenseStatus.Draft
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });

            var svc = CreateService();
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.ManagerApproval(new CreateApprovalRequestDto
                {
                    ExpenseId = "E4", ManagerId = "MGR1", Status = "approved"
                }));
        }

        // ── GetAllApprovals ───────────────────────────────────────────────────

        [Fact]
        public async Task GetAllApprovals_ReturnsPagedResult()
        {
            var approvals = new List<Approval>
            {
                new Approval { ApprovalId = "A1", ExpenseId = "E1", ManagerId = "MGR1", Status = ApprovalStatus.Approved },
                new Approval { ApprovalId = "A2", ExpenseId = "E2", ManagerId = "MGR1", Status = ApprovalStatus.Rejected }
            };
            var users = new List<User> { new User { UserId = "MGR1", UserName = "Manager One" } };
            var expenses = new List<Expense>
            {
                new Expense { ExpenseId = "E1", UserId = "EMP1", Amount = 100 },
                new Expense { ExpenseId = "E2", UserId = "EMP1", Amount = 200 }
            };

            _approvalRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(approvals);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expenses);
            SetupAudit();

            var svc = CreateService();
            var result = await svc.GetAllApprovals(new PaginationParams { PageNumber = 1, PageSize = 10 });

            result.Data.Should().HaveCount(2);
            result.TotalRecords.Should().Be(2);
        }
    }
}
