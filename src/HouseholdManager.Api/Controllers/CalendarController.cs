using HouseholdManager.Application.DTOs.Calendar;
using HouseholdManager.Application.DTOs.Common;
using HouseholdManager.Application.Interfaces.Services;
using HouseholdManager.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

namespace HouseholdManager.Api.Controllers
{
    /// <summary>
    /// Controller for calendar export and subscription
    /// Provides iCalendar (.ics) format exports for household tasks
    /// </summary>
    [ApiController]
    [Route("api/households/{householdId:guid}/calendar")]
    [Produces("application/json")]
    [Authorize]
    public class CalendarController : ControllerBase
    {
        private readonly ICalendarExportService _calendarExportService;
        private readonly IHouseholdService _householdService;
        private readonly ILogger<CalendarController> _logger;

        public CalendarController(
            ICalendarExportService calendarExportService,
            IHouseholdService householdService,
            ILogger<CalendarController> logger)
        {
            _calendarExportService = calendarExportService;
            _householdService = householdService;
            _logger = logger;
        }

        /// <summary>
        /// Export household tasks as iCalendar file (.ics) for download
        /// </summary>
        /// <param name="householdId">Household ID (from route)</param>
        /// <param name="startDate">Optional start date for filtering tasks (ISO 8601 format)</param>
        /// <param name="endDate">Optional end date for filtering tasks (ISO 8601 format)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>iCalendar file (.ics) containing household tasks</returns>
        /// <remarks>
        /// Download this file and import it to your calendar application:
        /// - **Google Calendar**: Settings → Import & Export → Import
        /// - **Outlook**: File → Open & Export → Import/Export
        /// - **Apple Calendar**: File → Import
        ///
        /// The exported calendar includes:
        /// - All active tasks (Regular and OneTime)
        /// - Task recurrence patterns (weekly, custom RRULE)
        /// - Completion history for recent executions
        /// - Task metadata (room, assigned user, priority)
        ///
        /// Example:
        /// GET /api/households/{householdId}/calendar/export.ics
        /// GET /api/households/{householdId}/calendar/export.ics?startDate=2025-01-01 and endDate=2025-12-31
        /// </remarks>
        [HttpGet("export.ics")]
        [Produces("text/calendar")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ExportCalendar(
            [FromRoute] Guid householdId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation(
                "User {UserId} exporting calendar for household {HouseholdId}",
                userId,
                householdId);

            // Validate user access
            await _householdService.ValidateUserAccessAsync(householdId, userId, cancellationToken);

            // Export calendar
            var icalContent = await _calendarExportService.ExportHouseholdTasksAsync(
                householdId,
                userId,
                startDate,
                endDate,
                cancellationToken);

            var bytes = Encoding.UTF8.GetBytes(icalContent);
            var fileName = $"household-tasks-{householdId:N}.ics";

            return File(
                bytes,
                "text/calendar",
                fileName);
        }

        /// <summary>
        /// Get calendar subscription URL for automatic updates (CalDAV feed)
        /// </summary>
        /// <param name="householdId">Household ID (from route)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Calendar subscription information with URL and instructions</returns>
        /// <remarks>
        /// Subscribe to this calendar feed for automatic updates:
        /// - **Google Calendar**: Add calendar → From URL
        /// - **Outlook**: Add calendar → Subscribe from web
        /// - **Apple Calendar**: File → New Calendar Subscription
        ///
        /// The calendar feed automatically updates when tasks are:
        /// - Created, modified, or deleted
        /// - Completed or invalidated
        /// - Assigned or reassigned
        ///
        /// Refresh frequency depends on your calendar application settings (typically 15 minutes to 24 hours).
        ///
        /// Example:
        /// `GET /api/households/{householdId}/calendar/subscription`
        /// </remarks>
        [HttpGet("subscription")]
        [ProducesResponseType(typeof(ApiResponse<CalendarSubscriptionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<CalendarSubscriptionDto>>> GetSubscriptionUrl(
            [FromRoute] Guid householdId,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation(
                "User {UserId} getting calendar subscription URL for household {HouseholdId}",
                userId,
                householdId);

            // Validate user access
            await _householdService.ValidateUserAccessAsync(householdId, userId, cancellationToken);

            // Get subscription URL
            var subscriptionInfo = await _calendarExportService.GetSubscriptionUrlAsync(
                householdId,
                userId,
                cancellationToken);

            return Ok(ApiResponse<CalendarSubscriptionDto>.SuccessResponse(
                subscriptionInfo,
                "Calendar subscription URL generated successfully"));
        }

        /// <summary>
        /// Calendar feed endpoint for CalDAV subscriptions
        /// </summary>
        /// <param name="householdId">Household ID (from route)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>iCalendar feed content (dynamically generated)</returns>
        /// <remarks>
        /// This endpoint is used by calendar applications that subscribe to the feed.
        /// It returns the same content as the export endpoint but is meant for automated polling.
        ///
        /// **Note:** In production, this endpoint should require authentication via:
        /// - Query parameter token: `?token={jwt}`
        /// - Or header-based authentication
        ///
        /// Example:
        /// `GET /api/households/{householdId}/calendar/feed.ics`
        /// </remarks>
        [HttpGet("feed.ics")]
        [Produces("text/calendar")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetCalendarFeed(
            [FromRoute] Guid householdId,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation(
                "User {UserId} accessing calendar feed for household {HouseholdId}",
                userId,
                householdId);

            // Validate user access
            await _householdService.ValidateUserAccessAsync(householdId, userId, cancellationToken);

            // Export calendar (same as export, but meant for subscription/polling)
            var icalContent = await _calendarExportService.ExportHouseholdTasksAsync(
                householdId,
                userId,
                cancellationToken: cancellationToken);

            var bytes = Encoding.UTF8.GetBytes(icalContent);

            // Set headers for calendar feed (not download)
            Response.Headers.Append("Content-Disposition", "inline");
            Response.Headers.Append("Cache-Control", "private, max-age=900"); // 15 minutes
            Response.Headers.Append("X-Published-TTL", "PT15M"); // CalDAV refresh interval

            return File(
                bytes,
                "text/calendar; charset=utf-8");
        }

        #region Private Helper Methods

        private string GetCurrentUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                throw AuthenticationException.MissingUserIdClaim();
            }

            return userId;
        }

        #endregion
    }
}
