using HouseholdManager.Application.Interfaces.Services;

namespace HouseholdManager.Api.Services
{
    /// <summary>
    /// Implementation of file system service using ASP.NET Core IWebHostEnvironment
    /// </summary>
    public class FileSystemService : IFileSystemService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly string _webRootPath;

        public FileSystemService(IWebHostEnvironment environment)
        {
            _environment = environment;
            
            // Ensure WebRootPath is set (can be null in API projects)
            _webRootPath = _environment.WebRootPath 
                ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            
            // Create wwwroot if it doesn't exist
            if (!Directory.Exists(_webRootPath))
            {
                Directory.CreateDirectory(_webRootPath);
            }
        }

        public string GetWebRootPath()
        {
            return _webRootPath;
        }

        public string CombinePath(params string[] paths)
        {
            if (paths == null || paths.Length == 0)
                throw new ArgumentNullException(nameof(paths), "At least one path must be provided");
            
            return Path.Combine(paths);
        }

        public bool FileExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
                
            return File.Exists(path);
        }

        public bool DirectoryExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
                
            return Directory.Exists(path);
        }

        public void CreateDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or whitespace", nameof(path));
                
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public void DeleteFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
                
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
