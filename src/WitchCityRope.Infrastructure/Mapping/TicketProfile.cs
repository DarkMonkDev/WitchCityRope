using AutoMapper;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Infrastructure.Mapping
{
    public class TicketProfile : Profile
    {
        public TicketProfile()
        {
            // Map Ticket entity to EventTicketResponse DTO
            CreateMap<Ticket, EventTicketResponse>()
                .ForMember(dest => dest.TicketId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ConfirmationCode, opt => opt.MapFrom(src => src.ConfirmationCode))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => GetTicketMessage(src.Status)));

            // Map EventTicketRequest to Ticket (partial mapping)
            CreateMap<EventTicketRequest, Ticket>()
                .ForMember(dest => dest.DietaryRestrictions, opt => opt.MapFrom(src => src.DietaryRestrictions))
                .ForMember(dest => dest.AccessibilityNeeds, opt => opt.MapFrom(src => src.AccessibilityNeeds))
                .ForMember(dest => dest.EmergencyContactName, opt => opt.MapFrom(src => src.EmergencyContactName))
                .ForMember(dest => dest.EmergencyContactPhone, opt => opt.MapFrom(src => src.EmergencyContactPhone))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                // User navigation property removed
                .ForMember(dest => dest.EventId, opt => opt.Ignore())
                .ForMember(dest => dest.Event, opt => opt.Ignore())
                .ForMember(dest => dest.SelectedPrice, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.PurchasedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ConfirmedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CancelledAt, opt => opt.Ignore())
                .ForMember(dest => dest.CancellationReason, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Payment, opt => opt.Ignore())
                .ForMember(dest => dest.CheckedInAt, opt => opt.Ignore())
                .ForMember(dest => dest.CheckedInBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ConfirmationCode, opt => opt.Ignore());

            // Map Ticket to a generic TicketDto if needed
            CreateMap<Ticket, TicketDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.EventId))
                .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : string.Empty))
                .ForMember(dest => dest.EventDate, opt => opt.MapFrom(src => src.Event != null ? src.Event.StartDate : DateTime.MinValue))
                .ForMember(dest => dest.EventLocation, opt => opt.MapFrom(src => src.Event != null ? src.Event.Location : string.Empty))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.PurchasedAt, opt => opt.MapFrom(src => src.PurchasedAt))
                .ForMember(dest => dest.ConfirmationCode, opt => opt.MapFrom(src => src.ConfirmationCode))
                .ForMember(dest => dest.AmountPaid, opt => opt.MapFrom(src => src.SelectedPrice.Amount))
                .ForMember(dest => dest.CheckedInAt, opt => opt.MapFrom(src => src.CheckedInAt));
        }

        private static string GetTicketMessage(TicketStatus status)
        {
            return status switch
            {
                TicketStatus.Pending => "Ticket is pending payment",
                TicketStatus.Confirmed => "Ticket confirmed successfully",
                TicketStatus.Cancelled => "Ticket has been cancelled",
                TicketStatus.CheckedIn => "Attendee has checked in",
                _ => "Unknown status"
            };
        }
    }
}