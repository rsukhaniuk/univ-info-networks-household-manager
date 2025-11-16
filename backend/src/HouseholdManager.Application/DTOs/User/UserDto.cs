using HouseholdManager.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.User
{
    /// <summary>
    /// Basic user information (Response DTO)
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// User ID from Auth0
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Email address
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// First name
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// Last name
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// Full name (computed)
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Profile picture URL from Auth0
        /// </summary>
        public string? ProfilePictureUrl { get; set; }

        /// <summary>
        /// Account creation date (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// System role (User or SystemAdmin)
        /// </summary>
        public SystemRole Role { get; set; }

        /// <summary>
        /// Currently active household ID
        /// </summary>
        public Guid? CurrentHouseholdId { get; set; }

        /// <summary>
        /// Indicates if user is system admin
        /// </summary>
        public bool IsSystemAdmin { get; set; }
    }
}
