using HouseholdManager.Models.Entities;
using HouseholdManager.Models.Enums;

namespace HouseholdManager.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for HouseholdMember operations
    /// Extends base repository with member-specific methods
    /// </summary>
    public interface IHouseholdMemberRepository : IRepository<HouseholdMember>
    {
        // Member queries
        Task<IReadOnlyList<HouseholdMember>> GetByHouseholdIdAsync(Guid householdId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HouseholdMember>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
        Task<HouseholdMember?> GetMemberAsync(Guid householdId, string userId, CancellationToken cancellationToken = default);

        // Role-based queries
        Task<IReadOnlyList<HouseholdMember>> GetMembersByRoleAsync(Guid householdId, HouseholdRole role, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HouseholdMember>> GetOwnersAsync(Guid householdId, CancellationToken cancellationToken = default);

        // Role management
        Task UpdateRoleAsync(Guid householdId, string userId, HouseholdRole newRole, CancellationToken cancellationToken = default);
        Task<bool> IsUserMemberAsync(Guid householdId, string userId, CancellationToken cancellationToken = default);
        Task<HouseholdRole?> GetUserRoleAsync(Guid householdId, string userId, CancellationToken cancellationToken = default);

        // Statistics
        Task<int> GetMemberCountAsync(Guid householdId, CancellationToken cancellationToken = default);
        Task<int> GetOwnerCountAsync(Guid householdId, CancellationToken cancellationToken = default);
    }
}
