using FluentValidation;
using WitchCityRope.Api.Features.CheckIn.Models;

namespace WitchCityRope.Api.Features.CheckIn.Validation;

/// <summary>
/// Validation rules for check-in requests
/// </summary>
public class CheckInRequestValidator : AbstractValidator<CheckInRequest>
{
    public CheckInRequestValidator()
    {
        RuleFor(x => x.AttendeeId)
            .NotEmpty()
            .WithMessage("Attendee ID is required")
            .Must(BeValidGuid)
            .WithMessage("Attendee ID must be a valid GUID");

        RuleFor(x => x.CheckInTime)
            .NotEmpty()
            .WithMessage("Check-in time is required")
            .Must(BeValidDateTime)
            .WithMessage("Check-in time must be a valid ISO 8601 datetime");

        RuleFor(x => x.StaffMemberId)
            .NotEmpty()
            .WithMessage("Staff member ID is required")
            .Must(BeValidGuid)
            .WithMessage("Staff member ID must be a valid GUID");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters");

        When(x => x.IsManualEntry, () =>
        {
            RuleFor(x => x.ManualEntryData)
                .NotNull()
                .WithMessage("Manual entry data is required for manual entries")
                .SetValidator(new ManualEntryDataValidator()!);
        });

        When(x => !x.IsManualEntry, () =>
        {
            RuleFor(x => x.ManualEntryData)
                .Null()
                .WithMessage("Manual entry data should not be provided for regular check-ins");
        });
    }

    private static bool BeValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }

    private static bool BeValidDateTime(string value)
    {
        return DateTime.TryParse(value, out _);
    }
}

/// <summary>
/// Validation rules for manual entry data
/// </summary>
public class ManualEntryDataValidator : AbstractValidator<ManualEntryData>
{
    public ManualEntryDataValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required for manual entries")
            .MaximumLength(100)
            .WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required for manual entries")
            .EmailAddress()
            .WithMessage("Email must be a valid email address")
            .MaximumLength(255)
            .WithMessage("Email cannot exceed 255 characters");

        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithMessage("Phone is required for manual entries")
            .MaximumLength(20)
            .WithMessage("Phone cannot exceed 20 characters")
            .Matches(@"^[\d\s\-\+\(\)\.]+$")
            .WithMessage("Phone must contain only numbers, spaces, and common phone formatting characters");

        RuleFor(x => x.DietaryRestrictions)
            .MaximumLength(500)
            .WithMessage("Dietary restrictions cannot exceed 500 characters");

        RuleFor(x => x.AccessibilityNeeds)
            .MaximumLength(500)
            .WithMessage("Accessibility needs cannot exceed 500 characters");

        RuleFor(x => x.HasCompletedWaiver)
            .Equal(true)
            .WithMessage("Waiver must be completed before check-in");
    }
}