using System;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.Exceptions;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Represents a free RSVP for a social event
    /// Business Rules:
    /// - Only available for Social Events (EventType.Social)
    /// - No payment required
    /// - Can optionally purchase ticket later
    /// - Can be cancelled anytime before event
    /// - Users who RSVP can still purchase tickets later to support the event
    /// </summary>
    public class RSVP
    {
        // Private constructor for EF Core
        private RSVP() 
        {
            ConfirmationCode = null!;
        }

        public RSVP(User user, Event eventToAttend, string? dietaryRestrictions = null)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            if (eventToAttend == null)
                throw new ArgumentNullException(nameof(eventToAttend));

            // Business rule: RSVP is only available for social events
            if (eventToAttend.EventType != EventType.Social)
                throw new DomainException("RSVP is only available for social events");

            // Business rule: Check event capacity
            if (!eventToAttend.HasAvailableCapacity())
                throw new DomainException("Event is at full capacity");

            Id = Guid.NewGuid();
            UserId = user.Id;
            User = user;
            EventId = eventToAttend.Id;
            Event = eventToAttend;
            Status = RSVPStatus.Confirmed;
            DietaryRestrictions = dietaryRestrictions;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            ConfirmationCode = GenerateConfirmationCode();
        }

        public Guid Id { get; private set; }
        
        public Guid UserId { get; private set; }
        public User User { get; private set; } = null!;
        
        public Guid EventId { get; private set; }
        public Event Event { get; private set; } = null!;
        
        public RSVPStatus Status { get; private set; }
        
        public string? DietaryRestrictions { get; private set; }
        
        public DateTime CreatedAt { get; private set; }
        
        public DateTime UpdatedAt { get; private set; }
        
        public DateTime? CancelledAt { get; private set; }
        
        public string? CancellationReason { get; private set; }
        
        public string ConfirmationCode { get; private set; }
        
        // Link to ticket if user decides to purchase later
        public Guid? TicketId { get; private set; }
        public Registration? Ticket { get; private set; }

        /// <summary>
        /// Cancels the RSVP with optional reason
        /// </summary>
        public void Cancel(string? reason = null)
        {
            if (Status == RSVPStatus.Cancelled)
                throw new DomainException("RSVP is already cancelled");

            Status = RSVPStatus.Cancelled;
            CancelledAt = DateTime.UtcNow;
            CancellationReason = reason ?? "User requested cancellation";
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Links a ticket purchase to this RSVP when user upgrades to paid ticket
        /// </summary>
        public void LinkTicketPurchase(Registration ticket)
        {
            if (ticket == null)
                throw new ArgumentNullException(nameof(ticket));

            if (ticket.EventId != EventId)
                throw new DomainException("Ticket must be for the same event");

            TicketId = ticket.Id;
            Ticket = ticket;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the RSVP as checked in at the event
        /// </summary>
        public void CheckIn()
        {
            if (Status == RSVPStatus.Cancelled)
                throw new DomainException("Cannot check in a cancelled RSVP");

            if (Status == RSVPStatus.CheckedIn)
                throw new DomainException("RSVP is already checked in");

            Status = RSVPStatus.CheckedIn;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates dietary restrictions
        /// </summary>
        public void UpdateDietaryRestrictions(string? dietaryRestrictions)
        {
            if (Status == RSVPStatus.Cancelled)
                throw new DomainException("Cannot update a cancelled RSVP");

            DietaryRestrictions = dietaryRestrictions;
            UpdatedAt = DateTime.UtcNow;
        }

        private string GenerateConfirmationCode()
        {
            // Generate an 8-digit random number for the confirmation code
            var random = new Random().Next(10000000, 99999999);
            return $"RSVP-{random}";
        }
    }
}