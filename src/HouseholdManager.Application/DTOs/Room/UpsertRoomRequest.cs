using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.Room
{
    /// <summary>
    /// Request for creating or updating a room (Upsert pattern)
    /// Id = null for Create, Id = value for Update
    /// </summary>
    public class UpsertRoomRequest
    {
        /// <summary>
        /// Room ID (null for create, value for update)
        /// </summary>
        [SwaggerSchema(ReadOnly = true, Description = "Used only for update")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Guid? Id { get; set; }

        /// <summary>
        /// Household ID that this room belongs to
        /// </summary>
        [JsonIgnore]                 
        [BindNever]                  
        [SwaggerSchema(ReadOnly = true, Description = "Comes from route")]
        public Guid HouseholdId { get; set; }

        /// <summary>
        /// Room name
        /// </summary>
        [Required(ErrorMessage = "Room name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional room description
        /// </summary>
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        /// <summary>
        /// Cleaning priority (1 = low, 10 = high)
        /// </summary>
        [Range(1, 10, ErrorMessage = "Priority must be between 1 and 10")]
        [SwaggerSchema(ReadOnly = true, Description = "Doesnt need")]
        [JsonIgnore]
        public int Priority { get; set; } = 5;

        /// <summary>
        /// Relative path to room photo (optional)
        /// Will be populated by file upload endpoint
        /// </summary>
        [SwaggerSchema(ReadOnly = true, Description = "Populated by upload endpoint")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [StringLength(260)]
        public string? PhotoPath { get; set; }
    }
}
