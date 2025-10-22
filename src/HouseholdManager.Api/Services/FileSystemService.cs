using HouseholdManager.Application.Interfaces.Services;

namespace HouseholdManager.Api.Services
{
    /// <summary>
    /// Implementation of file system service using ASP.NET Core IWebHostEnvironment
    /// </summary>
    public class FileSystemService : IFileSystemService
    {
        private readonly IWebHostEnvironment _environment;

        public FileSystemService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public string GetWebRootPath()
        {
            return _environment.WebRootPath;
        }

        public string CombinePath(params string[] paths)
        {
            return Path.Combine(paths);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
