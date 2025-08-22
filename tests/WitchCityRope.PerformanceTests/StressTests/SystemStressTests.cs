using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
// Removed NBomber.Http.CSharp - using standard HttpClient
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WitchCityRope.Api.Features.Auth.Models;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.PerformanceTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace WitchCityRope.PerformanceTests.StressTests
{
    public class SystemStressTests : PerformanceTestBase
    {
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _httpClient;

        public SystemStressTests(ITestOutputHelper output)
        {
            _output = output;
            _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        }

        [Fact]
        public async Task BreakingPoint_StressTest()
        {
            var results = new List<(int Load, double SuccessRate, double P95Latency)>();

            // Gradually increase load to find breaking point
            var loadLevels = new[] { 50, 100, 200, 400, 800, 1000, 1500, 2000 };

            foreach (var load in loadLevels)
            {
                _output.WriteLine($"Testing with load: {load} concurrent users");

                var scenario = Scenario.Create($"breaking_point_{load}", async context =>
                {
                    // Mix of operations
                    var operation = Random.Shared.Next(3);
                    
                    switch (operation)
                    {
                        case 0: // Login
                            var loginRequest = new LoginRequest
                            {
                                Email = $"stress_user_{Random.Shared.Next(1000)}@test.com",
                                Password = "TestPassword123!"
                            };
                            
                            var content = new StringContent(JsonSerializer.Serialize(loginRequest), System.Text.Encoding.UTF8, "application/json");
                            var loginResponse = await _httpClient.PostAsync("/api/auth/login", content);
                            return loginResponse.IsSuccessStatusCode ? Response.Ok() : Response.Fail(statusCode: loginResponse.ReasonPhrase ?? "Login failed");

                        case 1: // Event listing
                            var listResponse = await _httpClient.GetAsync("/api/events?page=1&pageSize=20");
                            return listResponse.IsSuccessStatusCode ? Response.Ok() : Response.Fail(statusCode: listResponse.ReasonPhrase ?? "List failed");

                        default: // Featured events
                            var featuredResponse = await _httpClient.GetAsync("/api/events/featured?count=6");
                            return featuredResponse.IsSuccessStatusCode ? Response.Ok() : Response.Fail(statusCode: featuredResponse.ReasonPhrase ?? "Featured failed");
                    }
                })
                .WithLoadSimulations(
                    Simulation.KeepConstant(copies: load, during: TimeSpan.FromMinutes(1))
                )
                .WithWarmUpDuration(TimeSpan.FromSeconds(10));

                var stats = RunScenarios(scenario);
                var scenarioStats = stats.ScenarioStats[0];
                
                var successRate = 100.0 - scenarioStats.Fail.Request.Percent;
                var p95Latency = scenarioStats.Ok.Latency.Percent95;
                
                results.Add((load, successRate, p95Latency));
                
                _output.WriteLine($"  Success rate: {successRate:F2}%");
                _output.WriteLine($"  P95 latency: {p95Latency}ms");
                _output.WriteLine($"  Throughput: {scenarioStats.Ok.Request.RPS:F2} req/s");

                // Stop if success rate drops below 90%
                if (successRate < 90)
                {
                    _output.WriteLine($"Breaking point found at {load} concurrent users");
                    break;
                }

                // Wait between tests to let system recover
                await Task.Delay(TimeSpan.FromSeconds(30));
            }

            // Output summary
            _output.WriteLine("\nStress Test Summary:");
            foreach (var (load, successRate, latency) in results)
            {
                _output.WriteLine($"Load: {load}, Success: {successRate:F2}%, P95: {latency}ms");
            }
        }

        [Fact]
        public async Task Recovery_StressTest()
        {
            var phases = new[]
            {
                ("Normal", 50, TimeSpan.FromMinutes(1)),
                ("Stress", 500, TimeSpan.FromMinutes(2)),
                ("Recovery", 50, TimeSpan.FromMinutes(2))
            };

            var phaseResults = new Dictionary<string, ScenarioStats>();

            foreach (var (phaseName, load, duration) in phases)
            {
                _output.WriteLine($"Starting {phaseName} phase with {load} users for {duration}");

                var scenario = Scenario.Create($"recovery_test_{phaseName}", async context =>
                {
                    var response = await _httpClient.GetAsync("/api/events?page=1&pageSize=20");
                    return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail(statusCode: response.ReasonPhrase ?? "Request failed");
                })
                .WithLoadSimulations(
                    Simulation.KeepConstant(copies: load, during: duration)
                );

                var stats = RunScenarios(scenario);
                phaseResults[phaseName] = stats.ScenarioStats[0];

                _output.WriteLine($"  Success rate: {100.0 - stats.ScenarioStats[0].Fail.Request.Percent:F2}%");
                _output.WriteLine($"  P95 latency: {stats.ScenarioStats[0].Ok.Latency.Percent95}ms");

                // Wait between phases
                await Task.Delay(TimeSpan.FromSeconds(30));
            }

            // Verify recovery
            var normalP95 = phaseResults["Normal"].Ok.Latency.Percent95;
            var recoveryP95 = phaseResults["Recovery"].Ok.Latency.Percent95;
            
            _output.WriteLine($"\nRecovery Analysis:");
            _output.WriteLine($"Normal phase P95: {normalP95}ms");
            _output.WriteLine($"Recovery phase P95: {recoveryP95}ms");
            _output.WriteLine($"Recovery ratio: {recoveryP95 / normalP95:F2}x");

            // Recovery should be within 1.5x of normal performance
            Assert.True(recoveryP95 < normalP95 * 1.5, "System did not recover to acceptable performance levels");
        }

        [Fact]
        public async Task MemoryLeak_StressTest()
        {
            var memoryReadings = new List<(TimeSpan Elapsed, long MemoryMB)>();
            var stopwatch = Stopwatch.StartNew();

            // Long-running test with memory monitoring
            var scenario = Scenario.Create("memory_leak_test", async context =>
            {
                // Create objects that might cause memory leaks
                var largeRequest = new
                {
                    Data = Enumerable.Range(0, 1000).Select(i => new
                    {
                        Id = Guid.NewGuid(),
                        Name = $"Item_{i}",
                        Description = new string('x', 1000),
                        Tags = Enumerable.Range(0, 50).Select(j => $"tag_{j}").ToList()
                    }).ToList()
                };

                var content = new StringContent(JsonSerializer.Serialize(largeRequest), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/events/search", content);

                // Periodically capture memory usage
                if (stopwatch.Elapsed.TotalSeconds % 10 < 1)
                {
                    var memoryMB = GC.GetTotalMemory(false) / 1024 / 1024;
                    memoryReadings.Add((stopwatch.Elapsed, memoryMB));
                }

                return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail(statusCode: response.ReasonPhrase ?? "Request failed");
            })
            .WithLoadSimulations(
                Simulation.KeepConstant(copies: 20, during: TimeSpan.FromMinutes(5))
            );

            var stats = RunScenarios(scenario);

            // Analyze memory growth
            if (memoryReadings.Count > 2)
            {
                var initialMemory = memoryReadings.First().MemoryMB;
                var finalMemory = memoryReadings.Last().MemoryMB;
                var memoryGrowth = finalMemory - initialMemory;
                var growthRate = memoryGrowth / stopwatch.Elapsed.TotalMinutes;

                _output.WriteLine($"Memory Analysis:");
                _output.WriteLine($"Initial memory: {initialMemory}MB");
                _output.WriteLine($"Final memory: {finalMemory}MB");
                _output.WriteLine($"Total growth: {memoryGrowth}MB");
                _output.WriteLine($"Growth rate: {growthRate:F2}MB/min");

                // Alert if memory growth is excessive (>50MB/min)
                Assert.True(growthRate < 50, $"Potential memory leak detected: {growthRate:F2}MB/min growth rate");
            }
        }

        [Fact]
        public async Task DatabaseConnectionPool_StressTest()
        {
            // Test connection pool exhaustion with many concurrent database operations
            var scenario = Scenario.Create("db_connection_pool_test", async context =>
            {
                // Simulate operations that hold database connections
                // Create multiple parallel database operations
                var responses = new List<HttpResponseMessage>();
                
                for (int i = 0; i < 5; i++)
                {
                    var response = await _httpClient.GetAsync($"/api/events?page={Random.Shared.Next(1, 100)}&pageSize=50&includeDetails=true");
                    responses.Add(response);
                }
                
                // If any request failed, consider the whole operation failed
                var anyFailed = responses.Any(r => !r.IsSuccessStatusCode);
                return anyFailed 
                    ? Response.Fail(statusCode: "One or more requests failed") 
                    : Response.Ok();
            })
            .WithLoadSimulations(
                Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
                Simulation.KeepConstant(copies: 200, during: TimeSpan.FromMinutes(1)),
                Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
            );

            var stats = RunScenarios(scenario);
            var scenarioStats = stats.ScenarioStats[0];

            _output.WriteLine($"Connection Pool Test Results:");
            _output.WriteLine($"Success rate: {100.0 - scenarioStats.Fail.Request.Percent:F2}%");
            _output.WriteLine($"Failed requests: {scenarioStats.Fail.Request.Count}");
            _output.WriteLine($"P95 latency: {scenarioStats.Ok.Latency.Percent95}ms");
            _output.WriteLine($"Max latency: {scenarioStats.Ok.Latency.MaxMs}ms");

            // Check for connection pool exhaustion indicators
            if (scenarioStats.Fail.Request.Count > 0)
            {
                _output.WriteLine("Warning: Connection pool may have been exhausted");
            }
        }

        [Fact]
        public async Task CascadingFailure_StressTest()
        {
            // Test system behavior when one component fails
            var services = new[] { "auth", "events", "payments" };
            var serviceHealth = new Dictionary<string, bool>
            {
                ["auth"] = true,
                ["events"] = true,
                ["payments"] = true
            };

            var scenario = Scenario.Create("cascading_failure_test", async context =>
            {
                var service = services[Random.Shared.Next(services.Length)];
                
                // Simulate service degradation after some time
                if (context.InvocationNumber > 1000 && service == "auth")
                {
                    serviceHealth["auth"] = false;
                }

                // If auth is down, other services might start failing
                if (!serviceHealth["auth"] && Random.Shared.NextDouble() < 0.3)
                {
                    serviceHealth["events"] = false;
                    serviceHealth["payments"] = false;
                }

                // Simulate requests based on service health
                if (!serviceHealth[service])
                {
                    await Task.Delay(Random.Shared.Next(1000, 5000)); // Simulate timeout
                    return Response.Fail(statusCode: $"{service} service unavailable");
                }

                var response = service switch
                {
                    "auth" => await _httpClient.PostAsync(
                        "/api/auth/login",
                        new StringContent(
                            JsonSerializer.Serialize(new LoginRequest 
                            { 
                                Email = "test@test.com", 
                                Password = "password" 
                            }), 
                            System.Text.Encoding.UTF8, 
                            "application/json")
                    ),
                    "events" => await _httpClient.GetAsync("/api/events"),
                    _ => await _httpClient.GetAsync("/api/payments/methods")
                };

                return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail(statusCode: response.ReasonPhrase ?? "Request failed");
            })
            .WithLoadSimulations(
                Simulation.KeepConstant(copies: 100, during: TimeSpan.FromMinutes(3))
            );

            var stats = RunScenarios(scenario);
            
            _output.WriteLine("Cascading Failure Test Results:");
            _output.WriteLine($"Total requests: {stats.AllOkCount + stats.AllFailCount}");
            _output.WriteLine($"Failed requests: {stats.AllFailCount}");
            _output.WriteLine($"Failure rate: {(double)stats.AllFailCount / (stats.AllOkCount + stats.AllFailCount) * 100:F2}%");
            
            foreach (var (service, health) in serviceHealth)
            {
                _output.WriteLine($"Service {service}: {(health ? "Healthy" : "Failed")}");
            }
        }
    }
}