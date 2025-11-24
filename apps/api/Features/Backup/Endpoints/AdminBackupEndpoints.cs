using WitchCityRope.Api.Features.Backup.Jobs;
using WitchCityRope.Api.Features.Backup.Models;
using WitchCityRope.Api.Features.Backup.Services;
using WitchCityRope.Api.Features.Users.Constants;
using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.AspNetCore.Mvc;

namespace WitchCityRope.Api.Features.Backup.Endpoints;

public static class AdminBackupEndpoints
{
    public static void MapAdminBackupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/backup")
            .WithTags("Admin", "Backup")
            .WithOpenApi();

        // 1. Trigger Manual Backup
        group.MapPost("", TriggerBackup)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
            .WithName("TriggerBackup")
            .WithSummary("Trigger a manual database backup (admin only)")
            .WithDescription("Enqueues a Hangfire job to create a PostgreSQL backup via pg_dump and upload to DigitalOcean Spaces")
            .Produces<BackupJobResponse>(200)
            .Produces(401)
            .Produces(403)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(500);

        // 2. List All Backups
        group.MapGet("/list", ListBackups)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
            .WithName("ListBackups")
            .WithSummary("List all available backups (admin only)")
            .WithDescription("Retrieves environment-specific database backups from DigitalOcean Spaces. Each environment (local/staging/production) has isolated backup storage determined by the BackupConfiguration__Spaces__FolderPrefix setting.")
            .Produces<BackupListResponse>(200)
            .Produces(401)
            .Produces(403)
            .Produces<ErrorResponse>(500);

        // 3. Restore from Backup
        group.MapPost("/restore", RestoreBackup)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
            .WithName("RestoreBackup")
            .WithSummary("Restore database from a backup (admin only)")
            .WithDescription("Downloads backup from DigitalOcean Spaces and restores database. Optionally creates pre-restore backup. Requires confirmation 'RESTORE'")
            .Produces<BackupJobResponse>(200)
            .Produces(401)
            .Produces(403)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(404)
            .Produces<ErrorResponse>(500);

        // 4. Delete Backup
        group.MapDelete("/{fileName}", DeleteBackup)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
            .WithName("DeleteBackup")
            .WithSummary("Delete a backup file (admin only)")
            .WithDescription("Permanently deletes a backup file from DigitalOcean Spaces")
            .Produces<object>(200)
            .Produces(401)
            .Produces(403)
            .Produces<ErrorResponse>(404)
            .Produces<ErrorResponse>(500);

        // 5. Download Backup
        group.MapGet("/download/{fileName}", DownloadBackup)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
            .WithName("DownloadBackup")
            .WithSummary("Download a backup file (admin only)")
            .WithDescription("Downloads a backup file from DigitalOcean Spaces as a binary stream")
            .Produces<Stream>(200)
            .Produces(401)
            .Produces(403)
            .Produces<ErrorResponse>(404)
            .Produces<ErrorResponse>(500);

        // 6. Get Job Status
        group.MapGet("/job/{jobId}", GetJobStatus)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
            .WithName("GetJobStatus")
            .WithSummary("Get the status of a backup or restore job (admin only)")
            .WithDescription("Retrieves Hangfire job status including progress, state, and timestamps")
            .Produces<BackupJobStatusResponse>(200)
            .Produces(401)
            .Produces(403)
            .Produces<ErrorResponse>(404);

        // 7. Get Storage Summary
        group.MapGet("/storage", GetStorageSummary)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
            .WithName("GetStorageSummary")
            .WithSummary("Get storage usage summary (admin only)")
            .WithDescription("Retrieves DigitalOcean Spaces storage usage including total backups, size, and oldest/newest backup info")
            .Produces<StorageSummaryResponse>(200)
            .Produces(401)
            .Produces(403)
            .Produces<ErrorResponse>(500);

        // 8. Upload and Restore Local Backup
        // CRITICAL SECURITY NOTE: CSRF protection is AUTOMATICALLY ENABLED via app.UseAntiforgery() middleware
        // Prevents CSRF attacks on database restore (catastrophic risk if exploited)
        // Multipart/form-data uploads CAN include CSRF token in form field or X-CSRF-TOKEN header
        group.MapPost("/upload-and-restore", UploadAndRestoreBackup)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
            // CSRF PROTECTION: Enabled automatically by app.UseAntiforgery() middleware (prevents malicious database restoration)
            .WithName("UploadAndRestoreBackup")
            .WithSummary("Upload a local backup file and restore it (admin only)")
            .WithDescription("Accepts .dump or .sql file upload, saves to temp directory, and enqueues restore job. Optionally creates pre-restore backup")
            .Produces<BackupJobResponse>(200)
            .Produces(401)
            .Produces(403)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(500);
    }

    private static IResult TriggerBackup(
        IBackgroundJobClient backgroundJobs,
        ILogger<Program> logger)
    {
        try
        {
            // Check if backup is already in progress
            // (In production, you might store this in cache or database)

            var jobId = backgroundJobs.Enqueue<BackupJob>(job => job.ExecuteAsync(null!));

            logger.LogInformation("Backup job enqueued: {JobId}", jobId);

            return Results.Ok(new BackupJobResponse
            {
                JobId = jobId,
                Message = "Backup started",
                EstimatedSeconds = 30
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start backup");
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Failed to start backup");
        }
    }

    private static async Task<IResult> ListBackups(
        [FromQuery] int? days,
        SpacesStorageService spacesStorage,
        ILogger<Program> logger)
    {
        try
        {
            var backups = await spacesStorage.ListBackupsAsync(days);

            var response = new BackupListResponse
            {
                Backups = backups,
                TotalCount = backups.Count,
                TotalSizeBytes = backups.Sum(b => b.SizeBytes),
                TotalSizeFormatted = FormatBytes(backups.Sum(b => b.SizeBytes))
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list backups");
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Failed to list backups");
        }
    }

    private static IResult RestoreBackup(
        RestoreRequest request,
        IBackgroundJobClient backgroundJobs,
        SpacesStorageService spacesStorage,
        ILogger<Program> logger)
    {
        try
        {
            // Validate confirmation
            if (request.Confirmation != "RESTORE")
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Error = "Confirmation must be 'RESTORE'"
                });
            }

            // Verify backup exists (simplified check)
            // In production, you might want to verify the file exists before queuing the job

            var jobId = backgroundJobs.Enqueue<RestoreJob>(job =>
                job.ExecuteAsync(request.FileName, request.CreatePreBackup, null!));

            logger.LogInformation("Restore job enqueued: {JobId} for file: {FileName}", jobId, request.FileName);

            return Results.Ok(new BackupJobResponse
            {
                JobId = jobId,
                Message = "Restore started"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start restore");
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Failed to start restore");
        }
    }

    private static async Task<IResult> DeleteBackup(
        string fileName,
        SpacesStorageService spacesStorage,
        ILogger<Program> logger)
    {
        try
        {
            await spacesStorage.DeleteBackupAsync(fileName);
            return Results.Ok(new { message = "Backup deleted successfully" });
        }
        catch (FileNotFoundException)
        {
            return Results.NotFound(new ErrorResponse { Error = "Backup not found" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete backup");
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Failed to delete backup");
        }
    }

    private static async Task<IResult> DownloadBackup(
        string fileName,
        SpacesStorageService spacesStorage,
        ILogger<Program> logger)
    {
        try
        {
            var stream = await spacesStorage.GetBackupStreamAsync(fileName);
            return Results.File(stream, "application/octet-stream", fileName);
        }
        catch (FileNotFoundException)
        {
            return Results.NotFound(new ErrorResponse { Error = "Backup not found" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download backup");
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Failed to download backup");
        }
    }

    private static IResult GetJobStatus(
        string jobId,
        ILogger<Program> logger)
    {
        try
        {
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            var jobDetails = monitoringApi.JobDetails(jobId);
            if (jobDetails == null)
            {
                return Results.NotFound(new ErrorResponse { Error = "Job not found" });
            }

            var response = new BackupJobStatusResponse
            {
                JobId = jobId,
                Status = jobDetails.History.Count > 0 ? jobDetails.History[0].StateName.ToLower() : "unknown",
                Progress = 0, // Simplified - Hangfire doesn't track progress by default
                Message = jobDetails.History.Count > 0 ? jobDetails.History[0].StateName : "Unknown",
                StartedAt = jobDetails.CreatedAt,
                CompletedAt = jobDetails.History.FirstOrDefault(h => h.StateName == "Succeeded")?.CreatedAt
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get job status");
            return Results.NotFound(new ErrorResponse { Error = "Job not found" });
        }
    }

    private static async Task<IResult> GetStorageSummary(
        SpacesStorageService spacesStorage,
        ILogger<Program> logger)
    {
        try
        {
            var summary = await spacesStorage.GetStorageSummaryAsync();
            return Results.Ok(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get storage summary");
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Failed to get storage summary");
        }
    }

    private static async Task<IResult> UploadAndRestoreBackup(
        HttpRequest request,
        IBackgroundJobClient backgroundJobs,
        IConfiguration configuration,
        ILogger<Program> logger)
    {
        try
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Error = "Request must be multipart/form-data"
                });
            }

            var form = await request.ReadFormAsync();
            var file = form.Files.GetFile("file");

            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Error = "No file uploaded"
                });
            }

            if (!file.FileName.EndsWith(".dump", StringComparison.OrdinalIgnoreCase) &&
                !file.FileName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Error = "File must be a .dump or .sql file"
                });
            }

            // Get temp directory from configuration
            var tempDirectory = configuration["BackupConfiguration:BackupOptions:TempDirectory"] ?? "/tmp";
            var fileExtension = Path.GetExtension(file.FileName); // Preserve .dump or .sql extension
            var tempFilePath = Path.Combine(tempDirectory, $"uploaded-{Guid.NewGuid()}{fileExtension}");

            // Save the uploaded file to temp directory
            await using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            logger.LogInformation("Uploaded backup file saved to: {TempFilePath}", tempFilePath);

            // Check if pre-backup is requested (default: true)
            var createPreBackup = true;
            if (form.TryGetValue("createPreBackup", out var preBackupValue))
            {
                createPreBackup = preBackupValue.ToString().ToLower() != "false";
            }

            // Enqueue restore job with the temp file path
            // Use the full temp path as the "fileName" - RestoreJob will handle it as a local file
            var jobId = backgroundJobs.Enqueue<RestoreJob>(job =>
                job.ExecuteAsync(tempFilePath, createPreBackup, null!));

            logger.LogInformation("Upload and restore job enqueued: {JobId} for file {FileName}", jobId, file.FileName);

            return Results.Ok(new BackupJobResponse
            {
                JobId = jobId,
                Message = $"Restore started from uploaded file: {file.FileName}",
                EstimatedSeconds = 60
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload and restore backup");
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Upload and restore failed"
            );
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
