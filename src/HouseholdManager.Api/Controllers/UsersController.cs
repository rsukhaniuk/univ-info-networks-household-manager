using HouseholdManager.Application.DTOs.Common;
using HouseholdManager.Application.DTOs.User;
using HouseholdManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace HouseholdManager.Api.Controllers
{
    /// <summary>
    /// API controller for user profile and dashboard operations
    /// </summary>
    /// <remarks>
    /// Manages user profile information, current household selection, and dashboard statistics.
    /// All endpoints require authentication (Auth0 JWT token in Phase 2).
    /// </remarks>
    // TODO Lab 3 Phase 2: Add [Authorize] after Auth0 integration
    [ApiController]
    [Route("api/users")]
    [Produces("application/json")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserService userService,
            ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        #region Profile Operations

        /// <summary>
        /// Get current user's profile with household memberships and statistics
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>User profile with statistics and household list</returns>
        /// <remarks>
        /// Returns complete user profile including:
        /// - Basic user info (name, email, role)
        /// - Dashboard statistics (households, tasks, activity)
        /// - List of household memberships with roles
        /// - Current household selection
        /// </remarks>
        [HttpGet("me")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetMyProfile(
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} requesting profile", userId);

            var profile = await _userService.GetUserProfileAsync(userId, cancellationToken);

            return Ok(ApiResponse<UserProfileDto>.SuccessResponse(
                profile,
                "Profile retrieved successfully"));
        }

        /// <summary>
        /// Update current user's profile information
        /// </summary>
        /// <param name="request">Updated profile data (first name, last name)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated user profile</returns>
        /// <remarks>
        /// Only first name and last name can be updated.
        /// Email is managed through Auth0 and cannot be changed here.
        /// 
        /// Sample request:
        /// 
        ///     PUT /api/users/me
        ///     {
        ///        "firstName": "John",
        ///        "lastName": "Doe"
        ///     }
        /// 
        /// </remarks>
        [HttpPut("me")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateMyProfile(
            [FromBody] UpdateProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} updating profile", userId);

            var user = await _userService.UpdateProfileAsync(userId, request, cancellationToken);

            return Ok(ApiResponse<UserDto>.SuccessResponse(
                user,
                "Profile updated successfully"));
        }

        #endregion

        #region Current Household Management

        /// <summary>
        /// Set the current active household for the user
        /// </summary>
        /// <param name="request">Current household selection (householdId or null to clear)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>No content on success</returns>
        /// <remarks>
        /// Sets which household is currently active for the user.
        /// Used by the UI to determine which household's data to display.
        /// 
        /// User must be a member of the household to select it.
        /// Pass null householdId to clear current household selection.
        /// 
        /// Sample request:
        /// 
        ///     PUT /api/users/me/current-household
        ///     {
        ///        "householdId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        ///     }
        /// 
        /// To clear:
        /// 
        ///     PUT /api/users/me/current-household
        ///     {
        ///        "householdId": null
        ///     }
        /// 
        /// </remarks>
        [HttpPut("me/current-household")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SetCurrentHousehold(
            [FromBody] SetCurrentHouseholdRequest request,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation(
                "User {UserId} setting current household to {HouseholdId}",
                userId,
                request.HouseholdId?.ToString() ?? "null");

            await _userService.SetCurrentHouseholdAsync(userId, request, cancellationToken);

            return NoContent(); // 204 No Content
        }

        #endregion

        #region Dashboard & Statistics

        /// <summary>
        /// Get dashboard statistics for current user
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dashboard statistics including households, tasks, and activity</returns>
        /// <remarks>
        /// Returns summary statistics for the user's dashboard:
        /// - Total number of households user belongs to
        /// - Number of households user owns
        /// - Total active tasks assigned to user
        /// - Number of tasks completed this week
        /// - Last activity timestamp
        /// </remarks>
        [HttpGet("me/dashboard")]
        [ProducesResponseType(typeof(ApiResponse<UserDashboardStats>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<UserDashboardStats>>> GetDashboardStats(
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} requesting dashboard stats", userId);

            var stats = await _userService.GetUserDashboardStatsAsync(userId, cancellationToken);

            return Ok(ApiResponse<UserDashboardStats>.SuccessResponse(
                stats,
                "Dashboard statistics retrieved successfully"));
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets current user ID from request context
        /// </summary>
        /// <returns>User ID string</returns>
        /// <remarks>
        /// TODO Lab 3 Phase 2: Replace with Auth0 JWT claim extraction
        /// Currently uses X-User-Id header for testing
        /// </remarks>
        private string GetCurrentUserId()
        {
            // TODO Lab 3 Phase 2: Extract from Auth0 JWT token
            // var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            // if (string.IsNullOrEmpty(userId))
            //     throw new UnauthorizedException("User ID not found in token");

            // Temporary: use header for testing without auth
            var userId = HttpContext.Request.Headers["X-User-Id"].FirstOrDefault();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("No X-User-Id header found, using default test user");
                return "test-user-123";
            }

            return userId;
        }

        #endregion
    }
}
