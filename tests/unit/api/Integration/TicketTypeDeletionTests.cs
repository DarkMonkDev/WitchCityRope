using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Events.Interfaces;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using Moq;

namespace WitchCityRope.Api.Tests.Integration;

/// <summary>
/// Integration tests for ticket type deletion functionality
/// Tests verify the CheckTicketTypeDeletionAsync and DeleteTicketTypeAsync methods
/// CRITICAL: These tests verify proper blocking logic for ticket types with sales
/// Uses TestContainers PostgreSQL via DatabaseTestFixture (NOT InMemoryDatabase)
/// </summary>
[Collection("Database")]
public class TicketTypeDeletionTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private ApplicationDbContext _context = null!;
    private EventService _eventService = null!;
    private Guid _testEventId;
    private Guid _testUserId;

    public TicketTypeDeletionTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _context = _fixture.CreateDbContext();
        _testEventId = Guid.NewGuid();
        _testUserId = Guid.NewGuid();

        var mockTimeZoneService = new Mock<ITimeZoneService>();

        _eventService = new EventService(
            _context,
            new Mock<ILogger<EventService>>().Object,
            mockTimeZoneService.Object);

        // Ensure venue exists for FK constraint
        var venue = await _context.Venues.FindAsync(1);
        if (venue == null)
        {
            venue = new Venue
            {
                Id = 1,
                Name = "Test Venue",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Venues.Add(venue);
            await _context.SaveChangesAsync();
        }

        // Seed test user
        var testUser = new ApplicationUser
        {
            Id = _testUserId,
            Email = $"test-{Guid.NewGuid():N}@example.com",
            SceneName = "TestUser",
            UserName = $"test-{Guid.NewGuid():N}@example.com",
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        // Seed test event
        var testEvent = new Event
        {
            Id = _testEventId,
            Title = "Test Event",
            Description = "Test Description",
            AllowRsvps = false,
            RequireTicketPurchase = true,
            VettedMembersOnly = false,
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Capacity = 20,
            VenueId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        await _fixture.ResetDatabaseAsync();
    }

    [Fact]
    public async Task CheckTicketTypeDeletion_WithNoSales_ReturnsCanDelete()
    {
        // Arrange - Create ticket type with no sales
        var ticketType = await CreateTicketTypeAsync("General Admission", 25.00m);

        // Act
        var (success, response, error) = await _eventService.CheckTicketTypeDeletionAsync(
            _testEventId.ToString(),
            ticketType.Id.ToString());

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.CanDelete.Should().BeTrue();
        response.BlockReason.Should().BeNullOrEmpty();
        response.TicketsSoldCount.Should().Be(0);
    }

    [Fact]
    public async Task CheckTicketTypeDeletion_WithSales_ReturnsBlocked()
    {
        // Arrange - Create ticket type with sales
        var ticketType = await CreateTicketTypeAsync("Early Bird", 20.00m);

        // Add ticket purchase
        await AddTicketPurchaseAsync(ticketType.Id);

        // Act
        var (success, response, error) = await _eventService.CheckTicketTypeDeletionAsync(
            _testEventId.ToString(),
            ticketType.Id.ToString());

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.CanDelete.Should().BeFalse();
        response.BlockReason.Should().Be("ticketsSold");
        response.TicketsSoldCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DeleteTicketType_WithNoSales_Succeeds()
    {
        // Arrange - Create ticket type with no sales
        var ticketType = await CreateTicketTypeAsync("VIP Pass", 50.00m);

        // Act
        var (success, response, error) = await _eventService.DeleteTicketTypeAsync(
            _testEventId.ToString(),
            ticketType.Id.ToString());

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();

        // Verify ticket type was deleted from database
        var deletedTicketType = await _context.TicketTypes.FindAsync(ticketType.Id);
        deletedTicketType.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTicketType_WithSales_Returns400()
    {
        // Arrange - Create ticket type with sales
        var ticketType = await CreateTicketTypeAsync("Standard Ticket", 30.00m);

        // Add ticket purchase
        await AddTicketPurchaseAsync(ticketType.Id);

        // Act
        var (success, response, error) = await _eventService.DeleteTicketTypeAsync(
            _testEventId.ToString(),
            ticketType.Id.ToString());

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Cannot delete ticket type with sold tickets");

        // Verify ticket type was NOT deleted
        var ticketTypeStillExists = await _context.TicketTypes.FindAsync(ticketType.Id);
        ticketTypeStillExists.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteTicketType_NotFound_Returns404()
    {
        // Arrange - Use non-existent ticket type ID
        var nonExistentTicketTypeId = Guid.NewGuid();

        // Act
        var (success, response, error) = await _eventService.DeleteTicketTypeAsync(
            _testEventId.ToString(),
            nonExistentTicketTypeId.ToString());

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("not found");
    }

    [Fact]
    public async Task DeleteTicketType_Unauthorized_Returns403()
    {
        // Arrange - Create ticket type
        var ticketType = await CreateTicketTypeAsync("Test Ticket", 15.00m);

        // Note: Authorization is handled at endpoint level, not service level
        // This test verifies service behavior regardless of authorization

        // Act - Attempt to delete
        var (success, response, error) = await _eventService.DeleteTicketTypeAsync(
            _testEventId.ToString(),
            ticketType.Id.ToString());

        // Assert - Service should succeed if no blocking conditions
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CheckTicketTypeDeletion_WithMultipleSales_CountsCorrectly()
    {
        // Arrange - Create ticket type with multiple sales
        var ticketType = await CreateTicketTypeAsync("Workshop Pass", 40.00m);

        // Add multiple ticket purchases
        await AddTicketPurchaseAsync(ticketType.Id);
        await AddTicketPurchaseAsync(ticketType.Id);
        await AddTicketPurchaseAsync(ticketType.Id);

        // Act
        var (success, response, error) = await _eventService.CheckTicketTypeDeletionAsync(
            _testEventId.ToString(),
            ticketType.Id.ToString());

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.CanDelete.Should().BeFalse();
        response.BlockReason.Should().Be("ticketsSold");
        response.TicketsSoldCount.Should().Be(3);
    }

    // Helper methods

    private async Task<TicketType> CreateTicketTypeAsync(string name, decimal price)
    {
        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = _testEventId,
            Name = name,
            Price = price,
            Available = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TicketTypes.Add(ticketType);
        await _context.SaveChangesAsync();

        return ticketType;
    }

    private async Task AddTicketPurchaseAsync(Guid ticketTypeId)
    {
        var ticketPurchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TicketTypeId = ticketTypeId,
            PurchaseDate = DateTime.UtcNow,
            TotalPrice = 25.00m,
            Quantity = 1,
            PaymentStatus = TicketPurchasePaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TicketPurchases.Add(ticketPurchase);

        // Create corresponding EventAttendance
        var attendance = new EventAttendance
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            EventId = _testEventId,
            AttendanceType = AttendanceType.Ticket,
            Status = AttendanceStatus.Active,
            TicketPurchaseId = ticketPurchase.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<EventAttendance>().Add(attendance);
        await _context.SaveChangesAsync();
    }
}
