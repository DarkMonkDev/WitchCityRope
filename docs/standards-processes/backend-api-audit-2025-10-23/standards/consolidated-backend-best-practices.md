# WitchCityRope Backend API - Consolidated Best Practices

**Last Updated:** 2025-10-23
**Version:** 1.0
**Status:** ACTIVE - PRIMARY REFERENCE FOR ALL BACKEND DEVELOPMENT
**Audit Source:** October 2025 API Standards Audit

## Document Purpose

This document consolidates research from Milan Jovanović (October 2025), Microsoft .NET 10 official guidance, and analysis of our current implementation into a **single source of truth** for backend API development at WitchCityRope.

**Supersedes:** Individual research documents (retained for reference)
**Audience:** Backend developers, code reviewers, AI agents
**Compliance:** MANDATORY for all new code, RECOMMENDED for refactoring

---

## Table of Contents

1. [Architecture Principles](#1-architecture-principles)
2. [Vertical Slice Pattern Implementation](#2-vertical-slice-pattern-implementation)
3. [Minimal API Endpoint Patterns](#3-minimal-api-endpoint-patterns)
4. [Service Layer Best Practices](#4-service-layer-best-practices)
5. [Entity Framework Core Optimization](#5-entity-framework-core-optimization)
6. [DTO and Type Generation](#6-dto-and-type-generation)
7. [Error Handling and Validation](#7-error-handling-and-validation)
8. [Performance Optimization](#8-performance-optimization)
9. [Testing Patterns](#9-testing-patterns)
10. [Security Best Practices](#10-security-best-practices)
11. [Common Anti-Patterns to Avoid](#11-common-anti-patterns-to-avoid)
12. [Code Examples and Templates](#12-code-examples-and-templates)

---

## 1. Architecture Principles

### 1.1 Vertical Slice Architecture (PRIMARY PATTERN)

**Principle:** Features are organized as self-contained vertical slices from endpoint to database.

**Milan's Latest (September 2025):**
> "Vertical Slice Architecture is **easier than you think**"

**✅ ADOPTED Pattern (Simplified):**
```
Features/[FeatureName]/
├── Endpoints/         # Minimal API endpoint definitions
├── Models/            # Request/Response DTOs
├── Services/          # Business logic (direct EF Core)
├── Configuration/     # EF Core configurations (singular, at root)
└── Validators/        # FluentValidation validators (plural)
```

**Rationale:**
- [From research summary] 87% alignment with Milan Jovanović's latest patterns
- [From implementation analysis] 17 features successfully implemented
- [Milan, Sept 2025] "Vertical Slice Architecture is easier than you think"

**✅ DO:**
- Keep features self-contained
- Direct service injection (no MediatR)
- Feature-specific entities in feature folder when appropriate
- Use Configuration/ folder (singular) at feature root for EF Core configurations
- Use Validators/ folder (plural) consistent with FluentValidation naming

**❌ DON'T:**
- Use MediatR/CQRS (eliminated for simplicity)
- Create repository abstractions over EF Core
- Share entities across feature boundaries unnecessarily
- Mix folder naming: Data/ vs Configuration/ vs Configurations/
- Nest configurations: Entities/Configuration/ (should be feature-root Configuration/)

**Current Compliance:** 82% (14/17 features fully compliant)

---

### 1.2 Folder Naming Standards (CONSISTENCY CRITICAL)

**Issue Identified:** [From implementation analysis] Inconsistent folder naming across features

**✅ STANDARD:**
```
Configuration/   (singular, at feature root - for EF Core configurations)
Validators/      (plural, FluentValidation convention)
Endpoints/       (plural)
Models/          (plural)
Services/        (plural)
```

**❌ VIOLATIONS TO FIX:**
```
Data/                      # Use Configuration/ instead
Configurations/            # Singular, not plural
Entities/Configuration/    # Don't nest, use feature root
Validation/               # Use Validators/ instead
```

**Migration Required:** [From analysis] 6 features need folder renaming

**Files Currently Non-Compliant:**
- CheckIn: Uses `Entities/Configuration/` (nested)
- Cms: Uses `Configurations/` (plural)
- Vetting: Uses `Entities/Configuration/` (nested)
- Participation: Uses `Data/` instead of `Configuration/`
- Payments: Uses `Data/` instead of `Configuration/`
- Safety: Has both `Validation/` AND `Validators/` folders

**Action Required:** Standardize all to feature-root `Configuration/` and `Validators/` folders.

---

## 2. Vertical Slice Pattern Implementation

### 2.1 Feature Organization

**Pattern Compliance:** [From analysis] 82% of features follow standard pattern

**Template Structure:**
```csharp
// Features/[FeatureName]/Endpoints/[FeatureName]Endpoints.cs
public static class FeatureNameEndpoints
{
    public static void MapFeatureNameEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/featurename")
            .WithTags("FeatureName")
            .WithOpenApi();

        // Endpoint definitions here
    }
}
```

**Real Example:** [From Health feature]
```csharp
// Features/Health/Endpoints/HealthEndpoints.cs
public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health/status", async (HealthService healthService) =>
        {
            var (success, response, error) = await healthService.GetHealthStatusAsync();
            return success
                ? Results.Ok(response)
                : Results.Problem(title: "Health Check Failed", detail: error);
        })
        .WithName("GetHealthStatus")
        .Produces<HealthResponse>(200)
        .Produces<ProblemDetails>(500);
    }
}
```

**Current Implementation:** 17 features implemented following this pattern

---

### 2.2 Service Layer Pattern

**✅ ADOPTED: Direct EF Core Services (No Repository Pattern)**

**Rationale:**
- [Milan] "Repository pattern over EF Core adds unnecessary abstraction"
- [Implementation] 40-60% faster development velocity
- [Research] Simpler debugging and query optimization

**Service Template:**
```csharp
public class FeatureService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FeatureService> _logger;

    public FeatureService(ApplicationDbContext context, ILogger<FeatureService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, ResponseDto? Data, string Error)> GetDataAsync(CancellationToken ct)
    {
        try
        {
            var data = await _context.Entities
                .AsNoTracking()  // CRITICAL: Always use for read-only queries
                .Include(e => e.RelatedEntity)
                .Where(e => e.IsActive)
                .Select(e => new ResponseDto  // Project to DTO in query
                {
                    Id = e.Id,
                    Name = e.Name
                })
                .ToListAsync(ct);

            return (true, data, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get data");
            return (false, null, "Database error occurred");
        }
    }
}
```

**Key Points:**
- Tuple return pattern: `(bool Success, T? Data, string Error)`
- Direct `ApplicationDbContext` injection
- Always use `AsNoTracking()` for read-only queries
- Project to DTOs in LINQ (server-side projection)

**Current Usage:** 15/17 features (88%) use Services/ pattern correctly

---

## 3. Minimal API Endpoint Patterns

### 3.1 Endpoint Definition Standards

**✅ REQUIRED: OpenAPI Metadata** [From analysis: 160 usages = EXCELLENT]

```csharp
app.MapGet("/api/users/{id}", async (Guid id, UserService service) =>
{
    var (success, user, error) = await service.GetUserByIdAsync(id);

    return success
        ? Results.Ok(user)
        : Results.Problem(
            title: "User Not Found",
            detail: error,
            statusCode: 404);
})
.WithName("GetUserById")              // ✅ REQUIRED: Route name
.WithSummary("Get user by ID")        // ✅ REQUIRED: Brief summary
.WithDescription("Returns full user profile including roles and settings")  // ✅ RECOMMENDED
.WithTags("Users")                    // ✅ REQUIRED: API grouping
.Produces<UserDto>(200)              // ✅ CRITICAL: Type metadata for NSwag
.Produces<ProblemDetails>(404)       // ✅ REQUIRED: Error responses
.Produces<ProblemDetails>(500)       // ✅ REQUIRED: Server errors
.RequireAuthorization();              // ✅ If authentication required
```

**Why .Produces<T>() is CRITICAL:**
- [From DTO audit] Enables NSwag type generation for frontend
- [From strategy] API DTOs are source of truth
- [From analysis] 160 usages = excellent coverage, maintain this

**Current Coverage:** 160 occurrences across 14 files ✅ EXCELLENT

---

### 3.2 Route Groups for Organization

**.NET 10 Pattern (Recommended):**
```csharp
public static class EventEndpoints
{
    public static void MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var events = app.MapGroup("/api/events")
            .WithTags("Events")
            .WithOpenApi()
            .RequireAuthorization();  // Apply to all routes

        events.MapGet("/", GetEventsAsync)
            .WithName("GetEvents")
            .Produces<List<EventDto>>(200);

        events.MapGet("/{id}", GetEventAsync)
            .WithName("GetEvent")
            .Produces<EventDto>(200)
            .Produces<ProblemDetails>(404);

        events.MapPost("/", CreateEventAsync)
            .WithName("CreateEvent")
            .Produces<EventDto>(201)
            .Produces<ProblemDetails>(400);
    }
}
```

**Benefits:**
- Reduced code duplication
- Consistent metadata across routes
- Easier policy application
- Cleaner endpoint definitions

**Current Pattern:** Partial - using extension methods, can improve with route groups

---

### 3.3 Error Handling Pattern

**✅ ADOPTED: Results.Problem() for RFC 7807 Compliance**

**Current Usage:** [From analysis] 66 occurrences (needs expansion to ~130)

**Standard Pattern:**
```csharp
app.MapPost("/api/events", async (CreateEventRequest request, EventService service) =>
{
    var (success, eventDto, error) = await service.CreateEventAsync(request);

    if (!success)
    {
        return Results.Problem(
            title: "Event Creation Failed",
            detail: error,
            statusCode: 400,
            type: "https://witchcityrope.com/errors/validation"
        );
    }

    return Results.Created($"/api/events/{eventDto.Id}", eventDto);
})
.Produces<EventDto>(201)
.Produces<ProblemDetails>(400);
```

**Why RFC 7807 Problem Details:**
- Standard error format across all endpoints
- Machine-readable error responses
- Better frontend error handling
- Professional API design

**Migration Required:** [From analysis] ~64 endpoints still using custom error responses

**Priority:** ✅ HIGH - Consistency improvement

---

## 4. Service Layer Best Practices

### 4.1 Tuple Return Pattern (SIMPLIFIED vs Railway-Oriented Programming)

**✅ ADOPTED:** Simple tuple returns
**❌ REJECTED:** Railway-Oriented Programming (too complex for our scale)

**Pattern:**
```csharp
public async Task<(bool Success, TDto? Data, string Error)> MethodAsync()
{
    // Success case
    return (true, data, string.Empty);

    // Failure case
    return (false, null, "Error message");
}
```

**Rationale:**
- [Milan comparison] 80% of benefits with 20% of complexity
- [Research] Team productivity > theoretical purity
- [Implementation] Already used across 14 features

**Why NOT Railway-Oriented Programming:**
```csharp
// ❌ TOO COMPLEX for our scale
public class Result<TValue, TError>
{
    public Result<TNewValue, TError> Bind<TNewValue>(
        Func<TValue, Result<TNewValue, TError>> func)
    {
        // Functional programming complexity
    }
}
```

**Simple Tuple Pattern Benefits:**
- No library dependencies (built-in C# tuples)
- Team familiarity (everyone understands tuples)
- Easy testing (simple to assert on success, data, error)
- Adequate expressiveness for our error handling needs

**Current Usage:** 100% of services use tuple pattern ✅

---

### 4.2 Dependency Injection Pattern

**Standard Service Registration:**
```csharp
// Program.cs or ServiceCollectionExtensions.cs
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<VettingService>();
```

**Service Constructor Pattern:**
```csharp
public class EventService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EventService> _logger;

    // Additional services as needed
    private readonly IEmailService? _emailService;

    public EventService(
        ApplicationDbContext context,
        ILogger<EventService> logger,
        IEmailService? emailService = null)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
    }
}
```

**✅ DO:**
- Use constructor injection
- Inject `ApplicationDbContext` directly
- Inject `ILogger<T>` for logging
- Make optional services nullable with default null

**❌ DON'T:**
- Use service locator pattern
- Create DbContext manually
- Inject repositories (use DbContext directly)
- Skip logging

---

## 5. Entity Framework Core Optimization

### 5.1 AsNoTracking() for Read-Only Queries (CRITICAL PERFORMANCE)

**Current Usage:** [From analysis] 75 occurrences across 14 files
**Target Coverage:** ~95% of read-only queries
**Performance Impact:** 20-40% faster queries, 40% less memory

**✅ ALWAYS USE:**
```csharp
var events = await _context.Events
    .AsNoTracking()  // READ-ONLY = AsNoTracking()
    .Where(e => e.IsPublished)
    .ToListAsync();
```

**❌ DON'T USE for:**
- Update operations (need change tracking)
- Delete operations (need change tracking)
- Any operation calling SaveChangesAsync()

**Missing Coverage:** [From analysis] 15-20 read queries need AsNoTracking() added

**Files With Good Coverage:**
- Health: ✅
- Events: ✅
- Participation: ✅
- Vetting: ✅
- Users: ✅
- Safety: ✅
- CheckIn: ✅
- Volunteers: ✅
- Authentication: ✅

**Files Needing Audit:**
- Cms: ⚠️ Some read queries may be missing AsNoTracking()
- Dashboard: ⚠️ Some read queries may be missing AsNoTracking()
- Metadata: ⚠️ Some read queries may be missing AsNoTracking()

**Action Required:** Comprehensive audit of all GET endpoints for AsNoTracking() compliance

**Priority:** ✅ HIGH - Free performance improvement

---

### 5.2 Server-Side Projection (DTO Mapping in LINQ)

**✅ BEST PRACTICE:**
```csharp
// Good: Project to DTO in database query
var events = await _context.Events
    .AsNoTracking()
    .Select(e => new EventDto
    {
        Id = e.Id,
        Title = e.Title,
        StartDate = e.StartDate,
        // Only properties needed
    })
    .ToListAsync();
```

**❌ ANTI-PATTERN:**
```csharp
// Bad: Load full entities then map
var events = await _context.Events
    .AsNoTracking()
    .ToListAsync();

var dtos = events.Select(e => MapToDto(e)).ToList();  // In-memory mapping
```

**Impact:**
- Server-side projection reduces data transfer
- Reduces memory usage (only properties needed)
- Faster query execution (smaller result set)

**Current Usage:** Widely used across features ✅

---

### 5.3 Compiled Queries (SELECTIVE OPTIMIZATION)

**Status:** [From analysis] Not currently used
**Recommendation:** [From research] Apply selectively to hot paths only

**Pattern:**
```csharp
private static readonly Func<ApplicationDbContext, Guid, Task<Event?>> GetEventByIdCompiled =
    EF.CompileAsyncQuery((ApplicationDbContext context, Guid id) =>
        context.Events
            .AsNoTracking()
            .FirstOrDefault(e => e.Id == id));

// Usage
var evt = await GetEventByIdCompiled(_context, eventId);
```

**When to Use:**
- High-frequency queries (>1000 req/day)
- Performance-critical paths
- After profiling proves benefit (5-15% improvement)

**When NOT to Use:**
- Infrequent queries
- Complex dynamic queries
- Before profiling

**Candidates for Compiled Queries:**
- Event listings (high traffic)
- User profile lookups (frequent)
- Check-in queries (hot path)
- Vetting status checks (repeated)

**Priority:** 🟡 MEDIUM - Profile first, optimize where proven

---

### 5.4 Include vs Explicit Loading

**✅ BEST PRACTICE: Explicit Include for Read Operations**
```csharp
var events = await _context.Events
    .AsNoTracking()
    .Include(e => e.Sessions)
    .Include(e => e.TicketTypes)
    .Where(e => e.IsPublished)
    .ToListAsync();
```

**❌ AVOID: Lazy Loading**
```csharp
// Lazy loading disabled by default - good!
// Causes N+1 query problems
var events = await _context.Events.ToListAsync();
foreach (var evt in events)
{
    var sessions = evt.Sessions; // Triggers separate query per event
}
```

**Current Configuration:** Lazy loading DISABLED ✅

---

## 6. DTO and Type Generation

### 6.1 NSwag Auto-Generation (MANDATORY)

**Status:** [From DTO audit] 62/100 compliance - NEEDS IMPROVEMENT
**Target:** 95/100 compliance

**✅ CRITICAL RULES:**

1. **API DTOs are Source of Truth** [From DTO-ALIGNMENT-STRATEGY.md]
2. **NEVER manually create TypeScript interfaces for API DTOs**
3. **Always add .Produces<T>() to endpoints** (enables type generation)
4. **Frontend imports from @witchcityrope/shared-types ONLY**

**Backend Developer Checklist:**
```csharp
// ✅ DO: Add OpenAPI metadata to ALL endpoints
app.MapGet("/api/users", async (UserService service) =>
{
    var users = await service.GetUsersAsync();
    return Results.Ok(users);
})
.Produces<List<UserDto>>(200)  // CRITICAL: Generates TypeScript type
.Produces<ProblemDetails>(500);

// ❌ DON'T: Omit .Produces<> metadata
app.MapGet("/api/users", async (UserService service) =>
{
    return Results.Ok(await service.GetUsersAsync());  // No type info!
});
```

**Violations Found:** [From DTO audit]
- 13+ frontend files with manual type definitions
- Missing .Produces<> on 6-8 backend endpoints
- Technical debt: 16-20 hours to achieve compliance

**Current Coverage:** 160 .Produces<T>() usages ✅ EXCELLENT foundation

**Action Required:**
1. Add missing .Produces<T>() to remaining endpoints
2. Audit frontend for manual TypeScript interfaces
3. Replace manual types with generated imports
4. Add ESLint rule to prevent future violations

**Priority:** ✅ HIGH - Type safety and DTO alignment

---

### 6.2 DTO Naming Conventions

**Standard Suffixes:**
- `*Dto` - Data transfer object (read operations)
- `*Request` - Command/mutation input
- `*Response` - Command/mutation output
- `*Command` - NOT USED (we don't use MediatR)
- `*Query` - NOT USED (we don't use MediatR)

**Example:**
```csharp
public class UserDto { }              // Read operations
public class CreateUserRequest { }    // Create operations
public class UpdateUserRequest { }    // Update operations
public class UserResponse { }         // Operation results
```

**Current Compliance:** 95% ✅

---

### 6.3 Type Generation Workflow

**Step 1:** Backend exposes OpenAPI specification
```
URL: http://localhost:5655/swagger/v1/swagger.json
Format: OpenAPI 3.0
```

**Step 2:** NSwag generates TypeScript types
```bash
cd packages/shared-types
npm run generate  # Runs NSwag CLI
```

**Step 3:** Frontend imports generated types
```typescript
import type { components } from '@witchcityrope/shared-types';
type UserDto = components['schemas']['UserDto'];
type EventDto = components['schemas']['EventDto'];
```

**Validation:** ✅ Process is correct and functional

**Generated Output:** 8,542 lines of TypeScript types

**Package Location:** `/packages/shared-types/src/generated/`

---

## 7. Error Handling and Validation

### 7.1 FluentValidation Integration

**✅ ADOPTED:** [From analysis] FluentValidation used across features

**Validator Pattern:**
```csharp
// Features/Events/Validators/CreateEventRequestValidator.cs
public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.StartDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Start date must be in the future");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Capacity must be greater than 0")
            .LessThanOrEqualTo(500).WithMessage("Capacity cannot exceed 500");
    }
}
```

**Endpoint Integration:**
```csharp
app.MapPost("/api/events", async (
    CreateEventRequest request,
    IValidator<CreateEventRequest> validator,
    EventService service) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }

    var (success, eventDto, error) = await service.CreateEventAsync(request);
    return success
        ? Results.Created($"/api/events/{eventDto.Id}", eventDto)
        : Results.Problem(title: "Creation Failed", detail: error);
});
```

**Service Registration:**
```csharp
// Program.cs
builder.Services.AddValidatorsFromAssemblyContaining<CreateEventRequestValidator>();
```

**Current Usage:** FluentValidation widely adopted across features ✅

---

### 7.2 RFC 7807 Problem Details

**Standard:** [From research] Use Results.Problem() for all error responses

**Error Response Pattern:**
```csharp
return Results.Problem(
    title: "Resource Not Found",           // Short error title
    detail: "User with ID 123 not found",  // Detailed explanation
    statusCode: 404,                       // HTTP status
    type: "https://witchcityrope.com/errors/not-found",  // Error type URI
    extensions: new Dictionary<string, object?>          // Additional context
    {
        { "userId", userId },
        { "timestamp", DateTime.UtcNow }
    }
);
```

**Consistent Error Format Benefits:**
- Standard across all endpoints
- Machine-readable error details
- Better frontend error handling
- Professional API design

**Current Usage:** 66 occurrences - need to expand to ~130

**Priority:** ✅ HIGH - API consistency

---

### 7.3 Global Exception Handler

**Pattern:**
```csharp
// Program.cs
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/problem+json";

        var error = context.Features.Get<IExceptionHandlerFeature>();
        if (error != null)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(error.Error, "Unhandled exception");

            var problem = new ProblemDetails
            {
                Title = "An error occurred",
                Detail = app.Environment.IsDevelopment()
                    ? error.Error.Message
                    : "An internal server error occurred",
                Status = 500,
                Instance = context.Request.Path
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
    });
});
```

**✅ DO:**
- Log all unhandled exceptions
- Return RFC 7807 Problem Details
- Hide error details in production
- Include request path in response

**❌ DON'T:**
- Return raw exception messages in production
- Skip logging
- Return HTML error pages for API endpoints

---

## 8. Performance Optimization

### 8.1 Query Optimization Checklist

**✅ ALWAYS:**
- Use AsNoTracking() for read-only queries (75 current, need 95)
- Project to DTOs in LINQ queries (server-side projection)
- Use Include() explicitly (avoid lazy loading)
- Filter before ordering (.Where before .OrderBy)
- Paginate large result sets

**⚠️ SELECTIVE:**
- Compiled queries for hot paths only (after profiling)
- Caching for static/infrequent-change data only
- Index optimization based on query patterns

**❌ AVOID:**
- Loading full entities then filtering in memory
- Multiple round trips (use Include or Join)
- Lazy loading (performance penalty)
- Missing AsNoTracking() on reads

---

### 8.2 Pagination Pattern

**Standard Pagination:**
```csharp
public async Task<(bool Success, PagedResponse<EventDto>? Data, string Error)> GetEventsAsync(
    int pageNumber = 1,
    int pageSize = 20,
    CancellationToken ct = default)
{
    try
    {
        var totalCount = await _context.Events.CountAsync(ct);

        var events = await _context.Events
            .AsNoTracking()
            .OrderByDescending(e => e.StartDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                StartDate = e.StartDate
            })
            .ToListAsync(ct);

        var response = new PagedResponse<EventDto>
        {
            Data = events,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        return (true, response, string.Empty);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get events");
        return (false, null, "Database error occurred");
    }
}
```

**Response DTO:**
```csharp
public class PagedResponse<T>
{
    public List<T> Data { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
```

---

### 8.3 Caching Strategy (NOT CURRENTLY IMPLEMENTED)

**Status:** [From analysis] No caching layer detected
**Recommendation:** [From research] Add selectively for static data

**When to Cache:**
- Static reference data (roles, categories)
- Infrequently-changed data (settings, configurations)
- Expensive computed results
- Third-party API responses

**When NOT to Cache:**
- Real-time data (event registrations)
- User-specific data (profiles)
- Frequently-changing data (availability)

**Pattern:**
```csharp
// Example: Cache role list (changes infrequently)
public async Task<List<string>> GetRolesAsync()
{
    return await _cache.GetOrCreateAsync("roles", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
        return await _context.Roles
            .AsNoTracking()
            .Select(r => r.Name)
            .ToListAsync();
    });
}
```

**Future Consideration:** HybridCache in .NET 10 for multi-server deployments

**Priority:** 🟡 LOW - Current single-server deployment doesn't require caching

---

## 9. Testing Patterns

### 9.1 Service Layer Testing

**Pattern:**
```csharp
[Fact]
public async Task GetUserById_ValidId_ReturnsUser()
{
    // Arrange
    using var context = CreateTestDbContext();
    var service = new UserService(context, NullLogger<UserService>.Instance);
    var userId = Guid.NewGuid();
    context.Users.Add(new User { Id = userId, Email = "test@test.com" });
    await context.SaveChangesAsync();

    // Act
    var (success, user, error) = await service.GetUserByIdAsync(userId);

    // Assert
    success.Should().BeTrue();
    user.Should().NotBeNull();
    user.Email.Should().Be("test@test.com");
    error.Should().BeEmpty();
}
```

**Use:**
- xUnit for test framework
- FluentAssertions for assertions
- TestContainers for real PostgreSQL testing

**Current Implementation:** ✅ Integration tests with TestContainers operational

---

### 9.2 Integration Testing with WebApplicationFactory

**Pattern:**
```csharp
[Fact]
public async Task CreateEvent_WithValidData_ShouldSucceed()
{
    // Arrange
    await using var factory = new WebApplicationFactory<Program>();
    var client = factory.CreateClient();

    var request = new CreateEventRequest
    {
        Title = "Test Event",
        StartDate = DateTime.UtcNow.AddDays(7)
    };

    // Act
    var response = await client.PostAsJsonAsync("/api/events", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var result = await response.Content.ReadFromJsonAsync<EventDto>();
    result.Title.Should().Be("Test Event");
}
```

**Benefits:**
- Tests real HTTP pipeline
- Validates routing, binding, validation
- End-to-end feature testing
- Real database with TestContainers

**Current Usage:** Integration tests operational ✅

---

## 10. Security Best Practices

### 10.1 Authentication Patterns

**✅ IMPLEMENTED:**
- JWT Bearer authentication
- httpOnly cookies for web clients
- Service-to-service authentication

**Endpoint Protection:**
```csharp
app.MapGet("/api/admin/users", async (UserService service) =>
{
    var users = await service.GetAllUsersAsync();
    return Results.Ok(users);
})
.RequireAuthorization(policy => policy.RequireRole("Administrator"));
```

**Current Implementation:** JWT authentication operational ✅

---

### 10.2 CORS Configuration

**Development CORS:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

**Production CORS:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://witchcityrope.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

**Current Status:** ✅ CORS properly configured

---

### 10.3 Input Validation and Sanitization

**✅ ALWAYS:**
- Use FluentValidation for complex validation
- Validate all user inputs
- Sanitize HTML content
- Use parameterized queries (EF Core does this)
- Validate file uploads

**❌ NEVER:**
- Trust user input
- Concatenate strings for queries
- Skip validation on "internal" endpoints
- Return sensitive data in error messages

---

## 11. Common Anti-Patterns to Avoid

### ❌ ANTI-PATTERN 1: MediatR/CQRS for Simple CRUD

**Why Rejected:** [From research] 3x code overhead for minimal benefit at our scale
**Use Instead:** Direct service injection

```csharp
// ❌ DON'T: MediatR/CQRS complexity
public record CreateEventCommand(...) : IRequest<Result<EventDto>>;
public class CreateEventHandler : IRequestHandler<CreateEventCommand, Result<EventDto>> { }

// ✅ DO: Direct service
public class EventService
{
    public async Task<(bool Success, EventDto? Data, string Error)> CreateEventAsync(...)
}
```

**Impact:** 3x more files, harder debugging, minimal benefit for 200-500 concurrent users

---

### ❌ ANTI-PATTERN 2: Repository Pattern Over EF Core

**Why Rejected:** [Milan] Unnecessary abstraction, harder debugging
**Use Instead:** Direct ApplicationDbContext injection

```csharp
// ❌ DON'T: Repository pattern
public interface IEventRepository
{
    Task<Event> GetByIdAsync(Guid id);
}

// ✅ DO: Direct DbContext
public class EventService
{
    public EventService(ApplicationDbContext context) { }
}
```

**Impact:** Extra abstraction layer, no real benefit, harder to optimize queries

---

### ❌ ANTI-PATTERN 3: Manual TypeScript DTO Interfaces

**Why Rejected:** [From DTO audit] Type drift risk, maintenance burden
**Use Instead:** NSwag auto-generation with .Produces<T>()

```csharp
// ✅ DO: Add .Produces<T>() for auto-generation
app.MapGet("/api/users/{id}", GetUserAsync)
    .Produces<UserDto>(200);
```

```typescript
// ✅ DO: Import generated types
import type { components } from '@witchcityrope/shared-types';
type UserDto = components['schemas']['UserDto'];

// ❌ DON'T: Manual interfaces
interface UserDto {
  id: string;
  email: string;
  // Risk: Doesn't match backend!
}
```

**Impact:** Type drift, maintenance burden, broken frontend when API changes

---

### ❌ ANTI-PATTERN 4: Loading Full Entities for DTOs

**Why Rejected:** Memory waste, slower queries
**Use Instead:** Server-side projection in LINQ

```csharp
// ❌ DON'T: Load full entities
var events = await _context.Events.ToListAsync();
var dtos = events.Select(e => MapToDto(e)).ToList();

// ✅ DO: Server-side projection
var dtos = await _context.Events
    .AsNoTracking()
    .Select(e => new EventDto { Id = e.Id, Title = e.Title })
    .ToListAsync();
```

**Impact:** 40-60% slower queries, higher memory usage

---

### ❌ ANTI-PATTERN 5: Missing AsNoTracking() on Reads

**Why Rejected:** [Research] 20-40% performance penalty
**Use Instead:** Always AsNoTracking() for read-only queries

```csharp
// ❌ DON'T: Track read-only queries
var events = await _context.Events.Where(e => e.IsPublished).ToListAsync();

// ✅ DO: AsNoTracking for reads
var events = await _context.Events
    .AsNoTracking()
    .Where(e => e.IsPublished)
    .ToListAsync();
```

**Impact:** 20-40% performance penalty, unnecessary memory usage

---

## 12. Code Examples and Templates

### 12.1 Complete Feature Template

**Minimal Feature Implementation:**

```csharp
// 1. DTO Models (Features/ExampleFeature/Models/)

public class ExampleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateExampleRequest
{
    public string Name { get; set; }
}

public class UpdateExampleRequest
{
    public string Name { get; set; }
}

// 2. Validator (Features/ExampleFeature/Validators/)

public class CreateExampleRequestValidator : AbstractValidator<CreateExampleRequest>
{
    public CreateExampleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200);
    }
}

// 3. Service (Features/ExampleFeature/Services/)

public class ExampleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ExampleService> _logger;

    public ExampleService(ApplicationDbContext context, ILogger<ExampleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, List<ExampleDto>? Data, string Error)> GetAllAsync(
        CancellationToken ct = default)
    {
        try
        {
            var data = await _context.Examples
                .AsNoTracking()
                .Select(e => new ExampleDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync(ct);

            return (true, data, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get examples");
            return (false, null, "Database error occurred");
        }
    }

    public async Task<(bool Success, ExampleDto? Data, string Error)> GetByIdAsync(
        Guid id, CancellationToken ct = default)
    {
        try
        {
            var example = await _context.Examples
                .AsNoTracking()
                .Where(e => e.Id == id)
                .Select(e => new ExampleDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    CreatedAt = e.CreatedAt
                })
                .FirstOrDefaultAsync(ct);

            if (example == null)
            {
                return (false, null, "Example not found");
            }

            return (true, example, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get example {ExampleId}", id);
            return (false, null, "Database error occurred");
        }
    }

    public async Task<(bool Success, ExampleDto? Data, string Error)> CreateAsync(
        CreateExampleRequest request, CancellationToken ct = default)
    {
        try
        {
            var example = new Example
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                CreatedAt = DateTime.UtcNow
            };

            _context.Examples.Add(example);
            await _context.SaveChangesAsync(ct);

            var dto = new ExampleDto
            {
                Id = example.Id,
                Name = example.Name,
                CreatedAt = example.CreatedAt
            };

            return (true, dto, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create example");
            return (false, null, "Database error occurred");
        }
    }
}

// 4. Endpoints (Features/ExampleFeature/Endpoints/)

public static class ExampleEndpoints
{
    public static void MapExampleEndpoints(this IEndpointRouteBuilder app)
    {
        var examples = app.MapGroup("/api/examples")
            .WithTags("Examples")
            .WithOpenApi();

        examples.MapGet("/", GetAllExamplesAsync)
            .WithName("GetAllExamples")
            .Produces<List<ExampleDto>>(200)
            .Produces<ProblemDetails>(500);

        examples.MapGet("/{id}", GetExampleByIdAsync)
            .WithName("GetExampleById")
            .Produces<ExampleDto>(200)
            .Produces<ProblemDetails>(404);

        examples.MapPost("/", CreateExampleAsync)
            .WithName("CreateExample")
            .Produces<ExampleDto>(201)
            .Produces<ProblemDetails>(400);
    }

    private static async Task<IResult> GetAllExamplesAsync(
        ExampleService service,
        CancellationToken ct)
    {
        var (success, data, error) = await service.GetAllAsync(ct);

        return success
            ? Results.Ok(data)
            : Results.Problem(
                title: "Failed to get examples",
                detail: error,
                statusCode: 500);
    }

    private static async Task<IResult> GetExampleByIdAsync(
        Guid id,
        ExampleService service,
        CancellationToken ct)
    {
        var (success, data, error) = await service.GetByIdAsync(id, ct);

        return success
            ? Results.Ok(data)
            : Results.Problem(
                title: "Example not found",
                detail: error,
                statusCode: 404);
    }

    private static async Task<IResult> CreateExampleAsync(
        CreateExampleRequest request,
        IValidator<CreateExampleRequest> validator,
        ExampleService service,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var (success, data, error) = await service.CreateAsync(request, ct);

        return success
            ? Results.Created($"/api/examples/{data.Id}", data)
            : Results.Problem(
                title: "Failed to create example",
                detail: error,
                statusCode: 400);
    }
}

// 5. Service Registration (Program.cs or ServiceCollectionExtensions.cs)

builder.Services.AddScoped<ExampleService>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateExampleRequestValidator>();

// 6. Endpoint Registration (Program.cs)

app.MapExampleEndpoints();
```

---

### 12.2 Endpoint Templates

**GET Single Resource:**
```csharp
app.MapGet("/api/resources/{id}", async (
    Guid id,
    ResourceService service,
    CancellationToken ct) =>
{
    var (success, data, error) = await service.GetByIdAsync(id, ct);

    return success
        ? Results.Ok(data)
        : Results.Problem(
            title: "Resource Not Found",
            detail: error,
            statusCode: 404);
})
.WithName("GetResource")
.WithSummary("Get resource by ID")
.Produces<ResourceDto>(200)
.Produces<ProblemDetails>(404);
```

**GET Collection:**
```csharp
app.MapGet("/api/resources", async (
    ResourceService service,
    int pageNumber = 1,
    int pageSize = 20,
    CancellationToken ct = default) =>
{
    var (success, data, error) = await service.GetAllAsync(pageNumber, pageSize, ct);

    return success
        ? Results.Ok(data)
        : Results.Problem(
            title: "Failed to get resources",
            detail: error,
            statusCode: 500);
})
.WithName("GetAllResources")
.Produces<PagedResponse<ResourceDto>>(200)
.Produces<ProblemDetails>(500);
```

**POST Create:**
```csharp
app.MapPost("/api/resources", async (
    CreateResourceRequest request,
    IValidator<CreateResourceRequest> validator,
    ResourceService service,
    CancellationToken ct) =>
{
    var validationResult = await validator.ValidateAsync(request, ct);
    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }

    var (success, data, error) = await service.CreateAsync(request, ct);

    return success
        ? Results.Created($"/api/resources/{data.Id}", data)
        : Results.Problem(
            title: "Failed to create resource",
            detail: error,
            statusCode: 400);
})
.WithName("CreateResource")
.Produces<ResourceDto>(201)
.Produces<ProblemDetails>(400);
```

**PUT Update:**
```csharp
app.MapPut("/api/resources/{id}", async (
    Guid id,
    UpdateResourceRequest request,
    IValidator<UpdateResourceRequest> validator,
    ResourceService service,
    CancellationToken ct) =>
{
    var validationResult = await validator.ValidateAsync(request, ct);
    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }

    var (success, data, error) = await service.UpdateAsync(id, request, ct);

    return success
        ? Results.Ok(data)
        : Results.Problem(
            title: "Failed to update resource",
            detail: error,
            statusCode: error.Contains("not found") ? 404 : 400);
})
.WithName("UpdateResource")
.Produces<ResourceDto>(200)
.Produces<ProblemDetails>(400)
.Produces<ProblemDetails>(404);
```

**DELETE:**
```csharp
app.MapDelete("/api/resources/{id}", async (
    Guid id,
    ResourceService service,
    CancellationToken ct) =>
{
    var (success, _, error) = await service.DeleteAsync(id, ct);

    return success
        ? Results.NoContent()
        : Results.Problem(
            title: "Failed to delete resource",
            detail: error,
            statusCode: error.Contains("not found") ? 404 : 400);
})
.WithName("DeleteResource")
.Produces(204)
.Produces<ProblemDetails>(404)
.Produces<ProblemDetails>(400);
```

---

### 12.3 Service Templates

**Basic CRUD Service:**
```csharp
public class ResourceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ResourceService> _logger;

    public ResourceService(
        ApplicationDbContext context,
        ILogger<ResourceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, List<ResourceDto>? Data, string Error)> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var resources = await _context.Resources
                .AsNoTracking()
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ResourceDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync(ct);

            return (true, resources, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get resources");
            return (false, null, "Database error occurred");
        }
    }

    public async Task<(bool Success, ResourceDto? Data, string Error)> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        try
        {
            var resource = await _context.Resources
                .AsNoTracking()
                .Where(r => r.Id == id)
                .Select(r => new ResourceDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    CreatedAt = r.CreatedAt
                })
                .FirstOrDefaultAsync(ct);

            if (resource == null)
            {
                return (false, null, "Resource not found");
            }

            return (true, resource, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get resource {ResourceId}", id);
            return (false, null, "Database error occurred");
        }
    }

    public async Task<(bool Success, ResourceDto? Data, string Error)> CreateAsync(
        CreateResourceRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var resource = new Resource
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                CreatedAt = DateTime.UtcNow
            };

            _context.Resources.Add(resource);
            await _context.SaveChangesAsync(ct);

            var dto = new ResourceDto
            {
                Id = resource.Id,
                Name = resource.Name,
                CreatedAt = resource.CreatedAt
            };

            return (true, dto, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create resource");
            return (false, null, "Database error occurred");
        }
    }

    public async Task<(bool Success, ResourceDto? Data, string Error)> UpdateAsync(
        Guid id,
        UpdateResourceRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var resource = await _context.Resources.FindAsync(new object[] { id }, ct);
            if (resource == null)
            {
                return (false, null, "Resource not found");
            }

            resource.Name = request.Name;
            resource.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            var dto = new ResourceDto
            {
                Id = resource.Id,
                Name = resource.Name,
                CreatedAt = resource.CreatedAt
            };

            return (true, dto, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update resource {ResourceId}", id);
            return (false, null, "Database error occurred");
        }
    }

    public async Task<(bool Success, object? Data, string Error)> DeleteAsync(
        Guid id,
        CancellationToken ct = default)
    {
        try
        {
            var resource = await _context.Resources.FindAsync(new object[] { id }, ct);
            if (resource == null)
            {
                return (false, null, "Resource not found");
            }

            _context.Resources.Remove(resource);
            await _context.SaveChangesAsync(ct);

            return (true, null, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete resource {ResourceId}", id);
            return (false, null, "Database error occurred");
        }
    }
}
```

---

## Compliance Scorecard

### Current Implementation Status

| Category | Current Score | Target Score | Priority |
|----------|--------------|--------------|----------|
| **Architecture** | 82/100 | 95/100 | ✅ HIGH |
| Vertical Slice Pattern | 82% | 95% | Fix 6 features |
| Folder Naming Consistency | 70% | 100% | Standardize naming |
| | | | |
| **Code Quality** | 85/100 | 92/100 | ✅ HIGH |
| AsNoTracking() Coverage | 75 usages | ~95 usages | Add 15-20 more |
| .Produces<T>() Metadata | 160 usages | 100% | ✅ Excellent |
| Results.Problem() Usage | 66 usages | ~130 usages | Expand coverage |
| | | | |
| **DTO Alignment** | 62/100 | 95/100 | ✅ HIGH |
| NSwag Auto-Generation | ✅ Operational | ✅ | Maintain |
| Frontend Type Usage | 72% (33/46 files) | 100% | Fix 13 files |
| Backend Metadata | 160 .Produces<> | All endpoints | Add missing |
| | | | |
| **Performance** | 75/100 | 88/100 | 🟡 MEDIUM |
| AsNoTracking() | Partial | All reads | Audit needed |
| Server-Side Projection | ✅ Wide use | All queries | Maintain |
| Compiled Queries | None | Hot paths | Profile first |
| | | | |
| **Testing** | 85/100 | 90/100 | 🟡 MEDIUM |
| Integration Tests | ✅ TestContainers | ✅ | Maintain |
| Service Tests | ✅ Good | Expand | Add coverage |
| | | | |
| **Security** | 90/100 | 95/100 | 🟢 LOW |
| JWT Authentication | ✅ Implemented | ✅ | Maintain |
| CORS Configuration | ✅ Configured | ✅ | Maintain |
| Input Validation | ✅ FluentValidation | ✅ | Maintain |

**Overall Score:** 82/100 ✅ STRONG
**Projected After Fixes:** 92/100 ✅ EXCELLENT

---

## Prioritized Recommendations

### IMMEDIATE (Week 1-2) - 10-12 hours

**1. AsNoTracking() Audit** ✅ HIGH IMPACT
- Review all EF Core queries in Cms, Dashboard, Metadata
- Add `.AsNoTracking()` to read-only operations
- **Impact:** 20-40% query performance improvement
- **Effort:** 4-6 hours
- **Files:** ~15-20 queries to fix

**2. Standardize Error Responses** ✅ HIGH IMPACT
- Replace custom errors with `Results.Problem()`
- RFC 7807 compliance across all endpoints
- **Impact:** Consistent frontend error handling
- **Effort:** 3-4 hours
- **Files:** ~64 endpoints to update

**3. Folder Naming Standardization** ✅ HIGH IMPACT
- Rename Configuration folders (Data → Configuration, Configurations → Configuration)
- Move nested configurations to feature root
- Standardize Validation → Validators
- **Impact:** Code consistency, easier navigation
- **Effort:** 3-4 hours
- **Features:** 6 features to fix

### SHORT-TERM (Week 3-4) - 12-16 hours

**4. Frontend DTO Alignment** ✅ HIGH IMPACT
- Add missing .Produces<T>() to 6-8 backend endpoints
- Replace 13 manual TypeScript interfaces with generated types
- **Impact:** Type safety, eliminates drift
- **Effort:** 8-10 hours
- **Files:** 13 frontend files + 6-8 backend endpoints

**5. Expand OpenAPI Metadata** 🟡 MEDIUM IMPACT
- Add summaries and descriptions to all endpoints
- Document all parameters
- **Impact:** Better API documentation
- **Effort:** 4-6 hours
- **Files:** All endpoint files

### FUTURE (Month 2+) - 10-14 hours

**6. Profile for Compiled Queries** 🟡 MEDIUM IMPACT
- Identify top 10 most-used endpoints
- Measure current performance
- Implement compiled queries where proven benefit
- **Impact:** 5-15% improvement on hot paths
- **Effort:** 6-8 hours

**7. Cleanup Orphaned Code** 🟡 MEDIUM IMPACT
- Delete /Models/ folder (888 lines duplicate code)
- Move shared services to Features/Shared/Services/
- **Impact:** Code cleanliness, eliminate confusion
- **Effort:** 2-3 hours

**8. Rate Limiting** 🟢 LOW PRIORITY
- Implement .NET 10 rate limiting
- Protect against abuse
- **Impact:** Security improvement
- **Effort:** 2-3 hours

**Total Estimated Effort:** 32-42 hours over 6-8 weeks

---

## References

**Primary Sources:**
- Milan Jovanović patterns (October 2025) - Complexity filtered for pragmatism
- Microsoft .NET 10 official documentation - Native features and best practices
- WitchCityRope implementation analysis (82/100 compliance baseline)
- DTO alignment strategy (62/100 compliance, improvement roadmap)

**Supporting Documents:**
- `/docs/standards-processes/backend-api-audit-2025-10-23/research/milan-jovanovic-patterns-october-2025.md`
- `/docs/standards-processes/backend-api-audit-2025-10-23/research/dotnet-9-minimal-api-best-practices.md`
- `/docs/standards-processes/backend-api-audit-2025-10-23/research/research-summary-and-recommendations.md`
- `/docs/standards-processes/backend-api-audit-2025-10-23/analysis/current-implementation-analysis.md`
- `/docs/standards-processes/backend-api-audit-2025-10-23/analysis/dto-alignment-audit.md`
- `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`

---

## Key Success Principles

**1. Simplicity Over Complexity**
- 80% benefit with 20% complexity (tuple pattern vs Railway-Oriented Programming)
- Direct services vs MediatR/CQRS
- Pragmatic over theoretically pure

**2. API DTOs as Source of Truth**
- NSwag auto-generation MANDATORY
- NEVER manual TypeScript interfaces for API DTOs
- .Produces<T>() on ALL endpoints

**3. Performance Through Best Practices**
- AsNoTracking() for all read-only queries
- Server-side projection for DTOs
- Profile before optimizing (compiled queries)

**4. Consistency Across Codebase**
- Standardized folder naming
- RFC 7807 Problem Details for errors
- FluentValidation for complex validation

**5. Team Productivity Focus**
- Simpler patterns = faster development
- Clear examples and templates
- Automated type generation

---

*Document created: 2025-10-23*
*Next review: After Phase 6 implementation*
*Compliance: MANDATORY for all new code, RECOMMENDED for refactoring*
*Confidence: 92% - Based on extensive research and implementation analysis*
