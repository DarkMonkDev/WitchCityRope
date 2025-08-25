using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Tests.Common.Builders;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.Api.Tests.Features.Events
{
    /// <summary>
    /// TDD tests for Event Session Matrix implementation
    /// Testing the core concept: Sessions are atomic units of capacity, not ticket types
    /// 
    /// Test Structure:
    /// 1. Event Session Creation & Management
    /// 2. Ticket Type Session Mapping
    /// 3. Capacity Calculations Across Sessions
    /// 4. RSVP vs Ticket Handling for Social Events
    /// 5. Complex Multi-Session Scenarios
    /// </summary>
    [Collection("Database")]
    public class EventSessionTests : DatabaseTestBase
    {
        public EventSessionTests(DatabaseTestFixture fixture) : base(fixture)
        {
        }

        #region Event Session Creation Tests

        [Fact]
        public async Task CreateEvent_WithMultipleSessions_CreatesS1S2S3Structure()
        {
            // Arrange
            var organizer = new UserBuilder()
                .WithEmail($"organizer-{Guid.NewGuid():N}@test.com")
                .AsOrganizer()
                .Build();
            
            await DbContext.Users.AddAsync(organizer);
            await DbContext.SaveChangesAsync();

            // Act & Assert - This is TDD, so we're testing the interface we want to build
            var eventWithSessions = new EventWithSessionsBuilder()
                .WithTitle("Advanced Rope Workshop Series")
                .WithOrganizer(organizer)
                .WithSession("S1", "Friday Workshop", capacity: 20, date: DateTime.UtcNow.AddDays(7))
                .WithSession("S2", "Saturday Workshop", capacity: 25, date: DateTime.UtcNow.AddDays(8))
                .WithSession("S3", "Sunday Workshop", capacity: 18, date: DateTime.UtcNow.AddDays(9))
                .Build();

            // Assert - Sessions should be atomic capacity units
            eventWithSessions.Sessions.Should().HaveCount(3);
            eventWithSessions.Sessions.Should().Contain(s => s.SessionName == "S1" && s.Capacity == 20);
            eventWithSessions.Sessions.Should().Contain(s => s.SessionName == "S2" && s.Capacity == 25);
            eventWithSessions.Sessions.Should().Contain(s => s.SessionName == "S3" && s.Capacity == 18);
            
            // Each session should have unique capacity
            var sessionCapacities = eventWithSessions.Sessions.Select(s => s.Capacity).ToList();
            sessionCapacities.Should().BeEquivalentTo(new[] { 20, 25, 18 });
        }

        [Fact]
        public async Task CreateEventSession_WithInvalidCapacity_ThrowsDomainException()
        {
            // Arrange & Act & Assert - TDD for validation
            var act = () => new EventSessionBuilder()
                .WithSessionName("Invalid Session")
                .WithCapacity(0) // Invalid capacity
                .Build();

            act.Should().Throw<ArgumentException>()
                .WithMessage("*capacity*greater than zero*");
        }

        [Fact]
        public async Task CreateEventSession_WithOverlappingTimes_ThrowsDomainException()
        {
            // Arrange
            var sessionDate = DateTime.UtcNow.AddDays(7);
            
            // Act & Assert - TDD for time validation
            var act = () => new EventWithSessionsBuilder()
                .WithSession("S1", "Morning Workshop", 20, sessionDate, TimeSpan.FromHours(9), TimeSpan.FromHours(12))
                .WithSession("S2", "Overlap Workshop", 20, sessionDate, TimeSpan.FromHours(11), TimeSpan.FromHours(14)) // Overlaps S1
                .Build();

            act.Should().Throw<DomainException>()
                .WithMessage("*sessions cannot overlap*same date*");
        }

        #endregion

        #region Ticket Type Session Mapping Tests

        [Fact]
        public async Task CreateTicketType_WithSessionMapping_MapsToCorrectSessions()
        {
            // Arrange
            var organizer = new UserBuilder().AsOrganizer().Build();
            await DbContext.Users.AddAsync(organizer);
            await DbContext.SaveChangesAsync();

            var eventWithSessions = new EventWithSessionsBuilder()
                .WithOrganizer(organizer)
                .WithSession("S1", "Friday", 20, DateTime.UtcNow.AddDays(7))
                .WithSession("S2", "Saturday", 20, DateTime.UtcNow.AddDays(8))
                .WithSession("S3", "Sunday", 18, DateTime.UtcNow.AddDays(9))
                .Build();

            // Act - TDD: Create ticket types that span sessions
            var fullSeriesPass = new TicketTypeBuilder()
                .WithName("Full Series Pass")
                .WithPrice(150m)
                .IncludingSessions("S1", "S2", "S3") // Spans all 3 sessions
                .Build();

            var weekendPass = new TicketTypeBuilder()
                .WithName("Weekend Pass")
                .WithPrice(100m)
                .IncludingSessions("S2", "S3") // Only Saturday/Sunday
                .Build();

            var fridayOnly = new TicketTypeBuilder()
                .WithName("Friday Only")
                .WithPrice(60m)
                .IncludingSessions("S1") // Single session
                .Build();

            // Assert - Ticket types should map to correct sessions
            fullSeriesPass.IncludedSessions.Should().HaveCount(3);
            fullSeriesPass.IncludedSessions.Should().Contain("S1", "S2", "S3");

            weekendPass.IncludedSessions.Should().HaveCount(2);
            weekendPass.IncludedSessions.Should().Contain("S2", "S3");
            weekendPass.IncludedSessions.Should().NotContain("S1");

            fridayOnly.IncludedSessions.Should().HaveCount(1);
            fridayOnly.IncludedSessions.Should().Contain("S1");
        }

        [Fact]
        public async Task CreateTicketType_MappingToNonExistentSession_ThrowsDomainException()
        {
            // Arrange & Act & Assert - TDD for validation
            var act = () => new TicketTypeBuilder()
                .WithName("Invalid Ticket")
                .IncludingSessions("S1", "S4") // S4 doesn't exist
                .Build();

            act.Should().Throw<DomainException>()
                .WithMessage("*session 'S4' does not exist*");
        }

        #endregion

        #region Capacity Calculation Tests

        [Fact]
        public async Task CalculateAvailability_SingleSessionTicket_ReturnsSessionCapacity()
        {
            // Arrange
            var event1 = CreateEventWithSessions("S1:20,S2:25,S3:18");
            var fridayOnlyTicket = CreateTicketType("Friday Only", "S1", 60m);

            // Act - TDD: Availability should be based on limiting session
            var availability = event1.CalculateTicketAvailability(fridayOnlyTicket);

            // Assert - For single session ticket, availability = session capacity
            availability.Should().Be(20); // S1 capacity
        }

        [Fact]
        public async Task CalculateAvailability_MultiSessionTicket_ReturnsLimitingSessionCapacity()
        {
            // Arrange - Sessions with different capacities
            var event1 = CreateEventWithSessions("S1:20,S2:25,S3:18"); // S3 has lowest capacity
            var fullSeriesTicket = CreateTicketType("Full Series", "S1,S2,S3", 150m);

            // Act - TDD: Multi-session availability limited by smallest session
            var availability = event1.CalculateTicketAvailability(fullSeriesTicket);

            // Assert - Availability limited by S3 (18 capacity)
            availability.Should().Be(18); // Limited by S3's capacity of 18
        }

        [Fact]
        public async Task CalculateAvailability_WithExistingRegistrations_ConsumesFromAllSessions()
        {
            // Arrange
            var event1 = CreateEventWithSessions("S1:20,S2:25,S3:18");
            var fullSeriesTicket = CreateTicketType("Full Series", "S1,S2,S3", 150m);

            // Simulate 5 people bought full series passes (consumes from all sessions)
            event1.RegisterAttendee(CreateUser(), fullSeriesTicket, quantity: 5);

            // Act - TDD: Should reduce availability across all sessions
            var availability = event1.CalculateTicketAvailability(fullSeriesTicket);

            // Assert - All sessions reduced by 5
            // S1: 20-5=15, S2: 25-5=20, S3: 18-5=13 → Limited by S3 = 13
            availability.Should().Be(13);
        }

        [Fact]
        public async Task CalculateAvailability_MixedTicketTypes_CorrectlyTracksCrossSessionCapacity()
        {
            // Arrange - Complex scenario: multiple ticket types affecting different sessions
            var event1 = CreateEventWithSessions("S1:20,S2:25,S3:18");
            
            var fullSeriesTicket = CreateTicketType("Full Series", "S1,S2,S3", 150m);
            var weekendPassTicket = CreateTicketType("Weekend Pass", "S2,S3", 100m);
            var fridayOnlyTicket = CreateTicketType("Friday Only", "S1", 60m);

            // Simulate registrations:
            // - 3 Full Series (affects S1, S2, S3)
            // - 5 Weekend Pass (affects S2, S3)  
            // - 2 Friday Only (affects S1)
            event1.RegisterAttendee(CreateUser(), fullSeriesTicket, quantity: 3);
            event1.RegisterAttendee(CreateUser(), weekendPassTicket, quantity: 5);
            event1.RegisterAttendee(CreateUser(), fridayOnlyTicket, quantity: 2);

            // Act & Assert - TDD: Each ticket type should show correct availability
            
            // Friday Only availability: S1 capacity - (3 full series + 2 friday only) = 20 - 5 = 15
            var fridayAvailability = event1.CalculateTicketAvailability(fridayOnlyTicket);
            fridayAvailability.Should().Be(15);

            // Weekend Pass availability: Limited by S2 or S3
            // S2: 25 - (3 full series + 5 weekend) = 17
            // S3: 18 - (3 full series + 5 weekend) = 10 → Limiting factor
            var weekendAvailability = event1.CalculateTicketAvailability(weekendPassTicket);
            weekendAvailability.Should().Be(10);

            // Full Series availability: Limited by most constrained session (S3)
            // S3: 18 - (3 full series + 5 weekend) = 10
            var fullSeriesAvailability = event1.CalculateTicketAvailability(fullSeriesTicket);
            fullSeriesAvailability.Should().Be(10);
        }

        #endregion

        #region RSVP vs Ticket Handling for Social Events

        [Fact]
        public async Task SocialEvent_WithRSVPMode_DoesNotRequirePayment()
        {
            // Arrange - TDD: Social events should support free RSVP
            var socialEvent = new EventWithSessionsBuilder()
                .WithEventType(EventType.Social) // Key: Social event
                .WithTitle("Monthly Social Gathering")
                .WithSession("S1", "Evening Social", 50, DateTime.UtcNow.AddDays(7))
                .Build();

            var rsvpTicket = new TicketTypeBuilder()
                .WithName("Free RSVP")
                .WithPrice(0m) // Free
                .WithRSVPMode(true) // No payment required
                .IncludingSessions("S1")
                .Build();

            var attendee = CreateUser();

            // Act - TDD: Should allow RSVP without payment
            var result = socialEvent.RegisterAttendee(attendee, rsvpTicket, quantity: 1);

            // Assert - Registration should succeed without payment
            result.RequiresPayment.Should().BeFalse();
            result.Status.Should().Be(RegistrationStatus.Confirmed); // Immediate confirmation
            result.PaymentStatus.Should().Be(PaymentStatus.NotRequired);
        }

        [Fact]
        public async Task SocialEvent_WithPaidTickets_RequiresPayment()
        {
            // Arrange - TDD: Social events can also have paid options
            var socialEvent = new EventWithSessionsBuilder()
                .WithEventType(EventType.Social)
                .WithTitle("Premium Social Event")
                .WithSession("S1", "VIP Social", 25, DateTime.UtcNow.AddDays(7))
                .Build();

            var paidTicket = new TicketTypeBuilder()
                .WithName("VIP Access")
                .WithPrice(25m) // Paid
                .WithRSVPMode(false) // Requires payment
                .IncludingSessions("S1")
                .Build();

            var attendee = CreateUser();

            // Act - TDD: Should require payment even for social events
            var result = socialEvent.RegisterAttendee(attendee, paidTicket, quantity: 1);

            // Assert - Registration should require payment
            result.RequiresPayment.Should().BeTrue();
            result.Status.Should().Be(RegistrationStatus.Pending); // Awaiting payment
            result.PaymentStatus.Should().Be(PaymentStatus.Pending);
        }

        [Fact]
        public async Task ClassEvent_AlwaysRequiresPayment_EvenWithZeroPrice()
        {
            // Arrange - TDD: Class events follow different rules than social
            var classEvent = new EventWithSessionsBuilder()
                .WithEventType(EventType.Class)
                .WithTitle("Free Beginner Class")
                .WithSession("S1", "Intro Class", 15, DateTime.UtcNow.AddDays(7))
                .Build();

            var freeClassTicket = new TicketTypeBuilder()
                .WithName("Free Class Access")
                .WithPrice(0m) // Free but still requires "payment" processing
                .WithRSVPMode(false) // Classes always process through payment system
                .IncludingSessions("S1")
                .Build();

            var attendee = CreateUser();

            // Act - TDD: Even free classes go through payment processing
            var result = classEvent.RegisterAttendee(attendee, freeClassTicket, quantity: 1);

            // Assert - Should go through payment flow (even for $0)
            result.RequiresPayment.Should().BeTrue(); // Always true for classes
            result.Status.Should().Be(RegistrationStatus.Pending);
            result.PaymentStatus.Should().Be(PaymentStatus.Pending);
        }

        #endregion

        #region Complex Multi-Session Scenarios

        [Fact]
        public async Task ComplexWorkshop_ThreeDayEvent_SupportsAllTicketCombinations()
        {
            // Arrange - TDD: Real-world complex scenario
            var complexEvent = new EventWithSessionsBuilder()
                .WithTitle("Advanced Rope Mastery Series")
                .WithEventType(EventType.Class)
                .WithSession("S1", "Foundations (Day 1)", 20, DateTime.UtcNow.AddDays(7))
                .WithSession("S2", "Intermediate (Day 2)", 18, DateTime.UtcNow.AddDays(8)) // Lower capacity venue
                .WithSession("S3", "Advanced (Day 3)", 15, DateTime.UtcNow.AddDays(9)) // Smallest venue
                .Build();

            // Multiple ticket types with different session combinations
            var allTicketTypes = new[]
            {
                CreateTicketType("Full Series (All 3 Days)", "S1,S2,S3", 300m),
                CreateTicketType("Days 1-2 Only", "S1,S2", 200m),
                CreateTicketType("Days 2-3 Only", "S2,S3", 220m), // More expensive (advanced content)
                CreateTicketType("Day 1 Only", "S1", 120m),
                CreateTicketType("Day 2 Only", "S2", 130m),
                CreateTicketType("Day 3 Only", "S3", 140m) // Most expensive single day
            };

            // Simulate realistic registration pattern
            complexEvent.RegisterAttendee(CreateUser(), allTicketTypes[0], 8); // 8 full series
            complexEvent.RegisterAttendee(CreateUser(), allTicketTypes[3], 5); // 5 day 1 only
            complexEvent.RegisterAttendee(CreateUser(), allTicketTypes[4], 3); // 3 day 2 only
            complexEvent.RegisterAttendee(CreateUser(), allTicketTypes[2], 2); // 2 days 2-3

            // Act & Assert - TDD: Verify complex availability calculations

            // Day 1 (S1): Capacity 20, Used by: 8 full + 5 day1 = 13, Available = 7
            var day1Availability = complexEvent.CalculateTicketAvailability(allTicketTypes[3]);
            day1Availability.Should().Be(7);

            // Day 2 (S2): Capacity 18, Used by: 8 full + 3 day2 + 2 days2-3 = 13, Available = 5
            var day2Availability = complexEvent.CalculateTicketAvailability(allTicketTypes[4]);
            day2Availability.Should().Be(5);

            // Day 3 (S3): Capacity 15, Used by: 8 full + 2 days2-3 = 10, Available = 5
            var day3Availability = complexEvent.CalculateTicketAvailability(allTicketTypes[5]);
            day3Availability.Should().Be(5);

            // Full Series: Limited by most constrained session (Day 2 and Day 3 both at 5)
            var fullSeriesAvailability = complexEvent.CalculateTicketAvailability(allTicketTypes[0]);
            fullSeriesAvailability.Should().Be(5);

            // Days 2-3: Limited by Day 2 or Day 3 (both at 5)
            var days23Availability = complexEvent.CalculateTicketAvailability(allTicketTypes[2]);
            days23Availability.Should().Be(5);
        }

        [Theory]
        [InlineData("S1", 1, 19)] // Single session, 1 registration
        [InlineData("S1,S2", 1, 19)] // Multi-session limited by S1 (20 capacity)
        [InlineData("S1,S2,S3", 1, 14)] // All sessions limited by S3 (15 capacity) 
        [InlineData("S2,S3", 3, 12)] // Weekend pass, 3 registrations
        public async Task TicketAvailability_VariousScenarios_CalculatesCorrectly(
            string sessionIds, int registrations, int expectedAvailability)
        {
            // Arrange - TDD: Theory test for edge cases
            var testEvent = CreateEventWithSessions("S1:20,S2:20,S3:15");
            var ticketType = CreateTicketType("Test Ticket", sessionIds, 100m);
            
            for (int i = 0; i < registrations; i++)
            {
                testEvent.RegisterAttendee(CreateUser(), ticketType, quantity: 1);
            }

            // Act
            var availability = testEvent.CalculateTicketAvailability(ticketType);

            // Assert
            availability.Should().Be(expectedAvailability);
        }

        #endregion

        #region Helper Methods for TDD Test Data

        // These methods represent the interfaces we want to build
        // They will fail initially (Red phase of TDD) until we implement them

        private EventWithSessions CreateEventWithSessions(string sessionSpec)
        {
            // Parse "S1:20,S2:25,S3:18" format
            var sessions = sessionSpec.Split(',')
                .Select(s => s.Split(':'))
                .Select(parts => new { Name = parts[0], Capacity = int.Parse(parts[1]) })
                .ToList();

            var builder = new EventWithSessionsBuilder()
                .WithTitle("Test Event")
                .WithOrganizer(CreateUser());

            foreach (var session in sessions)
            {
                builder.WithSession(session.Name, $"{session.Name} Session", 
                    session.Capacity, DateTime.UtcNow.AddDays(7));
            }

            return builder.Build();
        }

        private TicketType CreateTicketType(string name, string sessionIds, decimal price)
        {
            return new TicketTypeBuilder()
                .WithName(name)
                .WithPrice(price)
                .IncludingSessions(sessionIds.Split(','))
                .Build();
        }

        private User CreateUser()
        {
            return new UserBuilder()
                .WithEmail($"test-{Guid.NewGuid():N}@test.com")
                .Build();
        }

        #endregion
    }
}