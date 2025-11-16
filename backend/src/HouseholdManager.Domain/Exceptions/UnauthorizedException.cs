using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when user is not authorized to perform an action (HTTP 401)
    /// </summary>
    public class UnauthorizedException : DomainException
    {
        public UnauthorizedException()
            : base("You are not authorized to perform this action")
        {
        }

        public UnauthorizedException(string message) : base(message)
        {
        }

        public UnauthorizedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
