using HouseholdManager.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.User
{
    /// <summary>
    /// Query parameters for filtering, searching, sorting, and paginating users (Admin panel)
    /// </summary>
    public class UserQueryParameters : Common.BaseQueryParameters
    {
        /// <summary>
        /// Filter by system role
        /// </summary>
        public SystemRole? Role { get; set; }

        /// <summary>
        /// Filter by household membership
        /// </summary>
        public Guid? HouseholdId { get; set; }

        /// <summary>
        /// Filter by account creation date range - start date
        /// </summary>
        public DateTime? CreatedAfter { get; set; }

        /// <summary>
        /// Filter by account creation date range - end date
        /// </summary>
        public DateTime? CreatedBefore { get; set; }

        /// <summary>
        /// Filter users with active tasks
        /// </summary>
        public bool? HasActiveTasks { get; set; }

        /// <summary>
        /// Constructor with default sorting
        /// </summary>
        public UserQueryParameters()
        {
            SortBy = "LastName";  // Default sort by last name
            SortOrder = "asc";    // Alphabetical order
        }
    }
}
