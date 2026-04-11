using WitchCityRope.Api.Features.Vetting.Entities;

namespace WitchCityRope.Api.Features.Users.Models;

/// <summary>
/// Request model for admin user updates
/// Follows the simplified vertical slice architecture pattern
/// </summary>
public class UpdateUserRequest
{
    public string? SceneName { get; set; }
    public string? Role { get; set; }
    public string? Pronouns { get; set; }
    public bool? IsActive { get; set; }
    public bool? EmailConfirmed { get; set; }

    /// <summary>
    /// Phase 3b-1: Type changed from int? to VettingStatus? enum.
    /// Inbound JSON is parsed via JsonStringEnumConverter so admin clients
    /// can send "UnderReview", "Approved", etc. as strings. Previously
    /// accepted raw integers only.
    /// </summary>
    public VettingStatus? VettingStatus { get; set; }
}