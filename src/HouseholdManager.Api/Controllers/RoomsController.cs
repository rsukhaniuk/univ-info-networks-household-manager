using HouseholdManager.Application.DTOs.Common;
using HouseholdManager.Application.DTOs.Room;
using HouseholdManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace HouseholdManager.Api.Controllers
{
    /// <summary>
    /// API controller for room management operations within households
    /// </summary>
    [ApiController]
    [Route("api/households/{householdId:guid}/rooms")]
    [Produces("application/json")]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;
        private readonly IHouseholdService _householdService;
        private readonly ILogger<RoomsController> _logger;

        public RoomsController(
            IRoomService roomService,
            IHouseholdService householdService,
            ILogger<RoomsController> logger)
        {
            _roomService = roomService;
            _householdService = householdService;
            _logger = logger;
        }

        #region CRUD Operations

        /// <summary>
        /// Get all rooms in a household with optional filtering and sorting
        /// </summary>
        /// <param name="householdId">Household ID (GUID)</param>
        /// <param name="queryParameters">Query parameters for filtering and sorting</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of rooms in the household</returns>
        /// <remarks>
        /// Query parameters:
        /// - **SortBy**: Sort field (e.g., "Priority", "Name", "CreatedAt")
        /// - **SortOrder**: "asc" or "desc" (default: "desc")
        /// - **Search**: Search by room name or description
        /// - **MinPriority**: Filter rooms with priority >= value
        /// - **MaxPriority**: Filter rooms with priority &lt;= value
        /// - **HasPhoto**: Filter rooms with photos (true) or without (false)
        /// - **HasActiveTasks**: Filter rooms with active tasks (true) or without (false)
        /// 
        /// Example: `GET /api/households/{id}/rooms?sortBy=Priority&amp;sortOrder=desc&amp;search=kitchen`
        /// </remarks>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RoomDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<RoomDto>>>> GetHouseholdRooms(
            Guid householdId,
            [FromQuery] RoomQueryParameters queryParameters,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation(
                "User {UserId} requesting rooms for household {HouseholdId} with filters: Search={Search}",
                userId, householdId, queryParameters?.Search);

            // Validate household exists and user has access (throws exceptions if fails)
            await _householdService.ValidateUserAccessAsync(householdId, userId, cancellationToken);

            // Get all rooms for household
            var rooms = await _roomService.GetHouseholdRoomsAsync(householdId, cancellationToken);

            // Apply filters in controller (TODO: move to service/repository in future)
            var filteredRooms = rooms.AsQueryable();

            if (queryParameters != null)
            {
                // Search filter
                if (!string.IsNullOrWhiteSpace(queryParameters.Search))
                {
                    filteredRooms = filteredRooms.Where(r =>
                        r.Name.Contains(queryParameters.Search, StringComparison.OrdinalIgnoreCase) ||
                        (r.Description != null && r.Description.Contains(queryParameters.Search, StringComparison.OrdinalIgnoreCase)));
                }

                // Priority filters
                if (queryParameters.MinPriority.HasValue)
                {
                    filteredRooms = filteredRooms.Where(r => r.Priority >= queryParameters.MinPriority.Value);
                }

                if (queryParameters.MaxPriority.HasValue)
                {
                    filteredRooms = filteredRooms.Where(r => r.Priority <= queryParameters.MaxPriority.Value);
                }

                // HasPhoto filter
                if (queryParameters.HasPhoto.HasValue)
                {
                    filteredRooms = queryParameters.HasPhoto.Value
                        ? filteredRooms.Where(r => !string.IsNullOrEmpty(r.PhotoPath))
                        : filteredRooms.Where(r => string.IsNullOrEmpty(r.PhotoPath));
                }

                // HasActiveTasks filter
                if (queryParameters.HasActiveTasks.HasValue)
                {
                    filteredRooms = queryParameters.HasActiveTasks.Value
                        ? filteredRooms.Where(r => r.ActiveTaskCount > 0)
                        : filteredRooms.Where(r => r.ActiveTaskCount == 0);
                }

                // Apply sorting
                filteredRooms = queryParameters.SortBy?.ToLower() switch
                {
                    "name" => queryParameters.IsAscending
                        ? filteredRooms.OrderBy(r => r.Name)
                        : filteredRooms.OrderByDescending(r => r.Name),
                    "createdat" => queryParameters.IsAscending
                        ? filteredRooms.OrderBy(r => r.CreatedAt)
                        : filteredRooms.OrderByDescending(r => r.CreatedAt),
                    _ => queryParameters.IsAscending // Default: Priority
                        ? filteredRooms.OrderBy(r => r.Priority)
                        : filteredRooms.OrderByDescending(r => r.Priority)
                };
            }

            var result = filteredRooms.ToList();

            return Ok(ApiResponse<IReadOnlyList<RoomDto>>.SuccessResponse(
                result,
                $"Retrieved {result.Count} room(s) successfully"));
        }

        /// <summary>
        /// Get room details by ID
        /// </summary>
        /// <param name="householdId">Household ID (GUID)</param>
        /// <param name="id">Room ID (GUID)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Room details with associated tasks and statistics</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<RoomWithTasksDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<RoomWithTasksDto>>> GetRoomDetails(
            Guid householdId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation(
                "User {UserId} requesting details for room {RoomId} in household {HouseholdId}",
                userId, id, householdId);

            // Validate room access (throws UnauthorizedException if no access)
            await _roomService.ValidateRoomAccessAsync(id, userId, cancellationToken);

            // Get room with tasks (throws NotFoundException if not found)
            var room = await _roomService.GetRoomWithTasksAsync(id, cancellationToken);

            return Ok(ApiResponse<RoomWithTasksDto>.SuccessResponse(room));
        }

        /// <summary>
        /// Create a new room in a household
        /// </summary>
        /// <param name="householdId">Household ID (GUID)</param>
        /// <param name="request">Room creation data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created room</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/households/{householdId}/rooms
        ///     {
        ///        "name": "Living Room",
        ///        "description": "Main living area with TV and sofa",
        ///        "priority": 8
        ///     }
        /// 
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<RoomDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<RoomDto>>> CreateRoom(
            Guid householdId,
            [FromBody] UpsertRoomRequest request,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation(
                "User {UserId} creating room '{RoomName}' in household {HouseholdId}",
                userId, request.Name, householdId);

            // Validate owner access (throws ForbiddenException if not owner)
            await _householdService.ValidateOwnerAccessAsync(householdId, userId, cancellationToken);

            // Ensure householdId matches
            request.HouseholdId = householdId;
            request.Id = null; // Ensure no ID on create

            // Service will validate name uniqueness and throw ValidationException if duplicate
            var room = await _roomService.CreateRoomAsync(request, userId, cancellationToken);

            _logger.LogInformation(
                "User {UserId} created room {RoomId} in household {HouseholdId}",
                userId, room.Id, householdId);

            return CreatedAtAction(
                nameof(GetRoomDetails),
                new { householdId, id = room.Id },
                ApiResponse<RoomDto>.SuccessResponse(
                    room,
                    "Room created successfully"));
        }

        /// <summary>
        /// Update an existing room
        /// </summary>
        /// <param name="householdId">Household ID (GUID)</param>
        /// <param name="id">Room ID (GUID)</param>
        /// <param name="request">Updated room data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated room</returns>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<RoomDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<RoomDto>>> UpdateRoom(
            Guid householdId,
            Guid id,
            [FromBody] UpsertRoomRequest request,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation(
                "User {UserId} updating room {RoomId} in household {HouseholdId}",
                userId, id, householdId);

            // Validate owner access to room (throws ForbiddenException if not owner)
            await _roomService.ValidateRoomOwnerAccessAsync(id, userId, cancellationToken);

            // Ensure IDs match
            request.Id = id;
            request.HouseholdId = householdId;

            // Service will validate name uniqueness and throw ValidationException if duplicate
            var room = await _roomService.UpdateRoomAsync(id, request, userId, cancellationToken);

            _logger.LogInformation(
                "User {UserId} updated room {RoomId} in household {HouseholdId}",
                userId, id, householdId);

            return Ok(ApiResponse<RoomDto>.SuccessResponse(
                room,
                "Room updated successfully"));
        }

        /// <summary>
        /// Delete a room and all associated tasks
        /// </summary>
        /// <param name="householdId">Household ID (GUID)</param>
        /// <param name="id">Room ID (GUID)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>No content on success</returns>
        /// <remarks>
        /// **WARNING:** This operation cascades to all tasks in the room and their executions.
        /// Room photo is also deleted from the filesystem.
        /// </remarks>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteRoom(
            Guid householdId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation(
                "User {UserId} deleting room {RoomId} from household {HouseholdId}",
                userId, id, householdId);

            // Validate owner access (throws ForbiddenException if not owner)
            await _roomService.ValidateRoomOwnerAccessAsync(id, userId, cancellationToken);

            await _roomService.DeleteRoomAsync(id, userId, cancellationToken);

            return NoContent(); // 204 No Content - standard for DELETE
        }

        #endregion

        #region Photo Management

        /// <summary>
        /// Upload or replace room photo
        /// </summary>
        /// <param name="householdId">Household ID (GUID)</param>
        /// <param name="id">Room ID (GUID)</param>
        /// <param name="photo">Photo file (JPEG/PNG, max 5MB)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Photo URL</returns>
        /// <remarks>
        /// Supported formats: JPEG (.jpg, .jpeg), PNG (.png)  
        /// Maximum file size: 5 MB
        /// 
        /// If a photo already exists, it will be replaced.
        /// </remarks>
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
            Guid householdId,
            Guid id,
            IFormFile photo,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation(
                "User {UserId} uploading photo for room {RoomId} in household {HouseholdId}",
                userId, id, householdId);

            // Validate owner access (throws ForbiddenException if not owner)
            await _roomService.ValidateRoomOwnerAccessAsync(id, userId, cancellationToken);

            // Service validates file and handles upload (throws ValidationException if invalid)
            var photoPath = await _roomService.UploadRoomPhotoAsync(
                id,
                photo,
                userId,
                cancellationToken);

            _logger.LogInformation(
                "User {UserId} uploaded photo for room {RoomId}: {PhotoPath}",
                userId, id, photoPath);

            return Ok(ApiResponse<string>.SuccessResponse(
                photoPath,
                "Room photo uploaded successfully"));
        }

        /// <summary>
        /// Delete room photo
        /// </summary>
        /// <param name="householdId">Household ID (GUID)</param>
        /// <param name="id">Room ID (GUID)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>No content on success</returns>
        [HttpDelete("{id:guid}/photo")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeletePhoto(
            Guid householdId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation(
                "User {UserId} deleting photo for room {RoomId} in household {HouseholdId}",
                userId, id, householdId);

            // Validate owner access (throws ForbiddenException if not owner)
            await _roomService.ValidateRoomOwnerAccessAsync(id, userId, cancellationToken);

            await _roomService.DeleteRoomPhotoAsync(id, userId, cancellationToken);

            return NoContent(); // 204 No Content - standard for DELETE
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get current user ID from HTTP context
        /// TODO: Replace with Auth0 JWT claim extraction in Lab 3
        /// </summary>
        /// <returns>User ID string</returns>
        private string GetCurrentUserId()
        {
            // Temporary implementation - read from header or use default
            var userId = HttpContext.Request.Headers["X-User-Id"].FirstOrDefault();

            if (string.IsNullOrEmpty(userId))
            {
                // For development/testing - use hardcoded user
                // TODO: Remove this default in production
                userId = "test-user-123";
                _logger.LogWarning("No X-User-Id header provided, using default test user");
            }

            return userId;
        }

        #endregion
    }
}
