using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when authentication token is invalid or missing required claims
    /// Results in HTTP 401 Unauthorized
    /// </summary>
    public class AuthenticationException : DomainException
    {
        public AuthenticationException()
            : base("Authentication failed")
        {
        }

        public AuthenticationException(string message) : base(message)
        {
        }

        public AuthenticationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Exception for missing user ID claim in JWT token
        /// </summary>
        public static AuthenticationException MissingUserIdClaim()
        {
            return new AuthenticationException(
                "User ID not found in authentication token. This indicates a configuration issue with the authentication provider.");
        }

        /// <summary>
        /// Exception for invalid token format
        /// </summary>
        public static AuthenticationException InvalidToken(string reason)
        {
            return new AuthenticationException(
                $"Invalid authentication token: {reason}");
        }
    }
}
