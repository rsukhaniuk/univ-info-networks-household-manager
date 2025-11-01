using HouseholdManager.Application.DTOs.Common;
using HouseholdManager.Application.DTOs.Execution;
using HouseholdManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HouseholdManager.Api.Controllers
{
    /// <summary>
    /// Controller for managing task executions (completions) and execution history
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ExecutionsController : ControllerBase
    {
        private readonly ITaskExecutionService _executionService;
        private readonly ILogger<ExecutionsController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionsController"/> class.
        /// </summary>
        /// <param name="executionService">The service responsible for managing task executions. This parameter cannot be <see langword="null"/>.</param>
        /// <param name="logger">The logger instance used to log diagnostic and operational messages. This parameter cannot be <see
        /// langword="null"/>.</param>
        public ExecutionsController(
            ITaskExecutionService executionService,
            ILogger<ExecutionsController> logger)
        {
            _executionService = executionService;
            _logger = logger;
        }

        #region Query Operations

        /// <summary>
        /// Get execution by ID
        /// </summary>
        /// <param name="id">Execution ID</param>
        /// <returns>Execution details</returns>
        /// <response code="200">Returns the execution</response>
        /// <response code="404">Execution not found</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User doesn't have access to this execution</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ExecutionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<ExecutionDto>>> GetExecution(Guid id)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} fetching execution {ExecutionId}", userId, id);

            await _executionService.ValidateExecutionAccessAsync(id, userId);

            var execution = await _executionService.GetExecutionWithRelationsAsync(id);

            return Ok(ApiResponse<ExecutionDto>.SuccessResponse(
                execution,
                "Execution retrieved successfully"));
        }

        /// <summary>
        /// Get all executions for a specific task
        /// </summary>
        /// <param name="taskId">Task ID (from route)</param>
        /// <param name="parameters">Query parameters for filtering and pagination</param>
        /// <returns>Paginated list of task executions</returns>
        /// <remarks>
        /// Query parameters:
        /// - **Page**: Page number (default: 1)  
        /// - **PageSize**: Items per page (default: 20, max: 100)  
        /// - **SortBy**: Sort field (e.g., "CompletedAt")  
        /// - **SortOrder**: "asc" or "desc" (default: "desc")  
        /// - **Search**: Free-text search in notes/task title  
        /// - **UserId**: Filter by user ID  
        /// - **RoomId**: Filter by room ID  
        /// - **CompletedAfter / CompletedBefore**: UTC ISO 8601 range  
        /// - **WeekStarting**: Monday (UTC), for weekly stats  
        /// - **ThisWeekOnly**: true/false  
        /// - **HasPhoto**: true/false  
        ///
        /// Example:  
        /// `GET /api/executions/task/{taskId}?page=1&amp;pageSize=10&amp;hasPhoto=true&amp;sortBy=CompletedAt&amp;sortOrder=desc`
        /// </remarks>
        [HttpGet("task/{taskId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<ExecutionDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<PagedResult<ExecutionDto>>>> GetTaskExecutions(
            Guid taskId,
            [FromQuery] ExecutionQueryParameters parameters)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} fetching executions for task {TaskId}", userId, taskId);

            parameters.TaskId = taskId;

            var executions = await _executionService.GetTaskExecutionsAsync(taskId);

            // Apply filtering
            var filtered = executions.AsQueryable();

            if (parameters.CompletedAfter.HasValue)
                filtered = filtered.Where(e => e.CompletedAt >= parameters.CompletedAfter.Value);

            if (parameters.CompletedBefore.HasValue)
                filtered = filtered.Where(e => e.CompletedAt <= parameters.CompletedBefore.Value);

            if (parameters.HasPhoto.HasValue)
                filtered = filtered.Where(e => parameters.HasPhoto.Value ? e.HasPhoto : !e.HasPhoto);

            if (!string.IsNullOrEmpty(parameters.Search))
                filtered = filtered.Where(e =>
                    e.TaskTitle.Contains(parameters.Search, StringComparison.OrdinalIgnoreCase) ||
                    (e.Notes != null && e.Notes.Contains(parameters.Search, StringComparison.OrdinalIgnoreCase)));

            // Apply sorting
            filtered = parameters.SortBy?.ToLower() switch
            {
                "completedat" => parameters.IsAscending
                    ? filtered.OrderBy(e => e.CompletedAt)
                    : filtered.OrderByDescending(e => e.CompletedAt),
                "username" => parameters.IsAscending
                    ? filtered.OrderBy(e => e.UserName)
                    : filtered.OrderByDescending(e => e.UserName),
                _ => filtered.OrderByDescending(e => e.CompletedAt)
            };

            // Apply pagination
            var pagedResult = PagedResult<ExecutionDto>.Create(
                filtered,
                parameters.Page,
                parameters.PageSize);

            return Ok(ApiResponse<PagedResult<ExecutionDto>>.SuccessResponse(
                pagedResult,
                $"Retrieved {pagedResult.TotalCount} executions for task"));
        }

        /// <summary>
        /// Get all executions for a household
        /// </summary>
        /// <param name="householdId">Household ID (from route)</param>
        /// <param name="parameters">Query parameters for filtering and pagination</param>
        /// <returns>Paginated list of household executions</returns>
        /// <remarks>
        /// Query parameters:
        /// - **Page** / **PageSize** / **SortBy** / **SortOrder** / **Search**  
        /// - **UserId**, **RoomId**, **CompletedAfter**, **CompletedBefore** (UTC)  
        /// - **WeekStarting** (UTC Monday), **ThisWeekOnly**, **HasPhoto**  
        ///
        /// Example:  
        /// `GET /api/executions/household/{householdId}?roomId=...&amp;completedAfter=2025-10-01T00:00:00Z`
        /// </remarks>
        [HttpGet("household/{householdId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<ExecutionDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<PagedResult<ExecutionDto>>>> GetHouseholdExecutions(
            Guid householdId,
            [FromQuery] ExecutionQueryParameters parameters)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} fetching executions for household {HouseholdId}", userId, householdId);

            parameters.HouseholdId = householdId;

            var executions = await _executionService.GetHouseholdExecutionsAsync(householdId);

            // Apply filtering
            var filtered = executions.AsQueryable();

            if (!string.IsNullOrEmpty(parameters.UserId))
                filtered = filtered.Where(e => e.UserId == parameters.UserId);

            if (parameters.RoomId.HasValue)
                filtered = filtered.Where(e => e.RoomId == parameters.RoomId.Value);

            if (parameters.TaskId.HasValue)
                filtered = filtered.Where(e => e.TaskId == parameters.TaskId.Value);

            if (parameters.CompletedAfter.HasValue)
                filtered = filtered.Where(e => e.CompletedAt >= parameters.CompletedAfter.Value);

            if (parameters.CompletedBefore.HasValue)
                filtered = filtered.Where(e => e.CompletedAt <= parameters.CompletedBefore.Value);

            if (parameters.WeekStarting.HasValue)
                filtered = filtered.Where(e => e.WeekStarting == parameters.WeekStarting.Value);

            if (parameters.ThisWeekOnly == true)
                filtered = filtered.Where(e => e.IsThisWeek);

            if (parameters.HasPhoto.HasValue)
                filtered = filtered.Where(e => parameters.HasPhoto.Value ? e.HasPhoto : !e.HasPhoto);

            if (!string.IsNullOrEmpty(parameters.Search))
                filtered = filtered.Where(e =>
                    e.TaskTitle.Contains(parameters.Search, StringComparison.OrdinalIgnoreCase) ||
                    e.RoomName.Contains(parameters.Search, StringComparison.OrdinalIgnoreCase) ||
                    (e.Notes != null && e.Notes.Contains(parameters.Search, StringComparison.OrdinalIgnoreCase)));

            // Apply sorting
            filtered = parameters.SortBy?.ToLower() switch
            {
                "completedat" => parameters.IsAscending
                    ? filtered.OrderBy(e => e.CompletedAt)
                    : filtered.OrderByDescending(e => e.CompletedAt),
                "tasktitle" => parameters.IsAscending
                    ? filtered.OrderBy(e => e.TaskTitle)
                    : filtered.OrderByDescending(e => e.TaskTitle),
                "roomname" => parameters.IsAscending
                    ? filtered.OrderBy(e => e.RoomName)
                    : filtered.OrderByDescending(e => e.RoomName),
                "username" => parameters.IsAscending
                    ? filtered.OrderBy(e => e.UserName)
                    : filtered.OrderByDescending(e => e.UserName),
                _ => filtered.OrderByDescending(e => e.CompletedAt)
            };

            // Apply pagination
            var pagedResult = PagedResult<ExecutionDto>.Create(
                filtered,
                parameters.Page,
                parameters.PageSize);

            return Ok(ApiResponse<PagedResult<ExecutionDto>>.SuccessResponse(
                pagedResult,
                $"Retrieved {pagedResult.TotalCount} executions for household"));
        }

        /// <summary>
        /// Get user's executions in a household for current week
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <returns>List of user's executions this week</returns>
        /// <response code="200">Returns the list of executions</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User is not a member of this household</response>
        [HttpGet("household/{householdId:guid}/my-week")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ExecutionDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ExecutionDto>>>> GetMyExecutionsThisWeek(
            Guid householdId)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} fetching own executions for household {HouseholdId}", userId, householdId);

            var executions = await _executionService.GetUserExecutionsAsync(userId, householdId);

            return Ok(ApiResponse<IReadOnlyList<ExecutionDto>>.SuccessResponse(
                executions,
                $"Retrieved {executions.Count} executions for this week"));
        }

        /// <summary>
        /// Get weekly executions for a household
        /// </summary>
        /// <param name="householdId">Household ID</param>
        /// <param name="weekStarting">Optional week start date (defaults to current week)</param>
        /// <returns>List of executions for the specified week</returns>
        /// <response code="200">Returns the list of executions</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User is not a member of this household</response>
        [HttpGet("household/{householdId:guid}/weekly")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ExecutionDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ExecutionDto>>>> GetWeeklyExecutions(
            Guid householdId,
            [FromQuery] DateTime? weekStarting = null)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} fetching weekly executions for household {HouseholdId}", userId, householdId);

            var executions = await _executionService.GetWeeklyExecutionsAsync(
                householdId,
                weekStarting);

            var weekDate = weekStarting ?? DateTime.UtcNow;
            return Ok(ApiResponse<IReadOnlyList<ExecutionDto>>.SuccessResponse(
                executions,
                $"Retrieved {executions.Count} executions for week starting {weekDate:yyyy-MM-dd}"));
        }

        /// <summary>
        /// Check if task is completed this week
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <returns>Boolean indicating if task is completed this week</returns>
        /// <response code="200">Returns completion status</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User doesn't have access to this task</response>
        /// <response code="404">Task not found</response>
        [HttpGet("task/{taskId:guid}/completed-this-week")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<bool>>> IsTaskCompletedThisWeek(Guid taskId)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} checking if task {TaskId} is completed this week", userId, taskId);

            var isCompleted = await _executionService.IsTaskCompletedThisWeekAsync(taskId);

            return Ok(ApiResponse<bool>.SuccessResponse(
                isCompleted,
                isCompleted
                    ? "Task is completed this week"
                    : "Task is not completed this week"));
        }

        /// <summary>
        /// Get latest execution for a task
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <returns>Latest execution for the task</returns>
        /// <response code="200">Returns the latest execution</response>
        /// <response code="404">No executions found for this task</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User doesn't have access to this task</response>
        [HttpGet("task/{taskId:guid}/latest")]
        [ProducesResponseType(typeof(ApiResponse<ExecutionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<ExecutionDto>>> GetLatestExecution(Guid taskId)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} fetching latest execution for task {TaskId}", userId, taskId);

            var execution = await _executionService.GetLatestExecutionForTaskAsync(taskId);

            return Ok(ApiResponse<ExecutionDto>.SuccessResponse(
                execution,
                "Latest execution retrieved successfully"));
        }

        #endregion

        #region Command Operations

        /// <summary>
        /// Complete a task (create execution)
        /// </summary>
        /// <param name="request">Task completion request with optional notes and photo path</param>
        /// <param name="photo">Optional photo file</param>
        /// <returns>Created execution</returns>
        /// <response code="201">Task completed successfully</response>
        /// <response code="400">Invalid request data (e.g. task already completed this week)</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User doesn't have access to this task</response>
        /// <response code="404">Task not found</response>
        /// <response code="422">Validation errors</response>
        [HttpPost("complete")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 5 * 1024 * 1024)]
        [ProducesResponseType(typeof(ApiResponse<ExecutionDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<ExecutionDto>>> CompleteTask(
            [FromForm] CompleteTaskRequest request,
            IFormFile? photo = null)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} completing task {TaskId}", userId, request.TaskId);

            var execution = await _executionService.CompleteTaskAsync(
                request,
                userId,
                photo);

            return CreatedAtAction(
                nameof(GetExecution),
                new { id = execution.Id },
                ApiResponse<ExecutionDto>.SuccessResponse(
                    execution,
                    "Task completed successfully"));
        }

        /// <summary>
        /// Update execution notes and/or photo path
        /// </summary>
        /// <param name="id">Execution ID</param>
        /// <param name="request">Update request with notes and photo path</param>
        /// <returns>Updated execution</returns>
        /// <response code="200">Execution updated successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User doesn't have permission to update this execution</response>
        /// <response code="404">Execution not found</response>
        /// <response code="422">Validation errors</response>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ExecutionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<ExecutionDto>>> UpdateExecution(
            Guid id,
            [FromBody] UpdateExecutionRequest request)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} updating execution {ExecutionId}", userId, id);

            var execution = await _executionService.UpdateExecutionAsync(
                id,
                request,
                userId);

            return Ok(ApiResponse<ExecutionDto>.SuccessResponse(
                execution,
                "Execution updated successfully"));
        }

        /// <summary>
        /// Delete an execution
        /// </summary>
        /// <param name="id">Execution ID</param>
        /// <returns>No content</returns>
        /// <response code="204">Execution deleted successfully</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User doesn't have permission to delete this execution</response>
        /// <response code="404">Execution not found</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteExecution(Guid id)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} deleting execution {ExecutionId}", userId, id);

            await _executionService.DeleteExecutionAsync(id, userId);

            return NoContent();
        }

        #endregion

        #region Photo Management

        /// <summary>
        /// Upload or replace execution photo
        /// </summary>
        /// <param name="id">Execution ID</param>
        /// <param name="photo">Photo file to upload (max 5MB, JPEG/PNG only)</param>
        /// <returns>Photo URL</returns>
        /// <response code="200">Photo uploaded successfully</response>
        /// <response code="400">Invalid photo file (wrong format, too large, etc.)</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User doesn't have permission to upload photo</response>
        /// <response code="404">Execution not found</response>
        /// <response code="422">Photo validation failed</response>
        [HttpPost("{id:guid}/photo")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 5 * 1024 * 1024)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<string>>> UploadPhoto(
            Guid id,
            IFormFile photo)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} uploading photo for execution {ExecutionId}", userId, id);

            if (photo == null || photo.Length == 0)
            {
                throw new HouseholdManager.Domain.Exceptions.ValidationException(
                    "photo",
                    "Photo file is required");
            }

            var photoPath = await _executionService.UploadExecutionPhotoAsync(id, photo, userId);

            return Ok(ApiResponse<string>.SuccessResponse(
                photoPath,
                "Photo uploaded successfully"));
        }

        /// <summary>
        /// Delete execution photo
        /// </summary>
        /// <param name="id">Execution ID</param>
        /// <returns>No content</returns>
        /// <response code="204">Photo deleted successfully</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User doesn't have permission to delete photo</response>
        /// <response code="404">Execution not found or no photo exists</response>
        [HttpDelete("{id:guid}/photo")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeletePhoto(Guid id)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} deleting photo for execution {ExecutionId}", userId, id);

            await _executionService.DeleteExecutionPhotoAsync(id, userId);

            return NoContent();
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
