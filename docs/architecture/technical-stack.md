# Technical Stack Documentation

## Overview

Witch City Rope uses a pragmatic, simplified technical stack optimized for a single developer building a community platform for ~600 members. The architecture prioritizes simplicity, maintainability, and cost-effectiveness over complex patterns that would be overkill for this scale.

## Core Technology Stack

### Backend
- **Framework**: ASP.NET Core 9.0 (latest LTS)
- **Language**: C# 12
- **Architecture**: Vertical Slice Architecture with Direct Services
- **Database**: PostgreSQL 16 (migrating from SQLite)
- **ORM**: Entity Framework Core 9.0

### Frontend
- **Framework**: Blazor Server
- **UI Components**: Syncfusion Blazor (commercial license)
- **CSS**: Custom design system with CSS variables
- **JavaScript**: Minimal (mobile menu, interactions only)

### Infrastructure
- **Containerization**: Docker with multi-container architecture
  - Web application container (ASP.NET Core)
  - PostgreSQL database container
  - Nginx reverse proxy container (production)
- **Orchestration**: Docker Compose
- **Web Server**: Kestrel with Nginx reverse proxy
- **Hosting**: VPS with Docker support
- **CI/CD**: GitHub Actions with Docker build pipeline

## NuGet Packages

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- Core Framework -->
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="9.0.*" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.*" />

    <!-- UI Components -->
    <PackageReference Include="Syncfusion.Blazor" Version="24.1.*" />

    <!-- Authentication -->
    <PackageReference Include="Microsoft.AspNetCore.Authentication.Google" Version="9.0.*" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.*" />
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.*" />

    <!-- Payments -->
    <PackageReference Include="PayPalCheckoutSdk" Version="1.0.*" />

    <!-- Validation Only -->
    <PackageReference Include="FluentValidation" Version="11.9.*" />
    
    <!-- Email -->
    <PackageReference Include="SendGrid" Version="9.28.*" />
    
    <!-- Caching (included in ASP.NET Core) -->
    <PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="9.0.*" />
    
    <!-- Utilities -->
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.*" />

    <!-- Testing -->
    <PackageReference Include="xUnit" Version="2.6.*" />
    <PackageReference Include="Moq" Version="4.20.*" />
    <PackageReference Include="FluentAssertions" Version="6.12.*" />
    <PackageReference Include="bunit" Version="1.26.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.*" />
  </ItemGroup>
</Project>
```

## Vertical Slice Folder Structure

```
WitchCityRope/
├── src/
│   └── WitchCityRope.Web/
│       ├── Features/                   # Vertical Slices
│       │   ├── Auth/
│       │   │   ├── Services/
│       │   │   │   └── AuthService.cs
│       │   │   ├── Models/
│       │   │   │   ├── LoginRequest.cs
│       │   │   │   └── LoginResponse.cs
│       │   │   ├── Validators/
│       │   │   │   └── LoginValidator.cs
│       │   │   └── Pages/
│       │   │       ├── Login.razor
│       │   │       └── Register.razor
│       │   ├── Events/
│       │   │   ├── Services/
│       │   │   │   └── EventService.cs
│       │   │   ├── Models/
│       │   │   │   ├── EventDto.cs
│       │   │   │   └── CreateEventRequest.cs
│       │   │   ├── Validators/
│       │   │   │   └── EventValidator.cs
│       │   │   └── Pages/
│       │   │       ├── EventList.razor
│       │   │       └── CreateEvent.razor
│       │   ├── Vetting/
│       │   ├── Payments/
│       │   ├── CheckIn/
│       │   └── Safety/
│       ├── Shared/
│       │   ├── Services/              # Cross-cutting services
│       │   │   ├── IEmailService.cs
│       │   │   ├── EmailService.cs
│       │   │   ├── IEncryptionService.cs
│       │   │   └── EncryptionService.cs
│       │   ├── Components/
│       │   └── Layout/
│       ├── Data/
│       │   ├── WitchCityRopeDbContext.cs
│       │   ├── Entities/
│       │   └── Migrations/
│       ├── wwwroot/
│       ├── Program.cs
│       └── appsettings.json
├── tests/
└── docker-compose.yml
```

## Direct Service Pattern (No MediatR)

### Service Interface
```csharp
public interface IEventService
{
    Task<Result<EventDto>> GetEventAsync(int id);
    Task<Result<List<EventDto>>> GetEventsAsync(EventFilter filter);
    Task<Result<EventDto>> CreateEventAsync(CreateEventRequest request);
    Task<Result> UpdateEventAsync(int id, UpdateEventRequest request);
    Task<Result> DeleteEventAsync(int id);
}
```

### Service Implementation
```csharp
public class EventService : IEventService
{
    private readonly WitchCityRopeDbContext _db;
    private readonly ILogger<EventService> _logger;
    private readonly IMemoryCache _cache;

    public EventService(
        WitchCityRopeDbContext db, 
        ILogger<EventService> logger,
        IMemoryCache cache)
    {
        _db = db;
        _logger = logger;
        _cache = cache;
    }

    public async Task<Result<EventDto>> CreateEventAsync(CreateEventRequest request)
    {
        try
        {
            var eventEntity = new Event
            {
                Name = request.Name,
                Description = request.Description,
                StartTime = request.StartTime,
                Capacity = request.Capacity,
                Price = request.Price
            };

            _db.Events.Add(eventEntity);
            await _db.SaveChangesAsync();

            // Invalidate cache
            _cache.Remove("events_list");

            return Result<EventDto>.Success(new EventDto(eventEntity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create event");
            return Result<EventDto>.Failure("Failed to create event");
        }
    }

    public async Task<Result<List<EventDto>>> GetEventsAsync(EventFilter filter)
    {
        var cacheKey = $"events_{filter.GetHashCode()}";
        
        if (_cache.TryGetValue<List<EventDto>>(cacheKey, out var cached))
        {
            return Result<List<EventDto>>.Success(cached);
        }

        var query = _db.Events.AsQueryable();
        
        if (filter.StartDate.HasValue)
            query = query.Where(e => e.StartTime >= filter.StartDate.Value);
            
        var events = await query
            .OrderBy(e => e.StartTime)
            .Select(e => new EventDto(e))
            .ToListAsync();

        _cache.Set(cacheKey, events, TimeSpan.FromMinutes(5));
        
        return Result<List<EventDto>>.Success(events);
    }
}
```

### Blazor Component Usage
```razor
@page "/events/create"
@inject IEventService EventService
@inject IValidator<CreateEventRequest> Validator
@inject NavigationManager Navigation

<h3>Create Event</h3>

<EditForm Model="request" OnValidSubmit="HandleSubmit">
    <FluentValidationValidator />
    <ValidationSummary />
    
    <div class="form-group">
        <label>Event Name</label>
        <InputText @bind-Value="request.Name" class="form-control" />
    </div>
    
    <button type="submit" class="btn btn-primary" disabled="@isSubmitting">
        Create Event
    </button>
</EditForm>

@code {
    private CreateEventRequest request = new();
    private bool isSubmitting;

    private async Task HandleSubmit()
    {
        isSubmitting = true;
        
        var result = await EventService.CreateEventAsync(request);
        
        if (result.IsSuccess)
        {
            Navigation.NavigateTo($"/events/{result.Value.Id}");
        }
        else
        {
            // Handle error - show toast, etc.
        }
        
        isSubmitting = false;
    }
}
```

## Email Service Implementation (SendGrid)

```csharp
public interface IEmailService
{
    Task<Result> SendEmailAsync(string to, string subject, string htmlContent);
    Task<Result> SendTemplateEmailAsync(string to, string templateId, object data);
}

public class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _client;
    private readonly SendGridSettings _settings;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(
        IOptions<SendGridSettings> settings,
        ILogger<SendGridEmailService> logger)
    {
        _settings = settings.Value;
        _client = new SendGridClient(_settings.ApiKey);
        _logger = logger;
    }

    public async Task<Result> SendEmailAsync(string to, string subject, string htmlContent)
    {
        try
        {
            var from = new EmailAddress(_settings.FromEmail, _settings.FromName);
            var toAddress = new EmailAddress(to);
            var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, null, htmlContent);
            
            var response = await _client.SendEmailAsync(msg);
            
            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                return Result.Success();
            }
            
            _logger.LogError("SendGrid error: {StatusCode}", response.StatusCode);
            return Result.Failure("Failed to send email");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email");
            return Result.Failure("Email service error");
        }
    }
}

public class SendGridSettings
{
    public string ApiKey { get; set; }
    public string FromEmail { get; set; }
    public string FromName { get; set; }
}
```

## Dependency Injection Setup

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add database
builder.Services.AddDbContext<WitchCityRopeDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add feature services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IVettingService, VettingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Add validators
builder.Services.AddScoped<IValidator<LoginRequest>, LoginValidator>();
builder.Services.AddScoped<IValidator<CreateEventRequest>, EventValidator>();

// Add shared services
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IJwtService, JwtService>();

// Add caching
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

// Add email (SendGrid)
builder.Services.Configure<SendGridSettings>(
    builder.Configuration.GetSection("SendGrid"));
builder.Services.AddScoped<IEmailService, SendGridEmailService>();

// Add Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add Syncfusion
builder.Services.AddSyncfusionBlazor();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
```

## Validation with FluentValidation

```csharp
public class CreateEventValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Event name is required")
            .MaximumLength(200).WithMessage("Event name cannot exceed 200 characters");
            
        RuleFor(x => x.StartTime)
            .GreaterThan(DateTime.Now).WithMessage("Event must be in the future");
            
        RuleFor(x => x.Capacity)
            .InclusiveBetween(1, 100).WithMessage("Capacity must be between 1 and 100");
            
        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters");
    }
}
```

## Testing Strategy

```csharp
[Fact]
public async Task CreateEvent_ValidRequest_ReturnsSuccess()
{
    // Arrange
    var options = new DbContextOptionsBuilder<WitchCityRopeDbContext>()
        .UseInMemoryDatabase(databaseName: "TestDb")
        .Options;
        
    using var context = new WitchCityRopeDbContext(options);
    var cache = new MemoryCache(new MemoryCacheOptions());
    var service = new EventService(context, NullLogger<EventService>.Instance, cache);
    
    var request = new CreateEventRequest
    {
        Name = "Rope Basics",
        StartTime = DateTime.Now.AddDays(7),
        Capacity = 20
    };
    
    // Act
    var result = await service.CreateEventAsync(request);
    
    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal("Rope Basics", result.Value.Name);
}
```

## Security Implementation

### Direct Authorization in Services
```csharp
public class EventService : IEventService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public async Task<Result> DeleteEventAsync(int eventId)
    {
        var user = _httpContextAccessor.HttpContext.User;
        
        if (!user.IsInRole("Admin"))
        {
            return Result.Failure("Unauthorized");
        }
        
        var eventEntity = await _db.Events.FindAsync(eventId);
        if (eventEntity == null)
        {
            return Result.Failure("Event not found");
        }
        
        _db.Events.Remove(eventEntity);
        await _db.SaveChangesAsync();
        
        return Result.Success();
    }
}
```

## Deployment Strategy

### Docker Configuration (Multi-stage Build)
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/WitchCityRope.Web/WitchCityRope.Web.csproj", "WitchCityRope.Web/"]
COPY ["src/WitchCityRope.Core/WitchCityRope.Core.csproj", "WitchCityRope.Core/"]
COPY ["src/WitchCityRope.Infrastructure/WitchCityRope.Infrastructure.csproj", "WitchCityRope.Infrastructure/"]
RUN dotnet restore "WitchCityRope.Web/WitchCityRope.Web.csproj"
COPY src/ .
WORKDIR "/src/WitchCityRope.Web"
RUN dotnet build "WitchCityRope.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "WitchCityRope.Web.csproj" -c Release -o /app/publish

# Development stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS development
WORKDIR /app
EXPOSE 8080 8443
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENTRYPOINT ["dotnet", "watch", "run", "--project", "/src/WitchCityRope.Web/WitchCityRope.Web.csproj"]

# Final production stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080 8443
COPY --from=publish /app/publish .
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "WitchCityRope.Web.dll"]
```

### docker-compose.yml (Development)
```yaml
version: '3.8'

services:
  web:
    build:
      context: .
      dockerfile: Dockerfile
      target: development
    container_name: witchcityrope-web
    ports:
      - "5000:8080"
      - "5001:8443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=witchcityrope;Username=postgres;Password=${POSTGRES_PASSWORD}
    depends_on:
      db:
        condition: service_healthy
    volumes:
      - ./src:/src:cached
      - app_logs:/app/logs
    networks:
      - witchcityrope-network

  db:
    image: postgres:16-alpine
    container_name: witchcityrope-db
    ports:
      - "5432:5432"
    environment:
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
      - POSTGRES_DB=witchcityrope
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - witchcityrope-network

volumes:
  postgres_data:
  app_logs:

networks:
  witchcityrope-network:
    driver: bridge
```

### GitHub Actions CI/CD
```yaml
name: Build and Deploy

on:
  push:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 9.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore
      
    - name: Test
      run: dotnet test --no-build --verbosity normal
      
    - name: Build Docker image
      run: docker build -t witchcityrope:latest -f Dockerfile --target final .
      
    - name: Login to Docker Registry
      uses: docker/login-action@v2
      with:
        registry: ${{ secrets.DOCKER_REGISTRY }}
        username: ${{ secrets.DOCKER_USERNAME }}
        password: ${{ secrets.DOCKER_PASSWORD }}
        
    - name: Push Docker image
      run: |
        docker tag witchcityrope:latest ${{ secrets.DOCKER_REGISTRY }}/witchcityrope:latest
        docker push ${{ secrets.DOCKER_REGISTRY }}/witchcityrope:latest
      
    - name: Deploy to VPS
      uses: appleboy/ssh-action@v0.1.5
      with:
        host: ${{ secrets.VPS_HOST }}
        username: ${{ secrets.VPS_USER }}
        key: ${{ secrets.VPS_KEY }}
        script: |
          cd /opt/witchcityrope
          docker-compose -f docker-compose.prod.yml pull
          docker-compose -f docker-compose.prod.yml up -d
          docker-compose -f docker-compose.prod.yml exec -T web dotnet ef database update
```

## Performance Optimization

### PostgreSQL Configuration
```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseNpgsql(connectionString, options =>
    {
        options.CommandTimeout(30);
        options.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });
    
    // Enable connection pooling
    optionsBuilder.EnableServiceProviderCaching();
    
    // Add logging in development
    #if DEBUG
    optionsBuilder.EnableSensitiveDataLogging();
    optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
    #endif
}

// Connection string configuration for Docker
// Development: Host=db;Port=5432;Database=witchcityrope;Username=postgres;Password=dev_password
// Production: Uses environment variables or Docker secrets
```

### Blazor Server Optimization
- Use `@key` directive for list rendering
- Implement `IDisposable` for component cleanup
- Use `StateHasChanged()` judiciously
- Cache frequently accessed data in services
- Enable response compression

```csharp
// In Program.cs
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});
```

## Why This Architecture Is Better for Our Project

### 1. **Simplicity**
- Direct method calls instead of command/query objects
- Fewer files and classes to maintain
- Easier to understand and debug
- No pipeline configuration needed

### 2. **Performance**
- No MediatR pipeline overhead
- No reflection for handler discovery
- Direct dependency injection
- Fewer object allocations

### 3. **Developer Experience**
- IntelliSense shows methods directly
- F12 navigates to implementation immediately
- Easier refactoring with IDE support
- Clear execution flow

### 4. **Future Scalability**
- Services are already abstracted behind interfaces
- Can add caching, logging, or messaging decorators later
- RabbitMQ can be added when async messaging is needed
- No major refactoring required to scale

## Modern Standards Compliance

This simplified architecture still follows modern .NET best practices:

- ✅ **Dependency Injection** - All services use constructor injection
- ✅ **Async/Await** - All I/O operations are async
- ✅ **Result Pattern** - Consistent error handling without exceptions
- ✅ **Validation** - FluentValidation for complex rules
- ✅ **Vertical Slices** - Features are self-contained
- ✅ **SOLID Principles** - Single responsibility, interface segregation
- ✅ **Testability** - All components are easily testable
- ✅ **Docker Ready** - Single container deployment
- ✅ **Security** - JWT authentication, role-based authorization
- ✅ **Logging** - Structured logging with Serilog
- ✅ **Email** - SendGrid integration for transactional emails
- ✅ **Caching** - In-memory caching for performance

## Conclusion

This technical stack provides everything needed for the Witch City Rope platform while avoiding unnecessary complexity. By using direct service injection instead of MediatR, leveraging Entity Framework directly, and keeping the architecture flat, we achieve:

- Faster development time
- Easier maintenance
- Better performance
- Lower cognitive overhead
- Flexibility to add complexity only when truly needed

The key principle: **Start simple, add complexity only when the pain of not having it exceeds the cost of adding it.**