# Vertical Slice Architecture

**Purpose**: Feature-based code organization following vertical slice architecture principles.
**When to Read**: When implementing new features or understanding project structure.
**Related**: [Service Layer Patterns](./service-layer-patterns.md), [API Design Patterns](./api-design-patterns.md)

## Primary Reference

**Comprehensive Guide**: [Vertical Slice Implementation Guide](/docs/standards-processes/backend/vertical-slice-implementation-guide.md)

This document provides a quick reference. For detailed implementation patterns, see the comprehensive guide above.

## Core Principles

### What is Vertical Slice Architecture?

Instead of organizing by **technical layers** (Controllers, Services, Repositories):
```
❌ Traditional Layered Architecture:
/Controllers
  - EventController.cs
  - UserController.cs
/Services
  - EventService.cs
  - UserService.cs
/Repositories
  - EventRepository.cs
  - UserRepository.cs
```

Organize by **business features**:
```
✅ Vertical Slice Architecture:
/Features
  /Events
    - CreateEvent.cs      (Request, Handler, Validator)
    - GetEvent.cs         (Request, Handler, Response)
    - UpdateEvent.cs      (Request, Handler, Validator)
    - DeleteEvent.cs      (Request, Handler)
  /Users
    - RegisterUser.cs
    - UpdateProfile.cs
```

## Quick Pattern

### Feature Organization
```csharp
// Features/Events/CreateEvent.cs
namespace WitchCityRope.Features.Events;

// Request DTO
public record CreateEventRequest
{
    public required string Name { get; init; }
    public DateTime StartDateTime { get; init; }
    public int MaxCapacity { get; init; }
}

// Handler
public class CreateEventHandler
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CreateEventHandler> _logger;

    public CreateEventHandler(ApplicationDbContext db, ILogger<CreateEventHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<EventDto>> HandleAsync(CreateEventRequest request)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<EventDto>.Failure("Event name is required");
        }

        // Business logic
        var newEvent = new Event
        {
            Name = request.Name,
            StartDateTime = request.StartDateTime,
            MaxCapacity = request.MaxCapacity,
            CreatedAt = DateTime.UtcNow
        };

        _db.Events.Add(newEvent);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created event {EventId}: {EventName}", newEvent.Id, newEvent.Name);

        // Map to DTO
        var dto = new EventDto
        {
            Id = newEvent.Id,
            Name = newEvent.Name,
            StartDateTime = newEvent.StartDateTime,
            MaxCapacity = newEvent.MaxCapacity
        };

        return Result<EventDto>.Success(dto);
    }
}

// Endpoint registration
public static class CreateEventEndpoint
{
    public static void MapCreateEvent(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/events", async (CreateEventRequest request, CreateEventHandler handler) =>
        {
            var result = await handler.HandleAsync(request);
            return result.IsSuccess
                ? Results.Created($"/api/events/{result.Value.Id}", result.Value)
                : Results.BadRequest(result.Errors);
        })
        .RequireAuthorization("Admin");
    }
}
```

## Benefits

1. **Feature Cohesion**: All code for a feature in one place
2. **Easy Navigation**: Find everything related to "Create Event" in one file
3. **Reduced Coupling**: Features don't depend on each other
4. **Simplified Testing**: Test one feature slice in isolation
5. **Team Scalability**: Multiple devs work on different features without conflicts

## When to Use

- ✅ New features with distinct business operations
- ✅ CRUD operations (each operation is a slice)
- ✅ Complex workflows (multi-step processes)

## When NOT to Use

- ❌ Shared utilities (use /Common or /Shared)
- ❌ Cross-cutting concerns (authentication, logging)
- ❌ Domain entities (use /Domain or /Models)

## Implementation Guide

For detailed patterns including:
- MediatR integration
- FluentValidation
- Dependency injection setup
- Testing strategies
- Migration from layered architecture

See: [Vertical Slice Implementation Guide](/docs/standards-processes/backend/vertical-slice-implementation-guide.md)

---

*This document is maintained by the Backend Developer Agent.*
