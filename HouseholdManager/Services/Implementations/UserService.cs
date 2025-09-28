using HouseholdManager.Models.DTOs;
using HouseholdManager.Models.Entities;
using HouseholdManager.Models.Enums;
using HouseholdManager.Repositories.Interfaces;
using HouseholdManager.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HouseholdManager.Services.Implementations
{
    /// <summary>
    /// Implementation of user service with business logic for profile and admin operations
    /// </summary>
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHouseholdMemberRepository _memberRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IExecutionRepository _executionRepository;
        private readonly ILogger<UserService> _logger;

        public UserService(
            UserManager<ApplicationUser> userManager,
            IHouseholdMemberRepository memberRepository,
            ITaskRepository taskRepository,
            IExecutionRepository executionRepository,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _memberRepository = memberRepository;
            _taskRepository = taskRepository;
            _executionRepository = executionRepository;
            _logger = logger;
        }

        // Profile management
        public async Task<ApplicationUser?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<ApplicationUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<IdentityResult> UpdateUserProfileAsync(string userId, string? firstName, string? lastName, string? email, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            // Update profile fields
            user.FirstName = firstName;
            user.LastName = lastName;

            // Update email if changed
            if (!string.IsNullOrEmpty(email) && user.Email != email)
            {
                var emailResult = await _userManager.SetEmailAsync(user, email);
                if (!emailResult.Succeeded)
                    return emailResult;

                // Also update username to match email
                var usernameResult = await _userManager.SetUserNameAsync(user, email);
                if (!usernameResult.Succeeded)
                    return usernameResult;
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("Updated profile for user {UserId}", userId);
            }

            return result;
        }

        public async Task<IdentityResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                _logger.LogInformation("Changed password for user {UserId}", userId);
            }

            return result;
        }

        // Current household management
        public async Task SetCurrentHouseholdAsync(string userId, Guid? householdId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found");

            // Validate user is member of the household if setting one
            if (householdId.HasValue)
            {
                var isMember = await _memberRepository.IsUserMemberAsync(householdId.Value, userId, cancellationToken);
                if (!isMember)
                    throw new InvalidOperationException("User is not a member of the specified household");
            }

            user.CurrentHouseholdId = householdId;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Set current household {HouseholdId} for user {UserId}", householdId, userId);
        }

        public async Task<Guid?> GetCurrentHouseholdIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.CurrentHouseholdId;
        }

        // User listings
        public async Task<IReadOnlyList<ApplicationUser>> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            return await _userManager.Users
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ApplicationUser>> GetHouseholdUsersAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            var memberUserIds = await _memberRepository.GetByHouseholdIdAsync(householdId, cancellationToken);
            var userIds = memberUserIds.Select(m => m.UserId).ToList();

            return await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ApplicationUser>> SearchUsersAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            var normalizedTerm = searchTerm.ToLower();

            return await _userManager.Users
                .Where(u => (u.FirstName != null && u.FirstName.ToLower().Contains(normalizedTerm)) ||
                           (u.LastName != null && u.LastName.ToLower().Contains(normalizedTerm)) ||
                           (u.Email != null && u.Email.ToLower().Contains(normalizedTerm)) ||
                           (u.UserName != null && u.UserName.ToLower().Contains(normalizedTerm)))
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Take(50) // Limit results
                .ToListAsync(cancellationToken);
        }

        // System admin operations
        public async Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateSystemAdminAccessAsync(requestingUserId, cancellationToken);

            user.CreatedAt = DateTime.UtcNow;
            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                _logger.LogInformation("System admin {AdminId} created user {UserId}", requestingUserId, user.Id);
            }

            return result;
        }

        public async Task<IdentityResult> UpdateUserAsync(ApplicationUser user, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateSystemAdminAccessAsync(requestingUserId, cancellationToken);

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("System admin {AdminId} updated user {UserId}", requestingUserId, user.Id);
            }

            return result;
        }

        public async Task<IdentityResult> DeleteUserAsync(string userId, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateSystemAdminAccessAsync(requestingUserId, cancellationToken);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            // Prevent self-deletion
            if (userId == requestingUserId)
                return IdentityResult.Failed(new IdentityError { Description = "Cannot delete your own account" });

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("System admin {AdminId} deleted user {UserId}", requestingUserId, userId);
            }

            return result;
        }

        public async Task<IdentityResult> SetSystemRoleAsync(string userId, SystemRole role, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateSystemAdminAccessAsync(requestingUserId, cancellationToken);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            // Prevent changing own role
            if (userId == requestingUserId)
                return IdentityResult.Failed(new IdentityError { Description = "Cannot change your own system role" });

            user.Role = role;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInformation("System admin {AdminId} changed user {UserId} role to {Role}",
                    requestingUserId, userId, role);
            }

            return result;
        }

        // User statistics
        public async Task<UserDashboardStats> GetUserDashboardStatsAsync(string userId, CancellationToken cancellationToken = default)
        {
            var memberships = await _memberRepository.GetByUserIdAsync(userId, cancellationToken);
            var householdIds = memberships.Select(m => m.HouseholdId).ToList();

            var stats = new UserDashboardStats
            {
                TotalHouseholds = memberships.Count,
                OwnedHouseholds = memberships.Count(m => m.Role == HouseholdRole.Owner)
            };

            if (householdIds.Any())
            {
                // Get tasks across all households
                var userTasks = await _taskRepository.GetByAssignedUserIdAsync(userId, cancellationToken);
                stats.ActiveTasks = userTasks.Count;

                // Get executions this week across all households
                var thisWeekExecutions = new List<TaskExecution>();
                foreach (var householdId in householdIds)
                {
                    var executions = await _executionRepository.GetUserExecutionsThisWeekAsync(userId, householdId, cancellationToken);
                    thisWeekExecutions.AddRange(executions);
                }

                stats.CompletedTasksThisWeek = thisWeekExecutions.Count;
                stats.LastActivity = thisWeekExecutions.OrderByDescending(e => e.CompletedAt).FirstOrDefault()?.CompletedAt;
            }

            return stats;
        }

        public async Task<Dictionary<string, object>> GetUserActivitySummaryAsync(string userId, CancellationToken cancellationToken = default)
        {
            var stats = await GetUserDashboardStatsAsync(userId, cancellationToken);

            return new Dictionary<string, object>
            {
                ["totalHouseholds"] = stats.TotalHouseholds,
                ["ownedHouseholds"] = stats.OwnedHouseholds,
                ["activeTasks"] = stats.ActiveTasks,
                ["completedThisWeek"] = stats.CompletedTasksThisWeek,
                ["lastActivity"] = stats.LastActivity?.ToString("yyyy-MM-dd HH:mm") ?? "Never"
            };
        }

        // Validation helpers
        public async Task ValidateUserAccessAsync(string userId, string requestingUserId, CancellationToken cancellationToken = default)
        {
            // Users can access their own profile, system admins can access any profile
            if (userId != requestingUserId && !await IsSystemAdminAsync(requestingUserId, cancellationToken))
                throw new UnauthorizedAccessException("Access denied");
        }

        public async Task ValidateSystemAdminAccessAsync(string requestingUserId, CancellationToken cancellationToken = default)
        {
            if (!await IsSystemAdminAsync(requestingUserId, cancellationToken))
                throw new UnauthorizedAccessException("System administrator access required");
        }

        public async Task<bool> IsSystemAdminAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.Role == SystemRole.SystemAdmin;
        }
    }
}
