using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.Room
{
    /// <summary>
    /// Room information with active tasks (Response DTO)
    /// Used for room details page
    /// </summary>
    public class RoomWithTasksDto
    {
        /// <summary>
        /// Basic room information
        /// </summary>
        public RoomDto Room { get; set; } = null!;

        /// <summary>
        /// List of active tasks assigned to this room
        /// </summary>
        public IReadOnlyList<Task.TaskDto> ActiveTasks { get; set; } = new List<Task.TaskDto>();

        /// <summary>
        /// List of completed tasks for this room (this week)
        /// </summary>
        public IReadOnlyList<Execution.ExecutionDto> RecentExecutions { get; set; } = new List<Execution.ExecutionDto>();

        /// <summary>
        /// Indicates if the current user is an owner of this room's household
        /// </summary>
        public bool IsOwner { get; set; }

        /// <summary>
        /// Statistics for this room
        /// </summary>
        public RoomStatsDto Stats { get; set; } = null!;
    }

    /// <summary>
    /// Statistics for a room
    /// </summary>
    public class RoomStatsDto
    {
        /// <summary>
        /// Total number of tasks (active and inactive)
        /// </summary>
        public int TotalTasks { get; set; }

        /// <summary>
        /// Number of active tasks
        /// </summary>
        public int ActiveTasks { get; set; }

        /// <summary>
        /// Number of overdue tasks
        /// </summary>
        public int OverdueTasks { get; set; }

        /// <summary>
        /// Number of tasks completed this week
        /// </summary>
        public int CompletedThisWeek { get; set; }

        /// <summary>
        /// Average time to complete tasks (in minutes)
        /// </summary>
        public int? AverageCompletionTime { get; set; }

        /// <summary>
        /// Last activity timestamp (last task completion)
        /// </summary>
        public DateTime? LastActivity { get; set; }
    }
}
