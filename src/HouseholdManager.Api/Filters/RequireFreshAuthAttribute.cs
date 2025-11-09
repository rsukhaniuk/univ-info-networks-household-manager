using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace HouseholdManager.Api.Filters
{
    /// <summary>
    /// Authorization filter that requires fresh authentication (recent login).
    /// Used for sensitive operations like password changes.
    /// Checks the 'auth_time' claim in JWT to ensure user recently authenticated.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequireFreshAuthAttribute : Attribute, IAuthorizationFilter
    {
        /// <summary>
        /// Maximum age of authentication in seconds. Default is 300 seconds (5 minutes).
        /// </summary>
        public int MaxAuthAgeSeconds { get; set; } = 300;

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequireFreshAuthAttribute>>();

            // Ensure user is authenticated
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "Authentication required"
                });
                return;
            }

            // DEBUG: Log all claims to see what's in the token
            logger.LogInformation(
                "RequireFreshAuth: Checking claims for user {UserId}. All claims: {Claims}",
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                string.Join(", ", user.Claims.Select(c => $"{c.Type}={c.Value}"))
            );

            // Get auth_time claim (try both with and without namespace)
            var authTimeClaim = user.FindFirst("https://householdmanager.com/auth_time")
                                ?? user.FindFirst("auth_time");

            if (authTimeClaim == null)
            {
                logger.LogWarning(
                    "Missing auth_time claim for user {UserId}. Cannot verify authentication freshness.",
                    user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                );

                context.Result = new ObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Fresh Authentication Required",
                    Detail = "This operation requires recent authentication. Please re-authenticate and try again.",
                    Extensions = { ["reason"] = "missing_auth_time_claim" }
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            // Parse auth_time (Unix timestamp)
            if (!long.TryParse(authTimeClaim.Value, out long authTimeUnix))
            {
                logger.LogWarning(
                    "Invalid auth_time claim format for user {UserId}: {AuthTime}",
                    user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    authTimeClaim.Value
                );

                context.Result = new ObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Fresh Authentication Required",
                    Detail = "This operation requires recent authentication. Please re-authenticate and try again.",
                    Extensions = { ["reason"] = "invalid_auth_time_format" }
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            // Convert Unix timestamp to DateTime
            var authTime = DateTimeOffset.FromUnixTimeSeconds(authTimeUnix).UtcDateTime;
            var now = DateTime.UtcNow;
            var authAge = now - authTime;

            // Check if authentication is fresh enough
            if (authAge.TotalSeconds > MaxAuthAgeSeconds)
            {
                logger.LogWarning(
                    "Stale authentication detected for user {UserId}. Auth age: {AuthAge} seconds (max: {MaxAge})",
                    user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    (int)authAge.TotalSeconds,
                    MaxAuthAgeSeconds
                );

                context.Result = new ObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Fresh Authentication Required",
                    Detail = $"This operation requires authentication within the last {MaxAuthAgeSeconds} seconds. Please re-authenticate and try again.",
                    Extensions =
                    {
                        ["reason"] = "stale_authentication",
                        ["authAgeSeconds"] = (int)authAge.TotalSeconds,
                        ["maxAgeSeconds"] = MaxAuthAgeSeconds
                    }
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            // Authentication is fresh, allow the request to proceed
            logger.LogInformation(
                "Fresh authentication verified for user {UserId}. Auth age: {AuthAge} seconds",
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                (int)authAge.TotalSeconds
            );
        }
    }
}
