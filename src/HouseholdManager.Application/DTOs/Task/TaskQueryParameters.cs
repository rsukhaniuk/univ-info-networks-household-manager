using HouseholdManager.Domain.Enums;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.Task
{
    /// <summary>
    /// Query parameters for filtering, searching, sorting, and paginating tasks
    /// </summary>
    public class TaskQueryParameters : Common.BaseQueryParameters
    {
        /// <summary>
        /// Filter by household ID
        /// </summary>
        [JsonIgnore]
        [SwaggerIgnore]
        public Guid? HouseholdId { get; set; }

        /// <summary>
        /// Filter by room ID
        /// </summary>
        public Guid? RoomId { get; set; }

        /// <summary>
        /// Filter by task type (Regular or OneTime)
        /// </summary>
        public TaskType? Type { get; set; }

        /// <summary>
        /// Filter by task priority (Low, Medium, High)
        /// </summary>
        public TaskPriority? Priority { get; set; }

        /// <summary>
        /// Filter by assigned user ID
        /// </summary>
        public string? AssignedUserId { get; set; }

        /// <summary>
        /// Filter by active status
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Filter by overdue status (for OneTime tasks)
        /// </summary>
        public bool? IsOverdue { get; set; }

        /// <summary>
        /// Filter by scheduled weekday (for Regular tasks)
        /// </summary>
        public DayOfWeek? ScheduledWeekday { get; set; }

        /// <summary>
        /// Constructor with default sorting
        /// </summary>
        public TaskQueryParameters()
        {
            SortBy = "Priority"; // Default sort by priority
            SortOrder = "desc";  // High priority first
        }
    }
}
