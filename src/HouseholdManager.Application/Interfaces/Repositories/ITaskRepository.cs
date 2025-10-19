using HouseholdManager.Domain.Entities;

namespace HouseholdManager.Application.Interfaces.Repositories
{
    /// <summary>
    /// Repository interface for HouseholdTask operations
    /// Extends base repository with essential task-specific methods
    /// </summary>
    public interface ITaskRepository : IRepository<HouseholdTask>
    {
        // Basic queries with relations
        Task<HouseholdTask?> GetByIdWithRelationsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HouseholdTask>> GetByHouseholdIdAsync(Guid householdId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HouseholdTask>> GetActiveByHouseholdIdAsync(Guid householdId, CancellationToken cancellationToken = default);

        // Basic filtering
        Task<IReadOnlyList<HouseholdTask>> GetByRoomIdAsync(Guid roomId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HouseholdTask>> GetByAssignedUserIdAsync(string userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HouseholdTask>> GetUnassignedTasksAsync(Guid householdId, CancellationToken cancellationToken = default);

        // Assignment operations
        Task AssignTaskAsync(Guid taskId, string userId, CancellationToken cancellationToken = default);
        Task UnassignTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
        Task BulkAssignTasksAsync(Dictionary<Guid, string> taskAssignments, CancellationToken cancellationToken = default);

        // Task type specific queries
        Task<IReadOnlyList<HouseholdTask>> GetRegularTasksByWeekdayAsync(Guid householdId, DayOfWeek weekday, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HouseholdTask>> GetOverdueTasksAsync(Guid householdId, CancellationToken cancellationToken = default);
    }
}
