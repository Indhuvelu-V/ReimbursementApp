namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CompletePaymentRequestDto
    {
        // Request DTO for completing payment
      
            public string ExpenseId { get; set; } = string.Empty;
            public string ReferenceNo { get; set; } = string.Empty;
            public string PaymentMode { get; set; } = string.Empty;
        
    }
}
