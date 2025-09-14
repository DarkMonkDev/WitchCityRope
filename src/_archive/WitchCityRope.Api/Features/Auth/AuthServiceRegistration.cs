using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Api.Features.Auth.Services;

namespace WitchCityRope.Api.Features.Auth
{
    /// <summary>
    /// Extension methods for registering authentication services
    /// </summary>
    public static class AuthServiceRegistration
    {
        public static IServiceCollection AddAuthFeature(this IServiceCollection services)
        {
            // Register the AuthService
            services.AddScoped<AuthService>();
            
            // Note: The following dependencies should be registered in the Infrastructure layer:
            // - IUserRepository
            // - IPasswordHasher
            // - IJwtService
            // - IEmailService
            // - IEncryptionService
            
            return services;
        }
    }
}