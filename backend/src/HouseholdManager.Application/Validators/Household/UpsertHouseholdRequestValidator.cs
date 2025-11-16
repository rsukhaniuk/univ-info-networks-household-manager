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
    /// Validator for creating or updating a household
    /// </summary>
    public class UpsertHouseholdRequestValidator : AbstractValidator<UpsertHouseholdRequest>
    {
        public UpsertHouseholdRequestValidator()
        {
            // Name validation
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Household name is required")
                .MaximumLength(100)
                .WithMessage("Household name cannot exceed 100 characters")
                .MinimumLength(2)
                .WithMessage("Household name must be at least 2 characters");

            // Description validation (optional)
            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));

            // Id validation (for updates only)
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Household ID is required for updates")
                .When(x => x.Id.HasValue);
        }
    }
}
