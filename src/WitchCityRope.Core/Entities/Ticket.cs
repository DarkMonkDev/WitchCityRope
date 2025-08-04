using System;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Represents a user's ticket for a paid event
    /// Business Rules:
    /// - Tickets can be cancelled 48-72 hours before event (configurable per event)
    /// - Ticket requires payment to be confirmed
    /// - Users can select from sliding scale pricing
    /// </summary>
    public class Ticket
    {
        // Private constructor for EF Core
        private Ticket() 
        { 
            ConfirmationCode = string.Empty;
            // Nullable fields will default to null
        }

        public Ticket(
            Guid userId,
            Event eventToRegister,
            Money selectedPrice,
            string? dietaryRestrictions = null,
            string? accessibilityNeeds = null,
            string? emergencyContactName = null,
            string? emergencyContactPhone = null,
            bool skipCapacityCheck = false)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
            
            if (eventToRegister == null)
                throw new ArgumentNullException(nameof(eventToRegister));

            if (selectedPrice == null)
                throw new ArgumentNullException(nameof(selectedPrice));

            // Skip capacity check for waitlisted tickets
            if (!skipCapacityCheck)
            {
                ValidateEventCapacity(eventToRegister);
            }
            ValidateSelectedPrice(eventToRegister, selectedPrice);

            Id = Guid.NewGuid();
            UserId = userId;
            EventId = eventToRegister.Id;
            Event = eventToRegister;
            SelectedPrice = selectedPrice;
            Status = TicketStatus.Pending;
            DietaryRestrictions = dietaryRestrictions;
            AccessibilityNeeds = accessibilityNeeds;
            EmergencyContactName = emergencyContactName;
            EmergencyContactPhone = emergencyContactPhone;
            PurchasedAt = DateTime.UtcNow;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            ConfirmationCode = GenerateConfirmationCode();
            // CancellationReason will be null initially
        }

        public Guid Id { get; private set; }
        
        public Guid UserId { get; private set; }
        
        // Navigation property removed - user lookups will be handled differently
        
        public Guid EventId { get; private set; }
        
        public Event Event { get; private set; }
        
        /// <summary>
        /// The price selected from the sliding scale
        /// </summary>
        public Money SelectedPrice { get; private set; }
        
        public TicketStatus Status { get; private set; }
        
        public string? DietaryRestrictions { get; private set; }
        
        public string? AccessibilityNeeds { get; private set; }
        
        /// <summary>
        /// Emergency contact name for the attendee
        /// </summary>
        public string? EmergencyContactName { get; private set; }
        
        /// <summary>
        /// Emergency contact phone number for the attendee
        /// </summary>
        public string? EmergencyContactPhone { get; private set; }
        
        public DateTime PurchasedAt { get; private set; }
        
        public DateTime? ConfirmedAt { get; private set; }
        
        public DateTime? CancelledAt { get; private set; }
        
        public string? CancellationReason { get; private set; }
        
        public DateTime UpdatedAt { get; private set; }
        
        public Payment? Payment { get; private set; }
        
        /// <summary>
        /// Timestamp when the attendee was checked in to the event
        /// </summary>
        public DateTime? CheckedInAt { get; private set; }
        
        /// <summary>
        /// ID of the staff member who performed the check-in
        /// </summary>
        public Guid? CheckedInBy { get; private set; }
        
        /// <summary>
        /// Timestamp when the ticket was created
        /// </summary>
        public DateTime CreatedAt { get; private set; }
        
        /// <summary>
        /// Unique confirmation code for the ticket
        /// </summary>
        public string ConfirmationCode { get; private set; }

        /// <summary>
        /// Confirms the ticket after payment is processed
        /// </summary>
        public void Confirm(Payment payment)
        {
            if (Status != TicketStatus.Pending)
                throw new DomainException("Only pending tickets can be confirmed");

            if (payment == null)
                throw new ArgumentNullException(nameof(payment));

            if (payment.Status != PaymentStatus.Completed)
                throw new DomainException("Payment must be completed to confirm ticket");

            Status = TicketStatus.Confirmed;
            Payment = payment;
            ConfirmedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Cancels the ticket
        /// </summary>
        public void Cancel(string reason, int refundWindowHours = 48)
        {
            if (Status == TicketStatus.Cancelled)
                throw new DomainException("Ticket is already cancelled");

            if (Status == TicketStatus.CheckedIn)
                throw new DomainException("Cannot cancel after check-in");

            Status = TicketStatus.Cancelled;
            CancelledAt = DateTime.UtcNow;
            CancellationReason = reason ?? "User requested cancellation";
            UpdatedAt = DateTime.UtcNow;

            // Check if eligible for refund based on refund window
            if (IsEligibleForRefund(refundWindowHours) && Payment != null)
            {
                Payment.InitiateRefund();
            }
        }

        /// <summary>
        /// Cancels the ticket with default reason
        /// </summary>
        public void Cancel()
        {
            Cancel("User requested cancellation");
        }

        /// <summary>
        /// Confirms a waitlisted ticket when a spot becomes available
        /// </summary>
        public void ConfirmFromWaitlist()
        {
            if (Status != TicketStatus.Waitlisted)
                throw new DomainException("Only waitlisted tickets can be confirmed from waitlist");

            Status = TicketStatus.Confirmed;
            ConfirmedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the ticket as checked in at the event
        /// </summary>
        public void CheckIn(Guid checkedInBy)
        {
            if (Status != TicketStatus.Confirmed)
                throw new DomainException("Only confirmed tickets can be checked in");

            if (DateTime.UtcNow < Event.StartDate)
                throw new DomainException("Cannot check in before event starts");

            Status = TicketStatus.CheckedIn;
            CheckedInAt = DateTime.UtcNow;
            CheckedInBy = checkedInBy;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates dietary restrictions
        /// </summary>
        public void UpdateDietaryRestrictions(string? dietaryRestrictions)
        {
            if (Status == TicketStatus.Cancelled)
                throw new DomainException("Cannot update cancelled ticket");

            DietaryRestrictions = dietaryRestrictions;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates accessibility needs
        /// </summary>
        public void UpdateAccessibilityNeeds(string? accessibilityNeeds)
        {
            if (Status == TicketStatus.Cancelled)
                throw new DomainException("Cannot update cancelled ticket");

            AccessibilityNeeds = accessibilityNeeds;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if ticket is eligible for refund based on cancellation window
        /// </summary>
        public bool IsEligibleForRefund(int refundWindowHours = 48)
        {
            if (Status != TicketStatus.Cancelled)
                return false;

            if (Event == null)
                return false;

            var hoursUntilEvent = (Event.StartDate - DateTime.UtcNow).TotalHours;
            return hoursUntilEvent >= refundWindowHours;
        }

        private void ValidateEventCapacity(Event eventToRegister)
        {
            if (!eventToRegister.HasAvailableCapacity())
                throw new DomainException("Event is at full capacity");
        }

        private void ValidateSelectedPrice(Event eventToRegister, Money selectedPrice)
        {
            var validPrice = false;
            foreach (var tier in eventToRegister.PricingTiers)
            {
                if (tier.Amount == selectedPrice.Amount && tier.Currency == selectedPrice.Currency)
                {
                    validPrice = true;
                    break;
                }
            }

            if (!validPrice)
                throw new DomainException("Selected price is not valid for this event");
        }
        
        private string GenerateConfirmationCode()
        {
            // Generate a unique confirmation code
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmm");
            var random = new Random().Next(1000, 9999);
            return $"TKT-{timestamp}-{random}";
        }
    }
}