using HouseholdManager.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HouseholdManager.Domain.Entities
{
    /// <summary>
    /// Application user entity (Auth0 integration)
    /// No password storage - authentication handled by Auth0
    /// </summary>
    public class ApplicationUser
    {
        /// <summary>
        /// User ID from Auth0
        /// </summary>
        [Key]
        [StringLength(255)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// First name of the user
        /// </summary>
        [StringLength(50)]
        public string? FirstName { get; set; }

        /// <summary>
        /// Last name of the user
        /// </summary>
        [StringLength(50)]
        public string? LastName { get; set; }

        /// <summary>
        /// Profile picture URL from Auth0
        /// </summary>
        [StringLength(500)]
        public string? ProfilePictureUrl { get; set; }

        /// <summary>
        /// Date and time when the account was created (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// System role of the user in the platform
        /// </summary>
        public SystemRole Role { get; set; } = SystemRole.User;

        /// <summary>
        /// Currently active household selected for navigation
        /// Applies only to SystemRole.User - system admins do not have a "current" household
        /// </summary>
        public Guid? CurrentHouseholdId { get; set; }

        // Computed properties

        /// <summary>
        /// Computed full name (FirstName + LastName)
        /// </summary>
        public string FullName => $"{FirstName} {LastName}".Trim();

        /// <summary>
        /// Indicates whether the user is a system administrator
        /// </summary>
        public bool IsSystemAdmin => Role == SystemRole.SystemAdmin;

        // Navigation properties

        /// <summary>
        /// Household memberships of the user (only for SystemRole.User)
        /// </summary>
        public virtual ICollection<HouseholdMember> HouseholdMemberships { get; set; } = new List<HouseholdMember>();

        /// <summary>
        /// Tasks assigned to this user
        /// </summary>
        public virtual ICollection<HouseholdTask> AssignedTasks { get; set; } = new List<HouseholdTask>();

        /// <summary>
        /// Task executions performed by this user
        /// </summary>
        public virtual ICollection<TaskExecution> TaskExecutions { get; set; } = new List<TaskExecution>();

        /// <summary>
        /// Checks if the user has the Owner role in a specific household
        /// </summary>
        public bool IsOwnerOf(Guid householdId)
        {
            if (IsSystemAdmin) return true; // System admin has full access

            return HouseholdMemberships.Any(hm =>
                hm.HouseholdId == householdId &&
                hm.Role == HouseholdRole.Owner);
        }

        /// <summary>
        /// Checks if the user is a member of a specific household
        /// </summary>
        public bool IsMemberOf(Guid householdId)
        {
            if (IsSystemAdmin) return true; // System admin has full access

            return HouseholdMemberships.Any(hm => hm.HouseholdId == householdId);
        }
    }
}
