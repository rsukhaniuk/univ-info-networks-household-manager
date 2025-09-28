namespace HouseholdManager.Services.Interfaces
{
    /// <summary>
    /// Service interface for automatic task assignment
    /// Implements round-robin and other assignment algorithms
    /// </summary>
    public interface ITaskAssignmentService
    {
        /// <summary>
        /// Assign a single task to a user using round-robin algorithm
        /// </summary>
        Task<string> AssignTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Auto-assign all unassigned tasks in a household using round-robin
        /// </summary>
        Task<Dictionary<Guid, string>> AutoAssignAllTasksAsync(Guid householdId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Auto-assign weekly regular tasks for the current week
        /// </summary>
        Task<Dictionary<Guid, string>> AutoAssignWeeklyTasksAsync(Guid householdId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reassign task to next user in rotation
        /// </summary>
        Task<string> ReassignTaskToNextUserAsync(Guid taskId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get suggested assignee for a task based on workload balance
        /// </summary>
        Task<string?> GetSuggestedAssigneeAsync(Guid taskId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get workload statistics for household members
        /// </summary>
        Task<Dictionary<string, int>> GetWorkloadStatsAsync(Guid householdId, CancellationToken cancellationToken = default);
    }
}
