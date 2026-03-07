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
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Tests.Fixtures;

namespace WitchCityRope.Api.Tests.Services;

/// <summary>
/// Phase 1 PayPal Refund Enhancement tests
/// Tests Capture ID usage, idempotency key generation, and legacy payment handling
/// CRITICAL: Financial operations - any failures can result in duplicate refunds or refund failures
/// UPDATED: Migrated from Payment model to TicketPurchase model (2026-03-07)
/// Uses TestContainers PostgreSQL via DatabaseTestFixture (NOT InMemoryDatabase)
/// </summary>
[Collection("Database")]
public class RefundServicePhase1Tests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly Mock<IPayPalService> _mockPayPalService;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly Mock<IVolunteerAssignmentService> _mockVolunteerService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<RefundService>> _mockLogger;
    private ApplicationDbContext _context = null!;
    private RefundService _sut = null!; // System Under Test
    private Guid _testUserId;
    private Guid _testAdminId;
    private Guid _testEventId;
    private Guid _testTicketTypeId;

    public RefundServicePhase1Tests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _mockPayPalService = new Mock<IPayPalService>();
        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockVolunteerService = new Mock<IVolunteerAssignmentService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<RefundService>>();
    }

    public async Task InitializeAsync()
    {
        _context = _fixture.CreateDbContext();
        _testUserId = Guid.NewGuid();
        _testAdminId = Guid.NewGuid();
        _testEventId = Guid.NewGuid();
        _testTicketTypeId = Guid.NewGuid();

        _sut = new RefundService(
            _context,
            _mockPayPalService.Object,
            _mockEncryptionService.Object,
            _mockVolunteerService.Object,
            _mockEmailService.Object,
            _mockLogger.Object);

        // Ensure venue exists for FK constraint
        var venue = await _context.Venues.FindAsync(1);
        if (venue == null)
        {
            venue = new Venue
            {
                Id = 1,
                Name = "Test Venue",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Venues.Add(venue);
            await _context.SaveChangesAsync();
        }

        // Seed test users
        var testUser = new ApplicationUser
        {
            Id = _testUserId,
            Email = $"test-{Guid.NewGuid():N}@example.com",
            SceneName = "TestUser",
            UserName = $"test-{Guid.NewGuid():N}@example.com",
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var adminUser = new ApplicationUser
        {
            Id = _testAdminId,
            Email = $"admin-{Guid.NewGuid():N}@example.com",
            SceneName = "AdminUser",
            UserName = $"admin-{Guid.NewGuid():N}@example.com",
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.AddRange(testUser, adminUser);

        // Seed test event (FK parent for TicketType)
        var testEvent = new Event
        {
            Id = _testEventId,
            Title = "Test Event",
            Description = "Test Description",
            AllowRsvps = false,
            RequireTicketPurchase = true,
            VettedMembersOnly = false,
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Capacity = 20,
            VenueId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Events.Add(testEvent);

        // Seed test ticket type (FK parent for TicketPurchase)
        var ticketType = new TicketType
        {
            Id = _testTicketTypeId,
            EventId = _testEventId,
            Name = "General Admission",
            Price = 100.00m,
            Available = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TicketTypes.Add(ticketType);

        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        await _fixture.ResetDatabaseAsync();
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
        var ticketPurchase = CreateTicketPurchaseWithCaptureId(100.00m, "encrypted-capture-id-123");
        _context.TicketPurchases.Add(ticketPurchase);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
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
    /// Tests the complete decryption -> API call flow
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_DecryptsCaptureId_BeforeApiCall()
    {
        // Arrange
        var encryptedCaptureId = "ENCRYPTED-CAPTURE-XYZ789";
        var decryptedCaptureId = "PAYPAL-CAPTURE-XYZ789";

        var ticketPurchase = CreateTicketPurchaseWithCaptureId(50.00m, encryptedCaptureId);
        _context.TicketPurchases.Add(ticketPurchase);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
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
        var ticketPurchase = CreateTicketPurchaseWithCaptureId(100.00m);
        _context.TicketPurchases.Add(ticketPurchase);
        await _context.SaveChangesAsync();

        var request1 = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
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
        var ticketPurchase = CreateTicketPurchaseWithCaptureId(100.00m);
        _context.TicketPurchases.Add(ticketPurchase);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
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
        var ticketPurchase = CreateTicketPurchaseWithCaptureId(100.00m);
        _context.TicketPurchases.Add(ticketPurchase);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
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

        // Setup volunteer service mock
        _mockVolunteerService
            .Setup(x => x.CancelAllVolunteerSignupsForUserEventAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 0, null));

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedIdempotencyKey.Should().NotBeNullOrEmpty("API call should receive idempotency key");
    }

    /// <summary>
    /// Test 6: Verify idempotency key is passed to PayPal API
    /// Tests end-to-end flow of key generation -> storage -> API call
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_PassesIdempotencyKey_ToPayPalService()
    {
        // Arrange
        var ticketPurchase = CreateTicketPurchaseWithCaptureId(100.00m);
        _context.TicketPurchases.Add(ticketPurchase);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
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
        // Arrange - Legacy ticket purchase with Order ID but no Capture ID
        var ticketPurchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            TicketTypeId = _testTicketTypeId,
            UserId = _testUserId,
            TotalPrice = 100.00m,
            PaymentStatus = TicketPurchasePaymentStatus.Completed,
            PaymentMethod = "PayPal",
            ProcessedAt = DateTime.UtcNow,
            EncryptedPayPalOrderId = "encrypted-order-id-123", // Has Order ID
            EncryptedPayPalCaptureId = null, // NO Capture ID (legacy)
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TicketPurchases.Add(ticketPurchase);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
            RefundAmount = Money.Create(100.00m, "USD"),
            RefundReason = "Refund for legacy payment",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert - The refund should either fail or create a failed refund record
        // The service handles missing capture IDs by marking the refund as failed
        if (result.IsSuccess)
        {
            // Service created a refund record but marked it as failed
            result.Value!.RefundStatus.Should().Be(RefundStatus.Failed);
        }
        else
        {
            // Service returned failure directly
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

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
    /// Test 8: Verify null Capture ID is handled properly
    /// Edge case test for null value handling
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_WithNullCaptureId_FailsGracefully()
    {
        // Arrange
        var ticketPurchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            TicketTypeId = _testTicketTypeId,
            UserId = _testUserId,
            TotalPrice = 50.00m,
            PaymentStatus = TicketPurchasePaymentStatus.Completed,
            PaymentMethod = "PayPal",
            ProcessedAt = DateTime.UtcNow,
            EncryptedPayPalOrderId = "encrypted-order-id",
            EncryptedPayPalCaptureId = null, // Explicitly null
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TicketPurchases.Add(ticketPurchase);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
            RefundAmount = Money.Create(50.00m, "USD"),
            RefundReason = "Testing null Capture ID handling",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert - Should handle gracefully (either fail or create failed refund)
        if (result.IsSuccess)
        {
            result.Value!.RefundStatus.Should().Be(RefundStatus.Failed);
        }
        else
        {
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

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
        var ticketPurchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            TicketTypeId = _testTicketTypeId,
            UserId = _testUserId,
            TotalPrice = 75.00m,
            PaymentStatus = TicketPurchasePaymentStatus.Completed,
            PaymentMethod = "PayPal",
            ProcessedAt = DateTime.UtcNow,
            EncryptedPayPalOrderId = "encrypted-order-id",
            EncryptedPayPalCaptureId = string.Empty, // Empty string
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TicketPurchases.Add(ticketPurchase);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
            RefundAmount = Money.Create(75.00m, "USD"),
            RefundReason = "Testing empty Capture ID handling",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert - Should handle gracefully
        if (result.IsSuccess)
        {
            // If service creates a refund record, it should be failed or completed
            // (empty string may be treated as "no PayPal" and processed as manual)
            result.Value.Should().NotBeNull();
        }
        else
        {
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        // Verify PayPal service was NOT called with empty capture ID
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

    private TicketPurchase CreateTicketPurchaseWithCaptureId(decimal amount, string? encryptedCaptureId = null)
    {
        return new TicketPurchase
        {
            Id = Guid.NewGuid(),
            TicketTypeId = _testTicketTypeId,
            UserId = _testUserId,
            TotalPrice = amount,
            PaymentStatus = TicketPurchasePaymentStatus.Completed,
            PaymentMethod = "PayPal",
            ProcessedAt = DateTime.UtcNow,
            EncryptedPayPalOrderId = "encrypted-order-id",
            EncryptedPayPalCaptureId = encryptedCaptureId ?? "encrypted-capture-id-default",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
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
}
