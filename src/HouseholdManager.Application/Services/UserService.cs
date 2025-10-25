using AutoMapper;
using HouseholdManager.Application.DTOs.Common;
using HouseholdManager.Application.DTOs.User;
using HouseholdManager.Application.Interfaces.Repositories;
using HouseholdManager.Application.Interfaces.Services;
using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Enums;
using HouseholdManager.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace HouseholdManager.Services.Implementations
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
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository userRepository,
            IHouseholdMemberRepository memberRepository,
            ITaskRepository taskRepository,
            IExecutionRepository executionRepository,
            IMapper mapper,
            ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _memberRepository = memberRepository;
            _taskRepository = taskRepository;
            _executionRepository = executionRepository;
            _mapper = mapper;
            _logger = logger;
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

            // Update profile fields
            await _userRepository.UpdateProfileAsync(
                userId,
                request.FirstName,
                request.LastName,
                cancellationToken);

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
    }
}
