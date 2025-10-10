using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Participation.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.Fixtures;
using WitchCityRope.Api.Tests.TestBase;

namespace WitchCityRope.Api.Tests.Features.Participation;

/// <summary>
/// Extended integration tests for ParticipationService
/// Tests ticket purchase workflows, event participation management, and edge cases
/// Phase 1.5.3: Participation & Events API Test Suite
/// Created: 2025-10-10
/// </summary>
[Collection("Database")]
public class ParticipationServiceTests_Extended : DatabaseTestBase
{
    private readonly ParticipationService _participationService;
    private readonly Mock<ILogger<ParticipationService>> _mockLogger;

    public ParticipationServiceTests_Extended(DatabaseTestFixture databaseFixture) : base(databaseFixture)
    {
        _mockLogger = new Mock<ILogger<ParticipationService>>();
        _participationService = new ParticipationService(DbContext, _mockLogger.Object);
    }

    #region Ticket Purchase Tests

    [Fact]
    public async Task CreateTicketPurchaseAsync_WithValidAuthenticatedUser_CreatesTicketSuccessfully()
    {
        // Arrange
        var user = CreateTestUser(isVetted: false); // Non-vetted user can purchase tickets
        var classEvent = CreateTestEvent("Class");

        await DbContext.Users.AddAsync(user);
        await DbContext.Events.AddAsync(classEvent);
        await DbContext.SaveChangesAsync();

        var request = new CreateTicketPurchaseRequest
        {
            EventId = classEvent.Id,
            PaymentMethodId = "pm_test_12345",
            Notes = "Looking forward to this class"
        };

        // Act
        var result = await _participationService.CreateTicketPurchaseAsync(request, user.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(classEvent.Id, result.Value.EventId);
        Assert.Equal(user.Id, result.Value.UserId);
        Assert.Equal(ParticipationType.Ticket, result.Value.ParticipationType);
        Assert.Equal(ParticipationStatus.Active, result.Value.Status);

        // Verify in database
        var participation = await DbContext.EventParticipations
            .FirstOrDefaultAsync(ep => ep.EventId == classEvent.Id && ep.UserId == user.Id);
        Assert.NotNull(participation);
        Assert.Equal(ParticipationType.Ticket, participation.ParticipationType);

        // Verify history was created
        var history = await DbContext.ParticipationHistory
            .FirstOrDefaultAsync(ph => ph.ParticipationId == participation.Id);
        Assert.NotNull(history);
        Assert.Equal("Created", history.ActionType);
    }

    [Fact]
    public async Task CreateTicketPurchaseAsync_ForSocialEvent_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser(isVetted: true);
        var socialEvent = CreateTestEvent("Social");

        await DbContext.Users.AddAsync(user);
        await DbContext.Events.AddAsync(socialEvent);
        await DbContext.SaveChangesAsync();

        var request = new CreateTicketPurchaseRequest
        {
            EventId = socialEvent.Id,
            PaymentMethodId = "pm_test_12345"
        };

        // Act
        var result = await _participationService.CreateTicketPurchaseAsync(request, user.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("class events", result.Error);
    }

    [Fact]
    public async Task CreateTicketPurchaseAsync_ForFullEvent_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser(isVetted: true);
        var classEvent = CreateTestEvent("Class", capacity: 1);

        await DbContext.Users.AddAsync(user);
        await DbContext.Events.AddAsync(classEvent);

        // Create existing participation to fill capacity
        var existingUser = CreateTestUser(isVetted: true, email: "existing@test.com", sceneName: "ExistingUser");
        await DbContext.Users.AddAsync(existingUser);

        var existingParticipation = new EventParticipation(classEvent.Id, existingUser.Id, ParticipationType.Ticket);
        await DbContext.EventParticipations.AddAsync(existingParticipation);

        await DbContext.SaveChangesAsync();

        var request = new CreateTicketPurchaseRequest
        {
            EventId = classEvent.Id,
            PaymentMethodId = "pm_test_12345"
        };

        // Act
        var result = await _participationService.CreateTicketPurchaseAsync(request, user.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("full capacity", result.Error);
    }

    [Fact]
    public async Task CreateTicketPurchaseAsync_WithDuplicateActiveTicket_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser(isVetted: true);
        var classEvent = CreateTestEvent("Class");

        await DbContext.Users.AddAsync(user);
        await DbContext.Events.AddAsync(classEvent);

        // Create existing active ticket
        var existingParticipation = new EventParticipation(classEvent.Id, user.Id, ParticipationType.Ticket);
        await DbContext.EventParticipations.AddAsync(existingParticipation);

        await DbContext.SaveChangesAsync();

        var request = new CreateTicketPurchaseRequest
        {
            EventId = classEvent.Id,
            PaymentMethodId = "pm_test_12345"
        };

        // Act
        var result = await _participationService.CreateTicketPurchaseAsync(request, user.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("active participation", result.Error);
    }

    [Fact]
    public async Task CreateTicketPurchaseAsync_AfterCancelledTicket_CreatesNewTicketSuccessfully()
    {
        // Arrange - Test that cancelled tickets allow re-purchase
        var user = CreateTestUser(isVetted: true);
        var classEvent = CreateTestEvent("Class");

        await DbContext.Users.AddAsync(user);
        await DbContext.Events.AddAsync(classEvent);

        // Create cancelled ticket
        var cancelledParticipation = new EventParticipation(classEvent.Id, user.Id, ParticipationType.Ticket);
        cancelledParticipation.Cancel("Changed my mind");
        await DbContext.EventParticipations.AddAsync(cancelledParticipation);

        await DbContext.SaveChangesAsync();

        var request = new CreateTicketPurchaseRequest
        {
            EventId = classEvent.Id,
            PaymentMethodId = "pm_test_12345"
        };

        // Act
        var result = await _participationService.CreateTicketPurchaseAsync(request, user.Id);

        // Assert - Should succeed because cancelled tickets don't prevent new purchases
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(ParticipationStatus.Active, result.Value.Status);
    }

    [Fact]
    public async Task CreateTicketPurchaseAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var classEvent = CreateTestEvent("Class");
        await DbContext.Events.AddAsync(classEvent);
        await DbContext.SaveChangesAsync();

        var request = new CreateTicketPurchaseRequest
        {
            EventId = classEvent.Id,
            PaymentMethodId = "pm_test_12345"
        };

        // Act
        var result = await _participationService.CreateTicketPurchaseAsync(request, Guid.NewGuid());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("User not found", result.Error);
    }

    [Fact]
    public async Task CreateTicketPurchaseAsync_WithNonExistentEvent_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser(isVetted: true);
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();

        var request = new CreateTicketPurchaseRequest
        {
            EventId = Guid.NewGuid(),
            PaymentMethodId = "pm_test_12345"
        };

        // Act
        var result = await _participationService.CreateTicketPurchaseAsync(request, user.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Event not found", result.Error);
    }

    #endregion

    #region Event Participations Management Tests

    [Fact]
    public async Task GetEventParticipationsAsync_WithMultipleParticipations_ReturnsAllParticipations()
    {
        // Arrange
        var event1 = CreateTestEvent("Social");
        await DbContext.Events.AddAsync(event1);

        var user1 = CreateTestUser(isVetted: true, email: "user1@test.com", sceneName: "User1");
        var user2 = CreateTestUser(isVetted: true, email: "user2@test.com", sceneName: "User2");
        var user3 = CreateTestUser(isVetted: true, email: "user3@test.com", sceneName: "User3");
        await DbContext.Users.AddRangeAsync(user1, user2, user3);

        var participation1 = new EventParticipation(event1.Id, user1.Id, ParticipationType.RSVP);
        var participation2 = new EventParticipation(event1.Id, user2.Id, ParticipationType.RSVP);
        var participation3 = new EventParticipation(event1.Id, user3.Id, ParticipationType.RSVP)
        {
            Status = ParticipationStatus.Cancelled,
            CancelledAt = DateTime.UtcNow,
            CancellationReason = "Unable to attend"
        };
        await DbContext.EventParticipations.AddRangeAsync(participation1, participation2, participation3);

        await DbContext.SaveChangesAsync();

        // Act
        var result = await _participationService.GetEventParticipationsAsync(event1.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.Count); // Returns all participations including cancelled

        // Verify contains all users
        var userSceneNames = result.Value.Select(p => p.UserSceneName).OrderBy(n => n).ToList();
        Assert.Equal(new[] { "User1", "User2", "User3" }, userSceneNames);

        // Verify cancelled status is reflected
        var cancelledParticipation = result.Value.First(p => p.UserSceneName == "User3");
        Assert.Equal(ParticipationStatus.Cancelled, cancelledParticipation.Status);
        Assert.False(cancelledParticipation.CanCancel); // Cannot cancel already cancelled participation
    }

    [Fact]
    public async Task GetEventParticipationsAsync_WithNoParticipations_ReturnsEmptyList()
    {
        // Arrange
        var event1 = CreateTestEvent("Social");
        await DbContext.Events.AddAsync(event1);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _participationService.GetEventParticipationsAsync(event1.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetEventParticipationsAsync_WithMixedParticipationTypes_ReturnsAllTypes()
    {
        // Arrange
        var event1 = CreateTestEvent("Workshop", capacity: 20); // Can have both RSVPs and tickets
        await DbContext.Events.AddAsync(event1);

        var user1 = CreateTestUser(isVetted: true, email: "rsvp@test.com", sceneName: "RSVPUser");
        var user2 = CreateTestUser(isVetted: true, email: "ticket@test.com", sceneName: "TicketUser");
        await DbContext.Users.AddRangeAsync(user1, user2);

        var rsvpParticipation = new EventParticipation(event1.Id, user1.Id, ParticipationType.RSVP);
        var ticketParticipation = new EventParticipation(event1.Id, user2.Id, ParticipationType.Ticket);
        await DbContext.EventParticipations.AddRangeAsync(rsvpParticipation, ticketParticipation);

        await DbContext.SaveChangesAsync();

        // Act
        var result = await _participationService.GetEventParticipationsAsync(event1.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Contains(result.Value, p => p.ParticipationType == ParticipationType.RSVP);
        Assert.Contains(result.Value, p => p.ParticipationType == ParticipationType.Ticket);
    }

    #endregion

    #region Participation Status Edge Cases

    [Fact]
    public async Task GetParticipationStatusAsync_WithCancelledParticipation_ReturnsNull()
    {
        // Arrange - Cancelled participations should not be returned (only active ones count)
        var user = CreateTestUser(isVetted: true);
        var event1 = CreateTestEvent("Social");

        await DbContext.Users.AddAsync(user);
        await DbContext.Events.AddAsync(event1);

        var participation = new EventParticipation(event1.Id, user.Id, ParticipationType.RSVP);
        participation.Cancel("Test cancellation");
        await DbContext.EventParticipations.AddAsync(participation);

        await DbContext.SaveChangesAsync();

        // Act
        var result = await _participationService.GetParticipationStatusAsync(event1.Id, user.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value); // Cancelled participations are not returned
    }

    [Fact]
    public async Task GetParticipationStatusAsync_WithMultipleParticipations_ReturnsMostRecentActive()
    {
        // Arrange
        var user = CreateTestUser(isVetted: true);
        var event1 = CreateTestEvent("Social");

        await DbContext.Users.AddAsync(user);
        await DbContext.Events.AddAsync(event1);

        // Create older cancelled participation
        var oldParticipation = new EventParticipation(event1.Id, user.Id, ParticipationType.RSVP)
        {
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        };
        oldParticipation.Cancel("Changed mind");
        await DbContext.EventParticipations.AddAsync(oldParticipation);
        await DbContext.SaveChangesAsync(); // Save cancelled participation before creating new one

        // Create newer active participation
        var newParticipation = new EventParticipation(event1.Id, user.Id, ParticipationType.RSVP)
        {
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            Notes = "Re-registered after cancellation"
        };
        await DbContext.EventParticipations.AddAsync(newParticipation);

        await DbContext.SaveChangesAsync();

        // Act
        var result = await _participationService.GetParticipationStatusAsync(event1.Id, user.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(ParticipationStatus.Active, result.Value.Status);
        Assert.Equal("Re-registered after cancellation", result.Value.Notes);
    }

    #endregion

    #region Cancellation Edge Cases

    [Fact]
    public async Task CancelParticipationAsync_WithAlreadyCancelledParticipation_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser(isVetted: true);
        var event1 = CreateTestEvent("Social");

        await DbContext.Users.AddAsync(user);
        await DbContext.Events.AddAsync(event1);

        var participation = new EventParticipation(event1.Id, user.Id, ParticipationType.RSVP);
        participation.Cancel("First cancellation");
        await DbContext.EventParticipations.AddAsync(participation);

        await DbContext.SaveChangesAsync();

        // Act - Try to cancel already cancelled participation
        var result = await _participationService.CancelParticipationAsync(event1.Id, user.Id, "Second cancellation");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("No active participation", result.Error);
    }

    [Fact]
    public async Task CancelParticipationAsync_WithNoParticipation_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser(isVetted: true);
        var event1 = CreateTestEvent("Social");

        await DbContext.Users.AddAsync(user);
        await DbContext.Events.AddAsync(event1);
        await DbContext.SaveChangesAsync();

        // Act - Try to cancel non-existent participation
        var result = await _participationService.CancelParticipationAsync(event1.Id, user.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("No active participation", result.Error);
    }

    #endregion

    #region Helper Methods

    private ApplicationUser CreateTestUser(bool isVetted, string email = "test@example.com", string sceneName = "TestUser")
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            SceneName = $"{sceneName}_{Guid.NewGuid()}",
            IsVetted = isVetted,
            IsActive = true,
            Role = "Member",
            EncryptedLegalName = "Test User",
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            PronouncedName = "Test User",
            Pronouns = "they/them",
            EmailVerificationToken = string.Empty,
            VettingStatus = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private Event CreateTestEvent(string eventType, string title = "Test Event", int capacity = 10)
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

    #endregion
}
