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
                    return Results.Created($"/api/test-helpers/users/{data.Id}", data);
                }

                return Results.Problem(
                    title: "Failed to create test user",
                    detail: error,
                    statusCode: 400);
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
                    return Results.NoContent();
                }

                return Results.Problem(
                    title: "Failed to delete test user",
                    detail: error,
                    statusCode: 400);
            })
            .AllowAnonymous() // No auth required for test cleanup
            .WithName("DeleteTestUser")
            .WithSummary("Delete test user for cleanup")
            .WithDescription("Delete a test user by ID. Used in afterEach/afterAll hooks. ONLY available in Development/Test.")
            .WithTags("Testing", "TestHelpers")
            .Produces<object>(200)
            .Produces<object>(400);

        // Create test ticket purchase endpoint
        app.MapPost("/api/test-helpers/ticket-purchases", async (
            CreateTestTicketPurchaseRequest request,
            ITestHelperService testHelperService,
            CancellationToken cancellationToken) =>
            {
                var (success, data, error) = await testHelperService.CreateTestTicketPurchaseAsync(request, cancellationToken);

                if (success && data != null)
                {
                    return Results.Created($"/api/test-helpers/ticket-purchases/{data.Id}", data);
                }

                return Results.Problem(
                    title: "Failed to create test ticket purchase",
                    detail: error,
                    statusCode: 400);
            })
            .AllowAnonymous() // No auth required for test data creation
            .WithName("CreateTestTicketPurchase")
            .WithSummary("Create test ticket purchase for E2E testing")
            .WithDescription("Programmatically create a ticket purchase with specific properties for testing. Bypasses payment flow. ONLY available in Development/Test.")
            .WithTags("Testing", "TestHelpers")
            .Produces<object>(201)
            .Produces<object>(400);

        // Delete test ticket purchase endpoint
        app.MapDelete("/api/test-helpers/ticket-purchases/{ticketPurchaseId:guid}", async (
            Guid ticketPurchaseId,
            ITestHelperService testHelperService,
            CancellationToken cancellationToken) =>
            {
                var (success, error) = await testHelperService.DeleteTestTicketPurchaseAsync(ticketPurchaseId, cancellationToken);

                if (success)
                {
                    return Results.NoContent();
                }

                return Results.Problem(
                    title: "Failed to delete test ticket purchase",
                    detail: error,
                    statusCode: 400);
            })
            .AllowAnonymous() // No auth required for test cleanup
            .WithName("DeleteTestTicketPurchase")
            .WithSummary("Delete test ticket purchase for cleanup")
            .WithDescription("Delete a test ticket purchase by ID. Used in afterEach/afterAll hooks. ONLY available in Development/Test.")
            .WithTags("Testing", "TestHelpers")
            .Produces<object>(204)
            .Produces<object>(400);

        // Verify user email endpoint
        app.MapPost("/api/test-helpers/verify-email", async (
            VerifyUserEmailRequest request,
            ITestHelperService testHelperService,
            CancellationToken cancellationToken) =>
            {
                var (success, error) = await testHelperService.VerifyUserEmailAsync(request.Email, cancellationToken);

                if (success)
                {
                    return Results.Ok(new
                    {
                        Success = true,
                        Message = "Email verified successfully"
                    });
                }

                return Results.Problem(
                    title: "Failed to verify email",
                    detail: error,
                    statusCode: 400);
            })
            .AllowAnonymous() // No auth required for test email verification
            .WithName("VerifyUserEmail")
            .WithSummary("Verify user email for E2E testing")
            .WithDescription("Programmatically verify a user's email address for testing. Bypasses email confirmation flow. ONLY available in Development/Test.")
            .WithTags("Testing", "TestHelpers")
            .Produces<object>(200)
            .Produces<object>(400);

        // Health check endpoint to verify test helpers are available
        app.MapGet("/api/test-helpers/health", () =>
            {
                return Results.Ok(new
                {
                    Message = "Test helpers are available",
                    Environment = environment.EnvironmentName
                });
            })
            .AllowAnonymous()
            .WithName("TestHelpersHealth")
            .WithSummary("Check if test helpers are available")
            .WithDescription("Returns 200 if test helper endpoints are enabled (Development/Test only)")
            .WithTags("Testing", "TestHelpers")
            .Produces<object>(200);

        // ====================================================================
        // EVENT ENDPOINTS
        // ====================================================================

        // Create test event endpoint
        app.MapPost("/api/test-helpers/events", async (
            CreateTestEventRequest request,
            ITestHelperService testHelperService,
            CancellationToken cancellationToken) =>
            {
                var (success, data, error) = await testHelperService.CreateTestEventAsync(request, cancellationToken);

                if (success && data != null)
                {
                    return Results.Created($"/api/test-helpers/events/{data.Id}", data);
                }

                return Results.Problem(
                    title: "Failed to create test event",
                    detail: error,
                    statusCode: 400);
            })
            .AllowAnonymous()
            .WithName("CreateTestEvent")
            .WithSummary("Create test event for E2E testing")
            .WithDescription("Programmatically create an event with specific properties for testing. ONLY available in Development/Test.")
            .WithTags("Testing", "TestHelpers")
            .Produces<object>(201)
            .Produces<object>(400);

        // Delete test event endpoint
        app.MapDelete("/api/test-helpers/events/{eventId:guid}", async (
            Guid eventId,
            ITestHelperService testHelperService,
            CancellationToken cancellationToken) =>
            {
                var (success, error) = await testHelperService.DeleteTestEventAsync(eventId, cancellationToken);

                if (success)
                {
                    return Results.NoContent();
                }

                return Results.Problem(
                    title: "Failed to delete test event",
                    detail: error,
                    statusCode: 400);
            })
            .AllowAnonymous()
            .WithName("DeleteTestEvent")
            .WithSummary("Delete test event for cleanup")
            .WithDescription("Delete a test event by ID. Also deletes related sessions and ticket types. ONLY available in Development/Test.")
            .WithTags("Testing", "TestHelpers")
            .Produces(204)
            .Produces<object>(400);

        // ====================================================================
        // SESSION ENDPOINTS
        // ====================================================================

        // Create test session endpoint
        app.MapPost("/api/test-helpers/sessions", async (
            CreateTestSessionRequest request,
            ITestHelperService testHelperService,
            CancellationToken cancellationToken) =>
            {
                var (success, data, error) = await testHelperService.CreateTestSessionAsync(request, cancellationToken);

                if (success && data != null)
                {
                    return Results.Created($"/api/test-helpers/sessions/{data.Id}", data);
                }

                return Results.Problem(
                    title: "Failed to create test session",
                    detail: error,
                    statusCode: 400);
            })
            .AllowAnonymous()
            .WithName("CreateTestSession")
            .WithSummary("Create test session for E2E testing")
            .WithDescription("Programmatically create a session with specific properties for testing. ONLY available in Development/Test.")
            .WithTags("Testing", "TestHelpers")
            .Produces<object>(201)
            .Produces<object>(400);

        // Delete test session endpoint
        app.MapDelete("/api/test-helpers/sessions/{sessionId:guid}", async (
            Guid sessionId,
            ITestHelperService testHelperService,
            CancellationToken cancellationToken) =>
            {
                var (success, error) = await testHelperService.DeleteTestSessionAsync(sessionId, cancellationToken);

                if (success)
                {
                    return Results.NoContent();
                }

                return Results.Problem(
                    title: "Failed to delete test session",
                    detail: error,
                    statusCode: 400);
            })
            .AllowAnonymous()
            .WithName("DeleteTestSession")
            .WithSummary("Delete test session for cleanup")
            .WithDescription("Delete a test session by ID. ONLY available in Development/Test.")
            .WithTags("Testing", "TestHelpers")
            .Produces(204)
            .Produces<object>(400);

        // ====================================================================
        // TICKET TYPE ENDPOINTS
        // ====================================================================

        // Create test ticket type endpoint
        app.MapPost("/api/test-helpers/ticket-types", async (
            CreateTestTicketTypeRequest request,
            ITestHelperService testHelperService,
            CancellationToken cancellationToken) =>
            {
                var (success, data, error) = await testHelperService.CreateTestTicketTypeAsync(request, cancellationToken);

                if (success && data != null)
                {
                    return Results.Created($"/api/test-helpers/ticket-types/{data.Id}", data);
                }

                return Results.Problem(
                    title: "Failed to create test ticket type",
                    detail: error,
                    statusCode: 400);
            })
            .AllowAnonymous()
            .WithName("CreateTestTicketType")
            .WithSummary("Create test ticket type for E2E testing")
            .WithDescription("Programmatically create a ticket type with specific properties for testing. ONLY available in Development/Test.")
            .WithTags("Testing", "TestHelpers")
            .Produces<object>(201)
            .Produces<object>(400);

        // Delete test ticket type endpoint
        app.MapDelete("/api/test-helpers/ticket-types/{ticketTypeId:guid}", async (
            Guid ticketTypeId,
            ITestHelperService testHelperService,
            CancellationToken cancellationToken) =>
            {
                var (success, error) = await testHelperService.DeleteTestTicketTypeAsync(ticketTypeId, cancellationToken);

                if (success)
                {
                    return Results.NoContent();
                }

                return Results.Problem(
                    title: "Failed to delete test ticket type",
                    detail: error,
                    statusCode: 400);
            })
            .AllowAnonymous()
            .WithName("DeleteTestTicketType")
            .WithSummary("Delete test ticket type for cleanup")
            .WithDescription("Delete a test ticket type by ID. ONLY available in Development/Test.")
            .WithTags("Testing", "TestHelpers")
            .Produces(204)
            .Produces<object>(400);

        // ====================================================================
        // VOLUNTEER POSITION ENDPOINTS
        // ====================================================================

        // Create test volunteer position endpoint
        app.MapPost("/api/test-helpers/volunteer-positions", async (
            CreateTestVolunteerPositionRequest request,
            ITestHelperService testHelperService,
            CancellationToken cancellationToken) =>
            {
                var (success, data, error) = await testHelperService.CreateTestVolunteerPositionAsync(request, cancellationToken);

                if (success && data != null)
                {
                    return Results.Created($"/api/test-helpers/volunteer-positions/{data.Id}", data);
                }

                return Results.Problem(
                    title: "Failed to create test volunteer position",
                    detail: error,
                    statusCode: 400);
            })
            .AllowAnonymous()
            .WithName("CreateTestVolunteerPosition")
            .WithSummary("Create test volunteer position for E2E testing")
            .WithDescription("Programmatically create a volunteer position with specific properties for testing. ONLY available in Development/Test.")
            .WithTags("Testing", "TestHelpers")
            .Produces<object>(201)
            .Produces<object>(400);

        // Delete test volunteer position endpoint
        app.MapDelete("/api/test-helpers/volunteer-positions/{positionId:guid}", async (
            Guid positionId,
            ITestHelperService testHelperService,
            CancellationToken cancellationToken) =>
            {
                var (success, error) = await testHelperService.DeleteTestVolunteerPositionAsync(positionId, cancellationToken);

                if (success)
                {
                    return Results.NoContent();
                }

                return Results.Problem(
                    title: "Failed to delete test volunteer position",
                    detail: error,
                    statusCode: 400);
            })
            .AllowAnonymous()
            .WithName("DeleteTestVolunteerPosition")
            .WithSummary("Delete test volunteer position for cleanup")
            .WithDescription("Delete a test volunteer position by ID. ONLY available in Development/Test.")
            .WithTags("Testing", "TestHelpers")
            .Produces(204)
            .Produces<object>(400);

        // ====================================================================
        // VETTING APPLICATION ENDPOINTS
        // ====================================================================

        // Create test vetting application endpoint
        app.MapPost("/api/test-helpers/vetting-applications", async (
            CreateTestVettingApplicationRequest request,
            ITestHelperService testHelperService,
            CancellationToken cancellationToken) =>
            {
                var (success, data, error) = await testHelperService.CreateTestVettingApplicationAsync(request, cancellationToken);

                if (success && data != null)
                {
                    return Results.Created($"/api/test-helpers/vetting-applications/{data.Id}", data);
                }

                return Results.Problem(
                    title: "Failed to create test vetting application",
                    detail: error,
                    statusCode: 400);
            })
            .AllowAnonymous()
            .WithName("CreateTestVettingApplication")
            .WithSummary("Create test vetting application for E2E testing")
            .WithDescription("Programmatically create a vetting application with specific properties for testing. ONLY available in Development/Test.")
            .WithTags("Testing", "TestHelpers")
            .Produces<object>(201)
            .Produces<object>(400);

        // Delete test vetting application endpoint
        app.MapDelete("/api/test-helpers/vetting-applications/{applicationId:guid}", async (
            Guid applicationId,
            ITestHelperService testHelperService,
            CancellationToken cancellationToken) =>
            {
                var (success, error) = await testHelperService.DeleteTestVettingApplicationAsync(applicationId, cancellationToken);

                if (success)
                {
                    return Results.NoContent();
                }

                return Results.Problem(
                    title: "Failed to delete test vetting application",
                    detail: error,
                    statusCode: 400);
            })
            .AllowAnonymous()
            .WithName("DeleteTestVettingApplication")
            .WithSummary("Delete test vetting application for cleanup")
            .WithDescription("Delete a test vetting application by ID. ONLY available in Development/Test.")
            .WithTags("Testing", "TestHelpers")
            .Produces(204)
            .Produces<object>(400);
    }
}
