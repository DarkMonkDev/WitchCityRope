# WitchCityRope Coding Standards

## 🚨 CRITICAL: DTO Alignment Strategy 🚨

**ALL DEVELOPERS MUST READ**: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`

**MANDATORY DTO RULES**:
- ✅ **API DTOs are SOURCE OF TRUTH** - Frontend adapts to backend
- ✅ **TypeScript interfaces must match C# DTOs exactly**
- ✅ **30-day notice required for breaking DTO changes**
- ❌ **NEVER modify DTOs without frontend coordination**

## Overview

This document outlines the core coding principles and conventions used throughout the WitchCityRope project. For specific implementation patterns, see the specialized standards documents below.

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
    public async Task<RegistrationResult> RegisterForEventAsync(
        int eventId,
        int userId,
        PaymentInfo paymentInfo = null)
    {
        // Implementation with detailed comments explaining each step
    }
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
- [ ] Database queries are optimized and meet timing thresholds
- [ ] API endpoints respond within target times
- [ ] Caching is used appropriately
- [ ] Async/await is used for I/O operations

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

## Related Documentation

### Backend Development Standards
- [Service Layer Patterns](./backend/service-layer-patterns.md) - Service implementation patterns
- [Error Handling Patterns](./backend/error-handling-patterns.md) - Error handling and logging
- [Performance Standards](./backend/performance-standards.md) - Performance benchmarks and optimization
- [Security Patterns](./backend/security-patterns.md) - Security best practices
- [Entity Framework Patterns](./development-standards/entity-framework-patterns.md) - Database access patterns
- [Authentication Patterns](./development-standards/authentication-patterns.md) - Authentication service patterns

### Frontend Development Standards
- [React Patterns](./development-standards/react-patterns.md) - React component patterns
- [TypeScript Patterns](./development-standards/typescript-patterns.md) - TypeScript best practices

### Testing Standards
- [Test Standards](./testing/test-standards.md) - Test organization and quality
- [Testing Guide](./testing/TESTING_GUIDE.md) - Comprehensive testing guide
- [E2E Testing Patterns](./testing/E2E_TESTING_PATTERNS.md) - End-to-end testing with Playwright

### Validation Standards
- [Validation Standards](./validation-standardization/VALIDATION_STANDARDS.md) - Validation architecture
- [Form Fields and Validation Standards](./form-fields-and-validation-standards.md) - Form validation patterns
