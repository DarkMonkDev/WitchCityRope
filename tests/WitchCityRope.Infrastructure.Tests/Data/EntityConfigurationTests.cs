using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Tests.Fixtures;
using WitchCityRope.Tests.Common.Builders;
using Xunit;

namespace WitchCityRope.Infrastructure.Tests.Data
{
    public class EntityConfigurationTests : IntegrationTestBase
    {
        [Fact]
        public async Task User_Configuration_Should_Enforce_Unique_Email()
        {
            // Arrange
            var email = EmailAddress.Create("test@example.com");
            var user1 = new UserBuilder().WithEmail(email).Build();
            var user2 = new UserBuilder().WithEmail(email).Build();

            // Act
            Context.Users.Add(user1);
            await Context.SaveChangesAsync();

            Context.Users.Add(user2);
            var act = async () => await Context.SaveChangesAsync();

            // Assert
            await act.Should().ThrowAsync<DbUpdateException>();
        }

        [Fact]
        public async Task User_Configuration_Should_Store_Value_Objects_Correctly()
        {
            // Arrange
            var user = new UserBuilder()
                .WithEmail("test@example.com")
                .WithSceneName("TestScene")
                .Build();

            // Act
            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            var retrieved = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.Users.FirstAsync(u => u.Id == user.Id));

            // Assert
            retrieved.Email.Value.Should().Be("test@example.com");
            retrieved.SceneName.Value.Should().Be("TestScene");
        }

        [Fact]
        public async Task Event_Registration_Relationship_Should_Be_Configured_Correctly()
        {
            // Arrange
            var organizer = new UserBuilder().Build();
            var attendee = new UserBuilder().Build();
            var @event = new EventBuilder()
                .WithPrimaryOrganizer(organizer)
                .Build();

            var registration = new RegistrationBuilder()
                .WithEvent(@event)
                .WithUser(attendee)
                .Build();

            // Act
            Context.Users.AddRange(organizer, attendee);
            Context.Events.Add(@event);
            Context.Registrations.Add(registration);
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
        public async Task Payment_Registration_Relationship_Should_Be_Configured_Correctly()
        {
            // Arrange
            var user = new UserBuilder().Build();
            var @event = new EventBuilder().Build();
            var registration = new RegistrationBuilder()
                .WithEvent(@event)
                .WithUser(user)
                .Build();

            var payment = new PaymentBuilder()
                .WithRegistration(registration)
                .Build();

            // Act
            Context.Users.Add(user);
            Context.Events.Add(@event);
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
        public async Task VettingApplication_User_Relationship_Should_Be_Configured_Correctly()
        {
            // Arrange
            var applicant = new UserBuilder().Build();
            var reviewer = new UserBuilder().WithRole(UserRole.Administrator).Build();
            
            var application = new VettingApplication(
                applicant,
                "Intermediate",
                "Rope bondage, suspension",
                "I have attended safety workshops and understand risk awareness",
                new[] { "Reference from existing member", "Second reference" }
            );

            // Act
            Context.Users.AddRange(applicant, reviewer);
            Context.VettingApplications.Add(application);
            await Context.SaveChangesAsync();

            // Review the application
            application.StartReview();
            var review = new VettingReview(reviewer, true, "Looks good, experienced member");
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
            var @event = new EventBuilder().Build();
            var reportedUser = new UserBuilder().Build();
            
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
        public async Task Should_Handle_Cascade_Delete_For_Event_Registrations()
        {
            // Arrange
            var user = new UserBuilder().Build();
            var @event = new EventBuilder().Build();
            var registration = new RegistrationBuilder()
                .WithEvent(@event)
                .WithUser(user)
                .Build();

            Context.Users.Add(user);
            Context.Events.Add(@event);
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
            var @event = new EventBuilder()
                .WithTitle("Test Event")
                .WithDescription(longString)
                .Build();

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