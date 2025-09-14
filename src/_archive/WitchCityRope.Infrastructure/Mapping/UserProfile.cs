using AutoMapper;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Mapping
{
    // Basic UserDto for mapping purposes
    public class UserDto
    {
        public Guid Id { get; set; }
        public string SceneName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string PronouncedName { get; set; } = string.Empty;
        public string Pronouns { get; set; } = string.Empty;
        public bool IsVetted { get; set; }
        public bool IsActive { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class UserProfile : Profile
    {
        public UserProfile()
        {
            // Map User entity to UserDto
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.SceneName, opt => opt.MapFrom(src => src.SceneName.Value))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.DisplayName))
                .ForMember(dest => dest.PronouncedName, opt => opt.MapFrom(src => src.PronouncedName))
                .ForMember(dest => dest.Pronouns, opt => opt.MapFrom(src => src.Pronouns))
                .ForMember(dest => dest.IsVetted, opt => opt.MapFrom(src => src.IsVetted))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));

            // Reverse mapping if needed - User creation should go through domain methods
            CreateMap<UserDto, User>()
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
            CreateMap<User, UserDetailsDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.SceneName, opt => opt.MapFrom(src => src.SceneName.Value))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.DisplayName))
                .ForMember(dest => dest.PronouncedName, opt => opt.MapFrom(src => src.PronouncedName))
                .ForMember(dest => dest.Pronouns, opt => opt.MapFrom(src => src.Pronouns))
                .ForMember(dest => dest.IsVetted, opt => opt.MapFrom(src => src.IsVetted))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.GetAge()))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
                .ForMember(dest => dest.RegistrationCount, opt => opt.MapFrom(src => src.Registrations.Count))
                .ForMember(dest => dest.VettingApplicationCount, opt => opt.MapFrom(src => src.VettingApplications.Count));
        }
    }

    // Define UserDetailsDto if it doesn't exist
    public class UserDetailsDto
    {
        public Guid Id { get; set; }
        public string SceneName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string PronouncedName { get; set; } = string.Empty;
        public string Pronouns { get; set; } = string.Empty;
        public bool IsVetted { get; set; }
        public bool IsActive { get; set; }
        public string Role { get; set; } = string.Empty;
        public int Age { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int RegistrationCount { get; set; }
        public int VettingApplicationCount { get; set; }
    }
}