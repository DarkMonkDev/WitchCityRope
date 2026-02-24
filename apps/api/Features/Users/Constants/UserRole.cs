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
/// - EventOrganizer: Can organize and manage events
/// - DungeonMonitor: Monitors play spaces and ensures participant safety during events
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    /// <summary>
    /// Represents the absence of a special role - NEVER assign this as a role.
    /// Exists in the enum for serialization/display purposes only.
    /// Users without special roles have Role = "" (empty string) in the database.
    /// Excluded from UserRoleConstants.ValidRoles by design.
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
    /// Can organize and manage events.
    /// Has elevated permissions for event creation and management.
    /// </summary>
    EventOrganizer,

    /// <summary>
    /// Dungeon monitor responsible for safety monitoring during events.
    /// Monitors play spaces, enforces rules, and ensures participant safety.
    /// No elevated system permissions beyond regular member access.
    /// </summary>
    DungeonMonitor
}
