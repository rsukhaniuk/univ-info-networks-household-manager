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

        /// <summary>
        /// Initializes a new instance of ExecutionsController
        /// </summary>
        public ExecutionsController(ITaskExecutionService executionService)
        {
            _executionService = executionService;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in claims");

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
            await _executionService.ValidateExecutionAccessAsync(id, UserId);

            var execution = await _executionService.GetExecutionWithRelationsAsync(id);

            return Ok(ApiResponse<ExecutionDto>.SuccessResponse(
                execution,
                "Execution retrieved successfully"));
        }

        /// <summary>
        /// Get all executions for a specific task
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="parameters">Query parameters for filtering and pagination</param>
        /// <returns>Paginated list of task executions</returns>
        /// <response code="200">Returns the paginated list of executions</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User doesn't have access to this task</response>
        /// <response code="422">Invalid query parameters</response>
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
        /// <param name="householdId">Household ID</param>
        /// <param name="parameters">Query parameters for filtering and pagination</param>
        /// <returns>Paginated list of household executions</returns>
        /// <response code="200">Returns the paginated list of executions</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User is not a member of this household</response>
        /// <response code="422">Invalid query parameters</response>
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
            var executions = await _executionService.GetUserExecutionsAsync(UserId, householdId);

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
            var execution = await _executionService.CompleteTaskAsync(
                request,
                UserId,
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
            var execution = await _executionService.UpdateExecutionAsync(
                id,
                request,
                UserId);

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
            await _executionService.DeleteExecutionAsync(id, UserId);

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
            if (photo == null || photo.Length == 0)
            {
                throw new HouseholdManager.Domain.Exceptions.ValidationException(
                    "photo",
                    "Photo file is required");
            }

            var photoPath = await _executionService.UploadExecutionPhotoAsync(id, photo, UserId);

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
            await _executionService.DeleteExecutionPhotoAsync(id, UserId);

            return NoContent();
        }

        #endregion
    }
}
