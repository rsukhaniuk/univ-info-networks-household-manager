using HouseholdManager.Models;

namespace HouseholdManager.Services.Interfaces
{
    /// <summary>
    /// Service interface for household task business logic
    /// </summary>
    public interface IHouseholdTaskService
    {
        // Basic CRUD operations
        Task<HouseholdTask> CreateTaskAsync(HouseholdTask task, string requestingUserId, CancellationToken cancellationToken = default);
        Task<HouseholdTask?> GetTaskAsync(Guid id, CancellationToken cancellationToken = default);
        Task<HouseholdTask?> GetTaskWithRelationsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HouseholdTask>> GetHouseholdTasksAsync(Guid householdId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HouseholdTask>> GetActiveHouseholdTasksAsync(Guid householdId, CancellationToken cancellationToken = default);
        Task UpdateTaskAsync(HouseholdTask task, string requestingUserId, CancellationToken cancellationToken = default);
        Task DeleteTaskAsync(Guid id, string requestingUserId, CancellationToken cancellationToken = default);

        // Task filtering
        Task<IReadOnlyList<HouseholdTask>> GetRoomTasksAsync(Guid roomId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HouseholdTask>> GetUserTasksAsync(string userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HouseholdTask>> GetOverdueTasksAsync(Guid householdId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HouseholdTask>> GetTasksForWeekdayAsync(Guid householdId, DayOfWeek weekday, CancellationToken cancellationToken = default);

        // Assignment operations
        Task AssignTaskAsync(Guid taskId, string userId, string requestingUserId, CancellationToken cancellationToken = default);
        Task UnassignTaskAsync(Guid taskId, string requestingUserId, CancellationToken cancellationToken = default);
        Task AutoAssignTasksAsync(Guid householdId, string requestingUserId, CancellationToken cancellationToken = default);

        // Advanced assignment operations (delegate to TaskAssignmentService)
        Task<string> GetSuggestedAssigneeAsync(Guid taskId, string requestingUserId, CancellationToken cancellationToken = default);
        Task<string> ReassignTaskToNextUserAsync(Guid taskId, string requestingUserId, CancellationToken cancellationToken = default);
        Task AutoAssignWeeklyTasksAsync(Guid householdId, string requestingUserId, CancellationToken cancellationToken = default);

        // Task status operations
        Task ActivateTaskAsync(Guid taskId, string requestingUserId, CancellationToken cancellationToken = default);
        Task DeactivateTaskAsync(Guid taskId, string requestingUserId, CancellationToken cancellationToken = default);

        // Validation
        Task ValidateTaskAccessAsync(Guid taskId, string userId, CancellationToken cancellationToken = default);
        Task ValidateTaskOwnerAccessAsync(Guid taskId, string userId, CancellationToken cancellationToken = default);
    }
}
