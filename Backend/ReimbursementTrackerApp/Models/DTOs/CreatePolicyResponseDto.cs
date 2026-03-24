namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CreatePolicyResponseDto
    {
        public string PolicyId { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        
    
        public string Description { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
    }
}