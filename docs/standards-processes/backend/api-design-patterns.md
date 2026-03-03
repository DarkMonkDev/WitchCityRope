# API Design Patterns

**Purpose**: REST API design conventions, endpoint patterns, and HTTP standards for WitchCityRope.
**When to Read**: When designing or implementing API endpoints.
**Related**: [Service Layer Patterns](./service-layer-patterns.md), [Error Handling](./error-handling-patterns.md)

## API Architecture

**Pattern**: Minimal API (.NET 10)
**Base URL**: `http://localhost:5655/api` (development)
**Authentication**: JWT + HttpOnly Cookies

## Endpoint Naming Conventions

### Resource-Based URLs
```csharp
// ✅ CORRECT: Resource-based, RESTful naming
GET    /api/events              // List events
GET    /api/events/{id}         // Get single event
POST   /api/events              // Create event
PUT    /api/events/{id}         // Update event
DELETE /api/events/{id}         // Delete event

// Related resources
GET    /api/events/{id}/sessions           // List event sessions
GET    /api/events/{id}/registrations      // List event registrations
POST   /api/events/{id}/register           // Register for event
```

### Avoid Verb-Based URLs
```csharp
// ❌ WRONG: Verb-based URLs
POST /api/createEvent
POST /api/registerForEvent
GET  /api/getEvent
```

## HTTP Method Usage

### Standard HTTP Methods
```csharp
// GET - Retrieve resources (idempotent, cacheable)
app.MapGet("/api/events/{id}", async (int id, EventService service) =>
{
    var result = await service.GetEventAsync(id);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.NotFound(result.Errors);
});

// POST - Create resources (not idempotent)
app.MapPost("/api/events", async (CreateEventRequest request, EventService service) =>
{
    var result = await service.CreateEventAsync(request);
    return result.IsSuccess
        ? Results.Created($"/api/events/{result.Value.Id}", result.Value)
        : Results.BadRequest(result.Errors);
});

// PUT - Update entire resource (idempotent)
app.MapPut("/api/events/{id}", async (int id, UpdateEventRequest request, EventService service) =>
{
    var result = await service.UpdateEventAsync(id, request);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(result.Errors);
});

// PATCH - Partial update (idempotent)
app.MapPatch("/api/events/{id}", async (int id, JsonPatchDocument<Event> patch, EventService service) =>
{
    var result = await service.PatchEventAsync(id, patch);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(result.Errors);
});

// DELETE - Remove resource (idempotent)
app.MapDelete("/api/events/{id}", async (int id, EventService service) =>
{
    var result = await service.DeleteEventAsync(id);
    return result.IsSuccess
        ? Results.NoContent()
        : Results.BadRequest(result.Errors);
});
```

## Response Status Codes

### Standard Status Codes
```csharp
// 200 OK - Successful GET, PUT, PATCH
return Results.Ok(data);

// 201 Created - Successful POST
return Results.Created($"/api/events/{id}", data);

// 204 No Content - Successful DELETE
return Results.NoContent();

// 400 Bad Request - Validation errors
return Results.BadRequest(errors);

// 401 Unauthorized - Not authenticated
return Results.Unauthorized();

// 403 Forbidden - Not authorized for this resource
return Results.Forbid();

// 404 Not Found - Resource doesn't exist
return Results.NotFound();

// 409 Conflict - Resource state conflict (e.g., duplicate)
return Results.Conflict(error);

// 422 Unprocessable Entity - Business logic validation failed
return Results.UnprocessableEntity(errors);

// 500 Internal Server Error - Unexpected server error
return Results.Problem("Internal server error");
```

## Request/Response Patterns

### Request DTOs
```csharp
// ✅ CORRECT: Specific request DTOs for operations
public record CreateEventRequest
{
    public required string Name { get; init; }
    public required DateTime StartDateTime { get; init; }
    public required DateTime EndDateTime { get; init; }
    public int MaxCapacity { get; init; }
    public decimal? Price { get; init; }
}

public record UpdateEventRequest
{
    public string? Name { get; init; }
    public DateTime? StartDateTime { get; init; }
    public DateTime? EndDateTime { get; init; }
    public int? MaxCapacity { get; init; }
    public decimal? Price { get; init; }
}
```

### Response DTOs
```csharp
// ✅ CORRECT: Response DTOs expose only necessary data
public record EventDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public DateTime StartDateTime { get; init; }
    public DateTime EndDateTime { get; init; }
    public int MaxCapacity { get; init; }
    public int RegistrationCount { get; init; }
    public decimal? Price { get; init; }
}

// ❌ WRONG: Don't expose entity directly
// return Results.Ok(eventEntity);  // Contains navigation properties, internal fields
```

## Filtering and Pagination

### Query String Pattern
```csharp
public record EventsQuery
{
    public string? Search { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Type { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string SortBy { get; init; } = "startDateTime";
    public string SortDirection { get; init; } = "asc";
}

app.MapGet("/api/events", async ([AsParameters] EventsQuery query, EventService service) =>
{
    var result = await service.GetEventsAsync(query);
    return Results.Ok(result);
});

// Example URL:
// /api/events?search=rope&startDate=2025-01-01&page=1&pageSize=20&sortBy=name&sortDirection=asc
```

### Paginated Response
```csharp
public record PagedResult<T>
{
    public required List<T> Items { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
```

## Error Response Format

### Standard Error Response
```csharp
public record ErrorResponse
{
    public required string Message { get; init; }
    public List<string>? Errors { get; init; }
    public string? TraceId { get; init; }
}

// Usage
if (!result.IsSuccess)
{
    return Results.BadRequest(new ErrorResponse
    {
        Message = "Validation failed",
        Errors = result.Errors.ToList(),
        TraceId = Activity.Current?.Id
    });
}
```

## Authentication Endpoints

### Standard Auth Patterns
```csharp
// POST /api/auth/login
app.MapPost("/api/auth/login", async (LoginRequest request, AuthService service, HttpContext context) =>
{
    var result = await service.LoginAsync(request.Email, request.Password);

    if (result.IsSuccess)
    {
        // Set HttpOnly cookie
        context.Response.Cookies.Append("auth_token", result.Value.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });

        return Results.Ok(result.Value.User);
    }

    return Results.Unauthorized();
});

// POST /api/auth/logout
app.MapPost("/api/auth/logout", (HttpContext context) =>
{
    context.Response.Cookies.Delete("auth_token");
    return Results.NoContent();
});

// GET /api/auth/check-auth
app.MapGet("/api/auth/check-auth", async (HttpContext context, AuthService service) =>
{
    var token = context.Request.Cookies["auth_token"];
    if (string.IsNullOrEmpty(token))
    {
        return Results.Unauthorized();
    }

    var result = await service.ValidateTokenAsync(token);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.Unauthorized();
});
```

## CORS Configuration

### Development CORS
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", policy =>
    {
        policy.WithOrigins("http://localhost:5173")  // React dev server
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();  // Required for cookies
    });
});

app.UseCors("Development");
```

## API Versioning

### URL-Based Versioning (Current)
```csharp
// Version 1
app.MapGet("/api/v1/events", handler);

// Version 2 (when needed)
app.MapGet("/api/v2/events", handler);
```

## Standards Maintenance

When designing APIs:
1. Follow REST principles (resource-based URLs)
2. Use appropriate HTTP methods and status codes
3. Create specific Request/Response DTOs
4. Implement pagination for list endpoints
5. Return consistent error responses
6. Protect endpoints with authentication middleware
7. Enable CORS for frontend access

---

*This document is maintained by the Backend Developer Agent.*
