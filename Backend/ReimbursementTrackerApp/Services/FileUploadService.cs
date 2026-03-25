using ReimbursementTrackerApp.Interfaces;

namespace ReimbursementTrackerApp.Services
{
    /// <summary>
    /// Handles file upload validation and storage.
    /// Files are saved to /uploads inside wwwroot.
    /// Paths are returned as "/uploads/{filename}" for DB storage.
    /// Files are then accessible via: http://localhost:5138/uploads/{filename}
    /// </summary>
    public class FileUploadService : IFileUploadService
    {
        // ── Configuration ───────────────────────────────────────────────────
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg",    // images
            ".pdf",                     // pdf
            ".xlsx", ".xls",            // excel
            ".doc", ".docx"             // word
        };

        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/png", "image/jpg", "image/jpeg",
            "application/pdf",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB per file

        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FileUploadService> _logger;

        public FileUploadService(IWebHostEnvironment env, ILogger<FileUploadService> logger)
        {
            _env = env;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // SAVE SINGLE FILE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<string> SaveFileAsync(IFormFile file)
        {
            ValidateFile(file);

            var uploadFolder = GetUploadFolder();
            var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
            var fullPath = Path.Combine(uploadFolder, uniqueName);

            _logger.LogInformation("Saving file {OriginalName} as {UniqueName}", file.FileName, uniqueName);

            await using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(stream);

            _logger.LogInformation("File saved successfully: {UniqueName}", uniqueName);

            // Return relative URL that the frontend uses to access the file
            return $"/uploads/{uniqueName}";
        }

        // ─────────────────────────────────────────────────────────────────────
        // SAVE MULTIPLE FILES
        // ─────────────────────────────────────────────────────────────────────
        public async Task<List<string>> SaveFilesAsync(IList<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return new List<string>();

            var urls = new List<string>();

            foreach (var file in files)
            {
                var url = await SaveFileAsync(file);
                urls.Add(url);
            }

            return urls;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the absolute path to wwwroot/uploads, creating it if needed.
        /// </summary>
        private string GetUploadFolder()
        {
            // WebRootPath = wwwroot  — this is where UseStaticFiles serves from
            var wwwRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadFolder = Path.Combine(wwwRoot, "uploads");

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
                _logger.LogInformation("Created uploads directory at {Path}", uploadFolder);
            }

            return uploadFolder;
        }

        /// <summary>
        /// Validates file size, extension, and MIME type.
        /// Throws ArgumentException with a clear message on failure.
        /// </summary>
        private void ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null.");

            if (file.Length > MaxFileSizeBytes)
                throw new ArgumentException(
                    $"File '{file.FileName}' exceeds the maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)} MB.");

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
                throw new ArgumentException(
                    $"File type '{extension}' is not allowed. " +
                    $"Allowed types: {string.Join(", ", AllowedExtensions)}.");

            if (!AllowedMimeTypes.Contains(file.ContentType))
                throw new ArgumentException(
                    $"MIME type '{file.ContentType}' is not allowed for file '{file.FileName}'.");
        }
    }
}
