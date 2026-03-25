

//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Logging; // ✅ add this
//using ReimbursementTrackerApp.Interfaces;
//using ReimbursementTrackerApp.Models.Common;
//using ReimbursementTrackerApp.Models.DTOs;
//using ReimbursementTrackerApp.Services;
//using System.Security.Claims;

//namespace ReimbursementTrackerApp.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class ExpenseController : ControllerBase
//    {
//        private readonly IExpenseService _service;
//        private readonly ILogger<ExpenseController> _logger; // ✅ logger

//        public ExpenseController(
//            IExpenseService service,
//            ILogger<ExpenseController> logger) // ✅ inject
//        {
//            _service = service;
//            _logger = logger;
//        }

//        // =====================================================
//        // CREATE EXPENSE
//        // =====================================================
//        [HttpPost("Create")]
//        [Authorize(Roles = "Employee,Manager,Finance,Admin")]


//        public async Task<IActionResult> CreateExpense([FromForm] CreateExpenseRequestDto request)
//        {
//            try
//            {
//                var result = await _service.CreateExpense(request);
//                return Ok(new { success = true, expense = result });
//            }
//            catch (InvalidOperationException ex)
//            {
//                // Covers both:
//                //   • "Expense date must be within the current month …"
//                //   • "You have already submitted an expense for … Only one expense is allowed per month."
//                //   • "Amount exceeds limit for …"
//                return BadRequest(new { success = false, message = ex.Message });
//            }
//            catch (KeyNotFoundException ex)
//            {
//                return NotFound(new { success = false, message = ex.Message });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { success = false, message = "An unexpected error occurred.", detail = ex.Message });
//            }
//        }

//        // =====================================================
//        // UPDATE EXPENSE
//        // =====================================================
//        [HttpPut("{expenseId}")]
//        [Authorize(Roles = "Employee,Manager,Finance,Admin")]
//        public async Task<IActionResult> UpdateExpense(string expenseId, [FromForm] CreateExpenseRequestDto dto)
//        {
//            _logger.LogInformation("Request to update Expense {ExpenseId}", expenseId);

//            try
//            {
//                var documentUrls = dto.DocumentUrls ?? new List<string>();

//                if (dto.Documents != null)
//                {
//                    foreach (var file in dto.Documents)
//                    {
//                        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
//                        var path = Path.Combine("wwwroot/uploads", fileName);

//                        var dir = Path.GetDirectoryName(path);
//                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
//                            Directory.CreateDirectory(dir);

//                        using var stream = new FileStream(path, FileMode.Create);
//                        await file.CopyToAsync(stream);

//                        documentUrls.Add("/uploads/" + fileName);
//                    }
//                }

//                dto.DocumentUrls = documentUrls;

//                var result = await _service.UpdateExpenseSafe(expenseId, dto);

//                if (!result.IsSuccess)
//                {
//                    _logger.LogWarning("Failed to update Expense {ExpenseId}: {Message}", expenseId, result.Message);
//                    return BadRequest(new { success = false, message = result.Message });
//                }

//                _logger.LogInformation("Expense {ExpenseId} updated successfully", expenseId);

//                return Ok(new
//                {
//                    success = true,
//                    message = result.Message,
//                    updatedExpense = result.Expense
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error updating Expense {ExpenseId}", expenseId);
//                return StatusCode(500, new { success = false, message = "Unexpected error", details = ex.Message });
//            }
//        }

//        // =====================================================
//        // SUBMIT EXPENSE
//        // =====================================================
//        [HttpPost("Submit/{expenseId}")]
//        [Authorize(Roles = "Employee,Manager,Finance,Admin")]
//        public async Task<IActionResult> SubmitExpense(string expenseId)
//        {
//            _logger.LogInformation("Request to submit Expense {ExpenseId}", expenseId);

//            try
//            {
//                var result = await _service.SubmitExpense(expenseId);

//                _logger.LogInformation("Expense {ExpenseId} submitted successfully", expenseId);

//                return Ok(result);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error submitting Expense {ExpenseId}", expenseId);
//                return StatusCode(500, new { message = "Unexpected error", details = ex.Message });
//            }
//        }

//        // =====================================================
//        // DELETE EXPENSE
//        // =====================================================
//        [HttpDelete("{expenseId}")]
//        [Authorize]
//        public async Task<IActionResult> DeleteExpense(string expenseId)
//        {
//            _logger.LogInformation("Request to delete Expense {ExpenseId}", expenseId);

//            var result = await _service.DeleteExpenseSafe(expenseId);

//            if (!result.IsSuccess)
//            {
//                _logger.LogWarning("Failed to delete Expense {ExpenseId}: {Message}", expenseId, result.Message);

//                return BadRequest(new
//                {
//                    success = false,
//                    message = result.Message
//                });
//            }

//            _logger.LogInformation("Expense {ExpenseId} deleted successfully", expenseId);

//            return Ok(new
//            {
//                success = true,
//                message = result.Message,
//                deletedExpense = result.Expense
//            });
//        }

//        // =====================================================
//        // GET ALL EXPENSES
//        // =====================================================
//        [HttpPost("all")]
//        [Authorize(Roles = "Manager,Finance,Admin")]
//        public async Task<IActionResult> GetAllExpenses([FromQuery] PaginationParams paginationParams)
//        {
//            _logger.LogInformation("Fetching all expenses. Page {Page}, Size {Size}",
//                paginationParams.PageNumber, paginationParams.PageSize);

//            try
//            {
//                var expenses = await _service.GetAllExpenses(paginationParams);

//                _logger.LogInformation("Expenses fetched successfully");

//                return Ok(expenses);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error fetching all expenses");
//                return StatusCode(500, new { message = "Failed to fetch expenses", details = ex.Message });
//            }
//        }

//        // =====================================================
//        // GET MY EXPENSES
//        // =====================================================
//        [HttpGet("userexpenses")]
//        [Authorize]
//        public async Task<IActionResult> GetMyExpenses()
//        {
//            _logger.LogInformation("Fetching current user expenses");

//            var result = await _service.GetMyExpenses();

//            _logger.LogInformation("Fetched {Count} expenses for current user", result?.Count ?? 0);

//            return Ok(result);
//        }
//    }
//}

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
    public class ExpenseController : ControllerBase
    {
        private readonly IExpenseService _service;
        private readonly IFileUploadService _fileUploadService;  // ✅ NEW
        private readonly ILogger<ExpenseController> _logger;

        public ExpenseController(
            IExpenseService service,
            IFileUploadService fileUploadService,             // ✅ NEW
            ILogger<ExpenseController> logger)
        {
            _service = service;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        // =====================================================
        // CREATE EXPENSE
        // POST /api/Expense/Create
        //
        // ✅ File upload is handled here:
        //    - Documents (IFormFile[]) are saved to /uploads
        //    - Saved paths are passed to the service
        //
        // ✅ Rejected-expense reuse:
        //    If a rejected expense exists for this month the
        //    service updates it in-place (no new record created).
        // =====================================================
        [HttpPost("Create")]
        [Authorize(Roles = "Employee,Manager,Finance,Admin")]
        public async Task<IActionResult> CreateExpense([FromForm] CreateExpenseRequestDto request)
        {
            _logger.LogInformation("Request to create expense");

            try
            {
                // ── Save any uploaded files and attach URLs to the request ──
                if (request.Documents != null && request.Documents.Count > 0)
                {
                    var uploadedUrls = await _fileUploadService.SaveFilesAsync(request.Documents);
                    request.DocumentUrls ??= new List<string>();
                    request.DocumentUrls.AddRange(uploadedUrls);
                }

                var result = await _service.CreateExpense(request);

                _logger.LogInformation("Expense created/updated successfully");

                return Ok(new { success = true, expense = result });
            }
            catch (ArgumentException ex)
            {
                // File validation failures (bad type, size exceeded)
                _logger.LogWarning(ex, "File validation failed while creating expense");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating expense");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Category not found while creating expense");
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating expense");
                return StatusCode(500, new { success = false, message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        // =====================================================
        // UPDATE EXPENSE
        // PUT /api/Expense/{expenseId}
        //
        // ✅ Now also accepts Rejected expenses (user edits after rejection)
        // ✅ New files are appended to existing DocumentUrls
        // =====================================================
        [HttpPut("{expenseId}")]
        [Authorize(Roles = "Employee,Manager,Finance,Admin")]
        public async Task<IActionResult> UpdateExpense(string expenseId, [FromForm] CreateExpenseRequestDto dto)
        {
            _logger.LogInformation("Request to update Expense {ExpenseId}", expenseId);

            try
            {
                // ── Save any new files ────────────────────────────────────────
                if (dto.Documents != null && dto.Documents.Count > 0)
                {
                    var uploadedUrls = await _fileUploadService.SaveFilesAsync(dto.Documents);
                    dto.DocumentUrls ??= new List<string>();
                    dto.DocumentUrls.AddRange(uploadedUrls);
                }

                var result = await _service.UpdateExpenseSafe(expenseId, dto);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to update Expense {ExpenseId}: {Message}", expenseId, result.Message);
                    return BadRequest(new { success = false, message = result.Message });
                }

                _logger.LogInformation("Expense {ExpenseId} updated successfully", expenseId);

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    updatedExpense = result.Expense
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "File validation failed while updating expense {ExpenseId}", expenseId);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Expense {ExpenseId}", expenseId);
                return StatusCode(500, new { success = false, message = "Unexpected error", details = ex.Message });
            }
        }

        // =====================================================
        // SUBMIT EXPENSE
        // POST /api/Expense/Submit/{expenseId}
        // Draft → Submitted
        // (unchanged — resubmit uses the dedicated endpoint below)
        // =====================================================
        [HttpPost("Submit/{expenseId}")]
        [Authorize(Roles = "Employee,Manager,Finance,Admin")]
        public async Task<IActionResult> SubmitExpense(string expenseId)
        {
            _logger.LogInformation("Request to submit Expense {ExpenseId}", expenseId);

            try
            {
                var result = await _service.SubmitExpense(expenseId);

                _logger.LogInformation("Expense {ExpenseId} submitted successfully", expenseId);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting Expense {ExpenseId}", expenseId);
                return StatusCode(500, new { message = "Unexpected error", details = ex.Message });
            }
        }

        // =====================================================
        // RESUBMIT EXPENSE  ✅ NEW
        // POST /api/Expense/Resubmit/{expenseId}
        //
        // Changes status from Rejected or Draft → Submitted.
        //
        // Typical flow after rejection:
        //   1. Manager rejects → Status = Rejected
        //   2. Employee edits via PUT /api/Expense/{id} → Status stays Rejected / becomes Draft
        //   3. Employee calls POST /api/Expense/Resubmit/{id} → Status = Submitted
        // =====================================================
        [HttpPost("Resubmit/{expenseId}")]
        [Authorize(Roles = "Employee,Manager,Finance,Admin")]
        public async Task<IActionResult> ResubmitExpense(string expenseId)
        {
            _logger.LogInformation("Request to resubmit Expense {ExpenseId}", expenseId);

            try
            {
                var result = await _service.ResubmitExpense(expenseId);

                _logger.LogInformation("Expense {ExpenseId} resubmitted successfully", expenseId);

                return Ok(new
                {
                    success = true,
                    message = "Expense resubmitted successfully.",
                    expense = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Expense {ExpenseId} not found for resubmit", expenseId);
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized resubmit attempt for Expense {ExpenseId}", expenseId);
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid resubmit for Expense {ExpenseId}", expenseId);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resubmitting Expense {ExpenseId}", expenseId);
                return StatusCode(500, new { success = false, message = "Unexpected error", details = ex.Message });
            }
        }

        // =====================================================
        // DELETE EXPENSE — unchanged
        // =====================================================
        [HttpDelete("{expenseId}")]
        [Authorize]
        public async Task<IActionResult> DeleteExpense(string expenseId)
        {
            _logger.LogInformation("Request to delete Expense {ExpenseId}", expenseId);

            var result = await _service.DeleteExpenseSafe(expenseId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to delete Expense {ExpenseId}: {Message}", expenseId, result.Message);
                return BadRequest(new { success = false, message = result.Message });
            }

            _logger.LogInformation("Expense {ExpenseId} deleted successfully", expenseId);

            return Ok(new
            {
                success = true,
                message = result.Message,
                deletedExpense = result.Expense
            });
        }

        // =====================================================
        // GET ALL EXPENSES — unchanged
        // =====================================================
        [HttpPost("all")]
        [Authorize(Roles = "Manager,Finance,Admin")]
        public async Task<IActionResult> GetAllExpenses([FromQuery] PaginationParams paginationParams)
        {
            _logger.LogInformation("Fetching all expenses. Page {Page}, Size {Size}",
                paginationParams.PageNumber, paginationParams.PageSize);

            try
            {
                var expenses = await _service.GetAllExpenses(paginationParams);

                _logger.LogInformation("Expenses fetched successfully");

                return Ok(expenses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all expenses");
                return StatusCode(500, new { message = "Failed to fetch expenses", details = ex.Message });
            }
        }

        // =====================================================
        // GET MY EXPENSES — unchanged
        // =====================================================
        [HttpGet("userexpenses")]
        [Authorize]
        public async Task<IActionResult> GetMyExpenses()
        {
            _logger.LogInformation("Fetching current user expenses");

            var result = await _service.GetMyExpenses();

            _logger.LogInformation("Fetched {Count} expenses for current user", result?.Count ?? 0);

            return Ok(result);
        }
    }
}
