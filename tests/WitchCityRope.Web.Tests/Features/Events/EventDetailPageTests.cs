using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Bunit;
using WitchCityRope.Web.Features.Events.Pages;
using WitchCityRope.Web.Tests.Helpers;
using WitchCityRope.Web.Services;
using EventDetailPage = WitchCityRope.Web.Features.Events.Pages.EventDetail;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Enums;
using CoreEventType = WitchCityRope.Core.Enums.EventType;

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

        private WitchCityRope.Web.Services.EventDetail CreateTestEventDetail(Guid? id = null)
        {
            return new WitchCityRope.Web.Services.EventDetail
            {
                Id = id ?? Guid.NewGuid(),
                Title = "Advanced Rope Techniques",
                Description = "An in-depth workshop covering advanced rope bondage techniques including suspensions.",
                StartDateTime = DateTime.UtcNow.AddDays(14),
                EndDateTime = DateTime.UtcNow.AddDays(14).AddHours(4),
                Location = "The Rope Space, 456 Oak Street, Salem MA",
                AvailableSpots = 8,
                Price = 100,
                Organizers = new List<string> { "Jane Smith" },
                IsRegistered = false
            };
        }

        [Fact]
        public async Task EventDetail_LoadsAndDisplaysEventData()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventData = CreateTestEventDetail(eventId);
            _eventServiceMock.Setup(x => x.GetEventDetailAsync(eventId))
                .ReturnsAsync(eventData);

            // Act
            var component = RenderComponent<EventDetailPage>(parameters => parameters
                .Add(p => p.EventId, eventId));

            await Task.Delay(50); // Wait for async load

            // Assert
            component.Find("h1").TextContent.Should().Be("Advanced Rope Techniques");
            component.Find(".event-description").TextContent.Should().Contain("in-depth workshop");
            component.Find(".organizer-name").TextContent.Should().Contain("Jane Smith");
            component.Find(".event-price").TextContent.Should().Contain("$100");
        }

        [Fact]
        public async Task EventDetail_ShowsLoadingState()
        {
            // Arrange
            var tcs = new TaskCompletionSource<WitchCityRope.Web.Services.EventDetail>();
            _eventServiceMock.Setup(x => x.GetEventDetailAsync(It.IsAny<Guid>()))
                .Returns(tcs.Task);

            // Act
            var component = RenderComponent<EventDetailPage>(parameters => parameters
                .Add(p => p.EventId, Guid.NewGuid()));

            // Assert
            component.Find(".loading-container").Should().NotBeNull();
            component.Find(".loading-spinner").Should().NotBeNull();

            // Complete loading
            tcs.SetResult(CreateTestEventDetail());
            await Task.Delay(50);

            // Should no longer show loading
            component.FindAll(".loading-container").Should().BeEmpty();
        }

        [Fact]
        public async Task EventDetail_EventNotFound_ShowsErrorMessage()
        {
            // Arrange
            _eventServiceMock.Setup(x => x.GetEventDetailAsync(It.IsAny<Guid>()))
                .ReturnsAsync((WitchCityRope.Web.Services.EventDetail)null);

            // Act
            var component = RenderComponent<EventDetailPage>(parameters => parameters
                .Add(p => p.EventId, Guid.NewGuid()));

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
            var eventData = CreateTestEventDetail();
            _eventServiceMock.Setup(x => x.GetEventDetailAsync(It.IsAny<Guid>()))
                .ReturnsAsync(eventData);

            // Act
            var component = RenderComponent<EventDetailPage>(parameters => parameters
                .Add(p => p.EventId, Guid.NewGuid()));

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
            var eventData = CreateTestEventDetail();
            eventData.AvailableSpots = 10;
            
            _eventServiceMock.Setup(x => x.GetEventDetailAsync(It.IsAny<Guid>()))
                .ReturnsAsync(eventData);

            // Act
            var component = RenderComponent<EventDetailPage>(parameters => parameters
                .Add(p => p.EventId, Guid.NewGuid()));

            await Task.Delay(50);

            // Assert
            component.Find(".btn-register").TextContent.Should().Be("Register Now");
            component.Find(".availability-status").TextContent.Should().Contain("10 spots");
        }

        [Fact]
        public async Task EventDetail_SoldOut_ShowsSoldOutStatus()
        {
            // Arrange
            var eventData = CreateTestEventDetail();
            eventData.AvailableSpots = 0;
            
            _eventServiceMock.Setup(x => x.GetEventDetailAsync(It.IsAny<Guid>()))
                .ReturnsAsync(eventData);

            // Act
            var component = RenderComponent<EventDetailPage>(parameters => parameters
                .Add(p => p.EventId, Guid.NewGuid()));

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
            var eventData = CreateTestEventDetail(eventId);
            eventData.IsRegistered = true;
            
            _eventServiceMock.Setup(x => x.GetEventDetailAsync(eventId))
                .ReturnsAsync(eventData);

            // Act
            var component = RenderComponent<EventDetailPage>(parameters => parameters
                .Add(p => p.EventId, eventId));

            await Task.Delay(50);

            // Assert
            component.Find(".registered-status").Should().NotBeNull();
            component.Find(".registered-status").TextContent.Should().Contain("You're registered!");
            component.Find(".btn-cancel-registration").Should().NotBeNull();
        }

        [Fact]
        public async Task EventDetail_RegisterButton_CallsEventService()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventData = CreateTestEventDetail(eventId);
            
            _eventServiceMock.Setup(x => x.GetEventDetailAsync(eventId))
                .ReturnsAsync(eventData);
            
            _eventServiceMock.Setup(x => x.RegisterForEventAsync(eventId))
                .ReturnsAsync(true);

            var component = RenderComponent<EventDetailPage>(parameters => parameters
                .Add(p => p.EventId, eventId));

            await Task.Delay(50);

            // Act
            await component.Find(".btn-register").ClickAsync();

            // Assert
            _eventServiceMock.Verify(x => x.RegisterForEventAsync(eventId), Times.Once);
            ToastServiceMock.Verify(x => x.ShowSuccess(It.Is<string>(s => s.Contains("registered"))), Times.Once);
        }

        [Fact]
        public async Task EventDetail_CancelRegistration_CallsService()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var registrationId = Guid.NewGuid();
            var eventData = CreateTestEventDetail(eventId);
            eventData.IsRegistered = true;
            
            _eventServiceMock.Setup(x => x.GetEventDetailAsync(eventId))
                .ReturnsAsync(eventData);
            
            _registrationServiceMock.Setup(x => x.GetMyRegistrationsAsync())
                .ReturnsAsync(new List<UserRegistration> 
                {
                    new UserRegistration 
                    { 
                        Id = registrationId, 
                        EventId = eventId,
                        EventTitle = eventData.Title,
                        EventDate = eventData.StartDateTime,
                        Status = "Confirmed"
                    }
                });
            
            _registrationServiceMock.Setup(x => x.CancelRegistrationAsync(registrationId))
                .ReturnsAsync(true);

            var component = RenderComponent<EventDetailPage>(parameters => parameters
                .Add(p => p.EventId, eventId));

            await Task.Delay(50);

            // Act
            await component.Find(".btn-cancel-registration").ClickAsync();

            // Assert
            _registrationServiceMock.Verify(x => x.CancelRegistrationAsync(registrationId), Times.Once);
            ToastServiceMock.Verify(x => x.ShowSuccess(It.Is<string>(s => s.Contains("cancelled"))), Times.Once);
        }

        [Fact]
        public async Task EventDetail_PastEvent_ShowsPastEventStatus()
        {
            // Arrange
            var eventData = CreateTestEventDetail();
            eventData.StartDateTime = DateTime.UtcNow.AddDays(-1);
            eventData.EndDateTime = DateTime.UtcNow.AddDays(-1).AddHours(4);
            
            _eventServiceMock.Setup(x => x.GetEventDetailAsync(It.IsAny<Guid>()))
                .ReturnsAsync(eventData);

            // Act
            var component = RenderComponent<EventDetailPage>(parameters => parameters
                .Add(p => p.EventId, Guid.NewGuid()));

            await Task.Delay(50);

            // Assert
            component.Find(".past-event-notice").Should().NotBeNull();
            component.Find(".past-event-notice").TextContent.Should().Contain("This event has already occurred");
            component.FindAll(".btn-register").Should().BeEmpty();
        }

        // Prerequisites test removed - property not available in EventDetail class

        // WhatToBring test removed - property not available in EventDetail class

        [Fact]
        public async Task EventDetail_FreeEvent_ShowsFreePrice()
        {
            // Arrange
            var eventData = CreateTestEventDetail();
            eventData.Price = 0;
            
            _eventServiceMock.Setup(x => x.GetEventDetailAsync(It.IsAny<Guid>()))
                .ReturnsAsync(eventData);

            // Act
            var component = RenderComponent<EventDetailPage>(parameters => parameters
                .Add(p => p.EventId, Guid.NewGuid()));

            await Task.Delay(50);

            // Assert
            component.Find(".event-price").TextContent.Should().Contain("Free");
        }

        [Fact]
        public async Task EventDetail_ShareButton_CopiesLink()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventData = CreateTestEventDetail(eventId);
            
            _eventServiceMock.Setup(x => x.GetEventDetailAsync(eventId))
                .ReturnsAsync(eventData);

            var component = RenderComponent<EventDetailPage>(parameters => parameters
                .Add(p => p.EventId, eventId));

            await Task.Delay(50);

            // Act
            await component.Find(".btn-share").ClickAsync();

            // Assert
            JSRuntimeMock.Verify(x => x.InvokeAsync<It.IsAnyType>(
                "navigator.clipboard.writeText", 
                It.Is<object[]>(args => args[0].ToString().Contains(eventId.ToString()))), 
                Times.Once);
            
            ToastServiceMock.Verify(x => x.ShowSuccess("Event link copied to clipboard!"), Times.Once);
        }

        [Fact]
        public async Task EventDetail_BackButton_NavigatesToEventList()
        {
            // Arrange
            var eventData = CreateTestEventDetail();
            _eventServiceMock.Setup(x => x.GetEventDetailAsync(It.IsAny<Guid>()))
                .ReturnsAsync(eventData);

            var component = RenderComponent<EventDetailPage>(parameters => parameters
                .Add(p => p.EventId, Guid.NewGuid()));

            await Task.Delay(50);

            // Act
            await component.Find(".btn-back").ClickAsync();

            // Assert
            VerifyNavigation("/events");
        }
    }
}