using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;
using System.Security.Claims;

namespace ReimbursementTrackerApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApprovalController : ControllerBase
    {
        private readonly IApprovalService _approvalService;
        private readonly ILogger<ApprovalController> _logger;

        public ApprovalController(IApprovalService approvalService, ILogger<ApprovalController> logger)
        {
            _approvalService = approvalService;
            _logger = logger;
        }

        // ── Team Lead Approval ────────────────────────────────────────────────
        // POST /api/Approval/teamlead
        // Moves expense: PendingTeamLead → PendingManager (approved) or Rejected
        [HttpPost("teamlead")]
        [Authorize(Roles = "TeamLead")]
        public async Task<IActionResult> TeamLeadApproval([FromBody] CreateApprovalRequestDto request)
        {
            _logger.LogInformation("TeamLead approval for Expense {Id}", request.ExpenseId);
            return await HandleApproval(() => _approvalService.TeamLeadApproval(request));
        }

        // ── Manager Approval ──────────────────────────────────────────────────
        // POST /api/Approval/manager
        // Moves expense: PendingManager → PendingFinance (approved) or Rejected
        [HttpPost("manager")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ManagerApproval([FromBody] CreateApprovalRequestDto request)
        {
            _logger.LogInformation("Manager approval for Expense {Id}", request.ExpenseId);
            return await HandleApproval(() => _approvalService.ManagerApproval(request));
        }

        // ── Finance Approval ──────────────────────────────────────────────────
        // POST /api/Approval/finance
        // Moves expense: PendingFinance → Approved (approved) or Rejected
        [HttpPost("finance")]
        [Authorize(Roles = "Finance")]
        public async Task<IActionResult> FinanceApproval([FromBody] CreateApprovalRequestDto request)
        {
            _logger.LogInformation("Finance approval for Expense {Id}", request.ExpenseId);
            return await HandleApproval(() => _approvalService.FinanceApproval(request));
        }

        // ── Admin Approval (Manager/Finance expenses) ─────────────────────────
        // POST /api/Approval/admin
        [HttpPost("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminApproval([FromBody] CreateApprovalRequestDto request)
        {
            _logger.LogInformation("Admin approval for Expense {Id}", request.ExpenseId);
            return await HandleApproval(() => _approvalService.AdminApproval(request));
        }

        // ── Get All Approvals (paginated) ─────────────────────────────────────
        // GET /api/Approval/all
        [HttpGet("all")]
        [Authorize(Roles = "Admin,Manager,Finance,TeamLead")]
        public async Task<IActionResult> GetAllApprovals([FromQuery] PaginationParams paginationParams)
        {
            try
            {
                var result = await _approvalService.GetAllApprovals(paginationParams);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching approvals");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ── Get My Approval History ───────────────────────────────────────────
        // GET /api/Approval/my-history
        // Returns approvals where the caller is the approver (ManagerId = callerId)
        [HttpGet("my-history")]
        [Authorize(Roles = "TeamLead,Manager,Finance,Admin")]
        public async Task<IActionResult> GetMyApprovalHistory()
        {
            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(callerId))
                return Unauthorized();

            try
            {
                var result = await _approvalService.GetMyApprovalHistory(callerId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching approval history");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ── Get Pending Approvals for Current Approver ────────────────────────
        // GET /api/Approval/pending
        // Returns expenses waiting for the calling user's approval stage
        [HttpGet("pending")]
        [Authorize(Roles = "TeamLead,Manager,Finance,Admin")]
        public async Task<IActionResult> GetPendingForMe()
        {
            var approverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(approverId))
                return Unauthorized();

            try
            {
                var result = await _approvalService.GetPendingApprovalsForMe(approverId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending approvals");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ── Shared error-handling wrapper ─────────────────────────────────────
        private async Task<IActionResult> HandleApproval(Func<Task<CreateApprovalResponseDto?>> action)
        {
            try
            {
                var result = await action();
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during approval");
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
