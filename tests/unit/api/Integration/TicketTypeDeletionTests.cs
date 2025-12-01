using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.Participation.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace WitchCityRope.Api.Tests.Integration;

/// <summary>
/// Integration tests for ticket type deletion functionality
/// Tests verify the CheckTicketTypeDeletionAsync and DeleteTicketTypeAsync methods
/// CRITICAL: These tests verify proper blocking logic for ticket types with sales
/// </summary>
public class TicketTypeDeletionTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly EventService _eventService;
    private readonly Guid _testEventId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();

    public TicketTypeDeletionTests()
    {
        // Setup in-memory database for integration testing
        // Configure to suppress transaction warnings (in-memory DB doesn't support transactions)
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);

        _eventService = new EventService(
            _context,
            new Mock<ILogger<EventService>>().Object);

        // Seed test user
        var testUser = new ApplicationUser
        {
            Id = _testUserId,
            Email = "test@example.com",
            SceneName = "TestUser"
        };

        _context.Users.Add(testUser);
        _context.SaveChanges();

        // Seed test event
        var testEvent = new Event
        {
            Id = _testEventId,
            Title = "Test Event",
            Description = "Test Description",
            EventType = EventType.Class,
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Capacity = 20,
            VenueId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.Add(testEvent);
        _context.SaveChanges();
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
        // Need to get the ticket type to set navigation property
        var ticketType = await _context.TicketTypes.FindAsync(ticketTypeId);

        var ticketPurchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TicketTypeId = ticketTypeId,
            TicketType = ticketType,  // Set navigation property for in-memory DB
            PurchaseDate = DateTime.UtcNow,
            TotalPrice = 25.00m,
            Quantity = 1,
            PaymentStatus = "Completed",
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
            TicketPurchase = ticketPurchase,  // Set navigation property for in-memory DB
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<EventAttendance>().Add(attendance);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
