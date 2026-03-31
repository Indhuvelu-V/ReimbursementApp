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
    public class AuditLogServiceTests
    {
        private readonly Mock<IRepository<string, AuditLog>> _logRepo     = new();
        private readonly Mock<IHttpContextAccessor>           _httpContext = new();

        private AuditLogService CreateService(string userId = "U1", string userName = "Alice", string role = "Admin")
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Name, userName),
                new(ClaimTypes.Role, role)
            };
            var ctx = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) };
            _httpContext.Setup(h => h.HttpContext).Returns(ctx);
            return new AuditLogService(_logRepo.Object, _httpContext.Object);
        }

        private void SetupAdd() =>
            _logRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync((AuditLog l) => l);

        // ── CreateLog ─────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateLog_FromToken_PopulatesUserInfo()
        {
            SetupAdd();
            var svc = CreateService("U1", "Alice", "Employee");
            var result = await svc.CreateLog(new CreateAuditLogsRequestDto
            {
                Action = "Created Expense E1", ExpenseId = "E1", Amount = 500
            });
            result.UserName.Should().Be("Alice");
            result.UserId.Should().Be("U1");
            result.Action.Should().Be("Created Expense E1");
        }

        [Fact]
        public async Task CreateLog_WithExplicitUserInfo_UsesProvidedInfo()
        {
            SetupAdd();
            var svc = CreateService();
            var result = await svc.CreateLog(new CreateAuditLogsRequestDto
            {
                Action = "Registered User", UserId = "EX1", UserName = "Explicit", Role = UserRole.Admin
            });
            result.UserId.Should().Be("EX1");
            result.UserName.Should().Be("Explicit");
        }

        [Fact]
        public async Task CreateLog_SetsDateToUtcNow()
        {
            SetupAdd();
            var before = DateTime.UtcNow.AddSeconds(-1);
            var svc = CreateService();
            var result = await svc.CreateLog(new CreateAuditLogsRequestDto { Action = "Test Action" });
            result.Date.Should().BeAfter(before);
        }

        [Fact]
        public async Task CreateLog_ReturnsDescriptionWithAction()
        {
            SetupAdd();
            var svc = CreateService();
            var result = await svc.CreateLog(new CreateAuditLogsRequestDto { Action = "Deleted Expense E5" });
            result.Description.Should().Contain("Deleted Expense E5");
        }

        // ── GetAllLogs — exclusion ────────────────────────────────────────────

        [Fact]
        public async Task GetAllLogs_ExcludesFetchedLogs()
        {
            _logRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<AuditLog>
            {
                new() { LogId = "L1", Action = "Created Expense E1",   Date = DateTime.UtcNow, UserName = "Alice" },
                new() { LogId = "L2", Action = "Fetched all expenses", Date = DateTime.UtcNow, UserName = "Alice" },
                new() { LogId = "L3", Action = "Deleted Expense E2",   Date = DateTime.UtcNow, UserName = "Bob" }
            });
            var result = await CreateService().GetAllLogs(new PaginationParams { PageNumber = 1, PageSize = 10 });
            result.Data.Should().HaveCount(2);
            result.Data.Any(l => l.Action.Contains("Fetched")).Should().BeFalse();
        }

        [Fact]
        public async Task GetAllLogs_ExcludesGetLogs()
        {
            _logRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<AuditLog>
            {
                new() { LogId = "L1", Action = "get all users",      Date = DateTime.UtcNow, UserName = "Alice" },
                new() { LogId = "L2", Action = "Updated Expense E1", Date = DateTime.UtcNow, UserName = "Alice" }
            });
            var result = await CreateService().GetAllLogs(new PaginationParams { PageNumber = 1, PageSize = 10 });
            result.Data.Should().HaveCount(1);
            result.Data.First().LogId.Should().Be("L2");
        }

        // ── GetAllLogs — action filter ────────────────────────────────────────

        [Theory]
        [InlineData("payment",  "Paid Expense E1",                    true)]
        [InlineData("payment",  "CompletePayment executed on Payment", true)]
        [InlineData("payment",  "Created Expense E1",                 false)]
        [InlineData("approved", "Manager Approved expense E1",        true)]
        [InlineData("approved", "Rejected Expense E1",                false)]
        [InlineData("rejected", "Manager Rejected expense E1",        true)]
        [InlineData("rejected", "Approved expense E1",                false)]
        [InlineData("submitted","Submitted Expense E1",               true)]
        [InlineData("submitted","Resubmitted Expense E1",             true)]
        [InlineData("submitted","Created Expense E1",                 false)]
        [InlineData("created",  "Created Expense E1",                 true)]
        [InlineData("created",  "Deleted Expense E1",                 false)]
        [InlineData("updated",  "Updated Expense E1",                 true)]
        [InlineData("updated",  "Created Expense E1",                 false)]
        [InlineData("deleted",  "Deleted Expense E1",                 true)]
        [InlineData("deleted",  "Created Expense E1",                 false)]
        public async Task GetAllLogs_ActionFilter_ClassifiesCorrectly(
            string filterValue, string storedAction, bool shouldMatch)
        {
            _logRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<AuditLog>
            {
                new() { LogId = "L1", Action = storedAction, Date = DateTime.UtcNow, UserName = "Alice" }
            });
            var result = await CreateService().GetAllLogs(new PaginationParams
            {
                PageNumber = 1, PageSize = 10, Action = filterValue
            });
            if (shouldMatch)
                result.Data.Should().HaveCount(1);
            else
                result.Data.Should().BeEmpty();
        }

        // ── GetAllLogs — date filter ──────────────────────────────────────────

        [Fact]
        public async Task GetAllLogs_DateFilter_ReturnsOnlyInRange()
        {
            var today = DateTime.UtcNow;
            _logRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<AuditLog>
            {
                new() { LogId = "L1", Action = "Created Expense E1", Date = today.AddDays(-2), UserName = "A" },
                new() { LogId = "L2", Action = "Updated Expense E2", Date = today,             UserName = "A" },
                new() { LogId = "L3", Action = "Deleted Expense E3", Date = today.AddDays(2),  UserName = "A" }
            });
            var result = await CreateService().GetAllLogs(new PaginationParams
            {
                PageNumber = 1, PageSize = 10,
                FromDate = today.ToString("yyyy-MM-dd"),
                ToDate   = today.ToString("yyyy-MM-dd")
            });
            result.Data.Should().HaveCount(1);
            result.Data.First().LogId.Should().Be("L2");
        }

        // ── GetAllLogs — username filter ──────────────────────────────────────

        [Fact]
        public async Task GetAllLogs_UserNameFilter_ReturnsMatchingUser()
        {
            _logRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<AuditLog>
            {
                new() { LogId = "L1", Action = "Created Expense", Date = DateTime.UtcNow, UserName = "alice" },
                new() { LogId = "L2", Action = "Deleted Expense", Date = DateTime.UtcNow, UserName = "bob" }
            });
            var result = await CreateService().GetAllLogs(new PaginationParams
            {
                PageNumber = 1, PageSize = 10, UserName = "alice"
            });
            result.Data.Should().HaveCount(1);
            result.Data.First().UserName.Should().Be("alice");
        }

        // ── GetAllLogs — pagination ───────────────────────────────────────────

        [Fact]
        public async Task GetAllLogs_Pagination_ReturnsCorrectPage()
        {
            var logs = Enumerable.Range(1, 12)
                .Select(i => new AuditLog { LogId = $"L{i}", Action = $"Created Expense E{i}", Date = DateTime.UtcNow, UserName = "Alice" })
                .ToList();
            _logRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(logs);
            var result = await CreateService().GetAllLogs(new PaginationParams { PageNumber = 2, PageSize = 5 });
            result.Data.Should().HaveCount(5);
            result.TotalRecords.Should().Be(12);
        }

        [Fact]
        public async Task GetAllLogs_EmptyRepo_ReturnsEmpty()
        {
            _logRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<AuditLog>());
            var result = await CreateService().GetAllLogs(new PaginationParams { PageNumber = 1, PageSize = 10 });
            result.Data.Should().BeEmpty();
            result.TotalRecords.Should().Be(0);
        }

        // ── DeleteLog ─────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteLog_AdminRole_ReturnsTrue()
        {
            var log = new AuditLog { LogId = "L1", Action = "Test", UserName = "Alice" };
            _logRepo.Setup(r => r.GetByIdAsync("L1")).ReturnsAsync(log);
            _logRepo.Setup(r => r.DeleteAsync("L1")).ReturnsAsync(log);
            var result = await CreateService("U1", "Alice", "Admin").DeleteLog("L1");
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteLog_NonAdmin_ThrowsUnauthorized()
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                CreateService("U1", "Alice", "Employee").DeleteLog("L1"));
        }

        [Fact]
        public async Task DeleteLog_NotFound_ReturnsFalse()
        {
            _logRepo.Setup(r => r.GetByIdAsync("MISSING")).ThrowsAsync(new KeyNotFoundException());
            var result = await CreateService("U1", "Alice", "Admin").DeleteLog("MISSING");
            result.Should().BeFalse();
        }
    }
}
