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
    public class ComplexQueryTests : IntegrationTestBase
    {
        protected override async Task SeedDataAsync()
        {
            var users = new List<WitchCityRopeUser>();
            var events = new List<Event>();

            // Create users
            for (int i = 0; i < 10; i++)
            {
                users.Add(new IdentityUserBuilder()
                    .WithEmail($"user{i}@example.com")
                    .WithSceneName($"User{i}")
                    .Build());
            }

            // Add users to context
            Context.Users.AddRange(users);
            await Context.SaveChangesAsync();

            // Create events using reflection to bypass constructor requirements
            var eventType = typeof(Event);
            var eventCtor = eventType.GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            for (int i = 0; i < 5; i++)
            {
                var @event = (Event)eventCtor.Invoke(null);
                
                // Use reflection to set properties
                var idProp = eventType.GetProperty("Id");
                idProp.SetValue(@event, Guid.NewGuid());
                
                var titleProp = eventType.GetProperty("Title");
                titleProp.SetValue(@event, $"Event {i}");
                
                var descProp = eventType.GetProperty("Description");
                descProp.SetValue(@event, "Test event description");
                
                var startProp = eventType.GetProperty("StartDate");
                startProp.SetValue(@event, DateTime.UtcNow.AddDays(i - 2));
                
                var endProp = eventType.GetProperty("EndDate");
                endProp.SetValue(@event, DateTime.UtcNow.AddDays(i - 2).AddHours(3));
                
                var capProp = eventType.GetProperty("Capacity");
                capProp.SetValue(@event, 20);
                
                var typeProp = eventType.GetProperty("EventType");
                typeProp.SetValue(@event, EventType.Workshop);
                
                var locProp = eventType.GetProperty("Location");
                locProp.SetValue(@event, "Test Location");
                
                var pubProp = eventType.GetProperty("IsPublished");
                pubProp.SetValue(@event, true);
                
                var createdProp = eventType.GetProperty("CreatedAt");
                createdProp.SetValue(@event, DateTime.UtcNow);
                
                var updatedProp = eventType.GetProperty("UpdatedAt");
                updatedProp.SetValue(@event, DateTime.UtcNow);

                events.Add(@event);
            }

            Context.Events.AddRange(events);
            await Context.SaveChangesAsync();

            // Create registrations using reflection
            var regType = typeof(Registration);
            var regCtor = regType.GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            for (int i = 0; i < 3; i++)
            {
                for (int j = 1; j <= 5; j++)
                {
                    var registration = (Registration)regCtor.Invoke(null);
                    
                    var idProp = regType.GetProperty("Id");
                    idProp.SetValue(registration, Guid.NewGuid());
                    
                    var userIdProp = regType.GetProperty("UserId");
                    userIdProp.SetValue(registration, users[j].Id);
                    
                    var eventIdProp = regType.GetProperty("EventId");
                    eventIdProp.SetValue(registration, events[i].Id);
                    
                    var statusProp = regType.GetProperty("Status");
                    statusProp.SetValue(registration, RegistrationStatus.Confirmed);
                    
                    var priceProp = regType.GetProperty("SelectedPrice");
                    priceProp.SetValue(registration, Money.Create(50m));
                    
                    var regDateProp = regType.GetProperty("RegisteredAt");
                    regDateProp.SetValue(registration, DateTime.UtcNow);
                    
                    Context.Registrations.Add(registration);
                }
            }

            await Context.SaveChangesAsync();
        }

        [Fact]
        public async Task Should_Query_Upcoming_Events_With_Available_Spots()
        {
            // Act
            var upcomingEventsWithSpots = await Context.Events
                .Include(e => e.Registrations)
                .Where(e => e.StartDate > DateTime.UtcNow)
                .Where(e => e.IsPublished)
                .Select(e => new
                {
                    Event = e,
                    AvailableSpots = e.Capacity - e.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed),
                    ConfirmedAttendees = e.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed)
                })
                .Where(x => x.AvailableSpots > 0)
                .OrderBy(x => x.Event.StartDate)
                .ToListAsync();

            // Assert
            upcomingEventsWithSpots.Should().NotBeEmpty();
            upcomingEventsWithSpots.All(x => x.AvailableSpots > 0).Should().BeTrue();
            upcomingEventsWithSpots.All(x => x.Event.StartDate > DateTime.UtcNow).Should().BeTrue();
        }

        [Fact]
        public async Task Should_Query_User_Registration_History()
        {
            // Arrange
            var userId = (await Context.Users.FirstAsync()).Id;

            // Act
            var userHistory = await Context.Registrations
                .Include(r => r.Event)
                .Include(r => r.Payment)
                .Where(r => r.UserId == userId)
                .Select(r => new
                {
                    EventTitle = r.Event.Title,
                    EventDate = r.Event.StartDate,
                    RegistrationStatus = r.Status,
                    PaymentStatus = r.Payment != null ? r.Payment.Status : (PaymentStatus?)null,
                    RegistrationDate = r.RegisteredAt
                })
                .OrderByDescending(x => x.RegistrationDate)
                .ToListAsync();

            // Assert
            userHistory.Should().NotBeNull();
            if (userHistory.Any())
            {
                userHistory.Should().BeInDescendingOrder(x => x.RegistrationDate);
            }
        }

        [Fact]
        public async Task Should_Query_Event_Statistics()
        {
            // Act
            var eventStats = await Context.Events
                .Include(e => e.Registrations)
                .ThenInclude(r => r.Payment)
                .Select(e => new
                {
                    EventId = e.Id,
                    Title = e.Title,
                    TotalRegistrations = e.Registrations.Count,
                    ConfirmedRegistrations = e.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed),
                    Revenue = e.Registrations
                        .Where(r => r.Payment != null && r.Payment.Status == PaymentStatus.Completed)
                        .Sum(r => r.Payment!.Amount.Amount),
                    AttendanceRate = e.Registrations.Count > 0 
                        ? (double)e.Registrations.Count(r => r.Status == RegistrationStatus.CheckedIn) / e.Registrations.Count 
                        : 0
                })
                .ToListAsync();

            // Assert
            eventStats.Should().NotBeEmpty();
            eventStats.All(s => s.TotalRegistrations >= s.ConfirmedRegistrations).Should().BeTrue();
        }

        [Fact]
        public async Task Should_Execute_Complex_Join_Query()
        {
            // Act
            var result = await (
                from e in Context.Events
                join r in Context.Registrations on e.Id equals r.EventId into registrations
                from r in registrations.DefaultIfEmpty()
                join u in Context.Users on r.UserId equals u.Id into users
                from u in users.DefaultIfEmpty()
                where e.StartDate > DateTime.UtcNow.AddDays(-7)
                group new { e, r, u } by new { e.Id, e.Title } into g
                select new
                {
                    EventId = g.Key.Id,
                    EventTitle = g.Key.Title,
                    UniqueAttendees = g.Where(x => x.u != null).Select(x => x.u.Id).Distinct().Count(),
                    TotalRegistrations = g.Count(x => x.r != null)
                }
            ).ToListAsync();

            // Assert
            result.Should().NotBeNull();
            result.All(r => r.UniqueAttendees <= r.TotalRegistrations).Should().BeTrue();
        }

        [Fact]
        public async Task Should_Handle_Pagination_Correctly()
        {
            // Arrange
            const int pageSize = 3;
            const int pageNumber = 2;

            // Act
            var totalCount = await Context.Events.CountAsync();
            var pagedResults = await Context.Events
                .OrderBy(e => e.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Assert
            pagedResults.Should().HaveCount(c => c <= pageSize);
            if (totalCount > pageSize)
            {
                pagedResults.Should().NotBeEmpty();
            }
        }

        [Fact]
        public async Task Should_Execute_Raw_SQL_Query()
        {
            // Act
            var popularEvents = await Context.Events
                .FromSqlRaw(@"
                    SELECT e.* 
                    FROM ""Events"" e
                    LEFT JOIN ""Registrations"" r ON e.""Id"" = r.""EventId""
                    GROUP BY e.""Id"", e.""Title"", e.""Description"", e.""StartDate"", 
                             e.""EndDateTime"", e.""VenueId"", e.""Capacity"", e.""Price_Amount"", 
                             e.""Price_Currency"", e.""Status"", e.""Type"", e.""OrganizerId"", 
                             e.""CreatedAt"", e.""UpdatedAt"", e.""RequiresVetting"", e.""ImageUrl""
                    HAVING COUNT(r.""Id"") > 0
                    ORDER BY COUNT(r.""Id"") DESC")
                .ToListAsync();

            // Assert
            popularEvents.Should().NotBeNull();
        }

        [Fact]
        public async Task Should_Use_Compiled_Query_For_Performance()
        {
            // Arrange
            var compiledQuery = EF.CompileAsyncQuery(
                (WitchCityRope.Infrastructure.Data.WitchCityRopeIdentityDbContext ctx, Guid userId) =>
                    ctx.Registrations
                        .Include(r => r.Event)
                        .Where(r => r.UserId == userId && r.Status == RegistrationStatus.Confirmed)
                        .OrderByDescending(r => r.Event.StartDate));

            var userId = (await Context.Users.FirstAsync()).Id;

            // Act
            var query = await compiledQuery(Context, userId);
            var results = await query.ToListAsync();

            // Assert
            results.Should().NotBeNull();
            results.Should().BeInDescendingOrder(r => r.Event.StartDate);
        }

        [Fact]
        public async Task Should_Handle_Complex_Filtering_And_Sorting()
        {
            // Arrange
            var searchTerm = "Event";
            var minDate = DateTime.UtcNow.AddDays(-30);
            var maxDate = DateTime.UtcNow.AddDays(30);

            // Act
            var filteredEvents = await Context.Events
                .Include(e => e.Registrations)
                .Where(e => e.Title.Contains(searchTerm) || e.Description.Contains(searchTerm))
                .Where(e => e.StartDate >= minDate && e.StartDate <= maxDate)
                .Where(e => e.IsPublished)
                .OrderBy(e => e.StartDate)
                .ThenByDescending(e => e.Registrations.Count)
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.StartDate,
                    RegistrationCount = e.Registrations.Count,
                    IsFull = e.Registrations.Count >= e.Capacity
                })
                .ToListAsync();

            // Assert
            filteredEvents.Should().NotBeNull();
            filteredEvents.Should().BeInAscendingOrder(e => e.StartDate);
        }
    }
}