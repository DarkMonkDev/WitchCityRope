using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WitchCityRope.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthCheckController : ControllerBase
{
    private readonly ILogger<HealthCheckController> _logger;

    public HealthCheckController(ILogger<HealthCheckController> logger)
    {
        _logger = logger;
    }

    [HttpGet("endpoints")]
    [AllowAnonymous]
    public IActionResult GetEndpoints()
    {
        var endpoints = new
        {
            message = "Available API endpoints",
            endpoints = new[]
            {
                new { method = "GET", path = "/api/events", description = "Get all events", requiresAuth = false },
                new { method = "POST", path = "/api/events", description = "Create event", requiresAuth = true },
                new { method = "GET", path = "/api/events/{id}", description = "Get specific event", requiresAuth = false },
                new { method = "POST", path = "/api/events/{id}/rsvp", description = "RSVP to event", requiresAuth = true },
                new { method = "GET", path = "/api/events/upcoming", description = "Get upcoming events", requiresAuth = false },
                new { method = "GET", path = "/api/user/profile", description = "Get user profile", requiresAuth = true },
                new { method = "GET", path = "/api/users/me/rsvps", description = "Get user's RSVPs", requiresAuth = true },
                new { method = "GET", path = "/api/healthcheck/endpoints", description = "Get this list of endpoints", requiresAuth = false }
            }
        };

        return Ok(endpoints);
    }
}