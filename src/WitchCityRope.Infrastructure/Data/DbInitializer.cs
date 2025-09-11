using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(WitchCityRopeDbContext context, ILogger logger)
    {
        try
        {
            // Apply any pending migrations
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");

            bool dataSeeded = false;

            // Check and seed users if needed
            if (!await context.Users.AnyAsync())
            {
                logger.LogInformation("No users found. Seeding users...");
                await SeedUsersAsync(context, logger);
                await context.SaveChangesAsync();
                dataSeeded = true;
            }
            else
            {
                logger.LogInformation($"Found {await context.Users.CountAsync()} existing users");
            }

            // Check and seed events if needed
            if (!await context.Events.AnyAsync())
            {
                logger.LogInformation("No events found. Seeding events...");
                
                // Ensure we have the required users for events
                if (!await context.Users.AnyAsync(u => u.Email.Value == "admin@witchcityrope.com") ||
                    !await context.Users.AnyAsync(u => u.Email.Value == "organizer@witchcityrope.com"))
                {
                    logger.LogWarning("Required users for events not found. Seeding users first...");
                    await SeedUsersAsync(context, logger);
                    await context.SaveChangesAsync();
                }
                
                await SeedEventsAsync(context, logger);
                await context.SaveChangesAsync();
                dataSeeded = true;
            }
            else
            {
                logger.LogInformation($"Found {await context.Events.CountAsync()} existing events");
            }

            // Check and seed vetting applications if needed
            if (!await context.VettingApplications.AnyAsync())
            {
                logger.LogInformation("No vetting applications found. Seeding sample applications...");
                await SeedVettingApplicationsAsync(context, logger);
                await context.SaveChangesAsync();
                dataSeeded = true;
            }

            if (dataSeeded)
            {
                logger.LogInformation("Database seeding completed successfully");
            }
            else
            {
                logger.LogInformation("Database already contains data. No seeding required.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SeedUsersAsync(WitchCityRopeDbContext context, ILogger logger)
    {
        var usersToSeed = new[]
        {
            new { 
                Email = "admin@witchcityrope.com",
                User = new User(
                    encryptedLegalName: EncryptLegalName("Admin User"),
                    sceneName: SceneName.Create("Admin"),
                    email: EmailAddress.Create("admin@witchcityrope.com"),
                    dateOfBirth: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    role: UserRole.Administrator
                )
            },
            new {
                Email = "staff@witchcityrope.com",
                User = new User(
                    encryptedLegalName: EncryptLegalName("Staff Member"),
                    sceneName: SceneName.Create("StaffMember"),
                    email: EmailAddress.Create("staff@witchcityrope.com"),
                    dateOfBirth: new DateTime(1992, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                    role: UserRole.Moderator
                )
            },
            new {
                Email = "member@witchcityrope.com",
                User = new User(
                    encryptedLegalName: EncryptLegalName("Regular Member"),
                    sceneName: SceneName.Create("RopeLover"),
                    email: EmailAddress.Create("member@witchcityrope.com"),
                    dateOfBirth: new DateTime(1995, 8, 20, 0, 0, 0, DateTimeKind.Utc),
                    role: UserRole.Member
                )
            },
            new {
                Email = "guest@witchcityrope.com",
                User = new User(
                    encryptedLegalName: EncryptLegalName("Guest User"),
                    sceneName: SceneName.Create("CuriousGuest"),
                    email: EmailAddress.Create("guest@witchcityrope.com"),
                    dateOfBirth: new DateTime(2000, 3, 10, 0, 0, 0, DateTimeKind.Utc),
                    role: UserRole.Attendee
                )
            },
            new {
                Email = "organizer@witchcityrope.com",
                User = new User(
                    encryptedLegalName: EncryptLegalName("Event Organizer"),
                    sceneName: SceneName.Create("EventOrganizer"),
                    email: EmailAddress.Create("organizer@witchcityrope.com"),
                    dateOfBirth: new DateTime(1988, 11, 5, 0, 0, 0, DateTimeKind.Utc),
                    role: UserRole.Moderator
                )
            }
        };

        // Hash password for all users
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Test123!");
        
        int seededCount = 0;
        foreach (var userToSeed in usersToSeed)
        {
            // Check if user already exists
            var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email.Value == userToSeed.Email);
            if (existingUser != null)
            {
                logger.LogInformation($"User {userToSeed.Email} already exists. Skipping.");
                
                // Check if authentication record exists
                var existingAuth = await context.UserAuthentications.FirstOrDefaultAsync(a => a.UserId == existingUser.Id);
                if (existingAuth == null)
                {
                    logger.LogInformation($"Adding missing authentication record for {userToSeed.Email}");
                    var userAuth = new UserAuthentication
                    {
                        Id = Guid.NewGuid(),
                        UserId = existingUser.Id,
                        PasswordHash = passwordHash,
                        IsTwoFactorEnabled = false,
                        FailedLoginAttempts = 0
                    };
                    await context.UserAuthentications.AddAsync(userAuth);
                }
                continue;
            }
            
            await context.Users.AddAsync(userToSeed.User);
            
            // Add authentication record
            var auth = new UserAuthentication
            {
                Id = Guid.NewGuid(),
                UserId = userToSeed.User.Id,
                PasswordHash = passwordHash,
                IsTwoFactorEnabled = false,
                FailedLoginAttempts = 0
            };
            await context.UserAuthentications.AddAsync(auth);
            seededCount++;
        }

        logger.LogInformation($"Seeded {seededCount} new users");
    }

    private static async Task SeedEventsAsync(WitchCityRopeDbContext context, ILogger logger)
    {
        var adminUser = await context.Users.FirstAsync(u => u.Email.Value == "admin@witchcityrope.com");
        var organizerUser = await context.Users.FirstAsync(u => u.Email.Value == "organizer@witchcityrope.com");

        var events = new[]
        {
            // Upcoming events
            new Event(
                title: "Introduction to Rope Safety",
                description: "Perfect for beginners! Learn the fundamentals of rope safety, basic knots, and communication techniques in a supportive environment.",
                startDate: DateTime.UtcNow.AddDays(5).AddHours(14),
                endDate: DateTime.UtcNow.AddDays(5).AddHours(16),
                capacity: 30,
                eventType: EventType.Class,
                location: "The Rope Space - Main Room",
                primaryOrganizer: adminUser,
                pricingTiers: new[]
                {
                    Money.Create(45.00m, "USD"),
                    Money.Create(35.00m, "USD"),
                    Money.Create(25.00m, "USD")
                }
            ),
            new Event(
                title: "March Rope Jam",
                description: "Monthly practice space for vetted members. Bring your rope and practice partners for a social evening of rope bondage in a safe, monitored environment.",
                startDate: DateTime.UtcNow.AddDays(8).AddHours(19),
                endDate: DateTime.UtcNow.AddDays(8).AddHours(22),
                capacity: 60,
                eventType: EventType.Social,
                location: "The Rope Space - All Rooms",
                primaryOrganizer: organizerUser,
                pricingTiers: new[]
                {
                    Money.Create(15.00m, "USD"),
                    Money.Create(10.00m, "USD"),
                    Money.Create(5.00m, "USD")
                }
            ),
            new Event(
                title: "Suspension Intensive Workshop",
                description: "Take your skills to new heights! This intensive workshop covers suspension basics, safety protocols, and hands-on practice with experienced instructors.",
                startDate: DateTime.UtcNow.AddDays(12).AddHours(13),
                endDate: DateTime.UtcNow.AddDays(12).AddHours(18),
                capacity: 20,
                eventType: EventType.Class,
                location: "The Rope Space - Main Room",
                primaryOrganizer: adminUser,
                pricingTiers: new[]
                {
                    Money.Create(95.00m, "USD"),
                    Money.Create(85.00m, "USD"),
                    Money.Create(75.00m, "USD")
                }
            ),
            new Event(
                title: "Rope and Sensation Play",
                description: "Explore the intersection of rope bondage and sensation play. Learn how to incorporate different sensations safely into your rope practice.",
                startDate: DateTime.UtcNow.AddDays(15).AddHours(14),
                endDate: DateTime.UtcNow.AddDays(15).AddHours(17),
                capacity: 24,
                eventType: EventType.Class,
                location: "The Rope Space - Lounge",
                primaryOrganizer: organizerUser,
                pricingTiers: new[]
                {
                    Money.Create(55.00m, "USD"),
                    Money.Create(40.00m, "USD")
                }
            ),
            new Event(
                title: "Rope Fundamentals: Floor Work",
                description: "Master the basics of floor-based rope bondage. Perfect for beginners or those wanting to refine their fundamental skills.",
                startDate: DateTime.UtcNow.AddDays(18).AddHours(14),
                endDate: DateTime.UtcNow.AddDays(18).AddHours(16),
                capacity: 30,
                eventType: EventType.Class,
                location: "The Rope Space - Main Room",
                primaryOrganizer: adminUser,
                pricingTiers: new[]
                {
                    Money.Create(45.00m, "USD"),
                    Money.Create(35.00m, "USD"),
                    Money.Create(25.00m, "USD")
                }
            ),
            new Event(
                title: "Dynamic Suspension: Movement and Flow",
                description: "Advanced workshop focusing on dynamic suspension techniques. Learn to create beautiful, flowing transitions in your suspension work.",
                startDate: DateTime.UtcNow.AddDays(22).AddHours(13),
                endDate: DateTime.UtcNow.AddDays(22).AddHours(18),
                capacity: 16,
                eventType: EventType.Class,
                location: "The Rope Space - Main Room",
                primaryOrganizer: organizerUser,
                pricingTiers: new[]
                {
                    Money.Create(125.00m, "USD"),
                    Money.Create(110.00m, "USD")
                }
            ),
            new Event(
                title: "Monthly Rope Social",
                description: "Casual social gathering for all skill levels. Practice, learn from others, or just hang out with the rope community. Light refreshments provided.",
                startDate: DateTime.UtcNow.AddDays(25).AddHours(18),
                endDate: DateTime.UtcNow.AddDays(25).AddHours(21),
                capacity: 80,
                eventType: EventType.Social,
                location: "The Rope Space - All Rooms",
                primaryOrganizer: adminUser,
                pricingTiers: new[]
                {
                    Money.Create(0.00m, "USD")
                }
            ),
            new Event(
                title: "Rope Play Party",
                description: "Monthly play party for vetted members. Dungeon monitors on duty. BYOB and snacks to share. Must be 21+.",
                startDate: DateTime.UtcNow.AddDays(28).AddHours(20),
                endDate: DateTime.UtcNow.AddDays(29).AddHours(2),
                capacity: 50,
                eventType: EventType.Social,
                location: "Private Venue (address provided after RSVP)",
                primaryOrganizer: organizerUser,
                pricingTiers: new[]
                {
                    Money.Create(25.00m, "USD"),
                    Money.Create(15.00m, "USD")
                }
            ),
            new Event(
                title: "Virtual Rope Workshop: Self-Tying",
                description: "Learn the art of self-tying from the comfort of your home. This online workshop covers basic self-bondage techniques and safety.",
                startDate: DateTime.UtcNow.AddDays(30).AddHours(19),
                endDate: DateTime.UtcNow.AddDays(30).AddHours(21),
                capacity: 100,
                eventType: EventType.Class,
                location: "Online - Zoom",
                primaryOrganizer: adminUser,
                pricingTiers: new[]
                {
                    Money.Create(25.00m, "USD"),
                    Money.Create(15.00m, "USD")
                }
            ),
            new Event(
                title: "New England Rope Intensive",
                description: "3-day intensive rope bondage conference featuring workshops, performances, and vendor market. Guest instructors from around the world.",
                startDate: DateTime.UtcNow.AddDays(45).AddHours(17),
                endDate: DateTime.UtcNow.AddDays(48).AddHours(14),
                capacity: 200,
                eventType: EventType.Class,
                location: "Salem Convention Center",
                primaryOrganizer: adminUser,
                pricingTiers: new[]
                {
                    Money.Create(250.00m, "USD"),
                    Money.Create(200.00m, "USD"),
                    Money.Create(100.00m, "USD")
                }
            )
        };
        
        // Publish all events
        foreach (var @event in events)
        {
            @event.Publish();
        }

        await context.Events.AddRangeAsync(events);
        logger.LogInformation($"Seeded {events.Length} events");
    }

    private static string EncryptLegalName(string legalName)
    {
        // Simple encryption for demo - in production use proper AES encryption
        var bytes = System.Text.Encoding.UTF8.GetBytes(legalName);
        return Convert.ToBase64String(bytes);
    }

    private static async Task SeedVettingApplicationsAsync(WitchCityRopeDbContext context, ILogger logger)
    {
        // Get some users to create applications for
        var memberUser = await context.Users.FirstOrDefaultAsync(u => u.Email.Value == "member@witchcityrope.com");
        var guestUser = await context.Users.FirstOrDefaultAsync(u => u.Email.Value == "guest@witchcityrope.com");

        if (memberUser == null || guestUser == null)
        {
            logger.LogWarning("Required users for vetting applications not found. Skipping vetting application seeding.");
            return;
        }

        var applications = new[]
        {
            new VettingApplication(
                applicant: memberUser,
                experienceLevel: "Intermediate",
                experienceDescription: "2 years experience in rope bondage, attended workshops in Boston",
                interests: "Suspension, decorative rope, rope performance",
                safetyKnowledge: "RACK/SSC understanding, nerve safety, circulation monitoring",
                consentUnderstanding: "Enthusiastic, informed, ongoing consent with regular check-ins",
                whyJoin: "Looking to expand my skills and connect with the local rope community",
                references: new[] { "RiggerJohn@example.com", "BunnyAlice@example.com" }
            ),
            new VettingApplication(
                applicant: guestUser,
                experienceLevel: "Beginner",
                experienceDescription: "New to rope but experienced in other kink activities",
                interests: "Learning fundamentals, floor work, self-tying",
                safetyKnowledge: "Basic understanding from online resources and books",
                consentUnderstanding: "Clear negotiation, respecting limits, ongoing communication",
                whyJoin: "Want to learn rope safely from experienced practitioners",
                references: new[] { "LocalDungeonOwner@example.com" }
            )
        };

        await context.VettingApplications.AddRangeAsync(applications);
        logger.LogInformation($"Seeded {applications.Length} vetting applications");
    }
}