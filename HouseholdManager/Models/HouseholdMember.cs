using HouseholdManager.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HouseholdManager.Models
{
    /// <summary>
    /// Junction table linking users to households with roles
    /// Allows one user to have different roles in different households
    /// </summary>
    [Table("HouseholdMembers")]
    public class HouseholdMember
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // <summary>
        /// Foreign key to ApplicationUser
        /// </summary>
        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to Household
        /// </summary>
        [Required]
        public Guid HouseholdId { get; set; }

        /// <summary>
        /// User role in this household
        /// </summary>
        [Required]
        public HouseholdRole Role { get; set; }

        /// <summary>
        /// Date when the user joined the household
        /// </summary>
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        /// <summary>
        /// The user who is a member of the household
        /// </summary>
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        // <summary>
        /// The household the user belongs to
        /// </summary>
        [ForeignKey("HouseholdId")]
        [InverseProperty(nameof(Household.Members))]
        public virtual Household Household { get; set; } = null!;

        /// <summary>
        /// Task executions performed by this user in this household
        /// </summary>
        public virtual ICollection<TaskExecution> TaskExecutions { get; set; } = new List<TaskExecution>();

        /// <summary>

        /// <summary>
        /// Checks if this member is the owner of the household
        /// </summary>
        public bool IsOwner => Role == HouseholdRole.Owner;

        /// <summary>
        /// Checks if this member is a regular member
        /// </summary>
        public bool IsMember => Role == HouseholdRole.Member;
    }
}
