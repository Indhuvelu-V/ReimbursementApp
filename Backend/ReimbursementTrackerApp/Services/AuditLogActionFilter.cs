

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models.DTOs;
using ReimbursementTrackerApp.Models.Enums;
using System.Security.Claims;

namespace ReimbursementTrackerApp.Filters
{
    public class AuditLogActionFilter : IAsyncActionFilter
    {
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AuditLogActionFilter> _logger; // Added ILogger

        public AuditLogActionFilter(IAuditLogService auditLogService, ILogger<AuditLogActionFilter> logger)
        {
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            try
            {
                // Execute the action first
                var resultContext = await next();

                var userClaims = context.HttpContext.User;

                var userId = userClaims.FindFirstValue(ClaimTypes.NameIdentifier);
                var roleClaim = userClaims.FindFirstValue(ClaimTypes.Role);

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogDebug("Audit skipped: unauthenticated user.");
                    return; // Skip if not authenticated
                }

                // Controller & Action Name
                var controller = context.ActionDescriptor.RouteValues["controller"];
                var action = context.ActionDescriptor.RouteValues["action"];

                // Optional: capture entity ID (like ExpenseId)
                string entityId = string.Empty;
                if (context.ActionArguments.ContainsKey("expenseId"))
                    entityId = context.ActionArguments["expenseId"]?.ToString() ?? "";

                // Build a human-readable action string based on HTTP method + controller/action
                var httpMethod = context.HttpContext.Request.Method.ToUpper();

                // Map to clean readable action names for audit log filtering
                var actionKey = action?.ToLower() ?? "";
                string actionDescription;

                if (actionKey.Contains("resubmit"))      actionDescription = $"Resubmitted Expense (Id: {entityId})";
                else if (actionKey.Contains("submit"))   actionDescription = $"Submitted Expense (Id: {entityId})";
                else if (actionKey.Contains("complet"))  return; // PaymentService logs this directly
                else if (actionKey.Contains("manager"))  actionDescription = $"Manager Approval on Expense (Id: {entityId})";
                else if (actionKey.Contains("approv"))   actionDescription = $"Manager Approval on Expense (Id: {entityId})";
                else if (httpMethod == "DELETE" || actionKey.Contains("delet")) actionDescription = $"Deleted {controller} (Id: {entityId})";
                else if (httpMethod == "PUT"    || actionKey.Contains("updat")) actionDescription = $"Updated {controller} (Id: {entityId})";
                else if (httpMethod == "POST"   || actionKey.Contains("creat")) actionDescription = $"Created {controller} (Id: {entityId})";
                else if (httpMethod == "GET") return; // skip pure reads — no audit log needed
                else actionDescription = $"{httpMethod} {action} on {controller}" + (string.IsNullOrEmpty(entityId) ? "" : $" (Id: {entityId})");

                // Parse Role safely
                UserRole parsedRole = UserRole.Employee; // default
                if (!string.IsNullOrEmpty(roleClaim))
                    Enum.TryParse(roleClaim, true, out parsedRole);

                // Create audit log
                await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
                {
                    UserId = userId,
                    UserName = "", // Optional: fetch actual user name if needed
                    Role = parsedRole,
                    Action = actionDescription,
                    Date = DateTime.UtcNow
                });

                _logger.LogInformation("Audit log created for user {UserId}, action: {Action}", userId, actionDescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create audit log in AuditLogActionFilter.");
            }
        }
    }
}