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

        private PaymentService CreateService()
        {
            // Set up a Finance user in the HTTP context
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "FIN1"),
                new Claim(ClaimTypes.Name, "Finance User"),
                new Claim(ClaimTypes.Role, "Finance")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };
            _httpContext.Setup(h => h.HttpContext).Returns(httpContext);

            return new PaymentService(
                _paymentRepo.Object, _expenseRepo.Object,
                _userRepo.Object, _notifService.Object, _httpContext.Object);
        }

        private void SetupNotif() =>
            _notifService.Setup(n => n.CreateNotification(It.IsAny<CreateNotificationRequestDto>()))
                .ReturnsAsync(new CreateNotificationResponseDto());

        // ── CompletePayment ───────────────────────────────────────────────────

        [Fact]
        public async Task CompletePayment_ValidExpense_ReturnsPaidDto()
        {
            var expense = new Expense
            {
                ExpenseId = "E1", UserId = "EMP1",
                Amount = 1000, Status = ExpenseStatus.Approved
            };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _paymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);
            _paymentRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Payment>())).ReturnsAsync((string k, Payment p) => p);
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new User { UserId = "EMP1", UserName = "Employee" });
            SetupNotif();

            var svc = CreateService();
            var result = await svc.CompletePayment("E1", "REF001", "BankTransfer");

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

            var svc = CreateService();
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.CompletePayment("MISSING", "REF", "Cash"));
        }

        [Fact]
        public async Task CompletePayment_NotApproved_ThrowsInvalidOperation()
        {
            var expense = new Expense
            {
                ExpenseId = "E2", UserId = "EMP1",
                Amount = 500, Status = ExpenseStatus.Submitted
            };
            _expenseRepo.Setup(r => r.GetByIdAsync("E2")).ReturnsAsync(expense);

            var svc = CreateService();
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CompletePayment("E2", "REF", "Cash"));
        }

        // ── GetAllPayments ────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllPayments_NoFilters_ReturnsAllPaged()
        {
            var payments = new List<Payment>
            {
                new Payment { PaymentId = "P1", ExpenseId = "E1", UserId = "U1", AmountPaid = 100, PaymentStatus = PaymentStatusEnum.Paid, PaymentDate = DateTime.UtcNow },
                new Payment { PaymentId = "P2", ExpenseId = "E2", UserId = "U2", AmountPaid = 200, PaymentStatus = PaymentStatusEnum.Paid, PaymentDate = DateTime.UtcNow }
            };
            _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(payments);
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new User { UserId = "U1", UserName = "Alice" },
                new User { UserId = "U2", UserName = "Bob" }
            });

            var svc = CreateService();
            var result = await svc.GetAllPayments(new PaginationParams { PageNumber = 1, PageSize = 10 });

            result.Data.Should().HaveCount(2);
            result.TotalRecords.Should().Be(2);
        }

        [Fact]
        public async Task GetAllPayments_StatusFilter_ReturnsOnlyPaid()
        {
            var payments = new List<Payment>
            {
                new Payment { PaymentId = "P1", UserId = "U1", AmountPaid = 100, PaymentStatus = PaymentStatusEnum.Paid,    PaymentDate = DateTime.UtcNow },
                new Payment { PaymentId = "P2", UserId = "U1", AmountPaid = 200, PaymentStatus = PaymentStatusEnum.Pending, PaymentDate = DateTime.UtcNow }
            };
            _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(payments);
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new User { UserId = "U1", UserName = "Alice" }
            });

            var svc = CreateService();
            var result = await svc.GetAllPayments(new PaginationParams
            {
                PageNumber = 1, PageSize = 10, Status = "Paid"
            });

            result.Data.Should().HaveCount(1);
            result.Data.First().PaymentStatus.Should().Be("Paid");
        }

        [Fact]
        public async Task GetAllPayments_AmountFilter_ReturnsInRange()
        {
            var payments = new List<Payment>
            {
                new Payment { PaymentId = "P1", UserId = "U1", AmountPaid = 100, PaymentStatus = PaymentStatusEnum.Paid, PaymentDate = DateTime.UtcNow },
                new Payment { PaymentId = "P2", UserId = "U1", AmountPaid = 500, PaymentStatus = PaymentStatusEnum.Paid, PaymentDate = DateTime.UtcNow },
                new Payment { PaymentId = "P3", UserId = "U1", AmountPaid = 900, PaymentStatus = PaymentStatusEnum.Paid, PaymentDate = DateTime.UtcNow }
            };
            _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(payments);
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new User { UserId = "U1", UserName = "Alice" }
            });

            var svc = CreateService();
            var result = await svc.GetAllPayments(new PaginationParams
            {
                PageNumber = 1, PageSize = 10, MinAmount = 200, MaxAmount = 800
            });

            result.Data.Should().HaveCount(1);
            result.Data.First().AmountPaid.Should().Be(500);
        }
    }
}
