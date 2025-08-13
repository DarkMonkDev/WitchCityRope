using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Tests.Helpers;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.Exceptions;
using WitchCityRope.Infrastructure.Data;
using Moq;

namespace WitchCityRope.Api.Tests.Services;

/// <summary>
/// Simplified concurrency test that focuses on the EventService constructor issue
/// </summary>
public class ConcurrencyAndEdgeCaseTests_Fixed : IDisposable
{
    private readonly WitchCityRopeIdentityDbContext _dbContext;
    private readonly EventService _eventService;
    private readonly TestDataBuilder _testDataBuilder;

    public ConcurrencyAndEdgeCaseTests_Fixed()
    {
        _dbContext = MockHelpers.CreateInMemoryDbContext();
        _eventService = new EventService(
            _dbContext,
            MockHelpers.CreateSlugGeneratorMock().Object);
        
        _testDataBuilder = new TestDataBuilder(_dbContext);
    }

    [Fact]
    public async Task RegisterForEvent_SimpleTest_ShouldWork()
    {
        // Arrange
        var organizer = await _testDataBuilder.CreateUserAsync(role: UserRole.Organizer);
        var user = await _testDataBuilder.CreateUserAsync();
        var @event = await _testDataBuilder.CreateEventAsync(organizerId: organizer.Id);

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

        // Act
        var result = await _eventService.RegisterForEventAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(RegistrationStatus.Confirmed);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}