using Microsoft.Extensions.Configuration;
using NBomber;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WitchCityRope.PerformanceTests.Infrastructure
{
    public abstract class PerformanceTestBase
    {
        protected IConfiguration Configuration { get; }
        protected string BaseUrl { get; }
        protected PerformanceTestSettings Settings { get; }

        protected PerformanceTestBase()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            Settings = Configuration.GetSection("PerformanceTests").Get<PerformanceTestSettings>() 
                ?? throw new InvalidOperationException("PerformanceTests configuration section is missing");
            
            BaseUrl = Settings.BaseUrl;
        }

        protected LoadSimulation GetLoadSimulation(string scenarioName)
        {
            if (!Settings.Scenarios.TryGetValue(scenarioName, out var scenario))
            {
                throw new ArgumentException($"Scenario '{scenarioName}' not found in configuration");
            }

            return Simulation.KeepConstant(
                copies: scenario.KeepConstant,
                during: scenario.Duration
            );
        }

        protected NodeStats RunScenarios(params ScenarioProps[] scenarios)
        {
            return NBomberRunner
                .RegisterScenarios(scenarios)
                .WithReportFormats(Settings.ReportFormats.Select(f => (NBomber.Contracts.Stats.ReportFormat)Enum.Parse(typeof(NBomber.Contracts.Stats.ReportFormat), f.ToString())).ToArray())
                .WithReportFolder($"reports/{DateTime.Now:yyyy-MM-dd_HH-mm-ss}")
                .Run();
        }


        protected async Task<string> GetAuthTokenAsync(string email, string password)
        {
            // This would be implemented to get a real auth token
            // For now, returning a mock token
            await Task.Delay(100);
            return $"Bearer mock-token-for-{email}";
        }

        protected string GenerateTestUserEmail()
        {
            return $"perftest_{Guid.NewGuid():N}@test.com";
        }
    }
}