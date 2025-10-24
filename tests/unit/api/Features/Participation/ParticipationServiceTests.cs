using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Participation.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.Fixtures;
using WitchCityRope.Api.Tests.TestBase;

namespace WitchCityRope.Api.Tests.Features.Participation;

/// <summary>
/// Integration tests for ParticipationService
/// Tests against real PostgreSQL database via TestContainers
/// </summary>
public class ParticipationServiceTests : DatabaseTestBase
{
    private ParticipationService _participationService = null!;
    private readonly Mock<ILogger<ParticipationService>> _mockLogger;

    public ParticipationServiceTests(DatabaseTestFixture databaseFixture) : base(databaseFixture)
    {
        _mockLogger = new Mock<ILogger<ParticipationService>>();
    }

    public override async Task InitializeAsync()
    {
        // Call base to initialize DbContext
        await base.InitializeAsync();

        // Create service AFTER DbContext is initialized
        _participationService = new ParticipationService(DbContext, _mockLogger.Object);
    }

    [Fact]
    public async Task GetParticipationStatusAsync_WithNoParticipation_ReturnsNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var result = await _participationService.GetParticipationStatusAsync(eventId, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task CreateRSVPAsync_WithValidVettedUser_CreatesRSVPSuccessfully()
    {
        // Arrange
        var user = CreateTestUser(isVetted: true);
        var socialEvent = CreateTestEvent(EventType.Social);

        await DbContext.Users.AddAsync(user);
        await DbContext.Events.AddAsync(socialEvent);
        await DbContext.SaveChangesAsync();

        var request = new CreateRSVPRequest
        {
            EventId = socialEvent.Id,
            Notes = "Test RSVP notes"
        };

        // Act
        var result = await _participationService.CreateRSVPAsync(request, user.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(socialEvent.Id, result.Value.EventId);
        Assert.Equal(user.Id, result.Value.UserId);
        Assert.Equal(ParticipationType.RSVP, result.Value.ParticipationType);
        Assert.Equal(ParticipationStatus.Active, result.Value.Status);
        Assert.Equal("Test RSVP notes", result.Value.Notes);
        Assert.True(result.Value.CanCancel);

        // Verify in database
        var participation = await DbContext.EventParticipations
            .FirstOrDefaultAsync(ep => ep.EventId == socialEvent.Id && ep.UserId == user.Id);
        Assert.NotNull(participation);
        Assert.Equal(ParticipationType.RSVP, participation.ParticipationType);
    }

    [Fact]
    public async Task CreateRSVPAsync_WithNonVettedUser_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser(isVetted: false);
        var socialEvent = CreateTestEvent(EventType.Social);

        await DbContext.Users.AddAsync(user);
        await DbContext.Events.AddAsync(socialEvent);
        await DbContext.SaveChangesAsync();

        var request = new CreateRSVPRequest
        {
            EventId = socialEvent.Id,
            Notes = "Test RSVP notes"
        };

        // Act
        var result = await _participationService.CreateRSVPAsync(request, user.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("vetted members", result.Error);
    }

    [Fact]
    public async Task CreateRSVPAsync_ForClassEvent_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser(isVetted: true);
        var classEvent = CreateTestEvent(EventType.Class);

        await DbContext.Users.AddAsync(user);
        await DbContext.Events.AddAsync(classEvent);
        await DbContext.SaveChangesAsync();

        var request = new CreateRSVPRequest
        {
            EventId = classEvent.Id,
            Notes = "Test RSVP notes"
        };

        // Act
        var result = await _participationService.CreateRSVPAsync(request, user.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("social events", result.Error);
    }

    [Fact]
    public async Task CreateRSVPAsync_ForFullEvent_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser(isVetted: true);
        var socialEvent = CreateTestEvent(EventType.Social, capacity: 1);

        await DbContext.Users.AddAsync(user);
        await DbContext.Events.AddAsync(socialEvent);

        // Create existing participation to fill capacity
        var existingUser = CreateTestUser(isVetted: true, email: "existing@test.com", sceneName: "ExistingUser");
        await DbContext.Users.AddAsync(existingUser);

        var existingParticipation = new EventParticipation(socialEvent.Id, existingUser.Id, ParticipationType.RSVP);
        await DbContext.EventParticipations.AddAsync(existingParticipation);

        await DbContext.SaveChangesAsync();

        var request = new CreateRSVPRequest
        {
            EventId = socialEvent.Id,
            Notes = "Test RSVP notes"
        };

        // Act
        var result = await _participationService.CreateRSVPAsync(request, user.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("full capacity", result.Error);
    }

    [Fact]
    public async Task CancelParticipationAsync_WithActiveParticipation_CancelsSuccessfully()
    {
        // Arrange
        var user = CreateTestUser(isVetted: true);
        var socialEvent = CreateTestEvent(EventType.Social);

        await DbContext.Users.AddAsync(user);
        await DbContext.Events.AddAsync(socialEvent);

        var participation = new EventParticipation(socialEvent.Id, user.Id, ParticipationType.RSVP);
        await DbContext.EventParticipations.AddAsync(participation);

        await DbContext.SaveChangesAsync();

        // Act
        var result = await _participationService.CancelParticipationAsync(
            socialEvent.Id, user.Id, "Test cancellation reason");

        // Assert
        Assert.True(result.IsSuccess);

        // Verify in database
        var updatedParticipation = await DbContext.EventParticipations
            .FirstOrDefaultAsync(ep => ep.Id == participation.Id);
        Assert.NotNull(updatedParticipation);
        Assert.Equal(ParticipationStatus.Cancelled, updatedParticipation.Status);
        Assert.NotNull(updatedParticipation.CancelledAt);
        Assert.Equal("Test cancellation reason", updatedParticipation.CancellationReason);
    }

    [Fact]
    public async Task GetUserParticipationsAsync_WithMultipleParticipations_ReturnsAllParticipations()
    {
        // Arrange
        var user = CreateTestUser(isVetted: true);
        var event1 = CreateTestEvent(EventType.Social, title: "Event 1");
        var event2 = CreateTestEvent(EventType.Social, title: "Event 2");

        await DbContext.Users.AddAsync(user);
        await DbContext.Events.AddRangeAsync(event1, event2);

        var participation1 = new EventParticipation(event1.Id, user.Id, ParticipationType.RSVP);
        var participation2 = new EventParticipation(event2.Id, user.Id, ParticipationType.RSVP);
        await DbContext.EventParticipations.AddRangeAsync(participation1, participation2);

        await DbContext.SaveChangesAsync();

        // Act
        var result = await _participationService.GetUserParticipationsAsync(user.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);

        var eventTitles = result.Value.Select(p => p.EventTitle).OrderBy(t => t).ToList();
        Assert.Equal(new[] { "Event 1", "Event 2" }, eventTitles);
    }

    private ApplicationUser CreateTestUser(bool isVetted, string email = "test@example.com", string sceneName = "TestUser")
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            SceneName = sceneName,
            VettingStatus = isVetted ? 3 : 0, // 3 = Approved (vetted), 0 = UnderReview (not vetted)
            IsActive = true,
            Role = "Member",
            EncryptedLegalName = "Test User",
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            PronouncedName = "Test User",
            Pronouns = "they/them",
            EmailVerificationToken = string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private Event CreateTestEvent(EventType eventType, string title = "Test Event", int capacity = 10)
    {
        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "Test event description",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Capacity = capacity,
            EventType = eventType,
            Location = "Test Location",
            IsPublished = true,
            PricingTiers = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}