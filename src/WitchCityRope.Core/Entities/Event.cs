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
        
        /// <summary>
        /// Free RSVPs for social events
        /// </summary>
        public IReadOnlyCollection<RSVP> RSVPs => _rsvps.AsReadOnly();
        
        public IReadOnlyCollection<User> Organizers => _organizers.AsReadOnly();
        
        /// <summary>
        /// Sliding scale pricing tiers for the event
        /// </summary>
        public IReadOnlyCollection<Money> PricingTiers => _pricingTiers.AsReadOnly();
        
        /// <summary>
        /// Sessions within this event
        /// </summary>
        public IReadOnlyCollection<EventSession> Sessions => _sessions.AsReadOnly();
        
        /// <summary>
        /// Ticket types available for this event
        /// </summary>
        public IReadOnlyCollection<EventTicketType> TicketTypes => _ticketTypes.AsReadOnly();

        /// <summary>
        /// Gets the number of confirmed registrations (ticket purchases)
        /// </summary>
        public int GetConfirmedRegistrationCount()
        {
            return _registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
        }

        /// <summary>
        /// Gets the number of confirmed RSVPs (free attendees)
        /// </summary>
        public int GetConfirmedRSVPCount()
        {
            return _rsvps.Count(r => r.Status == RSVPStatus.Confirmed);
        }

        /// <summary>
        /// Gets the total number of confirmed attendees (registrations + RSVPs)
        /// </summary>
        public int GetTotalConfirmedAttendees()
        {
            return GetConfirmedRegistrationCount() + GetConfirmedRSVPCount();
        }

        /// <summary>
        /// Gets the number of available spots considering both registrations and RSVPs
        /// </summary>
        public int GetAvailableSpots()
        {
            return Capacity - GetTotalConfirmedAttendees();
        }

        /// <summary>
        /// Checks if the event has available capacity for both registrations and RSVPs
        /// </summary>
        public bool HasAvailableCapacity()
        {
            return GetAvailableSpots() > 0;
        }

        /// <summary>
        /// Checks if RSVPs are allowed for this event (Social events only)
        /// </summary>
        public bool AllowsRSVP()
        {
            return EventType == EventType.Social;
        }

        /// <summary>
        /// Checks if ticket purchases are required (Classes/Workshops) or optional (Social events)
        /// </summary>
        public bool RequiresTicketPurchase()
        {
            return EventType == EventType.Workshop;
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

            if (GetTotalConfirmedAttendees() > 0)
                throw new DomainException("Cannot unpublish event with confirmed registrations or RSVPs");

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

            if (newCapacity < GetTotalConfirmedAttendees())
                throw new DomainException("New capacity cannot be less than current confirmed attendees (registrations + RSVPs)");

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

            if (session.EventId != Id)
                throw new DomainException("Session must belong to this event");

            if (_sessions.Any(s => s.SessionIdentifier == session.SessionIdentifier))
                throw new DomainException($"Session identifier '{session.SessionIdentifier}' already exists for this event");

            _sessions.Add(session);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Removes a session from the event
        /// </summary>
        public void RemoveSession(EventSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            // Check if any ticket types include this session
            if (_ticketTypes.Any(tt => tt.SessionInclusions.Any(si => si.EventSessionId == session.Id)))
                throw new DomainException("Cannot remove session that is included in ticket types");

            _sessions.RemoveAll(s => s.Id == session.Id);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Adds a ticket type to the event
        /// </summary>
        public void AddTicketType(EventTicketType ticketType)
        {
            if (ticketType == null)
                throw new ArgumentNullException(nameof(ticketType));

            if (ticketType.EventId != Id)
                throw new DomainException("Ticket type must belong to this event");

            if (_ticketTypes.Any(tt => tt.Name.Equals(ticketType.Name, StringComparison.OrdinalIgnoreCase)))
                throw new DomainException($"Ticket type with name '{ticketType.Name}' already exists for this event");

            _ticketTypes.Add(ticketType);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Removes a ticket type from the event
        /// </summary>
        public void RemoveTicketType(EventTicketType ticketType)
        {
            if (ticketType == null)
                throw new ArgumentNullException(nameof(ticketType));

            if (ticketType.GetConfirmedRegistrationCount() > 0)
                throw new DomainException("Cannot remove ticket type with confirmed registrations");

            _ticketTypes.RemoveAll(tt => tt.Id == ticketType.Id);
            UpdatedAt = DateTime.UtcNow;
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