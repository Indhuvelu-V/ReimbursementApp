

using ReimbursementTrackerApp.Models.DTOs;
using ReimbursementTrackerApp.Models.Enums;

namespace ReimbursementTrackerApp.Services
{
    public interface IExpenseCategoryService
    {
       
        Task<CreateExpenseCategoryResponseDto> UpdateCategoryLimit(CreateExpenseCategoryRequestDto request);

        
        Task<List<CreateExpenseCategoryResponseDto>> GetAllCategories();

        Task<CreateExpenseCategoryResponseDto> GetCategoryByType(ExpenseCategoryType categoryType);
    }
}