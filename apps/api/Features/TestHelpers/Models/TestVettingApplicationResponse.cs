namespace WitchCityRope.Api.Features.TestHelpers.Models;

/// <summary>
/// Response model for test vetting application creation
/// Contains application ID for test cleanup
/// </summary>
public class TestVettingApplicationResponse
{
    /// <summary>
    /// Created application's ID (GUID)
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// User ID of the applicant
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// Workflow status
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// When application was submitted
    /// </summary>
    public required DateTime SubmittedAt { get; set; }
}
