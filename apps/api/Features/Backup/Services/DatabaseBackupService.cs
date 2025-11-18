using System.Diagnostics;
using WitchCityRope.Api.Features.Backup.Models;
using Microsoft.Extensions.Options;

namespace WitchCityRope.Api.Features.Backup.Services;

public class DatabaseBackupService
{
    private readonly BackupConfiguration _config;
    private readonly ILogger<DatabaseBackupService> _logger;

    public DatabaseBackupService(
        IOptions<BackupConfiguration> config,
        ILogger<DatabaseBackupService> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public async Task<string> ExecuteBackupAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_config.BackupOptions.TempDirectory, fileName);

        _logger.LogInformation("Starting database backup to {FilePath}", filePath);

        try
        {
            // Build pg_dump command
            var arguments = BuildPgDumpArguments(filePath);

            _logger.LogDebug("Executing pg_dump with arguments: {Arguments}", arguments);

            // Execute pg_dump
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "pg_dump",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Environment =
                    {
                        ["PGPASSWORD"] = _config.Database.Password
                    }
                }
            };

            var errorOutput = new List<string>();
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorOutput.Add(e.Data);
                    _logger.LogWarning("pg_dump stderr: {Error}", e.Data);
                }
            };

            process.Start();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var errorMessage = string.Join("\n", errorOutput);
                throw new InvalidOperationException($"pg_dump failed with exit code {process.ExitCode}: {errorMessage}");
            }

            // Verify file was created and has content
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                throw new InvalidOperationException("Backup file was not created");
            }

            if (fileInfo.Length == 0)
            {
                throw new InvalidOperationException("Backup file is empty");
            }

            _logger.LogInformation("Backup completed successfully. File size: {Size} bytes", fileInfo.Length);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute backup");

            // Clean up partial file
            if (File.Exists(filePath))
            {
                try { File.Delete(filePath); } catch { /* ignore */ }
            }

            throw;
        }
    }

    public async Task<bool> ExecuteRestoreAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting database restore from {FilePath}", filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Backup file not found", filePath);
        }

        try
        {
            // Build psql command (for plain SQL format backups)
            var arguments = BuildPsqlRestoreArguments(filePath);

            _logger.LogDebug("Executing psql with arguments: {Arguments}", arguments);

            // Execute psql to restore the backup
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "psql",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Environment =
                    {
                        ["PGPASSWORD"] = _config.Database.Password
                    }
                }
            };

            var errorOutput = new List<string>();
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorOutput.Add(e.Data);
                    // psql can output warnings to stderr even on success
                    _logger.LogDebug("psql stderr: {Error}", e.Data);
                }
            };

            process.Start();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var errorMessage = string.Join("\n", errorOutput);
                throw new InvalidOperationException($"psql failed with exit code {process.ExitCode}: {errorMessage}");
            }

            _logger.LogInformation("Restore completed successfully");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute restore");
            throw;
        }
    }

    public async Task<DatabaseStats> GetDatabaseStatsAsync()
    {
        // This is a simplified version - in production you might query actual table counts
        // For now, return estimated stats
        return await Task.FromResult(new DatabaseStats
        {
            TableCount = 12, // Approximate based on your schema
            EstimatedRowCount = 125000 // Approximate
        });
    }

    private string BuildPgDumpArguments(string outputFile)
    {
        var args = new List<string>
        {
            $"-h {_config.Database.Host}",
            $"-p {_config.Database.Port}",
            $"-U {_config.Database.Username}",
            $"-d {_config.Database.DatabaseName}",
            $"-n {_config.Database.SchemaName}",
            "-Fp", // Plain SQL format (universally compatible across PostgreSQL versions)
            "--clean", // Include DROP statements before CREATE (required for proper restore)
            "--if-exists", // Use DROP...IF EXISTS (prevents errors if objects don't exist)
            "--no-owner", // Don't restore ownership (for portability)
            "--no-privileges", // Don't restore privileges (for portability)
            $"-f {outputFile}"
        };

        return string.Join(" ", args);
    }

    private string BuildPsqlRestoreArguments(string inputFile)
    {
        var args = new List<string>
        {
            $"-h {_config.Database.Host}",
            $"-p {_config.Database.Port}",
            $"-U {_config.Database.Username}",
            $"-d {_config.Database.DatabaseName}",
            "-v ON_ERROR_STOP=1", // Stop immediately on error (critical for restore integrity)
            "-f", // Execute commands from file
            inputFile
        };

        return string.Join(" ", args);
    }
}
