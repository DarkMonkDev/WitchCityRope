using System;
using System.Collections.Generic;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Represents a session within an event (e.g., S1, S2, S3)
    /// Business Rules:
    /// - Session identifier must be unique per event (not globally unique)
    /// - Start time must be before end time
    /// - Capacity must be greater than 0
    /// - Cannot delete sessions with existing registrations
    /// </summary>
    public class EventSession
    {
        private readonly List<EventTicketTypeSession> _ticketTypeInclusions = new();

        // Private constructor for EF Core
        private EventSession()
        {
            SessionIdentifier = null!;
            Name = null!;
        }

        /// <summary>
        /// Creates a new event session
        /// </summary>
        /// <param name="event">The event this session belongs to</param>
        /// <param name="sessionIdentifier">Session identifier (S1, S2, etc.)</param>
        /// <param name="name">Display name of the session</param>
        /// <param name="startDateTime">Session start time (must be UTC)</param>
        /// <param name="endDateTime">Session end time (must be UTC)</param>
        /// <param name="capacity">Maximum capacity for this session</param>
        public EventSession(Event @event, string sessionIdentifier, string name, 
            DateTime startDateTime, DateTime endDateTime, int capacity)
        {
            Id = Guid.NewGuid(); // CRITICAL: Must set this first!
            Event = @event ?? throw new ArgumentNullException(nameof(@event));
            EventId = @event.Id;
            SessionIdentifier = ValidateSessionIdentifier(sessionIdentifier);
            Name = ValidateName(name);
            
            // Ensure DateTimes are UTC for PostgreSQL compatibility
            StartDateTime = DateTime.SpecifyKind(startDateTime, DateTimeKind.Utc);
            EndDateTime = DateTime.SpecifyKind(endDateTime, DateTimeKind.Utc);
            
            ValidateDateTimeRange(StartDateTime, EndDateTime);
            Capacity = ValidateCapacity(capacity);
            
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
        /// Session identifier unique per event (S1, S2, S3, etc.)
        /// </summary>
        public string SessionIdentifier { get; private set; }
        
        /// <summary>
        /// Display name of the session
        /// </summary>
        public string Name { get; private set; }
        
        /// <summary>
        /// Session start time (stored as UTC)
        /// </summary>
        public DateTime StartDateTime { get; private set; }
        
        /// <summary>
        /// Session end time (stored as UTC)
        /// </summary>
        public DateTime EndDateTime { get; private set; }
        
        /// <summary>
        /// Maximum capacity for this session
        /// </summary>
        public int Capacity { get; private set; }
        
        /// <summary>
        /// Whether this session is active
        /// </summary>
        public bool IsActive { get; private set; }
        
        public DateTime CreatedAt { get; private set; }
        
        public DateTime UpdatedAt { get; private set; }
        
        /// <summary>
        /// Ticket types that include this session
        /// </summary>
        public IReadOnlyCollection<EventTicketTypeSession> TicketTypeInclusions => _ticketTypeInclusions.AsReadOnly();

        /// <summary>
        /// Updates session details
        /// </summary>
        public void UpdateDetails(string name, DateTime startDateTime, DateTime endDateTime, int capacity)
        {
            Name = ValidateName(name);
            
            // Ensure DateTimes are UTC for PostgreSQL compatibility
            var utcStart = DateTime.SpecifyKind(startDateTime, DateTimeKind.Utc);
            var utcEnd = DateTime.SpecifyKind(endDateTime, DateTimeKind.Utc);
            
            ValidateDateTimeRange(utcStart, utcEnd);
            StartDateTime = utcStart;
            EndDateTime = utcEnd;
            
            Capacity = ValidateCapacity(capacity);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Deactivates the session (soft delete)
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Reactivates the session
        /// </summary>
        public void Activate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        private static string ValidateSessionIdentifier(string sessionIdentifier)
        {
            if (string.IsNullOrWhiteSpace(sessionIdentifier))
                throw new ArgumentException("Session identifier cannot be empty", nameof(sessionIdentifier));
            
            if (sessionIdentifier.Length > 10)
                throw new ArgumentException("Session identifier cannot exceed 10 characters", nameof(sessionIdentifier));
            
            return sessionIdentifier.Trim();
        }

        private static string ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Session name cannot be empty", nameof(name));
            
            if (name.Length > 200)
                throw new ArgumentException("Session name cannot exceed 200 characters", nameof(name));
            
            return name.Trim();
        }

        private static void ValidateDateTimeRange(DateTime start, DateTime end)
        {
            if (start >= end)
                throw new ArgumentException("Session start time must be before end time");
        }

        private static int ValidateCapacity(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentException("Session capacity must be greater than 0");
            
            return capacity;
        }
    }
}