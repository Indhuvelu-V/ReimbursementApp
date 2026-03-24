

using Microsoft.Extensions.Logging;
using ReimbursementTrackerApp.Data;
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models;
using ReimbursementTrackerApp.Models.DTOs;
using ReimbursementTrackerApp.Models.Enums;

namespace ReimbursementTrackerApp.Services
{
    public class ExpenseCategoryService : IExpenseCategoryService
    {
        private readonly IRepository<string, ExpenseCategory> _repository;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<ExpenseCategoryService> _logger; // Added ILogger

        public ExpenseCategoryService(
            IRepository<string, ExpenseCategory> repository,
            IAuditLogService auditLogService,
            ILogger<ExpenseCategoryService> logger) // Inject ILogger
        {
            _repository = repository;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        // =====================================================
        // Update the MaxLimit of an existing category
        // =====================================================
        public async Task<CreateExpenseCategoryResponseDto> UpdateCategoryLimit(CreateExpenseCategoryRequestDto request)
        {
            _logger.LogInformation("Attempting to update max limit for category {CategoryName}", request.CategoryName);

            var categories = await _repository.GetAllAsync();
            var category = categories?.FirstOrDefault(c => c.CategoryName == request.CategoryName);

            if (category == null)
            {
                _logger.LogWarning("Category {CategoryName} not found", request.CategoryName);
                throw new KeyNotFoundException("Category not found.");
            }

            if (request.MaxLimit <= 0)
            {
                _logger.LogWarning("Invalid MaxLimit {MaxLimit} for category {CategoryName}", request.MaxLimit, request.CategoryName);
                throw new ArgumentException("MaxLimit must be greater than zero.");
            }

            var oldMaxLimit = category.MaxLimit;
            category.MaxLimit = request.MaxLimit;

            await _repository.UpdateAsync(category.CategoryId, category);

            _logger.LogInformation("Updated MaxLimit for category {CategoryName} from {OldMax} to {NewMax}",
                category.CategoryName, oldMaxLimit, category.MaxLimit);

            // Audit log
            try
            {
                await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
                {
                    Action = $"Updated Expense Category '{category.CategoryName}'",
                    CategoryId = category.CategoryId,
                    MaxLimit = category.MaxLimit,
                    OldMaxLimit = oldMaxLimit
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit log failed for updating category {CategoryName}", category.CategoryName);
            }

            return new CreateExpenseCategoryResponseDto
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                MaxLimit = category.MaxLimit
            };
        }

        // =====================================================
        // Get all categories
        // =====================================================
        public async Task<List<CreateExpenseCategoryResponseDto>> GetAllCategories()
        {
            _logger.LogInformation("Fetching all expense categories");

            var categories = await _repository.GetAllAsync();

            // Audit log
            try
            {
                await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
                {
                    Action = "Fetched all expense categories"
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit log failed when fetching all categories");
            }

            _logger.LogInformation("Retrieved {Count} expense categories", categories?.Count() ?? 0);

            return categories?.Select(c => new CreateExpenseCategoryResponseDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                MaxLimit = c.MaxLimit
            }).ToList() ?? new List<CreateExpenseCategoryResponseDto>();
        }

        // =====================================================
        // Get single category by type
        // =====================================================
        public async Task<CreateExpenseCategoryResponseDto> GetCategoryByType(ExpenseCategoryType categoryType)
        {
            _logger.LogInformation("Fetching expense category by type {CategoryType}", categoryType);

            var categories = await _repository.GetAllAsync();
            var category = categories?.FirstOrDefault(c => c.CategoryName == categoryType);

            if (category == null)
            {
                _logger.LogWarning("Category of type {CategoryType} not found", categoryType);
                throw new KeyNotFoundException("Category not found.");
            }

            // Audit log
            try
            {
                await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
                {
                    Action = $"Fetched expense category by type '{categoryType}'",
                    CategoryId = category.CategoryId,
                    MaxLimit = category.MaxLimit
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit log failed when fetching category by type {CategoryType}", categoryType);
            }

            _logger.LogInformation("Retrieved category {CategoryName} with MaxLimit {MaxLimit}", category.CategoryName, category.MaxLimit);

            return new CreateExpenseCategoryResponseDto
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                MaxLimit = category.MaxLimit
            };
        }
    }
}