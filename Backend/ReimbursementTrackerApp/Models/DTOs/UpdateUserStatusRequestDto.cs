using ReimbursementTrackerApp.Models.Enums;

namespace ReimbursementTrackerApp.Models.DTOs
{
    public class UpdateUserStatusRequestDto
    {
        public string     UserId { get; set; } = string.Empty;
        public UserStatus Status { get; set; }
    }
}
