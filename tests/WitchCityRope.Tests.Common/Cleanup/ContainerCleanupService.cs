using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace WitchCityRope.Tests.Common.Cleanup
{
    /// <summary>
    /// Container Cleanup Service for Enhanced Containerized Testing Infrastructure
    /// Phase 1: Prevents orphaned containers through multi-layer cleanup strategy
    /// 
    /// Features:
    /// - Container registration and tracking
    /// - Shutdown hooks for graceful cleanup
    /// - Force cleanup on abnormal termination
    /// - Logging and monitoring of cleanup operations
    /// - Cross-platform support (Linux, Windows, macOS)
    /// </summary>
    public static class ContainerCleanupService
    {
        private static readonly ConcurrentDictionary<string, ContainerInfo> _activeContainers = new();
        private static readonly ILogger _logger;
        private static bool _shutdownHooksRegistered = false;
        private static readonly object _lockObject = new();

        static ContainerCleanupService()
        {
            // Initialize logger
            using var loggerFactory = LoggerFactory.Create(builder => 
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            _logger = loggerFactory.CreateLogger("ContainerCleanupService");

            RegisterShutdownHooks();
        }

        /// <summary>
        /// Container information for tracking and cleanup
        /// </summary>
        private class ContainerInfo
        {
            public string Id { get; set; } = string.Empty;
            public string Source { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public string ProcessId { get; set; } = Environment.ProcessId.ToString();
        }

        /// <summary>
        /// Register a container for cleanup tracking
        /// </summary>
        /// <param name="containerId">Docker container ID</param>
        /// <param name="source">Source component that created the container (e.g., "DatabaseTestFixture")</param>
        public static void RegisterContainer(string containerId, string source = "Unknown")
        {
            if (string.IsNullOrEmpty(containerId))
            {
                _logger.LogWarning("Attempted to register null or empty container ID from source: {Source}", source);
                return;
            }

            var containerInfo = new ContainerInfo
            {
                Id = containerId,
                Source = source,
                CreatedAt = DateTime.UtcNow,
                ProcessId = Environment.ProcessId.ToString()
            };

            _activeContainers.TryAdd(containerId, containerInfo);
            
            _logger.LogInformation("Container registered for cleanup tracking: {ContainerId} from {Source}", 
                containerId, source);
        }

        /// <summary>
        /// Unregister a container from cleanup tracking (successful disposal)
        /// </summary>
        /// <param name="containerId">Docker container ID</param>
        public static void UnregisterContainer(string containerId)
        {
            if (string.IsNullOrEmpty(containerId))
                return;

            if (_activeContainers.TryRemove(containerId, out var containerInfo))
            {
                _logger.LogInformation("Container unregistered from cleanup tracking: {ContainerId} from {Source}", 
                    containerId, containerInfo.Source);
            }
        }

        /// <summary>
        /// Get count of currently tracked containers
        /// </summary>
        public static int TrackedContainerCount => _activeContainers.Count;

        /// <summary>
        /// Register shutdown hooks for graceful cleanup
        /// </summary>
        private static void RegisterShutdownHooks()
        {
            lock (_lockObject)
            {
                if (_shutdownHooksRegistered)
                    return;

                try
                {
                    // Register for normal process exit
                    AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                    
                    // Register for CTRL+C and SIGTERM on Unix-like systems
                    Console.CancelKeyPress += OnCancelKeyPress;

                    // Register for unhandled exceptions
                    AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

                    _shutdownHooksRegistered = true;
                    _logger.LogInformation("Container cleanup shutdown hooks registered successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to register cleanup shutdown hooks");
                }
            }
        }

        /// <summary>
        /// Handle normal process exit
        /// </summary>
        private static void OnProcessExit(object? sender, EventArgs e)
        {
            _logger.LogInformation("Process exit detected, initiating container cleanup...");
            CleanupAllContainers("ProcessExit");
        }

        /// <summary>
        /// Handle CTRL+C and other cancellation signals
        /// </summary>
        private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            _logger.LogInformation("Cancel key press detected, initiating container cleanup...");
            CleanupAllContainers("CancelKeyPress");
            
            // Allow normal cancellation to proceed after cleanup
            e.Cancel = false;
        }

        /// <summary>
        /// Handle unhandled exceptions
        /// </summary>
        private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            _logger.LogError("Unhandled exception detected, initiating emergency container cleanup...");
            CleanupAllContainers("UnhandledException");
        }

        /// <summary>
        /// Force cleanup of all tracked containers
        /// </summary>
        /// <param name="trigger">What triggered the cleanup</param>
        public static void CleanupAllContainers(string trigger = "Manual")
        {
            if (_activeContainers.IsEmpty)
            {
                _logger.LogInformation("No containers to clean up (trigger: {Trigger})", trigger);
                return;
            }

            _logger.LogInformation("Starting cleanup of {Count} tracked containers (trigger: {Trigger})", 
                _activeContainers.Count, trigger);

            var cleanupTimer = Stopwatch.StartNew();
            var successCount = 0;
            var failureCount = 0;

            // Create a snapshot to avoid collection modification issues
            var containersToCleanup = _activeContainers.ToArray();

            foreach (var kvp in containersToCleanup)
            {
                var containerId = kvp.Key;
                var containerInfo = kvp.Value;

                try
                {
                    _logger.LogInformation("Force stopping container: {ContainerId} from {Source} (created: {CreatedAt})", 
                        containerId, containerInfo.Source, containerInfo.CreatedAt);

                    // Use Docker CLI to force remove the container
                    var result = ExecuteDockerCommand($"rm -f {containerId}");
                    
                    if (result.Success)
                    {
                        _activeContainers.TryRemove(containerId, out _);
                        successCount++;
                        _logger.LogInformation("Successfully cleaned up container: {ContainerId}", containerId);
                    }
                    else
                    {
                        failureCount++;
                        _logger.LogWarning("Failed to cleanup container {ContainerId}: {Error}", 
                            containerId, result.Error);
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    _logger.LogError(ex, "Exception during cleanup of container: {ContainerId}", containerId);
                }
            }

            cleanupTimer.Stop();
            
            _logger.LogInformation(
                "Container cleanup completed in {ElapsedTime:F2} seconds. Success: {SuccessCount}, Failed: {FailureCount}", 
                cleanupTimer.Elapsed.TotalSeconds, successCount, failureCount);

            if (cleanupTimer.Elapsed.TotalSeconds > 30)
            {
                _logger.LogWarning("Container cleanup exceeded 30-second target: {ActualTime:F2}s", 
                    cleanupTimer.Elapsed.TotalSeconds);
            }
        }

        /// <summary>
        /// Execute Docker CLI command with result capture
        /// </summary>
        /// <param name="arguments">Docker command arguments</param>
        /// <returns>Command execution result</returns>
        private static (bool Success, string Output, string Error) ExecuteDockerCommand(string arguments)
        {
            try
            {
                using var process = new Process();
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                
                process.WaitForExit();

                return (process.ExitCode == 0, output, error);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, ex.Message);
            }
        }

        /// <summary>
        /// Verify that no orphaned containers exist for this project
        /// </summary>
        /// <returns>True if no orphaned containers found</returns>
        public static bool VerifyNoOrphanedContainers()
        {
            try
            {
                // Find containers with our project labels that are still running
                var result = ExecuteDockerCommand("ps -a --filter \"label=project=witchcityrope\" --format \"{{.ID}} {{.Status}}\"");
                
                if (!result.Success)
                {
                    _logger.LogWarning("Could not verify orphaned containers: {Error}", result.Error);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(result.Output))
                {
                    _logger.LogInformation("No WitchCityRope containers found - all clean");
                    return true;
                }

                var lines = result.Output.Trim().Split('\n');
                var orphanedCount = lines.Length;

                _logger.LogWarning("Found {Count} potential orphaned containers:", orphanedCount);
                foreach (var line in lines)
                {
                    _logger.LogWarning("  {ContainerInfo}", line);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during orphaned container verification");
                return false;
            }
        }

        /// <summary>
        /// Get diagnostic information about the cleanup service
        /// </summary>
        /// <returns>Diagnostic information string</returns>
        public static string GetDiagnosticInfo()
        {
            var info = $"""
                ContainerCleanupService Diagnostics:
                - Tracked containers: {_activeContainers.Count}
                - Shutdown hooks registered: {_shutdownHooksRegistered}
                - Process ID: {Environment.ProcessId}
                - Platform: {RuntimeInformation.OSDescription}
                
                Active containers:
                """;

            foreach (var kvp in _activeContainers)
            {
                var container = kvp.Value;
                info += $"""
                  - {container.Id} ({container.Source}) - Created: {container.CreatedAt:yyyy-MM-dd HH:mm:ss}
                """;
            }

            return info;
        }
    }
}