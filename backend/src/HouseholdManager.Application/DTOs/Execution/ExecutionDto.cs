using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.Execution
{
    /// <summary>
    /// Task execution information (Response DTO)
    /// </summary>
    public class ExecutionDto
    {
        /// <summary>
        /// Execution unique identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Task ID that was executed
        /// </summary>
        public Guid TaskId { get; set; }

        /// <summary>
        /// Task title (for display)
        /// </summary>
        public string TaskTitle { get; set; } = string.Empty;

        /// <summary>
        /// User ID who completed the task
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// User name who completed the task (full name with fallback to email)
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// User email (optional, for additional display purposes)
        /// </summary>
        public string? UserEmail { get; set; }

        /// <summary>
        /// Household ID (denormalized)
        /// </summary>
        public Guid HouseholdId { get; set; }

        /// <summary>
        /// Room ID (denormalized)
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// Room name (for display)
        /// </summary>
        public string RoomName { get; set; } = string.Empty;

        /// <summary>
        /// When the task was completed (UTC)
        /// </summary>
        public DateTime CompletedAt { get; set; }

        /// <summary>
        /// Optional notes about completion
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Relative path to completion photo
        /// </summary>
        public string? PhotoPath { get; set; }

        /// <summary>
        /// Full URL to completion photo
        /// </summary>
        public string? PhotoUrl { get; set; }

        /// <summary>
        /// Week starting date (Monday) in UTC
        /// </summary>
        public DateTime WeekStarting { get; set; }

        /// <summary>
        /// Formatted time ago (e.g., "2 hours ago", "Just now")
        /// </summary>
        [JsonIgnore]
        [SwaggerSchema(ReadOnly = true, Description = "Generated internally, hidden from response")]
        public string TimeAgo { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if this execution is from the current week
        /// </summary>
        public bool IsThisWeek { get; set; }

        /// <summary>
        /// Indicates if execution has a photo
        /// </summary>
        public bool HasPhoto => !string.IsNullOrEmpty(PhotoPath);
    }
}
