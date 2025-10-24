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
            // Household ID validation
            RuleFor(x => x.HouseholdId)
                .NotEmpty()
                .WithMessage("Household ID is required")
                .Must(id => id != Guid.Empty)
                .WithMessage("Invalid household ID");

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

            // Regular tasks MUST have ScheduledWeekday
            RuleFor(x => x.ScheduledWeekday)
                .NotNull()
                .WithMessage("Regular tasks must have a scheduled weekday")
                .When(x => x.Type == TaskType.Regular);

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

            // OneTime tasks MUST NOT have ScheduledWeekday
            RuleFor(x => x.ScheduledWeekday)
                .Null()
                .WithMessage("One-time tasks should not have a scheduled weekday")
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
    }
}
