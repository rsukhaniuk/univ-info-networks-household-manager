namespace HouseholdManager.Services.Interfaces
{
    /// <summary>
    /// Service interface for file upload operations
    /// Handles room photos and task execution photos
    /// </summary>
    public interface IFileUploadService
    {
        /// <summary>
        /// Upload room photo and return relative path
        /// </summary>
        Task<string> UploadRoomPhotoAsync(IFormFile file, CancellationToken cancellationToken = default);

        /// <summary>
        /// Upload task execution photo and return relative path
        /// </summary>
        Task<string> UploadExecutionPhotoAsync(IFormFile file, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete file if it exists
        /// </summary>
        Task DeleteFileAsync(string? filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validate uploaded file (size, type, security)
        /// </summary>
        Task<bool> ValidateFileAsync(IFormFile file);

        /// <summary>
        /// Get full file path from relative path
        /// </summary>
        string GetFullPath(string relativePath);

        /// <summary>
        /// Check if file exists
        /// </summary>
        bool FileExists(string relativePath);
    }
}
