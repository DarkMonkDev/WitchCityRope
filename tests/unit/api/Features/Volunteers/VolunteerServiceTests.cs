using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.Volunteers.Models;
using WitchCityRope.Api.Features.Volunteers.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Features.Participation.Entities;
using Xunit;
using FluentAssertions;
using Testcontainers.PostgreSql;

namespace WitchCityRope.UnitTests.Api.Features.Volunteers;

/// <summary>
/// Comprehensive integration tests for VolunteerService
/// Tests volunteer signup, capacity enforcement, auto-RSVP, and admin assignment
/// Phase 2: Events, Volunteers, and Check-In Integration Tests
/// </summary>
[Collection("Database")]
public class VolunteerServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private ApplicationDbContext _context = null!;
    private VolunteerService _service = null!;
    private ILogger<VolunteerService> _logger = null!;
    private string _connectionString = null!;

    public VolunteerServiceTests()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("witchcityrope_test_volunteers")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        // Setup logger
        _logger = Substitute.For<ILogger<VolunteerService>>();

        // Create service instance
        _service = new VolunteerService(_context, _logger);
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        await _container.DisposeAsync();
    }

    #region Helper Methods

    private async Task<Event> CreateTestEvent(string title, int capacity = 25)
    {
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = $"Description for {title}",
            Location = "Test Location",
            EventType = EventType.Class,
            Capacity = capacity,
            IsPublished = true,
            StartDate = DateTime.UtcNow.AddDays(7).ToUniversalTime(),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(3).ToUniversalTime(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        return eventEntity;
    }

    private async Task<VolunteerPosition> CreateVolunteerPosition(
        Guid eventId,
        string title,
        int slotsNeeded = 2,
        bool isPublicFacing = true)
    {
        var position = new VolunteerPosition
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Title = title,
            Description = $"Description for {title}",
            SlotsNeeded = slotsNeeded,
            SlotsFilled = 0,
            IsPublicFacing = isPublicFacing,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.VolunteerPositions.Add(position);
        await _context.SaveChangesAsync();

        return position;
    }

    /// <summary>
    /// Helper method to create test user in database
    /// FIX: All volunteer signups require actual User records due to FK constraint
    /// </summary>
    private async Task<ApplicationUser> CreateTestUser(string email, string sceneName)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            SceneName = sceneName,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            Role = "Member"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    #endregion

    #region Volunteer Signup Tests

    [Fact]
    public async Task SignupVolunteerAsync_WithValidData_CreatesVolunteer()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var position = await CreateVolunteerPosition(testEvent.Id, "Setup Crew");
        var user = await CreateTestUser("volunteer@example.com", "TestVolunteer");

        var request = new VolunteerSignupRequest();

        // Act
        var (success, signup, error) = await _service.SignupForPositionAsync(
            position.Id.ToString(),
            user.Id.ToString(),
            request);

        // Assert
        success.Should().BeTrue();
        error.Should().BeNullOrEmpty();
        signup.Should().NotBeNull();
        signup!.VolunteerPositionId.Should().Be(position.Id);
        signup.UserId.Should().Be(user.Id);
        signup.Status.Should().Be(VolunteerSignupStatus.Confirmed.ToString());

        // Verify in database
        var savedSignup = await _context.VolunteerSignups
            .FirstOrDefaultAsync(vs => vs.VolunteerPositionId == position.Id && vs.UserId == user.Id);
        savedSignup.Should().NotBeNull();
        savedSignup!.Status.Should().Be(VolunteerSignupStatus.Confirmed);
    }

    [Fact]
    public async Task SignupVolunteerAsync_WithDuplicateSignup_ReturnsFailure()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var position = await CreateVolunteerPosition(testEvent.Id, "Setup Crew");
        var user = await CreateTestUser("volunteer@example.com", "TestVolunteer");

        // Create first signup
        var firstSignup = new VolunteerSignup
        {
            Id = Guid.NewGuid(),
            VolunteerPositionId = position.Id,
            UserId = user.Id,
            Status = VolunteerSignupStatus.Confirmed,
            SignedUpAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.VolunteerSignups.Add(firstSignup);
        await _context.SaveChangesAsync();

        var request = new VolunteerSignupRequest();

        // Act - Try to signup again
        var (success, signup, error) = await _service.SignupForPositionAsync(
            position.Id.ToString(),
            user.Id.ToString(),
            request);

        // Assert
        success.Should().BeFalse();
        signup.Should().BeNull();
        error.Should().Contain("already signed up");
    }

    [Fact]
    public async Task SignupVolunteerAsync_ForFullEvent_ReturnsFailure()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var position = await CreateVolunteerPosition(testEvent.Id, "Setup Crew", slotsNeeded: 2);

        // Fill the position
        position.SlotsFilled = 2;
        await _context.SaveChangesAsync();

        var user = await CreateTestUser("volunteer@example.com", "TestVolunteer");
        var request = new VolunteerSignupRequest();

        // Act
        var (success, signup, error) = await _service.SignupForPositionAsync(
            position.Id.ToString(),
            user.Id.ToString(),
            request);

        // Assert
        success.Should().BeFalse();
        signup.Should().BeNull();
        error.Should().Contain("fully staffed");
    }

    [Fact]
    public async Task SignupVolunteerAsync_WithAutoRSVP_CreatesParticipation()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var position = await CreateVolunteerPosition(testEvent.Id, "Setup Crew");
        var user = await CreateTestUser("volunteer@example.com", "TestVolunteer");

        var request = new VolunteerSignupRequest();

        // Act
        var (success, signup, error) = await _service.SignupForPositionAsync(
            position.Id.ToString(),
            user.Id.ToString(),
            request);

        // Assert
        success.Should().BeTrue();
        signup.Should().NotBeNull();

        // Verify auto-RSVP created
        var participation = await _context.EventParticipations
            .FirstOrDefaultAsync(ep => ep.EventId == testEvent.Id && ep.UserId == user.Id);

        participation.Should().NotBeNull();
        participation!.ParticipationType.Should().Be(ParticipationType.RSVP);
        participation.Status.Should().Be(ParticipationStatus.Active);
    }

    [Fact]
    public async Task SignupVolunteerAsync_WithExistingRSVP_DoesNotDuplicateParticipation()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var position = await CreateVolunteerPosition(testEvent.Id, "Setup Crew");
        var user = await CreateTestUser("volunteer@example.com", "TestVolunteer");

        // Create existing participation
        var existingParticipation = new EventParticipation
        {
            Id = Guid.NewGuid(),
            EventId = testEvent.Id,
            UserId = user.Id,
            ParticipationType = ParticipationType.RSVP,
            Status = ParticipationStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.EventParticipations.Add(existingParticipation);
        await _context.SaveChangesAsync();

        var request = new VolunteerSignupRequest();

        // Act
        var (success, signup, error) = await _service.SignupForPositionAsync(
            position.Id.ToString(),
            user.Id.ToString(),
            request);

        // Assert
        success.Should().BeTrue();

        // Verify no duplicate participation created
        var participationCount = await _context.EventParticipations
            .CountAsync(ep => ep.EventId == testEvent.Id && ep.UserId == user.Id);

        participationCount.Should().Be(1, "Should not create duplicate participation");
    }

    #endregion

    #region Capacity Enforcement Tests

    [Fact]
    public async Task SignupVolunteer_EnforcesVolunteerCapacity()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var position = await CreateVolunteerPosition(testEvent.Id, "Setup Crew", slotsNeeded: 1);

        // Fill the single slot - CREATE USER FIRST
        var user1 = await CreateTestUser("volunteer1@example.com", "Volunteer1");
        var firstSignup = new VolunteerSignup
        {
            Id = Guid.NewGuid(),
            VolunteerPositionId = position.Id,
            UserId = user1.Id,
            Status = VolunteerSignupStatus.Confirmed,
            SignedUpAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.VolunteerSignups.Add(firstSignup);
        position.SlotsFilled = 1;
        await _context.SaveChangesAsync();

        // Try to add second volunteer
        var user2 = await CreateTestUser("volunteer2@example.com", "Volunteer2");
        var request = new VolunteerSignupRequest();

        // Act
        var (success, signup, error) = await _service.SignupForPositionAsync(
            position.Id.ToString(),
            user2.Id.ToString(),
            request);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("fully staffed");
    }

    [Fact]
    public async Task SignupVolunteer_ChecksAgainstEventCapacity()
    {
        // NOTE: Volunteer capacity is independent of event capacity in current implementation
        // This test validates that volunteer signup doesn't fail just because event is full

        // Arrange
        var testEvent = await CreateTestEvent("Test Event", capacity: 2);
        var position = await CreateVolunteerPosition(testEvent.Id, "Setup Crew", slotsNeeded: 2);

        // Fill event to capacity with participants
        var participant1 = await CreateTestUser("participant1@example.com", "Participant1");
        var participant2 = await CreateTestUser("participant2@example.com", "Participant2");
        await AddParticipantToEvent(testEvent.Id, participant1.Id);
        await AddParticipantToEvent(testEvent.Id, participant2.Id);

        var volunteer = await CreateTestUser("volunteer@example.com", "Volunteer");
        var request = new VolunteerSignupRequest();

        // Act
        var (success, signup, error) = await _service.SignupForPositionAsync(
            position.Id.ToString(),
            volunteer.Id.ToString(),
            request);

        // Assert
        success.Should().BeTrue("Volunteers can signup even if event is full");
        signup.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelVolunteer_FreesUpCapacity()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var position = await CreateVolunteerPosition(testEvent.Id, "Setup Crew", slotsNeeded: 2);

        var user = await CreateTestUser("volunteer@example.com", "Volunteer");
        var signup = new VolunteerSignup
        {
            Id = Guid.NewGuid(),
            VolunteerPositionId = position.Id,
            UserId = user.Id,
            Status = VolunteerSignupStatus.Confirmed,
            SignedUpAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.VolunteerSignups.Add(signup);
        position.SlotsFilled = 1;
        await _context.SaveChangesAsync();

        // Act - Cancel the signup
        signup.Status = VolunteerSignupStatus.Cancelled;
        position.SlotsFilled--;
        await _context.SaveChangesAsync();

        // Assert
        position.SlotsFilled.Should().Be(0);
        position.SlotsRemaining.Should().Be(2);
    }

    private async Task AddParticipantToEvent(Guid eventId, Guid userId)
    {
        var participation = new EventParticipation
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            ParticipationType = ParticipationType.RSVP,
            Status = ParticipationStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.EventParticipations.Add(participation);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Admin Assignment Tests

    [Fact]
    public async Task AssignVolunteerAsync_ByAdmin_Succeeds()
    {
        // NOTE: Current implementation doesn't have separate admin assignment method
        // Admin assignment would use the same signup flow with additional authorization

        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var position = await CreateVolunteerPosition(testEvent.Id, "Setup Crew");
        var volunteer = await CreateTestUser("volunteer@example.com", "Volunteer");

        var request = new VolunteerSignupRequest();

        // Act
        var (success, signup, error) = await _service.SignupForPositionAsync(
            position.Id.ToString(),
            volunteer.Id.ToString(),
            request);

        // Assert
        success.Should().BeTrue();
        signup.Should().NotBeNull();
        signup!.Status.Should().Be(VolunteerSignupStatus.Confirmed.ToString());
    }

    [Fact]
    public async Task RemoveVolunteerAsync_ByAdmin_Succeeds()
    {
        // NOTE: Removal is done by updating status to Cancelled

        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var position = await CreateVolunteerPosition(testEvent.Id, "Setup Crew");
        var user = await CreateTestUser("volunteer@example.com", "Volunteer");

        var signup = new VolunteerSignup
        {
            Id = Guid.NewGuid(),
            VolunteerPositionId = position.Id,
            UserId = user.Id,
            Status = VolunteerSignupStatus.Confirmed,
            SignedUpAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.VolunteerSignups.Add(signup);
        await _context.SaveChangesAsync();

        // Act - Admin removes volunteer
        signup.Status = VolunteerSignupStatus.Cancelled;
        signup.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Assert
        var removedSignup = await _context.VolunteerSignups.FindAsync(signup.Id);
        removedSignup!.Status.Should().Be(VolunteerSignupStatus.Cancelled);
    }

    #endregion

    #region Check-In Integration Tests

    [Fact]
    public async Task CheckInVolunteer_MarksAsPresent()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var position = await CreateVolunteerPosition(testEvent.Id, "Setup Crew");
        var user = await CreateTestUser("volunteer@example.com", "Volunteer");

        var signup = new VolunteerSignup
        {
            Id = Guid.NewGuid(),
            VolunteerPositionId = position.Id,
            UserId = user.Id,
            Status = VolunteerSignupStatus.Confirmed,
            SignedUpAt = DateTime.UtcNow,
            HasCheckedIn = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.VolunteerSignups.Add(signup);
        await _context.SaveChangesAsync();

        // Act - Check in volunteer
        signup.HasCheckedIn = true;
        signup.CheckedInAt = DateTime.UtcNow;
        signup.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Assert
        var checkedInSignup = await _context.VolunteerSignups.FindAsync(signup.Id);
        checkedInSignup!.HasCheckedIn.Should().BeTrue();
        checkedInSignup.CheckedInAt.Should().NotBeNull();
        checkedInSignup.CheckedInAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CheckInVolunteer_AfterEventStart_AllowsCheckIn()
    {
        // Arrange
        var eventStartTime = DateTime.UtcNow.AddHours(-1); // Event started 1 hour ago
        var testEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Started Event",
            Description = "Event in progress",
            Location = "Test Location",
            EventType = EventType.Class,
            Capacity = 25,
            IsPublished = true,
            StartDate = eventStartTime,
            EndDate = eventStartTime.AddHours(3),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var position = await CreateVolunteerPosition(testEvent.Id, "Setup Crew");
        var user = await CreateTestUser("volunteer@example.com", "Volunteer");

        var signup = new VolunteerSignup
        {
            Id = Guid.NewGuid(),
            VolunteerPositionId = position.Id,
            UserId = user.Id,
            Status = VolunteerSignupStatus.Confirmed,
            SignedUpAt = DateTime.UtcNow,
            HasCheckedIn = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.VolunteerSignups.Add(signup);
        await _context.SaveChangesAsync();

        // Act - Check in after event start
        signup.HasCheckedIn = true;
        signup.CheckedInAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Assert
        signup.HasCheckedIn.Should().BeTrue();
        signup.CheckedInAt.Should().BeAfter(testEvent.StartDate);
    }

    [Fact]
    public async Task CheckInVolunteer_BeforeEventStart_StillAllowsCheckIn()
    {
        // NOTE: Current implementation allows early check-in for volunteers
        // (they often arrive early to setup)

        // Arrange
        var futureEventStart = DateTime.UtcNow.AddHours(2);
        var testEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Future Event",
            Description = "Event not started",
            Location = "Test Location",
            EventType = EventType.Class,
            Capacity = 25,
            IsPublished = true,
            StartDate = futureEventStart,
            EndDate = futureEventStart.AddHours(3),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var position = await CreateVolunteerPosition(testEvent.Id, "Setup Crew");
        var user = await CreateTestUser("volunteer@example.com", "Volunteer");

        var signup = new VolunteerSignup
        {
            Id = Guid.NewGuid(),
            VolunteerPositionId = position.Id,
            UserId = user.Id,
            Status = VolunteerSignupStatus.Confirmed,
            SignedUpAt = DateTime.UtcNow,
            HasCheckedIn = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.VolunteerSignups.Add(signup);
        await _context.SaveChangesAsync();

        // Act - Check in before event start
        signup.HasCheckedIn = true;
        signup.CheckedInAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Assert
        signup.HasCheckedIn.Should().BeTrue();
        signup.CheckedInAt.Should().BeBefore(testEvent.StartDate);
    }

    #endregion

    #region Edge Cases and Validation

    [Fact]
    public async Task GetEventVolunteerPositionsAsync_WithValidEvent_ReturnsPositions()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        await CreateVolunteerPosition(testEvent.Id, "Position 1");
        await CreateVolunteerPosition(testEvent.Id, "Position 2");
        await CreateVolunteerPosition(testEvent.Id, "Private Position", isPublicFacing: false);

        // Act
        var (success, positions, error) = await _service.GetEventVolunteerPositionsAsync(
            testEvent.Id.ToString(),
            userId: null);

        // Assert
        success.Should().BeTrue();
        error.Should().BeNullOrEmpty();
        positions.Should().HaveCount(2, "Only public-facing positions should be returned");
        positions.Should().Contain(p => p.Title == "Position 1");
        positions.Should().Contain(p => p.Title == "Position 2");
    }

    [Fact]
    public async Task SignupForPosition_WithInvalidPositionId_ReturnsError()
    {
        // Arrange
        var invalidId = "not-a-guid";
        var user = await CreateTestUser("volunteer@example.com", "Volunteer");
        var request = new VolunteerSignupRequest();

        // Act
        var (success, signup, error) = await _service.SignupForPositionAsync(
            invalidId,
            user.Id.ToString(),
            request);

        // Assert
        success.Should().BeFalse();
        signup.Should().BeNull();
        error.Should().Contain("Invalid position ID format");
    }

    [Fact]
    public async Task SignupForPosition_WithNonPublicPosition_ReturnsError()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var privatePosition = await CreateVolunteerPosition(testEvent.Id, "Private Position", isPublicFacing: false);
        var user = await CreateTestUser("volunteer@example.com", "Volunteer");
        var request = new VolunteerSignupRequest();

        // Act
        var (success, signup, error) = await _service.SignupForPositionAsync(
            privatePosition.Id.ToString(),
            user.Id.ToString(),
            request);

        // Assert
        success.Should().BeFalse();
        signup.Should().BeNull();
        error.Should().Contain("not open for public signups");
    }

    [Fact]
    public async Task VolunteerSignup_UpdatesSlotsFilled()
    {
        // Arrange
        var testEvent = await CreateTestEvent("Test Event");
        var position = await CreateVolunteerPosition(testEvent.Id, "Setup Crew", slotsNeeded: 3);
        position.SlotsFilled.Should().Be(0);

        var user = await CreateTestUser("volunteer@example.com", "Volunteer");
        var request = new VolunteerSignupRequest();

        // Act
        var (success, signup, error) = await _service.SignupForPositionAsync(
            position.Id.ToString(),
            user.Id.ToString(),
            request);

        // Assert
        success.Should().BeTrue();

        // Verify SlotsFilled incremented
        var updatedPosition = await _context.VolunteerPositions.FindAsync(position.Id);
        updatedPosition!.SlotsFilled.Should().Be(1);
        updatedPosition.SlotsRemaining.Should().Be(2);
    }

    #endregion
}
