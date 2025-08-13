extern alias WitchCityRopeWeb;
extern alias WitchCityRopeApi;

using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace WitchCityRope.IntegrationTests;

/// <summary>
/// Comprehensive deep link validation to ensure all navigation paths work correctly
/// </summary>
public class DeepLinkValidationTests : IClassFixture<WebApplicationFactory<WitchCityRopeWeb::Program>>
{
    private readonly WebApplicationFactory<WitchCityRopeWeb::Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public DeepLinkValidationTests(WebApplicationFactory<WitchCityRopeWeb::Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task ValidateAllLinks_RecursiveCheck_ShouldFindNoBrokenLinks()
    {
        // Configuration
        var maxDepth = 3; // How deep to crawl
        var startingPages = new[] { "/", "/events" };
        var visitedLinks = new ConcurrentDictionary<string, LinkValidationResult>();
        var linksToCheck = new ConcurrentQueue<(string url, int depth, string foundOn)>();

        // Initialize with starting pages
        foreach (var page in startingPages)
        {
            linksToCheck.Enqueue((page, 0, "initial"));
        }

        // Crawl and validate
        while (linksToCheck.TryDequeue(out var item))
        {
            var (url, depth, foundOn) = item;

            // Skip if already checked
            if (visitedLinks.ContainsKey(url))
                continue;

            // Validate the link
            var result = await ValidateLink(url, foundOn);
            visitedLinks.TryAdd(url, result);

            // If successful and not too deep, extract and queue child links
            if (result.IsSuccessful && depth < maxDepth && result.Content != null)
            {
                var childLinks = ExtractLinks(result.Content, url);
                foreach (var childLink in childLinks)
                {
                    if (!visitedLinks.ContainsKey(childLink))
                    {
                        linksToCheck.Enqueue((childLink, depth + 1, url));
                    }
                }
            }
        }

        // Generate report
        var brokenLinks = visitedLinks.Values
            .Where(r => !r.IsSuccessful && !r.IsAuthRequired)
            .ToList();

        var authRequiredLinks = visitedLinks.Values
            .Where(r => r.IsAuthRequired)
            .ToList();

        // Output summary
        _output.WriteLine($"Total links checked: {visitedLinks.Count}");
        _output.WriteLine($"Successful: {visitedLinks.Values.Count(r => r.IsSuccessful)}");
        _output.WriteLine($"Auth required: {authRequiredLinks.Count}");
        _output.WriteLine($"Broken: {brokenLinks.Count}");

        if (brokenLinks.Any())
        {
            _output.WriteLine("\nBroken links found:");
            foreach (var broken in brokenLinks)
            {
                _output.WriteLine($"  - {broken.Url} (Status: {broken.StatusCode}, Found on: {broken.FoundOn})");
            }
        }

        // Assert
        brokenLinks.Should().BeEmpty("No broken links should be found");
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/events")]
    public async Task SpecificPage_AllInternalLinks_ShouldBeValid(string pageUrl)
    {
        // Get the page
        var response = await _client.GetAsync(pageUrl);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Extract all links
        var links = ExtractLinks(content, pageUrl);
        _output.WriteLine($"Found {links.Count} links on {pageUrl}");

        // Validate each link
        var invalidLinks = new List<(string link, HttpStatusCode status)>();
        
        foreach (var link in links)
        {
            var linkResponse = await _client.GetAsync(link);
            
            if (!IsValidResponse(linkResponse))
            {
                invalidLinks.Add((link, linkResponse.StatusCode));
                _output.WriteLine($"Invalid link: {link} - Status: {linkResponse.StatusCode}");
            }
        }

        // Assert
        invalidLinks.Should().BeEmpty($"Page {pageUrl} should not contain invalid links");
    }

    [Fact]
    public async Task NavigationLinks_ShouldBe_ConsistentAcrossPages()
    {
        var pages = new[] { "/", "/events" };
        var navigationLinksByPage = new Dictionary<string, List<string>>();

        foreach (var page in pages)
        {
            var response = await _client.GetAsync(page);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var navLinks = ExtractNavigationLinks(content);
                navigationLinksByPage[page] = navLinks;
            }
        }

        // Compare navigation links across pages
        var firstPageLinks = navigationLinksByPage.Values.First();
        foreach (var (page, links) in navigationLinksByPage.Skip(1))
        {
            // Core navigation items should be the same
            var coreNavItems = new[] { "/", "/events" };
            foreach (var coreItem in coreNavItems)
            {
                links.Should().Contain(coreItem, 
                    $"Page {page} should contain core navigation item {coreItem}");
            }
        }
    }

    #region Helper Methods

    private async Task<LinkValidationResult> ValidateLink(string url, string foundOn)
    {
        try
        {
            var response = await _client.GetAsync(url);
            var content = response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : null;

            return new LinkValidationResult
            {
                Url = url,
                FoundOn = foundOn,
                StatusCode = response.StatusCode,
                IsSuccessful = IsValidResponse(response),
                IsAuthRequired = IsAuthRequiredResponse(response),
                Content = content
            };
        }
        catch (Exception ex)
        {
            return new LinkValidationResult
            {
                Url = url,
                FoundOn = foundOn,
                StatusCode = HttpStatusCode.InternalServerError,
                IsSuccessful = false,
                Error = ex.Message
            };
        }
    }

    private bool IsValidResponse(HttpResponseMessage response)
    {
        return response.StatusCode == HttpStatusCode.OK ||
               (response.StatusCode == HttpStatusCode.Redirect && IsAuthRedirect(response)) ||
               response.StatusCode == HttpStatusCode.Unauthorized ||
               response.StatusCode == HttpStatusCode.Forbidden;
    }

    private bool IsAuthRequiredResponse(HttpResponseMessage response)
    {
        return response.StatusCode == HttpStatusCode.Unauthorized ||
               response.StatusCode == HttpStatusCode.Forbidden ||
               (response.StatusCode == HttpStatusCode.Redirect && IsAuthRedirect(response));
    }

    private bool IsAuthRedirect(HttpResponseMessage response)
    {
        if (response.Headers.Location != null)
        {
            var location = response.Headers.Location.ToString().ToLower();
            return location.Contains("/login") || 
                   location.Contains("/auth") || 
                   location.Contains("/account");
        }
        return false;
    }

    private List<string> ExtractLinks(string html, string currentPage)
    {
        var links = new HashSet<string>();
        
        // Extract href attributes
        var hrefPattern = @"href\s*=\s*[""']([^""'#]+)[""']";
        var matches = Regex.Matches(html, hrefPattern, RegexOptions.IgnoreCase);
        
        foreach (Match match in matches)
        {
            var link = match.Groups[1].Value.Trim();
            
            // Skip external links, javascript, mailto, etc.
            if (link.StartsWith("http://") || 
                link.StartsWith("https://") ||
                link.StartsWith("javascript:") ||
                link.StartsWith("mailto:") ||
                link.Contains("{{") ||
                link.Contains("@") ||
                string.IsNullOrWhiteSpace(link))
            {
                continue;
            }

            // Convert relative links to absolute
            if (link.StartsWith("/"))
            {
                links.Add(link);
            }
            else if (!link.Contains(":"))
            {
                // Relative link - combine with current page
                var basePath = currentPage.Contains("/") ? 
                    currentPage.Substring(0, currentPage.LastIndexOf("/")) : "";
                links.Add($"{basePath}/{link}");
            }
        }

        // Also extract Blazor navigation calls
        var blazorNavPattern = @"NavigateTo\s*\(\s*[""']([^""']+)[""']\s*\)";
        var blazorMatches = Regex.Matches(html, blazorNavPattern);
        
        foreach (Match match in blazorMatches)
        {
            var link = match.Groups[1].Value.Trim();
            if (link.StartsWith("/") && !links.Contains(link))
            {
                links.Add(link);
            }
        }

        return links.ToList();
    }

    private List<string> ExtractNavigationLinks(string html)
    {
        var links = new List<string>();
        
        // Look specifically for navigation menu links
        var navPattern = @"<nav[^>]*class\s*=\s*[""'][^""']*main-nav[^""']*[""'][^>]*>(.*?)</nav>";
        var navMatch = Regex.Match(html, navPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        
        if (navMatch.Success)
        {
            var navContent = navMatch.Groups[1].Value;
            var hrefPattern = @"href\s*=\s*[""']([^""'#]+)[""']";
            var matches = Regex.Matches(navContent, hrefPattern, RegexOptions.IgnoreCase);
            
            foreach (Match match in matches)
            {
                var link = match.Groups[1].Value.Trim();
                if (link.StartsWith("/") && !link.Contains("{{"))
                {
                    links.Add(link);
                }
            }
        }

        return links.Distinct().ToList();
    }

    #endregion

    #region Supporting Classes

    private class LinkValidationResult
    {
        public string Url { get; set; } = "";
        public string FoundOn { get; set; } = "";
        public HttpStatusCode StatusCode { get; set; }
        public bool IsSuccessful { get; set; }
        public bool IsAuthRequired { get; set; }
        public string? Content { get; set; }
        public string? Error { get; set; }
    }

    #endregion
}