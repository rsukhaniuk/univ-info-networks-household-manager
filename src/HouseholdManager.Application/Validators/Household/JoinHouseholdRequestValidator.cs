using FluentValidation;
using HouseholdManager.Application.DTOs.Household;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.Validators.Household
{
    /// <summary>
    /// Validator for joining a household using invite code
    /// </summary>
    public class JoinHouseholdRequestValidator : AbstractValidator<JoinHouseholdRequest>
    {
        public JoinHouseholdRequestValidator()
        {
            // Invite code validation
            RuleFor(x => x.InviteCode)
                .NotEmpty()
                .WithMessage("Invite code is required")
                .Must(code => code != Guid.Empty)
                .WithMessage("Invalid invite code format");
        }
    }
}
