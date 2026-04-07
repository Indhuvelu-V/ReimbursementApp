

using ReimbursementTrackerApp.Models;
using ReimbursementTrackerApp.Models.Enums;
using System;
using System.Collections.Generic;

namespace ReimbursementTrackerApp
{
    public class User : IComparable<User>, IEquatable<User>
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public DepartmentType? Department { get; set; }
        public string Phone { get; set; } = string.Empty;
        public byte[] Password { get; set; } = Array.Empty<byte>();
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
        public UserStatus Status { get; set; } = UserStatus.Active;
        public ApprovalLevel? ApprovalLevel { get; set; }

        // Bank Details
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string IfscCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;

        // Reporting Manager (same department)
        public string? ManagerId { get; set; }
        public User? Manager { get; set; }

        // Relationships
        public ICollection<Expense>? Expenses { get; set; }
        public ICollection<Approval>? Approvals { get; set; }
        public ICollection<Notification>? Notifications { get; set; }
        public ICollection<AuditLog>? AuditLogs { get; set; }
        public ICollection<Payment>? Payments { get; set; }

        public int CompareTo(User? other) => other != null ? UserId.CompareTo(other.UserId) : 1;
        public bool Equals(User? other) => other != null && UserId == other.UserId;
        public override bool Equals(object? obj) => Equals(obj as User);
        public override int GetHashCode() => UserId.GetHashCode();
        public override string ToString() => $"Id: {UserId}, Name: {UserName}, Role: {Role}, ApprovalLevel: {ApprovalLevel}";

    }
}