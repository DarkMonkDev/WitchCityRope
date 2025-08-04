# Quick Start Guide - Simplified Architecture

## Overview
This guide helps you quickly understand the simplified architecture without MediatR or complex patterns.

## Key Principle: Direct and Simple

Instead of:
```csharp
// ❌ Complex MediatR pattern
public class CreateEventCommand : IRequest<Result> { }
public class CreateEventHandler : IRequestHandler<CreateEventCommand, Result> { }
await _mediator.Send(new CreateEventCommand());
```

We use:
```csharp
// ✅ Direct service injection
public class EventService 
{
    public async Task<Result> CreateEventAsync(CreateEventDto dto) { }
}
await _eventService.CreateEventAsync(dto);
```

## Creating a New Feature

### 1. Create the Service
```csharp
// Features/Events/EventService.cs
public class EventService
{
    private readonly WcrDbContext _db;
    private readonly ILogger<EventService> _logger;
    
    public EventService(WcrDbContext db, ILogger<EventService> logger)
    {
        _db = db;
        _logger = logger;
    }
    
    public async Task<Result<EventDto>> GetEventAsync(int id)
    {
        var evt = await _db.Events
            .Where(e => e.Id == id)
            .Select(e => new EventDto 
            { 
                Id = e.Id,
                Title = e.Title,
                // ... mapping
            })
            .FirstOrDefaultAsync();
            
        return evt != null 
            ? Result<EventDto>.Success(evt)
            : Result<EventDto>.Failure("Event not found");
    }
    
    public async Task<Result<int>> CreateEventAsync(CreateEventDto dto)
    {
        // Validation
        var validator = new CreateEventValidator();
        var validationResult = await validator.ValidateAsync(dto);
        
        if (!validationResult.IsValid)
        {
            return Result<int>.Failure(validationResult.Errors.First().ErrorMessage);
        }
        
        // Business logic
        var evt = new Event
        {
            Title = dto.Title,
            StartTime = dto.StartTime,
            EventType = dto.EventType,
            Capacity = dto.Capacity
        };
        
        _db.Events.Add(evt);
        await _db.SaveChangesAsync();
        
        _logger.LogInformation("Event {EventId} created by {UserId}", evt.Id, dto.CreatedByUserId);
        
        return Result<int>.Success(evt.Id);
    }
}
```

### 2. Register the Service
```csharp
// Program.cs
builder.Services.AddScoped<EventService>();
```

### 3. Use in Blazor Components
```razor
@page "/events/create"
@inject EventService EventService
@inject NavigationManager Navigation

<h3>Create New Event</h3>

<EditForm Model="@model" OnValidSubmit="@HandleSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary />
    
    <div class="form-group">
        <label>Event Title</label>
        <InputText @bind-Value="model.Title" class="form-control" />
        <ValidationMessage For="@(() => model.Title)" />
    </div>
    
    <button type="submit" disabled="@isSubmitting" class="btn btn-primary">
        @if (isSubmitting)
        {
            <span>Creating...</span>
        }
        else
        {
            <span>Create Event</span>
        }
    </button>
</EditForm>

@code {
    private CreateEventDto model = new();
    private bool isSubmitting;
    
    private async Task HandleSubmit()
    {
        isSubmitting = true;
        
        var result = await EventService.CreateEventAsync(model);
        
        if (result.IsSuccess)
        {
            Navigation.NavigateTo($"/events/{result.Value}");
        }
        else
        {
            // Show error
        }
        
        isSubmitting = false;
    }
}
```

## Database Access Pattern

### Direct EF Core Usage
```csharp
public async Task<List<EventListItemDto>> GetUpcomingEventsAsync()
{
    return await _db.Events
        .Where(e => e.StartTime > DateTime.UtcNow)
        .Where(e => !e.IsCancelled)
        .OrderBy(e => e.StartTime)
        .Select(e => new EventListItemDto
        {
            Id = e.Id,
            Title = e.Title,
            StartTime = e.StartTime,
            EventType = e.EventType,
            SpotsAvailable = e.Capacity - e.Registrations.Count
        })
        .ToListAsync();
}
```

### Including Related Data
```csharp
public async Task<EventDetailsDto> GetEventDetailsAsync(int id)
{
    return await _db.Events
        .Include(e => e.Registrations)
            .ThenInclude(r => r.User)
        .Where(e => e.Id == id)
        .Select(e => new EventDetailsDto
        {
            Id = e.Id,
            Title = e.Title,
            Attendees = e.Registrations.Select(r => new AttendeeDto
            {
                UserId = r.UserId,
                SceneName = r.User.SceneName,
                CheckedIn = r.CheckedInAt.HasValue
            }).ToList()
        })
        .FirstOrDefaultAsync();
}
```

## Common Patterns

### Result Type for Error Handling
```csharp
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T Value { get; private set; }
    public string Error { get; private set; }
    
    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}
```

### Validation with FluentValidation
```csharp
public class CreateEventValidator : AbstractValidator<CreateEventDto>
{
    public CreateEventValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Event title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");
            
        RuleFor(x => x.StartTime)
            .GreaterThan(DateTime.Now.AddHours(24))
            .WithMessage("Events must be scheduled at least 24 hours in advance");
            
        RuleFor(x => x.Capacity)
            .InclusiveBetween(1, 100)
            .WithMessage("Capacity must be between 1 and 100");
    }
}
```

### Authorization in Services
```csharp
public class AdminEventService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public async Task<Result> CancelEventAsync(int eventId)
    {
        // Check authorization
        var user = _httpContextAccessor.HttpContext.User;
        if (!user.IsInRole("Admin") && !user.IsInRole("EventStaff"))
        {
            return Result.Failure("Unauthorized");
        }
        
        // Business logic
        var evt = await _db.Events.FindAsync(eventId);
        if (evt == null)
        {
            return Result.Failure("Event not found");
        }
        
        evt.IsCancelled = true;
        evt.CancelledAt = DateTime.UtcNow;
        evt.CancelledByUserId = int.Parse(user.FindFirst("UserId").Value);
        
        await _db.SaveChangesAsync();
        
        return Result.Success();
    }
}
```

## Testing Pattern

### Service Tests
```csharp
public class EventServiceTests
{
    private WcrDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<WcrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new WcrDbContext(options);
    }
    
    [Fact]
    public async Task CreateEvent_ValidData_CreatesEvent()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new EventService(context, NullLogger<EventService>.Instance);
        
        var dto = new CreateEventDto
        {
            Title = "Test Event",
            StartTime = DateTime.Now.AddDays(7),
            EventType = EventType.Class,
            Capacity = 60
        };
        
        // Act
        var result = await service.CreateEventAsync(dto);
        
        // Assert
        Assert.True(result.IsSuccess);
        
        var evt = await context.Events.FindAsync(result.Value);
        Assert.NotNull(evt);
        Assert.Equal("Test Event", evt.Title);
    }
}
```

## File Organization

```
src/Features/Events/
├── EventService.cs           # Main business logic
├── AdminEventService.cs      # Admin-specific operations
├── EventModels.cs           # DTOs and view models
├── EventValidators.cs       # FluentValidation rules
├── EventEntity.cs           # EF Core entity (if not in Data/)
└── EventExtensions.cs       # Helper methods

src/Pages/Events/
├── List.razor               # Event list page
├── Detail.razor            # Event detail page
├── Create.razor            # Create event form
└── _EventCard.razor        # Reusable component
```

## Benefits Recap

1. **Simplicity** - No command/handler boilerplate
2. **Discoverability** - IntelliSense shows all available methods
3. **Debugging** - Direct call stacks, easy to trace
4. **Testing** - Simple unit tests without mediator setup
5. **Performance** - No reflection or handler resolution overhead
6. **Flexibility** - Easy to refactor or add patterns later if needed

## When to Add Complexity

Only add more patterns when you experience actual pain:
- Multiple teams stepping on each other → Consider boundaries
- Complex workflows → Consider saga pattern
- Event sourcing needs → Consider event store
- Microservices → Consider message bus

For a 600-member community site, direct services will serve you well!