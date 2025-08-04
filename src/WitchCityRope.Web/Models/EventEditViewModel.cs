using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.Validation;

namespace WitchCityRope.Web.Models
{
    /// <summary>
    /// ViewModel for the comprehensive event edit form with all tabs
    /// </summary>
    public class EventEditViewModel
    {
        // Basic Info Tab
        public Guid? Id { get; set; }
        
        [Required(ErrorMessage = EventValidationConstants.EVENT_TYPE_REQUIRED)]
        public EventType EventType { get; set; } = EventType.Workshop;
        
        [Required(ErrorMessage = EventValidationConstants.TITLE_REQUIRED)]
        [StringLength(EventValidationConstants.TITLE_MAX_LENGTH, ErrorMessage = EventValidationConstants.TITLE_MAX_LENGTH_MESSAGE)]
        public string Title { get; set; } = string.Empty;
        
        public string? ImageUrl { get; set; }
        
        [Required(ErrorMessage = EventValidationConstants.DESCRIPTION_REQUIRED)]
        [StringLength(EventValidationConstants.DESCRIPTION_MAX_LENGTH, ErrorMessage = EventValidationConstants.DESCRIPTION_MAX_LENGTH_MESSAGE)]
        public string Description { get; set; } = string.Empty;
        
        public Guid? InstructorId { get; set; }
        
        [Required(ErrorMessage = EventValidationConstants.START_DATE_REQUIRED)]
        [FutureDate]
        public DateTime StartDate { get; set; } = DateTime.Now.AddDays(7).Date.AddHours(19); // Default to 7PM next week
        
        [Required(ErrorMessage = EventValidationConstants.END_DATE_REQUIRED)]
        [EndDateAfterStartDate(nameof(StartDate))]
        [ValidEventDuration(nameof(StartDate))]
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(7).Date.AddHours(21); // Default to 9PM (2 hours)
        
        [Required(ErrorMessage = EventValidationConstants.LOCATION_REQUIRED)]
        [StringLength(EventValidationConstants.LOCATION_MAX_LENGTH, ErrorMessage = EventValidationConstants.LOCATION_MAX_LENGTH_MESSAGE)]
        public string Location { get; set; } = string.Empty;
        
        [Required(ErrorMessage = EventValidationConstants.CAPACITY_REQUIRED)]
        [Range(EventValidationConstants.CAPACITY_MIN, EventValidationConstants.CAPACITY_MAX, ErrorMessage = EventValidationConstants.CAPACITY_RANGE_MESSAGE)]
        public int Capacity { get; set; } = 60;
        
        public DateTime? RegistrationOpensAt { get; set; }
        
        [ValidRegistrationWindow(nameof(StartDate))]
        public DateTime? RegistrationClosesAt { get; set; }

        // Details & Requirements (from current implementation)
        public bool RequiresVetting { get; set; }
        public List<string> Tags { get; set; } = new();
        public string? SkillLevel { get; set; }
        public bool IsPublished { get; set; }

        // Tickets/Orders Tab
        public PricingType PricingType { get; set; } = PricingType.Fixed;
        public TicketType TicketTypes { get; set; } = TicketType.Individual;
        
        [RequiredForPricingType(nameof(PricingType), PricingType.Fixed)]
        [Range(typeof(decimal), "0", "10000", ErrorMessage = EventValidationConstants.PRICE_RANGE_MESSAGE)]
        public decimal? IndividualPrice { get; set; } = 35;
        
        [Range(typeof(decimal), "0", "10000", ErrorMessage = EventValidationConstants.PRICE_RANGE_MESSAGE)]
        public decimal? CouplesPrice { get; set; } = 60;
        
        // For sliding scale pricing (from PricingTiers)
        [RequiredForPricingType(nameof(PricingType), PricingType.SlidingScale)]
        [Range(typeof(decimal), "0", "10000", ErrorMessage = EventValidationConstants.PRICE_RANGE_MESSAGE)]
        [ValidSlidingScalePrices(nameof(MinimumPrice), nameof(SuggestedPrice), nameof(MaximumPrice))]
        public decimal? MinimumPrice { get; set; }
        
        [RequiredForPricingType(nameof(PricingType), PricingType.SlidingScale)]
        [Range(typeof(decimal), "0", "10000", ErrorMessage = EventValidationConstants.PRICE_RANGE_MESSAGE)]
        public decimal? SuggestedPrice { get; set; }
        
        [RequiredForPricingType(nameof(PricingType), PricingType.SlidingScale)]
        [Range(typeof(decimal), "0", "10000", ErrorMessage = EventValidationConstants.PRICE_RANGE_MESSAGE)]
        public decimal? MaximumPrice { get; set; }
        
        [Range(EventValidationConstants.REFUND_CUTOFF_MIN, EventValidationConstants.REFUND_CUTOFF_MAX, ErrorMessage = EventValidationConstants.REFUND_CUTOFF_RANGE_MESSAGE)]
        public int RefundCutoffHours { get; set; } = 48;
        
        // Current orders (populated from registrations)
        public List<EventOrderDto> Orders { get; set; } = new();
        
        // Volunteer tickets
        public List<VolunteerTicketDto> VolunteerTickets { get; set; } = new();

        // Emails Tab
        public List<EventEmailTemplateDto> EmailTemplates { get; set; } = new();
        public EventEmailTemplateDto? SelectedEmailTemplate { get; set; }
        
        // For sending custom emails
        public SendEventEmailRequest CustomEmail { get; set; } = new();

        // Volunteers/Staff Tab
        public List<VolunteerTaskDto> VolunteerTasks { get; set; } = new();
        public VolunteerSummaryDto VolunteerSummary { get; set; } = new();
        
        // Helper properties
        public bool IsNewEvent => !Id.HasValue;
        public string EventTypeDisplay => EventType == EventType.Workshop ? "Class" : "Meetup";
        public TimeSpan Duration => EndDate - StartDate;
        
        // Available options for dropdowns
        public List<UserOptionDto> AvailableInstructors { get; set; } = new();
        public List<UserOptionDto> AvailableVolunteers { get; set; } = new();
        
        // RSVPs for social events
        public List<RsvpDto> Rsvps { get; set; } = new();
    }

    /// <summary>
    /// Order information for the Tickets/Orders tab
    /// </summary>
    public class EventOrderDto
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string AttendeeName { get; set; } = string.Empty;
        public string TicketType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal Amount { get; set; }
        public Guid RegistrationId { get; set; }
    }

    /// <summary>
    /// Volunteer ticket information
    /// </summary>
    public class VolunteerTicketDto
    {
        public Guid VolunteerId { get; set; }
        public string VolunteerName { get; set; } = string.Empty;
        public string TaskName { get; set; } = string.Empty;
        public bool HasTicket { get; set; }
        public decimal TicketPrice { get; set; }
        public bool BackgroundCheckVerified { get; set; }
    }

    /// <summary>
    /// User option for dropdowns
    /// </summary>
    public class UserOptionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SceneName { get; set; } = string.Empty;
        public string DisplayName => $"{SceneName} ({Name})";
    }
}