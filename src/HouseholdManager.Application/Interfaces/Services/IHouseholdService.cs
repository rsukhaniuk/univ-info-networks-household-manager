using HouseholdManager.Application.DTOs.Household;
using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Enums;

namespace HouseholdManager.Application.Interfaces.Services
{
    /// <summary>
    /// Service interface for household business logic
    /// </summary>
    public interface IHouseholdService
    {
        // Basic CRUD operations
        Task<HouseholdDto> CreateHouseholdAsync(UpsertHouseholdRequest request, string ownerId, CancellationToken cancellationToken = default);
        Task<HouseholdDto?> GetHouseholdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<HouseholdDetailsDto?> GetHouseholdWithMembersAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HouseholdDto>> GetUserHouseholdsAsync(string userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HouseholdDto>> GetAllHouseholdsAsync(CancellationToken cancellationToken = default);
        Task<HouseholdDto> UpdateHouseholdAsync(Guid id, UpsertHouseholdRequest request, string requestingUserId, CancellationToken cancellationToken = default);

        Task DeleteHouseholdAsync(Guid id, string requestingUserId, CancellationToken cancellationToken = default);

        // Invite operations
        Task<HouseholdDto?> GetHouseholdByInviteCodeAsync(Guid inviteCode, CancellationToken cancellationToken = default);
        Task<Guid> RegenerateInviteCodeAsync(Guid householdId, string requestingUserId, CancellationToken cancellationToken = default);
        Task<HouseholdDto> JoinHouseholdAsync(JoinHouseholdRequest request, string userId, CancellationToken cancellationToken = default);


        // Member management
        Task<HouseholdMemberDto> AddMemberAsync(Guid householdId, string userId, HouseholdRole role, string requestingUserId, CancellationToken cancellationToken = default);
        Task RemoveMemberAsync(Guid householdId, string userId, string requestingUserId, CancellationToken cancellationToken = default);
        Task LeaveHouseholdAsync(Guid householdId, string userId, CancellationToken cancellationToken = default);
        Task<bool> IsUserMemberAsync(Guid householdId, string userId, CancellationToken cancellationToken = default);
        Task<bool> IsUserOwnerAsync(Guid householdId, string userId, CancellationToken cancellationToken = default);
        Task<HouseholdRole?> GetUserRoleAsync(Guid householdId, string userId, CancellationToken cancellationToken = default);

        // Permission validation
        Task ValidateUserAccessAsync(Guid householdId, string userId, CancellationToken cancellationToken = default);
        Task ValidateOwnerAccessAsync(Guid householdId, string userId, CancellationToken cancellationToken = default);
    }
}
