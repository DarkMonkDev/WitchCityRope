using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.CheckIn.Entities;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Models;
using WitchCityRope.Models;

namespace WitchCityRope.Api.Services.Seeding;

/// <summary>
/// Handles seeding of ticket purchases for both current and historical events.
/// Extracted from SeedDataService.cs for better maintainability.
/// Responsible for creating TicketPurchase records with realistic payment data.
/// </summary>
public class TicketPurchaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TicketPurchaseSeeder> _logger;

    public TicketPurchaseSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<TicketPurchaseSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Seeds ticket purchases for all ticket types across all events.
    /// Idempotent operation - skips if purchases already exist.
    ///
    /// Creates realistic purchase data:
    /// - Random purchase dates (1-30 days ago)
    /// - Sliding scale pricing with randomized amounts
    /// - Payment status variety (mostly Completed, some Pending/Failed)
    /// - Various payment methods (PayPal, Stripe, Venmo, Cash, Zelle)
    /// - Optional purchase notes
    ///
    /// Also creates specific test data for vetted test user (5 purchases) to support E2E dashboard testing.
    /// </summary>
    public async Task SeedTicketPurchasesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ticket purchases creation");

        // Check if purchases already exist (idempotent operation)
        var existingPurchaseCount = await _context.TicketPurchases.CountAsync(cancellationToken);
        if (existingPurchaseCount > 0)
        {
            _logger.LogInformation("Ticket purchases already exist ({Count}), skipping purchase seeding", existingPurchaseCount);
            return;
        }

        var ticketTypes = await _context.TicketTypes
            .Include(t => t.Event)
            .ToListAsync(cancellationToken);

        var users = await _userManager.Users.ToListAsync(cancellationToken);
        var purchasesToAdd = new List<TicketPurchase>();

        // Create realistic purchase data
        foreach (var ticketType in ticketTypes)
        {
            var purchaseCount = Math.Min(ticketType.Sold, users.Count);

            for (int i = 0; i < purchaseCount; i++)
            {
                var user = users[i % users.Count];
                var isRSVP = ticketType.Price == 0;

                // Calculate purchase price based on pricing type
                decimal totalPrice;
                if (isRSVP)
                {
                    totalPrice = 0;
                }
                else if (ticketType.PricingType == PricingType.SlidingScale)
                {
                    // Random price within sliding scale range
                    var minPrice = ticketType.MinPrice ?? 10m;
                    var maxPrice = ticketType.MaxPrice ?? 40m;
                    totalPrice = minPrice + (decimal)Random.Shared.NextDouble() * (maxPrice - minPrice);
                }
                else
                {
                    // Fixed price with slight variation for realism
                    totalPrice = (ticketType.Price ?? 0) * (0.5m + (decimal)Random.Shared.NextDouble() * 0.5m);
                }

                var purchase = new TicketPurchase
                {
                    TicketTypeId = ticketType.Id,
                    UserId = user.Id,
                    PurchaseDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
                    Quantity = 1,
                    TotalPrice = totalPrice,
                    PaymentStatus = isRSVP ? "Completed" : GetRandomPaymentStatus(),
                    PaymentMethod = isRSVP ? "RSVP" : GetRandomPaymentMethod(),
                    PaymentReference = Guid.NewGuid().ToString("N")[..8],
                    Notes = GetRandomPurchaseNotes()
                };

                purchasesToAdd.Add(purchase);
            }
        }

        // Create specific ticket purchases for vetted test user for E2E dashboard testing
        await CreateVettedUserTicketPurchasesAsync(purchasesToAdd, cancellationToken);

        await _context.TicketPurchases.AddRangeAsync(purchasesToAdd, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ticket purchases creation completed. Created: {PurchaseCount} purchases", purchasesToAdd.Count);
    }

    /// <summary>
    /// Creates specific ticket purchases for the vetted test user (vetted@witchcityrope.com)
    /// to provide realistic data for E2E testing of user dashboard functionality.
    ///
    /// Creates 3-5 ticket purchases covering different scenarios:
    /// - Upcoming paid event (workshop)
    /// - Upcoming free event (social RSVP)
    /// - Past attended event
    /// - Optional social event with ticket
    /// - Optional additional event for search/filter testing
    /// </summary>
    private async Task CreateVettedUserTicketPurchasesAsync(
        List<TicketPurchase> purchasesToAdd,
        CancellationToken cancellationToken)
    {
        // Get the vetted test user
        var vettedUser = await _userManager.FindByEmailAsync("vetted@witchcityrope.com");
        if (vettedUser == null)
        {
            _logger.LogWarning("Vetted test user not found, skipping dashboard test data creation");
            return;
        }

        // Check if vetted user already has ticket purchases
        var existingPurchases = await _context.TicketPurchases
            .Where(tp => tp.UserId == vettedUser.Id)
            .CountAsync(cancellationToken);

        if (existingPurchases > 0)
        {
            _logger.LogInformation("Vetted test user already has {Count} ticket purchases, skipping seed", existingPurchases);
            return;
        }

        _logger.LogInformation("Creating ticket purchases for vetted test user dashboard E2E testing");

        // Find events to register for
        var upcomingWorkshop = await _context.Events
            .Include(e => e.TicketTypes)
            .Where(e => e.EventType == EventType.Class &&
                       e.StartDate > DateTime.UtcNow &&
                       e.TicketTypes.Any())
            .OrderBy(e => e.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        var upcomingSocial = await _context.Events
            .Include(e => e.TicketTypes)
            .Where(e => e.EventType == EventType.Social &&
                       e.StartDate > DateTime.UtcNow &&
                       e.TicketTypes.Any())
            .OrderBy(e => e.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        var pastEvent = await _context.Events
            .Include(e => e.TicketTypes)
            .Where(e => e.EndDate < DateTime.UtcNow &&
                       e.TicketTypes.Any())
            .OrderByDescending(e => e.EndDate)
            .FirstOrDefaultAsync(cancellationToken);

        // Additional upcoming events for comprehensive testing
        var additionalUpcomingEvents = await _context.Events
            .Include(e => e.TicketTypes)
            .Where(e => e.StartDate > DateTime.UtcNow &&
                       e.TicketTypes.Any() &&
                       e.Id != (upcomingWorkshop != null ? upcomingWorkshop.Id : Guid.Empty) &&
                       e.Id != (upcomingSocial != null ? upcomingSocial.Id : Guid.Empty))
            .OrderBy(e => e.StartDate)
            .Take(2)
            .ToListAsync(cancellationToken);

        // Scenario 1: Upcoming Paid Event (Workshop/Class)
        if (upcomingWorkshop != null)
        {
            var paidTicket = upcomingWorkshop.TicketTypes
                .Where(tt => tt.Price > 0)
                .OrderBy(tt => tt.Price)
                .FirstOrDefault();

            if (paidTicket != null)
            {
                // Calculate price based on ticket type
                decimal purchasePrice;
                if (paidTicket.PricingType == PricingType.SlidingScale)
                {
                    purchasePrice = paidTicket.DefaultPrice ?? 20m; // Use default/suggested price
                }
                else
                {
                    purchasePrice = (paidTicket.Price ?? 0) * 0.75m; // Sliding scale discount for fixed price
                }

                purchasesToAdd.Add(new TicketPurchase
                {
                    Id = Guid.NewGuid(),
                    UserId = vettedUser.Id,
                    TicketTypeId = paidTicket.Id,
                    PurchaseDate = DateTime.UtcNow.AddDays(-5),
                    Quantity = 1,
                    TotalPrice = purchasePrice,
                    PaymentStatus = "Completed",
                    PaymentMethod = "PayPal",
                    PaymentReference = $"SEED_ORDER_{Guid.NewGuid().ToString()[..8]}",
                    Notes = "Sliding scale pricing applied"
                });

                _logger.LogInformation("Created paid workshop ticket purchase for event: {EventTitle}", upcomingWorkshop.Title);
            }
        }

        // Scenario 2: Upcoming Free Event (Social RSVP)
        if (upcomingSocial != null)
        {
            var freeTicket = upcomingSocial.TicketTypes
                .Where(tt => tt.Price == 0)
                .FirstOrDefault();

            if (freeTicket != null)
            {
                purchasesToAdd.Add(new TicketPurchase
                {
                    Id = Guid.NewGuid(),
                    UserId = vettedUser.Id,
                    TicketTypeId = freeTicket.Id,
                    PurchaseDate = DateTime.UtcNow.AddDays(-3),
                    Quantity = 1,
                    TotalPrice = 0,
                    PaymentStatus = "Completed",
                    PaymentMethod = "RSVP",
                    PaymentReference = $"RSVP_{Guid.NewGuid().ToString()[..8]}",
                    Notes = "Free RSVP - looking forward to this!"
                });

                _logger.LogInformation("Created free social RSVP for event: {EventTitle}", upcomingSocial.Title);
            }
        }

        // Scenario 3: Past Attended Event
        if (pastEvent != null)
        {
            var pastTicketType = pastEvent.TicketTypes.FirstOrDefault();

            if (pastTicketType != null)
            {
                var purchaseDate = pastEvent.StartDate.AddDays(-7);

                // Calculate price based on ticket type
                decimal totalPrice;
                bool isPaid;
                if (pastTicketType.PricingType == PricingType.SlidingScale)
                {
                    totalPrice = (pastTicketType.DefaultPrice ?? 20m) * 0.5m;
                    isPaid = true;
                }
                else
                {
                    var price = pastTicketType.Price ?? 0;
                    totalPrice = price > 0 ? price * 0.5m : 0;
                    isPaid = price > 0;
                }

                purchasesToAdd.Add(new TicketPurchase
                {
                    Id = Guid.NewGuid(),
                    UserId = vettedUser.Id,
                    TicketTypeId = pastTicketType.Id,
                    PurchaseDate = purchaseDate,
                    Quantity = 1,
                    TotalPrice = totalPrice,
                    PaymentStatus = "Completed",
                    PaymentMethod = isPaid ? "Stripe" : "RSVP",
                    PaymentReference = isPaid ? $"SEED_ORDER_{Guid.NewGuid().ToString()[..8]}" : $"RSVP_{Guid.NewGuid().ToString()[..8]}",
                    Notes = "Attended - great event!"
                });

                _logger.LogInformation("Created past event attendance for event: {EventTitle}", pastEvent.Title);
            }
        }

        // Scenario 4 & 5: Additional upcoming events for comprehensive testing (search, filters, etc.)
        foreach (var additionalEvent in additionalUpcomingEvents.Take(2))
        {
            var ticketType = additionalEvent.TicketTypes.FirstOrDefault();

            if (ticketType != null)
            {
                var isSocialEvent = additionalEvent.EventType == EventType.Social;

                // Calculate price based on ticket type
                decimal price;
                if (isSocialEvent)
                {
                    price = 0; // Free RSVP or donation
                }
                else if (ticketType.PricingType == PricingType.SlidingScale)
                {
                    // Use default price for sliding scale
                    price = ticketType.DefaultPrice ?? 20m;
                }
                else
                {
                    // Fixed price with slight discount
                    price = (ticketType.Price ?? 0) * 0.6m;
                }

                purchasesToAdd.Add(new TicketPurchase
                {
                    Id = Guid.NewGuid(),
                    UserId = vettedUser.Id,
                    TicketTypeId = ticketType.Id,
                    PurchaseDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(2, 10)),
                    Quantity = 1,
                    TotalPrice = price,
                    PaymentStatus = "Completed",
                    PaymentMethod = isSocialEvent ? "RSVP" : "Venmo",
                    PaymentReference = isSocialEvent ? $"RSVP_{Guid.NewGuid().ToString()[..8]}" : $"SEED_ORDER_{Guid.NewGuid().ToString()[..8]}",
                    Notes = isSocialEvent ? null! : "Can't wait for this class!"
                });

                _logger.LogInformation("Created additional event registration for event: {EventTitle}", additionalEvent.Title);
            }
        }

        var createdCount = purchasesToAdd.Count(p => p.UserId == vettedUser.Id);
        _logger.LogInformation("Created {Count} ticket purchases for vetted test user", createdCount);
    }

    /// <summary>
    /// Helper method to get random payment status for realistic purchase data.
    /// Most purchases are Completed (60%), some Pending (20%), few Failed (20%).
    /// </summary>
    private string GetRandomPaymentStatus()
    {
        var statuses = new[] { "Completed", "Completed", "Completed", "Pending", "Failed" };
        return statuses[Random.Shared.Next(statuses.Length)];
    }

    /// <summary>
    /// Helper method to get random payment method for realistic purchase data.
    /// Simulates various payment methods used by members.
    /// </summary>
    private string GetRandomPaymentMethod()
    {
        var methods = new[] { "PayPal", "Stripe", "Venmo", "Cash", "Zelle" };
        return methods[Random.Shared.Next(methods.Length)];
    }

    /// <summary>
    /// Helper method to get random purchase notes for realistic data.
    /// Most purchases have no notes (empty strings), occasional member comments.
    /// </summary>
    private string GetRandomPurchaseNotes()
    {
        var notes = new[]
        {
            "", "", "", // Most purchases have no notes
            "First time attending!",
            "Vegetarian meal preference",
            "Mobility assistance needed",
            "Paid sliding scale minimum",
            "Group purchase for partners"
        };
        return notes[Random.Shared.Next(notes.Length)];
    }

    /// <summary>
    /// Creates historical workshop ticket purchases, check-ins, and cancellations.
    ///
    /// For each workshop event:
    /// - Creates specified number of active ticket purchases by type
    /// - Creates check-in records for attended participants
    /// - Creates one cancelled ticket with refund
    ///
    /// Architecture:
    /// - EventParticipation: Central table for all participation (ParticipationType.Ticket)
    /// - EventAttendee: Registration details linking users to events
    /// - CheckIn: Actual check-in records for those who attended
    /// </summary>
    private async Task CreateHistoricalWorkshopTicketsAsync(
        EventSeeder eventSeeder,
        Guid eventId,
        int daysAgo,
        Dictionary<string, int> purchasesByType,
        Dictionary<string, int> checkInsByType,
        string canceledUserEmail,
        string canceledTicketType,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating historical workshop tickets for event {EventId} ({DaysAgo} days ago)", eventId, daysAgo);

        // 1. Get event details from database
        var evt = await _context.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (evt == null)
        {
            _logger.LogWarning("Event {EventId} not found for historical workshop tickets", eventId);
            return;
        }

        // 2. Get all users for distribution
        var users = await _userManager.Users.ToListAsync(cancellationToken);
        if (users.Count == 0)
        {
            _logger.LogWarning("No users available for historical workshop tickets");
            return;
        }

        // Calculate total tickets needed (including canceled ticket)
        var totalTicketsNeeded = purchasesByType.Values.Sum() + 1; // +1 for canceled ticket
        if (users.Count < totalTicketsNeeded)
        {
            _logger.LogWarning(
                "Not enough unique users ({UserCount}) for event {EventId}. Need {TicketsNeeded} unique users (one per ticket). Skipping historical ticket seeding.",
                users.Count, eventId, totalTicketsNeeded);
            return;
        }

        // Shuffle user list to ensure random but unique distribution for this event
        // This prevents duplicate user assignments to same event
        var shuffledUsers = users.OrderBy(_ => Random.Shared.Next()).ToList();
        int userIndex = 0;
        int globalTicketCounter = 1; // Global counter for all tickets in this event

        // 3. Create active tickets for each ticket type
        foreach (var (ticketTypeName, purchaseCount) in purchasesByType)
        {
            var ticketType = evt.TicketTypes.FirstOrDefault(tt => tt.Name == ticketTypeName);
            if (ticketType == null)
            {
                _logger.LogWarning("Ticket type '{TicketTypeName}' not found for event {EventId}", ticketTypeName, eventId);
                continue;
            }

            var checkInsNeeded = checkInsByType.GetValueOrDefault(ticketTypeName, 0);

            for (int i = 0; i < purchaseCount; i++)
            {
                // Check we haven't run out of unique users
                if (userIndex >= shuffledUsers.Count)
                {
                    _logger.LogWarning(
                        "Not enough unique users for all tickets. Needed: {Needed}, Available: {Available}. Stopping ticket creation.",
                        totalTicketsNeeded, shuffledUsers.Count);
                    break;
                }

                var user = shuffledUsers[userIndex];
                userIndex++;

                var shouldCheckIn = i < checkInsNeeded;
                var purchaseDate = DateTime.UtcNow.AddDays(-(daysAgo + 2 + i));

                // Create EventParticipation (central ticket purchase record)
                var participation = new EventParticipation(evt.Id, user.Id, ParticipationType.Ticket)
                {
                    Id = Guid.NewGuid(),
                    Status = ParticipationStatus.Active,
                    CreatedAt = purchaseDate,
                    UpdatedAt = purchaseDate,
                    CreatedBy = user.Id,
                    UpdatedBy = user.Id,
                    Metadata = $"{{\"ticketType\":\"{ticketTypeName}\",\"price\":{ticketType.Price},\"paymentMethod\":\"Stripe\"}}"
                };
                _context.EventParticipations.Add(participation);

                // Create EventAttendee (registration details)
                var attendee = new EventAttendee(evt.Id, user.Id, "confirmed")
                {
                    Id = Guid.NewGuid(),
                    TicketNumber = $"TKT-{evt.Id.ToString()[..8]}-{globalTicketCounter:D4}",
                    CreatedAt = purchaseDate,
                    UpdatedAt = purchaseDate,
                    CreatedBy = user.Id,
                    UpdatedBy = user.Id,
                    HasCompletedWaiver = true
                };
                _context.EventAttendees.Add(attendee);

                // Create CheckIn record if they attended
                if (shouldCheckIn)
                {
                    var checkInTime = evt.StartDate.AddMinutes(-15); // 15 min before start
                    var checkIn = new CheckIn(attendee.Id, evt.Id, user.Id)
                    {
                        Id = Guid.NewGuid(),
                        CheckInTime = checkInTime,
                        CreatedAt = checkInTime,
                        CreatedBy = user.Id,
                        Notes = $"Checked in for {ticketTypeName}"
                    };
                    _context.CheckIns.Add(checkIn);

                    _logger.LogDebug("Created check-in for user {UserId} at event {EventId} ({TicketType})", user.Id, evt.Id, ticketTypeName);
                }

                globalTicketCounter++; // Increment for every ticket created
            }

            _logger.LogInformation("Created {PurchaseCount} {TicketType} tickets ({CheckInCount} checked in)", purchaseCount, ticketTypeName, checkInsNeeded);
        }

        // 4. Create canceled ticket
        var canceledUser = await _userManager.FindByEmailAsync(canceledUserEmail);
        if (canceledUser == null)
        {
            _logger.LogWarning("Canceled user {Email} not found for event {EventId}", canceledUserEmail, eventId);
        }
        else
        {
            // Check if canceled user was already assigned in the active tickets loop
            // This prevents duplicate user assignments to the same event
            var userAlreadyAssigned = shuffledUsers.Take(userIndex).Any(u => u.Id == canceledUser.Id);

            if (userAlreadyAssigned)
            {
                _logger.LogWarning(
                    "Canceled user {Email} already has a ticket for event {EventId}. Skipping canceled ticket creation to avoid duplicate.",
                    canceledUserEmail, eventId);
            }
            else
            {
                var canceledType = evt.TicketTypes.FirstOrDefault(tt => tt.Name == canceledTicketType);
                if (canceledType != null)
                {
                    var canceledPurchaseDate = DateTime.UtcNow.AddDays(-(daysAgo + 5));
                    var canceledDate = DateTime.UtcNow.AddDays(-(daysAgo + 1));

                    // Create EventAttendee for canceled ticket (to ensure ticket number uniqueness)
                    var canceledAttendee = new EventAttendee(evt.Id, canceledUser.Id, "cancelled")
                    {
                        Id = Guid.NewGuid(),
                        TicketNumber = $"TKT-{evt.Id.ToString()[..8]}-{globalTicketCounter:D4}",
                        CreatedAt = canceledPurchaseDate,
                        UpdatedAt = canceledDate,
                        CreatedBy = canceledUser.Id,
                        UpdatedBy = canceledUser.Id,
                        HasCompletedWaiver = false
                    };
                    _context.EventAttendees.Add(canceledAttendee);

                    // Create canceled EventParticipation (NO CheckIn for canceled)
                    var canceledParticipation = new EventParticipation(evt.Id, canceledUser.Id, ParticipationType.Ticket)
                    {
                        Id = Guid.NewGuid(),
                        Status = ParticipationStatus.Refunded,
                        CreatedAt = canceledPurchaseDate,
                        UpdatedAt = canceledDate,
                        CancelledAt = canceledDate,
                        CancellationReason = "User requested refund",
                        CreatedBy = canceledUser.Id,
                        UpdatedBy = canceledUser.Id,
                        Metadata = $"{{\"ticketType\":\"{canceledTicketType}\",\"price\":{canceledType.Price},\"paymentMethod\":\"Stripe\",\"refundedAt\":\"{canceledDate:O}\"}}"
                    };
                    _context.EventParticipations.Add(canceledParticipation);

                    _logger.LogInformation("Created canceled ticket for user {UserId} (refunded {CanceledDate})", canceledUser.Id, canceledDate);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Historical workshop tickets created for event {EventId}", eventId);
    }

    /// <summary>
    /// Seeds historical workshop ticket purchases.
    /// Called after EventSeeder and SessionTicketSeeder have run.
    /// </summary>
    public async Task SeedHistoricalWorkshopTicketsAsync(EventSeeder eventSeeder, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting historical workshop tickets creation");

        // Check if historical tickets already exist
        var existingHistoricalTickets = await _context.EventParticipations
            .Where(ep => ep.EventId == eventSeeder.AdvancedSuspensionEventId || ep.EventId == eventSeeder.RopeFundamentalsEventId)
            .CountAsync(cancellationToken);

        if (existingHistoricalTickets > 0)
        {
            _logger.LogInformation("Historical workshop tickets already exist ({Count}), skipping", existingHistoricalTickets);
            return;
        }

        // Historical Workshop 1: Advanced Suspension Techniques (75 days ago)
        await CreateHistoricalWorkshopTicketsAsync(
            eventSeeder,
            eventSeeder.AdvancedSuspensionEventId,
            75, // days ago
            new Dictionary<string, int>
            {
                { "Full Workshop Pass", 6 },
                { "Single Session Ticket", 3 },
                { "Early Bird Full Pass", 1 }
            },
            new Dictionary<string, int>
            {
                { "Full Workshop Pass", 5 }, // 5 checked in, 1 no-show
                { "Single Session Ticket", 2 }, // 2 checked in, 1 no-show
                { "Early Bird Full Pass", 1 } // 1 checked in
            },
            "guest@witchcityrope.com", // canceled user
            "Full Workshop Pass", // canceled ticket type
            cancellationToken);

        // Historical Workshop 2: Rope Fundamentals Intensive (60 days ago)
        await CreateHistoricalWorkshopTicketsAsync(
            eventSeeder,
            eventSeeder.RopeFundamentalsEventId,
            60, // days ago
            new Dictionary<string, int>
            {
                { "Full Day Pass", 8 },
                { "Half Day Pass", 3 },
                { "Single Session", 1 }
            },
            new Dictionary<string, int>
            {
                { "Full Day Pass", 6 }, // 6 checked in, 2 no-shows
                { "Half Day Pass", 2 }, // 2 checked in, 1 no-show
                { "Single Session", 0 } // 0 checked in, 1 no-show
            },
            "member@witchcityrope.com", // canceled user
            "Full Day Pass", // canceled ticket type
            cancellationToken);

        _logger.LogInformation("Historical workshop tickets creation completed");
    }
}
