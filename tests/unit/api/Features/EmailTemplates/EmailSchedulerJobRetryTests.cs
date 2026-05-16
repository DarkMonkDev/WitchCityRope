using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.EmailTemplates.Jobs;
using WitchCityRope.Api.Features.EmailTemplates.Models;
using WitchCityRope.Api.Features.EmailTemplates.Services;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Shared.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.Fixtures;

namespace WitchCityRope.Api.Tests.Features.EmailTemplates;

/// <summary>
/// Tests for the bounded-retry guard in <see cref="EmailSchedulerJob"/> (tech-debt BE-16).
///
/// The hourly scheduler previously treated only a "Sent" EmailTriggerLog as "handled", so a
/// reminder that failed to send was re-attempted on every hourly run for the entire event
/// send window (~25 duplicate "Failed" rows observed in production). The fix caps retries at
/// MaxSendAttempts (3): once 3 "Failed" rows exist for a (template, session, type) tuple, the
/// scheduler stops re-attempting.
///
/// These tests pin the boundary — 2 failures still retries, 3 failures stops — so the
/// >= vs > comparison and the MaxSendAttempts value cannot silently drift.
///
/// Uses TestContainers PostgreSQL via DatabaseTestFixture (NOT InMemoryDatabase).
/// </summary>
[Collection("Database")]
public class EmailSchedulerJobRetryTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly Mock<IEmailTemplateService> _mockTemplateService;
    private readonly Mock<IEventRecipientService> _mockRecipientService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<EmailSchedulerJob>> _mockLogger;

    private ApplicationDbContext _context = null!;
    private EmailSchedulerJob _sut = null!;

    private Guid _templateId;
    private Guid _eventId;
    private Guid _sessionId;

    private const string TemplateType = "Reminder1Day";

    public EmailSchedulerJobRetryTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _mockTemplateService = new Mock<IEmailTemplateService>();
        _mockRecipientService = new Mock<IEventRecipientService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<EmailSchedulerJob>>();
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _context = _fixture.CreateDbContext();

        _templateId = Guid.NewGuid();
        _eventId = Guid.NewGuid();
        _sessionId = Guid.NewGuid();

        _sut = new EmailSchedulerJob(
            _context,
            _mockTemplateService.Object,
            _mockRecipientService.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // One time-based template, due for a session starting inside the offset window.
        // TimingOffsetDays = 1 → pre-event window of (now, now + 25h]; the seeded session
        // starts 10h out, so it qualifies.
        var template = new GlobalEmailTemplateDto
        {
            Id = _templateId,
            Category = "Events",
            TemplateType = TemplateType,
            TriggerType = TemplateTriggerType.TimeBased,
            SendingEnabled = true,
            IsActive = true,
            TimingOffsetDays = 1,
            RecipientGroup = EventRecipientGroup.RSVPTicketHolders
        };
        _mockTemplateService
            .Setup(x => x.GetTimeBasedTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<GlobalEmailTemplateDto>>.Success(
                new List<GlobalEmailTemplateDto> { template }));

        // Exactly one recipient — so a non-skipped run sends exactly one email.
        _mockRecipientService
            .Setup(x => x.GetRecipientsAsync(
                It.IsAny<EventRecipientGroup>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecipientInfo>
            {
                new RecipientInfo(Guid.NewGuid(), "attendee@example.com", "Attendee")
            });

        // The actual email send always "succeeds" in these tests — we are exercising the
        // idempotency/retry gate, not SendGrid behavior.
        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmailCategory>(),
                It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        await SeedPublishedSessionAsync();
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        await _fixture.ResetDatabaseAsync();
    }

    /// <summary>Seeds a published event with one session that qualifies for the template window.</summary>
    private async Task SeedPublishedSessionAsync()
    {
        var venue = await _context.Venues.FindAsync(1);
        if (venue == null)
        {
            _context.Venues.Add(new Venue
            {
                Id = 1,
                Name = "Test Venue",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        var start = DateTime.UtcNow.AddHours(10);

        _context.Events.Add(new Event
        {
            Id = _eventId,
            Title = "Rope Jam - Retry Test",
            Description = "Test event",
            AllowRsvps = true,
            RequireTicketPurchase = false,
            VettedMembersOnly = false,
            StartDate = start,
            EndDate = start.AddHours(3),
            Capacity = 20,
            VenueId = 1,
            IsPublished = true, // ProcessTemplateAsync only considers published events
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _context.Sessions.Add(new Session
        {
            Id = _sessionId,
            EventId = _eventId,
            SessionCode = "S1",
            Name = "Main Session",
            StartTime = start,
            EndTime = start.AddHours(3),
            Capacity = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    /// <summary>Seeds <paramref name="count"/> prior "Failed" trigger logs for the test tuple.</summary>
    private async Task SeedFailedAttemptsAsync(int count)
    {
        for (var i = 0; i < count; i++)
        {
            _context.Set<EmailTriggerLog>().Add(new EmailTriggerLog
            {
                Id = Guid.NewGuid(),
                TemplateId = _templateId,
                EventId = _eventId,
                SessionId = _sessionId,
                TemplateType = TemplateType,
                TriggerType = "TimeBased",
                RecipientGroup = "RSVPTicketHolders",
                RecipientCount = 1,
                TriggeredAt = DateTime.UtcNow.AddHours(-(count - i)),
                SentAt = null,
                Status = "Failed",
                ErrorMessage = "1 of 1 sends failed"
            });
        }
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Boundary test: with fewer than MaxSendAttempts (3) prior failures the scheduler still
    /// retries; once 3 failures exist it gives up. 0 failures is the baseline (fresh send).
    /// </summary>
    [Theory]
    [InlineData(0, true)]   // baseline — never attempted, must send
    [InlineData(2, true)]   // 2 failures (< 3) — still within retry budget, must send
    [InlineData(3, false)]  // 3 failures (== max) — budget exhausted, must skip
    [InlineData(5, false)]  // beyond max — must skip
    public async Task ExecuteAsync_RetriesUntilMaxFailedAttempts(int priorFailures, bool shouldSend)
    {
        // Arrange
        await SeedFailedAttemptsAsync(priorFailures);

        // Act
        await _sut.ExecuteAsync(CancellationToken.None);

        // Assert
        _mockEmailService.Verify(
            x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmailCategory>(),
                TemplateType, It.IsAny<Dictionary<string, string>>(),
                It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            shouldSend ? Times.Once() : Times.Never(),
            $"with {priorFailures} prior failure(s) the scheduler should "
            + (shouldSend ? "still retry the send" : "stop retrying (BE-16 bounded-retry guard)"));
    }

    /// <summary>
    /// A successful "Sent" log must short-circuit the scheduler regardless of how few attempts
    /// were made — this is the original idempotency behavior, preserved by the BE-16 fix.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_SkipsWhenAlreadySent()
    {
        // Arrange — one Failed row plus one Sent row for the tuple
        await SeedFailedAttemptsAsync(1);
        _context.Set<EmailTriggerLog>().Add(new EmailTriggerLog
        {
            Id = Guid.NewGuid(),
            TemplateId = _templateId,
            EventId = _eventId,
            SessionId = _sessionId,
            TemplateType = TemplateType,
            TriggerType = "TimeBased",
            RecipientGroup = "RSVPTicketHolders",
            RecipientCount = 1,
            TriggeredAt = DateTime.UtcNow.AddMinutes(-30),
            SentAt = DateTime.UtcNow.AddMinutes(-30),
            Status = "Sent"
        });
        await _context.SaveChangesAsync();

        // Act
        await _sut.ExecuteAsync(CancellationToken.None);

        // Assert — a prior Sent row means the reminder is already handled
        _mockEmailService.Verify(
            x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmailCategory>(),
                TemplateType, It.IsAny<Dictionary<string, string>>(),
                It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "an existing 'Sent' log must short-circuit the scheduler");
    }
}
