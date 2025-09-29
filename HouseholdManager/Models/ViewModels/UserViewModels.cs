using System.ComponentModel.DataAnnotations;

namespace HouseholdManager.Models.ViewModels
{
    /// <summary>
    /// ViewModel for user profile page
    /// </summary>
    public class UserProfileViewModel
    {
        public string UserId { get; set; } = string.Empty;

        [Display(Name = "First Name")]
        [StringLength(50)]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        [StringLength(50)]
        public string? LastName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Username")]
        public string? UserName { get; set; }

        public DateTime CreatedAt { get; set; }

        // Statistics
        public int TotalHouseholds { get; set; }
        public int OwnedHouseholds { get; set; }
        public int ActiveTasks { get; set; }
        public int CompletedTasksThisWeek { get; set; }
    }

    /// <summary>
    /// ViewModel for changing password
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
