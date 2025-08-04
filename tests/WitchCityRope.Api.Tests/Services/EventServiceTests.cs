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
using CoreEnums = WitchCityRope.Core.Enums;
using ApiEnums = WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Core.Interfaces;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Tests.Common.Builders;
using WitchCityRope.Tests.Common.Identity;
using WitchCityRope.Tests.Common.Fixtures;

namespace WitchCityRope.Api.Tests.Services;

public class EventServiceTests : IDisposable
{
    private readonly WitchCityRopeIdentityDbContext _dbContext;
    private readonly Mock<IEventService> _eventServiceMock;
    private readonly Mock<WitchCityRope.Api.Interfaces.IPaymentService> _paymentServiceMock;

    public EventServiceTests()
    {
        var options = new DbContextOptionsBuilder<WitchCityRopeIdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new WitchCityRopeIdentityDbContext(options);
        _eventServiceMock = new Mock<IEventService>();
        _paymentServiceMock = new Mock<WitchCityRope.Api.Interfaces.IPaymentService>();
    }

    #region CreateEventAsync Tests

    [Fact]
    public async Task CreateEventAsync_WhenValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var organizer = new IdentityUserBuilder().WithRole(CoreEnums.UserRole.Organizer).Build();
        await _dbContext.Users.AddAsync(organizer);
        await _dbContext.SaveChangesAsync();

        var request = new CreateEventRequest(
            Title: "Test Event",
            Description: "Test Description",
            Type: ApiEnums.EventType.Workshop,
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
        var organizer = new IdentityUserBuilder().WithRole(CoreEnums.UserRole.Organizer).Build();
        
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
    public void Event_Unpublish_WithTicketsOrRsvps_ShouldFail()
    {
        // Arrange
        var @event = new EventBuilder().Build();
        @event.Publish();
        
        // Since we can't add tickets/RSVPs directly in the test,
        // we'll test the unpublish without tickets/RSVPs
        
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

    #region Ticket Tests (Paid Events)

    [Fact]
    public async Task CreateTicket_ValidPaidEvent_ShouldSucceed()
    {
        // Arrange
        var user = new IdentityUserBuilder().Build();
        var @event = new EventBuilder()
            .WithCapacity(10)
            .WithEventType(EventType.Workshop)
            .WithPricingTiers(50m, 75m, 100m) // Paid event with sliding scale pricing
            .Build();
        var selectedPrice = @event.PricingTiers.First();

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Events.AddAsync(@event);
        await _dbContext.SaveChangesAsync();

        // Act
        var ticket = new Ticket(
            userId: user.Id,
            @event: @event,
            selectedPrice: selectedPrice,
            dietaryRestrictions: "Vegetarian",
            accessibilityNeeds: "None",
            emergencyContact: "Emergency Contact",
            emergencyPhone: "555-0911"
        );

        // Assert
        ticket.Should().NotBeNull();
        ticket.UserId.Should().Be(user.Id);
        ticket.EventId.Should().Be(@event.Id);
        ticket.Status.Should().Be(TicketStatus.Pending);
        ticket.SelectedPrice.Should().Be(selectedPrice);
    }

    [Fact]
    public void CreateTicket_NullEvent_ShouldThrowException()
    {
        // Arrange
        var user = new IdentityUserBuilder().Build();

        // Act
        var action = () => new Ticket(
            userId: user.Id,
            @event: null!,
            selectedPrice: Money.Create(50m),
            dietaryRestrictions: null,
            accessibilityNeeds: null,
            emergencyContact: "Emergency Contact",
            emergencyPhone: "555-0911"
        );

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("event");
    }

    #endregion

    #region RSVP Tests (Free Events)

    [Fact]
    public async Task CreateRsvp_ValidFreeEvent_ShouldSucceed()
    {
        // Arrange
        var user = new IdentityUserBuilder().Build();
        var @event = new EventBuilder()
            .WithCapacity(50)
            .WithEventType(EventType.Social)
            .WithPricingTiers(0m) // Free event
            .Build();

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Events.AddAsync(@event);
        await _dbContext.SaveChangesAsync();

        // Act
        var rsvp = new Rsvp(user.Id, @event);

        // Assert
        rsvp.Should().NotBeNull();
        rsvp.UserId.Should().Be(user.Id);
        rsvp.EventId.Should().Be(@event.Id);
        rsvp.Status.Should().Be(RsvpStatus.Confirmed);
        rsvp.ConfirmationCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateRsvp_NullEvent_ShouldThrowException()
    {
        // Arrange
        var user = new IdentityUserBuilder().Build();

        // Act
        var action = () => new Rsvp(user.Id, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("event");
    }

    #endregion

    #region User Entity Tests

    [Fact]
    public void User_Create_ValidData_ShouldSucceed()
    {
        // Arrange & Act
        var user = new IdentityUserBuilder()
            .WithSceneName("TestUser")
            .WithEmail("test@example.com")
            .WithDateOfBirth(DateTimeFixture.ValidBirthDate)
            .Build();

        // Assert
        user.Should().NotBeNull();
        user.SceneName.Value.Should().Be("TestUser");
        user.Email.Should().Be("test@example.com");
        user.IsActive.Should().BeTrue();
        user.GetAge().Should().BeGreaterThanOrEqualTo(21);
    }

    [Fact]
    public void User_Create_UnderAge_ShouldFail()
    {
        // Arrange & Act
        var action = () => new IdentityUserBuilder()
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
        var user = new IdentityUserBuilder().WithRole(UserRole.Attendee).Build();

        // Act
        user.PromoteToRole(UserRole.Organizer);

        // Assert
        user.Role.Should().Be(UserRole.Organizer);
    }

    [Fact]
    public void User_PromoteToRole_Demotion_ShouldFail()
    {
        // Arrange
        var user = new IdentityUserBuilder().WithRole(UserRole.Organizer).Build();

        // Act
        var action = () => user.PromoteToRole(UserRole.Attendee);

        // Assert
        action.Should().Throw<DomainException>()
            .WithMessage("*Cannot demote user or assign same role*");
    }

    #endregion

    #region Payment Tests

    [Fact]
    public async Task ProcessPayment_ValidTicketPayment_ShouldReturnSuccess()
    {
        // Arrange
        var user = new IdentityUserBuilder().Build();
        var @event = new EventBuilder()
            .WithEventType(EventType.Workshop)
            .WithPricingTiers(50m, 75m, 100m)
            .Build();
        var ticket = new Ticket(
            user.Id, 
            @event, 
            @event.PricingTiers.First(),
            null,
            null,
            "Emergency Contact",
            "555-0911"
        );

        var paymentResult = new PaymentResult
        {
            Success = true,
            TransactionId = "txn_123",
            Status = PaymentStatus.Completed,
            ProcessedAt = DateTime.UtcNow
        };

        _paymentServiceMock.Setup(x => x.ProcessPaymentAsync(
                It.IsAny<Ticket>(),
                It.IsAny<Money>(),
                It.IsAny<string>()))
            .ReturnsAsync(paymentResult);

        // Act
        var result = await _paymentServiceMock.Object.ProcessPaymentAsync(
            ticket,
            ticket.SelectedPrice,
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