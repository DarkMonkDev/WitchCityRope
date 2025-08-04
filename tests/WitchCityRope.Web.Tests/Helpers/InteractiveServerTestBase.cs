using System;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Moq;

namespace WitchCityRope.Web.Tests.Helpers
{
    /// <summary>
    /// Base class for testing components that use InteractiveServer render mode.
    /// This class provides additional setup to handle interactive server components in tests.
    /// </summary>
    public abstract class InteractiveServerTestBase : ComponentTestBase
    {
        protected InteractiveServerTestBase() : base()
        {
        }

        protected override void SetupDefaultServices()
        {
            base.SetupDefaultServices();
            
            // Add additional services needed for interactive server components
            ConfigureInteractiveServerServices();
        }

        /// <summary>
        /// Configures services specific to Interactive Server components
        /// </summary>
        private void ConfigureInteractiveServerServices()
        {
            // For unit tests, we mock the circuit/SignalR behavior
            // Components will effectively run in a simulated interactive mode
            
            // Add HttpContext accessor mock if needed
            var httpContextAccessorMock = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            Services.AddSingleton(httpContextAccessorMock.Object);
        }

        /// <summary>
        /// Renders a component and strips the rendermode attribute for testing
        /// </summary>
        protected IRenderedComponent<TComponent> RenderInteractiveComponent<TComponent>(
            params ComponentParameter[] parameters) where TComponent : IComponent
        {
            // In tests, components run in static mode regardless of their @rendermode directive
            // The bunit framework handles this automatically
            return RenderComponent<TComponent>(parameters);
        }

        /// <summary>
        /// Helper to simulate server-side state changes
        /// </summary>
        protected async Task SimulateServerStateChange(Func<Task> action)
        {
            await action();
            // Give the component time to re-render after state change
            await Task.Delay(50);
        }
    }
}