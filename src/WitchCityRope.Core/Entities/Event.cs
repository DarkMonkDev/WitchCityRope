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
        private readonly List<IUser> _organizers = new();
        private readonly List<Money> _pricingTiers = new();
        private readonly List<Ticket> _tickets = new();
        private readonly List<Rsvp> _rsvps = new();
        private readonly List<EventEmailTemplate> _emailTemplates = new();
        private readonly List<VolunteerTask> _volunteerTasks = new();

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
            IUser primaryOrganizer,
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
        public int Capacity { get; set; }
        
        public EventType EventType { get; private set; }
        
        public string Location { get; private set; }
        
        public bool IsPublished { get; private set; }
        
        public DateTime CreatedAt { get; private set; }
        
        public DateTime UpdatedAt { get; private set; }
        
        public IReadOnlyCollection<Registration> Registrations => _registrations.AsReadOnly();
        
        public IReadOnlyCollection<IUser> Organizers => _organizers.AsReadOnly();
        
        /// <summary>
        /// Sliding scale pricing tiers for the event
        /// </summary>
        public IReadOnlyCollection<Money> PricingTiers => _pricingTiers.AsReadOnly();
        
        /// <summary>
        /// Tickets for paid workshop/class events
        /// </summary>
        public IReadOnlyCollection<Ticket> Tickets => _tickets.AsReadOnly();
        
        /// <summary>
        /// RSVPs for free social events
        /// </summary>
        public IReadOnlyCollection<Rsvp> Rsvps => _rsvps.AsReadOnly();
        
        /// <summary>
        /// Email templates configured for this event
        /// </summary>
        public IReadOnlyCollection<EventEmailTemplate> EmailTemplates => _emailTemplates.AsReadOnly();
        
        /// <summary>
        /// Volunteer tasks defined for this event
        /// </summary>
        public IReadOnlyCollection<VolunteerTask> VolunteerTasks => _volunteerTasks.AsReadOnly();

        /// <summary>
        /// Gets the number of confirmed registrations
        /// </summary>
        public int GetConfirmedRegistrationCount()
        {
            return _registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
        }

        /// <summary>
        /// Gets the number of available spots
        /// </summary>
        public int GetAvailableSpots()
        {
            return Capacity - GetConfirmedRegistrationCount();
        }

        /// <summary>
        /// Checks if the event has available capacity
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
        public void AddOrganizer(IUser organizer)
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
        public void RemoveOrganizer(IUser organizer)
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