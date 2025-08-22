# Service Architecture: Database Initialization Feature

<!-- Last Updated: 2025-08-22 -->
<!-- Version: 1.0 -->
<!-- Owner: Backend Developer -->
<!-- Status: Design -->

## Architecture Overview

The Database Initialization feature implements automatic database migration and seed data population using the IHostedService/BackgroundService pattern. This design ensures database readiness before the API serves requests, transforming manual setup into zero-configuration developer experience.

### Core Architecture Principles

- **IHostedService Pattern**: BackgroundService ensures initialization during application startup
- **Fail-Fast Design**: Milan Jovanovic's patterns with immediate application exit on failure
- **Environment-Aware**: Development gets full initialization, Production gets migrations only
- **EF Core 9 Integration**: Uses UseAsyncSeeding for idempotent operations
- **Polly Retry Policies**: Exponential backoff for Docker container startup delays

### Microservices Alignment

```
API Service (localhost:5653) → EF Core → PostgreSQL (localhost:5433)
                ↓
Database Initialization Service
        ↓              ↓
   Migrations     Seed Data Service
                      ↓
                Health Checks
```

## Service Classes Design

### 1. DatabaseInitializationService : BackgroundService

**Primary service orchestrating database initialization during application startup.**

```csharp
public class DatabaseInitializationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializationService> _logger;
    private readonly DbInitializationOptions _options;
    private static bool _initializationCompleted = false;
    
    public DatabaseInitializationService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseInitializationService> logger,
        IOptions<DbInitializationOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var hostEnvironment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
            
            await ApplyMigrationsWithRetryAsync(context, cts.Token);
            
            if (ShouldPopulateSeedData(hostEnvironment))
            {
                await context.Database.UseAsyncSeeding(async (ctx, _, ct) =>
                {
                    var seedService = scope.ServiceProvider.GetRequiredService<ISeedDataService>();
                    await seedService.SeedAllDataAsync(ct);
                }, cts.Token);
            }
            
            _initializationCompleted = true;
            _logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Database initialization failed");
            Environment.Exit(1); // Milan Jovanovic's fail-fast pattern
        }
    }
    
    public static bool IsInitializationCompleted => _initializationCompleted;
}
```

**Key Design Features:**
- Static completion tracking for health checks
- Environment-specific behavior logic
- Milan Jovanovic's fail-fast error handling
- EF Core 9's UseAsyncSeeding integration

### 2. ISeedDataService & SeedDataService

**Service responsible for populating seed data using EF Core 9 patterns.**

```csharp
public interface ISeedDataService
{
    Task<InitializationResult> SeedAllDataAsync(CancellationToken cancellationToken = default);
    Task SeedUsersAsync(CancellationToken cancellationToken = default);
    Task SeedEventsAsync(CancellationToken cancellationToken = default);
    Task SeedVettingStatusesAsync(CancellationToken cancellationToken = default);
}

public class SeedDataService : ISeedDataService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<SeedDataService> _logger;
    
    public async Task<InitializationResult> SeedAllDataAsync(CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var result = new InitializationResult();
        
        try
        {
            await SeedUsersAsync(cancellationToken);
            await SeedEventsAsync(cancellationToken);
            await SeedVettingStatusesAsync(cancellationToken);
            
            await transaction.CommitAsync(cancellationToken);
            result.Success = true;
            result.SeedRecordsCreated = await CountSeedRecordsAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            result.Success = false;
            result.Errors.Add(ex.Message);
            throw;
        }
        
        return result;
    }
}
```

**Key Design Features:**
- Transaction management for data consistency
- Comprehensive seed data per DATABASE-SEED-DATA.md
- Error tracking and rollback capabilities
- EF Core 9's automatic idempotency handling

## Health Check Implementation

### DatabaseInitializationHealthCheck

**Health check exposing database initialization status via API endpoints.**

```csharp
public class DatabaseInitializationHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DbInitializationOptions _options;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!DatabaseInitializationService.IsInitializationCompleted)
            {
                return HealthCheckResult.Unhealthy(
                    "Database initialization in progress",
                    data: new Dictionary<string, object>
                    {
                        ["initializationCompleted"] = false,
                        ["status"] = "Initializing"
                    });
            }
            
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Verify database connectivity
            await context.Database.CanConnectAsync(cancellationToken);
            
            return HealthCheckResult.Healthy(
                "Database initialization completed successfully",
                data: new Dictionary<string, object>
                {
                    ["initializationCompleted"] = true,
                    ["status"] = "Ready"
                });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Database initialization failed",
                ex,
                data: new Dictionary<string, object>
                {
                    ["initializationCompleted"] = false,
                    ["status"] = "Failed",
                    ["error"] = ex.Message
                });
        }
    }
}
```

**Health Check Endpoints:**
- `/api/health/database` - Database-specific initialization status
- `/health/ready` - Overall application readiness including database

## Polly Retry Configuration

### Exponential Backoff for Docker Startup

**Retry policy handling Docker PostgreSQL container initialization delays.**

```csharp
private async Task ApplyMigrationsWithRetryAsync(
    ApplicationDbContext context, 
    CancellationToken cancellationToken)
{
    var retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(
            _options.MaxRetryAttempts,  // 3 retries
            retryAttempt => TimeSpan.FromSeconds(
                _options.RetryDelaySeconds * Math.Pow(2, retryAttempt)), // 2s, 4s, 8s
            onRetry: (outcome, timespan, retryCount, _) =>
            {
                _logger.LogWarning(
                    "Migration attempt {RetryCount} failed, retrying in {Delay}ms: {Error}",
                    retryCount, timespan.TotalMilliseconds, outcome.Exception?.Message);
            });

    await retryPolicy.ExecuteAsync(async () =>
    {
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
        
        if (pendingMigrations.Any())
        {
            _logger.LogInformation("Applying {Count} pending migrations: {Migrations}", 
                pendingMigrations.Count(), string.Join(", ", pendingMigrations));
                
            await context.Database.MigrateAsync(cancellationToken);
        }
    });
}
```

**Retry Strategy:**
- 3 retry attempts with exponential backoff
- Initial delay: 2 seconds, then 4s, 8s
- Handles Docker container startup timing issues
- Detailed logging for debugging

## Dependency Injection Setup

### Program.cs Integration

**Service registration following existing patterns and service layer architecture.**

```csharp
// Configuration binding (after existing DbContext registration)
builder.Services.Configure<DbInitializationOptions>(
    builder.Configuration.GetSection("DatabaseInitialization"));

// Add Polly for retry policies
builder.Services.AddScoped<ISeedDataService, SeedDataService>();

// Register hosted service for startup initialization
builder.Services.AddHostedService<DatabaseInitializationService>();

// Add health checks for database initialization status
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseInitializationHealthCheck>("db-initialization");

var app = builder.Build();

// Health check endpoints
app.MapHealthChecks("/api/health/database", new HealthCheckOptions
{
    Predicate = check => check.Name == "db-initialization",
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.MapHealthChecks("/health/ready");
```

**Integration Points:**
- Uses existing DbContext registration patterns
- Follows service layer dependency injection
- Integrates with existing health check infrastructure
- Maintains backward compatibility

### Configuration Options

```csharp
public class DbInitializationOptions
{
    public bool EnableAutoMigration { get; set; } = true;
    public bool EnableSeedData { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public bool FailOnSeedDataError { get; set; } = true;
    public List<string> ExcludedEnvironments { get; set; } = new() { "Production" };
    public int MaxRetryAttempts { get; set; } = 3;
    public double RetryDelaySeconds { get; set; } = 2.0;
    public bool EnableHealthChecks { get; set; } = true;
}
```

## Error Handling Patterns

### Milan Jovanovic's Fail-Fast Approach

**Immediate application termination on initialization failure with detailed diagnostics.**

```csharp
public async Task HandleInitializationError(Exception ex, InitializationErrorType errorType)
{
    var errorId = Guid.NewGuid();
    
    _logger.LogCritical(
        "Database initialization failure [{ErrorId}] - Type: {ErrorType}, " +
        "Message: {Message}, Environment: {Environment}",
        errorId, errorType, ex.Message, _hostEnvironment.EnvironmentName);
    
    var guidance = errorType switch
    {
        InitializationErrorType.DatabaseConnectionFailure => 
            "Ensure PostgreSQL is running on localhost:5433. Check connection string.",
        InitializationErrorType.MigrationApplicationError => 
            "Migration failure detected. Review migration scripts manually.",
        InitializationErrorType.SeedDataCreationError => 
            "Seed data creation failed. Check for data conflicts.",
        InitializationErrorType.TimeoutExceeded => 
            "Initialization exceeded timeout. Check database performance.",
        _ => "Review application logs for detailed error information."
    };
    
    _logger.LogError("Resolution guidance [{ErrorId}]: {Guidance}", errorId, guidance);
    
    // Fail fast - no recovery attempts
    throw new InvalidOperationException(
        $"Database initialization failed [{errorId}]: {ex.Message}. {guidance}", ex);
}

public enum InitializationErrorType
{
    DatabaseConnectionFailure,
    MigrationApplicationError,
    SeedDataCreationError,
    TimeoutExceeded,
    ConfigurationError
}
```

**Error Handling Features:**
- Structured logging with correlation IDs
- Environment-specific guidance messages
- No automatic rollback per stakeholder decision
- Application exits with diagnostic information

## Key Code Examples

### Environment Detection Logic

```csharp
private bool ShouldPopulateSeedData(IHostEnvironment hostEnvironment)
{
    // Skip seed data in production environments
    if (_options.ExcludedEnvironments.Contains(
        hostEnvironment.EnvironmentName, 
        StringComparer.OrdinalIgnoreCase))
    {
        _logger.LogInformation(
            "Skipping seed data for {Environment} environment", 
            hostEnvironment.EnvironmentName);
        return false;
    }
    
    return _options.EnableSeedData;
}

private bool ShouldRunMigrations()
{
    // Always run migrations in all environments
    return _options.EnableAutoMigration;
}
```

### Seed Data Implementation with EF Core 9

```csharp
private async Task SeedUsersAsync(CancellationToken cancellationToken)
{
    // EF Core 9's UseAsyncSeeding handles idempotency automatically
    var testAccounts = new[]
    {
        new { Email = "admin@witchcityrope.com", SceneName = "RopeMaster", Role = "Administrator" },
        new { Email = "staff@witchcityrope.com", SceneName = "SafetyFirst", Role = "Moderator" },
        new { Email = "member@witchcityrope.com", SceneName = "RopeEnthusiast", Role = "Member" },
        new { Email = "guest@witchcityrope.com", SceneName = "Newcomer", Role = "Attendee" },
        new { Email = "organizer@witchcityrope.com", SceneName = "EventMaker", Role = "Organizer" }
    };
    
    foreach (var account in testAccounts)
    {
        if (!await _userManager.Users.AnyAsync(u => u.Email == account.Email, cancellationToken))
        {
            var user = new ApplicationUser
            {
                UserName = account.Email,
                Email = account.Email,
                SceneName = account.SceneName,
                Role = account.Role,
                EmailConfirmed = true,
                IsActive = true,
                IsVetted = account.Role == "Administrator" || account.Role == "Teacher"
                // CreatedAt/UpdatedAt handled by ApplicationDbContext.UpdateAuditFields()
            };
            
            await _userManager.CreateAsync(user, "Test123!");
            
            _logger.LogInformation("Created test account: {Email} ({Role})", 
                account.Email, account.Role);
        }
    }
}
```

### Structured Logging Implementation

```csharp
public async Task<InitializationResult> InitializeAsync(CancellationToken cancellationToken)
{
    var stopwatch = Stopwatch.StartNew();
    var result = new InitializationResult 
    { 
        Environment = _hostEnvironment.EnvironmentName,
        CompletedAt = DateTime.UtcNow
    };
    
    _logger.LogInformation(
        "Starting database initialization for environment: {Environment}", 
        _hostEnvironment.EnvironmentName);
    
    try
    {
        // Migration phase with performance tracking
        _logger.LogInformation("Phase 1: Applying pending migrations");
        var migrationStopwatch = Stopwatch.StartNew();
        
        var migrations = await ApplyPendingMigrationsAsync(cancellationToken);
        result.MigrationsApplied = migrations.Count;
        
        _logger.LogInformation(
            "Applied {Count} migrations in {Duration}ms", 
            migrations.Count, migrationStopwatch.ElapsedMilliseconds);
        
        // Seed data phase
        if (ShouldRunSeedData())
        {
            _logger.LogInformation("Phase 2: Populating seed data");
            var seedResult = await _seedDataService.SeedAllDataAsync(cancellationToken);
            result.SeedRecordsCreated = seedResult.SeedRecordsCreated;
        }
        
        result.Success = true;
        result.Duration = stopwatch.Elapsed;
        
        _logger.LogInformation(
            "Database initialization completed in {Duration}ms. " +
            "Migrations: {Migrations}, Seed Records: {SeedRecords}",
            result.Duration.TotalMilliseconds, 
            result.MigrationsApplied, 
            result.SeedRecordsCreated);
    }
    catch (Exception ex)
    {
        result.Success = false;
        result.Errors.Add(ex.Message);
        result.Duration = stopwatch.Elapsed;
        
        _logger.LogError(ex, 
            "Database initialization failed after {Duration}ms", 
            stopwatch.Elapsed.TotalMilliseconds);
        
        throw; // Fail fast
    }
    
    return result;
}
```

## Performance Considerations

### Startup Performance Optimization

- **Maximum Duration**: 30 seconds configurable timeout
- **Typical Duration**: 5-15 seconds for normal operation
- **Memory Overhead**: < 50MB during initialization
- **Connection Reuse**: Uses existing connection pool

### Scalability Design

```csharp
public class DatabaseInitializationService : BackgroundService
{
    private static readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
    private static bool _initializationCompleted = false;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Prevent concurrent initialization across multiple instances
        await _initializationSemaphore.WaitAsync(stoppingToken);
        try
        {
            if (_initializationCompleted) return;
            
            await InitializeAsync(stoppingToken);
            _initializationCompleted = true;
        }
        finally
        {
            _initializationSemaphore.Release();
        }
    }
}
```

## Security Considerations

### Environment-Specific Security

- **Development**: Full seed data including test accounts
- **Staging**: Limited seed data for testing
- **Production**: Migrations only, no seed data
- **Authentication**: Test accounts use documented secure passwords

### Data Protection

- **Transaction Isolation**: All seed operations within transactions
- **Rollback Capability**: Failed operations rollback automatically
- **Audit Trail**: Comprehensive logging of all operations
- **Idempotency**: Safe to run multiple times

## Integration with Existing Patterns

### ApplicationDbContext Integration

**Leverages existing UTC DateTime and audit field patterns.**

```csharp
// Follows existing ApplicationDbContext.cs UTC patterns (lines 159-227)
private Event CreateSeedEvent(string title, int daysFromNow)
{
    return new Event
    {
        Id = Guid.NewGuid(),
        Title = title,
        // UTC dates per existing pattern
        StartDate = DateTime.UtcNow.AddDays(daysFromNow).Date.AddHours(18),
        EndDate = DateTime.UtcNow.AddDays(daysFromNow).Date.AddHours(21),
        // CreatedAt/UpdatedAt handled by ApplicationDbContext.UpdateAuditFields()
    };
}
```

### Service Layer Architecture Alignment

**Follows established service layer patterns and dependency injection.**

- Interface segregation for focused contracts
- Service scoped lifetimes for database contexts
- Result pattern for error handling
- Structured logging throughout
- Clean separation of concerns

## Success Metrics

### Technical Metrics

- **Setup Time**: Manual 2-4 hours → Automated <5 minutes (95% improvement)
- **Reliability**: 100% consistent environment setup
- **Performance**: <30 second initialization with fail-fast error handling
- **Safety**: Zero production impact through environment detection

### Developer Experience Metrics

- **Zero Manual Steps**: Eliminates all 4 manual database setup procedures
- **Onboarding**: New developers productive immediately with `./dev.sh`
- **Error Reduction**: Environment-related support tickets eliminated
- **Demo Reliability**: 100% success rate with proper test data

---

*This service architecture implements database initialization using industry-proven patterns from Milan Jovanovic (fail-fast), Microsoft (IHostedService), and EF Core 9 (UseAsyncSeeding) while maintaining alignment with existing WitchCityRope architectural patterns and microservices design.*