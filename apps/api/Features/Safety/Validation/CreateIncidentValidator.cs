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
        RuleFor(x => x.Severity)
            .IsInEnum()
            .WithMessage("Severity level must be Low, Medium, High, or Critical");

        RuleFor(x => x.IncidentDate)
            .NotEmpty()
            .WithMessage("Incident date is required")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Incident date cannot be more than 1 day in the future")
            .GreaterThan(DateTime.UtcNow.AddYears(-2))
            .WithMessage("Incident date cannot be more than 2 years in the past");

        RuleFor(x => x.Location)
            .NotEmpty()
            .WithMessage("Location is required")
            .Length(5, 200)
            .WithMessage("Location must be between 5 and 200 characters");

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

        RuleFor(x => x.ContactPhone)
            .Matches(@"^[\d\s\-\(\)\+\.]+$")
            .WithMessage("Phone number contains invalid characters")
            .Length(10, 20)
            .WithMessage("Phone number must be between 10 and 20 characters")
            .When(x => !string.IsNullOrEmpty(x.ContactPhone));

        // Business rule: Anonymous reports cannot request follow-up
        RuleFor(x => x.RequestFollowUp)
            .Equal(false)
            .WithMessage("Anonymous reports cannot request follow-up contact")
            .When(x => x.IsAnonymous);
    }
}