using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Bunit;
using WitchCityRope.Web.Shared.Layouts;
using WitchCityRope.Web.Tests.Helpers;

namespace WitchCityRope.Web.Tests.Shared.Layouts
{
    public class PublicLayoutTests : ComponentTestBase
    {
        [Fact]
        public void PublicLayout_BasicStructure_RendersCorrectly()
        {
            // Act
            var component = RenderComponent<PublicLayout>();

            // Assert
            component.Find(".public-layout").Should().NotBeNull();
            component.Find(".public-header").Should().NotBeNull();
            component.Find(".public-footer").Should().NotBeNull();
            component.Find(".layout-content").Should().NotBeNull();
        }

        [Fact]
        public void PublicLayout_Logo_NavigatesToHome()
        {
            // Arrange
            var component = RenderComponent<PublicLayout>();

            // Act
            component.Find(".logo").Click();

            // Assert
            VerifyNavigation("/");
        }

        [Fact]
        public void PublicLayout_MinimalNavigation_ShowsOnlyEssentialLinks()
        {
            // Arrange
            var component = RenderComponent<PublicLayout>();

            // Assert
            var navLinks = component.FindAll(".nav-link");
            navLinks.Should().HaveCountLessThan(5); // Minimal navigation
            navLinks.Should().Contain(x => x.TextContent == "Home");
            navLinks.Should().Contain(x => x.TextContent == "About");
            navLinks.Should().Contain(x => x.TextContent == "Contact");
        }

        [Fact]
        public void PublicLayout_NoUserMenu_EvenWhenAuthenticated()
        {
            // Arrange
            AuthenticationTestContext.SetupAuthenticatedUser(this);

            // Act
            var component = RenderComponent<PublicLayout>();

            // Assert
            component.FindAll(".user-menu").Should().BeEmpty();
            component.FindAll(".notification-bell").Should().BeEmpty();
        }

        [Fact]
        public void PublicLayout_SignInButton_AlwaysVisible()
        {
            // Act
            var component = RenderComponent<PublicLayout>();

            // Assert
            var signInButton = component.Find(".sign-in-button");
            signInButton.Should().NotBeNull();
            signInButton.TextContent.Should().Be("Sign In");
        }

        [Fact]
        public async Task PublicLayout_SignInButton_NavigatesToLogin()
        {
            // Arrange
            var component = RenderComponent<PublicLayout>();

            // Act
            await component.Find(".sign-in-button").ClickAsync();

            // Assert
            VerifyNavigation("/auth/login");
        }

        [Fact]
        public void PublicLayout_Footer_SimplifiedContent()
        {
            // Arrange
            var component = RenderComponent<PublicLayout>();

            // Assert
            var footer = component.Find(".public-footer");
            footer.TextContent.Should().Contain($"© {DateTime.Now.Year} Witch City Rope");
            
            // Should have minimal footer links
            var footerLinks = component.FindAll(".footer-link");
            footerLinks.Should().HaveCountLessThanOrEqualTo(3);
        }

        [Fact]
        public void PublicLayout_NoAdminLinks_RegardlessOfRole()
        {
            // Arrange
            AuthenticationTestContext.SetupAuthenticatedUser(this, 
                "admin-user", "admin@example.com", "AdminUser", new[] { "Admin" });

            // Act
            var component = RenderComponent<PublicLayout>();

            // Assert
            component.FindAll(".admin-link").Should().BeEmpty();
        }

        [Fact]
        public void PublicLayout_CleanDesign_NoExtraFeatures()
        {
            // Act
            var component = RenderComponent<PublicLayout>();

            // Assert
            // No notifications
            component.FindAll(".notification-bell").Should().BeEmpty();
            
            // No breadcrumbs
            component.FindAll(".breadcrumb").Should().BeEmpty();
            
            // No theme toggle
            component.FindAll(".theme-toggle").Should().BeEmpty();
            
            // No scroll to top
            component.FindAll(".scroll-to-top").Should().BeEmpty();
        }

        [Fact]
        public void PublicLayout_AgeNotice_DisplaysProminently()
        {
            // Act
            var component = RenderComponent<PublicLayout>();

            // Assert
            var ageNotice = component.Find(".age-notice");
            ageNotice.Should().NotBeNull();
            ageNotice.TextContent.Should().Contain("21+");
            ageNotice.TextContent.Should().Contain("AGE VERIFICATION REQUIRED");
        }

        [Fact]
        public async Task PublicLayout_MobileMenu_SimplifiedVersion()
        {
            // Arrange
            var component = RenderComponent<PublicLayout>();

            // Act
            await component.Find(".mobile-menu-toggle").ClickAsync();

            // Assert
            var mobileMenu = component.Find(".mobile-menu");
            mobileMenu.GetClasses().Should().Contain("open");
            
            // Should have minimal mobile menu items
            var mobileLinks = mobileMenu.FindAll(".mobile-nav-link");
            mobileLinks.Should().HaveCountLessThanOrEqualTo(4);
        }

        [Fact]
        public void PublicLayout_BackToMainSite_LinkForAuthenticatedUsers()
        {
            // Arrange
            AuthenticationTestContext.SetupAuthenticatedUser(this);

            // Act
            var component = RenderComponent<PublicLayout>();

            // Assert
            var backLink = component.Find(".back-to-dashboard");
            backLink.Should().NotBeNull();
            backLink.TextContent.Should().Contain("Back to Dashboard");
            backLink.GetAttribute("href").Should().Be("/member/dashboard");
        }

        [Fact]
        public void PublicLayout_ContentWrapper_HasProperStyling()
        {
            // Act
            var component = RenderComponent<PublicLayout>
            (
                childContent: builder =>
                {
                    builder.AddMarkupContent(0, "<div>Test Content</div>");
                }
            );

            // Assert
            var content = component.Find(".layout-content");
            content.Should().NotBeNull();
            content.GetClasses().Should().Contain("public-content");
            content.InnerHtml.Should().Contain("Test Content");
        }

        [Fact]
        public void PublicLayout_EmergencyContact_InFooter()
        {
            // Act
            var component = RenderComponent<PublicLayout>();

            // Assert
            var emergencyInfo = component.Find(".emergency-contact");
            emergencyInfo.Should().NotBeNull();
            emergencyInfo.TextContent.Should().Contain("Emergency");
            emergencyInfo.TextContent.Should().Contain("safety@witchcityrope.com");
        }

        [Fact]
        public void PublicLayout_SocialMediaLinks_NotPresent()
        {
            // Act
            var component = RenderComponent<PublicLayout>();

            // Assert
            // Public layout should not have social media links
            component.FindAll(".social-links").Should().BeEmpty();
            component.FindAll("[class*='facebook']").Should().BeEmpty();
            component.FindAll("[class*='twitter']").Should().BeEmpty();
            component.FindAll("[class*='instagram']").Should().BeEmpty();
        }

        [Fact]
        public void PublicLayout_RegisterButton_Visible()
        {
            // Act
            var component = RenderComponent<PublicLayout>();

            // Assert
            var registerButton = component.Find(".register-button");
            registerButton.Should().NotBeNull();
            registerButton.TextContent.Should().Be("Join Us");
            registerButton.GetAttribute("href").Should().Be("/auth/login#register");
        }

        [Fact]
        public void PublicLayout_SimplifiedColorScheme()
        {
            // Act
            var component = RenderComponent<PublicLayout>();

            // Assert
            // Check that the layout uses simplified styling
            var layout = component.Find(".public-layout");
            layout.GetClasses().Should().Contain("simplified-theme");
        }
    }
}