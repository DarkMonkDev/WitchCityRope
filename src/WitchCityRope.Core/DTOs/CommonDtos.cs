using System;
using System.Collections.Generic;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Core.DTOs
{
    // Event-related DTOs
    public class RegisterForEventRequest
    {
        public Guid EventId { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? DietaryRestrictions { get; set; }
        public string? AccessibilityNeeds { get; set; }
        public string? PaymentMethod { get; set; }
        public int? SelectedPriceId { get; set; }
    }

    public class CreateEventRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public int MaxAttendees { get; set; }
        public decimal Price { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<string> RequiredSkillLevels { get; set; } = new();
        public bool RequiresVetting { get; set; }
        public decimal? IndividualPrice { get; set; }
        public decimal? CouplesPrice { get; set; }
        public int? RefundCutoffHours { get; set; }
    }

    public class EventDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public int MaxAttendees { get; set; }
        public int CurrentAttendees { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public List<string> RequiredSkillLevels { get; set; } = new();
        public bool RequiresVetting { get; set; }
        public string EventType { get; set; } = string.Empty;
    }

    public class CreateEventResponse
    {
        public Guid EventId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class UpdateEventRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public int? MaxAttendees { get; set; }
        public decimal? Price { get; set; }
        public List<string>? Tags { get; set; }
        public List<string>? RequiredSkillLevels { get; set; }
        public bool? RequiresVetting { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? RegistrationOpensAt { get; set; }
        public DateTime? RegistrationClosesAt { get; set; }
        public List<string>? TicketTypes { get; set; }
        public decimal? IndividualPrice { get; set; }
        public decimal? CouplesPrice { get; set; }
        public int? RefundCutoffHours { get; set; }
    }

    // Registration-related DTOs
    public class EventRegistrationRequest
    {
        public string EmergencyContactName { get; set; } = string.Empty;
        public string EmergencyContactPhone { get; set; } = string.Empty;
        public string? DietaryRestrictions { get; set; }
        public string? AccessibilityNeeds { get; set; }
        public decimal? PaymentAmount { get; set; }
        public string? PaymentToken { get; set; }
    }

    public class EventRegistrationResponse
    {
        public Guid RegistrationId { get; set; }
        public string ConfirmationCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class RegistrationDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string EventLocation { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
        public string? ConfirmationCode { get; set; }
        public decimal? AmountPaid { get; set; }
        public DateTime? CheckedInAt { get; set; }
    }

    // Vetting-related DTOs
    public class VettingApplicationRequest
    {
        public string LegalName { get; set; } = string.Empty;
        public string PreferredName { get; set; } = string.Empty;
        public string FetlifeName { get; set; } = string.Empty;
        public string ReferenceOneName { get; set; } = string.Empty;
        public string ReferenceOneContact { get; set; } = string.Empty;
        public string ReferenceTwoName { get; set; } = string.Empty;
        public string ReferenceTwoContact { get; set; } = string.Empty;
        public string Experience { get; set; } = string.Empty;
        public string WhyJoin { get; set; } = string.Empty;
        public bool AgreeToCommunityGuidelines { get; set; }
        public bool AgreeToPhotoPolicy { get; set; }
    }

    public class VettingApplicationResponse
    {
        public Guid ApplicationId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class VettingApplicationDto
    {
        public Guid Id { get; set; }
        public Guid ApplicantId { get; set; }
        public string ApplicantName { get; set; } = string.Empty;
        public VettingStatus Status { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string? ReviewerNotes { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewerName { get; set; }
        public string ApplicationText { get; set; } = string.Empty;
        public string? ReviewNotes { get; set; }
    }

    public class ReviewApplicationRequest
    {
        public string Decision { get; set; } = string.Empty; // Approved, Rejected, NeedsMoreInfo
        public string Notes { get; set; } = string.Empty;
    }

    public class ReviewApplicationResponse
    {
        public Guid ApplicationId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    // Check-in DTOs
    public class CheckInRequest
    {
        public string ConfirmationCode { get; set; } = string.Empty;
        public bool AcceptedWaiver { get; set; }
        public string? EmergencyContactUpdate { get; set; }
    }

    public class CheckInResponse
    {
        public Guid RegistrationId { get; set; }
        public string AttendeeName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CheckInTime { get; set; }
    }

    // Payment DTOs
    public class ProcessPaymentRequest
    {
        public Guid PaymentId { get; set; }
        public Guid RegistrationId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string PaymentMethod { get; set; } = string.Empty;
        public string? PaymentToken { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }

    public class ProcessPaymentResponse
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public DateTime ProcessedAt { get; set; }
    }

    // Ticket and RSVP DTOs
    public class EventTicketRequest
    {
        public string EmergencyContactName { get; set; } = string.Empty;
        public string EmergencyContactPhone { get; set; } = string.Empty;
        public string? DietaryRestrictions { get; set; }
        public string? AccessibilityNeeds { get; set; }
        public decimal PaymentAmount { get; set; }
        public string? PaymentToken { get; set; }
    }

    public class EventTicketResponse
    {
        public Guid TicketId { get; set; }
        public string ConfirmationCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class TicketDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string EventLocation { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime PurchasedAt { get; set; }
        public string? ConfirmationCode { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime? CheckedInAt { get; set; }
    }

    public class RsvpDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string EventLocation { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CheckedInAt { get; set; }
        public string UserSceneName { get; set; } = string.Empty;
    }

    public class UpdateRsvpRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    // Safety DTOs
    public class IncidentReportRequest
    {
        public bool IsAnonymous { get; set; }
        public Guid? EventId { get; set; }
        public DateTime IncidentDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> InvolvedParties { get; set; } = new();
        public string Severity { get; set; } = string.Empty; // Low, Medium, High, Critical
        public bool RequiresImmediateAction { get; set; }
        public string? WitnessInformation { get; set; }
    }

    public class IncidentReportResponse
    {
        public Guid ReportId { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class IncidentReportDto
    {
        public Guid Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public DateTime ReportedAt { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsAnonymous { get; set; }
        public string? EventName { get; set; }
    }
}