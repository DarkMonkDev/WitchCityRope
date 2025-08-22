# Features Folder - Simple Vertical Slice Architecture

This folder contains the new simplified vertical slice architecture implementation for the WitchCityRope API. Each feature is organized as a self-contained slice with all related code in one place.

## Architecture Principles

**SIMPLICITY FIRST** - No MediatR, no CQRS, no complex patterns. Just simple, maintainable code.

### Key Patterns:
- **Direct Entity Framework Services**: Services call DbContext directly
- **Minimal API Endpoints**: Simple endpoint registration with direct service calls
- **Feature-Based Organization**: Group related functionality together
- **Simple Error Handling**: Basic Result pattern instead of complex pipelines

## Folder Structure

Each feature follows this consistent structure:

```
Features/[FeatureName]/
├── Services/
│   └── [FeatureName]Service.cs     # Direct Entity Framework service
├── Endpoints/
│   └── [FeatureName]Endpoints.cs   # Minimal API endpoint registration
├── Models/
│   ├── [Request]Request.cs         # Request DTOs for NSwag
│   └── [Response]Response.cs       # Response DTOs for NSwag
└── Validation/                     # Optional - if complex validation needed
    └── [Request]Validator.cs       # FluentValidation (if needed)
```

## Current Features

### ✅ Health (Complete Example)
- **Location**: `Features/Health/`
- **Purpose**: Simple health check endpoints showing the basic pattern
- **Endpoints**:
  - `GET /api/health` - Basic health status
  - `GET /api/health/detailed` - Detailed health metrics
  - `GET /health` - Legacy compatibility endpoint

### 🚧 Authentication (Template/Planning)
- **Location**: `Features/Authentication/`
- **Purpose**: Template showing how authentication will be migrated
- **Status**: Models created as examples, service implementation pending

## Implementation Guidelines

### 1. Service Pattern
```csharp
public class [Feature]Service
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<[Feature]Service> _logger;
    
    // Direct Entity Framework queries - NO repository pattern
    public async Task<(bool Success, T Response, string Error)> [Method]Async(...)
    {
        try
        {
            var result = await _context.[Entity]
                .AsNoTracking()  // For read-only queries
                .Where(...)
                .ToListAsync(cancellationToken);
            
            return (true, result.ToResponse(), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "...");
            return (false, null, "Error message");
        }
    }
}
```

### 2. Endpoint Pattern
```csharp
public static class [Feature]Endpoints
{
    public static void Map[Feature]Endpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/[feature]/[action]", async (
            [Feature]Service service,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await service.[Method]Async(cancellationToken);
                return success ? Results.Ok(response) : Results.BadRequest(error);
            })
            .WithName("[ActionName]")
            .WithSummary("...")
            .WithTags("[Feature]")
            .Produces<[Response]>(200);
    }
}
```

### 3. Registration Pattern

**Program.cs service registration**:
```csharp
// In ServiceCollectionExtensions.cs
services.AddScoped<[Feature]Service>();

// In Program.cs
builder.Services.AddFeatureServices();
```

**Program.cs endpoint registration**:
```csharp
// In WebApplicationExtensions.cs
app.Map[Feature]Endpoints();

// In Program.cs
app.MapFeatureEndpoints();
```

## Migration Strategy

### Week 1: Infrastructure (COMPLETE)
- [x] Create folder structure
- [x] Create Health feature as example
- [x] Update Program.cs integration
- [x] Create shared utilities (Result pattern)
- [x] Create extension methods for clean registration

### Week 2-6: Feature Migration (PLANNED)
1. **Authentication**: Migrate existing AuthController to Features/Authentication/
2. **Events**: Migrate existing EventsController to Features/Events/
3. **Users**: Create new user management features
4. **Validation**: Clean up and remove old controllers

## Benefits of This Architecture

1. **Simplicity**: Easy to understand and maintain
2. **Feature Cohesion**: Related code is grouped together
3. **Performance**: No MediatR overhead, direct database access
4. **Testing**: Simple service testing without complex handler mocking
5. **Scalability**: Add new features without affecting existing ones

## Anti-Patterns to Avoid

❌ **NEVER introduce these patterns**:
- MediatR handlers or command/query objects
- Complex repository patterns (use DbContext directly)
- Pipeline behaviors or middleware complexity
- CQRS separation (keep it simple)
- Excessive abstractions (favor simplicity)

✅ **ALWAYS follow these patterns**:
- Direct Entity Framework service calls
- Simple minimal API endpoints
- Feature-based organization
- Basic error handling with Result pattern
- Clear, straightforward code

## Next Steps

1. **Complete Authentication Migration**: Move AuthController logic to Features/Authentication/
2. **Events Feature**: Migrate EventsController 
3. **User Management**: Create new user profile features
4. **Testing**: Add comprehensive tests for each feature
5. **Documentation**: Update API documentation

This architecture prioritizes maintainability and simplicity over architectural complexity, making it perfect for our community platform's needs.