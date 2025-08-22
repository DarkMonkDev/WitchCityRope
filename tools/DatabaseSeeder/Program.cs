using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.Interfaces;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure;
using WitchCityRope.Infrastructure.Data;
using Microsoft.Extensions.Configuration;

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
    })
    .Build();

// Run the seeding
using (var scope = host.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<WitchCityRopeDbContext>();
    var encryptionService = services.GetRequiredService<IEncryptionService>();
    
    Console.WriteLine("Starting database seeding...");
    
    // Check if already seeded
    if (await context.Users.AnyAsync())
    {
        Console.WriteLine("Database already contains data. Skipping seed.");
        return;
    }
    
    // Create test users
    var users = new[]
    {
        new User(
            await encryptionService.EncryptAsync("Admin User"),
            SceneName.Create("Admin"),
            EmailAddress.Create("admin@witchcityrope.com"),
            DateTime.UtcNow.AddYears(-30),
            UserRole.Administrator
        ),
        new User(
            await encryptionService.EncryptAsync("Staff Member"),
            SceneName.Create("Staff"),
            EmailAddress.Create("staff@witchcityrope.com"),
            DateTime.UtcNow.AddYears(-28),
            UserRole.Moderator
        ),
        new User(
            await encryptionService.EncryptAsync("Event Organizer"),
            SceneName.Create("Organizer"),
            EmailAddress.Create("organizer@witchcityrope.com"),
            DateTime.UtcNow.AddYears(-35),
            UserRole.Moderator
        ),
        new User(
            await encryptionService.EncryptAsync("Regular Member"),
            SceneName.Create("Member"),
            EmailAddress.Create("member@witchcityrope.com"),
            DateTime.UtcNow.AddYears(-25),
            UserRole.Member
        ),
        new User(
            await encryptionService.EncryptAsync("Guest User"),
            SceneName.Create("Guest"),
            EmailAddress.Create("guest@witchcityrope.com"),
            DateTime.UtcNow.AddYears(-22),
            UserRole.Attendee
        )
    };
    
    context.Users.AddRange(users);
    await context.SaveChangesAsync();
    
    // Create user authentication records
    foreach (var user in users)
    {
        var auth = new UserAuthentication
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            IsTwoFactorEnabled = false,
            FailedLoginAttempts = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.UserAuthentications.Add(auth);
    }
    await context.SaveChangesAsync();
    
    // Create test events
    var adminUser = users[0];
    var organizerUser = users[2];
    
    var events = new[]
    {
        new Event(
            title: "Introduction to Rope Bondage",
            description: "Learn the basics of rope bondage in a safe, inclusive environment. Perfect for beginners!",
            startDate: DateTime.UtcNow.AddDays(7).AddHours(18),
            endDate: DateTime.UtcNow.AddDays(7).AddHours(21),
            capacity: 20,
            eventType: EventType.Workshop,
            location: "The Rope Space, 123 Main St, Salem, MA",
            primaryOrganizer: adminUser,
            pricingTiers: new[] { Money.Create(25m, "USD"), Money.Create(35m, "USD"), Money.Create(45m, "USD") }
        ),
        new Event(
            title: "Advanced Suspension Techniques",
            description: "For experienced practitioners only. Prerequisites required.",
            startDate: DateTime.UtcNow.AddDays(14).AddHours(19),
            endDate: DateTime.UtcNow.AddDays(14).AddHours(22),
            capacity: 12,
            eventType: EventType.Workshop,
            location: "The Rope Space, 123 Main St, Salem, MA",
            primaryOrganizer: adminUser,
            pricingTiers: new[] { Money.Create(65m, "USD"), Money.Create(75m, "USD") }
        ),
        new Event(
            title: "Monthly Rope Social",
            description: "Join us for our monthly rope social. All skill levels welcome!",
            startDate: DateTime.UtcNow.AddDays(21).AddHours(19),
            endDate: DateTime.UtcNow.AddDays(21).AddHours(23),
            capacity: 50,
            eventType: EventType.Social,
            location: "Community Center, 456 Oak Ave, Salem, MA",
            primaryOrganizer: organizerUser,
            pricingTiers: new[] { Money.Create(15m, "USD") }
        ),
        new Event(
            title: "Consent and Communication Workshop",
            description: "Essential workshop on consent, communication, and negotiation in rope bondage.",
            startDate: DateTime.UtcNow.AddDays(10).AddHours(14),
            endDate: DateTime.UtcNow.AddDays(10).AddHours(17),
            capacity: 30,
            eventType: EventType.Workshop,
            location: "The Rope Space, 123 Main St, Salem, MA",
            primaryOrganizer: adminUser,
            pricingTiers: new[] { Money.Create(20m, "USD"), Money.Create(30m, "USD") }
        ),
        new Event(
            title: "Rope Performance Night",
            description: "Watch amazing rope performances from local and visiting artists.",
            startDate: DateTime.UtcNow.AddDays(28).AddHours(20),
            endDate: DateTime.UtcNow.AddDays(28).AddHours(23),
            capacity: 100,
            eventType: EventType.Performance,
            location: "Salem Theater, 789 Broadway, Salem, MA",
            primaryOrganizer: organizerUser,
            pricingTiers: new[] { Money.Create(25m, "USD"), Money.Create(35m, "USD"), Money.Create(50m, "USD") }
        ),
        new Event(
            title: "Virtual Rope Jam",
            description: "Online rope practice session. Great for remote members!",
            startDate: DateTime.UtcNow.AddDays(5).AddHours(20),
            endDate: DateTime.UtcNow.AddDays(5).AddHours(22),
            capacity: 200,
            eventType: EventType.Virtual,
            location: "Online - Zoom link will be sent to registered participants",
            primaryOrganizer: adminUser,
            pricingTiers: new[] { Money.Create(10m, "USD") }
        ),
        new Event(
            title: "Rope Safety and First Aid",
            description: "Critical safety skills for all rope practitioners.",
            startDate: DateTime.UtcNow.AddDays(15).AddHours(13),
            endDate: DateTime.UtcNow.AddDays(15).AddHours(18),
            capacity: 25,
            eventType: EventType.Workshop,
            location: "The Rope Space, 123 Main St, Salem, MA",
            primaryOrganizer: adminUser,
            pricingTiers: new[] { Money.Create(40m, "USD"), Money.Create(50m, "USD") }
        ),
        new Event(
            title: "Members-Only Play Party",
            description: "Vetted members only. Must be pre-approved to attend.",
            startDate: DateTime.UtcNow.AddDays(30).AddHours(21),
            endDate: DateTime.UtcNow.AddDays(31).AddHours(2),
            capacity: 40,
            eventType: EventType.PlayParty,
            location: "Private Venue - Address provided after vetting",
            primaryOrganizer: organizerUser,
            pricingTiers: new[] { Money.Create(30m, "USD") }
        ),
        new Event(
            title: "Rope Photography Workshop",
            description: "Learn to capture beautiful rope art through photography.",
            startDate: DateTime.UtcNow.AddDays(17).AddHours(15),
            endDate: DateTime.UtcNow.AddDays(17).AddHours(19),
            capacity: 15,
            eventType: EventType.Workshop,
            location: "Photo Studio, 321 Art Lane, Salem, MA",
            primaryOrganizer: adminUser,
            pricingTiers: new[] { Money.Create(55m, "USD"), Money.Create(65m, "USD") }
        ),
        new Event(
            title: "WitchCity Rope Conference",
            description: "Our annual conference featuring workshops, performances, and vendors.",
            startDate: DateTime.UtcNow.AddDays(60).AddHours(9),
            endDate: DateTime.UtcNow.AddDays(62).AddHours(18),
            capacity: 300,
            eventType: EventType.Conference,
            location: "Salem Convention Center, 1000 Harbor Blvd, Salem, MA",
            primaryOrganizer: adminUser,
            pricingTiers: new[] { Money.Create(150m, "USD"), Money.Create(200m, "USD"), Money.Create(250m, "USD") }
        )
    };
    
    // Publish most events
    foreach (var evt in events.Take(8))
    {
        evt.Publish();
    }
    
    context.Events.AddRange(events);
    await context.SaveChangesAsync();
    
    Console.WriteLine($"Database seeded successfully!");
    Console.WriteLine($"- {users.Length} users created");
    Console.WriteLine($"- {events.Length} events created");
    Console.WriteLine($"- {users.Length} authentication records created");
    Console.WriteLine("\nTest accounts:");
    Console.WriteLine("- admin@witchcityrope.com / Test123! (Admin, Vetted)");
    Console.WriteLine("- staff@witchcityrope.com / Test123! (Staff, Vetted)");
    Console.WriteLine("- organizer@witchcityrope.com / Test123! (Staff/Organizer, Vetted)");
    Console.WriteLine("- member@witchcityrope.com / Test123! (Member, Not Vetted)");
    Console.WriteLine("- guest@witchcityrope.com / Test123! (Guest/Attendee, Not Vetted)");
}