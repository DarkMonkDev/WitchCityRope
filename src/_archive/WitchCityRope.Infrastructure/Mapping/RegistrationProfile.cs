using AutoMapper;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Infrastructure.Mapping
{
    public class RegistrationProfile : Profile
    {
        public RegistrationProfile()
        {
            // Map Registration entity to EventRegistrationResponse DTO
            CreateMap<Registration, EventRegistrationResponse>()
                .ForMember(dest => dest.RegistrationId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ConfirmationCode, opt => opt.MapFrom(src => src.ConfirmationCode))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => GetRegistrationMessage(src.Status)));

            // Map EventRegistrationRequest to Registration (partial mapping)
            CreateMap<EventRegistrationRequest, Registration>()
                .ForMember(dest => dest.DietaryRestrictions, opt => opt.MapFrom(src => src.DietaryRestrictions))
                .ForMember(dest => dest.AccessibilityNeeds, opt => opt.MapFrom(src => src.AccessibilityNeeds))
                .ForMember(dest => dest.EmergencyContactName, opt => opt.MapFrom(src => src.EmergencyContactName))
                .ForMember(dest => dest.EmergencyContactPhone, opt => opt.MapFrom(src => src.EmergencyContactPhone))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.EventId, opt => opt.Ignore())
                .ForMember(dest => dest.Event, opt => opt.Ignore())
                .ForMember(dest => dest.SelectedPrice, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.RegisteredAt, opt => opt.Ignore())
                .ForMember(dest => dest.ConfirmedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CancelledAt, opt => opt.Ignore())
                .ForMember(dest => dest.CancellationReason, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Payment, opt => opt.Ignore())
                .ForMember(dest => dest.CheckedInAt, opt => opt.Ignore())
                .ForMember(dest => dest.CheckedInBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ConfirmationCode, opt => opt.Ignore());

            // Map Registration to a generic RegistrationDto if needed
            CreateMap<Registration, RegistrationDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.EventId))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.SelectedPrice, opt => opt.MapFrom(src => src.SelectedPrice.Amount))
                .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.SelectedPrice.Currency))
                .ForMember(dest => dest.ConfirmationCode, opt => opt.MapFrom(src => src.ConfirmationCode))
                .ForMember(dest => dest.RegisteredAt, opt => opt.MapFrom(src => src.RegisteredAt))
                .ForMember(dest => dest.ConfirmedAt, opt => opt.MapFrom(src => src.ConfirmedAt))
                .ForMember(dest => dest.CheckedInAt, opt => opt.MapFrom(src => src.CheckedInAt))
                .ForMember(dest => dest.DietaryRestrictions, opt => opt.MapFrom(src => src.DietaryRestrictions))
                .ForMember(dest => dest.AccessibilityNeeds, opt => opt.MapFrom(src => src.AccessibilityNeeds))
                .ForMember(dest => dest.EmergencyContactName, opt => opt.MapFrom(src => src.EmergencyContactName))
                .ForMember(dest => dest.EmergencyContactPhone, opt => opt.MapFrom(src => src.EmergencyContactPhone));
        }

        private static string GetRegistrationMessage(RegistrationStatus status)
        {
            return status switch
            {
                RegistrationStatus.Pending => "Registration is pending payment",
                RegistrationStatus.Confirmed => "Registration confirmed successfully",
                RegistrationStatus.Cancelled => "Registration has been cancelled",
                RegistrationStatus.CheckedIn => "Attendee has checked in",
                _ => "Unknown status"
            };
        }
    }

    // Define the RegistrationDto if it doesn't exist
    public class RegistrationDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid EventId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal SelectedPrice { get; set; }
        public string Currency { get; set; } = "USD";
        public string ConfirmationCode { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? CheckedInAt { get; set; }
        public string? DietaryRestrictions { get; set; }
        public string? AccessibilityNeeds { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
    }
}