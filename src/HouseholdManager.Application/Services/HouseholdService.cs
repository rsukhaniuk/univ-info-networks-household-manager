using AutoMapper;
using HouseholdManager.Application.DTOs.Household;
using HouseholdManager.Application.DTOs.Room;
using HouseholdManager.Application.DTOs.Task;
using HouseholdManager.Application.Interfaces.Repositories;
using HouseholdManager.Application.Interfaces.Services;
using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Enums;
using HouseholdManager.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace HouseholdManager.Application.Services
{
    /// <summary>
    /// Implementation of household service with business logic
    /// </summary>
    public class HouseholdService : IHouseholdService
    {
        private readonly IHouseholdRepository _householdRepository;
        private readonly IHouseholdMemberRepository _memberRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRoomRepository _roomRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IExecutionRepository _executionRepository;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<HouseholdService> _logger;
        private readonly IMapper _mapper;

        public HouseholdService(
            IHouseholdRepository householdRepository,
            IHouseholdMemberRepository memberRepository,
            IUserRepository userRepository,
            IRoomRepository roomRepository,
            ITaskRepository taskRepository,
            IExecutionRepository executionRepository,
            IFileUploadService fileUploadService,
            IMapper mapper,
            ILogger<HouseholdService> logger)
        {
            _householdRepository = householdRepository;
            _memberRepository = memberRepository;
            _userRepository = userRepository;
            _roomRepository = roomRepository;
            _taskRepository = taskRepository;
            _executionRepository = executionRepository;
            _fileUploadService = fileUploadService;
            _mapper = mapper;
            _logger = logger;
        }

        // Basic CRUD operations
        public async Task<HouseholdDto> CreateHouseholdAsync(
            UpsertHouseholdRequest request,
            string ownerId,
            CancellationToken cancellationToken = default)
        {
            var household = _mapper.Map<Household>(request);

            // Set invite code expiration (24 hours from now)
            household.InviteCodeExpiresAt = DateTime.UtcNow.AddHours(24);

            // Create household
            var createdHousehold = await _householdRepository.AddAsync(household, cancellationToken);

            // Add creator as owner
            await _memberRepository.AddAsync(new HouseholdMember
            {
                HouseholdId = createdHousehold.Id,
                UserId = ownerId,
                Role = HouseholdRole.Owner,
                JoinedAt = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogInformation("Created household {HouseholdId} with owner {UserId}",
                createdHousehold.Id, ownerId);

            return _mapper.Map<HouseholdDto>(createdHousehold);
        }

        public async Task<HouseholdDto?> GetHouseholdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var household = await _householdRepository.GetByIdAsync(id, cancellationToken);
            return household == null ? null : _mapper.Map<HouseholdDto>(household);
        }

        public async Task<HouseholdDetailsDto?> GetHouseholdWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var household = await _householdRepository.GetByIdWithMembersAsync(id, cancellationToken);
            if (household == null) return null;

            var dto = _mapper.Map<HouseholdDetailsDto>(household);
            dto.IsOwner = false; // Set by controller based on current user

            return dto;
        }

        public async Task<IReadOnlyList<HouseholdDto>> GetAllHouseholdsAsync(CancellationToken cancellationToken = default)
        {
            var households = await _householdRepository.GetAllWithMembersAsync(cancellationToken);
            return _mapper.Map<IReadOnlyList<HouseholdDto>>(households);
        }

        public async Task<IReadOnlyList<HouseholdDto>> GetUserHouseholdsAsync(string userId, CancellationToken cancellationToken = default)
        {
            var households = await _householdRepository.GetUserHouseholdsAsync(userId, cancellationToken);
            var householdDtos = _mapper.Map<IReadOnlyList<HouseholdDto>>(households);

            // Асинхронно заповнюємо роль для кожного household
            foreach (var dto in householdDtos)
            {
                dto.Role = await _memberRepository.GetUserRoleAsync(dto.Id, userId, cancellationToken);
            }

            return householdDtos;
        }

        public async Task<HouseholdDto> UpdateHouseholdAsync(
            Guid id,
            UpsertHouseholdRequest request,
            string requestingUserId,
            CancellationToken cancellationToken = default)
        {
            await ValidateOwnerAccessAsync(id, requestingUserId, cancellationToken);

            var household = await _householdRepository.GetByIdAsync(id, cancellationToken);
            if (household == null)
                throw new NotFoundException(nameof(Household), id);

            // Update properties from request
            household.Name = request.Name;
            household.Description = request.Description;

            await _householdRepository.UpdateAsync(household, cancellationToken);

            _logger.LogInformation("Updated household {HouseholdId}", household.Id);

            return _mapper.Map<HouseholdDto>(household);
        }

        public async Task DeleteHouseholdAsync(Guid id, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateOwnerAccessAsync(id, requestingUserId, cancellationToken);

            // Delete all tasks and their executions first (due to Restrict on Task->Room)
            var tasks = await _taskRepository.GetByHouseholdIdAsync(id, cancellationToken);
            foreach (var task in tasks)
            {
                // Delete task executions
                var executions = await _executionRepository.GetByTaskIdAsync(task.Id, cancellationToken);
                foreach (var execution in executions)
                {
                    // Delete execution photo if exists
                    if (!string.IsNullOrEmpty(execution.PhotoPath))
                    {
                        await _fileUploadService.DeleteFileAsync(execution.PhotoPath, cancellationToken);
                    }
                    await _executionRepository.DeleteAsync(execution, cancellationToken);
                }

                // Delete the task
                await _taskRepository.DeleteAsync(task, cancellationToken);
            }
            _logger.LogInformation("Deleted {TaskCount} tasks with their executions for household {HouseholdId}", tasks.Count, id);

            // Delete all room photos
            var rooms = await _roomRepository.GetByHouseholdIdAsync(id, cancellationToken);
            foreach (var room in rooms)
            {
                if (!string.IsNullOrEmpty(room.PhotoPath))
                {
                    await _fileUploadService.DeleteFileAsync(room.PhotoPath, cancellationToken);
                }
            }

            // Now delete the household (will cascade delete rooms and members)
            await _householdRepository.DeleteByIdAsync(id, cancellationToken);
            _logger.LogInformation("Deleted household {HouseholdId} by user {UserId}", id, requestingUserId);
        }

        // Invite operations
        public async Task<HouseholdDto?> GetHouseholdByInviteCodeAsync(Guid inviteCode, CancellationToken cancellationToken = default)
        {
            var household = await _householdRepository.GetByInviteCodeAsync(inviteCode, cancellationToken);
            return household == null ? null : _mapper.Map<HouseholdDto>(household);
        }

        public async Task<Guid> RegenerateInviteCodeAsync(Guid householdId, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateOwnerAccessAsync(householdId, requestingUserId, cancellationToken);

            var household = await _householdRepository.GetByIdAsync(householdId, cancellationToken);
            if (household == null)
                throw new NotFoundException(nameof(Household), householdId);

            Guid newInviteCode;
            do
            {
                newInviteCode = Guid.NewGuid();
            } while (!await _householdRepository.IsInviteCodeUniqueAsync(newInviteCode, cancellationToken));

            household.InviteCode = newInviteCode;
            // Reset expiration to 24 hours from now
            household.InviteCodeExpiresAt = DateTime.UtcNow.AddHours(24);

            await _householdRepository.UpdateAsync(household, cancellationToken);

            _logger.LogInformation("Regenerated invite code for household {HouseholdId} with new expiration", householdId);
            return newInviteCode;
        }

        public async Task<HouseholdDto> JoinHouseholdAsync(
            JoinHouseholdRequest request,
            string userId,
            CancellationToken cancellationToken = default)
        {
            var household = await _householdRepository.GetByInviteCodeAsync(request.InviteCode, cancellationToken);
            if (household == null)
                throw new NotFoundException("Invalid invite code");

            // Check if invite code has expired
            if (household.InviteCodeExpiresAt.HasValue && household.InviteCodeExpiresAt.Value < DateTime.UtcNow)
            {
                _logger.LogWarning("Attempt to join household {HouseholdId} with expired invite code", household.Id);
                throw new ValidationException("Invite code has expired. Please request a new invite code from the household owner.");
            }

            // Check if user is already a member
            if (await _memberRepository.IsUserMemberAsync(household.Id, userId, cancellationToken))
                throw new ValidationException("User is already a member of this household");

            await _memberRepository.AddAsync(new HouseholdMember
            {
                HouseholdId = household.Id,
                UserId = userId,
                Role = HouseholdRole.Member,
                JoinedAt = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogInformation("User {UserId} joined household {HouseholdId}", userId, household.Id);

            return _mapper.Map<HouseholdDto>(household);
        }

        // Member management
        public async Task<HouseholdMemberDto> AddMemberAsync(
            Guid householdId,
            string userId,
            HouseholdRole role,
            string requestingUserId,
            CancellationToken cancellationToken = default)
        {
            await ValidateOwnerAccessAsync(householdId, requestingUserId, cancellationToken);

            if (await _memberRepository.IsUserMemberAsync(householdId, userId, cancellationToken))
                throw new ValidationException("User is already a member of this household");

            var member = await _memberRepository.AddAsync(new HouseholdMember
            {
                HouseholdId = householdId,
                UserId = userId,
                Role = role,
                JoinedAt = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogInformation("Added user {UserId} as {Role} to household {HouseholdId}",
                userId, role, householdId);

            // Load navigation properties for mapping
            member = await _memberRepository.GetMemberAsync(householdId, userId, cancellationToken);

            return _mapper.Map<HouseholdMemberDto>(member);
        }

        public async Task RemoveMemberAsync(Guid householdId, string userId, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateOwnerAccessAsync(householdId, requestingUserId, cancellationToken);

            var member = await _memberRepository.GetMemberAsync(householdId, userId, cancellationToken);
            if (member == null)
                throw new NotFoundException("User is not a member of this household");

            // Check if this is the last owner
            if (member.Role == HouseholdRole.Owner)
            {
                var ownerCount = await _memberRepository.GetOwnerCountAsync(householdId, cancellationToken);
                if (ownerCount <= 1)
                    throw new ValidationException("Cannot remove the last owner of the household");
            }

            await _memberRepository.DeleteAsync(member, cancellationToken);
            _logger.LogInformation("Owner {RequestingUserId} removed user {UserId} from household {HouseholdId}",
                requestingUserId, userId, householdId);
        }

        public async Task LeaveHouseholdAsync(Guid householdId, string userId, CancellationToken cancellationToken = default)
        {
            var member = await _memberRepository.GetMemberAsync(householdId, userId, cancellationToken);
            if (member == null)
                throw new NotFoundException("User is not a member of this household");

            // If user is an owner, check if they're the last owner
            if (member.Role == HouseholdRole.Owner)
            {
                var ownerCount = await _memberRepository.GetOwnerCountAsync(householdId, cancellationToken);
                if (ownerCount <= 1)
                    throw new ValidationException("Cannot leave household as the last owner. Transfer ownership or delete the household instead.");
            }

            // Clear current household if this was the user's current household
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user?.CurrentHouseholdId == householdId)
            {
                await _userRepository.SetCurrentHouseholdAsync(userId, null, cancellationToken);
            }

            await _memberRepository.DeleteAsync(member, cancellationToken);
            _logger.LogInformation("User {UserId} left household {HouseholdId}", userId, householdId);
        }

        public async Task<bool> IsUserMemberAsync(Guid householdId, string userId, CancellationToken cancellationToken = default)
        {
            // Check if user is SystemAdmin - they have access to all households
            if (await _userRepository.IsSystemAdminAsync(userId, cancellationToken))
                return true;

            return await _memberRepository.IsUserMemberAsync(householdId, userId, cancellationToken);
        }

        public async Task<bool> IsUserOwnerAsync(Guid householdId, string userId, CancellationToken cancellationToken = default)
        {
            // Check if user is SystemAdmin - they have owner access to all households
            if (await _userRepository.IsSystemAdminAsync(userId, cancellationToken))
                return true;

            var role = await _memberRepository.GetUserRoleAsync(householdId, userId, cancellationToken);
            return role == HouseholdRole.Owner;
        }

        public async Task<HouseholdRole?> GetUserRoleAsync(Guid householdId, string userId, CancellationToken cancellationToken = default)
        {
            return await _memberRepository.GetUserRoleAsync(householdId, userId, cancellationToken);
        }

        // Permission validation
        public async Task ValidateUserAccessAsync(Guid householdId, string userId, CancellationToken cancellationToken = default)
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
            if (!await IsUserMemberAsync(householdId, userId, cancellationToken))
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
            if (!await IsUserOwnerAsync(householdId, userId, cancellationToken))
                throw ForbiddenException.ForAction("modify", "household");
        }
    }
}
