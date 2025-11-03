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
            // Note: ASP.NET Core model binding will fail if the string cannot be parsed to Guid
            // This validator only checks that the successfully bound Guid is not empty
            RuleFor(x => x.InviteCode)
                .Must(code => code != Guid.Empty)
                .WithMessage("Invite code is required");
        }
    }
}
