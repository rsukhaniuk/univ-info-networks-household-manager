using HouseholdManager.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.Task
{
    /// <summary>
    /// Request for creating or updating a task (Upsert pattern)
    /// Id = null for Create, Id = value for Update
    /// </summary>
    public class UpsertTaskRequest
    {
        /// <summary>
        /// Task ID (null for create, value for update)
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// Household ID that this task belongs to
        /// </summary>
        [Required(ErrorMessage = "Household ID is required")]
        public Guid HouseholdId { get; set; }

        /// <summary>
        /// Task title
        /// </summary>
        [Required(ErrorMessage = "Task title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Optional task description
        /// </summary>
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        /// <summary>
        /// Task type (Regular or OneTime)
        /// </summary>
        [Required]
        public TaskType Type { get; set; }

        /// <summary>
        /// Task priority (Low, Medium, High)
        /// </summary>
        [Required]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        /// <summary>
        /// Estimated time to complete in minutes (5 minutes to 8 hours)
        /// </summary>
        [Range(5, 480, ErrorMessage = "Estimated time must be between 5 minutes and 8 hours")]
        public int EstimatedMinutes { get; set; } = 30;

        /// <summary>
        /// Room ID where this task is performed
        /// </summary>
        [Required(ErrorMessage = "Room ID is required")]
        public Guid RoomId { get; set; }

        /// <summary>
        /// Optional assigned user ID
        /// </summary>
        public string? AssignedUserId { get; set; }

        /// <summary>
        /// Active status
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Due date for OneTime tasks (UTC)
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Scheduled weekday for Regular tasks
        /// </summary>
        public DayOfWeek? ScheduledWeekday { get; set; }

        /// <summary>
        /// Concurrency control for optimistic locking
        /// </summary>
        public byte[]? RowVersion { get; set; }
    }
}
