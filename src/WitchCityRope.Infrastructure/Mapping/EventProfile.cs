using AutoMapper;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using System.Linq;

namespace WitchCityRope.Infrastructure.Mapping
{
    public class EventProfile : Profile
    {
        public EventProfile()
        {
            CreateMap<Event, EventDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.StartDateTime, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDateTime, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.MaxAttendees, opt => opt.MapFrom(src => src.Capacity))
                .ForMember(dest => dest.CurrentAttendees, opt => opt.MapFrom(src => src.GetConfirmedRegistrationCount()))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.PricingTiers.FirstOrDefault() != null ? src.PricingTiers.First().Amount : 0))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.IsPublished ? "Published" : "Draft"))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => new List<string>())) // Tags not in entity
                .ForMember(dest => dest.RequiredSkillLevels, opt => opt.MapFrom(src => new List<string>())) // Skills not in entity
                .ForMember(dest => dest.RequiresVetting, opt => opt.MapFrom(src => src.EventType == EventType.PlayParty || src.EventType == EventType.Conference));

            // Reverse mapping if needed
            CreateMap<EventDto, Event>()
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDateTime))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDateTime))
                .ForMember(dest => dest.Capacity, opt => opt.MapFrom(src => src.MaxAttendees))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Description, opt => opt.Ignore())
                .ForMember(dest => dest.Location, opt => opt.Ignore())
                .ForMember(dest => dest.EventType, opt => opt.Ignore())
                .ForMember(dest => dest.IsPublished, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Registrations, opt => opt.Ignore())
                .ForMember(dest => dest.Organizers, opt => opt.Ignore())
                .ForMember(dest => dest.PricingTiers, opt => opt.Ignore());
        }
    }
}