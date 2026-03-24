

using ReimbursementTrackerApp;
using ReimbursementTrackerApp.Models;
using ReimbursementTrackerApp.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

public class Expense
{
    public string ExpenseId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }

    // ---------------------------
    // Document URLs stored as JSON in DB
    // ---------------------------
    public string DocumentUrlsJson { get; set; } = string.Empty;

    [NotMapped]
    public List<string> DocumentUrls
    {
        get => string.IsNullOrEmpty(DocumentUrlsJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(DocumentUrlsJson) ?? new List<string>();
        set => DocumentUrlsJson = JsonSerializer.Serialize(value);
    }

    public ExpenseStatus Status { get; set; } = ExpenseStatus.Draft;

    public User? User { get; set; }
    public ExpenseCategory? Category { get; set; }
    public ICollection<Approval>? Approvals { get; set; }
    public ICollection<Payment>? Payments { get; set; }
}
