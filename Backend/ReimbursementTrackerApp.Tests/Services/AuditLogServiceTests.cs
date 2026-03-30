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

        private AuditLogService CreateService(string userId = "U1", string userName = "Alice", string role = "Employee")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, role)
            };
            var identity  = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ctx       = new DefaultHttpContext { User = principal };
            _httpContext.Setup(h => h.HttpContext).Returns(ctx);
            return new AuditLogService(_logRepo.Object, _httpContext.Object);
        }

        // ── CreateLog ─────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateLog_FromToken_PopulatesUserInfo()
        {
            _logRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync((AuditLog l) => l);

            var svc    = CreateService("U1", "Alice", "Employee");
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
            _logRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync((AuditLog l) => l);

            var svc    = CreateService();
            var result = await svc.CreateLog(new CreateAuditLogsRequestDto
            {
                Action   = "Registered User",
                UserId   = "EXPLICIT_ID",
                UserName = "Explicit User",
                Role     = UserRole.Admin
            });

            result.UserId.Should().Be("EXPLICIT_ID");
            result.UserName.Should().Be("Explicit User");
        }

        // ── GetAllLogs ────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllLogs_NoFilter_ReturnsAllNonViewLogs()
        {
            var logs = new List<AuditLog>
            {
                new AuditLog { LogId = "L1", Action = "Created Expense E1",  Date = DateTime.UtcNow, UserName = "Alice" },
                new AuditLog { LogId = "L2", Action = "Fetched all expenses", Date = DateTime.UtcNow, UserName = "Alice" }, // should be excluded
                new AuditLog { LogId = "L3", Action = "Deleted Expense E2",  Date = DateTime.UtcNow, UserName = "Bob" }
            };
            _logRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(logs);

            var svc    = CreateService();
            var result = await svc.GetAllLogs(new PaginationParams { PageNumber = 1, PageSize = 10 });

            // "Fetched" logs are excluded by the service
            result.Data.Should().HaveCount(2);
            result.Data.Any(l => l.Action.Contains("Fetched")).Should().BeFalse();
        }

        [Fact]
        public async Task GetAllLogs_DateFilter_ReturnsOnlyInRange()
        {
            var yesterday = DateTime.UtcNow.AddDays(-1);
            var today     = DateTime.UtcNow;
            var tomorrow  = DateTime.UtcNow.AddDays(1);

            var logs = new List<AuditLog>
            {
                new AuditLog { LogId = "L1", Action = "Created Expense E1", Date = yesterday, UserName = "Alice" },
                new AuditLog { LogId = "L2", Action = "Updated Expense E2", Date = today,     UserName = "Alice" },
                new AuditLog { LogId = "L3", Action = "Deleted Expense E3", Date = tomorrow,  UserName = "Alice" }
            };
            _logRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(logs);

            var svc    = CreateService();
            var result = await svc.GetAllLogs(new PaginationParams
            {
                PageNumber = 1, PageSize = 10,
                FromDate   = today.ToString("yyyy-MM-dd"),
                ToDate     = today.ToString("yyyy-MM-dd")
            });

            result.Data.Should().HaveCount(1);
            result.Data.First().LogId.Should().Be("L2");
        }

        [Fact]
        public async Task GetAllLogs_Pagination_ReturnsCorrectPage()
        {
            var logs = Enumerable.Range(1, 12)
                .Select(i => new AuditLog
                {
                    LogId    = $"L{i}",
                    Action   = $"Created Expense E{i}",
                    Date     = DateTime.UtcNow,
                    UserName = "Alice"
                })
                .ToList();
            _logRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(logs);

            var svc    = CreateService();
            var result = await svc.GetAllLogs(new PaginationParams { PageNumber = 2, PageSize = 5 });

            result.Data.Should().HaveCount(5);
            result.TotalRecords.Should().Be(12);
            result.TotalPages.Should().Be(3);
        }

        // ── DeleteLog ─────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteLog_ExistingLog_ReturnsTrue()
        {
            var log = new AuditLog { LogId = "L1", Action = "Test", UserName = "Alice" };
            _logRepo.Setup(r => r.GetByIdAsync("L1")).ReturnsAsync(log);
            _logRepo.Setup(r => r.DeleteAsync("L1")).ReturnsAsync(log);

            // Must be Admin to delete
            var svc    = CreateService("U1", "Alice", "Admin");
            var result = await svc.DeleteLog("L1");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteLog_NonAdmin_ThrowsUnauthorized()
        {
            var svc = CreateService("U1", "Alice", "Employee");
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.DeleteLog("L1"));
        }
    }
}
