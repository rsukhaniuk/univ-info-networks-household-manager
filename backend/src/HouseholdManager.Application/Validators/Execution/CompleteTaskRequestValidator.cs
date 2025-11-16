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
    /// Validator for completing a task
    /// </summary>
    public class CompleteTaskRequestValidator : AbstractValidator<CompleteTaskRequest>
    {
        public CompleteTaskRequestValidator()
        {
            // Task ID validation
            RuleFor(x => x.TaskId)
                .NotEmpty()
                .WithMessage("Task ID is required")
                .Must(id => id != Guid.Empty)
                .WithMessage("Invalid task ID");

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

            // Completion timestamp validation (optional - defaults to now)
            RuleFor(x => x.CompletedAt)
                .LessThanOrEqualTo(DateTime.UtcNow)
                .WithMessage("Completion date cannot be in the future")
                .GreaterThan(DateTime.UtcNow.AddYears(-1))
                .WithMessage("Completion date cannot be more than 1 year in the past")
                .When(x => x.CompletedAt.HasValue);
        }
    }
}
