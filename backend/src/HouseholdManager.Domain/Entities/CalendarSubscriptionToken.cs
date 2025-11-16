using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HouseholdManager.Domain.Entities
{
    /// <summary>
    /// Represents a secure token for calendar feed subscriptions
    /// Allows calendar apps (Google Calendar, Outlook, etc.) to access household task feeds without JWT authentication
    /// </summary>
    [Table("CalendarSubscriptionTokens")]
    public class CalendarSubscriptionToken
    {
        /// <summary>
        /// Unique identifier for the token record
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The household this token grants access to
        /// </summary>
        [Required]
        public Guid HouseholdId { get; set; }

        /// <summary>
        /// The user who generated this token (Auth0 user ID)
        /// </summary>
        [Required]
        [StringLength(255)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// The cryptographically secure token string
        /// Used in webcal:// URLs as query parameter
        /// </summary>
        [Required]
        [StringLength(255)]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// When this token was created
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional expiration date for the token
        /// Null means the token never expires
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Whether this token is currently active
        /// Can be set to false to revoke access without deleting the record
        /// </summary>
        [Required]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Last time this token was used to access the calendar feed
        /// Updated on each successful feed request
        /// </summary>
        public DateTime? LastAccessedAt { get; set; }

        // Navigation properties
        public Household Household { get; set; } = null!;
    }
}
