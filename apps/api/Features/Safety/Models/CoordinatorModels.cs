namespace WitchCityRope.Api.Features.Safety.Models;

/// <summary>
/// Request to assign coordinator to incident
/// </summary>
public class AssignCoordinatorRequest
{
    /// <summary>
    /// Coordinator user ID (NULL to unassign)
    /// </summary>
    public Guid? CoordinatorId { get; set; }
}

/// <summary>
/// User information for coordinator assignment dropdown
/// </summary>
public class UserCoordinatorDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User scene name (preferred display name)
    /// </summary>
    public string SceneName { get; set; } = string.Empty;

    /// <summary>
    /// Real name
    /// </summary>
    public string RealName { get; set; } = string.Empty;

    /// <summary>
    /// User role
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Number of active incidents assigned to this user
    /// </summary>
    public int ActiveIncidentCount { get; set; }
}
