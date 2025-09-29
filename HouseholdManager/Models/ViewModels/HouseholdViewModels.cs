using HouseholdManager.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace HouseholdManager.Models.ViewModels
{
    public class HouseholdDetailsViewModel
    {
        public Household Household { get; set; } = null!;
        public IReadOnlyList<Room> Rooms { get; set; } = new List<Room>();
        public IReadOnlyList<HouseholdTask> ActiveTasks { get; set; } = new List<HouseholdTask>();
        public bool IsOwner { get; set; }
    }

    /// <summary>
    /// ViewModel for creating and editing households (Upsert pattern)
    /// </summary>
    public class UpsertHouseholdViewModel
    {
        /// <summary>
        /// Household ID (empty for create, populated for edit)
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Household name
        /// </summary>
        [Required(ErrorMessage = "Household name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        [Display(Name = "Household Name")]
        public string? Name { get; set; }

        /// <summary>
        /// Optional household description
        /// </summary>
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        /// <summary>
        /// Indicates whether this is an edit operation
        /// </summary>
        public bool IsEdit { get; set; }

        /// <summary>
        /// Dynamic page title based on operation type
        /// </summary>
        public string PageTitle => IsEdit ? "Edit Household" : "Create New Household";

        /// <summary>
        /// Dynamic submit button text based on operation type
        /// </summary>
        public string SubmitText => IsEdit ? "Update Household" : "Create Household";

        /// <summary>
        /// Dynamic submit button CSS class based on operation type
        /// </summary>
        public string SubmitClass => IsEdit ? "btn-warning" : "btn-primary";

        /// <summary>
        /// Dynamic icon for submit button
        /// </summary>
        public string SubmitIcon => IsEdit ? "fas fa-save" : "fas fa-plus";
    }

    /// <summary>
    /// ViewModel for joining a household using an invite code
    /// </summary>
    public class JoinHouseholdViewModel
    {
        /// <summary>
        /// Invite code provided by household owner
        /// </summary>
        [Required(ErrorMessage = "Please enter an invite code")]
        [Display(Name = "Invite Code")]
        public Guid? InviteCode { get; set; }
    }
}
