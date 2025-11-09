using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Features.Vetting.Models;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.IntegrationTests.Api.Features.Vetting;

/// <summary>
/// Integration tests for automatic user profile updates during vetting application submission
/// Tests that firstName, lastName, pronouns, and fetLifeHandle are persisted to database
/// when a user submits a simplified vetting application
/// NOTE: Uses Sequential collection to prevent database deadlocks from parallel test execution
/// </summary>
[Collection("Sequential")]
public class VettingProfileUpdateIntegrationTests : IntegrationTestBase
{
    private readonly WebApplicationFactory<Program> _factory;

    public VettingProfileUpdateIntegrationTests(DatabaseTestFixture fixture)
        : base(fixture)
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the app's DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add DbContext using the test container's connection string
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseNpgsql(ConnectionString);
                    });
                });
            });
    }

    #region Full Profile Update Tests

    [Fact]
    public async Task SubmitSimplifiedApplication_WithAllFields_PersistsAllProfileFieldsToDatabase()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedUserAsync("testuser@example.com");

        var request = new SimplifiedApplicationRequest
        {
            FirstName = "Integration",
            LastName = "TestUser",
            PreferredSceneName = $"IntegrationScene_{Guid.NewGuid():N}"[..15],
            Email = "testuser@example.com",
            Pronouns = "they/them",
            FetLifeHandle = "IntegrationTestHandle",
            WhyJoin = "I want to join this amazing community.",
            ExperienceWithRope = "I have 3 years of experience with rope.",
            AgreeToCommunityStandards = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/vetting/applications/simplified", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created, "POST to create new vetting application should return 201 Created");

        // Verify database persistence
        await using var context = CreateDbContext();
        var user = await context.Users.FindAsync(userId);
        user.Should().NotBeNull();
        user!.FirstName.Should().Be("Integration", "first name should be persisted");
        user.LastName.Should().Be("TestUser", "last name should be persisted");
        user.Pronouns.Should().Be("they/them", "pronouns should be persisted");
        user.FetLifeName.Should().Be("IntegrationTestHandle", "FetLife handle should be persisted");
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Partial Update Tests

    [Fact]
    public async Task SubmitSimplifiedApplication_WithOnlyRequiredFields_DoesNotOverwriteExistingOptionalFields()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedUserAsync("partial@example.com");

        // Set existing optional fields
        await using var setupContext = CreateDbContext();
        var user = await setupContext.Users.FindAsync(userId);
        user!.Pronouns = "she/her";
        user.FetLifeName = "ExistingHandle";
        setupContext.Users.Update(user);
        await setupContext.SaveChangesAsync();

        var request = new SimplifiedApplicationRequest
        {
            FirstName = "Partial",
            LastName = "Update",
            PreferredSceneName = $"PartialScene_{Guid.NewGuid():N}"[..15],
            Email = "partial@example.com",
            Pronouns = null, // Not provided
            FetLifeHandle = null, // Not provided
            WhyJoin = "Testing partial updates.",
            ExperienceWithRope = "Some experience.",
            AgreeToCommunityStandards = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/vetting/applications/simplified", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created, "POST to create new vetting application should return 201 Created");

        // Verify database persistence
        await using var context = CreateDbContext();
        var updatedUser = await context.Users.FindAsync(userId);
        updatedUser.Should().NotBeNull();
        updatedUser!.FirstName.Should().Be("Partial");
        updatedUser.LastName.Should().Be("Update");
        updatedUser.Pronouns.Should().Be("she/her", "existing pronouns should be preserved");
        updatedUser.FetLifeName.Should().Be("ExistingHandle", "existing FetLife handle should be preserved");
    }

    #endregion

    #region Database Persistence Verification

    [Fact]
    public async Task SubmitSimplifiedApplication_CommitsChangesToDatabase()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedUserAsync("persist@example.com");

        var request = new SimplifiedApplicationRequest
        {
            FirstName = "Persist",
            LastName = "Test",
            PreferredSceneName = $"PersistScene_{Guid.NewGuid():N}"[..15],
            Email = "persist@example.com",
            Pronouns = "he/him",
            FetLifeHandle = "PersistHandle",
            WhyJoin = "Testing database persistence.",
            ExperienceWithRope = "Testing.",
            AgreeToCommunityStandards = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/vetting/applications/simplified", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created, "POST to create new vetting application should return 201 Created");

        // Create a NEW database context to verify changes are actually committed
        await using var newContext = CreateDbContext();
        var user = await newContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        user.Should().NotBeNull();
        user!.FirstName.Should().Be("Persist");
        user.LastName.Should().Be("Test");
        user.Pronouns.Should().Be("he/him");
        user.FetLifeName.Should().Be("PersistHandle");
    }

    #endregion

    #region Existing User Data Preserved

    [Fact]
    public async Task SubmitSimplifiedApplication_WithExistingPronouns_PreservesWhenNotProvided()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedUserAsync("preserve@example.com");

        // Set existing pronouns
        await using var setupContext = CreateDbContext();
        var user = await setupContext.Users.FindAsync(userId);
        user!.Pronouns = "ze/zir";
        setupContext.Users.Update(user);
        await setupContext.SaveChangesAsync();

        var request = new SimplifiedApplicationRequest
        {
            FirstName = "Preserve",
            LastName = "Pronouns",
            PreferredSceneName = $"PreserveScene_{Guid.NewGuid():N}"[..15],
            Email = "preserve@example.com",
            Pronouns = "", // Empty string (should not overwrite)
            FetLifeHandle = "NewHandle",
            WhyJoin = "Testing preservation.",
            ExperienceWithRope = "Testing.",
            AgreeToCommunityStandards = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/vetting/applications/simplified", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created, "POST to create new vetting application should return 201 Created");

        await using var context = CreateDbContext();
        var updatedUser = await context.Users.FindAsync(userId);
        updatedUser.Should().NotBeNull();
        updatedUser!.Pronouns.Should().Be("ze/zir", "existing pronouns should be preserved when empty string provided");
        updatedUser.FetLifeName.Should().Be("NewHandle", "FetLife handle should still be updated");
    }

    [Fact]
    public async Task SubmitSimplifiedApplication_WithExistingFetLifeName_PreservesWhenNotProvided()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedUserAsync("preserve-fetlife@example.com");

        // Set existing FetLifeName
        await using var setupContext = CreateDbContext();
        var user = await setupContext.Users.FindAsync(userId);
        user!.FetLifeName = "OriginalHandle";
        setupContext.Users.Update(user);
        await setupContext.SaveChangesAsync();

        var request = new SimplifiedApplicationRequest
        {
            FirstName = "Preserve",
            LastName = "FetLife",
            PreferredSceneName = $"PreserveFLScene_{Guid.NewGuid():N}"[..15],
            Email = "preserve-fetlife@example.com",
            Pronouns = "she/her",
            FetLifeHandle = "   ", // Whitespace (should not overwrite)
            WhyJoin = "Testing preservation.",
            ExperienceWithRope = "Testing.",
            AgreeToCommunityStandards = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/vetting/applications/simplified", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created, "POST to create new vetting application should return 201 Created");

        await using var context = CreateDbContext();
        var updatedUser = await context.Users.FindAsync(userId);
        updatedUser.Should().NotBeNull();
        updatedUser!.FetLifeName.Should().Be("OriginalHandle", "existing FetLife handle should be preserved when whitespace provided");
        updatedUser.Pronouns.Should().Be("she/her", "pronouns should still be updated");
    }

    #endregion

    #region Authentication Required

    [Fact]
    public async Task SubmitSimplifiedApplication_WithoutAuthentication_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        // No authentication header set

        var request = new SimplifiedApplicationRequest
        {
            FirstName = "Unauthorized",
            LastName = "User",
            PreferredSceneName = $"UnauthorizedScene_{Guid.NewGuid():N}"[..15],
            Email = "unauthorized@example.com",
            WhyJoin = "Testing unauthorized access.",
            ExperienceWithRope = "Testing.",
            AgreeToCommunityStandards = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/vetting/applications/simplified", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Verify no user profile was created or updated
        await using var context = CreateDbContext();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "unauthorized@example.com");
        user.Should().BeNull("no user should exist without authentication");
    }

    #endregion

    // Helper methods

    /// <summary>
    /// Creates an authenticated user and returns an HttpClient with JWT token
    /// </summary>
    private async Task<(HttpClient client, Guid userId)> CreateAuthenticatedUserAsync(string email)
    {
        var userId = Guid.NewGuid();
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);

        await using var context = CreateDbContext();

        var user = new ApplicationUser
        {
            Id = userId,
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpper(),
            NormalizedUserName = email.ToUpper(),
            SceneName = $"IntegrationUser-{uniqueId}",
            EmailConfirmed = true,
            Role = "Member",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);

        // Ensure Member role exists
        var memberRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Member");
        if (memberRole == null)
        {
            memberRole = new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = "Member",
                NormalizedName = "MEMBER"
            };
            context.Roles.Add(memberRole);
        }

        // Assign member role
        var userRole = new IdentityUserRole<Guid>
        {
            UserId = userId,
            RoleId = memberRole.Id
        };
        context.UserRoles.Add(userRole);

        await context.SaveChangesAsync();

        // Create client with JWT token
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(userId, email, "Member", user.SceneName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (client, userId);
    }
}
