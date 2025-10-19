using HouseholdManager.Domain.Entities;

namespace HouseholdManager.Application.Interfaces.Repositories
{
    /// <summary>
    /// Repository interface for TaskExecution operations
    /// Extends base repository with essential execution-specific methods
    /// </summary>
    public interface IExecutionRepository : IRepository<TaskExecution>
    {
        // Basic queries with relations
        Task<TaskExecution?> GetByIdWithRelationsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TaskExecution>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TaskExecution>> GetByHouseholdIdAsync(Guid householdId, CancellationToken cancellationToken = default);

        // Time-based queries
        Task<IReadOnlyList<TaskExecution>> GetThisWeekAsync(Guid householdId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TaskExecution>> GetByDateRangeAsync(Guid householdId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

        // User-specific queries
        Task<IReadOnlyList<TaskExecution>> GetUserExecutionsThisWeekAsync(string userId, Guid householdId, CancellationToken cancellationToken = default);

        // Task completion tracking
        Task<bool> IsTaskCompletedThisWeekAsync(Guid taskId, CancellationToken cancellationToken = default);
        Task<TaskExecution?> GetLatestExecutionForTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

        // Execution creation with denormalized fields
        Task<TaskExecution> CreateExecutionAsync(Guid taskId, string userId, string? notes = null, string? photoPath = null, CancellationToken cancellationToken = default);
    }
}
