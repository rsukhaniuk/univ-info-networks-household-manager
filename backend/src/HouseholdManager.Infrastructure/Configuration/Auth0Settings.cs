namespace HouseholdManager.Infrastructure.Configuration
{
    /// <summary>
    /// Auth0 configuration settings
    /// </summary>
    public class Auth0Settings
    {
        /// <summary>
        /// Auth0 tenant domain (e.g., dev-xxx.us.auth0.com)
        /// </summary>
        public string Domain { get; set; } = string.Empty;

        /// <summary>
        /// API identifier (audience for JWT tokens)
        /// </summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Application Client ID
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Application Client Secret
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Management API Client ID (M2M application)
        /// Used for server-to-server Auth0 Management API calls
        /// </summary>
        public string ManagementApiClientId { get; set; } = string.Empty;

        /// <summary>
        /// Management API Client Secret (M2M application)
        /// Used for server-to-server Auth0 Management API calls
        /// </summary>
        public string ManagementApiClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Management API Audience (typically https://{domain}/api/v2/)
        /// </summary>
        public string ManagementApiAudience { get; set; } = string.Empty;
    }
}
