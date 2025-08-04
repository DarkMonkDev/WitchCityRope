using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Infrastructure.Data;
using System.Text.RegularExpressions;

namespace WitchCityRope.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    private readonly WitchCityRopeIdentityDbContext _dbContext;
    private readonly ILogger<DebugController> _logger;

    public DebugController(WitchCityRopeIdentityDbContext dbContext, ILogger<DebugController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet("check-event/{eventId}")]
    public async Task<IActionResult> CheckEvent(Guid eventId)
    {
        try
        {
            // Get connection string (sanitized)
            var connectionString = _dbContext.Database.GetConnectionString() ?? "No connection string";
            var sanitizedConnectionString = Regex.Replace(connectionString, @"Password=([^;]+)", "Password=***");
            
            // Check if we can connect
            var canConnect = await _dbContext.Database.CanConnectAsync();
            
            // Count total events
            var totalEvents = await _dbContext.Events.CountAsync();
            
            // Check if the specific event exists
            var eventExists = await _dbContext.Events.AnyAsync(e => e.Id == eventId);
            
            // Get the event details if it exists
            object? eventDetails = null;
            if (eventExists)
            {
                eventDetails = await _dbContext.Events
                    .Where(e => e.Id == eventId)
                    .Select(e => new
                    {
                        e.Id,
                        e.Title,
                        e.IsPublished,
                        e.StartDate,
                        e.CreatedAt,
                        TicketCount = e.Tickets.Count(),
                        RsvpCount = e.Rsvps.Count()
                    })
                    .FirstOrDefaultAsync();
            }
            
            // Get first 5 events as samples
            var sampleEvents = await _dbContext.Events
                .OrderBy(e => e.CreatedAt)
                .Take(5)
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.IsPublished,
                    e.StartDate
                })
                .ToListAsync();
            
            return Ok(new
            {
                ConnectionString = sanitizedConnectionString,
                CanConnect = canConnect,
                TotalEvents = totalEvents,
                RequestedEventId = eventId,
                EventExists = eventExists,
                EventDetails = eventDetails,
                SampleEvents = sampleEvents,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking event {EventId}", eventId);
            return StatusCode(500, new
            {
                Error = ex.Message,
                Type = ex.GetType().Name,
                RequestedEventId = eventId
            });
        }
    }
    
    [HttpGet("database-info")]
    public async Task<IActionResult> GetDatabaseInfo()
    {
        try
        {
            // Execute raw SQL to get database name
            var databaseName = await _dbContext.Database
                .SqlQueryRaw<string>("SELECT current_database()")
                .FirstOrDefaultAsync();
                
            // Get schema version
            var schemaVersion = await _dbContext.Database
                .SqlQueryRaw<string>("SELECT version()")
                .FirstOrDefaultAsync();
            
            return Ok(new
            {
                DatabaseName = databaseName,
                PostgresVersion = schemaVersion,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Error = ex.Message,
                Type = ex.GetType().Name
            });
        }
    }
}