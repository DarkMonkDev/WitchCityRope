using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WitchCityRope.Api.Features.Authentication.Models;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Services;

namespace WitchCityRope.Api.Controllers;

/// <summary>
/// Protected controller for testing JWT authentication
/// For authentication vertical slice test
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires JWT Bearer token authentication
public class ProtectedController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<ProtectedController> _logger;

    public ProtectedController(IAuthService authService, ILogger<ProtectedController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Get personalized welcome message for authenticated users
    /// Tests JWT token authentication and claims extraction
    /// </summary>
    /// <returns>Welcome message with user information</returns>
    [HttpGet("welcome")]
    [ProducesResponseType(typeof(ProtectedWelcomeResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetWelcome()
    {
        try
        {
            // Extract user claims from JWT token
            // JWT uses "sub" claim for user ID
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
            var sceneName = User.FindFirst("scene_name")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Protected endpoint accessed without valid user ID claim");
                return Problem(
                    title: "Invalid Token",
                    detail: "User ID not found in token claims",
                    statusCode: 401);
            }

            // Get fresh user data from database
            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Protected endpoint accessed with token for non-existent user: {UserId}", userId);
                return Problem(
                    title: "User Not Found",
                    detail: "Token is valid but user no longer exists",
                    statusCode: 401);
            }

            var response = new ProtectedWelcomeResponse
            {
                Message = $"Welcome back, {user.SceneName}! You're successfully authenticated.",
                User = user,
                ServerTime = DateTime.UtcNow,
                TokenClaims = new TokenClaims
                {
                    UserId = userId,
                    Email = email ?? string.Empty,
                    SceneName = sceneName ?? string.Empty
                }
            };

            _logger.LogDebug("Protected welcome endpoint accessed by user: {UserId} ({SceneName})", userId, user.SceneName);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing protected welcome request");
            return Problem(
                title: "Internal Server Error",
                detail: "An error occurred processing your request",
                statusCode: 500);
        }
    }

    /// <summary>
    /// Get user profile information (additional protected endpoint for testing)
    /// </summary>
    /// <returns>Current user profile</returns>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(AuthUserResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Problem(
                    title: "Invalid Token",
                    detail: "User ID not found in token claims",
                    statusCode: 401);
            }

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return Problem(
                    title: "User Not Found",
                    detail: "Token is valid but user no longer exists",
                    statusCode: 404);
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return Problem(
                title: "Internal Server Error",
                detail: "An error occurred retrieving your profile",
                statusCode: 500);
        }
    }
}

/// <summary>
/// Protected welcome response model
/// </summary>
public class ProtectedWelcomeResponse
{
    public string Message { get; set; } = string.Empty;
    public AuthUserResponse User { get; set; } = new();
    public DateTime ServerTime { get; set; }
    public TokenClaims TokenClaims { get; set; } = new();
}

/// <summary>
/// Token claims extracted from JWT for debugging
/// </summary>
public class TokenClaims
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SceneName { get; set; } = string.Empty;
}
