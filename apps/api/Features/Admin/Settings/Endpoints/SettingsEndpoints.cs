using Microsoft.AspNetCore.Authorization;
using WitchCityRope.Api.Features.Admin.Settings.Models;
using WitchCityRope.Api.Features.Admin.Settings.Interfaces;
using WitchCityRope.Api.Features.Users.Constants;

namespace WitchCityRope.Api.Features.Admin.Settings.Endpoints;

/// <summary>
/// Admin-only endpoints for managing application settings
/// </summary>
public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        // Get all settings (admin only)
        app.MapGet("/api/admin/settings", async (
            ISettingsService settingsService,
            CancellationToken cancellationToken) =>
        {
            var settings = await settingsService.GetAllSettingsAsync(cancellationToken);
            return Results.Ok(settings);
        })
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
        .WithName("GetAdminSettings")
        .WithSummary("Get all application settings")
        .WithDescription("Returns all configurable settings (admin only)")
        .WithTags("Settings")
        .Produces<Dictionary<string, string>>(200)
        .Produces(401)
        .Produces(403);

        // Update settings (admin only)
        app.MapPut("/api/admin/settings", async (
            UpdateSettingsRequest request,
            ISettingsService settingsService,
            CancellationToken cancellationToken) =>
        {
            // Validate timezone if provided
            if (request.Settings.ContainsKey("EventTimeZone"))
            {
                var timeZoneId = request.Settings["EventTimeZone"];
                try
                {
                    TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                }
                catch (TimeZoneNotFoundException)
                {
                    return Results.Problem(
                        title: "Invalid Timezone",
                        detail: $"Timezone '{timeZoneId}' is not valid",
                        statusCode: 400);
                }
            }

            // Validate buffer minutes if provided
            if (request.Settings.ContainsKey("PreStartBufferMinutes"))
            {
                var bufferValue = request.Settings["PreStartBufferMinutes"];
                if (!int.TryParse(bufferValue, out int bufferMinutes) || bufferMinutes < 0)
                {
                    return Results.Problem(
                        title: "Invalid Buffer Minutes",
                        detail: "PreStartBufferMinutes must be a non-negative integer",
                        statusCode: 400);
                }
            }

            var (success, error) = await settingsService.UpdateMultipleSettingsAsync(
                request.Settings,
                cancellationToken);

            if (!success)
            {
                return Results.Problem(
                    title: "Update Failed",
                    detail: error,
                    statusCode: 500);
            }

            return Results.Ok(new { Message = "Settings updated successfully" });
        })
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
        .WithName("UpdateAdminSettings")
        .WithSummary("Update application settings")
        .WithDescription("Updates one or more application settings (admin only)")
        .WithTags("Settings")
        .Produces<object>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .Produces(500);
    }
}
