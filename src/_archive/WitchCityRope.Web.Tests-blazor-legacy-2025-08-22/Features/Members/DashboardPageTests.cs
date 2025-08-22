using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Bunit;
using WitchCityRope.Web.Features.Members.Pages;
using WitchCityRope.Web.Tests.Helpers;
using WitchCityRope.Web.Services;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Web.Tests.Features.Members
{
    public class DashboardPageTests : ComponentTestBase
    {
        private Mock<IEventService> _eventServiceMock;
        private Mock<IRegistrationService> _registrationServiceMock;
        private Mock<IUserService> _userServiceMock;

        protected override void SetupDefaultServices()
        {
            base.SetupDefaultServices();
            
            _eventServiceMock = new Mock<IEventService>();
            _registrationServiceMock = new Mock<IRegistrationService>();
            _userServiceMock = new Mock<IUserService>();
            
            Services.AddSingleton(_eventServiceMock.Object);
            Services.AddSingleton(_registrationServiceMock.Object);
            Services.AddSingleton(_userServiceMock.Object);

            // Setup authenticated user
            AuthenticationTestContext.SetupAuthenticatedUser(this, "test-user", "test@example.com", "TestUser");
        }

        private List<RegistrationDto> CreateTestRegistrations()
        {
            return new List<RegistrationDto>
            {
                new RegistrationDto
                {
                    Id = Guid.NewGuid(),
                    EventId = Guid.NewGuid(),
                    EventTitle = "Upcoming Workshop",
                    EventDate = DateTime.UtcNow.AddDays(7),
                    EventLocation = "Studio A",
                    Status = RegistrationStatus.Confirmed
                },
                new RegistrationDto
                {
                    Id = Guid.NewGuid(),
                    EventId = Guid.NewGuid(),
                    EventTitle = "Monthly Social",
                    EventDate = DateTime.UtcNow.AddDays(14),
                    EventLocation = "Community Center",
                    Status = RegistrationStatus.Confirmed
                }
            };
        }

        private UserStatsDto CreateTestUserStats()
        {
            return new UserStatsDto
            {
                TotalEventsAttended = 15,
                UpcomingEvents = 2,
                MemberSince = DateTime.UtcNow.AddMonths(-6),
                LastEventDate = DateTime.UtcNow.AddDays(-14),
                VettingStatus = "Approved",
                MembershipStatus = "Active"
            };
        }

        [Fact]
        public void Dashboard_InitialRender_ShowsWelcomeMessage()
        {
            // Arrange
            _registrationServiceMock.Setup(x => x.GetUserRegistrationsAsync())
                .ReturnsAsync(new List<RegistrationDto>());
            _userServiceMock.Setup(x => x.GetUserStatsAsync())
                .ReturnsAsync(CreateTestUserStats());

            // Act
            var component = RenderComponent<Dashboard>();

            // Assert
            component.Find(".welcome-message").TextContent.Should().Contain("Welcome back, TestUser!");
        }

        [Fact]
        public async Task Dashboard_LoadsUpcomingEvents()
        {
            // Arrange
            var registrations = CreateTestRegistrations();
            _registrationServiceMock.Setup(x => x.GetUserRegistrationsAsync())
                .ReturnsAsync(registrations);
            _userServiceMock.Setup(x => x.GetUserStatsAsync())
                .ReturnsAsync(CreateTestUserStats());

            // Act
            var component = RenderComponent<Dashboard>();
            await Task.Delay(50); // Wait for async load

            // Assert
            var eventCards = component.FindAll(".upcoming-event-card");
            eventCards.Should().HaveCount(2);
            eventCards[0].TextContent.Should().Contain("Upcoming Workshop");
            eventCards[1].TextContent.Should().Contain("Monthly Social");
        }

        [Fact]
        public async Task Dashboard_NoUpcomingEvents_ShowsEmptyState()
        {
            // Arrange
            _registrationServiceMock.Setup(x => x.GetUserRegistrationsAsync())
                .ReturnsAsync(new List<RegistrationDto>());
            _userServiceMock.Setup(x => x.GetUserStatsAsync())
                .ReturnsAsync(CreateTestUserStats());

            // Act
            var component = RenderComponent<Dashboard>();
            await Task.Delay(50);

            // Assert
            component.Find(".empty-state").Should().NotBeNull();
            component.Find(".empty-state").TextContent.Should().Contain("No upcoming events");
            component.Find(".btn-browse-events").Should().NotBeNull();
        }

        [Fact]
        public async Task Dashboard_DisplaysUserStats()
        {
            // Arrange
            var stats = CreateTestUserStats();
            _registrationServiceMock.Setup(x => x.GetUserRegistrationsAsync())
                .ReturnsAsync(new List<RegistrationDto>());
            _userServiceMock.Setup(x => x.GetUserStatsAsync())
                .ReturnsAsync(stats);

            // Act
            var component = RenderComponent<Dashboard>();
            await Task.Delay(50);

            // Assert
            var statCards = component.FindAll(".stat-card");
            statCards.Should().HaveCountGreaterOrEqualTo(3);
            
            component.Find(".events-attended-stat").TextContent.Should().Contain("15");
            component.Find(".upcoming-events-stat").TextContent.Should().Contain("2");
            component.Find(".member-since-stat").TextContent.Should().Contain("6 months");
        }

        [Fact]
        public async Task Dashboard_VettingStatus_ShowsApproved()
        {
            // Arrange
            var stats = CreateTestUserStats();
            stats.VettingStatus = "Approved";
            
            _registrationServiceMock.Setup(x => x.GetUserRegistrationsAsync())
                .ReturnsAsync(new List<RegistrationDto>());
            _userServiceMock.Setup(x => x.GetUserStatsAsync())
                .ReturnsAsync(stats);

            // Act
            var component = RenderComponent<Dashboard>();
            await Task.Delay(50);

            // Assert
            component.Find(".vetting-status").Should().NotBeNull();
            component.Find(".vetting-status-approved").TextContent.Should().Contain("Approved");
            component.Find(".vetting-status-approved").GetClasses().Should().Contain("status-success");
        }

        [Fact]
        public async Task Dashboard_VettingStatus_ShowsPending()
        {
            // Arrange
            var stats = CreateTestUserStats();
            stats.VettingStatus = "Pending";
            
            _registrationServiceMock.Setup(x => x.GetUserRegistrationsAsync())
                .ReturnsAsync(new List<RegistrationDto>());
            _userServiceMock.Setup(x => x.GetUserStatsAsync())
                .ReturnsAsync(stats);

            // Act
            var component = RenderComponent<Dashboard>();
            await Task.Delay(50);

            // Assert
            component.Find(".vetting-status-pending").TextContent.Should().Contain("Pending Review");
            component.Find(".vetting-status-pending").GetClasses().Should().Contain("status-warning");
        }

        [Fact]
        public async Task Dashboard_VettingStatus_ShowsNotStarted()
        {
            // Arrange
            var stats = CreateTestUserStats();
            stats.VettingStatus = "NotStarted";
            
            _registrationServiceMock.Setup(x => x.GetUserRegistrationsAsync())
                .ReturnsAsync(new List<RegistrationDto>());
            _userServiceMock.Setup(x => x.GetUserStatsAsync())
                .ReturnsAsync(stats);

            // Act
            var component = RenderComponent<Dashboard>();
            await Task.Delay(50);

            // Assert
            component.Find(".vetting-status-not-started").Should().NotBeNull();
            component.Find(".btn-start-vetting").Should().NotBeNull();
            component.Find(".btn-start-vetting").TextContent.Should().Contain("Start Application");
        }

        [Fact]
        public async Task Dashboard_QuickActions_DisplayCorrectly()
        {
            // Arrange
            _registrationServiceMock.Setup(x => x.GetUserRegistrationsAsync())
                .ReturnsAsync(new List<RegistrationDto>());
            _userServiceMock.Setup(x => x.GetUserStatsAsync())
                .ReturnsAsync(CreateTestUserStats());

            // Act
            var component = RenderComponent<Dashboard>();
            await Task.Delay(50);

            // Assert
            var quickActions = component.FindAll(".quick-action-card");
            quickActions.Should().HaveCountGreaterOrEqualTo(3);
            
            quickActions.Should().Contain(x => x.TextContent.Contains("Browse Events"));
            quickActions.Should().Contain(x => x.TextContent.Contains("My Tickets"));
            quickActions.Should().Contain(x => x.TextContent.Contains("Profile Settings"));
        }

        [Fact]
        public async Task Dashboard_BrowseEventsButton_NavigatesToEventList()
        {
            // Arrange
            _registrationServiceMock.Setup(x => x.GetUserRegistrationsAsync())
                .ReturnsAsync(new List<RegistrationDto>());
            _userServiceMock.Setup(x => x.GetUserStatsAsync())
                .ReturnsAsync(CreateTestUserStats());

            var component = RenderComponent<Dashboard>();
            await Task.Delay(50);

            // Act
            await component.Find(".btn-browse-events").ClickAsync();

            // Assert
            VerifyNavigation("/events");
        }

        [Fact]
        public async Task Dashboard_EventCard_NavigatesToEventDetail()
        {
            // Arrange
            var registrations = CreateTestRegistrations();
            _registrationServiceMock.Setup(x => x.GetUserRegistrationsAsync())
                .ReturnsAsync(registrations);
            _userServiceMock.Setup(x => x.GetUserStatsAsync())
                .ReturnsAsync(CreateTestUserStats());

            var component = RenderComponent<Dashboard>();
            await Task.Delay(50);

            // Act
            await component.Find(".upcoming-event-card").ClickAsync();

            // Assert
            VerifyNavigation($"/events/{registrations[0].EventId}");
        }

        [Fact]
        public async Task Dashboard_LoadingState_ShowsSpinner()
        {
            // Arrange
            var tcs = new TaskCompletionSource<List<RegistrationDto>>();
            _registrationServiceMock.Setup(x => x.GetUserRegistrationsAsync())
                .Returns(tcs.Task);
            _userServiceMock.Setup(x => x.GetUserStatsAsync())
                .ReturnsAsync(CreateTestUserStats());

            // Act
            var component = RenderComponent<Dashboard>();

            // Assert
            component.Find(".loading-spinner").Should().NotBeNull();
            
            // Complete loading
            tcs.SetResult(new List<RegistrationDto>());
            await Task.Delay(50);
            
            component.FindAll(".loading-spinner").Should().BeEmpty();
        }

        [Fact]
        public async Task Dashboard_ErrorLoadingData_ShowsErrorMessage()
        {
            // Arrange
            _registrationServiceMock.Setup(x => x.GetUserRegistrationsAsync())
                .ThrowsAsync(new Exception("Network error"));
            _userServiceMock.Setup(x => x.GetUserStatsAsync())
                .ReturnsAsync(CreateTestUserStats());

            // Act
            var component = RenderComponent<Dashboard>();
            await Task.Delay(50);

            // Assert
            component.Find(".error-alert").Should().NotBeNull();
            component.Find(".error-alert").TextContent.Should().Contain("Error loading your events");
            component.Find(".btn-retry").Should().NotBeNull();
        }

        [Fact]
        public async Task Dashboard_RetryButton_ReloadsData()
        {
            // Arrange
            var callCount = 0;
            _registrationServiceMock.Setup(x => x.GetUserRegistrationsAsync())
                .Callback(() => callCount++)
                .ReturnsAsync(() => callCount == 1 
                    ? throw new Exception("Network error") 
                    : new List<RegistrationDto>());
            
            _userServiceMock.Setup(x => x.GetUserStatsAsync())
                .ReturnsAsync(CreateTestUserStats());

            var component = RenderComponent<Dashboard>();
            await Task.Delay(50);

            // Act
            await component.Find(".btn-retry").ClickAsync();
            await Task.Delay(50);

            // Assert
            _registrationServiceMock.Verify(x => x.GetUserRegistrationsAsync(), Times.Exactly(2));
            component.FindAll(".error-alert").Should().BeEmpty();
        }

        [Fact]
        public async Task Dashboard_MembershipStatus_ShowsCorrectBadge()
        {
            // Arrange
            var stats = CreateTestUserStats();
            stats.MembershipStatus = "Active";
            
            _registrationServiceMock.Setup(x => x.GetUserRegistrationsAsync())
                .ReturnsAsync(new List<RegistrationDto>());
            _userServiceMock.Setup(x => x.GetUserStatsAsync())
                .ReturnsAsync(stats);

            // Act
            var component = RenderComponent<Dashboard>();
            await Task.Delay(50);

            // Assert
            component.Find(".membership-badge").Should().NotBeNull();
            component.Find(".membership-badge").TextContent.Should().Contain("Active Member");
            component.Find(".membership-badge").GetClasses().Should().Contain("badge-success");
        }

        [Fact]
        public async Task Dashboard_RecentAnnouncements_DisplaysIfPresent()
        {
            // Arrange
            _registrationServiceMock.Setup(x => x.GetUserRegistrationsAsync())
                .ReturnsAsync(new List<RegistrationDto>());
            _userServiceMock.Setup(x => x.GetUserStatsAsync())
                .ReturnsAsync(CreateTestUserStats());
            _userServiceMock.Setup(x => x.GetRecentAnnouncementsAsync())
                .ReturnsAsync(new List<AnnouncementDto>
                {
                    new AnnouncementDto 
                    { 
                        Title = "New Workshop Series", 
                        Content = "Starting next month",
                        Date = DateTime.UtcNow.AddDays(-1)
                    }
                });

            // Act
            var component = RenderComponent<Dashboard>();
            await Task.Delay(50);

            // Assert
            component.Find(".announcements-section").Should().NotBeNull();
            component.Find(".announcement-card").TextContent.Should().Contain("New Workshop Series");
        }

        // DTO classes for testing
        public class UserStatsDto
        {
            public int TotalEventsAttended { get; set; }
            public int UpcomingEvents { get; set; }
            public DateTime MemberSince { get; set; }
            public DateTime? LastEventDate { get; set; }
            public string VettingStatus { get; set; }
            public string MembershipStatus { get; set; }
        }

        public class AnnouncementDto
        {
            public string Title { get; set; }
            public string Content { get; set; }
            public DateTime Date { get; set; }
        }
    }
}