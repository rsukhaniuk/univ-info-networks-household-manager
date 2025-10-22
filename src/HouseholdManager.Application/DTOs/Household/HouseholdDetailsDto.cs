using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.Household
{
    /// <summary>
    /// Detailed household information with nested data (Response DTO)
    /// Used for household details page
    /// </summary>
    public class HouseholdDetailsDto
    {
        /// <summary>
        /// Basic household information
        /// </summary>
        public HouseholdDto Household { get; set; } = null!;

        /// <summary>
        /// List of rooms in this household
        /// </summary>
        public IReadOnlyList<Room.RoomDto> Rooms { get; set; } = new List<Room.RoomDto>();

        /// <summary>
        /// List of active tasks in this household
        /// </summary>
        public IReadOnlyList<Task.TaskDto> ActiveTasks { get; set; } = new List<Task.TaskDto>();

        /// <summary>
        /// List of members in this household with their roles
        /// </summary>
        public IReadOnlyList<HouseholdMemberDto> Members { get; set; } = new List<HouseholdMemberDto>();

        /// <summary>
        /// Indicates if the current user is an owner of this household
        /// </summary>
        public bool IsOwner { get; set; }

        /// <summary>
        /// Task counts per user (UserId → TaskCount)
        /// </summary>
        public Dictionary<string, int> TaskCountsByUser { get; set; } = new Dictionary<string, int>();
    }

    /// <summary>
    /// Household member information for details view
    /// </summary>
    public class HouseholdMemberDto
    {
        /// <summary>
        /// Member ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// User's display name
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// User's email
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// User's role in this household (Owner or Member)
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// When the user joined this household
        /// </summary>
        public DateTime JoinedAt { get; set; }

        /// <summary>
        /// Number of active tasks assigned to this user
        /// </summary>
        public int ActiveTaskCount { get; set; }

        /// <summary>
        /// Number of tasks completed by this user this week
        /// </summary>
        public int CompletedThisWeek { get; set; }
    }
}
