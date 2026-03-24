using ReimbursementTrackerApp.Models.Enums;

namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CreateUserResponseDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public UserRole Role { get; set; }          // Enum
        public DepartmentType Department { get; set; } // Enum

        public UserStatus Status { get; set; }
        public ApprovalLevel? ApprovalLevel { get; set; }
    }
}
