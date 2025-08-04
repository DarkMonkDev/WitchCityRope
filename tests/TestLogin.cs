using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Infrastructure.Identity;

namespace TestLogin
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder => builder.AddConsole());
            
            // Add DbContext
            services.AddDbContext<WitchCityRopeIdentityDbContext>(options =>
                options.UseNpgsql("Host=localhost;Port=5433;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!"));
            
            // Add Identity
            services.AddIdentity<WitchCityRopeUser, WitchCityRopeRole>()
                .AddEntityFrameworkStores<WitchCityRopeIdentityDbContext>()
                .AddDefaultTokenProviders();
            
            var serviceProvider = services.BuildServiceProvider();
            
            using (var scope = serviceProvider.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WitchCityRopeUser>>();
                var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<WitchCityRopeUser>>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                
                // Test login
                var email = "admin@witchcityrope.com";
                var password = "Test123!";
                
                logger.LogInformation("Testing login for {Email}", email);
                
                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    logger.LogError("User not found!");
                    return;
                }
                
                logger.LogInformation("User found: {Id}, EmailConfirmed: {EmailConfirmed}, IsActive: {IsActive}", 
                    user.Id, user.EmailConfirmed, user.IsActive);
                
                // Test password
                var passwordValid = await userManager.CheckPasswordAsync(user, password);
                logger.LogInformation("Password valid: {PasswordValid}", passwordValid);
                
                if (!passwordValid)
                {
                    // Try to set the password correctly
                    logger.LogInformation("Setting password for user...");
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    var result = await userManager.ResetPasswordAsync(user, token, password);
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Password set successfully!");
                    }
                    else
                    {
                        logger.LogError("Failed to set password: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                
                // Test sign in
                var signInResult = await signInManager.PasswordSignInAsync(user, password, false, false);
                logger.LogInformation("SignIn result: Succeeded={Succeeded}, IsLockedOut={IsLockedOut}, IsNotAllowed={IsNotAllowed}, RequiresTwoFactor={RequiresTwoFactor}", 
                    signInResult.Succeeded, signInResult.IsLockedOut, signInResult.IsNotAllowed, signInResult.RequiresTwoFactor);
            }
        }
    }
}