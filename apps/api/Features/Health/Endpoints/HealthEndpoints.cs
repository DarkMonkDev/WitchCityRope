using WitchCityRope.Api.Features.Health.Models;
using WitchCityRope.Api.Features.Health.Services;

namespace WitchCityRope.Api.Features.Health.Endpoints;

/// <summary>
/// Health check minimal API endpoints
/// Example of simple vertical slice endpoint registration - NO MediatR complexity
/// </summary>
public static class HealthEndpoints
{
    /// <summary>
    /// Register health check endpoints using minimal API pattern
    /// Shows simple direct service injection pattern
    /// </summary>
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // Basic health check endpoint
        app.MapGet("/api/health", async (
            HealthService healthService,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await healthService.GetHealthAsync(cancellationToken);
                
                return success 
                    ? Results.Ok(response)
                    : Results.Problem(
                        title: "Health Check Failed",
                        detail: error,
                        statusCode: 503);
            })
            .WithName("GetHealth")
            .WithSummary("Get basic API health status")
            .WithDescription("Returns basic health information including database connectivity and user count")
            .WithTags("Health")
            .Produces<HealthResponse>(200)
            .Produces(503);

        // Detailed health check endpoint  
        app.MapGet("/api/health/detailed", async (
            HealthService healthService,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await healthService.GetDetailedHealthAsync(cancellationToken);
                
                return success
                    ? Results.Ok(response)
                    : Results.Problem(
                        title: "Detailed Health Check Failed", 
                        detail: error,
                        statusCode: 503);
            })
            .WithName("GetDetailedHealth")
            .WithSummary("Get detailed API health information")
            .WithDescription("Returns comprehensive health metrics including database version and active user counts")
            .WithTags("Health")
            .Produces<DetailedHealthResponse>(200)
            .Produces(503);

        // Legacy health endpoint compatibility (existing /health endpoint)
        app.MapGet("/health", async (
            HealthService healthService,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await healthService.GetHealthAsync(cancellationToken);
                
                // Return simple status for legacy health checks
                return success 
                    ? Results.Ok(new { status = "Healthy" })
                    : Results.Problem(
                        title: "Health Check Failed",
                        detail: error,
                        statusCode: 503);
            })
            .WithName("GetLegacyHealth")
            .WithSummary("Legacy health check endpoint")
            .WithDescription("Simple health check for compatibility with existing monitoring")
            .WithTags("Health")
            .Produces(200)
            .Produces(503);
    }
}