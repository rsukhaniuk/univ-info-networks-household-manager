using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.Interfaces.Services
{
    /// <summary>
    /// Abstraction for file system operations
    /// Decouples Application layer from ASP.NET Core IWebHostEnvironment
    /// </summary>
    public interface IFileSystemService
    {
        /// <summary>
        /// Get the web root path (wwwroot)
        /// </summary>
        string GetWebRootPath();

        /// <summary>
        /// Combine multiple path segments into a single path
        /// </summary>
        string CombinePath(params string[] paths);

        /// <summary>
        /// Check if a file exists at the specified path
        /// </summary>
        bool FileExists(string path);

        /// <summary>
        /// Check if a directory exists at the specified path
        /// </summary>
        bool DirectoryExists(string path);

        /// <summary>
        /// Create a directory if it doesn't exist
        /// </summary>
        void CreateDirectory(string path);

        /// <summary>
        /// Delete a file if it exists
        /// </summary>
        void DeleteFile(string path);
    }
}
