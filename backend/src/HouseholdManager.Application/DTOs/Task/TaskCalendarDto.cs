using HouseholdManager.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.Task
{
    /// <summary>
    /// Task calendar view - tasks organized by weekday (Response DTO)
    /// </summary>
    public class TaskCalendarDto
    {
        /// <summary>
        /// Week starting date (Monday)
        /// </summary>
        public DateTime WeekStarting { get; set; }

        /// <summary>
        /// Week ending date (Sunday)
        /// </summary>
        public DateTime WeekEnding { get; set; }

        /// <summary>
        /// Tasks grouped by day of week
        /// </summary>
        public Dictionary<DayOfWeek, List<TaskCalendarItemDto>> TasksByDay { get; set; } = new();

        /// <summary>
        /// One-time tasks for this week (not tied to specific weekday)
        /// </summary>
        public List<TaskCalendarItemDto> OneTimeTasks { get; set; } = new();

        /// <summary>
        /// Weekly statistics
        /// </summary>
        public WeeklyStatsDto WeeklyStats { get; set; } = null!;
    }

    /// <summary>
    /// Calendar item for a single task
    /// </summary>
    public class TaskCalendarItemDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public TaskPriority Priority { get; set; }
        public int EstimatedMinutes { get; set; }
        public string? AssignedUserId { get; set; }
        public string? AssignedUserName { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsOverdue { get; set; }
    }

    /// <summary>
    /// Weekly statistics for calendar view
    /// </summary>
    public class WeeklyStatsDto
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
        public int OverdueTasks { get; set; }
        public double CompletionRate { get; set; }
    }
}
