using ReimbursementTrackerApp.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CreateExpenseCategoryRequestDto
    {
       
        [Required]
        public ExpenseCategoryType CategoryName { get; set; }

        [Range(1, double.MaxValue, ErrorMessage = "Max limit must be greater than zero.")]
        public decimal MaxLimit { get; set; }

      

    }
}
