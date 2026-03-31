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
        private readonly Mock<IRepository<string, User>>            _userRepo     = new();
        private readonly Mock<IAuditLogService>                     _auditService = new();
        private readonly Mock<IHttpContextAccessor>                 _httpContext  = new();
        private readonly Mock<IFileUploadService>                   _fileUpload   = new();

        private ExpenseService CreateService(string userId = "EMP1", string role = "Employee")
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Name, "Test User"),
                new(ClaimTypes.Role, role)
            };
            var ctx = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) };
            _httpContext.Setup(h => h.HttpContext).Returns(ctx);
            return new ExpenseService(
                _expenseRepo.Object, _categoryRepo.Object,
                _userRepo.Object, _auditService.Object,
                _httpContext.Object, _fileUpload.Object);
        }

        private void SetupAudit() =>
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

        private ExpenseCategory DefaultCategory(decimal maxLimit = 5000) =>
            new() { CategoryId = "C1", CategoryName = ExpenseCategoryType.Travel, MaxLimit = maxLimit };

        // ── CreateExpense ─────────────────────────────────────────────────────

        [Fact]
        public async Task CreateExpense_ValidRequest_ReturnsDraftExpense()
        {
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _categoryRepo.Setup(r => r.GetByIdAsync("C1")).ReturnsAsync(DefaultCategory());
            _expenseRepo.Setup(r => r.AddAsync(It.IsAny<Expense>())).ReturnsAsync((Expense e) => e);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            SetupAudit();

            var result = await CreateService().CreateExpense(new CreateExpenseRequestDto
            {
                CategoryId = "C1", Amount = 1000, ExpenseDate = DateTime.UtcNow
            });

            result.Should().NotBeNull();
            result!.Status.Should().Be("Draft");
            result.Amount.Should().Be(1000);
        }

        [Fact]
        public async Task CreateExpense_AmountExceedsLimit_ThrowsInvalidOperation()
        {
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _categoryRepo.Setup(r => r.GetByIdAsync("C1")).ReturnsAsync(DefaultCategory(500));
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService().CreateExpense(new CreateExpenseRequestDto
                {
                    CategoryId = "C1", Amount = 9999, ExpenseDate = DateTime.UtcNow
                }));
        }

        [Fact]
        public async Task CreateExpense_DuplicateActiveExpense_ThrowsInvalidOperation()
        {
            var existing = new Expense { ExpenseId = "E1", UserId = "EMP1", ExpenseDate = DateTime.UtcNow, Status = ExpenseStatus.Submitted };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { existing });
            _categoryRepo.Setup(r => r.GetByIdAsync("C1")).ReturnsAsync(DefaultCategory());
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService().CreateExpense(new CreateExpenseRequestDto
                {
                    CategoryId = "C1", Amount = 500, ExpenseDate = DateTime.UtcNow
                }));
        }

        [Fact]
        public async Task CreateExpense_RejectedExpenseExists_UpdatesInPlace()
        {
            var rejected = new Expense
            {
                ExpenseId = "E1", UserId = "EMP1",
                ExpenseDate = DateTime.UtcNow, Status = ExpenseStatus.Rejected,
                Amount = 200
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { rejected });
            _categoryRepo.Setup(r => r.GetByIdAsync("C1")).ReturnsAsync(DefaultCategory());
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(rejected);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            SetupAudit();

            var result = await CreateService().CreateExpense(new CreateExpenseRequestDto
            {
                CategoryId = "C1", Amount = 500, ExpenseDate = DateTime.UtcNow
            });

            result.Should().NotBeNull();
            result!.Status.Should().Be("Draft"); // reset to Draft after re-edit
            _expenseRepo.Verify(r => r.AddAsync(It.IsAny<Expense>()), Times.Never); // no new record
        }

        [Fact]
        public async Task CreateExpense_PastMonthDate_ThrowsInvalidOperation()
        {
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _categoryRepo.Setup(r => r.GetByIdAsync("C1")).ReturnsAsync(DefaultCategory());
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService().CreateExpense(new CreateExpenseRequestDto
                {
                    CategoryId = "C1", Amount = 500,
                    ExpenseDate = DateTime.UtcNow.AddMonths(-2)
                }));
        }

        [Fact]
        public async Task CreateExpense_CategoryNotFound_ThrowsKeyNotFound()
        {
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _categoryRepo.Setup(r => r.GetByIdAsync("MISSING")).ThrowsAsync(new KeyNotFoundException());
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                CreateService().CreateExpense(new CreateExpenseRequestDto
                {
                    CategoryId = "MISSING", Amount = 500, ExpenseDate = DateTime.UtcNow
                }));
        }

        // ── SubmitExpense ─────────────────────────────────────────────────────

        [Fact]
        public async Task SubmitExpense_DraftExpense_ChangesStatusToSubmitted()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);

            var result = await CreateService().SubmitExpense("E1");
            result!.Status.Should().Be("Submitted");
            expense.Status.Should().Be(ExpenseStatus.Submitted);
        }

        [Fact]
        public async Task SubmitExpense_NotDraft_ThrowsInvalidOperation()
        {
            var expense = new Expense { ExpenseId = "E1", Status = ExpenseStatus.Submitted };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().SubmitExpense("E1"));
        }

        [Fact]
        public async Task SubmitExpense_NotFound_ThrowsKeyNotFound()
        {
            _expenseRepo.Setup(r => r.GetByIdAsync("MISSING")).ThrowsAsync(new KeyNotFoundException());
            await Assert.ThrowsAsync<KeyNotFoundException>(() => CreateService().SubmitExpense("MISSING"));
        }

        // ── ResubmitExpense ───────────────────────────────────────────────────

        [Fact]
        public async Task ResubmitExpense_RejectedByOwner_ChangesStatusToSubmitted()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Rejected, Amount = 500 };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            SetupAudit();

            var result = await CreateService("EMP1").ResubmitExpense("E1");
            result!.Status.Should().Be("Submitted");
        }

        [Fact]
        public async Task ResubmitExpense_AlreadyApproved_ThrowsInvalidOperation()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Approved };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService("EMP1").ResubmitExpense("E1"));
        }

        [Fact]
        public async Task ResubmitExpense_DifferentUser_ThrowsUnauthorized()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Rejected };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                CreateService("OTHER_USER").ResubmitExpense("E1"));
        }

        // ── DeleteExpense ─────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteExpense_DraftByOwner_Succeeds()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, Amount = 100 };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.DeleteAsync("E1")).ReturnsAsync(expense);
            SetupAudit();

            var (success, msg, _) = await CreateService("EMP1").DeleteExpenseSafe("E1");
            success.Should().BeTrue();
            msg.Should().Contain("Deleted");
        }

        [Fact]
        public async Task DeleteExpense_ApprovedExpense_Fails()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Approved };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            var (success, _, _) = await CreateService("EMP1").DeleteExpenseSafe("E1");
            success.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteExpense_OtherUserNonAdmin_Fails()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            var (success, msg, _) = await CreateService("OTHER").DeleteExpenseSafe("E1");
            success.Should().BeFalse();
            msg.Should().Contain("authorized");
        }

        [Fact]
        public async Task DeleteExpense_NotFound_Fails()
        {
            _expenseRepo.Setup(r => r.GetByIdAsync("MISSING")).ThrowsAsync(new KeyNotFoundException());

            var (success, _, _) = await CreateService().DeleteExpenseSafe("MISSING");
            success.Should().BeFalse();
        }

        // ── GetAllExpenses ────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllExpenses_StatusFilter_ReturnsOnlyMatching()
        {
            var expenses = new List<Expense>
            {
                new() { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Submitted, ExpenseDate = DateTime.UtcNow, Amount = 100 },
                new() { ExpenseId = "E2", UserId = "EMP1", Status = ExpenseStatus.Draft,     ExpenseDate = DateTime.UtcNow, Amount = 200 }
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expenses);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var result = await CreateService("EMP1", "Manager").GetAllExpenses(new PaginationParams
            {
                PageNumber = 1, PageSize = 10, Status = "Submitted"
            });

            result.Data.Should().HaveCount(1);
            result.Data.First().Status.Should().Be("Submitted");
        }

        [Fact]
        public async Task GetAllExpenses_EmployeeSeesOnlyOwnExpenses()
        {
            var expenses = new List<Expense>
            {
                new() { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 100 },
                new() { ExpenseId = "E2", UserId = "EMP2", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 200 }
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expenses);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var result = await CreateService("EMP1", "Employee").GetAllExpenses(new PaginationParams { PageNumber = 1, PageSize = 10 });
            result.Data.Should().HaveCount(1);
            result.Data.First().UserId.Should().Be("EMP1");
        }

        [Fact]
        public async Task GetAllExpenses_AmountFilter_ReturnsInRange()
        {
            var expenses = new List<Expense>
            {
                new() { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 100 },
                new() { ExpenseId = "E2", UserId = "EMP1", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 500 },
                new() { ExpenseId = "E3", UserId = "EMP1", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 900 }
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expenses);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var result = await CreateService("EMP1", "Manager").GetAllExpenses(new PaginationParams
            {
                PageNumber = 1, PageSize = 10, MinAmount = 200, MaxAmount = 800
            });

            result.Data.Should().HaveCount(1);
            result.Data.First().Amount.Should().Be(500);
        }

        // ── GetMyExpenses ─────────────────────────────────────────────────────

        [Fact]
        public async Task GetMyExpenses_ReturnsOnlyCurrentUserExpenses()
        {
            var expenses = new List<Expense>
            {
                new() { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 100 },
                new() { ExpenseId = "E2", UserId = "EMP2", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 200 }
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expenses);

            var result = await CreateService("EMP1").GetMyExpenses();
            result.Should().HaveCount(1);
            result.First().UserId.Should().Be("EMP1");
        }

        // ── UpdateExpense ─────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateExpense_DraftByOwner_Succeeds()
        {
            var expense = new Expense
            {
                ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft,
                Amount = 100, CategoryId = "C1", CategoryName = "Travel"
            };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            _categoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExpenseCategory> { DefaultCategory() });
            SetupAudit();

            var (success, _, _) = await CreateService("EMP1").UpdateExpenseSafe("E1", new CreateExpenseRequestDto
            {
                CategoryId = "C1", CategoryName = "Travel", Amount = 300, ExpenseDate = DateTime.UtcNow
            });

            success.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateExpense_NotFound_ReturnsFalse()
        {
            _expenseRepo.Setup(r => r.GetByIdAsync("MISSING")).ThrowsAsync(new KeyNotFoundException());

            var (success, msg, _) = await CreateService().UpdateExpenseSafe("MISSING", new CreateExpenseRequestDto
            {
                CategoryId = "C1", Amount = 100, ExpenseDate = DateTime.UtcNow
            });

            success.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateExpense_OtherUserNonAdmin_ReturnsFalse()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            var (success, msg, _) = await CreateService("OTHER").UpdateExpenseSafe("E1", new CreateExpenseRequestDto
            {
                CategoryId = "C1", Amount = 100, ExpenseDate = DateTime.UtcNow
            });

            success.Should().BeFalse();
            msg.Should().Contain("authorized");
        }
    }
}
