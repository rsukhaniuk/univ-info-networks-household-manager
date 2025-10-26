using Azure.Core;
using HouseholdManager.Application.DTOs.Common;
using HouseholdManager.Application.DTOs.Task;
using HouseholdManager.Application.Extensions;
using HouseholdManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HouseholdManager.Api.Controllers
{
    /// <summary>
    /// Controller for managing household tasks
    /// Handles CRUD operations, assignment, status changes, and calendar view
    /// </summary>
    [ApiController]
    [Route("api/households/{householdId:guid}/tasks")]
    [Produces("application/json")]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly IHouseholdTaskService _taskService;
        private readonly IHouseholdService _householdService;
        private readonly ILogger<TasksController> _logger;

        public TasksController(
            IHouseholdTaskService taskService,
            IHouseholdService householdService,
            ILogger<TasksController> logger)
        {
            _taskService = taskService;
            _householdService = householdService;
            _logger = logger;
        }

        #region CRUD Operations

        /// <summary>
        /// Get all tasks for a household with optional filters
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="queryParameters">Query parameters for filtering and pagination</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paged list of tasks</returns>
        /// <response code="200">Returns filtered task list</response>
        /// <response code="401">Unauthorized - missing/invalid authentication</response>
        /// <response code="403">Forbidden - user is not a member of the household</response>
        /// <response code="404">Not Found - household not found</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<TaskDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<PagedResult<TaskDto>>>> GetTasks(
            [FromRoute] Guid householdId,
            [FromQuery] TaskQueryParameters queryParameters,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation(
                "User {UserId} fetching tasks for household {HouseholdId} with filters: Type={Type}, Priority={Priority}, AssignedUser={AssignedUserId}",
                userId, householdId, queryParameters.Type, queryParameters.Priority, queryParameters.AssignedUserId);

            // Validate member access
            await _householdService.ValidateUserAccessAsync(householdId, userId, cancellationToken);

            // Get all active tasks
            var allTasks = await _taskService.GetActiveHouseholdTasksAsync(householdId, cancellationToken);

            // Apply filters in memory
            var filteredTasks = allTasks.AsEnumerable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(queryParameters.Search))
            {
                filteredTasks = filteredTasks.Where(t =>
                    t.Title.Contains(queryParameters.Search, StringComparison.OrdinalIgnoreCase) ||
                    (t.Description != null && t.Description.Contains(queryParameters.Search, StringComparison.OrdinalIgnoreCase)));
            }

            // Room filter
            if (queryParameters.RoomId.HasValue)
            {
                filteredTasks = filteredTasks.Where(t => t.RoomId == queryParameters.RoomId.Value);
            }

            // Type filter
            if (queryParameters.Type.HasValue)
            {
                filteredTasks = filteredTasks.Where(t => t.Type == queryParameters.Type.Value);
            }

            // Priority filter
            if (queryParameters.Priority.HasValue)
            {
                filteredTasks = filteredTasks.Where(t => t.Priority == queryParameters.Priority.Value);
            }

            // Assigned user filter
            if (!string.IsNullOrEmpty(queryParameters.AssignedUserId))
            {
                filteredTasks = filteredTasks.Where(t => t.AssignedUserId == queryParameters.AssignedUserId);
            }

            // IsActive filter
            if (queryParameters.IsActive.HasValue)
            {
                filteredTasks = filteredTasks.Where(t => t.IsActive == queryParameters.IsActive.Value);
            }

            // Overdue filter
            if (queryParameters.IsOverdue.HasValue)
            {
                filteredTasks = queryParameters.IsOverdue.Value
                    ? filteredTasks.Where(t => t.IsOverdue)
                    : filteredTasks.Where(t => !t.IsOverdue);
            }

            // Scheduled weekday filter
            if (queryParameters.ScheduledWeekday.HasValue)
            {
                filteredTasks = filteredTasks.Where(t =>
                    t.ScheduledWeekday.HasValue &&
                    t.ScheduledWeekday.Value == queryParameters.ScheduledWeekday.Value);
            }

            // Sorting
            filteredTasks = queryParameters.SortBy?.ToLower() switch
            {
                "title" => queryParameters.IsAscending
                    ? filteredTasks.OrderBy(t => t.Title)
                    : filteredTasks.OrderByDescending(t => t.Title),
                "duedate" => queryParameters.IsAscending
                    ? filteredTasks.OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                    : filteredTasks.OrderByDescending(t => t.DueDate ?? DateTime.MinValue),
                "createdat" => queryParameters.IsAscending
                    ? filteredTasks.OrderBy(t => t.CreatedAt)
                    : filteredTasks.OrderByDescending(t => t.CreatedAt),
                _ => queryParameters.IsAscending
                    ? filteredTasks.OrderBy(t => t.Priority)
                    : filteredTasks.OrderByDescending(t => t.Priority)
            };

            // Pagination
            var pagedResult = PagedResult<TaskDto>.Create(
                filteredTasks,
                queryParameters.Page,
                queryParameters.PageSize);

            return Ok(ApiResponse<PagedResult<TaskDto>>.SuccessResponse(
                pagedResult,
                $"Retrieved {pagedResult.Items.Count} tasks (page {pagedResult.PageNumber} of {pagedResult.TotalPages})"));
        }

        /// <summary>
        /// Get detailed information about a specific task
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="taskId">Task ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task details with relations</returns>
        /// <response code="200">Returns task details</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - user is not a member</response>
        /// <response code="404">Not Found - task or household not found</response>
        [HttpGet("{taskId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<TaskDetailsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<TaskDetailsDto>>> GetTaskDetails(
            [FromRoute] Guid householdId,
            [FromRoute] Guid taskId,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} fetching details for task {TaskId}", userId, taskId);

            // Validate member access
            await _taskService.ValidateTaskAccessAsync(taskId, userId, cancellationToken);

            var task = await _taskService.GetTaskWithRelationsAsync(taskId, cancellationToken);

            // Set permissions flags
            if (task != null)
            {
                var isOwner = await _householdService.IsUserOwnerAsync(householdId, userId, cancellationToken);
                var isAssigned = task.Task.AssignedUserId == userId;

                task.Permissions = new TaskPermissionsDto
                {
                    IsOwner = isOwner,
                    IsSystemAdmin = false, // Set by auth middleware if needed
                    IsAssignedToCurrentUser = isAssigned,
                    CanEdit = isOwner,
                    CanDelete = isOwner,
                    CanComplete = isOwner || isAssigned,
                    CanAssign = isOwner
                };
            }

            return Ok(ApiResponse<TaskDetailsDto>.SuccessResponse(task, "Task details retrieved successfully"));
        }

        /// <summary>
        /// Create a new task in a household
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="request">Task creation data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created task</returns>
        /// <response code="201">Task created successfully</response>
        /// <response code="400">Bad Request - invalid data</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - only owners can create tasks</response>
        /// <response code="404">Not Found - household or room not found</response>
        /// <response code="422">Validation failed</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<ApiResponse<TaskDto>>> CreateTask(
            [FromRoute] Guid householdId,
            [FromBody] UpsertTaskRequest request,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            var dueDateFormatted = request.DueDate.ToLocalDateShort();

            _logger.LogInformation(
                "User {UserId} creating task '{Title}' in household {HouseholdId}, Room {RoomId}, Type={Type}, DueDate={DueDate}",
                userId, request.Title, householdId, request.RoomId, request.Type, dueDateFormatted);

            // Set householdId from route
            request.HouseholdId = householdId;

            var task = await _taskService.CreateTaskAsync(request, userId, cancellationToken);

            return CreatedAtAction(
                nameof(GetTaskDetails),
                new { householdId, taskId = task.Id },
                ApiResponse<TaskDto>.SuccessResponse(task, "Task created successfully"));
        }

        /// <summary>
        /// Update an existing task
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="taskId">Task ID</param>
        /// <param name="request">Task update data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated task</returns>
        /// <response code="200">Task updated successfully</response>
        /// <response code="400">Bad Request - invalid data</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - only owners can update tasks</response>
        /// <response code="404">Not Found</response>
        /// <response code="422">Validation failed</response>
        [HttpPut("{taskId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<ApiResponse<TaskDto>>> UpdateTask(
            [FromRoute] Guid householdId,
            [FromRoute] Guid taskId,
            [FromBody] UpsertTaskRequest request,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            var dueDateFormatted = request.DueDate.ToLocalDateShort();

            _logger.LogInformation(
                "User {UserId} updating task {TaskId}, Title='{Title}', DueDate={DueDate}",
                userId, taskId, request.Title, dueDateFormatted);

            // Set IDs from route
            request.Id = taskId;
            request.HouseholdId = householdId;

            var task = await _taskService.UpdateTaskAsync(taskId, request, userId, cancellationToken);

            return Ok(ApiResponse<TaskDto>.SuccessResponse(task, "Task updated successfully"));
        }

        /// <summary>
        /// Delete a task permanently
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="taskId">Task ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>No content</returns>
        /// <response code="204">Task deleted successfully</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - only owners can delete tasks</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("{taskId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTask(
            [FromRoute] Guid householdId,
            [FromRoute] Guid taskId,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} deleting task {TaskId}", userId, taskId);

            await _taskService.DeleteTaskAsync(taskId, userId, cancellationToken);

            return NoContent();
        }

        #endregion

        #region Assignment Operations

        /// <summary>
        /// Manually assign a task to a specific user
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="taskId">Task ID</param>
        /// <param name="request">Assignment data (userId)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated task</returns>
        /// <response code="200">Task assigned successfully</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - only owners can assign</response>
        /// <response code="404">Not Found</response>
        /// <response code="422">Validation failed - user is not a member</response>
        [HttpPost("{taskId:guid}/assign")]
        [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<ApiResponse<TaskDto>>> AssignTask(
            [FromRoute] Guid householdId,
            [FromRoute] Guid taskId,
            [FromBody] AssignTaskRequest request,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation(
                "User {UserId} assigning task {TaskId} to user {AssignedUserId}",
                userId, taskId, request.UserId ?? "unassigned");

            var task = await _taskService.AssignTaskAsync(taskId, request, userId, cancellationToken);

            return Ok(ApiResponse<TaskDto>.SuccessResponse(
                task,
                request.UserId != null
                    ? $"Task assigned to {task.AssignedUserName}"
                    : "Task unassigned"));
        }

        /// <summary>
        /// Unassign a task (remove assignment)
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="taskId">Task ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated task</returns>
        /// <response code="200">Task unassigned successfully</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - only owners can unassign</response>
        /// <response code="404">Not Found</response>
        [HttpPost("{taskId:guid}/unassign")]
        [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<TaskDto>>> UnassignTask(
            [FromRoute] Guid householdId,
            [FromRoute] Guid taskId,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} unassigning task {TaskId}", userId, taskId);

            await _taskService.UnassignTaskAsync(taskId, userId, cancellationToken);

            var task = await _taskService.GetTaskAsync(taskId, cancellationToken);

            return Ok(ApiResponse<TaskDto>.SuccessResponse(task, "Task unassigned successfully"));
        }

        /// <summary>
        /// Auto-assign all unassigned tasks in household using fair distribution
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success message</returns>
        /// <response code="200">Tasks auto-assigned successfully</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - only owners can auto-assign</response>
        /// <response code="404">Not Found</response>
        [HttpPost("auto-assign")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<string>>> AutoAssignTasks(
            [FromRoute] Guid householdId,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} auto-assigning tasks for household {HouseholdId}", userId, householdId);

            await _taskService.AutoAssignTasksAsync(householdId, userId, cancellationToken);

            return Ok(ApiResponse<string>.SuccessResponse("Tasks auto-assigned successfully"));
        }

        /// <summary>
        /// Reassign a task to the next member in rotation
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="taskId">Task ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated task</returns>
        /// <response code="200">Task reassigned successfully</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - only owners can reassign</response>
        /// <response code="404">Not Found</response>
        [HttpPost("{taskId:guid}/reassign")]
        [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<TaskDto>>> ReassignTask(
            [FromRoute] Guid householdId,
            [FromRoute] Guid taskId,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} reassigning task {TaskId} to next member", userId, taskId);

            var newAssigneeId = await _taskService.ReassignTaskToNextUserAsync(taskId, userId, cancellationToken);

            var task = await _taskService.GetTaskAsync(taskId, cancellationToken);

            return Ok(ApiResponse<TaskDto>.SuccessResponse(
                task,
                $"Task reassigned to {task.AssignedUserName}"));
        }
        #endregion

        #region Status Operations

        /// <summary>
        /// Activate a task (set IsActive = true)
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="taskId">Task ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated task</returns>
        /// <response code="200">Task activated successfully</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - only owners can activate</response>
        /// <response code="404">Not Found</response>
        [HttpPost("{taskId:guid}/activate")]
        [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<TaskDto>>> ActivateTask(
            [FromRoute] Guid householdId,
            [FromRoute] Guid taskId,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} activating task {TaskId}", userId, taskId);

            await _taskService.ActivateTaskAsync(taskId, userId, cancellationToken);

            var task = await _taskService.GetTaskAsync(taskId, cancellationToken);

            return Ok(ApiResponse<TaskDto>.SuccessResponse(task, "Task activated successfully"));
        }

        /// <summary>
        /// Deactivate a task (set IsActive = false)
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="taskId">Task ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated task</returns>
        /// <response code="200">Task deactivated successfully</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - only owners can deactivate</response>
        /// <response code="404">Not Found</response>
        [HttpPost("{taskId:guid}/deactivate")]
        [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<TaskDto>>> DeactivateTask(
            [FromRoute] Guid householdId,
            [FromRoute] Guid taskId,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} deactivating task {TaskId}", userId, taskId);

            await _taskService.DeactivateTaskAsync(taskId, userId, cancellationToken);

            var task = await _taskService.GetTaskAsync(taskId, cancellationToken);

            return Ok(ApiResponse<TaskDto>.SuccessResponse(task, "Task deactivated successfully"));
        }
        #endregion

        #region Calendar View

        /// <summary>
        /// Get calendar view of tasks for a specific week
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="weekStarting">Week start date (Monday), defaults to current week</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tasks grouped by day with weekly stats</returns>
        /// <response code="200">Returns calendar view</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - user is not a member</response>
        /// <response code="404">Not Found</response>
        [HttpGet("calendar")]
        [ProducesResponseType(typeof(ApiResponse<TaskCalendarDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<TaskCalendarDto>>> GetTaskCalendar(
            [FromRoute] Guid householdId,
            [FromQuery] DateTime? weekStarting = null,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            var weekStartFormatted = weekStarting.ToLocalDateShort();

            _logger.LogInformation(
                "User {UserId} fetching task calendar for household {HouseholdId}, week starting {WeekStart}",
                userId, householdId, weekStartFormatted);

            await _householdService.ValidateUserAccessAsync(householdId, userId, cancellationToken);

            // TODO: Implement GetTaskCalendarAsync in service
            return Ok(ApiResponse<TaskCalendarDto>.SuccessResponse(
                null,
                "Calendar view not yet implemented"));
        }
        #endregion

        #region Helper Methods

        /// <summary>
        /// Get current user ID from Auth0 JWT claims
        /// </summary>
        /// <returns>User ID string (Auth0 sub claim)</returns>
        private string GetCurrentUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("User ID not found in JWT claims");
                throw new UnauthorizedAccessException("User ID not found in authentication token");
            }

            return userId;
        }

        #endregion
    }
}
