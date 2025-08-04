using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Moq;

namespace WitchCityRope.Web.Tests.New.Helpers;

/// <summary>
/// Helper class for testing navigation in Blazor components
/// </summary>
public static class NavigationTestHelper
{
    /// <summary>
    /// Common route constants for testing
    /// </summary>
    public static class Routes
    {
        public const string Home = "/";
        public const string Login = "/login";
        public const string Register = "/register";
        public const string Dashboard = "/dashboard";
        public const string AdminDashboard = "/admin";
        public const string Events = "/events";
        public const string EventDetails = "/events/{0}";
        public const string Members = "/members";
        public const string MemberProfile = "/members/{0}";
        public const string MyProfile = "/profile";
        public const string Settings = "/settings";
        public const string NotFound = "/404";
        public const string Unauthorized = "/unauthorized";
    }

    /// <summary>
    /// Asserts that navigation occurred to the expected URI
    /// </summary>
    public static void AssertNavigatedTo(FakeNavigationManager navigationManager, string expectedUri)
    {
        navigationManager.History.Should().NotBeEmpty();
        navigationManager.Uri.Should().EndWith(expectedUri);
    }

    /// <summary>
    /// Asserts that navigation occurred to the expected URI with query parameters
    /// </summary>
    public static void AssertNavigatedToWithQuery(FakeNavigationManager navigationManager, 
        string expectedPath, 
        Dictionary<string, string> expectedQueryParams)
    {
        navigationManager.History.Should().NotBeEmpty();
        
        var uri = new Uri(navigationManager.Uri);
        uri.AbsolutePath.Should().Be(expectedPath);
        
        var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
        foreach (var param in expectedQueryParams)
        {
            queryParams[param.Key].Should().Be(param.Value);
        }
    }

    /// <summary>
    /// Asserts that no navigation occurred
    /// </summary>
    public static void AssertNoNavigation(FakeNavigationManager navigationManager)
    {
        navigationManager.History.Should().BeEmpty();
    }

    /// <summary>
    /// Simulates a navigation event
    /// </summary>
    public static void SimulateNavigation(FakeNavigationManager navigationManager, string uri)
    {
        navigationManager.NavigateTo(uri);
    }

    /// <summary>
    /// Creates a mock NavigationManager for unit testing
    /// </summary>
    public static Mock<NavigationManager> CreateNavigationManagerMock(string baseUri = "https://localhost/")
    {
        var mock = new Mock<NavigationManager>();
        mock.SetupGet(x => x.BaseUri).Returns(baseUri);
        mock.SetupGet(x => x.Uri).Returns(baseUri);
        
        mock.Setup(x => x.NavigateTo(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Callback<string, bool, bool>((uri, forceLoad, replace) =>
            {
                mock.SetupGet(x => x.Uri).Returns(new Uri(new Uri(baseUri), uri).ToString());
            });

        return mock;
    }

    /// <summary>
    /// Verifies navigation occurred with specific parameters
    /// </summary>
    public static void VerifyNavigation(Mock<NavigationManager> navigationManagerMock, 
        string expectedUri, 
        bool? forceLoad = null, 
        bool? replace = null)
    {
        if (forceLoad.HasValue && replace.HasValue)
        {
            navigationManagerMock.Verify(x => x.NavigateTo(expectedUri, forceLoad.Value, replace.Value), Times.Once);
        }
        else if (forceLoad.HasValue)
        {
            navigationManagerMock.Verify(x => x.NavigateTo(expectedUri, forceLoad.Value, It.IsAny<bool>()), Times.Once);
        }
        else
        {
            navigationManagerMock.Verify(x => x.NavigateTo(expectedUri, It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        }
    }

    /// <summary>
    /// Gets the current route from the navigation manager
    /// </summary>
    public static string GetCurrentRoute(NavigationManager navigationManager)
    {
        var uri = new Uri(navigationManager.Uri);
        return uri.AbsolutePath;
    }

    /// <summary>
    /// Gets query parameters from the current URI
    /// </summary>
    public static Dictionary<string, string> GetQueryParameters(NavigationManager navigationManager)
    {
        var uri = new Uri(navigationManager.Uri);
        var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
        
        var result = new Dictionary<string, string>();
        foreach (string key in queryParams.AllKeys)
        {
            if (key != null)
            {
                result[key] = queryParams[key] ?? string.Empty;
            }
        }
        
        return result;
    }

    /// <summary>
    /// Creates a URI with query parameters
    /// </summary>
    public static string CreateUriWithQuery(string path, Dictionary<string, string> queryParams)
    {
        if (queryParams == null || queryParams.Count == 0)
        {
            return path;
        }

        var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        return $"{path}?{queryString}";
    }

    /// <summary>
    /// Extension method to click a link and verify navigation
    /// </summary>
    public static void ClickLinkAndVerifyNavigation(this IRenderedFragment component, 
        string linkSelector, 
        string expectedUri)
    {
        var link = component.Find(linkSelector);
        link.Should().NotBeNull($"Link with selector '{linkSelector}' should exist");
        
        link.Click();
        
        var navigationManager = component.Services.GetService<FakeNavigationManager>();
        navigationManager.Should().NotBeNull();
        AssertNavigatedTo(navigationManager!, expectedUri);
    }

    /// <summary>
    /// Extension method to simulate navigation in a test context
    /// </summary>
    public static void NavigateToRoute(this TestContext context, string route)
    {
        var navigationManager = context.Services.GetService<FakeNavigationManager>();
        navigationManager?.NavigateTo(route);
    }
}