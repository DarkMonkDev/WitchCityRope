using WitchCityRope.Api.Features.Cms.Services;

namespace WitchCityRope.Api.Features.Cms
{
    /// <summary>
    /// Extension methods for registering CMS services
    /// </summary>
    public static class CmsServiceExtensions
    {
        /// <summary>
        /// Registers CMS services in the dependency injection container
        /// </summary>
        public static IServiceCollection AddCmsServices(this IServiceCollection services)
        {
            // Register ContentSanitizer as singleton (stateless, thread-safe)
            services.AddSingleton<ContentSanitizer>();

            return services;
        }
    }
}
