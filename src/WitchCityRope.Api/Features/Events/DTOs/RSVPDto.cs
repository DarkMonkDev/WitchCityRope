using System;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Api.Features.Events.DTOs
{
    /// <summary>
    /// DTO representing an RSVP for a social event
    /// </summary>
    public class RSVPDto
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string ConfirmationCode { get; set; } = string.Empty;
        public RSVPStatus Status { get; set; }
        public string? DietaryRestrictions { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        
        /// <summary>
        /// Indicates if user has also purchased a ticket for this event
        /// </summary>
        public bool HasLinkedTicket { get; set; }
    }

    /// <summary>
    /// Request to create a new RSVP for a social event
    /// </summary>
    public class RSVPRequest
    {
        public string? DietaryRestrictions { get; set; }
    }

    /// <summary>
    /// DTO representing user's attendance status for an event
    /// Shows whether they have an RSVP, ticket, or both
    /// </summary>
    public class AttendanceStatusDto
    {
        public Guid EventId { get; set; }
        
        /// <summary>
        /// True if user has a confirmed RSVP
        /// </summary>
        public bool HasRSVP { get; set; }
        
        /// <summary>
        /// True if user has a confirmed ticket/registration
        /// </summary>
        public bool HasTicket { get; set; }
        
        /// <summary>
        /// RSVP details if user has one
        /// </summary>
        public RSVPDto? RSVP { get; set; }
        
        /// <summary>
        /// Ticket/registration details if user has one
        /// </summary>
        public TicketDto? Ticket { get; set; }
        
        /// <summary>
        /// True if user can purchase additional tickets for this event
        /// </summary>
        public bool CanPurchaseTicket { get; set; }
        
        /// <summary>
        /// True if user can create a free RSVP for this event
        /// Only available for Social events and if user doesn't already have one
        /// </summary>
        public bool CanRSVP { get; set; }
        
        /// <summary>
        /// Reason why user cannot RSVP (if CanRSVP is false)
        /// </summary>
        public string? RSVPConstraintReason { get; set; }
    }

    /// <summary>
    /// DTO representing a ticket/registration for comparison with RSVP
    /// </summary>
    public class TicketDto
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string ConfirmationCode { get; set; } = string.Empty;
        public decimal PricePaid { get; set; }
        public string Currency { get; set; } = "USD";
        public RegistrationStatus Status { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
    }
}