using Bogus;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Core.Enums;
using WitchCityRope.E2E.Tests.Infrastructure;
using WitchCityRope.Api.Services;

namespace WitchCityRope.E2E.Tests.Fixtures;

public class TestDataManager
{
    private readonly DatabaseFixture _databaseFixture;
    private readonly TestDataSettings _settings;
    private readonly IPasswordHasher _passwordHasher;
    private readonly List<Guid> _createdUserIds = new();
    private readonly List<Guid> _createdEventIds = new();

    public TestDataManager(DatabaseFixture databaseFixture, TestDataSettings settings)
    {
        _databaseFixture = databaseFixture;
        _settings = settings;
        _passwordHasher = new PasswordHasher();
    }

    public async Task<TestUser> CreateTestUserAsync(
        string? email = null, 
        string? password = null,
        bool isVerified = true,
        bool isVetted = false)
    {
        var faker = new Faker();
        email ??= $"{_settings.TestUserPrefix}{faker.Random.AlphaNumeric(8)}@test.com";
        password ??= _settings.DefaultPassword;

        var user = new User(
            encryptedLegalName: faker.Name.FullName(), // Note: In production this should be encrypted
            sceneName: SceneName.Create($"Test{faker.Random.AlphaNumeric(6)}"),
            email: EmailAddress.Create(email),
            dateOfBirth: faker.Date.Past(30, DateTime.Now.AddYears(-21)), // Generate age between 21-51
            role: UserRole.Attendee
        );

        if (isVetted)
        {
            user.MarkAsVetted();
        }

        _databaseFixture.DbContext.Users.Add(user);
        await _databaseFixture.DbContext.SaveChangesAsync();
        
        _createdUserIds.Add(user.Id);

        return new TestUser
        {
            Id = user.Id,
            Email = email,
            Password = password,
            SceneName = user.SceneName.Value,
            IsVerified = isVerified,
            IsVetted = isVetted
        };
    }

    public async Task<TestEvent> CreateTestEventAsync(
        string? title = null,
        DateTime? startDate = null,
        decimal? price = null,
        int capacity = 50)
    {
        var faker = new Faker();
        title ??= faker.Lorem.Sentence(3);
        startDate ??= faker.Date.Future(1);
        price ??= faker.Random.Decimal(25, 200);

        // Need to create a primary organizer first
        var organizer = await CreateTestUserAsync();
        var organizerEntity = await _databaseFixture.DbContext.Users
            .FirstOrDefaultAsync(u => u.Id == organizer.Id)
            ?? throw new InvalidOperationException("Could not find organizer");
        
        var eventEntity = new Event(
            title: title,
            description: faker.Lorem.Paragraph(),
            startDate: startDate.Value,
            endDate: startDate.Value.AddHours(3),
            capacity: capacity,
            eventType: EventType.Class, // Default to class for tests
            location: faker.Address.FullAddress(),
            primaryOrganizer: organizerEntity,
            pricingTiers: new[] { Money.Create("USD", price.Value) }
        );
        
        // Publish the event
        eventEntity.Publish();

        _databaseFixture.DbContext.Events.Add(eventEntity);
        await _databaseFixture.DbContext.SaveChangesAsync();
        
        _createdEventIds.Add(eventEntity.Id);

        return new TestEvent
        {
            Id = eventEntity.Id,
            Title = eventEntity.Title,
            StartDate = eventEntity.StartDate,
            Price = eventEntity.Price.Amount,
            Capacity = eventEntity.Capacity
        };
    }

    public async Task<Registration> CreateRegistrationAsync(Guid userId, Guid eventId)
    {
        var user = await _databaseFixture.DbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new InvalidOperationException("User not found");
            
        var eventEntity = await _databaseFixture.DbContext.Events
            .Include(e => e.PricingTiers)
            .FirstOrDefaultAsync(e => e.Id == eventId)
            ?? throw new InvalidOperationException("Event not found");
            
        var selectedPrice = eventEntity.PricingTiers.FirstOrDefault() 
            ?? Money.Create("USD", 50); // Default price if none found
            
        var registration = new Registration(
            user: user,
            eventToRegister: eventEntity,
            selectedPrice: selectedPrice
        );
        
        // For testing, we'll leave registrations in pending state
        // In real scenarios, they would need payment to confirm

        _databaseFixture.DbContext.Registrations.Add(registration);
        await _databaseFixture.DbContext.SaveChangesAsync();

        return registration;
    }

    public async Task<VettingApplication> CreateVettingApplicationAsync(Guid userId)
    {
        var faker = new Faker();
        
        var applicant = await _databaseFixture.DbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new InvalidOperationException("User not found");
        
        var application = new VettingApplication(
            applicant: applicant,
            experienceLevel: "Intermediate",
            interests: faker.Lorem.Paragraph(),
            safetyKnowledge: faker.Lorem.Paragraph(),
            references: new[] { 
                $"Reference 1: {faker.Name.FullName()}", 
                $"Reference 2: {faker.Name.FullName()}" 
            }
        );

        _databaseFixture.DbContext.VettingApplications.Add(application);
        await _databaseFixture.DbContext.SaveChangesAsync();

        return application;
    }

    public async Task CleanupTestDataAsync()
    {
        // Delete test registrations
        var registrations = await _databaseFixture.DbContext.Registrations
            .Where(r => _createdUserIds.Contains(r.UserId) || _createdEventIds.Contains(r.EventId))
            .ToListAsync();
        _databaseFixture.DbContext.Registrations.RemoveRange(registrations);

        // Delete test events
        var events = await _databaseFixture.DbContext.Events
            .Where(e => _createdEventIds.Contains(e.Id))
            .ToListAsync();
        _databaseFixture.DbContext.Events.RemoveRange(events);

        // Delete test users
        var users = await _databaseFixture.DbContext.Users
            .Where(u => _createdUserIds.Contains(u.Id))
            .ToListAsync();
        _databaseFixture.DbContext.Users.RemoveRange(users);

        await _databaseFixture.DbContext.SaveChangesAsync();

        _createdUserIds.Clear();
        _createdEventIds.Clear();
    }
}

public class TestUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SceneName { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public bool IsVetted { get; set; }
}

public class TestEvent
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public decimal Price { get; set; }
    public int Capacity { get; set; }
}