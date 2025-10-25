using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.Interfaces.Repositories
{
    /// <summary>
    /// Repository interface for ApplicationUser entity operations
    /// Manages Auth0-synced users in the local database
    /// </summary>
    public interface IUserRepository : IRepository<ApplicationUser>
    {
        /// <summary>
        /// Gets a user by their Auth0 user ID
        /// </summary>
        /// <param name="userId">Auth0 user ID (e.g., "auth0|123abc")</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>ApplicationUser entity or null if not found</returns>
        Task<ApplicationUser?> GetByIdAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a user by email address
        /// </summary>
        /// <param name="email">User email</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>ApplicationUser entity or null if not found</returns>
        Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all users in the system
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of all users</returns>
        Task<IReadOnlyList<ApplicationUser>> GetAllUsersAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all users that are members of a specific household
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of users in the household</returns>
        Task<IReadOnlyList<ApplicationUser>> GetHouseholdUsersAsync(Guid householdId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches users by name or email
        /// </summary>
        /// <param name="searchTerm">Search term (partial match)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of matching users</returns>
        Task<IReadOnlyList<ApplicationUser>> SearchUsersAsync(string searchTerm, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates or updates a user (upsert operation for Auth0 sync)
        /// </summary>
        /// <param name="userId">Auth0 user ID</param>
        /// <param name="email">User email</param>
        /// <param name="firstName">First name (optional)</param>
        /// <param name="lastName">Last name (optional)</param>
        /// <param name="profilePictureUrl">Profile picture URL (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created or updated ApplicationUser entity</returns>
        Task<ApplicationUser> UpsertUserAsync(
            string userId,
            string email,
            string? firstName = null,
            string? lastName = null,
            string? profilePictureUrl = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates user profile information
        /// </summary>
        /// <param name="userId">Auth0 user ID</param>
        /// <param name="firstName">Updated first name</param>
        /// <param name="lastName">Updated last name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task UpdateProfileAsync(
            string userId,
            string? firstName,
            string? lastName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets or clears the user's current household
        /// </summary>
        /// <param name="userId">Auth0 user ID</param>
        /// <param name="householdId">Household ID (null to clear)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task SetCurrentHouseholdAsync(
            string userId,
            Guid? householdId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the user's system role (SystemAdmin or User)
        /// </summary>
        /// <param name="userId">Auth0 user ID</param>
        /// <param name="role">New system role</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task SetSystemRoleAsync(
            string userId,
            SystemRole role,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a user exists by Auth0 user ID
        /// </summary>
        /// <param name="userId">Auth0 user ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if user exists, false otherwise</returns>
        Task<bool> ExistsAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a user is a system administrator
        /// </summary>
        /// <param name="userId">Auth0 user ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if user is SystemAdmin, false otherwise</returns>
        Task<bool> IsSystemAdminAsync(string userId, CancellationToken cancellationToken = default);
    }
}
