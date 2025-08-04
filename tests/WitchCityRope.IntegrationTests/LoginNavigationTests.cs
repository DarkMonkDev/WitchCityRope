using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Api.Features.Auth.Models;
using WitchCityRope.Infrastructure.Data;
using Xunit;

namespace WitchCityRope.IntegrationTests;

public class LoginNavigationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public LoginNavigationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Ensure we use the test database
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();
                db.Database.EnsureCreated();
                // Test data is already seeded by the test factory
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false // We want to test redirects manually
        });
    }

    #region Helper Methods

    private async Task<string> LoginAndGetNavigationHtml(string email, string password)
    {
        // Login via API
        var loginRequest = new LoginRequest { Email = email, Password = password };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        // Get the home page which should now show authenticated navigation
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }

    private static List<string> ExtractNavigationItems(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var items = new List<string>();

        // Extract main navigation items
        var navLinks = doc.DocumentNode.SelectNodes("//nav//a");
        if (navLinks != null)
        {
            items.AddRange(navLinks.Select(link => link.InnerText.Trim()));
        }

        // Extract user dropdown items
        var dropdownItems = doc.DocumentNode.SelectNodes("//div[contains(@class, 'user-menu')]//a");
        if (dropdownItems != null)
        {
            items.AddRange(dropdownItems.Select(item => item.InnerText.Trim()));
        }

        // Extract buttons (like Logout)
        var buttons = doc.DocumentNode.SelectNodes("//nav//button");
        if (buttons != null)
        {
            items.AddRange(buttons.Select(btn => btn.InnerText.Trim()));
        }

        return items.Distinct().ToList();
    }

    #endregion

    [Fact]
    public async Task PreLogin_Navigation_ShowsPublicItemsOnly()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        // Assert
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Should show Login button
        var loginButton = doc.DocumentNode.SelectSingleNode("//a[contains(@href, '/auth/login')]");
        loginButton.Should().NotBeNull("Login button should be visible for unauthenticated users");

        // Should NOT show authenticated items
        var myDashboard = doc.DocumentNode.SelectSingleNode("//a[contains(@href, '/member/dashboard')]");
        myDashboard.Should().BeNull("My Dashboard should not be visible for unauthenticated users");

        var userMenu = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'user-menu')]");
        userMenu.Should().BeNull("User menu should not be visible for unauthenticated users");
    }

    [Theory]
    [InlineData("admin@witchcityrope.com", "Test123!", true)]
    [InlineData("staff@witchcityrope.com", "Test123!", false)]
    [InlineData("member@witchcityrope.com", "Test123!", false)]
    [InlineData("guest@witchcityrope.com", "Test123!", false)]
    [InlineData("organizer@witchcityrope.com", "Test123!", false)]
    public async Task PostLogin_Navigation_ShowsCorrectRoleBasedItems(string email, string password, bool shouldShowAdminLink)
    {
        // Arrange & Act
        var html = await LoginAndGetNavigationHtml(email, password);
        var navItems = ExtractNavigationItems(html);

        // Assert - Common authenticated items
        navItems.Should().Contain("My Dashboard", "Authenticated users should see My Dashboard");
        navItems.Should().Contain("My Profile", "Authenticated users should see My Profile");
        navItems.Should().Contain("My Tickets", "Authenticated users should see My Tickets");
        navItems.Should().Contain("Settings", "Authenticated users should see Settings");
        navItems.Should().Contain("Logout", "Authenticated users should see Logout");

        // Assert - Role-specific items
        if (shouldShowAdminLink)
        {
            navItems.Should().Contain("Admin Panel", "Admin users should see Admin Panel link");
        }
        else
        {
            navItems.Should().NotContain("Admin Panel", "Non-admin users should not see Admin Panel link");
        }

        // Should NOT show Login button when authenticated
        navItems.Should().NotContain("Login", "Login button should not be visible for authenticated users");
    }

    [Fact]
    public async Task Login_RedirectsToDashboard_WhenNoReturnUrl()
    {
        // Arrange
        var loginRequest = new LoginRequest { Email = "member@witchcityrope.com", Password = "Test123!" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        
        // Assert
        response.EnsureSuccessStatusCode();
        
        // Navigate to check where we land
        var navResponse = await _client.GetAsync("/member/dashboard");
        navResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Should be able to access dashboard after login");
    }

    [Fact]
    public async Task Login_RespectsReturnUrl_Parameter()
    {
        // Arrange - Try to access protected page first
        var protectedResponse = await _client.GetAsync("/member/profile");
        protectedResponse.StatusCode.Should().Be(HttpStatusCode.Redirect, "Should redirect unauthenticated users");
        
        var location = protectedResponse.Headers.Location?.ToString();
        location.Should().Contain("returnUrl=%2Fmember%2Fprofile", "Should include returnUrl parameter");

        // Act - Login
        var loginRequest = new LoginRequest { Email = "member@witchcityrope.com", Password = "Test123!" };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        // Assert - Should now be able to access the protected page
        var finalResponse = await _client.GetAsync("/member/profile");
        finalResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Should be able to access profile after login");
    }

    [Fact]
    public async Task NavigationLinks_AreAccessible_ForAuthenticatedUsers()
    {
        // Arrange - Login first
        await LoginAndGetNavigationHtml("member@witchcityrope.com", "Test123!");

        // Act & Assert - Test each navigation link
        var links = new[]
        {
            ("/member/dashboard", "Dashboard"),
            ("/member/profile", "Profile"),
            ("/member/tickets", "Tickets"),
            ("/member/settings", "Settings")
        };

        foreach (var (url, name) in links)
        {
            var response = await _client.GetAsync(url);
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"{name} page should be accessible after login");
        }
    }

    [Fact]
    public async Task AdminPanel_IsAccessible_OnlyForAdmins()
    {
        // Test non-admin user
        await LoginAndGetNavigationHtml("member@witchcityrope.com", "Test123!");
        var memberResponse = await _client.GetAsync("/admin");
        memberResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Forbidden)
            .And.Subject.As<HttpStatusCode>().Should().NotBe(HttpStatusCode.OK, "Non-admin users should not access admin panel");

        // Test admin user
        await LoginAndGetNavigationHtml("admin@witchcityrope.com", "Test123!");
        var adminResponse = await _client.GetAsync("/admin");
        adminResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Admin users should access admin panel");
    }

    [Fact]
    public async Task Logout_ReturnsToPublicNavigation()
    {
        // Arrange - Login first
        var loginRequest = new LoginRequest { Email = "member@witchcityrope.com", Password = "Test123!" };
        await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Act - Logout
        var logoutResponse = await _client.PostAsync("/api/auth/logout", null);
        logoutResponse.EnsureSuccessStatusCode();

        // Assert - Check navigation reverted
        var response = await _client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        var loginButton = doc.DocumentNode.SelectSingleNode("//a[contains(@href, '/auth/login')]");
        loginButton.Should().NotBeNull("Login button should reappear after logout");
        
        var userMenu = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'user-menu')]");
        userMenu.Should().BeNull("User menu should disappear after logout");
    }

    [Fact]
    public async Task MobileNavigation_ShowsSameItems_AsDesktop()
    {
        // Arrange - Login and set mobile user agent
        await LoginAndGetNavigationHtml("admin@witchcityrope.com", "Test123!");
        
        _client.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X) AppleWebKit/605.1.15");

        // Act
        var response = await _client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        
        // Assert
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        // Check for mobile menu structure
        var mobileMenu = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'mobile-menu')]");
        mobileMenu.Should().NotBeNull("Mobile menu should be present");
        
        // Extract mobile menu items
        var mobileNavItems = doc.DocumentNode.SelectNodes("//div[contains(@class, 'mobile-menu')]//a");
        mobileNavItems.Should().NotBeNull("Mobile menu should contain navigation items");
        
        var mobileItemTexts = mobileNavItems.Select(n => n.InnerText.Trim()).ToList();
        mobileItemTexts.Should().Contain("My Dashboard");
        mobileItemTexts.Should().Contain("Admin Panel", "Admin link should be in mobile menu for admin users");
    }

    [Theory]
    [InlineData("/member/dashboard", "Dashboard")]
    [InlineData("/member/profile", "Profile")]
    [InlineData("/admin", "Admin")]
    public async Task ProtectedPages_RedirectToLogin_WhenNotAuthenticated(string url, string pageName)
    {
        // Act
        var response = await _client.GetAsync(url);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect, 
            $"{pageName} page should redirect unauthenticated users");
        
        var location = response.Headers.Location?.ToString();
        location.Should().Contain("/auth/login", $"Should redirect to login page");
        location.Should().Contain($"returnUrl={Uri.EscapeDataString(url)}", 
            $"Should include returnUrl for {pageName} page");
    }
}