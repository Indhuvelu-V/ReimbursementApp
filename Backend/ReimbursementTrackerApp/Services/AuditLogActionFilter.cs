

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

                // Build description
                var actionDescription = $"{action} executed on {controller}";
                if (!string.IsNullOrEmpty(entityId))
                    actionDescription += $" - Id: {entityId}";

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