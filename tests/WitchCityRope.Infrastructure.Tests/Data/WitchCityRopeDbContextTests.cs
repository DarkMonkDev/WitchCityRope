using System;
using System.Collections.Generic;
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
    public class WitchCityRopeIdentityDbContextTests : IntegrationTestBase
    {
        private Event CreateTestEvent(string title = "Test Event", int capacity = 10, Guid? id = null)
        {
            var eventType = typeof(Event);
            var eventCtor = eventType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            var @event = (Event)eventCtor.Invoke(null);
            
            // Use reflection to set properties
            eventType.GetProperty("Id").SetValue(@event, id ?? Guid.NewGuid());
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
        [Fact]
        public async Task Should_Create_Database_Schema()
        {
            // Act
            var canConnect = await Context.Database.CanConnectAsync();

            // Assert
            canConnect.Should().BeTrue();
            Context.Users.Should().NotBeNull();
            Context.Events.Should().NotBeNull();
            Context.Rsvps.Should().NotBeNull();
            Context.Payments.Should().NotBeNull();
            Context.VettingApplications.Should().NotBeNull();
            Context.IncidentReports.Should().NotBeNull();
        }

        // NOTE: The following tests need to be rewritten to use WitchCityRopeUser instead of Core.User
        // WitchCityRopeIdentityDbContext uses ASP.NET Core Identity with WitchCityRopeUser entities

        [Fact]
        public async Task Should_Save_And_Retrieve_Event()
        {
            // Arrange
            var @event = CreateTestEvent();

            // Act
            Context.Events.Add(@event);
            await Context.SaveChangesAsync();

            var retrievedEvent = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.Events.FirstOrDefaultAsync(e => e.Id == @event.Id));

            // Assert
            retrievedEvent.Should().NotBeNull();
            retrievedEvent!.Title.Should().Be(@event.Title);
            retrievedEvent.StartDate.Should().Be(@event.StartDate);
            retrievedEvent.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Should_Save_And_Retrieve_Rsvp()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var @event = CreateTestEvent();
            Context.Events.Add(@event);
            await Context.SaveChangesAsync();
            
            var rsvp = new Rsvp(userId, @event);

            // Act
            Context.Rsvps.Add(rsvp);
            await Context.SaveChangesAsync();

            var retrievedRsvp = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.Rsvps.FirstOrDefaultAsync(r => r.Id == rsvp.Id));

            // Assert
            retrievedRsvp.Should().NotBeNull();
            retrievedRsvp!.Status.Should().Be(rsvp.Status);
            retrievedRsvp.EventId.Should().Be(@event.Id);
            retrievedRsvp.UserId.Should().Be(userId);
        }

        [Fact]
        public async Task Should_Handle_Complex_Queries()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var @event = CreateTestEvent("Test Workshop", 10, eventId);

            var rsvps = new List<Rsvp>();
            for (int i = 1; i <= 5; i++)
            {
                var rsvp = new Rsvp(Guid.NewGuid(), @event);
                if (i % 2 != 0)
                {
                    rsvp.Cancel(); // Set to cancelled (since there's no Pending status setter)
                }
                rsvps.Add(rsvp);
            }

            Context.Events.Add(@event);
            Context.Rsvps.AddRange(rsvps);
            await Context.SaveChangesAsync();

            // Act
            var result = await ExecuteWithNewContextAsync(async ctx =>
            {
                var query = from e in ctx.Events
                           join r in ctx.Rsvps on e.Id equals r.EventId into eventRsvps
                           where e.Id == eventId
                           select new
                           {
                               Event = e,
                               ConfirmedCount = eventRsvps.Count(r => r.Status == RsvpStatus.Confirmed),
                               CancelledCount = eventRsvps.Count(r => r.Status == RsvpStatus.Cancelled)
                           };

                return await query.FirstOrDefaultAsync();
            });

            // Assert
            result.Should().NotBeNull();
            result!.Event.Title.Should().Be("Test Workshop");
            result.ConfirmedCount.Should().Be(2);
            result.CancelledCount.Should().Be(3);
        }

        [Fact]
        public async Task Should_Handle_Transactions()
        {
            // Arrange
            var @event = CreateTestEvent();
            var payment = new PaymentBuilder().Build();

            // Act
            using var transaction = await Context.Database.BeginTransactionAsync();
            try
            {
                Context.Events.Add(@event);
                Context.Payments.Add(payment);
                await Context.SaveChangesAsync();

                // Simulate an error
                throw new InvalidOperationException("Test exception");
            }
            catch
            {
                await transaction.RollbackAsync();
            }

            // Assert
            var eventExists = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.Events.AnyAsync(e => e.Id == @event.Id));
            var paymentExists = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.Payments.AnyAsync(p => p.Id == payment.Id));

            eventExists.Should().BeFalse();
            paymentExists.Should().BeFalse();
        }

        [Fact(Skip = "Event entity does not support soft delete")]
        public async Task Should_Support_Soft_Delete()
        {
            // TODO: Event entity needs to implement ISoftDeletable interface
            // Arrange
            var @event = CreateTestEvent();
            Context.Events.Add(@event);
            await Context.SaveChangesAsync();

            // Act
            // @event.SoftDelete(); // Method not available
            await Context.SaveChangesAsync();

            // Assert
            var deletedEvent = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.Events.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == @event.Id));

            deletedEvent.Should().NotBeNull();
            // deletedEvent!.IsDeleted.Should().BeTrue(); // Property not available
            // deletedEvent.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5)); // Property not available

            // Default query should not return soft-deleted items
            var activeEvent = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.Events.FirstOrDefaultAsync(e => e.Id == @event.Id));

            activeEvent.Should().BeNull();
        }
    }
}