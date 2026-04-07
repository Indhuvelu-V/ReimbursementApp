using ReimbursementTrackerApp.Models.Enums;

namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CreateUserResponseDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public DepartmentType? Department { get; set; }
        public UserStatus Status { get; set; }
        public ApprovalLevel? ApprovalLevel { get; set; }
        public string? ReportingManagerId { get; set; }
        public string? ReportingManagerName { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string IfscCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
    }
}
