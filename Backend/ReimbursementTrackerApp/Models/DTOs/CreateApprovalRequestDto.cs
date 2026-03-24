using System.ComponentModel.DataAnnotations;

namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CreateApprovalRequestDto
    {
        [Required] public string ExpenseId { get; set; } = string.Empty;
        [Required] public string ManagerId { get; set; } = string.Empty;
        [Required] public string Status { get; set; } = string.Empty; // Approved / Rejected
        public string Comments { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty; // Optional approval level
    }
}
