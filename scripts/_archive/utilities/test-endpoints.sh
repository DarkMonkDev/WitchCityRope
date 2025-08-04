#!/bin/bash

echo "=== Endpoint Testing Script ==="
echo "This script will test your application endpoints to see what's actually happening"
echo "vs what your integration tests expect."
echo

# Change to the correct directory
cd /home/chad/repos/witchcityrope/WitchCityRope

echo "First, let's run the existing integration tests to see current failures:"
echo "Running: dotnet test tests/WitchCityRope.IntegrationTests/WitchCityRope.IntegrationTests.csproj --verbosity normal"
echo

# Run the integration tests to see what's failing
dotnet test tests/WitchCityRope.IntegrationTests/WitchCityRope.IntegrationTests.csproj --verbosity normal --logger "console;verbosity=detailed" | head -50

echo
echo "=========================="
echo "Now let's create a focused diagnostic..."
echo

# Create a simple diagnostic test file
cat > diagnostic_test.cs << 'EOF'
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using WitchCityRope.IntegrationTests;

// Simple diagnostic to understand what's happening
public class DiagnosticRunner
{
    public static async Task Main()
    {
        Console.WriteLine("=== Starting Diagnostic Test ===");
        
        // Create PostgreSQL container (same as integration tests)
        var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("witchcityrope_test")
            .WithUsername("postgres")
            .WithPassword("test123")
            .WithPortBinding(5432, true)
            .Build();

        try
        {
            Console.WriteLine("Starting PostgreSQL container...");
            await container.StartAsync();
            Console.WriteLine("PostgreSQL started.\n");

            // Create test factory (same as integration tests)
            using var factory = new TestWebApplicationFactory(container);
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            Console.WriteLine("Testing endpoints that are failing in integration tests:\n");

            // Test the main endpoints
            await TestEndpoint(client, "/", "Home page");
            await TestEndpoint(client, "/events", "Events page");
            await TestEndpoint(client, "/login", "Login page");
            await TestEndpoint(client, "/dashboard", "Dashboard (protected)");

            Console.WriteLine("\n=== Summary ===");
            Console.WriteLine("Compare these results with your integration test expectations.");
            Console.WriteLine("If any endpoint returns 500 errors, that's the main issue to fix.");
            Console.WriteLine("If endpoints return 404, they may not be properly configured.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
        }
        finally
        {
            await container.DisposeAsync();
        }
    }

    private static async Task TestEndpoint(HttpClient client, string url, string description)
    {
        Console.WriteLine($"Testing: {description} ({url})");
        try
        {
            var response = await client.GetAsync(url);
            Console.WriteLine($"  Status: {(int)response.StatusCode} {response.StatusCode}");
            
            if (response.Headers.Location != null)
            {
                Console.WriteLine($"  Redirects to: {response.Headers.Location}");
            }

            var content = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(content))
            {
                var preview = content.Length > 100 ? content.Substring(0, 100) + "..." : content;
                Console.WriteLine($"  Content starts with: {preview}");
            }

            // Analysis
            if ((int)response.StatusCode >= 500)
            {
                Console.WriteLine($"  🚨 SERVER ERROR - This is the problem!");
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine($"  ❌ NOT FOUND - Route not configured");
            }
            else if (response.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine($"  ✅ OK - Working correctly");
            }
            else if (response.StatusCode == HttpStatusCode.Redirect)
            {
                Console.WriteLine($"  ↪️ REDIRECT - May be expected for protected routes");
            }
            else
            {
                Console.WriteLine($"  ❓ Other status - Check if this is expected");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  💥 EXCEPTION: {ex.Message}");
        }
        Console.WriteLine();
    }
}
EOF

# Create project file
cat > diagnostic_test.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.0" />
    <PackageReference Include="Testcontainers.PostgreSql" Version="3.10.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="tests\WitchCityRope.IntegrationTests\WitchCityRope.IntegrationTests.csproj" />
  </ItemGroup>
</Project>
EOF

echo "Running diagnostic test..."
dotnet run --project diagnostic_test.csproj

# Clean up
rm -f diagnostic_test.cs diagnostic_test.csproj

echo
echo "=== Diagnostic Complete ==="
echo "The diagnostic test above shows you exactly what your endpoints are returning."
echo "Compare this with what your integration tests expect to identify the root cause."