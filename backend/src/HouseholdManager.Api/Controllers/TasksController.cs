using Azure.Core;
using HouseholdManager.Application.DTOs.Common;
using HouseholdManager.Application.DTOs.Task;
using HouseholdManager.Application.Extensions;
using HouseholdManager.Application.Helpers;
using HouseholdManager.Application.Interfaces.Services;
using HouseholdManager.Domain.Enums;
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
        private readonly ITaskExecutionService _taskExecutionService;
        private readonly IUserService _userService;
        private readonly ILogger<TasksController> _logger;

        public TasksController(
            IHouseholdTaskService taskService,
            IHouseholdService householdService,
            ITaskExecutionService taskExecutionService,
            IUserService userService,
            ILogger<TasksController> logger)
        {
            _taskService = taskService;
            _householdService = householdService;
            _taskExecutionService = taskExecutionService;
            _userService = userService;
            _logger = logger;
        }

        #region CRUD Operations

        /// <summary>
        /// Get all tasks for a household with optional filtering, sorting, and pagination
        /// </summary>
        /// <param name="householdId">Household ID (from route)</param>
        /// <param name="queryParameters">Query parameters for filtering, sorting, and pagination</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of tasks within the specified household</returns>
        /// <remarks>
        /// Query parameters:
        /// - **Page**: Page number (default: 1)
        /// - **PageSize**: Number of items per page (default: 20, max: 100)
        /// - **SortBy**: Sort field (e.g., "Title", "Priority", "CreatedAt", "DueDate", "RoomName", "Type", "IsActive", "AssignedUserName")
        /// - **SortOrder**: "asc" or "desc" (default: "desc")
        /// - **Search**: Search by task name or description
        /// - **RoomId**: Filter tasks belonging to a specific room (GUID)
        /// - **Type**: Filter by task type ("Regular" or "OneTime")
        /// - **Priority**: Filter by task priority ("Low", "Medium", "High")
        /// - **AssignedUserId**: Filter by assigned user (Auth0 user ID)
        /// - **IsActive**: Filter active/inactive tasks (true/false)
        /// - **IsOverdue**: Filter overdue tasks (true/false) — applies to OneTime tasks
        /// - **Weekday**: Filter by weekday (e.g., "Monday") — applies to Regular tasks with weekly recurrence
        ///
        /// Example:  
        /// `GET /api/households/{householdId}/tasks?page=1&amp;pageSize=10&amp;sortBy=Priority&amp;sortOrder=desc&amp;type=Regular&amp;isActive=true`
        /// </remarks>
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

            // Get all tasks (not just active ones, because we have IsActive filter)
            var allTasks = await _taskService.GetHouseholdTasksAsync(householdId, cancellationToken);

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

            // Weekday filter (for Regular tasks with weekly recurrence)
            if (queryParameters.Weekday.HasValue)
            {
                var targetWeekday = queryParameters.Weekday.Value;
                filteredTasks = filteredTasks.Where(t =>
                {
                    if (t.Type != TaskType.Regular || string.IsNullOrWhiteSpace(t.RecurrenceRule))
                        return false;

                    var weekdays = RruleHelper.ExtractWeekdays(t.RecurrenceRule);
                    return weekdays.Contains(targetWeekday);
                });
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
                "roomname" => queryParameters.IsAscending
                    ? filteredTasks.OrderBy(t => t.RoomName)
                    : filteredTasks.OrderByDescending(t => t.RoomName),
                "type" => queryParameters.IsAscending
                    ? filteredTasks.OrderBy(t => t.Type)
                    : filteredTasks.OrderByDescending(t => t.Type),
                "isactive" => queryParameters.IsAscending
                    ? filteredTasks.OrderBy(t => t.IsActive)
                    : filteredTasks.OrderByDescending(t => t.IsActive),
                "assignedusername" => queryParameters.IsAscending
                    ? filteredTasks.OrderBy(t => t.AssignedUserName ?? string.Empty)
                    : filteredTasks.OrderByDescending(t => t.AssignedUserName ?? string.Empty),
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
                var isSystemAdmin = await _userService.IsSystemAdminAsync(userId, cancellationToken);
                var isAssigned = task.Task.AssignedUserId == userId;

                task.Permissions = new TaskPermissionsDto
                {
                    IsOwner = isOwner,
                    IsSystemAdmin = isSystemAdmin,
                    IsAssignedToCurrentUser = isAssigned,
                    CanEdit = isOwner || isSystemAdmin,
                    CanDelete = isOwner || isSystemAdmin,
                    CanComplete = isOwner || isAssigned || isSystemAdmin,
                    CanAssign = isOwner || isSystemAdmin
                };
            }

            return Ok(ApiResponse<TaskDetailsDto>.SuccessResponse(task, "Task details retrieved successfully"));
        }

        /// <summary>
        /// Create a new task in a household
        /// </summary>
        /// <param name="householdId">Household ID (from route)</param>
        /// <param name="request">Task creation data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created task</returns>
        /// <remarks>
        /// Request body parameters:
        /// - **Title**: Task title (required, max 200 characters)  
        /// - **Description**: Optional task description (max 1000 characters)  
        /// - **Type**: Task type ("Regular" or "OneTime")  
        /// - **Priority**: Task priority ("Low", "Medium", "High")  
        /// - **RoomId**: ID of the room where this task is performed (required)
        /// - **AssignedUserId**: Optional Auth0 user ID of the assigned member
        /// - **IsActive**: Whether the task is active (true/false, default: true)
        /// - **DueDate**: Due date for OneTime tasks (UTC format, optional)
        /// - **RecurrenceRule**: iCalendar RRULE for Regular tasks (e.g., "FREQ=WEEKLY;BYDAY=MO")
        ///
        /// Example:
        /// ```json
        /// {
        ///   "title": "Clean kitchen",
        ///   "description": "Wipe counters and mop floor",
        ///   "type": "Regular",
        ///   "priority": "High",
        ///   "roomId": "bcd7b1e8-73f2-4a59-8eab-d7a7b6e1b4e2",
        ///   "assignedUserId": "auth0|690378562126962460b261ea",
        ///   "isActive": true,
        ///   "recurrenceRule": "FREQ=WEEKLY;BYDAY=MO"
        /// }
        /// ```
        /// </remarks>
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
        [ApiExplorerSettings(IgnoreApi = true)]
        [NonAction]
        [HttpPost("{taskId:guid}/assign")]
        [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<ApiResponse<TaskDto>>> AssignTask(
            [FromRoute] Guid householdId,
            [FromRoute] Guid taskId,
            [FromQuery] AssignTaskRequest request,
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
        [ApiExplorerSettings(IgnoreApi = true)]
        [NonAction]
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
        /// Preview how tasks would be auto-assigned without saving
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of preview assignments</returns>
        /// <response code="200">Returns preview of task assignments</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - only owners can preview</response>
        /// <response code="404">Not Found</response>
        [HttpPost("auto-assign/preview")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TaskAssignmentPreviewDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<TaskAssignmentPreviewDto>>>> PreviewAutoAssignTasks(
            [FromRoute] Guid householdId,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} previewing auto-assign for household {HouseholdId}", userId, householdId);

            var preview = await _taskService.PreviewAutoAssignTasksAsync(householdId, userId, cancellationToken);

            return Ok(ApiResponse<IReadOnlyList<TaskAssignmentPreviewDto>>.SuccessResponse(
                preview,
                $"Preview generated for {preview.Count} task assignments"));
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
        [ApiExplorerSettings(IgnoreApi = true)]
        [NonAction]
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

        /// <summary>
        /// Invalidate current period's execution for a Regular task (Owner-only)
        /// This uncounts the execution but preserves history, allowing the task to be completed again in the current period (daily/weekly/monthly/yearly)
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="taskId">Task ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success message</returns>
        /// <response code="200">Execution invalidated successfully</response>
        /// <response code="400">Bad Request - task is not a Regular task or not completed in current period</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - only owners can invalidate executions</response>
        /// <response code="404">Not Found</response>
        /// <remarks>
        /// This endpoint is useful when:
        /// - A task was marked as completed by mistake
        /// - A task needs to be completed again in the current period
        /// - The quality of work was not acceptable and needs to be redone
        ///
        /// **Important:** This does NOT delete the execution from history.
        /// It only sets `IsCountedForCompletion = false`, allowing the task to be recompleted.
        /// The original execution remains visible in history.
        ///
        /// The "current period" depends on the task's RecurrenceRule:
        /// - FREQ=DAILY: today
        /// - FREQ=WEEKLY: this week (Monday to Sunday)
        /// - FREQ=MONTHLY: this month
        /// - FREQ=YEARLY: this year
        ///
        /// Only Regular tasks can be invalidated. OneTime tasks cannot be reset.
        /// Only household owners can invalidate executions.
        /// </remarks>
        [HttpPost("{taskId:guid}/invalidate-execution")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<string>>> InvalidateExecutionInCurrentPeriod(
            [FromRoute] Guid householdId,
            [FromRoute] Guid taskId,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} invalidating execution for task {TaskId} in current period", userId, taskId);

            await _taskExecutionService.InvalidateExecutionInCurrentPeriodAsync(taskId, userId, cancellationToken);

            return Ok(ApiResponse<string>.SuccessResponse(
                "Execution invalidated successfully. Task can now be completed again in the current period. Previous execution remains in history.",
                "Execution invalidated successfully"));
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
        [ApiExplorerSettings(IgnoreApi = true)]
        [NonAction]
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
                _logger.LogError("User ID (sub claim) not found in JWT token. This indicates a configuration issue with Auth0.");
                throw Domain.Exceptions.AuthenticationException.MissingUserIdClaim();
            }

            return userId;
        }

        #endregion
    }
}
