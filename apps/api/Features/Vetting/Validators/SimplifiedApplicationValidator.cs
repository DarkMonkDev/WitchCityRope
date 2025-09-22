using FluentValidation;
using WitchCityRope.Api.Features.Vetting.Models;

namespace WitchCityRope.Api.Features.Vetting.Validators;

/// <summary>
/// Validator for simplified vetting application requests
/// Matches the client-side validation rules from the React form
/// </summary>
public class SimplifiedApplicationValidator : AbstractValidator<SimplifiedApplicationRequest>
{
    public SimplifiedApplicationValidator()
    {
        RuleFor(x => x.RealName)
            .NotEmpty()
            .WithMessage("Real name is required")
            .Length(2, 100)
            .WithMessage("Real name must be between 2 and 100 characters");

        RuleFor(x => x.PreferredSceneName)
            .NotEmpty()
            .WithMessage("Preferred scene name is required")
            .Length(2, 50)
            .WithMessage("Scene name must be between 2 and 50 characters");

        RuleFor(x => x.FetLifeHandle)
            .MaximumLength(50)
            .WithMessage("FetLife handle cannot exceed 50 characters")
            .Must(handle => string.IsNullOrEmpty(handle) || !handle.StartsWith("@"))
            .WithMessage("FetLife handle should not include the @ symbol");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email address is required")
            .EmailAddress()
            .WithMessage("Please enter a valid email address");

        RuleFor(x => x.WhyJoin)
            .NotEmpty()
            .WithMessage("Please explain why you'd like to join Witch City Rope")
            .Length(20, 2000)
            .WithMessage("Why join description must be between 20 and 2000 characters");

        RuleFor(x => x.ExperienceWithRope)
            .NotEmpty()
            .WithMessage("Experience description is required")
            .Length(50, 2000)
            .WithMessage("Experience description must be between 50 and 2000 characters");

        RuleFor(x => x.AgreeToCommunityStandards)
            .Equal(true)
            .WithMessage("You must agree to the community standards to submit an application");

        RuleFor(x => x.HowFoundUs)
            .MaximumLength(500)
            .WithMessage("How found us description cannot exceed 500 characters");
    }
}