using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WitchCityRope.Api.Features.Vetting.SubmitApplication;

/// <summary>
/// Validator for SubmitApplicationCommand
/// Ensures all required fields are present and valid
/// </summary>
public class SubmitApplicationCommandValidator : IValidator<SubmitApplicationCommand>
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"^\d{10,}$", RegexOptions.Compiled);

    public Task<ValidationResult> ValidateAsync(SubmitApplicationCommand command)
    {
        var result = new ValidationResult { IsValid = true };

        // Validate required fields
        if (string.IsNullOrWhiteSpace(command.ExperienceLevel))
            result.Errors.Add(new ValidationError { PropertyName = "ExperienceLevel", Message = "Experience level is required" });

        if (string.IsNullOrWhiteSpace(command.ExperienceDescription))
            result.Errors.Add(new ValidationError { PropertyName = "ExperienceDescription", Message = "Experience description is required" });

        if (command.SkillsAndInterests == null || !command.SkillsAndInterests.Any())
            result.Errors.Add(new ValidationError { PropertyName = "SkillsAndInterests", Message = "At least one skill or interest is required" });

        if (string.IsNullOrWhiteSpace(command.SafetyKnowledge))
            result.Errors.Add(new ValidationError { PropertyName = "SafetyKnowledge", Message = "Safety knowledge is required" });

        if (string.IsNullOrWhiteSpace(command.ConsentUnderstanding))
            result.Errors.Add(new ValidationError { PropertyName = "ConsentUnderstanding", Message = "Consent understanding is required" });

        if (string.IsNullOrWhiteSpace(command.WhyJoin))
            result.Errors.Add(new ValidationError { PropertyName = "WhyJoin", Message = "Reason for joining is required" });

        // Validate references
        if (command.References == null || command.References.Count < 2)
        {
            result.Errors.Add(new ValidationError { PropertyName = "References", Message = "At least 2 references are required" });
        }
        else
        {
            for (int i = 0; i < command.References.Count; i++)
            {
                var reference = command.References[i];
                
                if (string.IsNullOrWhiteSpace(reference.Name))
                    result.Errors.Add(new ValidationError { PropertyName = $"References[{i}].Name", Message = "Reference name is required" });

                if (string.IsNullOrWhiteSpace(reference.Email) || !EmailRegex.IsMatch(reference.Email))
                    result.Errors.Add(new ValidationError { PropertyName = $"References[{i}].Email", Message = "Valid email is required" });

                // Clean phone number and validate
                var cleanPhone = reference.Phone?.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "").Replace("+", "");
                if (string.IsNullOrWhiteSpace(cleanPhone) || cleanPhone.Length < 10)
                    result.Errors.Add(new ValidationError { PropertyName = $"References[{i}].Phone", Message = "Valid phone number is required" });

                if (reference.YearsKnown < 0)
                    result.Errors.Add(new ValidationError { PropertyName = $"References[{i}].YearsKnown", Message = "Years known must be non-negative" });
            }
        }

        // Validate agreements
        if (!command.AgreesToCodeOfConduct)
            result.Errors.Add(new ValidationError { PropertyName = "AgreesToCodeOfConduct", Message = "Must agree to code of conduct" });

        if (!command.AgreesToSafetyGuidelines)
            result.Errors.Add(new ValidationError { PropertyName = "AgreesToSafetyGuidelines", Message = "Must agree to safety guidelines" });

        if (!command.UnderstandsVettingProcess)
            result.Errors.Add(new ValidationError { PropertyName = "UnderstandsVettingProcess", Message = "Must understand vetting process" });

        result.IsValid = !result.Errors.Any();
        return Task.FromResult(result);
    }
}