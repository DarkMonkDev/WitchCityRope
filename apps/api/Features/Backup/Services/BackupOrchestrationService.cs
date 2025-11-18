using WitchCityRope.Api.Features.Backup.Models;

namespace WitchCityRope.Api.Features.Backup.Services;

public class BackupOrchestrationService
{
    private readonly DatabaseBackupService _databaseBackup;
    private readonly SpacesStorageService _spacesStorage;
    private readonly ILogger<BackupOrchestrationService> _logger;

    public BackupOrchestrationService(
        DatabaseBackupService databaseBackup,
        SpacesStorageService spacesStorage,
        ILogger<BackupOrchestrationService> logger)
    {
        _databaseBackup = databaseBackup;
        _spacesStorage = spacesStorage;
        _logger = logger;
    }

    public async Task<string> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        var fileName = GenerateBackupFileName();
        string? localFilePath = null;

        _logger.LogInformation("Starting backup orchestration: {FileName}", fileName);

        try
        {
            // Step 1: Execute pg_dump
            localFilePath = await _databaseBackup.ExecuteBackupAsync(fileName, cancellationToken);

            // Step 2: Create metadata
            var metadata = new BackupMetadata
            {
                FileName = fileName,
                Timestamp = DateTime.UtcNow,
                DatabaseName = "witchcityrope",
                SchemaName = "public",
                PgVersion = "16.0",
                CreatedBy = "manual-trigger",
                BackupType = "full",
                DbStats = await _databaseBackup.GetDatabaseStatsAsync()
            };

            // Step 3: Upload to Spaces
            await _spacesStorage.UploadBackupAsync(localFilePath, fileName, metadata, cancellationToken);

            _logger.LogInformation("Backup orchestration completed successfully");

            return fileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup orchestration failed");
            throw;
        }
        finally
        {
            // Cleanup local file
            if (!string.IsNullOrEmpty(localFilePath) && File.Exists(localFilePath))
            {
                try
                {
                    File.Delete(localFilePath);
                    _logger.LogDebug("Cleaned up local backup file: {FilePath}", localFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup local backup file: {FilePath}", localFilePath);
                }
            }
        }
    }

    public async Task<bool> RestoreBackupAsync(string fileName, bool createPreBackup = true, CancellationToken cancellationToken = default)
    {
        string? localFilePath = null;
        string? preBackupFileName = null;

        _logger.LogInformation("Starting restore orchestration: {FileName} (pre-backup: {CreatePreBackup})", fileName, createPreBackup);

        try
        {
            // Step 1: Create pre-restore backup if requested
            if (createPreBackup)
            {
                _logger.LogInformation("Creating pre-restore backup");
                preBackupFileName = await CreateBackupAsync(cancellationToken);
                _logger.LogInformation("Pre-restore backup created: {FileName}", preBackupFileName);
            }

            // Step 2: Download backup from Spaces OR use local uploaded file
            // Check if fileName is already a local file path (uploaded file scenario)
            if (File.Exists(fileName))
            {
                _logger.LogInformation("Using local uploaded file: {FileName}", fileName);
                localFilePath = fileName;
            }
            else
            {
                _logger.LogInformation("Downloading backup from Spaces: {FileName}", fileName);
                localFilePath = await _spacesStorage.DownloadBackupAsync(fileName, cancellationToken);
            }

            // Step 3: Execute pg_restore
            await _databaseBackup.ExecuteRestoreAsync(localFilePath, cancellationToken);

            _logger.LogInformation("Restore orchestration completed successfully");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore orchestration failed");
            throw;
        }
        finally
        {
            // Cleanup downloaded file
            if (!string.IsNullOrEmpty(localFilePath) && File.Exists(localFilePath))
            {
                try
                {
                    File.Delete(localFilePath);
                    _logger.LogDebug("Cleaned up downloaded backup file: {FilePath}", localFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup downloaded backup file: {FilePath}", localFilePath);
                }
            }
        }
    }

    private static string GenerateBackupFileName()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmss");
        return $"backup-{timestamp}.sql";
    }
}
