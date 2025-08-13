extern alias WitchCityRopeWeb;
extern alias WitchCityRopeApi;

using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using WitchCityRope.IntegrationTests.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace WitchCityRope.IntegrationTests.Admin;

public class AdminNotesControllerTests : IClassFixture<WebApplicationFactory<WitchCityRopeWeb::Program>>, IDisposable
{
    private readonly WebApplicationFactory<WitchCityRopeWeb::Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public AdminNotesControllerTests(WebApplicationFactory<WitchCityRopeWeb::Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _output = output;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetUserNotes_WithValidUser_ReturnsEmptyList()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<WitchCityRopeUser>>();
        
        // Create a test user
        var testUser = new WitchCityRopeUser(
            encryptedLegalName: "ENCRYPTED_TestNotes",
            sceneName: WitchCityRope.Core.ValueObjects.SceneName.Create("TestNotes"),
            email: WitchCityRope.Core.ValueObjects.EmailAddress.Create("testnotes@example.com"),
            dateOfBirth: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            role: UserRole.Member
        );
        await userManager.CreateAsync(testUser, "Test123!");

        // Authenticate as admin
        await _client.AuthenticateAsAdminAsync(_factory);

        // Act
        var response = await _client.GetAsync($"/api/admin/users/{testUser.Id}/notes");

        // Assert
        response.EnsureSuccessStatusCode();
        var notes = await response.Content.ReadFromJsonAsync<List<object>>();
        Assert.NotNull(notes);
        Assert.Empty(notes);

        _output.WriteLine($"Successfully retrieved empty notes list for user {testUser.Id}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateUserNote_WithValidData_CreatesNote()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<WitchCityRopeUser>>();
        
        // Create a test user
        var testUser = new WitchCityRopeUser(
            encryptedLegalName: "ENCRYPTED_TestNotes2",
            sceneName: WitchCityRope.Core.ValueObjects.SceneName.Create("TestNotes2"),
            email: WitchCityRope.Core.ValueObjects.EmailAddress.Create("testnotes2@example.com"),
            dateOfBirth: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            role: UserRole.Member
        );
        await userManager.CreateAsync(testUser, "Test123!");

        // Authenticate as admin
        await _client.AuthenticateAsAdminAsync(_factory);

        var createDto = new
        {
            NoteType = "General",
            Content = "This is a test admin note for integration testing."
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/admin/users/{testUser.Id}/notes", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var createdNote = await response.Content.ReadFromJsonAsync<object>();
        Assert.NotNull(createdNote);

        _output.WriteLine($"Successfully created note for user {testUser.Id}");

        // Verify the note exists
        var getResponse = await _client.GetAsync($"/api/admin/users/{testUser.Id}/notes");
        getResponse.EnsureSuccessStatusCode();
        
        var notes = await getResponse.Content.ReadFromJsonAsync<List<object>>();
        Assert.NotNull(notes);
        Assert.Single(notes);

        _output.WriteLine($"Verified note was created and can be retrieved");
    }

    [Fact]
    [Trait("Category", "Integration")]  
    public async Task GetUserNotes_WithInvalidUser_ReturnsNotFound()
    {
        // Arrange
        await _client.AuthenticateAsAdminAsync(_factory);
        var invalidUserId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/admin/users/{invalidUserId}/notes");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        _output.WriteLine($"Correctly returned 404 for invalid user {invalidUserId}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateUserNote_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var testUserId = Guid.NewGuid();
        var createDto = new
        {
            NoteType = "General",
            Content = "This should fail without authentication."
        };

        // Act (without authentication)
        var response = await _client.PostAsJsonAsync($"/api/admin/users/{testUserId}/notes", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        _output.WriteLine("Correctly returned 401 when not authenticated");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetNotesStats_AsAdmin_ReturnsStatistics()
    {
        // Arrange
        await _client.AuthenticateAsAdminAsync(_factory);

        // Act
        var response = await _client.GetAsync("/api/admin/users/notes/stats");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var stats = await response.Content.ReadFromJsonAsync<object>();
        Assert.NotNull(stats);

        _output.WriteLine("Successfully retrieved notes statistics");
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}