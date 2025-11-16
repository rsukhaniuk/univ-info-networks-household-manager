using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HouseholdManager.Models.Entities
{
    /// <summary>
    /// Represents a room within a household that can have tasks assigned to it
    /// </summary>
    public class Room
    {
        /// <summary>
        /// Identifier for the room
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Name of the room
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description or notes about the room
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Relative path to room photo (stored in wwwroot/uploads)
        /// </summary>
        [StringLength(260)]
        public string? PhotoPath { get; set; }

        /// <summary>
        /// Not used currently - Priority of the room for task assignment (1-10)
        /// </summary>
        [Range(1, 10)]
        public int Priority { get; set; } = 5;

        /// <summary>
        /// Foreign key to Household
        /// </summary>
        [Required]
        public Guid HouseholdId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        /// <summary>
        /// The household this room belongs to
        /// </summary>
        [ForeignKey(nameof(HouseholdId))]
        [InverseProperty(nameof(Household.Rooms))]
        public virtual Household Household { get; set; } = null!;

        /// <summary>
        /// Tasks assigned to this room
        /// </summary>
        [InverseProperty(nameof(HouseholdTask.Room))]
        public virtual ICollection<HouseholdTask> Tasks { get; set; } = new List<HouseholdTask>();

        /// <summary>
        /// Get active tasks for this room
        /// </summary>
        [NotMapped]
        public IEnumerable<HouseholdTask> ActiveTasks => Tasks.Where(t => t.IsActive);

        /// <summary>
        /// Get count of active tasks
        /// </summary>
        public int ActiveTaskCount => Tasks.Count(t => t.IsActive);
    }
}
