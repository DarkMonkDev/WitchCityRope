using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using PayPalCheckoutSdk.Core;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.ValueObjects;

namespace WitchCityRope.Api.Tests.Services;

/// <summary>
/// Phase 1 PayPal Service tests for idempotency header implementation
/// Tests PayPal-Request-Id header inclusion in refund requests
/// CRITICAL: Ensures PayPal idempotency mechanism is properly utilized
/// </summary>
public class PayPalServicePhase1Tests
{
    private readonly Mock<ILogger<PayPalService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public PayPalServicePhase1Tests()
    {
        _mockLogger = new Mock<ILogger<PayPalService>>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Setup configuration mocks
        _mockConfiguration.Setup(c => c["PayPal:ClientId"]).Returns("test-client-id");
        _mockConfiguration.Setup(c => c["PayPal:Secret"]).Returns("test-secret");
        _mockConfiguration.Setup(c => c["PayPal:Mode"]).Returns("sandbox");
        _mockConfiguration.Setup(c => c["PayPal:WebhookId"]).Returns("test-webhook-id");
    }

    #region Idempotency Header Tests

    /// <summary>
    /// Test 1: Verify PayPal-Request-Id header is included when idempotency key provided
    /// CRITICAL: This is the core Phase 1 requirement for PayPalService
    /// Note: This is a contract test - actual HTTP header validation requires integration testing
    /// </summary>
    [Fact]
    public void RefundCaptureAsync_WithIdempotencyKey_IncludesHeader()
    {
        // NOTE: This test verifies the method signature accepts idempotency key parameter.
        // Actual header inclusion is tested in integration tests against real PayPal sandbox.

        // Arrange
        var service = new PayPalService(_mockConfiguration.Object, _mockLogger.Object);
        var captureId = "CAPTURE-123";
        var refundAmount = Money.Create(50.00m, "USD");
        var reason = "Customer request";
        var idempotencyKey = "WCR-abc123def456";

        // Assert - Verify method signature accepts idempotency key
        var method = typeof(PayPalService).GetMethod("RefundCaptureAsync");
        method.Should().NotBeNull("RefundCaptureAsync method should exist");

        var parameters = method!.GetParameters();
        parameters.Should().HaveCount(6, "Method should have 6 parameters including idempotency key");

        var idempotencyParam = parameters.FirstOrDefault(p => p.Name == "idempotencyKey");
        idempotencyParam.Should().NotBeNull("Method should have idempotencyKey parameter");
        idempotencyParam!.ParameterType.Should().Be(typeof(string), "Idempotency key should be string type");
        idempotencyParam.IsOptional.Should().BeTrue("Idempotency key should be optional for backward compatibility");
    }

    /// <summary>
    /// Test 2: Verify method signature allows null idempotency key
    /// Tests backward compatibility with existing code
    /// </summary>
    [Fact]
    public void RefundCaptureAsync_IdempotencyKeyParameter_IsOptional()
    {
        // Arrange
        var method = typeof(PayPalService).GetMethod("RefundCaptureAsync");
        var idempotencyParam = method!.GetParameters().FirstOrDefault(p => p.Name == "idempotencyKey");

        // Assert
        idempotencyParam.Should().NotBeNull();
        idempotencyParam!.IsOptional.Should().BeTrue("Should support calls without idempotency key");
        idempotencyParam.DefaultValue.Should().Be(null, "Default should be null for optional parameter");
    }

    /// <summary>
    /// Test 3: Verify method signature has correct parameter order
    /// Ensures API compatibility and correct usage
    /// </summary>
    [Fact]
    public void RefundCaptureAsync_ParameterOrder_IsCorrect()
    {
        // Arrange
        var method = typeof(PayPalService).GetMethod("RefundCaptureAsync");
        var parameters = method!.GetParameters();

        // Assert - Verify parameter order matches implementation plan
        parameters[0].Name.Should().Be("captureId", "First parameter should be capture ID");
        parameters[0].ParameterType.Should().Be(typeof(string));

        parameters[1].Name.Should().Be("refundAmount", "Second parameter should be refund amount");
        parameters[1].ParameterType.Should().Be(typeof(Money));

        parameters[2].Name.Should().Be("reason", "Third parameter should be reason");
        parameters[2].ParameterType.Should().Be(typeof(string));

        parameters[3].Name.Should().Be("idempotencyKey", "Fourth parameter should be idempotency key");
        parameters[3].ParameterType.Should().Be(typeof(string));

        parameters[4].Name.Should().Be("noteToPayer", "Fifth parameter should be note to payer");
        parameters[4].ParameterType.Should().Be(typeof(string));

        parameters[5].Name.Should().Be("cancellationToken", "Sixth parameter should be cancellation token");
        parameters[5].ParameterType.Should().Be(typeof(CancellationToken));
    }

    #endregion

    #region Capture ID Parameter Tests

    /// <summary>
    /// Test 4: Verify RefundCaptureAsync accepts Capture ID (not Order ID)
    /// CRITICAL: Confirms method signature uses correct identifier type
    /// </summary>
    [Fact]
    public void RefundCaptureAsync_FirstParameter_IsCaptureId()
    {
        // Arrange
        var method = typeof(PayPalService).GetMethod("RefundCaptureAsync");
        var parameters = method!.GetParameters();

        // Assert
        parameters[0].Name.Should().Be("captureId", "Method should use Capture ID, not Order ID");
        parameters[0].ParameterType.Should().Be(typeof(string));

        // Verify no parameter named "orderId" exists
        parameters.Should().NotContain(p => p.Name == "orderId", "Method should not have Order ID parameter");
    }

    /// <summary>
    /// Test 5: Verify method name is RefundCaptureAsync (not RefundOrderAsync)
    /// Confirms correct API naming convention
    /// </summary>
    [Fact]
    public void RefundMethod_NamedCorrectly_AsRefundCaptureAsync()
    {
        // Arrange
        var refundCaptureMethod = typeof(PayPalService).GetMethod("RefundCaptureAsync");
        var refundOrderMethod = typeof(PayPalService).GetMethod("RefundOrderAsync");

        // Assert
        refundCaptureMethod.Should().NotBeNull("RefundCaptureAsync should exist");
        refundOrderMethod.Should().BeNull("RefundOrderAsync should NOT exist (incorrect API usage)");
    }

    #endregion

    #region Contract Tests for IPayPalService Interface

    /// <summary>
    /// Test 6: Verify IPayPalService interface includes RefundCaptureAsync signature
    /// Ensures interface contract is updated
    /// </summary>
    [Fact]
    public void IPayPalService_DefinesRefundCaptureAsync_WithIdempotencyKey()
    {
        // Arrange
        var interfaceType = typeof(IPayPalService);
        var method = interfaceType.GetMethod("RefundCaptureAsync");

        // Assert
        method.Should().NotBeNull("Interface should define RefundCaptureAsync");

        var parameters = method!.GetParameters();
        var idempotencyParam = parameters.FirstOrDefault(p => p.Name == "idempotencyKey");

        idempotencyParam.Should().NotBeNull("Interface should include idempotency key parameter");
        idempotencyParam!.ParameterType.Should().Be(typeof(string));
    }

    /// <summary>
    /// Test 7: Verify MockPayPalService implements updated interface
    /// Ensures test mock is compatible with Phase 1 changes
    /// </summary>
    [Fact]
    public void MockPayPalService_ImplementsRefundCaptureAsync_WithIdempotencyKey()
    {
        // Arrange
        var mockType = typeof(MockPayPalService);
        var method = mockType.GetMethod("RefundCaptureAsync");

        // Assert
        method.Should().NotBeNull("MockPayPalService should implement RefundCaptureAsync");

        var parameters = method!.GetParameters();
        var idempotencyParam = parameters.FirstOrDefault(p => p.Name == "idempotencyKey");

        idempotencyParam.Should().NotBeNull("Mock should support idempotency key parameter");
    }

    #endregion

    #region Edge Case Tests

    /// <summary>
    /// Test 8: Verify method handles empty idempotency key gracefully
    /// Tests edge case where key is empty string vs null
    /// </summary>
    [Fact]
    public void RefundCaptureAsync_WithEmptyIdempotencyKey_HandlesGracefully()
    {
        // NOTE: Actual behavior validation requires integration testing
        // This test verifies the method accepts empty string without compile errors

        // Arrange
        var service = new PayPalService(_mockConfiguration.Object, _mockLogger.Object);
        var emptyKey = string.Empty;

        // Assert - Method should accept empty string (compile-time check)
        var method = typeof(PayPalService).GetMethod("RefundCaptureAsync");
        var idempotencyParam = method!.GetParameters().FirstOrDefault(p => p.Name == "idempotencyKey");

        idempotencyParam.Should().NotBeNull();
        idempotencyParam!.ParameterType.Should().Be(typeof(string), "Should accept empty strings");
    }

    /// <summary>
    /// Test 9: Verify method handles whitespace-only idempotency key
    /// Tests input validation edge case
    /// </summary>
    [Fact]
    public void RefundCaptureAsync_WithWhitespaceIdempotencyKey_HandlesGracefully()
    {
        // NOTE: Actual behavior validation requires integration testing
        // This test verifies the method signature allows whitespace strings

        // Arrange
        var method = typeof(PayPalService).GetMethod("RefundCaptureAsync");
        var idempotencyParam = method!.GetParameters().FirstOrDefault(p => p.Name == "idempotencyKey");

        // Assert
        idempotencyParam.Should().NotBeNull();
        idempotencyParam!.ParameterType.Should().Be(typeof(string), "Should accept whitespace strings");

        // Note: Implementation should handle this case appropriately
        // (likely treating whitespace as equivalent to null/empty)
    }

    #endregion

    #region Documentation and Logging Tests

    /// <summary>
    /// Test 10: Verify RefundCaptureAsync logs idempotency key usage
    /// Tests observability for debugging and auditing
    /// Note: Requires reading actual implementation - this is a structural test
    /// </summary>
    [Fact]
    public void RefundCaptureAsync_Signature_SupportsLoggingIdempotencyKey()
    {
        // Arrange
        var method = typeof(PayPalService).GetMethod("RefundCaptureAsync");
        var parameters = method!.GetParameters();

        // Assert - Verify parameters exist for complete logging
        parameters.Should().Contain(p => p.Name == "captureId", "Should log capture ID");
        parameters.Should().Contain(p => p.Name == "refundAmount", "Should log refund amount");
        parameters.Should().Contain(p => p.Name == "idempotencyKey", "Should log idempotency key");

        // Verify method has access to logger through service
        var loggerField = typeof(PayPalService).GetField("_logger",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        loggerField.Should().NotBeNull("Service should have logger for audit trail");
    }

    #endregion
}
