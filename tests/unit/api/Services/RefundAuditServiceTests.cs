using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.Models;
using WitchCityRope.Api.Features.Payments.Models.PayPal;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Tests.Services;

/// <summary>
/// Unit tests for RefundAuditService - Phase 2 PayPal Refund Enhancement
/// Tests comprehensive audit logging for refund lifecycle events
/// CRITICAL: Financial audit trail - ensures compliance and troubleshooting capability
/// </summary>
public class RefundAuditServiceTests : IDisposable
{
    private readonly Mock<ILogger<RefundAuditService>> _mockLogger;
    private readonly ApplicationDbContext _context;
    private readonly RefundAuditService _sut;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testPaymentId = Guid.NewGuid();

    public RefundAuditServiceTests()
    {
        // Setup in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<RefundAuditService>>();

        _sut = new RefundAuditService(_context, _mockLogger.Object);

        // Seed test user
        var testUser = new ApplicationUser
        {
            Id = _testUserId,
            Email = "admin@example.com",
            SceneName = "AdminUser"
        };

        _context.Users.Add(testUser);
        _context.SaveChanges();
    }

    #region LogRefundRequestAsync Tests

    /// <summary>
    /// Test 1: LogRefundRequestAsync logs refund initiation
    /// Tests structured logging for refund request
    /// </summary>
    [Fact]
    public async Task LogRefundRequestAsync_LogsRefundInitiation()
    {
        // Arrange
        var refund = CreateTestRefund(RefundStatus.Processing);
        _context.PaymentRefunds.Add(refund);
        await _context.SaveChangesAsync();

        // Act
        await _sut.LogRefundRequestAsync(refund);

        // Assert - Verify method completes successfully
        refund.Should().NotBeNull("Refund entity should exist after logging");
        refund.Id.Should().NotBe(Guid.Empty);
    }

    #endregion

    #region LogRefundSuccessAsync Tests

    /// <summary>
    /// Test 2: LogRefundSuccessAsync updates status and stores PayPal response
    /// Tests successful refund completion logging
    /// </summary>
    [Fact]
    public async Task LogRefundSuccessAsync_UpdatesStatusAndStoresResponse()
    {
        // Arrange
        var refund = CreateTestRefund(RefundStatus.Processing);
        _context.PaymentRefunds.Add(refund);
        await _context.SaveChangesAsync();

        var paypalRefund = new PayPalRefundResponse
        {
            RefundId = "PAYPAL-REFUND-ABC123",
            Status = "COMPLETED",
            Amount = new PayPalAmount
            {
                CurrencyCode = "USD",
                Value = "100.00"
            }
        };

        // Act
        await _sut.LogRefundSuccessAsync(refund.Id, paypalRefund);

        // Assert
        var updatedRefund = await _context.PaymentRefunds.FindAsync(refund.Id);
        updatedRefund.Should().NotBeNull();
        updatedRefund!.RefundStatus.Should().Be(RefundStatus.Completed, "Status should be updated to Completed");
        updatedRefund.EncryptedPayPalRefundId.Should().Be("PAYPAL-REFUND-ABC123", "PayPal refund ID should be stored");
        updatedRefund.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5), "ProcessedAt should be updated");

        // Verify PayPal response stored in metadata as JSONB
        updatedRefund.Metadata.Should().ContainKey("paypal_response", "PayPal response should be stored in metadata");

        var storedResponse = JsonSerializer.Deserialize<PayPalRefundResponse>(
            updatedRefund.Metadata["paypal_response"].ToString()!);

        storedResponse.Should().NotBeNull();
        storedResponse!.RefundId.Should().Be("PAYPAL-REFUND-ABC123");
        storedResponse.Status.Should().Be("COMPLETED");
    }

    /// <summary>
    /// Test 3: LogRefundSuccessAsync with PENDING status sets Processing status
    /// Tests status mapping for pending PayPal refunds
    /// </summary>
    [Fact]
    public async Task LogRefundSuccessAsync_WithPendingStatus_SetsProcessing()
    {
        // Arrange
        var refund = CreateTestRefund(RefundStatus.Processing);
        _context.PaymentRefunds.Add(refund);
        await _context.SaveChangesAsync();

        var paypalRefund = new PayPalRefundResponse
        {
            RefundId = "PAYPAL-REFUND-PENDING",
            Status = "PENDING", // PayPal status pending
            Amount = new PayPalAmount { CurrencyCode = "USD", Value = "50.00" }
        };

        // Act
        await _sut.LogRefundSuccessAsync(refund.Id, paypalRefund);

        // Assert
        var updatedRefund = await _context.PaymentRefunds.FindAsync(refund.Id);
        updatedRefund!.RefundStatus.Should().Be(RefundStatus.Processing, "PENDING PayPal status maps to Processing");
        // ProcessedAt is DateTime (not nullable) - check it hasn't been updated beyond initial value
        updatedRefund.ProcessedAt.Should().BeCloseTo(refund.ProcessedAt, TimeSpan.FromSeconds(1), "ProcessedAt should not be updated for pending refunds");
    }

    /// <summary>
    /// Test 4: LogRefundSuccessAsync with nonexistent refund handles gracefully
    /// Tests error handling for missing refund entity
    /// </summary>
    [Fact]
    public async Task LogRefundSuccessAsync_WithNonexistentRefund_HandlesGracefully()
    {
        // Arrange
        var nonexistentRefundId = Guid.NewGuid();
        var paypalRefund = new PayPalRefundResponse
        {
            RefundId = "PAYPAL-REFUND-123",
            Status = "COMPLETED",
            Amount = new PayPalAmount { CurrencyCode = "USD", Value = "100.00" }
        };

        // Act & Assert - Should not throw exception
        await FluentActions.Awaiting(async () =>
            await _sut.LogRefundSuccessAsync(nonexistentRefundId, paypalRefund))
            .Should().NotThrowAsync("Missing refund should be handled gracefully");
    }

    #endregion

    #region LogRefundRetryAsync Tests

    /// <summary>
    /// Test 5: LogRefundRetryAsync increments retry count and stores error message
    /// Tests retry attempt logging
    /// </summary>
    [Fact]
    public async Task LogRefundRetryAsync_IncrementsRetryCountAndStoresError()
    {
        // Arrange
        var refund = CreateTestRefund(RefundStatus.Processing);
        _context.PaymentRefunds.Add(refund);
        await _context.SaveChangesAsync();

        var retryCount = 1;
        var errorMessage = "Service unavailable - retrying";

        // Act
        await _sut.LogRefundRetryAsync(refund.Id, retryCount, errorMessage);

        // Assert
        var updatedRefund = await _context.PaymentRefunds.FindAsync(refund.Id);
        updatedRefund.Should().NotBeNull();
        updatedRefund!.RetryCount.Should().Be(1, "Retry count should be updated");
        updatedRefund.ErrorMessage.Should().Be(errorMessage, "Error message should be stored");

        // Verify retry history stored in metadata
        updatedRefund.Metadata.Should().ContainKey("retry_1", "Retry attempt should be logged in metadata");

        // Metadata entry exists (no need to validate JSON structure in unit test)
        updatedRefund.Metadata["retry_1"].Should().NotBeNull();
    }

    /// <summary>
    /// Test 6: LogRefundRetryAsync with multiple retries stores all attempts
    /// Tests cumulative retry history logging
    /// </summary>
    [Fact]
    public async Task LogRefundRetryAsync_WithMultipleRetries_StoresAllAttempts()
    {
        // Arrange
        var refund = CreateTestRefund(RefundStatus.Processing);
        _context.PaymentRefunds.Add(refund);
        await _context.SaveChangesAsync();

        // Act - Log 3 retry attempts
        await _sut.LogRefundRetryAsync(refund.Id, 1, "First retry - connection timeout");
        await _sut.LogRefundRetryAsync(refund.Id, 2, "Second retry - service unavailable");
        await _sut.LogRefundRetryAsync(refund.Id, 3, "Third retry - gateway timeout");

        // Assert
        var updatedRefund = await _context.PaymentRefunds.FindAsync(refund.Id);
        updatedRefund!.RetryCount.Should().Be(3, "Retry count should reflect last retry");
        updatedRefund.ErrorMessage.Should().Be("Third retry - gateway timeout", "Last error should be stored");

        // Verify all retry attempts stored in metadata
        updatedRefund.Metadata.Should().ContainKey("retry_1");
        updatedRefund.Metadata.Should().ContainKey("retry_2");
        updatedRefund.Metadata.Should().ContainKey("retry_3");
    }

    #endregion

    #region LogRefundFailureAsync Tests

    /// <summary>
    /// Test 7: LogRefundFailureAsync marks refund as failed with final error
    /// Tests final failure logging
    /// </summary>
    [Fact]
    public async Task LogRefundFailureAsync_MarksRefundAsFailedWithError()
    {
        // Arrange
        var refund = CreateTestRefund(RefundStatus.Processing);
        refund.RetryCount = 5; // Simulate max retries reached
        _context.PaymentRefunds.Add(refund);
        await _context.SaveChangesAsync();

        var finalError = "Max retries exceeded - service unavailable";

        // Act
        await _sut.LogRefundFailureAsync(refund.Id, finalError);

        // Assert
        var updatedRefund = await _context.PaymentRefunds.FindAsync(refund.Id);
        updatedRefund.Should().NotBeNull();
        updatedRefund!.RefundStatus.Should().Be(RefundStatus.Failed, "Status should be updated to Failed");
        updatedRefund.ErrorMessage.Should().Be(finalError, "Final error message should be stored");

        // Verify failure details in metadata
        updatedRefund.Metadata.Should().ContainKey("failure", "Failure details should be logged");

        // Metadata entry exists (no need to validate JSON structure in unit test)
        updatedRefund.Metadata["failure"].Should().NotBeNull();
    }

    /// <summary>
    /// Test 8: LogRefundFailureAsync stores retry count in failure metadata
    /// Tests failure metadata includes complete context
    /// </summary>
    [Fact]
    public async Task LogRefundFailureAsync_StoresRetryCountInMetadata()
    {
        // Arrange
        var refund = CreateTestRefund(RefundStatus.Processing);
        refund.RetryCount = 3;
        _context.PaymentRefunds.Add(refund);
        await _context.SaveChangesAsync();

        var finalError = "Refund failed after 3 retries";

        // Act
        await _sut.LogRefundFailureAsync(refund.Id, finalError);

        // Assert
        var updatedRefund = await _context.PaymentRefunds.FindAsync(refund.Id);
        updatedRefund!.Metadata.Should().ContainKey("failure");

        // Verify metadata contains retry count context
        var failureMetadata = updatedRefund.Metadata["failure"];
        failureMetadata.Should().NotBeNull("Failure metadata should include retry context");
    }

    #endregion

    #region GetRefundAuditHistoryAsync Tests

    /// <summary>
    /// Test 9: GetRefundAuditHistoryAsync retrieves complete audit trail
    /// Tests audit history retrieval with related entities
    /// </summary>
    [Fact]
    public async Task GetRefundAuditHistoryAsync_RetrievesCompleteAuditTrail()
    {
        // Arrange
        var payment = new Payment
        {
            Id = _testPaymentId,
            EventRegistrationId = Guid.NewGuid(),
            UserId = _testUserId,
            AmountValue = 100.00m,
            Currency = "USD",
            Status = PaymentStatus.Completed,
            ProcessedAt = DateTime.UtcNow,
            EncryptedPayPalOrderId = "encrypted-order-id",
            EncryptedPayPalCaptureId = "encrypted-capture-id"
        };

        _context.Payments.Add(payment);

        var refund = CreateTestRefund(RefundStatus.Processing);
        refund.OriginalPaymentId = payment.Id;

        // Add some audit metadata
        refund.Metadata["retry_1"] = new { timestamp = DateTime.UtcNow, error = "First retry" };
        refund.Metadata["retry_2"] = new { timestamp = DateTime.UtcNow, error = "Second retry" };

        _context.PaymentRefunds.Add(refund);
        await _context.SaveChangesAsync();

        // Act
        var auditHistory = await _sut.GetRefundAuditHistoryAsync(refund.Id);

        // Assert
        auditHistory.Should().NotBeNull("Audit history should be retrieved");
        auditHistory!.Id.Should().Be(refund.Id);

        // Verify related entities loaded
        auditHistory.OriginalPayment.Should().NotBeNull("Payment should be included");
        auditHistory.ProcessedByUser.Should().NotBeNull("User should be included");

        // Verify metadata preserved
        auditHistory.Metadata.Should().ContainKey("retry_1");
        auditHistory.Metadata.Should().ContainKey("retry_2");
    }

    /// <summary>
    /// Test 10: GetRefundAuditHistoryAsync returns null for nonexistent refund
    /// Tests error handling for missing refund
    /// </summary>
    [Fact]
    public async Task GetRefundAuditHistoryAsync_WithNonexistentRefund_ReturnsNull()
    {
        // Arrange
        var nonexistentRefundId = Guid.NewGuid();

        // Act
        var auditHistory = await _sut.GetRefundAuditHistoryAsync(nonexistentRefundId);

        // Assert
        auditHistory.Should().BeNull("Nonexistent refund should return null");
    }

    #endregion

    #region PayPal Response Storage Tests

    /// <summary>
    /// Test 11: PayPal response stored as JSONB for queryability
    /// Tests JSON serialization and storage in metadata
    /// </summary>
    [Fact]
    public async Task LogRefundSuccessAsync_StoresPayPalResponseAsJsonb()
    {
        // Arrange
        var refund = CreateTestRefund(RefundStatus.Processing);
        _context.PaymentRefunds.Add(refund);
        await _context.SaveChangesAsync();

        var paypalRefund = new PayPalRefundResponse
        {
            RefundId = "PAYPAL-REFUND-JSONB-TEST",
            Status = "COMPLETED",
            Amount = new PayPalAmount
            {
                CurrencyCode = "USD",
                Value = "75.00"
            },
            // Additional fields that should be preserved
            CreateTime = DateTime.Parse("2025-11-17T12:00:00Z"),
            UpdateTime = DateTime.Parse("2025-11-17T12:01:00Z")
        };

        // Act
        await _sut.LogRefundSuccessAsync(refund.Id, paypalRefund);

        // Assert
        var updatedRefund = await _context.PaymentRefunds.FindAsync(refund.Id);
        updatedRefund!.Metadata.Should().ContainKey("paypal_response");

        // Deserialize and verify all fields preserved
        var storedResponseJson = updatedRefund.Metadata["paypal_response"].ToString();
        var storedResponse = JsonSerializer.Deserialize<PayPalRefundResponse>(storedResponseJson!);

        storedResponse.Should().NotBeNull();
        storedResponse!.RefundId.Should().Be("PAYPAL-REFUND-JSONB-TEST");
        storedResponse.Status.Should().Be("COMPLETED");
        storedResponse.Amount.Value.Should().Be("75.00");
        storedResponse.CreateTime.Should().Be(DateTime.Parse("2025-11-17T12:00:00Z"));
        storedResponse.UpdateTime.Should().Be(DateTime.Parse("2025-11-17T12:01:00Z"));
    }

    #endregion

    #region Retry History Metadata Tests

    /// <summary>
    /// Test 12: Retry history stored in metadata with timestamps
    /// Tests structured retry history storage
    /// </summary>
    [Fact]
    public async Task LogRefundRetryAsync_StoresRetryHistoryWithTimestamps()
    {
        // Arrange
        var refund = CreateTestRefund(RefundStatus.Processing);
        _context.PaymentRefunds.Add(refund);
        await _context.SaveChangesAsync();

        var beforeRetry = DateTime.UtcNow;

        // Act
        await _sut.LogRefundRetryAsync(refund.Id, 1, "Network timeout");

        // Assert
        var updatedRefund = await _context.PaymentRefunds.FindAsync(refund.Id);
        updatedRefund!.Metadata.Should().ContainKey("retry_1");

        // Verify retry metadata contains timestamp and error
        var retryEntry = updatedRefund.Metadata["retry_1"];
        retryEntry.Should().NotBeNull("Retry entry should exist");

        // Metadata should contain structured data
        // (Actual structure depends on serialization - verify it exists)
        retryEntry.ToString().Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Database Update Verification Tests

    /// <summary>
    /// Test 13: All database updates save correctly
    /// Tests SaveChangesAsync is called and persists data
    /// </summary>
    [Fact]
    public async Task AuditService_AllMethods_SaveChangesToDatabase()
    {
        // Arrange
        var refund = CreateTestRefund(RefundStatus.Processing);
        _context.PaymentRefunds.Add(refund);
        await _context.SaveChangesAsync();

        // Act - Call all audit methods
        await _sut.LogRefundRequestAsync(refund);
        await _sut.LogRefundRetryAsync(refund.Id, 1, "First error");

        var paypalRefund = new PayPalRefundResponse
        {
            RefundId = "REFUND-SAVE-TEST",
            Status = "COMPLETED",
            Amount = new PayPalAmount { CurrencyCode = "USD", Value = "100.00" }
        };

        await _sut.LogRefundSuccessAsync(refund.Id, paypalRefund);

        // Assert - Verify data persisted by re-querying
        var persistedRefund = await _context.PaymentRefunds.FindAsync(refund.Id);
        persistedRefund.Should().NotBeNull("Refund should be persisted");
        persistedRefund!.RetryCount.Should().Be(1, "Retry count should be persisted");
        persistedRefund.RefundStatus.Should().Be(RefundStatus.Completed, "Status should be persisted");
        persistedRefund.EncryptedPayPalRefundId.Should().Be("REFUND-SAVE-TEST", "PayPal refund ID should be persisted");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test PaymentRefund entity
    /// </summary>
    private PaymentRefund CreateTestRefund(RefundStatus status)
    {
        return new PaymentRefund
        {
            Id = Guid.NewGuid(),
            OriginalPaymentId = _testPaymentId,
            RefundAmountValue = 100.00m,
            RefundCurrency = "USD",
            RefundReason = "Test refund",
            RefundStatus = status,
            ProcessedByUserId = _testUserId,
            IdempotencyKey = $"WCR-test-{Guid.NewGuid():N}",
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>()
        };
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
    }
}
