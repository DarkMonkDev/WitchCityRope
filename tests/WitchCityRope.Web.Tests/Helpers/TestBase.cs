using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Syncfusion.Blazor;
using WitchCityRope.Web.Services;
using Xunit;

namespace WitchCityRope.Web.Tests.Helpers;

/// <summary>
/// Base class for all Blazor component tests providing common setup and utilities
/// </summary>
public abstract class TestBase : TestContext, IDisposable
{
    protected Mock<ILogger<TestBase>> LoggerMock { get; }
    protected FakeNavigationManager NavigationManager { get; }
    protected IServiceProvider ServiceProvider { get; }

    protected TestBase()
    {
        // Initialize mocks
        LoggerMock = new Mock<ILogger<TestBase>>();

        // Configure test services
        Services.AddLogging();
        // Services.AddSyncfusionBlazor(options => { }); // Commented out due to ambiguous reference
        
        // Add fake navigation manager
        // NavigationManager will be set up differently since it's from Services
        
        // Add common test services
        ConfigureTestServices(Services);
        
        // Build service provider
        ServiceProvider = Services.BuildServiceProvider();
    }

    /// <summary>
    /// Override this method to configure additional test services
    /// </summary>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
        // Default implementations of common services
        services.AddScoped<IAuthService>(sp => Mock.Of<IAuthService>());
        services.AddScoped<INotificationService>(sp => Mock.Of<INotificationService>());
        services.AddScoped<IApiClient>(sp => Mock.Of<IApiClient>());
    }

    /// <summary>
    /// Creates a mock logger for the specified type
    /// </summary>
    protected Mock<ILogger<T>> CreateLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    /// <summary>
    /// Renders a component with parameters
    /// </summary>
    protected IRenderedComponent<TComponent> RenderComponent<TComponent>(
        params ComponentParameter[] parameters) where TComponent : IComponent
    {
        return base.RenderComponent<TComponent>(parameters);
    }

    /// <summary>
    /// Asserts that a component contains the expected text
    /// </summary>
    protected void AssertContainsText(IRenderedFragment component, string expectedText)
    {
        component.Markup.Should().Contain(expectedText);
    }

    /// <summary>
    /// Asserts that a component does not contain the specified text
    /// </summary>
    protected void AssertDoesNotContainText(IRenderedFragment component, string unexpectedText)
    {
        component.Markup.Should().NotContain(unexpectedText);
    }

    /// <summary>
    /// Finds an element by CSS selector and asserts it exists
    /// </summary>
    protected AngleSharp.Dom.IElement FindElement(IRenderedFragment component, string cssSelector)
    {
        var element = component.Find(cssSelector);
        element.Should().NotBeNull($"Element with selector '{cssSelector}' should exist");
        return element;
    }

    /// <summary>
    /// Finds all elements by CSS selector
    /// </summary>
    protected IReadOnlyList<AngleSharp.Dom.IElement> FindElements(IRenderedFragment component, string cssSelector)
    {
        return component.FindAll(cssSelector);
    }

    /// <summary>
    /// Waits for an assertion to pass with retry logic
    /// </summary>
    protected async Task WaitForAssertion(Action assertion, TimeSpan? timeout = null)
    {
        var timeoutValue = timeout ?? TimeSpan.FromSeconds(1);
        var endTime = DateTime.UtcNow.Add(timeoutValue);

        while (DateTime.UtcNow < endTime)
        {
            try
            {
                assertion();
                return;
            }
            catch
            {
                if (DateTime.UtcNow >= endTime)
                    throw;
                
                await Task.Delay(50);
            }
        }
    }

    /// <summary>
    /// Disposes of test resources
    /// </summary>
    public new void Dispose()
    {
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}