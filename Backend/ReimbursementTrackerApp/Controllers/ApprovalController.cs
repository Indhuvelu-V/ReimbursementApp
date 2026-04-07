
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ApprovalController> _logger;

        public ApprovalController(
            IApprovalService approvalService,
            ILogger<ApprovalController> logger)
        {
            _approvalService = approvalService;
            _logger = logger;
        }

        // ======================================================
        // 1️⃣ Team Lead Approval / Rejection (Level1)
        //    Employee/TeamLead expenses in Submitted state.
        //    Approved → moves to Pending (awaiting Manager).
        // ======================================================
        [HttpPost("teamlead")]
        [Authorize(Roles = "TeamLead")]
        public async Task<IActionResult> TeamLeadApproval([FromBody] CreateApprovalRequestDto request)
        {
            _logger.LogInformation("TeamLead approval request for Expense {ExpenseId}", request.ExpenseId);
            try
            {
                var result = await _approvalService.TeamLeadApproval(request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during TeamLead approval for Expense {ExpenseId}", request.ExpenseId);
                return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
            }
        }

        // ======================================================
        // 2️⃣ Manager Approval / Rejection (Level2)
        //    Pending (post-TeamLead) or Submitted (Manager's own).
        //    Approved → ExpenseStatus.Approved (ready for Finance).
        // ======================================================
        [HttpPost("manager")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ManagerApproval([FromBody] CreateApprovalRequestDto request)
        {
            _logger.LogInformation("Manager approval request for Expense {ExpenseId}", request.ExpenseId);
            try
            {
                var result = await _approvalService.ManagerApproval(request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Manager approval for Expense {ExpenseId}", request.ExpenseId);
                return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
            }
        }

        // ======================================================
<<<<<<< HEAD
        // 2️⃣ Admin Approval / Rejection (for Manager/Finance expenses)
        // ======================================================
        [HttpPost("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminApproval([FromBody] CreateApprovalRequestDto request)
        {
            _logger.LogInformation("Admin approval request for Expense {ExpenseId}", request.ExpenseId);
            try
            {
                var result = await _approvalService.AdminApproval(request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ======================================================
        // 3️⃣ Admin View All Approvals
=======
        // 3️⃣ Get expenses awaiting TeamLead review
        //    Returns Submitted expenses from Employee/TeamLead roles
        // ======================================================
        [HttpGet("pending/teamlead")]
        [Authorize(Roles = "TeamLead,Admin")]
        public async Task<IActionResult> GetPendingTeamLeadExpenses()
        {
            try
            {
                var result = await _approvalService.GetExpensesPendingTeamLeadApproval();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending TeamLead expenses");
                return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
            }
        }

        // ======================================================
        // 4️⃣ Get expenses awaiting Manager review
        //    Returns Pending (post-TeamLead) + Manager's own Submitted
        // ======================================================
        [HttpGet("pending/manager")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> GetPendingManagerExpenses()
        {
            try
            {
                var result = await _approvalService.GetExpensesPendingManagerApproval();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending Manager expenses");
                return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
            }
        }

        // ======================================================
        // 5️⃣ Admin View All Approvals
>>>>>>> eba5464 (Feature added)
        // ======================================================
        [HttpGet("all")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetAllApprovals([FromQuery] PaginationParams paginationParams)
        {
            try
            {
                var approvals = await _approvalService.GetAllApprovals(paginationParams);
                return Ok(approvals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching approvals");
                return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
            }
        }
    }
}
