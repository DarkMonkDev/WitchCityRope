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
/// Comprehensive test suite for RefundService
/// Tests refund processing, validation, partial refunds, and audit trails
/// CRITICAL: Refund errors can lead to financial loss and legal issues
/// </summary>
public class RefundServiceTests : IDisposable
{
    private readonly Mock<IPayPalService> _mockPayPalService;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly Mock<ILogger<RefundService>> _mockLogger;
    private readonly ApplicationDbContext _context;
    private readonly RefundService _sut; // System Under Test
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testAdminId = Guid.NewGuid();

    public RefundServiceTests()
    {
        // Setup in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockPayPalService = new Mock<IPayPalService>();
        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockLogger = new Mock<ILogger<RefundService>>();

        _sut = new RefundService(
            _context,
            _mockPayPalService.Object,
            _mockEncryptionService.Object,
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

    #region Successful Refund Processing Tests

    /// <summary>
    /// Test 1: Verify successful full refund processing (happy path)
    /// CRITICAL: Most important refund test case
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_WithValidFullRefund_ReturnsSuccessAndCompletesRefund()
    {
        // Arrange
        var payment = CreateCompletedPayment(100.00m);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            PaymentId = payment.Id,
            RefundAmount = Money.Create(100.00m, "USD"),
            RefundReason = "Customer requested full refund due to event cancellation",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1",
            UserAgent = "Admin Portal"
        };

        SetupSuccessfulPayPalRefund();

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.RefundStatus.Should().Be(RefundStatus.Completed);
        result.Value.RefundAmountValue.Should().Be(100.00m);
        result.Value.OriginalPaymentId.Should().Be(payment.Id);
        result.Value.ProcessedByUserId.Should().Be(_testAdminId);

        // Verify payment status updated
        var updatedPayment = await _context.Payments.FindAsync(payment.Id);
        updatedPayment!.Status.Should().Be(PaymentStatus.Refunded);
        updatedPayment.RefundedAt.Should().NotBeNull();

        // Verify audit logs created
        var auditLogs = await _context.PaymentAuditLog
            .Where(a => a.PaymentId == payment.Id)
            .ToListAsync();
        auditLogs.Should().HaveCountGreaterOrEqualTo(2); // Initiation + completion
    }

    /// <summary>
    /// Test 2: Verify successful partial refund processing
    /// Tests that partial refunds update payment status correctly
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_WithValidPartialRefund_UpdatesPaymentStatusToPartiallyRefunded()
    {
        // Arrange
        var payment = CreateCompletedPayment(100.00m);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            PaymentId = payment.Id,
            RefundAmount = Money.Create(50.00m, "USD"), // Partial refund
            RefundReason = "Partial refund requested - customer attended half of workshop series",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1",
            UserAgent = "Admin Portal"
        };

        SetupSuccessfulPayPalRefund();

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.RefundAmountValue.Should().Be(50.00m);

        // Verify payment status is partially refunded, not fully refunded
        var updatedPayment = await _context.Payments.FindAsync(payment.Id);
        updatedPayment!.Status.Should().Be(PaymentStatus.PartiallyRefunded);
        updatedPayment.RefundAmountValue.Should().Be(50.00m);
    }

    #endregion

    #region Refund Validation Tests

    /// <summary>
    /// Test 3: Verify refund fails when payment is not eligible (not completed)
    /// CRITICAL: Prevents refunding payments that haven't been processed
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_WithPendingPayment_ReturnsFailure()
    {
        // Arrange
        var payment = new Payment
        {
            EventRegistrationId = Guid.NewGuid(),
            UserId = _testUserId,
            AmountValue = 100.00m,
            Currency = "USD",
            Status = PaymentStatus.Pending, // Not eligible for refund
            EncryptedPayPalOrderId = "encrypted-order-id"
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            PaymentId = payment.Id,
            RefundAmount = Money.Create(100.00m, "USD"),
            RefundReason = "Customer request",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not eligible for refund");
        result.ErrorMessage.Should().Contain("completed payments");
    }

    /// <summary>
    /// Test 4: Verify refund fails when amount exceeds maximum available
    /// Prevents over-refunding
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_WithAmountExceedingAvailable_ReturnsFailure()
    {
        // Arrange
        var payment = CreateCompletedPayment(100.00m);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            PaymentId = payment.Id,
            RefundAmount = Money.Create(150.00m, "USD"), // Exceeds payment amount
            RefundReason = "Customer request for excessive refund",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("exceeds maximum available refund");
        result.ErrorMessage.Should().Contain("$100.00");
    }

    /// <summary>
    /// Test 5: Verify refund fails with insufficient reason length
    /// Ensures proper audit trail documentation
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_WithShortRefundReason_ReturnsFailure()
    {
        // Arrange
        var payment = CreateCompletedPayment(100.00m);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            PaymentId = payment.Id,
            RefundAmount = Money.Create(100.00m, "USD"),
            RefundReason = "Short", // Less than 10 characters
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Refund reason is required");
        result.ErrorMessage.Should().Contain("at least 10 characters");
    }

    /// <summary>
    /// Test 6: Verify multiple partial refunds don't exceed original payment
    /// Tests refund amount calculation with existing refunds
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_WithMultiplePartialRefunds_PreventsOverRefunding()
    {
        // Arrange
        var payment = CreateCompletedPayment(100.00m);

        // Add existing partial refund of $60
        var existingRefund = new PaymentRefund
        {
            OriginalPaymentId = payment.Id,
            RefundAmountValue = 60.00m,
            RefundCurrency = "USD",
            RefundStatus = RefundStatus.Completed,
            ProcessedByUserId = _testAdminId,
            RefundReason = "First partial refund"
        };
        payment.Refunds.Add(existingRefund);
        payment.Status = PaymentStatus.PartiallyRefunded;
        payment.RefundAmountValue = 60.00m;

        await _context.SaveChangesAsync();

        // Try to refund $50 more (total would be $110, exceeding $100)
        var request = new ProcessRefundRequest
        {
            PaymentId = payment.Id,
            RefundAmount = Money.Create(50.00m, "USD"),
            RefundReason = "Second partial refund attempt",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("exceeds maximum available refund");
        result.ErrorMessage.Should().Contain("$40.00"); // Only $40 remaining
    }

    #endregion

    #region PayPal Integration Tests

    /// <summary>
    /// Test 7: Verify PayPal refund failure handling
    /// Tests external service failure scenarios
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_WhenPayPalRefundFails_MarksRefundAsFailed()
    {
        // Arrange
        var payment = CreateCompletedPayment(100.00m);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            PaymentId = payment.Id,
            RefundAmount = Money.Create(100.00m, "USD"),
            RefundReason = "Customer requested refund",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        // Setup PayPal refund to fail
        _mockEncryptionService
            .Setup(x => x.DecryptAsync(It.IsAny<string>()))
            .ReturnsAsync("PAYPAL-CAPTURE-123");

        _mockPayPalService
            .Setup(x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PayPalRefundResponse>.Failure("PayPal API Error: Capture already fully refunded"));

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Refund record created
        result.Value!.RefundStatus.Should().Be(RefundStatus.Failed);
        result.Value.Metadata.Should().ContainKey("failure_reason");
    }

    /// <summary>
    /// Test 8: Verify PayPal refund ID is encrypted
    /// CRITICAL: Security requirement for PCI compliance
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_EncryptsPayPalRefundId()
    {
        // Arrange
        var payment = CreateCompletedPayment(100.00m);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            PaymentId = payment.Id,
            RefundAmount = Money.Create(100.00m, "USD"),
            RefundReason = "Customer requested refund",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        var paypalRefundId = "PAYPAL-REFUND-123";
        var encryptedRefundId = "ENCRYPTED-REFUND-123";

        _mockEncryptionService
            .Setup(x => x.DecryptAsync(It.IsAny<string>()))
            .ReturnsAsync("PAYPAL-CAPTURE-123");

        _mockEncryptionService
            .Setup(x => x.EncryptAsync(paypalRefundId))
            .ReturnsAsync(encryptedRefundId);

        var paypalRefundResponse = new PayPalRefundResponse
        {
            RefundId = paypalRefundId,
            Status = "COMPLETED",
            CaptureId = "PAYPAL-CAPTURE-123",
            Amount = Money.Create(100.00m, "USD")
        };

        _mockPayPalService
            .Setup(x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PayPalRefundResponse>.Success(paypalRefundResponse));

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.EncryptedPayPalRefundId.Should().Be(encryptedRefundId);

        // Verify encryption was called
        _mockEncryptionService.Verify(
            x => x.EncryptAsync(paypalRefundId),
            Times.Once);
    }

    #endregion

    #region Refund Retrieval Tests

    /// <summary>
    /// Test 9: Verify retrieval of refunds by payment ID
    /// Tests refund history lookup
    /// </summary>
    [Fact]
    public async Task GetRefundsByPaymentIdAsync_ReturnsAllRefundsForPayment()
    {
        // Arrange
        var payment = CreateCompletedPayment(100.00m);

        var refund1 = new PaymentRefund
        {
            OriginalPaymentId = payment.Id,
            RefundAmountValue = 40.00m,
            RefundCurrency = "USD",
            RefundStatus = RefundStatus.Completed,
            ProcessedByUserId = _testAdminId,
            RefundReason = "First partial refund"
        };

        var refund2 = new PaymentRefund
        {
            OriginalPaymentId = payment.Id,
            RefundAmountValue = 30.00m,
            RefundCurrency = "USD",
            RefundStatus = RefundStatus.Completed,
            ProcessedByUserId = _testAdminId,
            RefundReason = "Second partial refund"
        };

        _context.PaymentRefunds.AddRange(refund1, refund2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetRefundsByPaymentIdAsync(payment.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(r => r.OriginalPaymentId == payment.Id);
        result.Value.Sum(r => r.RefundAmountValue).Should().Be(70.00m);
    }

    /// <summary>
    /// Test 10: Verify maximum refund amount calculation
    /// Tests remaining refund availability after partial refunds
    /// </summary>
    [Fact]
    public async Task GetMaximumRefundAmountAsync_WithPartialRefunds_ReturnsRemainingAmount()
    {
        // Arrange
        var payment = CreateCompletedPayment(100.00m);

        // Add completed refunds totaling $60
        var refund1 = new PaymentRefund
        {
            OriginalPaymentId = payment.Id,
            RefundAmountValue = 40.00m,
            RefundCurrency = "USD",
            RefundStatus = RefundStatus.Completed,
            ProcessedByUserId = _testAdminId,
            RefundReason = "First partial refund"
        };

        var refund2 = new PaymentRefund
        {
            OriginalPaymentId = payment.Id,
            RefundAmountValue = 20.00m,
            RefundCurrency = "USD",
            RefundStatus = RefundStatus.Completed,
            ProcessedByUserId = _testAdminId,
            RefundReason = "Second partial refund"
        };

        // Add failed refund (should not count)
        var failedRefund = new PaymentRefund
        {
            OriginalPaymentId = payment.Id,
            RefundAmountValue = 10.00m,
            RefundCurrency = "USD",
            RefundStatus = RefundStatus.Failed,
            ProcessedByUserId = _testAdminId,
            RefundReason = "Failed refund attempt"
        };

        payment.Refunds.AddRange(new[] { refund1, refund2, failedRefund });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetMaximumRefundAmountAsync(payment.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Amount.Should().Be(40.00m); // $100 - $40 - $20 = $40 remaining
        result.Value.Currency.Should().Be("USD");
    }

    #endregion

    #region Helper Methods

    private Payment CreateCompletedPayment(decimal amount)
    {
        return new Payment
        {
            EventRegistrationId = Guid.NewGuid(),
            UserId = _testUserId,
            AmountValue = amount,
            Currency = "USD",
            Status = PaymentStatus.Completed,
            ProcessedAt = DateTime.UtcNow,
            EncryptedPayPalOrderId = "encrypted-paypal-order-id"
        };
    }

    private void SetupSuccessfulPayPalRefund()
    {
        _mockEncryptionService
            .Setup(x => x.DecryptAsync(It.IsAny<string>()))
            .ReturnsAsync("PAYPAL-CAPTURE-123");

        _mockEncryptionService
            .Setup(x => x.EncryptAsync(It.IsAny<string>()))
            .ReturnsAsync("encrypted-refund-id");

        var paypalRefundResponse = new PayPalRefundResponse
        {
            RefundId = "PAYPAL-REFUND-123",
            Status = "COMPLETED",
            CaptureId = "PAYPAL-CAPTURE-123",
            Amount = Money.Create(100.00m, "USD")
        };

        _mockPayPalService
            .Setup(x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PayPalRefundResponse>.Success(paypalRefundResponse));
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
    }
}
