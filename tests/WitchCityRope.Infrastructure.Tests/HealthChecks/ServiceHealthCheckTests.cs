using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Xunit;
using Xunit.Abstractions;

namespace WitchCityRope.Infrastructure.Tests.HealthChecks;

/// <summary>
/// Pre-flight health checks to verify all services are running on correct ports
/// before running the full test suite. This prevents false test failures due to
/// infrastructure issues like incorrect port configurations.
/// </summary>
[Trait("Category", "HealthCheck")]
public class ServiceHealthCheckTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServiceHealthCheckTests> _logger;
    
    // Use configuration or environment variables for ports (not hardcoded)
    private readonly string _reactUrl;
    private readonly string _apiUrl;
    private readonly string _postgresConnection;

    public ServiceHealthCheckTests(ITestOutputHelper output)
    {
        _output = output;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        
        // Build configuration from environment variables or appsettings
        _configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.json", optional: true)
            .Build();
        
        // Use configuration with fallback to defaults
        _reactUrl = _configuration["ReactUrl"] ?? "http://localhost:5173";
        _apiUrl = _configuration["ApiUrl"] ?? "http://localhost:5655";
        _postgresConnection = _configuration["PostgresConnection"] ?? 
            "Host=localhost;Port=5433;Database=WitchCityRope_Dev;Username=postgres;Password=postgres";
        
        // Create logger
        using var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        _logger = loggerFactory.CreateLogger<ServiceHealthCheckTests>();
    }

    [Fact]
    [Trait("Priority", "Critical")]
    public async Task VerifyReactDevServerIsAccessible()
    {
        try
        {
            var response = await _httpClient.GetAsync(_reactUrl);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("<!doctype html>", content);
            
            _output.WriteLine($"✅ React dev server is accessible at {_reactUrl}");
        }
        catch (HttpRequestException ex)
        {
            _output.WriteLine($"❌ React dev server not responding on {_reactUrl}");
            _output.WriteLine($"   Error: {ex.Message}");
            _output.WriteLine($"   Fix: Run 'npm run dev' to start the React development server");
            throw new InvalidOperationException(
                $"React dev server not responding on {_reactUrl}. Run: npm run dev", ex);
        }
        catch (TaskCanceledException)
        {
            _output.WriteLine($"❌ React dev server timeout on {_reactUrl}");
            _output.WriteLine($"   The server took longer than 2 seconds to respond");
            _output.WriteLine($"   Fix: Check if the React app is starting up or if port 5173 is blocked");
            throw new InvalidOperationException(
                $"React dev server timeout on {_reactUrl}. Check if port 5173 is blocked");
        }
    }

    [Fact]
    [Trait("Priority", "Critical")]
    public async Task VerifyApiIsHealthy()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiUrl}/health");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Healthy", content);
            
            _output.WriteLine($"✅ API is healthy at {_apiUrl}");
        }
        catch (HttpRequestException ex)
        {
            _output.WriteLine($"❌ API not responding on {_apiUrl}");
            _output.WriteLine($"   Error: {ex.Message}");
            _output.WriteLine($"   Fix: Run 'docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d'");
            _output.WriteLine($"   Or check: docker logs witchcity-api");
            throw new InvalidOperationException(
                $"API not responding on {_apiUrl}. Run: docker-compose up -d", ex);
        }
        catch (TaskCanceledException)
        {
            _output.WriteLine($"❌ API timeout on {_apiUrl}");
            _output.WriteLine($"   The API took longer than 2 seconds to respond");
            _output.WriteLine($"   Fix: Check Docker containers with 'docker ps'");
            throw new InvalidOperationException(
                $"API timeout on {_apiUrl}. Check Docker containers");
        }
    }

    [Fact]
    [Trait("Priority", "Critical")]
    public async Task VerifyPostgreSqlIsAccessible()
    {
        try
        {
            using var connection = new NpgsqlConnection(_postgresConnection);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand("SELECT 1", connection);
            var result = await command.ExecuteScalarAsync();
            Assert.Equal(1, result);
            
            _output.WriteLine($"✅ PostgreSQL is accessible at port 5433");
        }
        catch (NpgsqlException ex)
        {
            _output.WriteLine($"❌ PostgreSQL connection failed");
            _output.WriteLine($"   Connection: {_postgresConnection.Replace("Password=postgres", "Password=***")}");
            _output.WriteLine($"   Error: {ex.Message}");
            _output.WriteLine($"   Fix: Check 'docker logs witchcity-postgres'");
            _output.WriteLine($"   Run: 'docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart postgres'");
            throw new InvalidOperationException(
                "PostgreSQL not accessible on port 5433. Check Docker containers", ex);
        }
        catch (TaskCanceledException)
        {
            _output.WriteLine($"❌ PostgreSQL connection timeout");
            _output.WriteLine($"   The database took longer than 2 seconds to connect");
            _output.WriteLine($"   Fix: Ensure PostgreSQL container is running with 'docker ps'");
            throw new InvalidOperationException(
                "PostgreSQL connection timeout. Ensure container is running");
        }
    }

    [Fact]
    [Trait("Priority", "Critical")]
    public async Task VerifyDockerContainersAreRunning()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps --format \"table {{.Names}}\t{{.Status}}\" --filter \"name=witchcity\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start docker process");
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _output.WriteLine($"❌ Docker command failed");
                _output.WriteLine($"   Error: {error}");
                _output.WriteLine($"   Fix: Ensure Docker Desktop is running");
                throw new InvalidOperationException($"Docker command failed: {error}");
            }

            // Check for specific containers
            var requiredContainers = new[] { "witchcity-api", "witchcity-postgres" };
            foreach (var container in requiredContainers)
            {
                if (!output.Contains(container))
                {
                    _output.WriteLine($"❌ Container '{container}' is not running");
                    _output.WriteLine($"   Fix: Run './dev.sh' or 'docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d'");
                    throw new InvalidOperationException($"Required container '{container}' is not running");
                }
            }

            _output.WriteLine($"✅ All required Docker containers are running");
            _output.WriteLine(output);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _output.WriteLine($"❌ Failed to check Docker containers");
            _output.WriteLine($"   Error: {ex.Message}");
            _output.WriteLine($"   Fix: Ensure Docker Desktop is installed and running");
            throw new InvalidOperationException("Failed to check Docker containers", ex);
        }
    }

    [Fact]
    [Trait("Priority", "Critical")]
    [Trait("Category", "MasterHealthCheck")]
    public async Task RunAllHealthChecks()
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<(string name, bool success, string message)>();

        // Run health checks in order of dependency
        var healthChecks = new[]
        {
            ("Docker Containers", (Func<Task>)VerifyDockerContainersAreRunning),
            ("PostgreSQL Database", (Func<Task>)VerifyPostgreSqlIsAccessible),
            ("API Service", (Func<Task>)VerifyApiIsHealthy),
            ("React Dev Server", (Func<Task>)VerifyReactDevServerIsAccessible)
        };

        foreach (var (name, check) in healthChecks)
        {
            try
            {
                await check();
                results.Add((name, true, "✅ Healthy"));
                _output.WriteLine($"✅ {name}: Healthy");
            }
            catch (Exception ex)
            {
                results.Add((name, false, $"❌ {ex.Message}"));
                _output.WriteLine($"❌ {name}: {ex.Message}");
            }
        }

        stopwatch.Stop();
        
        _output.WriteLine($"\n📊 Health Check Summary:");
        _output.WriteLine($"   Total Time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"   Passed: {results.Count(r => r.success)}");
        _output.WriteLine($"   Failed: {results.Count(r => !r.success)}");

        // Ensure all checks complete within 5 seconds
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
            $"Health checks took too long: {stopwatch.ElapsedMilliseconds}ms (max 5000ms)");

        // If any critical services are down, fail the master check
        if (results.Any(r => !r.success))
        {
            var failures = string.Join("\n", results.Where(r => !r.success).Select(r => $"  - {r.name}: {r.message}"));
            throw new InvalidOperationException($"Infrastructure health checks failed:\n{failures}");
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}