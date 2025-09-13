using FluentValidation;
using WitchCityRope.Api.Features.CheckIn.Models;
using WitchCityRope.Api.Features.CheckIn.Services;
using WitchCityRope.Api.Features.CheckIn.Validation;

namespace WitchCityRope.Api.Features.CheckIn.Extensions;

/// <summary>
/// Service registration extensions for CheckIn feature
/// </summary>
public static class CheckInServiceExtensions
{
    /// <summary>
    /// Register CheckIn feature services
    /// </summary>
    public static IServiceCollection AddCheckInServices(this IServiceCollection services)
    {
        // Register services
        services.AddScoped<ICheckInService, CheckInService>();
        services.AddScoped<ISyncService, SyncService>();

        // Register validators
        services.AddScoped<IValidator<CheckInRequest>, CheckInRequestValidator>();
        services.AddScoped<IValidator<ManualEntryData>, ManualEntryDataValidator>();
        services.AddScoped<IValidator<SyncRequest>, SyncRequestValidator>();
        services.AddScoped<IValidator<PendingCheckIn>, PendingCheckInValidator>();

        return services;
    }
}