using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using WitchCityRope.Web.Shared.Layouts;
using WitchCityRope.Web.Tests.Helpers;

namespace WitchCityRope.Web.Tests.Shared.Layouts
{
    public class MainLayoutTests : ComponentTestBase
    {
        [Fact]
        public void MainLayout_AuthenticatedUser_ShowsUserMenu()
        {
            // Arrange
            AuthenticationTestContext.SetupAuthenticatedUser(this, "test-user", "test@example.com", "TestUser");

            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            component.Find(".user-menu").Should().NotBeNull();
            component.Find(".user-name").TextContent.Should().Be("TestUser");
            component.FindAll(".login-button").Should().BeEmpty();
        }

        [Fact]
        public void MainLayout_AnonymousUser_ShowsLoginButton()
        {
            // Arrange
            AuthenticationTestContext.SetupAnonymousUser(this);

            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            component.Find(".login-button").Should().NotBeNull();
            component.Find(".login-button").TextContent.Should().Contain("Sign In");
            component.FindAll(".user-menu").Should().BeEmpty();
        }

        [Fact]
        public async Task MainLayout_UserMenuDropdown_ShowsMenuItems()
        {
            // Arrange
            AuthenticationTestContext.SetupAuthenticatedUser(this, "test-user", "test@example.com", "TestUser");
            var component = RenderComponent<MainLayout>();

            // Act
            await component.Find(".user-menu-toggle").ClickAsync();

            // Assert
            var menuItems = component.FindAll(".dropdown-item");
            menuItems.Should().Contain(x => x.TextContent.Contains("Dashboard"));
            menuItems.Should().Contain(x => x.TextContent.Contains("My Tickets"));
            menuItems.Should().Contain(x => x.TextContent.Contains("Profile"));
            menuItems.Should().Contain(x => x.TextContent.Contains("Sign Out"));
        }

        [Fact]
        public async Task MainLayout_SignOutButton_CallsLogout()
        {
            // Arrange
            AuthenticationTestContext.SetupAuthenticatedUser(this, "test-user", "test@example.com", "TestUser");
            AuthServiceMock.Setup(x => x.LogoutAsync()).Returns(Task.CompletedTask);
            
            var component = RenderComponent<MainLayout>();

            // Act
            await component.Find(".user-menu-toggle").ClickAsync();
            await component.Find(".sign-out-button").ClickAsync();

            // Assert
            AuthServiceMock.Verify(x => x.LogoutAsync(), Times.Once);
            VerifyNavigation("/");
        }

        [Fact]
        public void MainLayout_Logo_NavigatesToHome()
        {
            // Arrange
            var component = RenderComponent<MainLayout>();

            // Act
            component.Find(".logo").Click();

            // Assert
            VerifyNavigation("/");
        }

        [Fact]
        public void MainLayout_NavigationLinks_DisplayCorrectly()
        {
            // Arrange
            var component = RenderComponent<MainLayout>();

            // Assert
            var navLinks = component.FindAll(".nav-link");
            navLinks.Should().Contain(x => x.TextContent == "Events");
            navLinks.Should().Contain(x => x.TextContent == "About");
            navLinks.Should().Contain(x => x.TextContent == "Safety");
        }

        [Fact]
        public void MainLayout_AdminUser_ShowsAdminLink()
        {
            // Arrange
            AuthenticationTestContext.SetupAuthenticatedUser(this, 
                "admin-user", "admin@example.com", "AdminUser", new[] { "Admin" });

            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            component.Find(".admin-link").Should().NotBeNull();
            component.Find(".admin-link").TextContent.Should().Be("Admin");
        }

        [Fact]
        public void MainLayout_NonAdminUser_HidesAdminLink()
        {
            // Arrange
            AuthenticationTestContext.SetupAuthenticatedUser(this, 
                "test-user", "test@example.com", "TestUser", new[] { "Member" });

            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            component.FindAll(".admin-link").Should().BeEmpty();
        }

        [Fact]
        public async Task MainLayout_MobileMenu_TogglesCorrectly()
        {
            // Arrange
            var component = RenderComponent<MainLayout>();

            // Act
            await component.Find(".mobile-menu-toggle").ClickAsync();

            // Assert
            component.Find(".mobile-menu").GetClasses().Should().Contain("open");
            component.Find(".mobile-menu-overlay").Should().NotBeNull();

            // Act - Close menu
            await component.Find(".mobile-menu-close").ClickAsync();

            // Assert
            component.Find(".mobile-menu").GetClasses().Should().NotContain("open");
        }

        [Fact]
        public async Task MainLayout_MobileMenu_ClosesOnNavigation()
        {
            // Arrange
            var component = RenderComponent<MainLayout>();

            // Act - Open menu
            await component.Find(".mobile-menu-toggle").ClickAsync();
            component.Find(".mobile-menu").GetClasses().Should().Contain("open");

            // Act - Click a nav link
            await component.Find(".mobile-menu .nav-link[href='/events']").ClickAsync();

            // Assert
            component.Find(".mobile-menu").GetClasses().Should().NotContain("open");
        }

        [Fact]
        public void MainLayout_Footer_DisplaysCorrectInfo()
        {
            // Arrange
            var component = RenderComponent<MainLayout>();

            // Assert
            var footer = component.Find(".main-footer");
            footer.Should().NotBeNull();
            footer.TextContent.Should().Contain($"© {DateTime.Now.Year} Witch City Rope");
            
            var footerLinks = component.FindAll(".footer-link");
            footerLinks.Should().Contain(x => x.TextContent == "Privacy Policy");
            footerLinks.Should().Contain(x => x.TextContent == "Terms of Service");
            footerLinks.Should().Contain(x => x.TextContent == "Code of Conduct");
        }

        [Fact]
        public void MainLayout_NotificationBell_ShowsForAuthenticatedUsers()
        {
            // Arrange
            AuthenticationTestContext.SetupAuthenticatedUser(this);

            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            component.Find(".notification-bell").Should().NotBeNull();
        }

        [Fact]
        public void MainLayout_NotificationBadge_ShowsUnreadCount()
        {
            // Arrange
            AuthenticationTestContext.SetupAuthenticatedUser(this);
            var notificationServiceMock = new Mock<INotificationService>();
            notificationServiceMock.Setup(x => x.GetUnreadCountAsync()).ReturnsAsync(5);
            Services.AddSingleton(notificationServiceMock.Object);

            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            component.Find(".notification-badge").TextContent.Should().Be("5");
        }

        [Fact]
        public async Task MainLayout_ScrollToTop_AppearsOnScroll()
        {
            // Arrange
            var component = RenderComponent<MainLayout>();

            // Act - Simulate scroll
            await JSRuntimeMock.Object.InvokeAsync<object>("scrollTo", 0, 500);
            StateHasChanged();

            // Assert
            component.Find(".scroll-to-top").Should().NotBeNull();
            component.Find(".scroll-to-top").GetClasses().Should().Contain("visible");
        }

        [Fact]
        public void MainLayout_AnnouncementBanner_ShowsWhenActive()
        {
            // Arrange
            var announcementServiceMock = new Mock<IAnnouncementService>();
            announcementServiceMock.Setup(x => x.GetActiveBannerAsync())
                .ReturnsAsync(new AnnouncementBanner 
                { 
                    Message = "Site maintenance scheduled for tonight",
                    Type = "warning"
                });
            Services.AddSingleton(announcementServiceMock.Object);

            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            component.Find(".announcement-banner").Should().NotBeNull();
            component.Find(".announcement-banner").TextContent.Should().Contain("Site maintenance");
            component.Find(".announcement-banner").GetClasses().Should().Contain("banner-warning");
        }

        [Fact]
        public void MainLayout_BreadcrumbNavigation_ShowsOnSubPages()
        {
            // Arrange
            NavigationManagerMock.SetupProperty(x => x.Uri, "https://localhost/events/123");
            
            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            var breadcrumbs = component.FindAll(".breadcrumb-item");
            breadcrumbs.Should().HaveCountGreaterThan(1);
            breadcrumbs[0].TextContent.Should().Be("Home");
            breadcrumbs[1].TextContent.Should().Be("Events");
        }

        [Fact]
        public async Task MainLayout_DarkModeToggle_SwitchesTheme()
        {
            // Arrange
            var component = RenderComponent<MainLayout>();

            // Act
            await component.Find(".theme-toggle").ClickAsync();

            // Assert
            JSRuntimeMock.Verify(x => x.InvokeAsync<object>(
                "toggleTheme", 
                It.IsAny<object[]>()), 
                Times.Once);
            
            component.Find(".theme-toggle").GetClasses().Should().Contain("dark-mode");
        }
    }

    // Mock interfaces for testing
    public interface IAnnouncementService
    {
        Task<AnnouncementBanner> GetActiveBannerAsync();
    }

    public class AnnouncementBanner
    {
        public string Message { get; set; }
        public string Type { get; set; }
    }
}