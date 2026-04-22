using WitchCityRope.Api.Features.Logging.Models;
using WitchCityRope.Api.Features.Shared.Extensions;

namespace WitchCityRope.Api.Features.Logging.Endpoints;

public static class ClientErrorEndpoints
{
    private const int MaxBatchSize = 10;
    private const int MaxStackLength = 4000;
    private const long MaxPayloadBytes = 100 * 1024; // 100KB

    public static void MapClientErrorEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/client-errors", async (
            ClientErrorBatch batch,
            HttpContext httpContext,
            ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("Frontend");

                // Reject oversized payloads
                if (httpContext.Request.ContentLength > MaxPayloadBytes)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["payload"] = ["Payload exceeds maximum size of 100KB."]
                    });
                }

                if (batch.Errors is not { Count: > 0 })
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["errors"] = ["Batch must contain at least one error."]
                    });
                }

                if (batch.Errors.Count > MaxBatchSize)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["errors"] = [$"Batch must contain at most {MaxBatchSize} errors."]
                    });
                }

                var isAuthenticated = httpContext.Request.Cookies.ContainsKey("auth-token");
                var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                logger.LogInformation(
                    "Received client error batch. Count={ErrorCount}, Authenticated={IsAuthenticated}, ClientIP={ClientIP}",
                    batch.Errors.Count, isAuthenticated, clientIp);

                foreach (var error in batch.Errors)
                {
                    var truncatedStack = error.Stack?.Length > MaxStackLength
                        ? error.Stack[..MaxStackLength]
                        : error.Stack;

                    logger.LogWarning(
                        "Client error: {ErrorType} at {ErrorUrl}. Message={ErrorMessage}, Stack={Stack}, ComponentStack={ComponentStack}, Timestamp={ErrorTimestamp}, UserAgent={UserAgent}",
                        error.Type,
                        error.Url,
                        error.Message,
                        truncatedStack,
                        error.ComponentStack,
                        error.Timestamp,
                        error.UserAgent);
                }

                return Results.Json(new { received = batch.Errors.Count }, statusCode: 202);
            })
            .AllowAnonymous()
            .RequireRateLimiting("client-errors")
            .WithName("ReportClientErrors")
            .WithSummary("Ingest frontend error reports")
            .WithDescription("Receives a batch of client-side error reports for server-side logging and analysis")
            .WithTags("Logging")
            .Accepts<ClientErrorBatch>("application/json")
            .Produces(202)
            .Produces(400)
            .Produces(429);
    }
}
