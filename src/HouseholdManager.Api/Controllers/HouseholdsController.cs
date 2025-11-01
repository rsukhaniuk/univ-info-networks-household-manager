using HouseholdManager.Application.DTOs.Common;
using HouseholdManager.Application.DTOs.Household;
using HouseholdManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Authentication;
using System.Security.Claims;

namespace HouseholdManager.Api.Controllers
{
    /// <summary>
    /// API controller for household management operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class HouseholdsController : ControllerBase
    {
        private readonly IHouseholdService _householdService;
        private readonly ILogger<HouseholdsController> _logger;

        public HouseholdsController(
            IHouseholdService householdService,
            ILogger<HouseholdsController> logger)
        {
            _householdService = householdService;
            _logger = logger;
        }

        #region CRUD Operations

        /// <summary>
        /// Get all households for current user with optional pagination and filtering
        /// </summary>
        /// <param name="queryParameters">Query parameters for filtering, sorting, and pagination</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of user's households</returns>
        /// <remarks>
        /// Query parameters:
        /// - **Page**: Page number (default: 1)
        /// - **PageSize**: Items per page (default: 20, max: 100)
        /// - **SortBy**: Sort field (e.g., "CreatedAt", "Name")
        /// - **SortOrder**: "asc" or "desc" (default: "desc")
        /// - **Search**: Search by household name
        /// - **OwnedByUser**: Filter only households where user is Owner (true/false)
        /// - **MinMembers**: Minimum number of members
        /// - **MaxMembers**: Maximum number of members
        /// 
        /// Example: `GET /api/households?page=1&amp;pageSize=10&amp;sortBy=Name&amp;sortOrder=asc&amp;search=home`
        /// </remarks>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<HouseholdDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<PagedResult<HouseholdDto>>>> GetUserHouseholds(
            [FromQuery] HouseholdQueryParameters queryParameters,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation(
                "Getting households for user {UserId} with filters: Page={Page}, PageSize={PageSize}, Search={Search}",
                userId,
                queryParameters?.Page ?? 1,
                queryParameters?.PageSize ?? 20,
                queryParameters?.Search);

            // Set UserId filter to current user
            if (queryParameters == null)
            {
                queryParameters = new HouseholdQueryParameters { UserId = userId };
            }
            else
            {
                queryParameters.UserId = userId;
            }

            var allHouseholds = await _householdService.GetUserHouseholdsAsync(userId, cancellationToken);

            // Apply search filter
            var filteredHouseholds = allHouseholds.AsQueryable();

            if (!string.IsNullOrWhiteSpace(queryParameters.Search))
            {
                filteredHouseholds = filteredHouseholds.Where(h =>
                    h.Name.Contains(queryParameters.Search, StringComparison.OrdinalIgnoreCase) ||
                    (h.Description != null && h.Description.Contains(queryParameters.Search, StringComparison.OrdinalIgnoreCase)));
            }

            if (queryParameters.MinMembers.HasValue)
            {
                filteredHouseholds = filteredHouseholds.Where(h => h.MemberCount >= queryParameters.MinMembers.Value);
            }

            if (queryParameters.MaxMembers.HasValue)
            {
                filteredHouseholds = filteredHouseholds.Where(h => h.MemberCount <= queryParameters.MaxMembers.Value);
            }

            // Apply sorting
            filteredHouseholds = queryParameters.SortBy?.ToLower() switch
            {
                "name" => queryParameters.IsAscending
                    ? filteredHouseholds.OrderBy(h => h.Name)
                    : filteredHouseholds.OrderByDescending(h => h.Name),
                "membercount" => queryParameters.IsAscending
                    ? filteredHouseholds.OrderBy(h => h.MemberCount)
                    : filteredHouseholds.OrderByDescending(h => h.MemberCount),
                _ => queryParameters.IsAscending
                    ? filteredHouseholds.OrderBy(h => h.CreatedAt)
                    : filteredHouseholds.OrderByDescending(h => h.CreatedAt)
            };

            // Create paged result
            var pagedResult = PagedResult<HouseholdDto>.Create(
                filteredHouseholds,
                queryParameters.Page,
                queryParameters.PageSize);

            return Ok(ApiResponse<PagedResult<HouseholdDto>>.SuccessResponse(
                pagedResult,
                $"Retrieved {pagedResult.Items.Count} of {pagedResult.TotalCount} household(s) (Page {pagedResult.PageNumber}/{pagedResult.TotalPages})"));
        }

        /// <summary>
        /// Get household by ID with full details
        /// </summary>
        /// <param name="id">Household ID (GUID)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Household details including members, rooms, and tasks</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<HouseholdDetailsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<HouseholdDetailsDto>>> GetHousehold(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} requesting household {HouseholdId}", userId, id);

            // Validate access
            await _householdService.ValidateUserAccessAsync(id, userId, cancellationToken);

            var household = await _householdService.GetHouseholdWithMembersAsync(id, cancellationToken);

            // Set IsOwner flag
            if (household != null)
            {
                household.IsOwner = await _householdService.IsUserOwnerAsync(id, userId, cancellationToken);
            }

            return Ok(ApiResponse<HouseholdDetailsDto>.SuccessResponse(household));
        }

        /// <summary>
        /// Create a new household
        /// </summary>
        /// <param name="request">Household creation data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created household</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/households
        ///     {
        ///        "name": "My Home",
        ///        "description": "Family household"
        ///     }
        /// 
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<HouseholdDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<HouseholdDto>>> CreateHousehold(
            [FromBody] UpsertHouseholdRequest request,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} creating household: {HouseholdName}",
                userId, request.Name);

            var household = await _householdService.CreateHouseholdAsync(
                request,
                userId,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetHousehold),
                new { id = household.Id },
                ApiResponse<HouseholdDto>.SuccessResponse(
                    household,
                    "Household created successfully"));
        }

        /// <summary>
        /// Update an existing household
        /// </summary>
        /// <param name="id">Household ID (GUID)</param>
        /// <param name="request">Updated household data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated household</returns>
        /// <remarks>
        /// Only household owners can update household details.
        /// </remarks>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<HouseholdDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<HouseholdDto>>> UpdateHousehold(
            Guid id,
            [FromBody] UpsertHouseholdRequest request,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} updating household {HouseholdId}", userId, id);

            var household = await _householdService.UpdateHouseholdAsync(
                id,
                request,
                userId,
                cancellationToken);

            return Ok(ApiResponse<HouseholdDto>.SuccessResponse(
                household,
                "Household updated successfully"));
        }

        /// <summary>
        /// Delete a household (Owner only)
        /// </summary>
        /// <param name="id">Household ID (GUID)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>No content</returns>
        /// <remarks>
        /// Only household owners can delete the household.
        /// Deleting a household will also delete all associated rooms, tasks, and executions.
        /// </remarks>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteHousehold(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogWarning("User {UserId} deleting household {HouseholdId}", userId, id);

            await _householdService.DeleteHouseholdAsync(id, userId, cancellationToken);

            return NoContent();
        }

        #endregion

        #region Invite Operations

        /// <summary>
        /// Get household by invite code (for join preview)
        /// </summary>
        /// <param name="inviteCode">Invite code (GUID)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Basic household info (name, description, member count)</returns>
        [HttpGet("invite/{inviteCode:guid}")]
        [ProducesResponseType(typeof(ApiResponse<HouseholdDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<HouseholdDto>>> GetHouseholdByInviteCode(
            Guid inviteCode,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Looking up household by invite code: {InviteCode}", inviteCode);

            var household = await _householdService.GetHouseholdByInviteCodeAsync(
                inviteCode,
                cancellationToken);

            return Ok(ApiResponse<HouseholdDto>.SuccessResponse(household));
        }

        /// <summary>
        /// Join household using invite code
        /// </summary>
        /// <param name="request">Join request with invite code</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Joined household</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/households/join
        ///     {
        ///        "inviteCode": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        ///     }
        /// 
        /// </remarks>
        [HttpPost("join")]
        [ProducesResponseType(typeof(ApiResponse<HouseholdDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<HouseholdDto>>> JoinHousehold(
            [FromQuery] JoinHouseholdRequest request,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} joining household with invite code: {InviteCode}",
                userId, request.InviteCode);

            var household = await _householdService.JoinHouseholdAsync(
                request,
                userId,
                cancellationToken);

            return Ok(ApiResponse<HouseholdDto>.SuccessResponse(
                household,
                $"Successfully joined '{household.Name}'"));
        }

        /// <summary>
        /// Regenerate invite code for household (Owner only)
        /// </summary>
        /// <param name="id">Household ID (GUID)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>New invite code</returns>
        /// <remarks>
        /// This will invalidate the old invite code. Use this if the invite code was compromised.
        /// </remarks>
        [HttpPost("{id:guid}/regenerate-invite")]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<Guid>>> RegenerateInviteCode(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} regenerating invite code for household {HouseholdId}",
                userId, id);

            var newInviteCode = await _householdService.RegenerateInviteCodeAsync(
                id,
                userId,
                cancellationToken);

            return Ok(ApiResponse<Guid>.SuccessResponse(
                newInviteCode,
                "Invite code regenerated successfully"));
        }

        /// <summary>
        /// Leave household (cannot leave if you're the only owner)
        /// </summary>
        /// <param name="id">Household ID (GUID)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success message</returns>
        /// <remarks>
        /// If you are the only owner, you must either:
        /// - Promote another member to owner first, or
        /// - Delete the household instead
        /// </remarks>
        [HttpPost("{id:guid}/leave")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<object>>> LeaveHousehold(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} leaving household {HouseholdId}", userId, id);

            await _householdService.LeaveHouseholdAsync(id, userId, cancellationToken);

            return Ok(ApiResponse<object>.SuccessResponse("Successfully left household"));
        }

        #endregion

        #region Member Management

        /// <summary>
        /// Remove a member from household (Owner only)
        /// </summary>
        /// <param name="id">Household ID (GUID)</param>
        /// <param name="userId">User ID to remove</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>No content</returns>
        /// <remarks>
        /// Only household owners can remove members.
        /// Cannot remove the last owner - promote another member first.
        /// </remarks>
        [HttpDelete("{id:guid}/members/{userId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveMember(
            Guid id,
            string userId,
            CancellationToken cancellationToken = default)
        {
            var requestingUserId = GetCurrentUserId();

            _logger.LogInformation("User {RequestingUserId} removing user {UserId} from household {HouseholdId}",
                requestingUserId, userId, id);

            await _householdService.RemoveMemberAsync(id, userId, requestingUserId, cancellationToken);

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
