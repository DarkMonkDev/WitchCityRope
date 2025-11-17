using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.Models.PayPal;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using Result = WitchCityRope.Api.Features.Shared.Models.Result;

namespace WitchCityRope.Api.Tests.Services;

/// <summary>
/// Unit tests for RefundRetryService - Phase 2 PayPal Refund Enhancement
/// Tests exponential backoff retry logic, error classification, and idempotency key reuse
/// CRITICAL: Prevents duplicate refunds and ensures transient failure recovery
/// </summary>
public class RefundRetryServiceTests
{
    private readonly Mock<IPayPalService> _mockPayPalService;
    private readonly Mock<ILogger<RefundRetryService>> _mockLogger;
    private readonly RefundRetryService _sut;

    public RefundRetryServiceTests()
    {
        _mockPayPalService = new Mock<IPayPalService>();
        _mockLogger = new Mock<ILogger<RefundRetryService>>();

        _sut = new RefundRetryService(
            _mockPayPalService.Object,
            _mockLogger.Object);
    }

    #region Successful Refund Tests

    /// <summary>
    /// Test 1: Successful refund on first attempt (no retries needed)
    /// Tests happy path with immediate success
    /// </summary>
    [Fact]
    public async Task RefundWithRetryAsync_SuccessOnFirstAttempt_ReturnsSuccess()
    {
        // Arrange
        var captureId = "CAPTURE-123";
        var amount = Money.Create(50.00m, "USD");
        var reason = "Customer request";
        var idempotencyKey = "WCR-abc123";

        var successResponse = new PayPalRefundResponse
        {
            RefundId = "REFUND-123",
            Status = "COMPLETED",
            Amount = new PayPalAmount { CurrencyCode = "USD", Value = "50.00" }
        };

        _mockPayPalService
            .Setup(x => x.RefundCaptureAsync(
                captureId,
                amount,
                reason,
                idempotencyKey,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PayPalRefundResponse>.Success(successResponse));

        // Act
        var result = await _sut.RefundWithRetryAsync(captureId, amount, reason, idempotencyKey);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.RefundId.Should().Be("REFUND-123");

        // Verify PayPal service called exactly once (no retries)
        _mockPayPalService.Verify(
            x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Retry on 5xx Error Tests

    /// <summary>
    /// Test 2: Retry on 5xx server error with exponential backoff
    /// Tests automatic retry for transient server errors
    /// </summary>
    [Fact]
    public async Task RefundWithRetryAsync_On5xxError_RetriesWithExponentialBackoff()
    {
        // Arrange
        var captureId = "CAPTURE-123";
        var amount = Money.Create(100.00m, "USD");
        var reason = "Refund with retry";
        var idempotencyKey = "WCR-retry-test";

        var retryCount = 0;
        var retryCallback = new Mock<Func<int, string, Task>>();
        retryCallback
            .Setup(x => x.Invoke(It.IsAny<int>(), It.IsAny<string>()))
            .Callback<int, string>((count, error) => retryCount = count)
            .Returns(Task.CompletedTask);

        // First 2 attempts fail with 503 Service Unavailable
        _mockPayPalService
            .SetupSequence(x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service unavailable", null, HttpStatusCode.ServiceUnavailable))
            .ThrowsAsync(new HttpRequestException("Service unavailable", null, HttpStatusCode.ServiceUnavailable))
            .ReturnsAsync(Result<PayPalRefundResponse>.Success(new PayPalRefundResponse
            {
                RefundId = "REFUND-RETRY-SUCCESS",
                Status = "COMPLETED",
                Amount = new PayPalAmount { CurrencyCode = "USD", Value = "100.00" }
            }));

        // Act
        var result = await _sut.RefundWithRetryAsync(
            captureId,
            amount,
            reason,
            idempotencyKey,
            retryCallback.Object);

        // Assert
        result.IsSuccess.Should().BeTrue("Should succeed after retries");
        result.Value!.RefundId.Should().Be("REFUND-RETRY-SUCCESS");

        // Verify retry callback was invoked twice (for 2 retries)
        retryCallback.Verify(
            x => x.Invoke(It.IsAny<int>(), It.IsAny<string>()),
            Times.Exactly(2),
            "Retry callback should be invoked for each retry");

        // Verify total attempts (1 original + 2 retries = 3 attempts)
        _mockPayPalService.Verify(
            x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    #endregion

    #region Retry on Network Error Tests

    /// <summary>
    /// Test 3: Retry on network error (SocketException)
    /// Tests retry for connection failures
    /// </summary>
    [Fact]
    public async Task RefundWithRetryAsync_OnSocketException_Retries()
    {
        // Arrange
        var captureId = "CAPTURE-123";
        var amount = Money.Create(75.00m, "USD");
        var reason = "Network error test";
        var idempotencyKey = "WCR-network-test";

        var socketException = new SocketException((int)SocketError.HostUnreachable);
        var httpException = new HttpRequestException("Connection failed", socketException);

        // First attempt fails with network error, second succeeds
        _mockPayPalService
            .SetupSequence(x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(httpException)
            .ReturnsAsync(Result<PayPalRefundResponse>.Success(new PayPalRefundResponse
            {
                RefundId = "REFUND-AFTER-NETWORK-ERROR",
                Status = "COMPLETED",
                Amount = new PayPalAmount { CurrencyCode = "USD", Value = "75.00" }
            }));

        // Act
        var result = await _sut.RefundWithRetryAsync(captureId, amount, reason, idempotencyKey);

        // Assert
        result.IsSuccess.Should().BeTrue("Should succeed after network error retry");
        result.Value!.RefundId.Should().Be("REFUND-AFTER-NETWORK-ERROR");

        // Verify 2 attempts (1 original + 1 retry)
        _mockPayPalService.Verify(
            x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    /// <summary>
    /// Test 4: Retry on TaskCanceledException (timeout)
    /// Tests retry for request timeouts
    /// </summary>
    [Fact]
    public async Task RefundWithRetryAsync_OnTimeout_Retries()
    {
        // Arrange
        var captureId = "CAPTURE-123";
        var amount = Money.Create(60.00m, "USD");
        var reason = "Timeout test";
        var idempotencyKey = "WCR-timeout-test";

        // First attempt times out, second succeeds
        _mockPayPalService
            .SetupSequence(x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("Request timeout"))
            .ReturnsAsync(Result<PayPalRefundResponse>.Success(new PayPalRefundResponse
            {
                RefundId = "REFUND-AFTER-TIMEOUT",
                Status = "COMPLETED",
                Amount = new PayPalAmount { CurrencyCode = "USD", Value = "60.00" }
            }));

        // Act
        var result = await _sut.RefundWithRetryAsync(captureId, amount, reason, idempotencyKey);

        // Assert
        result.IsSuccess.Should().BeTrue("Should succeed after timeout retry");

        // Verify 2 attempts
        _mockPayPalService.Verify(
            x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    #endregion

    #region No Retry on 4xx Error Tests

    /// <summary>
    /// Test 5: No retry on 4xx client error (immediate failure)
    /// CRITICAL: Prevents infinite retries on non-retryable errors
    /// </summary>
    [Fact]
    public async Task RefundWithRetryAsync_On4xxError_FailsImmediately()
    {
        // Arrange
        var captureId = "CAPTURE-123";
        var amount = Money.Create(100.00m, "USD");
        var reason = "4xx error test";
        var idempotencyKey = "WCR-4xx-test";

        // Setup 400 Bad Request error (non-retryable)
        _mockPayPalService
            .Setup(x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Bad request", null, HttpStatusCode.BadRequest));

        // Act
        var result = await _sut.RefundWithRetryAsync(captureId, amount, reason, idempotencyKey);

        // Assert
        result.IsSuccess.Should().BeFalse("4xx errors should fail immediately");
        result.ErrorMessage.Should().Contain("client error", "Error message should indicate client error");

        // Verify only 1 attempt (no retries for 4xx)
        _mockPayPalService.Verify(
            x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "4xx errors should not be retried");
    }

    #endregion

    #region Max Retries Tests

    /// <summary>
    /// Test 6: Max retries (5) enforced, throws MaxRetriesExceededException
    /// Tests retry limit enforcement
    /// </summary>
    [Fact]
    public async Task RefundWithRetryAsync_AfterMaxRetries_ThrowsMaxRetriesExceededException()
    {
        // Arrange
        var captureId = "CAPTURE-123";
        var amount = Money.Create(100.00m, "USD");
        var reason = "Max retries test";
        var idempotencyKey = "WCR-max-retries-test";

        // All attempts fail with 503 (retryable error)
        _mockPayPalService
            .Setup(x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service unavailable", null, HttpStatusCode.ServiceUnavailable));

        // Act & Assert
        await FluentActions.Awaiting(async () =>
            await _sut.RefundWithRetryAsync(captureId, amount, reason, idempotencyKey))
            .Should().ThrowAsync<MaxRetriesExceededException>("Max retries should throw exception");

        // Verify 6 attempts (1 original + 5 retries)
        _mockPayPalService.Verify(
            x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(6),
            "Should attempt 1 original + 5 retries before throwing");
    }

    #endregion

    #region Idempotency Key Reuse Tests

    /// <summary>
    /// Test 7: Idempotency key reused across retries (CRITICAL!)
    /// Tests the same idempotency key is passed to all retry attempts
    /// </summary>
    [Fact]
    public async Task RefundWithRetryAsync_ReusesIdempotencyKey_AcrossRetries()
    {
        // Arrange
        var captureId = "CAPTURE-123";
        var amount = Money.Create(100.00m, "USD");
        var reason = "Idempotency test";
        var idempotencyKey = "WCR-idempotency-test-abc123";

        var capturedKeys = new List<string>();
        var attemptCount = 0;

        // Capture idempotency key on each attempt AND control success/failure
        _mockPayPalService
            .Setup(x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Money, string, string, string, CancellationToken>(
                (capture, amt, rsn, key, note, ct) =>
                {
                    capturedKeys.Add(key);
                    attemptCount++;
                })
            .ReturnsAsync(() =>
            {
                // Fail first 2 attempts, succeed on 3rd
                if (attemptCount <= 2)
                {
                    throw new HttpRequestException("Service unavailable", null, HttpStatusCode.ServiceUnavailable);
                }

                return Result<PayPalRefundResponse>.Success(new PayPalRefundResponse
                {
                    RefundId = "REFUND-SUCCESS",
                    Status = "COMPLETED",
                    Amount = new PayPalAmount { CurrencyCode = "USD", Value = "100.00" }
                });
            });

        // Act
        var result = await _sut.RefundWithRetryAsync(captureId, amount, reason, idempotencyKey);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify same idempotency key used on all 3 attempts
        capturedKeys.Should().HaveCount(3, "Should have 3 attempts");
        capturedKeys.Should().AllBe(idempotencyKey, "All attempts must use the SAME idempotency key");
    }

    #endregion

    #region Exponential Backoff Tests

    /// <summary>
    /// Test 8: Exponential backoff timing (2s, 4s, 8s, 16s, 32s)
    /// Tests backoff delay calculation and max retries enforcement
    /// NOTE: This test verifies retry logic with exponential backoff. Actual timing validation requires integration tests.
    /// </summary>
    [Fact]
    public async Task RefundWithRetryAsync_UsesExponentialBackoff()
    {
        // NOTE: Testing exact delay timing requires integration tests with time measurement
        // This test verifies the retry logic exhausts all retries with exponential backoff

        // Arrange
        var captureId = "CAPTURE-123";
        var amount = Money.Create(50.00m, "USD");
        var reason = "Backoff test";
        var idempotencyKey = "WCR-backoff-test";

        var attemptCount = 0;

        // All attempts fail (to trigger exponential backoff through all retries)
        _mockPayPalService
            .Setup(x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => attemptCount++)
            .ThrowsAsync(new HttpRequestException("Service unavailable", null, HttpStatusCode.ServiceUnavailable));

        // Act & Assert
        await FluentActions.Awaiting(async () =>
            await _sut.RefundWithRetryAsync(captureId, amount, reason, idempotencyKey))
            .Should().ThrowAsync<MaxRetriesExceededException>(
                "Should throw MaxRetriesExceededException after exhausting retries with exponential backoff");

        // Verify all retries were attempted (1 original + 5 retries = 6 total)
        attemptCount.Should().Be(6, "Should attempt 1 original + 5 retries with exponential backoff");
    }

    #endregion

    #region Retry Callback Tests

    /// <summary>
    /// Test 9: Retry callback invoked on each retry (for audit logging)
    /// Tests callback is called with retry count and error message
    /// </summary>
    [Fact]
    public async Task RefundWithRetryAsync_InvokesCallback_OnEachRetry()
    {
        // Arrange
        var captureId = "CAPTURE-123";
        var amount = Money.Create(100.00m, "USD");
        var reason = "Callback test";
        var idempotencyKey = "WCR-callback-test";

        var callbackInvocations = new List<(int retryCount, string errorMessage)>();
        var retryCallback = new Func<int, string, Task>((count, error) =>
        {
            callbackInvocations.Add((count, error));
            return Task.CompletedTask;
        });

        // Fail 3 times, then succeed
        _mockPayPalService
            .SetupSequence(x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Error 1", null, HttpStatusCode.ServiceUnavailable))
            .ThrowsAsync(new HttpRequestException("Error 2", null, HttpStatusCode.ServiceUnavailable))
            .ThrowsAsync(new HttpRequestException("Error 3", null, HttpStatusCode.ServiceUnavailable))
            .ReturnsAsync(Result<PayPalRefundResponse>.Success(new PayPalRefundResponse
            {
                RefundId = "REFUND-CALLBACK-SUCCESS",
                Status = "COMPLETED",
                Amount = new PayPalAmount { CurrencyCode = "USD", Value = "100.00" }
            }));

        // Act
        var result = await _sut.RefundWithRetryAsync(
            captureId,
            amount,
            reason,
            idempotencyKey,
            retryCallback);

        // Assert
        result.IsSuccess.Should().BeTrue();

        callbackInvocations.Should().HaveCount(3, "Callback should be invoked for each retry");

        // Verify retry counts are sequential: 1, 2, 3
        callbackInvocations[0].retryCount.Should().Be(1);
        callbackInvocations[1].retryCount.Should().Be(2);
        callbackInvocations[2].retryCount.Should().Be(3);

        // Verify error messages are passed
        callbackInvocations[0].errorMessage.Should().Contain("Error 1");
        callbackInvocations[1].errorMessage.Should().Contain("Error 2");
        callbackInvocations[2].errorMessage.Should().Contain("Error 3");
    }

    #endregion

    #region Non-Retryable Error Tests

    /// <summary>
    /// Test 10: Non-retryable errors fail immediately (PayPal business logic errors)
    /// Tests Result.Failure from PayPal service is not retried
    /// </summary>
    [Fact]
    public async Task RefundWithRetryAsync_OnBusinessLogicError_FailsImmediately()
    {
        // Arrange
        var captureId = "CAPTURE-123";
        var amount = Money.Create(100.00m, "USD");
        var reason = "Business logic error test";
        var idempotencyKey = "WCR-business-error-test";

        // PayPal service returns failure (not exception) - business logic error
        _mockPayPalService
            .Setup(x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PayPalRefundResponse>.Failure("Insufficient funds for refund"));

        // Act
        var result = await _sut.RefundWithRetryAsync(captureId, amount, reason, idempotencyKey);

        // Assert
        result.IsSuccess.Should().BeFalse("Business logic errors should not be retried");
        result.ErrorMessage.Should().Contain("Insufficient funds", "Error message should be preserved");

        // Verify only 1 attempt (no retries for business logic errors)
        _mockPayPalService.Verify(
            x => x.RefundCaptureAsync(
                It.IsAny<string>(),
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Business logic errors should not trigger retries");
    }

    #endregion
}
