using System;
using WitchCityRope.Core.Exceptions;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Junction entity linking ticket types to specific sessions
    /// This enables many-to-many relationship between EventTicketType and EventSession
    /// Business Rules:
    /// - Each combination of TicketTypeId and SessionIdentifier must be unique
    /// - SessionIdentifier must follow S1, S2, S3 format
    /// </summary>
    public class EventTicketTypeSession
    {
        // Private constructor for EF Core
        private EventTicketTypeSession()
        {
            SessionIdentifier = null!;
        }

        public EventTicketTypeSession(Guid ticketTypeId, string sessionIdentifier)
        {
            ValidateSessionIdentifier(sessionIdentifier);

            Id = Guid.NewGuid();
            TicketTypeId = ticketTypeId;
            SessionIdentifier = sessionIdentifier;
            CreatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }

        /// <summary>
        /// Foreign key to EventTicketType
        /// </summary>
        public Guid TicketTypeId { get; private set; }

        /// <summary>
        /// Session identifier (S1, S2, S3, etc.) that this ticket type includes
        /// </summary>
        public string SessionIdentifier { get; private set; }

        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Navigation property to the ticket type
        /// </summary>
        public EventTicketType TicketType { get; private set; }

        /// <summary>
        /// Navigation property to the session (via EventSession.SessionIdentifier)
        /// </summary>
        public EventSession EventSession { get; private set; }

        private void ValidateSessionIdentifier(string sessionIdentifier)
        {
            if (string.IsNullOrWhiteSpace(sessionIdentifier))
                throw new ArgumentException("Session identifier cannot be null or empty", nameof(sessionIdentifier));

            // Basic validation for session identifier format (S1, S2, etc.)
            if (!sessionIdentifier.StartsWith("S") || sessionIdentifier.Length < 2)
                throw new DomainException("Session identifier must follow format S1, S2, S3, etc.");
        }
    }
}