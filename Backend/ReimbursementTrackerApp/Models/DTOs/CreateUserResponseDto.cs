using ReimbursementTrackerApp.Models.Enums;

namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CreateUserResponseDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public DepartmentType Department { get; set; }
        public UserStatus Status { get; set; }
        public ApprovalLevel? ApprovalLevel { get; set; }
    }
}
