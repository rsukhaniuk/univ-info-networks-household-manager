using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HouseholdManager.Domain.Enums;

namespace HouseholdManager.Domain.Entities
{
    /// <summary>
    /// Represents a household task that can be assigned and tracked
    /// </summary>
    [Table("HouseholdTasks")]
    public class HouseholdTask : IValidatableObject
    {
        /// <summary>
        /// Identifier for the household task
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Title of the task
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Description or details about the task
        /// </summary>
        [StringLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Type of the task 
        /// </summary>
        [Required]
        public TaskType Type { get; set; }

        /// <summary>
        /// Priority of the task (Low, Medium, High)
        /// </summary>
        [Required]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        /// <summary>
        /// Estimated time to complete in minutes (5 minutes to 8 hours)
        /// </summary>
        [Range(5, 480)]
        public int EstimatedMinutes { get; set; } = 30;

        /// <summary>
        /// Due date for OneTime tasks only (stored in UTC)
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Scheduled weekday for Regular tasks only
        /// </summary>
        public DayOfWeek? ScheduledWeekday { get; set; }

        /// <summary>
        /// Foreign key to Household
        /// </summary>
        [Required]
        public Guid HouseholdId { get; set; }

        /// <summary>
        /// Foreign key to Room
        /// </summary>
        [Required]
        public Guid RoomId { get; set; }

        /// <summary>
        /// Foreign key to assigned user (optional)
        /// </summary>
        [StringLength(450)]
        public string? AssignedUserId { get; set; }

        /// <summary>
        /// Active status of the task
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Creation timestamp (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Concurrency control for optimistic locking
        /// </summary>
        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Navigation properties
        /// <summary>
        /// The household this task belongs to
        /// </summary>
        [ForeignKey(nameof(HouseholdId))]
        [InverseProperty(nameof(Household.Tasks))]
        public virtual Household Household { get; set; } = null!;

        /// <summary>
        /// The room this task is assigned to
        /// </summary>
        [ForeignKey(nameof(RoomId))]
        [InverseProperty(nameof(Room.Tasks))]
        public virtual Room Room { get; set; } = null!;

        /// <summary>
        /// The user assigned to this task (optional)
        /// </summary>
        [ForeignKey(nameof(AssignedUserId))]
        public virtual ApplicationUser? AssignedUser { get; set; }

        /// <summary>
        /// Execution history for this task
        /// </summary>
        public virtual ICollection<TaskExecution> Executions { get; set; } = new List<TaskExecution>();

        /// <summary>
        /// Custom validation based on task type
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Type == TaskType.Regular)
            {
                if (ScheduledWeekday == null)
                {
                    yield return new ValidationResult(
                        "Regular tasks must have a scheduled weekday.",
                        new[] { nameof(ScheduledWeekday) });
                }
                if (DueDate != null)
                {
                    yield return new ValidationResult(
                        "Regular tasks should not have a due date.",
                        new[] { nameof(DueDate) });
                }
            }
            else if (Type == TaskType.OneTime)
            {
                if (DueDate == null)
                {
                    yield return new ValidationResult(
                        "One-time tasks must have a due date.",
                        new[] { nameof(DueDate) });
                }
                if (ScheduledWeekday != null)
                {
                    yield return new ValidationResult(
                        "One-time tasks should not have a scheduled weekday.",
                        new[] { nameof(ScheduledWeekday) });
                }
            }
        }

        /// <summary>
        /// Check if task is overdue (for OneTime tasks only)
        /// </summary>
        public bool IsOverdue => Type == TaskType.OneTime &&
                                DueDate.HasValue &&
                                DueDate.Value < DateTime.UtcNow;

        /// <summary>
        /// Get formatted estimated time
        /// </summary>
        public string FormattedEstimatedTime
        {
            get
            {
                if (EstimatedMinutes < 60)
                    return $"{EstimatedMinutes} min";

                var hours = EstimatedMinutes / 60;
                var minutes = EstimatedMinutes % 60;
                return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
            }
        }
    }
}