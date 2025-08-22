# Backend Developer Implementation Guide - Simple Vertical Slice Architecture
<!-- Last Updated: 2025-08-22 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Executive Summary

This guide provides comprehensive implementation patterns for the **Simple Vertical Slice Architecture** used in WitchCityRope API development. The architecture prioritizes **SIMPLICITY ABOVE ALL** - no MediatR, no CQRS, no complex patterns. Just maintainable, performant code using direct Entity Framework services with minimal API endpoints.

### Key Principles
- **Direct Entity Framework Services**: No repository patterns, no abstraction layers
- **Feature-Based Organization**: Group related functionality together
- **Minimal API Endpoints**: Simple endpoint registration with direct service calls
- **Simple Error Handling**: Basic Result pattern instead of complex pipelines

---

## Working Implementation Reference

**ALWAYS reference the working Health feature example**: 
- Location: `/home/chad/repos/witchcityrope-react/apps/api/Features/Health/`
- Status: Complete implementation showing all patterns
- Purpose: Template for all new features

### Health Feature Structure (COPY THIS PATTERN)
```
Features/Health/
├── Services/
│   └── HealthService.cs         # Direct Entity Framework service
├── Endpoints/
│   └── HealthEndpoints.cs       # Minimal API endpoint registration
└── Models/
    ├── HealthResponse.cs        # Response DTOs for NSwag
    └── DetailedHealthResponse.cs
```

---

## 🚨 CRITICAL: Anti-Patterns (NEVER DO THIS)

### ❌ FORBIDDEN PATTERNS (IMMEDIATE VIOLATION)
```csharp
// ❌ NEVER use MediatR
using MediatR;
services.AddMediatR();
private readonly IMediator _mediator;

// ❌ NEVER create handlers
public class GetHealthHandler : IRequestHandler<GetHealthQuery, HealthResponse>

// ❌ NEVER use CQRS patterns
public record GetHealthQuery : IRequest<HealthResponse>;
public record UpdateHealthCommand : IRequest<bool>;

// ❌ NEVER create pipeline behaviors
public class ValidationPipelineBehavior<TRequest, TResponse>

// ❌ NEVER use complex repository patterns
public interface IHealthRepository
public class HealthRepository : IHealthRepository
```

### ✅ REQUIRED PATTERNS (ALWAYS DO THIS)
```csharp
// ✅ Direct Entity Framework service injection
public HealthService(ApplicationDbContext context, ILogger<HealthService> logger)

// ✅ Simple service registration
services.AddScoped<HealthService>();

// ✅ Direct minimal API endpoints
app.MapGet("/api/health", async (HealthService service) => { ... });

// ✅ Feature-based folder organization
Features/[FeatureName]/Services/
Features/[FeatureName]/Endpoints/
Features/[FeatureName]/Models/
```

---

## Implementation Patterns

### 1. Service Pattern (Direct Entity Framework)

**Template** (use exact pattern):
```csharp
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.[FeatureName].Models;

namespace WitchCityRope.Api.Features.[FeatureName].Services;

/// <summary>
/// [Description] service using direct Entity Framework access
/// Example of the simplified vertical slice architecture pattern
/// </summary>
public class [FeatureName]Service
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<[FeatureName]Service> _logger;

    public [FeatureName]Service(ApplicationDbContext context, ILogger<[FeatureName]Service> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// [Description] - Simple Entity Framework service - NO MediatR complexity
    /// </summary>
    public async Task<(bool Success, [Response]? Response, string Error)> [Method]Async(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Direct Entity Framework queries - SIMPLE approach
            var result = await _context.[Entity]
                .AsNoTracking()  // For read-only queries
                .Where(...)
                .ToListAsync(cancellationToken);
            
            var response = new [Response]
            {
                // Map entity to DTO
            };

            _logger.LogDebug("[Description] completed successfully");
            return (true, response, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Description] failed");
            return (false, null, "[Error message]");
        }
    }
}
```

**Real Example from Health Service**:
```csharp
public async Task<(bool Success, HealthResponse? Response, string Error)> GetHealthAsync(
    CancellationToken cancellationToken = default)
{
    try
    {
        // Simple database connectivity check using direct Entity Framework
        var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
        
        if (!canConnect)
        {
            return (false, null, "Database connection failed");
        }

        // Get basic database statistics
        var userCount = await _context.Users
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var response = new HealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            DatabaseConnected = canConnect,
            UserCount = userCount,
            Version = "1.0.0"
        };

        return (true, response, string.Empty);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Health check failed");
        return (false, null, "Health check failed");
    }
}
```

### 2. Endpoint Pattern (Minimal API)

**Template** (use exact pattern):
```csharp
using WitchCityRope.Api.Features.[FeatureName].Models;
using WitchCityRope.Api.Features.[FeatureName].Services;

namespace WitchCityRope.Api.Features.[FeatureName].Endpoints;

/// <summary>
/// [FeatureName] minimal API endpoints
/// Example of simple vertical slice endpoint registration - NO MediatR complexity
/// </summary>
public static class [FeatureName]Endpoints
{
    /// <summary>
    /// Register [FeatureName] endpoints using minimal API pattern
    /// Shows simple direct service injection pattern
    /// </summary>
    public static void Map[FeatureName]Endpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/[feature]/[action]", async (
            [FeatureName]Service service,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await service.[Method]Async(cancellationToken);
                
                return success 
                    ? Results.Ok(response)
                    : Results.Problem(
                        title: "[Action] Failed",
                        detail: error,
                        statusCode: 400);
            })
            .WithName("[ActionName]")
            .WithSummary("[Description]")
            .WithDescription("[Detailed description]")
            .WithTags("[FeatureName]")
            .Produces<[Response]>(200)
            .Produces(400);
    }
}
```

**Real Example from Health Endpoints**:
```csharp
public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
{
    // Basic health check endpoint
    app.MapGet("/api/health", async (
        HealthService healthService,
        CancellationToken cancellationToken) =>
        {
            var (success, response, error) = await healthService.GetHealthAsync(cancellationToken);
            
            return success 
                ? Results.Ok(response)
                : Results.Problem(
                    title: "Health Check Failed",
                    detail: error,
                    statusCode: 503);
        })
        .WithName("GetHealth")
        .WithSummary("Get basic API health status")
        .WithDescription("Returns basic health information including database connectivity and user count")
        .WithTags("Health")
        .Produces<HealthResponse>(200)
        .Produces(503);
}
```

### 3. Model Pattern (DTOs for NSwag)

**Template**:
```csharp
namespace WitchCityRope.Api.Features.[FeatureName].Models;

/// <summary>
/// [Description] DTO for NSwag type generation
/// </summary>
public class [Name]Response
{
    /// <summary>
    /// [Property description]
    /// </summary>
    public string Property { get; set; } = string.Empty;

    /// <summary>
    /// [Property description]
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// [Description] request DTO for validation
/// </summary>
public class [Name]Request
{
    /// <summary>
    /// [Property description]
    /// </summary>
    public string Property { get; set; } = string.Empty;
}
```

**Real Example from Health Models**:
```csharp
/// <summary>
/// Basic health check response DTO
/// Example of simple response model for NSwag type generation
/// </summary>
public class HealthResponse
{
    /// <summary>
    /// Overall health status of the API
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when health check was performed
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Database connectivity status
    /// </summary>
    public bool DatabaseConnected { get; set; }

    /// <summary>
    /// Total number of users in the system
    /// </summary>
    public int UserCount { get; set; }

    /// <summary>
    /// API version
    /// </summary>
    public string Version { get; set; } = string.Empty;
}
```

---

## Registration Patterns

### Service Registration
**Location**: Extensions/ServiceCollectionExtensions.cs
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {
        // Register all feature services
        services.AddScoped<HealthService>();
        services.AddScoped<[Feature]Service>();
        
        return services;
    }
}
```

### Endpoint Registration
**Location**: Extensions/WebApplicationExtensions.cs
```csharp
public static class WebApplicationExtensions
{
    public static WebApplication MapFeatureEndpoints(this WebApplication app)
    {
        // Register all feature endpoints
        app.MapHealthEndpoints();
        app.Map[Feature]Endpoints();
        
        return app;
    }
}
```

### Program.cs Integration
```csharp
// Service registration
builder.Services.AddFeatureServices();

// Endpoint registration
app.MapFeatureEndpoints();
```

---

## Database Patterns

### Direct Entity Framework Queries
```csharp
// ✅ CORRECT: Direct DbContext queries
var users = await _context.Users
    .AsNoTracking()  // Always for read-only
    .Where(u => u.IsActive)
    .OrderBy(u => u.Name)
    .ToListAsync(cancellationToken);

// ✅ CORRECT: Write operations
var user = new User { Name = "Test" };
_context.Users.Add(user);
await _context.SaveChangesAsync(cancellationToken);

// ❌ WRONG: Repository pattern
var users = await _userRepository.GetActiveUsersAsync();
```

### Transaction Handling (When Needed)
```csharp
public async Task<(bool Success, string Error)> ComplexOperationAsync(...)
{
    using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    
    try
    {
        // Multiple operations
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        
        _context.Events.Add(evt);
        await _context.SaveChangesAsync(cancellationToken);
        
        await transaction.CommitAsync(cancellationToken);
        return (true, string.Empty);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync(cancellationToken);
        _logger.LogError(ex, "Complex operation failed");
        return (false, "Operation failed");
    }
}
```

---

## Validation Patterns (FluentValidation)

### Request Validation
**Location**: Features/[FeatureName]/Validation/
```csharp
using FluentValidation;
using WitchCityRope.Api.Features.[FeatureName].Models;

namespace WitchCityRope.Api.Features.[FeatureName].Validation;

public class [Request]Validator : AbstractValidator<[Request]>
{
    public [Request]Validator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must be 100 characters or less");
    }
}
```

### Endpoint Integration
```csharp
app.MapPost("/api/[feature]", async (
    [Request] request,
    [FeatureName]Service service,
    IValidator<[Request]> validator,
    CancellationToken cancellationToken) =>
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.BadRequest(validationResult.Errors);
        }

        var (success, response, error) = await service.[Method]Async(request, cancellationToken);
        return success ? Results.Ok(response) : Results.BadRequest(error);
    });
```

---

## Testing Integration

### Service Testing Pattern
```csharp
[Test]
public async Task GetHealthAsync_WhenDatabaseConnected_ReturnsHealthyStatus()
{
    // Arrange
    using var context = new ApplicationDbContext(DatabaseTestFixture.GetDbContextOptions());
    var logger = new Mock<ILogger<HealthService>>();
    var service = new HealthService(context, logger.Object);

    // Act
    var (success, response, error) = await service.GetHealthAsync();

    // Assert
    success.Should().BeTrue();
    response.Should().NotBeNull();
    response.Status.Should().Be("Healthy");
    error.Should().BeEmpty();
}
```

---

## Common Implementation Scenarios

### 1. Simple CRUD Feature
**Example: User Profile Management**
```
Features/UserProfile/
├── Services/
│   └── UserProfileService.cs    # GetAsync, UpdateAsync, DeleteAsync
├── Endpoints/  
│   └── UserProfileEndpoints.cs  # GET/PUT/DELETE /api/userprofile
├── Models/
│   ├── UserProfileResponse.cs   # Response DTO
│   └── UpdateUserProfileRequest.cs # Request DTO
└── Validation/
    └── UpdateUserProfileValidator.cs # FluentValidation rules
```

### 2. Complex Business Logic
**Example: Event Registration**
```
Features/EventRegistration/
├── Services/
│   └── EventRegistrationService.cs  # RegisterAsync, CancelAsync, GetRegistrationsAsync
├── Endpoints/
│   └── EventRegistrationEndpoints.cs # POST/DELETE/GET /api/events/{id}/registration
├── Models/
│   ├── EventRegistrationResponse.cs
│   └── RegisterForEventRequest.cs
└── Validation/
    └── RegisterForEventValidator.cs
```

### 3. Query-Heavy Feature
**Example: Event Search**
```
Features/EventSearch/
├── Services/
│   └── EventSearchService.cs       # SearchAsync with filtering/paging
├── Endpoints/
│   └── EventSearchEndpoints.cs     # GET /api/events/search
├── Models/
│   ├── EventSearchResponse.cs      # Paged results
│   └── EventSearchRequest.cs       # Search criteria
└── Validation/
    └── EventSearchValidator.cs     # Search parameter validation
```

---

## Performance Considerations

### Database Query Optimization
```csharp
// ✅ CORRECT: Efficient queries
var events = await _context.Events
    .AsNoTracking()  // Read-only
    .Include(e => e.Instructor)  // Explicit includes
    .Where(e => e.Date >= DateTime.UtcNow)
    .OrderBy(e => e.Date)
    .Take(50)  // Limit results
    .ToListAsync(cancellationToken);

// ❌ WRONG: Inefficient queries
var events = await _context.Events
    .Include(e => e.Instructor)
        .ThenInclude(i => i.Profile)  // Over-inclusion
    .ToListAsync(); // No filtering
```

### Caching (When Appropriate)
```csharp
// Simple memory caching for reference data
public async Task<(bool Success, List<EventType> Types, string Error)> GetEventTypesAsync(
    CancellationToken cancellationToken = default)
{
    const string cacheKey = "EventTypes";
    
    if (_memoryCache.TryGetValue(cacheKey, out List<EventType> cachedTypes))
    {
        return (true, cachedTypes, string.Empty);
    }
    
    try
    {
        var types = await _context.EventTypes
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
            
        _memoryCache.Set(cacheKey, types, TimeSpan.FromMinutes(30));
        return (true, types, string.Empty);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get event types");
        return (false, new List<EventType>(), "Failed to get event types");
    }
}
```

---

## Error Handling Patterns

### Service-Level Error Handling
```csharp
public async Task<(bool Success, [Response]? Response, string Error)> [Method]Async(...)
{
    try
    {
        // Business logic
        return (true, response, string.Empty);
    }
    catch (ValidationException ex)
    {
        _logger.LogWarning(ex, "Validation failed for {Method}", nameof([Method]));
        return (false, null, ex.Message);
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "Database update failed for {Method}", nameof([Method]));
        return (false, null, "Database operation failed");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "{Method} failed unexpectedly", nameof([Method]));
        return (false, null, "An unexpected error occurred");
    }
}
```

### Endpoint-Level Error Handling
```csharp
app.MapPost("/api/[feature]", async ([Request] request, [Service] service) =>
{
    var (success, response, error) = await service.[Method]Async(request);
    
    return success 
        ? Results.Ok(response)
        : Results.Problem(
            title: "Operation Failed",
            detail: error,
            statusCode: error.Contains("validation") ? 400 : 500);
});
```

---

## Implementation Checklist

### ✅ Before Starting Feature Implementation
- [ ] Study Health feature example in `/apps/api/Features/Health/`
- [ ] Verify no MediatR references in codebase
- [ ] Confirm ApplicationDbContext is available
- [ ] Check existing similar features for patterns

### ✅ During Feature Implementation
- [ ] Create feature folder: `Features/[FeatureName]/`
- [ ] Create service with direct EF access
- [ ] Create minimal API endpoints
- [ ] Create DTOs with XML documentation
- [ ] Add FluentValidation if complex validation needed
- [ ] Register service and endpoints in Program.cs

### ✅ After Feature Implementation
- [ ] Write service unit tests
- [ ] Write integration tests with real database
- [ ] Test NSwag type generation
- [ ] Update API documentation
- [ ] Verify no MediatR patterns introduced

### ✅ Code Review Checklist
- [ ] No MediatR/CQRS patterns
- [ ] Direct Entity Framework service usage
- [ ] Proper feature folder organization
- [ ] Minimal API endpoint patterns
- [ ] XML documentation on public APIs
- [ ] Appropriate error handling
- [ ] Logging with proper log levels

---

## Success Metrics

### Code Quality Indicators
- **Simplicity**: New developers can understand feature in <15 minutes
- **Performance**: Sub-100ms response times for typical queries
- **Maintainability**: No circular dependencies, clear separation of concerns
- **Testability**: High unit test coverage with fast-running tests

### Anti-Pattern Detection
- **Zero MediatR References**: No `using MediatR;` statements
- **Zero Handler Classes**: No `IRequestHandler` implementations
- **Zero CQRS**: No command/query separation complexity
- **Zero Repository Patterns**: Direct Entity Framework usage only

### Feature Completeness
- **Service Implementation**: Direct EF service with error handling
- **Endpoint Registration**: Minimal API with proper OpenAPI documentation
- **DTO Models**: Clean request/response models with validation
- **Test Coverage**: Unit and integration tests with real database

---

Remember: **SIMPLICITY ABOVE ALL**. If you're thinking about adding MediatR, CQRS, or complex patterns - STOP. The Health feature example shows everything you need. Copy that pattern exactly for consistent, maintainable code.