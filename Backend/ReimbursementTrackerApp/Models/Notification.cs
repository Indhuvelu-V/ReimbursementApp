

using ReimbursementTrackerApp;
namespace ReimbursementTrackerApp
{

    public class Notification : IComparable<Notification>, IEquatable<Notification>
    {
        public string NotificationId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty; // Employee who receives the notification
        public string Message { get; set; } = string.Empty; // Manager message
        public string? Reply { get; set; } // Employee can reply
        public string Description { get; set; } = string.Empty;
        public string ReadStatus { get; set; } = "Unread"; // "Unread" or "Read"
        public string SenderRole { get; set; } = "Manager"; // "Manager" or "Employee"
        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }

        public int CompareTo(Notification? other) => other != null ? NotificationId.CompareTo(other.NotificationId) : 1;
        public bool Equals(Notification? other) => other != null && NotificationId == other.NotificationId;
        public override bool Equals(object? obj) => Equals(obj as Notification);
        public override int GetHashCode() => NotificationId.GetHashCode();
        public override string ToString() => $"Notification: {Message}, Read: {ReadStatus}, Reply: {Reply}";
    }
}