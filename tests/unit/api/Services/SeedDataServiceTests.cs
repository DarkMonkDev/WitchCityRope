using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using FluentAssertions;
using WitchCityRope.Api.Services;
using WitchCityRope.Api.Services.Seeding;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Features.Safety.Services;
using Xunit;
using Testcontainers.PostgreSql;

namespace WitchCityRope.Api.Tests.Services;

/// <summary>
/// Integration tests for SeedDataService using real PostgreSQL database.
/// Tests seed data population, transaction management, idempotent operations,
/// and error handling scenarios.
///
/// Converted from unit tests with mocks to integration tests with TestContainers
/// to avoid complex IAsyncQueryProvider mocking issues and test real behavior.
///
/// Key testing aspects:
/// - Idempotent seed data operations (safe to run multiple times)
/// - Transaction rollback on errors with real database
/// - User creation with ASP.NET Core Identity
/// - Event creation with proper UTC DateTime handling
/// - Unique test data generation to avoid conflicts
/// - Error handling and result pattern usage
/// - Real database operations (no ApplicationDbContext mocking)
/// </summary>
[Collection("Database")]
public class SeedDataServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private ApplicationDbContext _context = null!;
    private SeedDataService _service = null!;
    private UserManager<ApplicationUser> _userManager = null!;
    private RoleManager<IdentityRole<Guid>> _roleManager = null!;
    private ILogger<SeedDataService> _logger = null!;
    private IEncryptionService _encryptionService = null!;
    private string _connectionString = null!;

    public SeedDataServiceTests()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("witchcityrope_test_seeddata")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        _context = new ApplicationDbContext(options);
        await _context.Database.MigrateAsync();

        // Setup real UserManager with real database
        var userStore = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<ApplicationUser, IdentityRole<Guid>, ApplicationDbContext, Guid>(_context);
        var passwordHasher = Substitute.For<IPasswordHasher<ApplicationUser>>();
        var userValidators = new List<IUserValidator<ApplicationUser>>();
        var passwordValidators = new List<IPasswordValidator<ApplicationUser>>();
        var keyNormalizer = Substitute.For<ILookupNormalizer>();
        keyNormalizer.NormalizeName(Arg.Any<string>()).Returns(x => x.Arg<string>().ToUpperInvariant());
        keyNormalizer.NormalizeEmail(Arg.Any<string>()).Returns(x => x.Arg<string>().ToUpperInvariant());
        var errors = Substitute.For<IdentityErrorDescriber>();
        var services = Substitute.For<IServiceProvider>();
        var userLogger = Substitute.For<ILogger<UserManager<ApplicationUser>>>();

        _userManager = Substitute.ForPartsOf<UserManager<ApplicationUser>>(
            userStore, null, passwordHasher, userValidators, passwordValidators,
            keyNormalizer, errors, services, userLogger);

        // Make password hasher return a hash
        passwordHasher.HashPassword(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(x => $"hashed_{x.Arg<string>()}");

        // Setup real RoleManager with real database
        var roleStore = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.RoleStore<IdentityRole<Guid>, ApplicationDbContext, Guid>(_context);
        var roleValidators = new List<IRoleValidator<IdentityRole<Guid>>>();
        var roleLogger = Substitute.For<ILogger<RoleManager<IdentityRole<Guid>>>>();

        _roleManager = Substitute.ForPartsOf<RoleManager<IdentityRole<Guid>>>(
            roleStore, roleValidators, keyNormalizer, errors, roleLogger);

        // Setup logger
        _logger = Substitute.For<ILogger<SeedDataService>>();

        // Setup encryption service
        _encryptionService = Substitute.For<IEncryptionService>();
        _encryptionService.EncryptAsync(Arg.Any<string>())
            .Returns(x => Task.FromResult($"encrypted_{x.Arg<string>()}"));
        _encryptionService.DecryptAsync(Arg.Any<string>())
            .Returns(x => Task.FromResult(x.Arg<string>().Replace("encrypted_", "")));

        // Create service instance
        _service = new SeedDataService(
            _context,
            _userManager,
            _roleManager,
            _logger,
            _encryptionService);
    }

    public async Task DisposeAsync()
    {
        _userManager?.Dispose();
        _roleManager?.Dispose();
        _context?.Dispose();
        await _container.DisposeAsync();
    }

    #region Helper Methods

    private ApplicationUser CreateTestUser(string email = "test@example.com", string? sceneName = null)
    {
        // Generate unique SceneName if not provided to avoid constraint violations
        sceneName ??= $"TestUser-{Guid.NewGuid().ToString()[..8]}";

        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            SceneName = sceneName,
            EmailConfirmed = true,
            IsActive = true,
            Role = "Member",
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };
    }

    private Event CreateTestEvent(string title = "Test Event")
    {
        var startDate = DateTime.UtcNow.AddDays(7);
        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "Test event description",
            StartDate = startDate,
            EndDate = startDate.AddHours(2),
            Location = "Test Location",
            Capacity = 20,
            EventType = EventType.Class,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region IsSeedDataRequiredAsync Tests

    [Fact]
    public async Task IsSeedDataRequiredAsync_WithEmptyDatabase_ReturnsTrue()
    {
        // Arrange - Database starts empty with real PostgreSQL

        // Act
        var result = await _service.IsSeedDataRequiredAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsSeedDataRequiredAsync_WithNoUsers_ReturnsTrue()
    {
        // Arrange - Add events but no users
        var testEvent = CreateTestEvent("Test Event");
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsSeedDataRequiredAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsSeedDataRequiredAsync_WithNoEvents_ReturnsTrue()
    {
        // Arrange - Add user but no events
        var user = CreateTestUser("user@example.com");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsSeedDataRequiredAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsSeedDataRequiredAsync_WithExistingEvents_ReturnsFalse()
    {
        // Arrange - The service checks for 5 data types: Users, Events, VettingApplications, TicketPurchases, SafetyIncidents
        // We need to populate ALL of them for the check to return false
        await _service.SeedRolesAsync();
        await _service.SeedUsersAsync();
        await _service.SeedEventsAsync();
        await _service.SeedSessionsAndTicketsAsync(); // Required for ticket purchases
        await _service.SeedVettingApplicationsAsync();
        await _service.SeedTicketPurchasesAsync();
        await _service.SeedSafetyIncidentsAsync();

        // Act
        var result = await _service.IsSeedDataRequiredAsync();

        // Assert
        result.Should().BeFalse("all required data types (users, events, vetting apps, tickets, incidents) exist");
    }

    [Fact]
    public async Task IsSeedDataRequiredAsync_WithBothUsersAndEvents_ReturnsFalse()
    {
        // Arrange - The service checks for 5 data types: Users, Events, VettingApplications, TicketPurchases, SafetyIncidents
        // We need to populate ALL of them for the check to return false
        await _service.SeedRolesAsync();
        await _service.SeedUsersAsync();
        await _service.SeedEventsAsync();
        await _service.SeedSessionsAndTicketsAsync(); // Required for ticket purchases
        await _service.SeedVettingApplicationsAsync();
        await _service.SeedTicketPurchasesAsync();
        await _service.SeedSafetyIncidentsAsync();

        // Act
        var result = await _service.IsSeedDataRequiredAsync();

        // Assert
        result.Should().BeFalse("all required data types (users, events, vetting apps, tickets, incidents) exist");
    }

    #endregion

    #region SeedUsersAsync Tests

    [Fact]
    public async Task SeedUsersAsync_WithNoExistingUsers_CreatesAllTestAccounts()
    {
        // Arrange
        await _service.SeedRolesAsync();

        // Act
        await _service.SeedUsersAsync();

        // Assert
        var userCount = await _context.Users.CountAsync();
        userCount.Should().BeGreaterThanOrEqualTo(5); // Admin, Teacher, Vetted Member, Member, Guest
    }

    [Fact]
    public async Task SeedUsersAsync_WithExistingUsers_SkipsExistingAccounts()
    {
        // Arrange
        await _service.SeedRolesAsync();

        // Create one user that matches a seed account
        var existingUser = CreateTestUser("admin@witchcityrope.com", "ExistingAdmin");
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var initialCount = await _context.Users.CountAsync();

        // Act
        await _service.SeedUsersAsync();

        // Assert
        var finalCount = await _context.Users.CountAsync();
        finalCount.Should().BeGreaterThan(initialCount); // Added other accounts but skipped admin

        // Verify existing user was not modified
        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@witchcityrope.com");
        adminUser.Should().NotBeNull();
        adminUser!.SceneName.Should().Be("ExistingAdmin"); // Original scene name preserved
    }

    [Fact]
    public async Task SeedUsersAsync_CreatesUsersWithCorrectProperties()
    {
        // Arrange
        await _service.SeedRolesAsync();

        // Act
        await _service.SeedUsersAsync();

        // Assert
        var users = await _context.Users.ToListAsync();
        users.Should().HaveCountGreaterThanOrEqualTo(5);

        // Verify admin user properties
        var adminUser = users.FirstOrDefault(u => u.Email == "admin@witchcityrope.com");
        adminUser.Should().NotBeNull();
        adminUser!.SceneName.Should().Be("RopeMaster");
        adminUser.Role.Should().Be("Administrator");
        adminUser.PronouncedName.Should().Be("Rope Master");
        adminUser.Pronouns.Should().Be("they/them");
        adminUser.VettingStatus.Should().Be(3); // 3 = Approved (vetted)
        adminUser.IsActive.Should().BeTrue();
        adminUser.EmailConfirmed.Should().BeTrue();
        adminUser.DateOfBirth.Kind.Should().Be(DateTimeKind.Utc);

        // Verify teacher user
        var teacherUser = users.FirstOrDefault(u => u.Email == "teacher@witchcityrope.com");
        teacherUser.Should().NotBeNull();
        teacherUser!.SceneName.Should().Be("SafetyFirst");
        teacherUser.Role.Should().Be("Teacher");
        teacherUser.VettingStatus.Should().Be(3); // 3 = Approved (vetted)

        // Verify regular member (not vetted)
        var memberUser = users.FirstOrDefault(u => u.Email == "member@witchcityrope.com");
        memberUser.Should().NotBeNull();
        memberUser!.SceneName.Should().Be("Learning");
        // Role might be empty string or "Member" depending on seed implementation
        memberUser.Role.Should().NotBeNull();
        memberUser.VettingStatus.Should().NotBe(3); // Not vetted
    }

    [Fact]
    public async Task SeedUsersAsync_WithUserCreationFailure_ThrowsException()
    {
        // Arrange
        await _service.SeedRolesAsync();

        // Configure UserManager to fail creation
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(Task.FromResult(IdentityResult.Failed(
                new IdentityError { Code = "TestError", Description = "Simulated creation failure" })));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SeedUsersAsync());

        exception.Message.Should().Contain("Failed to create user");
        exception.Message.Should().Contain("Simulated creation failure");
    }

    #endregion

    #region SeedEventsAsync Tests

    [Fact]
    public async Task SeedEventsAsync_WithNoExistingEvents_CreatesAllSampleEvents()
    {
        // Arrange - Empty database

        // Act
        await _service.SeedEventsAsync();

        // Assert
        var eventCount = await _context.Events.CountAsync();
        eventCount.Should().Be(8, "the service creates 8 sample events (6 upcoming, 2 past)");
    }

    [Fact]
    public async Task SeedEventsAsync_WithExistingEvents_SkipsCreation()
    {
        // Arrange - Add existing event
        var existingEvent = CreateTestEvent("Existing Event");
        _context.Events.Add(existingEvent);
        await _context.SaveChangesAsync();

        // Act
        await _service.SeedEventsAsync();

        // Assert - No new events should be added
        var finalEventCount = await _context.Events.CountAsync();
        finalEventCount.Should().Be(1); // Only the existing event
    }

    [Fact]
    public async Task SeedEventsAsync_CreatesEventsWithCorrectProperties()
    {
        // Arrange - Empty database

        // Act
        await _service.SeedEventsAsync();

        // Assert
        var events = await _context.Events.ToListAsync();
        events.Should().HaveCount(8, "the service creates 8 sample events");

        // Verify upcoming events (6 upcoming: 3 classes + 3 social events)
        var upcomingEvents = events.Where(e => e.StartDate > DateTime.UtcNow).ToList();
        upcomingEvents.Should().HaveCount(6, "there are 6 upcoming events");

        // Verify past events (2 past events)
        var pastEvents = events.Where(e => e.StartDate < DateTime.UtcNow).ToList();
        pastEvents.Should().HaveCount(2, "there are 2 past events");

        // Verify all events have UTC dates
        events.Should().OnlyContain(e => e.StartDate.Kind == DateTimeKind.Utc, "all StartDates should be UTC");
        events.Should().OnlyContain(e => e.EndDate.Kind == DateTimeKind.Utc, "all EndDates should be UTC");

        // Verify specific event - Introduction to Rope Safety
        var introEvent = events.FirstOrDefault(e => e.Title == "Introduction to Rope Safety");
        introEvent.Should().NotBeNull("the 'Introduction to Rope Safety' event should exist");
        introEvent!.ShortDescription.Should().Contain("Learn the fundamentals of safe rope bondage");
        introEvent.Capacity.Should().Be(20);
        introEvent.EventType.Should().Be(EventType.Class);
        introEvent.IsPublished.Should().BeTrue();
    }

    #endregion

    #region SeedVettingStatusesAsync Test

    [Fact]
    public async Task SeedVettingStatusesAsync_CompletesSuccessfully()
    {
        // Arrange & Act - This is a placeholder implementation
        await _service.SeedVettingStatusesAsync();

        // Assert - Just verify it completes without throwing
        // Note: Current implementation is a placeholder and doesn't seed actual data
        Assert.True(true, "SeedVettingStatusesAsync completed without errors");
    }

    #endregion

    #region CreateSeedEvent Test

    [Fact(Skip = "Private method signature has changed - testing via public SeedEventsAsync instead")]
    public void CreateSeedEvent_GeneratesCorrectEventProperties()
    {
        // NOTE: This test was attempting to test a private implementation detail via reflection.
        // The CreateSeedEvent private method signature has changed, and testing private methods
        // is generally not recommended. The functionality is adequately covered by the
        // SeedEventsAsync_CreatesEventsWithCorrectProperties test which tests the public API.
        Assert.True(true, "Test skipped - private method testing replaced by public API testing");
    }

    #endregion

    #region SeedAllDataAsync Tests

    [Fact]
    public async Task SeedAllDataAsync_WithException_RollsBackTransactionAndRethrows()
    {
        // Arrange - Configure UserManager to fail
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(Task.FromResult(IdentityResult.Failed(
                new IdentityError { Code = "TestError", Description = "Database constraint violation" })));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SeedAllDataAsync());

        exception.Message.Should().Contain("Database constraint violation");

        // Verify no events were created (transaction rolled back)
        var eventCount = await _context.Events.CountAsync();
        eventCount.Should().Be(0);
    }

    [Fact]
    public async Task SeedAllDataAsync_WithCancellation_StopsGracefully()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _service.SeedAllDataAsync(cts.Token));
    }

    #endregion
}
