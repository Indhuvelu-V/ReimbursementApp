
namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CreateApprovalResponseDto
    {
        public string ExpenseId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Approved/Rejected/Pending
        public DateTime? ApprovalDate { get; set; }
        public string ApprovalId { get; set; } = string.Empty;
        public string ApproverName { get; set; } = string.Empty; // Manager or Finance

        public string Comments { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public DateTime? ApprovedAt { get; set; }

        // Notification message for employee
        public string NotificationMessage => Status.ToLower() switch
        {
            "approved" => $"Your expense {ExpenseId} has been approved.",
            "rejected" => $"Your expense {ExpenseId} has been rejected.",
            _ => $"Your expense {ExpenseId} is pending approval."
        };
    }
}