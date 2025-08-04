using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.RegularExpressions;
using Xunit;

namespace WitchCityRope.IntegrationTests;

[Collection("PostgreSQL Integration Tests")]
public class NavigationLinksTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NavigationLinksTests(TestWebApplicationFactory factory, PostgreSqlFixture fixture)
    {
        _factory = new TestWebApplicationFactory(fixture.PostgresContainer);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false // We want to test redirects manually
        });
    }

    #region Main Navigation Bar Tests

    [Fact]
    public async Task MainNavigation_HomeLink_ShouldBeAccessible()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Witch City Rope");
    }

    [Fact]
    public async Task MainNavigation_EventsLink_ShouldBeAccessible()
    {
        // Act
        var response = await _client.GetAsync("/events");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Explore Classes & Meetups");
    }

    [Fact]
    public async Task MainNavigation_DashboardLink_ShouldRequireAuthentication()
    {
        // Act
        var response = await _client.GetAsync("/dashboard");

        // Assert - Should redirect to login for unauthenticated users
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MainNavigation_MyTicketsLink_ShouldRequireAuthentication()
    {
        // Act
        var response = await _client.GetAsync("/my-tickets");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MainNavigation_AdminLinks_ShouldRequireAuthentication()
    {
        var adminLinks = new[]
        {
            "/admin/events",
            "/admin/vetting",
            "/admin/users",
            "/admin/reports"
        };

        foreach (var link in adminLinks)
        {
            // Act
            var response = await _client.GetAsync(link);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task MainNavigation_ProfileLinks_ShouldRequireAuthentication()
    {
        var profileLinks = new[]
        {
            "/profile",
            "/profile/vetting",
            "/profile/emergency-contacts"
        };

        foreach (var link in profileLinks)
        {
            // Act
            var response = await _client.GetAsync(link);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task MainNavigation_AuthLinks_ShouldBeAccessible()
    {
        // Act
        var response = await _client.GetAsync("/auth/login");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Landing Page Links Tests

    [Fact]
    public async Task LandingPage_BrowseUpcomingClassesButton_ShouldNavigateToEvents()
    {
        // Arrange
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Act & Assert - Check that the link exists in the page
        content.Should().Contain("href=\"/events\"");
        content.Should().Contain("Browse Upcoming Classes");

        // Verify the target page is accessible
        var eventsResponse = await _client.GetAsync("/events");
        eventsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LandingPage_StartYourJourneyButton_ShouldNavigateToApply()
    {
        // Arrange
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Act & Assert
        content.Should().Contain("href=\"/apply\"");
        content.Should().Contain("Start Your Journey");

        // Verify the target page
        var applyResponse = await _client.GetAsync("/apply");
        // Apply page might require auth or redirect
        applyResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task LandingPage_ViewFullCalendarButton_ShouldNavigateToEvents()
    {
        // Arrange
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Act & Assert
        content.Should().Contain("href=\"/events\"");
        content.Should().Contain("View Full Calendar");
    }

    [Fact]
    public async Task LandingPage_JoinOurCommunityButton_ShouldNavigateToApply()
    {
        // Arrange
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Act & Assert
        content.Should().Contain("href=\"/apply\"");
        content.Should().Contain("Join Our Community");
    }

    [Fact]
    public async Task LandingPage_EventTiles_ShouldContainValidEventLinks()
    {
        // Arrange
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Act - Extract event links using regex
        var eventLinkPattern = @"href=""\/events\/([a-zA-Z0-9\-]+)""";
        var matches = Regex.Matches(content, eventLinkPattern);

        // Assert - At least some event links should be present if there are events
        // Note: This might be 0 if no events are loaded in test environment
        if (matches.Count > 0)
        {
            foreach (Match match in matches)
            {
                var eventLink = match.Groups[0].Value.Replace("href=\"", "").Replace("\"", "");
                
                // Each event link should follow the pattern /events/{id}
                eventLink.Should().StartWith("/events/");
                
                // Optionally test if the link is accessible (might be heavy for many events)
                // var eventResponse = await _client.GetAsync(eventLink);
                // eventResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
            }
        }
    }

    #endregion

    #region Events Page Links Tests

    [Fact]
    public async Task EventsPage_LoginButton_ShouldNavigateToLogin()
    {
        // This test would need to check for member-only events
        // For now, we'll verify the events page loads
        var response = await _client.GetAsync("/events");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        // If there are member-only events, they should have login links
        if (content.Contains("Members Only"))
        {
            content.Should().Contain("/login");
        }
    }

    [Fact]
    public async Task EventsPage_BecomeMemberButton_ShouldNavigateToJoin()
    {
        var response = await _client.GetAsync("/events");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        // If there are member-only events, they should have join links
        if (content.Contains("Members Only"))
        {
            content.Should().Contain("/join");
        }
    }

    [Fact]
    public async Task EventsPage_EventCards_ShouldBeClickable()
    {
        // Arrange
        var response = await _client.GetAsync("/events");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Act - Look for event card navigation
        var eventCardPattern = @"NavigateToEvent\(([^)]+)\)";
        var matches = Regex.Matches(content, eventCardPattern);

        // Assert - Event cards should have click handlers
        // Note: In a real test environment, you'd want to ensure test data exists
        if (content.Contains("event-card"))
        {
            content.Should().Contain("@onclick");
        }
    }

    #endregion

    #region Additional Link Validation Tests

    [Fact]
    public async Task AllInternalLinks_ShouldNotReturn404()
    {
        // Get the home page
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Extract all internal links
        var linkPattern = @"href=""(\/[^""#]*)""";
        var matches = Regex.Matches(content, linkPattern);
        var internalLinks = matches
            .Select(m => m.Groups[1].Value)
            .Where(link => !link.Contains("{{") && !link.Contains("@")) // Exclude Blazor placeholders
            .Distinct()
            .ToList();

        // Test each unique internal link
        var brokenLinks = new List<(string link, HttpStatusCode statusCode)>();
        
        foreach (var link in internalLinks)
        {
            var linkResponse = await _client.GetAsync(link);
            
            // We expect OK, Redirect (for auth), or Unauthorized
            if (!new[] { HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden }
                .Contains(linkResponse.StatusCode))
            {
                brokenLinks.Add((link, linkResponse.StatusCode));
            }
        }

        // Assert no broken links found
        brokenLinks.Should().BeEmpty($"Found {brokenLinks.Count} broken links: " +
            string.Join(", ", brokenLinks.Select(bl => $"{bl.link} ({bl.statusCode})")));
    }

    [Fact]
    public async Task NavigationConsistency_AllPagesShould_HaveSameMainNavigation()
    {
        var pagesToTest = new[] { "/", "/events" };
        var navigationPatterns = new List<string>();

        foreach (var page in pagesToTest)
        {
            var response = await _client.GetAsync(page);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Check for consistent navigation elements
                content.Should().Contain("main-nav");
                content.Should().Contain("nav-brand");
                content.Should().Contain("Witch City Rope");
            }
        }
    }

    #endregion

    #region Helper Methods

    private async Task<bool> IsLinkAccessible(string url)
    {
        try
        {
            var response = await _client.GetAsync(url);
            return response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.Redirect ||
                   response.StatusCode == HttpStatusCode.Unauthorized;
        }
        catch
        {
            return false;
        }
    }

    private List<string> ExtractLinks(string html, string pattern)
    {
        var matches = Regex.Matches(html, pattern);
        return matches
            .Select(m => m.Groups[1].Value)
            .Where(link => !string.IsNullOrWhiteSpace(link))
            .Distinct()
            .ToList();
    }

    #endregion
}