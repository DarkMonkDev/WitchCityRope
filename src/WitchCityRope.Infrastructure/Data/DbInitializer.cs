using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(WcrDbContext context, ILogger logger)
    {
        try
        {
            // Apply any pending migrations
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");

            // Check if database has been seeded
            if (await context.Users.AnyAsync())
            {
                logger.LogInformation("Database already seeded");
                return;
            }

            // Seed users
            await SeedUsersAsync(context, logger);
            
            // Seed events
            await SeedEventsAsync(context, logger);
            
            await context.SaveChangesAsync();
            logger.LogInformation("Database seeded successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SeedUsersAsync(WcrDbContext context, ILogger logger)
    {
        var users = new[]
        {
            new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@witchcityrope.com",
                SceneName = "Admin",
                LegalName = EncryptLegalName("Admin User"),
                Role = UserRole.Administrator,
                IsEmailVerified = true,
                EmailVerifiedAt = DateTime.UtcNow,
                Status = UserStatus.Active,
                DateOfBirth = new DateTime(1990, 1, 1),
                AgreedToTermsAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "staff@witchcityrope.com",
                SceneName = "StaffMember",
                LegalName = EncryptLegalName("Staff Member"),
                Role = UserRole.Moderator,
                IsEmailVerified = true,
                EmailVerifiedAt = DateTime.UtcNow,
                Status = UserStatus.Active,
                DateOfBirth = new DateTime(1992, 5, 15),
                AgreedToTermsAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "member@witchcityrope.com",
                SceneName = "RopeLover",
                LegalName = EncryptLegalName("Regular Member"),
                Role = UserRole.Member,
                IsEmailVerified = true,
                EmailVerifiedAt = DateTime.UtcNow,
                Status = UserStatus.Active,
                DateOfBirth = new DateTime(1995, 8, 20),
                AgreedToTermsAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                VettedAt = DateTime.UtcNow.AddDays(-30),
                VettedBy = "Admin"
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "guest@witchcityrope.com",
                SceneName = "CuriousGuest",
                LegalName = EncryptLegalName("Guest User"),
                Role = UserRole.Guest,
                IsEmailVerified = true,
                EmailVerifiedAt = DateTime.UtcNow,
                Status = UserStatus.Active,
                DateOfBirth = new DateTime(2000, 3, 10),
                AgreedToTermsAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "organizer@witchcityrope.com",
                SceneName = "EventOrganizer",
                LegalName = EncryptLegalName("Event Organizer"),
                Role = UserRole.Moderator,
                IsEmailVerified = true,
                EmailVerifiedAt = DateTime.UtcNow,
                Status = UserStatus.Active,
                DateOfBirth = new DateTime(1988, 11, 5),
                AgreedToTermsAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }
        };

        // Hash password for all users
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Test123!");
        
        foreach (var user in users)
        {
            await context.Users.AddAsync(user);
            
            // Add authentication record
            var userAuth = new UserAuthentication
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await context.UserAuthentications.AddAsync(userAuth);
        }

        logger.LogInformation($"Seeded {users.Length} users");
    }

    private static async Task SeedEventsAsync(WcrDbContext context, ILogger logger)
    {
        var adminUser = await context.Users.FirstAsync(u => u.Email == "admin@witchcityrope.com");
        var organizerUser = await context.Users.FirstAsync(u => u.Email == "organizer@witchcityrope.com");

        var events = new[]
        {
            // Upcoming events
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Introduction to Rope Safety",
                Description = "Perfect for beginners! Learn the fundamentals of rope safety, basic knots, and communication techniques in a supportive environment.",
                StartDate = DateTime.UtcNow.AddDays(5).AddHours(14),
                EndDate = DateTime.UtcNow.AddDays(5).AddHours(16),
                Location = "The Rope Space - Main Room",
                Address = "123 Essex St, Salem, MA 01970",
                MaxAttendees = 30,
                CurrentAttendees = 12,
                Price = 45.00m,
                PriceTiers = new List<PriceTier>
                {
                    new() { Name = "Full Price", Amount = 45.00m, Description = "Standard ticket price" },
                    new() { Name = "Sliding Scale Mid", Amount = 35.00m, Description = "For those who need financial assistance" },
                    new() { Name = "Sliding Scale Low", Amount = 25.00m, Description = "Pay what you can - no questions asked" }
                },
                Status = EventStatus.Published,
                Type = EventType.Class,
                Category = "Safety",
                Tags = new List<string> { "beginner", "safety", "fundamentals" },
                SkillLevel = SkillLevel.Beginner,
                CreatedBy = adminUser.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "March Rope Jam",
                Description = "Monthly practice space for vetted members. Bring your rope and practice partners for a social evening of rope bondage in a safe, monitored environment.",
                StartDate = DateTime.UtcNow.AddDays(8).AddHours(19),
                EndDate = DateTime.UtcNow.AddDays(8).AddHours(22),
                Location = "The Rope Space - All Rooms",
                Address = "123 Essex St, Salem, MA 01970",
                MaxAttendees = 60,
                CurrentAttendees = 28,
                Price = 15.00m,
                PriceTiers = new List<PriceTier>
                {
                    new() { Name = "Member Price", Amount = 15.00m, Description = "Standard member rate" },
                    new() { Name = "Sliding Scale", Amount = 10.00m, Description = "Pay what you can" },
                    new() { Name = "Community Support", Amount = 5.00m, Description = "For those experiencing financial hardship" }
                },
                RequiresVetting = true,
                Status = EventStatus.Published,
                Type = EventType.Social,
                Category = "Practice",
                Tags = new List<string> { "jam", "practice", "social", "members-only" },
                SkillLevel = SkillLevel.AllLevels,
                CreatedBy = organizerUser.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Suspension Intensive Workshop",
                Description = "Take your skills to new heights! This intensive workshop covers suspension basics, safety protocols, and hands-on practice with experienced instructors.",
                StartDate = DateTime.UtcNow.AddDays(12).AddHours(13),
                EndDate = DateTime.UtcNow.AddDays(12).AddHours(18),
                Location = "The Rope Space - Main Room",
                Address = "123 Essex St, Salem, MA 01970",
                MaxAttendees = 20,
                CurrentAttendees = 18,
                Price = 95.00m,
                PriceTiers = new List<PriceTier>
                {
                    new() { Name = "Full Price", Amount = 95.00m, Description = "Standard workshop rate" },
                    new() { Name = "Early Bird", Amount = 85.00m, Description = "Register 2 weeks in advance" },
                    new() { Name = "Sliding Scale", Amount = 75.00m, Description = "Financial assistance available" }
                },
                Prerequisites = "Must have attended at least 3 previous classes or have equivalent experience",
                Status = EventStatus.Published,
                Type = EventType.Workshop,
                Category = "Advanced",
                Tags = new List<string> { "suspension", "advanced", "intensive" },
                SkillLevel = SkillLevel.Advanced,
                CreatedBy = adminUser.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Rope and Sensation Play",
                Description = "Explore the intersection of rope bondage and sensation play. Learn how to incorporate different sensations safely into your rope practice.",
                StartDate = DateTime.UtcNow.AddDays(15).AddHours(14),
                EndDate = DateTime.UtcNow.AddDays(15).AddHours(17),
                Location = "The Rope Space - Lounge",
                Address = "123 Essex St, Salem, MA 01970",
                MaxAttendees = 24,
                CurrentAttendees = 10,
                Price = 55.00m,
                PriceTiers = new List<PriceTier>
                {
                    new() { Name = "Full Price", Amount = 55.00m, Description = "Standard class rate" },
                    new() { Name = "Sliding Scale", Amount = 40.00m, Description = "Pay what you can" }
                },
                Status = EventStatus.Published,
                Type = EventType.Class,
                Category = "Specialty",
                Tags = new List<string> { "sensation", "intermediate", "specialty" },
                SkillLevel = SkillLevel.Intermediate,
                CreatedBy = organizerUser.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Rope Fundamentals: Floor Work",
                Description = "Master the basics of floor-based rope bondage. Perfect for beginners or those wanting to refine their fundamental skills.",
                StartDate = DateTime.UtcNow.AddDays(18).AddHours(14),
                EndDate = DateTime.UtcNow.AddDays(18).AddHours(16),
                Location = "The Rope Space - Main Room",
                Address = "123 Essex St, Salem, MA 01970",
                MaxAttendees = 30,
                CurrentAttendees = 8,
                Price = 45.00m,
                PriceTiers = new List<PriceTier>
                {
                    new() { Name = "Full Price", Amount = 45.00m, Description = "Standard ticket price" },
                    new() { Name = "Sliding Scale Mid", Amount = 35.00m, Description = "For those who need financial assistance" },
                    new() { Name = "Sliding Scale Low", Amount = 25.00m, Description = "Pay what you can - no questions asked" }
                },
                Status = EventStatus.Published,
                Type = EventType.Class,
                Category = "Fundamentals",
                Tags = new List<string> { "beginner", "floor work", "fundamentals" },
                SkillLevel = SkillLevel.Beginner,
                CreatedBy = adminUser.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Dynamic Suspension: Movement and Flow",
                Description = "Advanced workshop focusing on dynamic suspension techniques. Learn to create beautiful, flowing transitions in your suspension work.",
                StartDate = DateTime.UtcNow.AddDays(22).AddHours(13),
                EndDate = DateTime.UtcNow.AddDays(22).AddHours(18),
                Location = "The Rope Space - Main Room",
                Address = "123 Essex St, Salem, MA 01970",
                MaxAttendees = 16,
                CurrentAttendees = 14,
                Price = 125.00m,
                PriceTiers = new List<PriceTier>
                {
                    new() { Name = "Full Price", Amount = 125.00m, Description = "Standard workshop rate" },
                    new() { Name = "Early Bird", Amount = 110.00m, Description = "Register 2 weeks in advance" }
                },
                Prerequisites = "Must be comfortable with basic suspension. Vetting required.",
                RequiresVetting = true,
                Status = EventStatus.Published,
                Type = EventType.Workshop,
                Category = "Advanced",
                Tags = new List<string> { "suspension", "dynamic", "advanced", "movement" },
                SkillLevel = SkillLevel.Advanced,
                CreatedBy = organizerUser.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-25)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Monthly Rope Social",
                Description = "Casual social gathering for all skill levels. Practice, learn from others, or just hang out with the rope community. Light refreshments provided.",
                StartDate = DateTime.UtcNow.AddDays(25).AddHours(18),
                EndDate = DateTime.UtcNow.AddDays(25).AddHours(21),
                Location = "The Rope Space - All Rooms",
                Address = "123 Essex St, Salem, MA 01970",
                MaxAttendees = 80,
                CurrentAttendees = 35,
                Price = 0.00m,
                IsFree = true,
                Status = EventStatus.Published,
                Type = EventType.Social,
                Category = "Community",
                Tags = new List<string> { "social", "community", "free", "all-levels" },
                SkillLevel = SkillLevel.AllLevels,
                CreatedBy = adminUser.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Rope Play Party",
                Description = "Monthly play party for vetted members. Dungeon monitors on duty. BYOB and snacks to share. Must be 21+.",
                StartDate = DateTime.UtcNow.AddDays(28).AddHours(20),
                EndDate = DateTime.UtcNow.AddDays(29).AddHours(2),
                Location = "Private Venue (address provided after RSVP)",
                Address = "Salem, MA",
                MaxAttendees = 50,
                CurrentAttendees = 32,
                Price = 25.00m,
                PriceTiers = new List<PriceTier>
                {
                    new() { Name = "Member Price", Amount = 25.00m, Description = "Standard member rate" },
                    new() { Name = "Sliding Scale", Amount = 15.00m, Description = "Pay what you can" }
                },
                RequiresVetting = true,
                Status = EventStatus.Published,
                Type = EventType.PlayParty,
                Category = "Social",
                Tags = new List<string> { "play party", "vetted", "21+", "byob" },
                SkillLevel = SkillLevel.AllLevels,
                CreatedBy = organizerUser.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Virtual Rope Workshop: Self-Tying",
                Description = "Learn the art of self-tying from the comfort of your home. This online workshop covers basic self-bondage techniques and safety.",
                StartDate = DateTime.UtcNow.AddDays(30).AddHours(19),
                EndDate = DateTime.UtcNow.AddDays(30).AddHours(21),
                Location = "Online - Zoom",
                Address = "Virtual Event",
                MaxAttendees = 100,
                CurrentAttendees = 45,
                Price = 25.00m,
                PriceTiers = new List<PriceTier>
                {
                    new() { Name = "Standard Price", Amount = 25.00m, Description = "Access to live workshop and recording" },
                    new() { Name = "Sliding Scale", Amount = 15.00m, Description = "Pay what you can" }
                },
                IsVirtual = true,
                Status = EventStatus.Published,
                Type = EventType.Virtual,
                Category = "Workshop",
                Tags = new List<string> { "virtual", "self-tying", "online", "beginner-friendly" },
                SkillLevel = SkillLevel.Beginner,
                CreatedBy = adminUser.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-8)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "New England Rope Intensive",
                Description = "3-day intensive rope bondage conference featuring workshops, performances, and vendor market. Guest instructors from around the world.",
                StartDate = DateTime.UtcNow.AddDays(45).AddHours(17),
                EndDate = DateTime.UtcNow.AddDays(48).AddHours(14),
                Location = "Salem Convention Center",
                Address = "1 Salem Green, Salem, MA 01970",
                MaxAttendees = 200,
                CurrentAttendees = 156,
                Price = 250.00m,
                PriceTiers = new List<PriceTier>
                {
                    new() { Name = "Full Weekend Pass", Amount = 250.00m, Description = "Access to all workshops and events" },
                    new() { Name = "Early Bird Special", Amount = 200.00m, Description = "Limited time offer" },
                    new() { Name = "Single Day Pass", Amount = 100.00m, Description = "Access for one day only" }
                },
                Status = EventStatus.Published,
                Type = EventType.Conference,
                Category = "Conference",
                Tags = new List<string> { "conference", "intensive", "multi-day", "vendors" },
                SkillLevel = SkillLevel.AllLevels,
                CreatedBy = adminUser.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-60)
            },
            
            // Past events for testing
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "February Rope Jam",
                Description = "Monthly practice space for vetted members.",
                StartDate = DateTime.UtcNow.AddDays(-10).AddHours(19),
                EndDate = DateTime.UtcNow.AddDays(-10).AddHours(22),
                Location = "The Rope Space - All Rooms",
                Address = "123 Essex St, Salem, MA 01970",
                MaxAttendees = 60,
                CurrentAttendees = 52,
                Price = 15.00m,
                RequiresVetting = true,
                Status = EventStatus.Completed,
                Type = EventType.Social,
                Category = "Practice",
                Tags = new List<string> { "jam", "practice", "social", "members-only" },
                SkillLevel = SkillLevel.AllLevels,
                CreatedBy = organizerUser.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-40)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Valentine's Rope Workshop",
                Description = "Special couples workshop for Valentine's Day. Learn intimate rope techniques in a romantic setting.",
                StartDate = DateTime.UtcNow.AddDays(-15).AddHours(18),
                EndDate = DateTime.UtcNow.AddDays(-15).AddHours(21),
                Location = "The Rope Space - Lounge",
                Address = "123 Essex St, Salem, MA 01970",
                MaxAttendees = 20,
                CurrentAttendees = 20,
                Price = 75.00m,
                Status = EventStatus.Completed,
                Type = EventType.Workshop,
                Category = "Specialty",
                Tags = new List<string> { "couples", "valentine", "intimate", "special-event" },
                SkillLevel = SkillLevel.Intermediate,
                CreatedBy = adminUser.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-45)
            }
        };

        await context.Events.AddRangeAsync(events);
        logger.LogInformation($"Seeded {events.Length} events");
    }

    private static string EncryptLegalName(string legalName)
    {
        // Simple encryption for demo - in production use proper AES encryption
        var bytes = System.Text.Encoding.UTF8.GetBytes(legalName);
        return Convert.ToBase64String(bytes);
    }
}