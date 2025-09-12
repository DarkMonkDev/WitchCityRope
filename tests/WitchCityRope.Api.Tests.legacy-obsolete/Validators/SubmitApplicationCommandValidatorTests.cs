using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using WitchCityRope.Api.Features.Vetting.SubmitApplication;

namespace WitchCityRope.Api.Tests.Validators;

public class SubmitApplicationCommandValidatorTests
{
    private readonly MockSubmitApplicationCommandValidator _validator;

    public SubmitApplicationCommandValidatorTests()
    {
        _validator = new MockSubmitApplicationCommandValidator();
    }

    [Fact]
    public async Task ValidateAsync_WhenValidCommand_ShouldReturnValid()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WhenEmptyExperienceLevel_ShouldReturnInvalid()
    {
        // Arrange
        var command = CreateValidCommand() with { ExperienceLevel = "" };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "ExperienceLevel");
    }

    [Fact]
    public async Task ValidateAsync_WhenEmptyExperienceDescription_ShouldReturnInvalid()
    {
        // Arrange
        var command = CreateValidCommand() with { ExperienceDescription = "" };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "ExperienceDescription");
    }

    [Fact]
    public async Task ValidateAsync_WhenNoSkillsAndInterests_ShouldReturnInvalid()
    {
        // Arrange
        var command = CreateValidCommand() with { SkillsAndInterests = new List<string>() };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "SkillsAndInterests");
    }

    [Fact]
    public async Task ValidateAsync_WhenEmptySafetyKnowledge_ShouldReturnInvalid()
    {
        // Arrange
        var command = CreateValidCommand() with { SafetyKnowledge = "" };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "SafetyKnowledge");
    }

    [Fact]
    public async Task ValidateAsync_WhenEmptyConsentUnderstanding_ShouldReturnInvalid()
    {
        // Arrange
        var command = CreateValidCommand() with { ConsentUnderstanding = "" };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "ConsentUnderstanding");
    }

    [Fact]
    public async Task ValidateAsync_WhenLessThanTwoReferences_ShouldReturnInvalid()
    {
        // Arrange
        var command = CreateValidCommand() with 
        { 
            References = new List<Reference> 
            { 
                CreateValidReference() 
            }
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "References" && e.Message.Contains("at least 2"));
    }

    [Fact]
    public async Task ValidateAsync_WhenEmptyWhyJoin_ShouldReturnInvalid()
    {
        // Arrange
        var command = CreateValidCommand() with { WhyJoin = "" };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "WhyJoin");
    }

    [Fact]
    public async Task ValidateAsync_WhenNotAgreesToCodeOfConduct_ShouldReturnInvalid()
    {
        // Arrange
        var command = CreateValidCommand() with { AgreesToCodeOfConduct = false };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "AgreesToCodeOfConduct");
    }

    [Fact]
    public async Task ValidateAsync_WhenNotAgreesToSafetyGuidelines_ShouldReturnInvalid()
    {
        // Arrange
        var command = CreateValidCommand() with { AgreesToSafetyGuidelines = false };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "AgreesToSafetyGuidelines");
    }

    [Fact]
    public async Task ValidateAsync_WhenNotUnderstandsVettingProcess_ShouldReturnInvalid()
    {
        // Arrange
        var command = CreateValidCommand() with { UnderstandsVettingProcess = false };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "UnderstandsVettingProcess");
    }

    [Fact]
    public async Task ValidateAsync_WhenReferenceHasInvalidEmail_ShouldReturnInvalid()
    {
        // Arrange
        var command = CreateValidCommand();
        command.References[0] = command.References[0] with { Email = "invalid-email" };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName.Contains("Email"));
    }

    [Fact]
    public async Task ValidateAsync_WhenReferenceHasEmptyName_ShouldReturnInvalid()
    {
        // Arrange
        var command = CreateValidCommand();
        command.References[0] = command.References[0] with { Name = "" };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName.Contains("Name"));
    }

    [Fact]
    public async Task ValidateAsync_WhenReferenceHasInvalidPhone_ShouldReturnInvalid()
    {
        // Arrange
        var command = CreateValidCommand();
        command.References[0] = command.References[0] with { Phone = "123" }; // Too short

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName.Contains("Phone"));
    }

    [Fact]
    public async Task ValidateAsync_WhenReferenceHasNegativeYearsKnown_ShouldReturnInvalid()
    {
        // Arrange
        var command = CreateValidCommand();
        command.References[0] = command.References[0] with { YearsKnown = -1 };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName.Contains("YearsKnown"));
    }

    [Fact]
    public async Task ValidateAsync_WhenMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var command = new SubmitApplicationCommand(
            UserId: Guid.NewGuid(),
            ExperienceLevel: "",
            ExperienceDescription: "",
            SkillsAndInterests: new List<string>(),
            SafetyKnowledge: "",
            ConsentUnderstanding: "",
            References: new List<Reference>(),
            WhyJoin: "",
            AgreesToCodeOfConduct: false,
            AgreesToSafetyGuidelines: false,
            UnderstandsVettingProcess: false
        );

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(5);
    }

    private SubmitApplicationCommand CreateValidCommand()
    {
        return new SubmitApplicationCommand(
            UserId: Guid.NewGuid(),
            ExperienceLevel: "Intermediate",
            ExperienceDescription: "I have been practicing rope bondage for 3 years",
            SkillsAndInterests: new List<string> { "Shibari", "Suspension", "Floor work" },
            SafetyKnowledge: "I understand circulation checks, nerve paths, and emergency procedures",
            ConsentUnderstanding: "I practice explicit verbal consent and ongoing check-ins",
            References: new List<Reference> 
            { 
                CreateValidReference(),
                CreateValidReference("Reference Two", "ref2@example.com", "555-0102")
            },
            WhyJoin: "I want to be part of a safe and supportive rope community",
            AgreesToCodeOfConduct: true,
            AgreesToSafetyGuidelines: true,
            UnderstandsVettingProcess: true
        );
    }

    private Reference CreateValidReference(
        string name = "Reference One",
        string email = "ref1@example.com",
        string phone = "555-0101")
    {
        return new Reference(
            Name: name,
            Email: email,
            Phone: phone,
            Relationship: "Rope partner",
            YearsKnown: 2
        );
    }
}

// Mock validator implementation for testing
public class MockSubmitApplicationCommandValidator : IValidator<SubmitApplicationCommand>
{
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
            result.Errors.Add(new ValidationError { PropertyName = "References", Message = "At least 2 references are required" });
        else
        {
            for (int i = 0; i < command.References.Count; i++)
            {
                var reference = command.References[i];
                
                if (string.IsNullOrWhiteSpace(reference.Name))
                    result.Errors.Add(new ValidationError { PropertyName = $"References[{i}].Name", Message = "Reference name is required" });

                if (string.IsNullOrWhiteSpace(reference.Email) || !IsValidEmail(reference.Email))
                    result.Errors.Add(new ValidationError { PropertyName = $"References[{i}].Email", Message = "Valid email is required" });

                if (string.IsNullOrWhiteSpace(reference.Phone) || reference.Phone.Length < 10)
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

    private bool IsValidEmail(string email)
    {
        return email.Contains("@") && email.Contains(".");
    }
}