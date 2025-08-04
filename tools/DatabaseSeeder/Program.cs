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
    
    // Create test events
    var adminUser = users.FirstOrDefault(u => u.Role == UserRole.Administrator);
    var organizerUser = users.FirstOrDefault(u => u.Role == UserRole.Organizer);
    
    if (adminUser == null || organizerUser == null)
    {
        Console.WriteLine("Warning: Could not find admin or organizer user for event creation.");
        return;
    }
    
    // Create events without primaryOrganizer (will add organizers through join table)
    var eventsData = new[]
    {
        new
        {
            Title = "Introduction to Rope Bondage",
            Description = "Learn the basics of rope bondage in a safe, inclusive environment. Perfect for beginners!",
            StartDate = DateTime.UtcNow.AddDays(7).AddHours(18),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(21),
            Capacity = 20,
            EventType = EventType.Workshop,
            Location = "The Rope Space, 123 Main St, Salem, MA",
            PricingTiers = new[] { Money.Create(25m, "USD"), Money.Create(35m, "USD"), Money.Create(45m, "USD") }
        },
        new
        {
            Title = "Advanced Suspension Techniques",
            Description = "For experienced practitioners only. Prerequisites required.",
            StartDate = DateTime.UtcNow.AddDays(14).AddHours(19),
            EndDate = DateTime.UtcNow.AddDays(14).AddHours(22),
            Capacity = 12,
            EventType = EventType.Workshop,
            Location = "The Rope Space, 123 Main St, Salem, MA",
            PricingTiers = new[] { Money.Create(65m, "USD"), Money.Create(75m, "USD") }
        },
        new
        {
            Title = "Monthly Rope Social",
            Description = "Join us for our monthly rope social. All skill levels welcome!",
            StartDate = DateTime.UtcNow.AddDays(21).AddHours(19),
            EndDate = DateTime.UtcNow.AddDays(21).AddHours(23),
            Capacity = 50,
            EventType = EventType.Social,
            Location = "Community Center, 456 Oak Ave, Salem, MA",
            PricingTiers = new[] { Money.Create(15m, "USD") }
        },
        new
        {
            Title = "Consent and Communication Workshop",
            Description = "Essential workshop on consent, communication, and negotiation in rope bondage.",
            StartDate = DateTime.UtcNow.AddDays(10).AddHours(14),
            EndDate = DateTime.UtcNow.AddDays(10).AddHours(17),
            Capacity = 30,
            EventType = EventType.Workshop,
            Location = "The Rope Space, 123 Main St, Salem, MA",
            PricingTiers = new[] { Money.Create(20m, "USD"), Money.Create(30m, "USD") }
        },
        new
        {
            Title = "Rope Performance Night",
            Description = "Watch amazing rope performances from local and visiting artists.",
            StartDate = DateTime.UtcNow.AddDays(28).AddHours(20),
            EndDate = DateTime.UtcNow.AddDays(28).AddHours(23),
            Capacity = 100,
            EventType = EventType.Performance,
            Location = "Salem Theater, 789 Broadway, Salem, MA",
            PricingTiers = new[] { Money.Create(25m, "USD"), Money.Create(35m, "USD"), Money.Create(50m, "USD") }
        },
        new
        {
            Title = "Virtual Rope Jam",
            Description = "Online rope practice session. Great for remote members!",
            StartDate = DateTime.UtcNow.AddDays(5).AddHours(20),
            EndDate = DateTime.UtcNow.AddDays(5).AddHours(22),
            Capacity = 200,
            EventType = EventType.Virtual,
            Location = "Online - Zoom link will be sent to registered participants",
            PricingTiers = new[] { Money.Create(10m, "USD") }
        },
        new
        {
            Title = "Rope Safety and First Aid",
            Description = "Critical safety skills for all rope practitioners.",
            StartDate = DateTime.UtcNow.AddDays(15).AddHours(13),
            EndDate = DateTime.UtcNow.AddDays(15).AddHours(18),
            Capacity = 25,
            EventType = EventType.Workshop,
            Location = "The Rope Space, 123 Main St, Salem, MA",
            PricingTiers = new[] { Money.Create(40m, "USD"), Money.Create(50m, "USD") }
        },
        new
        {
            Title = "Members-Only Play Party",
            Description = "Vetted members only. Must be pre-approved to attend.",
            StartDate = DateTime.UtcNow.AddDays(30).AddHours(21),
            EndDate = DateTime.UtcNow.AddDays(31).AddHours(2),
            Capacity = 40,
            EventType = EventType.PlayParty,
            Location = "Private Venue - Address provided after vetting",
            PricingTiers = new[] { Money.Create(30m, "USD") }
        },
        new
        {
            Title = "Rope Photography Workshop",
            Description = "Learn to capture beautiful rope art through photography.",
            StartDate = DateTime.UtcNow.AddDays(17).AddHours(15),
            EndDate = DateTime.UtcNow.AddDays(17).AddHours(19),
            Capacity = 15,
            EventType = EventType.Workshop,
            Location = "Photo Studio, 321 Art Lane, Salem, MA",
            PricingTiers = new[] { Money.Create(55m, "USD"), Money.Create(65m, "USD") }
        },
        new
        {
            Title = "WitchCity Rope Conference",
            Description = "Our annual conference featuring workshops, performances, and vendors.",
            StartDate = DateTime.UtcNow.AddDays(60).AddHours(9),
            EndDate = DateTime.UtcNow.AddDays(62).AddHours(18),
            Capacity = 300,
            EventType = EventType.Conference,
            Location = "Salem Convention Center, 1000 Harbor Blvd, Salem, MA",
            PricingTiers = new[] { Money.Create(150m, "USD"), Money.Create(200m, "USD"), Money.Create(250m, "USD") }
        }
    };
    
    // Create events using direct database insert since Event entity requires User from Core
    foreach (var eventData in eventsData)
    {
        var eventId = Guid.NewGuid();
        var @event = new
        {
            Id = eventId,
            Title = eventData.Title,
            Description = eventData.Description,
            StartDate = eventData.StartDate,
            EndDate = eventData.EndDate,
            Capacity = eventData.Capacity,
            EventType = eventData.EventType.ToString(),
            Location = eventData.Location,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            PricingTiers = System.Text.Json.JsonSerializer.Serialize(
                eventData.PricingTiers.Select(m => new { Amount = m.Amount, Currency = m.Currency })
            )
        };
        
        // Insert event directly
        await context.Database.ExecuteSqlRawAsync(
            @"INSERT INTO ""Events"" (""Id"", ""Title"", ""Description"", ""StartDate"", ""EndDate"", 
              ""Capacity"", ""EventType"", ""Location"", ""IsPublished"", ""CreatedAt"", ""UpdatedAt"", ""PricingTiers"")
              VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11})",
            @event.Id, @event.Title, @event.Description, @event.StartDate, @event.EndDate,
            @event.Capacity, @event.EventType, @event.Location, @event.IsPublished,
            @event.CreatedAt, @event.UpdatedAt, @event.PricingTiers
        );
        
        // Add organizer relationship
        var organizerId = (eventData == eventsData[2] || eventData == eventsData[4] || eventData == eventsData[7]) 
            ? organizerUser.Id 
            : adminUser.Id;
            
        await context.Database.ExecuteSqlRawAsync(
            @"INSERT INTO ""EventOrganizers"" (""EventId"", ""UserId"") VALUES ({0}, {1})",
            eventId, organizerId
        );
    }
    
    Console.WriteLine($"Database seeded successfully!");
    Console.WriteLine($"- {users.Count} users created");
    Console.WriteLine($"- {eventsData.Length} events created");
    Console.WriteLine($"- {roles.Length} roles verified/created");
    Console.WriteLine("\nTest accounts:");
    Console.WriteLine("- admin@witchcityrope.com / Test123! (Administrator, Vetted)");
    Console.WriteLine("- staff@witchcityrope.com / Test123! (Moderator, Vetted)");
    Console.WriteLine("- organizer@witchcityrope.com / Test123! (Organizer, Vetted)");
    Console.WriteLine("- member@witchcityrope.com / Test123! (Member, Not Vetted)");
    Console.WriteLine("- guest@witchcityrope.com / Test123! (Attendee, Not Vetted)");
}