# Lessons Learned - Backend Developers (C#)
<!-- Last Updated: 2025-08-04 -->
<!-- Next Review: 2025-09-04 -->

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

### Configuration Location
**Issue**: EF configurations in OnModelCreating getting huge  
**Solution**: Use separate configuration classes
```csharp
// ✅ CORRECT
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events", "public");
        builder.HasKey(e => e.Id);
        // More configuration
    }
}
```
**Applies to**: All entity configurations

### Owned Entities with Nullable
**Issue**: EF Core can't configure nullable owned entities  
**Solution**: Use separate nullable properties
```csharp
// ❌ WRONG
public Money? RefundAmount { get; set; } // Owned entity

// ✅ CORRECT
public decimal? RefundAmountValue { get; set; }
public string? RefundCurrency { get; set; }
```
**Applies to**: Nullable value objects

### PostgreSQL Specifics
**Issue**: Case sensitivity in queries  
**Solution**: Use proper collation or lowercase
```csharp
// ❌ WRONG
.Where(u => u.Email == email)

// ✅ CORRECT
.Where(u => u.Email.ToLower() == email.ToLower())
// OR configure collation in DB
```
**Applies to**: String comparisons

## Authentication & Authorization

### JWT in API Project
**Issue**: Confusion about where JWT is used  
**Solution**: Only API uses JWT, Web uses cookies
```csharp
// In API Program.cs
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { });

// In Web Program.cs
services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => { });
```
**Applies to**: Authentication setup

### API Authentication Flow
**Issue**: How to handle login in API  
**Solution**: Return JWT token, let client handle storage
```csharp
[HttpPost("login")]
public async Task<IActionResult> Login(LoginRequest request)
{
    var user = await _authService.ValidateUserAsync(request);
    if (user == null) return Unauthorized();
    
    var token = _jwtService.GenerateToken(user);
    return Ok(new { token, user });
}
```
**Applies to**: API authentication endpoints

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

### DateTime Always UTC
**Issue**: DateTime.Now causing PostgreSQL errors  
**Solution**: Always use UTC
```csharp
// ❌ WRONG
entity.CreatedAt = DateTime.Now;

// ✅ CORRECT
entity.CreatedAt = DateTime.UtcNow;
```

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