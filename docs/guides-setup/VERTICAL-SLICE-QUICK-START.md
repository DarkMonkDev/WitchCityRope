# Vertical Slice Quick Start Guide - WitchCityRope API

<!-- Last Updated: 2025-08-22 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Overview

This guide provides **step-by-step instructions** for implementing new features using WitchCityRope's Simple Vertical Slice Architecture. Follow this guide to maintain consistency with our proven patterns that deliver **49ms response times** and **40-60% faster development**.

**Goal**: Get you building features quickly using our battle-tested patterns.

---

## Prerequisites

### Required Knowledge
- Basic C# and Entity Framework Core
- Understanding of minimal APIs
- Basic async/await patterns

### Development Environment
```bash
# Start the development environment
./dev.sh

# Or manually:
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
cd apps/web && npm run dev
```

### Test Database Access
- **Database**: PostgreSQL running on localhost:5433
- **Connection**: Automatically configured and seeded
- **Test accounts**: Available in documentation

---

## Step-by-Step: Create a New Feature

### Step 1: Copy the Health Feature Template

**ALWAYS start by copying the Health feature** - it's our proven template:

```bash
# Navigate to the Features directory
cd /home/chad/repos/witchcityrope-react/apps/api/Features

# Copy the Health feature structure
cp -r Health/ YourFeatureName/

# Rename the files inside
mv YourFeatureName/Services/HealthService.cs YourFeatureName/Services/YourFeatureNameService.cs
mv YourFeatureName/Endpoints/HealthEndpoints.cs YourFeatureName/Endpoints/YourFeatureNameEndpoints.cs
mv YourFeatureName/Models/HealthResponse.cs YourFeatureName/Models/YourFeatureNameResponse.cs
```

**Why copy Health?** It demonstrates all the patterns correctly and has been battle-tested in production.

### Step 2: Create Your Service

**Update `YourFeatureNameService.cs`:**

```csharp
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.YourFeatureName.Models;

namespace WitchCityRope.Api.Features.YourFeatureName.Services;

/// <summary>
/// [Your feature description] service using direct Entity Framework access
/// Example of the simplified vertical slice architecture pattern
/// </summary>
public class YourFeatureNameService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<YourFeatureNameService> _logger;

    public YourFeatureNameService(ApplicationDbContext context, ILogger<YourFeatureNameService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all items - Simple Entity Framework service - NO MediatR complexity
    /// </summary>
    public async Task<(bool Success, List<YourFeatureDto>? Response, string Error)> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Direct Entity Framework query with performance optimizations
            var items = await _context.YourEntitySet
                .AsNoTracking()  // CRITICAL: Always use for read-only queries
                .Where(x => x.IsActive)  // Your business logic
                .OrderBy(x => x.Name)    // Consistent ordering
                .Select(x => new YourFeatureDto  // Project to DTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} {FeatureName} items", items.Count, "YourFeatureName");
            
            return (true, items, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve {FeatureName} items", "YourFeatureName");
            return (false, null, "Failed to retrieve items");
        }
    }

    /// <summary>
    /// Get single item by ID
    /// </summary>
    public async Task<(bool Success, YourFeatureDto? Response, string Error)> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _context.YourEntitySet
                .AsNoTracking()
                .Where(x => x.Id == id && x.IsActive)
                .Select(x => new YourFeatureDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    CreatedAt = x.CreatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (item == null)
            {
                _logger.LogWarning("{FeatureName} not found: {Id}", "YourFeatureName", id);
                return (false, null, "Item not found");
            }

            _logger.LogDebug("{FeatureName} retrieved: {Id}", "YourFeatureName", id);
            return (true, item, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve {FeatureName}: {Id}", "YourFeatureName", id);
            return (false, null, "Failed to retrieve item");
        }
    }

    /// <summary>
    /// Create new item
    /// </summary>
    public async Task<(bool Success, int? Response, string Error)> CreateAsync(
        CreateYourFeatureDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple validation
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return (false, null, "Name is required");
            }

            // Business logic validation
            var existingItem = await _context.YourEntitySet
                .Where(x => x.Name == request.Name && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingItem != null)
            {
                return (false, null, "Item with this name already exists");
            }

            // Create new entity
            var newItem = new YourEntity
            {
                Name = request.Name,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.YourEntitySet.Add(newItem);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("{FeatureName} created: {Id} by user {UserId}", "YourFeatureName", newItem.Id, request.CreatedByUserId);
            
            return (true, newItem.Id, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create {FeatureName}: {Request}", "YourFeatureName", request.Name);
            return (false, null, "Failed to create item");
        }
    }

    /// <summary>
    /// Update existing item
    /// </summary>
    public async Task<(bool Success, string Error)> UpdateAsync(
        int id,
        UpdateYourFeatureDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Find existing item (tracked for updates)
            var existingItem = await _context.YourEntitySet
                .Where(x => x.Id == id && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingItem == null)
            {
                return (false, "Item not found");
            }

            // Update properties
            existingItem.Name = request.Name;
            existingItem.Description = request.Description;
            existingItem.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("{FeatureName} updated: {Id}", "YourFeatureName", id);
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update {FeatureName}: {Id}", "YourFeatureName", id);
            return (false, "Failed to update item");
        }
    }

    /// <summary>
    /// Soft delete item
    /// </summary>
    public async Task<(bool Success, string Error)> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existingItem = await _context.YourEntitySet
                .Where(x => x.Id == id && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingItem == null)
            {
                return (false, "Item not found");
            }

            // Soft delete
            existingItem.IsActive = false;
            existingItem.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("{FeatureName} deleted: {Id}", "YourFeatureName", id);
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete {FeatureName}: {Id}", "YourFeatureName", id);
            return (false, "Failed to delete item");
        }
    }
}
```

### Step 3: Create Your Models

**Create `YourFeatureNameResponse.cs` (and other DTOs):**

```csharp
namespace WitchCityRope.Api.Features.YourFeatureName.Models;

/// <summary>
/// Response DTO for YourFeatureName items
/// Used for API responses and NSwag TypeScript generation
/// </summary>
public class YourFeatureDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request DTO for creating new items
/// </summary>
public class CreateYourFeatureDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CreatedByUserId { get; set; }
}

/// <summary>
/// Request DTO for updating existing items
/// </summary>
public class UpdateYourFeatureDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// Response for paginated list results
/// </summary>
public class YourFeatureListResponse
{
    public List<YourFeatureDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage { get; set; }
}
```

### Step 4: Create Your Endpoints

**Update `YourFeatureNameEndpoints.cs`:**

```csharp
using WitchCityRope.Api.Features.YourFeatureName.Models;
using WitchCityRope.Api.Features.YourFeatureName.Services;

namespace WitchCityRope.Api.Features.YourFeatureName.Endpoints;

/// <summary>
/// YourFeatureName minimal API endpoints
/// Example of simple vertical slice endpoint registration - NO MediatR complexity
/// </summary>
public static class YourFeatureNameEndpoints
{
    /// <summary>
    /// Register YourFeatureName endpoints using minimal API pattern
    /// Shows simple direct service injection pattern
    /// </summary>
    public static void MapYourFeatureNameEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/your-feature-name")
            .WithTags("YourFeatureName")
            .WithOpenApi();

        // GET /api/your-feature-name
        group.MapGet("/", async (
            YourFeatureNameService service,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await service.GetAllAsync(cancellationToken);
                
                return success 
                    ? Results.Ok(response)
                    : Results.Problem(
                        title: "Failed to Get Items",
                        detail: error,
                        statusCode: 500);
            })
            .WithName("GetAllYourFeatureName")
            .WithSummary("Get all YourFeatureName items")
            .WithDescription("Retrieves all active YourFeatureName items ordered by name")
            .Produces<List<YourFeatureDto>>(200)
            .Produces(500);

        // GET /api/your-feature-name/{id}
        group.MapGet("/{id:int}", async (
            int id,
            YourFeatureNameService service,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await service.GetByIdAsync(id, cancellationToken);
                
                return success 
                    ? Results.Ok(response)
                    : Results.NotFound(new { message = error });
            })
            .WithName("GetYourFeatureNameById")
            .WithSummary("Get YourFeatureName item by ID")
            .WithDescription("Retrieves a specific YourFeatureName item by its unique identifier")
            .Produces<YourFeatureDto>(200)
            .Produces(404);

        // POST /api/your-feature-name
        group.MapPost("/", async (
            CreateYourFeatureDto request,
            YourFeatureNameService service,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await service.CreateAsync(request, cancellationToken);
                
                if (success)
                {
                    return Results.Created($"/api/your-feature-name/{response}", new { id = response });
                }
                
                return Results.BadRequest(new { message = error });
            })
            .WithName("CreateYourFeatureName")
            .WithSummary("Create new YourFeatureName item")
            .WithDescription("Creates a new YourFeatureName item with the provided information")
            .Accepts<CreateYourFeatureDto>("application/json")
            .Produces<object>(201)
            .Produces(400);

        // PUT /api/your-feature-name/{id}
        group.MapPut("/{id:int}", async (
            int id,
            UpdateYourFeatureDto request,
            YourFeatureNameService service,
            CancellationToken cancellationToken) =>
            {
                var (success, error) = await service.UpdateAsync(id, request, cancellationToken);
                
                return success 
                    ? Results.NoContent()
                    : Results.BadRequest(new { message = error });
            })
            .WithName("UpdateYourFeatureName")
            .WithSummary("Update existing YourFeatureName item")
            .WithDescription("Updates an existing YourFeatureName item with the provided information")
            .Accepts<UpdateYourFeatureDto>("application/json")
            .Produces(204)
            .Produces(400)
            .Produces(404);

        // DELETE /api/your-feature-name/{id}
        group.MapDelete("/{id:int}", async (
            int id,
            YourFeatureNameService service,
            CancellationToken cancellationToken) =>
            {
                var (success, error) = await service.DeleteAsync(id, cancellationToken);
                
                return success 
                    ? Results.NoContent()
                    : Results.BadRequest(new { message = error });
            })
            .WithName("DeleteYourFeatureName")
            .WithSummary("Delete YourFeatureName item")
            .WithDescription("Soft deletes a YourFeatureName item (marks as inactive)")
            .Produces(204)
            .Produces(400)
            .Produces(404);
    }
}
```

### Step 5: Register Your Feature

**Update `Program.cs` or `ServiceCollectionExtensions.cs`:**

```csharp
// In the service registration section
services.AddScoped<YourFeatureNameService>();

// In the endpoint registration section (after app.MapHealthEndpoints();)
app.MapYourFeatureNameEndpoints();
```

**Or better, add to `Features/Shared/Extensions/WebApplicationExtensions.cs`:**

```csharp
public static void MapAllFeatureEndpoints(this WebApplication app)
{
    app.MapHealthEndpoints();
    app.MapAuthenticationEndpoints();
    app.MapEventEndpoints();
    app.MapUserEndpoints();
    app.MapYourFeatureNameEndpoints();  // Add your feature here
}
```

---

## Real-World Examples from Our Codebase

### Example 1: Events Service (Simple CRUD)

**From `/apps/api/Features/Events/Services/EventService.cs`:**

```csharp
/// <summary>
/// Get all upcoming events with pagination
/// </summary>
public async Task<(bool Success, List<EventDto>? Response, string Error)> GetUpcomingEventsAsync(
    int page = 1,
    int pageSize = 20,
    CancellationToken cancellationToken = default)
{
    try
    {
        var events = await _context.Events
            .AsNoTracking()
            .Include(e => e.Instructor)
            .Where(e => e.StartTime > DateTime.UtcNow && !e.IsCancelled)
            .OrderBy(e => e.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                InstructorName = e.Instructor != null ? e.Instructor.SceneName : "TBD",
                Capacity = e.Capacity,
                RegisteredCount = e.Registrations.Count(r => !r.IsCancelled)
            })
            .ToListAsync(cancellationToken);

        return (true, events, string.Empty);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to retrieve upcoming events");
        return (false, null, "Failed to retrieve events");
    }
}
```

### Example 2: Authentication Service (Complex Business Logic)

**From `/apps/api/Features/Authentication/Services/AuthenticationService.cs`:**

```csharp
/// <summary>
/// Register new user with validation and role assignment
/// </summary>
public async Task<(bool Success, RegisterResponse? Response, string Error)> RegisterAsync(
    RegisterRequest request,
    CancellationToken cancellationToken = default)
{
    try
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return (false, null, "User with this email already exists");
        }

        // Create new user
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            SceneName = request.SceneName,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, null, errors);
        }

        // Assign default role
        await _userManager.AddToRoleAsync(user, "GeneralMember");

        var response = new RegisterResponse
        {
            UserId = user.Id,
            Email = user.Email,
            SceneName = user.SceneName,
            Message = "Registration successful. Please log in."
        };

        _logger.LogInformation("New user registered: {Email} ({SceneName})", user.Email, user.SceneName);
        return (true, response, string.Empty);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Registration failed for {Email}", request.Email);
        return (false, null, "Registration failed");
    }
}
```

### Example 3: Health Service (Monitoring)

**From `/apps/api/Features/Health/Services/HealthService.cs`:**

```csharp
/// <summary>
/// Get detailed health check with performance metrics
/// </summary>
public async Task<(bool Success, DetailedHealthResponse? Response, string Error)> GetDetailedHealthAsync(
    CancellationToken cancellationToken = default)
{
    try
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
        if (!canConnect)
        {
            return (false, null, "Database connection failed");
        }

        // Get multiple statistics efficiently
        var userCount = await _context.Users
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var activeUserCount = await _context.Users
            .AsNoTracking()
            .Where(u => u.LastLoginAt >= DateTime.UtcNow.AddDays(-30))
            .CountAsync(cancellationToken);

        var upcomingEventCount = await _context.Events
            .AsNoTracking()
            .Where(e => e.StartTime > DateTime.UtcNow && !e.IsCancelled)
            .CountAsync(cancellationToken);

        stopwatch.Stop();

        var response = new DetailedHealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            DatabaseConnected = canConnect,
            UserCount = userCount,
            ActiveUserCount = activeUserCount,
            UpcomingEventCount = upcomingEventCount,
            ResponseTimeMs = stopwatch.ElapsedMilliseconds,
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        };

        return (true, response, string.Empty);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Detailed health check failed");
        return (false, null, "Health check failed");
    }
}
```

---

## Testing Your Feature

### Step 1: Create Integration Tests

**Create `YourFeatureNameServiceTests.cs`:**

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using WitchCityRope.Api.Features.YourFeatureName.Models;
using WitchCityRope.Api.Features.YourFeatureName.Services;
using Xunit;

namespace WitchCityRope.Api.Tests.Services;

[Collection("DatabaseTest")]
public class YourFeatureNameServiceTests
{
    private readonly DatabaseTestFixture _fixture;

    public YourFeatureNameServiceTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllActiveItems()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var service = new YourFeatureNameService(context, NullLogger<YourFeatureNameService>.Instance);

        // Add test data
        context.YourEntitySet.AddRange(
            new YourEntity { Name = "Test Item 1", IsActive = true, CreatedAt = DateTime.UtcNow },
            new YourEntity { Name = "Test Item 2", IsActive = true, CreatedAt = DateTime.UtcNow },
            new YourEntity { Name = "Inactive Item", IsActive = false, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Response);
        Assert.Equal(2, result.Response.Count); // Only active items
        Assert.All(result.Response, item => Assert.True(item.Name.StartsWith("Test Item")));
    }

    [Fact]
    public async Task CreateAsync_ValidData_CreatesItem()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var service = new YourFeatureNameService(context, NullLogger<YourFeatureNameService>.Instance);

        var request = new CreateYourFeatureDto
        {
            Name = "New Test Item",
            Description = "Test description",
            CreatedByUserId = 1
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Response);
        Assert.True(result.Response > 0);

        // Verify in database
        var createdItem = await context.YourEntitySet.FindAsync(result.Response);
        Assert.NotNull(createdItem);
        Assert.Equal("New Test Item", createdItem.Name);
        Assert.True(createdItem.IsActive);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ReturnsFalse()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var service = new YourFeatureNameService(context, NullLogger<YourFeatureNameService>.Instance);

        // Add existing item
        context.YourEntitySet.Add(new YourEntity 
        { 
            Name = "Duplicate Name", 
            IsActive = true, 
            CreatedAt = DateTime.UtcNow 
        });
        await context.SaveChangesAsync();

        var request = new CreateYourFeatureDto
        {
            Name = "Duplicate Name",
            CreatedByUserId = 1
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already exists", result.Error);
    }
}
```

### Step 2: Test with Real Database

```bash
# Run your specific feature tests
dotnet test --filter "YourFeatureNameServiceTests"

# Run all service tests
dotnet test --filter "Category=Service"

# Run with verbose output
dotnet test --filter "YourFeatureNameServiceTests" --logger "console;verbosity=detailed"
```

### Step 3: Test API Endpoints

**Use the Swagger UI:**
1. Start the application: `./dev.sh`
2. Navigate to: `http://localhost:5653/swagger`
3. Find your feature endpoints under the "YourFeatureName" section
4. Test each endpoint with sample data

**Or use curl:**
```bash
# Test GET all
curl -X GET "http://localhost:5653/api/your-feature-name" \
     -H "accept: application/json"

# Test POST create
curl -X POST "http://localhost:5653/api/your-feature-name" \
     -H "accept: application/json" \
     -H "Content-Type: application/json" \
     -d '{
       "name": "Test Item",
       "description": "Test Description",
       "createdByUserId": 1
     }'

# Test GET by ID
curl -X GET "http://localhost:5653/api/your-feature-name/1" \
     -H "accept: application/json"
```

---

## Common Patterns and Anti-Patterns

### ✅ Do This (Good Patterns)

```csharp
// ✅ Use AsNoTracking for read-only queries
var items = await _context.Items.AsNoTracking().ToListAsync();

// ✅ Project to DTOs in queries
.Select(x => new ItemDto { Id = x.Id, Name = x.Name })

// ✅ Use explicit error messages
return (false, null, "Item with this name already exists");

// ✅ Log important operations
_logger.LogInformation("Item {Id} created by user {UserId}", itemId, userId);

// ✅ Use proper HTTP status codes
return success ? Results.Ok(response) : Results.BadRequest(error);

// ✅ Validate input early
if (string.IsNullOrWhiteSpace(request.Name))
    return (false, null, "Name is required");

// ✅ Use soft deletes
item.IsActive = false;
item.DeletedAt = DateTime.UtcNow;

// ✅ Include related data explicitly when needed
.Include(e => e.Instructor)
.ThenInclude(i => i.Profile)
```

### ❌ Don't Do This (Anti-Patterns)

```csharp
// ❌ Never use MediatR
using MediatR;
private readonly IMediator _mediator;

// ❌ Don't return database entities directly
return await _context.Items.ToListAsync(); // Returns Entity, not DTO

// ❌ Don't use lazy loading (causes N+1 problems)
// Just including without AsNoTracking enables lazy loading

// ❌ Don't use generic error messages
return (false, null, "Error"); // Too vague

// ❌ Don't catch and ignore exceptions
catch (Exception ex) { return null; } // Swallows important errors

// ❌ Don't use magic numbers
.Take(50); // Use const int DefaultPageSize = 50

// ❌ Don't hard delete (unless required)
_context.Items.Remove(item); // Permanently deletes data

// ❌ Don't forget cancellation tokens
public async Task<string> GetAsync() // Missing CancellationToken parameter

// ❌ Don't use complex LINQ in controllers/endpoints
// Put complex queries in services, keep endpoints simple
```

### Performance Best Practices

```csharp
// ✅ Efficient pagination
var items = await _context.Items
    .AsNoTracking()
    .Where(x => x.IsActive)
    .OrderBy(x => x.Name)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// ✅ Efficient counting for pagination
var totalCount = await _context.Items
    .AsNoTracking()
    .Where(x => x.IsActive)
    .CountAsync();

// ✅ Efficient search with indexes
var results = await _context.Items
    .AsNoTracking()
    .Where(x => x.Name.Contains(searchTerm) && x.IsActive) // Ensure Name has index
    .Take(100) // Limit results
    .ToListAsync();

// ✅ Batch operations when possible
var itemsToUpdate = await _context.Items
    .Where(x => x.Status == "Pending")
    .ToListAsync();

foreach (var item in itemsToUpdate)
{
    item.Status = "Processed";
    item.UpdatedAt = DateTime.UtcNow;
}

// Save once for all changes
await _context.SaveChangesAsync();
```

---

## Debugging Tips

### Common Issues and Solutions

**1. Service not found in DI container**
```
System.InvalidOperationException: Unable to resolve service for type 'YourFeatureNameService'
```
**Solution**: Add service registration in Program.cs:
```csharp
services.AddScoped<YourFeatureNameService>();
```

**2. Endpoints not found**
```
404 Not Found when calling /api/your-feature-name
```
**Solution**: Add endpoint mapping in Program.cs:
```csharp
app.MapYourFeatureNameEndpoints();
```

**3. Database queries failing**
```
Microsoft.EntityFrameworkCore.DbUpdateException
```
**Solution**: Check entity relationships and constraints:
```csharp
// Add logging to see generated SQL
_logger.LogInformation("Executing query: {Query}", _context.ChangeTracker.DebugView.ShortView);
```

**4. Performance issues**
```
Response times over 200ms
```
**Solutions**:
```csharp
// Add AsNoTracking() for reads
.AsNoTracking()

// Use explicit projections
.Select(x => new Dto { /* only needed properties */ })

// Add database indexes for common queries
// Check query execution plans
```

### Debugging Tools

```csharp
// 1. Enable sensitive data logging (development only)
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
    {
        optionsBuilder.EnableSensitiveDataLogging();
    }
}

// 2. Add query logging
_logger.LogDebug("Executing query for {Feature} with parameters: {Parameters}", 
    "YourFeature", new { id, searchTerm });

// 3. Measure performance
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
var result = await service.GetDataAsync();
stopwatch.Stop();
_logger.LogInformation("Query completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

// 4. Check SQL generated by EF
// Install Microsoft.Extensions.Logging.Console
// EF will log SQL to console in Development
```

---

## FAQ

### Q: Why don't we use MediatR?
**A**: For WitchCityRope's scale (600 members), MediatR adds complexity without benefits. Our direct service approach is:
- **Faster to develop** (40-60% improvement)
- **Easier to debug** (direct call stacks)
- **More performant** (49ms average response time)
- **Simpler to test** (no handler setup needed)

### Q: Should I create repositories?
**A**: No. Entity Framework Core IS our data access layer. Adding repositories creates unnecessary abstraction that:
- Complicates testing (you'd mock repositories instead of using real databases)
- Reduces performance (extra layer of indirection)
- Duplicates EF Core functionality
- Makes code harder to understand

### Q: How do I handle complex business logic?
**A**: Keep it in services, but organize it well:
```csharp
public async Task<(bool Success, string Error)> ProcessComplexWorkflowAsync(WorkflowRequest request)
{
    // 1. Validation
    var validationResult = await ValidateWorkflowAsync(request);
    if (!validationResult.Success) return validationResult;
    
    // 2. Business logic steps
    var step1Result = await ProcessStep1Async(request);
    if (!step1Result.Success) return step1Result;
    
    var step2Result = await ProcessStep2Async(request, step1Result.Data);
    if (!step2Result.Success) return step2Result;
    
    // 3. Finalization
    await FinalizeWorkflowAsync(request);
    
    return (true, string.Empty);
}

// Keep helper methods private in the same service
private async Task<ValidationResult> ValidateWorkflowAsync(WorkflowRequest request) { }
private async Task<ProcessResult> ProcessStep1Async(WorkflowRequest request) { }
```

### Q: How do I handle authentication in services?
**A**: Inject `IHttpContextAccessor` to access user context:
```csharp
public class YourFeatureNameService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public async Task<(bool Success, string Error)> SecureOperationAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !user.Identity.IsAuthenticated)
        {
            return (false, "Authentication required");
        }
        
        var userId = user.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return (false, "Invalid user context");
        }
        
        // Continue with business logic
    }
}
```

### Q: How do I add validation?
**A**: Use simple validation in services, FluentValidation for complex rules:
```csharp
// Simple validation
if (string.IsNullOrWhiteSpace(request.Name))
    return (false, null, "Name is required");

if (request.Name.Length > 200)
    return (false, null, "Name cannot exceed 200 characters");

// Complex validation with FluentValidation
public class CreateItemValidator : AbstractValidator<CreateItemDto>
{
    public CreateItemValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters")
            .MustAsync(BeUniqueName).WithMessage("Name must be unique");
    }
    
    private async Task<bool> BeUniqueName(string name, CancellationToken cancellationToken)
    {
        // Custom async validation logic
    }
}
```

### Q: What about caching?
**A**: Add caching when you need it, not preemptively:
```csharp
public class YourFeatureNameService
{
    private readonly IMemoryCache _cache;
    
    public async Task<(bool Success, List<ItemDto>? Response, string Error)> GetCachedItemsAsync()
    {
        const string cacheKey = "all_items";
        
        if (_cache.TryGetValue(cacheKey, out List<ItemDto>? cachedItems))
        {
            return (true, cachedItems, string.Empty);
        }
        
        var (success, items, error) = await GetItemsFromDatabaseAsync();
        if (success && items != null)
        {
            _cache.Set(cacheKey, items, TimeSpan.FromMinutes(5));
        }
        
        return (success, items, error);
    }
}
```

---

## Success Checklist

Before considering your feature complete, verify:

### ✅ Code Quality
- [ ] Copied Health feature template structure exactly
- [ ] All methods follow tuple return pattern: `(bool Success, T? Response, string Error)`
- [ ] Used `AsNoTracking()` for all read-only queries
- [ ] Projected to DTOs in queries (not returning entities)
- [ ] Added proper error handling and logging
- [ ] Validated inputs appropriately
- [ ] Used consistent naming conventions

### ✅ Performance
- [ ] All endpoints respond under 200ms average
- [ ] Database queries use explicit includes (no lazy loading)
- [ ] Large result sets use pagination
- [ ] No N+1 query problems

### ✅ Testing
- [ ] Integration tests written using DatabaseTestFixture
- [ ] Tests cover happy path and error cases
- [ ] All tests pass: `dotnet test --filter "YourFeatureNameServiceTests"`
- [ ] Manual API testing via Swagger UI

### ✅ Documentation
- [ ] OpenAPI documentation complete (WithSummary, WithDescription)
- [ ] Service methods have XML documentation comments
- [ ] DTOs have descriptive property comments
- [ ] README updated if needed

### ✅ Integration
- [ ] Service registered in DI container
- [ ] Endpoints registered in Program.cs
- [ ] No compiler warnings
- [ ] Application starts without errors
- [ ] Swagger UI shows endpoints correctly

---

## Getting Help

### Reference Materials
- **Health Feature Template**: `/apps/api/Features/Health/` - Copy this pattern exactly
- **API Architecture Overview**: `/docs/architecture/API-ARCHITECTURE-OVERVIEW.md`
- **AI Agent Implementation Guide**: `/docs/standards-processes/backend/vertical-slice-implementation-guide.md`

### Working Examples
- **Simple CRUD**: Health feature
- **Authentication Logic**: Authentication feature  
- **Complex Relationships**: Events feature
- **User Management**: Users feature

### Testing Resources
- **Test Fixtures**: `/tests/unit/api/Fixtures/DatabaseTestFixture.cs`
- **Test Examples**: `/tests/unit/api/Services/`
- **TestContainers Setup**: Real PostgreSQL for authentic testing

### When You're Stuck
1. **Check the Health feature** - it demonstrates all patterns correctly
2. **Review error messages** - they usually point to the exact issue
3. **Enable SQL logging** - see what queries EF is generating
4. **Test with Swagger UI** - verify endpoints work as expected
5. **Run integration tests** - catch issues early with real database

---

**Remember**: Our Simple Vertical Slice Architecture has delivered **49ms response times** and **40-60% faster development**. Follow these patterns exactly, and you'll build features quickly and reliably!

---

<!-- Document History -->
<!-- 2025-08-22: Created comprehensive developer quick start guide for vertical slice architecture -->
<!-- Next Review: 2025-09-22 (After developer feedback and additional examples) -->