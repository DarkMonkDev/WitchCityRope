using FluentAssertions;
using WitchCityRope.E2E.Tests.Infrastructure;
using WitchCityRope.E2E.Tests.Fixtures;
using WitchCityRope.E2E.Tests.PageObjects.Auth;
using WitchCityRope.E2E.Tests.PageObjects.Events;
using WitchCityRope.E2E.Tests.PageObjects.Members;

namespace WitchCityRope.E2E.Tests.Tests.UserJourneys;

[TestClass]
public class EventRegistrationFlowTests : BaseE2ETest
{
    private TestUser _testUser = null!;
    private TestEvent _testEvent = null!;

    [TestInitialize]
    public new async Task TestInitialize()
    {
        await base.TestInitialize();
        
        // Create test user and event
        _testUser = await TestDataManager.CreateTestUserAsync(isVerified: true, isVetted: true);
        _testEvent = await TestDataManager.CreateTestEventAsync(
            title: "Test Rope Workshop",
            startDate: DateTime.UtcNow.AddDays(7),
            price: 50m,
            capacity: 20
        );
    }

    [TestMethod]
    public async Task CompleteEventRegistrationFlow_ShouldRegisterUserForEvent()
    {
        // Arrange
        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        var eventListPage = new EventListPage(Page, TestSettings.BaseUrl);
        var eventDetailPage = new EventDetailPage(Page, TestSettings.BaseUrl);
        var dashboardPage = new DashboardPage(Page, TestSettings.BaseUrl);

        // Act - Login
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(_testUser.Email, _testUser.Password);
        await loginPage.WaitForLoginSuccessAsync();

        // Browse events
        await eventListPage.NavigateAsync();
        await eventListPage.WaitForEventsToLoadAsync();

        // Search for our test event
        await eventListPage.SearchEventsAsync(_testEvent.Title);
        
        // Verify event appears in list
        var events = await eventListPage.GetEventsAsync();
        events.Should().Contain(e => e.Title.Contains(_testEvent.Title));

        // Click on event to view details
        await eventListPage.ClickEventByTitleAsync(_testEvent.Title);
        
        // Verify we're on event detail page
        var eventDetails = await eventDetailPage.GetEventDetailsAsync();
        eventDetails.Title.Should().Contain(_testEvent.Title);
        eventDetails.Price.Should().Contain(_testEvent.Price.ToString());

        // Check if user can register
        (await eventDetailPage.IsUserRegisteredAsync()).Should().BeFalse();

        // Register for event
        await eventDetailPage.ClickRegisterAsync();

        // Fill registration form if required
        if (await eventDetailPage.IsRegistrationFormVisibleAsync())
        {
            var registrationData = new Dictionary<string, string>
            {
                ["dietaryRestrictions"] = "None",
                ["emergencyContact"] = "Emergency Contact Name",
                ["emergencyPhone"] = "555-0123",
                ["agreeToTerms"] = "true"
            };

            await eventDetailPage.FillRegistrationFormAsync(registrationData);
            await eventDetailPage.SubmitRegistrationFormAsync();
        }

        // Wait for registration to process
        await eventDetailPage.WaitForRegistrationProcessingAsync();

        // Verify registration was successful
        (await eventDetailPage.IsRegistrationSuccessfulAsync()).Should().BeTrue();
        var successMessage = await eventDetailPage.GetRegistrationSuccessMessageAsync();
        successMessage.Should().Contain("successfully registered");

        // Verify user is now registered
        await Page.ReloadAsync();
        (await eventDetailPage.IsUserRegisteredAsync()).Should().BeTrue();

        // Navigate to dashboard
        await dashboardPage.NavigateAsync();

        // Verify event appears in upcoming events
        var upcomingEvents = await dashboardPage.GetUpcomingEventTitlesAsync();
        upcomingEvents.Should().Contain(_testEvent.Title);

        // Take screenshot of successful registration
        await TakeScreenshotAsync("event_registration_success");
    }

    [TestMethod]
    public async Task EventRegistration_WhenNotVetted_ShouldShowRestriction()
    {
        // Arrange
        var nonVettedUser = await TestDataManager.CreateTestUserAsync(isVerified: true, isVetted: false);
        var vettedOnlyEvent = await TestDataManager.CreateTestEventAsync(
            title: "Vetted Members Only Workshop",
            startDate: DateTime.UtcNow.AddDays(14)
        );

        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        var eventListPage = new EventListPage(Page, TestSettings.BaseUrl);
        var eventDetailPage = new EventDetailPage(Page, TestSettings.BaseUrl);

        // Act - Login as non-vetted user
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(nonVettedUser.Email, nonVettedUser.Password);
        await loginPage.WaitForLoginSuccessAsync();

        // Navigate to event
        await eventListPage.NavigateAsync();
        await eventListPage.ClickEventByTitleAsync(vettedOnlyEvent.Title);

        // Assert - Should see restriction message
        var eventDetails = await eventDetailPage.GetEventDetailsAsync();
        eventDetails.Title.Should().Contain(vettedOnlyEvent.Title);

        // Registration button should be disabled or show restriction
        var canRegister = await Page.IsEnabledAsync("button:has-text('Register')");
        if (canRegister)
        {
            await eventDetailPage.ClickRegisterAsync();
            var errorMessage = await eventDetailPage.GetRegistrationErrorMessageAsync();
            errorMessage.ToLower().Should().Contain("vetted members");
        }
    }

    [TestMethod]
    public async Task EventRegistration_WhenEventIsFull_ShouldShowWaitlist()
    {
        // Arrange
        var fullEvent = await TestDataManager.CreateTestEventAsync(
            title: "Popular Workshop - Full",
            capacity: 1
        );

        // Create registration to fill the event
        var otherUser = await TestDataManager.CreateTestUserAsync();
        await TestDataManager.CreateRegistrationAsync(otherUser.Id, fullEvent.Id);

        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        var eventDetailPage = new EventDetailPage(Page, TestSettings.BaseUrl);

        // Act - Login and navigate to full event
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(_testUser.Email, _testUser.Password);
        await loginPage.WaitForLoginSuccessAsync();

        await Page.GotoAsync($"{TestSettings.BaseUrl}/events/{fullEvent.Id}");

        // Assert
        (await eventDetailPage.IsEventFullAsync()).Should().BeTrue();
        
        // Should show waitlist option or disabled register button
        var registerButton = await Page.QuerySelectorAsync("button:has-text('Register')");
        if (registerButton != null)
        {
            var isEnabled = await registerButton.IsEnabledAsync();
            isEnabled.Should().BeFalse();
        }
        else
        {
            // Look for waitlist button
            var waitlistButton = await Page.QuerySelectorAsync("button:has-text('Join Waitlist')");
            waitlistButton.Should().NotBeNull();
        }
    }

    [TestMethod]
    public async Task CancelEventRegistration_ShouldRemoveUserFromEvent()
    {
        // Arrange - Create a registration
        await TestDataManager.CreateRegistrationAsync(_testUser.Id, _testEvent.Id);

        var loginPage = new LoginPage(Page, TestSettings.BaseUrl);
        var eventDetailPage = new EventDetailPage(Page, TestSettings.BaseUrl);
        var dashboardPage = new DashboardPage(Page, TestSettings.BaseUrl);

        // Act - Login and navigate to event
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(_testUser.Email, _testUser.Password);
        await loginPage.WaitForLoginSuccessAsync();

        await Page.GotoAsync($"{TestSettings.BaseUrl}/events/{_testEvent.Id}");

        // Verify user is registered
        (await eventDetailPage.IsUserRegisteredAsync()).Should().BeTrue();

        // Cancel registration
        await eventDetailPage.ClickCancelRegistrationAsync();

        // Confirm cancellation if prompted
        var confirmButton = await Page.QuerySelectorAsync("button:has-text('Confirm')");
        if (confirmButton != null)
        {
            await confirmButton.ClickAsync();
        }

        // Wait for cancellation to process
        await Page.WaitForTimeoutAsync(1000);

        // Verify registration is cancelled
        await Page.ReloadAsync();
        (await eventDetailPage.IsUserRegisteredAsync()).Should().BeFalse();

        // Verify event is removed from dashboard
        await dashboardPage.NavigateAsync();
        var upcomingEvents = await dashboardPage.GetUpcomingEventTitlesAsync();
        upcomingEvents.Should().NotContain(_testEvent.Title);
    }

    [TestMethod]
    public async Task EventFiltering_ShouldShowCorrectResults()
    {
        // Arrange - Create multiple events
        var workshopEvent = await TestDataManager.CreateTestEventAsync(
            title: "Advanced Rope Workshop",
            startDate: DateTime.UtcNow.AddDays(5)
        );
        
        var socialEvent = await TestDataManager.CreateTestEventAsync(
            title: "Community Social Gathering",
            startDate: DateTime.UtcNow.AddDays(10)
        );

        var pastEvent = await TestDataManager.CreateTestEventAsync(
            title: "Past Event",
            startDate: DateTime.UtcNow.AddDays(-5)
        );

        var eventListPage = new EventListPage(Page, TestSettings.BaseUrl);

        // Act - Navigate to events
        await eventListPage.NavigateAsync();
        await eventListPage.WaitForEventsToLoadAsync();

        // Test search functionality
        await eventListPage.SearchEventsAsync("Workshop");
        var searchResults = await eventListPage.GetEventsAsync();
        searchResults.Should().Contain(e => e.Title.Contains("Workshop"));
        searchResults.Should().NotContain(e => e.Title.Contains("Social"));

        // Clear search
        await eventListPage.SearchEventsAsync("");

        // Test date filtering
        var futureDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd");
        await eventListPage.FilterByDateAsync(futureDate);
        
        var filteredEvents = await eventListPage.GetEventsAsync();
        // Should only show events after the selected date
        filteredEvents.Should().NotContain(e => e.Title.Contains("Past Event"));

        // Take screenshot of filtered results
        await TakeScreenshotAsync("event_filtering_results");
    }
}