using Auth0.Core.Exceptions;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using HouseholdManager.Application.Interfaces.ExternalServices;
using HouseholdManager.Domain.Exceptions;
using HouseholdManager.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace HouseholdManager.Infrastructure.ExternalServices.Auth0
{
    /// <summary>
    /// Auth0 Management API client for password and email operations
    /// Handles M2M authentication and API calls
    /// </summary>
    public class Auth0ManagementApiClient : IAuth0ManagementApiClient
    {
        private readonly Auth0Settings _auth0Settings;
        private readonly ILogger<Auth0ManagementApiClient> _logger;
        private readonly HttpClient _httpClient;
        private string? _cachedAccessToken;
        private DateTime _tokenExpiration = DateTime.MinValue;

        public Auth0ManagementApiClient(
            IOptions<Auth0Settings> auth0Settings,
            ILogger<Auth0ManagementApiClient> logger,
            IHttpClientFactory httpClientFactory)
        {
            _auth0Settings = auth0Settings.Value;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        #region Token Management

        /// <summary>
        /// Get M2M access token for Management API
        /// Caches token until expiration
        /// </summary>
        private async Task<string> GetManagementApiTokenAsync()
        {
            // Return cached token if still valid
            if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiration)
            {
                return _cachedAccessToken;
            }

            _logger.LogInformation("Requesting new Auth0 Management API token (M2M)");

            var tokenRequest = new
            {
                client_id = _auth0Settings.ManagementApiClientId,
                client_secret = _auth0Settings.ManagementApiClientSecret,
                audience = _auth0Settings.ManagementApiAudience,
                grant_type = "client_credentials"
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"https://{_auth0Settings.Domain}/oauth/token",
                tokenRequest);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get Management API token: {Error}", error);
                throw new InvalidOperationException($"Failed to authenticate with Auth0 Management API: {error}");
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>()
                ?? throw new InvalidOperationException("Failed to parse token response");

            _cachedAccessToken = tokenResponse.access_token;
            _tokenExpiration = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in - 60); // Refresh 1 min early

            _logger.LogInformation("Successfully obtained Management API token (expires in {Seconds}s)", tokenResponse.expires_in);

            return _cachedAccessToken;
        }

        /// <summary>
        /// Get authenticated ManagementApiClient instance
        /// </summary>
        private async Task<ManagementApiClient> GetManagementClientAsync()
        {
            var token = await GetManagementApiTokenAsync();
            return new ManagementApiClient(token, new Uri($"https://{_auth0Settings.Domain}/api/v2"));
        }

        #endregion

        #region Password Operations

        /// <inheritdoc />
        public async Task<string> CreatePasswordChangeTicketAsync(string userId, string resultUrl)
        {
            try
            {
                _logger.LogInformation("Creating password change ticket for user {UserId}", userId);

                var client = await GetManagementClientAsync();

                var request = new PasswordChangeTicketRequest
                {
                    UserId = userId,
                    //ResultUrl = resultUrl,
                    MarkEmailAsVerified = true, // Ensure email is marked as verified after password change
                    IncludeEmailInRedirect = false, // Don't include email in redirect URL
                    ClientId = _auth0Settings.ClientId,
                    // TTL in seconds (default: 432000 = 5 days)
                    Ttl = 86400 // 24 hours
                };

                var ticket = await client.Tickets.CreatePasswordChangeTicketAsync(request);

                _logger.LogInformation("Password change ticket created for user {UserId}: {TicketUrl}", userId, ticket.Value);

                return ticket.Value;
            }
            catch (ErrorApiException ex)
            {
                _logger.LogError(ex, "Auth0 API error creating password change ticket: {Message}", ex.Message);
                throw new InvalidOperationException($"Failed to create password change ticket: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating password change ticket");
                throw new InvalidOperationException("Failed to create password change ticket", ex);
            }
        }

        #endregion

        #region Email Operations

        /// <inheritdoc />
        public async Task ChangeUserEmailAsync(string userId, string newEmail, bool verifyEmail = false)
        {
            try
            {
                _logger.LogInformation("Changing email for user {UserId} to {NewEmail} (verify: {Verify})", 
                    userId, newEmail, verifyEmail);

                var client = await GetManagementClientAsync();

                var updateRequest = new UserUpdateRequest
                {
                    Email = newEmail,
                    EmailVerified = !verifyEmail, // If verifyEmail=true, set EmailVerified=false (user must verify)
                    // Optional: Update name to match new email (if name is based on email)
                    // Uncomment if you want name to sync with email:
                    // FullName = newEmail.Split('@')[0]
                };

                await client.Users.UpdateAsync(userId, updateRequest);

                _logger.LogInformation("Successfully changed email for user {UserId}", userId);
            }
            catch (ErrorApiException ex)
            {
                _logger.LogError(ex, "Auth0 API error changing email: {Message}", ex.Message);

                // Handle specific errors
                if (ex.Message.Contains("email already exists") || ex.Message.Contains("The specified new email already exists"))
                {
                    throw new HouseholdManager.Domain.Exceptions.ValidationException(
                        "email",
                        "This email address is already in use by another account");
                }

                throw new InvalidOperationException($"Failed to change email: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error changing email");
                throw new InvalidOperationException("Failed to change email", ex);
            }
        }

        #endregion

        #region User Connection Info

        /// <inheritdoc />
        public async Task<string> GetUserConnectionAsync(string userId)
        {
            try
            {
                var client = await GetManagementClientAsync();
                var user = await client.Users.GetAsync(userId);

                // Extract connection from identities
                var connection = user.Identities?.FirstOrDefault()?.Connection ?? "unknown";

                _logger.LogDebug("User {UserId} has connection: {Connection}", userId, connection);

                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user connection");
                throw new InvalidOperationException("Failed to get user connection", ex);
            }
        }

        /// <inheritdoc />
        public async Task<bool> CanUserChangePasswordAsync(string userId)
        {
            var connection = await GetUserConnectionAsync(userId);

            // Only "auth0" connection (Username-Password-Authentication) allows password changes
            // Social logins (google-oauth2, microsoft, etc.) manage passwords externally
            return connection.Equals("auth0", StringComparison.OrdinalIgnoreCase) ||
                   connection.Equals("Username-Password-Authentication", StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public async Task UpdateUserNameAsync(string userId, string fullName)
        {
            try
            {
                _logger.LogInformation("Updating name for user {UserId} to '{FullName}'", userId, fullName);

                var client = await GetManagementClientAsync();

                var updateRequest = new UserUpdateRequest
                {
                    FullName = fullName
                };

                await client.Users.UpdateAsync(userId, updateRequest);

                _logger.LogInformation("Successfully updated name for user {UserId}", userId);
            }
            catch (ErrorApiException ex)
            {
                _logger.LogError(ex, "Auth0 API error updating name: {Message}", ex.Message);
                throw new InvalidOperationException($"Failed to update user name: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating name");
                throw new InvalidOperationException("Failed to update user name", ex);
            }
        }

        #endregion

        #region Helper Classes

        private class TokenResponse
        {
            public string access_token { get; set; } = string.Empty;
            public int expires_in { get; set; }
            public string token_type { get; set; } = string.Empty;
        }

        #endregion
    }
}
