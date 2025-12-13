using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.CheckIn.Entities;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Safety.Services;
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
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<TicketPurchaseSeeder> _logger;

    public TicketPurchaseSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IEncryptionService encryptionService,
        ILogger<TicketPurchaseSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _encryptionService = encryptionService;
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

        // Get ticket types for class events only (social event donations handled by SeedSocialEventDonationTicketsAsync)
        // Include Sessions to create EventAttendance records with SessionId
        var ticketTypes = await _context.TicketTypes
            .Include(t => t.Event)
            .Include(t => t.Sessions)
            .Where(tt => tt.Event.RequireTicketPurchase)
            .ToListAsync(cancellationToken);

        var users = await _userManager.Users.ToListAsync(cancellationToken);
        var purchasesToAdd = new List<TicketPurchase>();
        var attendancesToAdd = new List<EventAttendance>();
        var attendeesToAdd = new List<EventAttendee>();

        // Track user-event combinations to enforce: ONE user = ONE active ticket per event
        // Business Rule: A user can only have ONE active ticket per event
        var userEventTickets = new Dictionary<(Guid userId, Guid eventId), bool>();

        // Track ticket numbers for EventAttendee generation
        var ticketCountersByEvent = new Dictionary<Guid, int>();

        // Create realistic purchase data
        foreach (var ticketType in ticketTypes)
        {
            var purchaseCount = Math.Min(ticketType.Sold, users.Count);

            for (int i = 0; i < purchaseCount; i++)
            {
                var user = users[i % users.Count];
                var eventId = ticketType.Event.Id;
                var userEventKey = (user.Id, eventId);

                // Business Rule Enforcement: ONE user = ONE ticket per event
                // Step 1: Check in-memory tracking first (for tickets added in this loop but not saved yet)
                if (userEventTickets.ContainsKey(userEventKey))
                {
                    _logger.LogDebug("User {UserId} already assigned ticket for event {EventId} in current batch, skipping", user.Id, eventId);
                    continue;
                }

                // Step 2: Check database for existing ticket
                // Query database to ensure user doesn't already have a ticket for this event
                // This catches tickets created by other seeding methods or previous runs
                var existingTicket = await _context.TicketPurchases
                    .Include(tp => tp.TicketType)
                    .FirstOrDefaultAsync(tp =>
                        tp.UserId == user.Id &&
                        tp.TicketType.EventId == eventId,
                        cancellationToken);

                if (existingTicket != null)
                {
                    _logger.LogDebug("User {UserId} already has ticket for event {EventId} in database, skipping", user.Id, eventId);
                    continue;
                }

                // Mark this user as having a ticket for this event (prevent duplicates in this batch)
                userEventTickets[userEventKey] = true;

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

                var paymentMethod = isRSVP ? "Free" : SeedingHelpers.GetRandomPaymentMethod();

                // Payment Status Distribution (PER USER-EVENT, not per ticket):
                // - 80% Completed (most users complete their payment)
                // - 10% Failed (payment declined)
                // - 5% Refunded (user requested refund after successful payment)
                // - 5% PartiallyRefunded (user got partial refund)
                // CRITICAL: Status assigned PER USER-EVENT, ensuring one user = one payment state per event
                var paymentStatus = isRSVP ? "Completed" : GetRandomPaymentStatus();

                var purchase = new TicketPurchase
                {
                    TicketTypeId = ticketType.Id,
                    UserId = user.Id,
                    PurchaseDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
                    Quantity = 1,
                    TotalPrice = totalPrice,
                    PaymentStatus = paymentStatus,
                    PaymentMethod = paymentMethod,
                    PaymentReference = Guid.NewGuid().ToString("N")[..8],
                    Notes = SeedingHelpers.GetRandomPurchaseNotes()
                };

                // Add PayPal fields for PayPal payments
                if (paymentMethod == "PayPal")
                {
                    purchase.EncryptedPayPalOrderId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalOrderId());
                    purchase.EncryptedPayPalCaptureId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalCaptureId());
                    purchase.EncryptedPayPalPayerId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalPayerId());
                }

                purchasesToAdd.Add(purchase);

                // Create EventAttendance and EventAttendee for this purchase
                CreateAttendanceAndAttendee(
                    purchase,
                    eventId,
                    user.Id,
                    ticketType,
                    paymentStatus,
                    paymentMethod,
                    attendancesToAdd,
                    attendeesToAdd,
                    ticketCountersByEvent, "HIST");
            }
        }

        // Create specific ticket purchases for vetted test user for E2E dashboard testing
        await CreateVettedUserTicketPurchasesAsync(purchasesToAdd, attendancesToAdd, attendeesToAdd, ticketCountersByEvent, cancellationToken);

        // Create historical test data for refund eligibility testing (transactions >90 days old)
        await CreateHistoricalTestDataForRefundTestingAsync(purchasesToAdd, attendancesToAdd, attendeesToAdd, ticketCountersByEvent, cancellationToken);

        // Save all entities together in transaction (atomic operation)
        await _context.TicketPurchases.AddRangeAsync(purchasesToAdd, cancellationToken);
        await _context.EventAttendances.AddRangeAsync(attendancesToAdd, cancellationToken);
        await _context.EventAttendees.AddRangeAsync(attendeesToAdd, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Ticket purchases creation completed. Created: {PurchaseCount} purchases, {AttendanceCount} attendances, {AttendeeCount} attendees",
            purchasesToAdd.Count,
            attendancesToAdd.Count,
            attendeesToAdd.Count);
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
        List<EventAttendance> attendancesToAdd,
        List<EventAttendee> attendeesToAdd,
        Dictionary<Guid, int> ticketCountersByEvent,
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
        // Include Sessions for creating EventAttendance records with SessionId
        var upcomingWorkshop = await _context.Events
            .Include(e => e.TicketTypes)
                .ThenInclude(tt => tt.Sessions)
            .Where(e => e.RequireTicketPurchase &&
                       e.StartDate > DateTime.UtcNow &&
                       e.TicketTypes.Any())
            .OrderBy(e => e.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        var upcomingSocial = await _context.Events
            .Include(e => e.TicketTypes)
                .ThenInclude(tt => tt.Sessions)
            .Where(e => e.AllowRsvps && !e.RequireTicketPurchase &&
                       e.StartDate > DateTime.UtcNow &&
                       e.TicketTypes.Any())
            .OrderBy(e => e.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        var pastEvent = await _context.Events
            .Include(e => e.TicketTypes)
                .ThenInclude(tt => tt.Sessions)
            .Where(e => e.EndDate < DateTime.UtcNow &&
                       e.TicketTypes.Any())
            .OrderByDescending(e => e.EndDate)
            .FirstOrDefaultAsync(cancellationToken);

        // Additional upcoming events for comprehensive testing
        var additionalUpcomingEvents = await _context.Events
            .Include(e => e.TicketTypes)
                .ThenInclude(tt => tt.Sessions)
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

                var vettedPaidPurchase = new TicketPurchase
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
                    Notes = "Sliding scale pricing applied",
                    EncryptedPayPalOrderId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalOrderId()),
                    EncryptedPayPalCaptureId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalCaptureId()),
                    EncryptedPayPalPayerId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalPayerId())
                };

                purchasesToAdd.Add(vettedPaidPurchase);

                // Create EventAttendance and EventAttendee for vetted user paid workshop
                CreateAttendanceAndAttendee(
                    vettedPaidPurchase,
                    upcomingWorkshop.Id,
                    vettedUser.Id,
                    paidTicket,
                    "Completed",
                    "PayPal",
                    attendancesToAdd,
                    attendeesToAdd,
                    ticketCountersByEvent,
                    "HIST");

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
                var freePurchase = new TicketPurchase
                {
                    Id = Guid.NewGuid(),
                    UserId = vettedUser.Id,
                    TicketTypeId = freeTicket.Id,
                    PurchaseDate = DateTime.UtcNow.AddDays(-3),
                    Quantity = 1,
                    TotalPrice = 0,
                    PaymentStatus = "Completed",
                    PaymentMethod = "Free",
                    PaymentReference = $"FREE_{Guid.NewGuid().ToString()[..8]}",
                    Notes = "Free RSVP - looking forward to this!"
                };

                purchasesToAdd.Add(freePurchase);

                // Create EventAttendance and EventAttendee for free social event
                CreateAttendanceAndAttendee(
                    freePurchase,
                    upcomingSocial.Id,
                    vettedUser.Id,
                    freeTicket,
                    "Completed",
                    "Free",
                    attendancesToAdd,
                    attendeesToAdd,
                    ticketCountersByEvent, "HIST");

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

                var pastPurchase = new TicketPurchase
                {
                    Id = Guid.NewGuid(),
                    UserId = vettedUser.Id,
                    TicketTypeId = pastTicketType.Id,
                    PurchaseDate = purchaseDate,
                    Quantity = 1,
                    TotalPrice = totalPrice,
                    PaymentStatus = "Completed",
                    PaymentMethod = isPaid ? "PayPal" : "Free",
                    PaymentReference = isPaid ? $"SEED_ORDER_{Guid.NewGuid().ToString()[..8]}" : $"FREE_{Guid.NewGuid().ToString()[..8]}",
                    Notes = "Attended - great event!"
                };

                // Add PayPal fields for paid tickets
                if (isPaid)
                {
                    pastPurchase.EncryptedPayPalOrderId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalOrderId());
                    pastPurchase.EncryptedPayPalCaptureId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalCaptureId());
                    pastPurchase.EncryptedPayPalPayerId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalPayerId());
                }

                purchasesToAdd.Add(pastPurchase);

                // Create EventAttendance and EventAttendee for past event
                CreateAttendanceAndAttendee(
                    pastPurchase,
                    pastEvent.Id,
                    vettedUser.Id,
                    pastTicketType,
                    "Completed",
                    isPaid ? "PayPal" : "Free",
                    attendancesToAdd,
                    attendeesToAdd,
                    ticketCountersByEvent, "HIST");

                _logger.LogInformation("Created past event attendance for event: {EventTitle}", pastEvent.Title);
            }
        }

        // Scenario 4 & 5: Additional upcoming events for comprehensive testing (search, filters, etc.)
        foreach (var additionalEvent in additionalUpcomingEvents.Take(2))
        {
            var ticketType = additionalEvent.TicketTypes.FirstOrDefault();

            if (ticketType != null)
            {
                var isSocialEvent = additionalEvent.AllowRsvps && !additionalEvent.RequireTicketPurchase;

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

                var additionalPurchase = new TicketPurchase
                {
                    Id = Guid.NewGuid(),
                    UserId = vettedUser.Id,
                    TicketTypeId = ticketType.Id,
                    PurchaseDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(2, 10)),
                    Quantity = 1,
                    TotalPrice = price,
                    PaymentStatus = "Completed",
                    PaymentMethod = isSocialEvent ? "Free" : "Venmo",
                    PaymentReference = isSocialEvent ? $"FREE_{Guid.NewGuid().ToString()[..8]}" : $"SEED_ORDER_{Guid.NewGuid().ToString()[..8]}",
                    Notes = isSocialEvent ? null! : "Can't wait for this class!"
                };

                purchasesToAdd.Add(additionalPurchase);

                // Create EventAttendance and EventAttendee for additional event
                CreateAttendanceAndAttendee(
                    additionalPurchase,
                    additionalEvent.Id,
                    vettedUser.Id,
                    ticketType,
                    "Completed",
                    isSocialEvent ? "Free" : "Venmo",
                    attendancesToAdd,
                    attendeesToAdd,
                    ticketCountersByEvent, "HIST");

                _logger.LogInformation("Created additional event registration for event: {EventTitle}", additionalEvent.Title);
            }
        }

        var createdCount = purchasesToAdd.Count(p => p.UserId == vettedUser.Id);
        _logger.LogInformation("Created {Count} ticket purchases for vetted test user", createdCount);
    }

    /// <summary>
    /// Creates historical test data for E2E testing of refund eligibility (90-day rule).
    ///
    /// Creates 2-3 ticket purchases older than 90 days to validate:
    /// - Backend correctly sets isRefundable=false for old transactions
    /// - Frontend correctly hides refund button for old transactions
    /// - Actions column shows "—" placeholder for non-refundable items
    /// </summary>
    private async Task CreateHistoricalTestDataForRefundTestingAsync(
        List<TicketPurchase> purchasesToAdd,
        List<EventAttendance> attendancesToAdd,
        List<EventAttendee> attendeesToAdd,
        Dictionary<Guid, int> ticketCountersByEvent,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating historical test data (>90 days old) for refund eligibility E2E testing");

        // Get admin user for historical purchases
        var adminUser = await _userManager.FindByEmailAsync("admin@witchcityrope.com");
        if (adminUser == null)
        {
            _logger.LogWarning("Admin user not found, skipping historical test data creation");
            return;
        }

        // Find any past event with paid tickets
        // Include Sessions for creating EventAttendance records with SessionId
        var pastEvent = await _context.Events
            .Include(e => e.TicketTypes)
                .ThenInclude(tt => tt.Sessions)
            .Where(e => e.EndDate < DateTime.UtcNow &&
                       e.TicketTypes.Any(tt => tt.Price > 0))
            .OrderByDescending(e => e.EndDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (pastEvent == null)
        {
            _logger.LogWarning("No past events found for historical test data");
            return;
        }

        var paidTicket = pastEvent.TicketTypes.FirstOrDefault(tt => tt.Price > 0);
        if (paidTicket == null)
        {
            _logger.LogWarning("No paid tickets found for past event");
            return;
        }

        // Get additional users for diverse test data
        var teacherUser = await _userManager.FindByEmailAsync("teacher@witchcityrope.com");
        var vettedUser = await _userManager.FindByEmailAsync("vetted@witchcityrope.com");
        var memberUser = await _userManager.FindByEmailAsync("member@witchcityrope.com");

        // Create diverse historical purchases with different statuses and users
        // Format: (daysAgo, user, paymentStatus, refundAmount, notes)
        var historicalPurchases = new[]
        {
            // Old completed purchases (can't be refunded - >90 days)
            (95, adminUser, "Completed", 0m, "Historical completed - 95 days old"),
            (120, teacherUser, "Completed", 0m, "Historical completed - 120 days old"),

            // Full refunds (different ages)
            (30, vettedUser, "Refunded", paidTicket.Price ?? 25m, "Full refund - 30 days old"),
            (60, memberUser, "Refunded", paidTicket.Price ?? 25m, "Full refund - 60 days old"),

            // Partial refunds (different amounts)
            (15, adminUser, "PartiallyRefunded", (paidTicket.Price ?? 25m) * 0.5m, "Partial refund (50%) - 15 days old"),
            (45, teacherUser, "PartiallyRefunded", (paidTicket.Price ?? 25m) * 0.3m, "Partial refund (30%) - 45 days old"),
        };

        foreach (var (daysAgo, user, status, refundAmount, notes) in historicalPurchases)
        {
            if (user == null) continue; // Skip if user not found

            var purchaseDate = DateTime.UtcNow.AddDays(-daysAgo);

            var historicalPurchase = new TicketPurchase
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TicketTypeId = paidTicket.Id,
                PurchaseDate = purchaseDate,
                Quantity = 1,
                TotalPrice = paidTicket.Price ?? 25m,
                PaymentStatus = status,
                PaymentMethod = "PayPal",
                PaymentReference = $"HIST_{Guid.NewGuid().ToString()[..8]}",
                Notes = notes,
                CreatedAt = purchaseDate,
                UpdatedAt = purchaseDate,
                EncryptedPayPalOrderId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalOrderId()),
                EncryptedPayPalCaptureId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalCaptureId()),
                EncryptedPayPalPayerId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalPayerId())
            };

            purchasesToAdd.Add(historicalPurchase);

            // Check if EventAttendee already exists (user might already have a ticket for this event)
            // Check both database AND in-memory list (in case we're creating multiple for same user in this batch)
            var existingAttendeeInDb = await _context.EventAttendees
                .AnyAsync(ea => ea.EventId == pastEvent.Id && ea.UserId == user.Id, cancellationToken);
            var existingAttendeeInList = attendeesToAdd.Any(ea => ea.EventId == pastEvent.Id && ea.UserId == user.Id);
            var existingAttendee = existingAttendeeInDb || existingAttendeeInList;

            // Check if EventAttendance with Ticket type already exists for this user+event
            // Users can only have ONE Ticket attendance per event (unique constraint: UQ_EventAttendances_User_Event_Type_Active)
            var existingTicketAttendanceInDb = await _context.EventAttendances
                .AnyAsync(ea => ea.EventId == pastEvent.Id && ea.UserId == user.Id && ea.AttendanceType == AttendanceType.Ticket, cancellationToken);
            var existingTicketAttendanceInList = attendancesToAdd
                .Any(ea => ea.EventId == pastEvent.Id && ea.UserId == user.Id && ea.AttendanceType == AttendanceType.Ticket);
            var existingTicketAttendance = existingTicketAttendanceInDb || existingTicketAttendanceInList;

            if (existingTicketAttendance)
            {
                // User already has a Ticket attendance - just link the purchase without creating another attendance
                _logger.LogDebug(
                    "Skipping EventAttendance creation for user {Email} - already has Ticket attendance for this event. Purchase will still be recorded.",
                    user.Email);
            }
            else if (existingAttendee)
            {
                // User has EventAttendee but no Ticket attendance yet - create only EventAttendance
                if (status != "Failed")
                {
                    var attendanceStatus = status switch
                    {
                        "Completed" => AttendanceStatus.Active,
                        "PartiallyRefunded" => AttendanceStatus.Active,
                        "Refunded" => AttendanceStatus.Refunded,
                        _ => AttendanceStatus.Active
                    };

                    // Create EventAttendance records - one per session (matching real behavior)
                    // Note: For historical test data, we use the paidTicket's sessions if available
                    if (paidTicket.Sessions != null && paidTicket.Sessions.Any())
                    {
                        foreach (var session in paidTicket.Sessions)
                        {
                            var attendance = new EventAttendance(pastEvent.Id, user.Id, AttendanceType.Ticket)
                            {
                                Id = Guid.NewGuid(),
                                SessionId = session.Id, // CRITICAL: Set SessionId for each session
                                Status = attendanceStatus,
                                TicketPurchaseId = historicalPurchase.Id,
                                CreatedAt = purchaseDate,
                                UpdatedAt = purchaseDate,
                                CreatedBy = user.Id,
                                UpdatedBy = user.Id,
                                Metadata = $"{{\"ticketType\":\"{paidTicket.Name}\",\"price\":{historicalPurchase.TotalPrice},\"paymentMethod\":\"PayPal\"}}"
                            };

                            if (status == "Refunded")
                            {
                                attendance.CancelledAt = purchaseDate.AddDays(Random.Shared.Next(1, 7));
                                attendance.CancellationReason = "User requested refund";
                            }

                            attendancesToAdd.Add(attendance);
                        }
                    }
                    else
                    {
                        // Legacy: No sessions - create single attendance without SessionId
                        var attendance = new EventAttendance(pastEvent.Id, user.Id, AttendanceType.Ticket)
                        {
                            Id = Guid.NewGuid(),
                            Status = attendanceStatus,
                            TicketPurchaseId = historicalPurchase.Id,
                            CreatedAt = purchaseDate,
                            UpdatedAt = purchaseDate,
                            CreatedBy = user.Id,
                            UpdatedBy = user.Id,
                            Metadata = $"{{\"ticketType\":\"{paidTicket.Name}\",\"price\":{historicalPurchase.TotalPrice},\"paymentMethod\":\"PayPal\"}}"
                        };

                        if (status == "Refunded")
                        {
                            attendance.CancelledAt = purchaseDate.AddDays(Random.Shared.Next(1, 7));
                            attendance.CancellationReason = "User requested refund";
                        }

                        attendancesToAdd.Add(attendance);
                    }

                    _logger.LogDebug("Created EventAttendance for existing attendee: {Email}", user.Email);
                }
            }
            else
            {
                // No existing EventAttendee or EventAttendance - create both
                CreateAttendanceAndAttendee(
                    historicalPurchase,
                    pastEvent.Id,
                    user.Id,
                    paidTicket,
                    status,
                    "PayPal",
                    attendancesToAdd,
                    attendeesToAdd,
                    ticketCountersByEvent,
                    "HIST");
            }

            _logger.LogInformation(
                "Created historical test purchase: {Status}, {DaysAgo} days old for user {Email} (EventAttendee existed: {ExistingAttendee})",
                status,
                daysAgo,
                user.Email,
                existingAttendee);
        }

        _logger.LogInformation("Created {Count} historical test purchases for refund eligibility testing (2 Completed, 2 Refunded, 2 PartiallyRefunded)", historicalPurchases.Length);
    }

    /// <summary>
    /// Helper method to create EventAttendance and EventAttendee records for a TicketPurchase.
    /// Centralizes attendance/attendee creation logic to reduce code duplication.
    /// Creates one EventAttendance per session (matching real ticket purchase behavior).
    /// </summary>
    /// <param name="ticketType">The ticket type with Sessions loaded to create attendance per session</param>
    /// <param name="ticketPrefix">Unique prefix for ticket numbers (e.g., "HIST", "CLASS", "SOCIAL") to prevent collisions between seeding methods</param>
    private void CreateAttendanceAndAttendee(
        TicketPurchase purchase,
        Guid eventId,
        Guid userId,
        TicketType ticketType,
        string paymentStatus,
        string paymentMethod,
        List<EventAttendance> attendancesToAdd,
        List<EventAttendee> attendeesToAdd,
        Dictionary<Guid, int> ticketCountersByEvent,
        string ticketPrefix = "TKT")
    {
        // Only create attendance for non-Failed payments
        if (paymentStatus == "Failed")
        {
            return;
        }

        // Determine attendance status based on payment status
        var attendanceStatus = paymentStatus switch
        {
            "Completed" => AttendanceStatus.Active,
            "PartiallyRefunded" => AttendanceStatus.Active,
            "Refunded" => AttendanceStatus.Refunded,
            _ => AttendanceStatus.Active
        };

        // Create EventAttendance records - one per session (matching real behavior in AttendanceService.cs)
        if (ticketType.Sessions != null && ticketType.Sessions.Any())
        {
            // Ticket has specific sessions - create one attendance per session
            foreach (var session in ticketType.Sessions)
            {
                var attendance = new EventAttendance(eventId, userId, AttendanceType.Ticket)
                {
                    Id = Guid.NewGuid(),
                    SessionId = session.Id, // CRITICAL: Set SessionId for each session
                    Status = attendanceStatus,
                    TicketPurchaseId = purchase.Id,
                    CreatedAt = purchase.PurchaseDate,
                    UpdatedAt = purchase.PurchaseDate,
                    CreatedBy = userId,
                    UpdatedBy = userId,
                    Metadata = $"{{\"ticketType\":\"{ticketType.Name}\",\"price\":{purchase.TotalPrice},\"paymentMethod\":\"{paymentMethod}\"}}"
                };

                // If refunded, set cancellation details
                if (paymentStatus == "Refunded")
                {
                    attendance.CancelledAt = purchase.PurchaseDate.AddDays(Random.Shared.Next(1, 7));
                    attendance.CancellationReason = "User requested refund";
                }

                attendancesToAdd.Add(attendance);
            }
        }
        else
        {
            // Legacy: Ticket has no specific sessions - create single attendance without SessionId
            var attendance = new EventAttendance(eventId, userId, AttendanceType.Ticket)
            {
                Id = Guid.NewGuid(),
                Status = attendanceStatus,
                TicketPurchaseId = purchase.Id,
                CreatedAt = purchase.PurchaseDate,
                UpdatedAt = purchase.PurchaseDate,
                CreatedBy = userId,
                UpdatedBy = userId,
                Metadata = $"{{\"ticketType\":\"{ticketType.Name}\",\"price\":{purchase.TotalPrice},\"paymentMethod\":\"{paymentMethod}\"}}"
            };

            // If refunded, set cancellation details
            if (paymentStatus == "Refunded")
            {
                attendance.CancelledAt = purchase.PurchaseDate.AddDays(Random.Shared.Next(1, 7));
                attendance.CancellationReason = "User requested refund";
            }

            attendancesToAdd.Add(attendance);
        }

        // Create EventAttendee record (registration details) - one per purchase
        if (!ticketCountersByEvent.ContainsKey(eventId))
        {
            ticketCountersByEvent[eventId] = 1;
        }
        else
        {
            ticketCountersByEvent[eventId]++;
        }

        var ticketCounter = ticketCountersByEvent[eventId];
        var attendeeStatus = paymentStatus == "Refunded" ? "cancelled" : "confirmed";

        var attendee = new EventAttendee(eventId, userId, attendeeStatus)
        {
            Id = Guid.NewGuid(),
            TicketNumber = $"{ticketPrefix}-{eventId.ToString()[..8]}-{ticketCounter:D4}",
            CreatedAt = purchase.PurchaseDate,
            UpdatedAt = purchase.PurchaseDate,
            CreatedBy = userId,
            UpdatedBy = userId,
            HasCompletedWaiver = paymentStatus != "Refunded"
        };

        attendeesToAdd.Add(attendee);
    }

    /// <summary>
    /// Helper method to get random payment status for realistic purchase data.
    /// Distribution (per user-event, NOT per ticket):
    /// - Completed: 80% (most users complete their payment)
    /// - Failed: 10% (payment declined - user has no active ticket)
    /// - Refunded: 5% (user requested refund after successful payment - no active ticket)
    /// - PartiallyRefunded: 5% (user got partial refund - still has active ticket)
    ///
    /// Business Rule: ONE user = ONE payment state per event
    /// This ensures no duplicate active tickets for same user-event combination
    /// </summary>
    private string GetRandomPaymentStatus()
    {
        var statuses = new[]
        {
            // 80% Completed
            "Completed", "Completed", "Completed", "Completed",
            "Completed", "Completed", "Completed", "Completed",
            "Completed", "Completed", "Completed", "Completed",
            "Completed", "Completed", "Completed", "Completed",

            // 10% Failed
            "Failed", "Failed",

            // 5% Refunded
            "Refunded",

            // 5% PartiallyRefunded
            "PartiallyRefunded"
        };
        return statuses[Random.Shared.Next(statuses.Length)];
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
    /// - EventParticipation: Central table for all participation (AttendanceType.Ticket)
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

        // 1. Get event details from database (include Sessions for CheckIn and EventAttendance)
        var evt = await _context.Events
            .Include(e => e.TicketTypes)
                .ThenInclude(tt => tt.Sessions)
            .Include(e => e.Sessions)
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

        // Get admin user for check-in staff member
        var adminUser = await _userManager.FindByEmailAsync("admin@witchcityrope.com");
        if (adminUser == null)
        {
            _logger.LogWarning("Admin user not found - check-ins will use system user");
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

        // Track user-event combinations to enforce business rule: ONE user = ONE ticket per event
        var userEventTickets = new Dictionary<Guid, string>(); // userId -> payment status

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

                // Business Rule Enforcement: Check database for existing ticket
                // Query database to ensure user doesn't already have a ticket for this event
                // This works even if tickets were created by other seeding methods
                var existingTicket = await _context.TicketPurchases
                    .Include(tp => tp.TicketType)
                    .FirstOrDefaultAsync(tp =>
                        tp.UserId == user.Id &&
                        tp.TicketType.EventId == evt.Id,
                        cancellationToken);

                if (existingTicket != null)
                {
                    _logger.LogDebug("User {UserId} already has ticket for event {EventId}, skipping", user.Id, evt.Id);
                    userIndex++;
                    continue;
                }

                // Also check in-memory tracking for this method's iterations
                if (userEventTickets.ContainsKey(user.Id))
                {
                    userIndex++;
                    continue;
                }

                var shouldCheckIn = i < checkInsNeeded;
                var purchaseDate = DateTime.UtcNow.AddDays(-(daysAgo + 2 + i));

                // Assign payment status PER USER-EVENT (not per ticket)
                // This ensures one user has only ONE payment state for this event
                var paymentStatus = GetRandomPaymentStatus();
                userEventTickets[user.Id] = paymentStatus;

                userIndex++;

                // Create TicketPurchase FIRST (so we have the ID for linking)
                var ticketPurchase = new TicketPurchase
                {
                    Id = Guid.NewGuid(),
                    TicketTypeId = ticketType.Id,
                    UserId = user.Id,
                    Quantity = 1,
                    TotalPrice = ticketType.Price ?? 0m,
                    PaymentStatus = paymentStatus, // Use status assigned for this user-event
                    PaymentMethod = "PayPal",
                    PaymentReference = $"HIST-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                    PurchaseDate = purchaseDate,
                    CreatedAt = purchaseDate,
                    UpdatedAt = purchaseDate,
                    EncryptedPayPalOrderId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalOrderId()),
                    EncryptedPayPalCaptureId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalCaptureId()),
                    EncryptedPayPalPayerId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalPayerId())
                };
                _context.TicketPurchases.Add(ticketPurchase);

                // Create EventAttendance records - one per session (matching real behavior)
                if (ticketType.Sessions != null && ticketType.Sessions.Any())
                {
                    // Ticket has specific sessions - create one attendance per session
                    foreach (var session in ticketType.Sessions)
                    {
                        var participation = new EventAttendance(evt.Id, user.Id, AttendanceType.Ticket)
                        {
                            Id = Guid.NewGuid(),
                            SessionId = session.Id, // CRITICAL: Set SessionId for each session
                            Status = AttendanceStatus.Active,
                            TicketPurchaseId = ticketPurchase.Id, // CRITICAL: Link to purchase
                            CreatedAt = purchaseDate,
                            UpdatedAt = purchaseDate,
                            CreatedBy = user.Id,
                            UpdatedBy = user.Id,
                            Metadata = $"{{\"ticketType\":\"{ticketTypeName}\",\"price\":{ticketType.Price},\"paymentMethod\":\"Stripe\"}}"
                        };
                        _context.EventAttendances.Add(participation);
                    }
                }
                else
                {
                    // Legacy: Ticket has no specific sessions - create single attendance without SessionId
                    var participation = new EventAttendance(evt.Id, user.Id, AttendanceType.Ticket)
                    {
                        Id = Guid.NewGuid(),
                        Status = AttendanceStatus.Active,
                        TicketPurchaseId = ticketPurchase.Id, // CRITICAL: Link to purchase
                        CreatedAt = purchaseDate,
                        UpdatedAt = purchaseDate,
                        CreatedBy = user.Id,
                        UpdatedBy = user.Id,
                        Metadata = $"{{\"ticketType\":\"{ticketTypeName}\",\"price\":{ticketType.Price},\"paymentMethod\":\"Stripe\"}}"
                    };
                    _context.EventAttendances.Add(participation);
                }

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
                    // Determine which session to check in to:
                    // - If ticket has specific session(s), use the first one
                    // - If ticket is multi-session (no Sessions), use first session of event
                    Guid sessionId;
                    if (ticketType.Sessions.Any())
                    {
                        sessionId = ticketType.Sessions.OrderBy(s => s.StartTime).First().Id;
                    }
                    else
                    {
                        // Multi-session ticket - use first session
                        var firstSession = evt.Sessions.OrderBy(s => s.StartTime).FirstOrDefault();
                        if (firstSession == null)
                        {
                            _logger.LogWarning("Event {EventId} has no sessions - cannot create CheckIn for user {UserId}", evt.Id, user.Id);
                            continue; // Skip check-in creation if no sessions exist
                        }
                        sessionId = firstSession.Id;
                    }

                    var checkInTime = evt.StartDate.AddMinutes(-15); // 15 min before start
                    var staffMemberId = adminUser?.Id ?? user.Id; // Use admin as staff, fallback to user if admin not found
                    var checkIn = new CheckIn(attendee.Id, evt.Id, sessionId, staffMemberId)
                    {
                        Id = Guid.NewGuid(),
                        CheckInTime = checkInTime,
                        CreatedAt = checkInTime,
                        CreatedBy = staffMemberId,
                        Notes = $"Checked in for {ticketTypeName}"
                    };
                    _context.CheckIns.Add(checkIn);

                    _logger.LogDebug("Created check-in for user {UserId} at event {EventId} session {SessionId} ({TicketType}) by staff {StaffId}",
                        user.Id, evt.Id, sessionId, ticketTypeName, staffMemberId);
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

                    // Create TicketPurchase FIRST for canceled ticket
                    var canceledPurchase = new TicketPurchase
                    {
                        Id = Guid.NewGuid(),
                        TicketTypeId = canceledType.Id,
                        UserId = canceledUser.Id,
                        Quantity = 1,
                        TotalPrice = canceledType.Price ?? 0m,
                        PaymentStatus = "Refunded",
                        PaymentMethod = "PayPal",
                        PaymentReference = $"REFUND-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                        PurchaseDate = canceledPurchaseDate,
                        CreatedAt = canceledPurchaseDate,
                        UpdatedAt = canceledDate,
                        EncryptedPayPalOrderId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalOrderId()),
                        EncryptedPayPalCaptureId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalCaptureId()),
                        EncryptedPayPalPayerId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalPayerId())
                    };
                    _context.TicketPurchases.Add(canceledPurchase);

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

                    // Create canceled EventAttendance records - one per session (matching real behavior)
                    if (canceledType.Sessions != null && canceledType.Sessions.Any())
                    {
                        // Ticket has specific sessions - create one attendance per session
                        foreach (var session in canceledType.Sessions)
                        {
                            var canceledParticipation = new EventAttendance(evt.Id, canceledUser.Id, AttendanceType.Ticket)
                            {
                                Id = Guid.NewGuid(),
                                SessionId = session.Id, // CRITICAL: Set SessionId for each session
                                Status = AttendanceStatus.Refunded,
                                TicketPurchaseId = canceledPurchase.Id, // CRITICAL: Link to purchase
                                CreatedAt = canceledPurchaseDate,
                                UpdatedAt = canceledDate,
                                CancelledAt = canceledDate,
                                CancellationReason = "User requested refund",
                                CreatedBy = canceledUser.Id,
                                UpdatedBy = canceledUser.Id,
                                Metadata = $"{{\"ticketType\":\"{canceledTicketType}\",\"price\":{canceledType.Price},\"paymentMethod\":\"Stripe\",\"refundedAt\":\"{canceledDate:O}\"}}"
                            };
                            _context.EventAttendances.Add(canceledParticipation);
                        }
                    }
                    else
                    {
                        // Legacy: Ticket has no specific sessions - create single attendance without SessionId
                        var canceledParticipation = new EventAttendance(evt.Id, canceledUser.Id, AttendanceType.Ticket)
                        {
                            Id = Guid.NewGuid(),
                            Status = AttendanceStatus.Refunded,
                            TicketPurchaseId = canceledPurchase.Id, // CRITICAL: Link to purchase
                            CreatedAt = canceledPurchaseDate,
                            UpdatedAt = canceledDate,
                            CancelledAt = canceledDate,
                            CancellationReason = "User requested refund",
                            CreatedBy = canceledUser.Id,
                            UpdatedBy = canceledUser.Id,
                            Metadata = $"{{\"ticketType\":\"{canceledTicketType}\",\"price\":{canceledType.Price},\"paymentMethod\":\"Stripe\",\"refundedAt\":\"{canceledDate:O}\"}}"
                        };
                        _context.EventAttendances.Add(canceledParticipation);
                    }

                    _logger.LogInformation("Created canceled ticket for user {UserId} (refunded {CanceledDate})", canceledUser.Id, canceledDate);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Historical workshop tickets created for event {EventId}", eventId);
    }

    /// <summary>
    /// Seeds ticket purchases for specific class events that require custom ticket counts.
    /// This method creates tickets for events like Suspension Basics, Introduction to Rope Safety, etc.
    /// that have specific seeding requirements beyond the general SeedTicketPurchasesAsync logic.
    ///
    /// Idempotent operation - checks database for existing tickets before creating.
    /// </summary>
    public async Task SeedSpecificClassEventTicketsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting specific class event ticket purchases creation");

        // Get all users for distribution
        var users = await _userManager.Users.ToListAsync(cancellationToken);
        if (users.Count == 0)
        {
            _logger.LogWarning("No users available for class event tickets");
            return;
        }

        var purchasesToAdd = new List<TicketPurchase>();
        var attendancesToAdd = new List<EventAttendance>();
        var attendeesToAdd = new List<EventAttendee>();
        var ticketCountersByEvent = new Dictionary<Guid, int>();

        // Get class events that need specific ticket counts
        // Include Sessions for creating EventAttendance records with SessionId
        var classEvents = await _context.Events
            .Include(e => e.TicketTypes)
                .ThenInclude(tt => tt.Sessions)
            .Where(e => e.RequireTicketPurchase)
            .ToListAsync(cancellationToken);

        foreach (var eventItem in classEvents)
        {
            // Special handling for Suspension Basics: Multiple ticket types
            if (eventItem.Title.Contains("Suspension Basics"))
            {
                await CreateSuspensionBasicsTicketsAsync(
                    eventItem,
                    users,
                    purchasesToAdd,
                    attendancesToAdd,
                    attendeesToAdd,
                    ticketCountersByEvent,
                    cancellationToken);
            }
            else
            {
                // General class event ticket creation with event-specific counts
                await CreateGeneralClassEventTicketsAsync(
                    eventItem,
                    users,
                    purchasesToAdd,
                    attendancesToAdd,
                    attendeesToAdd,
                    ticketCountersByEvent,
                    cancellationToken);
            }
        }

        if (purchasesToAdd.Count > 0)
        {
            // Save all entities together in transaction
            await _context.TicketPurchases.AddRangeAsync(purchasesToAdd, cancellationToken);
            await _context.EventAttendances.AddRangeAsync(attendancesToAdd, cancellationToken);
            await _context.EventAttendees.AddRangeAsync(attendeesToAdd, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Specific class event tickets creation completed. Created: {PurchaseCount} purchases, {AttendanceCount} attendances, {AttendeeCount} attendees",
                purchasesToAdd.Count,
                attendancesToAdd.Count,
                attendeesToAdd.Count);
        }
        else
        {
            _logger.LogInformation("No new class event tickets needed - all users already have tickets");
        }
    }

    /// <summary>
    /// Creates ticket purchases for Suspension Basics event with multiple ticket types.
    /// Creates 2 "All 2 Days" tickets and 4 "Day 1 Only" tickets using different users.
    /// </summary>
    private async Task CreateSuspensionBasicsTicketsAsync(
        Event eventItem,
        List<ApplicationUser> users,
        List<TicketPurchase> purchasesToAdd,
        List<EventAttendance> attendancesToAdd,
        List<EventAttendee> attendeesToAdd,
        Dictionary<Guid, int> ticketCountersByEvent,
        CancellationToken cancellationToken)
    {
        // Create 2 "All 2 Days" tickets
        var all2DaysTicket = eventItem.TicketTypes.FirstOrDefault(tt => tt.Name == "All 2 Days");
        if (all2DaysTicket != null)
        {
            for (int i = 0; i < 2 && i < users.Count; i++)
            {
                var user = users[i];

                // Check if user already has ticket for this event
                var existingTicket = await _context.TicketPurchases
                    .Include(tp => tp.TicketType)
                    .FirstOrDefaultAsync(tp =>
                        tp.UserId == user.Id &&
                        tp.TicketType.EventId == eventItem.Id,
                        cancellationToken);

                if (existingTicket != null)
                {
                    _logger.LogDebug("User {UserId} already has ticket for Suspension Basics, skipping", user.Id);
                    continue;
                }

                var purchaseAmount = (decimal)Random.Shared.Next(40, 80);
                var purchaseDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 20));

                var ticketPurchase = new TicketPurchase
                {
                    Id = Guid.NewGuid(),
                    TicketTypeId = all2DaysTicket.Id,
                    UserId = user.Id,
                    Quantity = 1,
                    TotalPrice = purchaseAmount,
                    PaymentStatus = "Completed",
                    PaymentMethod = "PayPal",
                    PaymentReference = $"PP-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                    PurchaseDate = purchaseDate,
                    CreatedAt = purchaseDate,
                    UpdatedAt = purchaseDate,
                    EncryptedPayPalOrderId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalOrderId()),
                    EncryptedPayPalCaptureId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalCaptureId()),
                    EncryptedPayPalPayerId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalPayerId())
                };

                purchasesToAdd.Add(ticketPurchase);

                // Create EventAttendance and EventAttendee
                CreateAttendanceAndAttendee(
                    ticketPurchase,
                    eventItem.Id,
                    user.Id,
                    all2DaysTicket,
                    "Completed",
                    "PayPal",
                    attendancesToAdd,
                    attendeesToAdd,
                    ticketCountersByEvent, "CLASS");

                _logger.LogDebug("Created 'All 2 Days' ticket for user {UserId}", user.Id);
            }
        }

        // Create 4 "Day 1 Only" tickets (using different users)
        var day1OnlyTicket = eventItem.TicketTypes.FirstOrDefault(tt => tt.Name == "Day 1 Only");
        if (day1OnlyTicket != null)
        {
            for (int i = 2; i < 6 && i < users.Count; i++) // Start at index 2 to use different users
            {
                var user = users[i];

                // Check if user already has ticket for this event
                var existingTicket = await _context.TicketPurchases
                    .Include(tp => tp.TicketType)
                    .FirstOrDefaultAsync(tp =>
                        tp.UserId == user.Id &&
                        tp.TicketType.EventId == eventItem.Id,
                        cancellationToken);

                if (existingTicket != null)
                {
                    _logger.LogDebug("User {UserId} already has ticket for Suspension Basics, skipping", user.Id);
                    continue;
                }

                var purchaseAmount = (decimal)Random.Shared.Next(20, 50);
                var purchaseDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 20));

                var ticketPurchase = new TicketPurchase
                {
                    Id = Guid.NewGuid(),
                    TicketTypeId = day1OnlyTicket.Id,
                    UserId = user.Id,
                    Quantity = 1,
                    TotalPrice = purchaseAmount,
                    PaymentStatus = "Completed",
                    PaymentMethod = SeedingHelpers.GetRandomPaymentMethod(),
                    PaymentReference = $"PP-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                    PurchaseDate = purchaseDate,
                    CreatedAt = purchaseDate,
                    UpdatedAt = purchaseDate
                };

                // Add PayPal fields if PayPal payment
                if (ticketPurchase.PaymentMethod == "PayPal")
                {
                    ticketPurchase.EncryptedPayPalOrderId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalOrderId());
                    ticketPurchase.EncryptedPayPalCaptureId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalCaptureId());
                    ticketPurchase.EncryptedPayPalPayerId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalPayerId());
                }

                purchasesToAdd.Add(ticketPurchase);

                // Create EventAttendance and EventAttendee
                CreateAttendanceAndAttendee(
                    ticketPurchase,
                    eventItem.Id,
                    user.Id,
                    day1OnlyTicket,
                    "Completed",
                    ticketPurchase.PaymentMethod,
                    attendancesToAdd,
                    attendeesToAdd,
                    ticketCountersByEvent, "CLASS");

                _logger.LogDebug("Created 'Day 1 Only' ticket for user {UserId}", user.Id);
            }
        }

        _logger.LogInformation("Created Suspension Basics tickets");
    }

    /// <summary>
    /// Creates ticket purchases for general class events with event-specific ticket counts.
    /// </summary>
    private async Task CreateGeneralClassEventTicketsAsync(
        Event eventItem,
        List<ApplicationUser> users,
        List<TicketPurchase> purchasesToAdd,
        List<EventAttendance> attendancesToAdd,
        List<EventAttendee> attendeesToAdd,
        Dictionary<Guid, int> ticketCountersByEvent,
        CancellationToken cancellationToken)
    {
        // Determine ticket count based on event title
        var ticketCount = eventItem.Title.Contains("Introduction to Rope Safety") ? 5 :
                         eventItem.Title.Contains("Advanced Floor Work") ? 3 : 2;

        var ticketType = eventItem.TicketTypes.FirstOrDefault();
        if (ticketType == null)
        {
            _logger.LogWarning("No ticket types found for event {EventTitle}, skipping", eventItem.Title);
            return;
        }

        for (int i = 0; i < Math.Min(ticketCount, users.Count); i++)
        {
            var user = users[i];

            // Check if user already has ticket for this event
            var existingTicket = await _context.TicketPurchases
                .Include(tp => tp.TicketType)
                .FirstOrDefaultAsync(tp =>
                    tp.UserId == user.Id &&
                    tp.TicketType.EventId == eventItem.Id,
                    cancellationToken);

            if (existingTicket != null)
            {
                _logger.LogDebug("User {UserId} already has ticket for event {EventId}, skipping", user.Id, eventItem.Id);
                continue;
            }

            var purchaseAmount = (decimal)Random.Shared.Next(15, 65);
            var purchaseDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 20));
            var paymentMethod = SeedingHelpers.GetRandomPaymentMethod();

            var ticketPurchase = new TicketPurchase
            {
                Id = Guid.NewGuid(),
                TicketTypeId = ticketType.Id,
                UserId = user.Id,
                Quantity = 1,
                TotalPrice = purchaseAmount,
                PaymentStatus = "Completed",
                PaymentMethod = paymentMethod,
                PaymentReference = $"PP-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                PurchaseDate = purchaseDate,
                CreatedAt = purchaseDate,
                UpdatedAt = purchaseDate,
                Notes = SeedingHelpers.GetRandomPurchaseNotes()
            };

            // Add PayPal fields if PayPal payment
            if (paymentMethod == "PayPal")
            {
                ticketPurchase.EncryptedPayPalOrderId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalOrderId());
                ticketPurchase.EncryptedPayPalCaptureId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalCaptureId());
                ticketPurchase.EncryptedPayPalPayerId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalPayerId());
            }

            purchasesToAdd.Add(ticketPurchase);

            // Create EventAttendance and EventAttendee
            CreateAttendanceAndAttendee(
                ticketPurchase,
                eventItem.Id,
                user.Id,
                ticketType,
                "Completed",
                paymentMethod,
                attendancesToAdd,
                attendeesToAdd,
                ticketCountersByEvent, "CLASS");
        }

        _logger.LogInformation("Created {Count} tickets for event {EventTitle}", ticketCount, eventItem.Title);
    }

    /// <summary>
    /// Seeds donation ticket purchases for social events.
    /// Some attendees of social events make optional donations, creating a TicketPurchase + Ticket attendance
    /// in ADDITION to their free RSVP attendance (which is created by AttendanceSeeder).
    ///
    /// Idempotent operation - checks database for existing tickets before creating.
    /// </summary>
    public async Task SeedSocialEventDonationTicketsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting social event donation ticket purchases creation");

        // Get all social events with donation ticket types
        // Include Sessions for creating EventAttendance records with SessionId
        var socialEvents = await _context.Events
            .Include(e => e.TicketTypes)
                .ThenInclude(tt => tt.Sessions)
            .Where(e => e.AllowRsvps && !e.RequireTicketPurchase &&
                       e.TicketTypes.Any(tt => tt.Name.Contains("Donation") || tt.Name.Contains("Suggested")))
            .ToListAsync(cancellationToken);

        if (socialEvents.Count == 0)
        {
            _logger.LogInformation("No social events with donation tickets found");
            return;
        }

        // Get all users
        var users = await _userManager.Users.ToListAsync(cancellationToken);
        if (users.Count == 0)
        {
            _logger.LogWarning("No users available for social event donations");
            return;
        }

        var purchasesToAdd = new List<TicketPurchase>();
        var attendancesToAdd = new List<EventAttendance>();
        var attendeesToAdd = new List<EventAttendee>();
        var ticketCountersByEvent = new Dictionary<Guid, int>();

        foreach (var eventItem in socialEvents)
        {
            var donationTicketType = eventItem.TicketTypes
                .FirstOrDefault(tt => tt.Name.Contains("Donation") || tt.Name.Contains("Suggested"));

            if (donationTicketType == null)
            {
                _logger.LogWarning("No donation ticket type found for social event {EventTitle}", eventItem.Title);
                continue;
            }

            // 20% of attendees make donations (matching AttendanceSeeder logic)
            var donationCount = Math.Max(1, users.Count / 5);

            for (int i = 0; i < Math.Min(donationCount, users.Count); i++)
            {
                var user = users[i];

                // Check if user already has donation ticket for this event
                var existingTicket = await _context.TicketPurchases
                    .Include(tp => tp.TicketType)
                    .FirstOrDefaultAsync(tp =>
                        tp.UserId == user.Id &&
                        tp.TicketType.EventId == eventItem.Id,
                        cancellationToken);

                if (existingTicket != null)
                {
                    _logger.LogDebug("User {UserId} already has donation ticket for event {EventId}, skipping", user.Id, eventItem.Id);
                    continue;
                }

                var donationAmount = (decimal)Random.Shared.Next(5, 31); // $5-$30 donation
                var purchaseDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 20));
                var paymentMethod = SeedingHelpers.GetRandomSocialEventPaymentMethod(); // 67% PayPal, 33% Cash

                var ticketPurchase = new TicketPurchase
                {
                    Id = Guid.NewGuid(),
                    TicketTypeId = donationTicketType.Id,
                    UserId = user.Id,
                    Quantity = 1,
                    TotalPrice = donationAmount,
                    PaymentStatus = "Completed",
                    PaymentMethod = paymentMethod,
                    PaymentReference = $"DN-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                    PurchaseDate = purchaseDate,
                    CreatedAt = purchaseDate,
                    UpdatedAt = purchaseDate
                };

                // Add PayPal fields if PayPal payment
                if (paymentMethod == "PayPal")
                {
                    ticketPurchase.EncryptedPayPalOrderId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalOrderId());
                    ticketPurchase.EncryptedPayPalCaptureId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalCaptureId());
                    ticketPurchase.EncryptedPayPalPayerId = await _encryptionService.EncryptAsync(SeedingHelpers.GeneratePayPalPayerId());
                }

                purchasesToAdd.Add(ticketPurchase);

                // Create EventAttendance and EventAttendee for donation
                // Note: These users will ALSO have an RSVP attendance created by AttendanceSeeder
                CreateAttendanceAndAttendee(
                    ticketPurchase,
                    eventItem.Id,
                    user.Id,
                    donationTicketType,
                    "Completed",
                    paymentMethod,
                    attendancesToAdd,
                    attendeesToAdd,
                    ticketCountersByEvent, "SOCIAL");

                _logger.LogDebug("Created donation ticket for user {UserId} at event {EventTitle}", user.Id, eventItem.Title);
            }

            _logger.LogInformation("Created {Count} donation tickets for social event {EventTitle}", donationCount, eventItem.Title);
        }

        if (purchasesToAdd.Count > 0)
        {
            // Save all entities together in transaction
            await _context.TicketPurchases.AddRangeAsync(purchasesToAdd, cancellationToken);
            await _context.EventAttendances.AddRangeAsync(attendancesToAdd, cancellationToken);
            await _context.EventAttendees.AddRangeAsync(attendeesToAdd, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Social event donation tickets creation completed. Created: {PurchaseCount} purchases, {AttendanceCount} attendances, {AttendeeCount} attendees",
                purchasesToAdd.Count,
                attendancesToAdd.Count,
                attendeesToAdd.Count);
        }
        else
        {
            _logger.LogInformation("No new social event donation tickets needed - all users already have tickets");
        }
    }

    /// <summary>
    /// Seeds historical workshop ticket purchases.
    /// Called after EventSeeder and SessionTicketSeeder have run.
    /// </summary>
    public async Task SeedHistoricalWorkshopTicketsAsync(EventSeeder eventSeeder, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting historical workshop tickets creation");

        // Check if historical tickets already exist
        var existingHistoricalTickets = await _context.EventAttendances
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

        // Create additional historical test data for refund testing (>90 days old, varied payment statuses)
        var refundTestPurchases = new List<TicketPurchase>();
        var refundTestAttendances = new List<EventAttendance>();
        var refundTestAttendees = new List<EventAttendee>();
        var refundTestCounters = new Dictionary<Guid, int>();

        await CreateHistoricalTestDataForRefundTestingAsync(
            refundTestPurchases,
            refundTestAttendances,
            refundTestAttendees,
            refundTestCounters,
            cancellationToken);

        if (refundTestPurchases.Count > 0)
        {
            await _context.TicketPurchases.AddRangeAsync(refundTestPurchases, cancellationToken);
            await _context.EventAttendances.AddRangeAsync(refundTestAttendances, cancellationToken);
            await _context.EventAttendees.AddRangeAsync(refundTestAttendees, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Saved {Count} refund test purchases", refundTestPurchases.Count);
        }

        _logger.LogInformation("Historical workshop tickets creation completed");
    }
}
