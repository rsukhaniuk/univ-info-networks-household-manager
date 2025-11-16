using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.Task
{
    /// <summary>
    /// Detailed task information with executions and context (Response DTO)
    /// </summary>
    public class TaskDetailsDto
    {
        /// <summary>
        /// Basic task information
        /// </summary>
        public TaskDto Task { get; set; } = null!;

        /// <summary>
        /// Room information
        /// </summary>
        public Room.RoomDto Room { get; set; } = null!;

        /// <summary>
        /// Recent executions for this task
        /// </summary>
        public IReadOnlyList<Execution.ExecutionDto> RecentExecutions { get; set; } = new List<Execution.ExecutionDto>();

        /// <summary>
        /// Available household members who can be assigned to this task
        /// </summary>
        public IReadOnlyList<TaskAssigneeDto> AvailableAssignees { get; set; } = new List<TaskAssigneeDto>();

        /// <summary>
        /// Current user permissions for this task
        /// </summary>
        public TaskPermissionsDto Permissions { get; set; } = null!;

        /// <summary>
        /// Task statistics
        /// </summary>
        public TaskStatsDto Stats { get; set; } = null!;
    }

    /// <summary>
    /// User who can be assigned to a task
    /// </summary>
    public class TaskAssigneeDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int CurrentTaskCount { get; set; }
    }

    /// <summary>
    /// Current user's permissions for a task
    /// </summary>
    public class TaskPermissionsDto
    {
        public bool IsOwner { get; set; }
        public bool IsSystemAdmin { get; set; }
        public bool IsAssignedToCurrentUser { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanComplete { get; set; }
        public bool CanAssign { get; set; }
    }

    /// <summary>
    /// Task statistics
    /// </summary>
    public class TaskStatsDto
    {
        public int TotalExecutions { get; set; }
        public int ExecutionsThisWeek { get; set; }
        public int ExecutionsThisMonth { get; set; }
        public DateTime? LastCompleted { get; set; }
        public string? LastCompletedBy { get; set; }
        public int? AverageCompletionTime { get; set; }
    }
}
