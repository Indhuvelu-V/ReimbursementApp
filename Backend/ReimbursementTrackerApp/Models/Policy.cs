
using System;

namespace ReimbursementTrackerApp.Models
{
    public class Policy : IComparable<Policy>, IEquatable<Policy>
    {
        public string PolicyId { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
   
        public string Description { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;


        public ExpenseCategory? Category { get; set; }

        public int CompareTo(Policy? other) => other != null ? PolicyId.CompareTo(other.PolicyId) : 1;
        public bool Equals(Policy? other) => other != null && PolicyId == other.PolicyId;
        public override bool Equals(object? obj) => Equals(obj as Policy);
        public override int GetHashCode() => PolicyId.GetHashCode();
       
        public override string ToString() => $"PolicyId: {PolicyId}";
    }
}