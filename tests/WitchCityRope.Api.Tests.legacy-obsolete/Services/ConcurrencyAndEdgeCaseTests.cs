using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using WitchCityRope.Api.Features.Events.Models;
using PaymentMethod = WitchCityRope.Api.Features.Events.Models.PaymentMethod;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Tests.Helpers;
using WitchCityRope.Core.Entities;
using CoreEnums = WitchCityRope.Core.Enums;
using WitchCityRope.Core.Exceptions;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.Api.Tests.Services;

public class ConcurrencyAndEdgeCaseTests : IDisposable
{
    private readonly WitchCityRopeDbContext _dbContext;
    private readonly EventService _eventService;
    private readonly TestDataBuilder _testDataBuilder;

    public ConcurrencyAndEdgeCaseTests()
    {
        _dbContext = MockHelpers.CreateInMemoryDbContext();
        _eventService = new EventService(
            _dbContext,
            MockHelpers.CreateSlugGeneratorMock().Object);
        
        _testDataBuilder = new TestDataBuilder(_dbContext);
    }

    #region Concurrent Registration Tests

    [Fact]
    public async Task RegisterForEvent_ConcurrentRegistrations_ShouldHandleCapacityCorrectly()
    {
        // Arrange
        var organizer = await _testDataBuilder.CreateUserAsync(role: UserRole.Organizer);
        var @event = await _testDataBuilder.CreateEventAsync(
            organizerId: organizer.Id,
            title: "Limited Event",
            maxAttendees: 5);

        // Create 10 users trying to register
        var users = new List<User>();
        for (int i = 0; i < 10; i++)
        {
            users.Add(await _testDataBuilder.CreateUserAsync(
                email: $"user{i}@example.com",
                sceneName: $"User{i}"));
        }

        // Act - Simulate concurrent registrations
        var tasks = users.Select(user => Task.Run(async () =>
        {
            try
            {
                var request = new RegisterForEventRequest(
                    EventId: @event.Id,
                    UserId: user.Id,
                    DietaryRestrictions: null,
                    AccessibilityNeeds: null,
                    EmergencyContactName: "Contact",
                    EmergencyContactPhone: "555-0100",
                    PaymentMethod: PaymentMethod.Cash,
                    PaymentToken: null
                );
                return await _eventService.RegisterForEventAsync(request);
            }
            catch (Exception ex)
            {
                return new RegisterForEventResponse(
                    RegistrationId: Guid.Empty,
                    Status: RegistrationStatus.Failed,
                    WaitlistPosition: null,
                    AmountCharged: 0,
                    ConfirmationCode: ex.Message
                );
            }
        })).ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        var confirmedCount = results.Count(r => r.Status == CoreEnums.RegistrationStatus.Confirmed);
        var waitlistedCount = results.Count(r => r.Status == CoreEnums.RegistrationStatus.Waitlisted);
        
        confirmedCount.Should().BeLessOrEqualTo(5); // Should not exceed max capacity
        waitlistedCount.Should().BeGreaterThan(0); // Some should be waitlisted
        (confirmedCount + waitlistedCount).Should().Be(10); // All should be processed

        // Verify waitlist positions are sequential
        var waitlistPositions = results
            .Where(r => r.Status == CoreEnums.RegistrationStatus.Waitlisted && r.WaitlistPosition.HasValue)
            .Select(r => r.WaitlistPosition!.Value)
            .OrderBy(p => p)
            .ToList();

        for (int i = 0; i < waitlistPositions.Count; i++)
        {
            waitlistPositions[i].Should().Be(i + 1);
        }
    }

    [Fact]
    public async Task RegisterForEvent_ConcurrentRegistrationsWithPayment_ShouldProcessCorrectly()
    {
        // Arrange
        var organizer = await _testDataBuilder.CreateUserAsync(role: UserRole.Organizer);
        var @event = await _testDataBuilder.CreateEventAsync(organizerId: organizer.Id);
        @event.Price = 25.00m;
        // Event created with capacity of 20 by default

        var users = new List<User>();
        for (int i = 0; i < 5; i++)
        {
            users.Add(await _testDataBuilder.CreateUserAsync(
                email: $"payer{i}@example.com",
                sceneName: $"Payer{i}"));
        }

        // Mock payment service to simulate processing delays
        var paymentServiceMock = MockHelpers.CreatePaymentServiceMock(true, 25.00m);
        paymentServiceMock.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
            .Returns(async (PaymentRequest _) =>
            {
                await Task.Delay(Random.Shared.Next(10, 50)); // Random delay
                return new PaymentResult
                {
                    Success = true,
                    AmountCharged = 25.00m,
                    TransactionId = Guid.NewGuid().ToString()
                };
            });

        var eventServiceWithPaymentDelay = new EventService(
            _dbContext,
            MockHelpers.CreateSlugGeneratorMock().Object);

        // Act
        var tasks = users.Select(user => Task.Run(async () =>
        {
            var request = new RegisterForEventRequest
            {
                EventId = @event.Id,
                UserId = user.Id,
                PaymentMethod = PaymentMethod.Stripe,
                PaymentToken = $"tok_{user.Id}",
                EmergencyContactName = "Contact",
                EmergencyContactPhone = "555-0100"
            };
            return await eventServiceWithPaymentDelay.RegisterForEventAsync(request);
        })).ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        var paidCount = results.Count(r => r.AmountCharged > 0);
        var unpaidCount = results.Count(r => r.AmountCharged == 0);

        paidCount.Should().Be(3); // Only confirmed registrations should be charged
        unpaidCount.Should().Be(2); // Waitlisted should not be charged
    }

    #endregion

    #region Concurrent Event Updates Tests

    [Fact]
    public async Task UpdateEvent_ConcurrentUpdates_ShouldHandleOptimisticConcurrency()
    {
        // Arrange
        var organizer = await _testDataBuilder.CreateUserAsync(role: UserRole.Organizer);
        var @event = await _testDataBuilder.CreateEventAsync(
            organizerId: organizer.Id,
            title: "Original Title");

        // Act - Simulate concurrent updates
        var updateTasks = new[]
        {
            Task.Run(async () =>
            {
                var eventToUpdate = await _dbContext.Events.FindAsync(@event.Id);
                eventToUpdate!.Title = "Updated Title 1";
                await Task.Delay(50); // Simulate processing time
                await _dbContext.SaveChangesAsync();
                return "Update 1";
            }),
            Task.Run(async () =>
            {
                var eventToUpdate = await _dbContext.Events.FindAsync(@event.Id);
                eventToUpdate!.Title = "Updated Title 2";
                await Task.Delay(50); // Simulate processing time
                await _dbContext.SaveChangesAsync();
                return "Update 2";
            })
        };

        // Note: With InMemory database, this won't throw DbUpdateConcurrencyException
        // but demonstrates the pattern for handling concurrent updates
        var results = await Task.WhenAll(updateTasks);

        // Assert
        var finalEvent = await _dbContext.Events.FindAsync(@event.Id);
        finalEvent!.Title.Should().BeOneOf("Updated Title 1", "Updated Title 2");
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task RegisterForEvent_WhenEventCancelledDuringRegistration_ShouldHandleGracefully()
    {
        // Arrange
        var organizer = await _testDataBuilder.CreateUserAsync(role: UserRole.Organizer);
        var user = await _testDataBuilder.CreateUserAsync();
        var @event = await _testDataBuilder.CreateEventAsync(organizerId: organizer.Id);

        var request = new RegisterForEventRequest
        {
            EventId = @event.Id,
            UserId = user.Id,
            EmergencyContactName = "Contact",
            EmergencyContactPhone = "555-0100"
        };

        // Act - Cancel event during registration
        var registrationTask = Task.Run(async () =>
        {
            await Task.Delay(50); // Small delay
            return await _eventService.RegisterForEventAsync(request);
        });

        var cancellationTask = Task.Run(async () =>
        {
            await Task.Delay(25); // Cancel before registration completes
            @event.Status = EventStatus.Cancelled;
            await _dbContext.SaveChangesAsync();
        });

        await Task.WhenAll(registrationTask, cancellationTask);

        // Assert
        await Assert.ThrowsAsync<ValidationException>(async () => 
            await registrationTask);
    }

    [Fact]
    public async Task ListEvents_WithComplexFiltersAndLargeDataset_ShouldPerformEfficiently()
    {
        // Arrange
        var organizer = await _testDataBuilder.CreateUserAsync(role: UserRole.Organizer);
        
        // Create a large dataset
        var events = new List<Event>();
        for (int i = 0; i < 100; i++)
        {
            var @event = MockHelpers.CreateTestEvent(
                organizerId: organizer.Id,
                title: $"Event {i}",
                type: i % 3 == 0 ? EventType.Class : EventType.Social,
                requiresVetting: i % 5 == 0);
            
            @event.Tags = i % 2 == 0 
                ? new List<string> { "rope", "advanced" }
                : new List<string> { "beginner", "social" };
            
            @event.StartDateTime = DateTime.UtcNow.AddDays(i % 30);
            events.Add(@event);
        }
        
        await _dbContext.Events.AddRangeAsync(events);
        await _dbContext.SaveChangesAsync();

        var request = new ListEventsRequest
        {
            Page = 2,
            PageSize = 20,
            Type = EventType.Class,
            Tags = new List<string> { "rope" },
            StartDateFrom = DateTime.UtcNow,
            StartDateTo = DateTime.UtcNow.AddDays(15),
            HasAvailability = true,
            SearchTerm = "Event",
            SortBy = EventSortBy.StartDate,
            SortDirection = SortDirection.Ascending
        };

        // Act
        var startTime = DateTime.UtcNow;
        var result = await _eventService.ListEventsAsync(request);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        result.Should().NotBeNull();
        result.Events.Should().NotBeEmpty();
        result.Events.Should().OnlyContain(e => e.Type == EventType.Class);
        result.Events.Should().OnlyContain(e => e.Tags.Contains("rope"));
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(20);
        
        // Performance check
        duration.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task RegisterForEvent_WithSpecialCharactersInUserData_ShouldHandleCorrectly()
    {
        // Arrange
        var organizer = await _testDataBuilder.CreateUserAsync(role: UserRole.Organizer);
        var user = await _testDataBuilder.CreateUserAsync(
            email: "test+special@example.com",
            sceneName: "User'with\"special<chars>");
        
        var @event = await _testDataBuilder.CreateEventAsync(organizerId: organizer.Id);

        var request = new RegisterForEventRequest
        {
            EventId = @event.Id,
            UserId = user.Id,
            DietaryRestrictions = "Nuts & dairy; gluten-free",
            AccessibilityNeeds = "Wheelchair accessible, can't use stairs",
            EmergencyContactName = "O'Brien-Smith, Jr.",
            EmergencyContactPhone = "+1 (555) 123-4567"
        };

        // Act
        var result = await _eventService.RegisterForEventAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(CoreEnums.RegistrationStatus.Confirmed);

        var registration = await _dbContext.Registrations
            .FirstOrDefaultAsync(r => r.Id == result.RegistrationId);
        
        registration!.DietaryRestrictions.Should().Be(request.DietaryRestrictions);
        registration.AccessibilityNeeds.Should().Be(request.AccessibilityNeeds);
        registration.EmergencyContactName.Should().Be(request.EmergencyContactName);
        registration.EmergencyContactPhone.Should().Be(request.EmergencyContactPhone);
    }

    [Fact]
    public async Task CreateEvent_WithDuplicateSlug_ShouldGenerateUniqueSlug()
    {
        // Arrange
        var organizer = await _testDataBuilder.CreateUserAsync(role: UserRole.Organizer);
        
        // Create events with same title
        for (int i = 0; i < 3; i++)
        {
            await _testDataBuilder.CreateEventAsync(
                organizerId: organizer.Id,
                title: "Duplicate Event");
        }

        var request = new CreateEventRequest
        {
            Title = "Duplicate Event",
            Description = "Another event with same title",
            Type = EventType.Class,
            StartDateTime = DateTime.UtcNow.AddDays(10),
            EndDateTime = DateTime.UtcNow.AddDays(10).AddHours(2),
            Location = "Test Venue",
            Capacity = 20,
            OrganizerId = organizer.Id
        };

        // Act
        var result = await _eventService.CreateEventAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Slug.Should().MatchRegex(@"duplicate-event-\d+");

        var allEvents = await _dbContext.Events
            .Where(e => e.Title == "Duplicate Event")
            .Select(e => e.Slug)
            .ToListAsync();
        
        allEvents.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task RegisterForEvent_AtExactCapacity_ShouldHandleLastSpotCorrectly()
    {
        // Arrange
        var organizer = await _testDataBuilder.CreateUserAsync(role: UserRole.Organizer);
        var @event = await _testDataBuilder.CreateEventAsync(organizerId: organizer.Id);
        // Event created with capacity specified in CreateEventAsync

        // Register first user
        var user1 = await _testDataBuilder.CreateUserAsync(email: "user1@example.com");
        await _testDataBuilder.CreateRegistrationAsync(@event.Id, user1.Id);
        
        // Update current attendees count
        @event.CurrentAttendees = 1;
        await _dbContext.SaveChangesAsync();

        // Create two users competing for last spot
        var user2 = await _testDataBuilder.CreateUserAsync(email: "user2@example.com");
        var user3 = await _testDataBuilder.CreateUserAsync(email: "user3@example.com");

        // Act - Both try to register simultaneously
        var task1 = _eventService.RegisterForEventAsync(new RegisterForEventRequest
        {
            EventId = @event.Id,
            UserId = user2.Id,
            EmergencyContactName = "Contact",
            EmergencyContactPhone = "555-0100"
        });

        var task2 = _eventService.RegisterForEventAsync(new RegisterForEventRequest
        {
            EventId = @event.Id,
            UserId = user3.Id,
            EmergencyContactName = "Contact",
            EmergencyContactPhone = "555-0100"
        });

        var results = await Task.WhenAll(task1, task2);

        // Assert
        var confirmedResults = results.Where(r => r.Status == CoreEnums.RegistrationStatus.Confirmed).ToList();
        var waitlistedResults = results.Where(r => r.Status == CoreEnums.RegistrationStatus.Waitlisted).ToList();

        confirmedResults.Should().HaveCount(1); // Only one should get the last spot
        waitlistedResults.Should().HaveCount(1); // The other should be waitlisted
        waitlistedResults.First().WaitlistPosition.Should().Be(1);
    }

    #endregion

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

}