namespace WitchCityRope.Api.Features.Users.Constants;

/// <summary>
/// Defines the valid user roles in the WitchCityRope system.
/// This is the single source of truth for all role validation.
/// </summary>
/// <remarks>
/// All role names are derived from the UserRole enum to ensure type safety.
/// Use the extension methods to convert between enum and string representations.
/// </remarks>
public static class UserRoleConstants
{
    /// <summary>
    /// Valid user roles in the system as strings for ASP.NET Core Identity.
    /// Derived from UserRole enum, excluding Member (which is the default state).
    /// </summary>
    public static readonly string[] ValidRoles = Enum.GetNames<UserRole>()
        .Where(r => r != nameof(UserRole.Member)) // Member is default, not assigned as role
        .ToArray();

    /// <summary>
    /// Converts a UserRole enum value to its string representation for ASP.NET Identity.
    /// </summary>
    /// <param name="role">The role enum value</param>
    /// <returns>String representation of the role</returns>
    public static string ToRoleString(this UserRole role) => role.ToString();

    /// <summary>
    /// Attempts to parse a string into a UserRole enum value.
    /// </summary>
    /// <param name="roleString">The role string to parse</param>
    /// <param name="role">The parsed UserRole value if successful</param>
    /// <returns>True if parsing succeeded, false otherwise</returns>
    public static bool TryParseRole(string roleString, out UserRole role)
    {
        return Enum.TryParse(roleString, ignoreCase: true, out role);
    }

    /// <summary>
    /// Gets all valid role enum values (excluding Member).
    /// </summary>
    public static IEnumerable<UserRole> GetAllRoles()
    {
        return Enum.GetValues<UserRole>()
            .Where(r => r != UserRole.Member);
    }

    /// <summary>
    /// Splits a comma-separated role string into individual role strings.
    /// Handles null/empty input, trims whitespace, and filters empty entries.
    /// This is the single source of truth for parsing the Role CSV field.
    /// </summary>
    public static string[] ParseRoles(string? roleString)
    {
        if (string.IsNullOrWhiteSpace(roleString))
            return Array.Empty<string>();
        return roleString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// Checks if a comma-separated role string contains a specific role.
    /// Case-insensitive comparison. Use this for in-memory checks on ApplicationUser.Role.
    /// For database queries, use RoleQueryExtensions.WhereHasRole instead.
    /// </summary>
    public static bool HasRole(string? roleString, string targetRole)
    {
        return ParseRoles(roleString).Any(r => string.Equals(r, targetRole, StringComparison.OrdinalIgnoreCase));
    }
}
