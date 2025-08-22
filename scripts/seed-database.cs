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
            new SceneName("Admin"),
            new EmailAddress("admin@witchcityrope.com"),
            DateTime.UtcNow.AddYears(-30),
            UserRole.Admin
        ),
        new User(
            await encryptionService.EncryptAsync("Staff Member"),
            new SceneName("Staff"),
            new EmailAddress("staff@witchcityrope.com"),
            DateTime.UtcNow.AddYears(-28),
            UserRole.Staff
        ),
        new User(
            await encryptionService.EncryptAsync("Event Organizer"),
            new SceneName("Organizer"),
            new EmailAddress("organizer@witchcityrope.com"),
            DateTime.UtcNow.AddYears(-35),
            UserRole.Staff
        ),
        new User(
            await encryptionService.EncryptAsync("Regular Member"),
            new SceneName("Member"),
            new EmailAddress("member@witchcityrope.com"),
            DateTime.UtcNow.AddYears(-25),
            UserRole.Member
        ),
        new User(
            await encryptionService.EncryptAsync("Guest User"),
            new SceneName("Guest"),
            new EmailAddress("guest@witchcityrope.com"),
            DateTime.UtcNow.AddYears(-22),
            UserRole.Attendee
        )
    };
    
    // Mark some users as vetted
    users[0].Vet(); // Admin
    users[1].Vet(); // Staff
    users[2].Vet(); // Organizer
    
    context.Users.AddRange(users);
    await context.SaveChangesAsync();
    
    // Create user authentication records
    foreach (var user in users)
    {
        var auth = new UserAuthentication(user.Id, BCrypt.Net.BCrypt.HashPassword("Test123!"));
        context.UserAuthentications.Add(auth);
    }
    await context.SaveChangesAsync();
    
    // Create test events
    var events = new[]
    {
        Event.Create(
            "Introduction to Rope Bondage",
            "Learn the basics of rope bondage in a safe, inclusive environment. Perfect for beginners!",
            DateTime.UtcNow.AddDays(7).AddHours(18),
            DateTime.UtcNow.AddDays(7).AddHours(21),
            20,
            EventType.Workshop,
            "The Rope Space, 123 Main St, Salem, MA",
            new[] { new Money(25m, "USD"), new Money(35m, "USD"), new Money(45m, "USD") }
        ),
        Event.Create(
            "Advanced Suspension Techniques",
            "For experienced practitioners only. Prerequisites required.",
            DateTime.UtcNow.AddDays(14).AddHours(19),
            DateTime.UtcNow.AddDays(14).AddHours(22),
            12,
            EventType.Workshop,
            "The Rope Space, 123 Main St, Salem, MA",
            new[] { new Money(65m, "USD"), new Money(75m, "USD") }
        ),
        Event.Create(
            "Monthly Rope Social",
            "Join us for our monthly rope social. All skill levels welcome!",
            DateTime.UtcNow.AddDays(21).AddHours(19),
            DateTime.UtcNow.AddDays(21).AddHours(23),
            50,
            EventType.Social,
            "Community Center, 456 Oak Ave, Salem, MA",
            new[] { new Money(15m, "USD") }
        ),
        Event.Create(
            "Consent and Communication Workshop",
            "Essential workshop on consent, communication, and negotiation in rope bondage.",
            DateTime.UtcNow.AddDays(10).AddHours(14),
            DateTime.UtcNow.AddDays(10).AddHours(17),
            30,
            EventType.Workshop,
            "The Rope Space, 123 Main St, Salem, MA",
            new[] { new Money(20m, "USD"), new Money(30m, "USD") }
        ),
        Event.Create(
            "Rope Performance Night",
            "Watch amazing rope performances from local and visiting artists.",
            DateTime.UtcNow.AddDays(28).AddHours(20),
            DateTime.UtcNow.AddDays(28).AddHours(23),
            100,
            EventType.Performance,
            "Salem Theater, 789 Broadway, Salem, MA",
            new[] { new Money(25m, "USD"), new Money(35m, "USD"), new Money(50m, "USD") }
        ),
        Event.Create(
            "Virtual Rope Jam",
            "Online rope practice session. Great for remote members!",
            DateTime.UtcNow.AddDays(5).AddHours(20),
            DateTime.UtcNow.AddDays(5).AddHours(22),
            200,
            EventType.Virtual,
            "Online - Zoom link will be sent to registered participants",
            new[] { new Money(10m, "USD") }
        ),
        Event.Create(
            "Rope Safety and First Aid",
            "Critical safety skills for all rope practitioners.",
            DateTime.UtcNow.AddDays(15).AddHours(13),
            DateTime.UtcNow.AddDays(15).AddHours(18),
            25,
            EventType.Workshop,
            "The Rope Space, 123 Main St, Salem, MA",
            new[] { new Money(40m, "USD"), new Money(50m, "USD") }
        ),
        Event.Create(
            "Members-Only Play Party",
            "Vetted members only. Must be pre-approved to attend.",
            DateTime.UtcNow.AddDays(30).AddHours(21),
            DateTime.UtcNow.AddDays(31).AddHours(2),
            40,
            EventType.PlayParty,
            "Private Venue - Address provided after vetting",
            new[] { new Money(30m, "USD") }
        ),
        Event.Create(
            "Rope Photography Workshop",
            "Learn to capture beautiful rope art through photography.",
            DateTime.UtcNow.AddDays(17).AddHours(15),
            DateTime.UtcNow.AddDays(17).AddHours(19),
            15,
            EventType.Workshop,
            "Photo Studio, 321 Art Lane, Salem, MA",
            new[] { new Money(55m, "USD"), new Money(65m, "USD") }
        ),
        Event.Create(
            "WitchCity Rope Conference",
            "Our annual conference featuring workshops, performances, and vendors.",
            DateTime.UtcNow.AddDays(60).AddHours(9),
            DateTime.UtcNow.AddDays(62).AddHours(18),
            300,
            EventType.Conference,
            "Salem Convention Center, 1000 Harbor Blvd, Salem, MA",
            new[] { new Money(150m, "USD"), new Money(200m, "USD"), new Money(250m, "USD") }
        )
    };
    
    // Publish most events
    foreach (var evt in events.Take(8))
    {
        evt.Publish();
    }
    
    // Add organizers to events
    foreach (var evt in events)
    {
        evt.AddOrganizer(users[2]); // Event Organizer
        if (evt.EventType == EventType.Workshop)
        {
            evt.AddOrganizer(users[1]); // Staff member for workshops
        }
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