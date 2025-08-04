using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using WitchCityRope.Web.Features.Events.Components;
using WitchCityRope.Web.Tests.Helpers;
using static WitchCityRope.Web.Features.Events.Components.EventCard;

namespace WitchCityRope.Web.Tests.Features.Events
{
    public class EventCardComponentTests : ComponentTestBase
    {
        private EventCardViewModel CreateTestEvent()
        {
            return new EventCardViewModel
            {
                Id = 123,
                Title = "Rope Basics Workshop",
                Description = "Learn the fundamentals of rope bondage in a safe and supportive environment. This workshop covers basic knots, safety protocols, and communication techniques.",
                StartDate = DateTime.Now.AddDays(7),
                Type = "Workshop",
                Location = "Studio A, 123 Main Street, Salem MA",
                Price = 50,
                AvailableSpots = 8,
                ImageUrl = "/images/workshop.jpg"
            };
        }

        [Fact]
        public void EventCard_WithAllData_RendersCorrectly()
        {
            // Arrange
            var eventData = CreateTestEvent();

            // Act
            var component = RenderComponent<EventCard>(parameters => parameters
                .Add(p => p.Event, eventData));

            // Assert
            component.Find(".event-card-title").TextContent.Should().Be("Rope Basics Workshop");
            component.Find(".event-type-badge").TextContent.Should().Be("Workshop");
            component.Find(".event-card-image img").GetAttribute("src").Should().Be("/images/workshop.jpg");
            component.Find(".price-amount").TextContent.Should().Be("$50");
            component.Find(".availability-good").TextContent.Should().Be("Available");
        }

        [Fact]
        public void EventCard_NoImage_ShowsPlaceholder()
        {
            // Arrange
            var eventData = CreateTestEvent();
            eventData.ImageUrl = null;

            // Act
            var component = RenderComponent<EventCard>(parameters => parameters
                .Add(p => p.Event, eventData));

            // Assert
            component.FindAll(".event-card-image img").Should().BeEmpty();
            component.Find(".event-card-image-placeholder").Should().NotBeNull();
            component.Find(".event-type-badge").TextContent.Should().Be("Workshop");
        }

        [Fact]
        public void EventCard_FreeEvent_ShowsFreeText()
        {
            // Arrange
            var eventData = CreateTestEvent();
            eventData.Price = 0;

            // Act
            var component = RenderComponent<EventCard>(parameters => parameters
                .Add(p => p.Event, eventData));

            // Assert
            component.Find(".price-free").TextContent.Should().Be("Free");
            component.FindAll(".price-amount").Should().BeEmpty();
        }

        [Fact]
        public void EventCard_SoldOut_ShowsSoldOutStatus()
        {
            // Arrange
            var eventData = CreateTestEvent();
            eventData.AvailableSpots = 0;

            // Act
            var component = RenderComponent<EventCard>(parameters => parameters
                .Add(p => p.Event, eventData));

            // Assert
            component.Find(".availability-none").TextContent.Should().Be("Sold Out");
            component.FindAll(".availability-good").Should().BeEmpty();
            component.FindAll(".availability-low").Should().BeEmpty();
        }

        [Fact]
        public void EventCard_LowAvailability_ShowsSpotsLeft()
        {
            // Arrange
            var eventData = CreateTestEvent();
            eventData.AvailableSpots = 3;

            // Act
            var component = RenderComponent<EventCard>(parameters => parameters
                .Add(p => p.Event, eventData));

            // Assert
            component.Find(".availability-low").TextContent.Should().Be("3 spots left");
            component.FindAll(".availability-good").Should().BeEmpty();
        }

        [Fact]
        public void EventCard_LongDescription_TruncatesText()
        {
            // Arrange
            var eventData = CreateTestEvent();
            eventData.Description = string.Join(" ", Enumerable.Repeat("Long description text", 20));

            // Act
            var component = RenderComponent<EventCard>(parameters => parameters
                .Add(p => p.Event, eventData));

            // Assert
            var description = component.Find(".event-card-description").TextContent;
            description.Should().EndWith("...");
            description.Length.Should().BeLessThanOrEqualTo(123); // 120 chars + "..."
        }

        [Fact]
        public void EventCard_ShortDescription_NoTruncation()
        {
            // Arrange
            var eventData = CreateTestEvent();
            eventData.Description = "Short description";

            // Act
            var component = RenderComponent<EventCard>(parameters => parameters
                .Add(p => p.Event, eventData));

            // Assert
            component.Find(".event-card-description").TextContent.Should().Be("Short description");
        }

        [Fact]
        public void EventCard_LongLocation_ShowsFirstPart()
        {
            // Arrange
            var eventData = CreateTestEvent();
            eventData.Location = "Studio A, 123 Main Street, Salem, MA 01970";

            // Act
            var component = RenderComponent<EventCard>(parameters => parameters
                .Add(p => p.Event, eventData));

            // Assert
            var locationText = component.FindAll(".meta-item")
                .First(x => x.InnerHtml.Contains("📍"))
                .TextContent;
            locationText.Should().Contain("Studio A");
            locationText.Should().NotContain("Salem");
        }

        [Fact]
        public void EventCard_NoLocation_ShowsTBA()
        {
            // Arrange
            var eventData = CreateTestEvent();
            eventData.Location = "";

            // Act
            var component = RenderComponent<EventCard>(parameters => parameters
                .Add(p => p.Event, eventData));

            // Assert
            var locationText = component.FindAll(".meta-item")
                .First(x => x.InnerHtml.Contains("📍"))
                .TextContent;
            locationText.Should().Contain("TBA");
        }

        [Fact]
        public async Task EventCard_Clickable_InvokesCallback()
        {
            // Arrange
            var eventData = CreateTestEvent();
            var clicked = false;

            // Act
            var component = RenderComponent<EventCard>(parameters => parameters
                .Add(p => p.Event, eventData)
                .Add(p => p.IsClickable, true)
                .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => clicked = true)));

            await component.Find(".event-card").ClickAsync();

            // Assert
            clicked.Should().BeTrue();
            component.Find(".event-card").GetClasses().Should().Contain("event-card-clickable");
        }

        [Fact]
        public async Task EventCard_NotClickable_NoCallback()
        {
            // Arrange
            var eventData = CreateTestEvent();
            var clicked = false;

            // Act
            var component = RenderComponent<EventCard>(parameters => parameters
                .Add(p => p.Event, eventData)
                .Add(p => p.IsClickable, false)
                .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => clicked = true)));

            await component.Find(".event-card").ClickAsync();

            // Assert
            clicked.Should().BeFalse();
            component.Find(".event-card").GetClasses().Should().NotContain("event-card-clickable");
        }

        [Fact]
        public void EventCard_DateFormatting_DisplaysCorrectly()
        {
            // Arrange
            var eventData = CreateTestEvent();
            eventData.StartDate = new DateTime(2025, 12, 25, 14, 30, 0);

            // Act
            var component = RenderComponent<EventCard>(parameters => parameters
                .Add(p => p.Event, eventData));

            // Assert
            var metaItems = component.FindAll(".meta-item");
            metaItems.Should().Contain(x => x.TextContent.Contains("Dec 25, 2025"));
            metaItems.Should().Contain(x => x.TextContent.Contains("2:30 PM"));
        }

        [Fact]
        public void EventCard_TypeBadge_HasCorrectClass()
        {
            // Arrange
            var eventData = CreateTestEvent();
            eventData.Type = "Social";

            // Act
            var component = RenderComponent<EventCard>(parameters => parameters
                .Add(p => p.Event, eventData));

            // Assert
            var badge = component.Find(".event-type-badge");
            badge.GetClasses().Should().Contain("badge-Social");
            badge.TextContent.Should().Be("Social");
        }

        [Fact]
        public void EventCard_MetaIcons_DisplayCorrectly()
        {
            // Arrange
            var eventData = CreateTestEvent();

            // Act
            var component = RenderComponent<EventCard>(parameters => parameters
                .Add(p => p.Event, eventData));

            // Assert
            var metaIcons = component.FindAll(".meta-icon");
            metaIcons.Should().HaveCount(3);
            metaIcons[0].TextContent.Should().Be("📅"); // Date
            metaIcons[1].TextContent.Should().Be("🕐"); // Time
            metaIcons[2].TextContent.Should().Be("📍"); // Location
        }

        [Fact]
        public void EventCard_PriceDisplay_FormatsCorrectly()
        {
            // Arrange
            var eventData = CreateTestEvent();
            eventData.Price = 99.99m;

            // Act
            var component = RenderComponent<EventCard>(parameters => parameters
                .Add(p => p.Event, eventData));

            // Assert
            component.Find(".price-amount").TextContent.Should().Be("$99.99");
        }

        [Fact]
        public void EventCard_ExactlyFiveSpots_ShowsLowAvailability()
        {
            // Arrange
            var eventData = CreateTestEvent();
            eventData.AvailableSpots = 5;

            // Act
            var component = RenderComponent<EventCard>(parameters => parameters
                .Add(p => p.Event, eventData));

            // Assert
            component.Find(".availability-low").TextContent.Should().Be("5 spots left");
        }

        [Fact]
        public void EventCard_SixSpots_ShowsNormalAvailability()
        {
            // Arrange
            var eventData = CreateTestEvent();
            eventData.AvailableSpots = 6;

            // Act
            var component = RenderComponent<EventCard>(parameters => parameters
                .Add(p => p.Event, eventData));

            // Assert
            component.Find(".availability-good").TextContent.Should().Be("Available");
        }
    }
}