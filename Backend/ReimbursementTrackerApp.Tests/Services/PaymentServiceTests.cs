using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models;
using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;
using ReimbursementTrackerApp.Models.Enums;
using ReimbursementTrackerApp.Services;
using System.Security.Claims;
using Xunit;

namespace ReimbursementTrackerApp.Tests.Services
{
    public class PaymentServiceTests
    {
        private readonly Mock<IRepository<string, Payment>> _paymentRepo = new();
        private readonly Mock<IRepository<string, Expense>> _expenseRepo = new();
        private readonly Mock<IRepository<string, User>> _userRepo = new();
        private readonly Mock<INotificationService> _notifService = new();
        private readonly Mock<IHttpContextAccessor> _httpContext = new();
        private readonly Mock<IAuditLogService> _auditService = new();

        private PaymentService CreateService(string role = "Finance")
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "FIN1"),
                new(ClaimTypes.Name, "Finance User"),
                new(ClaimTypes.Role, role)
            };

            var ctx = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            };

            _httpContext.Setup(h => h.HttpContext).Returns(ctx);

            return new PaymentService(
                _paymentRepo.Object,
                _expenseRepo.Object,
                _userRepo.Object,
                _notifService.Object,
                _httpContext.Object,
                _auditService.Object
            );
        }

        [Fact]
        public async Task CompletePayment_ExpenseNotFound_ThrowsKeyNotFound()
        {
            _expenseRepo.Setup(r => r.GetByIdAsync("MISSING"))
                .ThrowsAsync(new KeyNotFoundException());

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                CreateService().CompletePayment("MISSING", "REF", "Cash"));
        }

        [Fact]
        public async Task CompletePayment_NotApprovedStatus_ThrowsInvalidOperation()
        {
            var expense = new Expense
            {
                ExpenseId = "E2",
                Status = ExpenseStatus.Submitted
            };

            _expenseRepo.Setup(r => r.GetByIdAsync("E2")).ReturnsAsync(expense);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService().CompletePayment("E2", "REF", "Cash"));
        }

        [Fact]
        public async Task CompletePayment_NonFinanceRole_ThrowsUnauthorized()
        {
            var expense = new Expense
            {
                ExpenseId = "E4",
                Status = ExpenseStatus.Approved
            };

            _expenseRepo.Setup(r => r.GetByIdAsync("E4")).ReturnsAsync(expense);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                CreateService("Employee").CompletePayment("E4", "REF", "Cash"));
        }

        [Fact]
        public async Task GetAllPayments_ReturnsData()
        {
            var payments = new List<Payment>
            {
                new() { PaymentId = "P1", AmountPaid = 100, PaymentStatus = PaymentStatusEnum.Paid, PaymentDate = DateTime.UtcNow }
            };

            _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(payments);
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var result = await CreateService().GetAllPayments(new PaginationParams
            {
                PageNumber = 1,
                PageSize = 10
            });

            result.Data.Should().HaveCount(1);
        }
    }

    public class PaymentServiceBranchTests
    {
        private readonly Mock<IRepository<string, Payment>> _paymentRepo = new();
        private readonly Mock<IRepository<string, Expense>> _expenseRepo = new();
        private readonly Mock<IRepository<string, User>> _userRepo = new();
        private readonly Mock<INotificationService> _notifService = new();
        private readonly Mock<IHttpContextAccessor> _httpContext = new();
        private readonly Mock<IAuditLogService> _auditService = new();

        private PaymentService Svc(string role = "Finance")
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "FIN1"),
                new(ClaimTypes.Role, role)
            };

            var ctx = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            };

            _httpContext.Setup(h => h.HttpContext).Returns(ctx);

            return new PaymentService(
                _paymentRepo.Object,
                _expenseRepo.Object,
                _userRepo.Object,
                _notifService.Object,
                _httpContext.Object,
                _auditService.Object
            );
        }

        [Fact]
        public async Task GetAllPayments_Empty_ReturnsEmpty()
        {
            _paymentRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Payment>());

            var result = await Svc().GetAllPayments(new PaginationParams
            {
                PageNumber = 1,
                PageSize = 10
            });

            result.Data.Should().BeEmpty();
        }
    }

    public class PaymentServiceNewLogicTests
    {
        private readonly Mock<IRepository<string, Payment>> _paymentRepo = new();
        private readonly Mock<IRepository<string, Expense>> _expenseRepo = new();
        private readonly Mock<IRepository<string, User>> _userRepo = new();
        private readonly Mock<INotificationService> _notifService = new();
        private readonly Mock<IHttpContextAccessor> _httpContext = new();
        private readonly Mock<IAuditLogService> _auditService = new();

        private PaymentService Svc(string role = "Finance")
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "FIN1"),
                new(ClaimTypes.Role, role)
            };

            var ctx = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            };

            _httpContext.Setup(h => h.HttpContext).Returns(ctx);

            return new PaymentService(
                _paymentRepo.Object,
                _expenseRepo.Object,
                _userRepo.Object,
                _notifService.Object,
                _httpContext.Object,
                _auditService.Object
            );
        }

        [Fact]
        public async Task CompletePayment_UnauthorizedUser_Throws()
        {
            _httpContext.Setup(h => h.HttpContext)
                .Returns(new DefaultHttpContext());

            var svc = new PaymentService(
                _paymentRepo.Object,
                _expenseRepo.Object,
                _userRepo.Object,
                _notifService.Object,
                _httpContext.Object,
                _auditService.Object
            );

            var expense = new Expense
            {
                ExpenseId = "E1",
                Status = ExpenseStatus.Approved
            };

            _expenseRepo.Setup(r => r.GetByIdAsync("E1"))
                .ReturnsAsync(expense);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                svc.CompletePayment("E1", "REF", "Cash"));
        }
    }
}