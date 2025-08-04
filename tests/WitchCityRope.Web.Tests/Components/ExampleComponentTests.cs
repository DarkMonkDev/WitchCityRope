using Bunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Web.Services;
using WitchCityRope.Web.Models;
using WitchCityRope.Web.Tests.Helpers;
using WitchCityRope.Web.Tests.TestData;
using HelpersNotificationType = WitchCityRope.Web.Tests.Helpers.NotificationType;
using Xunit;

namespace WitchCityRope.Web.Tests.Components;

/// <summary>
/// Example test class demonstrating how to use the test infrastructure
/// </summary>
public class ExampleComponentTests : TestBase
{
    private readonly Mock<IApiClient> _apiClientMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IToastService> _toastServiceMock;

    public ExampleComponentTests()
    {
        // Set up mocks
        _apiClientMock = ServiceMockHelper.CreateApiClientMock();
        _notificationServiceMock = ServiceMockHelper.CreateNotificationServiceMock();
        _toastServiceMock = ServiceMockHelper.CreateToastServiceMock();
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);
        
        // Override default services with our mocks
        services.AddSingleton(_apiClientMock.Object);
        services.AddSingleton(_notificationServiceMock.Object);
        services.AddSingleton(_toastServiceMock.Object);
    }

    [Fact]
    public void Component_Should_Render_For_Authenticated_User()
    {
        // Arrange
        this.AddMemberAuthentication();
        
        // Act
        var component = RenderComponent<ExampleComponent>();
        
        // Assert
        AssertContainsText(component, "Welcome");
        AssertDoesNotContainText(component, "Please log in");
    }

    [Fact]
    public void Component_Should_Show_Login_Message_For_Unauthenticated_User()
    {
        // Arrange
        this.AddUnauthenticated();
        
        // Act
        var component = RenderComponent<ExampleComponent>();
        
        // Assert
        AssertContainsText(component, "Please log in");
        AssertDoesNotContainText(component, "Welcome");
    }

    [Fact]
    public async Task Should_Load_Data_And_Display_Results()
    {
        // Arrange
        var testData = new List<Web.Services.EventViewModel>
        {
            new() { Id = Guid.NewGuid(), Title = "Rope Basics", StartDateTime = DateTime.Now.AddDays(7) },
            new() { Id = Guid.NewGuid(), Title = "Advanced Techniques", StartDateTime = DateTime.Now.AddDays(14) }
        };
        
        ServiceMockHelper.SetupSuccessfulGet(_apiClientMock, "/api/events", testData);
        
        // Act
        var component = RenderComponent<EventListComponent>();
        
        // Wait for the component to load data
        await WaitForAssertion(() =>
        {
            component.FindAll(".event-item").Count.Should().Be(2);
        });
        
        // Assert
        AssertContainsText(component, "Rope Basics");
        AssertContainsText(component, "Advanced Techniques");
    }

    [Fact]
    public async Task Should_Show_Error_When_Api_Call_Fails()
    {
        // Arrange
        ServiceMockHelper.SetupFailedApiCall(_apiClientMock, "/api/events", "Network error");
        
        // Act
        var component = RenderComponent<EventListComponent>();
        
        // Wait for error state
        await WaitForAssertion(() =>
        {
            AssertContainsText(component, "Failed to load events");
        });
        
        // Assert
        ServiceMockHelper.VerifyToastShown(_toastServiceMock, 
            "Failed to load events", 
            HelpersNotificationType.Error);
    }

    [Fact]
    public void Should_Navigate_To_Event_Details_When_Clicked()
    {
        // Arrange
        var component = RenderComponent<EventCard>(
            ComponentParameter.CreateParameter("EventId", 123),
            ComponentParameter.CreateParameter("Title", "Test Event")
        );
        
        // Act & Assert
        component.ClickLinkAndVerifyNavigation(".event-details-link", "/events/123");
    }

    [Fact]
    public void Admin_Should_See_Edit_Button()
    {
        // Arrange
        this.AddAdminAuthentication();
        
        // Act
        var component = RenderComponent<EventCard>(
            ComponentParameter.CreateParameter("EventId", 123),
            ComponentParameter.CreateParameter("Title", "Test Event")
        );
        
        // Assert
        var editButton = FindElement(component, "button.edit-btn");
        editButton.Should().NotBeNull();
    }

    [Fact]
    public void Member_Should_Not_See_Edit_Button()
    {
        // Arrange
        this.AddMemberAuthentication();
        
        // Act
        var component = RenderComponent<EventCard>(
            ComponentParameter.CreateParameter("EventId", 123),
            ComponentParameter.CreateParameter("Title", "Test Event")
        );
        
        // Assert
        component.FindAll("button.edit-btn").Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Submit_Form_Successfully()
    {
        // Arrange
        var formData = new CreateEventRequest { Name = "New Event" };
        _apiClientMock
            .Setup(x => x.PostAsync<CreateEventRequest, Web.Services.EventViewModel>("/api/events", It.IsAny<CreateEventRequest>()))
            .ReturnsAsync(new Web.Services.EventViewModel { Id = Guid.NewGuid(), Title = "New Event" });
        
        var component = RenderComponent<EventForm>();
        
        // Act
        var titleInput = component.Find("input#title");
        await titleInput.ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs 
        { 
            Value = "New Event" 
        });
        
        var submitButton = component.Find("button[type='submit']");
        await submitButton.ClickAsync();
        
        // Assert
        _apiClientMock.Verify(x => x.PostAsync<CreateEventRequest, Web.Services.EventViewModel>("/api/events", 
            It.Is<CreateEventRequest>(dto => dto.Name == "New Event")), Times.Once);
        
        ServiceMockHelper.VerifyToastShown(_toastServiceMock, 
            "Event created successfully", 
            HelpersNotificationType.Success);
        
        NavigationTestHelper.AssertNavigatedTo(NavigationManager, "/events/");
    }
}

// Placeholder components for the example - these would be your actual components
internal class ExampleComponent : ComponentBase { }
internal class EventListComponent : ComponentBase { }
internal class EventCard : ComponentBase 
{
    [Parameter] public int EventId { get; set; }
    [Parameter] public string Title { get; set; } = string.Empty;
}
internal class EventForm : ComponentBase { }