using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WitchCityRope.Api.Tests.Features.Events;

/// <summary>
/// Unit tests for EventsManagementService
/// Tests the first vertical slice implementation (GET endpoints)
/// </summary>
public class EventsManagementServiceTests : IDisposable
{
    private readonly WitchCityRopeDbContext _context;
    private readonly EventsManagementService _service;
    private readonly Mock<ILogger<EventsManagementService>> _mockLogger;

    public EventsManagementServiceTests()
    {
        var options = new DbContextOptionsBuilder<WitchCityRopeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new WitchCityRopeDbContext(options);
        _mockLogger = new Mock<ILogger<EventsManagementService>>();
        _service = new EventsManagementService(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task GetPublishedEventsAsync_ReturnsEmptyList_WhenNoEventsExist()
    {
        // Act
        var (success, response, error) = await _service.GetPublishedEventsAsync();

        // Assert
        Assert.True(success);
        Assert.NotNull(response);
        Assert.Empty(response);
        Assert.Equal(string.Empty, error);
    }

    [Fact]
    public async Task GetPublishedEventsAsync_ReturnsPublishedEventsOnly()
    {
        // Arrange
        var organizer = CreateTestUser("organizer@example.com", UserRole.Organizer);
        var publishedEvent = CreateTestEvent("Published Event", organizer, isPublished: true);
        var unpublishedEvent = CreateTestEvent("Draft Event", organizer, isPublished: false);

        _context.Users.Add(organizer);
        _context.Events.AddRange(publishedEvent, unpublishedEvent);
        await _context.SaveChangesAsync();

        // Act
        var (success, response, error) = await _service.GetPublishedEventsAsync();

        // Assert
        Assert.True(success);
        Assert.NotNull(response);
        Assert.Single(response);
        Assert.Equal("Published Event", response[0].Title);
    }

    [Fact]
    public async Task GetEventDetailsAsync_ReturnsEventDetails_WhenEventExists()
    {
        // Arrange
        var organizer = CreateTestUser("organizer@example.com", UserRole.Organizer);
        var testEvent = CreateTestEvent("Test Event", organizer, isPublished: true);
        
        // Add a session
        var session = new EventSession(
            eventId: testEvent.Id,
            sessionIdentifier: "S1",
            name: "Session 1",
            date: DateTime.UtcNow.AddDays(1),
            startTime: new TimeSpan(18, 0, 0),
            endTime: new TimeSpan(21, 0, 0),
            capacity: 20
        );
        testEvent.AddSession(session);

        _context.Users.Add(organizer);
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        // Act
        var (success, response, error) = await _service.GetEventDetailsAsync(testEvent.Id);

        // Assert
        Assert.True(success);
        Assert.NotNull(response);
        Assert.Equal("Test Event", response.Title);
        Assert.Single(response.Sessions);
        Assert.Equal("S1", response.Sessions[0].SessionIdentifier);
    }

    [Fact]
    public async Task GetEventDetailsAsync_ReturnsError_WhenEventNotFound()
    {
        // Arrange
        var nonExistentEventId = Guid.NewGuid();

        // Act
        var (success, response, error) = await _service.GetEventDetailsAsync(nonExistentEventId);

        // Assert
        Assert.False(success);
        Assert.Null(response);
        Assert.Equal("Event not found", error);
    }

    [Fact]
    public async Task GetEventAvailabilityAsync_ReturnsAvailability_WhenEventExists()
    {
        // Arrange
        var organizer = CreateTestUser("organizer@example.com", UserRole.Organizer);
        var testEvent = CreateTestEvent("Test Event", organizer, isPublished: true);
        
        // Add a session
        var session = new EventSession(
            eventId: testEvent.Id,
            sessionIdentifier: "S1",
            name: "Session 1",
            date: DateTime.UtcNow.AddDays(1),
            startTime: new TimeSpan(18, 0, 0),
            endTime: new TimeSpan(21, 0, 0),
            capacity: 20
        );
        testEvent.AddSession(session);

        _context.Users.Add(organizer);
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        // Act
        var (success, response, error) = await _service.GetEventAvailabilityAsync(testEvent.Id);

        // Assert
        Assert.True(success);
        Assert.NotNull(response);
        Assert.Equal(testEvent.Id, response.EventId);
        Assert.Single(response.Sessions);
        Assert.Equal("S1", response.Sessions[0].SessionId);
        Assert.Equal(20, response.Sessions[0].Capacity);
        Assert.Equal(0, response.Sessions[0].Sold);
        Assert.Equal(20, response.Sessions[0].Available);
    }

    private User CreateTestUser(string email, UserRole role)
    {
        return new User(
            id: Guid.NewGuid(),
            email: Email.Create(email),
            passwordHash: "hashedpassword",
            sceneName: SceneName.Create($"Scene-{email.Split('@')[0]}"),
            role: role,
            isActive: true,
            isVetted: role == UserRole.Organizer || role == UserRole.Administrator,
            dateJoined: DateTime.UtcNow
        );
    }

    private Event CreateTestEvent(string title, User organizer, bool isPublished = false)
    {
        var pricingTiers = new List<Money> { Money.Create(50, "USD") };
        
        var eventEntity = new Event(
            title: title,
            description: $"Description for {title}",
            startDate: DateTime.UtcNow.AddDays(7),
            endDate: DateTime.UtcNow.AddDays(7).AddHours(3),
            capacity: 25,
            eventType: EventType.Class,
            location: "Test Venue",
            primaryOrganizer: organizer,
            pricingTiers: pricingTiers
        );

        if (isPublished)
        {
            eventEntity.Publish();
        }

        return eventEntity;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}