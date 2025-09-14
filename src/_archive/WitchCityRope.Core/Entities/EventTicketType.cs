using System;
using System.Collections.Generic;
using System.Linq;
using WitchCityRope.Core.Exceptions;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Represents a ticket type for an event with pricing and session inclusion
    /// Business Rules:
    /// - Ticket types must include at least one session
    /// - Price cannot be negative
    /// - Must have valid sale dates if specified
    /// - Can have quantity limits
    /// </summary>
    public class EventTicketType
    {
        private readonly List<EventTicketTypeSession> _ticketTypeSessions = new();

        // Private constructor for EF Core
        private EventTicketType()
        {
            Name = null!;
            Description = null!;
        }

        public EventTicketType(
            Guid eventId,
            string name,
            string description,
            decimal minPrice,
            decimal maxPrice,
            int? quantityAvailable = null,
            DateTime? salesEndDate = null,
            bool isRsvpMode = false)
        {
            ValidatePricing(minPrice, maxPrice);
            ValidateQuantity(quantityAvailable);
            ValidateSalesEndDate(salesEndDate);

            Id = Guid.NewGuid();
            EventId = eventId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            MinPrice = minPrice;
            MaxPrice = maxPrice;
            QuantityAvailable = quantityAvailable;
            SalesEndDate = salesEndDate;
            IsRsvpMode = isRsvpMode;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }

        public Guid EventId { get; private set; }

        /// <summary>
        /// Display name of the ticket type (e.g., "Single Person", "Couple")
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Description of what this ticket type includes
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Minimum price for sliding scale pricing
        /// </summary>
        public decimal MinPrice { get; private set; }

        /// <summary>
        /// Maximum price for sliding scale pricing
        /// </summary>
        public decimal MaxPrice { get; private set; }

        /// <summary>
        /// Number of tickets available (null = unlimited)
        /// </summary>
        public int? QuantityAvailable { get; private set; }

        /// <summary>
        /// Date when sales end for this ticket type
        /// </summary>
        public DateTime? SalesEndDate { get; private set; }

        /// <summary>
        /// Whether this is RSVP mode (no payment required)
        /// </summary>
        public bool IsRsvpMode { get; private set; }

        /// <summary>
        /// Whether this ticket type is currently active
        /// </summary>
        public bool IsActive { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        /// <summary>
        /// Navigation property to parent event
        /// </summary>
        public Event Event { get; private set; }

        /// <summary>
        /// Junction entities linking this ticket type to sessions
        /// </summary>
        public IReadOnlyCollection<EventTicketTypeSession> TicketTypeSessions => _ticketTypeSessions.AsReadOnly();

        /// <summary>
        /// Gets the session identifiers included in this ticket type
        /// </summary>
        public IEnumerable<string> GetIncludedSessionIdentifiers()
        {
            return _ticketTypeSessions.Select(tts => tts.SessionIdentifier);
        }

        /// <summary>
        /// Checks if this ticket type is currently on sale
        /// </summary>
        public bool IsCurrentlyOnSale()
        {
            if (!IsActive)
                return false;

            if (SalesEndDate.HasValue && DateTime.UtcNow > SalesEndDate.Value)
                return false;

            return true;
        }

        /// <summary>
        /// Checks if the ticket type has quantity limits
        /// </summary>
        public bool HasQuantityLimit()
        {
            return QuantityAvailable.HasValue;
        }

        /// <summary>
        /// Checks if the requested quantity is within limits
        /// </summary>
        public bool IsWithinQuantityLimit(int requestedQuantity, int currentSold = 0)
        {
            if (!HasQuantityLimit())
                return true;

            return (currentSold + requestedQuantity) <= QuantityAvailable.Value;
        }

        /// <summary>
        /// Updates the ticket type details
        /// </summary>
        public void UpdateDetails(string name, string description, decimal minPrice, decimal maxPrice)
        {
            ValidatePricing(minPrice, maxPrice);

            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            MinPrice = minPrice;
            MaxPrice = maxPrice;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the quantity availability
        /// </summary>
        public void UpdateQuantityAvailable(int? quantityAvailable)
        {
            ValidateQuantity(quantityAvailable);
            QuantityAvailable = quantityAvailable;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the sales end date
        /// </summary>
        public void UpdateSalesEndDate(DateTime? salesEndDate)
        {
            ValidateSalesEndDate(salesEndDate);
            SalesEndDate = salesEndDate;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Activates the ticket type
        /// </summary>
        public void Activate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Deactivates the ticket type
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Adds a session to this ticket type
        /// </summary>
        public void AddSession(string sessionIdentifier)
        {
            if (string.IsNullOrWhiteSpace(sessionIdentifier))
                throw new ArgumentException("Session identifier cannot be null or empty", nameof(sessionIdentifier));

            if (_ticketTypeSessions.Any(tts => tts.SessionIdentifier == sessionIdentifier))
                throw new DomainException($"Session {sessionIdentifier} is already included in this ticket type");

            var ticketTypeSession = new EventTicketTypeSession(Id, sessionIdentifier);
            _ticketTypeSessions.Add(ticketTypeSession);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Removes a session from this ticket type
        /// </summary>
        public void RemoveSession(string sessionIdentifier)
        {
            var ticketTypeSession = _ticketTypeSessions.FirstOrDefault(tts => tts.SessionIdentifier == sessionIdentifier);
            if (ticketTypeSession == null)
                throw new DomainException($"Session {sessionIdentifier} is not included in this ticket type");

            if (_ticketTypeSessions.Count == 1)
                throw new DomainException("Ticket type must include at least one session");

            _ticketTypeSessions.Remove(ticketTypeSession);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the sessions included in this ticket type
        /// </summary>
        public void UpdateIncludedSessions(IEnumerable<string> sessionIdentifiers)
        {
            if (sessionIdentifiers == null || !sessionIdentifiers.Any())
                throw new DomainException("Ticket type must include at least one session");

            _ticketTypeSessions.Clear();
            foreach (var sessionId in sessionIdentifiers.Distinct())
            {
                if (!string.IsNullOrWhiteSpace(sessionId))
                {
                    _ticketTypeSessions.Add(new EventTicketTypeSession(Id, sessionId));
                }
            }

            if (!_ticketTypeSessions.Any())
                throw new DomainException("Ticket type must include at least one valid session");

            UpdatedAt = DateTime.UtcNow;
        }

        private void ValidatePricing(decimal minPrice, decimal maxPrice)
        {
            if (minPrice < 0)
                throw new DomainException("Minimum price cannot be negative");

            if (maxPrice < 0)
                throw new DomainException("Maximum price cannot be negative");

            if (minPrice > maxPrice)
                throw new DomainException("Minimum price cannot be greater than maximum price");
        }

        private void ValidateQuantity(int? quantityAvailable)
        {
            if (quantityAvailable.HasValue && quantityAvailable.Value <= 0)
                throw new DomainException("Quantity available must be greater than zero when specified");
        }

        private void ValidateSalesEndDate(DateTime? salesEndDate)
        {
            if (salesEndDate.HasValue && salesEndDate.Value < DateTime.UtcNow)
                throw new DomainException("Sales end date cannot be in the past");
        }
    }
}