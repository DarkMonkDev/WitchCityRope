using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Web.Services;

namespace WitchCityRope.Web.Controllers;

[ApiController]
[Route("api/user")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly ApiClient _apiClient;
    private readonly ILogger<UserController> _logger;

    public UserController(ApiClient apiClient, ILogger<UserController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetUserProfile()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var profile = await _apiClient.GetAsync<UserDto>($"users/{userId}");
            if (profile == null)
            {
                return NotFound(new { error = "User profile not found" });
            }

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user profile");
            return StatusCode(500, new { error = "Failed to fetch user profile" });
        }
    }
}

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ApiClient _apiClient;
    private readonly ILogger<UsersController> _logger;

    public UsersController(ApiClient apiClient, ILogger<UsersController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    [HttpGet("me/rsvps")]
    public async Task<IActionResult> GetMyRsvps()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var rsvps = await _apiClient.GetAsync<List<RsvpDto>>($"users/{userId}/rsvps");
            return Ok(new { rsvps = rsvps ?? new List<RsvpDto>() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user RSVPs");
            return StatusCode(500, new { error = "Failed to fetch RSVPs" });
        }
    }
}