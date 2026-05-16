using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.EmailTemplates.Services;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Shared.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.Fixtures;

namespace WitchCityRope.Api.Tests.Features.EmailTemplates;

/// <summary>
/// Tests for the post-purchase "catch-up reminder" behavior of <see cref="EventEmailService"/>.
///
/// When a user buys a ticket late, <c>SendCatchUpRemindersAsync</c> replays the time-based
/// reminders that already went out for their session(s) so the late buyer is not left out.
///
/// REGRESSION GUARD (2026-05-16): the catch-up logic previously replayed EVERY time-based
/// "Sent" EmailTriggerLog regardless of template type. A plain ticket buyer therefore received
/// a <c>VolunteerReminder</c> email — a volunteer-only template, addressed with merge fields
/// ({{volunteer_name}}, {{volunteer_tasks_list}}, {{session_name}}) that the catch-up code does
/// not populate, so it arrived with raw unrendered placeholders. The fix added an explicit
/// allowlist (<c>EventEmailService.CatchUpEligibleTemplateTypes</c>). These tests pin that
/// behavior so the allowlist is not accidentally removed or widened to volunteer templates.
///
/// Uses TestContainers PostgreSQL via DatabaseTestFixture (NOT InMemoryDatabase).
/// </summary>
[Collection("Database")]
public class EventEmailServiceCatchUpTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<EventEmailService>> _mockLogger;
    private ApplicationDbContext _context = null!;
    private EventEmailService _sut = null!;

    private Guid _userId;
    private Guid _eventId;
    private Guid _sessionId;
    private Guid _ticketPurchaseId;

    public EventEmailServiceCatchUpTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _mockEmailService = new Mock<IEmailService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<EventEmailService>>();
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _context = _fixture.CreateDbContext();

        _sut = new EventEmailService(
            _context,
            _mockEmailService.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // The email service is mocked — every send "succeeds" so the catch-up loop runs fully.
        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmailCategory>(),
                It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        await SeedPurchaseGraphAsync();
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        await _fixture.ResetDatabaseAsync();
    }

    /// <summary>
    /// Seeds a user who has bought a ticket for a single-session event. The session already
    /// has TWO time-based reminders logged as "Sent": a VolunteerReminder (volunteer-only) and
    /// a Reminder1Day (attendee-facing). This is the exact shape that triggered the bug.
    /// </summary>
    private async Task SeedPurchaseGraphAsync()
    {
        _userId = Guid.NewGuid();
        _eventId = Guid.NewGuid();
        _sessionId = Guid.NewGuid();
        _ticketPurchaseId = Guid.NewGuid();

        // Venue (FK target for Event.VenueId)
        var venue = await _context.Venues.FindAsync(1);
        if (venue == null)
        {
            venue = new Venue
            {
                Id = 1,
                Name = "Test Venue",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Venues.Add(venue);
            await _context.SaveChangesAsync();
        }

        var user = new ApplicationUser
        {
            Id = _userId,
            Email = $"buyer-{Guid.NewGuid():N}@example.com",
            SceneName = "TicketBuyer",
            UserName = $"buyer-{Guid.NewGuid():N}@example.com",
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);

        var evt = new Event
        {
            Id = _eventId,
            Title = "Rope Jam - Catch-Up Test",
            Description = "Test event",
            AllowRsvps = false,
            RequireTicketPurchase = true,
            VettedMembersOnly = false,
            StartDate = DateTime.UtcNow.AddDays(3),
            EndDate = DateTime.UtcNow.AddDays(3).AddHours(3),
            Capacity = 20,
            VenueId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Events.Add(evt);

        var session = new Session
        {
            Id = _sessionId,
            EventId = _eventId,
            SessionCode = "S1",
            Name = "Main Session",
            StartTime = evt.StartDate,
            EndTime = evt.EndDate,
            Capacity = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Sessions.Add(session);

        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = _eventId,
            Name = "General Admission",
            Price = 20.00m,
            Available = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            // Many-to-many link: catch-up resolves sessions via TicketType.Sessions.
            Sessions = new List<Session> { session }
        };
        _context.TicketTypes.Add(ticketType);

        var purchase = new TicketPurchase
        {
            Id = _ticketPurchaseId,
            TicketTypeId = ticketType.Id,
            UserId = _userId,
            PurchaseDate = DateTime.UtcNow,
            Quantity = 1,
            TotalPrice = 20.00m,
            PaymentStatus = TicketPurchasePaymentStatus.Completed
        };
        _context.TicketPurchases.Add(purchase);

        // Two already-sent time-based reminders for this session.
        // VolunteerReminder must NOT be caught up; Reminder1Day must be.
        _context.Set<EmailTriggerLog>().AddRange(
            new EmailTriggerLog
            {
                Id = Guid.NewGuid(),
                TemplateId = Guid.NewGuid(),
                EventId = _eventId,
                SessionId = _sessionId,
                TemplateType = "VolunteerReminder",
                TriggerType = "TimeBased",
                RecipientGroup = "SessionVolunteers",
                RecipientCount = 1,
                TriggeredAt = DateTime.UtcNow.AddHours(-2),
                SentAt = DateTime.UtcNow.AddHours(-2),
                Status = "Sent"
            },
            new EmailTriggerLog
            {
                Id = Guid.NewGuid(),
                TemplateId = Guid.NewGuid(),
                EventId = _eventId,
                SessionId = _sessionId,
                TemplateType = "Reminder1Day",
                TriggerType = "TimeBased",
                RecipientGroup = "RSVPTicketHolders",
                RecipientCount = 5,
                TriggeredAt = DateTime.UtcNow.AddHours(-1),
                SentAt = DateTime.UtcNow.AddHours(-1),
                Status = "Sent"
            });

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// The core regression guard: a ticket buyer must NEVER be caught up on a VolunteerReminder.
    /// </summary>
    [Fact]
    public async Task SendPostPurchaseEmails_DoesNotCatchUpVolunteerReminder()
    {
        // Act
        await _sut.SendPostPurchaseEmailsAsync(
            _userId, new List<Guid> { _ticketPurchaseId }, CancellationToken.None);

        // Assert — VolunteerReminder is volunteer-only and must be excluded from catch-up.
        _mockEmailService.Verify(
            x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmailCategory>(),
                "VolunteerReminder", It.IsAny<Dictionary<string, string>>(),
                It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "a ticket buyer is not a confirmed volunteer and the volunteer-only merge "
            + "fields would render as raw {{placeholders}}");
    }

    /// <summary>
    /// The catch-up feature itself must still work: eligible attendee-facing reminders that
    /// already went out are replayed to the late buyer.
    /// </summary>
    [Fact]
    public async Task SendPostPurchaseEmails_CatchesUpEligibleAttendeeReminder()
    {
        // Act
        await _sut.SendPostPurchaseEmailsAsync(
            _userId, new List<Guid> { _ticketPurchaseId }, CancellationToken.None);

        // Assert — Reminder1Day is attendee-facing and on the allowlist, so it IS caught up.
        _mockEmailService.Verify(
            x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), EmailCategory.Events,
                "Reminder1Day", It.IsAny<Dictionary<string, string>>(),
                It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "an eligible already-sent reminder should be replayed to a late ticket buyer");
    }
}
