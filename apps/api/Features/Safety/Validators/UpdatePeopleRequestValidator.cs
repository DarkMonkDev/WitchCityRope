using FluentValidation;
using WitchCityRope.Api.Features.Safety.Models;

namespace WitchCityRope.Api.Features.Safety.Validators;

/// <summary>
/// Validator for incident people update request
/// </summary>
public class UpdatePeopleRequestValidator : AbstractValidator<UpdatePeopleRequest>
{
    public UpdatePeopleRequestValidator()
    {
        // At least one field must be provided (not both null)
        RuleFor(x => x)
            .Must(x => x.InvolvedParties != null || x.Witnesses != null)
            .WithMessage("At least one field (InvolvedParties or Witnesses) must be provided");

        // Validate involved parties length if provided
        RuleFor(x => x.InvolvedParties)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.InvolvedParties))
            .WithMessage("Involved parties text cannot exceed 2000 characters");

        // Validate witnesses length if provided
        RuleFor(x => x.Witnesses)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.Witnesses))
            .WithMessage("Witnesses text cannot exceed 2000 characters");
    }
}
