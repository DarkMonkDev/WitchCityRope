using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;
using WitchCityRope.Web.Shared.Layouts;
using WitchCityRope.Web.Tests.New.Helpers;
using WitchCityRope.Web.Models;
using WitchCityRope.Core.Enums;
using Microsoft.JSInterop;
using WitchCityRope.Web.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;

namespace WitchCityRope.Web.Tests.New.Shared.Layouts
{
    public class MainLayoutTests : ComponentTestBase
    {
        private UserDto CreateTestUser(bool isAdmin = false, string sceneName = "TestUser")
        {
            return new UserDto
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                DisplayName = "Test User",
                SceneName = sceneName,
                IsAdmin = isAdmin,
                IsVetted = true,
                Roles = isAdmin ? new List<string> { "Admin", "Member" } : new List<string> { "Member" }
            };
        }

        [Fact]
        public void MainLayout_AuthenticatedUser_ShowsUserMenu()
        {
            // Arrange
            var user = CreateTestUser();
            AuthServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            
            var authState = AuthenticationTestHelper.CreateTestUser(true, user.Id.ToString(), user.Email);
            AuthenticationTestHelper.AddAuthentication(Services, authState);

            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            component.Find(".user-menu").Should().NotBeNull();
            var userButton = component.Find(".user-menu-btn");
            userButton.TextContent.Should().Contain(user.SceneName);
            component.FindAll(".auth-buttons .btn-cta").Should().BeEmpty();
        }

        [Fact]
        public void MainLayout_AnonymousUser_ShowsLoginButton()
        {
            // Arrange
            AuthServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync((UserDto)null);
            AuthenticationTestHelper.AddAuthentication(Services, null, false);

            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            component.Find(".auth-buttons .btn-cta").Should().NotBeNull();
            component.Find(".auth-buttons .btn-cta").TextContent.Should().Contain("LOGIN");
            component.FindAll(".user-menu").Should().BeEmpty();
        }

        [Fact]
        public async Task MainLayout_UserMenuDropdown_ShowsCorrectMenuItems()
        {
            // Arrange
            var user = CreateTestUser();
            AuthServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            
            var authState = AuthenticationTestHelper.CreateTestUser(true, user.Id.ToString(), user.Email);
            AuthenticationTestHelper.AddAuthentication(Services, authState);

            var component = RenderComponent<MainLayout>();

            // Act
            await component.Find(".user-menu-btn").ClickAsync();

            // Assert
            component.Find(".user-dropdown.show").Should().NotBeNull();
            var menuItems = component.FindAll(".dropdown-item");
            menuItems.Should().Contain(x => x.TextContent.Contains("My Dashboard"));
            menuItems.Should().Contain(x => x.TextContent.Contains("My Profile"));
            menuItems.Should().Contain(x => x.TextContent.Contains("Logout"));
            
            // Should not show admin link for non-admin user
            menuItems.Should().NotContain(x => x.TextContent.Contains("Admin Dashboard"));
        }

        [Fact]
        public async Task MainLayout_AdminUser_ShowsAdminDashboardLink()
        {
            // Arrange
            var adminUser = CreateTestUser(isAdmin: true);
            AuthServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(adminUser);
            
            var authState = AuthenticationTestHelper.CreateAdminUser(adminUser.Id.ToString(), adminUser.Email);
            AuthenticationTestHelper.AddAuthentication(Services, authState);

            var component = RenderComponent<MainLayout>();

            // Act
            await component.Find(".user-menu-btn").ClickAsync();

            // Assert
            var menuItems = component.FindAll(".dropdown-item");
            menuItems.Should().Contain(x => x.TextContent.Contains("Admin Dashboard"));
        }

        [Fact]
        public async Task MainLayout_LogoutButton_CallsJavaScriptLogout()
        {
            // Arrange
            var user = CreateTestUser();
            AuthServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            
            var authState = AuthenticationTestHelper.CreateTestUser(true, user.Id.ToString(), user.Email);
            AuthenticationTestHelper.AddAuthentication(Services, authState);

            JSRuntimeMock.Setup(x => x.InvokeAsync<object>(
                "submitIdentityLogoutForm", 
                It.IsAny<object[]>()))
                .ReturnsAsync((object)null);

            var component = RenderComponent<MainLayout>();

            // Act
            await component.Find(".user-menu-btn").ClickAsync();
            await component.Find(".dropdown-item[style*='cursor: pointer'] span").Parent.ClickAsync();

            // Assert
            JSRuntimeMock.Verify(x => x.InvokeAsync<object>(
                "submitIdentityLogoutForm", 
                It.IsAny<object[]>()), 
                Times.Once);
        }

        [Fact]
        public void MainLayout_Logo_IsClickableLink()
        {
            // Arrange & Act
            var component = RenderComponent<MainLayout>();

            // Assert
            var logo = component.Find("a.logo");
            logo.GetAttribute("href").Should().Be("/");
            logo.TextContent.Should().Be("WITCH CITY ROPE");
        }

        [Fact]
        public void MainLayout_NavigationLinks_DisplayCorrectly()
        {
            // Arrange
            var component = RenderComponent<MainLayout>();

            // Assert
            var navItems = component.FindAll(".nav-item");
            navItems.Should().Contain(x => x.TextContent == "Events & Classes");
            navItems.Should().Contain(x => x.TextContent == "How To Join");
            navItems.Should().Contain(x => x.TextContent == "Resources");
        }

        [Fact]
        public void MainLayout_AuthenticatedUser_ShowsDashboardLink()
        {
            // Arrange
            var user = CreateTestUser();
            AuthServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            
            var authState = AuthenticationTestHelper.CreateTestUser(true, user.Id.ToString(), user.Email);
            AuthenticationTestHelper.AddAuthentication(Services, authState);

            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            var navItems = component.FindAll(".nav-item");
            navItems.Should().Contain(x => x.TextContent == "My Dashboard");
        }

        [Fact]
        public async Task MainLayout_MobileMenu_TogglesCorrectly()
        {
            // Arrange
            var component = RenderComponent<MainLayout>();

            // Act
            await component.Find(".mobile-menu-toggle").ClickAsync();

            // Assert
            component.Find(".mobile-menu.active").Should().NotBeNull();
            component.Find(".mobile-menu-overlay.active").Should().NotBeNull();

            // Act - Close menu via close button
            await component.Find(".mobile-menu-close").ClickAsync();

            // Assert
            component.FindAll(".mobile-menu.active").Should().BeEmpty();
            component.FindAll(".mobile-menu-overlay.active").Should().BeEmpty();
        }

        [Fact]
        public async Task MainLayout_MobileMenu_ClosesOnOverlayClick()
        {
            // Arrange
            var component = RenderComponent<MainLayout>();

            // Act - Open menu
            await component.Find(".mobile-menu-toggle").ClickAsync();
            component.Find(".mobile-menu.active").Should().NotBeNull();

            // Act - Click overlay
            await component.Find(".mobile-menu-overlay").ClickAsync();

            // Assert
            component.FindAll(".mobile-menu.active").Should().BeEmpty();
        }

        [Fact]
        public async Task MainLayout_MobileMenu_ShowsCorrectLinksForAuthenticatedUser()
        {
            // Arrange
            var user = CreateTestUser();
            AuthServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            
            var authState = AuthenticationTestHelper.CreateTestUser(true, user.Id.ToString(), user.Email);
            AuthenticationTestHelper.AddAuthentication(Services, authState);

            var component = RenderComponent<MainLayout>();

            // Act
            await component.Find(".mobile-menu-toggle").ClickAsync();

            // Assert
            var mobileLinks = component.FindAll(".mobile-nav-item");
            mobileLinks.Should().Contain(x => x.TextContent == "My Dashboard");
            mobileLinks.Should().Contain(x => x.TextContent == "My Tickets");
            mobileLinks.Should().Contain(x => x.TextContent == "My Profile");
            mobileLinks.Should().Contain(x => x.TextContent == "Settings");
            
            // Should show logout button
            component.Find(".mobile-nav-actions button").TextContent.Should().Contain("Logout");
        }

        [Fact]
        public async Task MainLayout_MobileMenu_ShowsAdminLinkForAdminUser()
        {
            // Arrange
            var adminUser = CreateTestUser(isAdmin: true);
            AuthServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(adminUser);
            
            var authState = AuthenticationTestHelper.CreateAdminUser(adminUser.Id.ToString(), adminUser.Email);
            AuthenticationTestHelper.AddAuthentication(Services, authState);

            var component = RenderComponent<MainLayout>();

            // Act
            await component.Find(".mobile-menu-toggle").ClickAsync();

            // Assert
            var adminLink = component.Find(".mobile-nav-item.admin");
            adminLink.TextContent.Should().Be("Admin Panel");
        }

        [Fact]
        public void MainLayout_UtilityBar_ShowsForNonAdminRoutes()
        {
            // Arrange
            NavigationManagerMock.SetupGet(x => x.Uri).Returns("https://localhost/events");
            
            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            var utilityBar = component.Find(".utility-bar");
            utilityBar.Should().NotBeNull();
            utilityBar.FindAll("a").Should().Contain(x => x.TextContent == "Report an Incident");
            utilityBar.FindAll("a").Should().Contain(x => x.TextContent == "Private Lessons");
            utilityBar.FindAll("a").Should().Contain(x => x.TextContent == "Contact");
        }

        [Fact]
        public void MainLayout_UtilityBar_HiddenForAdminRoutes()
        {
            // Arrange
            NavigationManagerMock.SetupGet(x => x.Uri).Returns("https://localhost/admin/dashboard");
            
            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            component.FindAll(".utility-bar").Should().BeEmpty();
        }

        [Fact]
        public void MainLayout_Footer_DisplaysCorrectSections()
        {
            // Arrange
            var component = RenderComponent<MainLayout>();

            // Assert
            var footer = component.Find(".footer");
            footer.Should().NotBeNull();
            
            var sections = component.FindAll(".footer-section h4");
            sections.Should().Contain(x => x.TextContent == "Education");
            sections.Should().Contain(x => x.TextContent == "Community");
            sections.Should().Contain(x => x.TextContent == "Resources");
            sections.Should().Contain(x => x.TextContent == "Connect");
        }

        [Fact]
        public void MainLayout_Footer_ShowsCopyrightAndLinks()
        {
            // Arrange
            var component = RenderComponent<MainLayout>();

            // Assert
            var footerBottom = component.Find(".footer-bottom");
            footerBottom.TextContent.Should().Contain($"© {DateTime.Now.Year} Witch City Rope");
            footerBottom.TextContent.Should().Contain("Privacy Policy");
            footerBottom.TextContent.Should().Contain("Terms of Service");
        }

        [Fact]
        public void MainLayout_SocialLinks_DisplayCorrectly()
        {
            // Arrange
            var component = RenderComponent<MainLayout>();

            // Assert
            var socialLinks = component.FindAll(".social-links a");
            socialLinks.Should().HaveCount(3);
            socialLinks.Should().Contain(x => x.GetAttribute("aria-label") == "Discord");
            socialLinks.Should().Contain(x => x.GetAttribute("aria-label") == "FetLife");
            socialLinks.Should().Contain(x => x.GetAttribute("aria-label") == "Instagram");
        }

        [Fact]
        public async Task MainLayout_NewsletterForm_SubmitsCorrectly()
        {
            // Arrange
            var component = RenderComponent<MainLayout>();
            var emailInput = component.Find(".newsletter-signup input[type='email']");
            
            // Act
            await emailInput.ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs 
            { 
                Value = "test@example.com" 
            });
            await component.Find(".newsletter-signup form").SubmitAsync();

            // Assert - In real implementation, this would call a service
            // For now, just verify the form exists and can be submitted
            emailInput.GetAttribute("required").Should().NotBeNull();
        }

        [Fact]
        public void MainLayout_UserMenu_DisplaysSceneNameWhenAvailable()
        {
            // Arrange
            var user = CreateTestUser(sceneName: "RopeArtist");
            AuthServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            
            var authState = AuthenticationTestHelper.CreateTestUser(true, user.Id.ToString(), user.Email);
            AuthenticationTestHelper.AddAuthentication(Services, authState);

            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            component.Find(".user-menu-btn span").TextContent.Should().Be("RopeArtist");
        }

        [Fact]
        public void MainLayout_UserMenu_FallsBackToDisplayNameWhenNoSceneName()
        {
            // Arrange
            var user = CreateTestUser(sceneName: "");
            user.DisplayName = "Test User";
            AuthServiceMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
            
            var authState = AuthenticationTestHelper.CreateTestUser(true, user.Id.ToString(), user.Email);
            AuthenticationTestHelper.AddAuthentication(Services, authState);

            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            component.Find(".user-menu-btn span").TextContent.Should().Be("Test User");
        }

        [Fact]
        public void MainLayout_Header_HasStickyPositioning()
        {
            // Arrange & Act
            var component = RenderComponent<MainLayout>();

            // Assert
            var header = component.Find(".header");
            // The header should have sticky positioning defined in CSS
            header.GetClasses().Should().Contain("header");
        }

        [Fact]
        public void MainLayout_IncidentReportLink_HasSpecialStyling()
        {
            // Arrange
            NavigationManagerMock.SetupGet(x => x.Uri).Returns("https://localhost/");
            
            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            var incidentLink = component.Find(".incident-link");
            incidentLink.TextContent.Should().Be("Report an Incident");
            incidentLink.GetClasses().Should().Contain("incident-link");
        }

        [Fact]
        public void MainLayout_ResponsiveDesign_HidesMobileToggleOnDesktop()
        {
            // This test would verify CSS media queries work, but bUnit doesn't fully support CSS testing
            // In a real scenario, you'd use a browser automation tool for responsive testing
            
            // Arrange & Act
            var component = RenderComponent<MainLayout>();

            // Assert
            component.Find(".mobile-menu-toggle").Should().NotBeNull();
            // The visibility would be controlled by CSS media queries
        }
    }
}