using System.Security.Claims;
using WitchCityRope.Api.Features.Authentication.Models;
using WitchCityRope.Api.Features.Shared.Extensions;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Services;

namespace WitchCityRope.Api.Features.Authentication.Endpoints;

/// <summary>
/// Protected endpoints used by the React frontend's "API connection test" flow
/// (apps/web/src/pages/ApiConnectionTest.tsx) to verify that JWT auth is working end-to-end.
///
/// Migrated from <c>Controllers/ProtectedController.cs</c> on 2026-04-21 as part of the
/// Error Handling Standard adoption (TD-029): MVC controllers couldn't go through the
/// <see cref="ResultExtensions.ToProblem{T}"/> helper, and ProtectedController was the last
/// live controller in <c>apps/api/Controllers/</c>. Routes are preserved exactly
/// (<c>/api/protected/welcome</c>, <c>/api/protected/profile</c>) so frontend code, tests,
/// and the OpenAPI contract require no changes.
///
/// Error handling: any path that previously called <c>Problem(...)</c> now produces a
/// <see cref="Result{T}"/> failure and routes through <c>ToProblem(title)</c>. Unhandled
/// exceptions bubble to <c>GlobalExceptionHandler</c> for a uniform 500 ProblemDetails.
/// </summary>
public static class ProtectedEndpoints
{
    public static void MapProtectedEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/protected")
            .WithTags("Protected")
            .RequireAuthorization(); // Both endpoints require a valid JWT

        group.MapGet("/welcome", GetWelcome)
            .WithName("GetProtectedWelcome")
            .WithSummary("Get personalized welcome message for authenticated users")
            .WithDescription(
                "Returns a personalized welcome message plus the resolved user profile and a " +
                "debug view of the JWT claims. Used by the React API-connection test page.")
            .Produces<ProtectedWelcomeResponse>(200)
            .ProducesProblem(401);

        group.MapGet("/profile", GetProfile)
            .WithName("GetProtectedProfile")
            .WithSummary("Get authenticated user's profile")
            .Produces<AuthUserResponse>(200)
            .ProducesProblem(401)
            .ProducesProblem(404);
    }

    /// <summary>
    /// GET /api/protected/welcome — returns a personalized welcome + JWT claims debug view.
    /// </summary>
    private static async Task<IResult> GetWelcome(
        ClaimsPrincipal user,
        IAuthService authService,
        ILogger<Program> logger)
    {
        // JWT uses "sub" claim for user ID, with NameIdentifier as fallback for older tokens.
        var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = user.FindFirst("email")?.Value ?? user.FindFirst(ClaimTypes.Email)?.Value;
        var sceneName = user.FindFirst("scene_name")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("Protected endpoint accessed without valid user ID claim");
            return Result<ProtectedWelcomeResponse>
                .Unauthorized("User ID not found in token claims.")
                .ToProblem("Invalid Token");
        }

        var dbUser = await authService.GetUserByIdAsync(userId);
        if (dbUser is null)
        {
            logger.LogWarning("Protected endpoint accessed with token for non-existent user: {UserId}", userId);
            // Token signature is valid but the user behind the sub claim no longer exists in
            // the DB — semantically Unauthorized (not NotFound) because the token itself is
            // no longer valid for this account, matching the original controller behavior.
            return Result<ProtectedWelcomeResponse>
                .Unauthorized("Token is valid but user no longer exists.")
                .ToProblem("User Not Found");
        }

        var response = new ProtectedWelcomeResponse
        {
            Message = $"Welcome back, {dbUser.SceneName}! You're successfully authenticated.",
            User = dbUser,
            ServerTime = DateTime.UtcNow,
            TokenClaims = new TokenClaims
            {
                UserId = userId,
                Email = email ?? string.Empty,
                SceneName = sceneName ?? string.Empty
            }
        };

        logger.LogDebug("Protected welcome endpoint accessed by user: {UserId} ({SceneName})",
            userId, dbUser.SceneName);

        return Results.Ok(response);
    }

    /// <summary>
    /// GET /api/protected/profile — returns the current user's full profile.
    /// </summary>
    private static async Task<IResult> GetProfile(
        ClaimsPrincipal user,
        IAuthService authService)
    {
        var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Result<AuthUserResponse>
                .Unauthorized("User ID not found in token claims.")
                .ToProblem("Invalid Token");
        }

        var dbUser = await authService.GetUserByIdAsync(userId);
        if (dbUser is null)
        {
            // Match the original controller: 404 here (the endpoint is /profile, lookup-style).
            return Result<AuthUserResponse>
                .NotFound("User no longer exists.")
                .ToProblem("User Not Found");
        }

        return Results.Ok(dbUser);
    }
}
