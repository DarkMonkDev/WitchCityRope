using System.Text.Json.Serialization;

namespace WitchCityRope.Api.Features.Users.Constants;

/// <summary>
/// Defines all user roles in the WitchCityRope system.
/// This enum is the single source of truth for role authorization and is auto-generated to TypeScript.
/// </summary>
/// <remarks>
/// Role semantics:
/// - Member: Regular member with no special privileges (default, not assigned as a role)
/// - Teacher: Can create and teach events/classes
/// - SafetyTeam: Part of the safety coordination team
/// - Administrator: Full administrative access to the system
/// - CheckInStaff: Can manage check-in at events
/// - EventOrganizer: Can organize and manage events
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    /// <summary>
    /// Regular member with no special privileges.
    /// This is the default state - not assigned as an actual role in ASP.NET Identity.
    /// </summary>
    Member,

    /// <summary>
    /// Can create and teach events/classes.
    /// Has access to create workshops and educational content.
    /// </summary>
    Teacher,

    /// <summary>
    /// Part of the safety coordination team.
    /// Can view and manage incident reports and safety protocols.
    /// </summary>
    SafetyTeam,

    /// <summary>
    /// Full administrative access to the system.
    /// Can manage users, content, settings, and all system features.
    /// </summary>
    Administrator,

    /// <summary>
    /// Can manage check-in at events.
    /// Has access to check-in kiosks and attendance tracking.
    /// </summary>
    CheckInStaff,

    /// <summary>
    /// Can organize and manage events.
    /// Has elevated permissions for event creation and management.
    /// </summary>
    EventOrganizer
}
