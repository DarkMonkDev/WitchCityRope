namespace WitchCityRope.Api.Features.Users.Constants;

/// <summary>
/// Defines the valid user roles in the WitchCityRope system.
/// This is the single source of truth for all role validation.
/// </summary>
/// <remarks>
/// Role semantics:
/// - Empty string or null = Regular member with no special privileges
/// - "Teacher" = Can create and teach events/classes
/// - "SafetyTeam" = Part of the safety coordination team
/// - "Administrator" = Full administrative access to the system
/// </remarks>
public static class UserRoleConstants
{
    /// <summary>
    /// Valid user roles in the system.
    /// Empty string or null represents a regular member with no special roles.
    /// </summary>
    public static readonly string[] ValidRoles =
    {
        "Teacher",
        "SafetyTeam",
        "Administrator"
    };
}
