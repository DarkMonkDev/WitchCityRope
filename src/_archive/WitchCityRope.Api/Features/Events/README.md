# Events Feature

This feature has been restructured to follow the direct service pattern instead of the command/query pattern.

## Structure

```
Features/Events/
├── Models/
│   ├── CreateEventRequest.cs      # Request/Response DTOs for creating events
│   ├── ListEventsRequest.cs       # Request/Response DTOs for listing events
│   ├── RegisterForEventRequest.cs # Request/Response DTOs for event registration
│   └── Enums.cs                  # Feature-specific enums
├── Services/
│   └── EventService.cs           # Main service containing all business logic
├── EventsController.cs           # API controller
└── EventsServiceExtensions.cs    # DI registration
```

## Usage

### 1. Register in Startup/Program.cs

```csharp
builder.Services.AddEventsFeature();
```

### 2. Service Dependencies

The `EventService` requires the following dependencies:
- `WitchCityRopeDbContext` - Entity Framework database context
- `IUserContext` - For getting current user information
- `IPaymentService` - For processing event payments
- `INotificationService` - For sending email notifications
- `ISlugGenerator` - For generating URL-friendly slugs

### 3. Example Usage in Controller

```csharp
[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpPost]
    public async Task<ActionResult<CreateEventResponse>> CreateEvent([FromBody] CreateEventRequest request)
    {
        var response = await _eventService.CreateEventAsync(request);
        return Created($"/api/events/{response.EventId}", response);
    }
}
```

## Key Changes from Command/Query Pattern

1. **Single Service**: All event-related logic is consolidated into `EventService` instead of separate command/query handlers
2. **Direct Method Calls**: Methods like `CreateEventAsync`, `ListEventsAsync`, etc. instead of using a mediator
3. **Simplified Structure**: Fewer files and folders, easier to navigate
4. **Request/Response DTOs**: Moved to a dedicated Models folder
5. **Direct Dependency Injection**: Service is injected directly into controllers

## Benefits

- **Simpler**: Less boilerplate code and fewer abstractions
- **Easier to Debug**: Direct method calls make stack traces clearer
- **Better IntelliSense**: IDE can show all available methods on the service
- **Familiar Pattern**: Standard service pattern that most developers know