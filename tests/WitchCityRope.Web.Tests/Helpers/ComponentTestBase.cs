using System;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Moq;
using WitchCityRope.Web.Services;
using Microsoft.JSInterop;

namespace WitchCityRope.Web.Tests.Helpers
{
    /// <summary>
    /// Base class for component tests with common setup
    /// </summary>
    public abstract class ComponentTestBase : TestContext, IDisposable
    {
        protected Mock<NavigationManager> NavigationManagerMock { get; private set; }
        protected Mock<IJSRuntime> JSRuntimeMock { get; private set; }
        protected Mock<IAuthService> AuthServiceMock { get; private set; }
        protected Mock<ILocalStorageService> LocalStorageMock { get; private set; }
        protected Mock<INotificationService> NotificationServiceMock { get; private set; }

        protected ComponentTestBase()
        {
            SetupDefaultServices();
        }

        /// <summary>
        /// Sets up default services that most component tests will need
        /// </summary>
        protected virtual void SetupDefaultServices()
        {
            // Setup Navigation Manager
            NavigationManagerMock = new Mock<NavigationManager>();
            NavigationManagerMock.SetupProperty(x => x.Uri, "https://localhost/");
            Services.AddSingleton(NavigationManagerMock.Object);

            // Setup JS Runtime
            JSRuntimeMock = new Mock<IJSRuntime>();
            Services.AddSingleton(JSRuntimeMock.Object);

            // Setup Auth Service
            AuthServiceMock = AuthenticationTestContext.CreateMockAuthService();
            Services.AddSingleton(AuthServiceMock.Object);

            // Setup Local Storage
            LocalStorageMock = ServiceMockHelpers.CreateMockLocalStorageService();
            Services.AddSingleton(LocalStorageMock.Object);

            // Setup Notification Service
            NotificationServiceMock = ServiceMockHelpers.CreateMockNotificationService();
            Services.AddSingleton(NotificationServiceMock.Object);

            // Add Syncfusion service (since many components use Syncfusion)
            Services.AddSyncfusionBlazor();
        }

        /// <summary>
        /// Helper method to wait for async operations to complete
        /// </summary>
        protected async Task WaitForAsync(Func<bool> condition, int timeoutMs = 1000)
        {
            var timeout = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (!condition() && DateTime.UtcNow < timeout)
            {
                await Task.Delay(50);
            }
        }

        /// <summary>
        /// Helper to find element and ensure it exists
        /// </summary>
        protected IElement FindElement(IRenderedFragment component, string cssSelector)
        {
            var element = component.Find(cssSelector);
            if (element == null)
            {
                throw new ElementNotFoundException($"Element with selector '{cssSelector}' not found");
            }
            return element;
        }

        /// <summary>
        /// Helper to find elements and ensure at least one exists
        /// </summary>
        protected IReadOnlyList<IElement> FindElements(IRenderedFragment component, string cssSelector)
        {
            var elements = component.FindAll(cssSelector);
            if (elements.Count == 0)
            {
                throw new ElementNotFoundException($"No elements found with selector '{cssSelector}'");
            }
            return elements;
        }

        /// <summary>
        /// Verifies navigation was called with the expected URL
        /// </summary>
        protected void VerifyNavigation(string expectedUrl, bool forceLoad = false)
        {
            NavigationManagerMock.Verify(
                x => x.NavigateTo(expectedUrl, forceLoad),
                Times.Once,
                $"Expected navigation to '{expectedUrl}' with forceLoad={forceLoad}"
            );
        }

        /// <summary>
        /// Verifies a notification was shown
        /// </summary>
        protected void VerifyNotification(string expectedMessage, NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Success:
                    NotificationServiceMock.Verify(x => x.ShowSuccessAsync(expectedMessage), Times.Once);
                    break;
                case NotificationType.Error:
                    NotificationServiceMock.Verify(x => x.ShowErrorAsync(expectedMessage), Times.Once);
                    break;
                case NotificationType.Warning:
                    NotificationServiceMock.Verify(x => x.ShowWarningAsync(expectedMessage), Times.Once);
                    break;
                case NotificationType.Info:
                    NotificationServiceMock.Verify(x => x.ShowInfoAsync(expectedMessage), Times.Once);
                    break;
            }
        }

        public new void Dispose()
        {
            base.Dispose();
        }
    }

    public enum NotificationType
    {
        Success,
        Error,
        Warning,
        Info
    }

    public class ElementNotFoundException : Exception
    {
        public ElementNotFoundException(string message) : base(message) { }
    }
}