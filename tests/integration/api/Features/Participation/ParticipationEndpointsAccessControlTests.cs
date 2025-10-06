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
using WitchCityRope.Api.Enums;
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
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the app's DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add DbContext using the test container's connection string
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseNpgsql(ConnectionString);
                    });
                });
            });
    }

    #region RSVP Access Control Tests (5 tests)

    [Fact]
    public async Task RsvpEndpoint_WhenUserIsApproved_Returns201()
    {
        // Arrange
        var (client, eventId) = await SetupTestScenarioAsync(VettingStatus.Approved);

        var request = new CreateRSVPRequest
        {
            EventId = eventId
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
    public async Task RsvpEndpoint_WhenUserHasNoApplication_Succeeds()
    {
        // Arrange
        var (client, eventId) = await SetupTestScenarioAsync(null); // No vetting application

        var request = new CreateRSVPRequest
        {
            EventId = eventId
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/rsvp", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "Users without vetting applications should be allowed to RSVP");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
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
            EventId = eventId
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
    public async Task TicketEndpoint_WhenUserHasNoApplication_Succeeds()
    {
        // Arrange
        var (client, eventId) = await SetupTestScenarioAsync(null, isClassEvent: true); // No vetting application

        var request = new CreateTicketPurchaseRequest
        {
            EventId = eventId
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/tickets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "Users without vetting applications should be allowed to purchase tickets");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test scenario with user, event, and optional vetting application
    /// </summary>
    private async Task<(HttpClient client, Guid eventId)> SetupTestScenarioAsync(
        VettingStatus? vettingStatus,
        bool isClassEvent = false)
    {
        var userId = Guid.NewGuid();
        var email = $"test-{userId:N}@witchcityrope.com";

        await using var context = CreateDbContext();

        // Create user
        var user = new ApplicationUser
        {
            Id = userId,
            Email = email,
            UserName = email,
            SceneName = $"TestUser{userId:N}",
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
                RealName = "User",
                WorkflowStatus = vettingStatus.Value,
                SubmittedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.VettingApplications.Add(application);
        }

        // Create event
        var eventId = Guid.NewGuid();
        var evt = new Event
        {
            Id = eventId,
            Title = isClassEvent ? "Test Class Event" : "Test Social Event",
            Description = "Test event for integration testing",
            EventType = isClassEvent ? EventType.Class.ToString() : EventType.Social.ToString(),
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Capacity = 50,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Events.Add(evt);

        await context.SaveChangesAsync();

        // Create authenticated client
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(userId, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

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
