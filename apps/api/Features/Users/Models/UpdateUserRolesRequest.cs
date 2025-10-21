using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Users.Models;

/// <summary>
/// Request model for updating user roles in admin user management
/// </summary>
public class UpdateUserRolesRequest
{
    /// <summary>
    /// List of roles to assign to the user
    /// Valid values: "Teacher", "SafetyTeam", "Administrator"
    /// Empty list = Regular member (no special roles)
    /// </summary>
    [Required]
    public List<string> Roles { get; set; } = new();
}
