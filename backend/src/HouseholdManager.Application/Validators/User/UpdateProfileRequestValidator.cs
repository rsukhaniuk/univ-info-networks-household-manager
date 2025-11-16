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
    /// Validator for updating user profile (first name, last name)
    /// Email and password managed by Auth0
    /// </summary>
    public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
    {
        public UpdateProfileRequestValidator()
        {
            // First name validation (optional)
            RuleFor(x => x.FirstName)
                .MaximumLength(50)
                .WithMessage("First name cannot exceed 50 characters")
                .MinimumLength(1)
                .WithMessage("First name must be at least 1 character")
                .When(x => !string.IsNullOrEmpty(x.FirstName));

            // Last name validation (optional)
            RuleFor(x => x.LastName)
                .MaximumLength(50)
                .WithMessage("Last name cannot exceed 50 characters")
                .MinimumLength(1)
                .WithMessage("Last name must be at least 1 character")
                .When(x => !string.IsNullOrEmpty(x.LastName));

            // At least one field must be provided
            RuleFor(x => x)
                .Must(x => !string.IsNullOrEmpty(x.FirstName) || !string.IsNullOrEmpty(x.LastName))
                .WithMessage("At least first name or last name must be provided for update");
        }
    }
}
