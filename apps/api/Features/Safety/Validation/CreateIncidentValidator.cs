using FluentValidation;
using WitchCityRope.Api.Features.Safety.Models;

namespace WitchCityRope.Api.Features.Safety.Validation;

/// <summary>
/// Validation rules for incident submission
/// </summary>
public class CreateIncidentValidator : AbstractValidator<CreateIncidentRequest>
{
    public CreateIncidentValidator()
    {
        RuleFor(x => x.IncidentDate)
            .NotEmpty()
            .WithMessage("Incident date is required")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Incident date cannot be more than 1 day in the future")
            .GreaterThan(DateTime.UtcNow.AddYears(-2))
            .WithMessage("Incident date cannot be more than 2 years in the past");

        // Title is auto-generated on backend, not user-provided
        RuleFor(x => x.Title)
            .Length(0, 200)
            .WithMessage("Title cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Title));

        // Location is now free-text field (not enum)
        RuleFor(x => x.Location)
            .NotEmpty()
            .WithMessage("Location is required")
            .MaximumLength(200)
            .WithMessage("Location cannot exceed 200 characters");

        // Type is required
        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Valid incident type is required");

        // WhereOccurred is required
        RuleFor(x => x.WhereOccurred)
            .IsInEnum()
            .WithMessage("Where incident occurred is required");

        // EventName is optional but has max length
        RuleFor(x => x.EventName)
            .MaximumLength(200)
            .WithMessage("Event name cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.EventName));

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Incident description is required")
            .Length(50, 5000)
            .WithMessage("Description must be between 50 and 5000 characters");

        RuleFor(x => x.InvolvedParties)
            .MaximumLength(2000)
            .WithMessage("Involved parties description cannot exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.InvolvedParties));

        RuleFor(x => x.Witnesses)
            .MaximumLength(2000)
            .WithMessage("Witness information cannot exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.Witnesses));

        // Contact validation for identified reports
        When(x => !x.IsAnonymous, () =>
        {
            RuleFor(x => x.ContactEmail)
                .NotEmpty()
                .WithMessage("Contact email is required for identified reports")
                .EmailAddress()
                .WithMessage("Valid email address is required");
        });

        RuleFor(x => x.ContactName)
            .MinimumLength(2)
            .WithMessage("Contact name must be at least 2 characters")
            .MaximumLength(200)
            .WithMessage("Contact name cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.ContactName));

        // Business rule: Anonymous reports cannot request follow-up
        RuleFor(x => x.RequestFollowUp)
            .Equal(false)
            .WithMessage("Anonymous reports cannot request follow-up contact")
            .When(x => x.IsAnonymous);
    }
}