using WitchCityRope.Api.Features.Shared.Models;
using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.Models;
using WitchCityRope.Api.Features.Payments.Models.PayPal;
using WitchCityRope.Api.Features.Payments.Models.AuthorizeNet;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Features.Shared.Services;
using WitchCityRope.Api.Features.Volunteers.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace WitchCityRope.Api.Tests.Services;

/// <summary>
/// Comprehensive test suite for RefundService
/// Tests refund processing, validation, partial refunds, and audit trails
/// CRITICAL: Refund errors can lead to financial loss and legal issues
/// Uses real PostgreSQL database via TestContainers for accurate testing
/// UPDATED: Migrated from Payment model to TicketPurchase model (2026-03-07)
/// </summary>
[Collection("Database")]
public class RefundServiceTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly Mock<IPayPalService> _mockPayPalService;
    private readonly Mock<IAuthorizeNetService> _mockAuthorizeNetService;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly Mock<IVolunteerAssignmentService> _mockVolunteerService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<RefundService>> _mockLogger;
    private RefundService _sut; // System Under Test
    private ApplicationDbContext _context;
    private Guid _testUserId = Guid.NewGuid();
    private Guid _testAdminId = Guid.NewGuid();

    public RefundServiceTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _mockPayPalService = new Mock<IPayPalService>();
        _mockAuthorizeNetService = new Mock<IAuthorizeNetService>();
        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockVolunteerService = new Mock<IVolunteerAssignmentService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<RefundService>>();
    }

    public async Task InitializeAsync()
    {
        // Reset database before each test
        await _fixture.ResetDatabaseAsync();

        // Create fresh context and service for each test
        _context = _fixture.CreateDbContext();
        _sut = new RefundService(
            _context,
            _mockPayPalService.Object,
            _mockEncryptionService.Object,
            _mockVolunteerService.Object,
            _mockEmailService.Object,
            _mockLogger.Object,
            _mockAuthorizeNetService.Object);

        // Seed test users with new GUIDs for each test
        _testUserId = Guid.NewGuid();
        _testAdminId = Guid.NewGuid();

        var testUser = new ApplicationUser
        {
            Id = _testUserId,
            Email = "test@example.com",
            SceneName = "TestUser",
            EncryptedLegalName = "Encrypted Test User",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            Role = "",
            PronouncedName = "Test User",
            Pronouns = "they/them",
            EmailVerificationToken = Guid.NewGuid().ToString()
        };

        var adminUser = new ApplicationUser
        {
            Id = _testAdminId,
            Email = "admin@example.com",
            SceneName = "AdminUser",
            EncryptedLegalName = "Encrypted Admin User",
            DateOfBirth = DateTime.UtcNow.AddYears(-35),
            Role = "Administrator",
            PronouncedName = "Admin User",
            Pronouns = "they/them",
            EmailVerificationToken = Guid.NewGuid().ToString()
        };

        _context.Users.AddRange(testUser, adminUser);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
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
        var ticketPurchase = await CreateCompletedTicketPurchaseWithEntities(100.00m);

        var request = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
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
        result.Value.TicketPurchaseId.Should().Be(ticketPurchase.Id);
        result.Value.ProcessedByUserId.Should().Be(_testAdminId);
    }

    /// <summary>
    /// M2c regression guard (2026-04-12): a successful refund via ProcessRefundAsync
    /// MUST centrally sync TicketPurchase.PaymentStatus to Refunded when the refund
    /// covers the full ticket price, without the caller having to do it.
    /// This is the behavior that makes the user-cancel path (ProcessAutomaticRefundAsync
    /// in AttendanceService) correct for free. Before the M2c fix, PaymentStatus
    /// would stay "Completed" for user-initiated cancellations because only
    /// ProcessVariableRefund knew to update it. See BE-12/BE-14 in /docs/technical-debt.md.
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_WithValidFullRefund_SyncsPaymentStatusToRefunded()
    {
        // Arrange
        var ticketPurchase = await CreateCompletedTicketPurchaseWithEntities(100.00m);

        var request = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
            RefundAmount = Money.Create(100.00m, "USD"),
            RefundReason = "User-initiated cancellation (regression test for M2c sync)",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        SetupSuccessfulPayPalRefund();

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.RefundStatus.Should().Be(RefundStatus.Completed);

        // Critical assertion: PaymentStatus must be synced to Refunded automatically
        // by RefundService — no explicit action by the caller.
        await using var verifyContext = _fixture.CreateDbContext();
        var updated = await verifyContext.TicketPurchases
            .FirstOrDefaultAsync(tp => tp.Id == ticketPurchase.Id);
        updated.Should().NotBeNull();
        updated!.PaymentStatus.Should().Be(TicketPurchasePaymentStatus.Refunded,
            "a full refund through ProcessRefundAsync must auto-sync PaymentStatus to Refunded " +
            "via SyncPaymentStatusFromRefundsAsync (M2c)");
    }

    /// <summary>
    /// M2c regression guard (2026-04-12): partial refund variant. TicketPurchase.PaymentStatus
    /// must transition to PartiallyRefunded when the refund is less than the full price.
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_WithValidPartialRefund_SyncsPaymentStatusToPartiallyRefunded()
    {
        // Arrange
        var ticketPurchase = await CreateCompletedTicketPurchaseWithEntities(100.00m);

        var request = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
            RefundAmount = Money.Create(40.00m, "USD"), // partial: 40 of 100
            RefundReason = "Partial user-initiated cancellation (regression test for M2c sync)",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        SetupSuccessfulPayPalRefund();

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.RefundStatus.Should().Be(RefundStatus.Completed);

        await using var verifyContext = _fixture.CreateDbContext();
        var updated = await verifyContext.TicketPurchases
            .FirstOrDefaultAsync(tp => tp.Id == ticketPurchase.Id);
        updated.Should().NotBeNull();
        updated!.PaymentStatus.Should().Be(TicketPurchasePaymentStatus.PartiallyRefunded,
            "a partial refund must sync PaymentStatus to PartiallyRefunded via M2c");
    }

    /// <summary>
    /// Test 2: Verify successful partial refund processing
    /// Tests that partial refunds are created correctly
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_WithValidPartialRefund_CreatesPartialRefundRecord()
    {
        // Arrange
        var ticketPurchase = await CreateCompletedTicketPurchaseWithEntities(100.00m);

        var request = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
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
        // Arrange - Create full entity chain for FK constraints
        var ticketType = await CreateTicketTypeWithDependencies();
        var ticketPurchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            TicketTypeId = ticketType.Id,
            UserId = _testUserId,
            TotalPrice = 100.00m,
            PaymentStatus = TicketPurchasePaymentStatus.Pending, // Not eligible for refund
            PaymentMethod = "PayPal",
            EncryptedPayPalOrderId = "encrypted-order-id"
        };
        _context.TicketPurchases.Add(ticketPurchase);
        await _context.SaveChangesAsync();

        var request = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
            RefundAmount = Money.Create(100.00m, "USD"),
            RefundReason = "Customer request for pending payment",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not eligible for refund");
    }

    /// <summary>
    /// Test 4: Verify refund fails when amount exceeds maximum available
    /// Prevents over-refunding
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_WithAmountExceedingAvailable_ReturnsFailure()
    {
        // Arrange
        var ticketPurchase = await CreateCompletedTicketPurchaseWithEntities(100.00m);

        var request = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
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
    }

    /// <summary>
    /// Test 5: Verify refund fails with insufficient reason length
    /// Ensures proper audit trail documentation
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_WithShortRefundReason_ReturnsFailure()
    {
        // Arrange
        var ticketPurchase = await CreateCompletedTicketPurchaseWithEntities(100.00m);

        var request = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
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
        var ticketPurchase = await CreateCompletedTicketPurchaseWithEntities(100.00m);

        // Add existing partial refund of $60
        var existingRefund = new PaymentRefund
        {
            TicketPurchaseId = ticketPurchase.Id,
            RefundAmountValue = 60.00m,
            RefundCurrency = "USD",
            RefundStatus = RefundStatus.Completed,
            ProcessedByUserId = _testAdminId,
            RefundReason = "First partial refund"
        };

        // Update ticket purchase status
        ticketPurchase.PaymentStatus = TicketPurchasePaymentStatus.PartiallyRefunded;

        // Save payment changes first
        await _context.SaveChangesAsync();

        // Add refund to database
        _context.PaymentRefunds.Add(existingRefund);
        await _context.SaveChangesAsync();

        // Try to refund $50 more (total would be $110, exceeding $100)
        var request = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
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
        var ticketPurchase = await CreateCompletedTicketPurchaseWithEntities(100.00m);

        var request = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
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
        var ticketPurchase = await CreateCompletedTicketPurchaseWithEntities(100.00m);

        var request = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
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

    #region Authorize.net Integration Tests

    /// <summary>
    /// Authorize.net refund success path (2026-05-16): when IAuthorizeNetService.RefundAsync
    /// returns Success with a non-empty TransactionId, the persisted PaymentRefund row must
    /// carry the encrypted Authorize.net refund transaction id, WasVoided, and an
    /// authnet_response metadata entry — mirroring how PayPal stores EncryptedPayPalRefundId
    /// + Metadata["paypal_response"].
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_WithSuccessfulAuthNetRefund_PersistsEncryptedTransactionIdAndMetadata()
    {
        // Arrange
        var ticketPurchase = await CreateCompletedAuthNetTicketPurchaseWithEntities(100.00m);

        var request = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
            RefundAmount = Money.Create(100.00m, "USD"),
            RefundReason = "Customer requested refund via Authorize.net card",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        SetupSuccessfulAuthNetRefund(transactionId: "AUTHNET-REFUND-789", wasVoided: false);

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.RefundStatus.Should().Be(RefundStatus.Completed);

        await using var verifyContext = _fixture.CreateDbContext();
        var persisted = await verifyContext.PaymentRefunds
            .FirstOrDefaultAsync(r => r.Id == result.Value.Id);

        persisted.Should().NotBeNull();
        persisted!.EncryptedAuthNetRefundTransactionId.Should().NotBeNullOrEmpty(
            "a successful Authorize.net refund with a transaction id must persist the encrypted id");
        persisted.EncryptedAuthNetRefundTransactionId.Should().Be("enc:AUTHNET-REFUND-789",
            "the deterministic mock encryptor prefixes plaintext with 'enc:'");
        persisted.WasVoided.Should().BeFalse();
        persisted.Metadata.Should().ContainKey("authnet_response");
    }

    /// <summary>
    /// Authorize.net void path (2026-05-16): when the processor reversed an unsettled
    /// charge, AuthorizeNetRefundResponse.WasVoided is true and that flag must be
    /// persisted on the PaymentRefund row.
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_WithVoidedAuthNetRefund_PersistsWasVoidedTrue()
    {
        // Arrange
        var ticketPurchase = await CreateCompletedAuthNetTicketPurchaseWithEntities(75.00m);

        var request = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
            RefundAmount = Money.Create(75.00m, "USD"),
            RefundReason = "Customer requested refund - charge was still unsettled",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        SetupSuccessfulAuthNetRefund(transactionId: "AUTHNET-VOID-456", wasVoided: true);

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.RefundStatus.Should().Be(RefundStatus.Completed);

        await using var verifyContext = _fixture.CreateDbContext();
        var persisted = await verifyContext.PaymentRefunds
            .FirstOrDefaultAsync(r => r.Id == result.Value.Id);

        persisted.Should().NotBeNull();
        persisted!.WasVoided.Should().BeTrue(
            "WasVoided from the processor response must be persisted on the refund row");
    }

    /// <summary>
    /// Authorize.net edge case (2026-05-16): a success response with a null/empty
    /// TransactionId must NOT attempt to encrypt an empty string — the encrypted column
    /// stays null and the refund still completes without throwing.
    /// </summary>
    [Fact]
    public async Task ProcessRefundAsync_WithSuccessfulAuthNetRefundButEmptyTransactionId_LeavesEncryptedIdNull()
    {
        // Arrange
        var ticketPurchase = await CreateCompletedAuthNetTicketPurchaseWithEntities(50.00m);

        var request = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
            RefundAmount = Money.Create(50.00m, "USD"),
            RefundReason = "Refund with processor returning no transaction id",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.1"
        };

        SetupSuccessfulAuthNetRefund(transactionId: null, wasVoided: false);

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.RefundStatus.Should().Be(RefundStatus.Completed);

        await using var verifyContext = _fixture.CreateDbContext();
        var persisted = await verifyContext.PaymentRefunds
            .FirstOrDefaultAsync(r => r.Id == result.Value.Id);

        persisted.Should().NotBeNull();
        persisted!.EncryptedAuthNetRefundTransactionId.Should().BeNull(
            "an empty processor transaction id must not be encrypted/persisted");

        // EncryptAsync must never be invoked with an empty/null transaction id
        _mockEncryptionService.Verify(
            x => x.EncryptAsync(It.Is<string>(s => string.IsNullOrEmpty(s))),
            Times.Never);
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
        var ticketPurchase = await CreateCompletedTicketPurchaseWithEntities(100.00m);

        var refund1 = new PaymentRefund
        {
            TicketPurchaseId = ticketPurchase.Id,
            RefundAmountValue = 40.00m,
            RefundCurrency = "USD",
            RefundStatus = RefundStatus.Completed,
            ProcessedByUserId = _testAdminId,
            RefundReason = "First partial refund"
        };

        var refund2 = new PaymentRefund
        {
            TicketPurchaseId = ticketPurchase.Id,
            RefundAmountValue = 30.00m,
            RefundCurrency = "USD",
            RefundStatus = RefundStatus.Completed,
            ProcessedByUserId = _testAdminId,
            RefundReason = "Second partial refund"
        };

        _context.PaymentRefunds.AddRange(refund1, refund2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetRefundsByPaymentIdAsync(ticketPurchase.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(r => r.TicketPurchaseId == ticketPurchase.Id);
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
        var ticketPurchase = await CreateCompletedTicketPurchaseWithEntities(100.00m);

        // Add completed refunds totaling $60
        var refund1 = new PaymentRefund
        {
            TicketPurchaseId = ticketPurchase.Id,
            RefundAmountValue = 40.00m,
            RefundCurrency = "USD",
            RefundStatus = RefundStatus.Completed,
            ProcessedByUserId = _testAdminId,
            RefundReason = "First partial refund"
        };

        var refund2 = new PaymentRefund
        {
            TicketPurchaseId = ticketPurchase.Id,
            RefundAmountValue = 20.00m,
            RefundCurrency = "USD",
            RefundStatus = RefundStatus.Completed,
            ProcessedByUserId = _testAdminId,
            RefundReason = "Second partial refund"
        };

        // Add failed refund (should not count)
        var failedRefund = new PaymentRefund
        {
            TicketPurchaseId = ticketPurchase.Id,
            RefundAmountValue = 10.00m,
            RefundCurrency = "USD",
            RefundStatus = RefundStatus.Failed,
            ProcessedByUserId = _testAdminId,
            RefundReason = "Failed refund attempt"
        };

        // Add refunds to database (not just the collection)
        _context.PaymentRefunds.AddRange(refund1, refund2, failedRefund);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetMaximumRefundAmountAsync(ticketPurchase.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Amount.Should().Be(40.00m); // $100 - $40 - $20 = $40 remaining
        result.Value.Currency.Should().Be("USD");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a completed ticket purchase with all required foreign key relationships
    /// CRITICAL: Always creates User entity to satisfy database constraints
    /// </summary>
    private async Task<TicketPurchase> CreateCompletedTicketPurchaseWithEntities(decimal amount)
    {
        // Create User entity (required foreign key)
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = $"payment-user-{Guid.NewGuid()}@example.com",
            SceneName = $"PaymentUser{Guid.NewGuid().ToString().Substring(0, 8)}",
            EncryptedLegalName = "Encrypted Legal Name",
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            Role = "",
            PronouncedName = "Test User",
            Pronouns = "they/them",
            EmailVerificationToken = Guid.NewGuid().ToString()
        };
        _context.Users.Add(user);

        // Create Venue → Event → TicketType chain (required FKs)
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
            Title = "Refund Test Event",
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
            Price = amount,
            Available = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TicketTypes.Add(ticketType);

        // Create TicketPurchase with proper foreign keys
        var ticketPurchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            TicketTypeId = ticketType.Id,
            UserId = user.Id,
            TotalPrice = amount,
            PaymentStatus = TicketPurchasePaymentStatus.Completed,
            PaymentMethod = "PayPal",
            ProcessedAt = DateTime.UtcNow,
            EncryptedPayPalOrderId = "encrypted-paypal-order-id",
            EncryptedPayPalCaptureId = "encrypted-paypal-capture-id"
        };
        _context.TicketPurchases.Add(ticketPurchase);

        // Save all entities together
        await _context.SaveChangesAsync();

        return ticketPurchase;
    }

    /// <summary>
    /// Creates a TicketType with all required FK dependencies (Venue → Event → TicketType)
    /// </summary>
    private async Task<TicketType> CreateTicketTypeWithDependencies(decimal price = 100.00m)
    {
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
            Title = "Refund Test Event",
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
            Price = price,
            Available = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TicketTypes.Add(ticketType);
        await _context.SaveChangesAsync();

        return ticketType;
    }

    /// <summary>
    /// Creates a completed ticket purchase paid via Authorize.net (credit card).
    /// Unlike the PayPal helper, this leaves the PayPal capture/order ids null and
    /// sets EncryptedAuthNetTransactionId so ProcessRefundAsync takes the Authorize.net branch.
    /// </summary>
    private async Task<TicketPurchase> CreateCompletedAuthNetTicketPurchaseWithEntities(decimal amount)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = $"authnet-user-{Guid.NewGuid()}@example.com",
            SceneName = $"AuthNetUser{Guid.NewGuid().ToString().Substring(0, 8)}",
            EncryptedLegalName = "Encrypted Legal Name",
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            Role = "",
            PronouncedName = "Test User",
            Pronouns = "they/them",
            EmailVerificationToken = Guid.NewGuid().ToString()
        };
        _context.Users.Add(user);

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
            Title = "Authorize.net Refund Test Event",
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
            Price = amount,
            Available = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TicketTypes.Add(ticketType);

        var ticketPurchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            TicketTypeId = ticketType.Id,
            UserId = user.Id,
            TotalPrice = amount,
            PaymentStatus = TicketPurchasePaymentStatus.Completed,
            PaymentMethod = "CreditCard",
            ProcessedAt = DateTime.UtcNow,
            // No PayPal ids → forces the Authorize.net refund branch
            EncryptedAuthNetTransactionId = "encrypted-authnet-transaction-id",
            CreditCardLastFour = "4242",
            CreditCardType = "Visa"
        };
        _context.TicketPurchases.Add(ticketPurchase);

        await _context.SaveChangesAsync();

        return ticketPurchase;
    }

    /// <summary>
    /// Configures the encryption + Authorize.net mocks for a successful refund.
    /// EncryptAsync is deterministic (enc:{plaintext}) so the persisted encrypted id is assertable.
    /// </summary>
    private void SetupSuccessfulAuthNetRefund(string? transactionId, bool wasVoided)
    {
        // Decrypt the stored ticket transaction id (input to RefundAsync)
        _mockEncryptionService
            .Setup(x => x.DecryptAsync(It.IsAny<string>()))
            .ReturnsAsync((string s) => s.StartsWith("enc:") ? s.Substring(4) : s);

        // Deterministic encryption so the persisted refund transaction id is verifiable
        _mockEncryptionService
            .Setup(x => x.EncryptAsync(It.IsAny<string>()))
            .ReturnsAsync((string s) => $"enc:{s}");

        _mockAuthorizeNetService
            .Setup(x => x.RefundAsync(
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(new AuthorizeNetRefundResponse
            {
                Success = true,
                TransactionId = transactionId,
                ResponseCode = "1",
                Message = "Refund processed",
                WasVoided = wasVoided
            });
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
    }

    #endregion
}
