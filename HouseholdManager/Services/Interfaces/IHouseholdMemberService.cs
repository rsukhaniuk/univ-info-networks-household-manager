using HouseholdManager.Models;
using HouseholdManager.Models.Enums;

namespace HouseholdManager.Services.Interfaces
{
    /// <summary>
    /// Service interface for household member business logic
    /// </summary>
    public interface IHouseholdMemberService
    {
        // Member queries
        Task<IReadOnlyList<HouseholdMember>> GetHouseholdMembersAsync(Guid householdId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HouseholdMember>> GetUserMembershipsAsync(string userId, CancellationToken cancellationToken = default);
        Task<HouseholdMember?> GetMemberAsync(Guid householdId, string userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HouseholdMember>> GetMembersByRoleAsync(Guid householdId, HouseholdRole role, CancellationToken cancellationToken = default);

        // Role management
        Task UpdateMemberRoleAsync(Guid householdId, string userId, HouseholdRole newRole, string requestingUserId, CancellationToken cancellationToken = default);
        Task PromoteToOwnerAsync(Guid householdId, string userId, string requestingUserId, CancellationToken cancellationToken = default);
        Task DemoteFromOwnerAsync(Guid householdId, string userId, string requestingUserId, CancellationToken cancellationToken = default);

        // Member statistics
        Task<int> GetMemberCountAsync(Guid householdId, CancellationToken cancellationToken = default);
        Task<int> GetOwnerCountAsync(Guid householdId, CancellationToken cancellationToken = default);
        //Task<Dictionary<string, int>> GetMemberTaskCountsAsync(Guid householdId, CancellationToken cancellationToken = default);

        // Validation
        Task ValidateMemberAccessAsync(Guid householdId, string userId, CancellationToken cancellationToken = default);
        Task ValidateOwnerAccessAsync(Guid householdId, string userId, CancellationToken cancellationToken = default);
    }
}
