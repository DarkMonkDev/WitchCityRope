using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.EmailTemplates.Services;

public class EventRecipientService : IEventRecipientService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EventRecipientService> _logger;

    public EventRecipientService(ApplicationDbContext context, ILogger<EventRecipientService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<RecipientInfo>> GetRecipientsAsync(
        EventRecipientGroup group, Guid sessionId, CancellationToken ct)
    {
        return group switch
        {
            EventRecipientGroup.RSVPTicketHolders => await GetRsvpTicketHoldersAsync(sessionId, ct),
            EventRecipientGroup.SessionAttendees => await GetSessionAttendeesAsync(sessionId, ct),
            EventRecipientGroup.SessionVolunteers => await GetSessionVolunteersAsync(sessionId, ct),
            EventRecipientGroup.Teachers => await GetTeachersAsync(sessionId, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(group), group, "Unknown recipient group")
        };
    }

    private async Task<List<RecipientInfo>> GetRsvpTicketHoldersAsync(Guid sessionId, CancellationToken ct)
    {
        // Get the event ID from the session
        var session = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.Id == sessionId)
            .Select(s => new { s.EventId })
            .FirstOrDefaultAsync(ct);

        if (session == null)
        {
            _logger.LogWarning("Session {SessionId} not found for RSVPTicketHolders lookup", sessionId);
            return [];
        }

        // Query EventAttendances for active RSVP/Ticket holders, deduplicate by UserId
        var recipients = await _context.EventAttendances
            .AsNoTracking()
            .Include(ea => ea.User)
            .Where(ea => ea.EventId == session.EventId
                && ea.Status == AttendanceStatus.Active
                && (ea.AttendanceType == AttendanceType.RSVP || ea.AttendanceType == AttendanceType.Ticket))
            .Select(ea => new { ea.UserId, ea.User!.Email, ea.User.SceneName })
            .Distinct()
            .ToListAsync(ct);

        return recipients
            .Where(r => !string.IsNullOrEmpty(r.Email))
            .GroupBy(r => r.UserId)
            .Select(g => g.First())
            .Select(r => new RecipientInfo(r.UserId, r.Email!, !string.IsNullOrEmpty(r.SceneName) ? r.SceneName : r.Email!))
            .ToList();
    }

    private async Task<List<RecipientInfo>> GetSessionAttendeesAsync(Guid sessionId, CancellationToken ct)
    {
        // Users who have checked in for this specific session
        var recipients = await _context.CheckIns
            .AsNoTracking()
            .Include(ci => ci.EventAttendee)
                .ThenInclude(ea => ea.User)
            .Where(ci => ci.SessionId == sessionId)
            .Select(ci => new { ci.EventAttendee.UserId, ci.EventAttendee.User.Email, ci.EventAttendee.User.SceneName })
            .Distinct()
            .ToListAsync(ct);

        return recipients
            .Where(r => !string.IsNullOrEmpty(r.Email))
            .Select(r => new RecipientInfo(r.UserId, r.Email!, !string.IsNullOrEmpty(r.SceneName) ? r.SceneName : r.Email!))
            .ToList();
    }

    private async Task<List<RecipientInfo>> GetSessionVolunteersAsync(Guid sessionId, CancellationToken ct)
    {
        // Look up the session's EventId so we can also find event-wide volunteer positions
        var session = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.Id == sessionId)
            .Select(s => new { s.EventId })
            .FirstOrDefaultAsync(ct);

        if (session == null)
        {
            _logger.LogWarning("Session {SessionId} not found for SessionVolunteers lookup", sessionId);
            return [];
        }

        // Confirmed volunteers for:
        // 1. Positions linked to THIS specific session (SessionId == sessionId)
        // 2. Event-wide positions (SessionId == null) for the same event
        //    - Event-wide volunteers get emails for EVERY session's trigger time,
        //      since they are volunteering for the whole event, not a specific session.
        //    - Session-specific volunteers only get emails for their session's trigger time.
        var recipients = await _context.VolunteerSignups
            .AsNoTracking()
            .Include(vs => vs.VolunteerPosition)
            .Include(vs => vs.User)
            .Where(vs => vs.Status == VolunteerSignupStatus.Confirmed
                && (vs.VolunteerPosition!.SessionId == sessionId
                    || (vs.VolunteerPosition!.SessionId == null
                        && vs.VolunteerPosition!.EventId == session.EventId)))
            .Select(vs => new
            {
                vs.UserId,
                vs.User!.Email,
                vs.User.SceneName,
                VolunteerRole = vs.VolunteerPosition!.Title,
                ShiftStart = vs.VolunteerPosition.StartTime,
                ShiftEnd = vs.VolunteerPosition.EndTime,
                // Track whether this signup is for a session-specific position.
                // Used during deduplication to prefer session-specific roles over event-wide ones,
                // since the email is triggered for a specific session and the session-specific
                // role is more relevant to the recipient.
                IsSessionSpecific = vs.VolunteerPosition.SessionId != null
            })
            .ToListAsync(ct);

        // Deduplicate by UserId — a volunteer with both an event-wide and session-specific
        // position should only receive one email per session trigger.
        // Prefer session-specific positions because the email references a specific session,
        // so the session-specific role/shift is more relevant than the event-wide one.
        return recipients
            .Where(r => !string.IsNullOrEmpty(r.Email))
            .GroupBy(r => r.UserId)
            .Select(g => g.OrderByDescending(r => r.IsSessionSpecific).First())
            .Select(r => new RecipientInfo(
                r.UserId, r.Email!, !string.IsNullOrEmpty(r.SceneName) ? r.SceneName : r.Email!,
                r.VolunteerRole, r.ShiftStart, r.ShiftEnd))
            .ToList();
    }

    private async Task<List<RecipientInfo>> GetTeachersAsync(Guid sessionId, CancellationToken ct)
    {
        // Get the event's organizers (teachers) via the session's event
        var session = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.Id == sessionId)
            .Select(s => new { s.EventId })
            .FirstOrDefaultAsync(ct);

        if (session == null)
        {
            _logger.LogWarning("Session {SessionId} not found for Teachers lookup", sessionId);
            return [];
        }

        var eventWithOrganizers = await _context.Events
            .AsNoTracking()
            .Include(e => e.Organizers)
            .FirstOrDefaultAsync(e => e.Id == session.EventId, ct);

        if (eventWithOrganizers == null)
        {
            _logger.LogWarning("Event {EventId} not found for Teachers lookup", session.EventId);
            return [];
        }

        return eventWithOrganizers.Organizers
            .Where(o => !string.IsNullOrEmpty(o.Email))
            .Select(o => new RecipientInfo(o.Id, o.Email!, !string.IsNullOrEmpty(o.SceneName) ? o.SceneName : o.Email!))
            .ToList();
    }
}
