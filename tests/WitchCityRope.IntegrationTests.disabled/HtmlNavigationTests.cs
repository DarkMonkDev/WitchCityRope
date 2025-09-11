using FluentAssertions;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace WitchCityRope.IntegrationTests;

/// <summary>
/// Enhanced navigation tests using HtmlAgilityPack for precise HTML parsing
/// </summary>
public class HtmlNavigationTests : IClassFixture<WebApplicationFactory<WitchCityRope.Web.Program>>
{
    private readonly WebApplicationFactory<WitchCityRope.Web.Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public HtmlNavigationTests(WebApplicationFactory<WitchCityRope.Web.Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task MainNavigation_AllLinks_ShouldBeProperlyFormed()
    {
        // Act
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Find main navigation
        var mainNav = doc.DocumentNode.SelectSingleNode("//nav[contains(@class, 'main-nav')]");
        mainNav.Should().NotBeNull("Main navigation should exist");

        // Extract all navigation links
        var navLinks = mainNav.Descendants("a")
            .Select(a => new
            {
                Text = a.InnerText.Trim(),
                Href = a.GetAttributeValue("href", ""),
                Classes = a.GetAttributeValue("class", "")
            })
            .Where(link => !string.IsNullOrEmpty(link.Href))
            .ToList();

        // Verify essential links exist
        navLinks.Should().Contain(l => l.Href == "/" && l.Text.Contains("Home"), 
            "Should have Home link");
        navLinks.Should().Contain(l => l.Href == "/events", 
            "Should have Events link");
        navLinks.Should().Contain(l => l.Href.Contains("/auth/login"), 
            "Should have Sign In link");

        // All links should be properly formed
        foreach (var link in navLinks)
        {
            link.Href.Should().NotBeNullOrWhiteSpace($"Link '{link.Text}' should have href");
            link.Href.Should().Match(href => 
                href.StartsWith("/") || 
                href.StartsWith("http") || 
                href.StartsWith("#"),
                $"Link '{link.Text}' href should be absolute or relative path");
        }

        _output.WriteLine($"Found {navLinks.Count} navigation links");
    }

    [Fact]
    public async Task LandingPage_AllCTAButtons_ShouldHaveValidTargets()
    {
        // Act
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Find all CTA buttons (links with btn class)
        var ctaButtonNodes = doc.DocumentNode.SelectNodes("//a[contains(@class, 'btn')]");
        var ctaButtons = new List<dynamic>();
        
        if (ctaButtonNodes != null)
        {
            ctaButtons = ctaButtonNodes.Select(a => new
            {
                Text = a.InnerText.Trim(),
                Href = a.GetAttributeValue("href", ""),
                Classes = a.GetAttributeValue("class", "")
            })
            .Cast<dynamic>()
            .ToList();
        }

        ctaButtons.Should().NotBeEmpty("Landing page should have CTA buttons");

        // Expected CTA buttons
        var expectedCTAs = new[]
        {
            ("Browse Upcoming Classes", "/events"),
            ("Start Your Journey", "/apply"),
            ("View Full Calendar", "/events"),
            ("Join Our Community", "/apply")
        };

        foreach (var (text, href) in expectedCTAs)
        {
            var found = false;
            foreach (dynamic button in ctaButtons)
            {
                if (button.Text.Contains(text) && button.Href == href)
                {
                    found = true;
                    break;
                }
            }
            found.Should().BeTrue($"Should have '{text}' button pointing to '{href}'");
        }

        _output.WriteLine($"Found {ctaButtons.Count} CTA buttons");
    }

    [Fact]
    public async Task EventsPage_EventCards_ShouldHaveProperStructure()
    {
        // Act
        var response = await _client.GetAsync("/events");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Find event cards
        var eventCards = doc.DocumentNode.SelectNodes("//div[contains(@class, 'event-card')]");
        
        if (eventCards != null && eventCards.Any())
        {
            foreach (var card in eventCards)
            {
                // Each card should have essential elements
                var title = card.SelectSingleNode(".//h3[contains(@class, 'event-image-title')]");
                var date = card.SelectSingleNode(".//div[contains(@class, 'event-date')]");
                var price = card.SelectSingleNode(".//span[contains(@class, 'event-price')]");
                var status = card.SelectSingleNode(".//span[contains(@class, 'event-status')]");

                title.Should().NotBeNull("Event card should have a title");
                date.Should().NotBeNull("Event card should have a date");
                price.Should().NotBeNull("Event card should have a price");
                status.Should().NotBeNull("Event card should have a status");

                // Card should have click handler
                var onclick = card.GetAttributeValue("onclick", "");
                onclick.Should().NotBeNullOrEmpty("Event card should be clickable");
            }

            _output.WriteLine($"Found {eventCards.Count} event cards with proper structure");
        }
        else
        {
            _output.WriteLine("No event cards found (may be due to no test data)");
        }
    }

    [Fact]
    public async Task AllPages_NavigationConsistency_Test()
    {
        var pagesToTest = new[] { "/", "/events", "/auth/login" };
        var navigationStructures = new Dictionary<string, List<string>>();

        foreach (var page in pagesToTest)
        {
            var response = await _client.GetAsync(page);
            if (response.IsSuccessStatusCode)
            {
                var html = await response.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var mainNav = doc.DocumentNode.SelectSingleNode("//nav[contains(@class, 'main-nav')]");
                if (mainNav != null)
                {
                    var links = mainNav.Descendants("a")
                        .Select(a => a.GetAttributeValue("href", ""))
                        .Where(href => !string.IsNullOrEmpty(href) && href.StartsWith("/"))
                        .OrderBy(href => href)
                        .ToList();

                    navigationStructures[page] = links;
                }
            }
        }

        // Core navigation items should be consistent
        var coreNavItems = new[] { "/", "/events" };
        foreach (var (page, links) in navigationStructures)
        {
            foreach (var coreItem in coreNavItems)
            {
                links.Should().Contain(coreItem, 
                    $"Page {page} should contain core navigation item {coreItem}");
            }
        }
    }

    [Fact]
    public async Task ValidateAccessibility_NavigationElements()
    {
        // Act
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Check for accessibility attributes
        var navToggle = doc.DocumentNode.SelectSingleNode("//button[contains(@class, 'nav-toggle')]");
        if (navToggle != null)
        {
            var ariaLabel = navToggle.GetAttributeValue("aria-label", "");
            ariaLabel.Should().NotBeNullOrEmpty("Navigation toggle should have aria-label");
        }

        // Check for semantic HTML
        var nav = doc.DocumentNode.SelectSingleNode("//nav");
        nav.Should().NotBeNull("Should use semantic <nav> element");

        var navList = nav?.SelectSingleNode(".//ul");
        navList.Should().NotBeNull("Navigation should use list structure");

        // Check for proper link text
        var links = doc.DocumentNode.SelectNodes("//a[@href]") ?? new HtmlNodeCollection(null);
        foreach (var link in links)
        {
            var text = link.InnerText.Trim();
            var href = link.GetAttributeValue("href", "");
            
            if (!href.StartsWith("#") && !string.IsNullOrEmpty(href))
            {
                text.Should().NotBeNullOrWhiteSpace(
                    $"Link with href='{href}' should have descriptive text");
            }
        }
    }

    [Fact]
    public async Task BrokenImageLinks_ShouldNotExist()
    {
        var pagesToTest = new[] { "/", "/events" };
        var brokenImages = new List<(string page, string src)>();

        foreach (var page in pagesToTest)
        {
            var response = await _client.GetAsync(page);
            if (response.IsSuccessStatusCode)
            {
                var html = await response.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var images = doc.DocumentNode.SelectNodes("//img[@src]");
                if (images != null)
                {
                    foreach (var img in images)
                    {
                        var src = img.GetAttributeValue("src", "");
                        if (!string.IsNullOrEmpty(src) && src.StartsWith("/"))
                        {
                            var imgResponse = await _client.GetAsync(src);
                            if (!imgResponse.IsSuccessStatusCode)
                            {
                                brokenImages.Add((page, src));
                            }
                        }
                    }
                }
            }
        }

        brokenImages.Should().BeEmpty("No broken image links should exist");
    }

    [Fact]
    public async Task ResponsiveNavigation_MobileMenu_ShouldExist()
    {
        // Act
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Check for mobile menu toggle
        var mobileToggle = doc.DocumentNode.SelectSingleNode("//button[contains(@class, 'nav-toggle')]");
        mobileToggle.Should().NotBeNull("Should have mobile navigation toggle");

        // Check for hamburger menu structure
        var hamburgerLines = mobileToggle?.SelectNodes(".//span[contains(@class, 'hamburger-line')]");
        hamburgerLines?.Count.Should().Be(3, "Hamburger menu should have 3 lines");

        // Check for responsive menu classes
        var navMenu = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'nav-menu')]");
        navMenu.Should().NotBeNull("Should have nav-menu element for responsive behavior");
    }
}