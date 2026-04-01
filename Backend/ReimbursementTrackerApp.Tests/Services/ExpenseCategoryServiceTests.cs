using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models;
using ReimbursementTrackerApp.Models.DTOs;
using ReimbursementTrackerApp.Models.Enums;
using ReimbursementTrackerApp.Services;
using Xunit;

namespace ReimbursementTrackerApp.Tests.Services
{
    public class ExpenseCategoryServiceTests
    {
        private readonly Mock<IRepository<string, ExpenseCategory>> _repo         = new();
        private readonly Mock<IAuditLogService>                      _auditService = new();
        private readonly Mock<ILogger<ExpenseCategoryService>>       _logger       = new();

        private ExpenseCategoryService CreateService() =>
            new(_repo.Object, _auditService.Object, _logger.Object);

        private void SetupAudit() =>
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

        private List<ExpenseCategory> DefaultCategories() => new()
        {
            new() { CategoryId = "C1", CategoryName = ExpenseCategoryType.Travel,        MaxLimit = 5000 },
            new() { CategoryId = "C2", CategoryName = ExpenseCategoryType.Food,          MaxLimit = 1000 },
            new() { CategoryId = "C3", CategoryName = ExpenseCategoryType.Medical,       MaxLimit = 10000 },
            new() { CategoryId = "C4", CategoryName = ExpenseCategoryType.OfficeSupplies,MaxLimit = 3000 }
        };

        // ── GetAllCategories ──────────────────────────────────────────────────

        [Fact]
        public async Task GetAllCategories_ReturnsAllCategories()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(DefaultCategories());
            SetupAudit();

            var result = await CreateService().GetAllCategories();

            result.Should().HaveCount(4);
            result.Any(c => c.CategoryName == ExpenseCategoryType.Travel).Should().BeTrue();
        }

        [Fact]
        public async Task GetAllCategories_EmptyRepo_ReturnsEmptyList()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExpenseCategory>());
            SetupAudit();

            var result = await CreateService().GetAllCategories();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllCategories_AuditFails_StillReturnsData()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(DefaultCategories());
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ThrowsAsync(new Exception("Audit failure"));

            var result = await CreateService().GetAllCategories();
            result.Should().HaveCount(4);
        }

        // ── GetCategoryByType ─────────────────────────────────────────────────

        [Fact]
        public async Task GetCategoryByType_ExistingType_ReturnsDto()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(DefaultCategories());
            SetupAudit();

            var result = await CreateService().GetCategoryByType(ExpenseCategoryType.Travel);

            result.Should().NotBeNull();
            result.CategoryName.Should().Be(ExpenseCategoryType.Travel);
            result.MaxLimit.Should().Be(5000);
        }

        [Fact]
        public async Task GetCategoryByType_NotFound_ThrowsKeyNotFound()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExpenseCategory>());
            SetupAudit();

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                CreateService().GetCategoryByType(ExpenseCategoryType.Medical));
        }

        // ── UpdateCategoryLimit ───────────────────────────────────────────────

        [Fact]
        public async Task UpdateCategoryLimit_ValidRequest_UpdatesMaxLimit()
        {
            var categories = DefaultCategories();
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);
            _repo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<ExpenseCategory>()))
                .ReturnsAsync((string k, ExpenseCategory c) => c);
            SetupAudit();

            var result = await CreateService().UpdateCategoryLimit(new CreateExpenseCategoryRequestDto
            {
                CategoryName = ExpenseCategoryType.Travel, MaxLimit = 8000
            });

            result.MaxLimit.Should().Be(8000);
            result.CategoryName.Should().Be(ExpenseCategoryType.Travel);
        }

        [Fact]
        public async Task UpdateCategoryLimit_CategoryNotFound_ThrowsKeyNotFound()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExpenseCategory>());

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                CreateService().UpdateCategoryLimit(new CreateExpenseCategoryRequestDto
                {
                    CategoryName = ExpenseCategoryType.Travel, MaxLimit = 5000
                }));
        }

        [Fact]
        public async Task UpdateCategoryLimit_ZeroMaxLimit_ThrowsArgumentException()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(DefaultCategories());

            await Assert.ThrowsAsync<ArgumentException>(() =>
                CreateService().UpdateCategoryLimit(new CreateExpenseCategoryRequestDto
                {
                    CategoryName = ExpenseCategoryType.Travel, MaxLimit = 0
                }));
        }

        [Fact]
        public async Task UpdateCategoryLimit_NegativeMaxLimit_ThrowsArgumentException()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(DefaultCategories());

            await Assert.ThrowsAsync<ArgumentException>(() =>
                CreateService().UpdateCategoryLimit(new CreateExpenseCategoryRequestDto
                {
                    CategoryName = ExpenseCategoryType.Food, MaxLimit = -100
                }));
        }

        [Fact]
        public async Task GetCategoryByType_AuditFails_StillReturnsDto()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(DefaultCategories());
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ThrowsAsync(new Exception("Audit fail"));

            var result = await CreateService().GetCategoryByType(ExpenseCategoryType.Travel);
            result.Should().NotBeNull();
            result.MaxLimit.Should().Be(5000);
        }

        [Fact]
        public async Task UpdateCategoryLimit_AuditFails_StillReturnsDto()
        {
            var categories = DefaultCategories();
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);
            _repo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<ExpenseCategory>()))
                .ReturnsAsync((string k, ExpenseCategory c) => c);
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ThrowsAsync(new Exception("Audit fail"));

            var result = await CreateService().UpdateCategoryLimit(new CreateExpenseCategoryRequestDto
            { CategoryName = ExpenseCategoryType.Travel, MaxLimit = 9000 });

            result.MaxLimit.Should().Be(9000);
        }

        [Fact]
        public async Task GetAllCategories_NullRepo_ReturnsEmpty()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync((IEnumerable<ExpenseCategory>?)null);
            SetupAudit();

            var result = await CreateService().GetAllCategories();
            result.Should().BeEmpty();
        }
    }
}
