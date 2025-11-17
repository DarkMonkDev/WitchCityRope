using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.Vetting.Models;
using WitchCityRope.Api.Features.Vetting.Services;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Models;
using Xunit;
using FluentAssertions;
using Testcontainers.PostgreSql;
using NSubstitute;

namespace WitchCityRope.UnitTests.Api.Features.Vetting;

/// <summary>
/// Unit tests for VettingService profile update feature
/// Tests that user profile fields (firstName, lastName, pronouns, fetLifeHandle)
/// are automatically updated when a vetting application is submitted
/// </summary>
[Collection("Database")]
public class VettingServiceProfileUpdateTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private ApplicationDbContext _context = null!;
    private VettingService _service = null!;
    private ILogger<VettingService> _logger = null!;
    private IVettingEmailService _emailService = null!;
    private string _connectionString = null!;

    public VettingServiceProfileUpdateTests()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("witchcityrope_test")
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
        await _context.Database.EnsureCreatedAsync();

        _logger = new LoggerFactory().CreateLogger<VettingService>();
        _emailService = Substitute.For<IVettingEmailService>();

        // Setup email service to always return success
        _emailService.SendApplicationConfirmationAsync(
            Arg.Any<VettingApplication>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<bool>.Success(true)));

        _service = new VettingService(_context, _logger, _emailService);
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        await _container.DisposeAsync();
    }

    #region Happy Path - All Fields Provided

    [Fact]
    public async Task SubmitSimplifiedApplication_WithAllFields_UpdatesAllProfileFields()
    {
        // Arrange
        var user = await CreateTestUser("testuser@example.com");
        var request = CreateValidRequest(
            firstName: "John",
            lastName: "Doe",
            pronouns: "he/him",
            fetLifeHandle: "JohnDoeRope"
        );

        // Act
        var result = await _service.SubmitSimplifiedApplicationAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify user profile was updated
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.FirstName.Should().Be("John");
        updatedUser.LastName.Should().Be("Doe");
        updatedUser.Pronouns.Should().Be("he/him");
        updatedUser.FetLifeName.Should().Be("JohnDoeRope");
    }

    [Fact]
    public async Task SubmitSimplifiedApplication_WithAllFields_UpdatesUpdatedAtTimestamp()
    {
        // Arrange
        var user = await CreateTestUser("testuser@example.com");
        var originalUpdatedAt = user.UpdatedAt;

        // Wait a tiny bit to ensure timestamp changes
        await Task.Delay(10);

        var request = CreateValidRequest(
            firstName: "Jane",
            lastName: "Smith"
        );

        // Act
        var result = await _service.SubmitSimplifiedApplicationAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    #endregion

    #region Optional Fields Not Provided

    [Fact]
    public async Task SubmitSimplifiedApplication_WithoutOptionalFields_DoesNotOverwriteExistingValues()
    {
        // Arrange - User already has pronouns and FetLifeName
        var user = await CreateTestUser("testuser@example.com");
        user.Pronouns = "they/them";
        user.FetLifeName = "ExistingHandle";
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        var request = CreateValidRequest(
            firstName: "Alex",
            lastName: "Johnson",
            pronouns: null, // Not provided
            fetLifeHandle: null // Not provided
        );

        // Act
        var result = await _service.SubmitSimplifiedApplicationAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.FirstName.Should().Be("Alex");
        updatedUser.LastName.Should().Be("Johnson");
        updatedUser.Pronouns.Should().Be("they/them", "existing pronouns should be preserved");
        updatedUser.FetLifeName.Should().Be("ExistingHandle", "existing FetLife handle should be preserved");
    }

    [Fact]
    public async Task SubmitSimplifiedApplication_WithEmptyOptionalFields_DoesNotOverwriteExistingValues()
    {
        // Arrange - User already has pronouns and FetLifeName
        var user = await CreateTestUser("testuser@example.com");
        user.Pronouns = "she/her";
        user.FetLifeName = "SheRope";
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        var request = CreateValidRequest(
            firstName: "Sarah",
            lastName: "Williams",
            pronouns: "", // Empty string
            fetLifeHandle: "   " // Whitespace only
        );

        // Act
        var result = await _service.SubmitSimplifiedApplicationAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.Pronouns.Should().Be("she/her", "empty pronouns should not overwrite existing value");
        updatedUser.FetLifeName.Should().Be("SheRope", "whitespace FetLife handle should not overwrite existing value");
    }

    #endregion

    #region Only Pronouns Provided

    [Fact]
    public async Task SubmitSimplifiedApplication_WithOnlyPronouns_UpdatesPronounsOnly()
    {
        // Arrange - User has existing FetLifeName
        var user = await CreateTestUser("testuser@example.com");
        user.FetLifeName = "ExistingHandle";
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        var request = CreateValidRequest(
            firstName: "Taylor",
            lastName: "Anderson",
            pronouns: "ze/zir",
            fetLifeHandle: null // Not provided
        );

        // Act
        var result = await _service.SubmitSimplifiedApplicationAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.Pronouns.Should().Be("ze/zir", "pronouns should be updated");
        updatedUser.FetLifeName.Should().Be("ExistingHandle", "FetLife handle should remain unchanged");
    }

    #endregion

    #region Only FetLifeHandle Provided

    [Fact]
    public async Task SubmitSimplifiedApplication_WithOnlyFetLifeHandle_UpdatesFetLifeHandleOnly()
    {
        // Arrange - User has existing Pronouns
        var user = await CreateTestUser("testuser@example.com");
        user.Pronouns = "he/him";
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        var request = CreateValidRequest(
            firstName: "Michael",
            lastName: "Brown",
            pronouns: null, // Not provided
            fetLifeHandle: "MikeBrownRope"
        );

        // Act
        var result = await _service.SubmitSimplifiedApplicationAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.Pronouns.Should().Be("he/him", "pronouns should remain unchanged");
        updatedUser.FetLifeName.Should().Be("MikeBrownRope", "FetLife handle should be updated");
    }

    #endregion

    #region Required Fields Always Updated

    [Fact]
    public async Task SubmitSimplifiedApplication_AlwaysUpdatesFirstNameAndLastName()
    {
        // Arrange - User has existing names
        var user = await CreateTestUser("testuser@example.com");
        user.FirstName = "OldFirst";
        user.LastName = "OldLast";
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        var request = CreateValidRequest(
            firstName: "NewFirst",
            lastName: "NewLast"
        );

        // Act
        var result = await _service.SubmitSimplifiedApplicationAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.FirstName.Should().Be("NewFirst", "first name should always be updated");
        updatedUser.LastName.Should().Be("NewLast", "last name should always be updated");
    }

    #endregion

    #region Transaction Atomicity

    [Fact]
    public async Task SubmitSimplifiedApplication_WhenUserNotFound_StillCreatesApplication()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var request = CreateValidRequest(
            firstName: "Test",
            lastName: "User",
            email: "nonexistent@example.com"
        );

        // Act
        var result = await _service.SubmitSimplifiedApplicationAsync(request, CancellationToken.None);

        // Assert
        // Service should succeed even if user doesn't exist (public application flow)
        result.IsSuccess.Should().BeTrue("application can be submitted without existing user account");

        // Verify application was created
        var application = await _context.VettingApplications
            .FirstOrDefaultAsync(a => a.Email == "nonexistent@example.com");

        application.Should().NotBeNull("application should be created even without existing user");
        application!.FirstName.Should().Be("Test");
        application.LastName.Should().Be("User");
        application.WorkflowStatus.Should().Be(VettingStatus.UnderReview);
    }

    #endregion

    #region Application Creation Verification

    [Fact]
    public async Task SubmitSimplifiedApplication_CreatesApplicationWithCorrectStatus()
    {
        // Arrange
        var user = await CreateTestUser("testuser@example.com");
        var request = CreateValidRequest(
            firstName: "Chris",
            lastName: "Davis"
        );

        // Act
        var result = await _service.SubmitSimplifiedApplicationAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var application = await _context.VettingApplications
            .FirstOrDefaultAsync(a => a.UserId == user.Id);

        application.Should().NotBeNull();
        application!.WorkflowStatus.Should().Be(VettingStatus.UnderReview);
        application.FirstName.Should().Be("Chris");
        application.LastName.Should().Be("Davis");
    }

    [Fact]
    public async Task SubmitSimplifiedApplication_UpdatesUserVettingStatus()
    {
        // Arrange
        var user = await CreateTestUser("testuser@example.com");
        user.VettingStatus = 0; // NotStarted
        user.HasVettingApplication = false;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        var request = CreateValidRequest(
            firstName: "Morgan",
            lastName: "Taylor"
        );

        // Act
        var result = await _service.SubmitSimplifiedApplicationAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.VettingStatus.Should().Be((int)VettingStatus.UnderReview);
        updatedUser.HasVettingApplication.Should().BeTrue();
    }

    #endregion

    // Helper methods
    private async Task<ApplicationUser> CreateTestUser(string email)
    {
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpper(),
            NormalizedUserName = email.ToUpper(),
            SceneName = $"Scene-{uniqueId}",
            EmailConfirmed = true,
            Role = "Member",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private SimplifiedApplicationRequest CreateValidRequest(
        string firstName,
        string lastName,
        string? pronouns = null,
        string? fetLifeHandle = null,
        string? email = null)
    {
        return new SimplifiedApplicationRequest
        {
            FirstName = firstName,
            LastName = lastName,
            PreferredSceneName = $"Scene_{Guid.NewGuid():N}"[..10],
            Email = email ?? "testuser@example.com",
            WhyJoin = "I want to learn rope bondage in a safe and welcoming community.",
            ExperienceWithRope = "I have been practicing rope for 2 years.",
            AgreeToCommunityStandards = true,
            Pronouns = pronouns,
            FetLifeHandle = fetLifeHandle
        };
    }
}
