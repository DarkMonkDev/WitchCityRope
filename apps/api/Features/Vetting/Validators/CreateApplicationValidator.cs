using FluentValidation;
using WitchCityRope.Api.Features.Vetting.Models;

namespace WitchCityRope.Api.Features.Vetting.Validators;

/// <summary>
/// FluentValidation validator for vetting application submission
/// Comprehensive validation following WitchCityRope patterns
/// </summary>
public class CreateApplicationValidator : AbstractValidator<CreateApplicationRequest>
{
    public CreateApplicationValidator()
    {
        // Personal Information (Step 1)
        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("Full name is required")
            .Length(2, 50)
            .WithMessage("Full name must be between 2 and 50 characters")
            .Matches(@"^[a-zA-Z\s\-'\.]+$")
            .WithMessage("Full name can only contain letters, spaces, hyphens, apostrophes, and periods");

        RuleFor(x => x.SceneName)
            .NotEmpty()
            .WithMessage("Scene name is required")
            .Length(2, 50)
            .WithMessage("Scene name must be between 2 and 50 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_\.]+$")
            .WithMessage("Scene name can only contain letters, numbers, spaces, hyphens, underscores, and periods");

        RuleFor(x => x.Pronouns)
            .MaximumLength(100)
            .WithMessage("Pronouns must be 100 characters or less")
            .When(x => !string.IsNullOrEmpty(x.Pronouns));

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email address is required")
            .EmailAddress()
            .WithMessage("Please provide a valid email address")
            .MaximumLength(255)
            .WithMessage("Email address is too long");

        RuleFor(x => x.Phone)
            .Matches(@"^[\+]?[1-9][\d]{0,15}$")
            .WithMessage("Please provide a valid phone number")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        // Experience & Knowledge (Step 2)
        RuleFor(x => x.ExperienceLevel)
            .InclusiveBetween(1, 4)
            .WithMessage("Please select a valid experience level");

        RuleFor(x => x.YearsExperience)
            .InclusiveBetween(0, 50)
            .WithMessage("Years of experience must be between 0 and 50");

        RuleFor(x => x.ExperienceDescription)
            .NotEmpty()
            .WithMessage("Experience description is required")
            .Length(50, 500)
            .WithMessage("Experience description must be between 50 and 500 characters");

        RuleFor(x => x.SafetyKnowledge)
            .NotEmpty()
            .WithMessage("Safety knowledge assessment is required")
            .Length(30, 300)
            .WithMessage("Safety knowledge assessment must be between 30 and 300 characters");

        RuleFor(x => x.ConsentUnderstanding)
            .NotEmpty()
            .WithMessage("Consent understanding is required")
            .Length(30, 300)
            .WithMessage("Consent understanding must be between 30 and 300 characters");

        // Community Understanding (Step 3)
        RuleFor(x => x.WhyJoinCommunity)
            .NotEmpty()
            .WithMessage("Please explain why you want to join the community")
            .Length(50, 400)
            .WithMessage("Community interest explanation must be between 50 and 400 characters");

        RuleFor(x => x.SkillsInterests)
            .NotEmpty()
            .WithMessage("Please select at least one skill or interest")
            .Must(x => x.Count <= 10)
            .WithMessage("Please select no more than 10 skills or interests");

        RuleForEach(x => x.SkillsInterests)
            .NotEmpty()
            .WithMessage("Skill/interest cannot be empty")
            .MaximumLength(50)
            .WithMessage("Each skill/interest must be 50 characters or less");

        RuleFor(x => x.ExpectationsGoals)
            .NotEmpty()
            .WithMessage("Expectations and goals are required")
            .Length(30, 300)
            .WithMessage("Expectations and goals must be between 30 and 300 characters");

        RuleFor(x => x.AgreesToGuidelines)
            .Equal(true)
            .WithMessage("You must agree to the community guidelines");

        // References (Step 4)
        RuleFor(x => x.References)
            .NotEmpty()
            .WithMessage("At least one reference is required")
            .Must(x => x.Count >= 2)
            .WithMessage("At least two references are required")
            .Must(x => x.Count <= 3)
            .WithMessage("No more than three references allowed");

        RuleForEach(x => x.References).SetValidator(new ReferenceValidator());

        // Ensure reference orders are valid
        RuleFor(x => x.References)
            .Must(refs => refs.Select(r => r.Order).Distinct().Count() == refs.Count)
            .WithMessage("Reference order numbers must be unique")
            .Must(refs => refs.All(r => r.Order >= 1 && r.Order <= 3))
            .WithMessage("Reference order must be 1, 2, or 3");

        // Ensure no duplicate emails in references
        RuleFor(x => x.References)
            .Must(refs => refs.Select(r => r.Email.ToLowerInvariant()).Distinct().Count() == refs.Count)
            .WithMessage("Reference email addresses must be unique");

        // Review & Submit (Step 5)
        RuleFor(x => x.AgreesToTerms)
            .Equal(true)
            .WithMessage("You must agree to the terms and conditions");

        RuleFor(x => x.ConsentToContact)
            .Equal(true)
            .WithMessage("You must consent to being contacted during the vetting process");

        // Cross-field validation: applicant email cannot be same as reference email
        RuleFor(x => x)
            .Must(x => !x.References.Any(r => r.Email.Equals(x.Email, StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Your email address cannot be used as a reference");
    }
}

/// <summary>
/// Validator for individual reference entries
/// </summary>
public class ReferenceValidator : AbstractValidator<ReferenceRequest>
{
    public ReferenceValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Reference name is required")
            .Length(2, 100)
            .WithMessage("Reference name must be between 2 and 100 characters")
            .Matches(@"^[a-zA-Z\s\-'\.]+$")
            .WithMessage("Reference name can only contain letters, spaces, hyphens, apostrophes, and periods");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Reference email is required")
            .EmailAddress()
            .WithMessage("Please provide a valid email address for the reference")
            .MaximumLength(255)
            .WithMessage("Reference email address is too long");

        RuleFor(x => x.Relationship)
            .NotEmpty()
            .WithMessage("Relationship description is required")
            .Length(5, 200)
            .WithMessage("Relationship description must be between 5 and 200 characters");

        RuleFor(x => x.Order)
            .InclusiveBetween(1, 3)
            .WithMessage("Reference order must be 1, 2, or 3");
    }
}