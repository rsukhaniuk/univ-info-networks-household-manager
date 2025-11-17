namespace HouseholdManager.Application.Interfaces.ExternalServices
{
    /// <summary>
    /// Interface for Auth0 Management API operations
    /// </summary>
    public interface IAuth0ManagementApiClient
    {
        /// <summary>
        /// Generate password reset ticket for user
        /// User will be redirected to Auth0 hosted password change page
        /// </summary>
        /// <param name="userId">Auth0 user ID (sub claim)</param>
        /// <param name="resultUrl">URL to redirect after password change</param>
        /// <returns>Ticket URL for password reset</returns>
        Task<string> CreatePasswordChangeTicketAsync(string userId, string resultUrl);

        /// <summary>
        /// Change user email (admin operation)
        /// WARNING: This skips email verification by default
        /// </summary>
        /// <param name="userId">Auth0 user ID (sub claim)</param>
        /// <param name="newEmail">New email address</param>
        /// <param name="verifyEmail">Whether to send verification email (default: false for admin operation)</param>
        /// <returns>Task</returns>
        Task ChangeUserEmailAsync(string userId, string newEmail, bool verifyEmail = false);

        /// <summary>
        /// Get user connection type (auth0, google-oauth2, etc.)
        /// </summary>
        /// <param name="userId">Auth0 user ID (sub claim)</param>
        /// <returns>Connection name (e.g., "auth0", "google-oauth2")</returns>
        Task<string> GetUserConnectionAsync(string userId);

        /// <summary>
        /// Check if user can change password (only for auth0 connection)
        /// </summary>
        /// <param name="userId">Auth0 user ID (sub claim)</param>
        /// <returns>True if user has auth0 connection (email/password)</returns>
        Task<bool> CanUserChangePasswordAsync(string userId);

        /// <summary>
        /// Update user's display name in Auth0
        /// Syncs FirstName + LastName to Auth0 name field
        /// </summary>
        /// <param name="userId">Auth0 user ID (sub claim)</param>
        /// <param name="fullName">Full name (FirstName + LastName)</param>
        /// <returns>Task</returns>
        Task UpdateUserNameAsync(string userId, string fullName);

        /// <summary>
        /// Delete user from Auth0
        /// WARNING: This is a permanent action and cannot be undone
        /// </summary>
        /// <param name="userId">Auth0 user ID (sub claim)</param>
        /// <returns>Task</returns>
        Task DeleteUserAsync(string userId);
    }
}
