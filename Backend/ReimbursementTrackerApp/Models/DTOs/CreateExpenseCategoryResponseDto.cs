using ReimbursementTrackerApp.Models.Enums;

namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CreateExpenseCategoryResponseDto
    {
        public string CategoryId { get; set; } = string.Empty;
        public ExpenseCategoryType CategoryName { get; set; } 
        public decimal MaxLimit { get; set; }
    }
}
