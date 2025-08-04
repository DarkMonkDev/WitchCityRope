using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Identity;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.Interfaces;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

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
        services.AddInfrastructure(context.Configuration);
        
        // Add Identity services
        services.AddIdentity<WitchCityRopeUser, WitchCityRopeRole>()
            .AddEntityFrameworkStores<WitchCityRopeIdentityDbContext>()
            .AddDefaultTokenProviders();
    })
    .Build();

// Run the seeding
using (var scope = host.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<WitchCityRopeIdentityDbContext>();
    var userManager = services.GetRequiredService<UserManager<WitchCityRopeUser>>();
    var roleManager = services.GetRequiredService<RoleManager<WitchCityRopeRole>>();
    var encryptionService = services.GetRequiredService<IEncryptionService>();
    
    Console.WriteLine("Starting database seeding...");
    
    // Check if already seeded
    if (await context.Users.AnyAsync())
    {
        Console.WriteLine("Database already contains data. Skipping seed.");
        return;
    }
    
    // Ensure roles exist
    var roles = new[] { "Administrator", "Moderator", "Organizer", "Member", "Attendee" };
    foreach (var roleName in roles)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new WitchCityRopeRole
            {
                Name = roleName,
                NormalizedName = roleName.ToUpper(),
                Description = $"{roleName} role",
                IsActive = true,
                Priority = Array.IndexOf(roles, roleName),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }
    
    // Create test users
    var usersData = new[]
    {
        new { Name = "Admin User", SceneName = "Admin", Email = "admin@witchcityrope.com", Age = -30, Role = "Administrator", IsVetted = true },
        new { Name = "Staff Member", SceneName = "Staff", Email = "staff@witchcityrope.com", Age = -28, Role = "Moderator", IsVetted = true },
        new { Name = "Event Organizer", SceneName = "Organizer", Email = "organizer@witchcityrope.com", Age = -35, Role = "Organizer", IsVetted = true },
        new { Name = "Regular Member", SceneName = "Member", Email = "member@witchcityrope.com", Age = -25, Role = "Member", IsVetted = false },
        new { Name = "Guest User", SceneName = "Guest", Email = "guest@witchcityrope.com", Age = -22, Role = "Attendee", IsVetted = false }
    };
    
    var users = new List<WitchCityRopeUser>();
    
    foreach (var userData in usersData)
    {
        var user = new WitchCityRopeUser
        {
            UserName = userData.Email,
            Email = userData.Email,
            EmailConfirmed = true,
            EncryptedLegalName = await encryptionService.EncryptAsync(userData.Name),
            SceneNameValue = userData.SceneName,
            DateOfBirth = DateTime.UtcNow.AddYears(userData.Age),
            Role = Enum.Parse<UserRole>(userData.Role),
            IsActive = true,
            IsVetted = userData.IsVetted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var result = await userManager.CreateAsync(user, "Test123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, userData.Role);
            users.Add(user);
        }
        else
        {
            Console.WriteLine($"Failed to create user {userData.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
    
    Console.WriteLine($"Database seeded successfully!");
    Console.WriteLine($"- {users.Count} users created");
    Console.WriteLine($"- {roles.Length} roles verified/created");
    Console.WriteLine("\nTest accounts:");
    Console.WriteLine("- admin@witchcityrope.com / Test123! (Administrator, Vetted)");
    Console.WriteLine("- staff@witchcityrope.com / Test123! (Moderator, Vetted)");
    Console.WriteLine("- organizer@witchcityrope.com / Test123! (Organizer, Vetted)");
    Console.WriteLine("- member@witchcityrope.com / Test123! (Member, Not Vetted)");
    Console.WriteLine("- guest@witchcityrope.com / Test123! (Attendee, Not Vetted)");
}