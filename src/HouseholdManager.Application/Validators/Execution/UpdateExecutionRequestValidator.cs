using FluentValidation;
using HouseholdManager.Application.DTOs.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.Validators.Execution
{
    /// <summary>
    /// Validator for updating task execution (notes and photo only)
    /// </summary>
    public class UpdateExecutionRequestValidator : AbstractValidator<UpdateExecutionRequest>
    {
        public UpdateExecutionRequestValidator()
        {
            // Notes validation (optional)
            RuleFor(x => x.Notes)
                .MaximumLength(1000)
                .WithMessage("Notes cannot exceed 1000 characters")
                .When(x => !string.IsNullOrEmpty(x.Notes));

            // Photo path validation (optional)
            RuleFor(x => x.PhotoPath)
                .MaximumLength(260)
                .WithMessage("Photo path cannot exceed 260 characters")
                .When(x => !string.IsNullOrEmpty(x.PhotoPath));

            // At least one field must be provided
            RuleFor(x => x)
                .Must(x => !string.IsNullOrEmpty(x.Notes) || !string.IsNullOrEmpty(x.PhotoPath))
                .WithMessage("At least notes or photo must be provided for update");
        }
    }
}
