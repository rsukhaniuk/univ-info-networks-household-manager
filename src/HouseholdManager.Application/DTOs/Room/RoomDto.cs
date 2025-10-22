using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.Room
{
    /// <summary>
    /// Basic room information (Response DTO)
    /// </summary>
    public class RoomDto
    {
        /// <summary>
        /// Room unique identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Household ID that this room belongs to
        /// </summary>
        public Guid HouseholdId { get; set; }

        /// <summary>
        /// Room name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional room description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Relative path to room photo
        /// </summary>
        public string? PhotoPath { get; set; }

        /// <summary>
        /// Full URL to room photo (computed from PhotoPath)
        /// </summary>
        public string? PhotoUrl { get; set; }

        /// <summary>
        /// Cleaning priority (1 = low, 10 = high)
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// When the room was created (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Number of active tasks assigned to this room
        /// </summary>
        public int ActiveTaskCount { get; set; }

        /// <summary>
        /// Indicates if the room has a photo
        /// </summary>
        public bool HasPhoto => !string.IsNullOrEmpty(PhotoPath);
    }
}
