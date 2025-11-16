using HouseholdManager.Domain.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
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
        [SwaggerSchema(ReadOnly = true, Description = "Used only for update")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Guid? Id { get; set; }

        /// <summary>
        /// Household ID that this task belongs to
        /// </summary>
        [JsonIgnore] // не приймається з JSON
        [BindNever]  // не біндиться з запиту
        [SwaggerSchema(ReadOnly = true, Description = "Comes from route")]
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
        /// Managed internally, not included in client requests.
        /// </summary>
        [SwaggerSchema(ReadOnly = true, Description = "Managed internally, not included in requests")]
        [JsonIgnore] // не показувати у Swagger і не приймати з body
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
        /// iCalendar RRULE format for recurrence patterns
        /// Example: "FREQ=WEEKLY;BYDAY=MO" for every Monday
        /// Required for Regular tasks, not allowed for OneTime tasks
        /// </summary>
        [StringLength(500, ErrorMessage = "Recurrence rule cannot exceed 500 characters")]
        public string? RecurrenceRule { get; set; }

        /// <summary>
        /// End date for recurring tasks (UTC)
        /// Defines when a recurring task should stop generating occurrences
        /// </summary>
        public DateTime? RecurrenceEndDate { get; set; }

        /// <summary>
        /// External calendar synchronization ID (for future bidirectional sync)
        /// Managed internally, typically not set by client
        /// </summary>
        [SwaggerSchema(ReadOnly = true, Description = "Managed internally for calendar sync")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [StringLength(255, ErrorMessage = "External calendar ID cannot exceed 255 characters")]
        public string? ExternalCalendarId { get; set; }

        /// <summary>
        /// Last synchronization timestamp with external calendar (UTC)
        /// Managed internally
        /// </summary>
        [SwaggerSchema(ReadOnly = true, Description = "Managed internally")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public DateTime? LastSyncedAt { get; set; }

        /// <summary>
        /// Concurrency control for optimistic locking
        /// </summary>
        [SwaggerSchema(ReadOnly = true, Description = "Used for concurrency check")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public byte[]? RowVersion { get; set; }
    }
}
