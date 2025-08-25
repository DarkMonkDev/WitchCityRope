using System;
using System.Collections.Generic;
using System.Linq;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.Tests.Common.Builders
{
    /// <summary>
    /// Test builder for Event Session Matrix entities
    /// This represents the domain model we want to build during TDD Green phase
    /// </summary>
    public class EventWithSessionsBuilder
    {
        private string _title = "Test Event";
        private string _description = "Test Description"; 
        private EventType _eventType = EventType.Class;
        private string _location = "Test Location";
        private User _organizer = null!;
        private readonly List<EventSessionBuilder> _sessions = new();
        private readonly List<TicketTypeBuilder> _ticketTypes = new();

        public EventWithSessionsBuilder WithTitle(string title)
        {
            _title = title;
            return this;
        }

        public EventWithSessionsBuilder WithDescription(string description)
        {
            _description = description;
            return this;
        }

        public EventWithSessionsBuilder WithEventType(EventType eventType)
        {
            _eventType = eventType;
            return this;
        }

        public EventWithSessionsBuilder WithLocation(string location)
        {
            _location = location;
            return this;
        }

        public EventWithSessionsBuilder WithOrganizer(User organizer)
        {
            _organizer = organizer ?? throw new ArgumentNullException(nameof(organizer));
            return this;
        }

        public EventWithSessionsBuilder WithSession(string sessionName, string displayName, int capacity, DateTime date, TimeSpan? startTime = null, TimeSpan? endTime = null)
        {
            var session = new EventSessionBuilder()
                .WithSessionName(sessionName)
                .WithDisplayName(displayName)
                .WithCapacity(capacity)
                .WithSessionDate(date)
                .WithStartTime(startTime ?? TimeSpan.FromHours(9))
                .WithEndTime(endTime ?? TimeSpan.FromHours(12));

            _sessions.Add(session);
            return this;
        }

        public EventWithSessionsBuilder WithTicketType(TicketTypeBuilder ticketType)
        {
            _ticketTypes.Add(ticketType);
            return this;
        }

        public EventWithSessions Build()
        {
            if (_organizer == null)
                throw new InvalidOperationException("Event must have an organizer");

            if (!_sessions.Any())
                throw new InvalidOperationException("Event must have at least one session");

            // Validate session overlaps on same date
            ValidateSessionTimes();

            var builtSessions = _sessions.Select(s => s.Build()).ToList();
            var builtTicketTypes = _ticketTypes.Select(t => t.Build()).ToList();

            // Validate ticket type session references
            ValidateTicketTypeSessionReferences(builtSessions, builtTicketTypes);

            return new EventWithSessions
            {
                Id = Guid.NewGuid(),
                Title = _title,
                Description = _description,
                EventType = _eventType,
                Location = _location,
                Organizer = _organizer,
                Sessions = builtSessions,
                TicketTypes = builtTicketTypes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private void ValidateSessionTimes()
        {
            var sessionsByDate = _sessions.GroupBy(s => s.SessionDate);
            
            foreach (var dateGroup in sessionsByDate)
            {
                var sessionsOnDate = dateGroup.OrderBy(s => s.StartTime).ToList();
                
                for (int i = 0; i < sessionsOnDate.Count - 1; i++)
                {
                    var current = sessionsOnDate[i];
                    var next = sessionsOnDate[i + 1];
                    
                    if (current.EndTime > next.StartTime)
                    {
                        throw new DomainException($"Sessions '{current.SessionName}' and '{next.SessionName}' cannot overlap on the same date");
                    }
                }
            }
        }

        private void ValidateTicketTypeSessionReferences(List<EventSession> sessions, List<TicketType> ticketTypes)
        {
            var sessionNames = sessions.Select(s => s.SessionName).ToHashSet();
            
            foreach (var ticketType in ticketTypes)
            {
                foreach (var sessionReference in ticketType.IncludedSessions)
                {
                    if (!sessionNames.Contains(sessionReference))
                    {
                        throw new DomainException($"Ticket type '{ticketType.Name}' references session '{sessionReference}' which does not exist");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Domain entity representing Event with Sessions (TDD target model)
    /// This is the structure the tests expect to exist after implementation
    /// </summary>
    public class EventWithSessions
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public EventType EventType { get; set; }
        public string Location { get; set; } = null!;
        public User Organizer { get; set; } = null!;
        public List<EventSession> Sessions { get; set; } = new();
        public List<TicketType> TicketTypes { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public int CalculateTicketAvailability(TicketType ticketType)
        {
            if (ticketType.IncludedSessions == null || !ticketType.IncludedSessions.Any())
                return 0;

            // Find the sessions this ticket type includes
            var includedSessions = Sessions.Where(s => ticketType.IncludedSessions.Contains(s.SessionName)).ToList();
            
            if (!includedSessions.Any())
                return 0;

            // Calculate available capacity for each session
            var availabilityPerSession = includedSessions.Select(session =>
            {
                var usedCapacity = CalculateUsedCapacityForSession(session.SessionName);
                return session.Capacity - usedCapacity;
            });

            // Return the minimum (most limiting session)
            return availabilityPerSession.Min();
        }

        public RegistrationResult RegisterAttendee(User attendee, TicketType ticketType, int quantity)
        {
            // Validate availability
            var availability = CalculateTicketAvailability(ticketType);
            if (availability < quantity)
            {
                throw new DomainException("Insufficient capacity for registration");
            }

            // Determine if payment is required based on event type and ticket configuration
            var requiresPayment = DeterminePaymentRequirement(ticketType);
            var paymentStatus = requiresPayment ? PaymentStatus.Pending : PaymentStatus.NotRequired;
            var registrationStatus = requiresPayment ? RegistrationStatus.Pending : RegistrationStatus.Confirmed;

            // Create registration record (simplified for TDD)
            var registration = new RegistrationResult
            {
                RegistrationId = Guid.NewGuid(),
                Status = registrationStatus,
                RequiresPayment = requiresPayment,
                PaymentStatus = paymentStatus
            };

            // Track capacity consumption for each session (simplified for TDD)
            foreach (var sessionName in ticketType.IncludedSessions)
            {
                var session = Sessions.First(s => s.SessionName == sessionName);
                session.RegisteredAttendees += quantity;
            }

            return registration;
        }

        private bool DeterminePaymentRequirement(TicketType ticketType)
        {
            // Class events always require payment processing (even for $0)
            if (EventType == EventType.Class)
                return true;

            // Social events with RSVP mode don't require payment
            if (EventType == EventType.Social && ticketType.IsRSVPMode)
                return false;

            // All other cases require payment
            return true;
        }

        private int CalculateUsedCapacityForSession(string sessionName)
        {
            var session = Sessions.FirstOrDefault(s => s.SessionName == sessionName);
            return session?.RegisteredAttendees ?? 0;
        }
    }
}