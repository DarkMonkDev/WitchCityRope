namespace WitchCityRope.Api.Features.TestHelpers.Models;

/// <summary>
/// Request model for verifying a user's email in test environments
/// CRITICAL: Only available in Development/Test environments
/// </summary>
public class VerifyUserEmailRequest
{
    /// <summary>
    /// Email address of the user to verify
    /// </summary>
    public required string Email { get; set; }
}
