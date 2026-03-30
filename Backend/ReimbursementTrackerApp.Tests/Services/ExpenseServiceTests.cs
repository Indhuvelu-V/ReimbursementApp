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
    public class ExpenseServiceTests
    {
        private readonly Mock<IRepository<string, Expense>>         _expenseRepo  = new();
        private readonly Mock<IRepository<string, ExpenseCategory>> _categoryRepo = new();
        private readonly Mock<IAuditLogService>                      _auditService = new();
        private readonly Mock<IHttpContextAccessor>                  _httpContext  = new();
        private readonly Mock<IFileUploadService>                    _fileUpload   = new();
        private readonly Mock<IRepository<string, User>>             _userRepo     = new();

        private ExpenseService CreateService(string userId = "EMP1", string role = "Employee")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };
            _httpContext.Setup(h => h.HttpContext).Returns(httpContext);

            return new ExpenseService(
                _expenseRepo.Object, _categoryRepo.Object,
                _userRepo.Object, _auditService.Object,
                _httpContext.Object, _fileUpload.Object);
        }

        private void SetupAudit() =>
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

        private ExpenseCategory DefaultCategory() =>
            new ExpenseCategory { CategoryId = "C1", CategoryName = ExpenseCategoryType.Travel, MaxLimit = 5000 };

        // ── CreateExpense ─────────────────────────────────────────────────────

        [Fact]
        public async Task CreateExpense_ValidRequest_ReturnsDraftExpense()
        {
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _categoryRepo.Setup(r => r.GetByIdAsync("C1")).ReturnsAsync(DefaultCategory());
            _expenseRepo.Setup(r => r.AddAsync(It.IsAny<Expense>())).ReturnsAsync((Expense e) => e);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            SetupAudit();

            var svc = CreateService();
            var result = await svc.CreateExpense(new CreateExpenseRequestDto
            {
                CategoryId = "C1", Amount = 1000,
                ExpenseDate = DateTime.UtcNow
            });

            result.Should().NotBeNull();
            result!.Status.Should().Be("Draft");
            result.Amount.Should().Be(1000);
        }

        [Fact]
        public async Task CreateExpense_AmountExceedsLimit_ThrowsInvalidOperation()
        {
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _categoryRepo.Setup(r => r.GetByIdAsync("C1")).ReturnsAsync(DefaultCategory()); // MaxLimit = 5000
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var svc = CreateService();
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CreateExpense(new CreateExpenseRequestDto
                {
                    CategoryId = "C1", Amount = 9999,
                    ExpenseDate = DateTime.UtcNow
                }));
        }

        [Fact]
        public async Task CreateExpense_DuplicateActiveExpense_ThrowsInvalidOperation()
        {
            var existing = new Expense
            {
                ExpenseId = "E1", UserId = "EMP1",
                ExpenseDate = DateTime.UtcNow,
                Status = ExpenseStatus.Submitted
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { existing });
            _categoryRepo.Setup(r => r.GetByIdAsync("C1")).ReturnsAsync(DefaultCategory());
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var svc = CreateService();
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CreateExpense(new CreateExpenseRequestDto
                {
                    CategoryId = "C1", Amount = 500,
                    ExpenseDate = DateTime.UtcNow
                }));
        }

        // ── SubmitExpense ─────────────────────────────────────────────────────

        [Fact]
        public async Task SubmitExpense_DraftExpense_ChangesStatusToSubmitted()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);

            var svc = CreateService();
            var result = await svc.SubmitExpense("E1");

            result!.Status.Should().Be("Submitted");
            expense.Status.Should().Be(ExpenseStatus.Submitted);
        }

        [Fact]
        public async Task SubmitExpense_NotDraft_ThrowsInvalidOperation()
        {
            var expense = new Expense { ExpenseId = "E1", Status = ExpenseStatus.Submitted };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            var svc = CreateService();
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.SubmitExpense("E1"));
        }

        // ── DeleteExpense ─────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteExpense_DraftByOwner_Succeeds()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, Amount = 100 };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.DeleteAsync("E1")).ReturnsAsync(expense);
            SetupAudit();

            var svc = CreateService("EMP1");
            var (success, msg, _) = await svc.DeleteExpenseSafe("E1");

            success.Should().BeTrue();
            msg.Should().Contain("Deleted");
        }

        [Fact]
        public async Task DeleteExpense_ApprovedExpense_Fails()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Approved };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            var svc = CreateService("EMP1");
            var (success, msg, _) = await svc.DeleteExpenseSafe("E1");

            success.Should().BeFalse();
        }

        // ── GetAllExpenses ────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllExpenses_StatusFilter_ReturnsOnlyMatching()
        {
            var expenses = new List<Expense>
            {
                new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Submitted, ExpenseDate = DateTime.UtcNow, Amount = 100 },
                new Expense { ExpenseId = "E2", UserId = "EMP1", Status = ExpenseStatus.Draft,     ExpenseDate = DateTime.UtcNow, Amount = 200 }
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expenses);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var svc = CreateService("EMP1", "Manager");
            var result = await svc.GetAllExpenses(new PaginationParams
            {
                PageNumber = 1, PageSize = 10, Status = "Submitted"
            });

            result.Data.Should().HaveCount(1);
            result.Data.First().Status.Should().Be("Submitted");
        }
    }
}
