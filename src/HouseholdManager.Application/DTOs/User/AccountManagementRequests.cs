using System.ComponentModel.DataAnnotations;

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
}
