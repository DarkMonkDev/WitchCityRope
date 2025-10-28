using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.CheckIn.Entities;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Services.Seeding;

/// <summary>
/// Handles seeding of event participation records (RSVPs and ticket-based attendance).
/// Extracted from SeedDataService.cs for better maintainability.
/// Responsible for creating EventParticipation records and corresponding TicketPurchase records.
/// </summary>
public class ParticipationSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly EventSeeder _eventSeeder;
    private readonly ILogger<ParticipationSeeder> _logger;

    public ParticipationSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        EventSeeder eventSeeder,
        ILogger<ParticipationSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _eventSeeder = eventSeeder;
        _logger = logger;
    }

    /// <summary>
    /// Seeds event participation records for both social events (RSVPs) and class events (ticket purchases).
    /// Idempotent operation - skips if participations already exist.
    ///
    /// Creates EventParticipation records:
    /// - Social events: RSVP type participations (no cost, free attendance)
    /// - Class events: Ticket type participations with corresponding TicketPurchase records
    ///
    /// For class events, also creates TicketPurchase records and updates TicketType.Sold counts.
    ///
    /// Note: This handles participations for ALL events (upcoming and historical).
    /// </summary>
    public async Task SeedEventParticipationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting event participations creation");

        var events = await _context.Events.Include(e => e.TicketTypes).ToListAsync(cancellationToken);
        var users = await _userManager.Users.ToListAsync(cancellationToken);
        var participationsToAdd = new List<EventParticipation>();
        var ticketPurchasesToAdd = new List<TicketPurchase>();
        var eventsProcessed = 0;

        foreach (var eventItem in events)
        {
            // Check if THIS specific event already has participations (idempotent per-event check)
            var hasParticipations = await _context.EventParticipations
                .AnyAsync(ep => ep.EventId == eventItem.Id, cancellationToken);

            if (hasParticipations)
            {
                _logger.LogDebug("Event {EventTitle} already has participations, skipping", eventItem.Title);
                continue; // Skip this event, but continue processing other events
            }

            eventsProcessed++;

            if (eventItem.EventType == EventType.Social)
            {
                // Social events: Create RSVPs for multiple users
                var rsvpCount = eventItem.Title.Contains("Community Rope Jam") ? 5 :
                               eventItem.Title.Contains("New Members Meetup") ? 8 :
                               eventItem.Title.Contains("Rope Social & Discussion") ? 6 : 3;

                for (int i = 0; i < Math.Min(rsvpCount, users.Count); i++)
                {
                    var user = users[i];
                    var participation = new EventParticipation(eventItem.Id, user.Id, ParticipationType.RSVP)
                    {
                        Id = Guid.NewGuid(),
                        Status = ParticipationStatus.Active,
                        CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 10)),
                        UpdatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 10)),
                        Notes = i == 0 ? "Looking forward to this event!" : null
                    };
                    participationsToAdd.Add(participation);
                }
            }
            else // Class events
            {
                // Class events: Create ticket purchases for multiple users
                var ticketCount = eventItem.Title.Contains("Introduction to Rope Safety") ? 5 :
                                 eventItem.Title.Contains("Suspension Basics") ? 4 :
                                 eventItem.Title.Contains("Advanced Floor Work") ? 3 : 2;

                // Get the first ticket type for this event (most events have one ticket type)
                var ticketType = eventItem.TicketTypes.FirstOrDefault();
                if (ticketType == null)
                {
                    _logger.LogWarning("No ticket types found for event {EventTitle}, skipping ticket purchases", eventItem.Title);
                    continue;
                }

                for (int i = 0; i < Math.Min(ticketCount, users.Count); i++)
                {
                    var user = users[i];
                    var purchaseAmount = (decimal)Random.Shared.Next(15, 65);

                    // Create EventParticipation record
                    var participation = new EventParticipation(eventItem.Id, user.Id, ParticipationType.Ticket)
                    {
                        Id = Guid.NewGuid(),
                        Status = ParticipationStatus.Active,
                        CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 20)),
                        UpdatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 20)),
                        Metadata = $"{{\"purchaseAmount\": {purchaseAmount}, \"paymentMethod\": \"PayPal\"}}"
                    };
                    participationsToAdd.Add(participation);

                    // Create corresponding TicketPurchase record
                    var ticketPurchase = new TicketPurchase
                    {
                        Id = Guid.NewGuid(),
                        TicketTypeId = ticketType.Id,
                        UserId = user.Id,
                        Quantity = 1,
                        TotalPrice = purchaseAmount,
                        PaymentStatus = "Completed",
                        PaymentMethod = "PayPal",
                        PaymentReference = $"PP-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                        PurchaseDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 20)),
                        CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 20)),
                        UpdatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 20))
                    };
                    ticketPurchasesToAdd.Add(ticketPurchase);
                }
            }
        }

        await _context.EventParticipations.AddRangeAsync(participationsToAdd, cancellationToken);
        await _context.TicketPurchases.AddRangeAsync(ticketPurchasesToAdd, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Update TicketType.Sold counts based on purchases
        foreach (var ticketPurchase in ticketPurchasesToAdd)
        {
            var ticketType = await _context.TicketTypes.FindAsync(new object[] { ticketPurchase.TicketTypeId }, cancellationToken);
            if (ticketType != null)
            {
                ticketType.Sold += ticketPurchase.Quantity;
            }
        }
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Event participations creation completed. Processed {EventsProcessed} events. Created: {ParticipationCount} participations and {PurchaseCount} ticket purchases",
            eventsProcessed, participationsToAdd.Count, ticketPurchasesToAdd.Count);
    }

    /// <summary>
    /// Seeds historical social event RSVPs with check-ins, cancellations, and optional donations.
    /// Creates comprehensive attendance data for past social events.
    /// Called explicitly by SeedCoordinator to ensure proper ordering of seed operations.
    /// </summary>
    public async Task SeedHistoricalSocialEventRSVPs(EventSeeder eventSeeder, CancellationToken cancellationToken)
    {
        // Check if historical social event participations already exist
        var practiceNightExists = await _context.EventParticipations
            .AnyAsync(ep => ep.EventId == eventSeeder.PracticeNightEventId, cancellationToken);

        if (!practiceNightExists)
        {
            // Historical Social Event 1: Monthly Rope Practice Night
            await CreateHistoricalSocialEventParticipationsAsync(
                eventSeeder.PracticeNightEventId,
                45, // days ago
                14, // total RSVPs
                9,  // check-ins
                5,  // donation tickets
                "guest@witchcityrope.com", // canceled user
                10.00m, // donation amount
                cancellationToken);

            _logger.LogInformation("Created 14 RSVPs for Monthly Rope Practice Night (9 check-ins, 5 no-shows, 1 canceled)");
        }

        var welcomeMixerExists = await _context.EventParticipations
            .AnyAsync(ep => ep.EventId == eventSeeder.WelcomeMixerEventId, cancellationToken);

        if (!welcomeMixerExists)
        {
            // Historical Social Event 2: New Member Welcome Mixer
            await CreateHistoricalSocialEventParticipationsAsync(
                eventSeeder.WelcomeMixerEventId,
                30, // days ago
                15, // total RSVPs
                10, // check-ins
                7,  // donation tickets
                "vetted@witchcityrope.com", // canceled user
                5.00m, // donation amount
                cancellationToken);

            _logger.LogInformation("Created 15 RSVPs for New Member Welcome Mixer (10 check-ins, 5 no-shows, 1 canceled)");
        }
    }

    /// <summary>
    /// Helper method to create comprehensive historical social event participation data.
    /// Creates RSVPs with varied notes, optional donation tickets, check-ins for attendees, and one cancellation.
    /// </summary>
    private async Task CreateHistoricalSocialEventParticipationsAsync(
        Guid eventId,
        int daysAgo,
        int totalRsvps,
        int checkInsCount,
        int donationTickets,
        string canceledUserEmail,
        decimal donationAmount,
        CancellationToken cancellationToken)
    {
        // 1. Get event from database
        var evt = await _context.Events.FindAsync(new object[] { eventId }, cancellationToken);
        if (evt == null)
        {
            _logger.LogWarning("Event {EventId} not found, skipping participation seeding", eventId);
            return;
        }

        // 2. Get users from database (excluding canceled user for active RSVPs)
        var users = await _userManager.Users.ToListAsync(cancellationToken);
        var availableUsers = users.Where(u => u.Email != canceledUserEmail).ToList();

        if (availableUsers.Count < totalRsvps)
        {
            _logger.LogWarning("Not enough users available for RSVPs. Need {TotalRsvps}, have {AvailableCount}",
                totalRsvps, availableUsers.Count);
            return;
        }

        // 3. Get suggested donation ticket type (if exists)
        var donationTicketType = await _context.TicketTypes
            .FirstOrDefaultAsync(tt => tt.EventId == eventId && tt.Name.Contains("Donation"), cancellationToken);

        // 4. Get admin user for check-in staff member
        var adminUser = await _userManager.FindByEmailAsync("admin@witchcityrope.com");
        if (adminUser == null)
        {
            _logger.LogWarning("Admin user not found for check-ins, skipping");
            return;
        }

        // 5. Create active RSVPs
        var rsvpNotes = new[]
        {
            "Looking forward to it!",
            "Bringing a friend",
            "First time!",
            "Excited to join!",
            "Can't wait!",
            "New member here",
            "Looking forward to meeting everyone",
            "See you there!",
            "Count me in!",
            "Thanks for organizing this!",
            "Will be there!",
            "Happy to participate",
            "Looking forward to connecting",
            "Excited for this event",
            "Glad to RSVP!"
        };

        for (int i = 0; i < totalRsvps; i++)
        {
            var user = availableUsers[i];
            var shouldCheckIn = i < checkInsCount;
            var shouldBuyDonation = i < donationTickets && donationTicketType != null;
            var rsvpCreatedAt = DateTime.UtcNow.AddDays(-(daysAgo + 3 + i / 3));

            // Create RSVP EventParticipation
            var rsvp = new EventParticipation
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                UserId = user.Id,
                ParticipationType = ParticipationType.RSVP,
                Status = ParticipationStatus.Active,
                Notes = rsvpNotes[i % rsvpNotes.Length],
                CreatedAt = rsvpCreatedAt,
                UpdatedAt = rsvpCreatedAt
            };
            _context.EventParticipations.Add(rsvp);

            // Create EventAttendee for active RSVPs
            var attendee = new EventAttendee
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                UserId = user.Id,
                RegistrationStatus = "confirmed",
                CreatedAt = rsvpCreatedAt,
                UpdatedAt = rsvpCreatedAt
            };

            // Create optional donation ticket participation
            if (shouldBuyDonation && donationTicketType != null)
            {
                var donation = new EventParticipation
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    UserId = user.Id,
                    ParticipationType = ParticipationType.Ticket,
                    Status = ParticipationStatus.Active,
                    Metadata = $"{{\"ticketType\":\"{donationTicketType.Name}\",\"price\":{donationAmount},\"paymentMethod\":\"Stripe\"}}",
                    CreatedAt = rsvpCreatedAt,
                    UpdatedAt = rsvpCreatedAt
                };
                _context.EventParticipations.Add(donation);

                // Update EventAttendee with donation ticket info
                attendee.TicketNumber = $"DN-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            }

            _context.EventAttendees.Add(attendee);

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
            var canceledRsvp = new EventParticipation
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                UserId = canceledUser.Id,
                ParticipationType = ParticipationType.RSVP,
                Status = ParticipationStatus.Cancelled,
                Notes = "Sorry, can't make it anymore",
                CreatedAt = DateTime.UtcNow.AddDays(-(daysAgo + 5)),
                CancelledAt = DateTime.UtcNow.AddDays(-(daysAgo + 1)),
                CancellationReason = "Schedule conflict",
                UpdatedAt = DateTime.UtcNow.AddDays(-(daysAgo + 1))
            };
            _context.EventParticipations.Add(canceledRsvp);
            // NO EventAttendee or CheckIn for canceled RSVPs
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
