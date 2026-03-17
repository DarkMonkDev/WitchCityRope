using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Users.Extensions;

/// <summary>
/// Extension methods for querying users by role in the database.
/// Handles the comma-separated role storage format in ApplicationUser.Role.
/// Use these for EF Core queries instead of manual string matching.
/// </summary>
public static class RoleQueryExtensions
{
    /// <summary>
    /// Filters users who have a specific role in their comma-separated Role field.
    /// Handles all positions: exact match, start, end, and middle of CSV string.
    /// Example: For role "SafetyTeam", matches "SafetyTeam", "SafetyTeam,Teacher",
    /// "Administrator,SafetyTeam", and "Administrator,SafetyTeam,Teacher".
    /// </summary>
    public static IQueryable<ApplicationUser> WhereHasRole(this IQueryable<ApplicationUser> query, string role)
    {
        return query.Where(u => u.Role != null && (
            u.Role == role ||
            u.Role.StartsWith(role + ",") ||
            u.Role.EndsWith("," + role) ||
            u.Role.Contains("," + role + ",")));
    }
}
