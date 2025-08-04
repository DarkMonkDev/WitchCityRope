using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Bunit;
using WitchCityRope.Web.Shared.Components.Navigation;
using WitchCityRope.Web.Tests.Helpers;
using WitchCityRope.Web.Services;

namespace WitchCityRope.Web.Tests.Shared.Components.Navigation
{
    public class MainNavTests : ComponentTestBase
    {
        [Fact]
        public void MainNav_DefaultLinks_AlwaysVisible()
        {
            // Act
            var component = RenderComponent<MainNav>();

            // Assert
            var navLinks = component.FindAll(".nav-link");
            navLinks.Should().Contain(x => x.TextContent == "Events" && x.GetAttribute("href") == "/events");
            navLinks.Should().Contain(x => x.TextContent == "About" && x.GetAttribute("href") == "/about");
            navLinks.Should().Contain(x => x.TextContent == "Safety" && x.GetAttribute("href") == "/safety");
        }

        [Fact]
        public void MainNav_AuthenticatedUser_ShowsMemberLinks()
        {
            // Arrange
            AuthenticationTestContext.SetupAuthenticatedUser(this);

            // Act
            var component = RenderComponent<MainNav>();

            // Assert
            var navLinks = component.FindAll(".nav-link");
            navLinks.Should().Contain(x => x.TextContent == "My Tickets" && x.GetAttribute("href") == "/member/tickets");
            navLinks.Should().Contain(x => x.TextContent == "Dashboard" && x.GetAttribute("href") == "/member/dashboard");
        }

        [Fact]
        public void MainNav_AnonymousUser_HidesMemberLinks()
        {
            // Arrange
            AuthenticationTestContext.SetupAnonymousUser(this);

            // Act
            var component = RenderComponent<MainNav>();

            // Assert
            var navLinks = component.FindAll(".nav-link");
            navLinks.Should().NotContain(x => x.TextContent == "My Tickets");
            navLinks.Should().NotContain(x => x.TextContent == "Dashboard");
        }

        [Fact]
        public void MainNav_AdminUser_ShowsAdminDropdown()
        {
            // Arrange
            AuthenticationTestContext.SetupAuthenticatedUser(this, 
                "admin-user", "admin@example.com", "AdminUser", new[] { "Admin" });

            // Act
            var component = RenderComponent<MainNav>();

            // Assert
            component.Find(".admin-dropdown").Should().NotBeNull();
            component.Find(".admin-dropdown-toggle").TextContent.Should().Be("Admin");
        }

        [Fact]
        public async Task MainNav_AdminDropdown_ShowsAdminLinks()
        {
            // Arrange
            AuthenticationTestContext.SetupAuthenticatedUser(this, 
                "admin-user", "admin@example.com", "AdminUser", new[] { "Admin" });
            var component = RenderComponent<MainNav>();

            // Act
            await component.Find(".admin-dropdown-toggle").ClickAsync();

            // Assert
            var dropdownItems = component.FindAll(".admin-dropdown-item");
            dropdownItems.Should().Contain(x => x.TextContent == "Event Management");
            dropdownItems.Should().Contain(x => x.TextContent == "Vetting Queue");
            dropdownItems.Should().Contain(x => x.TextContent == "User Management");
            dropdownItems.Should().Contain(x => x.TextContent == "Reports");
        }

        [Fact]
        public void MainNav_ActiveLink_HasActiveClass()
        {
            // Arrange
            NavigationManagerMock.SetupProperty(x => x.Uri, "https://localhost/events");

            // Act
            var component = RenderComponent<MainNav>();

            // Assert
            var eventsLink = component.FindAll(".nav-link")
                .First(x => x.GetAttribute("href") == "/events");
            eventsLink.GetClasses().Should().Contain("active");
        }

        [Fact]
        public void MainNav_MobileView_ShowsHamburgerMenu()
        {
            // Act
            var component = RenderComponent<MainNav>();

            // Assert
            component.Find(".mobile-menu-toggle").Should().NotBeNull();
            component.Find(".mobile-menu-toggle").GetAttribute("aria-label").Should().Be("Toggle navigation menu");
        }

        [Fact]
        public async Task MainNav_MobileMenu_TogglesVisibility()
        {
            // Arrange
            var component = RenderComponent<MainNav>();

            // Assert - Initially closed
            component.Find(".mobile-nav").GetClasses().Should().NotContain("open");

            // Act - Open menu
            await component.Find(".mobile-menu-toggle").ClickAsync();

            // Assert - Menu open
            component.Find(".mobile-nav").GetClasses().Should().Contain("open");
            component.Find(".mobile-overlay").Should().NotBeNull();

            // Act - Close menu via overlay
            await component.Find(".mobile-overlay").ClickAsync();

            // Assert - Menu closed
            component.Find(".mobile-nav").GetClasses().Should().NotContain("open");
        }

        [Fact]
        public async Task MainNav_MobileMenu_ClosesOnLinkClick()
        {
            // Arrange
            var component = RenderComponent<MainNav>();
            
            // Open menu
            await component.Find(".mobile-menu-toggle").ClickAsync();
            component.Find(".mobile-nav").GetClasses().Should().Contain("open");

            // Act - Click a nav link
            await component.Find(".mobile-nav .nav-link[href='/events']").ClickAsync();

            // Assert - Menu should close
            component.Find(".mobile-nav").GetClasses().Should().NotContain("open");
        }

        [Fact]
        public void MainNav_VettingBadge_ShowsForPendingUsers()
        {
            // Arrange
            AuthenticationTestContext.SetupAuthenticatedUser(this);
            var vettingServiceMock = new Mock<IVettingService>();
            vettingServiceMock.Setup(x => x.GetMyVettingStatusAsync()).ReturnsAsync(new VettingStatus { Status = "Pending" });
            Services.AddSingleton(vettingServiceMock.Object);

            // Act
            var component = RenderComponent<MainNav>();

            // Assert
            var vettingBadge = component.Find(".vetting-badge");
            vettingBadge.Should().NotBeNull();
            vettingBadge.TextContent.Should().Contain("Vetting Pending");
            vettingBadge.GetClasses().Should().Contain("badge-warning");
        }

        [Fact]
        public void MainNav_SearchBar_VisibleOnDesktop()
        {
            // Act
            var component = RenderComponent<MainNav>();

            // Assert
            var searchBar = component.Find(".nav-search");
            searchBar.Should().NotBeNull();
            searchBar.Find("input[type='search']").GetAttribute("placeholder")
                .Should().Be("Search events...");
        }

        [Fact]
        public async Task MainNav_SearchBar_SubmitsSearch()
        {
            // Arrange
            var component = RenderComponent<MainNav>();

            // Act
            var searchInput = component.Find(".nav-search input");
            await searchInput.ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs 
            { 
                Value = "rope basics" 
            });
            await searchInput.KeyPressAsync(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs 
            { 
                Key = "Enter" 
            });

            // Assert
            VerifyNavigation("/events?search=rope%20basics");
        }

        [Fact]
        public void MainNav_NotificationIcon_ShowsUnreadCount()
        {
            // Arrange
            AuthenticationTestContext.SetupAuthenticatedUser(this);
            var notificationServiceMock = new Mock<INotificationService>();
            notificationServiceMock.Setup(x => x.GetNotificationsAsync()).ReturnsAsync(new List<UserNotification> 
            {
                new UserNotification { Id = Guid.NewGuid(), Title = "Test", Message = "Test", IsRead = false },
                new UserNotification { Id = Guid.NewGuid(), Title = "Test2", Message = "Test2", IsRead = false },
                new UserNotification { Id = Guid.NewGuid(), Title = "Test3", Message = "Test3", IsRead = false }
            });
            Services.AddSingleton(notificationServiceMock.Object);

            // Act
            var component = RenderComponent<MainNav>();

            // Assert
            var notificationBadge = component.Find(".notification-count");
            notificationBadge.Should().NotBeNull();
            notificationBadge.TextContent.Should().Be("3");
        }

        [Fact]
        public void MainNav_EventsDropdown_ShowsCategories()
        {
            // Arrange
            var component = RenderComponent<MainNav>();

            // Act
            component.Find(".events-dropdown-toggle").MouseOver();

            // Assert
            var dropdownItems = component.FindAll(".events-dropdown-item");
            dropdownItems.Should().Contain(x => x.TextContent == "All Events");
            dropdownItems.Should().Contain(x => x.TextContent == "Workshops");
            dropdownItems.Should().Contain(x => x.TextContent == "Classes");
            dropdownItems.Should().Contain(x => x.TextContent == "Socials");
            dropdownItems.Should().Contain(x => x.TextContent == "Special Events");
        }

        [Fact]
        public void MainNav_AccessibilityFeatures_Present()
        {
            // Act
            var component = RenderComponent<MainNav>();

            // Assert
            // Skip to content link
            var skipLink = component.Find(".skip-to-content");
            skipLink.Should().NotBeNull();
            skipLink.GetAttribute("href").Should().Be("#main-content");

            // ARIA labels
            component.Find("nav").GetAttribute("aria-label").Should().Be("Main navigation");
            
            // Keyboard navigation indicators
            var navLinks = component.FindAll(".nav-link");
            navLinks.Should().AllSatisfy(link => 
                link.GetAttribute("tabindex").Should().BeNull()); // Should be naturally focusable
        }

        [Fact]
        public void MainNav_StickyBehavior_HasCorrectClasses()
        {
            // Act
            var component = RenderComponent<MainNav>();

            // Assert
            var nav = component.Find(".main-nav");
            nav.GetClasses().Should().Contain("sticky-nav");
            nav.GetAttribute("data-scroll-threshold").Should().Be("100");
        }

        [Fact]
        public async Task MainNav_QuickLinks_ForAuthenticatedUsers()
        {
            // Arrange
            AuthenticationTestContext.SetupAuthenticatedUser(this);
            var component = RenderComponent<MainNav>();

            // Act
            await component.Find(".quick-links-toggle").ClickAsync();

            // Assert
            var quickLinks = component.FindAll(".quick-link");
            quickLinks.Should().Contain(x => x.TextContent == "Upcoming Events");
            quickLinks.Should().Contain(x => x.TextContent == "My Registrations");
            quickLinks.Should().Contain(x => x.TextContent == "Profile Settings");
        }
    }
}