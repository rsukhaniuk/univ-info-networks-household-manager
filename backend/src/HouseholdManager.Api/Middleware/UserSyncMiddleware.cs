using HouseholdManager.Application.Interfaces.Repositories;
using HouseholdManager.Application.Interfaces.Services;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace HouseholdManager.Api.Middleware
{
    /// <summary>
    /// Middleware that automatically synchronizes users from Auth0 to local database
    /// </summary>
    public class UserSyncMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UserSyncMiddleware> _logger;

        // In-memory cache to avoid syncing same user multiple times
        // Key: Auth0 User ID, Value: Last sync timestamp
        private static readonly ConcurrentDictionary<string, DateTime> _syncCache
            = new ConcurrentDictionary<string, DateTime>();

        // Cache duration: user is considered synced for 5 minutes
        private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        public UserSyncMiddleware(RequestDelegate next, ILogger<UserSyncMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IUserService userService,
            IUserRepository userRepository)
        {
            // Only process authenticated requests
            if (context.User.Identity?.IsAuthenticated == true)
            {
                try
                {
                    await SyncUserIfNeededAsync(context, userService, userRepository);
                }
                catch (Exception ex)
                {
                    // Don't fail the request if sync fails
                    _logger.LogWarning(ex,
                        "Failed to sync user from Auth0. Request will proceed anyway.");
                }
            }

            // Continue to next middleware
            await _next(context);
        }

        private async Task SyncUserIfNeededAsync(
            HttpContext context,
            IUserService userService,
            IUserRepository userRepository)
        {
            // Extract Auth0 User ID from JWT token
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogDebug("No user ID found in JWT token, skipping sync");
                return;
            }

            // Check if user exists in database FIRST
            var userExists = await userRepository.ExistsAsync(userId);

            if (!userExists)
            {
                // User doesn't exist in DB, sync from Auth0 immediately (no cache check)
                _logger.LogInformation(
                    "User {UserId} not found in database, syncing from Auth0 (bypassing cache)",
                    userId);

                await PerformSyncAsync(context, userService, userId, isNewUser: true);
            }
            else
            {
                // Check if user was synced recently (within cache duration)
                if (_syncCache.TryGetValue(userId, out var lastSync))
                {
                    if (DateTime.UtcNow - lastSync < _cacheDuration)
                    {
                        // User synced recently, skip
                        _logger.LogTrace(
                            "User {UserId} synced {Seconds} seconds ago, skipping sync",
                            userId,
                            (DateTime.UtcNow - lastSync).TotalSeconds);
                        return;
                    }
                }

                // User exists, but sync anyway to update email/profile if changed
                _logger.LogTrace(
                    "User {UserId} already exists in database, syncing to update profile",
                    userId);

                await PerformSyncAsync(context, userService, userId, isNewUser: false);
            }
        }

        private async Task PerformSyncAsync(
            HttpContext context,
            IUserService userService,
            string userId,
            bool isNewUser)
        {
            // Extract user data from JWT token claims
            var email = context.User.FindFirst(ClaimTypes.Email)?.Value
                ?? context.User.FindFirst("https://householdmanager.com/email")?.Value
                ?? context.User.FindFirst("email")?.Value;

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning(
                    "No email found in JWT token for user {UserId}, cannot sync",
                    userId);
                return;
            }

            // Extract optional profile information
            var firstName = context.User.FindFirst(ClaimTypes.GivenName)?.Value
                 ?? context.User.FindFirst("https://householdmanager.com/first_name")?.Value
                 ?? context.User.FindFirst("given_name")?.Value;

            var lastName = context.User.FindFirst(ClaimTypes.Surname)?.Value
                ?? context.User.FindFirst("https://householdmanager.com/last_name")?.Value
                ?? context.User.FindFirst("family_name")?.Value;

            var profilePictureUrl = context.User.FindFirst("https://householdmanager.com/picture")?.Value
                ?? context.User.FindFirst("picture")?.Value;

            // Extract role from JWT token (Auth0 adds roles as claims)
            var roles = context.User.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            // Also check custom namespace for roles
            if (!roles.Any())
            {
                roles = context.User.FindAll("https://householdmanager.com/roles")
                    .Select(c => c.Value)
                    .ToList();
            }

            // Determine if user is SystemAdmin based on roles
            bool isSystemAdmin = roles.Contains("SystemAdmin", StringComparer.OrdinalIgnoreCase);

            // Sync user to database
            await userService.SyncUserFromAuth0Async(
                userId,
                email,
                firstName,
                lastName,
                profilePictureUrl,
                isSystemAdmin);

            // Update cache with current timestamp
            _syncCache[userId] = DateTime.UtcNow;

            _logger.LogInformation(
                "Successfully synced user {UserId} ({Email}) from Auth0 to database (new user: {IsNewUser})",
                userId,
                email,
                isNewUser);
        }

        /// <summary>
        /// Clear sync cache (useful for testing or manual refresh)
        /// </summary>
        public static void ClearCache()
        {
            _syncCache.Clear();
        }

        /// <summary>
        /// Clear cache for specific user
        /// </summary>
        public static void ClearCache(string userId)
        {
            _syncCache.TryRemove(userId, out _);
        }

        /// <summary>
        /// Get cache statistics (for monitoring/debugging)
        /// </summary>
        public static (int CachedUsers, DateTime? OldestEntry) GetCacheStats()
        {
            if (_syncCache.IsEmpty)
                return (0, null);

            var oldestEntry = _syncCache.Values.Min();
            return (_syncCache.Count, oldestEntry);
        }
    }
}
