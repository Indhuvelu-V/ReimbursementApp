using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReimbursementTrackerApp.Interfaces;

namespace ReimbursementTrackerApp.Controllers
{
    /// <summary>
    /// Standalone file upload endpoint.
    /// POST /api/FileUpload  → returns list of saved URL paths.
    /// These paths are stored in DocumentUrls on the Expense.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileUploadController : ControllerBase
    {
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<FileUploadController> _logger;

        public FileUploadController(
            IFileUploadService fileUploadService,
            ILogger<FileUploadController> logger)
        {
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        // =====================================================
        // UPLOAD FILES
        // POST /api/FileUpload
        // Accepts: multipart/form-data  key = "files"
        // Returns: { urls: ["/uploads/abc.png", ...] }
        // =====================================================
        [HttpPost]
        [Authorize(Roles = "Employee,Manager,Finance,Admin")]
        public async Task<IActionResult> UploadFiles([FromForm] List<IFormFile> files)
        {
            _logger.LogInformation("File upload request received. File count: {Count}", files?.Count ?? 0);

            if (files == null || files.Count == 0)
                return BadRequest(new { message = "No files were provided." });

            try
            {
                var urls = await _fileUploadService.SaveFilesAsync(files);

                _logger.LogInformation("Files uploaded successfully. URLs: {Urls}", string.Join(", ", urls));

                return Ok(new
                {
                    success = true,
                    message = $"{urls.Count} file(s) uploaded successfully.",
                    urls
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "File validation failed during upload.");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during file upload.");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An unexpected error occurred during file upload.",
                    details = ex.Message
                });
            }
        }
    }
}
