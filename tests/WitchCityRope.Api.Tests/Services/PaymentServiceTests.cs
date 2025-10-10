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
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Tests.Services;

/// <summary>
/// Comprehensive test suite for PaymentService
/// Tests payment processing with sliding scale pricing, validation, error handling, and audit trails
/// CRITICAL: Financial transactions must be thoroughly tested to prevent money loss
/// </summary>
public class PaymentServiceTests : IDisposable
{
    private readonly Mock<IPayPalService> _mockPayPalService;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly Mock<ILogger<PaymentService>> _mockLogger;
    private readonly ApplicationDbContext _context;
    private readonly PaymentService _sut; // System Under Test
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testRegistrationId = Guid.NewGuid();

    public PaymentServiceTests()
    {
        // Setup in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockPayPalService = new Mock<IPayPalService>();
        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockLogger = new Mock<ILogger<PaymentService>>();

        _sut = new PaymentService(
            _context,
            _mockPayPalService.Object,
            _mockEncryptionService.Object,
            _mockLogger.Object);

        // Seed test user
        var testUser = new ApplicationUser
        {
            Id = _testUserId,
            Email = "test@example.com",
            SceneName = "TestUser"
        };
        _context.Users.Add(testUser);
        _context.SaveChanges();
    }

    #region Successful Payment Processing Tests

    /// <summary>
    /// Test 1: Verify successful payment processing with valid data (happy path)
    /// This is the most critical test - ensures payments work correctly
    /// </summary>
    [Fact]
    public async Task ProcessPaymentAsync_WithValidData_ReturnsSuccessAndCreatesPayment()
    {
        // Arrange
        var request = new ProcessPaymentRequest
        {
            EventRegistrationId = _testRegistrationId,
            UserId = _testUserId,
            OriginalAmount = Money.Create(50.00m, "USD"),
            SlidingScalePercentage = 25m,
            PaymentMethodType = PaymentMethodType.PayPal,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        var paypalOrderResponse = new PayPalOrderResponse
        {
            OrderId = "PAYPAL-ORDER-123",
            Status = "CREATED",
            Links = new List<PayPalLink>()
        };

        _mockPayPalService
            .Setup(x => x.CreateOrderAsync(
                It.IsAny<Money>(),
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PayPalOrderResponse>.Success(paypalOrderResponse));

        _mockEncryptionService
            .Setup(x => x.EncryptAsync(It.IsAny<string>()))
            .ReturnsAsync("encrypted-order-id");

        // Act
        var result = await _sut.ProcessPaymentAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be(PaymentStatus.Pending);
        result.Value.AmountValue.Should().Be(37.50m); // 50 - 25% = 37.50
        result.Value.SlidingScalePercentage.Should().Be(25m);
        result.Value.UserId.Should().Be(_testUserId);
        result.Value.EventRegistrationId.Should().Be(_testRegistrationId);

        // Verify audit log was created
        var auditLogs = await _context.PaymentAuditLog.ToListAsync();
        auditLogs.Should().ContainSingle();
        auditLogs[0].PaymentId.Should().Be(result.Value.Id);
    }

    /// <summary>
    /// Test 2: Verify payment processing with zero sliding scale (full price)
    /// </summary>
    [Fact]
    public async Task ProcessPaymentAsync_WithZeroSlidingScale_ChargesFullAmount()
    {
        // Arrange
        var request = new ProcessPaymentRequest
        {
            EventRegistrationId = _testRegistrationId,
            UserId = _testUserId,
            OriginalAmount = Money.Create(100.00m, "USD"),
            SlidingScalePercentage = 0m,
            PaymentMethodType = PaymentMethodType.PayPal,
            IpAddress = "192.168.1.1",
            UserAgent = "Test Agent"
        };

        SetupSuccessfulPayPalOrder();

        // Act
        var result = await _sut.ProcessPaymentAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AmountValue.Should().Be(100.00m);
        result.Value.SlidingScalePercentage.Should().Be(0m);
    }

    /// <summary>
    /// Test 3: Verify payment processing with maximum sliding scale (75% discount)
    /// </summary>
    [Fact]
    public async Task ProcessPaymentAsync_WithMaximumSlidingScale_Applies75PercentDiscount()
    {
        // Arrange
        var request = new ProcessPaymentRequest
        {
            EventRegistrationId = _testRegistrationId,
            UserId = _testUserId,
            OriginalAmount = Money.Create(100.00m, "USD"),
            SlidingScalePercentage = 75m,
            PaymentMethodType = PaymentMethodType.PayPal,
            IpAddress = "192.168.1.1",
            UserAgent = "Test Agent"
        };

        SetupSuccessfulPayPalOrder();

        // Act
        var result = await _sut.ProcessPaymentAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AmountValue.Should().Be(25.00m); // 100 - 75% = 25
        result.Value.SlidingScalePercentage.Should().Be(75m);
    }

    #endregion

    #region Payment Validation Tests

    /// <summary>
    /// Test 4: Verify payment fails with invalid sliding scale percentage (exceeds 75%)
    /// </summary>
    [Fact]
    public async Task ProcessPaymentAsync_WithInvalidSlidingScale_ReturnsFailure()
    {
        // Arrange
        var request = new ProcessPaymentRequest
        {
            EventRegistrationId = _testRegistrationId,
            UserId = _testUserId,
            OriginalAmount = Money.Create(100.00m, "USD"),
            SlidingScalePercentage = 80m, // Invalid - exceeds 75%
            PaymentMethodType = PaymentMethodType.PayPal,
            IpAddress = "192.168.1.1",
            UserAgent = "Test Agent"
        };

        // Act
        var result = await _sut.ProcessPaymentAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid sliding scale percentage");
        result.ErrorMessage.Should().Contain("0%").And.Contain("75%");
    }

    /// <summary>
    /// Test 5: Verify payment fails with negative sliding scale percentage
    /// </summary>
    [Fact]
    public async Task ProcessPaymentAsync_WithNegativeSlidingScale_ReturnsFailure()
    {
        // Arrange
        var request = new ProcessPaymentRequest
        {
            EventRegistrationId = _testRegistrationId,
            UserId = _testUserId,
            OriginalAmount = Money.Create(100.00m, "USD"),
            SlidingScalePercentage = -10m, // Invalid - negative
            PaymentMethodType = PaymentMethodType.PayPal,
            IpAddress = "192.168.1.1",
            UserAgent = "Test Agent"
        };

        // Act
        var result = await _sut.ProcessPaymentAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid sliding scale percentage");
    }

    #endregion

    #region Duplicate Transaction Prevention Tests

    /// <summary>
    /// Test 6: Verify duplicate payment prevention (idempotency)
    /// CRITICAL: Prevents double-charging users
    /// </summary>
    [Fact]
    public async Task ProcessPaymentAsync_WithExistingCompletedPayment_ReturnsFailure()
    {
        // Arrange - Create existing completed payment
        var existingPayment = new Payment
        {
            EventRegistrationId = _testRegistrationId,
            UserId = _testUserId,
            AmountValue = 50.00m,
            Currency = "USD",
            Status = PaymentStatus.Completed,
            ProcessedAt = DateTime.UtcNow
        };
        _context.Payments.Add(existingPayment);
        await _context.SaveChangesAsync();

        var request = new ProcessPaymentRequest
        {
            EventRegistrationId = _testRegistrationId,
            UserId = _testUserId,
            OriginalAmount = Money.Create(50.00m, "USD"),
            SlidingScalePercentage = 0m,
            PaymentMethodType = PaymentMethodType.PayPal,
            IpAddress = "192.168.1.1",
            UserAgent = "Test Agent"
        };

        // Act
        var result = await _sut.ProcessPaymentAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Payment already completed");
        result.ErrorMessage.Should().Contain("event registration");
    }

    #endregion

    #region PayPal Integration Tests

    /// <summary>
    /// Test 7: Verify PayPal order creation failure handling
    /// Tests external service failure scenarios
    /// </summary>
    [Fact]
    public async Task ProcessPaymentAsync_WhenPayPalOrderCreationFails_ReturnsFailure()
    {
        // Arrange
        var request = new ProcessPaymentRequest
        {
            EventRegistrationId = _testRegistrationId,
            UserId = _testUserId,
            OriginalAmount = Money.Create(50.00m, "USD"),
            SlidingScalePercentage = 0m,
            PaymentMethodType = PaymentMethodType.PayPal,
            IpAddress = "192.168.1.1",
            UserAgent = "Test Agent"
        };

        _mockPayPalService
            .Setup(x => x.CreateOrderAsync(
                It.IsAny<Money>(),
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PayPalOrderResponse>.Failure("PayPal API error: Insufficient merchant balance"));

        // Act
        var result = await _sut.ProcessPaymentAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Failed to create PayPal order");
        result.ErrorMessage.Should().Contain("PayPal API error");

        // Verify payment failure was logged
        var failures = await _context.PaymentFailures.ToListAsync();
        failures.Should().ContainSingle();
        failures[0].FailureCode.Should().Be("paypal_order_creation_failed");
    }

    /// <summary>
    /// Test 8: Verify PayPal order ID is encrypted for PCI compliance
    /// CRITICAL: Security requirement - payment data must be encrypted
    /// </summary>
    [Fact]
    public async Task ProcessPaymentAsync_EncryptsPayPalOrderId()
    {
        // Arrange
        var request = new ProcessPaymentRequest
        {
            EventRegistrationId = _testRegistrationId,
            UserId = _testUserId,
            OriginalAmount = Money.Create(50.00m, "USD"),
            SlidingScalePercentage = 0m,
            PaymentMethodType = PaymentMethodType.PayPal,
            IpAddress = "192.168.1.1",
            UserAgent = "Test Agent"
        };

        SetupSuccessfulPayPalOrder();

        var encryptedOrderId = "ENCRYPTED-PAYPAL-ORDER-123";
        _mockEncryptionService
            .Setup(x => x.EncryptAsync("PAYPAL-ORDER-123"))
            .ReturnsAsync(encryptedOrderId);

        // Act
        var result = await _sut.ProcessPaymentAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.EncryptedPayPalOrderId.Should().Be(encryptedOrderId);

        // Verify encryption service was called
        _mockEncryptionService.Verify(
            x => x.EncryptAsync("PAYPAL-ORDER-123"),
            Times.Once);
    }

    #endregion

    #region Error Handling Tests

    /// <summary>
    /// Test 9: Verify payment fails gracefully when user not found
    /// </summary>
    [Fact]
    public async Task ProcessPaymentAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var request = new ProcessPaymentRequest
        {
            EventRegistrationId = _testRegistrationId,
            UserId = nonExistentUserId,
            OriginalAmount = Money.Create(50.00m, "USD"),
            SlidingScalePercentage = 0m,
            PaymentMethodType = PaymentMethodType.PayPal,
            IpAddress = "192.168.1.1",
            UserAgent = "Test Agent"
        };

        // Act
        var result = await _sut.ProcessPaymentAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("User not found");
    }

    /// <summary>
    /// Test 10: Verify database exception handling
    /// Tests resilience to infrastructure failures
    /// </summary>
    [Fact]
    public async Task ProcessPaymentAsync_WhenDatabaseThrowsException_ReturnsFailure()
    {
        // Arrange - Dispose the context to simulate database failure
        _context.Dispose();

        var request = new ProcessPaymentRequest
        {
            EventRegistrationId = _testRegistrationId,
            UserId = _testUserId,
            OriginalAmount = Money.Create(50.00m, "USD"),
            SlidingScalePercentage = 0m,
            PaymentMethodType = PaymentMethodType.PayPal,
            IpAddress = "192.168.1.1",
            UserAgent = "Test Agent"
        };

        // Act
        var result = await _sut.ProcessPaymentAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("error occurred while processing the payment");
    }

    #endregion

    #region Payment Status Tests

    /// <summary>
    /// Test 11: Verify payment status can be updated successfully
    /// Tests webhook handling scenario
    /// </summary>
    [Fact]
    public async Task UpdatePaymentStatusAsync_FromPendingToCompleted_UpdatesSuccessfully()
    {
        // Arrange
        var payment = new Payment
        {
            EventRegistrationId = _testRegistrationId,
            UserId = _testUserId,
            AmountValue = 50.00m,
            Currency = "USD",
            Status = PaymentStatus.Pending
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var paypalOrderId = "PAYPAL-ORDER-123";

        // Act
        var result = await _sut.UpdatePaymentStatusAsync(
            payment.Id,
            PaymentStatus.Completed,
            paypalOrderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(PaymentStatus.Completed);
        result.Value.ProcessedAt.Should().NotBeNull();
        result.Value.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify audit logs created
        var auditLogs = await _context.PaymentAuditLog
            .Where(a => a.PaymentId == payment.Id)
            .ToListAsync();
        auditLogs.Should().HaveCountGreaterOrEqualTo(2); // Status change + completion
    }

    /// <summary>
    /// Test 12: Verify payment status update with non-existent payment fails
    /// </summary>
    [Fact]
    public async Task UpdatePaymentStatusAsync_WithNonExistentPayment_ReturnsFailure()
    {
        // Arrange
        var nonExistentPaymentId = Guid.NewGuid();

        // Act
        var result = await _sut.UpdatePaymentStatusAsync(
            nonExistentPaymentId,
            PaymentStatus.Completed);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Payment not found");
    }

    #endregion

    #region Payment Retrieval Tests

    /// <summary>
    /// Test 13: Verify payment retrieval by ID with full details
    /// </summary>
    [Fact]
    public async Task GetPaymentByIdAsync_WithExistingPayment_ReturnsPaymentWithDetails()
    {
        // Arrange
        var payment = new Payment
        {
            EventRegistrationId = _testRegistrationId,
            UserId = _testUserId,
            AmountValue = 50.00m,
            Currency = "USD",
            Status = PaymentStatus.Completed,
            ProcessedAt = DateTime.UtcNow
        };
        _context.Payments.Add(payment);

        var auditLog = PaymentAuditLog.PaymentCompleted(payment.Id, "ORDER-123", 50.00m);
        _context.PaymentAuditLog.Add(auditLog);

        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetPaymentByIdAsync(payment.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(payment.Id);
        result.Value.AuditLogs.Should().ContainSingle();
    }

    /// <summary>
    /// Test 14: Verify retrieval of payments by user ID
    /// </summary>
    [Fact]
    public async Task GetPaymentsByUserIdAsync_ReturnsAllUserPayments()
    {
        // Arrange
        var payment1 = new Payment
        {
            EventRegistrationId = Guid.NewGuid(),
            UserId = _testUserId,
            AmountValue = 50.00m,
            Currency = "USD",
            Status = PaymentStatus.Completed
        };

        var payment2 = new Payment
        {
            EventRegistrationId = Guid.NewGuid(),
            UserId = _testUserId,
            AmountValue = 75.00m,
            Currency = "USD",
            Status = PaymentStatus.Pending
        };

        var otherUserPayment = new Payment
        {
            EventRegistrationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            AmountValue = 100.00m,
            Currency = "USD",
            Status = PaymentStatus.Completed
        };

        _context.Payments.AddRange(payment1, payment2, otherUserPayment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetPaymentsByUserIdAsync(_testUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(p => p.UserId == _testUserId);
    }

    /// <summary>
    /// Test 15: Verify payment retrieval by registration ID
    /// Tests event-specific payment lookup
    /// </summary>
    [Fact]
    public async Task GetPaymentByRegistrationIdAsync_WithExistingPayment_ReturnsPayment()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        var payment = new Payment
        {
            EventRegistrationId = registrationId,
            UserId = _testUserId,
            AmountValue = 50.00m,
            Currency = "USD",
            Status = PaymentStatus.Completed
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetPaymentByRegistrationIdAsync(registrationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.EventRegistrationId.Should().Be(registrationId);
    }

    #endregion

    #region Helper Methods

    private void SetupSuccessfulPayPalOrder()
    {
        var paypalOrderResponse = new PayPalOrderResponse
        {
            OrderId = "PAYPAL-ORDER-123",
            Status = "CREATED",
            Links = new List<PayPalLink>()
        };

        _mockPayPalService
            .Setup(x => x.CreateOrderAsync(
                It.IsAny<Money>(),
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PayPalOrderResponse>.Success(paypalOrderResponse));

        _mockEncryptionService
            .Setup(x => x.EncryptAsync(It.IsAny<string>()))
            .ReturnsAsync("encrypted-order-id");
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
    }
}
