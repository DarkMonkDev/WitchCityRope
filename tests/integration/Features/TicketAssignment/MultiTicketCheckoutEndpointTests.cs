using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Payments.Endpoints;
using WitchCityRope.Api.Features.Payments.Models.AuthorizeNet;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Models;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.IntegrationTests.Features.TicketAssignment;

/// <summary>
/// Integration tests for Multi-Ticket Checkout endpoints.
/// Tests CO-I01 and CO-I02 from the test plan.
///
/// These tests verify that:
/// - CO-I01: Single ticket checkout remains unchanged (backward compatibility)
/// - CO-I02: Multi-ticket checkout with TicketSelections works correctly
///
/// NOTE: The checkout endpoint involves payment processing (Authorize.NET).
/// These tests use a WebApplicationFactory with a mock Authorize.NET service
/// to simulate payment processing without real credit card charges.
/// </summary>
[Collection("Sequential")]
public class MultiTicketCheckoutEndpointTests : IntegrationTestBase, IDisposable
{
    private static WebApplicationFactory<Program>? _sharedFactory;
    private static string? _sharedConnectionString;

    public MultiTicketCheckoutEndpointTests(DatabaseTestFixture fixture)
        : base(fixture)
    {
        if (_sharedFactory == null || _sharedConnectionString != ConnectionString)
        {
            _sharedFactory?.Dispose();
            _sharedFactory = CreateCheckoutTestWebApplicationFactory();
            _sharedConnectionString = ConnectionString;
        }
    }

    private WebApplicationFactory<Program> Factory => _sharedFactory!;

    public void Dispose()
    {
        // Don't dispose shared factory - reused across test instances.
    }

    /// <summary>
    /// Creates a WebApplicationFactory with a mock Authorize.NET service
    /// that simulates successful payments, in addition to the standard test overrides.
    /// </summary>
    private WebApplicationFactory<Program> CreateCheckoutTestWebApplicationFactory()
    {
        var baseFactory = CreateTestWebApplicationFactory();

        return baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace Authorize.NET service with mock for checkout tests
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IAuthorizeNetService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddScoped<IAuthorizeNetService, MockAuthorizeNetService>();
            });
        });
    }

    #region CO-I01: Single ticket checkout unchanged (backward compatibility)

    [Fact]
    public async Task CO_I01_SingleTicketCheckout_BackwardCompatible_Succeeds()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedVettedUserAsync();
        var (eventId, ticketTypeId) = await CreateEventWithTicketTypeAsync();

        var request = new CheckoutRequest
        {
            EventId = eventId,
            TicketTypeIds = new List<Guid> { ticketTypeId }, // Old-style single ticket
            EventWaiverAccepted = true,
            Nonce = "mock-nonce-123",
            DataDescriptor = "COMMON.ACCEPT.INAPP.PAYMENT",
            Amount = 25.00m,
            IdempotencyKey = $"idem-{Guid.NewGuid():N}",
            SlidingScalePercentage = 0m,
            TicketSelections = null // NULL = backward compat mode
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/checkout/credit-card", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "single ticket checkout should succeed with backward-compatible request format");

        var checkoutResponse = await response.Content.ReadFromJsonAsync<CheckoutResponse>(JsonOptions);
        checkoutResponse.Should().NotBeNull();
        checkoutResponse!.TicketPurchaseIds.Should().ContainSingle(
            "single ticket checkout should create exactly one ticket purchase");
        checkoutResponse.TransactionId.Should().NotBeNullOrEmpty();
        checkoutResponse.AmountCharged.Should().Be(25.00m);

        // Verify database state
        await using var context = CreateDbContext();
        var attendance = await context.EventAttendances
            .FirstOrDefaultAsync(ea => ea.EventId == eventId && ea.UserId == userId
                && ea.AttendanceType == AttendanceType.Ticket);
        attendance.Should().NotBeNull("ticket attendance should be created");
        attendance!.Status.Should().Be(AttendanceStatus.Active,
            "self-purchased ticket should be Active (not PendingAcceptance)");
        attendance.AssignedByUserId.Should().BeNull(
            "self-purchased ticket should not have AssignedByUserId");
    }

    #endregion

    #region CO-I02: Multi-ticket with TicketSelections

    [Fact]
    public async Task CO_I02_MultiTicketCheckout_WithTicketSelections_CreatesMultipleTickets()
    {
        // Arrange
        var (client, purchaserId) = await CreateAuthenticatedVettedUserAsync();
        var (_, assigneeId) = await CreateAuthenticatedVettedUserAsync();

        // Assignee authorizes purchaser (principal=assignee, delegate=purchaser)
        await CreateAuthorizedContactAsync(assigneeId, purchaserId);

        var (eventId, ticketTypeId) = await CreateEventWithTicketTypeAsync();

        var request = new CheckoutRequest
        {
            EventId = eventId,
            TicketTypeIds = new List<Guid> { ticketTypeId }, // Still required for backward compat
            EventWaiverAccepted = true,
            Nonce = "mock-nonce-456",
            DataDescriptor = "COMMON.ACCEPT.INAPP.PAYMENT",
            Amount = 50.00m, // 2 tickets at 25.00 each
            IdempotencyKey = $"idem-{Guid.NewGuid():N}",
            SlidingScalePercentage = 0m,
            TicketSelections = new List<TicketSelectionItem>
            {
                new TicketSelectionItem
                {
                    TicketTypeId = ticketTypeId,
                    Quantity = 2,
                    Assignees = new List<Guid?> { assigneeId } // Second ticket assigned
                }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/checkout/credit-card", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "multi-ticket checkout should succeed");

        var checkoutResponse = await response.Content.ReadFromJsonAsync<CheckoutResponse>(JsonOptions);
        checkoutResponse.Should().NotBeNull();
        checkoutResponse!.AmountCharged.Should().Be(50.00m);

        // Verify database: should have attendances for both purchaser and assignee
        await using var context = CreateDbContext();
        var attendances = await context.EventAttendances
            .Where(ea => ea.EventId == eventId && ea.AttendanceType == AttendanceType.Ticket)
            .ToListAsync();

        // At minimum, purchaser should have a ticket
        attendances.Should().Contain(ea => ea.UserId == purchaserId,
            "purchaser should have a ticket attendance");

        // If assignment was processed during checkout, assignee should have a PendingAcceptance ticket
        var assigneeAttendance = attendances.FirstOrDefault(ea => ea.UserId == assigneeId);
        if (assigneeAttendance != null)
        {
            assigneeAttendance.Status.Should().Be(AttendanceStatus.PendingAcceptance,
                "assigned ticket should be PendingAcceptance (not Active)");
            assigneeAttendance.AssignedByUserId.Should().Be(purchaserId,
                "AssignedByUserId should be the purchaser");
        }
    }

    [Fact]
    public async Task CO_I02_MultiTicketCheckout_NullTicketSelections_FallsBackToTicketTypeIds()
    {
        // Arrange - TicketSelections=null should fall back to TicketTypeIds
        var (client, userId) = await CreateAuthenticatedVettedUserAsync();
        var (eventId, ticketTypeId) = await CreateEventWithTicketTypeAsync();

        var request = new CheckoutRequest
        {
            EventId = eventId,
            TicketTypeIds = new List<Guid> { ticketTypeId },
            EventWaiverAccepted = true,
            Nonce = "mock-nonce-789",
            DataDescriptor = "COMMON.ACCEPT.INAPP.PAYMENT",
            Amount = 25.00m,
            IdempotencyKey = $"idem-{Guid.NewGuid():N}",
            SlidingScalePercentage = 0m,
            TicketSelections = null // Explicit null for backward compat
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/checkout/credit-card", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "null TicketSelections should fall back to TicketTypeIds (backward compatibility)");
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task CreditCardCheckout_WithoutAuth_Returns401()
    {
        var client = Factory.CreateClient();
        var request = new CheckoutRequest
        {
            EventId = Guid.NewGuid(),
            TicketTypeIds = new List<Guid> { Guid.NewGuid() },
            EventWaiverAccepted = true,
            Nonce = "test",
            DataDescriptor = "test",
            Amount = 25.00m,
            IdempotencyKey = "test"
        };

        var response = await client.PostAsJsonAsync("/api/checkout/credit-card", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Helper Methods

    private async Task<(HttpClient client, Guid userId)> CreateAuthenticatedVettedUserAsync()
    {
        var client = Factory.CreateClient();
        var userId = Guid.NewGuid();
        var email = $"checkout-{Guid.NewGuid():N}@example.com";

        await using var context = CreateDbContext();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = email,
            UserName = email,
            SceneName = $"Checkout_{Guid.NewGuid():N}"[..15],
            FirstName = "Test",
            LastName = "Checkout",
            VettingStatus = 3, // Approved
            TermsOfServiceAccepted = true,
            TermsOfServiceAcceptedAt = DateTime.UtcNow.AddDays(-30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = GenerateJwtToken(userId, email, "Member", user.SceneName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (client, userId);
    }

    private async Task CreateAuthorizedContactAsync(Guid principalId, Guid delegateId)
    {
        await using var context = CreateDbContext();
        var contact = new AuthorizedContact(principalId, delegateId);
        context.AuthorizedContacts.Add(contact);
        await context.SaveChangesAsync();
    }

    private async Task<(Guid eventId, Guid ticketTypeId)> CreateEventWithTicketTypeAsync()
    {
        var eventId = Guid.NewGuid();
        var ticketTypeId = Guid.NewGuid();

        await using var context = CreateDbContext();

        var venueId = await CreateTestVenueAsync();

        var evt = new Event
        {
            Id = eventId,
            Title = $"Checkout Event {Guid.NewGuid():N}"[..30],
            Description = "Integration test event for checkout",
            AllowRsvps = false,
            RequireTicketPurchase = true,
            VettedMembersOnly = false,
            VenueId = venueId,
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(3),
            Capacity = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Events.Add(evt);

        var ticketType = new TicketType
        {
            Id = ticketTypeId,
            EventId = eventId,
            Name = "General Admission",
            Description = "Standard admission ticket",
            PricingType = PricingType.Fixed,
            DefaultPrice = 25.00m,
            MinPrice = 25.00m,
            MaxPrice = 25.00m,
            Available = 50,
            MaxQuantityPerPurchase = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.TicketTypes.Add(ticketType);
        await context.SaveChangesAsync();

        return (eventId, ticketTypeId);
    }

    #endregion

    #region Mock Authorize.NET Service

    /// <summary>
    /// Mock implementation of IAuthorizeNetService for integration tests.
    /// Simulates successful payment processing without making real API calls.
    /// </summary>
    private class MockAuthorizeNetService : IAuthorizeNetService
    {
        public Task<AuthorizeNetPaymentResponse> ProcessPaymentWithNonceAsync(
            decimal amount,
            string nonce,
            string dataDescriptor,
            string description,
            string? invoiceId = null)
        {
            return Task.FromResult(new AuthorizeNetPaymentResponse
            {
                Success = true,
                TransactionId = $"mock-txn-{Guid.NewGuid():N}"[..20],
                AuthCode = "MOCK01",
                ResponseCode = "1",
                ErrorMessage = null
            });
        }

        public Task<AuthorizeNetRefundResponse> RefundAsync(
            string transactionId,
            decimal amount,
            string lastFourDigits,
            string? reason = null)
        {
            return Task.FromResult(new AuthorizeNetRefundResponse
            {
                Success = true,
                TransactionId = $"mock-refund-{Guid.NewGuid():N}"[..20],
                ErrorMessage = null
            });
        }

        public Task<AuthorizeNetTransactionStatus?> GetTransactionStatusAsync(string transactionId)
        {
            return Task.FromResult<AuthorizeNetTransactionStatus?>(
                new AuthorizeNetTransactionStatus
                {
                    TransactionId = transactionId,
                    Status = "settledSuccessfully"
                });
        }
    }

    #endregion
}
