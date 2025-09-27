using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HouseholdManager.Models
{
    /// <summary>
    /// Represents a completed task execution with optional photo and notes
    /// </summary>
    [Table("TaskExecutions")]
    public class TaskExecution
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Foreign key to HouseholdTask
        /// </summary>
        [Required]
        public Guid TaskId { get; set; }

        /// <summary>
        /// Foreign key to ApplicationUser
        /// </summary>
        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// When the task was completed (stored in UTC)
        /// </summary>
        public DateTime CompletedAt { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        /// <summary>
        /// Relative path to completion photo (stored in wwwroot/uploads)
        /// </summary>
        [StringLength(260)]
        public string? PhotoPath { get; set; }

        /// <summary>
        /// Week starting date (Monday) in UTC for weekly tracking
        /// </summary>
        public DateTime WeekStarting { get; set; }

        // Denormalized fields for faster queries without joins
        /// <summary>
        /// Denormalized household ID for performance
        /// </summary>
        [Required]
        public Guid HouseholdId { get; set; }

        /// <summary>
        /// Denormalized room ID for performance
        /// </summary>
        [Required]
        public Guid RoomId { get; set; }

        // Navigation properties
        /// <summary>
        /// The task that was executed
        /// </summary>
        [ForeignKey("TaskId")]
        public virtual HouseholdTask Task { get; set; } = null!;

        /// <summary>
        /// The user who completed the task
        /// </summary>
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        /// <summary>
        /// The household this execution belongs to (denormalized)
        /// </summary>
        [ForeignKey("HouseholdId")]
        public virtual Household Household { get; set; } = null!;

        /// <summary>
        /// The room where task was executed (denormalized)
        /// </summary>
        [ForeignKey("RoomId")]
        public virtual Room Room { get; set; } = null!;

        /// <summary>
        /// Calculate week starting date (Monday) for a given date
        /// </summary>
        public static DateTime GetWeekStarting(DateTime date)
        {
            var utcDate = date.ToUniversalTime().Date;
            var daysFromMonday = ((int)utcDate.DayOfWeek - 1 + 7) % 7;
            return utcDate.AddDays(-daysFromMonday);
        }

        /// <summary>
        /// Check if execution was completed this week
        /// </summary>
        public bool IsThisWeek => WeekStarting == GetWeekStarting(DateTime.UtcNow);

        /// <summary>
        /// Get formatted completion time relative to now
        /// </summary>
        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.UtcNow - CompletedAt;

                if (timeSpan.TotalMinutes < 1)
                    return "Just now";
                if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes} minutes ago";
                if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours} hours ago";
                if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays} days ago";

                return CompletedAt.ToString("MMM dd, yyyy");
            }
        }
    }
}