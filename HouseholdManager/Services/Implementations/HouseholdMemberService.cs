using HouseholdManager.Models;
using HouseholdManager.Models.Enums;
using HouseholdManager.Repositories.Interfaces;
using HouseholdManager.Services.Interfaces;

namespace HouseholdManager.Services.Implementations
{
    /// <summary>
    /// Implementation of household member service with business logic
    /// </summary>
    public class HouseholdMemberService : IHouseholdMemberService
    {
        private readonly IHouseholdMemberRepository _memberRepository;
        private readonly IHouseholdRepository _householdRepository;
        private readonly ILogger<HouseholdMemberService> _logger;

        public HouseholdMemberService(
            IHouseholdMemberRepository memberRepository,
            IHouseholdRepository householdRepository,
            ILogger<HouseholdMemberService> logger)
        {
            _memberRepository = memberRepository;
            _householdRepository = householdRepository;
            _logger = logger;
        }

        // Member queries
        public async Task<IReadOnlyList<HouseholdMember>> GetHouseholdMembersAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            return await _memberRepository.GetByHouseholdIdAsync(householdId, cancellationToken);
        }

        public async Task<IReadOnlyList<HouseholdMember>> GetUserMembershipsAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _memberRepository.GetByUserIdAsync(userId, cancellationToken);
        }

        public async Task<HouseholdMember?> GetMemberAsync(Guid householdId, string userId, CancellationToken cancellationToken = default)
        {
            return await _memberRepository.GetMemberAsync(householdId, userId, cancellationToken);
        }

        public async Task<IReadOnlyList<HouseholdMember>> GetMembersByRoleAsync(Guid householdId, HouseholdRole role, CancellationToken cancellationToken = default)
        {
            return await _memberRepository.GetMembersByRoleAsync(householdId, role, cancellationToken);
        }

        // Role management
        public async Task UpdateMemberRoleAsync(Guid householdId, string userId, HouseholdRole newRole, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateOwnerAccessAsync(householdId, requestingUserId, cancellationToken);

            var member = await _memberRepository.GetMemberAsync(householdId, userId, cancellationToken);
            if (member == null)
                throw new InvalidOperationException("User is not a member of this household");

            // Prevent self-demotion if user is the last owner
            if (requestingUserId == userId && member.Role == HouseholdRole.Owner && newRole != HouseholdRole.Owner)
            {
                var ownerCount = await _memberRepository.GetOwnerCountAsync(householdId, cancellationToken);
                if (ownerCount <= 1)
                    throw new InvalidOperationException("Cannot demote yourself as the last owner of the household");
            }

            // If demoting from owner, check if there will be at least one owner left
            if (member.Role == HouseholdRole.Owner && newRole != HouseholdRole.Owner)
            {
                var ownerCount = await _memberRepository.GetOwnerCountAsync(householdId, cancellationToken);
                if (ownerCount <= 1)
                    throw new InvalidOperationException("Cannot demote the last owner of the household");
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

        //public async Task<Dictionary<string, int>> GetMemberTaskCountsAsync(Guid householdId, CancellationToken cancellationToken = default)
        //{
            
        //}

        // Validation
        public async Task ValidateMemberAccessAsync(Guid householdId, string userId, CancellationToken cancellationToken = default)
        {
            if (!await _memberRepository.IsUserMemberAsync(householdId, userId, cancellationToken))
                throw new UnauthorizedAccessException("User is not a member of this household");
        }

        public async Task ValidateOwnerAccessAsync(Guid householdId, string userId, CancellationToken cancellationToken = default)
        {
            var role = await _memberRepository.GetUserRoleAsync(householdId, userId, cancellationToken);
            if (role != HouseholdRole.Owner)
                throw new UnauthorizedAccessException("User is not an owner of this household");
        }
    }
}
