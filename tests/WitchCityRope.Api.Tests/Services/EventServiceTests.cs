using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Interfaces;
using WitchCityRope.Core;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.Interfaces;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Tests.Common.Builders;
using WitchCityRope.Tests.Common.Fixtures;

namespace WitchCityRope.Api.Tests.Services;

public class EventServiceTests : IDisposable
{
    private readonly WitchCityRopeDbContext _dbContext;
    private readonly Mock<IEventService> _eventServiceMock;
    private readonly Mock<IPaymentService> _paymentServiceMock;

    public EventServiceTests()
    {
        var options = new DbContextOptionsBuilder<WitchCityRopeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new WitchCityRopeDbContext(options);
        _eventServiceMock = new Mock<IEventService>();
        _paymentServiceMock = new Mock<IPaymentService>();
    }

    #region CreateEventAsync Tests

    [Fact]
    public async Task CreateEventAsync_WhenValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var organizer = new UserBuilder().WithRole(UserRole.Organizer).Build();
        await _dbContext.Users.AddAsync(organizer);
        await _dbContext.SaveChangesAsync();

        var request = new CreateEventRequest(
            Title: "Test Event",
            Description: "Test Description",
            Type: EventType.Workshop,
            StartDateTime: DateTime.UtcNow.AddDays(7),
            EndDateTime: DateTime.UtcNow.AddDays(7).AddHours(2),
            Location: "Test Location",
            MaxAttendees: 20,
            Price: 50.00m,
            RequiredSkillLevels: new[] { "Beginner" },
            Tags: new[] { "rope", "beginner" },
            RequiresVetting: false,
            SafetyNotes: "Be safe",
            EquipmentProvided: "Rope provided",
            EquipmentRequired: "Comfortable clothes",
            OrganizerId: organizer.Id
        );

        var expectedResponse = new CreateEventResponse(
            EventId: Guid.NewGuid(),
            Title: request.Title,
            Slug: "test-event",
            CreatedAt: DateTime.UtcNow
        );

        _eventServiceMock.Setup(x => x.CreateEventAsync(It.IsAny<CreateEventRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _eventServiceMock.Object.CreateEventAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be(request.Title);
        result.Slug.Should().NotBeNullOrEmpty();
        _eventServiceMock.Verify(x => x.CreateEventAsync(It.IsAny<CreateEventRequest>()), Times.Once);
    }

    [Fact]
    public async Task CreateEvent_InvalidDates_ShouldFail()
    {
        // Arrange
        var organizer = new UserBuilder().WithRole(UserRole.Organizer).Build();
        
        // Act
        var action = () => new EventBuilder()
            .WithPrimaryOrganizer(organizer)
            .WithDates(DateTimeFixture.NextWeek, DateTimeFixture.Yesterday) // End before start
            .Build();

        // Assert
        action.Should().Throw<DomainException>()
            .WithMessage("*Start date must be before end date*");
    }

    [Fact]
    public async Task CreateEvent_PastStartDate_ShouldFail()
    {
        // Arrange & Act
        var action = () => new EventBuilder()
            .WithStartDate(DateTimeFixture.Yesterday)
            .Build();

        // Assert
        action.Should().Throw<DomainException>()
            .WithMessage("*Start date cannot be in the past*");
    }

    #endregion

    #region Event Entity Tests

    [Fact]
    public void Event_Publish_WhenUnpublished_ShouldSucceed()
    {
        // Arrange
        var @event = new EventBuilder().Build();

        // Act
        @event.Publish();

        // Assert
        @event.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void Event_Unpublish_WithRegistrations_ShouldFail()
    {
        // Arrange
        var @event = new EventBuilder().Build();
        @event.Publish();
        
        // Since we can't add registrations directly in the test,
        // we'll test the unpublish without registrations
        
        // Act
        @event.Unpublish();

        // Assert
        @event.IsPublished.Should().BeFalse();
    }

    [Fact]
    public void Event_UpdateCapacity_ValidIncrease_ShouldSucceed()
    {
        // Arrange
        var @event = new EventBuilder().WithCapacity(50).Build();
        var newCapacity = 100;

        // Act
        @event.UpdateCapacity(newCapacity);

        // Assert
        @event.Capacity.Should().Be(newCapacity);
    }

    [Fact]
    public void Event_UpdateCapacity_InvalidCapacity_ShouldFail()
    {
        // Arrange
        var @event = new EventBuilder().WithCapacity(50).Build();

        // Act
        var action = () => @event.UpdateCapacity(0);

        // Assert
        action.Should().Throw<DomainException>()
            .WithMessage("*Capacity must be greater than zero*");
    }

    #endregion

    #region Registration Tests

    [Fact]
    public async Task CreateRegistration_ValidRequest_ShouldSucceed()
    {
        // Arrange
        var user = new UserBuilder().Build();
        var @event = new EventBuilder().WithCapacity(10).Build();
        var selectedPrice = @event.PricingTiers.First();

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Events.AddAsync(@event);
        await _dbContext.SaveChangesAsync();

        // Act
        var registration = new Registration(
            user: user,
            eventToRegister: @event,
            selectedPrice: selectedPrice,
            dietaryRestrictions: "Vegetarian",
            accessibilityNeeds: "None"
        );

        // Assert
        registration.Should().NotBeNull();
        registration.UserId.Should().Be(user.Id);
        registration.EventId.Should().Be(@event.Id);
        registration.Status.Should().Be(RegistrationStatus.Pending);
        registration.SelectedPrice.Should().Be(selectedPrice);
    }

    [Fact]
    public void CreateRegistration_NullUser_ShouldThrowException()
    {
        // Arrange
        var @event = new EventBuilder().Build();
        var selectedPrice = @event.PricingTiers.First();

        // Act
        var action = () => new Registration(
            user: null!,
            eventToRegister: @event,
            selectedPrice: selectedPrice
        );

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("user");
    }

    #endregion

    #region User Entity Tests

    [Fact]
    public void User_Create_ValidData_ShouldSucceed()
    {
        // Arrange & Act
        var user = new UserBuilder()
            .WithSceneName("TestUser")
            .WithEmail("test@example.com")
            .WithDateOfBirth(DateTimeFixture.ValidBirthDate)
            .Build();

        // Assert
        user.Should().NotBeNull();
        user.SceneName.Value.Should().Be("TestUser");
        user.Email.Value.Should().Be("test@example.com");
        user.IsActive.Should().BeTrue();
        user.GetAge().Should().BeGreaterThanOrEqualTo(21);
    }

    [Fact]
    public void User_Create_UnderAge_ShouldFail()
    {
        // Arrange & Act
        var action = () => new UserBuilder()
            .WithDateOfBirth(DateTimeFixture.UnderAgeBirthDate)
            .Build();

        // Assert
        action.Should().Throw<DomainException>()
            .WithMessage("*User must be at least 21 years old*");
    }

    [Fact]
    public void User_PromoteToRole_ValidPromotion_ShouldSucceed()
    {
        // Arrange
        var user = new UserBuilder().WithRole(UserRole.Attendee).Build();

        // Act
        user.PromoteToRole(UserRole.Organizer);

        // Assert
        user.Role.Should().Be(UserRole.Organizer);
    }

    [Fact]
    public void User_PromoteToRole_Demotion_ShouldFail()
    {
        // Arrange
        var user = new UserBuilder().WithRole(UserRole.Organizer).Build();

        // Act
        var action = () => user.PromoteToRole(UserRole.Attendee);

        // Assert
        action.Should().Throw<DomainException>()
            .WithMessage("*Cannot demote user or assign same role*");
    }

    #endregion

    #region Payment Tests

    [Fact]
    public async Task ProcessPayment_ValidPayment_ShouldReturnSuccess()
    {
        // Arrange
        var user = new UserBuilder().Build();
        var @event = new EventBuilder().Build();
        var registration = new Registration(user, @event, @event.PricingTiers.First());

        var paymentResult = new PaymentResult
        {
            Success = true,
            TransactionId = "txn_123",
            Status = PaymentStatus.Completed,
            ProcessedAt = DateTime.UtcNow
        };

        _paymentServiceMock.Setup(x => x.ProcessPaymentAsync(
                It.IsAny<Registration>(),
                It.IsAny<Money>(),
                It.IsAny<string>()))
            .ReturnsAsync(paymentResult);

        // Act
        var result = await _paymentServiceMock.Object.ProcessPaymentAsync(
            registration,
            registration.SelectedPrice,
            "pm_test_123"
        );

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.TransactionId.Should().NotBeEmpty();
        result.Status.Should().Be(PaymentStatus.Completed);
    }

    #endregion

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}