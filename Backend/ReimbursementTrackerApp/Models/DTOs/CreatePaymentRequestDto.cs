using System.ComponentModel.DataAnnotations;

namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CreatePaymentRequestDto
    {
        [Required] public string ExpenseId { get; set; } = string.Empty;
        [Required] public string UserId { get; set; } = string.Empty;
        [Required] public string CategoryName { get; set; } = string.Empty;
        [Required][Range(0.01, double.MaxValue)] public decimal AmountPaid { get; set; }
        [Required] public string PaymentMode { get; set; } = "BankTransfer"; // BankTransfer/Cash
        public string ReferenceNo { get; set; } = string.Empty;
    }
       
}
