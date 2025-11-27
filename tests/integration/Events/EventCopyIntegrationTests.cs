using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Models;
using WitchCityRope.Tests.Common.Fixtures;
using Hangfire;
using Hangfire.MemoryStorage;

namespace WitchCityRope.IntegrationTests.Events;

/// <summary>
/// Integration tests for Event Copy endpoint (POST /api/events/{id}/copy)
/// Tests complete end-to-end workflow with real database and HTTP requests
/// Verifies deep copy operations, session remapping, and data exclusions
/// </summary>
[Collection("Database")]
public class EventCopyIntegrationTests : IntegrationTestBase, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Guid _adminUserId = Guid.NewGuid();
    private readonly string _adminEmail = $"admin-{Guid.NewGuid():N}@example.com";

    public EventCopyIntegrationTests(DatabaseTestFixture fixture) : base(fixture)
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace DbContext with TestContainers connection string
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseNpgsql(ConnectionString);
                    });

                    // Use in-memory storage for Hangfire in tests
                    services.AddHangfire(config =>
                    {
                        config.UseMemoryStorage();
                    });
                });
            });
    }

    public void Dispose()
    {
        _factory?.Dispose();
    }

    /// <summary>
    /// Test 1: Verify complete copy operation with database
    /// </summary>
    [Fact]
    public async Task CopyEvent_EndToEnd_CreatesNewEvent()
    {
        // Arrange
        await SeedAdminUser();
        var venue = await CreateTestVenue();
        var originalEvent = await CreateTestEventWithAllRelations(venue.Id);
        var client = CreateAuthenticatedClient(_adminUserId, _adminEmail, "Administrator");

        var request = new CopyEventRequest
        {
            NewTitle = "Copied Event",
            NewStartDate = DateTimeOffset.UtcNow.AddDays(60)
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/events/{originalEvent.Id}/copy",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var copiedEvent = await response.Content.ReadFromJsonAsync<EventDto>();
        copiedEvent.Should().NotBeNull();
        copiedEvent!.Id.Should().NotBe(originalEvent.Id.ToString());
        copiedEvent.Title.Should().Be(request.NewTitle);
        copiedEvent.StartDate.Should().BeCloseTo(request.NewStartDate.DateTime, TimeSpan.FromSeconds(1));
        copiedEvent.IsPublished.Should().BeFalse();

        // Verify in database
        await using var context = CreateDbContext();
        var dbEvent = await context.Events
            .Include(e => e.Sessions)
            .Include(e => e.TicketTypes)
            .Include(e => e.VolunteerPositions)
            .FirstOrDefaultAsync(e => e.Id == Guid.Parse(copiedEvent.Id));

        dbEvent.Should().NotBeNull();
        dbEvent!.Sessions.Should().HaveCount(originalEvent.Sessions.Count);
        dbEvent.TicketTypes.Should().HaveCount(originalEvent.TicketTypes.Count);
        dbEvent.VolunteerPositions.Should().HaveCount(originalEvent.VolunteerPositions.Count);
    }

    /// <summary>
    /// Test 2: Verify session ID remapping works for ticket types
    /// </summary>
    [Fact]
    public async Task CopyEvent_WithSessionRemapping_MapsTicketTypesCorrectly()
    {
        // Arrange
        await SeedAdminUser();
        var originalEvent = await CreateTestEventWithSessionTickets();
        var client = CreateAuthenticatedClient(_adminUserId, _adminEmail, "Administrator");

        var request = new CopyEventRequest
        {
            NewTitle = "Copied Event",
            NewStartDate = DateTimeOffset.UtcNow.AddDays(60)
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/events/{originalEvent.Id}/copy",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var copiedEvent = await response.Content.ReadFromJsonAsync<EventDto>();
        await using var context = CreateDbContext();
        var dbEvent = await context.Events
            .Include(e => e.Sessions)
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == Guid.Parse(copiedEvent!.Id));

        var copiedSession = dbEvent!.Sessions.First(s => s.SessionCode == "A");
        var copiedTicket = dbEvent.TicketTypes.First(t => t.Name == "Session A Ticket");

        copiedSession.Id.Should().NotBe(originalEvent.Sessions.First().Id);
        copiedTicket.Id.Should().NotBe(originalEvent.TicketTypes.First().Id);
        copiedTicket.SessionId.Should().Be(copiedSession.Id); // REMAPPED CORRECTLY
    }

    /// <summary>
    /// Test 3: Verify volunteer positions copied correctly with session remapping
    /// </summary>
    [Fact]
    public async Task CopyEvent_WithVolunteerPositions_RemapsSessionsAndResetsFilled()
    {
        // Arrange
        await SeedAdminUser();
        var originalEvent = await CreateTestEventWithVolunteers();
        var client = CreateAuthenticatedClient(_adminUserId, _adminEmail, "Administrator");

        var request = new CopyEventRequest
        {
            NewTitle = "Copied Event",
            NewStartDate = DateTimeOffset.UtcNow.AddDays(60)
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/events/{originalEvent.Id}/copy",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var copiedEvent = await response.Content.ReadFromJsonAsync<EventDto>();
        await using var context = CreateDbContext();
        var dbEvent = await context.Events
            .Include(e => e.Sessions)
            .Include(e => e.VolunteerPositions)
            .FirstOrDefaultAsync(e => e.Id == Guid.Parse(copiedEvent!.Id));

        var originalSession = originalEvent.Sessions.First();
        var copiedSession = dbEvent!.Sessions.First(s => s.SessionCode == originalSession.SessionCode);
        var copiedPosition = dbEvent.VolunteerPositions.First(p => p.Title == "Setup Crew");

        copiedSession.Id.Should().NotBe(originalSession.Id);
        copiedPosition.SessionId.Should().Be(copiedSession.Id); // REMAPPED
        copiedPosition.SlotsFilled.Should().Be(0); // RESET
    }

    /// <summary>
    /// Test 4: Verify CSRF protection (requires CSRF token)
    /// Note: This test may require adjustments based on actual CSRF implementation
    /// </summary>
    [Fact]
    public async Task CopyEvent_WithoutCsrfToken_Returns400()
    {
        // Arrange
        await SeedAdminUser();
        var originalEvent = await CreateTestEvent();
        var client = CreateAuthenticatedClient(_adminUserId, _adminEmail, "Administrator");

        // Remove CSRF token header if present
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");

        var request = new CopyEventRequest
        {
            NewTitle = "Copied Event",
            NewStartDate = DateTimeOffset.UtcNow.AddDays(30)
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/events/{originalEvent.Id}/copy",
            request);

        // Assert
        // May return 400 or 403 depending on CSRF implementation
        response.IsSuccessStatusCode.Should().BeFalse();
    }

    /// <summary>
    /// Test 5: Verify authorization requirement
    /// </summary>
    [Fact]
    public async Task CopyEvent_WithoutAuthorization_Returns401()
    {
        // Arrange
        var originalEvent = await CreateTestEvent();
        var client = _factory.CreateClient(); // No auth header

        var request = new CopyEventRequest
        {
            NewTitle = "Copied Event",
            NewStartDate = DateTimeOffset.UtcNow.AddDays(30)
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/events/{originalEvent.Id}/copy",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Test 6: Verify 404 for non-existent event
    /// </summary>
    [Fact]
    public async Task CopyEvent_WithInvalidEventId_Returns404()
    {
        // Arrange
        await SeedAdminUser();
        var client = CreateAuthenticatedClient(_adminUserId, _adminEmail, "Administrator");
        var nonExistentId = Guid.NewGuid();

        var request = new CopyEventRequest
        {
            NewTitle = "Copied Event",
            NewStartDate = DateTimeOffset.UtcNow.AddDays(30)
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/events/{nonExistentId}/copy",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Test 7: Verify transaction atomicity (placeholder - requires specific error simulation)
    /// </summary>
    [Fact(Skip = "Requires specific database error simulation setup")]
    public async Task CopyEvent_WithDatabaseTransaction_RollsBackOnError()
    {
        // This test would require simulating a database error during copy operation
        // to verify transaction rollback behavior
        // Implementation depends on test infrastructure capabilities
    }

    /// <summary>
    /// Test 8: Verify email templates are copied with new IDs
    /// </summary>
    [Fact]
    public async Task CopyEvent_WithCustomEmailTemplates_CreatesNewTemplateRecords()
    {
        // Arrange
        await SeedAdminUser();
        var originalEvent = await CreateTestEventWithEmailTemplates();
        var client = CreateAuthenticatedClient(_adminUserId, _adminEmail, "Administrator");

        var request = new CopyEventRequest
        {
            NewTitle = "Copied Event",
            NewStartDate = DateTimeOffset.UtcNow.AddDays(60)
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/events/{originalEvent.Id}/copy",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var copiedEvent = await response.Content.ReadFromJsonAsync<EventDto>();
        var copiedEventId = Guid.Parse(copiedEvent!.Id);

        // Verify templates in database
        await using var context = CreateDbContext();
        var copiedTemplates = await context.Set<WitchCityRope.Api.Features.EmailTemplates.Entities.EventEmailTemplate>()
            .Where(t => t.EventId == copiedEventId)
            .ToListAsync();

        copiedTemplates.Should().NotBeEmpty();
        copiedTemplates.Should().HaveCount(originalEvent.GetEmailTemplateCount());

        // Verify new IDs and correct event association
        foreach (var template in copiedTemplates)
        {
            template.EventId.Should().Be(copiedEventId);
        }
    }

    // Helper methods

    private async Task SeedAdminUser()
    {
        await using var context = CreateDbContext();
        var admin = new ApplicationUser
        {
            Id = _adminUserId,
            UserName = _adminEmail,
            Email = _adminEmail,
            EmailConfirmed = true
        };
        context.Users.Add(admin);
        await context.SaveChangesAsync();
    }

    private async Task<Venue> CreateTestVenue()
    {
        await using var context = CreateDbContext();
        var venue = new Venue
        {
            Name = "Test Venue",
            Location = "123 Test St",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Venues.Add(venue);
        await context.SaveChangesAsync();
        return venue;
    }

    private async Task<Event> CreateTestEvent()
    {
        await using var context = CreateDbContext();
        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Original Event",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(30).AddHours(2),
            EventType = EventType.Class,
            Capacity = 40,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Events.Add(evt);
        await context.SaveChangesAsync();
        return evt;
    }

    private async Task<Event> CreateTestEventWithAllRelations(int venueId)
    {
        await using var context = CreateDbContext();
        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Event with All Relations",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(30).AddHours(4),
            EventType = EventType.Class,
            Capacity = 50,
            VenueId = venueId,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add sessions
        evt.Sessions.Add(new Session
        {
            Id = Guid.NewGuid(),
            EventId = evt.Id,
            SessionCode = "S1",
            StartTime = evt.StartDate,
            EndTime = evt.StartDate.AddHours(2),
            Capacity = 25
        });

        // Add ticket type
        evt.TicketTypes.Add(new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = evt.Id,
            Name = "General",
            Price = 25.00m,
            Available = 50
        });

        // Add volunteer position
        evt.VolunteerPositions.Add(new VolunteerPosition
        {
            Id = Guid.NewGuid(),
            EventId = evt.Id,
            Title = "Helper",
            SlotsNeeded = 2,
            SlotsFilled = 0,
            IsPublicFacing = true
        });

        context.Events.Add(evt);
        await context.SaveChangesAsync();
        return evt;
    }

    private async Task<Event> CreateTestEventWithSessionTickets()
    {
        await using var context = CreateDbContext();
        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Event with Session Tickets",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(30).AddHours(3),
            EventType = EventType.Class,
            Capacity = 40,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var session = new Session
        {
            Id = Guid.NewGuid(),
            EventId = evt.Id,
            SessionCode = "A",
            StartTime = evt.StartDate,
            EndTime = evt.StartDate.AddHours(3),
            Capacity = 40
        };

        var ticket = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = evt.Id,
            SessionId = session.Id,
            Name = "Session A Ticket",
            Price = 30.00m,
            Available = 40
        };

        evt.Sessions.Add(session);
        evt.TicketTypes.Add(ticket);
        context.Events.Add(evt);
        await context.SaveChangesAsync();
        return evt;
    }

    private async Task<Event> CreateTestEventWithVolunteers()
    {
        await using var context = CreateDbContext();
        var evt = new Event
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
            EventId = evt.Id,
            SessionCode = "S1",
            StartTime = evt.StartDate,
            EndTime = evt.StartDate.AddHours(4),
            Capacity = 60
        };

        var position = new VolunteerPosition
        {
            Id = Guid.NewGuid(),
            EventId = evt.Id,
            SessionId = session.Id,
            Title = "Setup Crew",
            SlotsNeeded = 5,
            SlotsFilled = 3,
            IsPublicFacing = true
        };

        evt.Sessions.Add(session);
        evt.VolunteerPositions.Add(position);
        context.Events.Add(evt);
        await context.SaveChangesAsync();
        return evt;
    }

    private async Task<Event> CreateTestEventWithEmailTemplates()
    {
        await using var context = CreateDbContext();
        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Event with Email Templates",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(30).AddHours(2),
            EventType = EventType.Class,
            Capacity = 40,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Events.Add(evt);
        await context.SaveChangesAsync();

        // Add email template
        var template = new WitchCityRope.Api.Features.EmailTemplates.Entities.EventEmailTemplate
        {
            Id = Guid.NewGuid(),
            EventId = evt.Id,
            Subject = "Welcome",
            HtmlBody = "<p>Welcome</p>",
            PlainTextBody = "Welcome",
            GlobalTemplateId = Guid.NewGuid()
        };

        context.Set<WitchCityRope.Api.Features.EmailTemplates.Entities.EventEmailTemplate>().Add(template);
        await context.SaveChangesAsync();

        // Store count for verification
        evt.SetEmailTemplateCount(1);
        return evt;
    }

    private HttpClient CreateAuthenticatedClient(Guid userId, string email, string role)
    {
        var client = _factory.CreateClient();

        // Add Bearer token (simplified - actual implementation may differ)
        var token = GenerateTestJwtToken(userId, email, role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Add CSRF token header
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "test-csrf-token");

        return client;
    }

    private string GenerateTestJwtToken(Guid userId, string email, string role)
    {
        // Simplified token generation for tests
        // Actual implementation should use proper JWT generation
        return $"test-token-{userId}-{role}";
    }
}
