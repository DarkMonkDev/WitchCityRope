namespace WitchCityRope.Api.Features.TestHelpers.Models;

/// <summary>
/// Response model for test user creation
/// Contains user ID for test cleanup
/// </summary>
public class TestUserResponse
{
    /// <summary>
    /// Created user's ID (GUID)
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// User's scene name
    /// </summary>
    public required string SceneName { get; set; }

    /// <summary>
    /// User's role
    /// </summary>
    public required string Role { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
