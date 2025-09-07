using System;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Junction entity representing which sessions are included in a ticket type
    /// Business Rules:
    /// - Ticket type and session must belong to the same event
    /// - Cannot create circular references
    /// - Unique combination of ticket type and session
    /// </summary>
    public class EventTicketTypeSession
    {
        // Private constructor for EF Core
        private EventTicketTypeSession()
        {
        }

        /// <summary>
        /// Creates a new ticket type session inclusion
        /// </summary>
        /// <param name="eventTicketType">The ticket type</param>
        /// <param name="eventSession">The session to include</param>
        public EventTicketTypeSession(EventTicketType eventTicketType, EventSession eventSession)
        {
            Id = Guid.NewGuid(); // CRITICAL: Must set this first!
            
            EventTicketType = eventTicketType ?? throw new ArgumentNullException(nameof(eventTicketType));
            EventTicketTypeId = eventTicketType.Id;
            
            EventSession = eventSession ?? throw new ArgumentNullException(nameof(eventSession));
            EventSessionId = eventSession.Id;
            
            // Validate that both ticket type and session belong to the same event
            if (eventTicketType.EventId != eventSession.EventId)
                throw new ArgumentException("Ticket type and session must belong to the same event");
            
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }
        
        public Guid EventTicketTypeId { get; private set; }
        
        /// <summary>
        /// Reference to the ticket type
        /// </summary>
        public EventTicketType EventTicketType { get; private set; } = null!;
        
        public Guid EventSessionId { get; private set; }
        
        /// <summary>
        /// Reference to the session
        /// </summary>
        public EventSession EventSession { get; private set; } = null!;
        
        public DateTime CreatedAt { get; private set; }
        
        public DateTime UpdatedAt { get; private set; }
    }
}