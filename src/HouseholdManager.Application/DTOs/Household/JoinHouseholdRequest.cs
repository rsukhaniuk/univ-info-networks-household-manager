using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.Household
{
    /// <summary>
    /// Request for joining a household using an invite code
    /// </summary>
    public class JoinHouseholdRequest
    {
        /// <summary>
        /// Invite code provided by household owner
        /// </summary>
        [Required(ErrorMessage = "Please enter an invite code")]
        public Guid InviteCode { get; set; }
    }
}
