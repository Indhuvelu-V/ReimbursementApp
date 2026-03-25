
//namespace ReimbursementTrackerApp.Models.DTOs
//{
//    public class CreateApprovalResponseDto
//    {
//        public string ExpenseId { get; set; } = string.Empty;
//        public string Status { get; set; } = string.Empty; // Approved/Rejected/Pending
//        public DateTime? ApprovalDate { get; set; }
//        public string ApprovalId { get; set; } = string.Empty;
//        public string ApproverName { get; set; } = string.Empty; // Manager or Finance

//        public string Comments { get; set; } = string.Empty;
//        public string Level { get; set; } = string.Empty;
//        public DateTime? ApprovedAt { get; set; }

//        // Notification message for employee
//        public string NotificationMessage => Status.ToLower() switch
//        {
//            "approved" => $"Your expense {ExpenseId} has been approved.",
//            "rejected" => $"Your expense {ExpenseId} has been rejected.",
//            _ => $"Your expense {ExpenseId} is pending approval."
//        };
//    }
//}

// FILE: Models/DTOs/CreateApprovalResponseDto.cs
// CHANGE: Added DocumentUrls and ExpenseAmount so the Manager Approval screen
//         can display the uploaded image without any extra API call.
//         No new DTO created — just one property added to the existing class.

namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CreateApprovalResponseDto
    {
        public string ExpenseId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Approved/Rejected/Pending
        public DateTime? ApprovalDate { get; set; }
        public string ApprovalId { get; set; } = string.Empty;
        public string ApproverName { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public DateTime? ApprovedAt { get; set; }

        // ✅ ADDED — image paths from the original expense, reused everywhere.
        // Stored as /uploads/xyz.jpg → frontend builds full URL with base URL.
        // No re-upload, no duplication — same paths from DB.
        public List<string> DocumentUrls { get; set; } = new();

        // ✅ ADDED — useful for approval screen to show expense amount alongside image
        public decimal ExpenseAmount { get; set; }
        public string AmountInRupees { get; set; } = string.Empty;

        public string NotificationMessage => Status.ToLower() switch
        {
            "approved" => $"Your expense {ExpenseId} has been approved.",
            "rejected" => $"Your expense {ExpenseId} has been rejected.",
            _ => $"Your expense {ExpenseId} is pending approval."
        };
    }
}
