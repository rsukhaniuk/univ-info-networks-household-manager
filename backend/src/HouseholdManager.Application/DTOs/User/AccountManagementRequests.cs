using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HouseholdManager.Application.DTOs.User
{
    /// <summary>
    /// Request to generate password reset ticket
    /// User will be redirected to Auth0 hosted password change page
    /// </summary>
    public class RequestPasswordChangeRequest
    {
        /// <summary>
        /// URL to redirect user after password change
        /// Typically the profile/settings page in your SPA
        /// </summary>
        [Required]
        [Url]
        public string ResultUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to change user email (admin operation)
    /// Only for auth0 connection users (not social logins)
    /// </summary>
    public class ChangeEmailRequest
    {
        /// <summary>
        /// New email address
        /// </summary>
        [Required]
        [EmailAddress]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        public string NewEmail { get; set; } = string.Empty;

        /// <summary>
        /// Whether to send verification email to new address
        /// If true, user must verify the email before it becomes active
        /// If false (default), email is changed immediately without verification (admin operation)
        /// </summary>
        [SwaggerIgnore]
        public bool VerifyEmail { get; set; } = false;
    }

    /// <summary>
    /// Response with password change ticket URL
    /// </summary>
    public class PasswordChangeTicketResponse
    {
        /// <summary>
        /// URL to Auth0 hosted password change page
        /// Redirect user to this URL
        /// </summary>
        public string TicketUrl { get; set; } = string.Empty;

        /// <summary>
        /// Message to display to user
        /// </summary>
        public string Message { get; set; } = "Redirecting to password change page...";
    }

    /// <summary>
    /// Result of checking if account can be deleted
    /// </summary>
    public class AccountDeletionCheckResult
    {
        /// <summary>
        /// Whether the account can be deleted
        /// False if user is owner of any household
        /// </summary>
        public bool CanDelete { get; set; }

        /// <summary>
        /// Number of households where user is owner
        /// </summary>
        public int OwnedHouseholdsCount { get; set; }

        /// <summary>
        /// Number of households where user is a member
        /// </summary>
        public int MemberHouseholdsCount { get; set; }

        /// <summary>
        /// Number of tasks assigned to user
        /// </summary>
        public int AssignedTasksCount { get; set; }

        /// <summary>
        /// List of household names where user is owner
        /// Empty if CanDelete is true
        /// </summary>
        public List<string> OwnedHouseholdNames { get; set; } = new();

        /// <summary>
        /// Message explaining why account cannot be deleted
        /// Null if CanDelete is true
        /// </summary>
        public string? Message { get; set; }
    }
}
