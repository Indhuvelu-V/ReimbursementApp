namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CreateNotificationResponseDto
    {
        public string NotificationId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ReadStatus { get; set; } = "Unread"; // Read/Unread
        public DateTime CreatedAt { get; set; } = DateTime.Now;
       
        public string Description { get; set; } = string.Empty;
        public string? Reply { get; set; }
      
        public string SenderRole { get; set; } = "Manager";
       

    }
}

