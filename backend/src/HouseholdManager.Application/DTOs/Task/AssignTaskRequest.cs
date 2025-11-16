using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.Task
{
    /// <summary>
    /// Request for assigning a task to a user
    /// </summary>
    public class AssignTaskRequest
    {
        /// <summary>
        /// User ID to assign the task to (null to unassign)
        /// </summary>
        public string? UserId { get; set; }
    }

    /// <summary>
    /// Request for auto-assigning tasks in a household
    /// </summary>
    public class AutoAssignTasksRequest
    {
        /// <summary>
        /// Household ID
        /// </summary>
        [Required]
        public Guid HouseholdId { get; set; }

        /// <summary>
        /// Optional: only assign tasks for specific weekday (for Regular tasks)
        /// </summary>
        public DayOfWeek? Weekday { get; set; }

        /// <summary>
        /// Optional: only assign unassigned tasks
        /// </summary>
        public bool UnassignedOnly { get; set; } = true;
    }
}
