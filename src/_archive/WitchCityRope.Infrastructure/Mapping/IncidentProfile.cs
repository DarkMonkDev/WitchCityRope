using AutoMapper;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Infrastructure.Mapping
{
    public class IncidentProfile : Profile
    {
        public IncidentProfile()
        {
            // Map IncidentReport entity to IncidentReportDto
            CreateMap<IncidentReport, IncidentReportDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ReferenceNumber, opt => opt.MapFrom(src => src.ReferenceNumber))
                .ForMember(dest => dest.ReportedAt, opt => opt.MapFrom(src => src.ReportedAt))
                .ForMember(dest => dest.Severity, opt => opt.MapFrom(src => src.Severity.ToString()))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.IsAnonymous, opt => opt.MapFrom(src => src.IsAnonymous))
                .ForMember(dest => dest.EventName, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : null));

            // Map IncidentReport to IncidentReportResponse
            CreateMap<IncidentReport, IncidentReportResponse>()
                .ForMember(dest => dest.ReportId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ReferenceNumber, opt => opt.MapFrom(src => src.ReferenceNumber))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => GetIncidentMessage(src.Status)));

            // Map IncidentReportRequest to IncidentReport (partial mapping)
            CreateMap<IncidentReportRequest, IncidentReport>()
                .ForMember(dest => dest.IsAnonymous, opt => opt.MapFrom(src => src.IsAnonymous))
                .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.EventId))
                .ForMember(dest => dest.IncidentDate, opt => opt.MapFrom(src => src.IncidentDate))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Severity, opt => opt.MapFrom(src => ParseSeverity(src.Severity)))
                // Note: RequiresImmediateAction and WitnessInformation are in the DTO but not in the entity
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ReporterId, opt => opt.Ignore())
                .ForMember(dest => dest.Reporter, opt => opt.Ignore())
                .ForMember(dest => dest.Event, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.ReportedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ResolvedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ResolutionNotes, opt => opt.Ignore())
                .ForMember(dest => dest.ReferenceNumber, opt => opt.Ignore())
                .ForMember(dest => dest.IncidentType, opt => opt.Ignore())
                .ForMember(dest => dest.Location, opt => opt.Ignore())
                .ForMember(dest => dest.RequestFollowUp, opt => opt.Ignore())
                .ForMember(dest => dest.PreferredContactMethod, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedToId, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedTo, opt => opt.Ignore())
                .ForMember(dest => dest.Reviews, opt => opt.Ignore())
                .ForMember(dest => dest.Actions, opt => opt.Ignore());
        }

        private static string GetIncidentMessage(IncidentStatus status)
        {
            return status switch
            {
                IncidentStatus.Submitted => "Incident report has been submitted and will be reviewed",
                IncidentStatus.Acknowledged => "Incident report has been acknowledged",
                IncidentStatus.UnderInvestigation => "Incident is currently under investigation",
                IncidentStatus.ActionTaken => "Actions have been taken regarding this incident",
                IncidentStatus.Resolved => "Incident has been resolved",
                _ => "Incident report status updated"
            };
        }

        private static IncidentSeverity ParseSeverity(string severity)
        {
            return severity?.ToLower() switch
            {
                "low" => IncidentSeverity.Low,
                "medium" => IncidentSeverity.Medium,
                "high" => IncidentSeverity.High,
                "critical" => IncidentSeverity.Critical,
                _ => IncidentSeverity.Medium // Default
            };
        }
    }
}