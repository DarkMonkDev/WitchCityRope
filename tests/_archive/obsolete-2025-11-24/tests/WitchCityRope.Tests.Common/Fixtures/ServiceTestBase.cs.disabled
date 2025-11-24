using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using WitchCityRope.Core.Entities;
using WitchCityRope.Tests.Common.Builders;

namespace WitchCityRope.Tests.Common.Fixtures
{
    /// <summary>
    /// Base class for testing services with common mocking patterns
    /// Provides in-memory database and helper methods for creating test entities
    /// </summary>
    public abstract class ServiceTestBase : UnitTestBase
    {        
        // Collections to track created entities for verification
        protected List<User> CreatedUsers { get; private set; }
        protected List<Event> CreatedEvents { get; private set; }

        protected ServiceTestBase()
        {
            CreatedUsers = new List<User>();
            CreatedEvents = new List<Event>();
        }

        /// <summary>
        /// Creates a test user and adds it to the in-memory database
        /// </summary>
        protected User CreateTestUser(string email = "test@example.com", string sceneName = "TestUser")
        {
            var user = new UserBuilder()
                .WithEmail(email)
                .WithSceneName(sceneName)
                .Build();
                
            DbContext.Users.Add(user);
            DbContext.SaveChanges();
            CreatedUsers.Add(user);
            
            return user;
        }

        /// <summary>
        /// Creates a test event and adds it to the in-memory database
        /// </summary>
        protected Event CreateTestEvent(string title = "Test Event", User? organizer = null)
        {
            organizer ??= CreateTestUser("organizer@example.com", "OrganizerUser");
            
            var testEvent = new EventBuilder()
                .WithTitle(title)
                .WithPrimaryOrganizer(organizer)
                .Build();
                
            DbContext.Events.Add(testEvent);
            DbContext.SaveChanges();
            CreatedEvents.Add(testEvent);
            
            return testEvent;
        }

        /// <summary>
        /// Verifies that a user was created with the expected properties
        /// </summary>
        protected void VerifyUserCreated(string expectedEmail, string expectedSceneName)
        {
            var user = CreatedUsers.FirstOrDefault(u => u.Email.Value == expectedEmail);
            user.Should().NotBeNull($"User with email {expectedEmail} should have been created");
            user!.SceneName.Value.Should().Be(expectedSceneName);
        }

        /// <summary>
        /// Verifies that the specified number of users were created
        /// </summary>
        protected void VerifyUserCount(int expectedCount)
        {
            CreatedUsers.Should().HaveCount(expectedCount, $"Expected {expectedCount} users to be created");
        }

        /// <summary>
        /// Seeds the database with realistic test data
        /// </summary>
        protected override void SeedTestData()
        {
            // Create a few test users with different roles
            var adminUser = new UserBuilder()
                .WithEmail("admin@witchcityrope.com")
                .WithSceneName("AdminUser")
                .AsAdministrator()
                .Build();
                
            var memberUser = new UserBuilder()
                .WithEmail("member@witchcityrope.com")
                .WithSceneName("MemberUser")
                .AsMember()
                .Build();

            DbContext.Users.AddRange(adminUser, memberUser);
            
            // Create a test event
            var testEvent = new EventBuilder()
                .WithTitle("Seeded Test Event")
                .WithPrimaryOrganizer(adminUser)
                .Build();
                
            DbContext.Events.Add(testEvent);
            DbContext.SaveChanges();
        }
    }
}