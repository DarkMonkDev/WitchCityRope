using System.Text.Json;
using WitchCityRope.Api.Features.Payments.ValueObjects;

namespace WitchCityRope.Infrastructure.Tests.PayPal;

/// <summary>
/// Helper utilities for PayPal testing across all test suites
/// Provides common test data, validation, and assertion methods
/// </summary>
public static class PayPalTestHelpers
{
    /// <summary>
    /// Standard test amounts for consistent testing
    /// </summary>
    public static class TestAmounts
    {
        public static readonly Money Small = new() { Amount = 10.00m, Currency = "USD" };
        public static readonly Money Medium = new() { Amount = 50.00m, Currency = "USD" };
        public static readonly Money Large = new() { Amount = 100.00m, Currency = "USD" };
        public static readonly Money SlidingScale25 = new() { Amount = 37.50m, Currency = "USD" }; // 25% off $50
        public static readonly Money SlidingScale50 = new() { Amount = 25.00m, Currency = "USD" }; // 50% off $50
        public static readonly Money Zero = new() { Amount = 0.00m, Currency = "USD" };
    }

    /// <summary>
    /// Test customer data for consistent testing
    /// </summary>
    public static class TestCustomers
    {
        public static readonly Guid AdminCustomer = new("11111111-1111-1111-1111-111111111111");
        public static readonly Guid VettedMember = new("22222222-2222-2222-2222-222222222222");
        public static readonly Guid RegularMember = new("33333333-3333-3333-3333-333333333333");
        public static readonly Guid GuestCustomer = new("44444444-4444-4444-4444-444444444444");

        public static Guid GetRandomCustomer() => Guid.NewGuid();
    }

    /// <summary>
    /// Standard test metadata patterns
    /// </summary>
    public static class TestMetadata
    {
        public static Dictionary<string, string> CreateEventMetadata(Guid eventId, Guid userId, int slidingScale = 0)
        {
            return new Dictionary<string, string>
            {
                ["eventId"] = eventId.ToString(),
                ["userId"] = userId.ToString(),
                ["slidingScale"] = slidingScale.ToString(),
                ["testTimestamp"] = DateTime.UtcNow.ToString("O"),
                ["testType"] = "integration-test"
            };
        }

        public static Dictionary<string, string> CreateWorkshopMetadata(string workshopType, decimal originalPrice)
        {
            return new Dictionary<string, string>
            {
                ["workshopType"] = workshopType,
                ["originalPrice"] = originalPrice.ToString("F2"),
                ["venue"] = "Test Venue",
                ["instructor"] = "Test Instructor",
                ["testTimestamp"] = DateTime.UtcNow.ToString("O")
            };
        }
    }

    /// <summary>
    /// Webhook test data generators
    /// </summary>
    public static class WebhookTestData
    {
        public static Dictionary<string, object> CreatePaymentCompletedEvent(string captureId, decimal amount)
        {
            return new Dictionary<string, object>
            {
                ["id"] = $"WH-TEST-COMPLETED-{Guid.NewGuid():N}",
                ["event_type"] = "PAYMENT.CAPTURE.COMPLETED",
                ["create_time"] = DateTime.UtcNow.ToString("O"),
                ["resource"] = new Dictionary<string, object>
                {
                    ["id"] = captureId,
                    ["status"] = "COMPLETED",
                    ["amount"] = new Dictionary<string, object>
                    {
                        ["currency_code"] = "USD",
                        ["value"] = amount.ToString("F2")
                    },
                    ["seller_receivable_breakdown"] = new Dictionary<string, object>
                    {
                        ["gross_amount"] = new { currency_code = "USD", value = amount.ToString("F2") },
                        ["paypal_fee"] = new { currency_code = "USD", value = (amount * 0.035m).ToString("F2") },
                        ["net_amount"] = new { currency_code = "USD", value = (amount * 0.965m).ToString("F2") }
                    }
                }
            };
        }

        public static Dictionary<string, object> CreatePaymentRefundedEvent(string refundId, string captureId, decimal amount)
        {
            return new Dictionary<string, object>
            {
                ["id"] = $"WH-TEST-REFUNDED-{Guid.NewGuid():N}",
                ["event_type"] = "PAYMENT.CAPTURE.REFUNDED",
                ["create_time"] = DateTime.UtcNow.ToString("O"),
                ["resource"] = new Dictionary<string, object>
                {
                    ["id"] = refundId,
                    ["amount"] = new Dictionary<string, object>
                    {
                        ["currency_code"] = "USD",
                        ["value"] = amount.ToString("F2")
                    },
                    ["invoice_id"] = $"INVOICE-TEST-{Guid.NewGuid():N}",
                    ["status"] = "COMPLETED",
                    ["create_time"] = DateTime.UtcNow.ToString("O"),
                    ["update_time"] = DateTime.UtcNow.ToString("O"),
                    ["links"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["href"] = $"https://api.sandbox.paypal.com/v2/payments/refunds/{refundId}",
                            ["rel"] = "self",
                            ["method"] = "GET"
                        },
                        new Dictionary<string, object>
                        {
                            ["href"] = $"https://api.sandbox.paypal.com/v2/payments/captures/{captureId}",
                            ["rel"] = "up",
                            ["method"] = "GET"
                        }
                    }
                }
            };
        }

        public static Dictionary<string, object> CreatePaymentDeniedEvent(string captureId, string reason)
        {
            return new Dictionary<string, object>
            {
                ["id"] = $"WH-TEST-DENIED-{Guid.NewGuid():N}",
                ["event_type"] = "PAYMENT.CAPTURE.DENIED",
                ["create_time"] = DateTime.UtcNow.ToString("O"),
                ["resource"] = new Dictionary<string, object>
                {
                    ["id"] = captureId,
                    ["status"] = "DECLINED",
                    ["status_details"] = new Dictionary<string, object>
                    {
                        ["reason"] = reason
                    },
                    ["amount"] = new Dictionary<string, object>
                    {
                        ["currency_code"] = "USD",
                        ["value"] = "0.00"
                    }
                }
            };
        }
    }

    /// <summary>
    /// PayPal response validation helpers
    /// </summary>
    public static class ValidationHelpers
    {
        public static bool IsValidOrderId(string? orderId)
        {
            if (string.IsNullOrEmpty(orderId)) return false;
            
            // Real PayPal order IDs are typically 17-20 characters alphanumeric
            // Mock order IDs start with "MOCK-ORDER-"
            return orderId.StartsWith("MOCK-ORDER-") || 
                   (orderId.Length >= 15 && orderId.All(char.IsLetterOrDigit));
        }

        public static bool IsValidCaptureId(string? captureId)
        {
            if (string.IsNullOrEmpty(captureId)) return false;
            
            return captureId.StartsWith("MOCK-CAPTURE-") ||
                   (captureId.Length >= 15 && captureId.All(char.IsLetterOrDigit));
        }

        public static bool IsValidRefundId(string? refundId)
        {
            if (string.IsNullOrEmpty(refundId)) return false;
            
            return refundId.StartsWith("MOCK-REFUND-") ||
                   (refundId.Length >= 15 && refundId.All(char.IsLetterOrDigit));
        }

        public static bool IsValidApprovalUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            
            return url.Contains("paypal.com") || url.Contains("/mock/paypal/approve");
        }

        public static bool IsValidMoneyAmount(Money money)
        {
            return money.Amount >= 0 && 
                   !string.IsNullOrEmpty(money.Currency) && 
                   money.Currency.Length == 3;
        }
    }

    /// <summary>
    /// Performance test helpers
    /// </summary>
    public static class PerformanceHelpers
    {
        public static async Task<TimeSpan> MeasureAsync(Func<Task> operation)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await operation();
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        public static async Task<(T Result, TimeSpan Duration)> MeasureAsync<T>(Func<Task<T>> operation)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await operation();
            stopwatch.Stop();
            return (result, stopwatch.Elapsed);
        }

        public static void AssertPerformance(TimeSpan actual, TimeSpan expected, string operationName)
        {
            if (actual > expected)
            {
                throw new InvalidOperationException(
                    $"{operationName} took {actual.TotalMilliseconds:F2}ms, expected under {expected.TotalMilliseconds:F2}ms");
            }
        }
    }

    /// <summary>
    /// Retry helpers for flaky operations
    /// </summary>
    public static class RetryHelpers
    {
        public static async Task<T> WithRetryAsync<T>(
            Func<Task<T>> operation, 
            int maxAttempts = 3, 
            TimeSpan? delay = null)
        {
            var actualDelay = delay ?? TimeSpan.FromMilliseconds(100);
            var lastException = new Exception("No attempts made");

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    if (attempt == maxAttempts)
                    {
                        throw new InvalidOperationException(
                            $"Operation failed after {maxAttempts} attempts. Last error: {ex.Message}", ex);
                    }

                    await Task.Delay(actualDelay);
                    actualDelay = TimeSpan.FromMilliseconds(actualDelay.TotalMilliseconds * 1.5); // Exponential backoff
                }
            }

            throw lastException;
        }
    }

    /// <summary>
    /// Test data file helpers
    /// </summary>
    public static class FileHelpers
    {
        public static async Task<T> LoadTestDataAsync<T>(string fileName)
        {
            var testDataPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "..", "..", "..",
                "fixtures", "paypal-webhooks",
                fileName);

            if (!File.Exists(testDataPath))
            {
                throw new FileNotFoundException($"Test data file not found: {testDataPath}");
            }

            var json = await File.ReadAllTextAsync(testDataPath);
            return JsonSerializer.Deserialize<T>(json) ??
                   throw new InvalidOperationException($"Failed to deserialize {fileName}");
        }

        public static string GetTestDataDirectory()
        {
            return Path.Combine(
                Directory.GetCurrentDirectory(),
                "..", "..", "..",
                "fixtures", "paypal-webhooks");
        }
    }

    /// <summary>
    /// Environment detection helpers
    /// </summary>
    public static class EnvironmentHelpers
    {
        public static bool IsCiEnvironment => 
            Environment.GetEnvironmentVariable("CI") == "true" ||
            Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";

        public static bool HasRealPayPalCredentials =>
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PAYPAL_CLIENT_ID")) &&
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PAYPAL_CLIENT_SECRET"));

        public static bool ShouldUseMockService =>
            Environment.GetEnvironmentVariable("USE_MOCK_PAYMENT_SERVICE")?.ToLower() == "true" ||
            IsCiEnvironment ||
            !HasRealPayPalCredentials;

        public static void LogEnvironmentInfo(Microsoft.Extensions.Logging.ILogger logger)
        {
            logger.LogInformation("Test Environment Information:");
            logger.LogInformation("  CI Environment: {IsCi}", IsCiEnvironment);
            logger.LogInformation("  Has PayPal Credentials: {HasCredentials}", HasRealPayPalCredentials);
            logger.LogInformation("  Should Use Mock: {ShouldUseMock}", ShouldUseMockService);
            logger.LogInformation("  GitHub Actions: {IsGitHubActions}", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        }
    }
}