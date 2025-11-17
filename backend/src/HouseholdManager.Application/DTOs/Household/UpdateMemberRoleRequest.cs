using HouseholdManager.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace HouseholdManager.Application.DTOs.Household
{
    /// <summary>
    /// Request for updating a household member's role
    /// </summary>
    public class UpdateMemberRoleRequest
    {
        /// <summary>
        /// New role for the household member
        /// </summary>
        [Required(ErrorMessage = "Role is required")]
        public HouseholdRole NewRole { get; set; }
    }
}
