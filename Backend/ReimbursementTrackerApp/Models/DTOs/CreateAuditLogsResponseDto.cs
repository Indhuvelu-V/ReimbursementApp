
using ReimbursementTrackerApp.Models.Enums;
using System;
using System.Collections.Generic;

namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CreateAuditLogsResponseDto
    {
        public string LogId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ExpenseId { get; set; }
        public decimal? OldAmount { get; set; }
        public decimal? Amount { get; set; }
        public List<string>? DocumentUrls { get; set; }
        public List<string>? OldDocumentUrls { get; set; }

       
        public DateTime Date { get; set; }
    }
}

