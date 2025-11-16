using FluentValidation;
using HouseholdManager.Application.DTOs.Task;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.Validators.Task
{
    /// <summary>
    /// Validator for assigning a task to a user
    /// </summary>
    public class AssignTaskRequestValidator : AbstractValidator<AssignTaskRequest>
    {
        public AssignTaskRequestValidator()
        {
            // User ID validation (optional - null means unassign)
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID cannot be empty string. Use null to unassign.")
                .When(x => x.UserId != null);
        }
    }
}
