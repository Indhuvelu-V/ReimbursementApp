

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // ✅ add this
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;
using System;
using System.Threading.Tasks;

namespace ReimbursementTrackerApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AuditLogsController> _logger; // ✅ logger

        public AuditLogsController(
            IAuditLogService auditLogService,
            ILogger<AuditLogsController> logger) // ✅ inject
        {
            _auditLogService = auditLogService;
            _logger = logger;
        }

        // =====================================================
        // CREATE AUDIT LOG
        // =====================================================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateLog(CreateAuditLogsRequestDto request)
        {
            _logger.LogInformation("Request to create audit log for Expense {ExpenseId}", request.ExpenseId);

            try
            {
                var result = await _auditLogService.CreateLog(request);

                _logger.LogInformation("Audit log created successfully for Expense {ExpenseId}", request.ExpenseId);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt to create audit log for Expense {ExpenseId}", request.ExpenseId);
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating audit log for Expense {ExpenseId}", request.ExpenseId);
                return StatusCode(500, new
                {
                    message = "Failed to create audit log.",
                    details = ex.Message
                });
            }
        }

        // =====================================================
        // GET PAGED LOGS
        // =====================================================
        [HttpGet("paged")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPagedLogs([FromQuery] PaginationParams paginationParams)
        {
            _logger.LogInformation("Request to fetch audit logs. Page {Page}, Size {Size}",
                paginationParams.PageNumber, paginationParams.PageSize);

            try
            {
                var result = await _auditLogService.GetAllLogs(paginationParams);

                _logger.LogInformation("Audit logs fetched successfully");

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt to fetch audit logs");
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching audit logs");
                return StatusCode(500, new
                {
                    message = "Failed to fetch audit logs.",
                    details = ex.Message
                });
            }
        }

        // =====================================================
        // DELETE LOG
        // =====================================================
        [HttpDelete("{logId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteLog(string logId)
        {
            _logger.LogInformation("Request to delete audit log {LogId}", logId);

            try
            {
                var result = await _auditLogService.DeleteLog(logId);

                if (!result)
                {
                    _logger.LogWarning("Audit log {LogId} not found", logId);
                    return NotFound(new { message = "Audit log not found." });
                }

                _logger.LogInformation("Audit log {LogId} deleted successfully", logId);

                return Ok(new
                {
                    message = "Audit log deleted successfully",
                    success = true
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt to delete audit log {LogId}", logId);
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting audit log {LogId}", logId);
                return StatusCode(500, new
                {
                    message = "Error deleting audit log.",
                    details = ex.Message
                });
            }
        }
    }
}