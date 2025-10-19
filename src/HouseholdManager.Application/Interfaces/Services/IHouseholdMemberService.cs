using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Enums;

namespace HouseholdManager.Application.Interfaces.Services
{
    /// <summary>
    /// Service interface for household member management and role operations
    /// </summary>
    public interface IHouseholdMemberService
    {
        // Member queries
        /// <summary>
        /// Gets all members of a specific household with their roles
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of household members</returns>
        Task<IReadOnlyList<HouseholdMember>> GetHouseholdMembersAsync(Guid householdId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all household memberships for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of user's household memberships</returns>
        Task<IReadOnlyList<HouseholdMember>> GetUserMembershipsAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific member record for a user in a household
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>HouseholdMember entity or null if not found</returns>
        Task<HouseholdMember?> GetMemberAsync(Guid householdId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all members with a specific role in a household
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="role">Role to filter by (Owner or Member)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of members with specified role</returns>
        Task<IReadOnlyList<HouseholdMember>> GetMembersByRoleAsync(Guid householdId, HouseholdRole role, CancellationToken cancellationToken = default);

        // Role management
        /// <summary>
        /// Updates a member's role in a household. Validates requesting user is Owner and prevents demoting last Owner.
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="userId">User ID to update</param>
        /// <param name="newRole">New role to assign</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task UpdateMemberRoleAsync(Guid householdId, string userId, HouseholdRole newRole, string requestingUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Promotes a member to owner role. Wrapper around UpdateMemberRoleAsync with Owner role.
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="userId">User ID to promote</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task PromoteToOwnerAsync(Guid householdId, string userId, string requestingUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Demotes an owner to member role. Cannot demote last owner in household.
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="userId">User ID to demote</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task DemoteFromOwnerAsync(Guid householdId, string userId, string requestingUserId, CancellationToken cancellationToken = default);

        // Member statistics
        /// <summary>
        /// Gets total count of members in a household
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Member count</returns>
        Task<int> GetMemberCountAsync(Guid householdId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets count of owners in a household
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Owner count</returns>
        Task<int> GetOwnerCountAsync(Guid householdId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets task count statistics for all members in a household. Returns count of active assigned tasks per user.
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary of UserId to task count</returns>
        Task<Dictionary<string, int>> GetMemberTaskCountsAsync(Guid householdId, CancellationToken cancellationToken = default);

        // Validation
        /// <summary>
        /// Validates that a user is a member of the household. Throws UnauthorizedAccessException if not.
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="userId">User ID to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task ValidateMemberAccessAsync(Guid householdId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates that a user is an owner of the household. Throws UnauthorizedAccessException if not.
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="userId">User ID to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task ValidateOwnerAccessAsync(Guid householdId, string userId, CancellationToken cancellationToken = default);
    }
}
