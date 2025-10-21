namespace WitchCityRope.Api.Features.Metadata.Models;

/// <summary>
/// Response DTO containing valid user roles in the system.
/// Used by frontend to populate role selection dropdowns and validate role assignments.
/// </summary>
public class ValidRolesResponse
{
    /// <summary>
    /// List of valid role names.
    /// Empty string or null role = Regular member with no special privileges.
    /// </summary>
    public List<string> Roles { get; set; } = new();
}
