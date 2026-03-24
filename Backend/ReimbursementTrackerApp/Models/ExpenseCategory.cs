
using ReimbursementTrackerApp.Models.Enums;
using System;
using System.Collections.Generic;

namespace ReimbursementTrackerApp.Models
{
    public class ExpenseCategory : IComparable<ExpenseCategory>, IEquatable<ExpenseCategory>
    {
        public string CategoryId { get; set; } = string.Empty;
       
        public ExpenseCategoryType CategoryName { get; set; }
        public decimal MaxLimit { get; set; }

        public ICollection<Expense>? Expenses { get; set; }
        public ICollection<Policy>? Policies { get; set; }

        public int CompareTo(ExpenseCategory? other) => other != null ? CategoryId.CompareTo(other.CategoryId) : 1;
        public bool Equals(ExpenseCategory? other) => other != null && CategoryId == other.CategoryId;
        public override bool Equals(object? obj) => Equals(obj as ExpenseCategory);
        public override int GetHashCode() => CategoryId.GetHashCode();
        public override string ToString() => $"Category: {CategoryName}, Max Limit: {MaxLimit}";
    }
}