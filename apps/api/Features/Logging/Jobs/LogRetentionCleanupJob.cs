using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;

namespace WitchCityRope.Api.Features.Logging.Jobs;

/// <summary>
/// Hangfire recurring job that deletes application log entries older than 90 days.
/// Uses batched deletes (10,000 rows per batch) to avoid long-running locks.
/// Scheduled to run at 3 AM UTC daily.
/// </summary>
public class LogRetentionCleanupJob
{
    private const int BatchSize = 10_000;
    private const int RetentionDays = 90;

    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<LogRetentionCleanupJob> _logger;

    public LogRetentionCleanupJob(
        ApplicationDbContext dbContext,
        ILogger<LogRetentionCleanupJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-RetentionDays);
        _logger.LogInformation(
            "Log retention cleanup started. Deleting logs older than {CutoffDate} ({RetentionDays} days)",
            cutoffDate, RetentionDays);

        var totalDeleted = 0L;

        try
        {
            int batchDeleted;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Batched delete using ctid subquery for efficient row identification
                batchDeleted = await _dbContext.Database.ExecuteSqlRawAsync(
                    """
                    DELETE FROM logging.application_logs
                    WHERE ctid IN (
                        SELECT ctid FROM logging.application_logs
                        WHERE timestamp < @p0
                        LIMIT @p1
                    )
                    """,
                    [cutoffDate, BatchSize],
                    cancellationToken);

                totalDeleted += batchDeleted;

                if (batchDeleted > 0)
                {
                    _logger.LogDebug(
                        "Log retention cleanup batch: deleted {BatchCount} rows, total so far: {TotalCount}",
                        batchDeleted, totalDeleted);
                }
            }
            while (batchDeleted == BatchSize);

            _logger.LogInformation(
                "Log retention cleanup completed. Deleted {TotalCount} log entries older than {CutoffDate}",
                totalDeleted, cutoffDate);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Log retention cleanup cancelled after deleting {TotalCount} rows", totalDeleted);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Log retention cleanup failed after deleting {TotalCount} rows", totalDeleted);
            throw;
        }
    }
}
