

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // ✅ add this
using ReimbursementTrackerApp.Models.DTOs;
using ReimbursementTrackerApp.Models.Enums;
using ReimbursementTrackerApp.Services;

namespace ReimbursementTrackerApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExpenseCategoryController : ControllerBase
    {
        private readonly IExpenseCategoryService _categoryService;
        private readonly ILogger<ExpenseCategoryController> _logger; // ✅ logger

        public ExpenseCategoryController(
            IExpenseCategoryService categoryService,
            ILogger<ExpenseCategoryController> logger) // ✅ inject
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        // =====================================================
        // UPDATE CATEGORY LIMIT
        // =====================================================
        [HttpPut]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategoryLimit([FromBody] CreateExpenseCategoryRequestDto request)
        {
            _logger.LogInformation("Request to update category limit for {CategoryType}", request.CategoryName);

            try
            {
                var result = await _categoryService.UpdateCategoryLimit(request);

                _logger.LogInformation("Category {CategoryType} updated successfully", request.CategoryName);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Category {CategoryType} not found", request.CategoryName);
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid data while updating category {CategoryType}", request.CategoryName);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category {CategoryType}", request.CategoryName);
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // =====================================================
        // GET ALL CATEGORIES
        // =====================================================
        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Employee,Finance,TeamLead")]
        public async Task<IActionResult> GetAllCategories()
        {
            _logger.LogInformation("Request to fetch all categories");

            try
            {
                var categories = await _categoryService.GetAllCategories();

                _logger.LogInformation("Fetched {Count} categories", categories?.Count ?? 0);

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching categories");
                return StatusCode(500, new { message = "Failed to fetch categories", details = ex.Message });
            }
        }

        // =====================================================
        // GET CATEGORY BY TYPE
        // =====================================================
        [HttpGet("{categoryType}")]
        [Authorize(Roles = "Admin,Manager,Employee,Finance,TeamLead")]
        public async Task<IActionResult> GetCategoryByType(ExpenseCategoryType categoryType)
        {
            _logger.LogInformation("Request to fetch category {CategoryType}", categoryType);

            try
            {
                var category = await _categoryService.GetCategoryByType(categoryType);

                _logger.LogInformation("Category {CategoryType} fetched successfully", categoryType);

                return Ok(category);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Category {CategoryType} not found", categoryType);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching category {CategoryType}", categoryType);
                return StatusCode(500, new { message = "Failed to fetch category", details = ex.Message });
            }
        }
    }
}