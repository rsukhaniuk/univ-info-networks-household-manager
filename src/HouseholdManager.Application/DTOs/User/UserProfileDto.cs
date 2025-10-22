using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.User
{
    /// <summary>
    /// User profile with statistics (Response DTO)
    /// </summary>
    public class UserProfileDto
    {
        /// <summary>
        /// Basic user information
        /// </summary>
        public UserDto User { get; set; } = null!;

        /// <summary>
        /// Dashboard statistics
        /// </summary>
        public Common.UserDashboardStats Stats { get; set; } = null!;

        /// <summary>
        /// User's households with roles
        /// </summary>
        public IReadOnlyList<UserHouseholdDto> Households { get; set; } = new List<UserHouseholdDto>();
    }

    /// <summary>
    /// User's household membership info
    /// </summary>
    public class UserHouseholdDto
    {
        public Guid HouseholdId { get; set; }
        public string HouseholdName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
        public int ActiveTaskCount { get; set; }
        public bool IsCurrent { get; set; }
    }
}
