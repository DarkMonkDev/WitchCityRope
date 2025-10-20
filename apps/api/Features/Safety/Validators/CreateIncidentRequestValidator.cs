using FluentValidation;
using WitchCityRope.Api.Features.Safety.Models;

namespace WitchCityRope.Api.Features.Safety.Validators;

/// <summary>
/// Validator for incident submission request
/// </summary>
public class CreateIncidentRequestValidator : AbstractValidator<CreateIncidentRequest>
{
    public CreateIncidentRequestValidator()
    {
        RuleFor(x => x.IncidentDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Incident date cannot be in the future");

        RuleFor(x => x.Location)
            .NotEmpty()
            .WithMessage("Location is required")
            .MaximumLength(200)
            .WithMessage("Location cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MinimumLength(10)
            .WithMessage("Description must be at least 10 characters")
            .MaximumLength(5000)
            .WithMessage("Description cannot exceed 5000 characters");

        RuleFor(x => x.InvolvedParties)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.InvolvedParties))
            .WithMessage("Involved parties information cannot exceed 2000 characters");

        RuleFor(x => x.Witnesses)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.Witnesses))
            .WithMessage("Witness information cannot exceed 2000 characters");

        RuleFor(x => x.ContactEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.ContactEmail))
            .WithMessage("Valid email address required");

        RuleFor(x => x.ContactName)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.ContactName))
            .WithMessage("Contact name cannot exceed 200 characters");

        // Business rule: If not anonymous, reporter ID must be provided
        RuleFor(x => x.ReporterId)
            .NotNull()
            .When(x => !x.IsAnonymous)
            .WithMessage("Reporter ID required for identified reports");

        // Business rule: Anonymous reports cannot request follow-up
        RuleFor(x => x.RequestFollowUp)
            .Equal(false)
            .When(x => x.IsAnonymous)
            .WithMessage("Anonymous reports cannot request follow-up");
    }
}
