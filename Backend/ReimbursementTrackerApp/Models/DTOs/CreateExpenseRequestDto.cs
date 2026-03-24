
using System.ComponentModel.DataAnnotations;

namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CreateExpenseRequestDto
    {
       

        [Required]
        public string CategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }


        [Required]
        public DateTime ExpenseDate { get; set; }
        public List<IFormFile>? Documents { get; set; } // uploaded files

        public List<string>? DocumentUrls { get; set; }
    }
}
