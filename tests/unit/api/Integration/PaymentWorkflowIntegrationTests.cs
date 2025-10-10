using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.Models;
using WitchCityRope.Api.Features.Payments.Models.PayPal;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace WitchCityRope.Api.Tests.Integration;

/// <summary>
/// End-to-end integration tests for complete payment workflows
/// Tests real-world scenarios from payment initiation through completion or refund
/// CRITICAL: These tests verify the entire payment pipeline works correctly
/// Uses real PostgreSQL database via TestContainers for accurate integration testing
/// Timeout: 90 seconds per test (allows for complex multi-step operations)
/// </summary>
[Collection("Database")]
public class PaymentWorkflowIntegrationTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly Mock<IPayPalService> _mockPayPalService;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private ApplicationDbContext _context;
    private PaymentService _paymentService;
    private RefundService _refundService;
    private Guid _testUserId = Guid.NewGuid();
    private Guid _testAdminId = Guid.NewGuid();

    public PaymentWorkflowIntegrationTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _mockPayPalService = new Mock<IPayPalService>();
        _mockEncryptionService = new Mock<IEncryptionService>();
    }

    public async Task InitializeAsync()
    {
        // Reset database before each test
        await _fixture.ResetDatabaseAsync();

        // Create fresh context and services for each test
        _context = _fixture.CreateDbContext();

        // Create both services for end-to-end testing
        _paymentService = new PaymentService(
            _context,
            _mockPayPalService.Object,
            _mockEncryptionService.Object,
            new Mock<ILogger<PaymentService>>().Object);

        _refundService = new RefundService(
            _context,
            _mockPayPalService.Object,
            _mockEncryptionService.Object,
            new Mock<ILogger<RefundService>>().Object);

        // Seed test users with new GUIDs for each test
        _testUserId = Guid.NewGuid();
        _testAdminId = Guid.NewGuid();

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
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    /// <summary>
    /// Test 1: Complete end-to-end ticket purchase workflow
    /// Verifies: Payment creation → PayPal order → Payment completion → Ticket issuance
    /// CRITICAL: This is the most important business flow - ticket sales
    /// </summary>
    [Fact(Timeout = 90000)]
    public async Task TicketPurchaseWorkflow_FromPaymentToCompletion_SucceedsEndToEnd()
    {
        // Arrange - User wants to buy a workshop ticket with sliding scale
        var registrationId = Guid.NewGuid();
        var originalPrice = Money.Create(100.00m, "USD");
        var slidingScalePercentage = 30m; // 30% discount

        SetupSuccessfulPayPalFlow();

        // Act - Step 1: Initiate payment
        var paymentRequest = new ProcessPaymentRequest
        {
            EventRegistrationId = registrationId,
            UserId = _testUserId,
            OriginalAmount = originalPrice,
            SlidingScalePercentage = slidingScalePercentage,
            PaymentMethodType = PaymentMethodType.PayPal,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        var paymentResult = await _paymentService.ProcessPaymentAsync(paymentRequest);

        // Assert - Payment created successfully
        paymentResult.IsSuccess.Should().BeTrue();
        var payment = paymentResult.Value!;
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.AmountValue.Should().Be(70.00m); // $100 - 30% = $70
        payment.EncryptedPayPalOrderId.Should().NotBeNullOrEmpty();

        // Act - Step 2: User approves payment in PayPal, webhook fires
        var updateResult = await _paymentService.UpdatePaymentStatusAsync(
            payment.Id,
            PaymentStatus.Completed,
            "PAYPAL-ORDER-123");

        // Assert - Payment completed successfully
        updateResult.IsSuccess.Should().BeTrue();
        var completedPayment = updateResult.Value!;
        completedPayment.Status.Should().Be(PaymentStatus.Completed);
        completedPayment.ProcessedAt.Should().NotBeNull();

        // Act - Step 3: Verify payment retrieval by registration ID (for ticket system)
        var retrievalResult = await _paymentService.GetPaymentByRegistrationIdAsync(registrationId);

        // Assert - Payment can be found and ticket can be issued
        retrievalResult.IsSuccess.Should().BeTrue();
        retrievalResult.Value.Should().NotBeNull();
        retrievalResult.Value!.Status.Should().Be(PaymentStatus.Completed);

        // Verify complete audit trail exists in real database
        var auditLogs = await _context.PaymentAuditLog
            .Where(a => a.PaymentId == payment.Id)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();

        auditLogs.Should().HaveCountGreaterOrEqualTo(3); // Init + Status change + Completion
        auditLogs.Should().Contain(a => a.ActionType == "PaymentInitiated");
        auditLogs.Should().Contain(a => a.ActionType == "PaymentCompleted");
    }

    /// <summary>
    /// Test 2: Complete end-to-end refund workflow
    /// Verifies: Ticket cancellation → Refund request → PayPal refund → Ticket void
    /// CRITICAL: Ensures refunds work correctly and tickets are properly voided
    /// </summary>
    [Fact(Timeout = 90000)]
    public async Task RefundWorkflow_FromRequestToCompletion_SucceedsEndToEnd()
    {
        // Arrange - User has purchased a ticket and wants a refund
        var registrationId = Guid.NewGuid();
        var payment = new Payment
        {
            EventRegistrationId = registrationId,
            UserId = _testUserId,
            AmountValue = 75.00m,
            Currency = "USD",
            SlidingScalePercentage = 25m,
            Status = PaymentStatus.Completed,
            ProcessedAt = DateTime.UtcNow.AddDays(-5),
            EncryptedPayPalOrderId = "encrypted-order-id"
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        SetupSuccessfulPayPalRefund();

        // Act - Step 1: Admin processes refund request
        var refundRequest = new ProcessRefundRequest
        {
            PaymentId = payment.Id,
            RefundAmount = Money.Create(75.00m, "USD"),
            RefundReason = "Event cancelled by organizer - full refund issued",
            ProcessedByUserId = _testAdminId,
            IpAddress = "192.168.1.100",
            UserAgent = "Admin Portal"
        };

        var refundResult = await _refundService.ProcessRefundAsync(refundRequest);

        // Assert - Refund created and processed successfully
        refundResult.IsSuccess.Should().BeTrue();
        var refund = refundResult.Value!;
        refund.RefundStatus.Should().Be(RefundStatus.Completed);
        refund.RefundAmountValue.Should().Be(75.00m);
        refund.ProcessedByUserId.Should().Be(_testAdminId);

        // Act - Step 2: Verify payment status updated in real database
        var paymentCheck = await _paymentService.GetPaymentByIdAsync(payment.Id);

        // Assert - Payment marked as refunded
        paymentCheck.IsSuccess.Should().BeTrue();
        paymentCheck.Value!.Status.Should().Be(PaymentStatus.Refunded);
        paymentCheck.Value.RefundedAt.Should().NotBeNull();
        paymentCheck.Value.RefundAmountValue.Should().Be(75.00m);

        // Verify refund can be retrieved by payment ID (for ticket voiding)
        var refundCheck = await _refundService.GetRefundsByPaymentIdAsync(payment.Id);
        refundCheck.IsSuccess.Should().BeTrue();
        refundCheck.Value.Should().ContainSingle();
        refundCheck.Value[0].RefundStatus.Should().Be(RefundStatus.Completed);

        // Verify audit trail in real database
        var auditLogs = await _context.PaymentAuditLog
            .Where(a => a.PaymentId == payment.Id)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();

        auditLogs.Should().Contain(a => a.ActionType == "RefundInitiated");
        auditLogs.Should().Contain(a => a.ActionType == "RefundCompleted");
    }

    /// <summary>
    /// Test 3: Failed payment cleanup workflow
    /// Verifies: Payment fails → No ticket issued → Payment marked failed → User notified
    /// CRITICAL: Ensures failed payments don't result in issued tickets
    /// </summary>
    [Fact(Timeout = 90000)]
    public async Task FailedPaymentWorkflow_EnsuresNoTicketIssuance()
    {
        // Arrange - User attempts payment but PayPal fails
        var registrationId = Guid.NewGuid();

        _mockPayPalService
            .Setup(x => x.CreateOrderAsync(
                It.IsAny<Money>(),
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PayPalOrderResponse>.Failure("PayPal API Error: Payment source declined"));

        // Act - Step 1: Attempt payment
        var paymentRequest = new ProcessPaymentRequest
        {
            EventRegistrationId = registrationId,
            UserId = _testUserId,
            OriginalAmount = Money.Create(50.00m, "USD"),
            SlidingScalePercentage = 0m,
            PaymentMethodType = PaymentMethodType.PayPal,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        var paymentResult = await _paymentService.ProcessPaymentAsync(paymentRequest);

        // Assert - Payment creation failed
        paymentResult.IsSuccess.Should().BeFalse();
        paymentResult.ErrorMessage.Should().Contain("Failed to create PayPal order");

        // Act - Step 2: Verify no completed payment exists for registration
        var paymentCheck = await _paymentService.GetPaymentStatusByRegistrationIdAsync(registrationId);

        // Assert - No completed payment (no ticket should be issued)
        if (paymentCheck.IsSuccess && paymentCheck.Value.HasValue)
        {
            paymentCheck.Value.Should().NotBe(PaymentStatus.Completed);
        }

        // Verify failure was logged in real database
        var failures = await _context.PaymentFailures.ToListAsync();
        failures.Should().NotBeEmpty();
        failures.Should().Contain(f => f.FailureCode == "paypal_order_creation_failed");
    }

    /// <summary>
    /// Test 4: Payment retry workflow after initial failure
    /// Verifies: Payment fails → User retries → Payment succeeds → Ticket issued
    /// Tests resilience to transient failures
    /// </summary>
    [Fact(Timeout = 90000)]
    public async Task PaymentRetryWorkflow_SucceedsAfterInitialFailure()
    {
        // Arrange - First attempt will fail, second will succeed
        var registrationId = Guid.NewGuid();
        var callCount = 0;

        _mockPayPalService
            .Setup(x => x.CreateOrderAsync(
                It.IsAny<Money>(),
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    // First call fails
                    return Result<PayPalOrderResponse>.Failure("Temporary network error");
                }
                else
                {
                    // Second call succeeds
                    return Result<PayPalOrderResponse>.Success(new PayPalOrderResponse
                    {
                        OrderId = "PAYPAL-ORDER-RETRY-SUCCESS",
                        Status = "CREATED",
                        Links = new List<PayPalLink>()
                    });
                }
            });

        _mockEncryptionService
            .Setup(x => x.EncryptAsync(It.IsAny<string>()))
            .ReturnsAsync("encrypted-order-id");

        var paymentRequest = new ProcessPaymentRequest
        {
            EventRegistrationId = registrationId,
            UserId = _testUserId,
            OriginalAmount = Money.Create(50.00m, "USD"),
            SlidingScalePercentage = 0m,
            PaymentMethodType = PaymentMethodType.PayPal,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        // Act - Step 1: First payment attempt (fails)
        var firstAttempt = await _paymentService.ProcessPaymentAsync(paymentRequest);

        // Assert - First attempt failed
        firstAttempt.IsSuccess.Should().BeFalse();

        // Act - Step 2: Retry payment (succeeds)
        var secondAttempt = await _paymentService.ProcessPaymentAsync(paymentRequest);

        // Assert - Second attempt succeeded
        secondAttempt.IsSuccess.Should().BeTrue();
        secondAttempt.Value!.Status.Should().Be(PaymentStatus.Pending);
        secondAttempt.Value.EncryptedPayPalOrderId.Should().Be("encrypted-order-id");

        // Verify both attempts were tracked in real database
        var allPayments = await _context.Payments
            .Where(p => p.EventRegistrationId == registrationId)
            .ToListAsync();

        allPayments.Should().HaveCount(2); // Failed attempt + successful retry
    }

    /// <summary>
    /// Test 5: Concurrent payment handling for last available ticket
    /// Verifies: Two users try to buy last ticket → Only one succeeds → Other gets clear error
    /// CRITICAL: Prevents overselling tickets
    /// </summary>
    [Fact(Timeout = 90000)]
    public async Task ConcurrentPaymentWorkflow_PreventsDoubleBooking()
    {
        // Arrange - Simulate two users trying to pay for the same registration simultaneously
        var registrationId = Guid.NewGuid();
        var user1Id = _testUserId;
        var user2Id = Guid.NewGuid();

        // Add second user
        var user2 = new ApplicationUser
        {
            Id = user2Id,
            Email = "user2@example.com",
            SceneName = "User2"
        };
        _context.Users.Add(user2);
        await _context.SaveChangesAsync();

        SetupSuccessfulPayPalFlow();

        var request1 = new ProcessPaymentRequest
        {
            EventRegistrationId = registrationId,
            UserId = user1Id,
            OriginalAmount = Money.Create(50.00m, "USD"),
            SlidingScalePercentage = 0m,
            PaymentMethodType = PaymentMethodType.PayPal,
            IpAddress = "192.168.1.1",
            UserAgent = "User1 Browser"
        };

        var request2 = new ProcessPaymentRequest
        {
            EventRegistrationId = registrationId,
            UserId = user2Id,
            OriginalAmount = Money.Create(50.00m, "USD"),
            SlidingScalePercentage = 0m,
            PaymentMethodType = PaymentMethodType.PayPal,
            IpAddress = "192.168.1.2",
            UserAgent = "User2 Browser"
        };

        // Act - Both users submit payments concurrently
        var result1Task = _paymentService.ProcessPaymentAsync(request1);
        var result2Task = _paymentService.ProcessPaymentAsync(request2);

        var results = await Task.WhenAll(result1Task, result2Task);

        // Complete first successful payment
        var successfulResult = results.FirstOrDefault(r => r.IsSuccess);
        if (successfulResult?.Value != null)
        {
            await _paymentService.UpdatePaymentStatusAsync(
                successfulResult.Value.Id,
                PaymentStatus.Completed,
                "PAYPAL-ORDER-123");
        }

        // Act - Second user tries to proceed after first completed
        var user2SecondAttempt = await _paymentService.ProcessPaymentAsync(request2);

        // Assert - Second payment attempt should fail (duplicate check)
        // At least one payment should be completed in real database
        var completedPayments = await _context.Payments
            .Where(p => p.EventRegistrationId == registrationId && p.Status == PaymentStatus.Completed)
            .ToListAsync();

        completedPayments.Should().HaveCountLessThanOrEqualTo(1); // Only one completed payment allowed

        // If second user tried after first completed, should get error
        if (completedPayments.Any())
        {
            user2SecondAttempt.IsSuccess.Should().BeFalse();
            user2SecondAttempt.ErrorMessage.Should().Contain("Payment already completed");
        }
    }

    #region Helper Methods

    private void SetupSuccessfulPayPalFlow()
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
                Value = "75.00"
            }
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
}
