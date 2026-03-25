namespace ReimbursementTrackerApp.Interfaces
{
    public interface IFileUploadService
    {
        /// <summary>
        /// Validates and saves a single uploaded file to /uploads.
        /// Returns the relative URL path, e.g. "/uploads/abc123.png"
        /// </summary>
        Task<string> SaveFileAsync(IFormFile file);

        /// <summary>
        /// Saves multiple uploaded files. Returns list of relative URL paths.
        /// </summary>
        Task<List<string>> SaveFilesAsync(IList<IFormFile> files);
    }
}
