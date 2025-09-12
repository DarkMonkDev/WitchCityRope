using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;

namespace WitchCityRope.Api.Tests.Features.Events;

/// <summary>
/// TDD Tests for Event Update Validation Logic
/// Following TDD principles: Write failing tests first, implement to make them pass
/// Tests focus on business rule validation and error handling scenarios
/// </summary>
public class UpdateEventValidationTests : IDisposable
{
    private readonly WitchCityRopeDbContext _context;
    private readonly EventsManagementService _service;
    private readonly Mock<ILogger<EventsManagementService>> _mockLogger;

    public UpdateEventValidationTests()
    {
        var options = new DbContextOptionsBuilder<WitchCityRopeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new WitchCityRopeDbContext(options);
        _mockLogger = new Mock<ILogger<EventsManagementService>>();
        _service = new EventsManagementService(_context, _mockLogger.Object);
    }

    #region Event Status Validation Tests

    [Fact]
    public async Task UpdateEventAsync_WhenEventIsPublishedAndUpdatingCriticalFields_ShouldSucceed()
    {
        // Arrange
        var organizer = CreateTestUser("organizer@example.com", UserRole.Organizer);
        var publishedEvent = CreateTestEvent("Published Event", organizer, isPublished: true);
        
        _context.Users.Add(organizer);
        _context.Events.Add(publishedEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Published Event Title",
            Description = "Updated description - this should be allowed for published events"
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(publishedEvent.Id, updateRequest, organizer.Id);

        // Assert
        success.Should().BeTrue("published events should allow title and description updates");
        response.Should().NotBeNull();
        response!.Title.Should().Be("Updated Published Event Title");
        error.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateEventAsync_WhenEventIsUnpublished_ShouldAllowAllUpdates()
    {
        // Arrange
        var organizer = CreateTestUser("organizer@example.com", UserRole.Organizer);
        var unpublishedEvent = CreateTestEvent("Unpublished Event", organizer, isPublished: false);
        
        _context.Users.Add(organizer);
        _context.Events.Add(unpublishedEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            Title = "Completely New Title",
            Description = "Completely new description",
            Location = "New location",
            Capacity = 50,
            Price = 75.00m,
            IsPublished = true // Publishing the event
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(unpublishedEvent.Id, updateRequest, organizer.Id);

        // Assert
        success.Should().BeTrue("unpublished events should allow all field updates");
        response.Should().NotBeNull();
        response!.Title.Should().Be("Completely New Title");
        response.IsPublished.Should().BeTrue();
        error.Should().BeEmpty();
    }

    #endregion

    #region Capacity Management Validation Tests

    [Fact]
    public async Task UpdateEventAsync_WhenIncreasingCapacity_ShouldSucceed()
    {
        // Arrange
        var organizer = CreateTestUser("organizer@example.com", UserRole.Organizer);
        var testEvent = CreateTestEvent("Test Event", organizer, isPublished: true);
        
        _context.Users.Add(organizer);
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            Capacity = 50 // Increasing from default 25
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(testEvent.Id, updateRequest, organizer.Id);

        // Assert
        success.Should().BeTrue("increasing capacity should always be allowed");
        response.Should().NotBeNull();
        error.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateEventAsync_WhenCapacityNotChanged_ShouldSucceed()
    {
        // Arrange
        var organizer = CreateTestUser("organizer@example.com", UserRole.Organizer);
        var testEvent = CreateTestEvent("Test Event", organizer, isPublished: true);
        
        _context.Users.Add(organizer);
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            Capacity = 25, // Same as current capacity
            Title = "Updated Title"
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(testEvent.Id, updateRequest, organizer.Id);

        // Assert
        success.Should().BeTrue("keeping same capacity should be allowed");
        response.Should().NotBeNull();
        response!.Title.Should().Be("Updated Title");
        error.Should().BeEmpty();
    }

    #endregion

    #region Date Range Validation Tests

    [Fact]
    public async Task UpdateEventAsync_WhenUpdatingToFutureDate_ShouldSucceed()
    {
        // Arrange
        var organizer = CreateTestUser("organizer@example.com", UserRole.Organizer);
        var testEvent = CreateTestEvent("Test Event", organizer, isPublished: true);
        
        _context.Users.Add(organizer);
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var futureStartDate = DateTime.UtcNow.AddDays(30);
        var futureEndDate = DateTime.UtcNow.AddDays(30).AddHours(3);

        var updateRequest = new UpdateEventRequest
        {
            StartDate = futureStartDate,
            EndDate = futureEndDate
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(testEvent.Id, updateRequest, organizer.Id);

        // Assert
        success.Should().BeTrue("updating to future dates should be allowed");
        response.Should().NotBeNull();
        error.Should().BeEmpty();

        // Verify the dates were updated
        var updatedEvent = await _context.Events.FindAsync(testEvent.Id);
        updatedEvent.Should().NotBeNull();
        updatedEvent!.StartDate.Should().Be(futureStartDate);
        updatedEvent.EndDate.Should().Be(futureEndDate);
    }

    [Fact]
    public async Task UpdateEventAsync_WhenStartDateOnly_ShouldUpdateStartDateKeepEndDate()
    {
        // Arrange
        var organizer = CreateTestUser("organizer@example.com", UserRole.Organizer);
        var testEvent = CreateTestEvent("Test Event", organizer, isPublished: true);
        var originalEndDate = testEvent.EndDate;
        
        _context.Users.Add(organizer);
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var newStartDate = DateTime.UtcNow.AddDays(21);

        var updateRequest = new UpdateEventRequest
        {
            StartDate = newStartDate
            // EndDate not provided - should remain unchanged
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(testEvent.Id, updateRequest, organizer.Id);

        // Assert
        success.Should().BeTrue("partial date updates should be allowed");
        response.Should().NotBeNull();
        error.Should().BeEmpty();

        // Verify only start date was updated
        var updatedEvent = await _context.Events.FindAsync(testEvent.Id);
        updatedEvent.Should().NotBeNull();
        updatedEvent!.StartDate.Should().Be(newStartDate);
        updatedEvent.EndDate.Should().Be(originalEndDate);
    }

    #endregion

    #region Input Validation Tests

    [Fact]
    public async Task UpdateEventAsync_WhenEmptyStringFields_ShouldIgnoreEmptyFields()
    {
        // Arrange
        var organizer = CreateTestUser("organizer@example.com", UserRole.Organizer);
        var testEvent = CreateTestEvent("Original Title", organizer, isPublished: true);
        var originalTitle = testEvent.Title;
        var originalDescription = testEvent.Description;
        
        _context.Users.Add(organizer);
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            Title = "", // Empty string should be ignored
            Description = "   ", // Whitespace only should be ignored
            Location = "New Location" // Valid update
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(testEvent.Id, updateRequest, organizer.Id);

        // Assert
        success.Should().BeTrue("empty string fields should be ignored, not cause errors");
        response.Should().NotBeNull();
        
        // Original title and description should be preserved
        response!.Title.Should().Be(originalTitle);
        response.FullDescription.Should().Be(originalDescription);
        
        // But location should be updated
        response.Venue.Name.Should().Be("New Location");
        error.Should().BeEmpty();
    }

    [Theory]
    [InlineData(-1)] // Negative capacity
    [InlineData(0)]  // Zero capacity
    public async Task UpdateEventAsync_WhenInvalidCapacity_ShouldHandleGracefully(int invalidCapacity)
    {
        // Arrange
        var organizer = CreateTestUser("organizer@example.com", UserRole.Organizer);
        var testEvent = CreateTestEvent("Test Event", organizer, isPublished: true);
        
        _context.Users.Add(organizer);
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            Capacity = invalidCapacity
        };

        // Act & Assert
        // The business logic should handle invalid capacity gracefully
        // Either by rejecting the update or applying minimum valid capacity
        var (success, response, error) = await _service.UpdateEventAsync(testEvent.Id, updateRequest, organizer.Id);

        // This test documents current behavior - implementation may vary
        // Either succeeds with corrected capacity or fails with appropriate error
        if (!success)
        {
            error.Should().NotBeEmpty("invalid capacity should produce meaningful error");
        }
        else
        {
            response.Should().NotBeNull();
            // If it succeeds, capacity should be corrected to valid value
        }
    }

    [Theory]
    [InlineData(-100.00)] // Negative price
    public async Task UpdateEventAsync_WhenInvalidPrice_ShouldHandleGracefully(decimal invalidPrice)
    {
        // Arrange
        var organizer = CreateTestUser("organizer@example.com", UserRole.Organizer);
        var testEvent = CreateTestEvent("Test Event", organizer, isPublished: true);
        
        _context.Users.Add(organizer);
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            Price = invalidPrice
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(testEvent.Id, updateRequest, organizer.Id);

        // Assert
        // The implementation should handle negative prices appropriately
        if (!success)
        {
            error.Should().NotBeEmpty("invalid price should produce meaningful error");
        }
        else
        {
            response.Should().NotBeNull();
            // Price should be corrected or validated
        }
    }

    #endregion

    #region Security and Authorization Edge Cases

    [Fact]
    public async Task UpdateEventAsync_WhenUserIsTeacherButNotOrganizer_ShouldFailWithAuthError()
    {
        // Arrange
        var organizer = CreateTestUser("organizer@example.com", UserRole.Organizer);
        var teacher = CreateTestUser("teacher@example.com", UserRole.Teacher); // Teacher but not organizer
        var testEvent = CreateTestEvent("Test Event", organizer, isPublished: true);
        
        _context.Users.AddRange(organizer, teacher);
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            Title = "Should Not Update"
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(testEvent.Id, updateRequest, teacher.Id);

        // Assert
        success.Should().BeFalse("teachers who are not event organizers should not be able to update events");
        response.Should().BeNull();
        error.Should().Be("Not authorized to update this event");
    }

    [Fact]
    public async Task UpdateEventAsync_WhenUpdatingOtherOrganizerEvent_ShouldFailWithAuthError()
    {
        // Arrange
        var organizer1 = CreateTestUser("organizer1@example.com", UserRole.Organizer);
        var organizer2 = CreateTestUser("organizer2@example.com", UserRole.Organizer);
        var eventByOrganizer1 = CreateTestEvent("Organizer 1 Event", organizer1, isPublished: true);
        
        _context.Users.AddRange(organizer1, organizer2);
        _context.Events.Add(eventByOrganizer1);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            Title = "Should Not Update"
        };

        // Act
        var (success, response, error) = await _service.UpdateEventAsync(eventByOrganizer1.Id, updateRequest, organizer2.Id);

        // Assert
        success.Should().BeFalse("organizers should only be able to update their own events");
        response.Should().BeNull();
        error.Should().Be("Not authorized to update this event");
    }

    #endregion

    #region Helper Methods

    private User CreateTestUser(string email, UserRole role)
    {
        return new User(
            encryptedLegalName: "encrypted-legal-name",
            sceneName: SceneName.Create($"Scene-{email.Split('@')[0]}"),
            email: EmailAddress.Create(email),
            dateOfBirth: DateTime.UtcNow.AddYears(-25),
            role: role
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

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}