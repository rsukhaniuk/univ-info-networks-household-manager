using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when a requested entity is not found (HTTP 404)
    /// </summary>
    public class NotFoundException : DomainException
    {
        public NotFoundException()
        {
        }

        public NotFoundException(string message) : base(message)
        {
        }

        public NotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public NotFoundException(string entityName, object entityId)
            : base($"{entityName} with ID '{entityId}' was not found")
        {
        }
    }
}
