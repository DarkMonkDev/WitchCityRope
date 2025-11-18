using WitchCityRope.Api.Features.Backup.Services;
using Hangfire;
using Hangfire.Server;

namespace WitchCityRope.Api.Features.Backup.Jobs;

public class BackupJob
{
    private readonly BackupOrchestrationService _orchestrationService;
    private readonly ILogger<BackupJob> _logger;

    public BackupJob(
        BackupOrchestrationService orchestrationService,
        ILogger<BackupJob> logger)
    {
        _orchestrationService = orchestrationService;
        _logger = logger;
    }

    public async Task ExecuteAsync(PerformContext context)
    {
        var jobId = context.BackgroundJob.Id;
        _logger.LogInformation("Backup job {JobId} started", jobId);

        try
        {
            var fileName = await _orchestrationService.CreateBackupAsync(context.CancellationToken.ShutdownToken);
            _logger.LogInformation("Backup job {JobId} completed successfully: {FileName}", jobId, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup job {JobId} failed", jobId);
            throw;
        }
    }
}
