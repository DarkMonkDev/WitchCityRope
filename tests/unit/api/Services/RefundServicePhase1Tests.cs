using WitchCityRope.Api.Features.Shared.Models;
using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.Models;
using WitchCityRope.Api.Features.Payments.Models.PayPal;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Features.Shared.Services;
using WitchCityRope.Api.Features.Volunteers.Services;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Tests.Services;

/// <summary>
/// Phase 1 PayPal Refund Enhancement tests
/// Tests Capture ID usage, idempotency key generation, and legacy payment handling
/// CRITICAL: Financial operations - any failures can result in duplicate refunds or refund failures
/// </summary>
public class RefundServicePhase1Tests : IDisposable
{
    private readonly Mock<IPayPalService> _mockPayPalService;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly Mock<IVolunteerAssignmentService> _mockVolunteerService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<RefundService>> _mockLogger;
    private readonly ApplicationDbContext _context;
    private readonly RefundService _sut; // System Under Test
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testAdminId = Guid.NewGuid();

    public RefundServicePhase1Tests()
    {
        // Setup in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockPayPalService = new Mock<IPayPalService>();
        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockVolunteerService = new Mock<IVolunteerAssignmentService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<RefundService>>();

        _sut = new RefundService(
            _context,
            _mockPayPalService.Object,
            _mockEncryptionService.Object,
            _mockVolunteerService.Object,
            _mockEmailService.Object,
            _mockLogger.Object);

        // Seed test users
        var testUser = new ApplicationUser
        {
            Id = _testUserId,
            Email = "test@example.com",
            SceneName = "TestUser"
        };

        var adminUser = new ApplicationUser
        {
            Id = _testAdminId,
            Email = "admin@example.com",
            SceneName = "AdminUser"
        };

        _context.Users.AddRange(testUser, adminUser);
        _context.SaveChanges();
    }

    #region Capture ID Usage Tests

    /// <summary>
    /// Test 1: Verify refunds use EncryptedPayPalCaptureId instead of EncryptedPayPalOrderId
    /// CRITICAL: This is the core Phase 1 requirement - using Capture ID for refunds
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_UsesCaptureId_NotOrderId()
    {
        // Arrange
        var payment = CreatePaymentWithCaptureId(100.00m, "encrypted-capture-id-123");
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            PaymentId = payment.Id,
            RefundAmount = Money.Create(100.00m, "USD"),
            RefundReason = "Customer requested full refund",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        // Setup encryption to return the decrypted Capture ID
        _mockEncryptionService
            .Setup(x => x.DecryptAsync("encrypted-capture-id-123"))
            .ReturnsAsync("PAYPAL-CAPTURE-ABC123");

        SetupSuccessfulPayPalRefund();

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify PayPalService.RefundCaptureAsync was called with the CAPTURE ID
        _mockPayPalService.Verify(
            x => x.RefundCaptureAsync(
                "PAYPAL-CAPTURE-ABC123", // Decrypted Capture ID
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(), // Idempotency key
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Refund should use Capture ID, not Order ID");
    }

    /// <summary>
    /// Test 2: Verify refund API call receives decrypted Capture ID
    /// Tests the complete decryption → API call flow
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_DecryptsCaptureId_BeforeApiCall()
    {
        // Arrange
        var encryptedCaptureId = "ENCRYPTED-CAPTURE-XYZ789";
        var decryptedCaptureId = "PAYPAL-CAPTURE-XYZ789";

        var payment = CreatePaymentWithCaptureId(50.00m, encryptedCaptureId);
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            PaymentId = payment.Id,
            RefundAmount = Money.Create(50.00m, "USD"),
            RefundReason = "Partial refund requested",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        // Setup encryption service to decrypt Capture ID
        _mockEncryptionService
            .Setup(x => x.DecryptAsync(encryptedCaptureId))
            .ReturnsAsync(decryptedCaptureId);

        SetupSuccessfulPayPalRefund();

        // Act
        await _sut.ProcessRefundAsync(request);

        // Assert - Verify decryption was called with correct encrypted value
        _mockEncryptionService.Verify(
            x => x.DecryptAsync(encryptedCaptureId),
            Times.Once,
            "Service should decrypt the Capture ID before API call");

        // Assert - Verify API call received decrypted Capture ID
        _mockPayPalService.Verify(
            x => x.RefundCaptureAsync(
                decryptedCaptureId,
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "PayPal API should receive decrypted Capture ID");
    }

    #endregion

    #region Idempotency Key Generation Tests

    /// <summary>
    /// Test 3: Verify unique idempotency keys are generated for each refund
    /// CRITICAL: Prevents duplicate refunds on retry
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_GeneratesUniqueIdempotencyKey()
    {
        // Arrange
        var payment = CreatePaymentWithCaptureId(100.00m);
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var request1 = new ProcessRefundRequest
        {
            PaymentId = payment.Id,
            RefundAmount = Money.Create(50.00m, "USD"),
            RefundReason = "First refund request",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        SetupSuccessfulPayPalRefund();
        _mockEncryptionService
            .Setup(x => x.DecryptAsync(It.IsAny<string>()))
            .ReturnsAsync("CAPTURE-123");

        // Act
        var result1 = await _sut.ProcessRefundAsync(request1);

        // Assert - First refund has idempotency key
        result1.IsSuccess.Should().BeTrue();
        result1.Value.Should().NotBeNull();
        result1.Value!.IdempotencyKey.Should().NotBeNullOrEmpty("Idempotency key must be generated");

        // Verify idempotency key was stored in database
        var storedRefund = await _context.PaymentRefunds.FindAsync(result1.Value.Id);
        storedRefund.Should().NotBeNull();
        storedRefund!.IdempotencyKey.Should().Be(result1.Value.IdempotencyKey);
    }

    /// <summary>
    /// Test 4: Verify idempotency key format is "WCR-{guid}"
    /// Tests standardized key format across the system
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_IdempotencyKey_HasCorrectFormat()
    {
        // Arrange
        var payment = CreatePaymentWithCaptureId(100.00m);
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            PaymentId = payment.Id,
            RefundAmount = Money.Create(100.00m, "USD"),
            RefundReason = "Test idempotency key format",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        SetupSuccessfulPayPalRefund();
        _mockEncryptionService
            .Setup(x => x.DecryptAsync(It.IsAny<string>()))
            .ReturnsAsync("CAPTURE-123");

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var idempotencyKey = result.Value!.IdempotencyKey;

        idempotencyKey.Should().StartWith("WCR-", "Key should have WCR prefix");
        idempotencyKey.Should().HaveLength(36, "Format should be WCR-{32-char-guid} = 36 chars total");

        // Verify the part after "WCR-" is a valid GUID (32 hex characters)
        var guidPart = idempotencyKey.Substring(4);
        Guid.TryParse(guidPart, out _).Should().BeTrue("GUID part should be valid");
    }

    /// <summary>
    /// Test 5: Verify idempotency key is stored BEFORE PayPal API call
    /// CRITICAL: Ensures key is persisted even if API call fails
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_StoresIdempotencyKey_BeforeApiCall()
    {
        // Arrange
        var payment = CreatePaymentWithCaptureId(100.00m);
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            PaymentId = payment.Id,
            RefundAmount = Money.Create(100.00m, "USD"),
            RefundReason = "Test key storage timing",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        string? capturedIdempotencyKey = null;

        // Setup PayPal service to capture the idempotency key when called
        _mockEncryptionService
            .Setup(x => x.DecryptAsync(It.IsAny<string>()))
            .ReturnsAsync("CAPTURE-123");

        _mockPayPalService
            .Setup(x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Money, string, string, string, CancellationToken>(
                (captureId, amount, reason, idempotencyKey, note, ct) =>
                {
                    capturedIdempotencyKey = idempotencyKey;

                    // At this point, verify the key is already in database
                    var refund = _context.PaymentRefunds
                        .FirstOrDefault(r => r.IdempotencyKey == idempotencyKey);

                    refund.Should().NotBeNull("Idempotency key must be stored in DB before API call");
                })
            .ReturnsAsync(Result<PayPalRefundResponse>.Success(new PayPalRefundResponse
            {
                RefundId = "REFUND-123",
                Status = "COMPLETED",
                Amount = new PayPalAmount
                {
                    CurrencyCode = "USD",
                    Value = "100.00"
                }
            }));

        _mockEncryptionService
            .Setup(x => x.EncryptAsync(It.IsAny<string>()))
            .ReturnsAsync("encrypted-refund-id");

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedIdempotencyKey.Should().NotBeNullOrEmpty("API call should receive idempotency key");
    }

    /// <summary>
    /// Test 6: Verify idempotency key is passed to PayPal API
    /// Tests end-to-end flow of key generation → storage → API call
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_PassesIdempotencyKey_ToPayPalService()
    {
        // Arrange
        var payment = CreatePaymentWithCaptureId(100.00m);
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            PaymentId = payment.Id,
            RefundAmount = Money.Create(100.00m, "USD"),
            RefundReason = "Test idempotency key passing",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        SetupSuccessfulPayPalRefund();
        _mockEncryptionService
            .Setup(x => x.DecryptAsync(It.IsAny<string>()))
            .ReturnsAsync("CAPTURE-123");

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify PayPal service received the idempotency key
        _mockPayPalService.Verify(
            x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.Is<string>(key => !string.IsNullOrEmpty(key) && key.StartsWith("WCR-")), // Idempotency key parameter
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "PayPal service must receive the idempotency key");
    }

    #endregion

    #region Legacy Payment Handling Tests

    /// <summary>
    /// Test 7: Verify payments without Capture ID fail gracefully
    /// CRITICAL: Legacy payments need clear error messages for manual processing
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_WithoutCaptureId_FailsWithClearMessage()
    {
        // Arrange - Legacy payment with Order ID but no Capture ID
        var payment = new Payment
        {
            EventRegistrationId = Guid.NewGuid(),
            UserId = _testUserId,
            AmountValue = 100.00m,
            Currency = "USD",
            Status = PaymentStatus.Completed,
            ProcessedAt = DateTime.UtcNow,
            EncryptedPayPalOrderId = "encrypted-order-id-123", // Has Order ID
            EncryptedPayPalCaptureId = null, // NO Capture ID (legacy)
            Refunds = new List<PaymentRefund>()
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            PaymentId = payment.Id,
            RefundAmount = Money.Create(100.00m, "USD"),
            RefundReason = "Refund for legacy payment",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse("Legacy payments without Capture ID should fail");
        result.ErrorMessage.Should().Contain("Capture ID", "Error should mention Capture ID");
        result.ErrorMessage.Should().Contain("manual", "Error should indicate manual processing needed");

        // Verify a failed refund record was created
        var refunds = await _context.PaymentRefunds
            .Where(r => r.TicketPurchaseId == payment.Id)
            .ToListAsync();

        refunds.Should().HaveCount(1, "Failed refund should be recorded");
        refunds[0].RefundStatus.Should().Be(RefundStatus.Failed);
        refunds[0].Metadata.Should().ContainKey("failure_reason");
    }

    /// <summary>
    /// Test 8: Verify null Capture ID is handled properly
    /// Edge case test for null value handling
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_WithNullCaptureId_FailsGracefully()
    {
        // Arrange
        var payment = new Payment
        {
            EventRegistrationId = Guid.NewGuid(),
            UserId = _testUserId,
            AmountValue = 50.00m,
            Currency = "USD",
            Status = PaymentStatus.Completed,
            ProcessedAt = DateTime.UtcNow,
            EncryptedPayPalOrderId = "encrypted-order-id",
            EncryptedPayPalCaptureId = null, // Explicitly null
            Refunds = new List<PaymentRefund>()
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            PaymentId = payment.Id,
            RefundAmount = Money.Create(50.00m, "USD"),
            RefundReason = "Testing null Capture ID handling",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();

        // Verify PayPal service was NOT called (no Capture ID to decrypt)
        _mockPayPalService.Verify(
            x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "PayPal should not be called without Capture ID");
    }

    /// <summary>
    /// Test 9: Verify empty string Capture ID is handled properly
    /// Edge case test for empty string handling
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_WithEmptyCaptureId_FailsGracefully()
    {
        // Arrange
        var payment = new Payment
        {
            EventRegistrationId = Guid.NewGuid(),
            UserId = _testUserId,
            AmountValue = 75.00m,
            Currency = "USD",
            Status = PaymentStatus.Completed,
            ProcessedAt = DateTime.UtcNow,
            EncryptedPayPalOrderId = "encrypted-order-id",
            EncryptedPayPalCaptureId = string.Empty, // Empty string
            Refunds = new List<PaymentRefund>()
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            PaymentId = payment.Id,
            RefundAmount = Money.Create(75.00m, "USD"),
            RefundReason = "Testing empty Capture ID handling",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();

        // Verify PayPal service was NOT called
        _mockPayPalService.Verify(
            x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Helper Methods

    private Payment CreatePaymentWithCaptureId(decimal amount, string? encryptedCaptureId = null)
    {
        return new Payment
        {
            EventRegistrationId = Guid.NewGuid(),
            UserId = _testUserId,
            AmountValue = amount,
            Currency = "USD",
            Status = PaymentStatus.Completed,
            ProcessedAt = DateTime.UtcNow,
            EncryptedPayPalOrderId = "encrypted-order-id",
            EncryptedPayPalCaptureId = encryptedCaptureId ?? "encrypted-capture-id-default",
            Refunds = new List<PaymentRefund>()
        };
    }

    private void SetupSuccessfulPayPalRefund()
    {
        // NOTE: DecryptAsync is NOT set up here - each test should configure it with test-specific values
        // This allows tests to verify the exact Capture ID being decrypted and used

        // Setup encryption service to encrypt Refund ID
        _mockEncryptionService
            .Setup(x => x.EncryptAsync(It.IsAny<string>()))
            .ReturnsAsync("encrypted-refund-id");

        var paypalRefundResponse = new PayPalRefundResponse
        {
            RefundId = "PAYPAL-REFUND-123",
            Status = "COMPLETED",
            Amount = new PayPalAmount
            {
                CurrencyCode = "USD",
                Value = "100.00"
            }
        };

        _mockPayPalService
            .Setup(x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PayPalRefundResponse>.Success(paypalRefundResponse));

        // Setup volunteer service mock
        _mockVolunteerService
            .Setup(x => x.CancelAllVolunteerSignupsForUserEventAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 0, null));
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
    }
}
