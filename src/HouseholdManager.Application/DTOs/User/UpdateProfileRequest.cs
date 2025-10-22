using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.User
{
    /// <summary>
    /// Request for updating user profile
    /// Email and Password changes handled by Auth0
    /// </summary>
    public class UpdateProfileRequest
    {
        /// <summary>
        /// First name
        /// </summary>
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string? FirstName { get; set; }

        /// <summary>
        /// Last name
        /// </summary>
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string? LastName { get; set; }
    }

    /// <summary>
    /// Request for setting current household
    /// </summary>
    public class SetCurrentHouseholdRequest
    {
        /// <summary>
        /// Household ID to set as current (null to clear)
        /// </summary>
        public Guid? HouseholdId { get; set; }
    }
}
