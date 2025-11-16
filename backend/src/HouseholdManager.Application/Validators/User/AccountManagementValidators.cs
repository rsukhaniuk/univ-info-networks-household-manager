using FluentValidation;
using HouseholdManager.Application.DTOs.User;

namespace HouseholdManager.Application.Validators.User
{
    /// <summary>
    /// Validator for password change ticket request
    /// </summary>
    public class RequestPasswordChangeRequestValidator : AbstractValidator<RequestPasswordChangeRequest>
    {
        public RequestPasswordChangeRequestValidator()
        {
            RuleFor(x => x.ResultUrl)
                .NotEmpty()
                .WithMessage("Result URL is required")
                .Must(BeValidUrl)
                .WithMessage("Result URL must be a valid HTTP or HTTPS URL");
        }

        private bool BeValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }

    /// <summary>
    /// Validator for email change request
    /// </summary>
    public class ChangeEmailRequestValidator : AbstractValidator<ChangeEmailRequest>
    {
        public ChangeEmailRequestValidator()
        {
            RuleFor(x => x.NewEmail)
                .NotEmpty()
                .WithMessage("New email is required")
                .EmailAddress()
                .WithMessage("Invalid email format")
                .MaximumLength(255)
                .WithMessage("Email cannot exceed 255 characters");
        }
    }
}
