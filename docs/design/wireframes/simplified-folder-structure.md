# Simplified Folder Structure (Without MediatR)

## Overview
This structure removes MediatR patterns (Commands, Queries, Handlers) and replaces them with direct service classes that can be injected into Blazor components. The vertical slice architecture is maintained but simplified for a small application.

## Updated Folder Structure

```
WitchCityRope/
├── src/
│   ├── WitchCityRope.Core/
│   │   ├── Entities/
│   │   │   ├── Event.cs
│   │   │   ├── IncidentReport.cs
│   │   │   ├── Payment.cs
│   │   │   ├── Registration.cs
│   │   │   ├── User.cs
│   │   │   └── VettingApplication.cs
│   │   ├── Enums/
│   │   │   ├── EventType.cs
│   │   │   ├── PaymentStatus.cs
│   │   │   ├── RegistrationStatus.cs
│   │   │   └── UserRole.cs
│   │   ├── Exceptions/
│   │   │   └── DomainException.cs
│   │   ├── Interfaces/
│   │   │   ├── IEmailService.cs
│   │   │   ├── IEncryptionService.cs
│   │   │   └── IPaymentService.cs
│   │   ├── ValueObjects/
│   │   │   ├── EmailAddress.cs
│   │   │   ├── Money.cs
│   │   │   └── SceneName.cs
│   │   └── WitchCityRope.Core.csproj
│   │
│   ├── WitchCityRope.Infrastructure/
│   │   ├── Data/
│   │   │   ├── Configurations/
│   │   │   │   ├── EventConfiguration.cs
│   │   │   │   ├── IncidentReportConfiguration.cs
│   │   │   │   ├── PaymentConfiguration.cs
│   │   │   │   ├── RegistrationConfiguration.cs
│   │   │   │   ├── UserConfiguration.cs
│   │   │   │   └── VettingApplicationConfiguration.cs
│   │   │   ├── Migrations/
│   │   │   └── WitchCityRopeDbContext.cs
│   │   ├── Email/
│   │   │   └── EmailService.cs
│   │   ├── PayPal/
│   │   │   └── PayPalService.cs
│   │   ├── Security/
│   │   │   ├── EncryptionService.cs
│   │   │   └── JwtTokenService.cs
│   │   ├── DependencyInjection.cs
│   │   └── WitchCityRope.Infrastructure.csproj
│   │
│   ├── WitchCityRope.Api/
│   │   ├── Features/
│   │   │   ├── Auth/
│   │   │   │   ├── Services/
│   │   │   │   │   ├── AuthService.cs
│   │   │   │   │   └── TwoFactorService.cs
│   │   │   │   ├── Validators/
│   │   │   │   │   ├── LoginValidator.cs
│   │   │   │   │   └── RegisterValidator.cs
│   │   │   │   ├── Models/
│   │   │   │   │   ├── LoginRequest.cs
│   │   │   │   │   ├── LoginResponse.cs
│   │   │   │   │   ├── RegisterRequest.cs
│   │   │   │   │   └── TwoFactorRequest.cs
│   │   │   │   └── AuthController.cs
│   │   │   │
│   │   │   ├── CheckIn/
│   │   │   │   ├── Services/
│   │   │   │   │   └── CheckInService.cs
│   │   │   │   ├── Models/
│   │   │   │   │   ├── CheckInRequest.cs
│   │   │   │   │   └── CheckInResponse.cs
│   │   │   │   └── CheckInController.cs
│   │   │   │
│   │   │   ├── Events/
│   │   │   │   ├── Services/
│   │   │   │   │   ├── EventService.cs
│   │   │   │   │   └── RegistrationService.cs
│   │   │   │   ├── Validators/
│   │   │   │   │   ├── CreateEventValidator.cs
│   │   │   │   │   └── EventRegistrationValidator.cs
│   │   │   │   ├── Models/
│   │   │   │   │   ├── CreateEventRequest.cs
│   │   │   │   │   ├── EventDetailResponse.cs
│   │   │   │   │   ├── EventListResponse.cs
│   │   │   │   │   └── RegisterForEventRequest.cs
│   │   │   │   └── EventsController.cs
│   │   │   │
│   │   │   ├── Payments/
│   │   │   │   ├── Services/
│   │   │   │   │   └── PaymentProcessingService.cs
│   │   │   │   ├── Models/
│   │   │   │   │   ├── PaymentRequest.cs
│   │   │   │   │   └── PaymentResponse.cs
│   │   │   │   └── PaymentsController.cs
│   │   │   │
│   │   │   ├── Safety/
│   │   │   │   ├── Services/
│   │   │   │   │   └── IncidentReportService.cs
│   │   │   │   ├── Validators/
│   │   │   │   │   └── IncidentReportValidator.cs
│   │   │   │   ├── Models/
│   │   │   │   │   ├── IncidentReportRequest.cs
│   │   │   │   │   └── IncidentReportResponse.cs
│   │   │   │   └── SafetyController.cs
│   │   │   │
│   │   │   └── Vetting/
│   │   │       ├── Services/
│   │   │       │   └── VettingService.cs
│   │   │       ├── Validators/
│   │   │       │   └── VettingApplicationValidator.cs
│   │   │       ├── Models/
│   │   │       │   ├── VettingApplicationRequest.cs
│   │   │       │   ├── VettingApplicationResponse.cs
│   │   │       │   └── ReviewApplicationRequest.cs
│   │   │       └── VettingController.cs
│   │   │
│   │   ├── Infrastructure/
│   │   │   └── ApiConfiguration.cs
│   │   ├── Program.cs
│   │   ├── Dockerfile
│   │   ├── appsettings.json
│   │   └── WitchCityRope.Api.csproj
│   │
│   └── WitchCityRope.Web/
│       ├── Features/
│       │   ├── Admin/
│       │   │   ├── Services/
│       │   │   │   ├── AdminEventService.cs
│       │   │   │   └── AdminVettingService.cs
│       │   │   └── Pages/
│       │   │       ├── EventManagement.razor
│       │   │       └── VettingQueue.razor
│       │   │
│       │   ├── Auth/
│       │   │   ├── Services/
│       │   │   │   └── AuthenticationService.cs
│       │   │   ├── Components/
│       │   │   │   └── TwoFactorModal.razor
│       │   │   └── Pages/
│       │   │       ├── Login.razor
│       │   │       └── Register.razor
│       │   │
│       │   ├── Events/
│       │   │   ├── Services/
│       │   │   │   └── EventClientService.cs
│       │   │   ├── Components/
│       │   │   │   └── EventCard.razor
│       │   │   └── Pages/
│       │   │       ├── EventDetail.razor
│       │   │       └── EventList.razor
│       │   │
│       │   └── Members/
│       │       ├── Services/
│       │       │   └── MemberService.cs
│       │       └── Pages/
│       │           ├── Dashboard.razor
│       │           └── MyTickets.razor
│       │
│       ├── Shared/
│       │   ├── Components/
│       │   │   ├── Navigation/
│       │   │   │   └── MainNav.razor
│       │   │   └── UI/
│       │   │       └── LoadingSpinner.razor
│       │   ├── Layouts/
│       │   │   └── MainLayout.razor
│       │   └── Services/
│       │       └── ApiClient.cs
│       │
│       ├── Pages/
│       │   └── Index.razor
│       ├── App.razor
│       ├── Program.cs
│       ├── _Imports.razor
│       ├── Dockerfile
│       ├── appsettings.json
│       └── WitchCityRope.Web.csproj
│
├── tests/
│   ├── WitchCityRope.Api.Tests/
│   │   └── Features/
│   │       ├── Auth/
│   │       │   └── AuthServiceTests.cs
│   │       ├── Events/
│   │       │   └── EventServiceTests.cs
│   │       └── Vetting/
│   │           └── VettingServiceTests.cs
│   │
│   ├── WitchCityRope.Web.Tests/
│   │   └── Features/
│   │       └── Events/
│   │           └── EventListTests.cs
│   │
│   └── WitchCityRope.IntegrationTests/
│       └── ApiIntegrationTests.cs
│
└── docker/
    ├── docker-compose.yml
    └── docker-compose.override.yml
```

## Key Changes Explained

### 1. Removed MediatR Patterns
- **Before**: `LoginCommand.cs`, `LoginCommandHandler.cs`, `ListEventsQuery.cs`, `ListEventsQueryHandler.cs`
- **After**: Direct service classes like `AuthService.cs`, `EventService.cs`

### 2. Simplified API Structure
Each feature in the API now contains:
- **Services/**: Business logic classes that handle operations directly
- **Models/**: Request and response DTOs
- **Validators/**: FluentValidation validators for input validation
- **Controller**: RESTful API endpoints that call services directly

### 3. Service-Based Architecture
Instead of:
```csharp
// Old MediatR approach
public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Login logic
    }
}
```

We now have:
```csharp
// New service approach
public class AuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        // Login logic
    }
}
```

### 4. Blazor Component Integration
Components can now inject services directly:
```csharp
@inject EventClientService EventService

// Instead of injecting IMediator and sending commands/queries
```

### 5. Maintained Vertical Slices
- Each feature (Auth, Events, Vetting, etc.) is still self-contained
- Related services, models, and validators are grouped together
- Easy to understand and navigate

### 6. Clear Separation of Concerns
- **Core**: Domain entities and interfaces (unchanged)
- **Infrastructure**: Data access and external services (unchanged)
- **Api**: Business logic services and API endpoints
- **Web**: UI services and Blazor components

## Benefits of This Structure

1. **Simplicity**: No need to understand MediatR patterns or command/query separation
2. **Direct Flow**: Easy to trace code execution from controller → service → repository
3. **Less Boilerplate**: No command/query/handler classes for every operation
4. **Testability**: Services are easy to unit test with mocked dependencies
5. **Maintainability**: Clear organization makes it easy to find and modify features
6. **Performance**: Slightly better performance without MediatR pipeline overhead

## Example Service Implementation

```csharp
// API Service
public class EventService
{
    private readonly WitchCityRopeDbContext _context;
    private readonly IEmailService _emailService;

    public EventService(WitchCityRopeDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<EventDetailResponse> GetEventAsync(int eventId)
    {
        var evt = await _context.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (evt == null)
            throw new NotFoundException($"Event {eventId} not found");

        return new EventDetailResponse
        {
            Id = evt.Id,
            Title = evt.Title,
            // ... mapping
        };
    }

    public async Task<int> CreateEventAsync(CreateEventRequest request)
    {
        var evt = new Event
        {
            Title = request.Title,
            // ... mapping
        };

        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        return evt.Id;
    }
}
```

## Migration Notes

When migrating from MediatR to this structure:

1. Convert command/query handlers to service methods
2. Move validation logic to separate validator classes
3. Update controllers to inject and call services directly
4. Update Blazor components to use service classes instead of IMediator
5. Update dependency injection registrations
6. Update unit tests to test services instead of handlers

This structure provides a good balance between organization and simplicity, perfect for a small to medium-sized application.