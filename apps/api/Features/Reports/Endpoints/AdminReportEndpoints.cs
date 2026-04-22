using Microsoft.AspNetCore.Mvc;
using WitchCityRope.Api.Features.Reports.Models;
using WitchCityRope.Api.Features.Reports.Services;
using WitchCityRope.Api.Features.Users.Constants;
using WitchCityRope.Api.Features.Shared.Extensions;

namespace WitchCityRope.Api.Features.Reports.Endpoints;

/// <summary>
/// Admin report endpoints for analytics dashboards.
/// Provides user summary statistics and daily transaction aggregations.
/// </summary>
public static class AdminReportEndpoints
{
    public static void MapAdminReportEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/admin/reports/user-summary
        // Returns current counts of users by verification and vetting status
        app.MapGet("/api/admin/reports/user-summary", async (
            IReportService reportService,
            CancellationToken cancellationToken) =>
        {
            var result = await reportService.GetUserSummaryAsync(cancellationToken);
            return Results.Ok(result);
        })
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString()))
        .WithName("GetUserSummary")
        .WithSummary("Get user summary statistics for admin reports dashboard")
        .WithTags("Admin", "Reports")
        .Produces<UserSummaryResponse>(200)
        .Produces(401)
        .Produces(403);

        // GET /api/admin/reports/daily-transactions?dateFrom=2026-03-01&dateTo=2026-03-17
        // Returns daily aggregated transaction counts for charting
        app.MapGet("/api/admin/reports/daily-transactions", async (
            [FromQuery] DateOnly dateFrom,
            [FromQuery] DateOnly dateTo,
            IReportService reportService,
            CancellationToken cancellationToken) =>
        {
            // Validate date range - dateTo must not precede dateFrom
            if (dateTo < dateFrom)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["dateTo"] = ["dateTo must be >= dateFrom"]
                });

            // Limit to 365 days to prevent excessive queries and large response payloads
            if (dateTo.DayNumber - dateFrom.DayNumber > 365)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["dateTo"] = ["Date range cannot exceed 365 days"]
                });

            var result = await reportService.GetDailyTransactionsAsync(dateFrom, dateTo, cancellationToken);
            return Results.Ok(result);
        })
        .RequireAuthorization(policy => policy.RequireRole(
            UserRole.Administrator.ToRoleString()))
        .WithName("GetDailyTransactions")
        .WithSummary("Get daily transaction aggregations for charting")
        .WithTags("Admin", "Reports")
        .Produces<DailyTransactionsResponse>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403);
    }
}
