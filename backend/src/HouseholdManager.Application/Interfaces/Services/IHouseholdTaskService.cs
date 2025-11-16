using HouseholdManager.Application.DTOs.Task;
using HouseholdManager.Domain.Entities;

namespace HouseholdManager.Application.Interfaces.Services
{
    /// <summary>
    /// Service interface for household task CRUD operations and assignment orchestration.
    /// Delegates assignment algorithms to ITaskAssignmentService.
    /// </summary>
    public interface IHouseholdTaskService
    {
        // Basic CRUD operations
        /// <summary>
        /// Creates a new task in a household. Validates room belongs to household and requesting user is Owner.
        /// </summary>
        /// <param name="task">Task entity to create</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created task entity</returns>
        Task<TaskDto> CreateTaskAsync(UpsertTaskRequest request, string requestingUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a single task by ID without navigation properties
        /// </summary>
        /// <param name="id">Task ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task entity or null</returns>
        Task<TaskDto?> GetTaskAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a single task by ID with Household, Room, and AssignedUser navigation properties loaded
        /// </summary>
        /// <param name="id">Task ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task entity with relations or null</returns>
        Task<TaskDetailsDto?> GetTaskWithRelationsAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all tasks (active and inactive) for a household
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of all household tasks</returns>
        Task<IReadOnlyList<TaskDto>> GetHouseholdTasksAsync(Guid householdId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets only active tasks (IsActive=true) for a household
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of active household tasks</returns>
        Task<IReadOnlyList<TaskDto>> GetActiveHouseholdTasksAsync(Guid householdId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing task. Validates room belongs to household and requesting user is Owner.
        /// </summary>
        /// <param name="task">Task entity with updated values</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task<TaskDto> UpdateTaskAsync(Guid id, UpsertTaskRequest request, string requestingUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a task permanently. Owner only operation.
        /// </summary>
        /// <param name="id">Task ID</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task DeleteTaskAsync(Guid id, string requestingUserId, CancellationToken cancellationToken = default);

        // Task filtering
        /// <summary>
        /// Gets all tasks for a specific room
        /// </summary>
        /// <param name="roomId">Room ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of room tasks</returns>
        Task<IReadOnlyList<TaskDto>> GetRoomTasksAsync(Guid roomId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active tasks assigned to a specific user across all their households
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of user's assigned tasks</returns>
        Task<IReadOnlyList<TaskDto>> GetUserTasksAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all overdue OneTime tasks (DueDate in the past) for a household
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of overdue tasks</returns>
        Task<IReadOnlyList<TaskDto>> GetOverdueTasksAsync(Guid householdId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all Regular tasks scheduled for a specific weekday in a household
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="weekday">Day of week</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of tasks for the weekday</returns>
        /// <remarks>
        /// TODO: Requires additional testing and validation
        /// </remarks>
        Task<IReadOnlyList<TaskDto>> GetTasksForWeekdayAsync(Guid householdId, DayOfWeek weekday, CancellationToken cancellationToken = default);

        // Assignment operations
        /// <summary>
        /// Manually assigns a task to a specific user. Owner only. Validates user is household member.
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="userId">User ID to assign task to</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task<TaskDto> AssignTaskAsync(Guid taskId, AssignTaskRequest request, string requestingUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes assignment from a task (sets AssignedUserId to null). Owner only.
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task UnassignTaskAsync(Guid taskId, string requestingUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Auto-assigns all unassigned tasks in household using fair distribution.
        /// Delegates to ITaskAssignmentService.AutoAssignAllTasksAsync. Owner only.
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        /// <remarks>
        /// TODO: Requires additional testing and validation
        /// </remarks>
        Task AutoAssignTasksAsync(Guid householdId, string requestingUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Previews how tasks would be auto-assigned without saving to database.
        /// Delegates to ITaskAssignmentService.PreviewAutoAssignAllTasksAsync. Owner only.
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of preview assignments with task and assignee details</returns>
        Task<IReadOnlyList<TaskAssignmentPreviewDto>> PreviewAutoAssignTasksAsync(Guid householdId, string requestingUserId, CancellationToken cancellationToken = default);

        // Advanced assignment operations (delegate to TaskAssignmentService)
        /// <summary>
        /// Gets suggested assignee based on workload. Delegates to ITaskAssignmentService.GetSuggestedAssigneeAsync.
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Suggested user ID</returns>
        /// <remarks>
        /// TODO: Requires additional testing and validation
        /// </remarks>
        Task<string> GetSuggestedAssigneeAsync(Guid taskId, string requestingUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reassigns task to next member in round-robin rotation. 
        /// Delegates to ITaskAssignmentService.ReassignTaskToNextUserAsync. Owner only.
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>New assigned user ID</returns>
        /// <remarks>
        /// TODO: Requires additional testing and validation
        /// </remarks>
        Task<string> ReassignTaskToNextUserAsync(Guid taskId, string requestingUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Auto-assigns weekly tasks grouped by weekday. 
        /// Delegates to ITaskAssignmentService.AutoAssignWeeklyTasksAsync. Owner only.
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        /// <remarks>
        /// TODO: Requires additional testing and validation
        /// </remarks>
        Task AutoAssignWeeklyTasksAsync(Guid householdId, string requestingUserId, CancellationToken cancellationToken = default);

        // Task status operations
        /// <summary>
        /// Activates a task (sets IsActive=true). Owner only.
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task ActivateTaskAsync(Guid taskId, string requestingUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deactivates a task (sets IsActive=false). Deactivated tasks don't appear in active lists. Owner only.
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="requestingUserId">ID of user making the request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task DeactivateTaskAsync(Guid taskId, string requestingUserId, CancellationToken cancellationToken = default);

        // Validation
        /// <summary>
        /// Validates that a user has access to a task (must be household member). 
        /// Throws UnauthorizedAccessException if not.
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="userId">User ID to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task ValidateTaskAccessAsync(Guid taskId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates that a user is an owner of the task's household. 
        /// Throws UnauthorizedAccessException if not.
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="userId">User ID to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task ValidateTaskOwnerAccessAsync(Guid taskId, string userId, CancellationToken cancellationToken = default);
    }
}
