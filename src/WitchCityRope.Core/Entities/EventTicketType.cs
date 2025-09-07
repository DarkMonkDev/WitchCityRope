using System;
using System.Collections.Generic;
using System.Linq;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Represents a ticket type for an event with pricing and session inclusions
    /// Business Rules:
    /// - Min price must be >= 0
    /// - Max price must be >= min price
    /// - Quantity available must be > 0 or null (unlimited)
    /// - Cannot delete ticket types with existing registrations
    /// - Must include at least one session per ticket type
    /// </summary>
    public class EventTicketType
    {
        private readonly List<EventTicketTypeSession> _sessionInclusions = new();
        private readonly List<Registration> _registrations = new();

        // Private constructor for EF Core
        private EventTicketType()
        {
            Name = null!;
        }

        /// <summary>
        /// Creates a new event ticket type
        /// </summary>
        /// <param name="event">The event this ticket type belongs to</param>
        /// <param name="name">Display name of the ticket type</param>
        /// <param name="ticketType">Type of ticket (Single/Couples)</param>
        /// <param name="minPrice">Minimum price for sliding scale</param>
        /// <param name="maxPrice">Maximum price for sliding scale</param>
        /// <param name="quantityAvailable">Available quantity (null for unlimited)</param>
        /// <param name="salesEndDateTime">End of sales period (null for no limit)</param>
        public EventTicketType(Event @event, string name, TicketTypeEnum ticketType,
            decimal minPrice, decimal maxPrice, int? quantityAvailable = null, 
            DateTime? salesEndDateTime = null)
        {
            Id = Guid.NewGuid(); // CRITICAL: Must set this first!
            Event = @event ?? throw new ArgumentNullException(nameof(@event));
            EventId = @event.Id;
            Name = ValidateName(name);
            TicketType = ticketType;
            
            ValidatePriceRange(minPrice, maxPrice);
            MinPrice = minPrice;
            MaxPrice = maxPrice;
            
            QuantityAvailable = ValidateQuantityAvailable(quantityAvailable);
            
            // Ensure DateTime is UTC if provided
            SalesEndDateTime = salesEndDateTime.HasValue 
                ? DateTime.SpecifyKind(salesEndDateTime.Value, DateTimeKind.Utc)
                : null;
            
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }
        
        public Guid EventId { get; private set; }
        
        /// <summary>
        /// Reference to the parent event
        /// </summary>
        public Event Event { get; private set; } = null!;
        
        /// <summary>
        /// Display name of the ticket type
        /// </summary>
        public string Name { get; private set; }
        
        /// <summary>
        /// Type of ticket (Single/Couples)
        /// </summary>
        public TicketTypeEnum TicketType { get; private set; }
        
        /// <summary>
        /// Minimum price for sliding scale pricing
        /// </summary>
        public decimal MinPrice { get; private set; }
        
        /// <summary>
        /// Maximum price for sliding scale pricing
        /// </summary>
        public decimal MaxPrice { get; private set; }
        
        /// <summary>
        /// Available quantity (null for unlimited)
        /// </summary>
        public int? QuantityAvailable { get; private set; }
        
        /// <summary>
        /// End of sales period (null for no limit, stored as UTC)
        /// </summary>
        public DateTime? SalesEndDateTime { get; private set; }
        
        /// <summary>
        /// Whether this ticket type is active
        /// </summary>
        public bool IsActive { get; private set; }
        
        public DateTime CreatedAt { get; private set; }
        
        public DateTime UpdatedAt { get; private set; }
        
        /// <summary>
        /// Sessions included in this ticket type
        /// </summary>
        public IReadOnlyCollection<EventTicketTypeSession> SessionInclusions => _sessionInclusions.AsReadOnly();
        
        /// <summary>
        /// Registrations using this ticket type
        /// </summary>
        public IReadOnlyCollection<Registration> Registrations => _registrations.AsReadOnly();

        /// <summary>
        /// Updates ticket type details
        /// </summary>
        public void UpdateDetails(string name, decimal minPrice, decimal maxPrice, 
            int? quantityAvailable = null, DateTime? salesEndDateTime = null)
        {
            if (GetConfirmedRegistrationCount() > 0)
                throw new InvalidOperationException("Cannot update ticket type with confirmed registrations");
            
            Name = ValidateName(name);
            
            ValidatePriceRange(minPrice, maxPrice);
            MinPrice = minPrice;
            MaxPrice = maxPrice;
            
            QuantityAvailable = ValidateQuantityAvailable(quantityAvailable);
            
            // Ensure DateTime is UTC if provided
            SalesEndDateTime = salesEndDateTime.HasValue 
                ? DateTime.SpecifyKind(salesEndDateTime.Value, DateTimeKind.Utc)
                : null;
            
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Adds a session inclusion to this ticket type
        /// </summary>
        public void AddSessionInclusion(EventSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            
            if (session.EventId != EventId)
                throw new ArgumentException("Session must belong to the same event as the ticket type");
            
            if (_sessionInclusions.Any(si => si.EventSessionId == session.Id))
                throw new InvalidOperationException("Session is already included in this ticket type");
            
            var inclusion = new EventTicketTypeSession(this, session);
            _sessionInclusions.Add(inclusion);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Removes a session inclusion from this ticket type
        /// </summary>
        public void RemoveSessionInclusion(EventSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            
            if (_sessionInclusions.Count == 1)
                throw new InvalidOperationException("Ticket type must include at least one session");
            
            _sessionInclusions.RemoveAll(si => si.EventSessionId == session.Id);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Deactivates the ticket type (soft delete)
        /// </summary>
        public void Deactivate()
        {
            if (GetConfirmedRegistrationCount() > 0)
                throw new InvalidOperationException("Cannot deactivate ticket type with confirmed registrations");
            
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Reactivates the ticket type
        /// </summary>
        public void Activate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the number of confirmed registrations for this ticket type
        /// </summary>
        public int GetConfirmedRegistrationCount()
        {
            return _registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
        }

        /// <summary>
        /// Gets the number of available tickets (null for unlimited)
        /// </summary>
        public int? GetAvailableQuantity()
        {
            if (!QuantityAvailable.HasValue)
                return null; // Unlimited
            
            return QuantityAvailable.Value - GetConfirmedRegistrationCount();
        }

        /// <summary>
        /// Checks if sales are still open for this ticket type
        /// </summary>
        public bool AreSalesOpen()
        {
            if (!SalesEndDateTime.HasValue)
                return true; // No end date set
            
            return DateTime.UtcNow < SalesEndDateTime.Value;
        }

        /// <summary>
        /// Checks if tickets are available for purchase
        /// </summary>
        public bool HasAvailableTickets()
        {
            if (!IsActive || !AreSalesOpen())
                return false;
            
            var available = GetAvailableQuantity();
            return !available.HasValue || available.Value > 0;
        }

        private static string ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Ticket type name cannot be empty", nameof(name));
            
            if (name.Length > 200)
                throw new ArgumentException("Ticket type name cannot exceed 200 characters", nameof(name));
            
            return name.Trim();
        }

        private static void ValidatePriceRange(decimal minPrice, decimal maxPrice)
        {
            if (minPrice < 0)
                throw new ArgumentException("Minimum price cannot be negative", nameof(minPrice));
            
            if (maxPrice < minPrice)
                throw new ArgumentException("Maximum price cannot be less than minimum price", nameof(maxPrice));
        }

        private static int? ValidateQuantityAvailable(int? quantityAvailable)
        {
            if (quantityAvailable.HasValue && quantityAvailable.Value <= 0)
                throw new ArgumentException("Quantity available must be greater than 0 if specified", nameof(quantityAvailable));
            
            return quantityAvailable;
        }
    }
}