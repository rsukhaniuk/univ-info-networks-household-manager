using HouseholdManager.Models.DTOs;
using HouseholdManager.Models.Entities;
using HouseholdManager.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace HouseholdManager.Services.Interfaces
{
    // <summary>
    /// Service interface for user management and profile operations
    /// Wraps UserManager functionality with business logic
    /// </summary>
    public interface IUserService
    {
        // Profile management
        Task<ApplicationUser?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
        Task<ApplicationUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<IdentityResult> UpdateUserProfileAsync(string userId, string? firstName, string? lastName, string? email, CancellationToken cancellationToken = default);
        Task<IdentityResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

        // Current household management
        Task SetCurrentHouseholdAsync(string userId, Guid? householdId, CancellationToken cancellationToken = default);
        Task<Guid?> GetCurrentHouseholdIdAsync(string userId, CancellationToken cancellationToken = default);

        // User listings (for admin/owner panels)
        Task<IReadOnlyList<ApplicationUser>> GetAllUsersAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ApplicationUser>> GetHouseholdUsersAsync(Guid householdId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ApplicationUser>> SearchUsersAsync(string searchTerm, CancellationToken cancellationToken = default);

        // System admin operations
        Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password, string requestingUserId, CancellationToken cancellationToken = default);
        Task<IdentityResult> UpdateUserAsync(ApplicationUser user, string requestingUserId, CancellationToken cancellationToken = default);
        Task<IdentityResult> DeleteUserAsync(string userId, string requestingUserId, CancellationToken cancellationToken = default);
        Task<IdentityResult> SetSystemRoleAsync(string userId, SystemRole role, string requestingUserId, CancellationToken cancellationToken = default);

        // User statistics
        Task<UserDashboardStats> GetUserDashboardStatsAsync(string userId, CancellationToken cancellationToken = default);
        Task<Dictionary<string, object>> GetUserActivitySummaryAsync(string userId, CancellationToken cancellationToken = default);

        // Validation helpers
        Task ValidateUserAccessAsync(string userId, string requestingUserId, CancellationToken cancellationToken = default);
        Task ValidateSystemAdminAccessAsync(string requestingUserId, CancellationToken cancellationToken = default);
        Task<bool> IsSystemAdminAsync(string userId, CancellationToken cancellationToken = default);
    }

}
