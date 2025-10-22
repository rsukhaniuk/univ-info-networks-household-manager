using HouseholdManager.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.Task
{
    /// <summary>
    /// Basic task information (Response DTO)
    /// </summary>
    public class TaskDto
    {
        /// <summary>
        /// Task unique identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Household ID
        /// </summary>
        public Guid HouseholdId { get; set; }

        /// <summary>
        /// Room ID
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// Room name (for display)
        /// </summary>
        public string RoomName { get; set; } = string.Empty;

        /// <summary>
        /// Task title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Optional task description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Task type (Regular or OneTime)
        /// </summary>
        public TaskType Type { get; set; }

        /// <summary>
        /// Task priority (Low, Medium, High)
        /// </summary>
        public TaskPriority Priority { get; set; }

        /// <summary>
        /// Estimated time in minutes
        /// </summary>
        public int EstimatedMinutes { get; set; }

        /// <summary>
        /// Formatted estimated time (e.g., "30 min", "1h 30m")
        /// </summary>
        public string FormattedEstimatedTime { get; set; } = string.Empty;

        /// <summary>
        /// Due date for OneTime tasks (UTC)
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Scheduled weekday for Regular tasks
        /// </summary>
        public DayOfWeek? ScheduledWeekday { get; set; }

        /// <summary>
        /// Assigned user ID
        /// </summary>
        public string? AssignedUserId { get; set; }

        /// <summary>
        /// Assigned user name (for display)
        /// </summary>
        public string? AssignedUserName { get; set; }

        /// <summary>
        /// Active status
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// When the task was created (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Indicates if task is overdue (for OneTime tasks)
        /// </summary>
        public bool IsOverdue { get; set; }

        /// <summary>
        /// Indicates if task was completed this week (for Regular tasks)
        /// </summary>
        public bool IsCompletedThisWeek { get; set; }

        /// <summary>
        /// Concurrency control
        /// </summary>
        public byte[]? RowVersion { get; set; }
    }
}
