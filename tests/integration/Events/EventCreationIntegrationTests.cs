using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net;
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
/// Integration tests for Event Creation endpoint (POST /api/events)
/// Tests complete end-to-end workflow with real database and HTTP requests
/// Verifies CSRF protection, auth, validation, and database persistence
/// </summary>
[Collection("Database")]
public class EventCreationIntegrationTests : IntegrationTestBase, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Guid _adminUserId = Guid.NewGuid();
    private readonly string _adminEmail = $"admin-{Guid.NewGuid():N}@example.com";
    private int _testVenueId;

    public EventCreationIntegrationTests(DatabaseTestFixture fixture) : base(fixture)
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

        // Create test venue
        using var context = CreateDbContext();
        var venue = new Venue
        {
            Name = "Integration Test Venue",
            Location = "123 Test St",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Venues.Add(venue);
        context.SaveChanges();
        _testVenueId = venue.Id;
    }

    public void Dispose()
    {
        _factory?.Dispose();
    }

    [Fact]
    public async Task POST_Events_ValidRequest_Returns200WithEventDto()
    {
        // Arrange
        await SeedAdminUser();
        var bearerToken = GenerateJwtToken(_adminUserId, _adminEmail, "Administrator");
        var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, bearerToken);

        var request = new CreateEventRequest
        {
            Title = "Integration Test Event",
            Description = "Test event description",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            VenueId = _testVenueId,
            EventType = "Class",
            Capacity = 20
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var eventDto = await response.Content.ReadFromJsonAsync<EventDto>();
        eventDto.Should().NotBeNull();
        eventDto!.Title.Should().Be(request.Title);
        eventDto.Description.Should().Be(request.Description);
        eventDto.EventType.Should().Be("Workshop");
        eventDto.Capacity.Should().Be(20);
        eventDto.IsPublished.Should().BeFalse(); // Draft by default
    }

    [Fact]
    public async Task POST_Events_WithAllRelations_CreatesDeepStructure()
    {
        // Arrange
        await SeedAdminUser();
        var teacher = await CreateTestTeacher();
        var bearerToken = GenerateJwtToken(_adminUserId, _adminEmail, "Administrator");
        var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, bearerToken);

        var request = new CreateEventRequest
        {
            Title = "Full Event with Relations",
            Description = "Event with all related entities",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(4),
            VenueId = _testVenueId,
            EventType = "Class",
            Capacity = 50,
            Sessions = new List<SessionDto>
            {
                new SessionDto
                {
                    SessionIdentifier = "S1",
                    Name = "Morning Session",
                    StartTime = DateTime.UtcNow.AddDays(7).AddHours(9),
                    EndTime = DateTime.UtcNow.AddDays(7).AddHours(12),
                    Capacity = 25
                }
            },
            TicketTypes = new List<TicketTypeDto>
            {
                new TicketTypeDto
                {
                    Name = "General",
                    PricingType = WitchCityRope.Models.PricingType.Fixed,
                    Price = 25m,
                    QuantityAvailable = 50,
                    SessionIdentifiers = new List<string> { "S1" }
                }
            },
            VolunteerPositions = new List<VolunteerPositionDto>
            {
                new VolunteerPositionDto
                {
                    Title = "Setup Crew",
                    Description = "Help setup",
                    SlotsNeeded = 2,
                    SlotsFilled = 0
                }
            },
            TeacherIds = new List<string> { teacher.Id.ToString() }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var eventDto = await response.Content.ReadFromJsonAsync<EventDto>();
        eventDto.Should().NotBeNull();
        eventDto!.Sessions.Should().HaveCount(1);
        eventDto.TicketTypes.Should().HaveCount(1);
        eventDto.VolunteerPositions.Should().HaveCount(1);
        eventDto.TeacherIds.Should().HaveCount(1);

        // Verify in database
        await using var context = CreateDbContext();
        var dbEvent = await context.Events
            .Include(e => e.Sessions)
            .Include(e => e.TicketTypes)
            .Include(e => e.VolunteerPositions)
            .Include(e => e.Organizers)
            .FirstOrDefaultAsync(e => e.Id == Guid.Parse(eventDto.Id));

        dbEvent.Should().NotBeNull();
        dbEvent!.Sessions.Should().HaveCount(1);
        dbEvent.TicketTypes.Should().HaveCount(1);
        dbEvent.VolunteerPositions.Should().HaveCount(1);
        dbEvent.Organizers.Should().HaveCount(1);
    }

    [Fact]
    public async Task POST_Events_WithoutCsrfToken_Returns400()
    {
        // Arrange
        await SeedAdminUser();
        var bearerToken = GenerateJwtToken(_adminUserId, _adminEmail, "Administrator");
        var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, bearerToken);

        // Remove CSRF token header
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");

        var request = new CreateEventRequest
        {
            Title = "Test Event",
            Description = "Test",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            VenueId = _testVenueId,
            EventType = "Class",
            Capacity = 20
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Events_Unauthenticated_Returns401()
    {
        // Arrange - No authentication
        var client = _factory.CreateClient();

        var request = new CreateEventRequest
        {
            Title = "Test Event",
            Description = "Test",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            VenueId = _testVenueId,
            EventType = "Class",
            Capacity = 20
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_Events_InvalidDates_Returns400WithValidationError()
    {
        // Arrange
        await SeedAdminUser();
        var bearerToken = GenerateJwtToken(_adminUserId, _adminEmail, "Administrator");
        var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, bearerToken);

        var request = new CreateEventRequest
        {
            Title = "Invalid Date Event",
            Description = "Start after end",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(6), // End before start!
            VenueId = _testVenueId,
            EventType = "Class",
            Capacity = 20
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("date");
    }

    [Fact]
    public async Task POST_Events_MissingRequiredFields_Returns400()
    {
        // Arrange
        await SeedAdminUser();
        var bearerToken = GenerateJwtToken(_adminUserId, _adminEmail, "Administrator");
        var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, bearerToken);

        var request = new CreateEventRequest
        {
            // Missing title, description, etc.
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Events_CreatesEventInDatabase_VerifyPersistence()
    {
        // Arrange
        await SeedAdminUser();
        var bearerToken = GenerateJwtToken(_adminUserId, _adminEmail, "Administrator");
        var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, bearerToken);

        var request = new CreateEventRequest
        {
            Title = $"Persistence Test {Guid.NewGuid()}",
            Description = "Verify database persistence",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            VenueId = _testVenueId,
            EventType = "Social",
            Capacity = 30
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var eventDto = await response.Content.ReadFromJsonAsync<EventDto>();
        var eventId = Guid.Parse(eventDto!.Id);

        // Verify in database
        await using var context = CreateDbContext();
        var dbEvent = await context.Events.FindAsync(eventId);

        dbEvent.Should().NotBeNull();
        dbEvent!.Title.Should().Be(request.Title);
        dbEvent.Description.Should().Be(request.Description);
        dbEvent.EventType.ToString().Should().Be("Social");
        dbEvent.Capacity.Should().Be(30);
    }

    [Fact]
    public async Task POST_Events_SetsIsPublishedFalse_ByDefault()
    {
        // Arrange
        await SeedAdminUser();
        var bearerToken = GenerateJwtToken(_adminUserId, _adminEmail, "Administrator");
        var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, bearerToken);

        var request = new CreateEventRequest
        {
            Title = "Draft Event",
            Description = "Should be draft by default",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            VenueId = _testVenueId,
            EventType = "Class",
            Capacity = 20
            // IsPublished not set - should default to false
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var eventDto = await response.Content.ReadFromJsonAsync<EventDto>();
        eventDto!.IsPublished.Should().BeFalse();

        // Verify in database
        await using var context = CreateDbContext();
        var dbEvent = await context.Events.FindAsync(Guid.Parse(eventDto.Id));
        dbEvent!.IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task POST_Events_GeneratesValidGuids_ForAllEntities()
    {
        // Arrange
        await SeedAdminUser();
        var bearerToken = GenerateJwtToken(_adminUserId, _adminEmail, "Administrator");
        var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, bearerToken);

        var request = new CreateEventRequest
        {
            Title = "GUID Test Event",
            Description = "Verify GUIDs generated",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(4),
            VenueId = _testVenueId,
            EventType = "Class",
            Capacity = 50,
            Sessions = new List<SessionDto>
            {
                new SessionDto
                {
                    SessionIdentifier = "S1",
                    Name = "Session 1",
                    StartTime = DateTime.UtcNow.AddDays(7),
                    EndTime = DateTime.UtcNow.AddDays(7).AddHours(2),
                    Capacity = 25
                }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var eventDto = await response.Content.ReadFromJsonAsync<EventDto>();

        // Verify Event GUID
        Guid.TryParse(eventDto!.Id, out var eventGuid).Should().BeTrue();
        eventGuid.Should().NotBe(Guid.Empty);

        // Verify Session GUID
        Guid.TryParse(eventDto.Sessions.First().Id, out var sessionGuid).Should().BeTrue();
        sessionGuid.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task POST_Events_InvalidEventType_Returns400()
    {
        // Arrange
        await SeedAdminUser();
        var bearerToken = GenerateJwtToken(_adminUserId, _adminEmail, "Administrator");
        var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, bearerToken);

        var request = new CreateEventRequest
        {
            Title = "Invalid Type Event",
            Description = "Test invalid event type",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            VenueId = _testVenueId,
            EventType = "InvalidTypeNotInEnum",
            Capacity = 20
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("event type");
    }

    #region Helper Methods

    private async Task SeedAdminUser()
    {
        await using var context = CreateDbContext();

        var admin = new ApplicationUser
        {
            Id = _adminUserId,
            Email = _adminEmail,
            UserName = _adminEmail,
            SceneName = "Admin User",
            EmailConfirmed = true
        };

        context.Users.Add(admin);
        await context.SaveChangesAsync();
    }

    private async Task<ApplicationUser> CreateTestTeacher()
    {
        await using var context = CreateDbContext();

        var teacher = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = $"teacher-{Guid.NewGuid():N}@test.com",
            UserName = $"teacher-{Guid.NewGuid():N}@test.com",
            SceneName = "Test Teacher"
        };

        context.Users.Add(teacher);
        await context.SaveChangesAsync();

        return teacher;
    }

    #endregion
}
