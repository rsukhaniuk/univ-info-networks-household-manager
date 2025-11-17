//using HouseholdManager.Application.DTOs;
using HouseholdManager.Application.DTOs.Common;
using HouseholdManager.Application.DTOs.User;
using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Enums;

namespace HouseholdManager.Application.Interfaces.Services
{
    // <summary>
    /// Service interface for user management and profile operations
    /// Wraps UserManager functionality with business logic
    /// </summary>
    public interface IUserService
    {
        // Profile management
        Task<UserDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
        Task<UserDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
        //Task UpdateUserProfileAsync(string userId, string? firstName, string? lastName, string? email, CancellationToken cancellationToken = default);

        // Current household management
        Task SetCurrentHouseholdAsync(
            string userId,
            SetCurrentHouseholdRequest request,
            CancellationToken cancellationToken = default);
        Task<Guid?> GetCurrentHouseholdIdAsync(string userId, CancellationToken cancellationToken = default);

        // User listings (for admin/owner panels)
        Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UserDto>> GetHouseholdUsersAsync(
            Guid householdId,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UserDto>> SearchUsersAsync(
            string searchTerm,
            CancellationToken cancellationToken = default);

        // System admin operations
        //Task<bool> CreateUserAsync(ApplicationUser user, string password, string requestingUserId, CancellationToken cancellationToken = default);
        //Task UpdateUserAsync(ApplicationUser user, string requestingUserId, CancellationToken cancellationToken = default);
        //Task DeleteUserAsync(string userId, string requestingUserId, CancellationToken cancellationToken = default);
        //Task SetSystemRoleAsync(string userId, SystemRole role, string requestingUserId, CancellationToken cancellationToken = default);

        // User statistics
        Task<UserDashboardStats> GetUserDashboardStatsAsync(string userId, CancellationToken cancellationToken = default);
        Task<Dictionary<string, object>> GetUserActivitySummaryAsync(string userId, CancellationToken cancellationToken = default);

        // Validation helpers
        Task ValidateUserAccessAsync(string userId, string requestingUserId, CancellationToken cancellationToken = default);
        Task ValidateSystemAdminAccessAsync(string requestingUserId, CancellationToken cancellationToken = default);
        Task<bool> IsSystemAdminAsync(string userId, CancellationToken cancellationToken = default);

        Task<UserProfileDto> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default);
        Task<UserDto> UpdateProfileAsync(
            string userId,
            UpdateProfileRequest request,
            CancellationToken cancellationToken = default);

        Task<UserDto> SyncUserFromAuth0Async(
            string auth0UserId,
            string email,
            string? firstName = null,
            string? lastName = null,
            string? profilePictureUrl = null,
            CancellationToken cancellationToken = default);

        // Account deletion
        /// <summary>
        /// Check if user can delete their account
        /// Returns false if user is owner of any household
        /// </summary>
        Task<AccountDeletionCheckResult> CanDeleteAccountAsync(
            string userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete user account permanently
        /// Removes user from Auth0 and database
        /// WARNING: This action cannot be undone
        /// </summary>
        Task DeleteAccountAsync(
            string userId,
            CancellationToken cancellationToken = default);
    }

}
