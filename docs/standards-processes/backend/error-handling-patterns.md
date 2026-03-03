# Error Handling and Logging Patterns

**Purpose**: Standardized error handling and logging practices for backend services.
**When to Read**: When implementing error handling, exception management, or logging.
**Related**: [Service Layer Patterns](./service-layer-patterns.md), [Performance Standards](./performance-standards.md)

## Result Pattern Usage

Use the Result pattern for operations that can fail in expected ways:

```csharp
/// <summary>
/// Processes a vetting application, performing all necessary validation steps
/// and business rule checks. Returns detailed result indicating success or
/// specific failure reasons for proper user feedback.
/// </summary>
public async Task<Result<VettingDecision>> ProcessVettingApplicationAsync(int applicationId)
{
    try
    {
        var application = await _repository.GetVettingApplicationAsync(applicationId);
        if (application == null)
        {
            return Result<VettingDecision>.Failure("Vetting application not found");
        }

        // Validate application completeness
        var validationResult = ValidateApplicationCompleteness(application);
        if (!validationResult.IsValid)
        {
            return Result<VettingDecision>.Failure(
                $"Application incomplete: {string.Join(", ", validationResult.Errors)}");
        }

        // Apply business rules for approval
        var decision = await ApplyVettingBusinessRulesAsync(application);

        // Log the decision for audit trail
        _logger.LogInformation(
            "Vetting decision made for application {ApplicationId}: {Decision} by {ReviewerId}",
            applicationId, decision.Decision, decision.ReviewerId);

        return Result<VettingDecision>.Success(decision);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "Unexpected error processing vetting application {ApplicationId}",
            applicationId);
        return Result<VettingDecision>.Failure("An unexpected error occurred processing the application");
    }
}
```

## Structured Logging

See the **[Serilog Logging Guide](./serilog-logging-guide.md)** for all logging patterns and standards.

**Quick rules for endpoint handlers:**
- Use message templates, not string interpolation: `"User {UserId} registered"` not `$"User {userId} registered"`
- `CorrelationId`, `UserId`, and `RequestPath` are injected by middleware — do not add them manually
- `_logger.BeginScope()` still works but `LogContext.PushProperty` is preferred for middleware-level enrichment

## Exception Handling Guidelines

### When to Use Try-Catch
- **Service layer operations**: Wrap public service methods
- **External API calls**: Catch and handle third-party service failures
- **Database operations**: Handle connection and transaction failures
- **File I/O**: Handle file system errors gracefully

### When NOT to Use Try-Catch
- **Validation logic**: Use Result pattern instead
- **Expected failures**: Return Result<T> with error message
- **Flow control**: Don't use exceptions for normal program flow

### Custom Exceptions
```csharp
/// <summary>
/// Thrown when payment processing fails for a specific business reason
/// </summary>
public class PaymentException : Exception
{
    public string PaymentProvider { get; }
    public string TransactionId { get; }

    public PaymentException(string message, string provider, string transactionId)
        : base(message)
    {
        PaymentProvider = provider;
        TransactionId = transactionId;
    }
}
```

## Logging Levels

- **Critical**: System is unusable, immediate action required
- **Error**: Operation failed, but system continues
- **Warning**: Unexpected situation, but operation succeeded
- **Information**: Important business events (registration, payment)
- **Debug**: Detailed diagnostic information for troubleshooting
- **Trace**: Very detailed diagnostic information
