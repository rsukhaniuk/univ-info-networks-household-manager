using HouseholdManager.Application.DTOs.Execution;
using HouseholdManager.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace HouseholdManager.Application.Interfaces.Services
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
        Task<ExecutionDto> CompleteTaskAsync(
            CompleteTaskRequest request,
            string userId,
            IFormFile? photo = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a single execution by ID without relations
        /// </summary>
        /// <param name="id">Execution ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>TaskExecution entity or null</returns>
        Task<ExecutionDto?> GetExecutionAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a single execution by ID with Task and User navigation properties loaded
        /// </summary>
        /// <param name="id">Execution ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>TaskExecution entity with relations or null</returns>
        Task<ExecutionDto?> GetExecutionWithRelationsAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates execution notes and photo. Only creator or household Owner can update.
        /// </summary>
        /// <param name="execution">Execution entity with updated values</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task<ExecutionDto> UpdateExecutionAsync(
              Guid id,
              UpdateExecutionRequest request,
              string requestingUserId,
              CancellationToken cancellationToken = default);

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
        Task<IReadOnlyList<ExecutionDto>> GetTaskExecutionsAsync(
            Guid taskId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all executions for a household, ordered by CompletedAt descending
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of household executions</returns>
        Task<IReadOnlyList<ExecutionDto>> GetHouseholdExecutionsAsync(
            Guid householdId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets executions by a specific user in a household for the current week
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="householdId">Household ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of user's executions this week</returns>
        Task<IReadOnlyList<ExecutionDto>> GetUserExecutionsAsync(
            string userId,
            Guid householdId,
            CancellationToken cancellationToken = default);

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
        Task<IReadOnlyList<ExecutionDto>> GetWeeklyExecutionsAsync(
            Guid householdId,
            DateTime? weekStarting = null,
            CancellationToken cancellationToken = default);

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
        Task<ExecutionDto?> GetLatestExecutionForTaskAsync(
            Guid taskId,
            CancellationToken cancellationToken = default);

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

        /// <summary>
        /// Invalidates (uncounts) this week's execution for a Regular task, allowing it to be completed again.
        /// The execution remains in history but is not counted for weekly completion tracking.
        /// Only household Owner can invalidate executions.
        /// </summary>
        /// <param name="taskId">Task ID to reset</param>
        /// <param name="requestingUserId">ID of user making the request (must be owner)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        /// <remarks>
        /// This operation:
        /// - Validates the task is a Regular task
        /// - Validates the user is a household owner
        /// - Sets IsCountedForCompletion = false for this week's execution
        /// - Preserves execution history and photos
        /// - Allows task to be recompleted this week
        /// </remarks>
        Task InvalidateExecutionThisWeekAsync(Guid taskId, string requestingUserId, CancellationToken cancellationToken = default);
    }
}
