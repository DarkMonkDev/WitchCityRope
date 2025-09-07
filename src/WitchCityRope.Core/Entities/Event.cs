using System;
using System.Collections.Generic;
using System.Linq;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Represents an event in the WitchCityRope system
    /// Business Rules:
    /// - Events have capacity limits
    /// - Events support sliding scale pricing
    /// - Events must have at least one organizer
    /// - Events can have multiple sessions with different capacities
    /// - Events can have multiple ticket types with different pricing
    /// </summary>
    public class Event
    {
        private readonly List<Registration> _registrations = new();
        private readonly List<RSVP> _rsvps = new();
        private readonly List<User> _organizers = new();
        private readonly List<Money> _pricingTiers = new();
        private readonly List<EventSession> _sessions = new();
        private readonly List<EventTicketType> _ticketTypes = new();

        // Private constructor for EF Core
        private Event() 
        { 
            Title = null!;
            Description = null!;
            Location = null!;
        }

        public Event(
            string title,
            string description,
            DateTime startDate,
            DateTime endDate,
            int capacity,
            EventType eventType,
            string location,
            User primaryOrganizer,
            IEnumerable<Money> pricingTiers)
        {
            ValidateCapacity(capacity);
            ValidatePricingTiers(pricingTiers);

            Id = Guid.NewGuid();
            
            // Validate dates after Id is set
            ValidateDates(startDate, endDate);
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            StartDate = startDate;
            EndDate = endDate;
            Capacity = capacity;
            EventType = eventType;
            Location = location ?? throw new ArgumentNullException(nameof(location));
            IsPublished = false;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            _organizers.Add(primaryOrganizer ?? throw new ArgumentNullException(nameof(primaryOrganizer)));
            _pricingTiers.AddRange(pricingTiers);
        }

        public Guid Id { get; private set; }
        
        public string Title { get; private set; }
        
        public string Description { get; private set; }
        
        public DateTime StartDate { get; private set; }
        
        public DateTime EndDate { get; private set; }
        
        /// <summary>
        /// Maximum number of attendees allowed
        /// </summary>
        public int Capacity { get; private set; }
        
        public EventType EventType { get; private set; }
        
        public string Location { get; private set; }
        
        public bool IsPublished { get; private set; }
        
        public DateTime CreatedAt { get; private set; }
        
        public DateTime UpdatedAt { get; private set; }
        
        public IReadOnlyCollection<Registration> Registrations => _registrations.AsReadOnly();
        
        public IReadOnlyCollection<RSVP> RSVPs => _rsvps.AsReadOnly();
        
        public IReadOnlyCollection<User> Organizers => _organizers.AsReadOnly();
        
        /// <summary>
        /// Sliding scale pricing tiers for the event
        /// </summary>
        public IReadOnlyCollection<Money> PricingTiers => _pricingTiers.AsReadOnly();

        /// <summary>
        /// Sessions within this event (S1, S2, S3, etc.)
        /// </summary>
        public IReadOnlyCollection<EventSession> Sessions => _sessions.AsReadOnly();

        /// <summary>
        /// Ticket types available for this event
        /// </summary>
        public IReadOnlyCollection<EventTicketType> TicketTypes => _ticketTypes.AsReadOnly();

        /// <summary>
        /// Business rule property: Determines if this event allows free RSVPs
        /// Only Social events allow RSVP functionality
        /// </summary>
        public bool AllowsRSVP => EventType == EventType.Social;

        /// <summary>
        /// Business rule property: Determines if this event requires payment for attendance
        /// Classes and Workshops require payment, Social events allow both RSVP and payment
        /// </summary>
        public bool RequiresPayment => EventType == EventType.Workshop || EventType == EventType.Class;

        /// <summary>
        /// Gets the number of confirmed registrations
        /// </summary>
        public int GetConfirmedRegistrationCount()
        {
            return _registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
        }

        /// <summary>
        /// Gets the number of confirmed RSVPs
        /// </summary>
        public int GetConfirmedRSVPCount()
        {
            return _rsvps.Count(r => r.Status == RSVPStatus.Confirmed);
        }

        /// <summary>
        /// Gets the total number of confirmed attendees (registrations + RSVPs)
        /// This includes both paid ticket holders and free RSVPs
        /// </summary>
        public int GetCurrentAttendeeCount()
        {
            var rsvpCount = _rsvps.Count(r => r.Status == RSVPStatus.Confirmed);
            var ticketCount = _registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
            return rsvpCount + ticketCount;
        }

        /// <summary>
        /// Gets the number of available spots (considering both RSVPs and tickets)
        /// </summary>
        public int GetAvailableSpots()
        {
            return Math.Max(0, Capacity - GetCurrentAttendeeCount());
        }

        /// <summary>
        /// Checks if the event has available capacity (considering both RSVPs and tickets)
        /// </summary>
        public bool HasAvailableCapacity()
        {
            return GetAvailableSpots() > 0;
        }

        /// <summary>
        /// Publishes the event, making it visible for registration
        /// </summary>
        public void Publish()
        {
            if (IsPublished)
                throw new DomainException("Event is already published");

            if (!_pricingTiers.Any())
                throw new DomainException("Event must have at least one pricing tier");

            IsPublished = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Unpublishes the event
        /// </summary>
        public void Unpublish()
        {
            if (!IsPublished)
                throw new DomainException("Event is not published");

            if (GetConfirmedRegistrationCount() > 0)
                throw new DomainException("Cannot unpublish event with confirmed registrations");

            IsPublished = false;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates event details
        /// </summary>
        public void UpdateDetails(string title, string description, string location)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Location = location ?? throw new ArgumentNullException(nameof(location));
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates event dates
        /// </summary>
        public void UpdateDates(DateTime startDate, DateTime endDate)
        {
            if (startDate >= endDate)
                throw new DomainException("Start date must be before end date");

            if (startDate < DateTime.UtcNow)
                throw new DomainException("Start date cannot be in the past");
            
            if (DateTime.UtcNow > StartDate)
                throw new DomainException("Cannot update dates for an event that has already started");

            StartDate = startDate;
            EndDate = endDate;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates event capacity
        /// </summary>
        public void UpdateCapacity(int newCapacity)
        {
            ValidateCapacity(newCapacity);

            if (newCapacity < GetConfirmedRegistrationCount())
                throw new DomainException("New capacity cannot be less than current confirmed registrations");

            Capacity = newCapacity;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Adds an organizer to the event
        /// </summary>
        public void AddOrganizer(User organizer)
        {
            if (organizer == null)
                throw new ArgumentNullException(nameof(organizer));

            if (_organizers.Any(o => o.Id == organizer.Id))
                throw new DomainException("User is already an organizer for this event");

            _organizers.Add(organizer);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Removes an organizer from the event
        /// </summary>
        public void RemoveOrganizer(User organizer)
        {
            if (_organizers.Count == 1)
                throw new DomainException("Event must have at least one organizer");

            _organizers.RemoveAll(o => o.Id == organizer.Id);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates pricing tiers for sliding scale
        /// </summary>
        public void UpdatePricingTiers(IEnumerable<Money> pricingTiers)
        {
            ValidatePricingTiers(pricingTiers);

            if (GetConfirmedRegistrationCount() > 0)
                throw new DomainException("Cannot update pricing tiers after registrations have been confirmed");

            _pricingTiers.Clear();
            _pricingTiers.AddRange(pricingTiers);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Adds a session to the event
        /// </summary>
        public void AddSession(EventSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (_sessions.Any(s => s.SessionIdentifier == session.SessionIdentifier))
                throw new DomainException($"Session with identifier '{session.SessionIdentifier}' already exists");

            // Check for session time overlaps on the same date
            var overlappingSession = _sessions.FirstOrDefault(s => s.OverlapsWith(session));
            if (overlappingSession != null)
                throw new DomainException($"Session '{session.SessionIdentifier}' overlaps with existing session '{overlappingSession.SessionIdentifier}'");

            _sessions.Add(session);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Removes a session from the event
        /// </summary>
        public void RemoveSession(string sessionIdentifier)
        {
            var session = _sessions.FirstOrDefault(s => s.SessionIdentifier == sessionIdentifier);
            if (session == null)
                throw new DomainException($"Session '{sessionIdentifier}' not found");

            // Check if any ticket types reference this session
            var referencingTicketTypes = _ticketTypes.Where(tt => 
                tt.TicketTypeSessions.Any(tts => tts.SessionIdentifier == sessionIdentifier));
            
            if (referencingTicketTypes.Any())
            {
                var ticketTypeNames = string.Join(", ", referencingTicketTypes.Select(tt => tt.Name));
                throw new DomainException($"Cannot remove session '{sessionIdentifier}' because it is referenced by ticket types: {ticketTypeNames}");
            }

            _sessions.Remove(session);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets a session by its identifier
        /// </summary>
        public EventSession? GetSession(string sessionIdentifier)
        {
            return _sessions.FirstOrDefault(s => s.SessionIdentifier == sessionIdentifier);
        }

        /// <summary>
        /// Adds a ticket type to the event
        /// </summary>
        public void AddTicketType(EventTicketType ticketType)
        {
            if (ticketType == null)
                throw new ArgumentNullException(nameof(ticketType));

            if (_ticketTypes.Any(tt => tt.Name == ticketType.Name))
                throw new DomainException($"Ticket type with name '{ticketType.Name}' already exists");

            // Validate that all referenced sessions exist
            var sessionIdentifiers = ticketType.GetIncludedSessionIdentifiers();
            var existingSessionIds = _sessions.Select(s => s.SessionIdentifier).ToHashSet();
            
            var missingSessionIds = sessionIdentifiers.Where(id => !existingSessionIds.Contains(id)).ToList();
            if (missingSessionIds.Any())
            {
                throw new DomainException($"Ticket type references non-existent sessions: {string.Join(", ", missingSessionIds)}");
            }

            _ticketTypes.Add(ticketType);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Removes a ticket type from the event
        /// </summary>
        public void RemoveTicketType(Guid ticketTypeId)
        {
            var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Id == ticketTypeId);
            if (ticketType == null)
                throw new DomainException("Ticket type not found");

            _ticketTypes.Remove(ticketType);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets a ticket type by its ID
        /// </summary>
        public EventTicketType? GetTicketType(Guid ticketTypeId)
        {
            return _ticketTypes.FirstOrDefault(tt => tt.Id == ticketTypeId);
        }

        /// <summary>
        /// Calculates the available capacity for a specific ticket type based on its included sessions
        /// </summary>
        public int CalculateTicketTypeAvailability(EventTicketType ticketType)
        {
            if (ticketType == null)
                return 0;

            var includedSessionIds = ticketType.GetIncludedSessionIdentifiers();
            if (!includedSessionIds.Any())
                return 0;

            // Find the minimum available capacity across all included sessions
            var availableCapacities = new List<int>();
            foreach (var sessionId in includedSessionIds)
            {
                var session = GetSession(sessionId);
                if (session != null)
                {
                    availableCapacities.Add(session.GetAvailableSpots());
                }
            }

            return availableCapacities.Any() ? availableCapacities.Min() : 0;
        }

        /// <summary>
        /// Checks if the event supports session-based ticketing
        /// </summary>
        public bool HasSessions()
        {
            return _sessions.Any();
        }

        /// <summary>
        /// Checks if the event has ticket types configured
        /// </summary>
        public bool HasTicketTypes()
        {
            return _ticketTypes.Any();
        }

        /// <summary>
        /// Gets the total capacity across all sessions (for reporting purposes)
        /// </summary>
        public int GetTotalSessionCapacity()
        {
            return _sessions.Sum(s => s.Capacity);
        }

        private void ValidateDates(DateTime startDate, DateTime endDate)
        {
            if (startDate >= endDate)
                throw new DomainException("Start date must be before end date");

            // Only validate future dates for new events (when Id is empty)
            // This allows loading existing events from the database
            if (Id == Guid.Empty && startDate < DateTime.UtcNow)
                throw new DomainException("Start date cannot be in the past");
        }

        private void ValidateCapacity(int capacity)
        {
            if (capacity <= 0)
                throw new DomainException("Capacity must be greater than zero");
        }

        private void ValidatePricingTiers(IEnumerable<Money> pricingTiers)
        {
            if (pricingTiers == null || !pricingTiers.Any())
                throw new DomainException("Event must have at least one pricing tier");

            if (pricingTiers.Any(p => p.Amount < 0))
                throw new DomainException("Pricing tiers cannot have negative amounts");
        }
    }
}