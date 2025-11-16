namespace HouseholdManager.Application.Interfaces.Services
{
    /// <summary>
    /// Service for managing calendar subscription tokens
    /// Provides secure token-based authentication for calendar feed URLs
    /// </summary>
    public interface ICalendarTokenService
    {
        /// <summary>
        /// Generates a new calendar subscription token for a household
        /// Returns existing token if one already exists for this household/user combination
        /// </summary>
        /// <param name="householdId">The household ID</param>
        /// <param name="userId">The user ID (Auth0)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The token string to be used in webcal:// URLs</returns>
        Task<string> GenerateTokenAsync(Guid householdId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a calendar subscription token and returns the associated household and user
        /// Updates LastAccessedAt timestamp on successful validation
        /// </summary>
        /// <param name="token">The token string from the URL</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tuple of (householdId, userId) if valid, null if invalid/expired</returns>
        Task<(Guid householdId, string userId)?> ValidateTokenAsync(string? token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes a calendar subscription token for a specific household/user
        /// Sets IsActive to false without deleting the record
        /// </summary>
        /// <param name="householdId">The household ID</param>
        /// <param name="userId">The user ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if token was found and revoked, false if no token exists</returns>
        Task<bool> RevokeTokenAsync(Guid householdId, string userId, CancellationToken cancellationToken = default);
    }
}
