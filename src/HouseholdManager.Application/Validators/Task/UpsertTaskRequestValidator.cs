using FluentValidation;
using HouseholdManager.Application.DTOs.Task;
using HouseholdManager.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.Validators.Task
{
    /// <summary>
    /// Validator for creating or updating a household task
    /// Includes type-specific validation for Regular vs OneTime tasks
    /// </summary>
    public class UpsertTaskRequestValidator : AbstractValidator<UpsertTaskRequest>
    {
        public UpsertTaskRequestValidator()
        {
            // Household ID validation (always required, set from route in controller)
            // Note: Controller sets this from route, so we don't validate during model binding
            // We only validate it's been set properly after controller processes it

            // Room ID validation
            RuleFor(x => x.RoomId)
                .NotEmpty()
                .WithMessage("Room ID is required")
                .Must(id => id != Guid.Empty)
                .WithMessage("Invalid room ID");

            // Title validation
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Task title is required")
                .MaximumLength(200)
                .WithMessage("Task title cannot exceed 200 characters")
                .MinimumLength(3)
                .WithMessage("Task title must be at least 3 characters");

            // Description validation (optional)
            RuleFor(x => x.Description)
                .MaximumLength(1000)
                .WithMessage("Description cannot exceed 1000 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));

            // Task type validation
            RuleFor(x => x.Type)
                .IsInEnum()
                .WithMessage("Invalid task type");

            // Priority validation
            RuleFor(x => x.Priority)
                .IsInEnum()
                .WithMessage("Invalid task priority");

            // Estimated minutes validation
            RuleFor(x => x.EstimatedMinutes)
                .InclusiveBetween(5, 480)
                .WithMessage("Estimated time must be between 5 minutes and 8 hours (480 minutes)");

            // ===== TYPE-SPECIFIC VALIDATION =====

            // Regular tasks MUST have RecurrenceRule
            RuleFor(x => x.RecurrenceRule)
                .NotEmpty()
                .WithMessage("Regular tasks must have a recurrence rule")
                .When(x => x.Type == TaskType.Regular);

            // RecurrenceRule validation (optional, basic format check)
            RuleFor(x => x.RecurrenceRule)
                .MaximumLength(500)
                .WithMessage("Recurrence rule cannot exceed 500 characters")
                .Must(BeValidRrule)
                .WithMessage("Recurrence rule must be a valid iCalendar RRULE format (e.g., 'FREQ=DAILY;INTERVAL=2')")
                .When(x => !string.IsNullOrWhiteSpace(x.RecurrenceRule));

            // RecurrenceEndDate validation (optional, must be in future)
            RuleFor(x => x.RecurrenceEndDate)
                .Must(date => date > DateTime.UtcNow)
                .WithMessage("Recurrence end date must be in the future")
                .When(x => x.RecurrenceEndDate.HasValue);

            // Regular tasks MUST NOT have DueDate
            RuleFor(x => x.DueDate)
                .Null()
                .WithMessage("Regular tasks should not have a due date")
                .When(x => x.Type == TaskType.Regular);

            // OneTime tasks MUST have DueDate
            RuleFor(x => x.DueDate)
                .NotNull()
                .WithMessage("One-time tasks must have a due date")
                .Must(date => date > DateTime.UtcNow)
                .WithMessage("Due date must be in the future")
                .When(x => x.Type == TaskType.OneTime);

            // OneTime tasks MUST NOT have RecurrenceRule
            RuleFor(x => x.RecurrenceRule)
                .Null()
                .WithMessage("One-time tasks should not have a recurrence rule")
                .When(x => x.Type == TaskType.OneTime);

            // OneTime tasks MUST NOT have RecurrenceEndDate
            RuleFor(x => x.RecurrenceEndDate)
                .Null()
                .WithMessage("One-time tasks should not have a recurrence end date")
                .When(x => x.Type == TaskType.OneTime);

            // Assigned user ID validation (optional)
            RuleFor(x => x.AssignedUserId)
                .NotEmpty()
                .WithMessage("Assigned user ID cannot be empty")
                .When(x => !string.IsNullOrEmpty(x.AssignedUserId));

            // Id validation (for updates only)
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Task ID is required for updates")
                .When(x => x.Id.HasValue);

            // RowVersion validation (for updates with optimistic concurrency)
            RuleFor(x => x.RowVersion)
                .NotNull()
                .WithMessage("Row version is required for updates to prevent concurrency conflicts")
                .When(x => x.Id.HasValue);
        }

        /// <summary>
        /// Validates that the RRULE format is correct (basic check)
        /// </summary>
        private bool BeValidRrule(string? rrule)
        {
            if (string.IsNullOrWhiteSpace(rrule))
                return true; // Null/empty is valid (will be caught by required validation if needed)

            // Basic RRULE format check: must contain FREQ=
            if (!rrule.Contains("FREQ=", StringComparison.OrdinalIgnoreCase))
                return false;

            // Valid frequency types
            var validFrequencies = new[] { "DAILY", "WEEKLY", "MONTHLY", "YEARLY" };
            var hasValidFrequency = validFrequencies.Any(freq =>
                rrule.Contains($"FREQ={freq}", StringComparison.OrdinalIgnoreCase));

            return hasValidFrequency;
        }
    }
}
