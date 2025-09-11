using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Api;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Tests.Common.Builders;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.Tests.Integration.Events
{
    /// <summary>
    /// Integration tests for Event Session Matrix complete workflow
    /// Tests the full stack: React → API → Database → Business Logic
    /// 
    /// Test Coverage:
    /// 1. Event Creation with Sessions and Ticket Types
    /// 2. Session Capacity Management
    /// 3. Complex Ticket Registration Scenarios  
    /// 4. Availability Calculations
    /// 5. RSVP vs Paid Registration Workflows
    /// 6. Cross-Session Capacity Tracking
    /// </summary>
    [Collection("PostgreSQL Integration Tests")]
    public class EventSessionMatrixIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly PostgreSqlIntegrationFixture _dbFixture;

        public EventSessionMatrixIntegrationTests(
            WebApplicationFactory<Program> factory,
            PostgreSqlIntegrationFixture dbFixture)
        {
            _factory = factory;
            _dbFixture = dbFixture;
            _client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace DbContext with test database
                    services.Remove(services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<WitchCityRopeDbContext>))!);
                    services.AddDbContext<WitchCityRopeDbContext>(options =>
                        options.UseNpgsql(_dbFixture.ConnectionString));
                });
            }).CreateClient();
        }

        #region End-to-End Event Creation with Session Matrix

        [Fact]
        public async Task CreateEventWithSessions_CompleteWorkflow_CreatesEventSessionTicketStructure()
        {
            // Arrange - Create organizer user
            var organizerResponse = await CreateTestUser("organizer@test.com", UserRole.Organizer);
            var organizerId = GetUserIdFromResponse(organizerResponse);

            var createEventRequest = new
            {
                Title = "Advanced Rope Workshop Series",
                Description = "3-day intensive rope bondage workshop",
                EventType = EventType.Class,
                Location = "Salem Community Center",
                IsPublished = false,
                Sessions = new[]
                {
                    new { 
                        SessionName = "S1", 
                        SessionDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"),
                        StartTime = "09:00",
                        EndTime = "12:00",
                        Capacity = 20 
                    },
                    new { 
                        SessionName = "S2", 
                        SessionDate = DateTime.UtcNow.AddDays(8).ToString("yyyy-MM-dd"),
                        StartTime = "09:00", 
                        EndTime = "12:00",
                        Capacity = 18 // Different venue, lower capacity
                    },
                    new { 
                        SessionName = "S3", 
                        SessionDate = DateTime.UtcNow.AddDays(9).ToString("yyyy-MM-dd"),
                        StartTime = "09:00",
                        EndTime = "12:00", 
                        Capacity = 15 // Smallest venue
                    }
                },
                TicketTypes = new[]
                {
                    new {
                        Name = "Full Series Pass",
                        Description = "All 3 days of training",
                        Price = 300.00m,
                        IncludedSessions = new[] { "S1", "S2", "S3" }
                    },
                    new {
                        Name = "Weekend Pass",
                        Description = "Saturday and Sunday only", 
                        Price = 200.00m,
                        IncludedSessions = new[] { "S2", "S3" }
                    },
                    new {
                        Name = "Friday Only",
                        Description = "Introduction day only",
                        Price = 120.00m,
                        IncludedSessions = new[] { "S1" }
                    }
                },
                OrganizerId = organizerId
            };

            // Act - Create event via API
            var json = JsonSerializer.Serialize(createEventRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/events", content);

            // Assert - Event created successfully
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var eventResponse = JsonSerializer.Deserialize<EventCreatedResponse>(responseContent, 
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            
            eventResponse.Should().NotBeNull();
            eventResponse.EventId.Should().NotBeEmpty();
            eventResponse.SessionsCreated.Should().Be(3);
            eventResponse.TicketTypesCreated.Should().Be(3);

            // Verify database structure
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<WitchCityRopeDbContext>();
            
            var createdEvent = await dbContext.Events
                .Include(e => e.Sessions)
                .Include(e => e.TicketTypes)
                .ThenInclude(tt => tt.IncludedSessions)
                .FirstOrDefaultAsync(e => e.Id == eventResponse.EventId);

            createdEvent.Should().NotBeNull();
            createdEvent.Sessions.Should().HaveCount(3);
            createdEvent.TicketTypes.Should().HaveCount(3);

            // Verify session data
            var s1 = createdEvent.Sessions.First(s => s.SessionName == "S1");
            s1.Capacity.Should().Be(20);
            
            var s3 = createdEvent.Sessions.First(s => s.SessionName == "S3");
            s3.Capacity.Should().Be(15);

            // Verify ticket type session mappings
            var fullSeries = createdEvent.TicketTypes.First(tt => tt.Name == "Full Series Pass");
            fullSeries.IncludedSessions.Should().HaveCount(3);
            fullSeries.IncludedSessions.Select(is => is.SessionId).Should().BeEquivalentTo(
                createdEvent.Sessions.Select(s => s.Id));
        }

        #endregion

        #region Session Availability Calculations

        [Fact]
        public async Task GetTicketAvailability_MultipleTicketTypes_CalculatesCorrectAvailability()
        {
            // Arrange - Create event with session matrix
            var eventId = await CreateTestEventWithSessions();
            
            // Act - Get availability for each ticket type
            var availabilityResponse = await _client.GetAsync($"/api/events/{eventId}/ticket-availability");
            availabilityResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var availabilityJson = await availabilityResponse.Content.ReadAsStringAsync();
            var availability = JsonSerializer.Deserialize<TicketAvailabilityResponse>(availabilityJson,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            // Assert - Availability calculations
            availability.Should().NotBeNull();
            availability.TicketTypes.Should().HaveCount(3);

            // Full Series Pass limited by S3 (15 capacity)
            var fullSeriesAvailability = availability.TicketTypes.First(tt => tt.Name == "Full Series Pass");
            fullSeriesAvailability.Available.Should().Be(15); // Limited by S3

            // Weekend Pass limited by S3 (15 capacity)  
            var weekendAvailability = availability.TicketTypes.First(tt => tt.Name == "Weekend Pass");
            weekendAvailability.Available.Should().Be(15); // Limited by S3

            // Friday Only limited by S1 (20 capacity)
            var fridayAvailability = availability.TicketTypes.First(tt => tt.Name == "Friday Only");
            fridayAvailability.Available.Should().Be(20); // S1 capacity
        }

        [Fact]
        public async Task RegisterForEvent_MultipleRegistrations_UpdatesAvailabilityCorrectly()
        {
            // Arrange
            var eventId = await CreateTestEventWithSessions();
            var attendee1Id = GetUserIdFromResponse(await CreateTestUser("attendee1@test.com"));
            var attendee2Id = GetUserIdFromResponse(await CreateTestUser("attendee2@test.com"));
            var attendee3Id = GetUserIdFromResponse(await CreateTestUser("attendee3@test.com"));

            // Get ticket type IDs
            var ticketTypesResponse = await _client.GetAsync($"/api/events/{eventId}/ticket-types");
            var ticketTypes = await ParseJsonResponse<TicketTypesResponse>(ticketTypesResponse);
            
            var fullSeriesId = ticketTypes.TicketTypes.First(tt => tt.Name == "Full Series Pass").Id;
            var weekendPassId = ticketTypes.TicketTypes.First(tt => tt.Name == "Weekend Pass").Id; 
            var fridayOnlyId = ticketTypes.TicketTypes.First(tt => tt.Name == "Friday Only").Id;

            // Act - Make multiple registrations
            
            // 5 Full Series registrations (affects all sessions)
            await RegisterUserForEvent(eventId, attendee1Id, fullSeriesId, 5);
            
            // 3 Weekend Pass registrations (affects S2, S3)
            await RegisterUserForEvent(eventId, attendee2Id, weekendPassId, 3);
            
            // 2 Friday Only registrations (affects S1)
            await RegisterUserForEvent(eventId, attendee3Id, fridayOnlyId, 2);

            // Get updated availability
            var availabilityResponse = await _client.GetAsync($"/api/events/{eventId}/ticket-availability");
            var availability = await ParseJsonResponse<TicketAvailabilityResponse>(availabilityResponse);

            // Assert - Capacity calculations after registrations
            
            // S1 (20): Used by 5 full series + 2 friday only = 7, Available = 13
            var fridayAvailability = availability.TicketTypes.First(tt => tt.Name == "Friday Only");
            fridayAvailability.Available.Should().Be(13);

            // S2 (18): Used by 5 full series + 3 weekend = 8, Available = 10
            // S3 (15): Used by 5 full series + 3 weekend = 8, Available = 7
            // Weekend Pass limited by S3 = 7
            var weekendAvailability = availability.TicketTypes.First(tt => tt.Name == "Weekend Pass");
            weekendAvailability.Available.Should().Be(7);

            // Full Series Pass limited by S3 = 7
            var fullSeriesAvailability = availability.TicketTypes.First(tt => tt.Name == "Full Series Pass");
            fullSeriesAvailability.Available.Should().Be(7);
        }

        #endregion

        #region RSVP vs Paid Registration Workflow

        [Fact]
        public async Task SocialEvent_RSVPMode_AllowsFreeRegistrationWithoutPayment()
        {
            // Arrange - Create social event with RSVP ticket
            var organizerId = GetUserIdFromResponse(await CreateTestUser("social-organizer@test.com", UserRole.Organizer));
            
            var socialEventRequest = new
            {
                Title = "Monthly Social Gathering",
                Description = "Free community social event",
                EventType = EventType.Social,
                Location = "Community Center",
                IsPublished = true,
                Sessions = new[]
                {
                    new {
                        SessionName = "S1",
                        SessionDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"),
                        StartTime = "19:00",
                        EndTime = "22:00", 
                        Capacity = 50
                    }
                },
                TicketTypes = new[]
                {
                    new {
                        Name = "Free RSVP",
                        Description = "Free attendance",
                        Price = 0.00m,
                        IsRSVPMode = true, // Key: RSVP mode
                        IncludedSessions = new[] { "S1" }
                    }
                },
                OrganizerId = organizerId
            };

            var eventResponse = await CreateEventViaAPI(socialEventRequest);
            var attendeeId = GetUserIdFromResponse(await CreateTestUser("attendee@test.com"));

            // Act - Register for RSVP event
            var registrationRequest = new
            {
                EventId = eventResponse.EventId,
                UserId = attendeeId,
                TicketTypeId = eventResponse.TicketTypeIds.First(),
                Quantity = 1
            };

            var registrationResponse = await _client.PostAsJsonAsync("/api/events/register", registrationRequest);

            // Assert - Registration should succeed immediately without payment
            registrationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var registration = await ParseJsonResponse<RegistrationResponse>(registrationResponse);
            registration.Status.Should().Be("Confirmed"); // Immediate confirmation
            registration.PaymentRequired.Should().BeFalse();
            registration.PaymentStatus.Should().Be("NotRequired");
        }

        [Fact] 
        public async Task ClassEvent_WithZeroPrice_StillRequiresPaymentProcessing()
        {
            // Arrange - Create free class (but not RSVP mode)
            var organizerId = GetUserIdFromResponse(await CreateTestUser("class-organizer@test.com", UserRole.Organizer));
            
            var freeClassRequest = new
            {
                Title = "Free Beginner Workshop",
                Description = "Community outreach class",
                EventType = EventType.Class, // Class type
                Location = "Community Center",
                IsPublished = true,
                Sessions = new[]
                {
                    new {
                        SessionName = "S1",
                        SessionDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"),
                        StartTime = "10:00",
                        EndTime = "12:00",
                        Capacity = 20
                    }
                },
                TicketTypes = new[]
                {
                    new {
                        Name = "Free Class Access",
                        Description = "No charge class",
                        Price = 0.00m,
                        IsRSVPMode = false, // Payment processing required
                        IncludedSessions = new[] { "S1" }
                    }
                },
                OrganizerId = organizerId
            };

            var eventResponse = await CreateEventViaAPI(freeClassRequest);
            var attendeeId = GetUserIdFromResponse(await CreateTestUser("class-attendee@test.com"));

            // Act - Register for free class
            var registrationRequest = new
            {
                EventId = eventResponse.EventId, 
                UserId = attendeeId,
                TicketTypeId = eventResponse.TicketTypeIds.First(),
                Quantity = 1
            };

            var registrationResponse = await _client.PostAsJsonAsync("/api/events/register", registrationRequest);

            // Assert - Should require payment processing (even for $0)
            registrationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var registration = await ParseJsonResponse<RegistrationResponse>(registrationResponse);
            registration.PaymentRequired.Should().BeTrue(); // Always true for classes
            registration.PaymentStatus.Should().Be("Pending");
            registration.Status.Should().Be("Pending"); // Awaiting payment confirmation
        }

        #endregion

        #region Complex Multi-Session Scenarios

        [Fact]
        public async Task ComplexWorkshopSeries_AllTicketCombinations_HandlesCorrectly()
        {
            // Arrange - Create complex 3-day workshop with all possible ticket combinations
            var eventId = await CreateComplexWorkshopEvent();
            
            // Simulate realistic registration pattern
            var registrations = new[]
            {
                ("full-series-1@test.com", "Full Series (All 3 Days)", 8),
                ("days12-1@test.com", "Days 1-2 Only", 3),  
                ("days23-1@test.com", "Days 2-3 Only", 4),
                ("day1-1@test.com", "Day 1 Only", 2),
                ("day2-1@test.com", "Day 2 Only", 1),
                ("day3-1@test.com", "Day 3 Only", 1)
            };

            // Act - Process all registrations
            foreach (var (email, ticketName, quantity) in registrations)
            {
                var userId = GetUserIdFromResponse(await CreateTestUser(email));
                var ticketTypeId = await GetTicketTypeId(eventId, ticketName);
                await RegisterUserForEvent(eventId, userId, ticketTypeId, quantity);
            }

            // Get final availability 
            var availability = await GetTicketAvailability(eventId);

            // Assert - Complex capacity calculations
            
            // Day 1 (S1, capacity 20): 8 full + 3 days12 + 2 day1 = 13 used, 7 available
            var day1Availability = availability.TicketTypes.First(tt => tt.Name == "Day 1 Only");
            day1Availability.Available.Should().Be(7);

            // Day 2 (S2, capacity 18): 8 full + 3 days12 + 4 days23 + 1 day2 = 16 used, 2 available
            var day2Availability = availability.TicketTypes.First(tt => tt.Name == "Day 2 Only");
            day2Availability.Available.Should().Be(2);

            // Day 3 (S3, capacity 15): 8 full + 4 days23 + 1 day3 = 13 used, 2 available
            var day3Availability = availability.TicketTypes.First(tt => tt.Name == "Day 3 Only");
            day3Availability.Available.Should().Be(2);

            // Full Series: Limited by most constrained session (Day 2 with 2 available)
            var fullSeriesAvailability = availability.TicketTypes.First(tt => tt.Name == "Full Series (All 3 Days)");
            fullSeriesAvailability.Available.Should().Be(2);
        }

        [Fact]
        public async Task OverbookedSession_PreventsNewRegistrations()
        {
            // Arrange - Create event and fill to capacity
            var eventId = await CreateTestEventWithSessions();
            var fridayOnlyTicketId = await GetTicketTypeId(eventId, "Friday Only");

            // Fill S1 to capacity (20)
            for (int i = 0; i < 20; i++)
            {
                var userId = GetUserIdFromResponse(await CreateTestUser($"user{i}@test.com"));
                await RegisterUserForEvent(eventId, userId, fridayOnlyTicketId, 1);
            }

            // Act - Try to register one more
            var overflowUserId = GetUserIdFromResponse(await CreateTestUser("overflow@test.com"));
            var overflowResponse = await _client.PostAsJsonAsync("/api/events/register", new
            {
                EventId = eventId,
                UserId = overflowUserId,
                TicketTypeId = fridayOnlyTicketId,
                Quantity = 1
            });

            // Assert - Should be rejected due to capacity
            overflowResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var error = await overflowResponse.Content.ReadAsStringAsync();
            error.Should().Contain("insufficient capacity");
        }

        #endregion

        #region Helper Methods

        private async Task<CreateUserResponse> CreateTestUser(string email, UserRole role = UserRole.Member)
        {
            var userRequest = new
            {
                Email = email,
                SceneName = $"TestUser_{Guid.NewGuid():N}",
                Role = role.ToString()
            };

            var response = await _client.PostAsJsonAsync("/api/users", userRequest);
            response.EnsureSuccessStatusCode();
            return await ParseJsonResponse<CreateUserResponse>(response);
        }

        private async Task<EventCreatedResponse> CreateEventViaAPI(object eventRequest)
        {
            var response = await _client.PostAsJsonAsync("/api/events", eventRequest);
            response.EnsureSuccessStatusCode();
            return await ParseJsonResponse<EventCreatedResponse>(response);
        }

        private async Task<Guid> CreateTestEventWithSessions()
        {
            var organizerId = GetUserIdFromResponse(await CreateTestUser("test-organizer@test.com", UserRole.Organizer));
            
            var eventRequest = new
            {
                Title = "Test Workshop Series", 
                Description = "Test event for capacity calculations",
                EventType = EventType.Class,
                Location = "Test Venue",
                IsPublished = true,
                Sessions = new[]
                {
                    new { SessionName = "S1", SessionDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"), StartTime = "09:00", EndTime = "12:00", Capacity = 20 },
                    new { SessionName = "S2", SessionDate = DateTime.UtcNow.AddDays(8).ToString("yyyy-MM-dd"), StartTime = "09:00", EndTime = "12:00", Capacity = 18 },
                    new { SessionName = "S3", SessionDate = DateTime.UtcNow.AddDays(9).ToString("yyyy-MM-dd"), StartTime = "09:00", EndTime = "12:00", Capacity = 15 }
                },
                TicketTypes = new[]
                {
                    new { Name = "Full Series Pass", Description = "All 3 days", Price = 300.00m, IncludedSessions = new[] { "S1", "S2", "S3" } },
                    new { Name = "Weekend Pass", Description = "Days 2-3", Price = 200.00m, IncludedSessions = new[] { "S2", "S3" } },
                    new { Name = "Friday Only", Description = "Day 1", Price = 120.00m, IncludedSessions = new[] { "S1" } }
                },
                OrganizerId = organizerId
            };

            var response = await CreateEventViaAPI(eventRequest);
            return response.EventId;
        }

        private async Task<Guid> CreateComplexWorkshopEvent()
        {
            var organizerId = GetUserIdFromResponse(await CreateTestUser("complex-organizer@test.com", UserRole.Organizer));
            
            var eventRequest = new
            {
                Title = "Advanced Rope Mastery Series",
                Description = "Complex 3-day workshop",
                EventType = EventType.Class,
                Location = "Various Venues",
                IsPublished = true,
                Sessions = new[]
                {
                    new { SessionName = "S1", SessionDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"), StartTime = "09:00", EndTime = "17:00", Capacity = 20 },
                    new { SessionName = "S2", SessionDate = DateTime.UtcNow.AddDays(8).ToString("yyyy-MM-dd"), StartTime = "09:00", EndTime = "17:00", Capacity = 18 },
                    new { SessionName = "S3", SessionDate = DateTime.UtcNow.AddDays(9).ToString("yyyy-MM-dd"), StartTime = "09:00", EndTime = "17:00", Capacity = 15 }
                },
                TicketTypes = new[]
                {
                    new { Name = "Full Series (All 3 Days)", Description = "Complete workshop", Price = 450.00m, IncludedSessions = new[] { "S1", "S2", "S3" } },
                    new { Name = "Days 1-2 Only", Description = "Foundation days", Price = 280.00m, IncludedSessions = new[] { "S1", "S2" } },
                    new { Name = "Days 2-3 Only", Description = "Advanced days", Price = 320.00m, IncludedSessions = new[] { "S2", "S3" } },
                    new { Name = "Day 1 Only", Description = "Introduction", Price = 150.00m, IncludedSessions = new[] { "S1" } },
                    new { Name = "Day 2 Only", Description = "Intermediate", Price = 160.00m, IncludedSessions = new[] { "S2" } },
                    new { Name = "Day 3 Only", Description = "Advanced", Price = 170.00m, IncludedSessions = new[] { "S3" } }
                },
                OrganizerId = organizerId
            };

            var response = await CreateEventViaAPI(eventRequest);
            return response.EventId;
        }

        private async Task RegisterUserForEvent(Guid eventId, Guid userId, Guid ticketTypeId, int quantity)
        {
            var request = new
            {
                EventId = eventId,
                UserId = userId,
                TicketTypeId = ticketTypeId,
                Quantity = quantity
            };

            var response = await _client.PostAsJsonAsync("/api/events/register", request);
            response.EnsureSuccessStatusCode();
        }

        private async Task<Guid> GetTicketTypeId(Guid eventId, string ticketTypeName)
        {
            var response = await _client.GetAsync($"/api/events/{eventId}/ticket-types");
            var ticketTypes = await ParseJsonResponse<TicketTypesResponse>(response);
            return ticketTypes.TicketTypes.First(tt => tt.Name == ticketTypeName).Id;
        }

        private async Task<TicketAvailabilityResponse> GetTicketAvailability(Guid eventId)
        {
            var response = await _client.GetAsync($"/api/events/{eventId}/ticket-availability");
            return await ParseJsonResponse<TicketAvailabilityResponse>(response);
        }

        private Guid GetUserIdFromResponse(CreateUserResponse response) => response.UserId;

        private async Task<T> ParseJsonResponse<T>(HttpResponse response)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })!;
        }

        #endregion

        #region Response DTOs (TDD - These define the API contracts we want)

        public record CreateUserResponse(Guid UserId, string Email, string SceneName);
        
        public record EventCreatedResponse(Guid EventId, string Title, int SessionsCreated, int TicketTypesCreated, Guid[] TicketTypeIds);
        
        public record TicketTypesResponse(TicketTypeDto[] TicketTypes);
        
        public record TicketTypeDto(Guid Id, string Name, string Description, decimal Price, string[] IncludedSessions);
        
        public record TicketAvailabilityResponse(TicketAvailabilityDto[] TicketTypes);
        
        public record TicketAvailabilityDto(Guid Id, string Name, int Available, int Capacity, string[] LimitingSessions);
        
        public record RegistrationResponse(Guid RegistrationId, string Status, bool PaymentRequired, string PaymentStatus);

        #endregion
    }
}