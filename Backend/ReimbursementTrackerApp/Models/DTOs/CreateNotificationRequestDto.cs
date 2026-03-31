using System.ComponentModel.DataAnnotations;

namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CreateNotificationRequestDto
    {
        [Required] public string UserId { get; set; } = string.Empty;
        [Required] public string Message { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SenderRole { get; set; } = "Manager";
        public string? SenderId { get; set; }
    }
}
