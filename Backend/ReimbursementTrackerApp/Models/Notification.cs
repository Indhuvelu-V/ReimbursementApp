

using ReimbursementTrackerApp;
namespace ReimbursementTrackerApp
{

    public class Notification : IComparable<Notification>, IEquatable<Notification>
    {
        public string NotificationId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;     // Recipient
        public string SenderId { get; set; } = string.Empty;   // Who sent it
        public string Message { get; set; } = string.Empty;
        public string? Reply { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ReadStatus { get; set; } = "Unread";
        public string SenderRole { get; set; } = "Manager";
        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }

        public int CompareTo(Notification? other) => other != null ? NotificationId.CompareTo(other.NotificationId) : 1;
        public bool Equals(Notification? other) => other != null && NotificationId == other.NotificationId;
        public override bool Equals(object? obj) => Equals(obj as Notification);
        public override int GetHashCode() => NotificationId.GetHashCode();
        public override string ToString() => $"Notification: {Message}, Read: {ReadStatus}, Reply: {Reply}";
    }
}