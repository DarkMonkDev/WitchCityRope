using Bogus;
using NBomber.Contracts;
using NBomber.CSharp;
// Removed NBomber.Http.CSharp - using standard HttpClient
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WitchCityRope.Api.Features.Auth.Models;
using WitchCityRope.PerformanceTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;
using System.Net.Http.Headers;

namespace WitchCityRope.PerformanceTests.LoadTests
{
    public class AuthenticationLoadTests : PerformanceTestBase
    {
        private readonly ITestOutputHelper _output;
        private readonly Faker _faker;
        private readonly HttpClient _httpClient;

        public AuthenticationLoadTests(ITestOutputHelper output)
        {
            _output = output;
            _faker = new Faker();
            _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        }

        [Fact]
        public async Task Login_LoadTest_Normal()
        {
            // Pre-create test users
            var testUsers = await CreateTestUsersAsync(Settings.TestUsers.Count);

            var scenario = Scenario.Create("login_scenario", async context =>
            {
                var randomUser = testUsers[Random.Shared.Next(testUsers.Count)];
                
                var request = new LoginRequest
                {
                    Email = randomUser.Email,
                    Password = randomUser.Password
                };

                var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
                var response = await _httpClient.PostAsync("/api/auth/login", content);

                return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail(statusCode: response.ReasonPhrase ?? "Login failed");
            })
            .WithLoadSimulations(GetLoadSimulation("Normal"))
            .WithWarmUpDuration(Settings.WarmupDuration);

            var stats = RunScenarios(scenario);

            Assert.True(stats.AllOkCount > 0);
            Assert.True(stats.AllFailCount < stats.AllOkCount * 0.01); // Less than 1% errors
        }

        [Fact]
        public async Task Registration_LoadTest_Surge()
        {
            var scenario = Scenario.Create("registration_surge", async context =>
            {
                var request = new RegisterRequest
                {
                    Email = _faker.Internet.Email(),
                    Password = Settings.TestUsers.PasswordPattern,
                    SceneName = _faker.Internet.UserName(),
                    LegalName = _faker.Name.FullName(),
                    DateOfBirth = _faker.Date.Past(30, DateTime.Now.AddYears(-18)),
                    Pronouns = _faker.PickRandom("they/them", "she/her", "he/him", "xe/xir")
                };

                var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
                var response = await _httpClient.PostAsync("/api/auth/register", content);

                return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail(statusCode: response.ReasonPhrase ?? "Login failed");
            })
            .WithLoadSimulations(
                Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
                Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(30)),
                Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)) // Surge
            )
            .WithWarmUpDuration(TimeSpan.FromSeconds(10));

            var stats = RunScenarios(scenario);

            Assert.True(stats.AllOkCount > 0);
            _output.WriteLine($"Total requests: {stats.AllOkCount + stats.AllFailCount}");
            _output.WriteLine($"Success rate: {(double)stats.AllOkCount / (stats.AllOkCount + stats.AllFailCount) * 100:F2}%");
        }

        [Fact]
        public async Task TokenRefresh_LoadTest_Continuous()
        {
            // Pre-create test users and get their tokens
            var testTokens = await GetTestRefreshTokensAsync(100);

            var scenario = Scenario.Create("token_refresh", async context =>
            {
                var randomToken = testTokens[Random.Shared.Next(testTokens.Count)];
                
                var request = new RefreshTokenRequest
                {
                    RefreshToken = randomToken
                };

                var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
                var response = await _httpClient.PostAsync("/api/auth/refresh", content);

                return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail(statusCode: response.ReasonPhrase ?? "Login failed");
            })
            .WithLoadSimulations(GetLoadSimulation("Normal"))
            .WithWarmUpDuration(Settings.WarmupDuration);

            var stats = RunScenarios(scenario);

            // Token refresh should have very high success rate
            Assert.True(stats.AllFailCount < stats.AllOkCount * 0.005); // Less than 0.5% errors
        }

        private async Task<List<(string Email, string Password)>> CreateTestUsersAsync(int count)
        {
            var users = new List<(string Email, string Password)>();
            
            // In a real scenario, this would create users via the API
            // For testing, we'll simulate pre-existing users
            for (int i = 0; i < count; i++)
            {
                users.Add((
                    Email: $"loadtest_{i}@test.com",
                    Password: Settings.TestUsers.PasswordPattern
                ));
            }

            await Task.Delay(100); // Simulate API calls
            return users;
        }

        private async Task<List<string>> GetTestRefreshTokensAsync(int count)
        {
            var tokens = new List<string>();
            
            // In a real scenario, this would login users and collect refresh tokens
            for (int i = 0; i < count; i++)
            {
                tokens.Add($"mock-refresh-token-{Guid.NewGuid()}");
            }

            await Task.Delay(100); // Simulate API calls
            return tokens;
        }
    }
}