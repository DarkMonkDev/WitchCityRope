using System;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Represents a user's registration for an event
    /// Business Rules:
    /// - Registrations can be cancelled 48-72 hours before event (configurable per event)
    /// - Registration requires payment to be confirmed
    /// - Users can select from sliding scale pricing
    /// </summary>
    public class Registration
    {
        // Private constructor for EF Core
        private Registration() { }

        public Registration(
            User user,
            Event eventToRegister,
            Money selectedPrice,
            EventTicketType? ticketType = null,
            string? dietaryRestrictions = null,
            string? accessibilityNeeds = null,
            string? emergencyContactName = null,
            string? emergencyContactPhone = null)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            if (eventToRegister == null)
                throw new ArgumentNullException(nameof(eventToRegister));

            if (selectedPrice == null)
                throw new ArgumentNullException(nameof(selectedPrice));

            ValidateEventCapacity(eventToRegister);
            ValidateSelectedPrice(eventToRegister, selectedPrice);
            ValidateTicketType(eventToRegister, ticketType);

            Id = Guid.NewGuid();
            UserId = user.Id;
            User = user;
            EventId = eventToRegister.Id;
            Event = eventToRegister;
            EventTicketTypeId = ticketType?.Id;
            EventTicketType = ticketType;
            SelectedPrice = selectedPrice;
            Status = RegistrationStatus.Pending;
            DietaryRestrictions = dietaryRestrictions;
            AccessibilityNeeds = accessibilityNeeds;
            EmergencyContactName = emergencyContactName;
            EmergencyContactPhone = emergencyContactPhone;
            RegisteredAt = DateTime.UtcNow;
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
        /// Optional reference to the ticket type used for this registration
        /// </summary>
        public Guid? EventTicketTypeId { get; private set; }
        
        /// <summary>
        /// The ticket type used for this registration (nullable for legacy registrations)
        /// </summary>
        public EventTicketType? EventTicketType { get; private set; }
        
        /// <summary>
        /// The price selected from the sliding scale
        /// </summary>
        public Money SelectedPrice { get; private set; }
        
        public RegistrationStatus Status { get; private set; }
        
        public string DietaryRestrictions { get; private set; }
        
        public string AccessibilityNeeds { get; private set; }
        
        /// <summary>
        /// Emergency contact name for the attendee
        /// </summary>
        public string EmergencyContactName { get; private set; }
        
        /// <summary>
        /// Emergency contact phone number for the attendee
        /// </summary>
        public string EmergencyContactPhone { get; private set; }
        
        public DateTime RegisteredAt { get; private set; }
        
        public DateTime? ConfirmedAt { get; private set; }
        
        public DateTime? CancelledAt { get; private set; }
        
        public string CancellationReason { get; private set; }
        
        public DateTime UpdatedAt { get; private set; }
        
        public Payment Payment { get; private set; }
        
        /// <summary>
        /// Timestamp when the attendee was checked in to the event
        /// </summary>
        public DateTime? CheckedInAt { get; private set; }
        
        /// <summary>
        /// ID of the staff member who performed the check-in
        /// </summary>
        public Guid? CheckedInBy { get; private set; }
        
        /// <summary>
        /// Timestamp when the registration was created
        /// </summary>
        public DateTime CreatedAt { get; private set; }
        
        /// <summary>
        /// Unique confirmation code for the registration
        /// </summary>
        public string ConfirmationCode { get; private set; }

        /// <summary>
        /// Confirms the registration after payment is processed
        /// </summary>
        public void Confirm(Payment payment)
        {
            if (Status != RegistrationStatus.Pending)
                throw new DomainException("Only pending registrations can be confirmed");

            if (payment == null)
                throw new ArgumentNullException(nameof(payment));

            if (payment.Status != PaymentStatus.Completed)
                throw new DomainException("Payment must be completed to confirm registration");

            Status = RegistrationStatus.Confirmed;
            Payment = payment;
            ConfirmedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Cancels the registration
        /// </summary>
        public void Cancel(string reason, int refundWindowHours = 48)
        {
            if (Status == RegistrationStatus.Cancelled)
                throw new DomainException("Registration is already cancelled");

            if (Status == RegistrationStatus.CheckedIn)
                throw new DomainException("Cannot cancel after check-in");

            Status = RegistrationStatus.Cancelled;
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
        /// Marks the registration as checked in at the event
        /// </summary>
        public void CheckIn(Guid checkedInBy)
        {
            if (Status != RegistrationStatus.Confirmed)
                throw new DomainException("Only confirmed registrations can be checked in");

            if (DateTime.UtcNow < Event.StartDate)
                throw new DomainException("Cannot check in before event starts");

            Status = RegistrationStatus.CheckedIn;
            CheckedInAt = DateTime.UtcNow;
            CheckedInBy = checkedInBy;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates dietary restrictions
        /// </summary>
        public void UpdateDietaryRestrictions(string dietaryRestrictions)
        {
            if (Status == RegistrationStatus.Cancelled)
                throw new DomainException("Cannot update cancelled registration");

            DietaryRestrictions = dietaryRestrictions;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates accessibility needs
        /// </summary>
        public void UpdateAccessibilityNeeds(string accessibilityNeeds)
        {
            if (Status == RegistrationStatus.Cancelled)
                throw new DomainException("Cannot update cancelled registration");

            AccessibilityNeeds = accessibilityNeeds;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if registration is eligible for refund based on cancellation window
        /// </summary>
        public bool IsEligibleForRefund(int refundWindowHours = 48)
        {
            if (Status != RegistrationStatus.Cancelled)
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

        private void ValidateTicketType(Event eventToRegister, EventTicketType? ticketType)
        {
            if (ticketType == null)
                return; // Ticket type is optional for legacy support
            
            if (ticketType.EventId != eventToRegister.Id)
                throw new DomainException("Ticket type must belong to the event being registered for");
            
            if (!ticketType.IsCurrentlyOnSale())
                throw new DomainException("Selected ticket type is not currently on sale");
        }
        
        private string GenerateConfirmationCode()
        {
            // Generate a unique ticket confirmation code
            // Format: TKT-YYYYMMDDHHMM-XXXX
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmm");
            var random = new Random().Next(1000, 9999);
            return $"TKT-{timestamp}-{random}";
        }
    }
}