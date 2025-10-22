using HouseholdManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HouseholdManager.Application.Services
{
    /// <summary>
    /// Implementation of file upload service using IFileSystemService abstraction
    /// </summary>
    public class FileUploadService : IFileUploadService
    {
        private readonly IFileSystemService _fileSystem;
        private readonly ILogger<FileUploadService> _logger;

        // Configuration constants
        private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
        private const string RoomsFolder = "uploads/rooms";
        private const string ExecutionsFolder = "uploads/executions";
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };

        public FileUploadService(IFileSystemService fileSystem, ILogger<FileUploadService> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;

            // Ensure upload directories exist
            EnsureDirectoriesExist();
        }

        public async Task<string> UploadRoomPhotoAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (!await ValidateFileAsync(file))
                throw new InvalidOperationException("Invalid file");

            return await UploadFileAsync(file, RoomsFolder, cancellationToken);
        }

        public async Task<string> UploadExecutionPhotoAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (!await ValidateFileAsync(file))
                throw new InvalidOperationException("Invalid file");

            return await UploadFileAsync(file, ExecutionsFolder, cancellationToken);
        }

        public async Task DeleteFileAsync(string? filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            try
            {
                var fullPath = GetFullPath(filePath);
                if (_fileSystem.FileExists(fullPath))
                {
                    await Task.Run(() => _fileSystem.DeleteFile(fullPath), cancellationToken);
                    _logger.LogInformation("Deleted file: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete file: {FilePath}", filePath);
                // Don't throw - file deletion is not critical
            }
        }

        public async Task<bool> ValidateFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            // Check file size
            if (file.Length > MaxFileSizeBytes)
            {
                _logger.LogWarning("File too large: {Size} bytes", file.Length);
                return false;
            }

            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                _logger.LogWarning("Invalid file extension: {Extension}", extension);
                return false;
            }

            // Check MIME type
            if (!AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                _logger.LogWarning("Invalid MIME type: {MimeType}", file.ContentType);
                return false;
            }

            // Basic security check - read first few bytes to verify it's actually an image
            try
            {
                using var stream = file.OpenReadStream();
                var buffer = new byte[8];
                await stream.ReadAsync(buffer, 0, buffer.Length);

                // Check for common image file signatures
                if (!IsValidImageSignature(buffer))
                {
                    _logger.LogWarning("Invalid image signature for file: {FileName}", file.FileName);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate file signature for: {FileName}", file.FileName);
                return false;
            }

            return true;
        }

        public string GetFullPath(string relativePath)
        {
            return _fileSystem.CombinePath(
                _fileSystem.GetWebRootPath(),
                relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        public bool FileExists(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return false;

            var fullPath = GetFullPath(relativePath);
            return _fileSystem.FileExists(fullPath);
        }

        private async Task<string> UploadFileAsync(IFormFile file, string folder, CancellationToken cancellationToken)
        {
            // Generate unique filename
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{extension}";
            var relativePath = $"{folder}/{fileName}";
            var fullPath = GetFullPath(relativePath);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !_fileSystem.DirectoryExists(directory))
            {
                _fileSystem.CreateDirectory(directory);
            }

            // Save file
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            _logger.LogInformation("Uploaded file: {RelativePath}", relativePath);
            return relativePath;
        }

        private void EnsureDirectoriesExist()
        {
            var roomsPath = _fileSystem.CombinePath(_fileSystem.GetWebRootPath(), RoomsFolder);
            var executionsPath = _fileSystem.CombinePath(_fileSystem.GetWebRootPath(), ExecutionsFolder);

            _fileSystem.CreateDirectory(roomsPath);
            _fileSystem.CreateDirectory(executionsPath);
        }

        private static bool IsValidImageSignature(byte[] buffer)
        {
            // Check for common image file signatures
            // JPEG: FF D8 FF
            if (buffer.Length >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
                return true;

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (buffer.Length >= 8 && buffer[0] == 0x89 && buffer[1] == 0x50 &&
                buffer[2] == 0x4E && buffer[3] == 0x47 && buffer[4] == 0x0D &&
                buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A)
                return true;

            // GIF: 47 49 46 38 (GIF8)
            if (buffer.Length >= 4 && buffer[0] == 0x47 && buffer[1] == 0x49 &&
                buffer[2] == 0x46 && buffer[3] == 0x38)
                return true;

            // WebP: starts with RIFF, then WEBP at offset 8
            if (buffer.Length >= 4 && buffer[0] == 0x52 && buffer[1] == 0x49 &&
                buffer[2] == 0x46 && buffer[3] == 0x46)
                return true; // Basic RIFF check, could be more specific

            return false;
        }
    }
}
