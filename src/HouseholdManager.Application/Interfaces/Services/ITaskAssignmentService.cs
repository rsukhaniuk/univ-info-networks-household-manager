namespace HouseholdManager.Application.Interfaces.Services
{
    /// <summary>
    /// Service interface for automatic task assignment
    /// </summary>
    public interface ITaskAssignmentService
    {
        // <summary>
        /// Assigns a single task to the user with the least current workload
        /// </summary>
        /// <param name="taskId">Task ID to assign</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>User ID of the assigned user</returns>
        Task<string> AssignTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Auto-assigns all unassigned tasks in a household using fair distribution algorithm.
        /// Orders tasks by Priority (high to low), then CreatedAt. Distributes evenly based on current workload stats.
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary of TaskId to assigned UserId</returns>
        /// <remarks>
        /// TODO: Requires additional testing and validation
        /// </remarks>
        Task<Dictionary<Guid, string>> AutoAssignAllTasksAsync(Guid householdId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Auto-assigns all unassigned regular (weekly) tasks grouped by weekday.
        /// Uses separate workload tracking for each day to ensure fair distribution across the week.
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary of TaskId to assigned UserId</returns>
        /// <remarks>
        /// TODO: Requires additional testing and validation
        /// </remarks>
        Task<Dictionary<Guid, string>> AutoAssignWeeklyTasksAsync(Guid householdId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reassigns task to the next member in round-robin rotation.
        /// Finds current assignee index and assigns to next member in the list (wraps around).
        /// </summary>
        /// <param name="taskId">Task ID to reassign</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>User ID of the new assigned user</returns>
        /// <remarks>
        /// TODO: Requires additional testing and validation
        /// </remarks>
        Task<string> ReassignTaskToNextUserAsync(Guid taskId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Suggests the best assignee for a task based on current workload.
        /// Returns the user with the fewest currently assigned active tasks.
        /// </summary>
        /// <param name="taskId">Task ID to get suggestion for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Suggested user ID, or null if no members available</returns>
        /// <remarks>
        /// TODO: Requires additional testing and validation
        /// </remarks>
        Task<string?> GetSuggestedAssigneeAsync(Guid taskId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Suggests the best assignee for a task based on current workload.
        /// Returns the user with the fewest currently assigned active tasks.
        /// </summary>
        /// <param name="taskId">Task ID to get suggestion for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Suggested user ID, or null if no members available</returns>
        /// <remarks>
        /// TODO: Requires additional testing and validation
        /// </remarks>
        Task<Dictionary<string, int>> GetWorkloadStatsAsync(Guid householdId, CancellationToken cancellationToken = default);
    }
}
