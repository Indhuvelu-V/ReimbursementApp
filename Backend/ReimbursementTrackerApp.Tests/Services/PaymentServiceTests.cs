using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models;
using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;
using ReimbursementTrackerApp.Models.Enums;
using ReimbursementTrackerApp.Services;
using Xunit;

namespace ReimbursementTrackerApp.Tests.Services
{
    public class PaymentServiceTests
    {
        private readonly Mock<IRepository<string, Payment>> _paymentRepo  = new();
        private readonly Mock<IRepository<string, Expense>> _expenseRepo  = new();
        private readonly Mock<IRepository<string, User>>    _userRepo     = new();
        private readonly Mock<INotificationService>         _notifService = new();
        private readonly Mock<IHttpContextAccessor>         _httpContext  = new();
        private readonly Mock<IAuditLogService>             _auditService = new();

        private PaymentService CreateService(string role = "Finance")
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "FIN1"),
                new(ClaimTypes.Name, "Finance User"),
                new(ClaimTypes.Role, role)
            };
            var ctx = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) };
            _httpContext.Setup(h => h.HttpContext).Returns(ctx);
            return new PaymentService(
                _paymentRepo.Object, _expenseRepo.Object,
                _userRepo.Object, _notifService.Object,
                _httpContext.Object, _auditService.Object);
        }

        private void SetupNotif() =>
            _notifService.Setup(n => n.CreateNotification(It.IsAny<CreateNotificationRequestDto>()))
                .ReturnsAsync(new CreateNotificationResponseDto());

        private void SetupAudit() =>
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

        // ── CompletePayment ───────────────────────────────────────────────────

        [Fact]
        public async Task CompletePayment_ValidApprovedExpense_ReturnsPaidDto()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Amount = 1000, Status = ExpenseStatus.Approved };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _paymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);
            _paymentRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Payment>())).ReturnsAsync((string k, Payment p) => p);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new User { UserId = "EMP1", UserName = "Employee" });
            SetupNotif(); SetupAudit();

            var result = await CreateService().CompletePayment("E1", "REF001", "BankTransfer");

            result.Should().NotBeNull();
            result!.PaymentStatus.Should().Be("Paid");
            result.AmountPaid.Should().Be(1000);
            result.ReferenceNo.Should().Be("REF001");
            expense.Status.Should().Be(ExpenseStatus.Paid);
        }

        [Fact]
        public async Task CompletePayment_ExpenseNotFound_ThrowsKeyNotFound()
        {
            _expenseRepo.Setup(r => r.GetByIdAsync("MISSING")).ThrowsAsync(new KeyNotFoundException());
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                CreateService().CompletePayment("MISSING", "REF", "Cash"));
        }

        [Fact]
        public async Task CompletePayment_NotApprovedStatus_ThrowsInvalidOperation()
        {
            var expense = new Expense { ExpenseId = "E2", UserId = "EMP1", Amount = 500, Status = ExpenseStatus.Submitted };
            _expenseRepo.Setup(r => r.GetByIdAsync("E2")).ReturnsAsync(expense);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService().CompletePayment("E2", "REF", "Cash"));
        }

        [Fact]
        public async Task CompletePayment_AlreadyPaid_ThrowsInvalidOperation()
        {
            var payment = new Payment { PaymentId = "P1", PaymentStatus = PaymentStatusEnum.Paid };
            var expense = new Expense
            {
                ExpenseId = "E3", UserId = "EMP1", Amount = 500,
                Status = ExpenseStatus.Approved,
                Payments = new List<Payment> { payment }
            };
            _expenseRepo.Setup(r => r.GetByIdAsync("E3")).ReturnsAsync(expense);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService().CompletePayment("E3", "REF", "Cash"));
        }

        [Fact]
        public async Task CompletePayment_NonFinanceRole_ThrowsUnauthorized()
        {
            var expense = new Expense { ExpenseId = "E4", UserId = "EMP1", Amount = 500, Status = ExpenseStatus.Approved };
            _expenseRepo.Setup(r => r.GetByIdAsync("E4")).ReturnsAsync(expense);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                CreateService("Employee").CompletePayment("E4", "REF", "Cash"));
        }

        // ── GetAllPayments ────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllPayments_NoFilters_ReturnsAllPaged()
        {
            var payments = new List<Payment>
            {
                new() { PaymentId = "P1", ExpenseId = "E1", UserId = "U1", AmountPaid = 100, PaymentStatus = PaymentStatusEnum.Paid, PaymentDate = DateTime.UtcNow },
                new() { PaymentId = "P2", ExpenseId = "E2", UserId = "U2", AmountPaid = 200, PaymentStatus = PaymentStatusEnum.Paid, PaymentDate = DateTime.UtcNow }
            };
            _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(payments);
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "U1", UserName = "Alice" },
                new() { UserId = "U2", UserName = "Bob" }
            });

            var result = await CreateService().GetAllPayments(new PaginationParams { PageNumber = 1, PageSize = 10 });
            result.Data.Should().HaveCount(2);
            result.TotalRecords.Should().Be(2);
        }

        [Fact]
        public async Task GetAllPayments_StatusFilter_ReturnsOnlyPaid()
        {
            var payments = new List<Payment>
            {
                new() { PaymentId = "P1", UserId = "U1", AmountPaid = 100, PaymentStatus = PaymentStatusEnum.Paid,    PaymentDate = DateTime.UtcNow },
                new() { PaymentId = "P2", UserId = "U1", AmountPaid = 200, PaymentStatus = PaymentStatusEnum.Pending, PaymentDate = DateTime.UtcNow }
            };
            _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(payments);
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { new() { UserId = "U1", UserName = "Alice" } });

            var result = await CreateService().GetAllPayments(new PaginationParams { PageNumber = 1, PageSize = 10, Status = "Paid" });
            result.Data.Should().HaveCount(1);
            result.Data.First().PaymentStatus.Should().Be("Paid");
        }

        [Fact]
        public async Task GetAllPayments_AmountFilter_ReturnsInRange()
        {
            var payments = new List<Payment>
            {
                new() { PaymentId = "P1", UserId = "U1", AmountPaid = 100, PaymentStatus = PaymentStatusEnum.Paid, PaymentDate = DateTime.UtcNow },
                new() { PaymentId = "P2", UserId = "U1", AmountPaid = 500, PaymentStatus = PaymentStatusEnum.Paid, PaymentDate = DateTime.UtcNow },
                new() { PaymentId = "P3", UserId = "U1", AmountPaid = 900, PaymentStatus = PaymentStatusEnum.Paid, PaymentDate = DateTime.UtcNow }
            };
            _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(payments);
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { new() { UserId = "U1", UserName = "Alice" } });

            var result = await CreateService().GetAllPayments(new PaginationParams { PageNumber = 1, PageSize = 10, MinAmount = 200, MaxAmount = 800 });
            result.Data.Should().HaveCount(1);
            result.Data.First().AmountPaid.Should().Be(500);
        }

        [Fact]
        public async Task GetAllPayments_DateFilter_ReturnsInRange()
        {
            var today = DateTime.UtcNow;
            var payments = new List<Payment>
            {
                new() { PaymentId = "P1", UserId = "U1", AmountPaid = 100, PaymentStatus = PaymentStatusEnum.Paid, PaymentDate = today.AddDays(-5) },
                new() { PaymentId = "P2", UserId = "U1", AmountPaid = 200, PaymentStatus = PaymentStatusEnum.Paid, PaymentDate = today }
            };
            _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(payments);
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { new() { UserId = "U1", UserName = "Alice" } });

            var result = await CreateService().GetAllPayments(new PaginationParams
            {
                PageNumber = 1, PageSize = 10,
                FromDate = today.ToString("yyyy-MM-dd"),
                ToDate   = today.ToString("yyyy-MM-dd")
            });
            result.Data.Should().HaveCount(1);
            result.Data.First().PaymentId.Should().Be("P2");
        }

        [Fact]
        public async Task GetAllPayments_UserNameFilter_ReturnsMatchingUser()
        {
            var payments = new List<Payment>
            {
                new() { PaymentId = "P1", UserId = "U1", AmountPaid = 100, PaymentStatus = PaymentStatusEnum.Paid, PaymentDate = DateTime.UtcNow },
                new() { PaymentId = "P2", UserId = "U2", AmountPaid = 200, PaymentStatus = PaymentStatusEnum.Paid, PaymentDate = DateTime.UtcNow }
            };
            _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(payments);
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "U1", UserName = "Alice" },
                new() { UserId = "U2", UserName = "Bob" }
            });

            var result = await CreateService().GetAllPayments(new PaginationParams { PageNumber = 1, PageSize = 10, UserName = "Alice" });
            result.Data.Should().HaveCount(1);
            result.Data.First().PaymentId.Should().Be("P1");
        }

        // ── GetPaymentByExpenseId ─────────────────────────────────────────────

        [Fact]
        public async Task GetPaymentByExpenseId_FinanceRole_ReturnsPayment()
        {
            var payment = new Payment { PaymentId = "P1", ExpenseId = "E1", UserId = "EMP1", AmountPaid = 500, PaymentStatus = PaymentStatusEnum.Paid };
            _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment> { payment });
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(new Expense { ExpenseId = "E1" });
            _userRepo.Setup(r => r.GetByIdAsync("EMP1")).ReturnsAsync(new User { UserId = "EMP1", UserName = "Employee" });

            var result = await CreateService().GetPaymentByExpenseId("E1", "FIN1", "Finance");
            result.Should().NotBeNull();
            result!.PaymentId.Should().Be("P1");
        }

        [Fact]
        public async Task GetPaymentByExpenseId_EmployeeOwnPayment_ReturnsPayment()
        {
            var payment = new Payment { PaymentId = "P1", ExpenseId = "E1", UserId = "EMP1", AmountPaid = 500, PaymentStatus = PaymentStatusEnum.Paid };
            _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment> { payment });
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(new Expense { ExpenseId = "E1" });
            _userRepo.Setup(r => r.GetByIdAsync("EMP1")).ReturnsAsync(new User { UserId = "EMP1" });

            var result = await CreateService().GetPaymentByExpenseId("E1", "EMP1", "Employee");
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetPaymentByExpenseId_EmployeeOtherPayment_ReturnsNull()
        {
            var payment = new Payment { PaymentId = "P1", ExpenseId = "E1", UserId = "EMP1", AmountPaid = 500, PaymentStatus = PaymentStatusEnum.Paid };
            _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment> { payment });

            var result = await CreateService().GetPaymentByExpenseId("E1", "OTHER_USER", "Employee");
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetPaymentByExpenseId_NotFound_ReturnsNull()
        {
            _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());
            var result = await CreateService().GetPaymentByExpenseId("MISSING", "U1", "Finance");
            result.Should().BeNull();
        }
    }

    // ── Additional branch coverage ────────────────────────────────────────────

    public class PaymentServiceBranchTests
    {
        private readonly Mock<IRepository<string, Payment>> _paymentRepo  = new();
        private readonly Mock<IRepository<string, Expense>> _expenseRepo  = new();
        private readonly Mock<IRepository<string, User>>    _userRepo     = new();
        private readonly Mock<INotificationService>         _notifService = new();
        private readonly Mock<IHttpContextAccessor>         _httpContext  = new();
        private readonly Mock<IAuditLogService>             _auditService = new();

        private PaymentService Svc(string role = "Finance")
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "FIN1"),
                new(ClaimTypes.Name, "Finance User"),
                new(ClaimTypes.Role, role)
            };
            var ctx = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) };
            _httpContext.Setup(h => h.HttpContext).Returns(ctx);
            return new PaymentService(_paymentRepo.Object, _expenseRepo.Object,
                _userRepo.Object, _notifService.Object, _httpContext.Object, _auditService.Object);
        }

        private void SetupNotif() =>
            _notifService.Setup(n => n.CreateNotification(It.IsAny<CreateNotificationRequestDto>()))
                .ReturnsAsync(new CreateNotificationResponseDto());

        private void SetupAudit() =>
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

        // Branch: CompletePayment — existing pending payment (not null) → updates it
        [Fact]
        public async Task CompletePayment_ExistingPendingPayment_UpdatesInsteadOfCreating()
        {
            var existingPayment = new Payment { PaymentId = "P1", PaymentStatus = PaymentStatusEnum.Pending };
            var expense = new Expense
            {
                ExpenseId = "E1", UserId = "EMP1", Amount = 500,
                Status = ExpenseStatus.Approved,
                Payments = new List<Payment> { existingPayment }
            };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _paymentRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Payment>())).ReturnsAsync(existingPayment);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new User { UserId = "EMP1" });
            SetupNotif(); SetupAudit();

            var result = await Svc().CompletePayment("E1", "REF001", "Cash");

            result.Should().NotBeNull();
            result!.PaymentStatus.Should().Be("Paid");
            _paymentRepo.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Never);
        }

        // Branch: CompletePayment — user load fails → UserName still works
        [Fact]
        public async Task CompletePayment_UserLoadFails_StillCompletes()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Amount = 500, Status = ExpenseStatus.Approved };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _paymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);
            _paymentRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Payment>())).ReturnsAsync((string k, Payment p) => p);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ThrowsAsync(new KeyNotFoundException());
            SetupNotif(); SetupAudit();

            var result = await Svc().CompletePayment("E1", "REF", "Cash");
            result.Should().NotBeNull();
        }

        // Branch: GetAllPayments — null payments → returns empty
        [Fact]
        public async Task GetAllPayments_NullPayments_ReturnsEmpty()
        {
            _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync((IEnumerable<Payment>?)null);
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var result = await Svc().GetAllPayments(new PaginationParams { PageNumber = 1, PageSize = 10 });
            result.Data.Should().BeEmpty();
        }

        // Branch: GetPaymentByExpenseId — Manager role, own payment → returns
        [Fact]
        public async Task GetPaymentByExpenseId_ManagerOwnPayment_Returns()
        {
            var payment = new Payment { PaymentId = "P1", ExpenseId = "E1", UserId = "MGR1", AmountPaid = 500, PaymentStatus = PaymentStatusEnum.Paid };
            _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment> { payment });
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(new Expense { ExpenseId = "E1" });
            _userRepo.Setup(r => r.GetByIdAsync("MGR1")).ReturnsAsync(new User { UserId = "MGR1" });

            var result = await Svc().GetPaymentByExpenseId("E1", "MGR1", "Manager");
            result.Should().NotBeNull();
        }

        // Branch: GetPaymentByExpenseId — Manager role, other's payment → null
        [Fact]
        public async Task GetPaymentByExpenseId_ManagerOtherPayment_ReturnsNull()
        {
            var payment = new Payment { PaymentId = "P1", ExpenseId = "E1", UserId = "EMP1", AmountPaid = 500, PaymentStatus = PaymentStatusEnum.Paid };
            _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment> { payment });

            var result = await Svc().GetPaymentByExpenseId("E1", "MGR1", "Manager");
            result.Should().BeNull();
        }

        // Branch: GetPaymentByExpenseId — expense load fails → DocumentUrls empty
        [Fact]
        public async Task GetPaymentByExpenseId_ExpenseLoadFails_DocumentUrlsEmpty()
        {
            var payment = new Payment { PaymentId = "P1", ExpenseId = "E1", UserId = "FIN1", AmountPaid = 500, PaymentStatus = PaymentStatusEnum.Paid };
            _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment> { payment });
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ThrowsAsync(new KeyNotFoundException());
            _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new User { UserId = "FIN1" });

            var result = await Svc().GetPaymentByExpenseId("E1", "FIN1", "Finance");
            result.Should().NotBeNull();
            result!.DocumentUrls.Should().BeEmpty();
        }

        // Branch: GetAllPayments — user not in map → UserName empty
        [Fact]
        public async Task GetAllPayments_UserNotInMap_UserNameEmpty()
        {
            var payments = new List<Payment>
            {
                new() { PaymentId = "P1", UserId = "UNKNOWN", AmountPaid = 100, PaymentStatus = PaymentStatusEnum.Paid, PaymentDate = DateTime.UtcNow }
            };
            _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(payments);
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var result = await Svc().GetAllPayments(new PaginationParams { PageNumber = 1, PageSize = 10 });
            result.Data.First().UserName.Should().Be("");
        }
    }
}
