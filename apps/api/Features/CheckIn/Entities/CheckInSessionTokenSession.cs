namespace WitchCityRope.Api.Features.CheckIn.Entities;

/// <summary>
/// Join table linking check-in tokens to multiple sessions
/// Allows one token to grant access to multiple sessions
/// Pattern: Many-to-many relationship between tokens and sessions
/// </summary>
public class CheckInSessionTokenSession
{
    /// <summary>
    /// Foreign key to the parent token
    /// </summary>
    public Guid TokenId { get; set; }

    /// <summary>
    /// Foreign key to the session
    /// </summary>
    public Guid SessionId { get; set; }

    // Navigation properties
    public CheckInSessionToken? Token { get; set; }
    public WitchCityRope.Api.Models.Session? Session { get; set; }
}
