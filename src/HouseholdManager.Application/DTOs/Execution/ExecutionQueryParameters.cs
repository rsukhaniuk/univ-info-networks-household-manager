using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.Execution
{
    /// <summary>
    /// Query parameters for filtering, searching, sorting, and paginating task executions
    /// </summary>
    public class ExecutionQueryParameters : Common.BaseQueryParameters
    {
        /// <summary>
        /// Filter by household ID
        /// </summary>
        public Guid? HouseholdId { get; set; }

        /// <summary>
        /// Filter by task ID
        /// </summary>
        public Guid? TaskId { get; set; }

        /// <summary>
        /// Filter by user ID (who completed the task)
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Filter by room ID
        /// </summary>
        public Guid? RoomId { get; set; }

        /// <summary>
        /// Filter by completion date range - start date
        /// </summary>
        public DateTime? CompletedAfter { get; set; }

        /// <summary>
        /// Filter by completion date range - end date
        /// </summary>
        public DateTime? CompletedBefore { get; set; }

        /// <summary>
        /// Filter by week starting date (for weekly statistics)
        /// </summary>
        public DateTime? WeekStarting { get; set; }

        /// <summary>
        /// Filter executions from current week only
        /// </summary>
        public bool? ThisWeekOnly { get; set; }

        /// <summary>
        /// Filter executions with photos only
        /// </summary>
        public bool? HasPhoto { get; set; }

        /// <summary>
        /// Constructor with default sorting
        /// </summary>
        public ExecutionQueryParameters()
        {
            SortBy = "CompletedAt";  // Default sort by completion date
            SortOrder = "desc";       // Most recent first
        }
    }
}
