namespace ReimbursementTrackerApp.Models.DTOs
{
    public class UpdateMyProfileRequestDto
    {
        public string? Phone         { get; set; }
        public string? BankName      { get; set; }
        public string? AccountNumber { get; set; }
        public string? IfscCode      { get; set; }
        public string? BranchName    { get; set; }
    }
}
