using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using WitchCityRope.Api.Features.Backup.Models;
using Microsoft.Extensions.Options;

namespace WitchCityRope.Api.Features.Backup.Services;

/// <summary>
/// DigitalOcean Spaces storage service for database backups.
///
/// Environment-Specific Storage Structure:
/// - Local Development: backups/local/
/// - Staging: backups/staging/
/// - Production: backups/production/
///
/// Each environment uses isolated storage folders to prevent cross-environment contamination.
/// Folder prefix configured via BackupConfiguration__Spaces__FolderPrefix environment variable.
///
/// Bucket: witchcityrope
/// Endpoint: https://nyc3.digitaloceanspaces.com
/// </summary>
public class SpacesStorageService
{
    private readonly BackupConfiguration _config;
    private readonly ILogger<SpacesStorageService> _logger;
    private readonly IAmazonS3 _s3Client;

    public SpacesStorageService(
        IOptions<BackupConfiguration> config,
        ILogger<SpacesStorageService> logger)
    {
        _config = config.Value;
        _logger = logger;

        // Validate required configuration values
        if (string.IsNullOrWhiteSpace(_config.Spaces.Endpoint))
        {
            throw new InvalidOperationException(
                "DigitalOcean Spaces Endpoint is not configured. " +
                "Set the BackupConfiguration__Spaces__Endpoint environment variable.");
        }

        if (string.IsNullOrWhiteSpace(_config.Spaces.BucketName))
        {
            throw new InvalidOperationException(
                "DigitalOcean Spaces BucketName is not configured. " +
                "Set the BackupConfiguration__Spaces__BucketName environment variable.");
        }

        if (string.IsNullOrWhiteSpace(_config.Spaces.AccessKey))
        {
            throw new InvalidOperationException(
                "DigitalOcean Spaces AccessKey is not configured. " +
                "Set the BackupConfiguration__Spaces__AccessKey environment variable.");
        }

        if (string.IsNullOrWhiteSpace(_config.Spaces.SecretKey))
        {
            throw new InvalidOperationException(
                "DigitalOcean Spaces SecretKey is not configured. " +
                "Set the BackupConfiguration__Spaces__SecretKey environment variable.");
        }

        try
        {
            // Initialize S3 client for DigitalOcean Spaces
            var s3Config = new AmazonS3Config
            {
                ServiceURL = _config.Spaces.Endpoint,
                ForcePathStyle = true
            };

            _s3Client = new AmazonS3Client(
                _config.Spaces.AccessKey,
                _config.Spaces.SecretKey,
                s3Config);

            _logger.LogInformation(
                "SpacesStorageService initialized successfully. Endpoint: {Endpoint}, Bucket: {Bucket}",
                _config.Spaces.Endpoint,
                _config.Spaces.BucketName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize S3 client for DigitalOcean Spaces");
            throw;
        }
    }

    public async Task<string> UploadBackupAsync(string localFilePath, string fileName, BackupMetadata metadata, CancellationToken cancellationToken = default)
    {
        var fileInfo = new FileInfo(localFilePath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("Backup file not found", localFilePath);
        }

        var key = $"{_config.Spaces.FolderPrefix}/{fileName}";
        var metadataKey = $"{_config.Spaces.FolderPrefix}/{fileName}.meta.json";

        _logger.LogInformation("Uploading backup to Spaces: {Key} ({Size} bytes)", key, fileInfo.Length);

        try
        {
            // Upload backup file
            var transferUtility = new TransferUtility(_s3Client);
            var uploadRequest = new TransferUtilityUploadRequest
            {
                FilePath = localFilePath,
                BucketName = _config.Spaces.BucketName,
                Key = key,
                StorageClass = S3StorageClass.Standard,
                CannedACL = S3CannedACL.Private // Keep backups private
            };

            await transferUtility.UploadAsync(uploadRequest, cancellationToken);

            _logger.LogInformation("Backup file uploaded successfully");

            // Upload metadata file
            metadata.SizeBytes = fileInfo.Length;
            var metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });

            var metadataRequest = new PutObjectRequest
            {
                BucketName = _config.Spaces.BucketName,
                Key = metadataKey,
                ContentBody = metadataJson,
                ContentType = "application/json",
                CannedACL = S3CannedACL.Private
            };

            await _s3Client.PutObjectAsync(metadataRequest, cancellationToken);

            _logger.LogInformation("Metadata file uploaded successfully");

            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload backup to Spaces");
            throw;
        }
    }

    public async Task<string> DownloadBackupAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var key = $"{_config.Spaces.FolderPrefix}/{fileName}";
        var localFilePath = Path.Combine(_config.BackupOptions.TempDirectory, fileName);

        _logger.LogInformation("Downloading backup from Spaces: {Key}", key);

        try
        {
            var transferUtility = new TransferUtility(_s3Client);
            await transferUtility.DownloadAsync(localFilePath, _config.Spaces.BucketName, key, cancellationToken);

            var fileInfo = new FileInfo(localFilePath);
            _logger.LogInformation("Backup downloaded successfully ({Size} bytes)", fileInfo.Length);

            return localFilePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download backup from Spaces");
            throw;
        }
    }

    public async Task<List<BackupListItem>> ListBackupsAsync(int? lastDays = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing backups from Spaces (last {Days} days)", lastDays ?? 0);

        try
        {
            var listRequest = new ListObjectsV2Request
            {
                BucketName = _config.Spaces.BucketName,
                Prefix = $"{_config.Spaces.FolderPrefix}/backup-"
            };

            var response = await _s3Client.ListObjectsV2Async(listRequest, cancellationToken);

            var backups = (response.S3Objects ?? new List<S3Object>())
                .Where(obj => obj.Key.EndsWith(".dump") || obj.Key.EndsWith(".sql"))
                .Select(obj => new BackupListItem
                {
                    FileName = Path.GetFileName(obj.Key),
                    Timestamp = obj.LastModified,
                    SizeBytes = obj.Size,
                    SizeFormatted = FormatBytes(obj.Size)
                })
                .OrderByDescending(b => b.Timestamp)
                .ToList();

            // Filter by date if specified
            if (lastDays.HasValue && lastDays.Value > 0)
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-lastDays.Value);
                backups = backups.Where(b => b.Timestamp >= cutoffDate).ToList();
            }

            _logger.LogInformation("Found {Count} backups", backups.Count);

            return backups;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list backups from Spaces");
            throw;
        }
    }

    public async Task<bool> DeleteBackupAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var key = $"{_config.Spaces.FolderPrefix}/{fileName}";
        var metadataKey = $"{_config.Spaces.FolderPrefix}/{fileName}.meta.json";

        _logger.LogInformation("Deleting backup from Spaces: {Key}", key);

        try
        {
            // Delete backup file
            await _s3Client.DeleteObjectAsync(_config.Spaces.BucketName, key, cancellationToken);

            // Delete metadata file (ignore if doesn't exist)
            try
            {
                await _s3Client.DeleteObjectAsync(_config.Spaces.BucketName, metadataKey, cancellationToken);
            }
            catch
            {
                _logger.LogWarning("Metadata file not found or could not be deleted: {Key}", metadataKey);
            }

            _logger.LogInformation("Backup deleted successfully");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete backup from Spaces");
            throw;
        }
    }

    public async Task<Stream> GetBackupStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var key = $"{_config.Spaces.FolderPrefix}/{fileName}";

        _logger.LogInformation("Getting backup stream from Spaces: {Key}", key);

        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _config.Spaces.BucketName,
                Key = key
            };

            var response = await _s3Client.GetObjectAsync(request, cancellationToken);
            return response.ResponseStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get backup stream from Spaces");
            throw;
        }
    }

    public async Task<StorageSummaryResponse> GetStorageSummaryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting storage summary from Spaces");

        try
        {
            var backups = await ListBackupsAsync(null, cancellationToken);

            var totalSize = backups.Sum(b => b.SizeBytes);
            var limitBytes = 250L * 1024 * 1024 * 1024; // 250 GB

            return new StorageSummaryResponse
            {
                TotalBackups = backups.Count,
                TotalSizeBytes = totalSize,
                TotalSizeFormatted = FormatBytes(totalSize),
                LimitBytes = limitBytes,
                LimitFormatted = FormatBytes(limitBytes),
                PercentUsed = Math.Round((double)totalSize / limitBytes * 100, 2)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get storage summary");
            throw;
        }
    }

    public async Task CleanupOldBackupsAsync(int retentionDays = 30, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting cleanup of backups older than {Days} days", retentionDays);

        try
        {
            var backups = await ListBackupsAsync(cancellationToken: cancellationToken);
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            var oldBackups = backups.Where(b => b.Timestamp < cutoffDate).ToList();

            if (oldBackups.Count == 0)
            {
                _logger.LogInformation("No old backups to clean up");
                return;
            }

            _logger.LogInformation("Found {Count} backups to delete", oldBackups.Count);

            foreach (var backup in oldBackups)
            {
                try
                {
                    await DeleteBackupAsync(backup.FileName, cancellationToken);
                    _logger.LogInformation("Deleted old backup: {FileName}", backup.FileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete backup: {FileName}", backup.FileName);
                    // Continue with other backups even if one fails
                }
            }

            _logger.LogInformation("Cleanup complete. Deleted {Count} old backups", oldBackups.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old backups");
            throw;
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
