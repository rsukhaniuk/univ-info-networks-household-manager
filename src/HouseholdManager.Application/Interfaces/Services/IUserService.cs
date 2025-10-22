//using HouseholdManager.Application.DTOs;
using HouseholdManager.Application.DTOs.Common;
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
        Task<ApplicationUser?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
        Task<ApplicationUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
        //Task UpdateUserProfileAsync(string userId, string? firstName, string? lastName, string? email, CancellationToken cancellationToken = default);

        // Current household management
        Task SetCurrentHouseholdAsync(string userId, Guid? householdId, CancellationToken cancellationToken = default);
        Task<Guid?> GetCurrentHouseholdIdAsync(string userId, CancellationToken cancellationToken = default);

        // User listings (for admin/owner panels)
        Task<IReadOnlyList<ApplicationUser>> GetAllUsersAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ApplicationUser>> GetHouseholdUsersAsync(Guid householdId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ApplicationUser>> SearchUsersAsync(string searchTerm, CancellationToken cancellationToken = default);

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
    }

}
