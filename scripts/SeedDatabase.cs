// PostgreSQL Database Seeder for WitchCityRope
// This can be run using dotnet-script or integrated into the application
// Usage: dotnet script SeedDatabase.cs

#r "nuget: Npgsql, 8.0.0"
#r "nuget: BCrypt.Net-Next, 4.0.3"

using System;
using System.Threading.Tasks;
using Npgsql;
using BCrypt.Net;

var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
    ?? "Host=localhost;Port=5432;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!";

// Test user password
var testPassword = "Test123!";
var passwordHash = BCrypt.Net.BCrypt.HashPassword(testPassword);

// User IDs
var adminId = Guid.Parse("a1111111-1111-1111-1111-111111111111");
var teacherId = Guid.Parse("b2222222-2222-2222-2222-222222222222");
var vettedMemberId = Guid.Parse("c3333333-3333-3333-3333-333333333333");
var memberId = Guid.Parse("d4444444-4444-4444-4444-444444444444");
var aliceId = Guid.Parse("e5555555-5555-5555-5555-555555555555");
var bobId = Guid.Parse("f6666666-6666-6666-6666-666666666666");
var charlieId = Guid.Parse("a7777777-7777-7777-7777-777777777777");

// Event IDs
var workshopId = Guid.Parse("e1111111-1111-1111-1111-111111111111");
var performanceId = Guid.Parse("e2222222-2222-2222-2222-222222222222");
var socialId = Guid.Parse("e3333333-3333-3333-3333-333333333333");
var advancedWorkshopId = Guid.Parse("e4444444-4444-4444-4444-444444444444");
var pastEventId = Guid.Parse("e5555555-5555-5555-5555-555555555555");

try
{
    using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    
    Console.WriteLine("Connected to PostgreSQL database...");
    
    // Begin transaction
    using var transaction = await conn.BeginTransactionAsync();
    
    try
    {
        // Clear existing data
        Console.WriteLine("Clearing existing data...");
        await ExecuteNonQuery(conn, "DELETE FROM \"RefreshTokens\"");
        await ExecuteNonQuery(conn, "DELETE FROM \"IncidentActions\"");
        await ExecuteNonQuery(conn, "DELETE FROM \"IncidentReviews\"");
        await ExecuteNonQuery(conn, "DELETE FROM \"IncidentReports\"");
        await ExecuteNonQuery(conn, "DELETE FROM \"Payments\"");
        await ExecuteNonQuery(conn, "DELETE FROM \"Registrations\"");
        await ExecuteNonQuery(conn, "DELETE FROM \"EventOrganizers\"");
        await ExecuteNonQuery(conn, "DELETE FROM \"VettingApplications\"");
        await ExecuteNonQuery(conn, "DELETE FROM \"UserAuthentications\"");
        await ExecuteNonQuery(conn, "DELETE FROM \"Events\"");
        await ExecuteNonQuery(conn, "DELETE FROM \"Users\"");
        
        // Seed Users
        Console.WriteLine("Seeding users...");
        await SeedUser(conn, adminId, "encrypted_admin_legal_name", "Rope Master Admin", "admin@witchcityrope.com", 
            new DateTime(1990, 1, 1), "Admin", true, "they/them", true);
        
        await SeedUser(conn, teacherId, "encrypted_teacher_legal_name", "Mistress Knots", "teacher@witchcityrope.com", 
            new DateTime(1985, 5, 15), "Teacher", true, "she/her", true);
        
        await SeedUser(conn, vettedMemberId, "encrypted_vetted_legal_name", "Salem Bound", "vetted@witchcityrope.com", 
            new DateTime(1992, 8, 20), "VettedMember", true, "he/him", true);
        
        await SeedUser(conn, memberId, "encrypted_member_legal_name", "Rope Curious", "member@witchcityrope.com", 
            new DateTime(1995, 12, 10), "Member", false, "she/they", false);
        
        await SeedUser(conn, aliceId, "encrypted_alice_legal_name", "Witch City Alice", "alice@example.com", 
            new DateTime(1988, 3, 25), "VettedMember", true, "she/her", true);
        
        await SeedUser(conn, bobId, "encrypted_bob_legal_name", "Bondage Bob", "bob@example.com", 
            new DateTime(1993, 7, 14), "Member", false, "he/him", false);
        
        await SeedUser(conn, charlieId, "encrypted_charlie_legal_name", "Charlie Chains", "charlie@example.com", 
            new DateTime(1991, 11, 30), "Attendee", false, "they/them", false);
        
        // Seed UserAuthentications
        Console.WriteLine("Seeding user authentications...");
        await SeedUserAuth(conn, Guid.NewGuid(), adminId, passwordHash);
        await SeedUserAuth(conn, Guid.NewGuid(), teacherId, passwordHash);
        await SeedUserAuth(conn, Guid.NewGuid(), vettedMemberId, passwordHash);
        await SeedUserAuth(conn, Guid.NewGuid(), memberId, passwordHash);
        await SeedUserAuth(conn, Guid.NewGuid(), aliceId, passwordHash);
        await SeedUserAuth(conn, Guid.NewGuid(), bobId, passwordHash);
        await SeedUserAuth(conn, Guid.NewGuid(), charlieId, passwordHash);
        
        // Seed Events
        Console.WriteLine("Seeding events...");
        await SeedEvent(conn, workshopId, "Introduction to Rope Bondage", 
            "Learn the basics of rope bondage in a safe, inclusive environment. This workshop covers fundamental ties, safety protocols, and consent practices.",
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(7).AddHours(3), 20, "Workshop", 
            "WitchCity Community Center, Salem MA", true,
            new[] { 50m, 75m, 100m });
        
        await SeedEvent(conn, performanceId, "Midnight Rope Performance", 
            "An artistic rope performance showcasing the beauty and artistry of rope bondage. Featured performers from around New England.",
            DateTime.UtcNow.AddDays(14), DateTime.UtcNow.AddDays(14).AddHours(2), 100, "Performance", 
            "Salem Arts Theatre", true,
            new[] { 25m, 35m, 50m });
        
        await SeedEvent(conn, socialId, "Monthly Rope Social", 
            "Our regular monthly social gathering for rope enthusiasts. Practice your skills, meet new people, and enjoy our community.",
            DateTime.UtcNow.AddDays(21), DateTime.UtcNow.AddDays(21).AddHours(4), 50, "Social", 
            "WitchCity Dungeon Space", true,
            new[] { 15m, 20m, 30m });
        
        await SeedEvent(conn, advancedWorkshopId, "Advanced Suspension Techniques", 
            "For experienced riggers only. Learn advanced suspension techniques with a focus on safety and dynamic movement.",
            DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4), 12, "Workshop", 
            "Private Studio, Salem MA", true,
            new[] { 100m, 125m, 150m });
        
        await SeedEvent(conn, pastEventId, "Halloween Rope Party", 
            "Special Halloween-themed rope party with costumes encouraged!",
            DateTime.UtcNow.AddDays(-60), DateTime.UtcNow.AddDays(-60).AddHours(5), 75, "Social", 
            "WitchCity Dungeon Space", true,
            new[] { 20m, 30m, 40m });
        
        // Seed EventOrganizers
        Console.WriteLine("Seeding event organizers...");
        await SeedEventOrganizer(conn, workshopId, teacherId);
        await SeedEventOrganizer(conn, performanceId, adminId);
        await SeedEventOrganizer(conn, performanceId, teacherId);
        await SeedEventOrganizer(conn, socialId, vettedMemberId);
        await SeedEventOrganizer(conn, advancedWorkshopId, teacherId);
        await SeedEventOrganizer(conn, pastEventId, adminId);
        
        // Seed VettingApplications
        Console.WriteLine("Seeding vetting applications...");
        await SeedVettingApplication(conn, Guid.Parse("v1111111-1111-1111-1111-111111111111"), memberId,
            "Beginner - I have attended a few workshops",
            "I am interested in learning both topping and bottoming, with a focus on floor work initially.",
            "I understand the importance of checking circulation, nerve function, and having safety scissors nearby.",
            "I have attended 3 workshops at other venues and have been practicing basic column ties and chest harnesses.",
            "Consent is ongoing and can be revoked at any time. Clear communication and regular check-ins are essential.",
            "I want to join to be part of a supportive community where I can learn and grow in my rope journey safely.",
            "Pending", DateTime.UtcNow.AddDays(-3),
            "[{\"Name\": \"Rope Mentor Mike\", \"Contact\": \"mike@example.com\"}, {\"Name\": \"Workshop Leader Lisa\", \"Contact\": \"lisa@example.com\"}]");
        
        await SeedVettingApplication(conn, Guid.Parse("v2222222-2222-2222-2222-222222222222"), bobId,
            "Intermediate - I have been practicing for 2 years",
            "Primarily interested in artistic rope and photography. Some interest in rope suspension.",
            "I know about nerve pathways, proper placement to avoid injury, and emergency release techniques.",
            "Two years of regular practice, including several intensives. Comfortable with TKs and hip harnesses.",
            "Enthusiastic consent is key. I believe in negotiation before play and continuous communication during.",
            "Looking for a community that values safety and artistic expression. Want to contribute my photography skills.",
            "Pending", DateTime.UtcNow.AddDays(-1),
            "[{\"Name\": \"Previous Partner Pat\", \"Contact\": \"pat@example.com\"}]");
        
        await transaction.CommitAsync();
        
        Console.WriteLine("\nDatabase seeded successfully!");
        
        // Display summary
        Console.WriteLine("\nSummary:");
        Console.WriteLine($"Users created: {await GetCount(conn, "Users")}");
        Console.WriteLine($"Events created: {await GetCount(conn, "Events")}");
        Console.WriteLine($"User Authentications created: {await GetCount(conn, "UserAuthentications")}");
        Console.WriteLine($"Event Organizers created: {await GetCount(conn, "EventOrganizers")}");
        Console.WriteLine($"Vetting Applications created: {await GetCount(conn, "VettingApplications")}");
        
        Console.WriteLine("\nTest Accounts:");
        Console.WriteLine("Admin: admin@witchcityrope.com / Test123!");
        Console.WriteLine("Teacher: teacher@witchcityrope.com / Test123!");
        Console.WriteLine("Vetted Member: vetted@witchcityrope.com / Test123!");
        Console.WriteLine("General Member: member@witchcityrope.com / Test123!");
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        throw new Exception($"Error during seeding: {ex.Message}", ex);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}

async Task ExecuteNonQuery(NpgsqlConnection conn, string sql)
{
    using var cmd = new NpgsqlCommand(sql, conn);
    await cmd.ExecuteNonQueryAsync();
}

async Task<int> GetCount(NpgsqlConnection conn, string tableName)
{
    using var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM \"{tableName}\"", conn);
    return Convert.ToInt32(await cmd.ExecuteScalarAsync());
}

async Task SeedUser(NpgsqlConnection conn, Guid id, string encryptedLegalName, string sceneName, 
    string email, DateTime dateOfBirth, string role, bool isActive, string pronouns, bool isVetted)
{
    var sql = @"INSERT INTO ""Users"" 
        (""Id"", ""EncryptedLegalName"", ""SceneName"", ""Email"", ""DateOfBirth"", ""Role"", 
         ""IsActive"", ""CreatedAt"", ""UpdatedAt"", ""PronouncedName"", ""Pronouns"", ""IsVetted"")
        VALUES (@id, @encryptedLegalName, @sceneName, @email, @dateOfBirth, @role, 
                @isActive, @createdAt, @updatedAt, @pronouncedName, @pronouns, @isVetted)";
    
    using var cmd = new NpgsqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("encryptedLegalName", encryptedLegalName);
    cmd.Parameters.AddWithValue("sceneName", sceneName);
    cmd.Parameters.AddWithValue("email", email);
    cmd.Parameters.AddWithValue("dateOfBirth", dateOfBirth);
    cmd.Parameters.AddWithValue("role", role);
    cmd.Parameters.AddWithValue("isActive", isActive);
    cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
    cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);
    cmd.Parameters.AddWithValue("pronouncedName", sceneName); // Using scene name as pronounced name
    cmd.Parameters.AddWithValue("pronouns", pronouns);
    cmd.Parameters.AddWithValue("isVetted", isVetted);
    
    await cmd.ExecuteNonQueryAsync();
}

async Task SeedUserAuth(NpgsqlConnection conn, Guid id, Guid userId, string passwordHash)
{
    var sql = @"INSERT INTO ""UserAuthentications"" 
        (""Id"", ""UserId"", ""PasswordHash"", ""TwoFactorSecret"", ""IsTwoFactorEnabled"", 
         ""LastPasswordChangeAt"", ""FailedLoginAttempts"", ""LockedOutUntil"", ""CreatedAt"", ""UpdatedAt"")
        VALUES (@id, @userId, @passwordHash, NULL, false, @lastPasswordChange, 0, NULL, @createdAt, @updatedAt)";
    
    using var cmd = new NpgsqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("userId", userId);
    cmd.Parameters.AddWithValue("passwordHash", passwordHash);
    cmd.Parameters.AddWithValue("lastPasswordChange", DateTime.UtcNow);
    cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
    cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);
    
    await cmd.ExecuteNonQueryAsync();
}

async Task SeedEvent(NpgsqlConnection conn, Guid id, string title, string description, 
    DateTime startDate, DateTime endDate, int capacity, string eventType, string location, 
    bool isPublished, decimal[] pricingTiers)
{
    var pricingJson = "[" + string.Join(", ", pricingTiers.Select(p => $"{{\"Amount\": {p}, \"Currency\": \"USD\"}}")) + "]";
    
    var sql = @"INSERT INTO ""Events"" 
        (""Id"", ""Title"", ""Description"", ""StartDate"", ""EndDate"", ""Capacity"", 
         ""EventType"", ""Location"", ""IsPublished"", ""CreatedAt"", ""UpdatedAt"", ""PricingTiers"")
        VALUES (@id, @title, @description, @startDate, @endDate, @capacity, 
                @eventType, @location, @isPublished, @createdAt, @updatedAt, @pricingTiers)";
    
    using var cmd = new NpgsqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("title", title);
    cmd.Parameters.AddWithValue("description", description);
    cmd.Parameters.AddWithValue("startDate", startDate);
    cmd.Parameters.AddWithValue("endDate", endDate);
    cmd.Parameters.AddWithValue("capacity", capacity);
    cmd.Parameters.AddWithValue("eventType", eventType);
    cmd.Parameters.AddWithValue("location", location);
    cmd.Parameters.AddWithValue("isPublished", isPublished);
    cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow.AddDays(-14));
    cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);
    cmd.Parameters.AddWithValue("pricingTiers", pricingJson);
    
    await cmd.ExecuteNonQueryAsync();
}

async Task SeedEventOrganizer(NpgsqlConnection conn, Guid eventId, Guid userId)
{
    var sql = @"INSERT INTO ""EventOrganizers"" (""EventId"", ""UserId"") VALUES (@eventId, @userId)";
    
    using var cmd = new NpgsqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("eventId", eventId);
    cmd.Parameters.AddWithValue("userId", userId);
    
    await cmd.ExecuteNonQueryAsync();
}

async Task SeedVettingApplication(NpgsqlConnection conn, Guid id, Guid applicantId, 
    string experienceLevel, string interests, string safetyKnowledge, string experienceDescription,
    string consentUnderstanding, string whyJoin, string status, DateTime submittedAt, string references)
{
    var sql = @"INSERT INTO ""VettingApplications"" 
        (""Id"", ""ApplicantId"", ""ExperienceLevel"", ""Interests"", ""SafetyKnowledge"", 
         ""ExperienceDescription"", ""ConsentUnderstanding"", ""WhyJoin"", ""Status"", 
         ""SubmittedAt"", ""UpdatedAt"", ""ReviewedAt"", ""DecisionNotes"", ""References"")
        VALUES (@id, @applicantId, @experienceLevel, @interests, @safetyKnowledge, 
                @experienceDescription, @consentUnderstanding, @whyJoin, @status, 
                @submittedAt, @updatedAt, NULL, '', @references)";
    
    using var cmd = new NpgsqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("applicantId", applicantId);
    cmd.Parameters.AddWithValue("experienceLevel", experienceLevel);
    cmd.Parameters.AddWithValue("interests", interests);
    cmd.Parameters.AddWithValue("safetyKnowledge", safetyKnowledge);
    cmd.Parameters.AddWithValue("experienceDescription", experienceDescription);
    cmd.Parameters.AddWithValue("consentUnderstanding", consentUnderstanding);
    cmd.Parameters.AddWithValue("whyJoin", whyJoin);
    cmd.Parameters.AddWithValue("status", status);
    cmd.Parameters.AddWithValue("submittedAt", submittedAt);
    cmd.Parameters.AddWithValue("updatedAt", submittedAt);
    cmd.Parameters.AddWithValue("references", references);
    
    await cmd.ExecuteNonQueryAsync();
}

Console.WriteLine("\nScript completed.");