using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WitchCityRope.Api.Features.Dashboard.Models;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Infrastructure.Data;
using VettingStatus = WitchCityRope.Core.Entities.VettingStatus;

namespace WitchCityRope.Api.Features.Dashboard
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly WitchCityRopeDbContext _context;
        private readonly ILogger<DashboardController> _logger;
        
        public DashboardController(
            WitchCityRopeDbContext context,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        [HttpGet("{userId}")]
        [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDashboard(Guid userId)
        {
            // Verify user can access this dashboard
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId != userId.ToString() && !User.IsInRole("Administrator"))
            {
                return Forbid();
            }
            
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
                
            if (user == null)
            {
                return NotFound();
            }
            
            // Get the user's latest vetting status
            var vettingStatus = await _context.VettingApplications
                .Where(v => v.ApplicantId == userId)
                .OrderByDescending(v => v.SubmittedAt)
                .Select(v => v.Status)
                .FirstOrDefaultAsync();
                
            var dashboard = new DashboardDto
            {
                SceneName = user.SceneName.Value,
                Role = user.Role,
                VettingStatus = vettingStatus
            };
            
            return Ok(dashboard);
        }
        
        [HttpGet("users/{userId}/upcoming-events")]
        [ProducesResponseType(typeof(List<Models.EventDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUpcomingEvents(Guid userId, [FromQuery] int count = 3)
        {
            // Verify user can access this data
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId != userId.ToString() && !User.IsInRole("Administrator"))
            {
                return Forbid();
            }
            
            var upcomingEvents = await _context.Registrations
                .AsNoTracking()
                .Include(r => r.Event)
                .Where(r => r.UserId == userId && 
                           r.Event.StartDate > DateTime.UtcNow &&
                           r.Status != RegistrationStatus.Cancelled)
                .OrderBy(r => r.Event.StartDate)
                .Take(count)
                .Select(r => new Models.EventDto
                {
                    Id = r.Event.Id,
                    Title = r.Event.Title,
                    StartDate = r.Event.StartDate,
                    EndDate = r.Event.EndDate,
                    Location = r.Event.Location,
                    EventType = r.Event.EventType,
                    InstructorName = "TBD", // TODO: Load organizer data
                    RegistrationStatus = MapRegistrationStatus(r.Status),
                    TicketId = r.Id
                })
                .ToListAsync();
            
            return Ok(upcomingEvents);
        }
        
        [HttpGet("users/{userId}/stats")]
        [ProducesResponseType(typeof(MembershipStatsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMembershipStats(Guid userId)
        {
            // Verify user can access this data
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId != userId.ToString() && !User.IsInRole("Administrator"))
            {
                return Forbid();
            }
            
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
                
            if (user == null)
            {
                return NotFound();
            }
            
            // Count attended events (past events with Attended status)
            var eventsAttended = await _context.Registrations
                .CountAsync(r => r.UserId == userId && 
                                r.Event.EndDate < DateTime.UtcNow &&
                                r.Status == RegistrationStatus.Confirmed);
            
            // Calculate months as member
            var monthsAsMember = (int)Math.Ceiling((DateTime.UtcNow - user.CreatedAt).TotalDays / 30);
            
            // Count consecutive events (simplified - count events in last 6 months)
            var consecutiveEvents = await _context.Registrations
                .CountAsync(r => r.UserId == userId && 
                                r.Event.StartDate > DateTime.UtcNow.AddMonths(-6) &&
                                r.Status == RegistrationStatus.Confirmed);
            
            // Get vetting status info
            var vettingApp = await _context.VettingApplications
                .Where(v => v.ApplicantId == userId)
                .OrderByDescending(v => v.SubmittedAt)
                .FirstOrDefaultAsync();
            
            var stats = new MembershipStatsDto
            {
                IsVerified = user.IsVetted,
                EventsAttended = eventsAttended,
                MonthsAsMember = monthsAsMember,
                ConsecutiveEvents = consecutiveEvents,
                JoinDate = user.CreatedAt,
                VettingStatus = vettingApp?.Status ?? VettingStatus.Submitted,
                NextInterviewDate = null // TODO: Add interview scheduling
            };
            
            return Ok(stats);
        }
        
        private string MapRegistrationStatus(RegistrationStatus status)
        {
            return status switch
            {
                RegistrationStatus.Confirmed => "Registered",
                RegistrationStatus.Waitlisted => "Waitlisted",
                RegistrationStatus.Cancelled => "Cancelled",
                // RegistrationStatus.Attended => "Attended", // Not available in current enum
                _ => "Registered"
            };
        }
    }
    
    // DTOs
    namespace Models
    {
        public class DashboardDto
        {
            public string SceneName { get; set; } = string.Empty;
            public UserRole Role { get; set; }
            public VettingStatus VettingStatus { get; set; }
        }
        
        public class EventDto
        {
            public Guid Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Location { get; set; } = string.Empty;
            public EventType EventType { get; set; }
            public string InstructorName { get; set; } = string.Empty;
            public string RegistrationStatus { get; set; } = string.Empty;
            public Guid TicketId { get; set; }
        }
        
        public class MembershipStatsDto
        {
            public bool IsVerified { get; set; }
            public int EventsAttended { get; set; }
            public int MonthsAsMember { get; set; }
            public int ConsecutiveEvents { get; set; }
            public DateTime JoinDate { get; set; }
            public VettingStatus VettingStatus { get; set; }
            public DateTime? NextInterviewDate { get; set; }
        }
    }
}