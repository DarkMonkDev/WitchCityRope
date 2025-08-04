using AutoMapper;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Infrastructure.Mapping
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            // Map WitchCityRopeUser entity to UserDto
            CreateMap<WitchCityRopeUser, UserDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.SceneName, opt => opt.MapFrom(src => src.SceneNameValue))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? string.Empty))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.DisplayName))
                .ForMember(dest => dest.PronouncedName, opt => opt.MapFrom(src => src.PronouncedName))
                .ForMember(dest => dest.Pronouns, opt => opt.MapFrom(src => src.Pronouns))
                .ForMember(dest => dest.IsVetted, opt => opt.MapFrom(src => src.IsVetted))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.IsAdmin, opt => opt.MapFrom(src => src.Role == UserRole.Administrator))
                .ForMember(dest => dest.EmailVerified, opt => opt.MapFrom(src => false)) // Default to false, needs integration with Identity
                .ForMember(dest => dest.TwoFactorEnabled, opt => opt.MapFrom(src => false)) // Default to false, needs integration with Identity
                .ForMember(dest => dest.IsEmailConfirmed, opt => opt.MapFrom(src => false)) // Default to false, needs integration with Identity
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => new List<string> { src.Role.ToString() }))
                .ForMember(dest => dest.VettingStatus, opt => opt.MapFrom(src => src.IsVetted ? VettingStatus.Approved : VettingStatus.NotStarted))
                .ForMember(dest => dest.LastLoginAt, opt => opt.MapFrom(src => (DateTime?)null)); // Default to null, needs tracking

            // Reverse mapping if needed - WitchCityRopeUser creation should go through domain methods
            CreateMap<UserDto, WitchCityRopeUser>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EncryptedLegalName, opt => opt.Ignore())
                .ForMember(dest => dest.SceneName, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForMember(dest => dest.DateOfBirth, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.DisplayName, opt => opt.Ignore())
                .ForMember(dest => dest.PronouncedName, opt => opt.Ignore())
                .ForMember(dest => dest.Pronouns, opt => opt.Ignore())
                .ForMember(dest => dest.IsVetted, opt => opt.Ignore())
                .ForMember(dest => dest.Registrations, opt => opt.Ignore())
                .ForMember(dest => dest.VettingApplications, opt => opt.Ignore());

            // Map to a more detailed UserDetailsDto if needed
            CreateMap<WitchCityRopeUser, UserDetailsDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.SceneName, opt => opt.MapFrom(src => src.SceneNameValue))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? string.Empty))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.DisplayName))
                .ForMember(dest => dest.PronouncedName, opt => opt.MapFrom(src => src.PronouncedName))
                .ForMember(dest => dest.Pronouns, opt => opt.MapFrom(src => src.Pronouns))
                .ForMember(dest => dest.IsVetted, opt => opt.MapFrom(src => src.IsVetted))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.GetAge()))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
                .ForMember(dest => dest.RegistrationCount, opt => opt.MapFrom(src => src.Registrations != null ? src.Registrations.Count : 0))
                .ForMember(dest => dest.VettingApplicationCount, opt => opt.MapFrom(src => src.VettingApplications != null ? src.VettingApplications.Count : 0))
                .ForMember(dest => dest.IsAdmin, opt => opt.MapFrom(src => src.Role == UserRole.Administrator))
                .ForMember(dest => dest.EmailVerified, opt => opt.MapFrom(src => false)) // Default to false, needs integration with Identity
                .ForMember(dest => dest.TwoFactorEnabled, opt => opt.MapFrom(src => false)) // Default to false, needs integration with Identity
                .ForMember(dest => dest.IsEmailConfirmed, opt => opt.MapFrom(src => false)) // Default to false, needs integration with Identity
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => new List<string> { src.Role.ToString() }))
                .ForMember(dest => dest.VettingStatus, opt => opt.MapFrom(src => src.IsVetted ? VettingStatus.Approved : VettingStatus.NotStarted))
                .ForMember(dest => dest.LastLoginAt, opt => opt.MapFrom(src => (DateTime?)null)); // Default to null, needs tracking
        }
    }

}