using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Dashboard.Services;
using WitchCityRope.Api.Features.Dashboard.Models;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace WitchCityRope.Api.Tests.Features.Dashboard;

/// <summary>
/// Phase 1.5.4: User Dashboard Profile Service Tests
/// Comprehensive test suite for UserDashboardProfileService covering:
/// - User profile management
/// - Event registration retrieval
/// - Vetting status display
/// - Password changes
/// Created: 2025-10-10
///
/// Tests dashboard functionality for user self-service features
/// Uses real PostgreSQL database via TestContainers for accurate testing
/// </summary>
[Collection("Database")]
public class UserDashboardProfileServiceTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<ILogger<UserDashboardProfileService>> _mockLogger;
    private UserDashboardProfileService _sut; // System Under Test
    private ApplicationDbContext _context;

    public UserDashboardProfileServiceTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;

        // Mock UserManager with proper setup
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null, null, null, null, null, null, null, null);

        _mockLogger = new Mock<ILogger<UserDashboardProfileService>>();
    }

    public async Task InitializeAsync()
    {
        // Reset database before each test
        await _fixture.ResetDatabaseAsync();

        // Create fresh context and service for each test
        _context = _fixture.CreateDbContext();
        _sut = new UserDashboardProfileService(
            _context,
            _mockUserManager.Object,
            _mockLogger.Object);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    #region GetUserProfileAsync Tests

    /// <summary>
    /// Test 1: Verify successful profile retrieval with all fields
    /// </summary>
    [Fact]
    public async Task GetUserProfileAsync_WithExistingUser_ReturnsCompleteProfile()
    {
        // Arrange
        var user = await CreateTestUserAsync("ProfileUser", pronouns: "they/them", bio: "Test bio");

        _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.GetUserProfileAsync(user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.UserId.Should().Be(user.Id);
        result.Value.SceneName.Should().Be(user.SceneName);
        result.Value.Email.Should().Be(user.Email);
        result.Value.Pronouns.Should().Be("they/them");
        result.Value.Bio.Should().Be("Test bio");
    }

    /// <summary>
    /// Test 2: Verify profile retrieval fails with non-existent user
    /// </summary>
    [Fact]
    public async Task GetUserProfileAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        _mockUserManager.Setup(x => x.FindByIdAsync(nonExistentUserId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.GetUserProfileAsync(nonExistentUserId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("User not found");
    }

    #endregion

    #region UpdateUserProfileAsync Tests

    /// <summary>
    /// Test 3: Verify successful profile update with all fields
    /// </summary>
    [Fact]
    public async Task UpdateUserProfileAsync_WithValidData_UpdatesAllFields()
    {
        // Arrange
        var user = await CreateTestUserAsync("UpdateUser");

        _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var request = new UpdateProfileDto
        {
            SceneName = $"NewName_{Guid.NewGuid().ToString().Substring(0, 8)}",
            FirstName = "Jane",
            LastName = "Doe",
            Email = user.Email ?? "",
            Pronouns = "she/her",
            Bio = "Updated bio",
            DiscordName = "discord#1234",
            FetLifeName = "fetlife_user",
            PhoneNumber = "555-1234"
        };

        // Act
        var result = await _sut.UpdateUserProfileAsync(user.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.SceneName.Should().Be(request.SceneName);
        result.Value.FirstName.Should().Be("Jane");
        result.Value.LastName.Should().Be("Doe");
        result.Value.Pronouns.Should().Be("she/her");
        result.Value.Bio.Should().Be("Updated bio");
        result.Value.DiscordName.Should().Be("discord#1234");
        result.Value.FetLifeName.Should().Be("fetlife_user");
    }

    /// <summary>
    /// Test 4: Verify profile update fails when user not found
    /// </summary>
    [Fact]
    public async Task UpdateUserProfileAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        _mockUserManager.Setup(x => x.FindByIdAsync(nonExistentUserId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        var request = new UpdateProfileDto
        {
            SceneName = "NewName",
            Email = "test@example.com"
        };

        // Act
        var result = await _sut.UpdateUserProfileAsync(nonExistentUserId, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("User not found");
    }

    /// <summary>
    /// Test 5: Verify profile update handles UserManager errors
    /// </summary>
    [Fact]
    public async Task UpdateUserProfileAsync_WhenUserManagerFails_ReturnsFailure()
    {
        // Arrange
        var user = await CreateTestUserAsync("FailUser");

        _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        var identityError = new IdentityError { Description = "Update failed" };
        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        var request = new UpdateProfileDto
        {
            SceneName = "NewName",
            Email = user.Email ?? ""
        };

        // Act
        var result = await _sut.UpdateUserProfileAsync(user.Id, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Failed to update profile");
    }

    #endregion

    #region GetUserEventsAsync Tests

    /// <summary>
    /// Test 6: Verify retrieval of user's registered events
    /// </summary>
    [Fact]
    public async Task GetUserEventsAsync_WithRegisteredEvents_ReturnsEvents()
    {
        // Arrange
        var user = await CreateTestUserAsync("EventUser");
        var evt = await CreateTestEventAsync("Test Event");
        var ticketType = await CreateTestTicketTypeAsync(evt.Id, "General Admission", 25.00m);
        await CreateTestTicketPurchaseAsync(user.Id, ticketType.Id);

        // Act
        var result = await _sut.GetUserEventsAsync(user.Id, includePast: false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().ContainSingle();
        result.Value![0].Title.Should().Be("Test Event");
        result.Value![0].RegistrationStatus.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Test 7: Verify event list excludes past events when not requested
    /// </summary>
    [Fact]
    public async Task GetUserEventsAsync_WithIncludePastFalse_ExcludesPastEvents()
    {
        // Arrange
        var user = await CreateTestUserAsync("PastEventUser");

        // Create past event
        var pastEvent = await CreateTestEventAsync("Past Event", startDate: DateTime.UtcNow.AddDays(-30));
        var pastTicketType = await CreateTestTicketTypeAsync(pastEvent.Id, "Past Ticket", 10.00m);
        await CreateTestTicketPurchaseAsync(user.Id, pastTicketType.Id);

        // Create future event
        var futureEvent = await CreateTestEventAsync("Future Event", startDate: DateTime.UtcNow.AddDays(30));
        var futureTicketType = await CreateTestTicketTypeAsync(futureEvent.Id, "Future Ticket", 15.00m);
        await CreateTestTicketPurchaseAsync(user.Id, futureTicketType.Id);

        // Act
        var result = await _sut.GetUserEventsAsync(user.Id, includePast: false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().ContainSingle();
        result.Value![0].Title.Should().Be("Future Event");
    }

    /// <summary>
    /// Test 8: Verify event list includes past events when requested
    /// </summary>
    [Fact]
    public async Task GetUserEventsAsync_WithIncludePastTrue_IncludesPastEvents()
    {
        // Arrange
        var user = await CreateTestUserAsync("AllEventUser");

        // Create past event
        var pastEvent = await CreateTestEventAsync("Past Event", startDate: DateTime.UtcNow.AddDays(-30));
        var pastTicketType = await CreateTestTicketTypeAsync(pastEvent.Id, "Past Ticket", 10.00m);
        await CreateTestTicketPurchaseAsync(user.Id, pastTicketType.Id);

        // Act
        var result = await _sut.GetUserEventsAsync(user.Id, includePast: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().ContainSingle();
        result.Value![0].Title.Should().Be("Past Event");
    }

    #endregion

    #region GetVettingStatusAsync Tests

    /// <summary>
    /// Test 9: Verify vetting status retrieval for UnderReview status
    /// </summary>
    [Fact]
    public async Task GetVettingStatusAsync_WithUnderReviewStatus_ReturnsAppropriateMessage()
    {
        // Arrange
        var user = await CreateTestUserAsync("UnderReviewUser", vettingStatus: VettingStatus.UnderReview);

        _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.GetVettingStatusAsync(user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be("UnderReview");
        result.Value.Message.Should().Contain("under review");
    }

    /// <summary>
    /// Test 10: Verify vetting status retrieval for InterviewApproved status
    /// </summary>
    [Fact]
    public async Task GetVettingStatusAsync_WithInterviewApprovedStatus_IncludesScheduleUrl()
    {
        // Arrange
        var user = await CreateTestUserAsync("InterviewUser", vettingStatus: VettingStatus.InterviewApproved);

        _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.GetVettingStatusAsync(user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be("InterviewApproved");
        result.Value.Message.Should().Contain("approved for interview");
        result.Value.InterviewScheduleUrl.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Test 11: Verify vetting status retrieval for Denied status
    /// </summary>
    [Fact]
    public async Task GetVettingStatusAsync_WithDeniedStatus_IncludesReapplyUrl()
    {
        // Arrange
        var user = await CreateTestUserAsync("DeniedUser", vettingStatus: VettingStatus.Denied);

        _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.GetVettingStatusAsync(user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be("Denied");
        result.Value.Message.Should().Contain("not approved");
        result.Value.ReapplyInfoUrl.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region ChangePasswordAsync Tests

    /// <summary>
    /// Test 12: Verify successful password change
    /// </summary>
    [Fact]
    public async Task ChangePasswordAsync_WithValidPassword_ChangesPassword()
    {
        // Arrange
        var user = await CreateTestUserAsync("PasswordUser");

        _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, "OldPassword123!"))
            .ReturnsAsync(true);

        _mockUserManager.Setup(x => x.ChangePasswordAsync(user, "OldPassword123!", "NewPassword123!"))
            .ReturnsAsync(IdentityResult.Success);

        var request = new ChangePasswordDto
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = await _sut.ChangePasswordAsync(user.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _mockUserManager.Verify(x => x.ChangePasswordAsync(user, "OldPassword123!", "NewPassword123!"), Times.Once);
    }

    /// <summary>
    /// Test 13: Verify password change fails with incorrect current password
    /// </summary>
    [Fact]
    public async Task ChangePasswordAsync_WithIncorrectCurrentPassword_ReturnsFailure()
    {
        // Arrange
        var user = await CreateTestUserAsync("WrongPasswordUser");

        _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, "WrongPassword"))
            .ReturnsAsync(false);

        var request = new ChangePasswordDto
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = await _sut.ChangePasswordAsync(user.Id, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Current password is incorrect");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper method to create test user with unique identifiers
    /// Ensures all required fields are populated for database insertion
    /// </summary>
    private async Task<ApplicationUser> CreateTestUserAsync(
        string baseName,
        string? pronouns = null,
        string? bio = null,
        VettingStatus vettingStatus = VettingStatus.UnderReview)
    {
        var uniqueId = Guid.NewGuid().ToString().Substring(0, 8);
        var sceneName = $"{baseName}_{uniqueId}";

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = $"{sceneName}@example.com".ToLower(),
            SceneName = sceneName,
            Pronouns = pronouns ?? "",
            Bio = bio,
            Role = "Member",
            IsActive = true,
            VettingStatus = (int)vettingStatus,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    /// <summary>
    /// Helper method to create test event
    /// </summary>
    private async Task<Event> CreateTestEventAsync(string title, DateTime? startDate = null)
    {
        var uniqueId = Guid.NewGuid().ToString().Substring(0, 8);
        var start = startDate ?? DateTime.UtcNow.AddDays(7);

        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            ShortDescription = $"Test event {uniqueId}",
            Description = "Test event description",
            EventType = "Workshop",
            Location = "Test Location",
            StartDate = start,
            EndDate = start.AddHours(2),
            Capacity = 20,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        return evt;
    }

    /// <summary>
    /// Helper method to create test ticket type
    /// </summary>
    private async Task<TicketType> CreateTestTicketTypeAsync(Guid eventId, string name, decimal price)
    {
        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Name = name,
            Description = "Test ticket",
            Price = price,
            Available = 10,
            Sold = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TicketTypes.Add(ticketType);
        await _context.SaveChangesAsync();

        return ticketType;
    }

    /// <summary>
    /// Helper method to create test ticket purchase
    /// </summary>
    private async Task<TicketPurchase> CreateTestTicketPurchaseAsync(Guid userId, Guid ticketTypeId)
    {
        var purchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TicketTypeId = ticketTypeId,
            PurchaseDate = DateTime.UtcNow,
            Quantity = 1,
            TotalPrice = 0,
            PaymentStatus = "Confirmed",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TicketPurchases.Add(purchase);
        await _context.SaveChangesAsync();

        return purchase;
    }

    #endregion
}
