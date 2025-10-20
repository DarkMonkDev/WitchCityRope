using FluentValidation;
using WitchCityRope.Api.Features.Safety.Models;

namespace WitchCityRope.Api.Features.Safety.Validators;

/// <summary>
/// Validator for add note request
/// </summary>
public class AddNoteRequestValidator : AbstractValidator<AddNoteRequest>
{
    public AddNoteRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Note content is required")
            .MinimumLength(3)
            .WithMessage("Note content must be at least 3 characters")
            .MaximumLength(5000)
            .WithMessage("Note content cannot exceed 5000 characters");

        RuleFor(x => x.Tags)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.Tags))
            .WithMessage("Tags cannot exceed 200 characters");
    }
}
