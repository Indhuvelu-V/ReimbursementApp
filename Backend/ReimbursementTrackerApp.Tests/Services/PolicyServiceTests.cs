using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models;
using ReimbursementTrackerApp.Models.DTOs;
using ReimbursementTrackerApp.Services;
using Xunit;

namespace ReimbursementTrackerApp.Tests.Services
{
    public class PolicyServiceTests
    {
        private readonly Mock<IRepository<string, Policy>> _repo         = new();
        private readonly Mock<IAuditLogService>             _auditService = new();
        private readonly Mock<ILogger<PolicyService>>       _logger       = new();

        private PolicyService CreateService() =>
            new(_repo.Object, _auditService.Object, _logger.Object);

        private void SetupAudit() =>
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

        private List<Policy> DefaultPolicies() => new()
        {
            new() { PolicyId = "P1", CategoryId = "C1", CategoryName = "Travel",        Description = "Travel policy" },
            new() { PolicyId = "P2", CategoryId = "C2", CategoryName = "Food",          Description = "Food policy" },
            new() { PolicyId = "P3", CategoryId = "C3", CategoryName = "Medical",       Description = "Medical policy" },
            new() { PolicyId = "P4", CategoryId = "C4", CategoryName = "OfficeSupplies",Description = "Office policy" }
        };

        [Fact]
        public async Task GetAllPoliciesAsync_ReturnsPolicies()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(DefaultPolicies());
            SetupAudit();

            var result = (await CreateService().GetAllPoliciesAsync()).ToList();

            result.Should().HaveCount(4);
            result.Any(p => p.CategoryName == "Travel").Should().BeTrue();
        }

        [Fact]
        public async Task GetAllPoliciesAsync_EmptyRepo_ReturnsEmptyList()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Policy>());
            SetupAudit();

            var result = await CreateService().GetAllPoliciesAsync();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllPoliciesAsync_MapsFieldsCorrectly()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Policy>
            {
                new() { PolicyId = "P1", CategoryId = "C1", CategoryName = "Travel", Description = "Travel policy" }
            });
            SetupAudit();

            var result = (await CreateService().GetAllPoliciesAsync()).First();

            result.PolicyId.Should().Be("P1");
            result.CategoryId.Should().Be("C1");
            result.Description.Should().Be("Travel policy");
        }

        [Fact]
        public async Task GetAllPoliciesAsync_RepoThrows_ThrowsException()
        {
            _repo.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("DB error"));

            await Assert.ThrowsAsync<Exception>(() => CreateService().GetAllPoliciesAsync());
        }

        [Fact]
        public async Task GetAllPoliciesAsync_NullRepo_ReturnsEmpty()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync((IEnumerable<Policy>?)null);
            SetupAudit();

            var result = await CreateService().GetAllPoliciesAsync();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllPoliciesAsync_AuditLogCalled()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(DefaultPolicies());
            SetupAudit();

            await CreateService().GetAllPoliciesAsync();

            _auditService.Verify(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()), Times.Once);
        }
    }
}
