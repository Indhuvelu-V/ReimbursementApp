
using ReimbursementTrackerApp.Models.Enums;
using System;

namespace ReimbursementTrackerApp.Models
{
   

    public class Payment : IComparable<Payment>, IEquatable<Payment>
    {
        public string PaymentId { get; set; } = string.Empty;
        public string ExpenseId { get; set; } = string.Empty;
        public Expense? Expense { get; set; }

        public string? UserId { get; set; } // Recipient of payment
        public User? User { get; set; }

        public decimal AmountPaid { get; set; }
        public PaymentStatusEnum PaymentStatus { get; set; } = PaymentStatusEnum.Pending;
        public DateTime? PaymentDate { get; set; }
        public string ReferenceNo { get; set; } = string.Empty;
        public string? PaymentMode { get; set; } // e.g., "BankTransfer", "Cheque"

        public int CompareTo(Payment? other) => other != null ? PaymentId.CompareTo(other.PaymentId) : 1;
        public bool Equals(Payment? other) => other != null && PaymentId == other.PaymentId;
        public override bool Equals(object? obj) => Equals(obj as Payment);
        public override int GetHashCode() => PaymentId.GetHashCode();
        public override string ToString() => $"PaymentId: {PaymentId}, Status: {PaymentStatus}, Amount: {AmountPaid}";
    }
}