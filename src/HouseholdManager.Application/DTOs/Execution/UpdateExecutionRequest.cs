using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.Execution
{
    /// <summary>
    /// Request for updating an execution (notes and photo only)
    /// </summary>
    public class UpdateExecutionRequest
    {
        /// <summary>
        /// Updated notes
        /// </summary>
        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? Notes { get; set; }

        /// <summary>
        /// Optional photo path (set by file upload endpoint)
        /// </summary>
        [JsonIgnore]
        [SwaggerSchema(ReadOnly = true, Description = "Managed internally; not included in requests")]
        [StringLength(260)]
        public string? PhotoPath { get; set; }
    }
}
