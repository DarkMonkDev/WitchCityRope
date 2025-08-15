# Lessons Learned - Backend Developers (C#)
<!-- Last Updated: 2025-08-12 -->
<!-- Next Review: 2025-09-12 -->

## Architecture Patterns

### Web + API Separation
**Issue**: Web project directly accessing database  
**Solution**: Web must call API for all business operations
```csharp
// ❌ WRONG - In Web project
var events = await _dbContext.Events.ToListAsync();

// ✅ CORRECT - In Web project
var events = await _apiClient.GetAsync<List<EventDto>>("/api/events");
```
**Applies to**: All data access in Web project

### Service Layer Pattern
**Issue**: Controllers containing business logic  
**Solution**: Keep controllers thin, logic in services
```csharp
// ❌ WRONG - Logic in controller
[HttpPost]
public async Task<IActionResult> CreateEvent(EventDto model)
{
    if (model.StartTime < DateTime.UtcNow)
        return BadRequest();
    // More logic...
}

// ✅ CORRECT - Logic in service
[HttpPost]
public async Task<IActionResult> CreateEvent(EventDto model)
{
    var result = await _eventService.CreateEventAsync(model);
    return result.Success ? Ok(result) : BadRequest(result);
}
```
**Applies to**: All API endpoints

## Entity Framework Core

### Entity Framework Core Patterns
**Reference**: See [Entity Framework Patterns](/docs/standards-processes/development-standards/entity-framework-patterns.md) for comprehensive EF Core guidelines including:
- DateTime UTC handling for PostgreSQL
- Entity ID initialization patterns
- Configuration organization
- Nullable owned entities handling  
- Navigation property management
- PostgreSQL specifics
- Query optimization
- Migration best practices

**Key Points from Experience**:
- Entity discovery through navigation properties causes migration failures
- Entity ID initialization prevents duplicate key violations
- Navigation properties to ignored entities must be removed completely

## Authentication & Authorization

### Authentication Patterns
**Reference**: See [Authentication Patterns](/docs/standards-processes/development-standards/authentication-patterns.md) for complete authentication implementation including:
- Blazor Server authentication architecture
- API endpoint patterns
- Service layer implementation
- Security best practices
- Common issues and solutions

**Key Point**: NEVER use SignInManager directly in Blazor components - always use API endpoints

## Dependency Injection

### Scoped vs Singleton
**Issue**: DbContext registered incorrectly  
**Solution**: Always use Scoped for DbContext
```csharp
// ❌ WRONG
services.AddSingleton<WitchCityRopeDbContext>();

// ✅ CORRECT
services.AddDbContext<WitchCityRopeDbContext>(options =>
    options.UseNpgsql(connectionString));
```
**Applies to**: Service registration

### Interface Segregation
**Issue**: Large interfaces with many responsibilities  
**Solution**: Split into focused interfaces
```csharp
// ❌ WRONG
public interface IUserService
{
    Task<User> GetUserAsync(Guid id);
    Task<bool> ValidatePasswordAsync(string email, string password);
    Task SendEmailAsync(string to, string subject);
    Task<List<Event>> GetUserEventsAsync(Guid userId);
}

// ✅ CORRECT
public interface IUserRepository { }
public interface IAuthService { }
public interface IEmailService { }
```
**Applies to**: Service design

## Common Pitfalls

### Async All The Way
**Issue**: Deadlocks from mixing sync/async  
**Solution**: Use async consistently
```csharp
// ❌ WRONG
public User GetUser(Guid id)
{
    return _repository.GetUserAsync(id).Result;
}

// ✅ CORRECT
public async Task<User> GetUserAsync(Guid id)
{
    return await _repository.GetUserAsync(id);
}
```

### Proper HTTP Status Codes
**Issue**: Always returning 200 OK  
**Solution**: Use appropriate status codes
```csharp
return result switch
{
    not null => Ok(result),           // 200
    null => NotFound(),               // 404
    _ when !ModelState.IsValid => BadRequest(ModelState), // 400
    _ => StatusCode(500)              // 500
};
```

## Docker Development

### Connection Strings
**Issue**: Hardcoded localhost connections  
**Solution**: Use container names in Docker
```json
// appsettings.Docker.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Port=5432;Database=witchcityrope_db;Username=postgres;Password=postgres"
  }
}
```

### Service Discovery
**Issue**: Services can't find each other  
**Solution**: Use container names
```csharp
// In Web project calling API
var apiUrl = Configuration["ApiUrl"]; // "http://api:8080" in Docker
```

## Integration Testing

### Data Protection Configuration
**Issue**: Integration tests failing with "Access to the path '/app' is denied" CryptographicException  
**Solution**: Configure ephemeral data protection for Testing environment
```csharp
// In Program.cs
if (builder.Environment.EnvironmentName == "Testing")
{
    // Use ephemeral data protection for tests to avoid file system permission issues
    builder.Services.AddDataProtection()
        .SetApplicationName("WitchCityRope")
        .UseEphemeralDataProtectionProvider();
}
else
{
    builder.Services.AddDataProtection()
        .SetApplicationName("WitchCityRope")
        .PersistKeysToFileSystem(new DirectoryInfo(
            builder.Environment.IsDevelopment() 
                ? Path.Combine(Directory.GetCurrentDirectory(), "temp", "keys")
                : "/app/shared/keys"));
}
```
**Root Cause**: Tests running in containers or restricted environments can't write to `/app` directory  
**Applies to**: All integration tests that use authentication or session state

## Authentication Issues

### JWT Token Creation in Web Service
**Issue**: JWT tokens for API authentication not created when users are already authenticated
**Solution**: Authentication events only trigger during login, not for existing sessions
```csharp
// ❌ WRONG - AuthenticationEventHandler.SigningIn only called during actual login
// For users already logged in, JWT tokens are never created

// ✅ CORRECT - Request JWT token on-demand in AuthenticationDelegatingHandler
if (httpContext?.User?.Identity?.IsAuthenticated == true)
{
    var token = await _jwtTokenService.GetTokenAsync();
    if (string.IsNullOrEmpty(token))
    {
        // Get JWT token on-demand for authenticated users
        var apiAuthService = httpContext.RequestServices.GetService<IApiAuthenticationService>();
        var userManager = httpContext.RequestServices.GetService<UserManager<WitchCityRopeUser>>();
        
        if (userManager != null && apiAuthService != null)
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user != null)
            {
                var newToken = await apiAuthService.GetJwtTokenForUserAsync(user);
                if (!string.IsNullOrEmpty(newToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                }
            }
        }
    }
}
```
**Root Cause**: Cookie authentication vs JWT authentication bridge not working for existing sessions
**Applies to**: Web → API authentication pattern

### API Endpoint Path Mismatch
**Issue**: ApiAuthenticationService calling wrong endpoint path
**Solution**: Ensure correct API route prefix
```csharp
// ❌ WRONG - Missing api prefix
var response = await _httpClient.PostAsync("/auth/web-service-login", content);

// ✅ CORRECT - Include full API route
var response = await _httpClient.PostAsync("/api/auth/web-service-login", content);
```
**Applies to**: Web service calling API endpoints

## Performance Tips

### Include vs Select
**Issue**: Loading entire entities when only need few fields  
**Solution**: Use Select for projections
```csharp
// ❌ WRONG - Loads entire entity
var names = await _context.Users
    .Include(u => u.Events)
    .Select(u => u.Name)
    .ToListAsync();

// ✅ CORRECT - Only loads what's needed
var names = await _context.Users
    .Select(u => u.Name)
    .ToListAsync();
```

### Pagination
**Issue**: Loading all records into memory  
**Solution**: Always paginate large datasets
```csharp
var pagedResults = await query
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

---

*Remember: The API is the single source of truth for business logic. The Web project is just a UI layer.*