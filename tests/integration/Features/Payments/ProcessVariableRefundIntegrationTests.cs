using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.Models.Requests;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Models;
using WitchCityRope.Models;
using WitchCityRope.Tests.Common.Fixtures;
using Hangfire;
using Hangfire.MemoryStorage;

namespace WitchCityRope.IntegrationTests.Features.Payments;

/// <summary>
/// Integration tests for variable refund endpoint (POST /api/payments/transactions/{id}/refund)
/// Tests complete end-to-end workflow with real database and HTTP requests
/// CRITICAL: Verifies that variable refunds do NOT cancel RSVP/ticket
/// </summary>
[Collection("Database")]
public class ProcessVariableRefundIntegrationTests : IntegrationTestBase, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Guid _adminUserId = Guid.NewGuid();
    private readonly string _adminEmail = $"admin-{Guid.NewGuid():N}@example.com";
    private readonly Guid _memberUserId = Guid.NewGuid();
    private readonly string _memberEmail = $"member-{Guid.NewGuid():N}@example.com";

    public ProcessVariableRefundIntegrationTests(DatabaseTestFixture fixture) : base(fixture)
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace DbContext with TestContainers connection string
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseNpgsql(ConnectionString);
                    });

                    // ✅ FIX HANGFIRE: Use in-memory storage for tests instead of PostgreSQL
                    // This prevents Hangfire from trying to connect to port 5432
                    services.AddHangfire(config =>
                    {
                        config.UseMemoryStorage();
                    });
                });
            });
    }

    public void Dispose()
    {
        _factory?.Dispose();
    }

    [Fact]
    public async Task ProcessVariableRefund_WithValidPartialRefund_ReturnsSuccess()
    {
        // Arrange
        await SeedTestUsers();
        var ticketPurchase = await CreateCompletedPayPalTicketPurchase(100.00m);
        var client = CreateAuthenticatedClient(_adminUserId, _adminEmail, "Administrator");

        var request = new VariableRefundRequest
        {
            RefundAmount = 50.00m,
            RefundReason = "Customer requested partial refund for one day of workshop"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/payments/transactions/{ticketPurchase.Id}/refund", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<VariableRefundResponse>();
        result.Should().NotBeNull();
        result!.Amount.Should().Be(50.00m);
        result.Status.Should().Be("Completed");
        result.PaymentStatus.Should().Be("PartiallyRefunded");
        result.RemainingRefundableAmount.Should().Be(50.00m);
        result.Message.Should().Contain("Partial refund");
        result.Message.Should().Contain("NOT cancelled");

        // Verify database state
        await using var context = CreateDbContext();
        var updated = await context.TicketPurchases.FindAsync(ticketPurchase.Id);
        updated!.PaymentStatus.Should().Be("PartiallyRefunded");
        updated.Notes.Should().Contain("[REFUND");

        var refunds = await context.PaymentRefunds
            .Where(r => r.TicketPurchaseId == ticketPurchase.Id)
            .ToListAsync();
        refunds.Should().ContainSingle();
        refunds[0].RefundAmountValue.Should().Be(50.00m);
    }

    [Fact]
    public async Task ProcessVariableRefund_WithValidFullRefund_ReturnsSuccess()
    {
        // Arrange
        await SeedTestUsers();
        var ticketPurchase = await CreateCompletedPayPalTicketPurchase(100.00m);
        var client = CreateAuthenticatedClient(_adminUserId, _adminEmail, "Administrator");

        var request = new VariableRefundRequest
        {
            RefundAmount = 100.00m,
            RefundReason = "Customer requested full refund for cancelled workshop"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/payments/transactions/{ticketPurchase.Id}/refund", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<VariableRefundResponse>();
        result.Should().NotBeNull();
        result!.Amount.Should().Be(100.00m);
        result.PaymentStatus.Should().Be("Refunded"); // Full refund
        result.RemainingRefundableAmount.Should().Be(0.00m);
        result.Message.Should().Contain("Full refund");
    }

    [Fact]
    public async Task ProcessVariableRefund_WithMultiplePartials_AccumulatesCorrectly()
    {
        // Arrange
        await SeedTestUsers();
        var ticketPurchase = await CreateCompletedPayPalTicketPurchase(100.00m);
        var client = CreateAuthenticatedClient(_adminUserId, _adminEmail, "Administrator");

        // First partial refund: $25
        var request1 = new VariableRefundRequest
        {
            RefundAmount = 25.00m,
            RefundReason = "First partial refund for workshop session 1"
        };
        await client.PostAsJsonAsync($"/api/payments/transactions/{ticketPurchase.Id}/refund", request1);

        // Second partial refund: $25
        var request2 = new VariableRefundRequest
        {
            RefundAmount = 25.00m,
            RefundReason = "Second partial refund for workshop session 2"
        };
        var response2 = await client.PostAsJsonAsync($"/api/payments/transactions/{ticketPurchase.Id}/refund", request2);

        // Assert
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        var result2 = await response2.Content.ReadFromJsonAsync<VariableRefundResponse>();
        result2!.PaymentStatus.Should().Be("PartiallyRefunded"); // Still partial
        result2.RemainingRefundableAmount.Should().Be(50.00m); // 100 - 50 = 50 remaining

        // Third partial refund: $50 (total = 100, should be "Refunded")
        var request3 = new VariableRefundRequest
        {
            RefundAmount = 50.00m,
            RefundReason = "Third partial refund for remaining sessions"
        };
        var response3 = await client.PostAsJsonAsync($"/api/payments/transactions/{ticketPurchase.Id}/refund", request3);

        // Assert
        response3.StatusCode.Should().Be(HttpStatusCode.OK);
        var result3 = await response3.Content.ReadFromJsonAsync<VariableRefundResponse>();
        result3!.PaymentStatus.Should().Be("Refunded"); // Now fully refunded
        result3.RemainingRefundableAmount.Should().Be(0.00m);

        // Verify database has 3 refund records
        await using var context = CreateDbContext();
        var refunds = await context.PaymentRefunds
            .Where(r => r.TicketPurchaseId == ticketPurchase.Id)
            .ToListAsync();
        refunds.Should().HaveCount(3);
        refunds.Sum(r => r.RefundAmountValue).Should().Be(100.00m);
    }

    [Fact]
    public async Task ProcessVariableRefund_DoesNotCancelRSVP()
    {
        // Arrange
        await SeedTestUsers();
        var ticketPurchase = await CreateCompletedPayPalTicketPurchaseWithRSVP(100.00m);
        var client = CreateAuthenticatedClient(_adminUserId, _adminEmail, "Administrator");

        var request = new VariableRefundRequest
        {
            RefundAmount = 100.00m,
            RefundReason = "Full refund but RSVP should remain active"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/payments/transactions/{ticketPurchase.Id}/refund", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // CRITICAL: Verify RSVP is NOT cancelled
        await using var context = CreateDbContext();
        var rsvp = await context.EventAttendances
            .FirstOrDefaultAsync(ea => ea.TicketPurchaseId == ticketPurchase.Id);

        rsvp.Should().NotBeNull("RSVP should still exist after variable refund");
        rsvp!.Status.Should().Be(AttendanceStatus.Active, "RSVP should still be active");
        rsvp.AttendanceType.Should().Be(AttendanceType.Ticket);
    }

    [Fact]
    public async Task ProcessVariableRefund_WithAmountExceedingRemaining_Returns400()
    {
        // Arrange
        await SeedTestUsers();
        var ticketPurchase = await CreateCompletedPayPalTicketPurchase(100.00m);
        var client = CreateAuthenticatedClient(_adminUserId, _adminEmail, "Administrator");

        // First refund $60
        var request1 = new VariableRefundRequest
        {
            RefundAmount = 60.00m,
            RefundReason = "First partial refund of $60"
        };
        await client.PostAsJsonAsync($"/api/payments/transactions/{ticketPurchase.Id}/refund", request1);

        // Try to refund $50 (total would be $110)
        var request2 = new VariableRefundRequest
        {
            RefundAmount = 50.00m,
            RefundReason = "Trying to refund more than remaining"
        };

        // Act
        var response2 = await client.PostAsJsonAsync($"/api/payments/transactions/{ticketPurchase.Id}/refund", request2);

        // Assert
        response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorContent = await response2.Content.ReadAsStringAsync();
        errorContent.Should().Contain("exceeds remaining refundable amount");
        errorContent.Should().Contain("$40.00"); // Remaining should be 100 - 60 = 40
    }

    [Fact]
    public async Task ProcessVariableRefund_WithZeroAmount_Returns400()
    {
        // Arrange
        await SeedTestUsers();
        var ticketPurchase = await CreateCompletedPayPalTicketPurchase(100.00m);
        var client = CreateAuthenticatedClient(_adminUserId, _adminEmail, "Administrator");

        var request = new VariableRefundRequest
        {
            RefundAmount = 0.00m,
            RefundReason = "Invalid zero amount refund"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/payments/transactions/{ticketPurchase.Id}/refund", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("must be greater than 0");
    }

    [Fact]
    public async Task ProcessVariableRefund_WithNonPayPalPayment_ProcessesAsManualRefund()
    {
        // Arrange
        await SeedTestUsers();
        var ticketPurchase = await CreateCompletedTicketPurchase(100.00m, "Cash");
        var client = CreateAuthenticatedClient(_adminUserId, _adminEmail, "Administrator");

        var request = new VariableRefundRequest
        {
            RefundAmount = 50.00m,
            RefundReason = "Manual refund for cash payment (admin must process externally)"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/payments/transactions/{ticketPurchase.Id}/refund", request);

        // Assert - Should succeed with manual refund
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VariableRefundResponse>();
        result.Should().NotBeNull();
        result!.Amount.Should().Be(50.00m);
        result.Status.Should().Be("Completed");
        result.PaymentStatus.Should().Be("PartiallyRefunded");
        result.Message.Should().Contain("NOT cancelled");
    }

    [Fact]
    public async Task ProcessVariableRefund_WithMemberRole_Returns403()
    {
        // Arrange
        await SeedTestUsers();
        var ticketPurchase = await CreateCompletedPayPalTicketPurchase(100.00m);
        var client = CreateAuthenticatedClient(_memberUserId, _memberEmail, "Member");

        var request = new VariableRefundRequest
        {
            RefundAmount = 50.00m,
            RefundReason = "Member attempting to process refund"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/payments/transactions/{ticketPurchase.Id}/refund", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #region Helper Methods

    private async Task SeedTestUsers()
    {
        // Generate unique identifier for this test run
        var testRunId = Guid.NewGuid().ToString().Substring(0, 8);

        await using var context = CreateDbContext();

        var admin = new ApplicationUser
        {
            Id = _adminUserId,
            Email = _adminEmail,
            SceneName = $"AdminUser-{testRunId}",  // Unique per test run
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            Role = "Administrator",
            EmailVerificationToken = Guid.NewGuid().ToString()
        };

        var member = new ApplicationUser
        {
            Id = _memberUserId,
            Email = _memberEmail,
            SceneName = $"MemberUser-{testRunId}",  // Unique per test run
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            Role = "Member",
            EmailVerificationToken = Guid.NewGuid().ToString()
        };

        context.Users.AddRange(admin, member);
        await context.SaveChangesAsync();
    }

    private async Task<TicketPurchase> CreateCompletedPayPalTicketPurchase(decimal amount)
    {
        return await CreateCompletedTicketPurchase(amount, "PayPal");
    }

    private async Task<TicketPurchase> CreateCompletedTicketPurchase(decimal amount, string paymentMethod)
    {
        await using var context = CreateDbContext();

        // ✅ FIRST: Create Venue (required FK for Event)
        var venue = new Venue
        {
            Name = $"Test Venue {Guid.NewGuid():N}",  // Unique name
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Venues.Add(venue);
        await context.SaveChangesAsync();

        // ✅ SECOND: Create Event entity (requires VenueId FK)
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Test Event for Refund",
            Description = "Integration test event",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            VenueId = venue.Id,  // ✅ Reference actual Venue
            EventType = EventType.Class,
            Capacity = 20,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();

        // ✅ THEN: Create TicketType referencing the real Event
        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = eventEntity.Id,  // ✅ Reference actual Event
            Name = "General Admission",
            Description = "Test ticket for refund testing",
            PricingType = PricingType.Fixed,
            Price = amount,
            Available = 100
        };

        var ticketPurchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            UserId = _memberUserId,
            TicketTypeId = ticketType.Id,
            Quantity = 1,
            TotalPrice = amount,
            PaymentMethod = paymentMethod,
            PaymentStatus = "Completed",  // IsPaymentCompleted is computed from this
            PurchaseDate = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow,  // Payment was processed
            EncryptedPayPalCaptureId = paymentMethod == "PayPal" ? "encrypted-capture-id" : null,
            TicketType = ticketType,
            Notes = "Initial purchase"
        };

        context.TicketTypes.Add(ticketType);
        context.TicketPurchases.Add(ticketPurchase);
        await context.SaveChangesAsync();

        return ticketPurchase;
    }

    private async Task<TicketPurchase> CreateCompletedPayPalTicketPurchaseWithRSVP(decimal amount)
    {
        var ticketPurchase = await CreateCompletedPayPalTicketPurchase(amount);

        await using var context = CreateDbContext();

        // Use existing seeded event
        var testEvent = await context.Events.FirstAsync();

        // Create RSVP/EventAttendance record
        var attendance = new EventAttendance
        {
            Id = Guid.NewGuid(),
            EventId = testEvent.Id,
            UserId = _memberUserId,
            TicketPurchaseId = ticketPurchase.Id,
            Status = AttendanceStatus.Active,
            AttendanceType = AttendanceType.Ticket,
            CreatedAt = DateTime.UtcNow
        };

        context.EventAttendances.Add(attendance);
        await context.SaveChangesAsync();

        return ticketPurchase;
    }

    private HttpClient CreateAuthenticatedClient(Guid userId, string email, string role)
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(userId, email, role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private class VariableRefundResponse
    {
        public Guid RefundId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public decimal RemainingRefundableAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
    }

    #endregion
}
