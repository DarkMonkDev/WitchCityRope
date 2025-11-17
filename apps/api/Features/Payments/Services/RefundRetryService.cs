using System.Net.Sockets;
using WitchCityRope.Api.Features.Payments.Models.PayPal;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.Payments.Services;

/// <summary>
/// Retry service for PayPal refund operations with exponential backoff.
///
/// <para>
/// Retry strategy:
/// - Max 5 retries with exponential backoff (2^retryCount seconds)
/// - Retry schedule: 2s, 4s, 8s, 16s, 32s (total 62 seconds max wait)
/// - Retryable errors: 5xx server errors, network timeouts, connection failures
/// - Non-retryable errors: 4xx client errors (invalid request, insufficient funds)
/// - Idempotency key reused across all retries (critical for preventing duplicate refunds)
/// </para>
///
/// <para>
/// Why retry logic matters:
/// - PayPal API can experience transient failures (network issues, temporary outages)
/// - Automatic retry prevents manual intervention for transient errors
/// - Exponential backoff prevents overwhelming PayPal servers during outages
/// - Idempotency ensures retries don't create duplicate refunds
/// </para>
///
/// <para>
/// Error handling philosophy:
/// - 5xx errors: Retry (server-side transient failure)
/// - 4xx errors: Fail immediately (client error, won't succeed on retry)
/// - Network errors: Retry (connection/timeout issues)
/// - PayPal API errors: Fail immediately (business rule violations)
/// </para>
/// </summary>
public class RefundRetryService : IRefundRetryService
{
    private readonly IPayPalService _payPalService;
    private readonly ILogger<RefundRetryService> _logger;
    private const int MaxRetries = 5;

    public RefundRetryService(
        IPayPalService payPalService,
        ILogger<RefundRetryService> logger)
    {
        _payPalService = payPalService;
        _logger = logger;
    }

    /// <summary>
    /// Executes PayPal refund with automatic retry logic for transient failures.
    /// </summary>
    /// <param name="captureId">PayPal Capture ID to refund</param>
    /// <param name="amount">Refund amount (null for full refund)</param>
    /// <param name="reason">Refund reason for audit trail</param>
    /// <param name="idempotencyKey">Idempotency key (MUST be reused across retries)</param>
    /// <param name="onRetry">Optional callback invoked on each retry attempt with retry count and error message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing PayPalRefundResponse if successful, or error details if failed</returns>
    /// <exception cref="MaxRetriesExceededException">Thrown when max retries exceeded for retryable errors</exception>
    public async Task<Result<PayPalRefundResponse>> RefundWithRetryAsync(
        string captureId,
        Money? amount,
        string? reason,
        string idempotencyKey,
        Func<int, string, Task>? onRetry = null,
        CancellationToken cancellationToken = default)
    {
        var retryCount = 0;
        Exception? lastException = null;

        _logger.LogInformation(
            "Starting refund with retry for capture {CaptureId}, amount {Amount}, idempotency key {IdempotencyKey}",
            captureId, amount?.ToDisplayString() ?? "FULL", idempotencyKey);

        while (retryCount <= MaxRetries)
        {
            try
            {
                // Make API call (idempotency key ensures no duplicates even on retries)
                var result = await _payPalService.RefundCaptureAsync(
                    captureId,
                    amount,
                    reason ?? "Refund requested",
                    idempotencyKey,
                    $"Refund attempt {retryCount + 1}/{MaxRetries + 1}",
                    cancellationToken);

                // Success - return result
                if (result.IsSuccess && result.Value != null)
                {
                    if (retryCount > 0)
                    {
                        _logger.LogInformation(
                            "Refund succeeded on retry {RetryCount} for capture {CaptureId}",
                            retryCount, captureId);
                    }

                    return result;
                }

                // PayPal service returned failure (business logic error, not retryable)
                _logger.LogWarning(
                    "PayPal refund failed with business error: {ErrorMessage}. Not retrying.",
                    result.ErrorMessage);

                return result;
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;

                // Check if retryable error (5xx, network)
                if (IsRetryableError(ex))
                {
                    if (retryCount >= MaxRetries)
                    {
                        _logger.LogError(
                            ex,
                            "Refund failed after {MaxRetries} retries for capture {CaptureId}",
                            MaxRetries, captureId);

                        throw new MaxRetriesExceededException(
                            $"Refund failed after {MaxRetries} retries: {ex.Message}",
                            ex);
                    }

                    retryCount++;

                    // Exponential backoff: 2^retryCount seconds
                    var delaySeconds = Math.Pow(2, retryCount);

                    _logger.LogWarning(
                        ex,
                        "Retryable error on attempt {Attempt}/{MaxAttempts} for capture {CaptureId}. " +
                        "Retrying in {DelaySeconds}s. Error: {ErrorMessage}",
                        retryCount, MaxRetries + 1, captureId, delaySeconds, ex.Message);

                    // Invoke callback if provided (for audit logging)
                    if (onRetry != null)
                    {
                        await onRetry(retryCount, ex.Message);
                    }

                    // Wait before retry
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                    continue;
                }

                // Non-retryable error (4xx client error) - throw immediately
                _logger.LogError(
                    ex,
                    "Non-retryable HTTP error for capture {CaptureId}: {StatusCode} {ErrorMessage}",
                    captureId, ex.StatusCode, ex.Message);

                return Result<PayPalRefundResponse>.Failure(
                    $"Refund failed with client error: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                // Timeout error - retryable
                lastException = ex;

                if (retryCount >= MaxRetries)
                {
                    _logger.LogError(
                        ex,
                        "Refund timed out after {MaxRetries} retries for capture {CaptureId}",
                        MaxRetries, captureId);

                    throw new MaxRetriesExceededException(
                        $"Refund timed out after {MaxRetries} retries",
                        ex);
                }

                retryCount++;
                var delaySeconds = Math.Pow(2, retryCount);

                _logger.LogWarning(
                    "Request timeout on attempt {Attempt}/{MaxAttempts} for capture {CaptureId}. " +
                    "Retrying in {DelaySeconds}s.",
                    retryCount, MaxRetries + 1, captureId, delaySeconds);

                if (onRetry != null)
                {
                    await onRetry(retryCount, "Request timeout");
                }

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                continue;
            }
            catch (Exception ex)
            {
                // Unexpected error - log and return failure (don't retry unknown errors)
                _logger.LogError(
                    ex,
                    "Unexpected error during refund for capture {CaptureId}: {ErrorType} {ErrorMessage}",
                    captureId, ex.GetType().Name, ex.Message);

                return Result<PayPalRefundResponse>.Failure(
                    $"Unexpected error during refund: {ex.Message}");
            }
        }

        // Should never reach here (safety fallback)
        var fallbackMessage = lastException?.Message ?? "Unexpected retry loop exit";
        _logger.LogError(
            "Retry loop exited unexpectedly for capture {CaptureId}: {Message}",
            captureId, fallbackMessage);

        throw lastException ?? new Exception(fallbackMessage);
    }

    /// <summary>
    /// Determines if an HTTP error is retryable based on status code and error type.
    ///
    /// <para>
    /// Retryable errors (transient failures):
    /// - 5xx server errors (PayPal internal issues)
    /// - Network-level errors (SocketException, DNS failures)
    /// - Timeout errors (TaskCanceledException without user cancellation)
    /// </para>
    ///
    /// <para>
    /// Non-retryable errors (permanent failures):
    /// - 4xx client errors (invalid request, insufficient funds, authorization)
    /// - PayPal API business logic errors
    /// </para>
    /// </summary>
    /// <param name="ex">HTTP request exception</param>
    /// <returns>True if error is retryable, false otherwise</returns>
    private bool IsRetryableError(HttpRequestException ex)
    {
        // Check for HTTP status code
        if (ex.StatusCode.HasValue)
        {
            var statusCode = (int)ex.StatusCode.Value;

            // 5xx server errors are retryable (transient server issues)
            if (statusCode >= 500 && statusCode < 600)
            {
                return true;
            }

            // 4xx client errors are NOT retryable (permanent request issues)
            if (statusCode >= 400 && statusCode < 500)
            {
                return false;
            }
        }

        // Network-level errors (no status code) are retryable
        // - SocketException: Connection refused, host unreachable
        // - TaskCanceledException: Request timeout
        if (ex.InnerException is SocketException)
        {
            return true;
        }

        if (ex.InnerException is TaskCanceledException)
        {
            return true;
        }

        // Unknown errors without status code: retry to be safe
        // (could be network issue, DNS failure, etc.)
        if (!ex.StatusCode.HasValue)
        {
            return true;
        }

        // Default: don't retry unknown scenarios
        return false;
    }
}

/// <summary>
/// Interface for refund retry service
/// </summary>
public interface IRefundRetryService
{
    /// <summary>
    /// Executes PayPal refund with automatic retry logic for transient failures.
    /// </summary>
    /// <param name="captureId">PayPal Capture ID to refund</param>
    /// <param name="amount">Refund amount (null for full refund)</param>
    /// <param name="reason">Refund reason for audit trail</param>
    /// <param name="idempotencyKey">Idempotency key (MUST be reused across retries)</param>
    /// <param name="onRetry">Optional callback invoked on each retry attempt with retry count and error message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing PayPalRefundResponse if successful, or error details if failed</returns>
    Task<Result<PayPalRefundResponse>> RefundWithRetryAsync(
        string captureId,
        Money? amount,
        string? reason,
        string idempotencyKey,
        Func<int, string, Task>? onRetry = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Exception thrown when max retries exceeded for a refund operation.
/// Indicates all retry attempts failed due to persistent transient errors.
/// </summary>
public class MaxRetriesExceededException : Exception
{
    /// <summary>
    /// Creates a new MaxRetriesExceededException with error message and inner exception.
    /// </summary>
    /// <param name="message">Error message describing the failure</param>
    /// <param name="inner">Inner exception containing the last retry failure</param>
    public MaxRetriesExceededException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
