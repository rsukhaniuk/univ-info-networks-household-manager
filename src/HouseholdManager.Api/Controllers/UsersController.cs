using HouseholdManager.Application.DTOs.Common;
using HouseholdManager.Application.DTOs.User;
using HouseholdManager.Application.Interfaces.ExternalServices;
using HouseholdManager.Application.Interfaces.Services;
using HouseholdManager.Api.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HouseholdManager.Api.Controllers
{
    /// <summary>
    /// API controller for user profile and dashboard operations
    /// </summary>
    /// <remarks>
    /// Manages user profile information, current household selection, and dashboard statistics.
    /// All endpoints require authentication via Auth0 JWT token.
    /// </remarks>
    [Authorize]
    [ApiController]
    [Route("api/users")]
    [Produces("application/json")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuth0ManagementApiClient _auth0Client;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserService userService,
            IAuth0ManagementApiClient auth0Client,
            ILogger<UsersController> logger)
        {
            _userService = userService;
            _auth0Client = auth0Client;
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

        #region Account Management (Password & Email)

        /// <summary>
        /// Request password change ticket (Auth0 Hosted Page)
        /// </summary>
        /// <param name="request">Password change request with result URL</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Ticket URL to redirect user to Auth0 password change page</returns>
        /// <remarks>
        /// This endpoint generates a password reset ticket for Auth0's hosted password change page.
        /// 
        /// **Process:**
        /// 1. User clicks "Change Password" in your SPA
        /// 2. SPA calls this endpoint with `resultUrl` (where to redirect after password change)
        /// 3. API returns ticket URL
        /// 4. SPA redirects user to ticket URL (Auth0 hosted page)
        /// 5. User changes password on Auth0 page
        /// 6. Auth0 redirects back to your `resultUrl`
        /// 7. Show toast: "Password updated. Please sign in again"
        /// 
        /// **Limitations:**
        /// - Only works for auth0 connection users (email/password)
        /// - Social login users (Google, Microsoft) must change password in their provider account
        /// 
        /// Sample request:
        /// 
        ///     POST /api/users/me/password-reset-ticket
        ///     {
        ///        "resultUrl": "https://localhost:4200/profile"
        ///     }
        /// 
        /// Sample response:
        /// 
        ///     {
        ///        "ticketUrl": "https://your-tenant.auth0.com/lo/reset?ticket=abc123...",
        ///        "message": "Redirecting to password change page..."
        ///     }
        /// 
        /// </remarks>
        [HttpPost("me/password-reset-ticket")]
        [ProducesResponseType(typeof(ApiResponse<PasswordChangeTicketResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<PasswordChangeTicketResponse>>> RequestPasswordChange(
            [FromBody] RequestPasswordChangeRequest request,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} requesting password change ticket", userId);

            // Check if user can change password (only auth0 connection)
            var canChangePassword = await _auth0Client.CanUserChangePasswordAsync(userId);
            if (!canChangePassword)
            {
                var connection = await _auth0Client.GetUserConnectionAsync(userId);
                throw new Domain.Exceptions.ValidationException(
                    "password",
                    $"Password change is not available for {connection} connection. " +
                    $"Please change your password in your {GetProviderName(connection)} account.");
            }

            // Create ticket
            var ticketUrl = await _auth0Client.CreatePasswordChangeTicketAsync(userId, request.ResultUrl);

            var response = new PasswordChangeTicketResponse
            {
                TicketUrl = ticketUrl,
                Message = "Redirecting to password change page..."
            };

            return Ok(ApiResponse<PasswordChangeTicketResponse>.SuccessResponse(
                response,
                "Password change ticket created successfully"));
        }

        /// <summary>
        /// Change user email (admin operation)
        /// </summary>
        /// <param name="request">Email change request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success message</returns>
        /// <remarks>
        /// This endpoint changes the user's email address in Auth0.
        /// 
        /// **Important:**
        /// - Only works for auth0 connection users (email/password)
        /// - Social login users (Google, Microsoft) must change email in their provider account
        /// - By default, email is changed immediately without verification (admin operation)
        /// - Set `verifyEmail: true` to send verification email to new address
        /// 
        /// **After email change:**
        /// - User should logout and login again to refresh JWT claims
        /// - Old email will no longer work for login
        /// 
        /// **Security:**
        /// - Consider rate limiting (3-5 email changes per hour per user)
        /// - Log email changes for audit purposes
        /// 
        /// Sample request:
        /// 
        ///     POST /api/users/me/change-email
        ///     {
        ///        "newEmail": "newemail@example.com",
        ///        "verifyEmail": false
        ///     }
        /// 
        /// </remarks>
        [HttpPost("me/change-email")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<object>>> ChangeEmail(
            [FromBody] ChangeEmailRequest request,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} requesting email change to {NewEmail}", userId, request.NewEmail);

            // Check connection type
            var connection = await _auth0Client.GetUserConnectionAsync(userId);
            var isAuth0Connection = connection.Equals("auth0", StringComparison.OrdinalIgnoreCase) ||
                                    connection.Equals("Username-Password-Authentication", StringComparison.OrdinalIgnoreCase);

            if (!isAuth0Connection)
            {
                throw new Domain.Exceptions.ValidationException(
                    "email",
                    $"Email change is not available for {connection} connection. " +
                    $"Please change your email in your {GetProviderName(connection)} account.");
            }

            // Get current user to check if email is different
            var user = await _userService.GetUserByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                throw new Domain.Exceptions.NotFoundException("User", userId);
            }

            if (user.Email.Equals(request.NewEmail, StringComparison.OrdinalIgnoreCase))
            {
                throw new Domain.Exceptions.ValidationException(
                    "newEmail",
                    "New email must be different from current email");
            }

            // Change email in Auth0
            await _auth0Client.ChangeUserEmailAsync(userId, request.NewEmail, request.VerifyEmail);

            // Update local database immediately for consistency
            await _userService.SyncUserFromAuth0Async(
                userId,
                request.NewEmail,
                user.FirstName,
                user.LastName,
                null, // Keep existing profile picture
                cancellationToken);

            // Clear sync cache for this user so next request will re-sync from Auth0
            UserSyncMiddleware.ClearCache(userId);

            _logger.LogInformation("Successfully updated email for user {UserId} to {NewEmail}", userId, request.NewEmail);

            var message = request.VerifyEmail
                ? "Email change requested. Please check your new email inbox to verify. You will need to sign in again."
                : "Email changed successfully. Please sign in again to refresh your authentication token.";

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                Message = message,
                RequiresReAuthentication = true,
                NewEmail = request.NewEmail
            }));
        }

        /// <summary>
        /// Check if user can change password/email (connection type)
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Connection info and capabilities</returns>
        /// <remarks>
        /// Returns information about user's Auth0 connection type and what account operations are available.
        /// 
        /// Use this endpoint to show/hide password/email change buttons in your UI.
        /// 
        /// Example response:
        /// 
        ///     {
        ///        "connection": "auth0",
        ///        "canChangePassword": true,
        ///        "canChangeEmail": true,
        ///        "providerName": "Email/Password"
        ///     }
        /// 
        /// For social logins:
        /// 
        ///     {
        ///        "connection": "google-oauth2",
        ///        "canChangePassword": false,
        ///        "canChangeEmail": false,
        ///        "providerName": "Google"
        ///     }
        /// 
        /// </remarks>
        [HttpGet("me/connection-info")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<object>>> GetConnectionInfo(
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            var connection = await _auth0Client.GetUserConnectionAsync(userId);
            var canChange = connection.Equals("auth0", StringComparison.OrdinalIgnoreCase) ||
                            connection.Equals("Username-Password-Authentication", StringComparison.OrdinalIgnoreCase);

            var info = new
            {
                connection,
                canChangePassword = canChange,
                canChangeEmail = canChange,
                providerName = GetProviderName(connection)
            };

            return Ok(ApiResponse<object>.SuccessResponse(info));
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
                _logger.LogError("User ID (sub claim) not found in JWT token. " +
                    "This may occur after account changes (email/password). User should re-authenticate.");
                throw Domain.Exceptions.AuthenticationException.MissingUserIdClaim();
            }

            return userId;
        }

        /// <summary>
        /// Get friendly provider name from connection string
        /// </summary>
        private static string GetProviderName(string connection) => connection.ToLower() switch
        {
            "auth0" => "Email/Password",
            "username-password-authentication" => "Email/Password",
            "google-oauth2" => "Google",
            "windowslive" => "Microsoft",
            "github" => "GitHub",
            "facebook" => "Facebook",
            "twitter" => "Twitter",
            _ => connection
        };

        #endregion
    }
}
