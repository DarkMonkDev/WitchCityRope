using WitchCityRope.Api.Features.Metadata.Models;
using WitchCityRope.Api.Features.Users.Constants;

namespace WitchCityRope.Api.Features.Metadata.Endpoints;

/// <summary>
/// Metadata API endpoints providing system configuration information.
/// These endpoints expose reference data used by the frontend for validation and UI.
/// </summary>
public static class MetadataEndpoints
{
    /// <summary>
    /// Register metadata endpoints using minimal API pattern.
    /// </summary>
    public static void MapMetadataEndpoints(this IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Get valid user roles in the system.
        /// Returns the list of valid role names that can be assigned to users.
        /// Empty string or null role represents a regular member with no special privileges.
        /// </summary>
        /// <response code="200">Returns the list of valid roles</response>
        /// <remarks>
        /// This endpoint is public (no authorization required) as it exposes metadata
        /// needed for frontend validation and UI rendering.
        ///
        /// Valid roles:
        /// - Teacher: Can create and teach events/classes
        /// - SafetyTeam: Part of the safety coordination team
        /// - Administrator: Full administrative access
        /// - Empty/null: Regular member with no special privileges
        /// </remarks>
        app.MapGet("/api/metadata/valid-roles", () =>
            {
                var response = new ValidRolesResponse
                {
                    Roles = UserRoleConstants.ValidRoles.ToList()
                };

                return Results.Ok(response);
            })
            .WithName("GetValidRoles")
            .WithSummary("Get valid user roles")
            .WithDescription("Returns the list of valid role names that can be assigned to users in the system")
            .WithTags("Metadata")
            .Produces<ValidRolesResponse>(200)
            .AllowAnonymous(); // Public metadata endpoint
    }
}
