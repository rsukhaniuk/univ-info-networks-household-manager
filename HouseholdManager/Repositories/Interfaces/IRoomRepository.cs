using HouseholdManager.Models;

namespace HouseholdManager.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for Room operations
    /// Extends base repository with essential room-specific methods
    /// </summary>
    public interface IRoomRepository : IRepository<Room>
    {
        // Basic household-scoped operations
        Task<IReadOnlyList<Room>> GetByHouseholdIdAsync(Guid householdId, CancellationToken cancellationToken = default);
        Task<Room?> GetByIdWithTasksAsync(Guid id, CancellationToken cancellationToken = default);

        // Validation
        Task<bool> ExistsInHouseholdAsync(Guid roomId, Guid householdId, CancellationToken cancellationToken = default);
        Task<bool> IsNameUniqueInHouseholdAsync(string name, Guid householdId, Guid? excludeRoomId = null, CancellationToken cancellationToken = default);
    }
}
