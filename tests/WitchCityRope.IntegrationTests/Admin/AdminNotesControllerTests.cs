using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Infrastructure.Identity;
using Xunit;
using Xunit.Abstractions;

namespace WitchCityRope.IntegrationTests.Admin;

public class AdminNotesControllerTests : IClassFixture<WitchCityRopeWebApplicationFactory>, IDisposable
{
    private readonly WitchCityRopeWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public AdminNotesControllerTests(WitchCityRopeWebApplicationFactory factory, ITestOutputHelper output)
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
        var testUser = new WitchCityRopeUser("testnotes@example.com", "TestNotes");
        testUser.PromoteToRole(UserRole.Member);
        await userManager.CreateAsync(testUser, "Test123!");

        // Authenticate as admin
        await _client.AuthenticateAsAdminAsync();

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
        var testUser = new WitchCityRopeUser("testnotes2@example.com", "TestNotes2");
        testUser.PromoteToRole(UserRole.Member);
        await userManager.CreateAsync(testUser, "Test123!");

        // Authenticate as admin
        await _client.AuthenticateAsAdminAsync();

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
        await _client.AuthenticateAsAdminAsync();
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
        await _client.AuthenticateAsAdminAsync();

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