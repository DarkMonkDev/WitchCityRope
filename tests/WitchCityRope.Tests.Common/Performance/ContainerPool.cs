using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;
using WitchCityRope.Tests.Common.Cleanup;

namespace WitchCityRope.Tests.Common.Performance
{
    /// <summary>
    /// Container Pooling Implementation
    /// Phase 2: Test Suite Integration - Enhanced Containerized Testing Infrastructure
    /// 
    /// Features:
    /// - Pre-warm containers for faster startup
    /// - Queue management for available containers
    /// - Database reset between uses
    /// - Performance optimization
    /// - Resource management and cleanup
    /// </summary>
    public static class ContainerPool
    {
        private static readonly ConcurrentQueue<PooledContainer> AvailableContainers = new();
        private static readonly ConcurrentDictionary<string, PooledContainer> InUseContainers = new();
        private static readonly object InitializationLock = new();
        private static volatile bool _isInitialized = false;
        private static readonly ILogger Logger;

        // Configuration
        private const int DefaultPoolSize = 3;
        private const int MaxPoolSize = 5;
        private const int ContainerStartupTimeoutMs = 30000;
        private const int DatabaseResetTimeoutMs = 10000;

        static ContainerPool()
        {
            // Create logger for container pool operations
            using var loggerFactory = LoggerFactory.Create(builder => 
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            Logger = loggerFactory.CreateLogger("ContainerPool");

            // Register cleanup on process exit
            AppDomain.CurrentDomain.ProcessExit += (s, e) => CleanupAllContainers();
            Console.CancelKeyPress += (s, e) => CleanupAllContainers();
        }

        /// <summary>
        /// Initializes the container pool with pre-warmed containers
        /// </summary>
        public static async Task InitializeAsync(int poolSize = DefaultPoolSize)
        {
            if (_isInitialized)
            {
                Logger.LogInformation("Container pool already initialized, skipping");
                return;
            }

            lock (InitializationLock)
            {
                if (_isInitialized) return;

                Logger.LogInformation("Initializing container pool with {PoolSize} containers...", poolSize);
                _isInitialized = true;
            }

            var poolInitializationTimer = Stopwatch.StartNew();

            try
            {
                // Validate pool size
                if (poolSize <= 0 || poolSize > MaxPoolSize)
                {
                    throw new ArgumentException($"Pool size must be between 1 and {MaxPoolSize}", nameof(poolSize));
                }

                // Pre-warm containers in parallel for faster initialization
                var containerTasks = new List<Task<PooledContainer>>();
                
                for (int i = 0; i < poolSize; i++)
                {
                    containerTasks.Add(CreatePooledContainerAsync($"pool-container-{i + 1}"));
                }

                var containers = await Task.WhenAll(containerTasks);
                
                foreach (var container in containers)
                {
                    AvailableContainers.Enqueue(container);
                    Logger.LogInformation("Container {ContainerId} added to pool", container.Container.Id);
                }

                poolInitializationTimer.Stop();
                Logger.LogInformation("Container pool initialization completed in {ElapsedTime:F2} seconds with {ContainerCount} containers",
                    poolInitializationTimer.Elapsed.TotalSeconds, containers.Length);
            }
            catch (Exception ex)
            {
                poolInitializationTimer.Stop();
                Logger.LogError(ex, "Container pool initialization failed after {ElapsedTime:F2} seconds",
                    poolInitializationTimer.Elapsed.TotalSeconds);
                
                // Cleanup any partially created containers
                await CleanupAllContainersAsync();
                _isInitialized = false;
                throw;
            }
        }

        /// <summary>
        /// Gets a container from the pool, creating one if pool is exhausted
        /// </summary>
        public static async Task<PooledContainer> GetContainerAsync(string? requesterId = null)
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            var getContainerTimer = Stopwatch.StartNew();
            requesterId ??= $"requester-{Guid.NewGuid():N}";

            Logger.LogInformation("Getting container for requester {RequesterId}...", requesterId);

            try
            {
                PooledContainer? container;

                // Try to get container from available pool
                if (AvailableContainers.TryDequeue(out container!))
                {
                    Logger.LogInformation("Retrieved pooled container {ContainerId} for {RequesterId}",
                        container.Container.Id, requesterId);
                }
                else
                {
                    // Pool exhausted, create new container
                    Logger.LogWarning("Container pool exhausted, creating new container for {RequesterId}", requesterId);
                    container = await CreatePooledContainerAsync($"on-demand-{requesterId}");
                }

                // Reset database for clean state
                await ResetContainerDatabaseAsync(container);

                // Track container as in use
                InUseContainers.TryAdd(requesterId, container);
                container.AssignedTo = requesterId;
                container.LastUsed = DateTime.UtcNow;

                getContainerTimer.Stop();
                Logger.LogInformation("Container {ContainerId} assigned to {RequesterId} in {ElapsedTime:F2} seconds",
                    container.Container.Id, requesterId, getContainerTimer.Elapsed.TotalSeconds);

                return container;
            }
            catch (Exception ex)
            {
                getContainerTimer.Stop();
                Logger.LogError(ex, "Failed to get container for {RequesterId} after {ElapsedTime:F2} seconds",
                    requesterId, getContainerTimer.Elapsed.TotalSeconds);
                throw;
            }
        }

        /// <summary>
        /// Returns a container to the pool for reuse
        /// </summary>
        public static async Task ReturnContainerAsync(PooledContainer container, string? requesterId = null)
        {
            if (container == null)
            {
                Logger.LogWarning("Attempted to return null container");
                return;
            }

            requesterId ??= container.AssignedTo;
            var returnTimer = Stopwatch.StartNew();

            Logger.LogInformation("Returning container {ContainerId} from {RequesterId}...",
                container.Container.Id, requesterId);

            try
            {
                // Remove from in-use tracking
                if (!string.IsNullOrEmpty(requesterId))
                {
                    InUseContainers.TryRemove(requesterId, out _);
                }

                // Verify container is still healthy
                if (!await IsContainerHealthyAsync(container))
                {
                    Logger.LogWarning("Container {ContainerId} is unhealthy, disposing instead of returning to pool",
                        container.Container.Id);
                    await DisposeContainerAsync(container);
                    return;
                }

                // Reset container state
                await ResetContainerDatabaseAsync(container);

                // Update container metadata
                container.AssignedTo = null;
                container.LastUsed = DateTime.UtcNow;
                container.UsageCount++;

                // Return to available pool
                AvailableContainers.Enqueue(container);

                returnTimer.Stop();
                Logger.LogInformation("Container {ContainerId} returned to pool in {ElapsedTime:F2} seconds (usage count: {UsageCount})",
                    container.Container.Id, returnTimer.Elapsed.TotalSeconds, container.UsageCount);
            }
            catch (Exception ex)
            {
                returnTimer.Stop();
                Logger.LogError(ex, "Failed to return container {ContainerId} after {ElapsedTime:F2} seconds",
                    container.Container.Id, returnTimer.Elapsed.TotalSeconds);
                
                // Dispose unhealthy container
                await DisposeContainerAsync(container);
            }
        }

        /// <summary>
        /// Creates a new pooled container with all necessary setup
        /// </summary>
        private static async Task<PooledContainer> CreatePooledContainerAsync(string containerName)
        {
            var createTimer = Stopwatch.StartNew();
            
            Logger.LogInformation("Creating new pooled container {ContainerName}...", containerName);

            try
            {
                // Create PostgreSQL container with enhanced configuration
                var container = new PostgreSqlBuilder()
                    .WithImage("postgres:16-alpine")  // Production parity
                    .WithDatabase("witchcityrope_test")
                    .WithUsername("test_user")
                    .WithPassword("Test123!")
                    .WithPortBinding(0, 5432)  // Dynamic port allocation
                    .WithCleanUp(true)  // Ensure Ryuk cleanup
                    // Container labeling for identification and cleanup
                    .WithLabel("project", "witchcityrope")
                    .WithLabel("purpose", "testing-pool")
                    .WithLabel("pool-container", "true")
                    .WithLabel("created", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"))
                    .WithLabel("name", containerName)
                    .Build();

                // Register with cleanup service
                ContainerCleanupService.RegisterContainer(container.Id, $"ContainerPool-{containerName}");

                // Start container with timeout
                using var cts = new CancellationTokenSource(ContainerStartupTimeoutMs);
                await container.StartAsync(cts.Token);

                // Apply migrations and setup respawn
                var connectionString = container.GetConnectionString();
                
                // Apply EF Core migrations (placeholder - would use actual migration logic)
                Logger.LogInformation("Applying migrations to container {ContainerId}", container.Id);
                
                // Setup Respawn for fast database reset
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                
                var respawner = await Respawn.Respawner.CreateAsync(connection, new RespawnerOptions
                {
                    DbAdapter = DbAdapter.Postgres,
                    SchemasToInclude = new[] { "public" },
                    TablesToIgnore = new Respawn.Graph.Table[] { "__EFMigrationsHistory" }
                });

                var pooledContainer = new PooledContainer
                {
                    Container = container,
                    Respawner = respawner,
                    ConnectionString = connectionString,
                    CreatedAt = DateTime.UtcNow,
                    UsageCount = 0,
                    Name = containerName
                };

                createTimer.Stop();
                Logger.LogInformation("Pooled container {ContainerName} created successfully in {ElapsedTime:F2} seconds (ID: {ContainerId})",
                    containerName, createTimer.Elapsed.TotalSeconds, container.Id);

                return pooledContainer;
            }
            catch (Exception ex)
            {
                createTimer.Stop();
                Logger.LogError(ex, "Failed to create pooled container {ContainerName} after {ElapsedTime:F2} seconds",
                    containerName, createTimer.Elapsed.TotalSeconds);
                throw;
            }
        }

        /// <summary>
        /// Resets the database in a container to clean state
        /// </summary>
        private static async Task ResetContainerDatabaseAsync(PooledContainer container)
        {
            var resetTimer = Stopwatch.StartNew();
            
            try
            {
                Logger.LogInformation("Resetting database for container {ContainerId}...", container.Container.Id);

                using var cts = new CancellationTokenSource(DatabaseResetTimeoutMs);
                
                await using var connection = new NpgsqlConnection(container.ConnectionString);
                await connection.OpenAsync(cts.Token);
                await container.Respawner.ResetAsync(connection);

                resetTimer.Stop();
                Logger.LogInformation("Database reset completed for container {ContainerId} in {ElapsedTime:F2} seconds",
                    container.Container.Id, resetTimer.Elapsed.TotalSeconds);
            }
            catch (Exception ex)
            {
                resetTimer.Stop();
                Logger.LogError(ex, "Database reset failed for container {ContainerId} after {ElapsedTime:F2} seconds",
                    container.Container.Id, resetTimer.Elapsed.TotalSeconds);
                throw;
            }
        }

        /// <summary>
        /// Checks if a container is healthy and ready for use
        /// </summary>
        private static async Task<bool> IsContainerHealthyAsync(PooledContainer container)
        {
            try
            {
                // Test database connectivity directly - more reliable than container state checks
                // Note: TestContainers API varies between versions, so we focus on database connectivity

                // Test database connectivity
                await using var connection = new NpgsqlConnection(container.ConnectionString);
                await connection.OpenAsync();
                await using var command = new NpgsqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync();

                Logger.LogDebug("Container {ContainerId} health check passed", container.Container.Id);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Container {ContainerId} health check failed", container.Container.Id);
                return false;
            }
        }

        /// <summary>
        /// Disposes a single container and unregisters it from cleanup
        /// </summary>
        private static async Task DisposeContainerAsync(PooledContainer container)
        {
            try
            {
                Logger.LogInformation("Disposing container {ContainerId}...", container.Container.Id);

                // Unregister from cleanup service
                ContainerCleanupService.UnregisterContainer(container.Container.Id);

                // Stop and dispose container
                await container.Container.StopAsync();
                await container.Container.DisposeAsync();

                Logger.LogInformation("Container {ContainerId} disposed successfully", container.Container.Id);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to dispose container {ContainerId}", container.Container.Id);
            }
        }

        /// <summary>
        /// Gets pool statistics for monitoring and debugging
        /// </summary>
        public static PoolStatistics GetStatistics()
        {
            return new PoolStatistics
            {
                AvailableContainers = AvailableContainers.Count,
                InUseContainers = InUseContainers.Count,
                TotalContainers = AvailableContainers.Count + InUseContainers.Count,
                IsInitialized = _isInitialized
            };
        }

        /// <summary>
        /// Cleans up all containers (synchronous version for process exit)
        /// </summary>
        private static void CleanupAllContainers()
        {
            Logger.LogInformation("Emergency cleanup of all pooled containers...");

            try
            {
                CleanupAllContainersAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Emergency cleanup failed");
            }
        }

        /// <summary>
        /// Cleans up all containers in the pool
        /// </summary>
        public static async Task CleanupAllContainersAsync()
        {
            Logger.LogInformation("Cleaning up all pooled containers...");

            var cleanupTasks = new List<Task>();

            // Cleanup available containers
            while (AvailableContainers.TryDequeue(out var container))
            {
                cleanupTasks.Add(DisposeContainerAsync(container));
            }

            // Cleanup in-use containers
            foreach (var kvp in InUseContainers)
            {
                cleanupTasks.Add(DisposeContainerAsync(kvp.Value));
            }
            InUseContainers.Clear();

            // Wait for all cleanup tasks to complete
            await Task.WhenAll(cleanupTasks);

            _isInitialized = false;
            Logger.LogInformation("All pooled containers cleaned up successfully");
        }
    }

    /// <summary>
    /// Represents a container in the pool with additional metadata
    /// </summary>
    public class PooledContainer
    {
        public PostgreSqlContainer Container { get; set; } = null!;
        public Respawner Respawner { get; set; } = null!;
        public string ConnectionString { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime LastUsed { get; set; }
        public string? AssignedTo { get; set; }
        public int UsageCount { get; set; }
        public string Name { get; set; } = null!;
    }

    /// <summary>
    /// Pool statistics for monitoring
    /// </summary>
    public class PoolStatistics
    {
        public int AvailableContainers { get; set; }
        public int InUseContainers { get; set; }
        public int TotalContainers { get; set; }
        public bool IsInitialized { get; set; }
    }
}