using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using FluentAssertions;
using Xunit;
using WitchCityRope.Api.Features.Auth.Models;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Models;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Api.Tests.Models;

public class RequestModelValidationTests
{
    #region LoginRequest Validation Tests

    [Fact]
    public void LoginRequest_WhenValid_ShouldPassValidation()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "SecurePassword123!"
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void LoginRequest_WhenEmailEmpty_ShouldFailValidation(string email)
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = email,
            Password = "Password123!"
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("Email"));
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user.example.com")]
    public void LoginRequest_WhenEmailInvalid_ShouldFailValidation(string email)
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = email,
            Password = "Password123!"
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("Email"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void LoginRequest_WhenPasswordEmpty_ShouldFailValidation(string password)
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = password
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("Password"));
    }

    #endregion

    #region RegisterRequest Validation Tests

    [Fact]
    public void RegisterRequest_WhenValid_ShouldPassValidation()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "SecurePassword123!",
            SceneName = "NewUser",
            LegalName = "John Doe",
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            PronouncedName = "John",
            Pronouns = "he/him"
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData("ab")] // Too short
    [InlineData("a")] // Way too short
    public void RegisterRequest_WhenSceneNameTooShort_ShouldFailValidation(string sceneName)
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = "Password123!",
            SceneName = sceneName,
            LegalName = "John Doe",
            DateOfBirth = DateTime.UtcNow.AddYears(-25)
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("SceneName"));
    }

    [Fact]
    public void RegisterRequest_WhenSceneNameTooLong_ShouldFailValidation()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = "Password123!",
            SceneName = new string('a', 51), // 51 characters
            LegalName = "John Doe",
            DateOfBirth = DateTime.UtcNow.AddYears(-25)
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("SceneName"));
    }

    [Theory]
    [InlineData("short")] // Less than 8 characters
    [InlineData("password")] // No uppercase or numbers
    [InlineData("PASSWORD")] // No lowercase or numbers
    [InlineData("Password")] // No numbers
    [InlineData("12345678")] // No letters
    public void RegisterRequest_WhenPasswordWeak_ShouldFailValidation(string password)
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = password,
            SceneName = "TestUser",
            LegalName = "John Doe",
            DateOfBirth = DateTime.UtcNow.AddYears(-25)
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("Password"));
    }

    [Fact]
    public void RegisterRequest_WhenDateOfBirthInFuture_ShouldFailValidation()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = "Password123!",
            SceneName = "TestUser",
            LegalName = "John Doe",
            DateOfBirth = DateTime.UtcNow.AddDays(1) // Future date
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("DateOfBirth"));
    }

    #endregion

    #region CreateEventRequest Validation Tests

    [Fact]
    public void CreateEventRequest_WhenValid_ShouldPassValidation()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = "Test Event",
            Description = "A test event description",
            Type = EventType.Workshop,
            StartDateTime = DateTime.UtcNow.AddDays(7),
            EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(2),
            Location = "Test Venue",
            MaxAttendees = 20,
            Price = 50.00m,
            RequiredSkillLevels = new List<string> { "Beginner" },
            Tags = new List<string> { "rope", "workshop" },
            RequiresVetting = false,
            SafetyNotes = "Standard safety rules apply",
            EquipmentProvided = "Rope provided",
            EquipmentRequired = "Comfortable clothes"
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateEventRequest_WhenTitleEmpty_ShouldFailValidation(string title)
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = title,
            Description = "Description",
            Type = EventType.Workshop,
            StartDateTime = DateTime.UtcNow.AddDays(7),
            EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(2),
            Location = "Venue",
            MaxAttendees = 20
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("Title"));
    }

    [Fact]
    public void CreateEventRequest_WhenTitleTooLong_ShouldFailValidation()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = new string('a', 201), // 201 characters
            Description = "Description",
            Type = EventType.Workshop,
            StartDateTime = DateTime.UtcNow.AddDays(7),
            EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(2),
            Location = "Venue",
            MaxAttendees = 20
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("Title"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateEventRequest_WhenMaxAttendeesInvalid_ShouldFailValidation(int maxAttendees)
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = "Test Event",
            Description = "Description",
            Type = EventType.Workshop,
            StartDateTime = DateTime.UtcNow.AddDays(7),
            EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(2),
            Location = "Venue",
            MaxAttendees = maxAttendees
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("MaxAttendees"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void CreateEventRequest_WhenPriceNegative_ShouldFailValidation(decimal price)
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = "Test Event",
            Description = "Description",
            Type = EventType.Workshop,
            StartDateTime = DateTime.UtcNow.AddDays(7),
            EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(2),
            Location = "Venue",
            MaxAttendees = 20,
            Price = price
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("Price"));
    }

    #endregion

    #region UpdateProfileRequest Validation Tests

    [Fact]
    public void UpdateProfileRequest_WhenValid_ShouldPassValidation()
    {
        // Arrange
        var request = new UpdateProfileRequest
        {
            SceneName = "UpdatedName",
            Bio = "Updated bio text",
            Pronouns = "they/them",
            PublicProfile = true
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void UpdateProfileRequest_WhenAllFieldsNull_ShouldPassValidation()
    {
        // Arrange - All fields are optional for partial updates
        var request = new UpdateProfileRequest();

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void UpdateProfileRequest_WhenBioTooLong_ShouldFailValidation()
    {
        // Arrange
        var request = new UpdateProfileRequest
        {
            Bio = new string('a', 1001) // 1001 characters
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("Bio"));
    }

    #endregion

    #region Helper Methods

    private IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, context, validationResults, true);
        return validationResults;
    }

    #endregion
}

// Add validation attributes to models for testing
public static class ModelValidationAttributes
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", 
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Scene name is required")]
        [MinLength(3, ErrorMessage = "Scene name must be at least 3 characters")]
        [MaxLength(50, ErrorMessage = "Scene name must not exceed 50 characters")]
        public string SceneName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Legal name is required")]
        public string LegalName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        [DateNotInFuture]
        public DateTime DateOfBirth { get; set; }

        public string? PronouncedName { get; set; }
        public string? Pronouns { get; set; }
    }

    public class CreateEventRequest
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200, ErrorMessage = "Title must not exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Event type is required")]
        public EventType Type { get; set; }

        [Required(ErrorMessage = "Start date/time is required")]
        public DateTime StartDateTime { get; set; }

        [Required(ErrorMessage = "End date/time is required")]
        public DateTime EndDateTime { get; set; }

        [Required(ErrorMessage = "Location is required")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Maximum attendees is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Maximum attendees must be at least 1")]
        public int MaxAttendees { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Price cannot be negative")]
        public decimal Price { get; set; }

        public List<string> RequiredSkillLevels { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public bool RequiresVetting { get; set; }
        public string? SafetyNotes { get; set; }
        public string? EquipmentProvided { get; set; }
        public string? EquipmentRequired { get; set; }
        public Guid OrganizerId { get; set; }
    }

    public class UpdateProfileRequest
    {
        [MinLength(3, ErrorMessage = "Scene name must be at least 3 characters")]
        [MaxLength(50, ErrorMessage = "Scene name must not exceed 50 characters")]
        public string? SceneName { get; set; }

        [MaxLength(1000, ErrorMessage = "Bio must not exceed 1000 characters")]
        public string? Bio { get; set; }

        public string? Pronouns { get; set; }
        public bool? PublicProfile { get; set; }
    }

    // Custom validation attribute
    public class DateNotInFutureAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is DateTime dateTime && dateTime > DateTime.UtcNow)
            {
                return new ValidationResult("Date cannot be in the future");
            }
            return ValidationResult.Success;
        }
    }
}