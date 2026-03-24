namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CreatePaymentResponseDto
    {
        public string PaymentId { get; set; } = string.Empty;
        
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMode { get; set; } = string.Empty;
        public string ReferenceNo { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string? AmountInRupees { get; set; } // For Rupees format
        public string ExpenseId { get; set; } = string.Empty;

    }
}
