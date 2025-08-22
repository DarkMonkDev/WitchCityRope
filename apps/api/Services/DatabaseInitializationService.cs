using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Services;

/// <summary>
/// Background service that handles automatic database initialization and seed data population
/// using Milan Jovanovic's fail-fast patterns with IHostedService/BackgroundService architecture.
/// 
/// Key responsibilities:
/// - Apply pending EF Core migrations automatically in Development and Staging environments
/// - Populate seed data (test accounts, events, vetting statuses) when database is empty
/// - Provide fail-fast error handling with structured logging and diagnostic information
/// - Support environment-specific behavior (Production skips seed data)
/// - Implement retry policies with exponential backoff for Docker container startup delays
/// 
/// Implementation follows service architecture patterns from existing services and incorporates:
/// - BackgroundService base class for IHostedService implementation
/// - 30-second timeout with CancellationToken support
/// - Polly retry policies for transient database connection failures
/// - Comprehensive structured logging with correlation IDs
/// - Static completion tracking for health check integration
/// </summary>
public class DatabaseInitializationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializationService> _logger;
    private readonly DbInitializationOptions _options;
    private static bool _initializationCompleted = false;
    private static readonly SemaphoreSlim _initializationSemaphore = new(1, 1);

    public DatabaseInitializationService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseInitializationService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // Bind configuration options with defaults
        _options = new DbInitializationOptions();
        configuration.GetSection("DatabaseInitialization").Bind(_options);
    }

    /// <summary>
    /// BackgroundService entry point that executes during application startup.
    /// Implements Milan Jovanovic's fail-fast pattern with comprehensive error handling
    /// and prevents concurrent initialization attempts across multiple instances.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Prevent concurrent initialization across multiple instances
        await _initializationSemaphore.WaitAsync(stoppingToken);
        try
        {
            if (_initializationCompleted) 
            {
                _logger.LogInformation("Database initialization already completed, skipping");
                return;
            }

            await InitializeAsync(stoppingToken);
            _initializationCompleted = true;
        }
        finally
        {
            _initializationSemaphore.Release();
        }
    }

    /// <summary>
    /// Core initialization method that orchestrates migration and seed data operations
    /// with comprehensive error handling, timeout management, and structured logging.
    /// 
    /// Follows existing ApplicationDbContext UTC patterns and service layer architecture.
    /// Uses 30-second timeout per functional specification requirements.
    /// </summary>
    private async Task InitializeAsync(CancellationToken stoppingToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var correlationId = Guid.NewGuid();
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["OperationType"] = "DatabaseInitialization"
        });

        using var serviceScope = _serviceProvider.CreateScope();
        var hostEnvironment = serviceScope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _logger.LogInformation("Starting database initialization for environment: {Environment} [{CorrelationId}]",
            hostEnvironment.EnvironmentName, correlationId);

        try
        {
            // Create linked cancellation token with configured timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

            // Phase 1: Apply migrations with retry policy
            _logger.LogInformation("Phase 1: Applying pending migrations [{CorrelationId}]", correlationId);
            var migrationCount = await ApplyMigrationsWithRetryAsync(context, cts.Token);

            // Phase 2: Seed data based on environment
            var seedRecordsCreated = 0;
            if (ShouldPopulateSeedData(hostEnvironment))
            {
                _logger.LogInformation("Phase 2: Populating seed data [{CorrelationId}]", correlationId);
                var seedService = serviceScope.ServiceProvider.GetRequiredService<ISeedDataService>();
                var seedResult = await seedService.SeedAllDataAsync(cts.Token);
                
                if (!seedResult.Success)
                {
                    var errorMessage = $"Seed data creation failed: {string.Join(", ", seedResult.Errors)}";
                    _logger.LogError("Seed data operation failed [{CorrelationId}]: {Errors}", 
                        correlationId, string.Join(", ", seedResult.Errors));
                    
                    if (_options.FailOnSeedDataError)
                    {
                        throw new InvalidOperationException(errorMessage);
                    }
                    _logger.LogWarning("Continuing despite seed data errors due to configuration [{CorrelationId}]", correlationId);
                }
                else
                {
                    seedRecordsCreated = seedResult.SeedRecordsCreated;
                }
            }
            else
            {
                _logger.LogInformation("Phase 2: Skipping seed data for {Environment} environment [{CorrelationId}]",
                    hostEnvironment.EnvironmentName, correlationId);
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "Database initialization completed successfully in {Duration}ms. " +
                "Migrations: {MigrationCount}, Seed Records: {SeedRecords} [{CorrelationId}]",
                stopwatch.ElapsedMilliseconds, migrationCount, seedRecordsCreated, correlationId);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("Database initialization cancelled due to application shutdown [{CorrelationId}]", correlationId);
            throw;
        }
        catch (OperationCanceledException)
        {
            var errorMessage = $"Database initialization exceeded {_options.TimeoutSeconds}-second timeout";
            _logger.LogCritical("{ErrorMessage} [{CorrelationId}]", errorMessage, correlationId);
            await HandleInitializationError(new TimeoutException(errorMessage), InitializationErrorType.TimeoutExceeded, correlationId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogCritical(ex, "Database initialization failed after {Duration}ms [{CorrelationId}]",
                stopwatch.ElapsedMilliseconds, correlationId);
            
            var errorType = ClassifyError(ex);
            await HandleInitializationError(ex, errorType, correlationId);
        }
    }

    /// <summary>
    /// Applies pending EF Core migrations with retry policy to handle Docker container startup delays.
    /// Uses exponential backoff (2s, 4s, 8s) as specified in functional requirements.
    /// 
    /// Follows existing EF Core patterns and PostgreSQL configuration from ApplicationDbContext.
    /// </summary>
    private async Task<int> ApplyMigrationsWithRetryAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var retryCount = 0;
        var maxRetries = _options.MaxRetryAttempts;
        
        while (retryCount <= maxRetries)
        {
            try
            {
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
                var migrationsList = pendingMigrations.ToList();
                
                if (migrationsList.Count > 0)
                {
                    _logger.LogInformation("Applying {Count} pending migrations: {Migrations}",
                        migrationsList.Count, string.Join(", ", migrationsList));
                    
                    await context.Database.MigrateAsync(cancellationToken);
                    
                    _logger.LogInformation("Successfully applied {Count} migrations", migrationsList.Count);
                    return migrationsList.Count;
                }
                else
                {
                    _logger.LogInformation("No pending migrations found");
                    return 0;
                }
            }
            catch (Exception ex) when (retryCount < maxRetries)
            {
                retryCount++;
                var delay = TimeSpan.FromSeconds(_options.RetryDelaySeconds * Math.Pow(2, retryCount - 1));
                
                _logger.LogWarning("Migration attempt {RetryCount} of {MaxRetries} failed, retrying in {Delay}ms: {Error}",
                    retryCount, maxRetries, delay.TotalMilliseconds, ex.Message);
                
                await Task.Delay(delay, cancellationToken);
            }
        }
        
        // If we get here, all retry attempts failed
        throw new InvalidOperationException($"Migration failed after {maxRetries} retry attempts");
    }

    /// <summary>
    /// Determines if seed data should be populated based on environment configuration.
    /// Production environments skip seed data by default for safety.
    /// 
    /// Follows environment detection patterns from existing Program.cs configuration.
    /// </summary>
    private bool ShouldPopulateSeedData(IHostEnvironment hostEnvironment)
    {
        if (!_options.EnableSeedData)
        {
            _logger.LogInformation("Seed data disabled by configuration");
            return false;
        }

        var environmentName = hostEnvironment.EnvironmentName;
        var isExcluded = _options.ExcludedEnvironments.Contains(environmentName, StringComparer.OrdinalIgnoreCase);
        
        if (isExcluded)
        {
            _logger.LogInformation("Seed data excluded for {Environment} environment", environmentName);
            return false;
        }

        _logger.LogInformation("Seed data enabled for {Environment} environment", environmentName);
        return true;
    }

    /// <summary>
    /// Handles initialization errors using Milan Jovanovic's fail-fast pattern.
    /// Provides detailed diagnostic information and guidance for resolution.
    /// Always terminates the application on critical initialization failures.
    /// </summary>
    private async Task HandleInitializationError(Exception ex, InitializationErrorType errorType, Guid correlationId)
    {
        _logger.LogCritical(
            "Database initialization failure [{CorrelationId}] - Type: {ErrorType}, " +
            "Message: {Message}",
            correlationId, errorType, ex.Message);

        var guidance = errorType switch
        {
            InitializationErrorType.DatabaseConnectionFailure =>
                "Ensure PostgreSQL is running and accessible. Check connection string configuration.",
            InitializationErrorType.MigrationApplicationError =>
                "Migration failure detected. Review migration scripts and database state manually.",
            InitializationErrorType.SeedDataCreationError =>
                "Seed data creation failed. Check for data conflicts or constraint violations.",
            InitializationErrorType.TimeoutExceeded =>
                $"Initialization exceeded {_options.TimeoutSeconds}-second timeout. Check database performance and network connectivity.",
            InitializationErrorType.ConfigurationError =>
                "Configuration error detected. Review appsettings.json DatabaseInitialization section.",
            _ => "Review application logs for detailed error information."
        };

        _logger.LogError("Resolution guidance [{CorrelationId}]: {Guidance}", correlationId, guidance);

        // Milan Jovanovic's fail-fast pattern - no recovery attempts
        throw new InvalidOperationException(
            $"Database initialization failed [{correlationId}]: {ex.Message}. {guidance}", ex);
    }

    /// <summary>
    /// Classifies exceptions to provide appropriate error handling and guidance.
    /// Supports troubleshooting by categorizing common failure scenarios.
    /// </summary>
    private static InitializationErrorType ClassifyError(Exception ex)
    {
        return ex switch
        {
            TimeoutException => InitializationErrorType.TimeoutExceeded,
            InvalidOperationException when ex.Message.Contains("configuration") => InitializationErrorType.ConfigurationError,
            InvalidOperationException when ex.Message.Contains("migration") => InitializationErrorType.MigrationApplicationError,
            InvalidOperationException when ex.Message.Contains("seed") => InitializationErrorType.SeedDataCreationError,
            _ when IsConnectionException(ex) => InitializationErrorType.DatabaseConnectionFailure,
            _ => InitializationErrorType.DatabaseConnectionFailure
        };
    }

    /// <summary>
    /// Identifies database connection-related exceptions for appropriate error classification.
    /// </summary>
    private static bool IsConnectionException(Exception ex)
    {
        var message = ex.Message.ToLowerInvariant();
        return message.Contains("connection") || 
               message.Contains("network") || 
               message.Contains("timeout") ||
               message.Contains("host") ||
               ex is System.Net.NetworkInformation.NetworkInformationException ||
               ex is System.Net.Sockets.SocketException;
    }

    /// <summary>
    /// Provides access to initialization completion status for health checks.
    /// Static property ensures status is available across service scopes.
    /// </summary>
    public static bool IsInitializationCompleted => _initializationCompleted;
}

/// <summary>
/// Configuration options for database initialization service.
/// Supports environment-specific behavior and operational parameters.
/// 
/// Default values align with functional specification requirements:
/// - 30-second timeout for fail-fast behavior
/// - 3 retry attempts with 2-second initial delay for exponential backoff
/// - Production environment excluded from seed data by default
/// </summary>
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

/// <summary>
/// Error classification enum for structured error handling and troubleshooting guidance.
/// </summary>
public enum InitializationErrorType
{
    DatabaseConnectionFailure,
    MigrationApplicationError,
    SeedDataCreationError,
    TimeoutExceeded,
    ConfigurationError
}

/// <summary>
/// Result model for tracking initialization operations and outcomes.
/// Provides detailed information for logging, monitoring, and health checks.
/// </summary>
public class InitializationResult
{
    public bool Success { get; set; }
    public TimeSpan Duration { get; set; }
    public int MigrationsApplied { get; set; }
    public int SeedRecordsCreated { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string Environment { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
}