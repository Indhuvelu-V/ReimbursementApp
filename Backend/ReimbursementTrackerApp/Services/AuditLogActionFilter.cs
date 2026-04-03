

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
                var resultContext = await next();

                var userClaims = context.HttpContext.User;
                var userId    = userClaims.FindFirstValue(ClaimTypes.NameIdentifier);
                var userName  = userClaims.FindFirstValue(ClaimTypes.Name) ?? "";
                var roleClaim = userClaims.FindFirstValue(ClaimTypes.Role);

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogDebug("Audit skipped: unauthenticated user.");
                    return;
                }

                var controller = context.ActionDescriptor.RouteValues["controller"] ?? "";
                var action     = context.ActionDescriptor.RouteValues["action"]     ?? "";
                var httpMethod = context.HttpContext.Request.Method.ToUpper();
                var actionKey  = action.ToLower();

                // Step 1: try route params first
                string entityId = string.Empty;
                if (context.ActionArguments.ContainsKey("expenseId"))
                    entityId = context.ActionArguments["expenseId"]?.ToString() ?? "";
                else if (context.ActionArguments.ContainsKey("logId"))
                    entityId = context.ActionArguments["logId"]?.ToString() ?? "";
                else if (context.ActionArguments.ContainsKey("notificationId"))
                    entityId = context.ActionArguments["notificationId"]?.ToString() ?? "";
                else
                {
                    // Step 2: try to find ExpenseId inside a request body DTO
                    // e.g. CreateApprovalRequestDto has ExpenseId as a property
                    foreach (var arg in context.ActionArguments.Values)
                    {
                        if (arg == null) continue;
                        var prop = arg.GetType().GetProperty("ExpenseId");
                        if (prop != null)
                        {
                            entityId = prop.GetValue(arg)?.ToString() ?? "";
                            break;
                        }
                    }
                }

                // Actions already logged by services directly — skip to avoid duplicates
                // These are EXACT action method names (lowercased) from the controllers
                var serviceHandled = new HashSet<string>
                {
                    "createexpense",
                    "updateexpense",
                    "deleteexpense",
                    "resubmitexpense",
                    "managerapproval",
                    "completepayment"
                };

                // Exact match first, then contains fallback
                if (serviceHandled.Contains(actionKey)) return;
                if (serviceHandled.Any(s => actionKey.Contains(s))) return;
                if (httpMethod == "GET") return; // skip pure reads

                string idPart = string.IsNullOrEmpty(entityId) ? "" : $" (Id: {entityId})";

                string actionDescription;

                if (actionKey.Contains("resubmit"))
                    actionDescription = $"Resubmitted Expense{idPart}";
                else if (actionKey.Contains("submit"))
                    actionDescription = $"Submitted Expense{idPart}";
                else if (actionKey.Contains("register"))
                    actionDescription = $"Registered User";
                else if (actionKey.Contains("login"))
                    actionDescription = $"User Login";
                else if (actionKey.Contains("reply"))
                    actionDescription = $"Replied to Notification{idPart}";
                else if (actionKey.Contains("markread") || actionKey.Contains("mark") || actionKey.Contains("read"))
                    actionDescription = $"Marked Notification as Read{idPart}";
                else if (httpMethod == "DELETE" || actionKey.Contains("delet"))
                    actionDescription = $"Deleted {controller}{idPart}";
                else if (httpMethod == "PUT" || actionKey.Contains("updat"))
                    actionDescription = $"Updated {controller}{idPart}";
                else if (httpMethod == "POST" || actionKey.Contains("creat"))
                    actionDescription = $"Created {controller}{idPart}";
                else
                    actionDescription = $"{action} on {controller}{idPart}";

                UserRole parsedRole = UserRole.Employee;
                if (!string.IsNullOrEmpty(roleClaim))
                    Enum.TryParse(roleClaim, true, out parsedRole);

                await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
                {
                    UserId   = userId,
                    UserName = userName,
                    Role     = parsedRole,
                    Action   = actionDescription,
                    Date     = DateTime.UtcNow
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