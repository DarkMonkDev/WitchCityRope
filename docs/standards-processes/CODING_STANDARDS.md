# WitchCityRope Coding Standards

## Overview

This document outlines the coding standards, principles, and conventions used throughout the WitchCityRope project. These standards ensure code quality, maintainability, and consistency across all project contributions.

## Core Principles

### SOLID Principles

We adhere to SOLID principles where they make the most sense and add clear value to the codebase:

#### 1. Single Responsibility Principle (SRP)
- **Classes should have one reason to change**
- Each service handles a single domain area (e.g., `EventService` handles only event-related operations)
- Separate concerns between data access, business logic, and presentation
- Example: `AuthService` handles authentication, `EmailService` handles email operations

```csharp
// ✅ Good - Single responsibility
public class EventService : IEventService
{
    public Task<Result<EventDto>> CreateEventAsync(CreateEventRequest request) { }
    public Task<Result<EventDto>> GetEventAsync(int id) { }
    public Task<Result<List<EventDto>>> GetEventsAsync(EventFilter filter) { }
}

// ❌ Bad - Multiple responsibilities
public class EventService : IEventService
{
    public Task<Result<EventDto>> CreateEventAsync(CreateEventRequest request) { }
    public Task<Result> SendEventNotificationAsync(int eventId) { } // Should be in NotificationService
    public Task<Result> ProcessPaymentAsync(PaymentRequest request) { } // Should be in PaymentService
}
```

#### 2. Open/Closed Principle (OCP)
- **Open for extension, closed for modification**
- Use interfaces and inheritance to extend functionality
- Avoid modifying existing code when adding new features
- Example: Payment processing supports multiple providers through interfaces

```csharp
// ✅ Good - Extensible payment system
public interface IPaymentProvider
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
}

public class PayPalPaymentProvider : IPaymentProvider
{
    public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request) { }
}

// Easy to add new providers without modifying existing code
public class StripePaymentProvider : IPaymentProvider
{
    public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request) { }
}
```

#### 3. Liskov Substitution Principle (LSP)
- **Derived classes must be substitutable for their base classes**
- Implementations must honor the contract defined by interfaces
- Example: All payment providers behave consistently

```csharp
// ✅ Good - All implementations follow the same contract
public class PaymentService
{
    private readonly IPaymentProvider _paymentProvider;
    
    // Can use any IPaymentProvider implementation
    public PaymentService(IPaymentProvider paymentProvider)
    {
        _paymentProvider = paymentProvider;
    }
}
```

#### 4. Interface Segregation Principle (ISP)
- **Clients should not depend on interfaces they don't use**
- Create focused, specific interfaces rather than large, general ones
- Example: Separate read and write operations when appropriate

```csharp
// ✅ Good - Focused interfaces
public interface IEventReader
{
    Task<EventDto> GetEventAsync(int id);
    Task<List<EventDto>> GetEventsAsync(EventFilter filter);
}

public interface IEventWriter
{
    Task<EventDto> CreateEventAsync(CreateEventRequest request);
    Task<EventDto> UpdateEventAsync(int id, UpdateEventRequest request);
    Task DeleteEventAsync(int id);
}

// ❌ Bad - Clients forced to depend on methods they don't use
public interface IEventService
{
    Task<EventDto> GetEventAsync(int id);
    Task<List<EventDto>> GetEventsAsync(EventFilter filter);
    Task<EventDto> CreateEventAsync(CreateEventRequest request);
    Task<EventDto> UpdateEventAsync(int id, UpdateEventRequest request);
    Task DeleteEventAsync(int id);
    Task<byte[]> ExportEventsToExcelAsync(); // Not all clients need this
    Task<ValidationResult> ValidateEventBusinessRulesAsync(EventDto evt); // Business logic shouldn't be in interface
}
```

#### 5. Dependency Inversion Principle (DIP)
- **Depend on abstractions, not concretions**
- High-level modules should not depend on low-level modules
- Both should depend on abstractions
- Example: Services depend on interfaces, not concrete implementations

```csharp
// ✅ Good - Depends on abstraction
public class VettingService : IVettingService
{
    private readonly IVettingRepository _repository;
    private readonly IEmailService _emailService;
    
    public VettingService(IVettingRepository repository, IEmailService emailService)
    {
        _repository = repository;
        _emailService = emailService;
    }
}

// ❌ Bad - Depends on concrete implementation
public class VettingService : IVettingService
{
    private readonly VettingRepository _repository; // Concrete class
    private readonly SendGridEmailService _emailService; // Concrete class
}
```

### When NOT to Apply SOLID

SOLID principles should enhance code quality, not create unnecessary complexity:

- **Don't create interfaces for classes that will never have multiple implementations**
- **Don't over-abstract simple operations**
- **Don't split classes when they naturally belong together**
- **Don't create complex inheritance hierarchies for simple scenarios**

```csharp
// ✅ Good - Simple utility class doesn't need interface
public static class DateTimeHelper
{
    public static DateTime ToEasternTime(DateTime utcDateTime)
    {
        var easternZone = TimeZoneInfo.FindSystemTimeZoneById("US/Eastern");
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, easternZone);
    }
}

// ❌ Bad - Unnecessary abstraction
public interface IDateTimeHelper
{
    DateTime ToEasternTime(DateTime utcDateTime);
}
```

## Code Documentation Standards

All code must be well-documented so future engineers can easily understand what each section does.

### XML Documentation Comments

Use XML documentation for all public APIs:

```csharp
/// <summary>
/// Creates a new event with the specified details and validates business rules.
/// </summary>
/// <param name="request">The event creation request containing event details</param>
/// <returns>
/// A Result containing the created EventDto if successful, or error details if failed.
/// Common failure reasons: invalid date range, duplicate event name, insufficient permissions.
/// </returns>
/// <exception cref="ArgumentNullException">Thrown when request is null</exception>
/// <example>
/// <code>
/// var request = new CreateEventRequest
/// {
///     Name = "Rope Basics Workshop",
///     StartTime = DateTime.Now.AddDays(7),
///     Capacity = 20
/// };
/// var result = await eventService.CreateEventAsync(request);
/// if (result.IsSuccess)
/// {
///     Console.WriteLine($"Created event with ID: {result.Value.Id}");
/// }
/// </code>
/// </example>
public async Task<Result<EventDto>> CreateEventAsync(CreateEventRequest request)
```

### Inline Comments

Use inline comments to explain complex business logic, algorithms, or non-obvious decisions:

```csharp
public async Task<Result<List<EventDto>>> GetEventsAsync(EventFilter filter)
{
    var cacheKey = $"events_{filter.GetHashCode()}";
    
    // Check cache first to avoid expensive database queries
    // Events data changes infrequently, so 5-minute cache is appropriate
    if (_cache.TryGetValue<List<EventDto>>(cacheKey, out var cached))
    {
        return Result<List<EventDto>>.Success(cached);
    }

    var query = _db.Events.AsQueryable();
    
    // Apply date filter - events are typically queried by future dates
    // Past events are kept for historical reporting but filtered by default
    if (filter.StartDate.HasValue)
    {
        query = query.Where(e => e.StartTime >= filter.StartDate.Value);
    }
    
    // Order by start time ascending - users expect chronological order
    // for event browsing and registration workflows
    var events = await query
        .OrderBy(e => e.StartTime)
        .Select(e => new EventDto(e))
        .ToListAsync();

    // Cache for 5 minutes - balances performance with data freshness
    // Events don't change frequently enough to need real-time updates
    _cache.Set(cacheKey, events, TimeSpan.FromMinutes(5));
    
    return Result<List<EventDto>>.Success(events);
}
```

### Class and Method Documentation

Document the purpose, responsibilities, and usage patterns:

```csharp
/// <summary>
/// Handles all event-related business operations including creation, modification,
/// registration management, and capacity tracking. This service enforces business
/// rules such as capacity limits, payment requirements, and vetting status checks.
/// 
/// Key responsibilities:
/// - Event lifecycle management (create, update, cancel)
/// - Registration processing and waitlist management
/// - Capacity and payment rule enforcement
/// - Integration with payment and notification services
/// 
/// Business rules enforced:
/// - Classes require advance payment, meetups allow pay-at-door
/// - Only vetted members can register for member-only events
/// - Teachers don't count toward capacity limits
/// - Refunds allowed until 48-72 hours before event start
/// </summary>
public class EventService : IEventService
{
    /// <summary>
    /// Registers a user for an event, handling payment processing, capacity checks,
    /// and waitlist management according to business rules.
    /// 
    /// This method performs several critical operations:
    /// 1. Validates user eligibility (vetting status, existing registrations)
    /// 2. Checks event capacity and manages waitlist if needed
    /// 3. Processes payment if required (classes vs meetups)
    /// 4. Sends confirmation email with event details
    /// 5. Updates capacity tracking and statistics
    /// 
    /// Payment handling varies by event type:
    /// - Classes: Payment required at registration time
    /// - Meetups: Payment optional (pay at door allowed)
    /// - Free events: No payment processing needed
    /// </summary>
    /// <param name="eventId">The unique identifier of the event to register for</param>
    /// <param name="userId">The unique identifier of the user registering</param>
    /// <param name="paymentInfo">Payment details (null for free events or pay-at-door)</param>
    /// <returns>
    /// RegistrationResult containing:
    /// - Success: Registration confirmation with payment receipt
    /// - Waitlist: Position on waitlist if event is full
    /// - Failure: Specific error (not eligible, payment failed, etc.)
    /// </returns>
    public async Task<RegistrationResult> RegisterForEventAsync(
        int eventId, 
        int userId, 
        PaymentInfo paymentInfo = null)
    {
        // Implementation with detailed comments explaining each step
    }
}
```

### Complex Algorithms and Business Logic

Document complex algorithms, business rules, and decision trees:

```csharp
/// <summary>
/// Calculates event pricing based on multiple factors including user membership status,
/// sliding scale preferences, and event type. This implements the organization's
/// progressive pricing model designed to ensure accessibility while covering costs.
/// 
/// Pricing algorithm:
/// 1. Base price is set per event type (classes: $35-65, meetups: $0-35)
/// 2. Sliding scale discount applied based on user selection (0-75% off)
/// 3. Member discounts applied for certain event types
/// 4. Teacher exemptions handled (teachers don't pay for their own classes)
/// 5. Special circumstances (financial hardship) require manual override
/// 
/// The sliding scale is honor-system based - we trust members to pay what they can
/// afford while ensuring the organization can cover venue and instructor costs.
/// </summary>
/// <param name="eventDetails">Event information including base price and type</param>
/// <param name="userProfile">User information including membership and financial preferences</param>
/// <param name="isTeaching">True if user is teaching this specific event</param>
/// <returns>Final calculated price with breakdown of applied discounts</returns>
public PricingCalculation CalculateEventPrice(
    EventDetails eventDetails, 
    UserProfile userProfile, 
    bool isTeaching = false)
{
    var calculation = new PricingCalculation
    {
        BasePrice = eventDetails.BasePrice,
        EventType = eventDetails.Type
    };
    
    // Teachers don't pay for events they're teaching
    // This is both a financial incentive and recognition of their contribution
    if (isTeaching)
    {
        calculation.TeacherDiscount = calculation.BasePrice;
        calculation.FinalPrice = 0;
        calculation.Notes = "Teacher exemption applied";
        return calculation;
    }
    
    // Apply sliding scale based on user's self-reported financial situation
    // Scale ranges from 0% (full price) to 75% (maximum discount)
    // We cap at 75% to ensure minimum contribution toward venue costs
    var slidingScalePercent = Math.Min(userProfile.SlidingScalePreference, 0.75m);
    calculation.SlidingScaleDiscount = calculation.BasePrice * slidingScalePercent;
    
    // Member discount applies to certain event types
    // Classes: 10% member discount on remaining amount after sliding scale
    // Meetups: No additional member discount (already low cost)
    if (userProfile.IsMember && eventDetails.Type == EventType.Class)
    {
        var remainingAfterSliding = calculation.BasePrice - calculation.SlidingScaleDiscount;
        calculation.MemberDiscount = remainingAfterSliding * 0.10m; // 10% member discount
    }
    
    // Calculate final price ensuring it never goes below $0
    calculation.FinalPrice = Math.Max(0, 
        calculation.BasePrice - calculation.SlidingScaleDiscount - calculation.MemberDiscount);
    
    return calculation;
}
```

## Naming Conventions

### General Principles
- Use clear, descriptive names that explain purpose and intent
- Prefer longer, explicit names over short, cryptic ones
- Use domain language that matches business terminology
- Avoid abbreviations unless they're widely understood in context

### C# Specific Conventions

#### Classes and Interfaces
```csharp
// ✅ Good - Clear purpose and responsibility
public class EventRegistrationService { }
public class PaymentProcessingService { }
public interface IVettingApplicationRepository { }
public interface IEmailNotificationService { }

// ❌ Bad - Vague or abbreviated
public class EventSvc { }
public class PmtProc { }
public interface IVetRepo { }
public interface IEmailer { }
```

#### Methods
```csharp
// ✅ Good - Describes what the method does and returns
public async Task<RegistrationResult> RegisterUserForEventAsync(int userId, int eventId)
public async Task<bool> IsUserEligibleForMemberEventsAsync(int userId)
public async Task<List<EventDto>> GetUpcomingEventsForUserAsync(int userId, int maxResults = 10)

// ❌ Bad - Unclear purpose or inconsistent naming
public async Task<object> DoReg(int u, int e)
public async Task<bool> CheckUser(int userId)
public async Task<List<EventDto>> GetEvents(int userId, int max = 10)
```

#### Variables and Parameters
```csharp
// ✅ Good - Self-documenting variable names
var registrationConfirmationEmail = await _emailService.BuildRegistrationEmailAsync(registration);
var isEventAtCapacity = event.RegisteredCount >= event.MaxCapacity;
var daysUntilEvent = (event.StartDate - DateTime.UtcNow).Days;

// ❌ Bad - Requires mental mapping to understand
var email = await _emailService.BuildEmailAsync(reg);
var isFull = event.Count >= event.Max;
var days = (event.Date - DateTime.UtcNow).Days;
```

#### Constants and Configuration
```csharp
// ✅ Good - Clearly identifies purpose and scope
public const int MaxEventCapacity = 60;
public const int RefundCutoffHours = 48;
public const string DefaultEventTimeZone = "US/Eastern";
public static readonly TimeSpan RegistrationCacheTimeout = TimeSpan.FromMinutes(5);

// ❌ Bad - Magic numbers or unclear scope
public const int Max = 60;
public const int Hours = 48;
public const string TZ = "US/Eastern";
public static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);
```

### Database Conventions
```csharp
// ✅ Good - Clear entity relationships and purpose
public class Event { }
public class EventRegistration { }
public class VettingApplication { }
public class PaymentTransaction { }

// Table names (EF Core conventions)
// Events, EventRegistrations, VettingApplications, PaymentTransactions

// ✅ Good - Descriptive column names
public DateTime EventStartDateTime { get; set; }
public string UserSceneName { get; set; }
public decimal EventPrice { get; set; }
public bool IsVettingApplicationApproved { get; set; }

// ❌ Bad - Ambiguous or abbreviated
public DateTime Start { get; set; }
public string Name { get; set; }  // Which name? Scene name? Legal name? Event name?
public decimal Price { get; set; }
public bool IsApproved { get; set; }  // Approved for what?
```

## Error Handling and Logging

### Result Pattern Usage
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

### Structured Logging
Use structured logging with proper context and correlation:

```csharp
/// <summary>
/// Processes event registration with comprehensive logging for troubleshooting
/// and business intelligence. Logs include user context, event details, and
/// outcome for complete audit trail.
/// </summary>
public async Task<RegistrationResult> RegisterForEventAsync(int userId, int eventId)
{
    using var scope = _logger.BeginScope(new Dictionary<string, object>
    {
        ["UserId"] = userId,
        ["EventId"] = eventId,
        ["OperationType"] = "EventRegistration"
    });

    _logger.LogInformation("Starting event registration process for user {UserId} and event {EventId}",
        userId, eventId);

    try
    {
        // Check user eligibility
        var eligibilityCheck = await CheckUserEligibilityAsync(userId, eventId);
        if (!eligibilityCheck.IsEligible)
        {
            _logger.LogWarning("User {UserId} not eligible for event {EventId}: {Reason}",
                userId, eventId, eligibilityCheck.Reason);
            return RegistrationResult.NotEligible(eligibilityCheck.Reason);
        }

        // Check event capacity
        var capacityCheck = await CheckEventCapacityAsync(eventId);
        if (!capacityCheck.HasCapacity)
        {
            _logger.LogInformation("Event {EventId} at capacity, adding user {UserId} to waitlist",
                eventId, userId);
            return await AddToWaitlistAsync(userId, eventId);
        }

        // Process registration
        var registration = await CreateRegistrationAsync(userId, eventId);
        
        _logger.LogInformation("Successfully registered user {UserId} for event {EventId}. " +
            "Registration ID: {RegistrationId}",
            userId, eventId, registration.Id);

        return RegistrationResult.Success(registration);
    }
    catch (PaymentException ex)
    {
        _logger.LogError(ex, "Payment failed for user {UserId} registering for event {EventId}",
            userId, eventId);
        return RegistrationResult.PaymentFailed(ex.Message);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error during event registration for user {UserId} and event {EventId}",
            userId, eventId);
        return RegistrationResult.SystemError("Registration could not be completed at this time");
    }
}
```

## Testing Standards

### Test Organization and Naming
```csharp
/// <summary>
/// Tests for EventService focusing on event registration business logic.
/// Tests are organized by method being tested, then by scenario.
/// Each test follows the Arrange-Act-Assert pattern with clear naming
/// that describes the scenario and expected outcome.
/// </summary>
public class EventServiceTests
{
    /// <summary>
    /// Tests successful event registration when all conditions are met:
    /// - User is eligible (vetted, not already registered)
    /// - Event has available capacity
    /// - Payment processing succeeds
    /// </summary>
    [Fact]
    public async Task RegisterForEventAsync_WhenUserEligibleAndEventHasCapacity_ShouldCreateRegistration()
    {
        // Arrange - Set up test data and dependencies
        var userId = 123;
        var eventId = 456;
        var mockEvent = CreateMockEvent(eventId, capacity: 20, registeredCount: 15);
        var mockUser = CreateMockUser(userId, isVetted: true);
        
        var mockRepository = new Mock<IEventRepository>();
        mockRepository.Setup(r => r.GetEventAsync(eventId))
                     .ReturnsAsync(mockEvent);
        
        var service = new EventService(mockRepository.Object, _logger, _cache);

        // Act - Execute the method being tested
        var result = await service.RegisterForEventAsync(userId, eventId);

        // Assert - Verify the expected outcome
        Assert.True(result.IsSuccess);
        Assert.Equal(RegistrationStatus.Confirmed, result.Registration.Status);
        mockRepository.Verify(r => r.CreateRegistrationAsync(
            It.Is<Registration>(reg => reg.UserId == userId && reg.EventId == eventId)), 
            Times.Once);
    }

    /// <summary>
    /// Tests that registration fails appropriately when event is at capacity,
    /// and user is added to waitlist instead of being given confirmed registration.
    /// </summary>
    [Fact]
    public async Task RegisterForEventAsync_WhenEventAtCapacity_ShouldAddUserToWaitlist()
    {
        // Test implementation with clear documentation of the scenario
    }
}
```

### Test Data Management
```csharp
/// <summary>
/// Provides consistent test data creation methods to ensure tests are
/// reliable and maintainable. Uses builder pattern for complex objects
/// and provides reasonable defaults while allowing customization.
/// </summary>
public static class TestDataBuilder
{
    /// <summary>
    /// Creates a mock event with sensible defaults for testing.
    /// Allows customization of key properties while providing
    /// realistic default values for all required fields.
    /// </summary>
    public static Event CreateEvent(
        int id = 1,
        string name = "Test Event",
        DateTime? startTime = null,
        int capacity = 20,
        int registeredCount = 0,
        EventType type = EventType.Class,
        decimal price = 50.00m)
    {
        return new Event
        {
            Id = id,
            Name = name,
            StartTime = startTime ?? DateTime.UtcNow.AddDays(7), // Default to one week from now
            Capacity = capacity,
            RegisteredCount = registeredCount,
            Type = type,
            Price = price,
            Description = $"Test description for {name}",
            Location = "Test Location",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };
    }
}
```

## Performance Considerations

### Database Query Optimization
```csharp
/// <summary>
/// Retrieves upcoming events with optimized querying to minimize database load.
/// Uses selective loading, appropriate indexing hints, and result caching
/// to ensure responsive performance even with large event datasets.
/// 
/// Performance optimizations applied:
/// - Select only required columns (projection)
/// - Use indexed columns for filtering (StartTime, IsActive)
/// - Include related data in single query to avoid N+1 problems
/// - Cache results for frequently accessed data
/// - Use async methods to avoid blocking threads
/// </summary>
public async Task<List<EventSummaryDto>> GetUpcomingEventsAsync(int maxResults = 50)
{
    const string cacheKey = "upcoming_events";
    
    // Check cache first - upcoming events change infrequently
    if (_cache.TryGetValue<List<EventSummaryDto>>(cacheKey, out var cachedEvents))
    {
        return cachedEvents;
    }

    // Query with explicit projection to minimize data transfer
    // Only select columns needed for the summary view
    var events = await _dbContext.Events
        .Where(e => e.StartTime > DateTime.UtcNow && e.IsActive) // Use indexed columns
        .Include(e => e.Organizer) // Fetch related data in single query
        .OrderBy(e => e.StartTime) // Use indexed column for ordering
        .Take(maxResults) // Limit results to prevent large data transfers
        .Select(e => new EventSummaryDto // Project to DTO to minimize data transfer
        {
            Id = e.Id,
            Name = e.Name,
            StartTime = e.StartTime,
            Price = e.Price,
            AvailableSpots = e.Capacity - e.RegisteredCount,
            OrganizerName = e.Organizer.SceneName
        })
        .ToListAsync();

    // Cache for 10 minutes - balance between performance and data freshness
    _cache.Set(cacheKey, events, TimeSpan.FromMinutes(10));
    
    return events;
}
```

## Security Best Practices

### Input Validation and Sanitization
```csharp
/// <summary>
/// Validates and sanitizes vetting application input to prevent security
/// vulnerabilities while preserving data integrity. Applies multiple
/// layers of validation including format, content, and business rules.
/// 
/// Security measures implemented:
/// - Input length limits to prevent buffer overflow attacks
/// - HTML encoding to prevent XSS attacks
/// - SQL injection prevention through parameterized queries
/// - Business rule validation to prevent invalid state
/// - Audit logging for security monitoring
/// </summary>
public async Task<Result<VettingApplication>> SubmitVettingApplicationAsync(VettingApplicationRequest request)
{
    // Input validation - first line of defense
    if (request == null)
    {
        _logger.LogWarning("Vetting application submission attempted with null request");
        return Result<VettingApplication>.Failure("Invalid application data");
    }

    // Sanitize text inputs to prevent XSS attacks
    var sanitizedRequest = new VettingApplicationRequest
    {
        SceneName = SanitizeInput(request.SceneName, maxLength: 100),
        Email = SanitizeEmail(request.Email),
        Experience = SanitizeInput(request.Experience, maxLength: 2000),
        References = SanitizeInput(request.References, maxLength: 1000),
        ReasonForJoining = SanitizeInput(request.ReasonForJoining, maxLength: 2000)
    };

    // Business rule validation
    var validationResult = await _validator.ValidateAsync(sanitizedRequest);
    if (!validationResult.IsValid)
    {
        _logger.LogWarning("Vetting application failed validation: {Errors}",
            string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
        return Result<VettingApplication>.Failure("Application data is invalid");
    }

    // Create application entity with audit trail
    var application = new VettingApplication
    {
        SceneName = sanitizedRequest.SceneName,
        Email = sanitizedRequest.Email.ToLowerInvariant(), // Normalize email
        Experience = sanitizedRequest.Experience,
        References = sanitizedRequest.References,
        ReasonForJoining = sanitizedRequest.ReasonForJoining,
        SubmittedAt = DateTime.UtcNow,
        Status = VettingStatus.Pending,
        IpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
    };

    try
    {
        await _repository.CreateVettingApplicationAsync(application);
        
        // Log successful submission for audit trail
        _logger.LogInformation("Vetting application submitted successfully for {Email} from IP {IpAddress}",
            application.Email, application.IpAddress);

        return Result<VettingApplication>.Success(application);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to save vetting application for {Email}", request.Email);
        return Result<VettingApplication>.Failure("Could not submit application at this time");
    }
}

/// <summary>
/// Sanitizes text input to prevent XSS attacks while preserving readability.
/// Removes potentially dangerous HTML/script content and enforces length limits.
/// </summary>
private string SanitizeInput(string input, int maxLength)
{
    if (string.IsNullOrWhiteSpace(input))
        return string.Empty;

    // Remove potentially dangerous content
    var sanitized = HttpUtility.HtmlEncode(input.Trim());
    
    // Enforce length limits
    if (sanitized.Length > maxLength)
    {
        sanitized = sanitized.Substring(0, maxLength);
    }

    return sanitized;
}
```

## Code Review Checklist

When reviewing code, ensure the following standards are met:

### Functionality
- [ ] Code does what it's supposed to do according to requirements
- [ ] Edge cases are handled appropriately
- [ ] Error conditions are handled gracefully
- [ ] Business rules are correctly implemented

### SOLID Principles
- [ ] Classes have single, clear responsibilities
- [ ] Code is open for extension without modification
- [ ] Implementations can be substituted without breaking functionality
- [ ] Interfaces are focused and client-specific
- [ ] Dependencies are on abstractions, not concrete implementations

### Documentation
- [ ] All public methods have XML documentation
- [ ] Complex business logic is explained with comments
- [ ] Non-obvious decisions are documented
- [ ] Examples are provided for complex APIs

### Security
- [ ] Input is validated and sanitized
- [ ] Authentication/authorization is properly implemented
- [ ] Sensitive data is properly protected
- [ ] No secrets or credentials in code

### Performance
- [ ] Database queries are optimized
- [ ] Caching is used appropriately
- [ ] Async/await is used for I/O operations
- [ ] Memory usage is reasonable

### Testing
- [ ] Unit tests cover key scenarios
- [ ] Tests are readable and maintainable
- [ ] Test data is realistic and consistent
- [ ] Integration tests verify end-to-end functionality

### Maintainability
- [ ] Code follows established patterns and conventions
- [ ] Names are clear and descriptive
- [ ] Methods are focused and not too long
- [ ] Dependencies are clearly defined

## Conclusion

These coding standards ensure that the WitchCityRope codebase remains maintainable, secure, and performant as it evolves. By following SOLID principles where appropriate, maintaining comprehensive documentation, and adhering to consistent conventions, we create code that is easy to understand, modify, and extend.

Remember: The goal is not to follow these principles blindly, but to use them as tools to create better, more maintainable software that serves the community effectively.