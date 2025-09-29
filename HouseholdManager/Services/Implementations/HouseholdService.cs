using HouseholdManager.Models.Entities;
using HouseholdManager.Models.Enums;
using HouseholdManager.Repositories.Interfaces;
using HouseholdManager.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace HouseholdManager.Services.Implementations
{
    /// <summary>
    /// Implementation of household service with business logic
    /// </summary>
    public class HouseholdService : IHouseholdService
    {
        private readonly IHouseholdRepository _householdRepository;
        private readonly IHouseholdMemberRepository _memberRepository;
        private readonly ILogger<HouseholdService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public HouseholdService(
            IHouseholdRepository householdRepository,
            IHouseholdMemberRepository memberRepository,
            UserManager<ApplicationUser> userManager,
            ILogger<HouseholdService> logger)
        {
            _householdRepository = householdRepository;
            _memberRepository = memberRepository;
            _userManager = userManager;
            _logger = logger;
        }

        // Basic CRUD operations
        public async Task<Household> CreateHouseholdAsync(string name, string? description, string ownerId, CancellationToken cancellationToken = default)
        {
            var household = new Household
            {
                Name = name,
                Description = description,
                InviteCode = Guid.NewGuid()
            };

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

            return createdHousehold;
        }

        public async Task<Household?> GetHouseholdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _householdRepository.GetByIdAsync(id, cancellationToken);
        }

        public async Task<Household?> GetHouseholdWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _householdRepository.GetByIdWithMembersAsync(id, cancellationToken);
        }

        public async Task<IReadOnlyList<Household>> GetAllHouseholdsAsync(CancellationToken cancellationToken = default)
        {
            return await _householdRepository.GetAllWithMembersAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Household>> GetUserHouseholdsAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _householdRepository.GetUserHouseholdsAsync(userId, cancellationToken);
        }

        public async Task UpdateHouseholdAsync(Household household, CancellationToken cancellationToken = default)
        {
            await _householdRepository.UpdateAsync(household, cancellationToken);
            _logger.LogInformation("Updated household {HouseholdId}", household.Id);
        }

        public async Task DeleteHouseholdAsync(Guid id, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateOwnerAccessAsync(id, requestingUserId, cancellationToken);

            await _householdRepository.DeleteByIdAsync(id, cancellationToken);
            _logger.LogInformation("Deleted household {HouseholdId} by user {UserId}", id, requestingUserId);
        }

        // Invite operations
        public async Task<Household?> GetHouseholdByInviteCodeAsync(Guid inviteCode, CancellationToken cancellationToken = default)
        {
            return await _householdRepository.GetByInviteCodeAsync(inviteCode, cancellationToken);
        }

        public async Task<Guid> RegenerateInviteCodeAsync(Guid householdId, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateOwnerAccessAsync(householdId, requestingUserId, cancellationToken);

            var household = await _householdRepository.GetByIdAsync(householdId, cancellationToken);
            if (household == null)
                throw new InvalidOperationException("Household not found");

            Guid newInviteCode;
            do
            {
                newInviteCode = Guid.NewGuid();
            } while (!await _householdRepository.IsInviteCodeUniqueAsync(newInviteCode, cancellationToken));

            household.InviteCode = newInviteCode;
            await _householdRepository.UpdateAsync(household, cancellationToken);

            _logger.LogInformation("Regenerated invite code for household {HouseholdId}", householdId);
            return newInviteCode;
        }

        public async Task<HouseholdMember> JoinHouseholdAsync(Guid inviteCode, string userId, CancellationToken cancellationToken = default)
        {
            var household = await _householdRepository.GetByInviteCodeAsync(inviteCode, cancellationToken);
            if (household == null)
                throw new InvalidOperationException("Invalid invite code");

            // Check if user is already a member
            if (await _memberRepository.IsUserMemberAsync(household.Id, userId, cancellationToken))
                throw new InvalidOperationException("User is already a member of this household");

            var member = await _memberRepository.AddAsync(new HouseholdMember
            {
                HouseholdId = household.Id,
                UserId = userId,
                Role = HouseholdRole.Member,
                JoinedAt = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogInformation("User {UserId} joined household {HouseholdId}", userId, household.Id);
            return member;
        }

        // Member management
        public async Task<HouseholdMember> AddMemberAsync(Guid householdId, string userId, HouseholdRole role, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateOwnerAccessAsync(householdId, requestingUserId, cancellationToken);

            if (await _memberRepository.IsUserMemberAsync(householdId, userId, cancellationToken))
                throw new InvalidOperationException("User is already a member of this household");

            var member = await _memberRepository.AddAsync(new HouseholdMember
            {
                HouseholdId = householdId,
                UserId = userId,
                Role = role,
                JoinedAt = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogInformation("Added user {UserId} as {Role} to household {HouseholdId}",
                userId, role, householdId);

            return member;
        }

        public async Task RemoveMemberAsync(Guid householdId, string userId, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateOwnerAccessAsync(householdId, requestingUserId, cancellationToken);

            var member = await _memberRepository.GetMemberAsync(householdId, userId, cancellationToken);
            if (member == null)
                throw new InvalidOperationException("User is not a member of this household");

            // Check if this is the last owner
            if (member.Role == HouseholdRole.Owner)
            {
                var ownerCount = await _memberRepository.GetOwnerCountAsync(householdId, cancellationToken);
                if (ownerCount <= 1)
                    throw new InvalidOperationException("Cannot remove the last owner of the household");
            }

            await _memberRepository.DeleteAsync(member, cancellationToken);
            _logger.LogInformation("Owner {RequestingUserId} removed user {UserId} from household {HouseholdId}",
                requestingUserId, userId, householdId);
        }

        public async Task LeaveHouseholdAsync(Guid householdId, string userId, CancellationToken cancellationToken = default)
        {
            var member = await _memberRepository.GetMemberAsync(householdId, userId, cancellationToken);
            if (member == null)
                throw new InvalidOperationException("User is not a member of this household");

            // If user is an owner, check if they're the last owner
            if (member.Role == HouseholdRole.Owner)
            {
                var ownerCount = await _memberRepository.GetOwnerCountAsync(householdId, cancellationToken);
                if (ownerCount <= 1)
                    throw new InvalidOperationException("Cannot leave household as the last owner. Transfer ownership or delete the household first.");
            }

            // Clear current household if this was the user's current household
            var user = await _memberRepository.GetMemberAsync(householdId, userId, cancellationToken);
            if (user?.User?.CurrentHouseholdId == householdId)
            {
                // This would require UserService to clear current household
                // For now, we'll leave it as is and handle in the controller
            }

            await _memberRepository.DeleteAsync(member, cancellationToken);
            _logger.LogInformation("User {UserId} left household {HouseholdId}", userId, householdId);
        }

        public async Task<bool> IsUserMemberAsync(Guid householdId, string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user?.IsSystemAdmin == true)
            {
                return true;
            }

            return await _memberRepository.IsUserMemberAsync(householdId, userId, cancellationToken);
        }

        public async Task<bool> IsUserOwnerAsync(Guid householdId, string userId, CancellationToken cancellationToken = default)
        {

            var user = await _userManager.FindByIdAsync(userId);
            if (user?.IsSystemAdmin == true)
            {
                return true; 
            }

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

            var user = await _userManager.FindByIdAsync(userId);
            if (user?.IsSystemAdmin == true)
            {
                return;
            }

            if (!await IsUserMemberAsync(householdId, userId, cancellationToken))
                throw new UnauthorizedAccessException("User is not a member of this household");
        }

        public async Task ValidateOwnerAccessAsync(Guid householdId, string userId, CancellationToken cancellationToken = default)
        {
            if (!await IsUserOwnerAsync(householdId, userId, cancellationToken))
                throw new UnauthorizedAccessException("User is not an owner of this household");

        }
    }
}
