using WitchCityRope.Api.Features.Backup.Services;
using Hangfire;
using Hangfire.Server;

namespace WitchCityRope.Api.Features.Backup.Jobs;

public class RestoreJob
{
    private readonly BackupOrchestrationService _orchestrationService;
    private readonly ILogger<RestoreJob> _logger;

    public RestoreJob(
        BackupOrchestrationService orchestrationService,
        ILogger<RestoreJob> logger)
    {
        _orchestrationService = orchestrationService;
        _logger = logger;
    }

    public async Task ExecuteAsync(string fileName, bool createPreBackup, PerformContext context)
    {
        var jobId = context.BackgroundJob.Id;
        _logger.LogInformation("Restore job {JobId} started: {FileName} (pre-backup: {CreatePreBackup})", jobId, fileName, createPreBackup);

        try
        {
            await _orchestrationService.RestoreBackupAsync(fileName, createPreBackup, context.CancellationToken.ShutdownToken);
            _logger.LogInformation("Restore job {JobId} completed successfully", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore job {JobId} failed", jobId);
            throw;
        }
    }
}
