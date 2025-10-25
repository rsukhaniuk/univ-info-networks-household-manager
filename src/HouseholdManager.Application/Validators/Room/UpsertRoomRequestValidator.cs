using FluentValidation;
using HouseholdManager.Application.DTOs.Room;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.Validators.Room
{
    /// <summary>
    /// Validator for creating or updating a room
    /// </summary>
    public class UpsertRoomRequestValidator : AbstractValidator<UpsertRoomRequest>
    {
        public UpsertRoomRequestValidator()
        {
            // Household ID validation
            RuleFor(x => x.HouseholdId)
                .NotEmpty()
                .WithMessage("Household ID is required")
                .Must(id => id != Guid.Empty)
                .WithMessage("Invalid household ID");

            // Name validation
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Room name is required")
                .MaximumLength(100)
                .WithMessage("Room name cannot exceed 100 characters")
                .MinimumLength(2)
                .WithMessage("Room name must be at least 2 characters");

            // Description validation (optional)
            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));

            // Priority validation
            RuleFor(x => x.Priority)
                .InclusiveBetween(1, 10)
                .WithMessage("Priority must be between 1 and 10");

            // Photo path validation (optional)
            RuleFor(x => x.PhotoPath)
                .MaximumLength(260)
                .WithMessage("Photo path cannot exceed 260 characters")
                .When(x => !string.IsNullOrEmpty(x.PhotoPath));

            // Id validation (for updates only)
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Room ID is required for updates")
                .When(x => x.Id.HasValue);
        }
    }
}
