using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.IntegrationTests.Api.Features.Participation;

/// <summary>
/// Integration tests for Participation endpoints access control
/// Tests vetting status enforcement for RSVP and ticket purchases
/// Phase 2: Integration Tests - Complete Vetting Workflow
/// </summary>
[Collection("Database")]
public class ParticipationEndpointsAccessControlTests : IntegrationTestBase
{
    private readonly WebApplicationFactory<Program> _factory;

    public ParticipationEndpointsAccessControlTests(DatabaseTestFixture fixture)
        : base(fixture)
    {
        _factory = CreateTestWebApplicationFactory();
    }

    #region RSVP Access Control Tests (5 tests)

    [Fact]
    public async Task RsvpEndpoint_WhenUserIsApproved_Returns201()
    {
        // Arrange
        var (client, eventId) = await SetupTestScenarioAsync(VettingStatus.Approved);

        var request = new CreateRSVPRequest
        {
            EventId = eventId,
            EventWaiverAccepted = true
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/rsvp", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RsvpEndpoint_WhenUserIsDenied_Returns403()
    {
        // Arrange
        var (client, eventId) = await SetupTestScenarioAsync(VettingStatus.Denied);

        var request = new CreateRSVPRequest
        {
            EventId = eventId
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/rsvp", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Denied", "Response should indicate vetting denial");
    }

    [Fact]
    public async Task RsvpEndpoint_WhenUserIsOnHold_Returns403()
    {
        // Arrange
        var (client, eventId) = await SetupTestScenarioAsync(VettingStatus.OnHold);

        var request = new CreateRSVPRequest
        {
            EventId = eventId
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/rsvp", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("OnHold", "Response should indicate vetting on hold status");
    }

    [Fact]
    public async Task RsvpEndpoint_WhenUserIsWithdrawn_Returns403()
    {
        // Arrange
        var (client, eventId) = await SetupTestScenarioAsync(VettingStatus.Withdrawn);

        var request = new CreateRSVPRequest
        {
            EventId = eventId
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/rsvp", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Withdrawn", "Response should indicate withdrawn status");
    }

    [Fact]
    public async Task RsvpEndpoint_WhenUserHasNoApplication_Returns400ForVettedOnlyEvent()
    {
        // Arrange — event is VettedMembersOnly, user has no vetting application (VettingStatus=0)
        var (client, eventId) = await SetupTestScenarioAsync(null); // No vetting application

        var request = new CreateRSVPRequest
        {
            EventId = eventId
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/rsvp", request);

        // Assert — Non-vetted users cannot RSVP for vetted-only events
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Users without vetting (VettingStatus=0) should be blocked from vetted-only events");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("vetted", "Response should indicate vetting requirement");
    }

    #endregion

    #region Ticket Purchase Access Control Tests (5 tests)

    [Fact]
    public async Task TicketEndpoint_WhenUserIsApproved_Returns201()
    {
        // Arrange
        var (client, eventId) = await SetupTestScenarioAsync(VettingStatus.Approved, isClassEvent: true);

        var request = new CreateTicketPurchaseRequest
        {
            EventId = eventId,
            TicketTypeIds = new List<Guid> { _lastTicketTypeId },
            EventWaiverAccepted = true
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/tickets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TicketEndpoint_WhenUserIsDenied_Returns403()
    {
        // Arrange
        var (client, eventId) = await SetupTestScenarioAsync(VettingStatus.Denied, isClassEvent: true);

        var request = new CreateTicketPurchaseRequest
        {
            EventId = eventId
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/tickets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Denied", "Response should indicate vetting denial");
    }

    [Fact]
    public async Task TicketEndpoint_WhenUserIsOnHold_Returns403()
    {
        // Arrange
        var (client, eventId) = await SetupTestScenarioAsync(VettingStatus.OnHold, isClassEvent: true);

        var request = new CreateTicketPurchaseRequest
        {
            EventId = eventId
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/tickets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("OnHold", "Response should indicate vetting on hold status");
    }

    [Fact]
    public async Task TicketEndpoint_WhenUserIsWithdrawn_Returns403()
    {
        // Arrange
        var (client, eventId) = await SetupTestScenarioAsync(VettingStatus.Withdrawn, isClassEvent: true);

        var request = new CreateTicketPurchaseRequest
        {
            EventId = eventId
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/tickets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Withdrawn", "Response should indicate withdrawn status");
    }

    [Fact]
    public async Task TicketEndpoint_WhenUserHasNoApplication_Returns400ForVettedOnlyEvent()
    {
        // Arrange — event is VettedMembersOnly, user has no vetting application (VettingStatus=0)
        var (client, eventId) = await SetupTestScenarioAsync(null, isClassEvent: true); // No vetting application

        var request = new CreateTicketPurchaseRequest
        {
            EventId = eventId
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/tickets", request);

        // Assert — Non-vetted users cannot purchase tickets for vetted-only events
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Users without vetting (VettingStatus=0) should be blocked from vetted-only events");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("vetted", "Response should indicate vetting requirement");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test scenario with user, event, and optional vetting application.
    /// For class events, also creates Session and TicketType (required for ticket purchase).
    /// Returns (client, eventId) or for class events (client, eventId) where the TicketTypeId
    /// is stored in _lastTicketTypeId for use in ticket purchase requests.
    /// </summary>
    private Guid _lastTicketTypeId;

    private async Task<(HttpClient client, Guid eventId)> SetupTestScenarioAsync(
        VettingStatus? vettingStatus,
        bool isClassEvent = false)
    {
        var userId = Guid.NewGuid();
        var email = $"test-{userId:N}@witchcityrope.com";

        await using var context = CreateDbContext();

        // Create user — VettingStatus int must match VettingApplication.WorkflowStatus
        // IsVetted checks VettingStatus == 3 (Approved)
        var user = new ApplicationUser
        {
            Id = userId,
            Email = email,
            UserName = email,
            SceneName = $"TestUser{userId:N}",
            VettingStatus = vettingStatus ?? VettingStatus.UnderReview,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);

        // Create vetting application if status is specified
        if (vettingStatus.HasValue)
        {
            var application = new VettingApplication
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Email = email,
                SceneName = "Test",
                FirstName = "Test",
                LastName = "User",
                WorkflowStatus = vettingStatus.Value,
                SubmittedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.VettingApplications.Add(application);
        }

        // Create venue first (required for foreign key constraint)
        var venueId = await CreateTestVenueAsync();

        // Create event
        var eventId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(7);
        var evt = new Event
        {
            Id = eventId,
            Title = isClassEvent ? "Test Class Event" : "Test Social Event",
            Description = "Test event for integration testing",
            AllowRsvps = !isClassEvent,
            RequireTicketPurchase = isClassEvent,
            VettedMembersOnly = true,
            VenueId = venueId,
            StartDate = startDate,
            EndDate = startDate.AddHours(2),
            Capacity = 50,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Events.Add(evt);

        // For class events, create Session and TicketType (required for ticket purchase)
        if (isClassEvent)
        {
            var session = new WitchCityRope.Api.Models.Session
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                SessionCode = "S1",
                Name = "Main Session",
                StartTime = startDate,
                EndTime = startDate.AddHours(2),
                Capacity = 50,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Sessions.Add(session);

            var ticketType = new TicketType
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Name = "General Admission",
                Price = 25.00m,
                Available = 50,
                Sessions = new List<WitchCityRope.Api.Models.Session> { session },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.TicketTypes.Add(ticketType);
            _lastTicketTypeId = ticketType.Id;
        }

        await context.SaveChangesAsync();

        // Create authenticated client with CSRF token
        var token = GenerateJwtToken(userId, email);
        var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, token);

        return (client, eventId);
    }

    // GenerateJwtToken method inherited from IntegrationTestBase
    // Uses proper JWT generation matching API configuration

    #endregion

    public override async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await base.DisposeAsync();
    }
}
