
using ReimbursementTrackerApp.Models.Enums;

using System;

namespace ReimbursementTrackerApp.Models
{
    public class Approval : IComparable<Approval>, IEquatable<Approval>
    {
        public string ApprovalId { get; set; } = string.Empty;
        public string ExpenseId { get; set; } = string.Empty;
        public Expense? Expense { get; set; }

        public string Level { get; set; } = string.Empty; // e.g., "TeamLead", "Manager", "Finance"
        public ApprovalStage Stage { get; set; } = ApprovalStage.Manager;
       
        public string Comments { get; set; } = string.Empty;
        public DateTime? ApprovedAt { get; set; }
       

       public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    
        public string ManagerId { get; set; } = string.Empty;
        public User? Manager { get; set; }

        public int CompareTo(Approval? other) => other != null ? ApprovalId.CompareTo(other.ApprovalId) : 1;
        public bool Equals(Approval? other) => other != null && ApprovalId == other.ApprovalId;
        public override bool Equals(object? obj) => Equals(obj as Approval);
        public override int GetHashCode() => ApprovalId.GetHashCode();
        public override string ToString() => $"ApprovalId: {ApprovalId}, Status: {Status}";
    }
}