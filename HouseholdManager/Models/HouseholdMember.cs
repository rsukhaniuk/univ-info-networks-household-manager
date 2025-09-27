using HouseholdManager.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace HouseholdManager.Models
{
    /// <summary>
    /// Relationship between a user and a household with a role
    /// Allows one user to have different roles in different households
    /// </summary>
    public class HouseholdMember
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// User ID (link to ApplicationUser)
        /// </summary>
        [Required]
        [StringLength(450)] // Standard length for Identity UserId
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Household ID
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
        public virtual ApplicationUser User { get; set; } = null!;

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
