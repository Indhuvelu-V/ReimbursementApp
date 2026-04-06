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
        private readonly Mock<IRepository<string, Expense>>         _expenseRepo    = new();
        private readonly Mock<IRepository<string, ExpenseCategory>> _categoryRepo   = new();
        private readonly Mock<IRepository<string, User>>            _userRepo       = new();
        private readonly Mock<IRepository<string, Approval>>        _approvalRepo   = new();
        private readonly Mock<IAuditLogService>                     _auditService   = new();
        private readonly Mock<IHttpContextAccessor>                 _httpContext     = new();
        private readonly Mock<IFileUploadService>                   _fileUpload     = new();
        private readonly Mock<INotificationService>                 _notifService   = new();

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
            return new ExpenseService(_expenseRepo.Object, _categoryRepo.Object,
                _userRepo.Object, _approvalRepo.Object, _auditService.Object,
                _httpContext.Object, _fileUpload.Object, _notifService.Object);
        }

        private ExpenseService CreateUnauthenticatedService()
        {
            _httpContext.Setup(h => h.HttpContext).Returns(new DefaultHttpContext());
            return new ExpenseService(_expenseRepo.Object, _categoryRepo.Object,
                _userRepo.Object, _approvalRepo.Object, _auditService.Object,
                _httpContext.Object, _fileUpload.Object, _notifService.Object);
        }

        private void SetupAudit() =>
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

        private ExpenseCategory Cat(decimal max = 5000) =>
            new() { CategoryId = "C1", CategoryName = ExpenseCategoryType.Travel, MaxLimit = max };

        // ── CreateExpense ─────────────────────────────────────────────────────

        [Fact]
        public async Task CreateExpense_ValidRequest_ReturnsDraft()
        {
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _categoryRepo.Setup(r => r.GetByIdAsync("C1")).ReturnsAsync(Cat());
            _expenseRepo.Setup(r => r.AddAsync(It.IsAny<Expense>())).ReturnsAsync((Expense e) => e);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            SetupAudit();

            var result = await CreateService().CreateExpense(new CreateExpenseRequestDto
            { CategoryId = "C1", Amount = 1000, ExpenseDate = DateTime.UtcNow });

            result!.Status.Should().Be("Draft");
            result.Amount.Should().Be(1000);
        }

        [Fact]
        public async Task CreateExpense_WithDocumentUrls_AttachesUrls()
        {
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _categoryRepo.Setup(r => r.GetByIdAsync("C1")).ReturnsAsync(Cat());
            _expenseRepo.Setup(r => r.AddAsync(It.IsAny<Expense>())).ReturnsAsync((Expense e) => e);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            SetupAudit();

            var result = await CreateService().CreateExpense(new CreateExpenseRequestDto
            {
                CategoryId = "C1", Amount = 500, ExpenseDate = DateTime.UtcNow,
                DocumentUrls = new List<string> { "/uploads/file1.pdf" }
            });

            result!.DocumentUrls.Should().Contain("/uploads/file1.pdf");
        }

        [Fact]
        public async Task CreateExpense_AmountExceedsLimit_Throws()
        {
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _categoryRepo.Setup(r => r.GetByIdAsync("C1")).ReturnsAsync(Cat(500));
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService().CreateExpense(new CreateExpenseRequestDto
                { CategoryId = "C1", Amount = 9999, ExpenseDate = DateTime.UtcNow }));
        }

        [Fact]
        public async Task CreateExpense_DuplicateActiveExpense_Throws()
        {
            var existing = new Expense { ExpenseId = "E1", UserId = "EMP1", ExpenseDate = DateTime.UtcNow, Status = ExpenseStatus.Submitted };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { existing });
            _categoryRepo.Setup(r => r.GetByIdAsync("C1")).ReturnsAsync(Cat());
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService().CreateExpense(new CreateExpenseRequestDto
                { CategoryId = "C1", Amount = 500, ExpenseDate = DateTime.UtcNow }));
        }

        [Fact]
        public async Task CreateExpense_DuplicateDraftExpense_Throws()
        {
            var existing = new Expense { ExpenseId = "E1", UserId = "EMP1", ExpenseDate = DateTime.UtcNow, Status = ExpenseStatus.Draft };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { existing });
            _categoryRepo.Setup(r => r.GetByIdAsync("C1")).ReturnsAsync(Cat());
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService().CreateExpense(new CreateExpenseRequestDto
                { CategoryId = "C1", Amount = 500, ExpenseDate = DateTime.UtcNow }));
        }

        [Fact]
        public async Task CreateExpense_RejectedExpenseExists_UpdatesInPlace()
        {
            var rejected = new Expense { ExpenseId = "E1", UserId = "EMP1", ExpenseDate = DateTime.UtcNow, Status = ExpenseStatus.Rejected, Amount = 200 };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { rejected });
            _categoryRepo.Setup(r => r.GetByIdAsync("C1")).ReturnsAsync(Cat());
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(rejected);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            SetupAudit();

            var result = await CreateService().CreateExpense(new CreateExpenseRequestDto
            { CategoryId = "C1", Amount = 500, ExpenseDate = DateTime.UtcNow });

            result!.Status.Should().Be("Draft");
            _expenseRepo.Verify(r => r.AddAsync(It.IsAny<Expense>()), Times.Never);
        }

        [Fact]
        public async Task CreateExpense_PastMonthDate_Throws()
        {
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _categoryRepo.Setup(r => r.GetByIdAsync("C1")).ReturnsAsync(Cat());
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService().CreateExpense(new CreateExpenseRequestDto
                { CategoryId = "C1", Amount = 500, ExpenseDate = DateTime.UtcNow.AddMonths(-2) }));
        }

        [Fact]
        public async Task CreateExpense_FutureMonthDate_Throws()
        {
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _categoryRepo.Setup(r => r.GetByIdAsync("C1")).ReturnsAsync(Cat());
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService().CreateExpense(new CreateExpenseRequestDto
                { CategoryId = "C1", Amount = 500, ExpenseDate = DateTime.UtcNow.AddMonths(2) }));
        }

        [Fact]
        public async Task CreateExpense_CategoryNotFound_Throws()
        {
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _categoryRepo.Setup(r => r.GetByIdAsync("MISSING")).ThrowsAsync(new KeyNotFoundException());
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                CreateService().CreateExpense(new CreateExpenseRequestDto
                { CategoryId = "MISSING", Amount = 500, ExpenseDate = DateTime.UtcNow }));
        }

        // ── SubmitExpense ─────────────────────────────────────────────────────

        [Fact]
        public async Task SubmitExpense_Draft_ChangesStatusToSubmitted()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);

            var result = await CreateService().SubmitExpense("E1");
            result!.Status.Should().Be("Submitted");
            expense.Status.Should().Be(ExpenseStatus.Submitted);
        }

        [Fact]
        public async Task SubmitExpense_NotDraft_Throws()
        {
            var expense = new Expense { ExpenseId = "E1", Status = ExpenseStatus.Submitted };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().SubmitExpense("E1"));
        }

        [Fact]
        public async Task SubmitExpense_NotFound_Throws()
        {
            _expenseRepo.Setup(r => r.GetByIdAsync("MISSING")).ThrowsAsync(new KeyNotFoundException());
            await Assert.ThrowsAsync<KeyNotFoundException>(() => CreateService().SubmitExpense("MISSING"));
        }

        [Fact]
        public async Task SubmitExpense_ApprovedExpense_Throws()
        {
            var expense = new Expense { ExpenseId = "E1", Status = ExpenseStatus.Approved };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().SubmitExpense("E1"));
        }

        // ── ResubmitExpense ───────────────────────────────────────────────────

        [Fact]
        public async Task ResubmitExpense_RejectedByOwner_Succeeds()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Rejected, Amount = 500 };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            SetupAudit();

            var result = await CreateService("EMP1").ResubmitExpense("E1");
            result!.Status.Should().Be("Submitted");
        }

        [Fact]
        public async Task ResubmitExpense_DraftByOwner_Succeeds()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, Amount = 300 };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            SetupAudit();

            var result = await CreateService("EMP1").ResubmitExpense("E1");
            result!.Status.Should().Be("Submitted");
        }

        [Fact]
        public async Task ResubmitExpense_AdminCanResubmitOthers()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Rejected, Amount = 300 };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            SetupAudit();

            var result = await CreateService("ADMIN1", "Admin").ResubmitExpense("E1");
            result!.Status.Should().Be("Submitted");
        }

        [Fact]
        public async Task ResubmitExpense_AlreadyApproved_Throws()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Approved };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService("EMP1").ResubmitExpense("E1"));
        }

        [Fact]
        public async Task ResubmitExpense_AlreadyPaid_Throws()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Paid };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService("EMP1").ResubmitExpense("E1"));
        }

        [Fact]
        public async Task ResubmitExpense_DifferentUser_Throws()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Rejected };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                CreateService("OTHER").ResubmitExpense("E1"));
        }

        [Fact]
        public async Task ResubmitExpense_NotFound_Throws()
        {
            _expenseRepo.Setup(r => r.GetByIdAsync("MISSING")).ThrowsAsync(new KeyNotFoundException());
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                CreateService("EMP1").ResubmitExpense("MISSING"));
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
        public async Task DeleteExpense_SubmittedByOwner_Succeeds()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Submitted, Amount = 100 };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.DeleteAsync("E1")).ReturnsAsync(expense);
            SetupAudit();

            var (success, _, _) = await CreateService("EMP1").DeleteExpenseSafe("E1");
            success.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteExpense_AdminDeletesOthers_Succeeds()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, Amount = 100 };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.DeleteAsync("E1")).ReturnsAsync(expense);
            SetupAudit();

            var (success, _, _) = await CreateService("ADMIN1", "Admin").DeleteExpenseSafe("E1");
            success.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteExpense_ApprovedExpense_Fails()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Approved };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            var (success, msg, _) = await CreateService("EMP1").DeleteExpenseSafe("E1");
            success.Should().BeFalse();
            msg.Should().Contain("Cannot delete");
        }

        [Fact]
        public async Task DeleteExpense_PaidExpense_Fails()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Paid };
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

        // ── GetExpenseById ────────────────────────────────────────────────────

        [Fact]
        public async Task GetExpenseById_OwnerEmployee_ReturnsDto()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, Amount = 100, ExpenseDate = DateTime.UtcNow };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            var result = await CreateService("EMP1", "Employee").GetExpenseById("E1");
            result.Should().NotBeNull();
            result!.ExpenseId.Should().Be("E1");
        }

        [Fact]
        public async Task GetExpenseById_EmployeeOtherExpense_ReturnsNull()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP2", Status = ExpenseStatus.Draft, Amount = 100, ExpenseDate = DateTime.UtcNow };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            var result = await CreateService("EMP1", "Employee").GetExpenseById("E1");
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetExpenseById_ManagerCanSeeAny_ReturnsDto()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, Amount = 100, ExpenseDate = DateTime.UtcNow };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            var result = await CreateService("MGR1", "Manager").GetExpenseById("E1");
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetExpenseById_NotFound_ReturnsNull()
        {
            _expenseRepo.Setup(r => r.GetByIdAsync("MISSING")).ThrowsAsync(new KeyNotFoundException());

            var result = await CreateService().GetExpenseById("MISSING");
            result.Should().BeNull();
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

            var result = await CreateService("EMP1", "Manager").GetAllExpenses(
                new PaginationParams { PageNumber = 1, PageSize = 10, Status = "Submitted" });

            result.Data.Should().HaveCount(1);
            result.Data.First().Status.Should().Be("Submitted");
        }

        [Fact]
        public async Task GetAllExpenses_EmployeeSeesOnlyOwn()
        {
            var expenses = new List<Expense>
            {
                new() { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 100 },
                new() { ExpenseId = "E2", UserId = "EMP2", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 200 }
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expenses);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var result = await CreateService("EMP1", "Employee").GetAllExpenses(
                new PaginationParams { PageNumber = 1, PageSize = 10 });

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

            var result = await CreateService("EMP1", "Manager").GetAllExpenses(
                new PaginationParams { PageNumber = 1, PageSize = 10, MinAmount = 200, MaxAmount = 800 });

            result.Data.Should().HaveCount(1);
            result.Data.First().Amount.Should().Be(500);
        }

        [Fact]
        public async Task GetAllExpenses_DateFilter_ReturnsInRange()
        {
            var today = DateTime.UtcNow;
            var expenses = new List<Expense>
            {
                new() { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, ExpenseDate = today.AddDays(-5), Amount = 100 },
                new() { ExpenseId = "E2", UserId = "EMP1", Status = ExpenseStatus.Draft, ExpenseDate = today,             Amount = 200 }
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expenses);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var result = await CreateService("EMP1", "Manager").GetAllExpenses(new PaginationParams
            {
                PageNumber = 1, PageSize = 10,
                FromDate = today.ToString("yyyy-MM-dd"),
                ToDate   = today.ToString("yyyy-MM-dd")
            });

            result.Data.Should().HaveCount(1);
            result.Data.First().ExpenseId.Should().Be("E2");
        }

        [Fact]
        public async Task GetAllExpenses_UserNameFilter_ReturnsMatchingUser()
        {
            var expenses = new List<Expense>
            {
                new() { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 100 },
                new() { ExpenseId = "E2", UserId = "EMP2", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 200 }
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expenses);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "EMP1", UserName = "Alice" },
                new() { UserId = "EMP2", UserName = "Bob" }
            });

            var result = await CreateService("MGR1", "Manager").GetAllExpenses(
                new PaginationParams { PageNumber = 1, PageSize = 10, UserName = "alice" });

            result.Data.Should().HaveCount(1);
            result.Data.First().UserId.Should().Be("EMP1");
        }

        [Fact]
        public async Task GetAllExpenses_Pagination_ReturnsCorrectPage()
        {
            var expenses = Enumerable.Range(1, 12)
                .Select(i => new Expense { ExpenseId = $"E{i}", UserId = "EMP1", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = i * 100 })
                .ToList();
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expenses);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var result = await CreateService("EMP1", "Manager").GetAllExpenses(
                new PaginationParams { PageNumber = 2, PageSize = 5 });

            result.Data.Should().HaveCount(5);
            result.TotalRecords.Should().Be(12);
        }

        // ── GetMyExpenses ─────────────────────────────────────────────────────

        [Fact]
        public async Task GetMyExpenses_ReturnsOnlyCurrentUser()
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

        [Fact]
        public async Task GetMyExpenses_NoExpenses_ReturnsEmpty()
        {
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());

            var result = await CreateService("EMP1").GetMyExpenses();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMyExpenses_Unauthenticated_ReturnsEmpty()
        {
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>
            {
                new() { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 100 }
            });

            // Anonymous user — userId = "Anonymous", won't match "EMP1"
            var result = await CreateUnauthenticatedService().GetMyExpenses();
            result.Should().BeEmpty();
        }

        // ── UpdateExpense ─────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateExpense_DraftByOwner_Succeeds()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, Amount = 100, CategoryId = "C1", CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            _categoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExpenseCategory> { Cat() });
            SetupAudit();

            var (success, _, _) = await CreateService("EMP1").UpdateExpenseSafe("E1",
                new CreateExpenseRequestDto { CategoryId = "C1", CategoryName = "Travel", Amount = 300, ExpenseDate = DateTime.UtcNow });

            success.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateExpense_RejectedByOwner_Succeeds()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Rejected, Amount = 100, CategoryId = "C1", CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            _categoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExpenseCategory> { Cat() });
            SetupAudit();

            var (success, _, _) = await CreateService("EMP1").UpdateExpenseSafe("E1",
                new CreateExpenseRequestDto { CategoryId = "C1", CategoryName = "Travel", Amount = 300, ExpenseDate = DateTime.UtcNow });

            success.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateExpense_AdminUpdatesOthers_Succeeds()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, Amount = 100, CategoryId = "C1", CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            _categoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExpenseCategory> { Cat() });
            SetupAudit();

            var (success, _, _) = await CreateService("ADMIN1", "Admin").UpdateExpenseSafe("E1",
                new CreateExpenseRequestDto { CategoryId = "C1", CategoryName = "Travel", Amount = 300, ExpenseDate = DateTime.UtcNow });

            success.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateExpense_ApprovedStatus_Fails()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Approved };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            var (success, msg, _) = await CreateService("EMP1").UpdateExpenseSafe("E1",
                new CreateExpenseRequestDto { CategoryId = "C1", Amount = 100, ExpenseDate = DateTime.UtcNow });

            success.Should().BeFalse();
            msg.Should().Contain("Only Draft");
        }

        [Fact]
        public async Task UpdateExpense_WithEmptySentinel_ClearsDocuments()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, Amount = 100, CategoryId = "C1", CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            _categoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExpenseCategory> { Cat() });
            SetupAudit();

            var (success, _, dto) = await CreateService("EMP1").UpdateExpenseSafe("E1",
                new CreateExpenseRequestDto
                {
                    CategoryId = "C1", CategoryName = "Travel", Amount = 300, ExpenseDate = DateTime.UtcNow,
                    DocumentUrls = new List<string> { "__EMPTY__" }
                });

            success.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateExpense_NullDocumentUrls_KeepsOldDocs()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, Amount = 100, CategoryId = "C1", CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            _categoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExpenseCategory> { Cat() });
            SetupAudit();

            var (success, _, _) = await CreateService("EMP1").UpdateExpenseSafe("E1",
                new CreateExpenseRequestDto
                {
                    CategoryId = "C1", CategoryName = "Travel", Amount = 300, ExpenseDate = DateTime.UtcNow,
                    DocumentUrls = null
                });

            success.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateExpense_NotFound_ReturnsFalse()
        {
            _expenseRepo.Setup(r => r.GetByIdAsync("MISSING")).ThrowsAsync(new KeyNotFoundException());

            var (success, _, _) = await CreateService().UpdateExpenseSafe("MISSING",
                new CreateExpenseRequestDto { CategoryId = "C1", Amount = 100, ExpenseDate = DateTime.UtcNow });

            success.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateExpense_OtherUserNonAdmin_ReturnsFalse()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            var (success, msg, _) = await CreateService("OTHER").UpdateExpenseSafe("E1",
                new CreateExpenseRequestDto { CategoryId = "C1", Amount = 100, ExpenseDate = DateTime.UtcNow });

            success.Should().BeFalse();
            msg.Should().Contain("authorized");
        }

        // ── Additional branch coverage ────────────────────────────────────────

        [Fact]
        public async Task UpdateExpense_SubmittedByOwner_Succeeds()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Submitted, Amount = 100, CategoryId = "C1", CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            _categoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExpenseCategory> { Cat() });
            SetupAudit();

            var (success, _, _) = await CreateService("EMP1").UpdateExpenseSafe("E1",
                new CreateExpenseRequestDto { CategoryId = "C1", CategoryName = "Travel", Amount = 300, ExpenseDate = DateTime.UtcNow });

            success.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateExpense_PaidStatus_ReturnsFalse()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Paid };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            var (success, msg, _) = await CreateService("EMP1").UpdateExpenseSafe("E1",
                new CreateExpenseRequestDto { CategoryId = "C1", Amount = 100, ExpenseDate = DateTime.UtcNow });

            success.Should().BeFalse();
            msg.Should().Contain("Only Draft");
        }

        [Fact]
        public async Task UpdateExpense_NoCategoryName_LooksUpFromRepo()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, Amount = 100, CategoryId = "C1", CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            _categoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExpenseCategory> { Cat() });
            SetupAudit();

            // CategoryName is empty — service should look it up from repo
            var (success, _, _) = await CreateService("EMP1").UpdateExpenseSafe("E1",
                new CreateExpenseRequestDto { CategoryId = "C1", CategoryName = "", Amount = 300, ExpenseDate = DateTime.UtcNow });

            success.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateExpense_NoCategoryNameAndRepoFails_FallsBackToExisting()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, Amount = 100, CategoryId = "C1", CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            _categoryRepo.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("DB error"));
            SetupAudit();

            var (success, _, _) = await CreateService("EMP1").UpdateExpenseSafe("E1",
                new CreateExpenseRequestDto { CategoryId = "C1", CategoryName = "", Amount = 300, ExpenseDate = DateTime.UtcNow });

            success.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateExpense_NoCategoryNameAndNoMatch_FallsBackToCategoryId()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, Amount = 100, CategoryId = "C1", CategoryName = "" };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            _categoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExpenseCategory>());
            SetupAudit();

            var (success, _, dto) = await CreateService("EMP1").UpdateExpenseSafe("E1",
                new CreateExpenseRequestDto { CategoryId = "C1", CategoryName = "", Amount = 300, ExpenseDate = DateTime.UtcNow });

            success.Should().BeTrue();
        }

        [Fact]
        public async Task GetAllExpenses_FromDateOnly_FiltersCorrectly()
        {
            var today = DateTime.UtcNow;
            var expenses = new List<Expense>
            {
                new() { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, ExpenseDate = today.AddDays(-5), Amount = 100 },
                new() { ExpenseId = "E2", UserId = "EMP1", Status = ExpenseStatus.Draft, ExpenseDate = today,             Amount = 200 }
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expenses);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var result = await CreateService("EMP1", "Manager").GetAllExpenses(new PaginationParams
            { PageNumber = 1, PageSize = 10, FromDate = today.ToString("yyyy-MM-dd") });

            result.Data.Should().HaveCount(1);
            result.Data.First().ExpenseId.Should().Be("E2");
        }

        [Fact]
        public async Task GetAllExpenses_ToDateOnly_FiltersCorrectly()
        {
            var today = DateTime.UtcNow;
            var expenses = new List<Expense>
            {
                new() { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, ExpenseDate = today.AddDays(-1), Amount = 100 },
                new() { ExpenseId = "E2", UserId = "EMP1", Status = ExpenseStatus.Draft, ExpenseDate = today.AddDays(5),  Amount = 200 }
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expenses);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var result = await CreateService("EMP1", "Manager").GetAllExpenses(new PaginationParams
            { PageNumber = 1, PageSize = 10, ToDate = today.ToString("yyyy-MM-dd") });

            result.Data.Should().HaveCount(1);
            result.Data.First().ExpenseId.Should().Be("E1");
        }

        [Fact]
        public async Task GetAllExpenses_MinAmountOnly_FiltersCorrectly()
        {
            var expenses = new List<Expense>
            {
                new() { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 100 },
                new() { ExpenseId = "E2", UserId = "EMP1", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 500 }
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expenses);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var result = await CreateService("EMP1", "Manager").GetAllExpenses(new PaginationParams
            { PageNumber = 1, PageSize = 10, MinAmount = 300 });

            result.Data.Should().HaveCount(1);
            result.Data.First().Amount.Should().Be(500);
        }

        [Fact]
        public async Task GetAllExpenses_MaxAmountOnly_FiltersCorrectly()
        {
            var expenses = new List<Expense>
            {
                new() { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 100 },
                new() { ExpenseId = "E2", UserId = "EMP1", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 500 }
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expenses);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var result = await CreateService("EMP1", "Manager").GetAllExpenses(new PaginationParams
            { PageNumber = 1, PageSize = 10, MaxAmount = 300 });

            result.Data.Should().HaveCount(1);
            result.Data.First().Amount.Should().Be(100);
        }

        [Fact]
        public async Task GetAllExpenses_Unauthenticated_TreatedAsEmployee()
        {
            var expenses = new List<Expense>
            {
                new() { ExpenseId = "E1", UserId = "Anonymous", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 100 }
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expenses);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var result = await CreateUnauthenticatedService().GetAllExpenses(new PaginationParams { PageNumber = 1, PageSize = 10 });
            // Anonymous user treated as Employee, sees only own (Anonymous) expenses
            result.Data.Should().HaveCount(1);
        }

        [Fact]
        public async Task MapToDto_EmptyCategoryName_FallsBackToCategory()
        {
            var category = new ExpenseCategory { CategoryId = "C1", CategoryName = ExpenseCategoryType.Travel, MaxLimit = 5000 };
            var expense = new Expense
            {
                ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft,
                Amount = 100, ExpenseDate = DateTime.UtcNow,
                CategoryId = "C1", CategoryName = "", // empty — should fall back to Category nav prop
                Category = category
            };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            var result = await CreateService("EMP1", "Employee").GetExpenseById("E1");
            result.Should().NotBeNull();
            result!.CategoryName.Should().Be("Travel");
        }

        [Fact]
        public async Task GetExpenseById_AdminCanSeeAny_ReturnsDto()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, Amount = 100, ExpenseDate = DateTime.UtcNow };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            var result = await CreateService("ADMIN1", "Admin").GetExpenseById("E1");
            result.Should().NotBeNull();
        }
    }
}

// NOTE: The class above is closed. New tests below are in a separate partial class
// to avoid reopening the large file.

namespace ReimbursementTrackerApp.Tests.Services
{
    public class ExpenseServiceNewLogicTests
    {
        private readonly Mock<IRepository<string, Expense>>         _expenseRepo  = new();
        private readonly Mock<IRepository<string, ExpenseCategory>> _categoryRepo = new();
        private readonly Mock<IRepository<string, User>>            _userRepo     = new();
        private readonly Mock<IRepository<string, Approval>>        _approvalRepo = new();
        private readonly Mock<IAuditLogService>                     _auditService = new();
        private readonly Mock<IHttpContextAccessor>                 _httpContext  = new();
        private readonly Mock<IFileUploadService>                   _fileUpload   = new();
        private readonly Mock<INotificationService>                 _notifService = new();

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
            return new ExpenseService(_expenseRepo.Object, _categoryRepo.Object,
                _userRepo.Object, _approvalRepo.Object, _auditService.Object,
                _httpContext.Object, _fileUpload.Object, _notifService.Object);
        }

        private void SetupAudit() =>
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

        private void SetupNotif() =>
            _notifService.Setup(n => n.CreateNotification(It.IsAny<CreateNotificationRequestDto>()))
                .ReturnsAsync(new CreateNotificationResponseDto());

        // ── SubmitExpense: Admin auto-approve path ────────────────────────────

        [Fact]
        public async Task SubmitExpense_AdminExpense_AutoApproved()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "ADMIN1", Status = ExpenseStatus.Draft, Amount = 500, CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "ADMIN1", UserName = "Admin", Role = UserRole.Admin },
                new() { UserId = "FIN1",   UserName = "Finance", Role = UserRole.Finance }
            });
            SetupNotif();

            var result = await CreateService("ADMIN1", "Admin").SubmitExpense("E1");

            result!.Status.Should().Be("Approved");
            expense.Status.Should().Be(ExpenseStatus.Approved);
        }

        [Fact]
        public async Task SubmitExpense_AdminExpense_NotifiesFinance()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "ADMIN1", Status = ExpenseStatus.Draft, Amount = 500, CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "ADMIN1", UserName = "Admin", Role = UserRole.Admin },
                new() { UserId = "FIN1",   UserName = "Finance", Role = UserRole.Finance }
            });
            SetupNotif();

            await CreateService("ADMIN1", "Admin").SubmitExpense("E1");

            _notifService.Verify(n => n.CreateNotification(
                It.Is<CreateNotificationRequestDto>(r => r.UserId == "FIN1")), Times.Once);
        }

        [Fact]
        public async Task SubmitExpense_AdminExpense_NoFinanceUser_DoesNotThrow()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "ADMIN1", Status = ExpenseStatus.Draft, Amount = 500, CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "ADMIN1", UserName = "Admin", Role = UserRole.Admin }
                // no Finance user
            });
            SetupNotif();

            var result = await CreateService("ADMIN1", "Admin").SubmitExpense("E1");
            result!.Status.Should().Be("Approved");
        }

        [Fact]
        public async Task SubmitExpense_EmployeeWithManager_NotifiesManager()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, Amount = 300, CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "EMP1", UserName = "Employee", Role = UserRole.Employee, ManagerId = "MGR1" },
                new() { UserId = "MGR1", UserName = "Manager",  Role = UserRole.Manager }
            });
            SetupNotif();

            await CreateService("EMP1").SubmitExpense("E1");

            _notifService.Verify(n => n.CreateNotification(
                It.Is<CreateNotificationRequestDto>(r => r.UserId == "MGR1")), Times.Once);
        }

        [Fact]
        public async Task SubmitExpense_ManagerRole_NotifiesAdmin()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "MGR1", Status = ExpenseStatus.Draft, Amount = 300, CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "MGR1",   UserName = "Manager", Role = UserRole.Manager },
                new() { UserId = "ADMIN1", UserName = "Admin",   Role = UserRole.Admin }
            });
            SetupNotif();

            var result = await CreateService("MGR1", "Manager").SubmitExpense("E1");

            result!.Status.Should().Be("Submitted");
            _notifService.Verify(n => n.CreateNotification(
                It.Is<CreateNotificationRequestDto>(r => r.UserId == "ADMIN1")), Times.Once);
        }

        [Fact]
        public async Task SubmitExpense_EmployeeNoManagerId_DoesNotThrow()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, Amount = 300, CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "EMP1", UserName = "Employee", Role = UserRole.Employee, ManagerId = null }
            });
            SetupNotif();

            var result = await CreateService("EMP1").SubmitExpense("E1");
            result!.Status.Should().Be("Submitted");
        }

        // ── ResubmitExpense: notify approver ──────────────────────────────────

        [Fact]
        public async Task ResubmitExpense_NotifiesManager()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Rejected, Amount = 300, CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "EMP1", UserName = "Employee", Role = UserRole.Employee, ManagerId = "MGR1" },
                new() { UserId = "MGR1", UserName = "Manager",  Role = UserRole.Manager }
            });
            SetupNotif(); SetupAudit();

            await CreateService("EMP1").ResubmitExpense("E1");

            _notifService.Verify(n => n.CreateNotification(
                It.Is<CreateNotificationRequestDto>(r => r.UserId == "MGR1")), Times.Once);
        }

        // ── GetMyExpenses: approval comments mapped ───────────────────────────

        [Fact]
        public async Task GetMyExpenses_WithApproval_MapsCommentAndApproverName()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Approved, ExpenseDate = DateTime.UtcNow, Amount = 500 };
            var approval = new Approval { ApprovalId = "A1", ExpenseId = "E1", ManagerId = "MGR1", Comments = "Looks good", ApprovedAt = DateTime.UtcNow };

            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Approval> { approval });
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "EMP1", UserName = "Employee" },
                new() { UserId = "MGR1", UserName = "Manager One" }
            });

            var result = await CreateService("EMP1").GetMyExpenses();

            result.Should().HaveCount(1);
            result.First().ApprovalComment.Should().Be("Looks good");
            result.First().ApproverName.Should().Be("Manager One");
        }

        [Fact]
        public async Task GetMyExpenses_NoApproval_CommentEmpty()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Submitted, ExpenseDate = DateTime.UtcNow, Amount = 300 };

            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Approval>());
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var result = await CreateService("EMP1").GetMyExpenses();

            result.Should().HaveCount(1);
            result.First().ApprovalComment.Should().BeNullOrEmpty();
        }
    }
}

namespace ReimbursementTrackerApp.Tests.Services
{
    public class ExpenseServiceCoverageTests
    {
        private readonly Mock<IRepository<string, Expense>>         _expenseRepo  = new();
        private readonly Mock<IRepository<string, ExpenseCategory>> _categoryRepo = new();
        private readonly Mock<IRepository<string, User>>            _userRepo     = new();
        private readonly Mock<IRepository<string, Approval>>        _approvalRepo = new();
        private readonly Mock<IAuditLogService>                     _auditService = new();
        private readonly Mock<IHttpContextAccessor>                 _httpContext  = new();
        private readonly Mock<IFileUploadService>                   _fileUpload   = new();
        private readonly Mock<INotificationService>                 _notifService = new();

        private ExpenseService Svc(string userId = "EMP1", string role = "Employee")
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Name, "Test User"),
                new(ClaimTypes.Role, role)
            };
            var ctx = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) };
            _httpContext.Setup(h => h.HttpContext).Returns(ctx);
            return new ExpenseService(_expenseRepo.Object, _categoryRepo.Object,
                _userRepo.Object, _approvalRepo.Object, _auditService.Object,
                _httpContext.Object, _fileUpload.Object, _notifService.Object);
        }

        private ExpenseService SvcUnauth()
        {
            _httpContext.Setup(h => h.HttpContext).Returns(new DefaultHttpContext());
            return new ExpenseService(_expenseRepo.Object, _categoryRepo.Object,
                _userRepo.Object, _approvalRepo.Object, _auditService.Object,
                _httpContext.Object, _fileUpload.Object, _notifService.Object);
        }

        private void SetupAudit() =>
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

        private void SetupNotif() =>
            _notifService.Setup(n => n.CreateNotification(It.IsAny<CreateNotificationRequestDto>()))
                .ReturnsAsync(new CreateNotificationResponseDto());

        private ExpenseCategory Cat(decimal max = 5000) =>
            new() { CategoryId = "C1", CategoryName = ExpenseCategoryType.Travel, MaxLimit = max };

        // ── CreateExpense: null DocumentUrls → defaults to empty ──────────────
        [Fact]
        public async Task CreateExpense_NullDocumentUrls_DefaultsToEmpty()
        {
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense>());
            _categoryRepo.Setup(r => r.GetByIdAsync("C1")).ReturnsAsync(Cat());
            _expenseRepo.Setup(r => r.AddAsync(It.IsAny<Expense>())).ReturnsAsync((Expense e) => e);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            SetupAudit();

            var result = await Svc().CreateExpense(new CreateExpenseRequestDto
            { CategoryId = "C1", Amount = 500, ExpenseDate = DateTime.UtcNow, DocumentUrls = null });

            result!.DocumentUrls.Should().NotBeNull().And.BeEmpty();
        }

        // ── CreateExpense: rejected expense updates DocumentUrls ──────────────
        [Fact]
        public async Task CreateExpense_RejectedExpense_UpdatesDocumentUrls()
        {
            var rejected = new Expense
            {
                ExpenseId = "E1", UserId = "EMP1", ExpenseDate = DateTime.UtcNow,
                Status = ExpenseStatus.Rejected, Amount = 200,
                DocumentUrlsJson = System.Text.Json.JsonSerializer.Serialize(new List<string> { "/old.pdf" })
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { rejected });
            _categoryRepo.Setup(r => r.GetByIdAsync("C1")).ReturnsAsync(Cat());
            _expenseRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Expense>())).ReturnsAsync(rejected);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            SetupAudit();

            var result = await Svc().CreateExpense(new CreateExpenseRequestDto
            {
                CategoryId = "C1", Amount = 500, ExpenseDate = DateTime.UtcNow,
                DocumentUrls = new List<string> { "/new.pdf" }
            });

            result!.DocumentUrls.Should().Contain("/new.pdf");
        }

        // ── SubmitExpense: Finance role → notifies Admin ──────────────────────
        [Fact]
        public async Task SubmitExpense_FinanceRole_NotifiesAdmin()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "FIN1", Status = ExpenseStatus.Draft, Amount = 300, CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "FIN1",   UserName = "Finance", Role = UserRole.Finance },
                new() { UserId = "ADMIN1", UserName = "Admin",   Role = UserRole.Admin }
            });
            SetupNotif();

            var result = await Svc("FIN1", "Finance").SubmitExpense("E1");

            result!.Status.Should().Be("Submitted");
            _notifService.Verify(n => n.CreateNotification(
                It.Is<CreateNotificationRequestDto>(r => r.UserId == "ADMIN1")), Times.Once);
        }

        // ── SubmitExpense: Admin expense, Finance notification fails → no throw
        [Fact]
        public async Task SubmitExpense_AdminExpense_FinanceNotifFails_NoThrow()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "ADMIN1", Status = ExpenseStatus.Draft, Amount = 500, CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "ADMIN1", UserName = "Admin",   Role = UserRole.Admin },
                new() { UserId = "FIN1",   UserName = "Finance", Role = UserRole.Finance }
            });
            _notifService.Setup(n => n.CreateNotification(It.IsAny<CreateNotificationRequestDto>()))
                .ThrowsAsync(new Exception("Notif fail"));

            var result = await Svc("ADMIN1", "Admin").SubmitExpense("E1");
            result!.Status.Should().Be("Approved");
        }

        // ── SubmitExpense: submitter not found → no notification, no throw ─────
        [Fact]
        public async Task SubmitExpense_SubmitterNotFound_NoNotification()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, Amount = 300, CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>()); // no users
            SetupNotif();

            var result = await Svc("EMP1").SubmitExpense("E1");
            result!.Status.Should().Be("Submitted");
            _notifService.Verify(n => n.CreateNotification(It.IsAny<CreateNotificationRequestDto>()), Times.Never);
        }

        // ── GetAllExpenses: Manager sees dept employees only ──────────────────
        [Fact]
        public async Task GetAllExpenses_ManagerSeesOnlyDeptEmployees()
        {
            var expenses = new List<Expense>
            {
                new() { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 100 },
                new() { ExpenseId = "E2", UserId = "EMP2", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 200 },
                new() { ExpenseId = "E3", UserId = "EMP3", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 300 }
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expenses);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "MGR1", UserName = "Mgr",  Role = UserRole.Manager,  Department = DepartmentType.IT },
                new() { UserId = "EMP1", UserName = "Emp1", Role = UserRole.Employee, Department = DepartmentType.IT },
                new() { UserId = "EMP2", UserName = "Emp2", Role = UserRole.Employee, Department = DepartmentType.IT },
                new() { UserId = "EMP3", UserName = "Emp3", Role = UserRole.Employee, Department = DepartmentType.HR }
            });

            var result = await Svc("MGR1", "Manager").GetAllExpenses(
                new PaginationParams { PageNumber = 1, PageSize = 10 });

            // Manager in IT sees only IT employees (EMP1, EMP2), not HR (EMP3)
            result.Data.Should().HaveCount(2);
            result.Data.All(e => e.UserId == "EMP1" || e.UserId == "EMP2").Should().BeTrue();
        }

        // ── GetAllExpenses: Manager not found in users → sees all (no dept filter applied) ──
        [Fact]
        public async Task GetAllExpenses_ManagerNotInUserList_SeesAll()
        {
            var expenses = new List<Expense>
            {
                new() { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 100 }
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expenses);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "EMP1", UserName = "Emp1", Role = UserRole.Employee, Department = DepartmentType.IT }
                // MGR1 not in list → managerUser is null → no dept filter → sees all
            });

            var result = await Svc("MGR1", "Manager").GetAllExpenses(
                new PaginationParams { PageNumber = 1, PageSize = 10 });

            result.Data.Should().HaveCount(1);
        }

        // ── GetAllExpenses: Admin sees all ────────────────────────────────────
        [Fact]
        public async Task GetAllExpenses_AdminSeesAll()
        {
            var expenses = new List<Expense>
            {
                new() { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 100 },
                new() { ExpenseId = "E2", UserId = "EMP2", Status = ExpenseStatus.Draft, ExpenseDate = DateTime.UtcNow, Amount = 200 }
            };
            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expenses);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var result = await Svc("ADMIN1", "Admin").GetAllExpenses(
                new PaginationParams { PageNumber = 1, PageSize = 10 });

            result.Data.Should().HaveCount(2);
        }

        // ── GetMyExpenses: multiple approvals → picks latest ──────────────────
        [Fact]
        public async Task GetMyExpenses_MultipleApprovals_PicksLatest()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Approved, ExpenseDate = DateTime.UtcNow, Amount = 500 };
            var older = new Approval { ApprovalId = "A1", ExpenseId = "E1", ManagerId = "MGR1", Comments = "Old comment", ApprovedAt = DateTime.UtcNow.AddDays(-2) };
            var newer = new Approval { ApprovalId = "A2", ExpenseId = "E1", ManagerId = "MGR2", Comments = "New comment", ApprovedAt = DateTime.UtcNow };

            _expenseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Expense> { expense });
            _approvalRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Approval> { older, newer });
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "EMP1", UserName = "Employee" },
                new() { UserId = "MGR2", UserName = "Manager Two" }
            });

            var result = await Svc("EMP1").GetMyExpenses();
            result.First().ApprovalComment.Should().Be("New comment");
            result.First().ApproverName.Should().Be("Manager Two");
        }

        // ── DeleteExpense: RejectedExpense → cannot delete ────────────────────
        [Fact]
        public async Task DeleteExpense_RejectedExpense_Fails()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Rejected };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            var (success, msg, _) = await Svc("EMP1").DeleteExpenseSafe("E1");
            success.Should().BeFalse();
            msg.Should().Contain("Cannot delete");
        }

        // ── UpdateExpense: DocumentUrls with real paths kept ──────────────────
        [Fact]
        public async Task UpdateExpense_DocumentUrlsWithRealPaths_KeepsThem()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, Amount = 100, CategoryId = "C1", CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            _categoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExpenseCategory> { Cat() });
            SetupAudit();

            var (success, _, _) = await Svc("EMP1").UpdateExpenseSafe("E1",
                new CreateExpenseRequestDto
                {
                    CategoryId = "C1", CategoryName = "Travel", Amount = 300, ExpenseDate = DateTime.UtcNow,
                    DocumentUrls = new List<string> { "/uploads/file1.pdf", "__EMPTY__", "/uploads/file2.pdf" }
                });

            success.Should().BeTrue();
            // __EMPTY__ sentinel stripped, real paths kept
            expense.DocumentUrlsJson.Should().Contain("file1.pdf");
            expense.DocumentUrlsJson.Should().Contain("file2.pdf");
            expense.DocumentUrlsJson.Should().NotContain("__EMPTY__");
        }

        // ── CanEdit: Paid expense → CanEdit false ─────────────────────────────
        [Fact]
        public async Task GetExpenseById_PaidExpense_CanEditFalse()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Paid, Amount = 100, ExpenseDate = DateTime.UtcNow };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            var result = await Svc("EMP1", "Employee").GetExpenseById("E1");
            result!.CanEdit.Should().BeFalse();
        }

        // ── CanEdit: Draft expense by owner → CanEdit true ───────────────────
        [Fact]
        public async Task GetExpenseById_DraftByOwner_CanEditTrue()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Draft, Amount = 100, ExpenseDate = DateTime.UtcNow };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            var result = await Svc("EMP1", "Employee").GetExpenseById("E1");
            result!.CanEdit.Should().BeTrue();
        }

        // ── CanEdit: Rejected expense by owner → CanEdit true ────────────────
        [Fact]
        public async Task GetExpenseById_RejectedByOwner_CanEditTrue()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Rejected, Amount = 100, ExpenseDate = DateTime.UtcNow };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);

            var result = await Svc("EMP1", "Employee").GetExpenseById("E1");
            result!.CanEdit.Should().BeTrue();
        }

        // ── ResubmitExpense: notif fails → no throw ───────────────────────────
        [Fact]
        public async Task ResubmitExpense_NotifFails_NoThrow()
        {
            var expense = new Expense { ExpenseId = "E1", UserId = "EMP1", Status = ExpenseStatus.Rejected, Amount = 300, CategoryName = "Travel" };
            _expenseRepo.Setup(r => r.GetByIdAsync("E1")).ReturnsAsync(expense);
            _expenseRepo.Setup(r => r.UpdateAsync("E1", It.IsAny<Expense>())).ReturnsAsync(expense);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "EMP1", UserName = "Employee", Role = UserRole.Employee, ManagerId = "MGR1" }
            });
            _notifService.Setup(n => n.CreateNotification(It.IsAny<CreateNotificationRequestDto>()))
                .ThrowsAsync(new Exception("Notif fail"));
            SetupAudit();

            var result = await Svc("EMP1").ResubmitExpense("E1");
            result!.Status.Should().Be("Submitted");
        }
    }
}
