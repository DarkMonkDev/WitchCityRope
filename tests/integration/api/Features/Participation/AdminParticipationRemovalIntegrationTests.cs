using System;
using System.Collections.Generic;
using System.Linq;
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
using WitchCityRope.Api.Features.CheckIn.Entities;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.Models;
using WitchCityRope.Api.Models;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.IntegrationTests.Api.Features.Participation;

/// <summary>
/// Integration tests for admin RSVP removal and ticket refund endpoints
/// Tests full end-to-end flows including database updates, cascading effects, and authorization
/// NOTE: Uses Sequential collection to prevent database deadlocks from parallel test execution
/// </summary>
[Collection("Sequential")]
public class AdminParticipationRemovalIntegrationTests : IntegrationTestBase
{
    private readonly WebApplicationFactory<Program> _factory;

    public AdminParticipationRemovalIntegrationTests(DatabaseTestFixture fixture)
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

    #region Full Flow Tests - Admin Remove RSVP

    [Fact]
    public async Task AdminRemoveRsvp_EndToEnd_DatabaseUpdated()
    {
        // Arrange
        var (adminClient, adminUserId) = await CreateAuthenticatedAdminAsync("admin-remove@example.com");
        var (userClient, userId) = await CreateAuthenticatedUserAsync("user-rsvp@example.com");

        var eventId = await CreateTestEventAsync();
        await CreateRsvpAsync(eventId, userId);

        // Act
        var response = await adminClient.DeleteAsync(
            $"/api/admin/events/{eventId}/participations/{userId}/remove");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "admin should be able to remove RSVP");

        var result = await response.Content.ReadFromJsonAsync<AdminRemoveRsvpResponse>();
        result.Should().NotBeNull();
        result!.RsvpRemoved.Should().BeTrue("RSVP should be removed");

        // Verify database state
        await using var context = CreateDbContext();
        var rsvp = await context.EventAttendances
            .FirstOrDefaultAsync(ea => ea.EventId == eventId &&
                                      ea.UserId == userId &&
                                      ea.AttendanceType == AttendanceType.RSVP);

        rsvp.Should().NotBeNull("RSVP record should still exist");
        rsvp!.Status.Should().Be(AttendanceStatus.Cancelled, "RSVP should be cancelled");
        rsvp.CancelledAt.Should().NotBeNull("cancellation timestamp should be set");
        rsvp.CancellationReason.Should().Contain("admin", "cancellation reason should mention admin");
    }

    [Fact]
    public async Task AdminRemoveRsvp_WithTicket_DatabaseShowsRefund()
    {
        // Arrange
        var (adminClient, adminUserId) = await CreateAuthenticatedAdminAsync("admin-refund@example.com");
        var (userClient, userId) = await CreateAuthenticatedUserAsync("user-ticket@example.com");

        var eventId = await CreateTestEventAsync();
        await CreateRsvpAsync(eventId, userId);
        var ticketId = await CreateTicketAsync(eventId, userId, 25.00m);

        // Act
        var response = await adminClient.DeleteAsync(
            $"/api/admin/events/{eventId}/participations/{userId}/remove");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "admin should be able to remove RSVP with ticket");

        var result = await response.Content.ReadFromJsonAsync<AdminRemoveRsvpResponse>();
        result.Should().NotBeNull();
        result!.RsvpRemoved.Should().BeTrue("RSVP should be removed");
        result.TicketRefunded.Should().BeTrue("ticket should be refunded");
        result.RefundAmount.Should().Be(25.00m, "refund amount should match ticket price");

        // Verify database state
        await using var context = CreateDbContext();

        var ticket = await context.EventAttendances
            .FirstOrDefaultAsync(ea => ea.Id == ticketId);
        ticket.Should().NotBeNull("ticket record should still exist");
        ticket!.Status.Should().Be(AttendanceStatus.Refunded, "ticket should be refunded");

        var rsvp = await context.EventAttendances
            .FirstOrDefaultAsync(ea => ea.EventId == eventId &&
                                      ea.UserId == userId &&
                                      ea.AttendanceType == AttendanceType.RSVP);
        rsvp.Should().NotBeNull("RSVP record should still exist");
        rsvp!.Status.Should().Be(AttendanceStatus.Cancelled, "RSVP should be cancelled");
    }

    [Fact]
    public async Task AdminRemoveRsvp_CancelsVolunteerShiftsInDatabase()
    {
        // Arrange
        var (adminClient, adminUserId) = await CreateAuthenticatedAdminAsync("admin-volunteer@example.com");
        var (userClient, userId) = await CreateAuthenticatedUserAsync("user-volunteer@example.com");

        var eventId = await CreateTestEventAsync();
        await CreateRsvpAsync(eventId, userId);
        var volunteerPositionId = await CreateVolunteerPositionAsync(eventId, "Test Volunteer Position");
        await CreateVolunteerSignupAsync(userId, volunteerPositionId);

        // Act
        var response = await adminClient.DeleteAsync(
            $"/api/admin/events/{eventId}/participations/{userId}/remove");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "admin should be able to remove RSVP with volunteer signup");

        var result = await response.Content.ReadFromJsonAsync<AdminRemoveRsvpResponse>();
        result.Should().NotBeNull();
        result!.RsvpRemoved.Should().BeTrue("RSVP should be removed");
        result.VolunteerShiftsRemoved.Should().BeTrue("volunteer shifts should be cancelled");
        result.VolunteerShiftNames.Should().ContainSingle()
            .Which.Should().Be("Test Volunteer Position", "volunteer position title should be included");

        // Verify database state
        await using var context = CreateDbContext();
        var volunteerSignup = await context.VolunteerSignups
            .FirstOrDefaultAsync(vs => vs.UserId == userId && vs.VolunteerPositionId == volunteerPositionId);

        volunteerSignup.Should().NotBeNull("volunteer signup record should still exist");
        volunteerSignup!.Status.Should().Be(VolunteerSignupStatus.Cancelled, "volunteer signup should be cancelled");
    }

    [Fact]
    public async Task AdminRemoveRsvp_UpdatesEventAttendeeStatus()
    {
        // Arrange
        var (adminClient, adminUserId) = await CreateAuthenticatedAdminAsync("admin-attendee@example.com");
        var (userClient, userId) = await CreateAuthenticatedUserAsync("user-attendee@example.com");

        var eventId = await CreateTestEventAsync();
        await CreateRsvpAsync(eventId, userId);
        await CreateEventAttendeeAsync(eventId, userId);

        // Act
        var response = await adminClient.DeleteAsync(
            $"/api/admin/events/{eventId}/participations/{userId}/remove");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "admin should be able to remove RSVP");

        // Verify database state
        await using var context = CreateDbContext();
        var eventAttendee = await context.EventAttendees
            .FirstOrDefaultAsync(ea => ea.EventId == eventId && ea.UserId == userId);

        eventAttendee.Should().NotBeNull("event attendee record should still exist");
        eventAttendee!.RegistrationStatus.Should().Be("cancelled", "registration status should be updated to cancelled");
        eventAttendee.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5),
            "updated timestamp should be recent");
    }

    #endregion

    #region Full Flow Tests - Admin Refund Ticket

    [Fact]
    public async Task AdminRefundTicket_EndToEnd_DatabaseUpdated()
    {
        // Arrange
        var (adminClient, adminUserId) = await CreateAuthenticatedAdminAsync("admin-refund2@example.com");
        var (userClient, userId) = await CreateAuthenticatedUserAsync("user-refund@example.com");

        var eventId = await CreateTestEventAsync();
        var ticketId = await CreateTicketAsync(eventId, userId, 35.00m);

        var request = new AdminRefundTicketRequest { AlsoRemoveRsvp = false };

        // Act
        var response = await adminClient.PostAsJsonAsync(
            $"/api/admin/events/{eventId}/tickets/{userId}/refund",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "admin should be able to refund ticket");

        var result = await response.Content.ReadFromJsonAsync<AdminRefundTicketResponse>();
        result.Should().NotBeNull();
        result!.TicketRefunded.Should().BeTrue("ticket should be refunded");
        result.RefundAmount.Should().Be(35.00m, "refund amount should match ticket price");
        result.RsvpRemoved.Should().BeFalse("RSVP should not be removed when AlsoRemoveRsvp is false");

        // Verify database state
        await using var context = CreateDbContext();
        var ticket = await context.EventAttendances
            .FirstOrDefaultAsync(ea => ea.Id == ticketId);

        ticket.Should().NotBeNull("ticket record should still exist");
        ticket!.Status.Should().Be(AttendanceStatus.Refunded, "ticket should be refunded");
        ticket.CancelledAt.Should().NotBeNull("cancellation timestamp should be set");
    }

    [Fact]
    public async Task AdminRefundTicket_WithRsvp_DatabaseShowsRsvpRemoval()
    {
        // Arrange
        var (adminClient, adminUserId) = await CreateAuthenticatedAdminAsync("admin-both@example.com");
        var (userClient, userId) = await CreateAuthenticatedUserAsync("user-both@example.com");

        var eventId = await CreateTestEventAsync();
        await CreateRsvpAsync(eventId, userId);
        var ticketId = await CreateTicketAsync(eventId, userId, 40.00m);

        var request = new AdminRefundTicketRequest { AlsoRemoveRsvp = true };

        // Act
        var response = await adminClient.PostAsJsonAsync(
            $"/api/admin/events/{eventId}/tickets/{userId}/refund",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "admin should be able to refund ticket and remove RSVP");

        var result = await response.Content.ReadFromJsonAsync<AdminRefundTicketResponse>();
        result.Should().NotBeNull();
        result!.TicketRefunded.Should().BeTrue("ticket should be refunded");
        result.RefundAmount.Should().Be(40.00m, "refund amount should match ticket price");
        result.RsvpRemoved.Should().BeTrue("RSVP should be removed when AlsoRemoveRsvp is true");

        // Verify database state
        await using var context = CreateDbContext();

        var ticket = await context.EventAttendances
            .FirstOrDefaultAsync(ea => ea.Id == ticketId);
        ticket!.Status.Should().Be(AttendanceStatus.Refunded, "ticket should be refunded");

        var rsvp = await context.EventAttendances
            .FirstOrDefaultAsync(ea => ea.EventId == eventId &&
                                      ea.UserId == userId &&
                                      ea.AttendanceType == AttendanceType.RSVP);
        rsvp.Should().NotBeNull("RSVP record should still exist");
        rsvp!.Status.Should().Be(AttendanceStatus.Cancelled, "RSVP should be cancelled");
    }

    [Fact]
    public async Task AdminRefundTicket_CancelsVolunteerShiftsInDatabase()
    {
        // Arrange
        var (adminClient, adminUserId) = await CreateAuthenticatedAdminAsync("admin-volunteer2@example.com");
        var (userClient, userId) = await CreateAuthenticatedUserAsync("user-volunteer2@example.com");

        var eventId = await CreateTestEventAsync();
        var ticketId = await CreateTicketAsync(eventId, userId, 30.00m);
        var volunteerPositionId = await CreateVolunteerPositionAsync(eventId, "Cleanup Crew");
        await CreateVolunteerSignupAsync(userId, volunteerPositionId);

        var request = new AdminRefundTicketRequest { AlsoRemoveRsvp = false };

        // Act
        var response = await adminClient.PostAsJsonAsync(
            $"/api/admin/events/{eventId}/tickets/{userId}/refund",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "admin should be able to refund ticket with volunteer signup");

        var result = await response.Content.ReadFromJsonAsync<AdminRefundTicketResponse>();
        result.Should().NotBeNull();
        result!.TicketRefunded.Should().BeTrue("ticket should be refunded");
        result.VolunteerShiftsRemoved.Should().BeTrue("volunteer shifts should be cancelled");
        result.VolunteerShiftNames.Should().ContainSingle()
            .Which.Should().Be("Cleanup Crew", "volunteer position title should be included");

        // Verify database state
        await using var context = CreateDbContext();
        var volunteerSignup = await context.VolunteerSignups
            .FirstOrDefaultAsync(vs => vs.UserId == userId && vs.VolunteerPositionId == volunteerPositionId);

        volunteerSignup.Should().NotBeNull("volunteer signup record should still exist");
        volunteerSignup!.Status.Should().Be(VolunteerSignupStatus.Cancelled, "volunteer signup should be cancelled");
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task AdminRemoveRsvp_AsNonAdmin_Returns403()
    {
        // Arrange
        var (userClient, userId) = await CreateAuthenticatedUserAsync("regular-user@example.com");
        var (targetClient, targetUserId) = await CreateAuthenticatedUserAsync("target-user@example.com");

        var eventId = await CreateTestEventAsync();
        await CreateRsvpAsync(eventId, targetUserId);

        // Act - Regular user trying to remove another user's RSVP
        var response = await userClient.DeleteAsync(
            $"/api/admin/events/{eventId}/participations/{targetUserId}/remove");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "non-admin users should not be able to remove RSVPs");
    }

    [Fact]
    public async Task AdminRefundTicket_AsNonAdmin_Returns403()
    {
        // Arrange
        var (userClient, userId) = await CreateAuthenticatedUserAsync("regular-user2@example.com");
        var (targetClient, targetUserId) = await CreateAuthenticatedUserAsync("target-user2@example.com");

        var eventId = await CreateTestEventAsync();
        await CreateTicketAsync(eventId, targetUserId, 25.00m);

        var request = new AdminRefundTicketRequest { AlsoRemoveRsvp = false };

        // Act - Regular user trying to refund another user's ticket
        var response = await userClient.PostAsJsonAsync(
            $"/api/admin/events/{eventId}/tickets/{targetUserId}/refund",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "non-admin users should not be able to refund tickets");
    }

    #endregion

    #region Helper Methods

    private async Task<(HttpClient client, Guid userId)> CreateAuthenticatedAdminAsync(string email)
    {
        var client = _factory.CreateClient();
        var userId = Guid.NewGuid();

        // Create admin user in database
        await using var context = CreateDbContext();
        var adminUser = new WitchCityRope.Api.Models.ApplicationUser
        {
            Id = userId,
            Email = email,
            SceneName = $"Admin_{Guid.NewGuid():N}"[..15],
            FirstName = "Admin",
            LastName = "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        // Create JWT token with Administrator role
        var token = GenerateJwtToken(userId.ToString(), email, new[] { "Administrator" });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (client, userId);
    }

    private async Task<Guid> CreateTestEventAsync()
    {
        var eventId = Guid.NewGuid();
        await using var context = CreateDbContext();

        var evt = new Event
        {
            Id = eventId,
            Title = $"Test Event {Guid.NewGuid():N}"[..30],
            Description = "Test event for admin participation removal tests",
            EventType = EventType.SocialEvent,
            StartTime = DateTime.UtcNow.AddDays(7),
            EndTime = DateTime.UtcNow.AddDays(7).AddHours(3),
            Capacity = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Events.Add(evt);
        await context.SaveChangesAsync();

        return eventId;
    }

    private async Task CreateRsvpAsync(Guid eventId, Guid userId)
    {
        await using var context = CreateDbContext();

        var rsvp = new EventAttendance
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            AttendanceType = AttendanceType.RSVP,
            Status = AttendanceStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.EventAttendances.Add(rsvp);
        await context.SaveChangesAsync();
    }

    private async Task<Guid> CreateTicketAsync(Guid eventId, Guid userId, decimal amount)
    {
        await using var context = CreateDbContext();

        var ticketId = Guid.NewGuid();
        var ticket = new EventAttendance
        {
            Id = ticketId,
            EventId = eventId,
            UserId = userId,
            AttendanceType = AttendanceType.Ticket,
            Status = AttendanceStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.EventAttendances.Add(ticket);

        // Create associated payment
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            EventRegistrationId = ticketId,
            AmountValue = amount,
            Currency = "USD",
            Status = PaymentStatus.Completed,
            UserId = userId,
            PaymentMethodType = PaymentMethodType.NewCard,
            CreatedAt = DateTime.UtcNow
        };
        context.Payments.Add(payment);

        await context.SaveChangesAsync();
        return ticketId;
    }

    private async Task<Guid> CreateVolunteerPositionAsync(Guid eventId, string title)
    {
        await using var context = CreateDbContext();

        var positionId = Guid.NewGuid();
        var position = new VolunteerPosition
        {
            Id = positionId,
            EventId = eventId,
            Title = title,
            Description = $"Description for {title}",
            SlotsNeeded = 5,
            CreatedAt = DateTime.UtcNow
        };
        context.VolunteerPositions.Add(position);
        await context.SaveChangesAsync();

        return positionId;
    }

    private async Task CreateVolunteerSignupAsync(Guid userId, Guid volunteerPositionId)
    {
        await using var context = CreateDbContext();

        var signup = new VolunteerSignup
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            VolunteerPositionId = volunteerPositionId,
            Status = VolunteerSignupStatus.Confirmed,
            CreatedAt = DateTime.UtcNow
        };
        context.VolunteerSignups.Add(signup);
        await context.SaveChangesAsync();
    }

    private async Task CreateEventAttendeeAsync(Guid eventId, Guid userId)
    {
        await using var context = CreateDbContext();

        var attendee = new EventAttendee
        {
            EventId = eventId,
            UserId = userId,
            RegistrationStatus = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.EventAttendees.Add(attendee);
        await context.SaveChangesAsync();
    }

    #endregion

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;
}
