namespace WitchCityRope.Api.Features.Authentication.Models;

/// <summary>
/// Response payload for the /api/protected/welcome endpoint.
/// Returns a personalized message plus the authenticated user's profile and a debug view of
/// the JWT claims that were resolved on the request — used by the React frontend's
/// "API connection test" page (apps/web/src/pages/ApiConnectionTest.tsx) to verify the
/// auth pipeline end-to-end.
/// </summary>
public class ProtectedWelcomeResponse
{
    public string Message { get; set; } = string.Empty;
    public AuthUserResponse User { get; set; } = new();
    public DateTime ServerTime { get; set; }
    public TokenClaims TokenClaims { get; set; } = new();
}

/// <summary>
/// Token claims extracted from JWT for debugging.
/// Surfaced via /api/protected/welcome so the test page can show "we read these claims from
/// your token" — never used for authorization decisions (those go through the Authorize
/// attribute and IAuthService).
/// </summary>
public class TokenClaims
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SceneName { get; set; } = string.Empty;
}
