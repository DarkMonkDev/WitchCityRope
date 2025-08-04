using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Infrastructure;
using System;
using System.Threading.Tasks;

// Check command line arguments
if (args.Length != 2)
{
    Console.WriteLine("Usage: PasswordReset <email> <new-password>");
    Console.WriteLine("Example: PasswordReset admin@witchcityrope.com NewPassword123!");
    Environment.Exit(1);
}

var email = args[0];
var newPassword = args[1];

// Build the host
var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.SetBasePath(Directory.GetCurrentDirectory());
        config.AddJsonFile("appsettings.json", optional: false);
        config.AddJsonFile("appsettings.Development.json", optional: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        // Add infrastructure services
        services.AddInfrastructure(context.Configuration);
        
        // Add Identity services
        services.AddIdentity<WitchCityRopeUser, WitchCityRopeRole>(options =>
        {
            // Password settings - these should match the main application settings
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            
            // User settings
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ ";
        })
        .AddEntityFrameworkStores<WitchCityRopeIdentityDbContext>()
        .AddDefaultTokenProviders();
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
    })
    .Build();

// Run the password reset
using (var scope = host.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var userManager = services.GetRequiredService<UserManager<WitchCityRopeUser>>();
        
        logger.LogInformation("Looking for user with email: {Email}", email);
        
        // Find the user by email
        var user = await userManager.FindByEmailAsync(email);
        
        if (user == null)
        {
            logger.LogError("User with email '{Email}' not found", email);
            Console.WriteLine($"Error: User with email '{email}' not found");
            Environment.Exit(1);
        }
        
        logger.LogInformation("Found user: {SceneName} (ID: {UserId})", user.SceneNameValue, user.Id);
        
        // Generate a password reset token
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        
        // Reset the password
        var result = await userManager.ResetPasswordAsync(user, resetToken, newPassword);
        
        if (result.Succeeded)
        {
            // Update the last password change timestamp
            user.LastPasswordChangeAt = DateTime.UtcNow;
            await userManager.UpdateAsync(user);
            
            logger.LogInformation("Password reset successful for user: {Email}", email);
            Console.WriteLine($"Success: Password has been reset for user '{email}'");
            Console.WriteLine($"Scene Name: {user.SceneNameValue}");
            Console.WriteLine($"User can login with either email or scene name");
        }
        else
        {
            logger.LogError("Password reset failed. Errors: {Errors}", 
                string.Join(", ", result.Errors.Select(e => $"{e.Code}: {e.Description}")));
            Console.WriteLine("Error: Failed to reset password");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"  - {error.Description}");
            }
            Environment.Exit(1);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while resetting the password");
        Console.WriteLine($"Error: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
        }
        Environment.Exit(1);
    }
}