using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Logging.Entities;

namespace WitchCityRope.Api.Features.Logging.Jobs;

/// <summary>
/// Hangfire recurring job that aggregates the previous day's application logs
/// into daily_log_summaries for admin dashboard reporting.
/// Scheduled to run at 1 AM UTC daily.
/// </summary>
public class DailyLogSummaryJob
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DailyLogSummaryJob> _logger;

    public DailyLogSummaryJob(
        ApplicationDbContext dbContext,
        ILogger<DailyLogSummaryJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var startUtc = yesterday.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = yesterday.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        _logger.LogInformation("Daily log summary job started for {Date}", yesterday);

        try
        {
            var summaries = new List<DailyLogSummary>();

            // Aggregate credit card failures by error code from properties
            await AggregateByCategoryAsync(summaries, yesterday, startUtc, endUtc,
                "cc_failure",
                "source_context LIKE '%Payment%' AND level >= 3 AND message LIKE '%credit card%fail%'",
                "properties->>'ErrorCode'",
                cancellationToken);

            // Aggregate credit card successes
            await AggregateByCategoryAsync(summaries, yesterday, startUtc, endUtc,
                "cc_success",
                "source_context LIKE '%Payment%' AND level <= 2 AND message LIKE '%payment%completed%'",
                null,
                cancellationToken);

            // Aggregate login failures by reason
            await AggregateByCategoryAsync(summaries, yesterday, startUtc, endUtc,
                "login_failure",
                "source_context LIKE '%Auth%' AND level >= 3 AND message LIKE '%login%fail%'",
                "properties->>'FailureReason'",
                cancellationToken);

            // Aggregate login successes
            await AggregateByCategoryAsync(summaries, yesterday, startUtc, endUtc,
                "login_success",
                "source_context LIKE '%Auth%' AND level <= 2 AND message LIKE '%login%success%'",
                null,
                cancellationToken);

            // Aggregate registrations
            await AggregateByCategoryAsync(summaries, yesterday, startUtc, endUtc,
                "registration",
                "source_context LIKE '%Auth%' AND message LIKE '%register%'",
                null,
                cancellationToken);

            // Aggregate 4xx API errors by status code
            await AggregateByCategoryAsync(summaries, yesterday, startUtc, endUtc,
                "api_error_4xx",
                "level = 3 AND message LIKE '%responded 4%'",
                "properties->>'StatusCode'",
                cancellationToken);

            // Aggregate 5xx API errors by status code
            await AggregateByCategoryAsync(summaries, yesterday, startUtc, endUtc,
                "api_error_5xx",
                "level >= 4 AND message LIKE '%responded 5%'",
                "properties->>'StatusCode'",
                cancellationToken);

            // Aggregate frontend errors by type
            await AggregateByCategoryAsync(summaries, yesterday, startUtc, endUtc,
                "frontend_error",
                "source_context = 'ClientError'",
                "properties->>'ErrorType'",
                cancellationToken);

            // Aggregate email automation successes by template type
            await AggregateByCategoryAsync(summaries, yesterday, startUtc, endUtc,
                "email_sent",
                "source_context LIKE '%Email%' AND level <= 2 AND (message LIKE '%email sent%' OR message LIKE '%Webhook updated%Completed%' OR message LIKE '%post-purchase%' OR message LIKE '%catch-up%')",
                "properties->>'TemplateType'",
                cancellationToken);

            // Aggregate email automation failures by template type
            await AggregateByCategoryAsync(summaries, yesterday, startUtc, endUtc,
                "email_failure",
                "source_context LIKE '%Email%' AND level >= 3",
                "properties->>'TemplateType'",
                cancellationToken);

            // Aggregate email scheduler job runs
            await AggregateByCategoryAsync(summaries, yesterday, startUtc, endUtc,
                "email_scheduler",
                "source_context LIKE '%EmailScheduler%'",
                null,
                cancellationToken);

            // Upsert all summaries using the unique constraint
            foreach (var summary in summaries)
            {
                await UpsertSummaryAsync(summary, cancellationToken);
            }

            _logger.LogInformation(
                "Daily log summary job completed for {Date}. Generated {Count} summary records",
                yesterday, summaries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Daily log summary job failed for {Date}", yesterday);
            throw;
        }
    }

    private async Task AggregateByCategoryAsync(
        List<DailyLogSummary> summaries,
        DateOnly date,
        DateTime startUtc,
        DateTime endUtc,
        string category,
        string whereClause,
        string? subcategoryExpression,
        CancellationToken cancellationToken)
    {
        string sql;

        if (subcategoryExpression != null)
        {
            sql = $"""
                SELECT COALESCE({subcategoryExpression}, 'unknown') AS subcategory, COUNT(*) AS count
                FROM logging.application_logs
                WHERE timestamp >= @p0 AND timestamp <= @p1
                  AND {whereClause}
                GROUP BY COALESCE({subcategoryExpression}, 'unknown')
                """;
        }
        else
        {
            sql = $"""
                SELECT NULL AS subcategory, COUNT(*) AS count
                FROM logging.application_logs
                WHERE timestamp >= @p0 AND timestamp <= @p1
                  AND {whereClause}
                """;
        }

        using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        var p0 = command.CreateParameter();
        p0.ParameterName = "@p0";
        p0.Value = startUtc;
        command.Parameters.Add(p0);

        var p1 = command.CreateParameter();
        p1.ParameterName = "@p1";
        p1.Value = endUtc;
        command.Parameters.Add(p1);

        await _dbContext.Database.OpenConnectionAsync(cancellationToken);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var count = reader.GetInt64(reader.GetOrdinal("count"));
            if (count == 0) continue;

            var subcategory = reader.IsDBNull(reader.GetOrdinal("subcategory"))
                ? null
                : reader.GetString(reader.GetOrdinal("subcategory"));

            summaries.Add(new DailyLogSummary
            {
                Date = date,
                Category = category,
                Subcategory = subcategory,
                Count = (int)count
            });
        }
    }

    private async Task UpsertSummaryAsync(DailyLogSummary summary, CancellationToken cancellationToken)
    {
        // Use raw SQL upsert leveraging the unique constraint on (date, category, subcategory)
        var sql = """
            INSERT INTO logging.daily_log_summaries ("Date", "Category", "Subcategory", "Count", "CreatedAt")
            VALUES (@p0, @p1, @p2, @p3, NOW())
            ON CONFLICT ("Date", "Category", "Subcategory")
            DO UPDATE SET "Count" = @p3, "CreatedAt" = NOW()
            """;

        await _dbContext.Database.ExecuteSqlRawAsync(
            sql,
            [summary.Date, summary.Category, (object?)summary.Subcategory ?? DBNull.Value, summary.Count],
            cancellationToken);
    }
}
