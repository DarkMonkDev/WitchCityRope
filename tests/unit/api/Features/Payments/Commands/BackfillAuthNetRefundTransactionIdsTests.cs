using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WitchCityRope.Api.Features.Logging.Entities;
using WitchCityRope.Api.Features.Payments.Commands;
using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.Models;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.Fixtures;
using Xunit;
using FluentAssertions;

namespace WitchCityRope.UnitTests.Api.Features.Payments.Commands;

/// <summary>
/// Tests for the one-off BackfillAuthNetRefundTransactionIds command.
///
/// The command reads logging.application_logs rows whose MessageTemplate matches the
/// "Authorize.net refund completed" Serilog template, parses TicketId + TransactionId
/// out of the structured Properties JSON, finds PaymentRefund rows whose
/// EncryptedAuthNetRefundTransactionId is still NULL, encrypts the id and stores it.
///
/// RunAsync is private, so these tests drive the public Execute entry point and
/// inspect the returned IResult (an Ok&lt;BackfillReport&gt; on success).
///
/// Uses DatabaseTestFixture with real PostgreSQL via TestContainers.
/// </summary>
[Collection("Database")]
public class BackfillAuthNetRefundTransactionIdsTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private WitchCityRope.Api.Data.ApplicationDbContext _context = null!;
    private IEncryptionService _mockEncryptionService = null!;
    private ILogger<BackfillAuthNetRefundTransactionIds.BackfillReport> _logger = null!;

    /// <summary>
    /// Exact template emitted by RefundService — must match the command's constant.
    /// </summary>
    private const string AuthNetRefundCompletedTemplate =
        "Authorize.net refund completed for ticket {TicketId}, refund transaction: {TransactionId}";

    public BackfillAuthNetRefundTransactionIdsTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _context = _fixture.CreateDbContext();

        _logger = Substitute.For<ILogger<BackfillAuthNetRefundTransactionIds.BackfillReport>>();
        _mockEncryptionService = Substitute.For<IEncryptionService>();
        _mockEncryptionService.EncryptAsync(Arg.Any<string>())
            .Returns(args => Task.FromResult($"enc:{(string)args[0]}"));
        _mockEncryptionService.DecryptAsync(Arg.Any<string>())
            .Returns(args =>
            {
                var s = (string)args[0];
                return Task.FromResult(s.StartsWith("enc:") ? s.Substring(4) : s);
            });
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        await _fixture.ResetDatabaseAsync();
    }

    [Fact]
    public async Task Execute_BackfillsNullTransactionId_ThenIsIdempotentOnSecondRun()
    {
        // Arrange: seed a TicketPurchase, a matching PaymentRefund with NULL
        // EncryptedAuthNetRefundTransactionId, and an application_logs row whose
        // Properties JSON contains that ticket's id and a transaction id.
        var adminUserId = await SeedUserAsync();
        var ticketPurchaseId = await SeedTicketPurchaseAsync();
        var refundId = await SeedRefundAsync(
            ticketPurchaseId, adminUserId, encryptedAuthNetRefundTransactionId: null);

        const string transactionId = "AUTHNET-REFUND-HISTORICAL-001";
        await SeedRefundCompletedLogAsync(ticketPurchaseId, transactionId);

        // Act 1 — first run should backfill the row
        var firstReport = await ExecuteAndGetReportAsync();

        // Assert 1
        firstReport.LogEntriesFound.Should().Be(1);
        firstReport.RefundRowsUpdated.Should().Be(1);
        firstReport.RefundRowsAlreadyPopulated.Should().Be(0);
        firstReport.Unmatched.Should().BeEmpty();

        await using (var verify1 = _fixture.CreateDbContext())
        {
            var refund = await verify1.PaymentRefunds.FirstAsync(r => r.Id == refundId);
            refund.EncryptedAuthNetRefundTransactionId.Should().Be($"enc:{transactionId}",
                "the backfill must encrypt and persist the recovered transaction id");
        }

        // Act 2 — second run must be idempotent (only fills NULLs)
        var secondReport = await ExecuteAndGetReportAsync();

        // Assert 2
        secondReport.LogEntriesFound.Should().Be(1, "the log row is still present");
        secondReport.RefundRowsUpdated.Should().Be(0,
            "the refund row is already populated, so nothing is updated on a re-run");
        secondReport.RefundRowsAlreadyPopulated.Should().BeGreaterThanOrEqualTo(1,
            "the already-backfilled row is counted as already populated");
        secondReport.Unmatched.Should().BeEmpty();

        await using (var verify2 = _fixture.CreateDbContext())
        {
            var refund = await verify2.PaymentRefunds.FirstAsync(r => r.Id == refundId);
            refund.EncryptedAuthNetRefundTransactionId.Should().Be($"enc:{transactionId}",
                "the second run must not overwrite the already-stored value");
        }
    }

    #region Helpers

    /// <summary>
    /// Runs Execute and unwraps the Ok&lt;BackfillReport&gt; result.
    /// RunAsync is private; the public Execute returns Results.Ok(report) on success.
    /// </summary>
    private async Task<BackfillAuthNetRefundTransactionIds.BackfillReport> ExecuteAndGetReportAsync()
    {
        var result = await BackfillAuthNetRefundTransactionIds.Execute(
            _context, _mockEncryptionService, _logger, CancellationToken.None);

        var ok = result.Should().BeOfType<Ok<BackfillAuthNetRefundTransactionIds.BackfillReport>>(
            "a successful backfill returns Results.Ok(report)").Subject;

        ok.Value.Should().NotBeNull();
        return ok.Value!;
    }

    private async Task<Guid> SeedUserAsync()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = $"backfill-{Guid.NewGuid()}@example.com",
            SceneName = $"User{Guid.NewGuid().ToString().Substring(0, 8)}",
            EncryptedLegalName = "Encrypted Legal Name",
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            Role = "",
            PronouncedName = "Test User",
            Pronouns = "they/them",
            EmailVerificationToken = Guid.NewGuid().ToString()
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user.Id;
    }

    private async Task<Guid> SeedTicketPurchaseAsync()
    {
        var purchaserId = await SeedUserAsync();

        var venue = new Venue
        {
            Name = $"Venue-{Guid.NewGuid():N}"[..30],
            Directions = "Test",
            VenueInformation = "Test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Venues.Add(venue);
        await _context.SaveChangesAsync();

        var testEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Backfill Test Event",
            Description = "Test",
            VenueId = venue.Id,
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Capacity = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Events.Add(testEvent);

        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = testEvent.Id,
            Name = "Test Ticket",
            Price = 100.00m,
            Available = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TicketTypes.Add(ticketType);

        var ticketPurchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            TicketTypeId = ticketType.Id,
            UserId = purchaserId,
            TotalPrice = 100.00m,
            PaymentStatus = TicketPurchasePaymentStatus.Refunded,
            PaymentMethod = "CreditCard",
            ProcessedAt = DateTime.UtcNow,
            EncryptedAuthNetTransactionId = "encrypted-authnet-transaction-id"
        };
        _context.TicketPurchases.Add(ticketPurchase);
        await _context.SaveChangesAsync();

        return ticketPurchase.Id;
    }

    private async Task<Guid> SeedRefundAsync(
        Guid ticketPurchaseId, Guid processedByUserId, string? encryptedAuthNetRefundTransactionId)
    {
        var refund = new PaymentRefund
        {
            Id = Guid.NewGuid(),
            TicketPurchaseId = ticketPurchaseId,
            RefundAmountValue = 100.00m,
            RefundCurrency = "USD",
            RefundStatus = RefundStatus.Completed,
            ProcessedByUserId = processedByUserId,
            RefundReason = "Historical Authorize.net refund (pre-backfill)",
            EncryptedAuthNetRefundTransactionId = encryptedAuthNetRefundTransactionId
        };
        _context.PaymentRefunds.Add(refund);
        await _context.SaveChangesAsync();
        return refund.Id;
    }

    /// <summary>
    /// Inserts an application_logs row mimicking what Serilog's PostgreSQL sink writes
    /// for the "Authorize.net refund completed" message. Properties is JSONB holding
    /// the structured TicketId / TransactionId tokens.
    /// </summary>
    private async Task SeedRefundCompletedLogAsync(Guid ticketPurchaseId, string transactionId)
    {
        var properties = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["TicketId"] = ticketPurchaseId.ToString(),
            ["TransactionId"] = transactionId
        });

        var log = new ApplicationLog
        {
            // Id is an identity-always column — left unset so PostgreSQL generates it.
            Timestamp = DateTime.UtcNow,
            Level = 2,
            LevelName = "Information",
            Message = $"Authorize.net refund completed for ticket {ticketPurchaseId}, "
                      + $"refund transaction: {transactionId}",
            MessageTemplate = AuthNetRefundCompletedTemplate,
            Properties = properties
        };
        _context.ApplicationLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    #endregion
}
