using System;
using System.Security.Cryptography;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Represents a free RSVP for social events in the WitchCityRope system
    /// Business Rules:
    /// - RSVPs are only allowed for Social events (EventType.Social)
    /// - Classes/Workshops require ticket purchase instead of RSVPs
    /// - Users can RSVP for free and optionally purchase tickets later to support the event
    /// - RSVP confirmation codes use format: RSVP-XXXXXXXX
    /// </summary>
    public class RSVP
    {
        // Private constructor for EF Core
        private RSVP() { }

        public RSVP(
            User user,
            Event eventToRsvp,
            string? emergencyContactName = null,
            string? emergencyContactPhone = null)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            if (eventToRsvp == null)
                throw new ArgumentNullException(nameof(eventToRsvp));

            // Business rule: RSVPs only allowed for Social events
            if (eventToRsvp.EventType != EventType.Social)
                throw new DomainException("RSVPs are only allowed for Social events");

            ValidateEventCapacity(eventToRsvp);
            ValidateUserEligibility(user, eventToRsvp);

            Id = Guid.NewGuid();
            UserId = user.Id;
            User = user;
            EventId = eventToRsvp.Id;
            Event = eventToRsvp;
            Status = eventToRsvp.HasAvailableCapacity() ? RSVPStatus.Confirmed : RSVPStatus.Waitlisted;
            EmergencyContactName = emergencyContactName;
            EmergencyContactPhone = emergencyContactPhone;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            ConfirmationCode = GenerateConfirmationCode();
        }

        public Guid Id { get; private set; }
        
        public Guid UserId { get; private set; }
        
        public User User { get; private set; }
        
        public Guid EventId { get; private set; }
        
        public Event Event { get; private set; }
        
        /// <summary>
        /// Current status of the RSVP
        /// </summary>
        public RSVPStatus Status { get; private set; }
        
        /// <summary>
        /// Emergency contact name for the attendee
        /// </summary>
        public string? EmergencyContactName { get; private set; }
        
        /// <summary>
        /// Emergency contact phone number for the attendee
        /// </summary>
        public string? EmergencyContactPhone { get; private set; }
        
        /// <summary>
        /// Timestamp when the RSVP was created
        /// </summary>
        public DateTime CreatedAt { get; private set; }
        
        /// <summary>
        /// Timestamp when the RSVP was last updated
        /// </summary>
        public DateTime UpdatedAt { get; private set; }
        
        /// <summary>
        /// Timestamp when the attendee was checked in to the event
        /// </summary>
        public DateTime? CheckedInAt { get; private set; }
        
        /// <summary>
        /// ID of the staff member who performed the check-in
        /// </summary>
        public Guid? CheckedInBy { get; private set; }
        
        /// <summary>
        /// Timestamp when the RSVP was cancelled
        /// </summary>
        public DateTime? CancelledAt { get; private set; }
        
        /// <summary>
        /// Reason for cancellation
        /// </summary>
        public string? CancellationReason { get; private set; }
        
        /// <summary>
        /// Unique confirmation code for the RSVP (format: RSVP-XXXXXXXX)
        /// </summary>
        public string ConfirmationCode { get; private set; }

        /// <summary>
        /// Cancels the RSVP
        /// </summary>
        public void Cancel(string? reason = null)
        {
            if (Status == RSVPStatus.Cancelled)
                throw new DomainException("RSVP is already cancelled");

            if (Status == RSVPStatus.CheckedIn)
                throw new DomainException("Cannot cancel after check-in");

            Status = RSVPStatus.Cancelled;
            CancelledAt = DateTime.UtcNow;
            CancellationReason = reason ?? "User requested cancellation";
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Moves RSVP from waitlist to confirmed when capacity becomes available
        /// </summary>
        public void ConfirmFromWaitlist()
        {
            if (Status != RSVPStatus.Waitlisted)
                throw new DomainException("Only waitlisted RSVPs can be confirmed");

            Status = RSVPStatus.Confirmed;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the RSVP as checked in at the event
        /// </summary>
        public void CheckIn(Guid checkedInBy)
        {
            if (Status != RSVPStatus.Confirmed)
                throw new DomainException("Only confirmed RSVPs can be checked in");

            if (DateTime.UtcNow < Event.StartDate)
                throw new DomainException("Cannot check in before event starts");

            Status = RSVPStatus.CheckedIn;
            CheckedInAt = DateTime.UtcNow;
            CheckedInBy = checkedInBy;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates emergency contact information
        /// </summary>
        public void UpdateEmergencyContact(string? name, string? phone)
        {
            if (Status == RSVPStatus.Cancelled)
                throw new DomainException("Cannot update cancelled RSVP");

            EmergencyContactName = name;
            EmergencyContactPhone = phone;
            UpdatedAt = DateTime.UtcNow;
        }

        private void ValidateEventCapacity(Event eventToRsvp)
        {
            // Note: We allow waitlisted RSVPs, so we don't throw an exception for full events
            // The constructor will set Status appropriately based on capacity
        }

        private void ValidateUserEligibility(User user, Event eventToRsvp)
        {
            // Check if event requires vetting and user is not vetted
            // Note: This assumes there's a property on User or Event to check vetting requirements
            // Implementation would depend on the actual vetting logic in the system
        }

        private string GenerateConfirmationCode()
        {
            // Generate cryptographically secure random code
            // Format: RSVP-XXXXXXXX (8 random alphanumeric characters)
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[8];
            rng.GetBytes(bytes);
            
            var result = new char[8];
            for (int i = 0; i < 8; i++)
            {
                result[i] = chars[bytes[i] % chars.Length];
            }
            
            return $"RSVP-{new string(result)}";
        }
    }
}