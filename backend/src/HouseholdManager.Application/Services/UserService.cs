using AutoMapper;
using HouseholdManager.Application.DTOs.Common;
using HouseholdManager.Application.DTOs.User;
using HouseholdManager.Application.Interfaces.Repositories;
using HouseholdManager.Application.Interfaces.Services;
using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Enums;
using HouseholdManager.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace HouseholdManager.Application.Services
{
    /// <summary>
    /// User service with Auth0 integration
    /// Manages application-specific user data, profile, and statistics
    /// NOTE: Authentication is handled by Auth0, not this service
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IHouseholdMemberRepository _memberRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IExecutionRepository _executionRepository;
        private readonly IHouseholdRepository _householdRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;
        private readonly Interfaces.ExternalServices.IAuth0ManagementApiClient? _auth0Client;

        public UserService(
            IUserRepository userRepository,
            IHouseholdMemberRepository memberRepository,
            ITaskRepository taskRepository,
            IExecutionRepository executionRepository,
            IHouseholdRepository householdRepository,
            IMapper mapper,
            ILogger<UserService> logger,
            Interfaces.ExternalServices.IAuth0ManagementApiClient? auth0Client = null)
        {
            _userRepository = userRepository;
            _memberRepository = memberRepository;
            _taskRepository = taskRepository;
            _executionRepository = executionRepository;
            _householdRepository = householdRepository;
            _mapper = mapper;
            _logger = logger;
            _auth0Client = auth0Client;
        }

        #region User Queries

        public async Task<UserDto?> GetUserByIdAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            return user == null ? null : _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto?> GetUserByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
            return user == null ? null : _mapper.Map<UserDto>(user);
        }

        public async Task<IReadOnlyList<UserDto>> GetAllUsersAsync(
            CancellationToken cancellationToken = default)
        {
            var users = await _userRepository.GetAllUsersAsync(cancellationToken);
            return _mapper.Map<IReadOnlyList<UserDto>>(users);
        }

        public async Task<IReadOnlyList<UserDto>> GetHouseholdUsersAsync(
            Guid householdId,
            CancellationToken cancellationToken = default)
        {
            var users = await _userRepository.GetHouseholdUsersAsync(householdId, cancellationToken);
            return _mapper.Map<IReadOnlyList<UserDto>>(users);
        }

        public async Task<IReadOnlyList<UserDto>> SearchUsersAsync(
            string searchTerm,
            CancellationToken cancellationToken = default)
        {
            var users = await _userRepository.SearchUsersAsync(searchTerm, cancellationToken);
            return _mapper.Map<IReadOnlyList<UserDto>>(users);
        }

        #endregion

        #region Profile Management

        public async Task<UserProfileDto> GetUserProfileAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new NotFoundException("User", userId);

            var dto = new UserProfileDto
            {
                User = _mapper.Map<UserDto>(user),
                Stats = await GetUserDashboardStatsAsync(userId, cancellationToken),
                Households = _mapper.Map<List<UserHouseholdDto>>(user.HouseholdMemberships)
            };

            // Set IsCurrent flag
            foreach (var household in dto.Households)
            {
                household.IsCurrent = household.HouseholdId == user.CurrentHouseholdId;
            }

            return dto;
        }

        public async Task<UserDto> UpdateProfileAsync(
            string userId,
            UpdateProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new NotFoundException("User", userId);

            // Update profile fields in local database
            await _userRepository.UpdateProfileAsync(
                userId,
                request.FirstName,
                request.LastName,
                cancellationToken);

            // Sync name to Auth0 (if Auth0 client is available)
            if (_auth0Client != null && (!string.IsNullOrWhiteSpace(request.FirstName) || !string.IsNullOrWhiteSpace(request.LastName)))
            {
                var fullName = $"{request.FirstName} {request.LastName}".Trim();
                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    try
                    {
                        await _auth0Client.UpdateUserNameAsync(userId, fullName);
                        _logger.LogInformation("Synced name to Auth0 for user {UserId}: '{FullName}'", userId, fullName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to sync name to Auth0 for user {UserId}. Profile still updated in local database.", userId);
                        // Don't fail the whole operation if Auth0 sync fails
                    }
                }
            }

            // Fetch updated user
            user = await _userRepository.GetByIdAsync(userId, cancellationToken);

            _logger.LogInformation("Updated profile for user {UserId}", userId);

            return _mapper.Map<UserDto>(user!);
        }

        #endregion

        #region Current Household Management

        public async Task SetCurrentHouseholdAsync(
            string userId,
            SetCurrentHouseholdRequest request,
            CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new NotFoundException("User", userId);

            // Validate user is member of the household if setting one
            if (request.HouseholdId.HasValue)
            {
                var isMember = await _memberRepository.IsUserMemberAsync(
                    request.HouseholdId.Value, userId, cancellationToken);

                if (!isMember)
                    throw new ForbiddenException(
                        "You must be a member of the household to set it as current");
            }

            await _userRepository.SetCurrentHouseholdAsync(
                userId,
                request.HouseholdId,
                cancellationToken);

            _logger.LogInformation(
                "Set current household {HouseholdId} for user {UserId}",
                request.HouseholdId,
                userId);
        }

        public async Task<Guid?> GetCurrentHouseholdIdAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new NotFoundException("User", userId);

            return user.CurrentHouseholdId;
        }

        #endregion

        #region User Statistics

        public async Task<UserDashboardStats> GetUserDashboardStatsAsync(
            string userId,
            CancellationToken cancellationToken = default)
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
                // Get active tasks assigned to user
                var userTasks = await _taskRepository.GetByAssignedUserIdAsync(userId, cancellationToken);
                stats.ActiveTasks = userTasks.Count(t => t.IsActive);

                // Get executions this week across all households
                var thisWeekExecutions = new List<TaskExecution>();
                foreach (var householdId in householdIds)
                {
                    var executions = await _executionRepository
                        .GetUserExecutionsThisWeekAsync(userId, householdId, cancellationToken);
                    thisWeekExecutions.AddRange(executions);
                }

                stats.CompletedTasksThisWeek = thisWeekExecutions.Count;
                stats.LastActivity = thisWeekExecutions
                    .OrderByDescending(e => e.CompletedAt)
                    .FirstOrDefault()?.CompletedAt;
            }

            return stats;
        }

        public async Task<Dictionary<string, object>> GetUserActivitySummaryAsync(
            string userId,
            CancellationToken cancellationToken = default)
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

        #endregion

        #region System Admin Operations

        public async Task SetSystemRoleAsync(
            string userId,
            SystemRole role,
            string requestingUserId,
            CancellationToken cancellationToken = default)
        {
            // Only SystemAdmin can change roles
            await ValidateSystemAdminAccessAsync(requestingUserId, cancellationToken);

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new NotFoundException("User", userId);

            // Prevent removing the last SystemAdmin
            if (user.Role == SystemRole.SystemAdmin && role != SystemRole.SystemAdmin)
            {
                var adminCount = (await _userRepository.GetAllUsersAsync(cancellationToken))
                    .Count(u => u.Role == SystemRole.SystemAdmin);

                if (adminCount <= 1)
                    throw new ValidationException(
                        "Cannot remove the last system administrator");
            }

            await _userRepository.SetSystemRoleAsync(userId, role, cancellationToken);

            _logger.LogWarning(
                "SystemAdmin {RequestingUserId} changed role of user {UserId} to {Role}",
                requestingUserId,
                userId,
                role);
        }

        #endregion

        #region User Sync (Auth0 Integration)

        /// <summary>
        /// Synchronize user from Auth0 to local database
        /// Called when user logs in or signs up
        /// </summary>
        public async Task<UserDto> SyncUserFromAuth0Async(
            string auth0UserId,
            string email,
            string? firstName = null,
            string? lastName = null,
            string? profilePictureUrl = null,
            CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.UpsertUserAsync(
                auth0UserId,
                email,
                firstName,
                lastName,
                profilePictureUrl,
                cancellationToken);

            _logger.LogInformation(
                "Synced user from Auth0: {Email} (Auth0 ID: {Auth0Id})",
                email,
                auth0UserId);

            return _mapper.Map<UserDto>(user);
        }

        #endregion

        #region Validation Helpers

        public async Task ValidateUserAccessAsync(
            string userId,
            string requestingUserId,
            CancellationToken cancellationToken = default)
        {
            // Users can access their own profile, system admins can access any profile
            if (userId != requestingUserId &&
                !await IsSystemAdminAsync(requestingUserId, cancellationToken))
            {
                throw new ForbiddenException(
                    $"You do not have permission to access user '{userId}'");
            }
        }

        public async Task ValidateSystemAdminAccessAsync(
            string requestingUserId,
            CancellationToken cancellationToken = default)
        {
            if (!await IsSystemAdminAsync(requestingUserId, cancellationToken))
            {
                throw new ForbiddenException("System administrator access required");
            }
        }

        public async Task<bool> IsSystemAdminAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return await _userRepository.IsSystemAdminAsync(userId, cancellationToken);
        }

        #endregion

        #region Account Deletion

        public async Task<AccountDeletionCheckResult> CanDeleteAccountAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Checking if user {UserId} can delete account", userId);

            // SystemAdmin cannot delete their account
            if (await IsSystemAdminAsync(userId, cancellationToken))
            {
                return new AccountDeletionCheckResult
                {
                    CanDelete = false,
                    OwnedHouseholdsCount = 0,
                    MemberHouseholdsCount = 0,
                    AssignedTasksCount = 0,
                    OwnedHouseholdNames = new List<string>(),
                    Message = "System administrators cannot delete their account. Please contact another administrator."
                };
            }

            var (soleOwnedCount, memberCount, assignedTasksCount, soleOwnedHouseholdNames) =
                await _userRepository.GetUserDeletionInfoAsync(userId, cancellationToken);

            // User can always delete account now - sole-owner households will be auto-deleted
            var canDelete = true;

            string? message = null;
            if (soleOwnedCount > 0)
            {
                message = $"Warning: {soleOwnedCount} household(s) where you are the sole owner will be permanently deleted: {string.Join(", ", soleOwnedHouseholdNames)}";
            }

            var result = new AccountDeletionCheckResult
            {
                CanDelete = canDelete,
                OwnedHouseholdsCount = soleOwnedCount,
                MemberHouseholdsCount = memberCount,
                AssignedTasksCount = assignedTasksCount,
                OwnedHouseholdNames = soleOwnedHouseholdNames,
                Message = message
            };

            _logger.LogInformation(
                "User {UserId} deletion check: CanDelete={CanDelete}, SoleOwnedHouseholds={SoleOwnedCount}, MemberHouseholds={MemberCount}, AssignedTasks={TasksCount}",
                userId, canDelete, soleOwnedCount, memberCount, assignedTasksCount);

            return result;
        }

        public async Task DeleteAccountAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("Starting account deletion for user {UserId}", userId);

            // 0. Prevent SystemAdmin from deleting their account
            if (await IsSystemAdminAsync(userId, cancellationToken))
            {
                throw new ValidationException("System administrators cannot delete their account. Please contact another administrator.");
            }

            // 1. Get sole-owner households to delete
            var soleOwnerHouseholdIds = await _userRepository.GetSoleOwnerHouseholdIdsAsync(userId, cancellationToken);

            if (soleOwnerHouseholdIds.Any())
            {
                _logger.LogInformation("User {UserId} is sole owner of {Count} household(s), will auto-delete",
                    userId, soleOwnerHouseholdIds.Count);

                // Delete each sole-owner household
                foreach (var householdId in soleOwnerHouseholdIds)
                {
                    var household = await _householdRepository.GetByIdAsync(householdId, cancellationToken);
                    if (household != null)
                    {
                        await _householdRepository.DeleteAsync(household, cancellationToken);
                        _logger.LogInformation("Deleted household {HouseholdId} ({Name}) for user {UserId}",
                            householdId, household.Name, userId);
                    }
                }
            }

            // 2. Verify user exists
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new NotFoundException("User", userId);

            try
            {
                // 3. Delete from database first
                // This will:
                // - CASCADE delete HouseholdMember records
                // - SET NULL for HouseholdTask.AssignedUserId
                // - RESTRICT TaskExecution (but we're not deleting those)
                await _userRepository.DeleteUserAsync(userId, cancellationToken);

                _logger.LogInformation("User {UserId} deleted from database", userId);

                // 4. Delete from Auth0
                if (_auth0Client != null)
                {
                    await _auth0Client.DeleteUserAsync(userId);
                    _logger.LogInformation("User {UserId} deleted from Auth0", userId);
                }
                else
                {
                    _logger.LogWarning("Auth0 client not available, user {UserId} not deleted from Auth0", userId);
                }

                _logger.LogWarning("Account deletion completed for user {UserId} ({Email})", userId, user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete account for user {UserId}", userId);
                throw new InvalidOperationException("Failed to delete account. Please try again or contact support.", ex);
            }
        }

        #endregion
    }
}
