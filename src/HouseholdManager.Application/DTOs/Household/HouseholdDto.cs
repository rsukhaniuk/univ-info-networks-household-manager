using HouseholdManager.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.Household
{
    /// <summary>
    /// Basic household information (Response DTO)
    /// </summary>
    public class HouseholdDto
    {
        /// <summary>
        /// Household unique identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Household name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional household description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Unique invite code for joining this household
        /// </summary>
        public Guid InviteCode { get; set; }

        /// <summary>
        /// Date and time when the invite code expires (UTC)
        /// Null means the code never expires (for legacy households)
        /// </summary>
        public DateTime? InviteCodeExpiresAt { get; set; }

        /// <summary>
        /// When the household was created (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Total number of members in this household
        /// </summary>
        public int MemberCount { get; set; }

        /// <summary>
        /// Number of active tasks in this household
        /// </summary>
        public int ActiveTaskCount { get; set; }

        /// <summary>
        /// Number of rooms in this household
        /// </summary>
        public int RoomCount { get; set; }

        /// <summary>
        /// Gets or sets the role of the household member.
        /// </summary>
        public HouseholdRole? Role { get; set; }
    }
}
