using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Logging.Models;

namespace WitchCityRope.Api.Features.Logging.Endpoints;

/// <summary>
/// Admin-only endpoints for querying application logs and daily summaries.
/// All data is read-only (AsNoTracking).
/// </summary>
public static class AdminLogEndpoints
{
    private const int MaxPageSize = 200;
    private const int MaxDaysBack = 90;

    public static void MapAdminLogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/logs")
            .WithTags("Admin Logs")
            .WithOpenApi()
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

        group.MapGet("", QueryLogs)
            .WithName("QueryLogs")
            .WithSummary("Query application logs")
            .WithDescription("Search and filter raw application logs with pagination. Returns logs ordered by timestamp descending.")
            .Produces<LogQueryResponse>(200)
            .ProducesProblem(401)
            .ProducesProblem(403);

        group.MapGet("/summaries", QuerySummaries)
            .WithName("QueryDailyLogSummaries")
            .WithSummary("Query daily log summaries")
            .WithDescription("Retrieve aggregated daily log summaries with optional category filter.")
            .Produces<List<DailyLogSummaryDto>>(200)
            .ProducesProblem(401)
            .ProducesProblem(403);

        group.MapGet("/summaries/cc-failures", QueryCcFailures)
            .WithName("QueryCcFailureSummaries")
            .WithSummary("Query credit card failure summaries")
            .WithDescription("Convenience endpoint to retrieve daily credit card failure summaries.")
            .Produces<List<DailyLogSummaryDto>>(200)
            .ProducesProblem(401)
            .ProducesProblem(403);
    }

    private static async Task<IResult> QueryLogs(
        [AsParameters] LogQueryParameters parameters,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var from = parameters.From ?? now.AddDays(-7);
        var to = parameters.To ?? now;

        // Enforce max 90 days back
        var earliest = now.AddDays(-MaxDaysBack);
        if (from < earliest)
        {
            from = earliest;
        }

        // Clamp page and page size
        var page = Math.Max(1, parameters.Page);
        var pageSize = Math.Clamp(parameters.PageSize, 1, MaxPageSize);

        var query = context.ApplicationLogs
            .AsNoTracking()
            .Where(l => l.Timestamp >= from.UtcDateTime && l.Timestamp <= to.UtcDateTime);

        if (!string.IsNullOrWhiteSpace(parameters.Level))
        {
            query = query.Where(l => l.LevelName == parameters.Level);
        }

        if (!string.IsNullOrWhiteSpace(parameters.SourceContext))
        {
            query = query.Where(l => l.SourceContext != null && l.SourceContext.Contains(parameters.SourceContext));
        }

        if (parameters.UserId.HasValue)
        {
            query = query.Where(l => l.UserId == parameters.UserId.Value);
        }

        if (parameters.CorrelationId.HasValue)
        {
            query = query.Where(l => l.CorrelationId == parameters.CorrelationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            query = query.Where(l => l.Message.Contains(parameters.Search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LogEntryDto
            {
                Id = l.Id,
                Timestamp = l.Timestamp,
                LevelName = l.LevelName,
                Message = l.Message,
                Exception = l.Exception,
                SourceContext = l.SourceContext,
                UserId = l.UserId,
                CorrelationId = l.CorrelationId,
                RequestPath = l.RequestPath
            })
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return Results.Ok(new LogQueryResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasPreviousPage = page > 1,
            HasNextPage = page < totalPages
        });
    }

    private static async Task<IResult> QuerySummaries(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? category,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var fromDate = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var query = context.DailyLogSummaries
            .AsNoTracking()
            .Where(s => s.Date >= fromDate && s.Date <= toDate);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(s => s.Category == category);
        }

        var summaries = await query
            .OrderByDescending(s => s.Date)
            .ThenBy(s => s.Category)
            .Select(s => new DailyLogSummaryDto
            {
                Date = s.Date,
                Category = s.Category,
                Subcategory = s.Subcategory,
                Count = s.Count,
                Metadata = s.Metadata
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(summaries);
    }

    private static async Task<IResult> QueryCcFailures(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var fromDate = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var summaries = await context.DailyLogSummaries
            .AsNoTracking()
            .Where(s => s.Date >= fromDate && s.Date <= toDate && s.Category == "cc_failure")
            .OrderByDescending(s => s.Date)
            .ThenBy(s => s.Category)
            .Select(s => new DailyLogSummaryDto
            {
                Date = s.Date,
                Category = s.Category,
                Subcategory = s.Subcategory,
                Count = s.Count,
                Metadata = s.Metadata
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(summaries);
    }
}

/// <summary>
/// Paginated response for log queries.
/// </summary>
public class LogQueryResponse
{
    public List<LogEntryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}
