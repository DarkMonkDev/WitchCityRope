using AutoMapper;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Mapping
{
    public class VettingProfile : Profile
    {
        public VettingProfile()
        {
            // Map VettingApplication entity to VettingApplicationDto
            CreateMap<VettingApplication, VettingApplicationDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ApplicantId, opt => opt.MapFrom(src => src.ApplicantId))
                .ForMember(dest => dest.ApplicantName, opt => opt.MapFrom(src => src.Applicant != null ? src.Applicant.DisplayName : string.Empty))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.SubmittedAt, opt => opt.MapFrom(src => src.SubmittedAt))
                .ForMember(dest => dest.ReviewerNotes, opt => opt.MapFrom(src => src.DecisionNotes));

            // Map VettingApplication to VettingApplicationResponse
            CreateMap<VettingApplication, VettingApplicationResponse>()
                .ForMember(dest => dest.ApplicationId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => GetApplicationMessage(src.Status)));

            // Map VettingApplicationRequest to VettingApplication (partial mapping)
            // Note: VettingApplication entity has different properties than the DTO
            // This mapping is for reference only as entity creation should go through domain constructors
            CreateMap<VettingApplicationRequest, VettingApplication>()
                .ForMember(dest => dest.ExperienceDescription, opt => opt.MapFrom(src => src.Experience))
                .ForMember(dest => dest.WhyJoin, opt => opt.MapFrom(src => src.WhyJoin))
                // References would need to be formatted from the request properties
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ApplicantId, opt => opt.Ignore())
                .ForMember(dest => dest.Applicant, opt => opt.Ignore())
                .ForMember(dest => dest.ExperienceLevel, opt => opt.Ignore())
                .ForMember(dest => dest.Interests, opt => opt.Ignore())
                .ForMember(dest => dest.SafetyKnowledge, opt => opt.Ignore())
                .ForMember(dest => dest.ConsentUnderstanding, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.SubmittedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ReviewedAt, opt => opt.Ignore())
                .ForMember(dest => dest.DecisionNotes, opt => opt.Ignore())
                .ForMember(dest => dest.References, opt => opt.Ignore())
                .ForMember(dest => dest.Reviews, opt => opt.Ignore());

            // Map review request/response
            CreateMap<VettingApplication, ReviewApplicationResponse>()
                .ForMember(dest => dest.ApplicationId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => GetReviewMessage(src.Status)));
        }

        private static string GetApplicationMessage(Core.Entities.VettingStatus status)
        {
            return status switch
            {
                Core.Entities.VettingStatus.Submitted => "Application submitted successfully and is pending review",
                Core.Entities.VettingStatus.UnderReview => "Application is currently under review",
                Core.Entities.VettingStatus.Approved => "Application has been approved",
                Core.Entities.VettingStatus.Rejected => "Application has been rejected",
                Core.Entities.VettingStatus.MoreInfoRequested => "Additional information is required for your application",
                _ => "Unknown status"
            };
        }

        private static string GetReviewMessage(Core.Entities.VettingStatus status)
        {
            return status switch
            {
                Core.Entities.VettingStatus.Approved => "Application approved successfully",
                Core.Entities.VettingStatus.Rejected => "Application rejected",
                Core.Entities.VettingStatus.MoreInfoRequested => "Additional information requested from applicant",
                _ => "Review status updated"
            };
        }
    }
}