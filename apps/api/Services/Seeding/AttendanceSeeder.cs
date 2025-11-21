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
/// Handles seeding of event attendance records (RSVPs and ticket-based attendance).
/// Extracted from SeedDataService.cs for better maintainability.
/// Responsible for creating EventAttendance records and corresponding TicketPurchase records.
/// </summary>
public class AttendanceSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly EventSeeder _eventSeeder;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<AttendanceSeeder> _logger;

    public AttendanceSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        EventSeeder eventSeeder,
        IEncryptionService encryptionService,
        ILogger<AttendanceSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _eventSeeder = eventSeeder;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    /// <summary>
    /// Generates a random PayPal Order ID in format like "5O190127TN364715T"
    /// </summary>
    private string GeneratePayPalOrderId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var orderId = new char[17];
        for (int i = 0; i < 17; i++)
        {
            orderId[i] = chars[Random.Shared.Next(chars.Length)];
        }
        return new string(orderId);
    }

    /// <summary>
    /// Generates a random PayPal Capture ID in format like "3C679366H7890240F"
    /// </summary>
    private string GeneratePayPalCaptureId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var captureId = new char[17];
        for (int i = 0; i < 17; i++)
        {
            captureId[i] = chars[Random.Shared.Next(chars.Length)];
        }
        return new string(captureId);
    }

    /// <summary>
    /// Seeds event attendance records for both social events (RSVPs) and class events (ticket purchases).
    /// Idempotent operation - skips if attendances already exist.
    ///
    /// Creates EventAttendance records:
    /// - Social events: RSVP type attendances (no cost, free attendance)
    /// - Class events: Ticket type attendances with corresponding TicketPurchase records
    ///
    /// For class events, also creates TicketPurchase records.
    ///
    /// Note: This handles attendances for ALL events (upcoming and historical).
    /// </summary>
    public async Task SeedEventParticipationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting event attendances creation");

        var events = await _context.Events.Include(e => e.TicketTypes).ToListAsync(cancellationToken);
        var users = await _userManager.Users.ToListAsync(cancellationToken);
        var attendancesToAdd = new List<EventAttendance>();
        var ticketPurchasesToAdd = new List<TicketPurchase>();
        var attendeesToAdd = new List<EventAttendee>();
        var eventsProcessed = 0;

        foreach (var eventItem in events)
        {
            // Skip historical social events - they're seeded by SeedHistoricalSocialEventRSVPs with check-ins/cancellations
            if (eventItem.Title == "Monthly Rope Practice Night" || eventItem.Title == "New Member Welcome Mixer")
            {
                _logger.LogDebug("Skipping historical social event {EventTitle} - seeded separately", eventItem.Title);
                continue;
            }

            // Check if THIS specific event already has attendances (idempotent per-event check)
            var hasAttendances = await _context.EventAttendances
                .AnyAsync(ea => ea.EventId == eventItem.Id, cancellationToken);

            if (hasAttendances)
            {
                _logger.LogDebug("Event {EventTitle} already has attendances, skipping", eventItem.Title);
                continue; // Skip this event, but continue processing other events
            }

            eventsProcessed++;

            if (eventItem.EventType == EventType.Social)
            {
                // Social events: Create RSVPs for multiple VETTED users only
                // Business rule: Only vetted members (VettingStatus >= 3/Approved) can attend social events
                var vettedUsers = users.Where(u => u.VettingStatus >= 3).ToList();

                var rsvpCount = eventItem.Title.Contains("Community Rope Jam") ? 5 :
                               eventItem.Title.Contains("New Members Meetup") ? 8 :
                               eventItem.Title.Contains("Rope Social & Discussion") ? 6 : 3;

                // Find donation ticket type for this social event
                var donationTicketType = eventItem.TicketTypes.FirstOrDefault(tt => tt.Name.Contains("Donation"));
                var donationCount = (int)Math.Ceiling(rsvpCount / 2.0); // At least half purchase donations

                for (int i = 0; i < Math.Min(rsvpCount, vettedUsers.Count); i++)
                {
                    var user = vettedUsers[i];
                    var createdAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 10));
                    var notes = i == 0 ? "Looking forward to this event!" : null;
                    var shouldBuyDonation = i < donationCount && donationTicketType != null;

                    // Create EventAttendee record for all participants
                    var attendee = new EventAttendee
                    {
                        Id = Guid.NewGuid(),
                        EventId = eventItem.Id,
                        UserId = user.Id,
                        RegistrationStatus = "confirmed",
                        HasCompletedWaiver = true,
                        CreatedAt = createdAt,
                        UpdatedAt = createdAt
                    };

                    // CRITICAL FIX: Social event donation buyers get BOTH RSVP + Ticket attendance
                    // RSVP is the base attendance (required for all social event attendees)
                    // Donation ticket is ADDITIONAL attendance on top of RSVP
                    if (shouldBuyDonation)
                    {
                        // Create RSVP attendance FIRST (required for ALL social event attendees)
                        var rsvpAttendance = new EventAttendance(eventItem.Id, user.Id, AttendanceType.RSVP)
                        {
                            Id = Guid.NewGuid(),
                            Status = AttendanceStatus.Active,
                            Notes = notes,
                            CreatedAt = createdAt,
                            UpdatedAt = createdAt
                        };
                        attendancesToAdd.Add(rsvpAttendance);

                        var donationAmount = (decimal)Random.Shared.Next(5, 31); // FIXED: $5-$30 donation (minimum $5)

                        // Randomly assign payment method for social event donations (33% Cash, 67% PayPal)
                        // Cash payments can only happen at the door for social events
                        var paymentMethod = Random.Shared.Next(0, 3) == 0 ? "Cash" : "PayPal";

                        // THEN create donation ticket purchase and attendance (in ADDITION to RSVP)
                        var ticketPurchase = new TicketPurchase
                        {
                            Id = Guid.NewGuid(),
                            TicketTypeId = donationTicketType.Id,
                            UserId = user.Id,
                            Quantity = 1,
                            TotalPrice = donationAmount,
                            PaymentStatus = "Completed",
                            PaymentMethod = paymentMethod,
                            PaymentReference = $"DN-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                            PurchaseDate = createdAt,
                            CreatedAt = createdAt,
                            UpdatedAt = createdAt
                        };

                        // Add encrypted PayPal fields for PayPal payments (PCI compliance)
                        if (paymentMethod == "PayPal")
                        {
                            ticketPurchase.EncryptedPayPalOrderId = await _encryptionService.EncryptAsync(GeneratePayPalOrderId());
                            ticketPurchase.EncryptedPayPalCaptureId = await _encryptionService.EncryptAsync(GeneratePayPalCaptureId());
                            ticketPurchase.EncryptedPayPalPayerId = await _encryptionService.EncryptAsync($"PAYER{Guid.NewGuid().ToString("N")[..10].ToUpper()}");
                        }

                        ticketPurchasesToAdd.Add(ticketPurchase);

                        var ticketAttendance = new EventAttendance(eventItem.Id, user.Id, AttendanceType.Ticket)
                        {
                            Id = Guid.NewGuid(),
                            Status = AttendanceStatus.Active,
                            TicketPurchaseId = ticketPurchase.Id,
                            Metadata = $"{{\"ticketType\":\"Suggested Donation\",\"price\":{donationAmount},\"paymentMethod\":\"{paymentMethod}\"}}",
                            CreatedAt = createdAt,
                            UpdatedAt = createdAt
                        };
                        attendancesToAdd.Add(ticketAttendance);

                        // Update attendee with donation ticket number
                        attendee.TicketNumber = $"DN-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                    }
                    else
                    {
                        // Create RSVP attendance for non-donation users (unchanged)
                        var rsvpAttendance = new EventAttendance(eventItem.Id, user.Id, AttendanceType.RSVP)
                        {
                            Id = Guid.NewGuid(),
                            Status = AttendanceStatus.Active,
                            Notes = notes,
                            CreatedAt = createdAt,
                            UpdatedAt = createdAt
                        };
                        attendancesToAdd.Add(rsvpAttendance);
                    }

                    attendeesToAdd.Add(attendee);
                }
            }
            else // Class events
            {
                // Special handling for Suspension Basics: Multiple ticket types
                if (eventItem.Title.Contains("Suspension Basics"))
                {
                    // Create 2 "All 2 Days" tickets
                    var all2DaysTicket = eventItem.TicketTypes.FirstOrDefault(tt => tt.Name == "All 2 Days");
                    if (all2DaysTicket != null)
                    {
                        for (int i = 0; i < 2 && i < users.Count; i++)
                        {
                            var user = users[i];
                            var purchaseAmount = (decimal)Random.Shared.Next(15, 65);
                            var createdAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 20));

                            var ticketPurchase = new TicketPurchase
                            {
                                Id = Guid.NewGuid(),
                                TicketTypeId = all2DaysTicket.Id,
                                UserId = user.Id,
                                Quantity = 1,
                                TotalPrice = purchaseAmount,
                                PaymentStatus = "Completed",
                                PaymentMethod = "PayPal",
                                PaymentReference = $"PP-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                                PurchaseDate = createdAt,
                                CreatedAt = createdAt,
                                UpdatedAt = createdAt,
                                // Add encrypted PayPal fields (PCI compliance)
                                EncryptedPayPalOrderId = await _encryptionService.EncryptAsync(GeneratePayPalOrderId()),
                                EncryptedPayPalCaptureId = await _encryptionService.EncryptAsync(GeneratePayPalCaptureId()),
                                EncryptedPayPalPayerId = await _encryptionService.EncryptAsync($"PAYER{Guid.NewGuid().ToString("N")[..10].ToUpper()}")
                            };
                            ticketPurchasesToAdd.Add(ticketPurchase);

                            var attendance = new EventAttendance(eventItem.Id, user.Id, AttendanceType.Ticket)
                            {
                                Id = Guid.NewGuid(),
                                Status = AttendanceStatus.Active,
                                TicketPurchaseId = ticketPurchase.Id,
                                CreatedAt = createdAt,
                                UpdatedAt = createdAt,
                                Metadata = $"{{\"purchaseAmount\": {purchaseAmount}, \"paymentMethod\": \"PayPal\"}}"
                            };
                            attendancesToAdd.Add(attendance);

                            var attendee = new EventAttendee
                            {
                                Id = Guid.NewGuid(),
                                EventId = eventItem.Id,
                                UserId = user.Id,
                                TicketNumber = $"TKT-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                                RegistrationStatus = "confirmed",
                                CreatedAt = createdAt,
                                UpdatedAt = createdAt,
                                HasCompletedWaiver = true
                            };
                            attendeesToAdd.Add(attendee);
                        }
                    }

                    // Create 4 "Day 1 Only" tickets (using different users)
                    var day1OnlyTicket = eventItem.TicketTypes.FirstOrDefault(tt => tt.Name == "Day 1 Only");
                    if (day1OnlyTicket != null)
                    {
                        for (int i = 2; i < 6 && i < users.Count; i++) // Start at index 2 to use different users
                        {
                            var user = users[i];
                            var purchaseAmount = (decimal)Random.Shared.Next(15, 65);
                            var createdAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 20));

                            var ticketPurchase = new TicketPurchase
                            {
                                Id = Guid.NewGuid(),
                                TicketTypeId = day1OnlyTicket.Id,
                                UserId = user.Id,
                                Quantity = 1,
                                TotalPrice = purchaseAmount,
                                PaymentStatus = "Completed",
                                PaymentMethod = "PayPal",
                                PaymentReference = $"PP-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                                PurchaseDate = createdAt,
                                CreatedAt = createdAt,
                                UpdatedAt = createdAt,
                                // Add encrypted PayPal fields (PCI compliance)
                                EncryptedPayPalOrderId = await _encryptionService.EncryptAsync(GeneratePayPalOrderId()),
                                EncryptedPayPalCaptureId = await _encryptionService.EncryptAsync(GeneratePayPalCaptureId()),
                                EncryptedPayPalPayerId = await _encryptionService.EncryptAsync($"PAYER{Guid.NewGuid().ToString("N")[..10].ToUpper()}")
                            };
                            ticketPurchasesToAdd.Add(ticketPurchase);

                            var attendance = new EventAttendance(eventItem.Id, user.Id, AttendanceType.Ticket)
                            {
                                Id = Guid.NewGuid(),
                                Status = AttendanceStatus.Active,
                                TicketPurchaseId = ticketPurchase.Id,
                                CreatedAt = createdAt,
                                UpdatedAt = createdAt,
                                Metadata = $"{{\"purchaseAmount\": {purchaseAmount}, \"paymentMethod\": \"PayPal\"}}"
                            };
                            attendancesToAdd.Add(attendance);

                            var attendee = new EventAttendee
                            {
                                Id = Guid.NewGuid(),
                                EventId = eventItem.Id,
                                UserId = user.Id,
                                TicketNumber = $"TKT-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                                RegistrationStatus = "confirmed",
                                CreatedAt = createdAt,
                                UpdatedAt = createdAt,
                                HasCompletedWaiver = true
                            };
                            attendeesToAdd.Add(attendee);
                        }
                    }

                    continue; // Skip the general ticket creation logic below
                }

                // General ticket creation for other class events
                var ticketCount = eventItem.Title.Contains("Introduction to Rope Safety") ? 5 :
                                 eventItem.Title.Contains("Advanced Floor Work") ? 3 : 2;

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
                    var createdAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 20));

                    // Create TicketPurchase record FIRST (so we have the ID)
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
                        PurchaseDate = createdAt,
                        CreatedAt = createdAt,
                        UpdatedAt = createdAt,
                        // Add encrypted PayPal fields (PCI compliance)
                        EncryptedPayPalOrderId = await _encryptionService.EncryptAsync(GeneratePayPalOrderId()),
                        EncryptedPayPalCaptureId = await _encryptionService.EncryptAsync(GeneratePayPalCaptureId()),
                        EncryptedPayPalPayerId = await _encryptionService.EncryptAsync($"PAYER{Guid.NewGuid().ToString("N")[..10].ToUpper()}")
                    };
                    ticketPurchasesToAdd.Add(ticketPurchase);

                    // Create EventAttendance record LINKED to purchase
                    var attendance = new EventAttendance(eventItem.Id, user.Id, AttendanceType.Ticket)
                    {
                        Id = Guid.NewGuid(),
                        Status = AttendanceStatus.Active,
                        TicketPurchaseId = ticketPurchase.Id, // CRITICAL: Link to purchase
                        CreatedAt = createdAt,
                        UpdatedAt = createdAt,
                        Metadata = $"{{\"purchaseAmount\": {purchaseAmount}, \"paymentMethod\": \"PayPal\"}}"
                    };
                    attendancesToAdd.Add(attendance);

                    // Create EventAttendee record so attendee appears in check-in system
                    var attendee = new EventAttendee
                    {
                        Id = Guid.NewGuid(),
                        EventId = eventItem.Id,
                        UserId = user.Id,
                        TicketNumber = $"TKT-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                        RegistrationStatus = "confirmed",
                        CreatedAt = createdAt,
                        UpdatedAt = createdAt,
                        HasCompletedWaiver = true
                    };
                    attendeesToAdd.Add(attendee);
                }
            }
        }

        await _context.EventAttendances.AddRangeAsync(attendancesToAdd, cancellationToken);
        await _context.TicketPurchases.AddRangeAsync(ticketPurchasesToAdd, cancellationToken);
        await _context.EventAttendees.AddRangeAsync(attendeesToAdd, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // DELETE: TicketType.Sold is now a calculated property, no manual updates needed
        // Previous code manually incremented Sold counts - now it's calculated from EventAttendances

        _logger.LogInformation("Event attendances creation completed. Processed {EventsProcessed} events. Created: {AttendanceCount} attendances, {AttendeeCount} attendees, and {PurchaseCount} ticket purchases",
            eventsProcessed, attendancesToAdd.Count, attendeesToAdd.Count, ticketPurchasesToAdd.Count);
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
                4,  // donation tickets (at least half)
                "guest@witchcityrope.com", // canceled user
                cancellationToken);

            _logger.LogInformation("Created 8 RSVPs for Monthly Rope Practice Night (6 check-ins, 2 no-shows, 1 canceled, 4 donations)");
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
                4,  // donation tickets (at least half)
                "vetted@witchcityrope.com", // canceled user
                cancellationToken);

            _logger.LogInformation("Created 8 RSVPs for New Member Welcome Mixer (5 check-ins, 3 no-shows, 1 canceled, 4 donations)");
        }
    }

    /// <summary>
    /// Helper method to create comprehensive historical social event attendance data.
    /// Creates RSVPs with varied notes, optional donation tickets, check-ins for attendees, and one cancellation.
    /// Donation amounts are randomized from $0 to $30 per ticket.
    /// </summary>
    private async Task CreateHistoricalSocialEventParticipationsAsync(
        Guid eventId,
        int daysAgo,
        int totalRsvps,
        int checkInsCount,
        int donationTickets,
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
            var userNotes = rsvpNotes[i % rsvpNotes.Length];

            // Create EventAttendee for all participants
            var attendee = new EventAttendee
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                UserId = user.Id,
                RegistrationStatus = "confirmed",
                HasCompletedWaiver = true,
                CreatedAt = rsvpCreatedAt,
                UpdatedAt = rsvpCreatedAt
            };

            // CRITICAL FIX: Social event donation buyers get BOTH RSVP + Ticket attendance
            // RSVP is the base attendance (required for all social event attendees)
            // Donation ticket is ADDITIONAL attendance on top of RSVP
            if (shouldBuyDonation && donationTicketType != null)
            {
                // Create RSVP attendance FIRST (required for ALL social event attendees)
                var rsvpAttendance = new EventAttendance
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    UserId = user.Id,
                    AttendanceType = AttendanceType.RSVP,
                    Status = AttendanceStatus.Active,
                    Notes = userNotes,
                    CreatedAt = rsvpCreatedAt,
                    UpdatedAt = rsvpCreatedAt
                };
                _context.EventAttendances.Add(rsvpAttendance);

                // Generate random donation amount ($5-$30 minimum $5)
                var donationAmount = (decimal)Random.Shared.Next(5, 31);

                // Randomly assign payment method for social event donations (33% Cash, 67% PayPal)
                // Cash payments can only happen at the door for social events
                var paymentMethod = Random.Shared.Next(0, 3) == 0 ? "Cash" : "PayPal";

                // THEN create donation ticket purchase and attendance (in ADDITION to RSVP)
                var donationPurchase = new TicketPurchase
                {
                    Id = Guid.NewGuid(),
                    TicketTypeId = donationTicketType.Id,
                    UserId = user.Id,
                    Quantity = 1,
                    TotalPrice = donationAmount,
                    PaymentStatus = "Completed",
                    PaymentMethod = paymentMethod,
                    PaymentReference = $"DN-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                    PurchaseDate = rsvpCreatedAt,
                    CreatedAt = rsvpCreatedAt,
                    UpdatedAt = rsvpCreatedAt
                };

                // Add encrypted PayPal fields for PayPal payments (PCI compliance)
                if (paymentMethod == "PayPal")
                {
                    donationPurchase.EncryptedPayPalOrderId = await _encryptionService.EncryptAsync(GeneratePayPalOrderId());
                    donationPurchase.EncryptedPayPalCaptureId = await _encryptionService.EncryptAsync(GeneratePayPalCaptureId());
                    donationPurchase.EncryptedPayPalPayerId = await _encryptionService.EncryptAsync($"PAYER{Guid.NewGuid().ToString("N")[..10].ToUpper()}");
                }

                _context.TicketPurchases.Add(donationPurchase);

                var ticketAttendance = new EventAttendance
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    UserId = user.Id,
                    AttendanceType = AttendanceType.Ticket,
                    Status = AttendanceStatus.Active,
                    TicketPurchaseId = donationPurchase.Id,
                    Metadata = $"{{\"ticketType\":\"{donationTicketType.Name}\",\"price\":{donationAmount},\"paymentMethod\":\"{paymentMethod}\"}}",
                    CreatedAt = rsvpCreatedAt,
                    UpdatedAt = rsvpCreatedAt
                };
                _context.EventAttendances.Add(ticketAttendance);

                // Update EventAttendee with donation ticket info
                attendee.TicketNumber = $"DN-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            }
            else
            {
                // Create RSVP attendance for non-donation users (unchanged)
                var rsvpAttendance = new EventAttendance
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    UserId = user.Id,
                    AttendanceType = AttendanceType.RSVP,
                    Status = AttendanceStatus.Active,
                    Notes = userNotes,
                    CreatedAt = rsvpCreatedAt,
                    UpdatedAt = rsvpCreatedAt
                };
                _context.EventAttendances.Add(rsvpAttendance);
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
