
using System;
using System.Collections.Generic;

namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CreateExpenseResponseDto
    {
        public string ExpenseId { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string ExpenseDate { get; set; } = string.Empty;// <-- string now
        public string Status { get; set; } = string.Empty;
        public List<string>? DocumentUrls { get; set; }
        public bool CanEdit { get; set; }
        public string? Message { get; set; }
        public string? NotificationMessage { get; set; }
        public string? AmountInRupees { get; set; } // For Rupees format
    }
}