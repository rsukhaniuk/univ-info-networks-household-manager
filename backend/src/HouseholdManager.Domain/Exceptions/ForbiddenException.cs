using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when a user is authenticated but lacks permission to perform an action
    /// Maps to HTTP 403 Forbidden
    /// </summary>
    public class ForbiddenException : DomainException
    {
        /// <summary>
        /// Initializes a new instance with default message
        /// </summary>
        public ForbiddenException()
            : base("You do not have permission to perform this action")
        {
        }

        /// <summary>
        /// Initializes a new instance with a custom message
        /// </summary>
        /// <param name="message">Custom error message</param>
        public ForbiddenException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance with a custom message and inner exception
        /// </summary>
        /// <param name="message">Custom error message</param>
        /// <param name="innerException">Inner exception</param>
        public ForbiddenException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates a ForbiddenException for resource access denial
        /// </summary>
        /// <param name="resourceType">Type of resource (e.g., "Household", "Task")</param>
        /// <param name="resourceId">ID of the resource</param>
        /// <returns>ForbiddenException with formatted message</returns>
        public static ForbiddenException ForResource(string resourceType, object resourceId)
        {
            return new ForbiddenException(
                $"You do not have permission to access {resourceType} with ID '{resourceId}'");
        }

        /// <summary>
        /// Creates a ForbiddenException for action on resource
        /// </summary>
        /// <param name="action">Action being attempted (e.g., "update", "delete")</param>
        /// <param name="resourceType">Type of resource</param>
        /// <returns>ForbiddenException with formatted message</returns>
        public static ForbiddenException ForAction(string action, string resourceType)
        {
            return new ForbiddenException(
                $"You do not have permission to {action} {resourceType}. Only owners can perform this action.");
        }
    }
}
