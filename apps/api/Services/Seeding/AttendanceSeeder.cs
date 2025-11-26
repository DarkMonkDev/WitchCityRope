using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.CheckIn.Entities;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Services.Seeding;

/// <summary>
/// Handles seeding of RSVP-type event attendance records.
/// Extracted from SeedDataService.cs for better maintainability.
///
/// ARCHITECTURE NOTE (Phase 2 Refactor - 2025-11-26):
/// - This seeder creates ONLY RSVP-type EventAttendance records for social events
/// - ALL TicketPurchase creation is now handled by TicketPurchaseSeeder
/// - ALL Ticket-type EventAttendance creation is handled by TicketPurchaseSeeder
/// - This ensures Single Responsibility Principle and eliminates duplicate ticket creation
/// </summary>
public class AttendanceSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly EventSeeder _eventSeeder;
    private readonly ILogger<AttendanceSeeder> _logger;

    public AttendanceSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        EventSeeder eventSeeder,
        ILogger<AttendanceSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _eventSeeder = eventSeeder;
        _logger = logger;
    }

    /// <summary>
    /// Seeds RSVP-type event attendance records for social events.
    /// Idempotent operation - skips if attendances already exist.
    ///
    /// ARCHITECTURE NOTE (Phase 2 Refactor):
    /// - Creates ONLY RSVP-type EventAttendance records for social events
    /// - Does NOT create TicketPurchase records (handled by TicketPurchaseSeeder)
    /// - Does NOT create Ticket-type EventAttendance records (handled by TicketPurchaseSeeder)
    /// - Does NOT handle class events (all class event tickets + attendances handled by TicketPurchaseSeeder)
    ///
    /// Social event attendees can have:
    /// - RSVP attendance (created here - free, no payment)
    /// - Optional Ticket attendance (created by TicketPurchaseSeeder if they make a donation)
    /// </summary>
    public async Task SeedEventParticipationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting RSVP attendance creation for social events");

        // Get only social events (excluding historical ones that are seeded separately)
        var socialEvents = await _context.Events
            .Where(e => e.EventType == EventType.Social &&
                       e.Title != "Monthly Rope Practice Night" &&
                       e.Title != "New Member Welcome Mixer")
            .ToListAsync(cancellationToken);

        // Get all vetted users (only vetted members can attend social events)
        var vettedUsers = await _userManager.Users
            .Where(u => u.VettingStatus >= 3)
            .ToListAsync(cancellationToken);

        if (vettedUsers.Count == 0)
        {
            _logger.LogWarning("No vetted users found for social event RSVPs");
            return;
        }

        var attendancesToAdd = new List<EventAttendance>();
        var attendeesToAdd = new List<EventAttendee>();
        var eventsProcessed = 0;

        foreach (var eventItem in socialEvents)
        {
            // Check if THIS specific event already has RSVP attendances (idempotent per-event check)
            var hasAttendances = await _context.EventAttendances
                .AnyAsync(ea => ea.EventId == eventItem.Id && ea.AttendanceType == AttendanceType.RSVP, cancellationToken);

            if (hasAttendances)
            {
                _logger.LogDebug("Event {EventTitle} already has RSVP attendances, skipping", eventItem.Title);
                continue; // Skip this event, but continue processing other events
            }

            eventsProcessed++;

            // Determine RSVP count based on event title
            var rsvpCount = eventItem.Title.Contains("Community Rope Jam") ? 5 :
                           eventItem.Title.Contains("New Members Meetup") ? 8 :
                           eventItem.Title.Contains("Rope Social & Discussion") ? 6 : 3;

            // Create RSVP attendances for vetted users
            for (int i = 0; i < Math.Min(rsvpCount, vettedUsers.Count); i++)
            {
                var user = vettedUsers[i];
                var createdAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 10));
                var notes = SeedingHelpers.GetRandomRsvpNotes();

                // Check if EventAttendee already exists (user might have a ticket purchase)
                // Social events allow both RSVP (free) AND ticket purchase (donation)
                var existingAttendee = await _context.EventAttendees
                    .AnyAsync(ea => ea.EventId == eventItem.Id && ea.UserId == user.Id, cancellationToken);

                // Only create EventAttendee if one doesn't already exist
                if (!existingAttendee)
                {
                    // Create EventAttendee record for RSVP participant
                    var attendee = new EventAttendee(eventItem.Id, user.Id, "confirmed")
                    {
                        Id = Guid.NewGuid(),
                        HasCompletedWaiver = true,
                        CreatedAt = createdAt,
                        UpdatedAt = createdAt,
                        CreatedBy = user.Id,
                        UpdatedBy = user.Id
                        // Note: TicketNumber is null for RSVP attendees (only set for ticket purchases)
                    };
                    attendeesToAdd.Add(attendee);
                }

                // Always create RSVP attendance record (user can have both RSVP and Ticket attendance types)
                var rsvpAttendance = new EventAttendance(eventItem.Id, user.Id, AttendanceType.RSVP)
                {
                    Id = Guid.NewGuid(),
                    Status = AttendanceStatus.Active,
                    Notes = notes,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt,
                    CreatedBy = user.Id,
                    UpdatedBy = user.Id
                    // Note: TicketPurchaseId is null for RSVP attendances
                };
                attendancesToAdd.Add(rsvpAttendance);

                _logger.LogDebug("Created RSVP for user {UserId} at event {EventTitle} (EventAttendee existed: {ExistingAttendee})",
                    user.Id, eventItem.Title, existingAttendee);
            }

            _logger.LogInformation("Created {RsvpCount} RSVPs for social event {EventTitle}",
                Math.Min(rsvpCount, vettedUsers.Count), eventItem.Title);
        }

        if (attendancesToAdd.Count > 0)
        {
            // Save RSVP attendances and attendees
            await _context.EventAttendances.AddRangeAsync(attendancesToAdd, cancellationToken);
            await _context.EventAttendees.AddRangeAsync(attendeesToAdd, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "RSVP attendance creation completed. Processed {EventsProcessed} social events. Created: {AttendanceCount} RSVP attendances, {AttendeeCount} attendees",
                eventsProcessed,
                attendancesToAdd.Count,
                attendeesToAdd.Count);
        }
        else
        {
            _logger.LogInformation("No new RSVP attendances needed - all social events already have RSVPs");
        }
    }

    /// <summary>
    /// Seeds historical social event RSVPs with check-ins, cancellations, and optional donations.
    /// Creates comprehensive attendance data for past social events.
    /// Called explicitly by SeedCoordinator to ensure proper ordering of seed operations.
    /// </summary>
    public async Task SeedHistoricalSocialEventRSVPs(EventSeeder eventSeeder, CancellationToken cancellationToken)
    {
        // Check if historical social event attendances already exist
        var practiceNightExists = await _context.EventAttendances
            .AnyAsync(ea => ea.EventId == eventSeeder.PracticeNightEventId, cancellationToken);

        if (!practiceNightExists)
        {
            // Historical Social Event 1: Monthly Rope Practice Night
            // Note: Limited to 8 RSVPs because we only have 9 vetted users (excludes canceled user)
            await CreateHistoricalSocialEventParticipationsAsync(
                eventSeeder.PracticeNightEventId,
                45, // days ago
                8, // total RSVPs (limited by available vetted users)
                6,  // check-ins
                "guest@witchcityrope.com", // canceled user
                cancellationToken);

            _logger.LogInformation("Created 8 RSVPs for Monthly Rope Practice Night (6 check-ins, 2 no-shows, 1 canceled). Donations handled by TicketPurchaseSeeder.");
        }

        var welcomeMixerExists = await _context.EventAttendances
            .AnyAsync(ea => ea.EventId == eventSeeder.WelcomeMixerEventId, cancellationToken);

        if (!welcomeMixerExists)
        {
            // Historical Social Event 2: New Member Welcome Mixer
            // Note: Limited to 8 RSVPs because we only have 9 vetted users (excludes canceled user)
            await CreateHistoricalSocialEventParticipationsAsync(
                eventSeeder.WelcomeMixerEventId,
                30, // days ago
                8, // total RSVPs (limited by available vetted users)
                5, // check-ins
                "vetted@witchcityrope.com", // canceled user
                cancellationToken);

            _logger.LogInformation("Created 8 RSVPs for New Member Welcome Mixer (5 check-ins, 3 no-shows, 1 canceled). Donations handled by TicketPurchaseSeeder.");
        }
    }

    /// <summary>
    /// Helper method to create comprehensive historical social event RSVP attendance data.
    /// Creates RSVPs with varied notes, check-ins for attendees, and one cancellation.
    /// Note: Donation tickets are created separately by TicketPurchaseSeeder.SeedSocialEventDonationTicketsAsync
    /// </summary>
    private async Task CreateHistoricalSocialEventParticipationsAsync(
        Guid eventId,
        int daysAgo,
        int totalRsvps,
        int checkInsCount,
        string canceledUserEmail,
        CancellationToken cancellationToken)
    {
        // 1. Get event from database
        var evt = await _context.Events.FindAsync(new object[] { eventId }, cancellationToken);
        if (evt == null)
        {
            _logger.LogWarning("Event {EventId} not found, skipping attendance seeding", eventId);
            return;
        }

        // 2. Get VETTED users from database (excluding canceled user for active RSVPs)
        // Business rule: Only vetted members (VettingStatus >= 3/Approved) can attend social events
        var users = await _userManager.Users.ToListAsync(cancellationToken);
        var availableUsers = users.Where(u => u.Email != canceledUserEmail && u.VettingStatus >= 3).ToList();

        if (availableUsers.Count < totalRsvps)
        {
            _logger.LogWarning("Not enough users available for RSVPs. Need {TotalRsvps}, have {AvailableCount}",
                totalRsvps, availableUsers.Count);
            return;
        }

        // 3. Get admin user for check-in staff member
        var adminUser = await _userManager.FindByEmailAsync("admin@witchcityrope.com");
        if (adminUser == null)
        {
            _logger.LogWarning("Admin user not found for check-ins, skipping");
            return;
        }

        // 4. Create active RSVPs using SeedingHelpers for notes
        for (int i = 0; i < totalRsvps; i++)
        {
            var user = availableUsers[i];
            var shouldCheckIn = i < checkInsCount;
            var rsvpCreatedAt = DateTime.UtcNow.AddDays(-(daysAgo + 3 + i / 3));
            var userNotes = SeedingHelpers.GetRandomRsvpNotes();

            // Check if EventAttendee already exists (user might have a donation ticket)
            // Historical social events allow both RSVP (free) AND ticket purchase (donation)
            var existingAttendee = await _context.EventAttendees
                .FirstOrDefaultAsync(ea => ea.EventId == eventId && ea.UserId == user.Id, cancellationToken);

            EventAttendee attendee;
            if (existingAttendee != null)
            {
                // Use existing attendee (from donation ticket)
                attendee = existingAttendee;
            }
            else
            {
                // Create EventAttendee for RSVP-only participants
                attendee = new EventAttendee
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    UserId = user.Id,
                    RegistrationStatus = "confirmed",
                    HasCompletedWaiver = true,
                    CreatedAt = rsvpCreatedAt,
                    UpdatedAt = rsvpCreatedAt
                };
                _context.EventAttendees.Add(attendee);
            }

            // Always create RSVP attendance for all participants
            // Note: Donation tickets (if any) are created by TicketPurchaseSeeder.SeedSocialEventDonationTicketsAsync
            var rsvpAttendance = new EventAttendance(eventId, user.Id, AttendanceType.RSVP)
            {
                Id = Guid.NewGuid(),
                Status = AttendanceStatus.Active,
                Notes = userNotes,
                CreatedAt = rsvpCreatedAt,
                UpdatedAt = rsvpCreatedAt,
                CreatedBy = user.Id,
                UpdatedBy = user.Id
            };
            _context.EventAttendances.Add(rsvpAttendance);

            // Create CheckIn if attended
            if (shouldCheckIn)
            {
                var checkIn = new CheckIn
                {
                    Id = Guid.NewGuid(),
                    EventAttendeeId = attendee.Id,
                    EventId = eventId,
                    StaffMemberId = adminUser.Id,
                    CheckInTime = evt.StartDate.AddMinutes(-10), // 10 min before event start
                    CreatedAt = evt.StartDate.AddMinutes(-10),
                    CreatedBy = adminUser.Id
                };
                _context.CheckIns.Add(checkIn);

                // Update attendee status to checked-in
                attendee.RegistrationStatus = "checked-in";
            }
        }

        // 6. Create canceled RSVP
        var canceledUser = await _userManager.FindByEmailAsync(canceledUserEmail);
        if (canceledUser != null)
        {
            var canceledRsvp = new EventAttendance
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                UserId = canceledUser.Id,
                AttendanceType = AttendanceType.RSVP,
                Status = AttendanceStatus.Cancelled,
                Notes = "Sorry, can't make it anymore",
                CreatedAt = DateTime.UtcNow.AddDays(-(daysAgo + 5)),
                CancelledAt = DateTime.UtcNow.AddDays(-(daysAgo + 1)),
                CancellationReason = "Schedule conflict",
                UpdatedAt = DateTime.UtcNow.AddDays(-(daysAgo + 1))
            };
            _context.EventAttendances.Add(canceledRsvp);
            // NO EventAttendee or CheckIn for canceled RSVPs
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
