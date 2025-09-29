using HouseholdManager.Models.Entities;

namespace HouseholdManager.Services.Interfaces
{
    /// <summary>
    /// Service interface for task execution (completion) and execution history management
    /// </summary>
    public interface ITaskExecutionService
    {
        // Execution operations
        /// <summary>
        /// Completes a task with optional notes and photo. Creates TaskExecution with denormalized fields.
        /// Regular tasks can only be completed once per week (Monday-Sunday).
        /// </summary>
        /// <param name="taskId">Task ID to complete</param>
        /// <param name="userId">User ID completing the task</param>
        /// <param name="notes">Optional completion notes</param>
        /// <param name="photo">Optional photo of completed task</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created TaskExecution entity</returns>
        Task<TaskExecution> CompleteTaskAsync(Guid taskId, string userId, string? notes = null, IFormFile? photo = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a single execution by ID without relations
        /// </summary>
        /// <param name="id">Execution ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>TaskExecution entity or null</returns>
        Task<TaskExecution?> GetExecutionAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a single execution by ID with Task and User navigation properties loaded
        /// </summary>
        /// <param name="id">Execution ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>TaskExecution entity with relations or null</returns>
        Task<TaskExecution?> GetExecutionWithRelationsAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates execution notes and photo. Only creator or household Owner can update.
        /// </summary>
        /// <param name="execution">Execution entity with updated values</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task UpdateExecutionAsync(TaskExecution execution, string requestingUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an execution. Only creator or household Owner can delete. Deletes photo from filesystem.
        /// </summary>
        /// <param name="id">Execution ID</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task DeleteExecutionAsync(Guid id, string requestingUserId, CancellationToken cancellationToken = default);

        // Query operations
        /// <summary>
        /// Gets all executions for a specific task, ordered by CompletedAt descending
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of task executions</returns>
        Task<IReadOnlyList<TaskExecution>> GetTaskExecutionsAsync(Guid taskId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all executions for a household, ordered by CompletedAt descending
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of household executions</returns>
        Task<IReadOnlyList<TaskExecution>> GetHouseholdExecutionsAsync(Guid householdId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets executions by a specific user in a household for the current week
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="householdId">Household ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of user's executions this week</returns>
        Task<IReadOnlyList<TaskExecution>> GetUserExecutionsAsync(string userId, Guid householdId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets executions for a specific week. If weekStarting is null, returns current week (Monday-Sunday).
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="weekStarting">Optional week start date (Monday), defaults to current week</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of executions for the week</returns>
        /// <remarks>
        /// TODO: Requires additional testing and validation
        /// </remarks> 
        Task<IReadOnlyList<TaskExecution>> GetWeeklyExecutionsAsync(Guid householdId, DateTime? weekStarting = null, CancellationToken cancellationToken = default);

        // Status checking
        /// <summary>
        /// Checks if a task has been completed this week (Monday-Sunday)
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if task completed this week, false otherwise</returns>
        /// <remarks>
        /// TODO: Requires additional testing and validation
        /// </remarks>
        Task<bool> IsTaskCompletedThisWeekAsync(Guid taskId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the most recent execution for a task
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Latest TaskExecution or null if none exist</returns>
        /// <remarks>
        /// TODO: Requires additional testing and validation
        /// </remarks>
        Task<TaskExecution?> GetLatestExecutionForTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

        // Photo management
        /// <summary>
        /// Uploads or replaces execution photo. Only creator or household Owner can upload.
        /// </summary>
        /// <param name="executionId">Execution ID</param>
        /// <param name="photo">Photo file to upload</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Relative path to uploaded photo</returns>
        Task<string> UploadExecutionPhotoAsync(Guid executionId, IFormFile photo, string requestingUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes execution photo from filesystem and clears PhotoPath. Only creator or household Owner can delete.
        /// </summary>
        /// <param name="executionId">Execution ID</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task DeleteExecutionPhotoAsync(Guid executionId, string requestingUserId, CancellationToken cancellationToken = default);

        // Validation
        /// <summary>
        /// Validates that a user has access to an execution (must be household member).
        /// For edit/delete operations, must be creator or household Owner.
        /// </summary>
        /// <param name="executionId">Execution ID</param>
        /// <param name="userId">User ID to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task ValidateExecutionAccessAsync(Guid executionId, string userId, CancellationToken cancellationToken = default);
    }
}
