namespace WitchCityRope.Api.Features.Volunteers.Models;

/// <summary>
/// DTO for member assigned to a volunteer position
/// </summary>
public class VolunteerAssignmentDto
{
    public Guid SignupId { get; set; }
    public Guid UserId { get; set; }
    public Guid VolunteerPositionId { get; set; }
    public string SceneName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FetLifeName { get; set; }
    public string? DiscordName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SignedUpAt { get; set; }
    public bool HasCheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
}

/// <summary>
/// Request to assign a member to a volunteer position
/// </summary>
public class AssignVolunteerRequest
{
    public Guid UserId { get; set; }
}

/// <summary>
/// DTO for user search results
/// </summary>
public class UserSearchResultDto
{
    public Guid UserId { get; set; }
    public string SceneName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DiscordName { get; set; }
    public string? RealName { get; set; }
}
