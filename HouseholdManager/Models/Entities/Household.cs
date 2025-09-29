using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HouseholdManager.Models.Entities
{
    /// <summary>
    /// Represents a household where users can manage tasks and chores together
    /// </summary>
    [Table("Households")]
    public class Household
    {
        /// <summary>
        /// Identifier for the household
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Name of the household
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description or notes about the household
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Unique invite code for joining this household
        /// </summary>
        [Required]
        public Guid InviteCode { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Date and time when the household was created (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        /// <summary>
        /// Members of this household with their roles
        /// </summary>
        public virtual ICollection<HouseholdMember> Members { get; set; } = new List<HouseholdMember>();

        /// <summary>
        /// Rooms in this household
        /// </summary>
        [InverseProperty(nameof(Room.Household))]
        public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();

        /// <summary>
        /// Tasks defined for this household
        /// </summary>
        [InverseProperty(nameof(HouseholdTask.Household))]
        public virtual ICollection<HouseholdTask> Tasks { get; set; } = new List<HouseholdTask>();

        /// <summary>
        /// Generate a new unique invite code
        /// </summary>
        public static Guid GenerateInviteCode()
        {
            return Guid.NewGuid();
        }

        /// <summary>
        /// Get all owners of this household
        /// </summary>
        [NotMapped]
        public IEnumerable<HouseholdMember> Owners => Members.Where(m => m.IsOwner);

        /// <summary>
        /// Get total number of members
        /// </summary>
        public int MemberCount => Members.Count;
    }
}
