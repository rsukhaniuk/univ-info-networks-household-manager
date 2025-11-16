using HouseholdManager.Models.Entities;
using HouseholdManager.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HouseholdManager.Models.ViewModels
{
    public class TaskIndexViewModel
    {
        public Household Household { get; set; } = null!;
        public IReadOnlyList<HouseholdTask> Tasks { get; set; } = new List<HouseholdTask>();
        public bool IsOwner { get; set; }

        // Filter options
        public List<SelectListItem> Rooms { get; set; } = new();
        public List<SelectListItem> Members { get; set; } = new();

        // Selected filters
        public Guid? SelectedRoomId { get; set; }
        public TaskType? SelectedType { get; set; }
        public TaskPriority? SelectedPriority { get; set; }
        public string? SelectedAssigneeId { get; set; }
        public string? SearchQuery { get; set; }
        public string? SelectedStatus { get; set; }
    }

    public class TaskDetailsViewModel
    {
        public HouseholdTask Task { get; set; } = null!;
        public IReadOnlyList<TaskExecution> Executions { get; set; } = new List<TaskExecution>();
        public IReadOnlyList<HouseholdMember> HouseholdMembers { get; set; } = new List<HouseholdMember>();
        public bool IsOwner { get; set; }
        public bool IsSystemAdmin { get; set; }
        public bool IsAssignedToCurrentUser { get; set; }
        public bool IsCompletedThisWeek { get; set; }
        public bool CanComplete { get; set; }
    }

    public class TaskUpsertViewModel
    {
        public Guid Id { get; set; }
        public Guid HouseholdId { get; set; }
        public string HouseholdName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Task title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        [Display(Name = "Task Title")]
        public string? Title { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Task Type")]
        public TaskType Type { get; set; }

        [Required]
        [Display(Name = "Priority")]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        [Required]
        [Range(5, 480, ErrorMessage = "Estimated time must be between 5 minutes and 8 hours")]
        [Display(Name = "Estimated Time (minutes)")]
        public int EstimatedMinutes { get; set; } = 30;

        [Required]
        [Display(Name = "Room")]
        public Guid RoomId { get; set; }

        [Display(Name = "Assigned To")]
        public string? AssignedUserId { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // Type-specific fields
        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Scheduled Weekday")]
        public DayOfWeek? ScheduledWeekday { get; set; }

        // Concurrency control
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public bool IsEdit { get; set; }

        // Populated by controller
        public List<SelectListItem> Rooms { get; set; } = new();
        public List<SelectListItem> Members { get; set; } = new();

        // Helper properties
        public string PageTitle => IsEdit ? "Edit Task" : "Create New Task";
        public string SubmitText => IsEdit ? "Update Task" : "Create Task";
        public string SubmitClass => IsEdit ? "btn-warning" : "btn-success";
        public string SubmitIcon => IsEdit ? "fas fa-save" : "fas fa-plus";
    }
}
