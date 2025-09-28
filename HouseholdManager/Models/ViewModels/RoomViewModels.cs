using HouseholdManager.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace HouseholdManager.Models.ViewModels
{
    public class RoomIndexViewModel
    {
        public Household Household { get; set; } = null!;
        public IReadOnlyList<Room> Rooms { get; set; } = new List<Room>();
        public bool IsOwner { get; set; }
    }

    public class RoomDetailsViewModel
    {
        public Room Room { get; set; } = null!;
        public bool IsOwner { get; set; }
    }

    /// <summary>
    /// ViewModel for creating and editing rooms (Upsert pattern)
    /// </summary>
    public class UpsertRoomViewModel
    {
        public Guid Id { get; set; }
        public Guid HouseholdId { get; set; }
        public string HouseholdName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Room name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        [Display(Name = "Room Name")]
        public string? Name { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Range(1, 10, ErrorMessage = "Priority must be between 1 and 10")]
        [Display(Name = "Priority (1-10)")]
        public int Priority { get; set; } = 5;

        public string? PhotoPath { get; set; }
        public bool IsEdit { get; set; }

        /// <summary>
        /// Dynamic page title based on operation type
        /// </summary>
        public string PageTitle => IsEdit ? "Edit Room" : "Create New Room";

        /// <summary>
        /// Dynamic submit button text based on operation type
        /// </summary>
        public string SubmitText => IsEdit ? "Update Room" : "Create Room";

        /// <summary>
        /// Dynamic submit button CSS class based on operation type
        /// </summary>
        public string SubmitClass => IsEdit ? "btn-warning" : "btn-success";

        /// <summary>
        /// Dynamic icon for submit button
        /// </summary>
        public string SubmitIcon => IsEdit ? "fas fa-save" : "fas fa-plus";
    }
}
