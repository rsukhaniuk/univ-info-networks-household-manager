using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.Household
{
    /// <summary>
    /// Request for creating or updating a household (Upsert pattern)
    /// Id = null for Create, Id = value for Update
    /// </summary>
    public class UpsertHouseholdRequest
    {
        /// <summary>
        /// Household ID (null for create, value for update)
        /// </summary>
        [SwaggerSchema(ReadOnly = true, Description = "Used only for update")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Guid? Id { get; set; }

        /// <summary>
        /// Household name
        /// </summary>
        [Required(ErrorMessage = "Household name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional household description
        /// </summary>
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }
    }
}
