using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Bunit;
using WitchCityRope.Web.Features.Events.Pages;
using WitchCityRope.Web.Services;
using WitchCityRope.Web.Tests.Helpers;

namespace WitchCityRope.Web.Tests.Features.Events
{
    public class EventListTests : TestContext
    {
        private readonly Mock<IEventService> _eventServiceMock;
        private readonly Mock<NavigationManager> _navigationManagerMock;

        public EventListTests()
        {
            _eventServiceMock = new Mock<IEventService>();
            _navigationManagerMock = new Mock<NavigationManager>();
            
            // Register services
            Services.AddSingleton(_eventServiceMock.Object);
            Services.AddSingleton(_navigationManagerMock.Object);
        }

        [Fact]
        public void EventList_InitialRender_ShowsLoadingState()
        {
            // Arrange
            var tcs = new TaskCompletionSource<List<EventListItem>>();
            _eventServiceMock
                .Setup(x => x.GetUpcomingEventsAsync())
                .Returns(tcs.Task);

            // Act
            var component = RenderComponent<EventList>();

            // Assert
            component.Find(".loading-spinner").Should().NotBeNull();
            component.Markup.Should().Contain("Loading events...");
        }

        [Fact]
        public async Task EventList_WithEvents_DisplaysEventCards()
        {
            // Arrange
            var events = new List<EventListItem>
            {
                new EventListItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Rope Basics Workshop",
                    StartDateTime = DateTime.UtcNow.AddDays(3),
                    Location = "Studio A",
                    AvailableSpots = 7,
                    Price = 25m
                },
                new EventListItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Advanced Suspension",
                    StartDateTime = DateTime.UtcNow.AddDays(7),
                    Location = "Main Hall",
                    AvailableSpots = 0,
                    Price = 40m
                }
            };

            _eventServiceMock
                .Setup(x => x.GetUpcomingEventsAsync())
                .ReturnsAsync(events);

            // Act
            var component = RenderComponent<EventList>();
            await Task.Delay(50); // Allow async operation to complete

            // Assert
            var eventCards = component.FindAll(".event-card");
            eventCards.Should().HaveCount(2);

            // Verify first event
            var firstEvent = eventCards[0];
            firstEvent.InnerHtml.Should().Contain("Rope Basics Workshop");
            firstEvent.InnerHtml.Should().Contain("Studio A");
            firstEvent.InnerHtml.Should().Contain("7 spots available");

            // Verify second event (should show as full)
            var secondEvent = eventCards[1];
            secondEvent.InnerHtml.Should().Contain("Advanced Suspension");
            secondEvent.InnerHtml.Should().Contain("FULL");
        }

        [Fact]
        public async Task EventList_EmptyList_ShowsNoEventsMessage()
        {
            // Arrange
            _eventServiceMock
                .Setup(x => x.GetUpcomingEventsAsync())
                .ReturnsAsync(new List<EventListItem>());

            // Act
            var component = RenderComponent<EventList>();
            await Task.Delay(50); // Allow async operation to complete

            // Assert
            component.Markup.Should().Contain("No upcoming events");
            component.FindAll(".event-card").Should().BeEmpty();
        }

        [Fact]
        public async Task EventList_ClickEvent_NavigatesToEventDetails()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var events = new List<EventListItem>
            {
                new EventListItem
                {
                    Id = eventId,
                    Title = "Test Event",
                    StartDateTime = DateTime.UtcNow.AddDays(1),
                    Location = "Test Location",
                    AvailableSpots = 20,
                    Price = 30m
                }
            };

            _eventServiceMock
                .Setup(x => x.GetUpcomingEventsAsync())
                .ReturnsAsync(events);

            var component = RenderComponent<EventList>();
            await Task.Delay(50); // Allow async operation to complete

            // Act
            var eventCard = component.Find(".event-card");
            await eventCard.ClickAsync();

            // Assert
            _navigationManagerMock.Verify(
                x => x.NavigateTo($"/events/{eventId}", false),
                Times.Once
            );
        }

        [Fact]
        public async Task EventList_FilterByDate_ShowsOnlyMatchingEvents()
        {
            // Arrange
            var today = DateTime.UtcNow.Date;
            var events = new List<EventListItem>
            {
                new EventListItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Today's Event",
                    StartDateTime = today.AddHours(14),
                    Location = "Room A",
                    AvailableSpots = 20,
                    Price = 25m
                },
                new EventListItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Tomorrow's Event",
                    StartDateTime = today.AddDays(1).AddHours(14),
                    Location = "Room B",
                    AvailableSpots = 20,
                    Price = 25m
                },
                new EventListItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Next Week's Event",
                    StartDateTime = today.AddDays(7).AddHours(14),
                    Location = "Room C",
                    AvailableSpots = 20,
                    Price = 25m
                }
            };

            _eventServiceMock
                .Setup(x => x.GetUpcomingEventsAsync())
                .ReturnsAsync(events);

            var component = RenderComponent<EventList>();
            await Task.Delay(50);

            // Act - Select "This Week" filter
            var filterDropdown = component.Find("select.date-filter");
            await filterDropdown.ChangeAsync(new ChangeEventArgs 
            { 
                Value = "this-week" 
            });

            // Assert
            var visibleEvents = component.FindAll(".event-card:not(.hidden)");
            visibleEvents.Should().HaveCount(2); // Today's and Tomorrow's events
            component.Markup.Should().Contain("Today's Event");
            component.Markup.Should().Contain("Tomorrow's Event");
            component.Markup.Should().NotContain("Next Week's Event");
        }

        [Fact]
        public async Task EventList_SearchFilter_ShowsMatchingEvents()
        {
            // Arrange
            var events = new List<EventListItem>
            {
                new EventListItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Rope Basics",
                    StartDateTime = DateTime.UtcNow.AddDays(1),
                    Location = "Studio",
                    AvailableSpots = 20,
                    Price = 25m
                },
                new EventListItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Photography Workshop",
                    StartDateTime = DateTime.UtcNow.AddDays(2),
                    Location = "Gallery",
                    AvailableSpots = 15,
                    Price = 35m
                }
            };

            _eventServiceMock
                .Setup(x => x.GetUpcomingEventsAsync())
                .ReturnsAsync(events);

            var component = RenderComponent<EventList>();
            await Task.Delay(50);

            // Act
            var searchInput = component.Find("input.search-box");
            await searchInput.InputAsync(new ChangeEventArgs 
            { 
                Value = "rope" 
            });

            // Assert
            var visibleEvents = component.FindAll(".event-card:not(.hidden)");
            visibleEvents.Should().HaveCount(1);
            component.Markup.Should().Contain("Rope Basics");
            component.Markup.Should().NotContain("Photography Workshop");
        }

        [Fact]
        public async Task EventList_RefreshButton_ReloadsEvents()
        {
            // Arrange
            var initialEvents = new List<EventListItem>
            {
                new EventListItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Initial Event",
                    StartDateTime = DateTime.UtcNow.AddDays(1),
                    Location = "Room A",
                    AvailableSpots = 20,
                    Price = 25m
                }
            };

            var refreshedEvents = new List<EventListItem>
            {
                new EventListItem
                {
                    Id = Guid.NewGuid(),
                    Title = "New Event",
                    StartDateTime = DateTime.UtcNow.AddDays(2),
                    Location = "Room B",
                    AvailableSpots = 20,
                    Price = 25m
                }
            };

            _eventServiceMock
                .SetupSequence(x => x.GetUpcomingEventsAsync())
                .ReturnsAsync(initialEvents)
                .ReturnsAsync(refreshedEvents);

            var component = RenderComponent<EventList>();
            await Task.Delay(50);

            // Verify initial state
            component.Markup.Should().Contain("Initial Event");

            // Act
            var refreshButton = component.Find("button.refresh-button");
            await refreshButton.ClickAsync();
            await Task.Delay(50);

            // Assert
            component.Markup.Should().NotContain("Initial Event");
            component.Markup.Should().Contain("New Event");
            _eventServiceMock.Verify(x => x.GetUpcomingEventsAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task EventList_ErrorLoading_ShowsErrorMessage()
        {
            // Arrange
            _eventServiceMock
                .Setup(x => x.GetUpcomingEventsAsync())
                .ThrowsAsync(new Exception("Network error"));

            // Act
            var component = RenderComponent<EventList>();
            await Task.Delay(50);

            // Assert
            component.Markup.Should().Contain("Error loading events");
            component.Markup.Should().Contain("Network error");
            component.FindAll(".event-card").Should().BeEmpty();
        }
    }
}