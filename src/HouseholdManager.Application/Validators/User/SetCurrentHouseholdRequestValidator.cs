using FluentValidation;
using HouseholdManager.Application.DTOs.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.Validators.User
{
    /// <summary>
    /// Validator for setting current household (optional - null to clear)
    /// </summary>
    public class SetCurrentHouseholdRequestValidator : AbstractValidator<SetCurrentHouseholdRequest>
    {
        public SetCurrentHouseholdRequestValidator()
        {
            // Household ID validation (optional - null means clear current)
            RuleFor(x => x.HouseholdId)
                .Must(id => id != Guid.Empty)
                .WithMessage("Invalid household ID. Use null to clear current household.")
                .When(x => x.HouseholdId.HasValue);
        }
    }
}
