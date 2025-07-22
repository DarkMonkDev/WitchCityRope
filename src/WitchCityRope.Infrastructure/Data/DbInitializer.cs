using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(WitchCityRopeIdentityDbContext context, ILogger logger)
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

            // Check and seed events if needed
            if (!await context.Events.AnyAsync())
            {
                logger.LogInformation("No events found. Seeding events...");
                
                // Ensure we have the required users for events
                if (!await context.Users.AnyAsync(u => u.Email == "admin@witchcityrope.com") ||
                    !await context.Users.AnyAsync(u => u.Email == "teacher@witchcityrope.com"))
                {
                    logger.LogWarning("Required users for events not found. Seeding users first...");
                    await SeedUsersAsync(context, logger);
                    await context.SaveChangesAsync();
                }
                
                await SeedEventsAsync(context, logger);
                dataSeeded = true;
            }

            // Check and seed registrations/tickets if needed
            // TODO: Registration seeding is disabled because it depends on Event seeding
            /*
            if (!await context.Registrations.AnyAsync())
            {
                logger.LogInformation("No registrations found. Seeding registrations...");
                await SeedRegistrationsAsync(context, logger);
                await context.SaveChangesAsync();
                dataSeeded = true;
            }
            */

            // Check and seed vetting applications if needed
            // TODO: Vetting application seeding is disabled because VettingApplication constructor is incompatible
            /*
            if (!await context.VettingApplications.AnyAsync())
            {
                logger.LogInformation("No vetting applications found. Seeding vetting applications...");
                await SeedVettingApplicationsAsync(context, logger);
                await context.SaveChangesAsync();
                dataSeeded = true;
            }
            */

            if (!dataSeeded)
            {
                logger.LogInformation("Database already contains data. Skipping seeding.");
            }
            else
            {
                logger.LogInformation("Database seeding completed successfully");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while initializing database");
            throw;
        }
    }

    private static async Task SeedUsersAsync(WitchCityRopeIdentityDbContext context, ILogger logger)
    {
        var usersToSeed = new[]
        {
            new { 
                Email = "admin@witchcityrope.com",
                LegalName = "Admin User",
                SceneName = "Admin",
                DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Role = UserRole.Administrator
            },
            new {
                Email = "staff@witchcityrope.com",
                LegalName = "Staff Member",
                SceneName = "StaffMember",
                DateOfBirth = new DateTime(1992, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                Role = UserRole.Moderator
            },
            new {
                Email = "member@witchcityrope.com",
                LegalName = "Regular Member",
                SceneName = "RopeLover",
                DateOfBirth = new DateTime(1995, 8, 20, 0, 0, 0, DateTimeKind.Utc),
                Role = UserRole.Member
            },
            new {
                Email = "guest@witchcityrope.com",
                LegalName = "Guest User",
                SceneName = "CuriousGuest",
                DateOfBirth = new DateTime(2000, 3, 10, 0, 0, 0, DateTimeKind.Utc),
                Role = UserRole.Attendee
            },
            new {
                Email = "organizer@witchcityrope.com",
                LegalName = "Event Organizer",
                SceneName = "EventOrganizer",
                DateOfBirth = new DateTime(1988, 11, 5, 0, 0, 0, DateTimeKind.Utc),
                Role = UserRole.Member  // Event organizers are typically members
            }
        };

        // Create password hasher
        var passwordHasher = new PasswordHasher<WitchCityRopeUser>();
        
        int seededCount = 0;
        foreach (var userToSeed in usersToSeed)
        {
            // Check if user already exists
            var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email == userToSeed.Email);
            if (existingUser != null)
            {
                logger.LogInformation($"User {userToSeed.Email} already exists. Skipping.");
                continue;
            }
            
            // Create WitchCityRopeUser
            var user = new WitchCityRopeUser(
                encryptedLegalName: EncryptLegalName(userToSeed.LegalName),
                sceneName: SceneName.Create(userToSeed.SceneName),
                email: EmailAddress.Create(userToSeed.Email),
                dateOfBirth: userToSeed.DateOfBirth,
                role: userToSeed.Role
            );
            
            // Set additional Identity properties
            user.UserName = userToSeed.Email;
            user.NormalizedUserName = userToSeed.Email.ToUpperInvariant();
            user.Email = userToSeed.Email;
            user.NormalizedEmail = userToSeed.Email.ToUpperInvariant();
            user.EmailConfirmed = true;
            user.PasswordHash = passwordHasher.HashPassword(user, "Test123!");
            user.SecurityStamp = Guid.NewGuid().ToString();
            
            await context.Users.AddAsync(user);
            seededCount++;
        }

        logger.LogInformation($"Seeded {seededCount} new users");
    }

    private static async Task SeedEventsAsync(WitchCityRopeIdentityDbContext context, ILogger logger)
    {
        // Check if events already exist
        if (await context.Events.AnyAsync())
        {
            logger.LogInformation("Events already seeded.");
            return;
        }

        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@witchcityrope.com");
        var organizerUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "organizer@witchcityrope.com");

        if (adminUser == null || organizerUser == null)
        {
            logger.LogWarning($"Required users not found for event seeding. AdminUser: {adminUser?.Email ?? "null"}, OrganizerUser: {organizerUser?.Email ?? "null"}");
            return;
        }

        logger.LogInformation($"Found users - Admin: {adminUser.Email}, Organizer: {organizerUser.Email}");

        var events = new[]
        {
            // Upcoming events
            new Event(
                title: "Introduction to Rope Safety",
                description: "Perfect for beginners! Learn the fundamentals of rope safety, basic knots, and communication techniques in a supportive environment.",
                startDate: DateTime.UtcNow.AddDays(5).AddHours(14),
                endDate: DateTime.UtcNow.AddDays(5).AddHours(17),
                capacity: 20,
                eventType: EventType.Workshop,
                location: "The Dungeon, 123 Main St, Salem, MA 01970",
                primaryOrganizer: adminUser,
                pricingTiers: new[] { Money.Create(35.00m, "USD"), Money.Create(45.00m, "USD"), Money.Create(55.00m, "USD") }
            ),
            new Event(
                title: "Advanced Suspension Techniques",
                description: "For experienced practitioners only. Explore advanced suspension techniques with emphasis on safety and risk assessment.",
                startDate: DateTime.UtcNow.AddDays(10).AddHours(16),
                endDate: DateTime.UtcNow.AddDays(10).AddHours(20),
                capacity: 15,
                eventType: EventType.Workshop,
                location: "The Dungeon, 123 Main St, Salem, MA 01970",
                primaryOrganizer: organizerUser,
                pricingTiers: new[] { Money.Create(65.00m, "USD"), Money.Create(75.00m, "USD"), Money.Create(85.00m, "USD") }
            ),
            new Event(
                title: "Rope Jam Social",
                description: "A casual practice space for all skill levels. Bring your rope and practice with others in a relaxed atmosphere.",
                startDate: DateTime.UtcNow.AddDays(7).AddHours(18),
                endDate: DateTime.UtcNow.AddDays(7).AddHours(22),
                capacity: 40,
                eventType: EventType.Social,
                location: "Community Center, 456 Oak Ave, Salem, MA 01970",
                primaryOrganizer: organizerUser,
                pricingTiers: new[] { Money.Create(20.00m, "USD") }
            ),
            // Past events
            new Event(
                title: "Consent and Communication Workshop",
                description: "Essential workshop covering consent, negotiation, and communication in rope practice.",
                startDate: DateTime.UtcNow.AddDays(-10).AddHours(13),
                endDate: DateTime.UtcNow.AddDays(-10).AddHours(16),
                capacity: 25,
                eventType: EventType.Workshop,
                location: "The Dungeon, 123 Main St, Salem, MA 01970",
                primaryOrganizer: adminUser,
                pricingTiers: new[] { Money.Create(30.00m, "USD"), Money.Create(40.00m, "USD") }
            ),
            new Event(
                title: "Rope Art Photography Session",
                description: "Collaborate with photographers to create beautiful rope art imagery. Models and riggers welcome.",
                startDate: DateTime.UtcNow.AddDays(-5).AddHours(10),
                endDate: DateTime.UtcNow.AddDays(-5).AddHours(14),
                capacity: 10,
                eventType: EventType.Performance,
                location: "Art Studio, 789 Creative Blvd, Salem, MA 01970",
                primaryOrganizer: organizerUser,
                pricingTiers: new[] { Money.Create(50.00m, "USD") }
            )
        };

        try
        {
            foreach (var evt in events)
            {
                // Publish all events
                evt.Publish();
                
                await context.Events.AddAsync(evt);
            }

            await context.SaveChangesAsync();
            logger.LogInformation($"Seeded {events.Length} events successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding events");
            throw;
        }
    }

    // TODO: Registration seeding is disabled because it depends on Event seeding
    /*
    private static async Task SeedRegistrationsAsync(WitchCityRopeIdentityDbContext context, ILogger logger)
    {
        var memberUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "member@witchcityrope.com");
        var guestUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "guest@witchcityrope.com");
        
        if (memberUser == null || guestUser == null)
        {
            logger.LogWarning("Required users not found for registration seeding. Skipping.");
            return;
        }

        var upcomingEvent = await context.Events
            .Where(e => e.StartDate > DateTime.UtcNow)
            .OrderBy(e => e.StartDate)
            .FirstOrDefaultAsync();

        var pastEvent = await context.Events
            .Where(e => e.StartDate < DateTime.UtcNow)
            .OrderByDescending(e => e.StartDate)
            .FirstOrDefaultAsync();

        if (upcomingEvent == null || pastEvent == null)
        {
            logger.LogWarning("Required events not found for registration seeding. Skipping.");
            return;
        }

        // Create registrations
        var registrations = new[]
        {
            // Member registered for upcoming event
            new Registration(upcomingEvent.Id, memberUser.Id),
            // Guest registered for upcoming event
            new Registration(upcomingEvent.Id, guestUser.Id),
            // Member attended past event
            new Registration(pastEvent.Id, memberUser.Id)
        };

        // Mark past event registration as attended
        registrations[2].MarkAsAttended();

        foreach (var registration in registrations)
        {
            await context.Registrations.AddAsync(registration);
        }

        logger.LogInformation($"Seeded {registrations.Length} registrations");
    }
    */

    // Simple encryption for demo purposes
    private static string EncryptLegalName(string legalName)
    {
        // In production, use proper encryption with key management
        var bytes = System.Text.Encoding.UTF8.GetBytes(legalName);
        return Convert.ToBase64String(bytes);
    }

    // TODO: Vetting application seeding is disabled because VettingApplication constructor is incompatible
    /*
    private static async Task SeedVettingApplicationsAsync(WitchCityRopeIdentityDbContext context, ILogger logger)
    {
        // Get some users to create applications for
        var memberUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "member@witchcityrope.com");
        var guestUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "guest@witchcityrope.com");

        if (memberUser == null || guestUser == null)
        {
            logger.LogWarning("Required users not found for vetting application seeding. Skipping.");
            return;
        }

        var vettingApplications = new[]
        {
            // Approved application
            VettingApplication.Create(
                memberUser.Id,
                "I have been practicing rope bondage for 3 years. I learned from workshops at various conferences and have been an active member of the Boston rope community.",
                "RopeLover from Boston Rope Group, KnotMaster from Providence Rope Circle",
                EmailAddress.Create("reference1@email.com"),
                PhoneNumber.Create("617-555-0001")
            ),
            // Pending application
            VettingApplication.Create(
                guestUser.Id,
                "I'm new to rope but very interested in learning. I've attended a few introductory workshops and have been practicing basic knots at home.",
                "TeacherName from Introduction Workshop",
                EmailAddress.Create("teacher@ropeschool.com"),
                PhoneNumber.Create("617-555-0002")
            )
        };

        // Approve the first application
        vettingApplications[0].Approve("Verified references and experience. Welcome to the community!");

        foreach (var app in vettingApplications)
        {
            await context.VettingApplications.AddAsync(app);
        }

        logger.LogInformation($"Seeded {vettingApplications.Length} vetting applications");
    }
    */
}