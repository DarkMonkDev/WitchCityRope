using AutoMapper;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Mapping
{
    public class CheckInProfile : Profile
    {
        public CheckInProfile()
        {
            // Map Registration to CheckInResponse
            CreateMap<Registration, CheckInResponse>()
                .ForMember(dest => dest.RegistrationId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.AttendeeName, opt => opt.MapFrom(src => src.User.DisplayName))
                .ForMember(dest => dest.Success, opt => opt.MapFrom(src => src.CheckedInAt.HasValue))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.CheckedInAt.HasValue 
                    ? "Check-in successful" 
                    : "Check-in pending"))
                .ForMember(dest => dest.CheckInTime, opt => opt.MapFrom(src => src.CheckedInAt ?? DateTime.UtcNow));

            // Map CheckInRequest to partial registration updates (if needed)
            // Check-in is handled through domain methods, so all properties are ignored
            CreateMap<CheckInRequest, Registration>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.EventId, opt => opt.Ignore())
                .ForMember(dest => dest.Event, opt => opt.Ignore())
                .ForMember(dest => dest.SelectedPrice, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.DietaryRestrictions, opt => opt.Ignore())
                .ForMember(dest => dest.AccessibilityNeeds, opt => opt.Ignore())
                .ForMember(dest => dest.EmergencyContactName, opt => opt.Ignore())
                .ForMember(dest => dest.EmergencyContactPhone, opt => opt.Ignore())
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
        }
    }
}