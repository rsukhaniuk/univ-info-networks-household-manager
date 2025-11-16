using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Enums;

namespace HouseholdManager.Application.Interfaces.Repositories
{
    /// <summary>
    /// Repository interface for Household operations
    /// Extends base repository with essential household-specific methods
    /// </summary>
    public interface IHouseholdRepository : IRepository<Household>
    {
        // Essential queries with relations
        Task<Household?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Household>> GetUserHouseholdsAsync(string userId, CancellationToken cancellationToken = default);

        // Invite code operations
        Task<Household?> GetByInviteCodeAsync(Guid inviteCode, CancellationToken cancellationToken = default);
        Task<bool> IsInviteCodeUniqueAsync(Guid inviteCode, CancellationToken cancellationToken = default);

        // Member management
        Task<HouseholdMember> AddMemberAsync(Guid householdId, string userId, HouseholdRole role, CancellationToken cancellationToken = default);
        Task RemoveMemberAsync(Guid householdId, string userId, CancellationToken cancellationToken = default);
        Task<bool> IsUserMemberAsync(Guid householdId, string userId, CancellationToken cancellationToken = default);
        Task<HouseholdRole?> GetUserRoleAsync(Guid householdId, string userId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Household>> GetAllWithMembersAsync(CancellationToken cancellationToken = default);
    }
}
