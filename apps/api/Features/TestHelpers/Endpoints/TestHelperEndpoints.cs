using WitchCityRope.Api.Features.TestHelpers.Models;
using WitchCityRope.Api.Features.TestHelpers.Services;

namespace WitchCityRope.Api.Features.TestHelpers.Endpoints;

/// <summary>
/// Test helper endpoints for E2E testing
/// CRITICAL: Only available in Development/Test environments
/// </summary>
public static class TestHelperEndpoints
{
    /// <summary>
    /// Register test helper endpoints
    /// ONLY registers if environment is Development or Test
    /// </summary>
    public static void MapTestHelperEndpoints(this IEndpointRouteBuilder app)
    {
        // SECURITY: Only enable test helpers in Development/Test environments
        var environment = app.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        if (!environment.IsDevelopment() && environment.EnvironmentName != "Test")
        {
            // Skip registration in production-like environments
            return;
        }

        var logger = app.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("⚠️ Test Helper endpoints are ENABLED - Development/Test environment detected");

        // Create test user endpoint
        app.MapPost("/api/test-helpers/users", async (
            CreateTestUserRequest request,
            ITestHelperService testHelperService,
            CancellationToken cancellationToken) =>
            {
                var (success, data, error) = await testHelperService.CreateTestUserAsync(request, cancellationToken);

                if (success && data != null)
                {
                    return Results.Created($"/api/test-helpers/users/{data.Id}", new
                    {
                        Success = true,
                        Data = data,
                        Message = "Test user created successfully"
                    });
                }

                return Results.BadRequest(new
                {
                    Success = false,
                    Error = error,
                    Message = "Failed to create test user"
                });
            })
            .AllowAnonymous() // No auth required for test user creation
            .WithName("CreateTestUser")
            .WithSummary("Create test user for E2E testing")
            .WithDescription("Programmatically create a user with specific properties for testing. ONLY available in Development/Test.")
            .WithTags("Testing", "TestHelpers")
            .Produces<object>(201)
            .Produces<object>(400);

        // Delete test user endpoint
        app.MapDelete("/api/test-helpers/users/{userId}", async (
            string userId,
            ITestHelperService testHelperService,
            CancellationToken cancellationToken) =>
            {
                var (success, error) = await testHelperService.DeleteTestUserAsync(userId, cancellationToken);

                if (success)
                {
                    return Results.Ok(new
                    {
                        Success = true,
                        Message = "Test user deleted successfully"
                    });
                }

                return Results.BadRequest(new
                {
                    Success = false,
                    Error = error,
                    Message = "Failed to delete test user"
                });
            })
            .AllowAnonymous() // No auth required for test cleanup
            .WithName("DeleteTestUser")
            .WithSummary("Delete test user for cleanup")
            .WithDescription("Delete a test user by ID. Used in afterEach/afterAll hooks. ONLY available in Development/Test.")
            .WithTags("Testing", "TestHelpers")
            .Produces<object>(200)
            .Produces<object>(400);

        // Health check endpoint to verify test helpers are available
        app.MapGet("/api/test-helpers/health", () =>
            {
                return Results.Ok(new
                {
                    Success = true,
                    Message = "Test helpers are available",
                    Environment = environment.EnvironmentName,
                    Timestamp = DateTime.UtcNow
                });
            })
            .AllowAnonymous()
            .WithName("TestHelpersHealth")
            .WithSummary("Check if test helpers are available")
            .WithDescription("Returns 200 if test helper endpoints are enabled (Development/Test only)")
            .WithTags("Testing", "TestHelpers")
            .Produces<object>(200);
    }
}
