using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.Room
{
    /// <summary>
    /// Query parameters for filtering, searching, sorting, and paginating rooms
    /// </summary>
    public class RoomQueryParameters : Common.BaseQueryParameters
    {
        /// <summary>
        /// Filter by household ID
        /// </summary>
        [JsonIgnore]
        [SwaggerIgnore]
        public Guid? HouseholdId { get; set; }

        /// <summary>
        /// Filter by minimum priority
        /// </summary>
        public int? MinPriority { get; set; }

        /// <summary>
        /// Filter by maximum priority
        /// </summary>
        public int? MaxPriority { get; set; }

        /// <summary>
        /// Filter rooms with photos only
        /// </summary>
        public bool? HasPhoto { get; set; }

        /// <summary>
        /// Filter rooms with active tasks
        /// </summary>
        public bool? HasActiveTasks { get; set; }

        /// <summary>
        /// Constructor with default sorting
        /// </summary>
        public RoomQueryParameters()
        {
            SortBy = "Priority";  // Default sort by priority
            SortOrder = "desc";   // High priority first
        }
    }
}
