using FluentValidation;
using WitchCityRope.Api.Features.CheckIn.Models;

namespace WitchCityRope.Api.Features.CheckIn.Validation;

/// <summary>
/// Validation rules for sync requests
/// </summary>
public class SyncRequestValidator : AbstractValidator<SyncRequest>
{
    public SyncRequestValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty()
            .WithMessage("Device ID is required")
            .MaximumLength(100)
            .WithMessage("Device ID cannot exceed 100 characters");

        RuleFor(x => x.PendingCheckIns)
            .NotNull()
            .WithMessage("Pending check-ins list is required");

        RuleForEach(x => x.PendingCheckIns)
            .SetValidator(new PendingCheckInValidator());

        RuleFor(x => x.LastSyncTimestamp)
            .NotEmpty()
            .WithMessage("Last sync timestamp is required")
            .Must(BeValidDateTime)
            .WithMessage("Last sync timestamp must be a valid ISO 8601 datetime");

        RuleFor(x => x.PendingCheckIns)
            .Must(x => x.Count <= 100)
            .WithMessage("Cannot sync more than 100 pending check-ins at once");
    }

    private static bool BeValidDateTime(string value)
    {
        return DateTime.TryParse(value, out _);
    }
}

/// <summary>
/// Validation rules for pending check-in data
/// </summary>
public class PendingCheckInValidator : AbstractValidator<PendingCheckIn>
{
    public PendingCheckInValidator()
    {
        RuleFor(x => x.LocalId)
            .NotEmpty()
            .WithMessage("Local ID is required")
            .MaximumLength(50)
            .WithMessage("Local ID cannot exceed 50 characters");

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