using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Bunit;
using WitchCityRope.Web.Features.Events.Pages;
using WitchCityRope.Web.Tests.Helpers;
using WitchCityRope.Web.Services;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Web.Tests.Features.Events
{
    public class EventDetailPageTests : ComponentTestBase
    {
        private Mock<IEventService> _eventServiceMock;
        private Mock<IRegistrationService> _registrationServiceMock;

        protected override void SetupDefaultServices()
        {
            base.SetupDefaultServices();
            
            _eventServiceMock = new Mock<IEventService>();
            _registrationServiceMock = new Mock<IRegistrationService>();
            
            Services.AddSingleton(_eventServiceMock.Object);
            Services.AddSingleton(_registrationServiceMock.Object);
        }

        private EventDto CreateTestEvent(Guid? id = null)
        {
            return new EventDto
            {
                Id = id ?? Guid.NewGuid(),
                Title = "Advanced Rope Techniques",
                Description = "An in-depth workshop covering advanced rope bondage techniques including suspensions.",
                StartDateTime = DateTime.UtcNow.AddDays(14),
                EndDateTime = DateTime.UtcNow.AddDays(14).AddHours(4),
                Location = "The Rope Space, 456 Oak Street, Salem MA",
                MaxAttendees = 20,
                CurrentAttendees = 12,
                Price = 100,
                IsPublic = true,
                Type = EventType.Workshop,
                Status = EventStatus.Published,
                Prerequisites = "Must have completed Rope Basics or equivalent experience",
                WhatToBring = "Your own rope (minimum 6 pieces), water bottle, comfortable clothing",
                InstructorName = "Jane Smith",
                InstructorBio = "20 years of rope experience, certified instructor",
                ImageUrl = "/images/advanced-workshop.jpg"
            };
        }

        [Fact]
        public async Task EventDetail_LoadsAndDisplaysEventData()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventData = CreateTestEvent(eventId);
            _eventServiceMock.Setup(x => x.GetEventByIdAsync(eventId))
                .ReturnsAsync(eventData);

            // Act
            var component = RenderComponent<EventDetail>(parameters => parameters
                .Add(p => p.EventId, eventId.ToString()));

            await Task.Delay(50); // Wait for async load

            // Assert
            component.Find("h1").TextContent.Should().Be("Advanced Rope Techniques");
            component.Find(".event-description").TextContent.Should().Contain("in-depth workshop");
            component.Find(".instructor-name").TextContent.Should().Contain("Jane Smith");
            component.Find(".event-price").TextContent.Should().Contain("$100");
        }

        [Fact]
        public async Task EventDetail_ShowsLoadingState()
        {
            // Arrange
            var tcs = new TaskCompletionSource<EventDto>();
            _eventServiceMock.Setup(x => x.GetEventByIdAsync(It.IsAny<Guid>()))
                .Returns(tcs.Task);

            // Act
            var component = RenderComponent<EventDetail>(parameters => parameters
                .Add(p => p.EventId, Guid.NewGuid().ToString()));

            // Assert
            component.Find(".loading-container").Should().NotBeNull();
            component.Find(".loading-spinner").Should().NotBeNull();

            // Complete loading
            tcs.SetResult(CreateTestEvent());
            await Task.Delay(50);

            // Should no longer show loading
            component.FindAll(".loading-container").Should().BeEmpty();
        }

        [Fact]
        public async Task EventDetail_EventNotFound_ShowsErrorMessage()
        {
            // Arrange
            _eventServiceMock.Setup(x => x.GetEventByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((EventDto)null);

            // Act
            var component = RenderComponent<EventDetail>(parameters => parameters
                .Add(p => p.EventId, Guid.NewGuid().ToString()));

            await Task.Delay(50);

            // Assert
            component.Find(".error-container").Should().NotBeNull();
            component.Find(".error-message").TextContent.Should().Contain("Event not found");
            component.Find(".btn-back").Should().NotBeNull();
        }

        [Fact]
        public async Task EventDetail_UserNotAuthenticated_ShowsLoginPrompt()
        {
            // Arrange
            AuthServiceMock.Setup(x => x.IsAuthenticatedAsync()).ReturnsAsync(false);
            var eventData = CreateTestEvent();
            _eventServiceMock.Setup(x => x.GetEventByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(eventData);

            // Act
            var component = RenderComponent<EventDetail>(parameters => parameters
                .Add(p => p.EventId, Guid.NewGuid().ToString()));

            await Task.Delay(50);

            // Assert
            component.Find(".login-prompt").Should().NotBeNull();
            component.Find(".login-prompt").TextContent.Should().Contain("Please log in to register");
            var loginButton = component.Find(".btn-login");
            loginButton.Should().NotBeNull();
        }

        [Fact]
        public async Task EventDetail_AvailableSpots_ShowsRegisterButton()
        {
            // Arrange
            var eventData = CreateTestEvent();
            eventData.CurrentAttendees = 10;
            eventData.MaxAttendees = 20;
            
            _eventServiceMock.Setup(x => x.GetEventByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(eventData);

            // Act
            var component = RenderComponent<EventDetail>(parameters => parameters
                .Add(p => p.EventId, Guid.NewGuid().ToString()));

            await Task.Delay(50);

            // Assert
            component.Find(".btn-register").TextContent.Should().Be("Register Now");
            component.Find(".availability-status").TextContent.Should().Contain("10 spots available");
        }

        [Fact]
        public async Task EventDetail_SoldOut_ShowsSoldOutStatus()
        {
            // Arrange
            var eventData = CreateTestEvent();
            eventData.CurrentAttendees = eventData.MaxAttendees;
            
            _eventServiceMock.Setup(x => x.GetEventByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(eventData);

            // Act
            var component = RenderComponent<EventDetail>(parameters => parameters
                .Add(p => p.EventId, Guid.NewGuid().ToString()));

            await Task.Delay(50);

            // Assert
            component.Find(".btn-sold-out").Should().NotBeNull();
            component.Find(".btn-sold-out").GetAttribute("disabled").Should().NotBeNull();
            component.Find(".btn-sold-out").TextContent.Should().Be("Sold Out");
        }

        [Fact]
        public async Task EventDetail_AlreadyRegistered_ShowsRegisteredStatus()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventData = CreateTestEvent(eventId);
            
            _eventServiceMock.Setup(x => x.GetEventByIdAsync(eventId))
                .ReturnsAsync(eventData);
            
            _registrationServiceMock.Setup(x => x.IsUserRegisteredAsync(eventId))
                .ReturnsAsync(true);

            // Act
            var component = RenderComponent<EventDetail>(parameters => parameters
                .Add(p => p.EventId, eventId.ToString()));

            await Task.Delay(50);

            // Assert
            component.Find(".registered-status").Should().NotBeNull();
            component.Find(".registered-status").TextContent.Should().Contain("You're registered!");
            component.Find(".btn-cancel-registration").Should().NotBeNull();
        }

        [Fact]
        public async Task EventDetail_RegisterButton_CallsRegistrationService()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventData = CreateTestEvent(eventId);
            
            _eventServiceMock.Setup(x => x.GetEventByIdAsync(eventId))
                .ReturnsAsync(eventData);
            
            _registrationServiceMock.Setup(x => x.RegisterForEventAsync(eventId))
                .ReturnsAsync(new ServiceMockHelpers.RegistrationResult { Success = true });

            var component = RenderComponent<EventDetail>(parameters => parameters
                .Add(p => p.EventId, eventId.ToString()));

            await Task.Delay(50);

            // Act
            await component.Find(".btn-register").ClickAsync();

            // Assert
            _registrationServiceMock.Verify(x => x.RegisterForEventAsync(eventId), Times.Once);
            NotificationServiceMock.Verify(x => x.ShowSuccessAsync(It.Is<string>(s => s.Contains("registered"))), Times.Once);
        }

        [Fact]
        public async Task EventDetail_CancelRegistration_CallsService()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventData = CreateTestEvent(eventId);
            
            _eventServiceMock.Setup(x => x.GetEventByIdAsync(eventId))
                .ReturnsAsync(eventData);
            
            _registrationServiceMock.Setup(x => x.IsUserRegisteredAsync(eventId))
                .ReturnsAsync(true);
            
            _registrationServiceMock.Setup(x => x.CancelRegistrationAsync(eventId))
                .ReturnsAsync(new ServiceMockHelpers.RegistrationResult { Success = true });

            var component = RenderComponent<EventDetail>(parameters => parameters
                .Add(p => p.EventId, eventId.ToString()));

            await Task.Delay(50);

            // Act
            await component.Find(".btn-cancel-registration").ClickAsync();

            // Assert
            _registrationServiceMock.Verify(x => x.CancelRegistrationAsync(eventId), Times.Once);
            NotificationServiceMock.Verify(x => x.ShowSuccessAsync(It.Is<string>(s => s.Contains("cancelled"))), Times.Once);
        }

        [Fact]
        public async Task EventDetail_PastEvent_ShowsPastEventStatus()
        {
            // Arrange
            var eventData = CreateTestEvent();
            eventData.StartDateTime = DateTime.UtcNow.AddDays(-1);
            eventData.EndDateTime = DateTime.UtcNow.AddDays(-1).AddHours(4);
            
            _eventServiceMock.Setup(x => x.GetEventByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(eventData);

            // Act
            var component = RenderComponent<EventDetail>(parameters => parameters
                .Add(p => p.EventId, Guid.NewGuid().ToString()));

            await Task.Delay(50);

            // Assert
            component.Find(".past-event-notice").Should().NotBeNull();
            component.Find(".past-event-notice").TextContent.Should().Contain("This event has already occurred");
            component.FindAll(".btn-register").Should().BeEmpty();
        }

        [Fact]
        public async Task EventDetail_DisplaysPrerequisites()
        {
            // Arrange
            var eventData = CreateTestEvent();
            _eventServiceMock.Setup(x => x.GetEventByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(eventData);

            // Act
            var component = RenderComponent<EventDetail>(parameters => parameters
                .Add(p => p.EventId, Guid.NewGuid().ToString()));

            await Task.Delay(50);

            // Assert
            component.Find(".prerequisites-section").Should().NotBeNull();
            component.Find(".prerequisites-content").TextContent
                .Should().Contain("Must have completed Rope Basics");
        }

        [Fact]
        public async Task EventDetail_DisplaysWhatToBring()
        {
            // Arrange
            var eventData = CreateTestEvent();
            _eventServiceMock.Setup(x => x.GetEventByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(eventData);

            // Act
            var component = RenderComponent<EventDetail>(parameters => parameters
                .Add(p => p.EventId, Guid.NewGuid().ToString()));

            await Task.Delay(50);

            // Assert
            component.Find(".what-to-bring-section").Should().NotBeNull();
            component.Find(".what-to-bring-content").TextContent
                .Should().Contain("Your own rope");
        }

        [Fact]
        public async Task EventDetail_FreeEvent_ShowsFreePrice()
        {
            // Arrange
            var eventData = CreateTestEvent();
            eventData.Price = 0;
            
            _eventServiceMock.Setup(x => x.GetEventByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(eventData);

            // Act
            var component = RenderComponent<EventDetail>(parameters => parameters
                .Add(p => p.EventId, Guid.NewGuid().ToString()));

            await Task.Delay(50);

            // Assert
            component.Find(".event-price").TextContent.Should().Contain("Free");
        }

        [Fact]
        public async Task EventDetail_ShareButton_CopiesLink()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventData = CreateTestEvent(eventId);
            
            _eventServiceMock.Setup(x => x.GetEventByIdAsync(eventId))
                .ReturnsAsync(eventData);

            var component = RenderComponent<EventDetail>(parameters => parameters
                .Add(p => p.EventId, eventId.ToString()));

            await Task.Delay(50);

            // Act
            await component.Find(".btn-share").ClickAsync();

            // Assert
            JSRuntimeMock.Verify(x => x.InvokeAsync<It.IsAnyType>(
                "navigator.clipboard.writeText", 
                It.Is<object[]>(args => args[0].ToString().Contains(eventId.ToString()))), 
                Times.Once);
            
            NotificationServiceMock.Verify(x => x.ShowSuccessAsync("Event link copied to clipboard!"), Times.Once);
        }

        [Fact]
        public async Task EventDetail_BackButton_NavigatesToEventList()
        {
            // Arrange
            var eventData = CreateTestEvent();
            _eventServiceMock.Setup(x => x.GetEventByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(eventData);

            var component = RenderComponent<EventDetail>(parameters => parameters
                .Add(p => p.EventId, Guid.NewGuid().ToString()));

            await Task.Delay(50);

            // Act
            await component.Find(".btn-back").ClickAsync();

            // Assert
            VerifyNavigation("/events");
        }
    }
}