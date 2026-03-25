//namespace ReimbursementTrackerApp.Models.DTOs
//{
//    public class CreatePaymentResponseDto
//    {
//        public string PaymentId { get; set; } = string.Empty;

//        public string UserId { get; set; } = string.Empty;
//        public string UserName { get; set; } = string.Empty;
//        public decimal AmountPaid { get; set; }
//        public string PaymentStatus { get; set; } = string.Empty;
//        public string PaymentMode { get; set; } = string.Empty;
//        public string ReferenceNo { get; set; } = string.Empty;
//        public DateTime PaymentDate { get; set; } = DateTime.Now;
//        public string? AmountInRupees { get; set; } // For Rupees format
//        public string ExpenseId { get; set; } = string.Empty;

//    }
//}
// FILE: Models/DTOs/CreatePaymentResponseDto.cs
// CHANGE: Added DocumentUrls and ExpenseId so the Payment screen
//         can display the original expense image.
//         No new DTO — existing class extended.

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
        public string? AmountInRupees { get; set; }

        // ✅ ADDED — ExpenseId already needed to look up payment; now surfaced in DTO
        public string ExpenseId { get; set; } = string.Empty;

        // ✅ ADDED — image paths from the original expense.
        // Same /uploads/xyz.jpg paths stored in Expense.DocumentUrls.
        // No re-upload — just carried through from expense → payment response.
        public List<string> DocumentUrls { get; set; } = new();
    }
}
