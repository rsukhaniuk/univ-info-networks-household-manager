using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.Execution
{
    /// <summary>
    /// Request for completing a task
    /// </summary>
    public class CompleteTaskRequest
    {
        /// <summary>
        /// Task ID to complete
        /// </summary>
        [Required]
        public Guid TaskId { get; set; }

        /// <summary>
        /// Optional notes about the task completion
        /// </summary>
        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? Notes { get; set; }

        /// <summary>
        /// Optional photo of the completed task (uploaded separately)
        /// Photo path will be set by file upload endpoint
        /// </summary>
        [StringLength(260)]
        public string? PhotoPath { get; set; }

        /// <summary>
        /// Optional: custom completion timestamp (defaults to now)
        /// </summary>
        public DateTime? CompletedAt { get; set; }
    }
}
