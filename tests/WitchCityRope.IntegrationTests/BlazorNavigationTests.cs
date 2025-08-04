using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace WitchCityRope.IntegrationTests;

/// <summary>
/// Integration tests specifically for Blazor navigation components and client-side routing
/// </summary>
public class BlazorNavigationTests : IClassFixture<WebApplicationFactory<WitchCityRope.Web.Program>>
{
    private readonly WebApplicationFactory<WitchCityRope.Web.Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public BlazorNavigationTests(WebApplicationFactory<WitchCityRope.Web.Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                });
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    #region Navigation Component Structure Tests

    [Fact]
    public async Task NavigationComponent_ShouldRender_AllRequiredElements()
    {
        // Act
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Check main navigation structure
        content.Should().Contain("<nav", "Page should contain navigation element");
        content.Should().Contain("logo", "Navigation should contain logo");
        content.Should().Contain("Events", "Navigation should contain Events link");
        content.Should().Contain("Login", "Navigation should contain Login link");
    }

    [Fact]
    public async Task NavigationComponent_ShouldInclude_AllMainMenuItems()
    {
        // Act
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Check for all main navigation items
        var expectedMenuItems = new[]
        {
            ("Home", "href=\"/\""),
            ("Events", "href=\"/events\""),
            ("Login", "href=\"/login\"")
        };

        foreach (var (text, href) in expectedMenuItems)
        {
            content.Should().Contain(href, $"Navigation should contain link to {text}");
        }
    }

    #endregion

    #region Landing Page Navigation Tests

    [Fact]
    public async Task LandingPage_HeroSection_ShouldContain_CTAButtons()
    {
        // Act
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Hero section CTA buttons
        content.Should().Contain("hero-actions");
        content.Should().Contain("href=\"/events\"");
        content.Should().Contain("Browse Upcoming Classes");
        content.Should().Contain("href=\"/apply\"");
        content.Should().Contain("Start Your Journey");
    }

    [Fact]
    public async Task LandingPage_EventsSection_ShouldContain_EventLinks()
    {
        // Act
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Events section structure
        content.Should().Contain("upcoming-section");
        content.Should().Contain("events-preview");
        
        // Bottom CTA to view all events
        content.Should().Contain("View Full Calendar");
    }

    [Fact]
    public async Task LandingPage_CTASection_ShouldContain_JoinButton()
    {
        // Act
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Assert - CTA section
        content.Should().Contain("cta-section");
        content.Should().Contain("href=\"/apply\"");
        content.Should().Contain("Join Our Community");
    }

    #endregion

    #region Events Page Navigation Tests

    [Fact]
    public async Task EventsPage_ShouldRender_FilterControls()
    {
        // Act
        var response = await _client.GetAsync("/events");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Filter bar elements
        content.Should().Contain("filter-bar");
        content.Should().Contain("filter-tabs");
        content.Should().Contain("filter-controls");
        content.Should().Contain("search-box");
    }

    [Fact]
    public async Task EventsPage_EventCards_ShouldHave_NavigationHandlers()
    {
        // Act
        var response = await _client.GetAsync("/events");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Event cards should have click handlers
        if (content.Contains("event-card"))
        {
            content.Should().Contain("@onclick");
            content.Should().Contain("NavigateToEvent");
        }
    }

    #endregion

    #region Route Accessibility Tests

    [Theory]
    [InlineData("/", HttpStatusCode.OK)]
    [InlineData("/events", HttpStatusCode.OK)]
    [InlineData("/auth/login", HttpStatusCode.OK)]
    [InlineData("/apply", HttpStatusCode.OK, HttpStatusCode.Redirect)]
    [InlineData("/dashboard", HttpStatusCode.Redirect, HttpStatusCode.Unauthorized)]
    [InlineData("/my-tickets", HttpStatusCode.Redirect, HttpStatusCode.Unauthorized)]
    [InlineData("/profile", HttpStatusCode.Redirect, HttpStatusCode.Unauthorized)]
    public async Task PublicRoutes_ShouldReturn_ExpectedStatusCodes(string route, params HttpStatusCode[] expectedStatusCodes)
    {
        // Act
        var response = await _client.GetAsync(route);

        // Assert
        expectedStatusCodes.Should().Contain(response.StatusCode,
            $"Route {route} should return one of the expected status codes");
    }

    [Theory]
    [InlineData("/admin/events")]
    [InlineData("/admin/vetting")]
    [InlineData("/admin/users")]
    [InlineData("/admin/reports")]
    public async Task AdminRoutes_ShouldRequire_Authentication(string route)
    {
        // Act
        var response = await _client.GetAsync(route);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Redirect,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden);
    }

    #endregion

    #region Navigation Consistency Tests

    [Fact]
    public async Task AllPages_ShouldMaintain_ConsistentNavigation()
    {
        var publicPages = new[] { "/", "/events", "/auth/login" };
        var navigationElements = new HashSet<string>();

        foreach (var page in publicPages)
        {
            var response = await _client.GetAsync(page);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // All pages should have the main navigation
                content.Should().Contain("<nav", $"Page {page} should contain main navigation");
                content.Should().Contain("Witch City Rope", $"Page {page} should contain brand name");
            }
        }
    }

    #endregion

    #region Link Validation Tests

    [Fact]
    public async Task AllNavigationLinks_ShouldNot_ReturnNotFound()
    {
        // Get all pages to test
        var pagesToScan = new[] { "/", "/events" };
        var allLinks = new HashSet<string>();
        var brokenLinks = new List<(string page, string link, HttpStatusCode status)>();

        foreach (var page in pagesToScan)
        {
            var response = await _client.GetAsync(page);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var links = ExtractNavigationLinks(content);
                
                foreach (var link in links)
                {
                    if (!allLinks.Contains(link))
                    {
                        allLinks.Add(link);
                        
                        // Test the link
                        var linkResponse = await _client.GetAsync(link);
                        
                        // Check if it's a broken link (404 or 5xx errors)
                        if ((int)linkResponse.StatusCode >= 400 && (int)linkResponse.StatusCode < 600)
                        {
                            // Exception for auth-required pages
                            if (linkResponse.StatusCode != HttpStatusCode.Unauthorized &&
                                linkResponse.StatusCode != HttpStatusCode.Forbidden &&
                                !(linkResponse.StatusCode == HttpStatusCode.Redirect && IsAuthRedirect(linkResponse)))
                            {
                                brokenLinks.Add((page, link, linkResponse.StatusCode));
                            }
                        }
                    }
                }
            }
        }

        // Report any broken links
        if (brokenLinks.Any())
        {
            var report = string.Join("\n", brokenLinks.Select(bl => 
                $"Page: {bl.page}, Link: {bl.link}, Status: {bl.status}"));
            
            _output.WriteLine($"Found {brokenLinks.Count} broken links:\n{report}");
        }

        brokenLinks.Should().BeEmpty("No broken links should be found");
    }

    #endregion

    #region Helper Methods

    private List<string> ExtractNavigationLinks(string html)
    {
        var links = new List<string>();
        
        // Extract href attributes
        var hrefPattern = @"href=""(\/[^""#\s]+)""";
        var matches = System.Text.RegularExpressions.Regex.Matches(html, hrefPattern);
        
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var link = match.Groups[1].Value;
            if (!link.Contains("{{") && !link.Contains("@") && !link.Contains("*"))
            {
                links.Add(link);
            }
        }

        return links.Distinct().ToList();
    }

    private bool IsAuthRedirect(HttpResponseMessage response)
    {
        if (response.Headers.Location != null)
        {
            var location = response.Headers.Location.ToString();
            return location.Contains("/login") || location.Contains("/auth");
        }
        return false;
    }

    #endregion
}

/// <summary>
/// Custom WebApplicationFactory for Blazor integration tests
/// </summary>
public class BlazorWebApplicationFactory : WebApplicationFactory<WitchCityRope.Web.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Add any test-specific services here
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Warning);
            });
        });

        builder.UseEnvironment("Test");
        
        // Disable HTTPS redirection for tests
        builder.UseKestrel(options =>
        {
            options.ListenLocalhost(8080); // HTTP only for tests
        });
    }
}