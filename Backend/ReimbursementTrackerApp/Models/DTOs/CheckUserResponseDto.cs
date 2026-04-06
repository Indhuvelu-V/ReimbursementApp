using ReimbursementTrackerApp.Models.Enums;

namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CheckUserResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string? ReportingManagerName { get; set; }
        public string? ReportingManagerId { get; set; }
    }
}