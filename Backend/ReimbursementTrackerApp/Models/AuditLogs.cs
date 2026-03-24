
using System;
using System.Collections.Generic;
using System.Text.Json;
using ReimbursementTrackerApp.Models.Enums;

namespace ReimbursementTrackerApp.Models
{
    public class AuditLog : IComparable<AuditLog>, IEquatable<AuditLog>
    {
        public string LogId { get; set; } = string.Empty;
        public string? UserId { get; set; } 
        public string UserName { get; set; } = string.Empty;
        public UserRole Role { get; set; }

        public string Action { get; set; } = string.Empty;

        // Optional: Expense tracking
        public string? ExpenseId { get; set; }
        public decimal? Amount { get; set; }
        public decimal? OldAmount { get; set; }

        public string DocumentUrlsJson { get; set; } = string.Empty;
        public string OldDocumentUrlsJson { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<string> DocumentUrls
        {
            get => string.IsNullOrEmpty(DocumentUrlsJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(DocumentUrlsJson) ?? new List<string>();
            set => DocumentUrlsJson = JsonSerializer.Serialize(value);
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<string> OldDocumentUrls
        {
            get => string.IsNullOrEmpty(OldDocumentUrlsJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(OldDocumentUrlsJson) ?? new List<string>();
            set => OldDocumentUrlsJson = JsonSerializer.Serialize(value);
        }

        public DateTime Date { get; set; } = DateTime.UtcNow;
        public User? User { get; set; }

        public int CompareTo(AuditLog? other) => other != null ? LogId.CompareTo(other.LogId) : 1;
        public bool Equals(AuditLog? other) => other != null && LogId == other.LogId;
        public override bool Equals(object? obj) => Equals(obj as AuditLog);
        public override int GetHashCode() => LogId.GetHashCode();
        public override string ToString() => $"LogId: {LogId}, Action: {Action}, Role: {Role}";
    }
}