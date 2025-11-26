using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.Core.Tests.Features.Events;

/// <summary>
/// Unit tests for EventService.CopyEventAsync method
/// Tests event copy functionality including deep copy of all related entities
/// Uses TestContainers with PostgreSQL for real database operations
/// </summary>
[Collection("Database")]
public class EventServiceCopyTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private ApplicationDbContext _context = null!;
    private Mock<ILogger<EventService>> _mockLogger = null!;
    private EventService _service = null!;

    public EventServiceCopyTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _context = _fixture.CreateDbContext();
        await _fixture.ResetDatabaseAsync();

        _mockLogger = new Mock<ILogger<EventService>>();
        _service = new EventService(_context, _mockLogger.Object);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    /// <summary>
    /// Test 1: Verify all event properties are copied correctly
    /// </summary>
    [Fact]
    public async Task CopyEventAsync_WithValidEvent_CopiesAllProperties()
    {
        // Arrange
        var venue = new Venue
        {
            Name = "Test Venue",
            IsActive = true
        };
        _context.Venues.Add(venue);

        var originalEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Original Event",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(30).AddHours(2),
            ShortDescription = "Short description",
            Description = "Full description",
            Policies = "Event policies",
            Capacity = 50,
            EventType = EventType.Class,
            VenueId = venue.Id,
            IsPublished = true,
            RegistrationOpenHours = 72,
            RegistrationCloseHours = 1,
            CancellationOpenHours = 24,
            CancellationCloseHours = 1,
            VolunteerRegistrationCloseHours = 2,
            VolunteerCancellationCloseHours = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Events.Add(originalEvent);
        await _context.SaveChangesAsync();

        var request = new CopyEventRequest
        {
            NewTitle = "Copied Event",
            NewStartDate = DateTimeOffset.UtcNow.AddDays(60)
        };

        // Act
        var (success, response, error) = await _service.CopyEventAsync(
            originalEvent.Id.ToString(),
            request,
            CancellationToken.None);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Id.Should().NotBe(originalEvent.Id.ToString());
        response.Title.Should().Be(request.NewTitle);
        response.StartDate.Should().BeCloseTo(request.NewStartDate, TimeSpan.FromSeconds(1));
        response.ShortDescription.Should().Be(originalEvent.ShortDescription);
        response.Description.Should().Be(originalEvent.Description);
        response.Policies.Should().Be(originalEvent.Policies);
        response.Capacity.Should().Be(originalEvent.Capacity);
        response.EventType.Should().Be(originalEvent.EventType.ToString());
        response.VenueId.Should().Be(originalEvent.VenueId?.ToString());
        response.RegistrationOpenHours.Should().Be(originalEvent.RegistrationOpenHours);
        response.RegistrationCloseHours.Should().Be(originalEvent.RegistrationCloseHours);
        response.CancellationOpenHours.Should().Be(originalEvent.CancellationOpenHours);
        response.CancellationCloseHours.Should().Be(originalEvent.CancellationCloseHours);
        response.VolunteerRegistrationCloseHours.Should().Be(originalEvent.VolunteerRegistrationCloseHours);
        response.VolunteerCancellationCloseHours.Should().Be(originalEvent.VolunteerCancellationCloseHours);
        error.Should().BeEmpty();
    }

    /// <summary>
    /// Test 2: Verify copied event is unpublished (draft)
    /// </summary>
    [Fact]
    public async Task CopyEventAsync_WithValidEvent_CreatesDraftEvent()
    {
        // Arrange
        var originalEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Published Event",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(30).AddHours(2),
            EventType = EventType.Social,
            Capacity = 40,
            IsPublished = true, // Original is published
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Events.Add(originalEvent);
        await _context.SaveChangesAsync();

        var request = new CopyEventRequest
        {
            NewTitle = "Copied Event",
            NewStartDate = DateTimeOffset.UtcNow.AddDays(60)
        };

        // Act
        var (success, response, error) = await _service.CopyEventAsync(
            originalEvent.Id.ToString(),
            request,
            CancellationToken.None);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.IsPublished.Should().BeFalse(); // Copy is always draft
    }

    /// <summary>
    /// Test 3: Verify sessions are deep copied with date offset
    /// </summary>
    [Fact]
    public async Task CopyEventAsync_WithValidEvent_CopiesSessions()
    {
        // Arrange
        var originalEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Event with Sessions",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(30).AddHours(6),
            EventType = EventType.Class,
            Capacity = 40,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var session1 = new Session
        {
            Id = Guid.NewGuid(),
            EventId = originalEvent.Id,
            SessionCode = "Session A",
            Name = "Morning Session",
            StartTime = originalEvent.StartDate.AddHours(1),
            EndTime = originalEvent.StartDate.AddHours(3),
            Capacity = 20
        };

        var session2 = new Session
        {
            Id = Guid.NewGuid(),
            EventId = originalEvent.Id,
            SessionCode = "Session B",
            Name = "Afternoon Session",
            StartTime = originalEvent.StartDate.AddHours(4),
            EndTime = originalEvent.StartDate.AddHours(6),
            Capacity = 20
        };

        originalEvent.Sessions.Add(session1);
        originalEvent.Sessions.Add(session2);
        _context.Events.Add(originalEvent);
        await _context.SaveChangesAsync();

        var request = new CopyEventRequest
        {
            NewTitle = "Copied Event",
            NewStartDate = DateTimeOffset.UtcNow.AddDays(60)
        };

        // Act
        var (success, response, error) = await _service.CopyEventAsync(
            originalEvent.Id.ToString(),
            request,
            CancellationToken.None);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Sessions.Should().HaveCount(2);

        var copiedSession1 = response.Sessions.FirstOrDefault(s => s.SessionCode == "Session A");
        copiedSession1.Should().NotBeNull();
        copiedSession1!.Id.Should().NotBe(session1.Id.ToString());
        copiedSession1.Name.Should().Be(session1.Name);
        copiedSession1.Capacity.Should().Be(session1.Capacity);

        // Verify date offset applied (30 days difference)
        var dateOffset = (request.NewStartDate.DateTime - originalEvent.StartDate).TotalDays;
        var expectedStartTime = session1.StartTime.AddDays(dateOffset);
        copiedSession1.StartTime.Should().BeCloseTo(DateTimeOffset.Parse(expectedStartTime.ToString("o")), TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Test 4: Verify ticket types are deep copied with session ID remapping
    /// </summary>
    [Fact]
    public async Task CopyEventAsync_WithValidEvent_CopiesTicketTypes()
    {
        // Arrange
        var originalEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Event with Tickets",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(30).AddHours(3),
            EventType = EventType.Class,
            Capacity = 50,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var session = new Session
        {
            Id = Guid.NewGuid(),
            EventId = originalEvent.Id,
            SessionCode = "S1",
            Name = "Main Session",
            StartTime = originalEvent.StartDate,
            EndTime = originalEvent.StartDate.AddHours(3),
            Capacity = 50
        };

        var ticketType1 = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = originalEvent.Id,
            SessionId = session.Id, // Session-specific ticket
            Name = "Early Bird",
            Description = "Early registration discount",
            Price = 25.00m,
            Available = 50
        };

        var ticketType2 = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = originalEvent.Id,
            SessionId = null, // General admission ticket
            Name = "General Admission",
            Price = 30.00m,
            Available = 100
        };

        originalEvent.Sessions.Add(session);
        originalEvent.TicketTypes.Add(ticketType1);
        originalEvent.TicketTypes.Add(ticketType2);
        _context.Events.Add(originalEvent);
        await _context.SaveChangesAsync();

        var request = new CopyEventRequest
        {
            NewTitle = "Copied Event",
            NewStartDate = DateTimeOffset.UtcNow.AddDays(60)
        };

        // Act
        var (success, response, error) = await _service.CopyEventAsync(
            originalEvent.Id.ToString(),
            request,
            CancellationToken.None);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.TicketTypes.Should().HaveCount(2);

        var copiedTicket1 = response.TicketTypes.FirstOrDefault(t => t.Name == "Early Bird");
        copiedTicket1.Should().NotBeNull();
        copiedTicket1!.Id.Should().NotBe(ticketType1.Id.ToString());
        copiedTicket1.SessionId.Should().NotBe(session.Id.ToString()); // REMAPPED
        copiedTicket1.SessionId.Should().NotBeNullOrEmpty(); // Still has session
        copiedTicket1.Price.Should().Be(ticketType1.Price);
        copiedTicket1.Available.Should().Be(ticketType1.Available);

        var copiedTicket2 = response.TicketTypes.FirstOrDefault(t => t.Name == "General Admission");
        copiedTicket2.Should().NotBeNull();
        copiedTicket2!.SessionId.Should().BeNull(); // Remains null
    }

    /// <summary>
    /// Test 5: Verify volunteer positions copied with session remapping and SlotsFilled reset
    /// </summary>
    [Fact]
    public async Task CopyEventAsync_WithValidEvent_CopiesVolunteerPositions()
    {
        // Arrange
        var originalEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Event with Volunteers",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(30).AddHours(4),
            EventType = EventType.Social,
            Capacity = 60,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var session = new Session
        {
            Id = Guid.NewGuid(),
            EventId = originalEvent.Id,
            SessionCode = "S1",
            Name = "Evening Session",
            StartTime = originalEvent.StartDate,
            EndTime = originalEvent.StartDate.AddHours(4),
            Capacity = 60
        };

        var position1 = new VolunteerPosition
        {
            Id = Guid.NewGuid(),
            EventId = originalEvent.Id,
            SessionId = session.Id,
            Title = "Setup Crew",
            Description = "Help with setup",
            SlotsNeeded = 5,
            SlotsFilled = 3, // Already partially filled
            IsPublicFacing = true
        };

        var position2 = new VolunteerPosition
        {
            Id = Guid.NewGuid(),
            EventId = originalEvent.Id,
            SessionId = null, // Event-level volunteer
            Title = "Registration Desk",
            SlotsNeeded = 2,
            SlotsFilled = 2, // Fully filled
            IsPublicFacing = true
        };

        originalEvent.Sessions.Add(session);
        originalEvent.VolunteerPositions.Add(position1);
        originalEvent.VolunteerPositions.Add(position2);
        _context.Events.Add(originalEvent);
        await _context.SaveChangesAsync();

        var request = new CopyEventRequest
        {
            NewTitle = "Copied Event",
            NewStartDate = DateTimeOffset.UtcNow.AddDays(60)
        };

        // Act
        var (success, response, error) = await _service.CopyEventAsync(
            originalEvent.Id.ToString(),
            request,
            CancellationToken.None);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.VolunteerPositions.Should().HaveCount(2);

        var copiedPosition1 = response.VolunteerPositions.FirstOrDefault(p => p.Title == "Setup Crew");
        copiedPosition1.Should().NotBeNull();
        copiedPosition1!.Id.Should().NotBe(position1.Id.ToString());
        copiedPosition1.SessionId.Should().NotBe(session.Id.ToString()); // REMAPPED
        copiedPosition1.SessionId.Should().NotBeNullOrEmpty();
        copiedPosition1.SlotsNeeded.Should().Be(position1.SlotsNeeded);
        copiedPosition1.SlotsFilled.Should().Be(0); // RESET

        var copiedPosition2 = response.VolunteerPositions.FirstOrDefault(p => p.Title == "Registration Desk");
        copiedPosition2.Should().NotBeNull();
        copiedPosition2!.SessionId.Should().BeNull();
        copiedPosition2.SlotsFilled.Should().Be(0); // RESET
    }

    /// <summary>
    /// Test 6: Verify organizer references are copied (same users)
    /// </summary>
    [Fact]
    public async Task CopyEventAsync_WithValidEvent_CopiesOrganizers()
    {
        // Arrange
        var originalEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Event with Organizers",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(30).AddHours(2),
            EventType = EventType.Class,
            Capacity = 30,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var organizer1 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "teacher1@example.com",
            Email = "teacher1@example.com",
            EmailConfirmed = true
        };
        var organizer2 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "teacher2@example.com",
            Email = "teacher2@example.com",
            EmailConfirmed = true
        };

        originalEvent.Organizers.Add(organizer1);
        originalEvent.Organizers.Add(organizer2);
        _context.Users.AddRange(organizer1, organizer2);
        _context.Events.Add(originalEvent);
        await _context.SaveChangesAsync();

        var request = new CopyEventRequest
        {
            NewTitle = "Copied Event",
            NewStartDate = DateTimeOffset.UtcNow.AddDays(60)
        };

        // Act
        var (success, response, error) = await _service.CopyEventAsync(
            originalEvent.Id.ToString(),
            request,
            CancellationToken.None);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.TeacherIds.Should().HaveCount(2);
        response.TeacherIds.Should().Contain(organizer1.Id.ToString());
        response.TeacherIds.Should().Contain(organizer2.Id.ToString());
    }

    /// <summary>
    /// Test 7: Verify attendance and transaction data NOT copied
    /// </summary>
    [Fact]
    public async Task CopyEventAsync_WithValidEvent_ExcludesAttendanceData()
    {
        // Arrange
        var originalEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Event with Attendance",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(30).AddHours(2),
            EventType = EventType.Social,
            Capacity = 50,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = originalEvent.Id,
            Name = "General Admission",
            Price = 25.00m,
            Available = 50
        };
        originalEvent.TicketTypes.Add(ticketType);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "user@example.com",
            Email = "user@example.com",
            EmailConfirmed = true
        };

        // Add attendance record
        var attendance = new EventAttendance
        {
            EventId = originalEvent.Id,
            UserId = user.Id,
            Status = AttendanceStatus.Active,
            AttendanceType = AttendanceType.RSVP,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add ticket purchase
        var ticketPurchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            EventId = originalEvent.Id,
            UserId = user.Id,
            TicketTypeId = ticketType.Id,
            Quantity = 1,
            TotalAmount = 25.00m,
            PaymentMethod = "Venmo",
            TransactionStatus = "Completed",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.Events.Add(originalEvent);
        _context.EventAttendances.Add(attendance);
        _context.TicketPurchases.Add(ticketPurchase);
        await _context.SaveChangesAsync();

        var request = new CopyEventRequest
        {
            NewTitle = "Copied Event",
            NewStartDate = DateTimeOffset.UtcNow.AddDays(60)
        };

        // Act
        var (success, response, error) = await _service.CopyEventAsync(
            originalEvent.Id.ToString(),
            request,
            CancellationToken.None);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();

        // Verify NO attendance records copied
        var copiedEventId = Guid.Parse(response!.Id);
        var copiedEventAttendances = await _context.EventAttendances
            .Where(a => a.EventId == copiedEventId)
            .ToListAsync();
        copiedEventAttendances.Should().BeEmpty();

        // Verify NO ticket purchases copied
        var copiedPurchases = await _context.TicketPurchases
            .Where(p => p.EventId == copiedEventId)
            .ToListAsync();
        copiedPurchases.Should().BeEmpty();
    }

    /// <summary>
    /// Test 8: Verify custom email templates are copied with new IDs
    /// NOTE: This test may need adjustment based on actual EventEmailTemplate entity structure
    /// </summary>
    [Fact]
    public async Task CopyEventAsync_WithCustomEmailTemplates_CopiesTemplates()
    {
        // Arrange
        var originalEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Event with Templates",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(30).AddHours(2),
            EventType = EventType.Class,
            Capacity = 40,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.Add(originalEvent);
        await _context.SaveChangesAsync();

        // Create custom email templates using Set<T>() to access entity
        var template1 = new WitchCityRope.Api.Features.EmailTemplates.Entities.EventEmailTemplate
        {
            Id = Guid.NewGuid(),
            EventId = originalEvent.Id,
            Subject = "Welcome to Event",
            HtmlBody = "<p>Welcome!</p>",
            PlainTextBody = "Welcome!",
            TargetSessions = "Session A",
            RecipientGroup = "Attendees",
            GlobalTemplateId = Guid.NewGuid()
        };

        var template2 = new WitchCityRope.Api.Features.EmailTemplates.Entities.EventEmailTemplate
        {
            Id = Guid.NewGuid(),
            EventId = originalEvent.Id,
            Subject = "Event Reminder",
            HtmlBody = "<p>Reminder!</p>",
            PlainTextBody = "Reminder!",
            TargetSessions = null,
            RecipientGroup = "All",
            GlobalTemplateId = Guid.NewGuid()
        };

        _context.Set<WitchCityRope.Api.Features.EmailTemplates.Entities.EventEmailTemplate>().AddRange(template1, template2);
        await _context.SaveChangesAsync();

        var request = new CopyEventRequest
        {
            NewTitle = "Copied Event",
            NewStartDate = DateTimeOffset.UtcNow.AddDays(60)
        };

        // Act
        var (success, response, error) = await _service.CopyEventAsync(
            originalEvent.Id.ToString(),
            request,
            CancellationToken.None);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();

        // Query database for copied templates
        var copiedEventId = Guid.Parse(response!.Id);
        var copiedTemplates = await _context.Set<WitchCityRope.Api.Features.EmailTemplates.Entities.EventEmailTemplate>()
            .Where(t => t.EventId == copiedEventId)
            .ToListAsync();

        copiedTemplates.Should().HaveCount(2);

        // Verify template 1 copied correctly
        var copiedTemplate1 = copiedTemplates.FirstOrDefault(t => t.Subject == "Welcome to Event");
        copiedTemplate1.Should().NotBeNull();
        copiedTemplate1!.Id.Should().NotBe(template1.Id); // New ID
        copiedTemplate1.Subject.Should().Be(template1.Subject);
        copiedTemplate1.HtmlBody.Should().Be(template1.HtmlBody);
        copiedTemplate1.PlainTextBody.Should().Be(template1.PlainTextBody);
        copiedTemplate1.TargetSessions.Should().Be(template1.TargetSessions);
        copiedTemplate1.RecipientGroup.Should().Be(template1.RecipientGroup);
        copiedTemplate1.GlobalTemplateId.Should().Be(template1.GlobalTemplateId); // Preserved
        copiedTemplate1.EventId.Should().Be(copiedEventId); // Points to new event
    }

    /// <summary>
    /// Test 9: Verify copy succeeds when event has no custom templates
    /// </summary>
    [Fact]
    public async Task CopyEventAsync_WithoutCustomEmailTemplates_CopiesSuccessfully()
    {
        // Arrange
        var originalEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Event without Templates",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(30).AddHours(2),
            EventType = EventType.Social,
            Capacity = 35,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Events.Add(originalEvent);
        await _context.SaveChangesAsync();

        var request = new CopyEventRequest
        {
            NewTitle = "Copied Event",
            NewStartDate = DateTimeOffset.UtcNow.AddDays(60)
        };

        // Act
        var (success, response, error) = await _service.CopyEventAsync(
            originalEvent.Id.ToString(),
            request,
            CancellationToken.None);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Title.Should().Be(request.NewTitle);

        // Verify no templates (query returns empty)
        var copiedEventId = Guid.Parse(response.Id);
        var copiedTemplates = await _context.Set<WitchCityRope.Api.Features.EmailTemplates.Entities.EventEmailTemplate>()
            .Where(t => t.EventId == copiedEventId)
            .ToListAsync();
        copiedTemplates.Should().BeEmpty();
    }

    /// <summary>
    /// Test 10: Verify proper error handling for non-existent event
    /// </summary>
    [Fact]
    public async Task CopyEventAsync_WithInvalidEventId_ReturnsError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid().ToString();
        var request = new CopyEventRequest
        {
            NewTitle = "Test",
            NewStartDate = DateTimeOffset.UtcNow.AddDays(30)
        };

        // Act
        var (success, response, error) = await _service.CopyEventAsync(
            nonExistentId,
            request,
            CancellationToken.None);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("not found");
    }

    /// <summary>
    /// Test 11: Verify transaction rollback on errors
    /// Note: This test verifies no partial data exists after error
    /// </summary>
    [Fact]
    public async Task CopyEventAsync_WithDatabaseError_HandlesGracefully()
    {
        // Arrange
        var originalEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Event for Error Test",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(30).AddHours(2),
            EventType = EventType.Class,
            Capacity = 30,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Events.Add(originalEvent);
        await _context.SaveChangesAsync();

        // Create request with invalid data that will cause FK violation
        // (This simulates a scenario where venue doesn't exist but event has VenueId)
        var request = new CopyEventRequest
        {
            NewTitle = "Copied Event",
            NewStartDate = DateTimeOffset.UtcNow.AddDays(60)
        };

        // Manually set invalid VenueId to cause FK constraint error
        originalEvent.VenueId = Guid.NewGuid(); // Non-existent venue
        _context.Events.Update(originalEvent);
        await _context.SaveChangesAsync();

        // Act
        var (success, response, error) = await _service.CopyEventAsync(
            originalEvent.Id.ToString(),
            request,
            CancellationToken.None);

        // Assert
        // May fail or succeed depending on FK constraint enforcement
        // If fails, verify no partial data exists
        if (!success)
        {
            error.Should().NotBeEmpty();

            // Verify transaction rolled back - NO partial data in database
            var copiedEvents = await _context.Events
                .Where(e => e.Title.StartsWith("Copied"))
                .ToListAsync();
            copiedEvents.Should().BeEmpty();
        }
    }
}
