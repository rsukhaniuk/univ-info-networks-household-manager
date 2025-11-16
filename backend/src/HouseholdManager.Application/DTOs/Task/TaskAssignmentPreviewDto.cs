namespace HouseholdManager.Application.DTOs.Task
{
    /// <summary>
    /// DTO for previewing task assignment
    /// </summary>
    public class TaskAssignmentPreviewDto
    {
        /// <summary>
        /// Task ID
        /// </summary>
        public Guid TaskId { get; set; }

        /// <summary>
        /// Task title
        /// </summary>
        public string TaskTitle { get; set; } = string.Empty;

        /// <summary>
        /// Task priority
        /// </summary>
        public Domain.Enums.TaskPriority Priority { get; set; }

        /// <summary>
        /// Room name where task is performed
        /// </summary>
        public string? RoomName { get; set; }

        /// <summary>
        /// User ID that will be assigned
        /// </summary>
        public string AssignedUserId { get; set; } = string.Empty;

        /// <summary>
        /// User name that will be assigned
        /// </summary>
        public string AssignedUserName { get; set; } = string.Empty;
    }
}
