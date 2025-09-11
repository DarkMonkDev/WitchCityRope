using System;
using WitchCityRope.Core.Exceptions;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Represents a session within an event (e.g., S1, S2, S3)
    /// Business Rules:
    /// - Sessions have capacity limits
    /// - Sessions can be required or optional
    /// - Session times cannot overlap on the same date
    /// - Session capacity must be greater than zero
    /// </summary>
    public class EventSession
    {
        // Private constructor for EF Core
        private EventSession()
        {
            SessionIdentifier = null!;
            Name = null!;
        }

        public EventSession(
            Guid eventId,
            string sessionIdentifier,
            string name,
            DateTime date,
            TimeSpan startTime,
            TimeSpan endTime,
            int capacity,
            bool isRequired = false)
        {
            ValidateCapacity(capacity);
            ValidateTimes(startTime, endTime);
            ValidateSessionIdentifier(sessionIdentifier);

            Id = Guid.NewGuid();
            EventId = eventId;
            SessionIdentifier = sessionIdentifier ?? throw new ArgumentNullException(nameof(sessionIdentifier));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Date = date;
            StartTime = startTime;
            EndTime = endTime;
            Capacity = capacity;
            IsRequired = isRequired;
            RegisteredCount = 0;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }

        public Guid EventId { get; private set; }

        /// <summary>
        /// Session identifier (S1, S2, S3, etc.)
        /// </summary>
        public string SessionIdentifier { get; private set; }

        /// <summary>
        /// Display name for the session (e.g., "Friday Workshop", "Saturday Workshop")
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Date when this session occurs
        /// </summary>
        public DateTime Date { get; private set; }

        /// <summary>
        /// Start time of the session
        /// </summary>
        public TimeSpan StartTime { get; private set; }

        /// <summary>
        /// End time of the session
        /// </summary>
        public TimeSpan EndTime { get; private set; }

        /// <summary>
        /// Maximum number of attendees for this session
        /// </summary>
        public int Capacity { get; private set; }

        /// <summary>
        /// Whether this session is required for the event
        /// </summary>
        public bool IsRequired { get; private set; }

        /// <summary>
        /// Current number of registered attendees
        /// </summary>
        public int RegisteredCount { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        /// <summary>
        /// Navigation property to parent event
        /// </summary>
        public Event Event { get; private set; }

        /// <summary>
        /// Gets the number of available spots
        /// </summary>
        public int GetAvailableSpots()
        {
            return Capacity - RegisteredCount;
        }

        /// <summary>
        /// Checks if the session has available capacity
        /// </summary>
        public bool HasAvailableCapacity(int requestedQuantity = 1)
        {
            return GetAvailableSpots() >= requestedQuantity;
        }

        /// <summary>
        /// Updates the session details
        /// </summary>
        public void UpdateDetails(string name, DateTime date, TimeSpan startTime, TimeSpan endTime, int capacity)
        {
            ValidateTimes(startTime, endTime);
            ValidateCapacity(capacity);

            if (capacity < RegisteredCount)
                throw new DomainException("Cannot reduce capacity below current registered count");

            Name = name ?? throw new ArgumentNullException(nameof(name));
            Date = date;
            StartTime = startTime;
            EndTime = endTime;
            Capacity = capacity;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the registered count for this session
        /// </summary>
        public void UpdateRegisteredCount(int registeredCount)
        {
            if (registeredCount < 0)
                throw new DomainException("Registered count cannot be negative");

            if (registeredCount > Capacity)
                throw new DomainException("Registered count cannot exceed capacity");

            RegisteredCount = registeredCount;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Increments the registered count for this session
        /// </summary>
        public void IncrementRegisteredCount(int quantity = 1)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive", nameof(quantity));

            if (RegisteredCount + quantity > Capacity)
                throw new DomainException("Cannot register more attendees than capacity allows");

            RegisteredCount += quantity;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Decrements the registered count for this session
        /// </summary>
        public void DecrementRegisteredCount(int quantity = 1)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive", nameof(quantity));

            if (RegisteredCount - quantity < 0)
                throw new DomainException("Cannot have negative registered count");

            RegisteredCount -= quantity;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if this session overlaps with another session on the same date
        /// </summary>
        public bool OverlapsWith(EventSession other)
        {
            if (other == null)
                return false;

            if (Date.Date != other.Date.Date)
                return false;

            // Check for time overlap
            return StartTime < other.EndTime && EndTime > other.StartTime;
        }

        private void ValidateCapacity(int capacity)
        {
            if (capacity <= 0)
                throw new DomainException("Session capacity must be greater than zero");
        }

        private void ValidateTimes(TimeSpan startTime, TimeSpan endTime)
        {
            if (startTime >= endTime)
                throw new DomainException("Session start time must be before end time");
        }

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