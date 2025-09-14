using System;
using System.Threading.Tasks;

namespace WitchCityRope.Api.Features.CheckIn;

/// <summary>
/// Command to check in an attendee to an event
/// Supports check-in by confirmation code, QR code, or manual search
/// </summary>
public record CheckInAttendeeCommand(
    Guid EventId,
    string? ConfirmationCode,
    string? QrCodeData,
    Guid? UserId,
    Guid CheckedInBy,
    bool OverrideRestrictions = false
) : IRequest<CheckInAttendeeResult>;

public record CheckInAttendeeResult(
    Guid RegistrationId,
    Guid UserId,
    string AttendeeName,
    CheckInStatus Status,
    DateTime CheckedInAt,
    string? SpecialNotes,
    AttendeeDetails Details
);

public record AttendeeDetails(
    string? DietaryRestrictions,
    string? AccessibilityNeeds,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? PronouncedName,
    string? Pronouns,
    bool IsFirstTimeAttendee,
    bool HasCompletedWaiver
);

public enum CheckInStatus
{
    Success,
    AlreadyCheckedIn,
    NotRegistered,
    EventNotStarted,
    EventEnded,
    PaymentRequired,
    WaiverRequired
}

/// <summary>
/// Handles event check-in process
/// Validates registration, updates attendance, and handles special cases
/// </summary>
public class CheckInAttendeeCommandHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICheckInService _checkInService;
    private readonly IWaiverService _waiverService;

    public CheckInAttendeeCommandHandler(
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IUserRepository userRepository,
        ICheckInService checkInService,
        IWaiverService waiverService)
    {
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _userRepository = userRepository;
        _checkInService = checkInService;
        _waiverService = waiverService;
    }

    public async Task<CheckInAttendeeResult> Execute(CheckInAttendeeCommand command)
    {
        // Validate that at least one identifier is provided
        if (string.IsNullOrEmpty(command.ConfirmationCode) && 
            string.IsNullOrEmpty(command.QrCodeData) && 
            !command.UserId.HasValue)
        {
            throw new ValidationException("At least one identifier (confirmation code, QR code, or user ID) must be provided");
        }

        // Get event details
        var @event = await _eventRepository.GetByIdAsync(command.EventId);
        if (@event == null)
        {
            throw new NotFoundException("Event not found");
        }

        // Verify check-in staff has permission
        var staff = await _userRepository.GetByIdAsync(command.CheckedInBy);
        if (staff == null)
        {
            throw new NotFoundException("Staff member not found");
        }

        if (!staff.Roles.Contains("CheckInStaff") && 
            !staff.Roles.Contains("Organizer") && 
            !staff.Roles.Contains("Admin"))
        {
            throw new ForbiddenException("User does not have permission to check in attendees");
        }

        // Find registration
        Registration? registration = null;
        
        if (!string.IsNullOrEmpty(command.QrCodeData))
        {
            // Parse QR code data
            var qrData = _checkInService.ParseQrCode(command.QrCodeData);
            registration = await _registrationRepository.GetByIdAsync(qrData.RegistrationId);
        }
        else if (!string.IsNullOrEmpty(command.ConfirmationCode))
        {
            registration = await _registrationRepository.GetByConfirmationCodeAsync(
                command.EventId, 
                command.ConfirmationCode
            );
        }
        else if (command.UserId.HasValue)
        {
            registration = await _registrationRepository.GetByEventAndUserAsync(
                command.EventId, 
                command.UserId.Value
            );
        }

        if (registration == null)
        {
            return new CheckInAttendeeResult(
                RegistrationId: Guid.Empty,
                UserId: command.UserId ?? Guid.Empty,
                AttendeeName: "Unknown",
                Status: CheckInStatus.NotRegistered,
                CheckedInAt: DateTime.UtcNow,
                SpecialNotes: null,
                Details: new AttendeeDetails(null, null, null, null, null, null, false, false)
            );
        }

        // Get user details
        var user = await _userRepository.GetByIdAsync(registration.UserId);
        if (user == null)
        {
            throw new NotFoundException("User not found");
        }

        // Check if already checked in
        if (registration.CheckedInAt.HasValue)
        {
            return new CheckInAttendeeResult(
                RegistrationId: registration.Id,
                UserId: user.Id,
                AttendeeName: user.DisplayName,
                Status: CheckInStatus.AlreadyCheckedIn,
                CheckedInAt: registration.CheckedInAt.Value,
                SpecialNotes: "Already checked in at " + registration.CheckedInAt.Value.ToString("g"),
                Details: BuildAttendeeDetails(user, registration)
            );
        }

        // Validate event timing (unless override is specified)
        if (!command.OverrideRestrictions)
        {
            var now = DateTime.UtcNow;
            var checkInWindow = TimeSpan.FromMinutes(30); // Allow check-in 30 minutes early

            if (now < @event.StartDateTime.Subtract(checkInWindow))
            {
                return new CheckInAttendeeResult(
                    RegistrationId: registration.Id,
                    UserId: user.Id,
                    AttendeeName: user.DisplayName,
                    Status: CheckInStatus.EventNotStarted,
                    CheckedInAt: DateTime.UtcNow,
                    SpecialNotes: $"Event starts at {@event.StartDateTime:g}",
                    Details: BuildAttendeeDetails(user, registration)
                );
            }

            if (now > @event.EndDateTime)
            {
                return new CheckInAttendeeResult(
                    RegistrationId: registration.Id,
                    UserId: user.Id,
                    AttendeeName: user.DisplayName,
                    Status: CheckInStatus.EventEnded,
                    CheckedInAt: DateTime.UtcNow,
                    SpecialNotes: $"Event ended at {@event.EndDateTime:g}",
                    Details: BuildAttendeeDetails(user, registration)
                );
            }
        }

        // Check waiver status
        var hasValidWaiver = await _waiverService.HasValidWaiverAsync(user.Id);
        if (!hasValidWaiver && !command.OverrideRestrictions)
        {
            return new CheckInAttendeeResult(
                RegistrationId: registration.Id,
                UserId: user.Id,
                AttendeeName: user.DisplayName,
                Status: CheckInStatus.WaiverRequired,
                CheckedInAt: DateTime.UtcNow,
                SpecialNotes: "Waiver must be signed before check-in",
                Details: BuildAttendeeDetails(user, registration)
            );
        }

        // Perform check-in
        registration.CheckedInAt = DateTime.UtcNow;
        registration.CheckedInBy = command.CheckedInBy;
        await _registrationRepository.UpdateAsync(registration);

        // Update event statistics
        await _eventRepository.IncrementCheckedInCountAsync(command.EventId);

        // Get attendance history for first-time detection
        var isFirstTime = await _checkInService.IsFirstTimeAttendeeAsync(user.Id);

        // Build special notes
        var specialNotes = BuildSpecialNotes(user, registration, isFirstTime);

        return new CheckInAttendeeResult(
            RegistrationId: registration.Id,
            UserId: user.Id,
            AttendeeName: user.DisplayName,
            Status: CheckInStatus.Success,
            CheckedInAt: registration.CheckedInAt.Value,
            SpecialNotes: specialNotes,
            Details: BuildAttendeeDetails(user, registration, isFirstTime, hasValidWaiver)
        );
    }

    private AttendeeDetails BuildAttendeeDetails(
        User user, 
        Registration registration, 
        bool? isFirstTime = null, 
        bool? hasWaiver = null)
    {
        return new AttendeeDetails(
            DietaryRestrictions: registration.DietaryRestrictions,
            AccessibilityNeeds: registration.AccessibilityNeeds,
            EmergencyContactName: registration.EmergencyContactName,
            EmergencyContactPhone: registration.EmergencyContactPhone,
            PronouncedName: user.PronouncedName,
            Pronouns: user.Pronouns,
            IsFirstTimeAttendee: isFirstTime ?? false,
            HasCompletedWaiver: hasWaiver ?? true
        );
    }

    private string? BuildSpecialNotes(User user, Registration registration, bool isFirstTime)
    {
        var notes = new List<string>();

        if (isFirstTime)
            notes.Add("FIRST TIME ATTENDEE - Please provide extra welcome!");

        if (!string.IsNullOrEmpty(registration.AccessibilityNeeds))
            notes.Add($"Accessibility needs: {registration.AccessibilityNeeds}");

        if (!string.IsNullOrEmpty(registration.DietaryRestrictions))
            notes.Add($"Dietary restrictions: {registration.DietaryRestrictions}");

        if (user.PronouncedName != user.DisplayName && !string.IsNullOrEmpty(user.PronouncedName))
            notes.Add($"Name pronounced: {user.PronouncedName}");

        return notes.Any() ? string.Join(" | ", notes) : null;
    }
}

// Marker interface for future MediatR implementation
public interface IRequest<TResponse> { }

// Custom exceptions
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

// Domain models
public class Registration
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public string? DietaryRestrictions { get; set; }
    public string? AccessibilityNeeds { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public Guid? CheckedInBy { get; set; }
}

public class Event
{
    public Guid Id { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
}

public class User
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string PronouncedName { get; set; } = string.Empty;
    public string Pronouns { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

// Service models
public class QrCodeData
{
    public Guid RegistrationId { get; set; }
    public Guid EventId { get; set; }
    public string ConfirmationCode { get; set; } = string.Empty;
}

// Repository and service interfaces
public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid eventId);
    Task IncrementCheckedInCountAsync(Guid eventId);
}

public interface IRegistrationRepository
{
    Task<Registration?> GetByIdAsync(Guid registrationId);
    Task<Registration?> GetByConfirmationCodeAsync(Guid eventId, string confirmationCode);
    Task<Registration?> GetByEventAndUserAsync(Guid eventId, Guid userId);
    Task UpdateAsync(Registration registration);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId);
}

public interface ICheckInService
{
    QrCodeData ParseQrCode(string qrCodeData);
    Task<bool> IsFirstTimeAttendeeAsync(Guid userId);
}

public interface IWaiverService
{
    Task<bool> HasValidWaiverAsync(Guid userId);
}