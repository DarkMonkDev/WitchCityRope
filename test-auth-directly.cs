using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WitchCityRope.Web.Services;

// Quick integration test to call the authentication service directly
var host = Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        // Add minimal required services for testing
        services.AddLogging();
    })
    .Build();

Console.WriteLine("Testing authentication service directly...");

try 
{
    // Test the login credentials directly
    Console.WriteLine("Admin login test:");
    Console.WriteLine("Email: admin@witchcityrope.com");
    Console.WriteLine("Password: Test123!");
    
    // We need to create a proper test context with the actual app services
    Console.WriteLine("\nThis test needs to be run as a proper integration test with the full service container.");
    Console.WriteLine("Let me create a proper integration test instead...");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack: {ex.StackTrace}");
}