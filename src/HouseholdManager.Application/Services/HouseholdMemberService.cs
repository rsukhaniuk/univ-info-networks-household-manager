using AutoMapper;
using HouseholdManager.Application.DTOs.Household;
using HouseholdManager.Application.Interfaces.Repositories;
using HouseholdManager.Application.Interfaces.Services;
using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Enums;
using HouseholdManager.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace HouseholdManager.Application.Services
{
    /// <summary>
    /// Implementation of household member service with business logic
    /// </summary>
    public class HouseholdMemberService : IHouseholdMemberService
    {
        private readonly IHouseholdMemberRepository _memberRepository;
        private readonly IHouseholdRepository _householdRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<HouseholdMemberService> _logger;
        private readonly IMapper _mapper;

        public HouseholdMemberService(
            IHouseholdMemberRepository memberRepository,
            IHouseholdRepository householdRepository,
            ITaskRepository taskRepository,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<HouseholdMemberService> logger)
        {
            _memberRepository = memberRepository;
            _householdRepository = householdRepository;
            _taskRepository = taskRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
        }

        // Member queries
        public async Task<IReadOnlyList<HouseholdMemberDto>> GetHouseholdMembersAsync(
            Guid householdId,
            CancellationToken cancellationToken = default)
        {
            var members = await _memberRepository.GetByHouseholdIdAsync(householdId, cancellationToken);
            return _mapper.Map<IReadOnlyList<HouseholdMemberDto>>(members);
        }

        public async Task<IReadOnlyList<HouseholdMemberDto>> GetUserMembershipsAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            var memberships = await _memberRepository.GetByUserIdAsync(userId, cancellationToken);
            return _mapper.Map<IReadOnlyList<HouseholdMemberDto>>(memberships);
        }

        public async Task<HouseholdMemberDto?> GetMemberAsync(
            Guid householdId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            var member = await _memberRepository.GetMemberAsync(householdId, userId, cancellationToken);
            return member == null ? null : _mapper.Map<HouseholdMemberDto>(member);
        }

        public async Task<IReadOnlyList<HouseholdMemberDto>> GetMembersByRoleAsync(
           Guid householdId,
           HouseholdRole role,
           CancellationToken cancellationToken = default)
        {
            var members = await _memberRepository.GetMembersByRoleAsync(householdId, role, cancellationToken);
            return _mapper.Map<IReadOnlyList<HouseholdMemberDto>>(members);
        }

        // Role management
        public async Task UpdateMemberRoleAsync(Guid householdId, string userId, HouseholdRole newRole, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateOwnerAccessAsync(householdId, requestingUserId, cancellationToken);

            var member = await _memberRepository.GetMemberAsync(householdId, userId, cancellationToken);
            if (member == null)
                throw new NotFoundException("User is not a member of this household");

            // Prevent self-demotion if user is the last owner
            if (requestingUserId == userId && member.Role == HouseholdRole.Owner && newRole != HouseholdRole.Owner)
            {
                var ownerCount = await _memberRepository.GetOwnerCountAsync(householdId, cancellationToken);
                if (ownerCount <= 1)
                    throw new ValidationException("Cannot demote yourself as the last owner of the household");
            }

            // If demoting from owner, check if there will be at least one owner left
            if (member.Role == HouseholdRole.Owner && newRole != HouseholdRole.Owner)
            {
                var ownerCount = await _memberRepository.GetOwnerCountAsync(householdId, cancellationToken);
                if (ownerCount <= 1)
                    throw new ValidationException("Cannot demote the last owner of the household");
            }

            await _memberRepository.UpdateRoleAsync(householdId, userId, newRole, cancellationToken);
            _logger.LogInformation("Updated member {UserId} role to {Role} in household {HouseholdId}",
                userId, newRole, householdId);
        }

        public async Task PromoteToOwnerAsync(Guid householdId, string userId, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await UpdateMemberRoleAsync(householdId, userId, HouseholdRole.Owner, requestingUserId, cancellationToken);
            _logger.LogInformation("Promoted user {UserId} to owner in household {HouseholdId}", userId, householdId);
        }

        public async Task DemoteFromOwnerAsync(Guid householdId, string userId, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await UpdateMemberRoleAsync(householdId, userId, HouseholdRole.Member, requestingUserId, cancellationToken);
            _logger.LogInformation("Demoted user {UserId} from owner in household {HouseholdId}", userId, householdId);
        }

        // Member statistics
        public async Task<int> GetMemberCountAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            return await _memberRepository.GetMemberCountAsync(householdId, cancellationToken);
        }

        public async Task<int> GetOwnerCountAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            return await _memberRepository.GetOwnerCountAsync(householdId, cancellationToken);
        }

        public async Task<Dictionary<string, int>> GetMemberTaskCountsAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            var members = await _memberRepository.GetByHouseholdIdAsync(householdId, cancellationToken);
            var taskCounts = new Dictionary<string, int>();

            // Initialize all members with 0 tasks
            foreach (var member in members)
            {
                taskCounts[member.UserId] = 0;
            }

            // Get active tasks and count by assigned user
            var activeTasks = await _taskRepository.GetActiveByHouseholdIdAsync(householdId, cancellationToken);

            foreach (var task in activeTasks.Where(t => !string.IsNullOrEmpty(t.AssignedUserId)))
            {
                if (taskCounts.ContainsKey(task.AssignedUserId!))
                {
                    taskCounts[task.AssignedUserId!]++;
                }
            }

            return taskCounts;
        }

        // Validation
        public async Task ValidateMemberAccessAsync(Guid householdId, string userId, CancellationToken cancellationToken = default)
        {
            // First, verify household exists
            var household = await _householdRepository.GetByIdAsync(householdId, cancellationToken);
            if (household == null)
                throw new NotFoundException(nameof(Household), householdId);

            // SystemAdmin always has access
            var isSystemAdmin = await _userRepository.IsSystemAdminAsync(userId, cancellationToken);
            if (isSystemAdmin)
            {
                _logger.LogInformation("SystemAdmin {UserId} accessing household {HouseholdId}", userId, householdId);
                return;
            }

            // Regular users must be members
            if (!await _memberRepository.IsUserMemberAsync(householdId, userId, cancellationToken))
                throw ForbiddenException.ForResource("Household", householdId);
        }

        public async Task ValidateOwnerAccessAsync(Guid householdId, string userId, CancellationToken cancellationToken = default)
        {
            // First, verify household exists
            var household = await _householdRepository.GetByIdAsync(householdId, cancellationToken);
            if (household == null)
                throw new NotFoundException(nameof(Household), householdId);

            // SystemAdmin always has owner-level access
            var isSystemAdmin = await _userRepository.IsSystemAdminAsync(userId, cancellationToken);
            if (isSystemAdmin)
            {
                _logger.LogInformation("SystemAdmin {UserId} performing owner action on household {HouseholdId}", userId, householdId);
                return;
            }

            // Regular users must be owners
            var role = await _memberRepository.GetUserRoleAsync(householdId, userId, cancellationToken);
            if (role != HouseholdRole.Owner)
                throw ForbiddenException.ForAction("modify", "household");
        }
    }
}
