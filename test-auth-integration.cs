using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Web.Services;
using WitchCityRope.Web;

public class AuthTestProgram
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== DIRECT AUTHENTICATION SERVICE TEST ===");
        
        try
        {
            // Create a test application factory
            await using var application = new WebApplicationFactory<Program>();
            
            // Get the authentication service from the container
            using var scope = application.Services.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
            
            Console.WriteLine("✓ Authentication service retrieved from DI container");
            
            // Test login directly
            Console.WriteLine("\n--- Testing admin login ---");
            Console.WriteLine("Email: admin@witchcityrope.com");
            Console.WriteLine("Password: Test123!");
            
            var result = await authService.LoginAsync("admin@witchcityrope.com", "Test123!", false);
            
            Console.WriteLine($"\nResult: Success = {result.Success}");
            if (!result.Success)
            {
                Console.WriteLine($"Error: {result.Error}");
            }
            else
            {
                Console.WriteLine("✓ Login successful!");
                
                // Test getting current user
                var currentUser = await authService.GetCurrentUserAsync();
                if (currentUser != null)
                {
                    Console.WriteLine($"✓ Current user: {currentUser.Email} ({currentUser.DisplayName})");
                }
                else
                {
                    Console.WriteLine("⚠ GetCurrentUserAsync returned null after successful login");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ ERROR: {ex.Message}");
            Console.WriteLine($"Type: {ex.GetType().Name}");
            Console.WriteLine($"Stack trace:\n{ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"\nInner exception: {ex.InnerException.Message}");
                Console.WriteLine($"Inner stack trace:\n{ex.InnerException.StackTrace}");
            }
        }
        
        Console.WriteLine("\n=== TEST COMPLETE ===");
    }
}