using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.Household
{
    /// <summary>
    /// Query parameters for filtering, searching, sorting, and paginating households
    /// </summary>
    public class HouseholdQueryParameters : Common.BaseQueryParameters
    {
        /// <summary>
        /// Filter by user ID (households where user is a member)
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Filter households where user is an owner
        /// </summary>
        public bool? OwnedByUser { get; set; }

        /// <summary>
        /// Filter by minimum number of members
        /// </summary>
        public int? MinMembers { get; set; }

        /// <summary>
        /// Filter by maximum number of members
        /// </summary>
        public int? MaxMembers { get; set; }

        /// <summary>
        /// Constructor with default sorting
        /// </summary>
        public HouseholdQueryParameters()
        {
            SortBy = "CreatedAt";  // Default sort by creation date
            SortOrder = "desc";    // Newest first
        }
    }
}
