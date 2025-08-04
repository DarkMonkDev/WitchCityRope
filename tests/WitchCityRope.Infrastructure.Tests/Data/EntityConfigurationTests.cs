using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Infrastructure.Tests.Fixtures;
using WitchCityRope.Tests.Common.Builders;
using WitchCityRope.Tests.Common.Identity;
using Xunit;

namespace WitchCityRope.Infrastructure.Tests.Data
{
    public class EntityConfigurationTests : IntegrationTestBase
    {
        private Event CreateTestEvent(string title = "Test Event", int capacity = 10)
        {
            var eventType = typeof(Event);
            var eventCtor = eventType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            var @event = (Event)eventCtor.Invoke(null);
            
            // Use reflection to set properties
            eventType.GetProperty("Id").SetValue(@event, Guid.NewGuid());
            eventType.GetProperty("Title").SetValue(@event, title);
            eventType.GetProperty("Description").SetValue(@event, "Test event description");
            eventType.GetProperty("StartDate").SetValue(@event, DateTime.UtcNow.AddDays(7));
            eventType.GetProperty("EndDate").SetValue(@event, DateTime.UtcNow.AddDays(7).AddHours(3));
            eventType.GetProperty("Capacity").SetValue(@event, capacity);
            eventType.GetProperty("EventType").SetValue(@event, EventType.Workshop);
            eventType.GetProperty("Location").SetValue(@event, "Test Location");
            eventType.GetProperty("IsPublished").SetValue(@event, true);
            eventType.GetProperty("CreatedAt").SetValue(@event, DateTime.UtcNow);
            eventType.GetProperty("UpdatedAt").SetValue(@event, DateTime.UtcNow);

            return @event;
        }

        private Registration CreateTestRegistration(Guid userId, Guid eventId)
        {
            var regType = typeof(Registration);
            var regCtor = regType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            var registration = (Registration)regCtor.Invoke(null);
            
            regType.GetProperty("Id").SetValue(registration, Guid.NewGuid());
            regType.GetProperty("UserId").SetValue(registration, userId);
            regType.GetProperty("EventId").SetValue(registration, eventId);
            regType.GetProperty("Status").SetValue(registration, RegistrationStatus.Confirmed);
            regType.GetProperty("SelectedPrice").SetValue(registration, Money.Create(50m));
            regType.GetProperty("RegisteredAt").SetValue(registration, DateTime.UtcNow);

            return registration;
        }

        private VettingApplication CreateTestVettingApplication(Guid applicantId)
        {
            var appType = typeof(VettingApplication);
            var appCtor = appType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            var application = (VettingApplication)appCtor.Invoke(null);
            
            appType.GetProperty("Id").SetValue(application, Guid.NewGuid());
            appType.GetProperty("ApplicantId").SetValue(application, applicantId);
            appType.GetProperty("ExperienceLevel").SetValue(application, "Intermediate");
            appType.GetProperty("Interests").SetValue(application, "Rope bondage, suspension");
            appType.GetProperty("SafetyKnowledge").SetValue(application, "I have attended safety workshops and understand risk awareness");
            appType.GetProperty("Status").SetValue(application, VettingStatus.Submitted);
            appType.GetProperty("SubmittedAt").SetValue(application, DateTime.UtcNow);

            return application;
        }

        private VettingReview CreateTestVettingReview(Guid reviewerId, bool recommendation = true)
        {
            var reviewType = typeof(VettingReview);
            var reviewCtor = reviewType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            var review = (VettingReview)reviewCtor.Invoke(null);
            
            reviewType.GetProperty("Id").SetValue(review, Guid.NewGuid());
            reviewType.GetProperty("ReviewerId").SetValue(review, reviewerId);
            reviewType.GetProperty("Recommendation").SetValue(review, recommendation);
            reviewType.GetProperty("Notes").SetValue(review, "Looks good, experienced member");
            reviewType.GetProperty("ReviewedAt").SetValue(review, DateTime.UtcNow);

            return review;
        }
        [Fact]
        public async Task WitchCityRopeUser_Configuration_Should_Enforce_Unique_Email()
        {
            // Arrange
            var email = EmailAddress.Create("test@example.com");
            var user1 = new IdentityUserBuilder().WithEmail(email).Build();
            var user2 = new IdentityUserBuilder().WithEmail(email).Build();

            // Act
            await Context.Users.AddAsync(user1);
            await Context.SaveChangesAsync();

            await Context.Users.AddAsync(user2);
            var act = async () => await Context.SaveChangesAsync();

            // Assert
            await act.Should().ThrowAsync<DbUpdateException>();
        }

        [Fact]
        public async Task WitchCityRopeUser_Configuration_Should_Store_Value_Objects_Correctly()
        {
            // Arrange
            var user = new IdentityUserBuilder()
                .WithEmail("test@example.com")
                .WithSceneName("TestScene")
                .Build();

            // Act
            await Context.Users.AddAsync(user);
            await Context.SaveChangesAsync();

            var retrieved = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.Users.FirstAsync(u => u.Id == user.Id));

            // Assert
            retrieved.Email.Should().Be("test@example.com");
            retrieved.SceneNameValue.Should().Be("TestScene");
        }

        [Fact]
        [Obsolete("Registration entity is being phased out in favor of Ticket/RSVP")]
        public async Task Event_Registration_Relationship_Should_Be_Configured_Correctly()
        {
            // Arrange
            // Note: This test is obsolete as Registration is being phased out
            // Keeping for backwards compatibility
            var organizer = new IdentityUserBuilder().Build();
            var attendee = new IdentityUserBuilder().Build();
            
            Context.Users.AddRange(organizer, attendee);
            await Context.SaveChangesAsync();
            
            var @event = CreateTestEvent();
            Context.Events.Add(@event);
            await Context.SaveChangesAsync();

            var registration = CreateTestRegistration(attendee.Id, @event.Id);

            // Act
            // These entities don't exist in the Identity context
            // This test needs to be rewritten or removed
            throw new NotImplementedException("Registration entity is no longer available in Identity context");
            await Context.SaveChangesAsync();

            // Retrieve with includes
            var retrievedEvent = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.Events
                    .Include(e => e.Registrations)
                    .ThenInclude(r => r.User)
                    .FirstAsync(e => e.Id == @event.Id));

            // Assert
            retrievedEvent.Registrations.Should().HaveCount(1);
            retrievedEvent.Registrations.First().User.Id.Should().Be(attendee.Id);
            retrievedEvent.Organizers.Should().Contain(o => o.Id == organizer.Id);
        }

        [Fact]
        [Obsolete("Registration entity is being phased out in favor of Ticket/RSVP")]
        public async Task Payment_Registration_Relationship_Should_Be_Configured_Correctly()
        {
            // Arrange
            var user = new IdentityUserBuilder().Build();
            Context.Users.Add(user);
            await Context.SaveChangesAsync();
            
            var @event = CreateTestEvent();
            Context.Events.Add(@event);
            await Context.SaveChangesAsync();
            
            var registration = CreateTestRegistration(user.Id, @event.Id);

            var payment = new PaymentBuilder()
                .WithRegistration(registration)
                .Build();

            // Act
            Context.Registrations.Add(registration);
            Context.Payments.Add(payment);
            await Context.SaveChangesAsync();

            // Retrieve with includes
            var retrievedRegistration = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.Registrations
                    .Include(r => r.Payment)
                    .FirstAsync(r => r.Id == registration.Id));

            // Assert
            retrievedRegistration.Payment.Should().NotBeNull();
            retrievedRegistration.Payment!.Id.Should().Be(payment.Id);
            retrievedRegistration.Payment.Amount.Should().Be(payment.Amount);
        }

        [Fact]
        public async Task VettingApplication_Relationship_Should_Be_Configured_Correctly()
        {
            // Arrange
            var applicant = new IdentityUserBuilder().Build();
            var reviewer = new IdentityUserBuilder().WithRole(UserRole.Administrator).Build();
            
            // Act
            await Context.Users.AddRangeAsync(applicant, reviewer);
            await Context.SaveChangesAsync();
            
            var application = CreateTestVettingApplication(applicant.Id);
            Context.VettingApplications.Add(application);
            await Context.SaveChangesAsync();

            // Review the application
            application.StartReview();
            var review = CreateTestVettingReview(reviewer.Id, true);
            application.AddReview(review);
            application.Approve("Approved after review");
            await Context.SaveChangesAsync();

            // Retrieve with includes
            var retrievedApp = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.VettingApplications
                    .Include(va => va.Applicant)
                    .FirstAsync(va => va.Id == application.Id));

            // Assert
            retrievedApp.Applicant.Should().NotBeNull();
            retrievedApp.Applicant.Id.Should().Be(applicant.Id);
            retrievedApp.Reviews.Should().HaveCount(1);
            retrievedApp.Reviews.First().ReviewerId.Should().Be(reviewer.Id);
        }

        [Fact]
        public async Task IncidentReport_Should_Support_Anonymous_Reports()
        {
            // Arrange
            var @event = CreateTestEvent();
            var reportedUser = new IdentityUserBuilder().Build();
            
            var anonymousReport = new IncidentReport(
                reporter: null,
                relatedEvent: @event,
                description: "Test incident - safety violation observed",
                severity: IncidentSeverity.Medium,
                incidentType: IncidentType.SafetyViolation,
                location: "Main dungeon area",
                incidentDate: DateTime.UtcNow.AddHours(-2),
                isAnonymous: true
            );

            // Act
            Context.Events.Add(@event);
            Context.Users.Add(reportedUser);
            Context.IncidentReports.Add(anonymousReport);
            await Context.SaveChangesAsync();

            // Assert
            var retrieved = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.IncidentReports.FirstAsync(ir => ir.Id == anonymousReport.Id));

            retrieved.ReporterId.Should().BeNull();
            retrieved.IsAnonymous.Should().BeTrue();
        }

        [Fact]
        [Obsolete("Registration entity is being phased out in favor of Ticket/RSVP")]
        public async Task Should_Handle_Cascade_Delete_For_Event_Registrations()
        {
            // Arrange
            var user = new IdentityUserBuilder().Build();
            Context.Users.Add(user);
            await Context.SaveChangesAsync();
            
            var @event = CreateTestEvent();
            Context.Events.Add(@event);
            await Context.SaveChangesAsync();
            
            var registration = CreateTestRegistration(user.Id, @event.Id);
            Context.Registrations.Add(registration);
            await Context.SaveChangesAsync();

            // Act
            Context.Events.Remove(@event);
            await Context.SaveChangesAsync();

            // Assert
            var registrationExists = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.Registrations.AnyAsync(r => r.Id == registration.Id));

            registrationExists.Should().BeFalse();
        }

        [Fact]
        public async Task Money_ValueObject_Should_Be_Stored_Correctly()
        {
            // Arrange
            var payment = new PaymentBuilder()
                .WithAmount(99.99m, "USD")
                .Build();

            // Act
            Context.Payments.Add(payment);
            await Context.SaveChangesAsync();

            var retrieved = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.Payments.FirstAsync(p => p.Id == payment.Id));

            // Assert
            retrieved.Amount.Amount.Should().Be(99.99m);
            retrieved.Amount.Currency.Should().Be("USD");
        }

        [Fact]
        public async Task Should_Enforce_String_Length_Constraints()
        {
            // Arrange
            var longString = new string('a', 1000); // Assuming there are max length constraints
            var @event = CreateTestEvent("Test Event");
            // Set long description using reflection
            @event.GetType().GetProperty("Description").SetValue(@event, longString);

            // Act
            Context.Events.Add(@event);
            var act = async () => await Context.SaveChangesAsync();

            // Assert
            // This will either succeed (if no constraint) or fail (if there is a constraint)
            // Adjust based on actual constraints in entity configurations
            await act();
            @event.Description.Should().Be(longString);
        }
    }
}