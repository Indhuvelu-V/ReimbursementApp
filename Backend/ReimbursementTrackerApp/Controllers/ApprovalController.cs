

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // ✅ add this
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;

namespace ReimbursementTrackerApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApprovalController : ControllerBase
    {
        private readonly IApprovalService _approvalService;
        private readonly ILogger<ApprovalController> _logger; // ✅ logger

        public ApprovalController(
            IApprovalService approvalService,
            ILogger<ApprovalController> logger) // ✅ inject
        {
            _approvalService = approvalService;
            _logger = logger;
        }

        // ======================================================
        // 1️⃣ Manager Approval / Rejection
        // ======================================================
        [HttpPost("manager")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ManagerApproval([FromBody] CreateApprovalRequestDto request)
        {
            _logger.LogInformation("Manager approval request received for Expense {ExpenseId}", request.ExpenseId);

            try
            {
                var result = await _approvalService.ManagerApproval(request);

                _logger.LogInformation("Manager approval processed successfully for Expense {ExpenseId}", request.ExpenseId);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Expense {ExpenseId} not found during approval", request.ExpenseId);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation during approval for Expense {ExpenseId}", request.ExpenseId);
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request data for Expense {ExpenseId}", request.ExpenseId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during manager approval for Expense {ExpenseId}", request.ExpenseId);
                return StatusCode(500, new
                {
                    message = "An unexpected error occurred.",
                    details = ex.Message
                });
            }
        }

        // ======================================================
        // 2️⃣ Admin View All Approvals
        // ======================================================
        [HttpGet("all")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetAllApprovals([FromQuery] PaginationParams paginationParams)
        {
            _logger.LogInformation("Request to fetch all approvals. Page {Page}, Size {Size}",
                paginationParams.PageNumber, paginationParams.PageSize);

            try
            {
                var approvals = await _approvalService.GetAllApprovals(paginationParams);

                _logger.LogInformation("Approvals fetched successfully");

                return Ok(approvals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching approvals");
                return StatusCode(500, new
                {
                    message = "An unexpected error occurred.",
                    details = ex.Message
                });
            }
        }
    }
}