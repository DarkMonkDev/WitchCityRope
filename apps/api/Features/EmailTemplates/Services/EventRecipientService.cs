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
            .Select(ea => new { ea.UserId, ea.User!.Email, ea.User.UserName })
            .Distinct()
            .ToListAsync(ct);

        return recipients
            .Where(r => !string.IsNullOrEmpty(r.Email))
            .GroupBy(r => r.UserId)
            .Select(g => g.First())
            .Select(r => new RecipientInfo(r.UserId, r.Email!, r.UserName ?? r.Email!))
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
            .Select(ci => new { ci.EventAttendee.UserId, ci.EventAttendee.User.Email, ci.EventAttendee.User.UserName })
            .Distinct()
            .ToListAsync(ct);

        return recipients
            .Where(r => !string.IsNullOrEmpty(r.Email))
            .Select(r => new RecipientInfo(r.UserId, r.Email!, r.UserName ?? r.Email!))
            .ToList();
    }

    private async Task<List<RecipientInfo>> GetSessionVolunteersAsync(Guid sessionId, CancellationToken ct)
    {
        // Confirmed volunteers for positions linked to this session
        var recipients = await _context.VolunteerSignups
            .AsNoTracking()
            .Include(vs => vs.VolunteerPosition)
            .Include(vs => vs.User)
            .Where(vs => vs.VolunteerPosition!.SessionId == sessionId
                && vs.Status == VolunteerSignupStatus.Confirmed)
            .Select(vs => new { vs.UserId, vs.User!.Email, vs.User.UserName })
            .Distinct()
            .ToListAsync(ct);

        return recipients
            .Where(r => !string.IsNullOrEmpty(r.Email))
            .Select(r => new RecipientInfo(r.UserId, r.Email!, r.UserName ?? r.Email!))
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
            .Select(o => new RecipientInfo(o.Id, o.Email!, o.UserName ?? o.Email!))
            .ToList();
    }
}
