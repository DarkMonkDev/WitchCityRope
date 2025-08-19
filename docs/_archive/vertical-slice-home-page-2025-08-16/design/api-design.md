# API Design Specification: Vertical Slice Home Page Events

**Created**: 2025-08-16  
**Purpose**: .NET Minimal API design for events display proof-of-concept  
**Status**: Design Phase - Implementation Ready  
**Owner**: Backend Developer Agent

## Overview

This document defines the API design for the vertical slice proof-of-concept. The API provides a single endpoint for retrieving events data, implemented progressively from hardcoded responses to database integration.

## Architecture Context

- **API Service**: .NET 9 Minimal API at http://localhost:5653
- **Pattern**: React (localhost:5173) → HTTP → API → PostgreSQL
- **Scope**: Single GET endpoint for events retrieval
- **Lifetime**: Throwaway code for stack validation

## API Endpoint Design

### GET /api/events

**Purpose**: Retrieve events for home page display

| Attribute | Value |
|-----------|--------|
| **Method** | GET |
| **Path** | `/api/events` |
| **Auth** | None (POC) |
| **Cache** | None (POC) |
| **Rate Limit** | None (POC) |

#### Request Format

```http
GET /api/events HTTP/1.1
Host: localhost:5653
Accept: application/json
```

**Parameters**: None (simplified for POC)

#### Response Format

**Success Response (200 OK)**:
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "title": "Rope Basics Workshop",
    "description": "Learn the fundamentals of rope bondage in a safe, educational environment. Perfect for beginners.",
    "startDate": "2025-08-25T14:00:00Z",
    "location": "Salem Community Center"
  },
  {
    "id": "550e8400-e29b-41d4-a716-446655440001",
    "title": "Advanced Suspension Techniques",
    "description": "Advanced workshop covering suspension safety, rigging points, and dynamic movements.",
    "startDate": "2025-08-30T19:00:00Z",
    "location": "Studio Space Downtown"
  }
]
```

**Error Response (500 Internal Server Error)**:
```json
{
  "message": "Failed to retrieve events",
  "timestamp": "2025-08-16T10:30:00Z"
}
```

#### Status Codes

| Code | Description | Condition |
|------|-------------|-----------|
| 200 | Success | Events retrieved successfully |
| 500 | Server Error | Database connection failure, unhandled exception |

## Progressive Implementation Plan

### Step 1: Hardcoded Controller (No Database)

**Goal**: Prove React ↔ API communication

#### Controller Implementation
```csharp
// apps/api/Controllers/EventsController.cs
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly ILogger<EventsController> _logger;

    public EventsController(ILogger<EventsController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents()
    {
        try
        {
            _logger.LogInformation("GET /api/events called - returning hardcoded data");

            // Hardcoded test data
            var events = new[]
            {
                new EventDto
                {
                    Id = "550e8400-e29b-41d4-a716-446655440000",
                    Title = "Rope Basics Workshop",
                    Description = "Learn the fundamentals of rope bondage in a safe, educational environment. Perfect for beginners.",
                    StartDate = new DateTime(2025, 8, 25, 14, 0, 0, DateTimeKind.Utc),
                    Location = "Salem Community Center"
                },
                new EventDto
                {
                    Id = "550e8400-e29b-41d4-a716-446655440001", 
                    Title = "Advanced Suspension Techniques",
                    Description = "Advanced workshop covering suspension safety, rigging points, and dynamic movements.",
                    StartDate = new DateTime(2025, 8, 30, 19, 0, 0, DateTimeKind.Utc),
                    Location = "Studio Space Downtown"
                }
            };

            // Simulate async operation
            await Task.Delay(100);

            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events from hardcoded data");
            return StatusCode(500, new { message = "Failed to retrieve events", timestamp = DateTime.UtcNow });
        }
    }
}
```

#### Data Transfer Object
```csharp
// apps/api/Models/EventDto.cs
public class EventDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public string Location { get; set; } = string.Empty;
}
```

### Step 2: Database Integration

**Goal**: Prove API ↔ Database communication

#### Service Layer Implementation
```csharp
// apps/api/Services/IEventService.cs
public interface IEventService
{
    Task<IEnumerable<EventDto>> GetEventsAsync(CancellationToken cancellationToken = default);
}

// apps/api/Services/EventService.cs
using Microsoft.EntityFrameworkCore;

public class EventService : IEventService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EventService> _logger;

    public EventService(ApplicationDbContext context, ILogger<EventService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<EventDto>> GetEventsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Querying events from database");

            var events = await _context.Events
                .AsNoTracking()
                .Where(e => e.StartDate > DateTime.UtcNow)
                .OrderBy(e => e.StartDate)
                .Take(10) // Limit for POC
                .Select(e => new EventDto
                {
                    Id = e.Id.ToString(),
                    Title = e.Title,
                    Description = e.Description,
                    StartDate = e.StartDate,
                    Location = e.Location
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {EventCount} events from database", events.Count);
            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve events from database");
            throw;
        }
    }
}
```

#### Updated Controller
```csharp
// apps/api/Controllers/EventsController.cs (Step 2)
[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IEventService eventService, ILogger<EventsController> logger)
    {
        _eventService = eventService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("GET /api/events called - querying database");

            var events = await _eventService.GetEventsAsync(cancellationToken);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events");
            return StatusCode(500, new { message = "Failed to retrieve events", timestamp = DateTime.UtcNow });
        }
    }
}
```

## .NET Minimal API Structure

### Project Organization
```
apps/api/
├── Controllers/
│   └── EventsController.cs          # Single endpoint controller
├── Models/
│   ├── Event.cs                     # Database entity (Step 2)
│   └── EventDto.cs                  # API response model
├── Services/
│   ├── IEventService.cs             # Service interface (Step 2)
│   └── EventService.cs              # Database service (Step 2)
├── Data/
│   └── ApplicationDbContext.cs      # EF Core context (Step 2)
├── Program.cs                       # App configuration
└── appsettings.Development.json     # Configuration
```

### Program.cs Configuration

#### Step 1 (Hardcoded)
```csharp
// apps/api/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for React dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRouting();
app.MapControllers();

app.Run();
```

#### Step 2 (Database)
```csharp
// apps/api/Program.cs (Step 2)
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<IEventService, EventService>();

// CORS for React dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

### Configuration Files

#### appsettings.Development.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=witchcityrope_test;Username=postgres;Password=test123"
  },
  "AllowedOrigins": ["http://localhost:5173"]
}
```

## CORS Configuration

### Development Setup
```csharp
// Allow React dev server on localhost:5173
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // For future auth cookies
    });
});
```

### Security Notes
- **Development Only**: Permissive CORS for localhost
- **Production**: Will require specific origin restrictions
- **Future**: Add authentication headers to CORS policy

## Error Handling

### Exception Strategy
```csharp
// Simple try-catch with logging
try
{
    var events = await _eventService.GetEventsAsync(cancellationToken);
    return Ok(events);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error retrieving events");
    return StatusCode(500, new { 
        message = "Failed to retrieve events", 
        timestamp = DateTime.UtcNow 
    });
}
```

### Logging Configuration
```csharp
// Structured logging for debugging
_logger.LogInformation("GET /api/events called");
_logger.LogInformation("Retrieved {EventCount} events", events.Count);
_logger.LogError(ex, "Failed to retrieve events from database");
```

### Error Response Format
```json
{
  "message": "Failed to retrieve events",
  "timestamp": "2025-08-16T10:30:00Z"
}
```

## Testing Approach

### Step 1 Testing (Hardcoded Data)

#### Manual Testing
```bash
# Test API endpoint directly
curl http://localhost:5653/api/events

# Expected response: JSON array with 2 events
```

#### Integration Test
```csharp
[TestMethod]
public async Task GetEvents_WithHardcodedData_ReturnsSuccess()
{
    // Arrange
    var factory = new WebApplicationFactory<Program>();
    var client = factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/events");

    // Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    var events = JsonSerializer.Deserialize<EventDto[]>(content);
    
    Assert.IsNotNull(events);
    Assert.AreEqual(2, events.Length);
    Assert.AreEqual("Rope Basics Workshop", events[0].Title);
}
```

### Step 2 Testing (Database Integration)

#### Database Health Check
```bash
# Test database connection
curl http://localhost:5653/health

# Expected response: Healthy status
```

#### Integration Test with Database
```csharp
[TestMethod]
public async Task GetEvents_WithDatabase_ReturnsSuccess()
{
    // Arrange
    var factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
            });
        });

    using var scope = factory.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Seed test data
    context.Events.Add(new Event
    {
        Title = "Test Event",
        Description = "Test Description", 
        StartDate = DateTime.UtcNow.AddDays(1),
        Location = "Test Location"
    });
    await context.SaveChangesAsync();

    var client = factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/events");

    // Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    var events = JsonSerializer.Deserialize<EventDto[]>(content);
    
    Assert.IsNotNull(events);
    Assert.IsTrue(events.Length > 0);
}
```

### E2E Testing Strategy
```typescript
// Playwright test to verify full stack
test('API returns events to React app', async ({ page }) => {
  // Navigate to home page
  await page.goto('/');
  
  // Wait for API call to complete
  await page.waitForSelector('[data-testid="event-card"]');
  
  // Verify events display
  const eventCards = await page.locator('[data-testid="event-card"]').count();
  expect(eventCards).toBeGreaterThan(0);
});
```

## Performance Considerations

### Database Optimization (Step 2)
```csharp
// Efficient query with projections
var events = await _context.Events
    .AsNoTracking()           // Read-only for better performance
    .Where(e => e.StartDate > DateTime.UtcNow)  // Filter in database
    .OrderBy(e => e.StartDate) // Sort in database
    .Take(10)                 // Limit results
    .Select(e => new EventDto  // Project to DTO in database
    {
        Id = e.Id.ToString(),
        Title = e.Title,
        Description = e.Description,
        StartDate = e.StartDate,
        Location = e.Location
    })
    .ToListAsync(cancellationToken);
```

### Caching Strategy (Future)
```csharp
// Future: Add simple in-memory caching
builder.Services.AddMemoryCache();

// In service:
// var cacheKey = "events";
// if (!_cache.TryGetValue(cacheKey, out var events))
// {
//     events = await QueryDatabase();
//     _cache.Set(cacheKey, events, TimeSpan.FromMinutes(5));
// }
```

## Docker Integration

### Dockerfile for API
```dockerfile
# apps/api/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["apps/api/WitchCityRope.Api.csproj", "apps/api/"]
RUN dotnet restore "apps/api/WitchCityRope.Api.csproj"
COPY . .
WORKDIR "/src/apps/api"
RUN dotnet build "WitchCityRope.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WitchCityRope.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WitchCityRope.Api.dll"]
```

### Docker Compose Integration
```yaml
# In docker-compose.dev.yml
api:
  build: ./apps/api
  ports:
    - "5653:80"
  environment:
    - ASPNETCORE_ENVIRONMENT=Development
    - ConnectionStrings__DefaultConnection=Host=postgres;Database=witchcityrope_test;Username=postgres;Password=test123
  depends_on:
    - postgres
```

## Implementation Checklist

### Step 1: Hardcoded Implementation
- [ ] Create EventsController with hardcoded data
- [ ] Create EventDto model 
- [ ] Configure CORS for localhost:5173
- [ ] Add basic error handling and logging
- [ ] Test API endpoint manually with curl
- [ ] Verify React app can fetch data
- [ ] Write basic integration test

### Step 2: Database Integration
- [ ] Create Event entity model
- [ ] Create ApplicationDbContext
- [ ] Create EventService with database queries
- [ ] Update Controller to use EventService
- [ ] Add health checks for database
- [ ] Run EF Core migrations
- [ ] Seed test data
- [ ] Test database queries
- [ ] Verify full React → API → Database flow

### Quality Gates
- [ ] No compilation errors
- [ ] API starts without errors
- [ ] Endpoint returns valid JSON
- [ ] CORS allows React dev server
- [ ] Basic error handling works
- [ ] Logs provide debugging information
- [ ] Integration tests pass

## Success Criteria

### Technical Validation
- [ ] React app successfully calls API endpoint
- [ ] API returns properly formatted JSON
- [ ] Events display correctly in React UI
- [ ] Error handling prevents crashes
- [ ] CORS configuration allows development

### Database Validation (Step 2)
- [ ] EF Core successfully connects to PostgreSQL
- [ ] API queries database without errors
- [ ] Data flows React → API → Database → API → React
- [ ] Database migrations work correctly
- [ ] Test data displays in React app

### Documentation Requirements
- [ ] Document any CORS issues encountered
- [ ] Record EF Core configuration lessons
- [ ] Note JSON serialization patterns
- [ ] Update backend lessons learned

---

**Implementation Notes**:
- Start with Step 1 to prove HTTP communication
- Move to Step 2 only after Step 1 is fully working
- Focus on simple, debuggable code over optimization
- Log extensively for troubleshooting
- This is throwaway code - prioritize learning over perfection

**Next Steps**: Begin implementation with Step 1 hardcoded controller after human review of this design.