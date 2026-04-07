

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // ✅ add this
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;
using System.Security.Claims;

namespace ReimbursementTrackerApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _service;
        private readonly ILogger<PaymentController> _logger; // ✅ logger field

        public PaymentController(
            IPaymentService service,
            ILogger<PaymentController> logger) // ✅ inject logger
        {
            _service = service;
            _logger = logger;
        }

        // =====================================================
        // 🔹 Complete Payment (Finance Only)
        // =====================================================
        [HttpPost("CompletePayment/{expenseId}")]
        [Authorize(Roles = "Finance")]
        public async Task<IActionResult> CompletePayment(string expenseId, [FromBody] CompletePaymentRequestDto request)
        {
            _logger.LogInformation("Request to complete payment for Expense {ExpenseId}", expenseId);

            try
            {
                var result = await _service.CompletePayment(expenseId, request.ReferenceNo, request.PaymentMode);

                _logger.LogInformation("Payment completed successfully for Expense {ExpenseId}", expenseId);

                return Ok(new { success = true, payment = result });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Expense {ExpenseId} not found while completing payment", expenseId);
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while completing payment for Expense {ExpenseId}", expenseId);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while completing payment for Expense {ExpenseId}", expenseId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unexpected error while completing payment.",
                    details = ex.Message
                });
            }
        }

        // =====================================================
        // 🔹 Get Payment by ExpenseId
        // =====================================================
        [HttpGet("{expenseId}")]
        [Authorize(Roles = "Employee,TeamLead,Finance,Admin,Manager")]
        public async Task<IActionResult> GetPaymentByExpenseId(string expenseId)
        {
            _logger.LogInformation("Request to fetch payment for Expense {ExpenseId}", expenseId);

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
                var role   = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

                var payment = await _service.GetPaymentByExpenseId(expenseId, userId, role);

                if (payment == null)
                {
                    _logger.LogWarning("Payment not found or access denied for Expense {ExpenseId}", expenseId);
                    return NotFound(new { message = "Payment not found or access denied." });
                }

                _logger.LogInformation("Payment fetched successfully for Expense {ExpenseId}", expenseId);

                return Ok(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching payment for Expense {ExpenseId}", expenseId);
                return StatusCode(500, new
                {
                    message = "Failed to fetch payment.",
                    details = ex.Message
                });
            }
        }

        // =====================================================
        // 🔹 Get All Payments (Admin/Finance) with Pagination
        // =====================================================
        [HttpGet]
        [Authorize(Roles = "Finance,Admin")]
        public async Task<IActionResult> GetAllPayments([FromQuery] PaginationParams paginationParams)
        {
            _logger.LogInformation("Request to fetch all payments. Page {Page}, Size {Size}",
                paginationParams.PageNumber, paginationParams.PageSize);

            try
            {
                var payments = await _service.GetAllPayments(paginationParams);

                _logger.LogInformation("Payments fetched successfully");

                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching all payments");
                return StatusCode(500, new
                {
                    message = "Failed to fetch payments.",
                    details = ex.Message
                });
            }
        }
    }
}