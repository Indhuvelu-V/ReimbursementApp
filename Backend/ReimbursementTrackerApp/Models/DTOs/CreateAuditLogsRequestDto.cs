
using ReimbursementTrackerApp.Models.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CreateAuditLogsRequestDto
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public UserRole? Role { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty; // e.g., "Created Expense"

        public string? ExpenseId { get; set; } // Optional
        public decimal? Amount { get; set; } // Optional
        public decimal? OldAmount { get; set; }

        public List<string>? DocumentUrls { get; set; }
        public List<string>? OldDocumentUrls { get; set; }


        public DateTime Date { get; set; } = DateTime.UtcNow;

        public decimal MaxLimit { get; set; }
        public decimal OldMaxLimit { get; set; }
        public string CategoryId { get; set; }=string.Empty;

        public string Description { get; set; } = string.Empty;
        public string? AmountInRupees { get; set; } // For Rupees format
    }
}