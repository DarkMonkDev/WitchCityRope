using Bogus;
using NBomber.Contracts;
using NBomber.CSharp;
// Removed NBomber.Http.CSharp - using standard HttpClient
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Core.Enums;
using WitchCityRope.PerformanceTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace WitchCityRope.PerformanceTests.LoadTests
{
    public class EventsLoadTests : PerformanceTestBase
    {
        private readonly ITestOutputHelper _output;
        private readonly Faker _faker;
        private readonly HttpClient _httpClient;

        public EventsLoadTests(ITestOutputHelper output)
        {
            _output = output;
            _faker = new Faker();
            _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        }

        [Fact]
        public async Task EventListing_LoadTest_HighTraffic()
        {
            var scenario = Scenario.Create("event_listing", async context =>
            {
                // Simulate different query patterns
                var queryParams = GenerateRandomQueryParams();
                
                var response = await _httpClient.GetAsync($"/api/events?{queryParams}");

                return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail(statusCode: response.ReasonPhrase ?? "Request failed");
            })
            .WithLoadSimulations(GetLoadSimulation("Heavy"))
            .WithWarmUpDuration(Settings.WarmupDuration);

            var stats = RunScenarios(scenario);

            Assert.True(stats.AllOkCount > 0);
            Assert.True(stats.ScenarioStats[0].Ok.Latency.Percent95 < 500); // 95% under 500ms
        }

        [Fact]
        public async Task EventRegistration_LoadTest_FlashSale()
        {
            // Pre-create some events
            var eventIds = await CreateTestEventsAsync(10);
            var authTokens = await GetTestAuthTokensAsync(Settings.TestUsers.Count);

            var scenario = Scenario.Create("event_registration_flash_sale", async context =>
            {
                var eventId = eventIds[Random.Shared.Next(eventIds.Count)];
                var authToken = authTokens[Random.Shared.Next(authTokens.Count)];
                
                var request = new RegisterForEventRequest(
                    EventId: eventId,
                    UserId: Guid.NewGuid(), // Simulated user ID
                    DietaryRestrictions: null,
                    AccessibilityNeeds: null,
                    EmergencyContactName: "Test Contact",
                    EmergencyContactPhone: "555-0123",
                    PaymentMethod: WitchCityRope.Api.Features.Events.Models.PaymentMethod.PayPal,
                    PaymentToken: null
                );

                var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/api/events/{eventId}/register")
                {
                    Content = content
                };
                requestMessage.Headers.Add("Authorization", authToken);
                var response = await _httpClient.SendAsync(requestMessage);

                return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail(statusCode: response.ReasonPhrase ?? "Request failed");
            })
            .WithLoadSimulations(
                Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)),  // Ramp up
                Simulation.Inject(rate: 200, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)), // Flash sale burst
                Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20))   // Cool down
            )
            .WithWarmUpDuration(TimeSpan.FromSeconds(5));

            var stats = RunScenarios(scenario);

            _output.WriteLine($"Total registrations attempted: {stats.AllOkCount + stats.AllFailCount}");
            _output.WriteLine($"Successful registrations: {stats.AllOkCount}");
            _output.WriteLine($"Failed registrations: {stats.AllFailCount}");
            _output.WriteLine($"P95 latency: {stats.ScenarioStats[0].Ok.Latency.Percent95}ms");
        }

        [Fact]
        public async Task FeaturedEvents_LoadTest_Homepage()
        {
            var scenario = Scenario.Create("featured_events", async context =>
            {
                var response = await _httpClient.GetAsync("/api/events/featured?count=6");

                return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail(statusCode: response.ReasonPhrase ?? "Request failed");
            })
            .WithLoadSimulations(
                Simulation.KeepConstant(copies: 100, during: TimeSpan.FromMinutes(2))
            )
            .WithWarmUpDuration(Settings.WarmupDuration);

            var stats = RunScenarios(scenario);

            // Check if we have valid stats before assertions
            Assert.NotNull(stats);
            Assert.NotNull(stats.ScenarioStats);
            Assert.NotEmpty(stats.ScenarioStats);
            
            var scenarioStat = stats.ScenarioStats.FirstOrDefault();
            Assert.NotNull(scenarioStat);

            // Homepage requests should be very fast
            Assert.True(scenarioStat.Ok.Latency.Percent95 < 200); // 95% under 200ms
            Assert.True(scenarioStat.Ok.Latency.Percent99 < 500); // 99% under 500ms
        }

        [Fact]
        public async Task MixedEventOperations_LoadTest_Realistic()
        {
            var eventIds = await CreateTestEventsAsync(20);
            var authTokens = await GetTestAuthTokensAsync(100);

            // Event listing scenario (70% of traffic)
            var listingScenario = Scenario.Create("event_listing_mixed", async context =>
            {
                var queryParams = GenerateRandomQueryParams();
                var response = await _httpClient.GetAsync($"/api/events?{queryParams}");
                return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail(statusCode: response.ReasonPhrase ?? "Request failed");
            })
            .WithWeight(70)
            .WithLoadSimulations(Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(3)));

            // Featured events scenario (20% of traffic)
            var featuredScenario = Scenario.Create("featured_events_mixed", async context =>
            {
                var response = await _httpClient.GetAsync("/api/events/featured?count=6");
                return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail(statusCode: response.ReasonPhrase ?? "Request failed");
            })
            .WithWeight(20)
            .WithLoadSimulations(Simulation.KeepConstant(copies: 20, during: TimeSpan.FromMinutes(3)));

            // Registration scenario (10% of traffic)
            var registrationScenario = Scenario.Create("registration_mixed", async context =>
            {
                var eventId = eventIds[Random.Shared.Next(eventIds.Count)];
                var authToken = authTokens[Random.Shared.Next(authTokens.Count)];
                
                var request = new RegisterForEventRequest(
                    EventId: eventId,
                    UserId: Guid.NewGuid(), // Simulated user ID
                    DietaryRestrictions: null,
                    AccessibilityNeeds: null,
                    EmergencyContactName: "Test Contact",
                    EmergencyContactPhone: "555-0123",
                    PaymentMethod: WitchCityRope.Api.Features.Events.Models.PaymentMethod.PayPal,
                    PaymentToken: null
                );

                var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/api/events/{eventId}/register")
                {
                    Content = content
                };
                requestMessage.Headers.Add("Authorization", authToken);
                var response = await _httpClient.SendAsync(requestMessage);
                return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail(statusCode: response.ReasonPhrase ?? "Request failed");
            })
            .WithWeight(10)
            .WithLoadSimulations(Simulation.KeepConstant(copies: 10, during: TimeSpan.FromMinutes(3)));

            var stats = RunScenarios(listingScenario, featuredScenario, registrationScenario);

            foreach (var scenario in stats.ScenarioStats)
            {
                _output.WriteLine($"Scenario: {scenario.ScenarioName}");
                _output.WriteLine($"  Success rate: {100.0 - scenario.Fail.Request.Percent:F2}%");
                _output.WriteLine($"  P95 latency: {scenario.Ok.Latency.Percent95}ms");
                _output.WriteLine($"  Throughput: {scenario.Ok.Request.RPS:F2} req/s");
            }
        }

        private string GenerateRandomQueryParams()
        {
            var queries = new[]
            {
                "page=1&pageSize=20",
                "page=2&pageSize=20&eventType=Workshop",
                "page=1&pageSize=50&status=Published",
                "search=rope&page=1&pageSize=10",
                "startDate=2024-01-01&endDate=2024-12-31&page=1",
                "eventType=Social&status=Published&page=1&pageSize=30"
            };

            return queries[Random.Shared.Next(queries.Length)];
        }

        private async Task<List<Guid>> CreateTestEventsAsync(int count)
        {
            var eventIds = new List<Guid>();
            
            // In a real scenario, this would create events via the API
            for (int i = 0; i < count; i++)
            {
                eventIds.Add(Guid.NewGuid());
            }

            await Task.Delay(100); // Simulate API calls
            return eventIds;
        }

        private async Task<List<string>> GetTestAuthTokensAsync(int count)
        {
            var tokens = new List<string>();
            
            // In a real scenario, this would login users and collect auth tokens
            for (int i = 0; i < count; i++)
            {
                tokens.Add($"Bearer mock-auth-token-{Guid.NewGuid()}");
            }

            await Task.Delay(100); // Simulate API calls
            return tokens;
        }
    }
}