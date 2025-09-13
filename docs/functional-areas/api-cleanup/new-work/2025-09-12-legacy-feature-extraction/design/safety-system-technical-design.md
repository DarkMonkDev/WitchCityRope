# Safety System Technical Design - WitchCityRope
<!-- Last Updated: 2025-09-12 -->
<!-- Version: 1.0 -->
<!-- Owner: Backend Developer Agent -->
<!-- Status: Complete - Ready for Implementation -->

## Executive Summary

This document provides the complete technical architecture and implementation roadmap for the Safety incident reporting system, following the modern vertical slice architecture pattern established in the WitchCityRope React migration. The design implements anonymous incident reporting with encrypted sensitive data, comprehensive audit trails, and legal compliance requirements while maintaining high performance and security standards.

**Key Architecture Decisions:**
- Vertical slice pattern in `/apps/api/Features/Safety/`
- Direct Entity Framework Core access (no repositories or MediatR)
- Result<T> pattern for error handling
- Cookie-based authentication integration
- AES-256 field-level encryption for sensitive data
- PostgreSQL with performance-optimized indexing
- Minimal API endpoints with OpenAPI documentation

## Architecture Context

### Modern API Integration
The Safety System integrates with the existing modern API architecture at `/apps/api/` following established patterns:

- **Database**: PostgreSQL 15+ via Entity Framework Core 9
- **Authentication**: Cookie-based user authentication via Identity
- **Authorization**: Role-based access control (SafetyTeam, Admin roles)
- **Error Handling**: Result<T> pattern for consistent error management
- **Logging**: Structured logging with Serilog
- **API Documentation**: OpenAPI/Swagger integration
- **Testing**: xUnit with TestContainers for integration tests

### Dependencies
- **User Management**: Existing ApplicationUser and Identity system
- **Email Service**: Existing email infrastructure for notifications
- **Authorization**: Existing role-based permission system
- **Audit System**: New audit logging specific to safety incidents

## Vertical Slice Architecture

### Directory Structure
```
/apps/api/Features/Safety/
├── Services/
│   ├── ISafetyService.cs               # Main business logic interface
│   ├── SafetyService.cs                # Core incident management
│   ├── IEncryptionService.cs           # Data encryption interface
│   ├── EncryptionService.cs            # AES-256 encryption implementation
│   ├── IAuditService.cs                # Audit logging interface
│   ├── AuditService.cs                 # Audit trail implementation
│   └── INotificationService.cs         # Email notification interface
├── Endpoints/
│   └── SafetyEndpoints.cs              # Minimal API endpoint registration
├── Models/
│   ├── Requests/
│   │   ├── SubmitIncidentRequest.cs    # Incident submission
│   │   ├── UpdateIncidentRequest.cs    # Status/assignment updates
│   │   └── SearchIncidentsRequest.cs   # Admin search/filter
│   ├── Responses/
│   │   ├── IncidentResponse.cs         # Full incident details
│   │   ├── IncidentSummaryResponse.cs  # List/dashboard summary
│   │   ├── IncidentStatusResponse.cs   # Anonymous tracking
│   │   ├── SafetyDashboardResponse.cs  # Admin dashboard data
│   │   └── SubmissionResponse.cs       # Post-submission confirmation
│   └── Dtos/
│       ├── IncidentDto.cs              # Core incident data
│       ├── AuditLogDto.cs              # Audit trail entry
│       └── NotificationDto.cs          # Email notification data
├── Validation/
│   ├── SubmitIncidentValidator.cs      # FluentValidation rules
│   ├── UpdateIncidentValidator.cs      # Update validation
│   └── SearchIncidentsValidator.cs     # Search parameter validation
├── Entities/
│   ├── SafetyIncident.cs              # Main incident entity
│   ├── IncidentAuditLog.cs            # Audit trail entity
│   └── IncidentNotification.cs        # Email notification entity
└── Extensions/
    └── SafetyServiceExtensions.cs     # DI registration
```

## Service Layer Design

### 1. ISafetyService Interface
```csharp
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Safety.Models.Requests;
using WitchCityRope.Api.Features.Safety.Models.Responses;

namespace WitchCityRope.Api.Features.Safety.Services;

/// <summary>
/// Main safety incident management service
/// Follows simplified vertical slice pattern with direct Entity Framework access
/// </summary>
public interface ISafetyService
{
    /// <summary>
    /// Submit new safety incident report (anonymous or identified)
    /// </summary>
    Task<Result<SubmissionResponse>> SubmitIncidentAsync(
        SubmitIncidentRequest request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get incident status for anonymous tracking
    /// </summary>
    Task<Result<IncidentStatusResponse>> GetIncidentStatusAsync(
        string referenceNumber, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get detailed incident information for safety team
    /// </summary>
    Task<Result<IncidentResponse>> GetIncidentDetailAsync(
        Guid incidentId, 
        Guid userId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get safety dashboard data for admin interface
    /// </summary>
    Task<Result<SafetyDashboardResponse>> GetDashboardDataAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search and filter incidents for admin management
    /// </summary>
    Task<Result<PagedResponse<IncidentSummaryResponse>>> SearchIncidentsAsync(
        SearchIncidentsRequest request, 
        Guid userId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update incident status and assignment
    /// </summary>
    Task<Result<IncidentResponse>> UpdateIncidentAsync(
        Guid incidentId, 
        UpdateIncidentRequest request, 
        Guid userId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's own incident reports
    /// </summary>
    Task<Result<IEnumerable<IncidentSummaryResponse>>> GetUserReportsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);
}
```

### 2. SafetyService Implementation
```csharp
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Safety.Entities;
using WitchCityRope.Api.Features.Safety.Models.Requests;
using WitchCityRope.Api.Features.Safety.Models.Responses;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.Safety.Services;

/// <summary>
/// Safety incident service using direct Entity Framework access
/// Example of simplified vertical slice architecture pattern
/// </summary>
public class SafetyService : ISafetyService
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SafetyService> _logger;

    public SafetyService(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        IAuditService auditService,
        INotificationService notificationService,
        ILogger<SafetyService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _auditService = auditService;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Submit new safety incident report
    /// Direct Entity Framework operations - NO MediatR complexity
    /// </summary>
    public async Task<Result<SubmissionResponse>> SubmitIncidentAsync(
        SubmitIncidentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate unique reference number
            var referenceNumber = await GenerateReferenceNumberAsync(cancellationToken);

            // Create incident entity with encrypted sensitive data
            var incident = new SafetyIncident
            {
                ReferenceNumber = referenceNumber,
                ReporterId = request.IsAnonymous ? null : request.ReporterId,
                Severity = request.Severity,
                IncidentDate = request.IncidentDate.ToUniversalTime(),
                Location = request.Location,
                EncryptedDescription = await _encryptionService.EncryptAsync(request.Description),
                EncryptedInvolvedParties = !string.IsNullOrEmpty(request.InvolvedParties) 
                    ? await _encryptionService.EncryptAsync(request.InvolvedParties) : null,
                EncryptedWitnesses = !string.IsNullOrEmpty(request.Witnesses) 
                    ? await _encryptionService.EncryptAsync(request.Witnesses) : null,
                EncryptedContactEmail = !string.IsNullOrEmpty(request.ContactEmail) 
                    ? await _encryptionService.EncryptAsync(request.ContactEmail) : null,
                EncryptedContactPhone = !string.IsNullOrEmpty(request.ContactPhone) 
                    ? await _encryptionService.EncryptAsync(request.ContactPhone) : null,
                IsAnonymous = request.IsAnonymous,
                RequestFollowUp = request.RequestFollowUp,
                Status = IncidentStatus.New,
                CreatedBy = request.IsAnonymous ? null : request.ReporterId
            };

            // Save to database
            _context.SafetyIncidents.Add(incident);
            await _context.SaveChangesAsync(cancellationToken);

            // Log incident creation
            await _auditService.LogActionAsync(
                incident.Id, 
                request.IsAnonymous ? null : request.ReporterId, 
                "Created", 
                "Safety incident report submitted",
                cancellationToken: cancellationToken);

            // Send notifications based on severity
            await _notificationService.SendIncidentNotificationAsync(incident, cancellationToken);

            var response = new SubmissionResponse
            {
                ReferenceNumber = referenceNumber,
                TrackingUrl = $"/safety/track/{referenceNumber}",
                SubmittedAt = incident.CreatedAt
            };

            _logger.LogInformation("Safety incident submitted successfully: {ReferenceNumber}, Severity: {Severity}, Anonymous: {IsAnonymous}", 
                referenceNumber, incident.Severity, incident.IsAnonymous);

            return Result<SubmissionResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit safety incident");
            return Result<SubmissionResponse>.Failure("Failed to submit incident report", ex.Message);
        }
    }

    /// <summary>
    /// Get incident status for anonymous tracking
    /// Public endpoint accessible without authentication
    /// </summary>
    public async Task<Result<IncidentStatusResponse>> GetIncidentStatusAsync(
        string referenceNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var incident = await _context.SafetyIncidents
                .AsNoTracking()
                .Where(i => i.ReferenceNumber == referenceNumber)
                .Select(i => new IncidentStatusResponse
                {
                    ReferenceNumber = i.ReferenceNumber,
                    Status = i.Status.ToString(),
                    LastUpdated = i.UpdatedAt,
                    CanProvideMoreInfo = !i.IsAnonymous && i.RequestFollowUp
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (incident == null)
            {
                return Result<IncidentStatusResponse>.Failure("Incident not found");
            }

            return Result<IncidentStatusResponse>.Success(incident);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get incident status for reference: {ReferenceNumber}", referenceNumber);
            return Result<IncidentStatusResponse>.Failure("Failed to retrieve incident status");
        }
    }

    /// <summary>
    /// Get detailed incident for safety team with decrypted data
    /// Requires safety team authorization
    /// </summary>
    public async Task<Result<IncidentResponse>> GetIncidentDetailAsync(
        Guid incidentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify user has safety team access
            var hasAccess = await VerifySafetyTeamAccessAsync(userId, cancellationToken);
            if (!hasAccess)
            {
                return Result<IncidentResponse>.Failure("Access denied - safety team role required");
            }

            var incident = await _context.SafetyIncidents
                .Include(i => i.Reporter)
                .Include(i => i.AssignedUser)
                .Include(i => i.AuditLogs)
                    .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);

            if (incident == null)
            {
                return Result<IncidentResponse>.Failure("Incident not found");
            }

            // Decrypt sensitive data for safety team
            var response = new IncidentResponse
            {
                Id = incident.Id,
                ReferenceNumber = incident.ReferenceNumber,
                ReporterId = incident.ReporterId,
                ReporterName = incident.Reporter?.SceneName,
                Severity = incident.Severity,
                IncidentDate = incident.IncidentDate,
                ReportedAt = incident.ReportedAt,
                Location = incident.Location,
                Description = await _encryptionService.DecryptAsync(incident.EncryptedDescription),
                InvolvedParties = !string.IsNullOrEmpty(incident.EncryptedInvolvedParties) 
                    ? await _encryptionService.DecryptAsync(incident.EncryptedInvolvedParties) : null,
                Witnesses = !string.IsNullOrEmpty(incident.EncryptedWitnesses) 
                    ? await _encryptionService.DecryptAsync(incident.EncryptedWitnesses) : null,
                ContactEmail = !string.IsNullOrEmpty(incident.EncryptedContactEmail) 
                    ? await _encryptionService.DecryptAsync(incident.EncryptedContactEmail) : null,
                ContactPhone = !string.IsNullOrEmpty(incident.EncryptedContactPhone) 
                    ? await _encryptionService.DecryptAsync(incident.EncryptedContactPhone) : null,
                IsAnonymous = incident.IsAnonymous,
                RequestFollowUp = incident.RequestFollowUp,
                Status = incident.Status,
                AssignedTo = incident.AssignedTo,
                AssignedUserName = incident.AssignedUser?.SceneName,
                AuditTrail = incident.AuditLogs.Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    ActionType = a.ActionType,
                    ActionDescription = a.ActionDescription,
                    UserId = a.UserId,
                    UserName = a.User?.SceneName,
                    CreatedAt = a.CreatedAt
                }).OrderByDescending(a => a.CreatedAt).ToList(),
                CreatedAt = incident.CreatedAt,
                UpdatedAt = incident.UpdatedAt
            };

            // Log access to incident
            await _auditService.LogActionAsync(incidentId, userId, "Viewed", 
                "Incident details accessed by safety team member", cancellationToken: cancellationToken);

            return Result<IncidentResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get incident detail: {IncidentId}", incidentId);
            return Result<IncidentResponse>.Failure("Failed to retrieve incident details");
        }
    }

    /// <summary>
    /// Generate unique reference number using PostgreSQL function
    /// </summary>
    private async Task<string> GenerateReferenceNumberAsync(CancellationToken cancellationToken)
    {
        var result = await _context.Database
            .SqlQueryRaw<string>("SELECT generate_safety_reference_number()")
            .FirstAsync(cancellationToken);
        
        return result;
    }

    /// <summary>
    /// Verify user has safety team access
    /// </summary>
    private async Task<bool> VerifySafetyTeamAccessAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Check if user has SafetyTeam or Admin role
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Role })
            .FirstOrDefaultAsync(cancellationToken);

        return user?.Role == "SafetyTeam" || user?.Role == "Admin";
    }

    // Additional methods for dashboard, search, update operations...
}
```

### 3. Encryption Service
```csharp
using System.Security.Cryptography;
using System.Text;

namespace WitchCityRope.Api.Features.Safety.Services;

/// <summary>
/// AES-256 encryption service for sensitive safety data
/// </summary>
public interface IEncryptionService
{
    Task<string> EncryptAsync(string plainText);
    Task<string> DecryptAsync(string encryptedText);
}

public class EncryptionService : IEncryptionService
{
    private readonly string _encryptionKey;
    private readonly ILogger<EncryptionService> _logger;

    public EncryptionService(IConfiguration configuration, ILogger<EncryptionService> logger)
    {
        _encryptionKey = configuration["Safety:EncryptionKey"] 
            ?? throw new InvalidOperationException("Safety encryption key not configured");
        _logger = logger;
    }

    public async Task<string> EncryptAsync(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(_encryptionKey);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            await cs.WriteAsync(plainBytes, 0, plainBytes.Length);
            await cs.FlushFinalBlockAsync();

            var encrypted = ms.ToArray();
            var result = new byte[aes.IV.Length + encrypted.Length];
            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt data");
            throw new InvalidOperationException("Data encryption failed", ex);
        }
    }

    public async Task<string> DecryptAsync(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return string.Empty;

        try
        {
            var encryptedData = Convert.FromBase64String(encryptedText);

            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(_encryptionKey);

            var iv = new byte[16];
            var encrypted = new byte[encryptedData.Length - 16];
            Array.Copy(encryptedData, 0, iv, 0, 16);
            Array.Copy(encryptedData, 16, encrypted, 0, encrypted.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(encrypted);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cs);

            return await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt data");
            throw new InvalidOperationException("Data decryption failed", ex);
        }
    }
}
```

### 4. Audit Service
```csharp
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Safety.Entities;

namespace WitchCityRope.Api.Features.Safety.Services;

/// <summary>
/// Audit logging service for safety incident actions
/// </summary>
public interface IAuditService
{
    Task LogActionAsync(Guid incidentId, Guid? userId, string actionType, 
        string description, object? oldValues = null, object? newValues = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<AuditLogDto>> GetAuditTrailAsync(Guid incidentId, 
        CancellationToken cancellationToken = default);
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        ApplicationDbContext context, 
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogActionAsync(Guid incidentId, Guid? userId, string actionType, 
        string description, object? oldValues = null, object? newValues = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var auditLog = new IncidentAuditLog
            {
                IncidentId = incidentId,
                UserId = userId,
                ActionType = actionType,
                ActionDescription = description,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
                UserAgent = httpContext?.Request?.Headers["User-Agent"].ToString()
            };

            _context.IncidentAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Audit log created: {ActionType} for incident {IncidentId} by user {UserId}", 
                actionType, incidentId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for incident {IncidentId}", incidentId);
            // Don't throw - audit logging failure shouldn't break main operations
        }
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditTrailAsync(Guid incidentId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.IncidentAuditLogs
            .AsNoTracking()
            .Where(a => a.IncidentId == incidentId)
            .Include(a => a.User)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                ActionType = a.ActionType,
                ActionDescription = a.ActionDescription,
                UserId = a.UserId,
                UserName = a.User != null ? a.User.SceneName : "System",
                CreatedAt = a.CreatedAt
            })
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
```

## API Endpoint Design

### Minimal API Endpoints
```csharp
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using WitchCityRope.Api.Features.Safety.Models.Requests;
using WitchCityRope.Api.Features.Safety.Models.Responses;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Features.Safety.Validation;

namespace WitchCityRope.Api.Features.Safety.Endpoints;

/// <summary>
/// Safety incident reporting minimal API endpoints
/// Follows simplified vertical slice pattern with direct service injection
/// </summary>
public static class SafetyEndpoints
{
    /// <summary>
    /// Register safety endpoints using minimal API pattern
    /// </summary>
    public static void MapSafetyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/safety")
            .WithTags("Safety");

        // Public endpoint for incident submission (anonymous or authenticated)
        group.MapPost("/incidents", async (
            SubmitIncidentRequest request,
            ISafetyService safetyService,
            IValidator<SubmitIncidentRequest> validator,
            CancellationToken cancellationToken) =>
        {
            // Validate request
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await safetyService.SubmitIncidentAsync(request, cancellationToken);
            
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "Incident Submission Failed",
                    detail: result.Error,
                    statusCode: 400);
        })
        .WithName("SubmitIncident")
        .WithSummary("Submit safety incident report")
        .WithDescription("Submit a new safety incident report (anonymous or identified)")
        .Produces<SubmissionResponse>(200)
        .Produces(400)
        .Produces(422);

        // Public endpoint for anonymous incident tracking
        group.MapGet("/incidents/{referenceNumber}/status", async (
            string referenceNumber,
            ISafetyService safetyService,
            CancellationToken cancellationToken) =>
        {
            var result = await safetyService.GetIncidentStatusAsync(referenceNumber, cancellationToken);
            
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        })
        .WithName("GetIncidentStatus")
        .WithSummary("Get incident status for tracking")
        .WithDescription("Get current status of incident by reference number (public access)")
        .Produces<IncidentStatusResponse>(200)
        .Produces(404);

        // Safety team dashboard endpoint
        group.MapGet("/admin/dashboard", async (
            ISafetyService safetyService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var result = await safetyService.GetDashboardDataAsync(userId, cancellationToken);
            
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "Dashboard Load Failed",
                    detail: result.Error,
                    statusCode: 500);
        })
        .WithName("GetSafetyDashboard")
        .WithSummary("Get safety team dashboard data")
        .WithDescription("Get dashboard statistics and recent incidents for safety team")
        .RequireAuthorization(policy => policy.RequireRole("SafetyTeam", "Admin"))
        .Produces<SafetyDashboardResponse>(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // Safety team incident detail endpoint
        group.MapGet("/admin/incidents/{incidentId:guid}", async (
            Guid incidentId,
            ISafetyService safetyService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var result = await safetyService.GetIncidentDetailAsync(incidentId, userId, cancellationToken);
            
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "Incident Retrieval Failed",
                    detail: result.Error,
                    statusCode: result.Error.Contains("Access denied") ? 403 : 404);
        })
        .WithName("GetIncidentDetail")
        .WithSummary("Get detailed incident information")
        .WithDescription("Get full incident details with decrypted data for safety team")
        .RequireAuthorization(policy => policy.RequireRole("SafetyTeam", "Admin"))
        .Produces<IncidentResponse>(200)
        .Produces(401)
        .Produces(403)
        .Produces(404);

        // Update incident status/assignment endpoint
        group.MapPatch("/admin/incidents/{incidentId:guid}", async (
            Guid incidentId,
            UpdateIncidentRequest request,
            ISafetyService safetyService,
            IValidator<UpdateIncidentRequest> validator,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            // Validate request
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var result = await safetyService.UpdateIncidentAsync(incidentId, request, userId, cancellationToken);
            
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "Incident Update Failed",
                    detail: result.Error,
                    statusCode: result.Error.Contains("Access denied") ? 403 : 400);
        })
        .WithName("UpdateIncident")
        .WithSummary("Update incident status and assignment")
        .WithDescription("Update incident status, assignment, and add notes (safety team only)")
        .RequireAuthorization(policy => policy.RequireRole("SafetyTeam", "Admin"))
        .Produces<IncidentResponse>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .Produces(422);

        // User's personal incident reports endpoint
        group.MapGet("/my-reports", async (
            ISafetyService safetyService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var result = await safetyService.GetUserReportsAsync(userId, cancellationToken);
            
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "Reports Retrieval Failed",
                    detail: result.Error,
                    statusCode: 500);
        })
        .WithName("GetUserReports")
        .WithSummary("Get user's incident reports")
        .WithDescription("Get list of incident reports submitted by current user")
        .RequireAuthorization()
        .Produces<IEnumerable<IncidentSummaryResponse>>(200)
        .Produces(401)
        .Produces(500);
    }
}
```

## Data Models

### Request Models
```csharp
using WitchCityRope.Api.Features.Safety.Entities;

namespace WitchCityRope.Api.Features.Safety.Models.Requests;

/// <summary>
/// Request model for submitting new safety incident
/// </summary>
public class SubmitIncidentRequest
{
    /// <summary>
    /// Reporter user ID (null for anonymous reports)
    /// </summary>
    public Guid? ReporterId { get; set; }

    /// <summary>
    /// Incident severity level
    /// </summary>
    public IncidentSeverity Severity { get; set; }

    /// <summary>
    /// When the incident occurred
    /// </summary>
    public DateTime IncidentDate { get; set; }

    /// <summary>
    /// Location where incident occurred
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the incident
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Information about involved parties (optional)
    /// </summary>
    public string? InvolvedParties { get; set; }

    /// <summary>
    /// Witness information (optional)
    /// </summary>
    public string? Witnesses { get; set; }

    /// <summary>
    /// Whether this is an anonymous report
    /// </summary>
    public bool IsAnonymous { get; set; }

    /// <summary>
    /// Whether reporter requests follow-up contact
    /// </summary>
    public bool RequestFollowUp { get; set; }

    /// <summary>
    /// Contact email for identified reports
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Contact phone for identified reports (optional)
    /// </summary>
    public string? ContactPhone { get; set; }
}

/// <summary>
/// Request model for updating incident status and assignment
/// </summary>
public class UpdateIncidentRequest
{
    /// <summary>
    /// New incident status
    /// </summary>
    public IncidentStatus? Status { get; set; }

    /// <summary>
    /// Assign incident to safety team member
    /// </summary>
    public Guid? AssignedTo { get; set; }

    /// <summary>
    /// Safety team notes about action taken
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Request model for searching/filtering incidents
/// </summary>
public class SearchIncidentsRequest
{
    /// <summary>
    /// Filter by incident status
    /// </summary>
    public IncidentStatus? Status { get; set; }

    /// <summary>
    /// Filter by minimum severity level
    /// </summary>
    public IncidentSeverity? MinSeverity { get; set; }

    /// <summary>
    /// Filter by assigned team member
    /// </summary>
    public Guid? AssignedTo { get; set; }

    /// <summary>
    /// Filter by date range start
    /// </summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>
    /// Filter by date range end
    /// </summary>
    public DateTime? DateTo { get; set; }

    /// <summary>
    /// Search keywords in location/description
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Page number for pagination
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size for pagination
    /// </summary>
    public int PageSize { get; set; } = 25;
}
```

### Response Models
```csharp
using WitchCityRope.Api.Features.Safety.Entities;

namespace WitchCityRope.Api.Features.Safety.Models.Responses;

/// <summary>
/// Response after successful incident submission
/// </summary>
public class SubmissionResponse
{
    /// <summary>
    /// Generated reference number for tracking
    /// </summary>
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// URL for tracking incident status
    /// </summary>
    public string TrackingUrl { get; set; } = string.Empty;

    /// <summary>
    /// When the incident was submitted
    /// </summary>
    public DateTime SubmittedAt { get; set; }
}

/// <summary>
/// Response for anonymous incident status tracking
/// </summary>
public class IncidentStatusResponse
{
    /// <summary>
    /// Incident reference number
    /// </summary>
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Current status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Whether reporter can provide additional information
    /// </summary>
    public bool CanProvideMoreInfo { get; set; }
}

/// <summary>
/// Complete incident details for safety team
/// </summary>
public class IncidentResponse
{
    public Guid Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public Guid? ReporterId { get; set; }
    public string? ReporterName { get; set; }
    public IncidentSeverity Severity { get; set; }
    public DateTime IncidentDate { get; set; }
    public DateTime ReportedAt { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty; // Decrypted for safety team
    public string? InvolvedParties { get; set; } // Decrypted for safety team
    public string? Witnesses { get; set; } // Decrypted for safety team
    public string? ContactEmail { get; set; } // Decrypted for safety team
    public string? ContactPhone { get; set; } // Decrypted for safety team
    public bool IsAnonymous { get; set; }
    public bool RequestFollowUp { get; set; }
    public IncidentStatus Status { get; set; }
    public Guid? AssignedTo { get; set; }
    public string? AssignedUserName { get; set; }
    public List<AuditLogDto> AuditTrail { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Summary for incident listings
/// </summary>
public class IncidentSummaryResponse
{
    public Guid Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public IncidentSeverity Severity { get; set; }
    public DateTime IncidentDate { get; set; }
    public DateTime ReportedAt { get; set; }
    public string Location { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }
    public IncidentStatus Status { get; set; }
    public Guid? AssignedTo { get; set; }
    public string? AssignedUserName { get; set; }
}

/// <summary>
/// Safety dashboard data for admin interface
/// </summary>
public class SafetyDashboardResponse
{
    public SafetyStatistics Statistics { get; set; } = new();
    public List<IncidentSummaryResponse> RecentIncidents { get; set; } = new();
    public List<ActionItem> PendingActions { get; set; } = new();
}

public class SafetyStatistics
{
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
    public int TotalCount { get; set; }
    public int NewCount { get; set; }
    public int InProgressCount { get; set; }
    public int ResolvedCount { get; set; }
    public int ThisMonth { get; set; }
}

public class ActionItem
{
    public Guid IncidentId { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string ActionNeeded { get; set; } = string.Empty;
    public IncidentSeverity Priority { get; set; }
    public DateTime DueDate { get; set; }
}

/// <summary>
/// Audit trail entry
/// </summary>
public class AuditLogDto
{
    public Guid Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string ActionDescription { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Paginated response wrapper
/// </summary>
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
```

## Validation Rules

### FluentValidation Implementation
```csharp
using FluentValidation;
using WitchCityRope.Api.Features.Safety.Models.Requests;

namespace WitchCityRope.Api.Features.Safety.Validation;

/// <summary>
/// Validation rules for incident submission
/// </summary>
public class SubmitIncidentValidator : AbstractValidator<SubmitIncidentRequest>
{
    public SubmitIncidentValidator()
    {
        RuleFor(x => x.Severity)
            .IsInEnum()
            .WithMessage("Severity level must be Low, Medium, High, or Critical");

        RuleFor(x => x.IncidentDate)
            .NotEmpty()
            .WithMessage("Incident date is required")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Incident date cannot be more than 1 day in the future")
            .GreaterThan(DateTime.UtcNow.AddYears(-2))
            .WithMessage("Incident date cannot be more than 2 years in the past");

        RuleFor(x => x.Location)
            .NotEmpty()
            .WithMessage("Location is required")
            .Length(5, 200)
            .WithMessage("Location must be between 5 and 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Incident description is required")
            .Length(50, 5000)
            .WithMessage("Description must be between 50 and 5000 characters");

        RuleFor(x => x.InvolvedParties)
            .MaximumLength(2000)
            .WithMessage("Involved parties description cannot exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.InvolvedParties));

        RuleFor(x => x.Witnesses)
            .MaximumLength(2000)
            .WithMessage("Witness information cannot exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.Witnesses));

        // Contact validation for identified reports
        When(x => !x.IsAnonymous, () =>
        {
            RuleFor(x => x.ContactEmail)
                .NotEmpty()
                .WithMessage("Contact email is required for identified reports")
                .EmailAddress()
                .WithMessage("Valid email address is required");
        });

        RuleFor(x => x.ContactPhone)
            .Matches(@"^[\d\s\-\(\)\+\.]+$")
            .WithMessage("Phone number contains invalid characters")
            .Length(10, 20)
            .WithMessage("Phone number must be between 10 and 20 characters")
            .When(x => !string.IsNullOrEmpty(x.ContactPhone));

        // Business rule: Anonymous reports cannot request follow-up
        RuleFor(x => x.RequestFollowUp)
            .Equal(false)
            .WithMessage("Anonymous reports cannot request follow-up contact")
            .When(x => x.IsAnonymous);
    }
}

/// <summary>
/// Validation rules for incident updates
/// </summary>
public class UpdateIncidentValidator : AbstractValidator<UpdateIncidentRequest>
{
    public UpdateIncidentValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Status must be New, InProgress, Resolved, or Archived")
            .When(x => x.Status.HasValue);

        RuleFor(x => x.Notes)
            .NotEmpty()
            .WithMessage("Notes are required when updating an incident")
            .Length(10, 2000)
            .WithMessage("Notes must be between 10 and 2000 characters");

        // Must provide at least one update
        RuleFor(x => x)
            .Must(x => x.Status.HasValue || x.AssignedTo.HasValue || !string.IsNullOrEmpty(x.Notes))
            .WithMessage("At least one field must be updated (Status, AssignedTo, or Notes)");
    }
}

/// <summary>
/// Validation rules for incident search
/// </summary>
public class SearchIncidentsValidator : AbstractValidator<SearchIncidentsRequest>
{
    public SearchIncidentsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Status must be New, InProgress, Resolved, or Archived")
            .When(x => x.Status.HasValue);

        RuleFor(x => x.MinSeverity)
            .IsInEnum()
            .WithMessage("Severity must be Low, Medium, High, or Critical")
            .When(x => x.MinSeverity.HasValue);

        RuleFor(x => x.DateFrom)
            .LessThanOrEqualTo(x => x.DateTo)
            .WithMessage("Date from must be less than or equal to date to")
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue);

        RuleFor(x => x.SearchText)
            .MaximumLength(100)
            .WithMessage("Search text cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.SearchText));
    }
}
```

## Database Integration

### Entity Configuration
The Safety entities integrate with the existing ApplicationDbContext following established patterns. Database design and migration details are covered in the [Safety System Database Design](./safety-system-database-design.md) document.

Key integration points:
- **Foreign Keys**: References to existing ApplicationUser table
- **Constraints**: PostgreSQL check constraints with proper naming
- **Indexes**: Performance-optimized for safety team queries
- **Audit Fields**: Standard CreatedAt/UpdatedAt patterns
- **Encryption**: Field-level encryption for sensitive data

## Error Handling Strategy

### Result Pattern Implementation
Following the established Result<T> pattern for consistent error handling:

```csharp
// Service layer error handling
public async Task<Result<SubmissionResponse>> SubmitIncidentAsync(...)
{
    try
    {
        // Business logic
        return Result<SubmissionResponse>.Success(response);
    }
    catch (ValidationException ex)
    {
        return Result<SubmissionResponse>.Failure("Validation failed", ex.Message);
    }
    catch (UnauthorizedAccessException ex)
    {
        return Result<SubmissionResponse>.Failure("Access denied", ex.Message);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error in incident submission");
        return Result<SubmissionResponse>.Failure("Operation failed", "An unexpected error occurred");
    }
}

// Endpoint error handling
var result = await safetyService.SubmitIncidentAsync(request, cancellationToken);

return result.IsSuccess
    ? Results.Ok(result.Value)
    : Results.Problem(
        title: "Incident Submission Failed",
        detail: result.Error,
        statusCode: DetermineStatusCode(result.Error));
```

### Error Categories
- **Validation Errors (400)**: Invalid input data, business rule violations
- **Authentication Errors (401)**: Missing or invalid authentication
- **Authorization Errors (403)**: Insufficient permissions for safety team features
- **Not Found Errors (404)**: Invalid incident ID or reference number
- **System Errors (500)**: Database failures, encryption errors, external service failures

## Performance Optimization

### Query Optimization Patterns
```csharp
// ✅ CORRECT - Uses composite index for dashboard queries
public async Task<SafetyDashboardResponse> GetDashboardDataAsync(...)
{
    var recentIncidents = await _context.SafetyIncidents
        .AsNoTracking()
        .Where(i => i.Status != IncidentStatus.Archived)
        .OrderByDescending(i => i.ReportedAt)
        .ThenByDescending(i => i.Severity)
        .Take(10)
        .Select(i => new IncidentSummaryResponse
        {
            Id = i.Id,
            ReferenceNumber = i.ReferenceNumber,
            Severity = i.Severity,
            Status = i.Status,
            ReportedAt = i.ReportedAt,
            Location = i.Location,
            IsAnonymous = i.IsAnonymous
        })
        .ToListAsync(cancellationToken);

    // Use parallel queries for statistics
    var statistics = await GetStatisticsAsync(cancellationToken);
    
    return new SafetyDashboardResponse
    {
        Statistics = statistics,
        RecentIncidents = recentIncidents,
        PendingActions = await GetPendingActionsAsync(cancellationToken)
    };
}

// ✅ CORRECT - Efficient search with proper indexing
public async Task<PagedResponse<IncidentSummaryResponse>> SearchIncidentsAsync(...)
{
    var query = _context.SafetyIncidents.AsNoTracking();

    // Apply filters in order of selectivity (uses composite index)
    if (request.Status.HasValue)
        query = query.Where(i => i.Status == request.Status.Value);
        
    if (request.MinSeverity.HasValue)
        query = query.Where(i => i.Severity >= request.MinSeverity.Value);

    if (request.DateFrom.HasValue)
        query = query.Where(i => i.ReportedAt >= request.DateFrom.Value);

    if (request.DateTo.HasValue)
        query = query.Where(i => i.ReportedAt <= request.DateTo.Value);

    // Text search on indexed location field
    if (!string.IsNullOrEmpty(request.SearchText))
        query = query.Where(i => i.Location.Contains(request.SearchText));

    // Use composite index for ordering
    query = query.OrderByDescending(i => i.ReportedAt)
                 .ThenBy(i => i.Id); // Tie-breaker for consistent pagination

    var totalCount = await query.CountAsync(cancellationToken);
    var items = await query
        .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(i => new IncidentSummaryResponse { /* projection */ })
        .ToListAsync(cancellationToken);

    return new PagedResponse<IncidentSummaryResponse>
    {
        Items = items,
        TotalCount = totalCount,
        Page = request.Page,
        PageSize = request.PageSize
    };
}
```

### Caching Strategy
```csharp
// Cache reference number sequence for performance
private readonly IMemoryCache _cache;

private async Task<string> GenerateReferenceNumberAsync(CancellationToken cancellationToken)
{
    // Cache sequence values to reduce database calls
    var cacheKey = $"safety_sequence_{DateTime.UtcNow:yyyyMMdd}";
    
    if (!_cache.TryGetValue(cacheKey, out int sequenceValue))
    {
        sequenceValue = await GetNextSequenceValueAsync(cancellationToken);
        _cache.Set(cacheKey, sequenceValue, TimeSpan.FromHours(1));
    }

    return $"SAF-{DateTime.UtcNow:yyyyMMdd}-{sequenceValue:D4}";
}

// Cache safety team members for dropdown population
public async Task<IEnumerable<UserOption>> GetSafetyTeamMembersAsync(CancellationToken cancellationToken)
{
    const string cacheKey = "safety_team_members";
    
    if (_cache.TryGetValue(cacheKey, out IEnumerable<UserOption>? cached))
        return cached ?? Enumerable.Empty<UserOption>();

    var members = await _context.Users
        .AsNoTracking()
        .Where(u => u.Role == "SafetyTeam" || u.Role == "Admin")
        .Select(u => new UserOption { Id = u.Id, Name = u.SceneName })
        .ToListAsync(cancellationToken);

    _cache.Set(cacheKey, members, TimeSpan.FromMinutes(30));
    return members;
}
```

## Security Implementation

### Data Protection
```csharp
// Encryption service registration with configuration
builder.Services.Configure<EncryptionOptions>(
    builder.Configuration.GetSection("Safety:Encryption"));

builder.Services.AddSingleton<IEncryptionService, EncryptionService>();

// Audit logging with IP address exclusion for anonymous reports
public async Task LogActionAsync(Guid incidentId, Guid? userId, string actionType, 
    string description, object? oldValues = null, object? newValues = null,
    CancellationToken cancellationToken = default)
{
    var httpContext = _httpContextAccessor.HttpContext;
    
    // Get incident to check if anonymous
    var incident = await _context.SafetyIncidents
        .AsNoTracking()
        .Where(i => i.Id == incidentId)
        .Select(i => new { i.IsAnonymous })
        .FirstOrDefaultAsync(cancellationToken);

    var auditLog = new IncidentAuditLog
    {
        IncidentId = incidentId,
        UserId = userId,
        ActionType = actionType,
        ActionDescription = description,
        OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
        NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
        // CRITICAL: Don't log IP for anonymous reports
        IpAddress = incident?.IsAnonymous == true ? null : httpContext?.Connection?.RemoteIpAddress?.ToString(),
        UserAgent = httpContext?.Request?.Headers["User-Agent"].ToString()
    };

    _context.IncidentAuditLogs.Add(auditLog);
    await _context.SaveChangesAsync(cancellationToken);
}
```

### Authorization Policies
```csharp
// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SafetyTeam", policy =>
        policy.RequireRole("SafetyTeam", "Admin"));
        
    options.AddPolicy("SafetyAdmin", policy =>
        policy.RequireRole("Admin"));
});

// Role checking in service layer
private async Task<bool> VerifySafetyTeamAccessAsync(Guid userId, CancellationToken cancellationToken)
{
    var userRole = await _context.Users
        .AsNoTracking()
        .Where(u => u.Id == userId)
        .Select(u => u.Role)
        .FirstOrDefaultAsync(cancellationToken);

    return userRole == "SafetyTeam" || userRole == "Admin";
}
```

## Testing Strategy

### Unit Tests
```csharp
[TestClass]
public class SafetyServiceTests
{
    private readonly Mock<ApplicationDbContext> _mockContext;
    private readonly Mock<IEncryptionService> _mockEncryption;
    private readonly Mock<IAuditService> _mockAudit;
    private readonly Mock<INotificationService> _mockNotification;
    private readonly SafetyService _service;

    [TestMethod]
    public async Task SubmitIncidentAsync_ValidAnonymousReport_ReturnsSuccess()
    {
        // Arrange
        var request = new SubmitIncidentRequest
        {
            IsAnonymous = true,
            Severity = IncidentSeverity.High,
            IncidentDate = DateTime.UtcNow.AddHours(-2),
            Location = "Test Location",
            Description = "Test incident description of sufficient length to meet validation requirements"
        };

        _mockEncryption.Setup(x => x.EncryptAsync(It.IsAny<string>()))
            .ReturnsAsync("encrypted_data");

        // Act
        var result = await _service.SubmitIncidentAsync(request);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Value.ReferenceNumber.StartsWith("SAF-"));
        
        _mockEncryption.Verify(x => x.EncryptAsync(request.Description), Times.Once);
        _mockAudit.Verify(x => x.LogActionAsync(It.IsAny<Guid>(), null, "Created", 
            It.IsAny<string>(), null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task GetIncidentDetailAsync_SafetyTeamUser_ReturnsDecryptedData()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var incident = new SafetyIncident
        {
            Id = incidentId,
            ReferenceNumber = "SAF-20250912-0001",
            EncryptedDescription = "encrypted_description",
            Severity = IncidentSeverity.Critical,
            Status = IncidentStatus.New
        };

        _mockContext.Setup(x => x.SafetyIncidents)
            .ReturnsDbSet(new[] { incident });

        _mockEncryption.Setup(x => x.DecryptAsync("encrypted_description"))
            .ReturnsAsync("Decrypted incident description");

        // Setup user role verification
        SetupSafetyTeamUser(userId);

        // Act
        var result = await _service.GetIncidentDetailAsync(incidentId, userId);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Decrypted incident description", result.Value.Description);
        
        _mockEncryption.Verify(x => x.DecryptAsync("encrypted_description"), Times.Once);
        _mockAudit.Verify(x => x.LogActionAsync(incidentId, userId, "Viewed", 
            It.IsAny<string>(), null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task GetIncidentDetailAsync_NonSafetyTeamUser_ReturnsAccessDenied()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        SetupRegularUser(userId);

        // Act
        var result = await _service.GetIncidentDetailAsync(incidentId, userId);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.Error.Contains("Access denied"));
    }

    private void SetupSafetyTeamUser(Guid userId)
    {
        var user = new ApplicationUser { Id = userId, Role = "SafetyTeam" };
        _mockContext.Setup(x => x.Users)
            .ReturnsDbSet(new[] { user });
    }

    private void SetupRegularUser(Guid userId)
    {
        var user = new ApplicationUser { Id = userId, Role = "Member" };
        _mockContext.Setup(x => x.Users)
            .ReturnsDbSet(new[] { user });
    }
}
```

### Integration Tests
```csharp
[Collection("PostgreSQL Integration Tests")]
public class SafetySystemIntegrationTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;
    private readonly ApplicationDbContext _context;
    private readonly SafetyService _safetyService;

    public SafetySystemIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
        _context = _fixture.CreateContext();
        
        // Setup test services
        var encryptionService = new EncryptionService(
            _fixture.Configuration, 
            Mock.Of<ILogger<EncryptionService>>());
        var auditService = new AuditService(
            _context, 
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<ILogger<AuditService>>());
        var notificationService = Mock.Of<INotificationService>();

        _safetyService = new SafetyService(
            _context,
            encryptionService,
            auditService,
            notificationService,
            Mock.Of<ILogger<SafetyService>>());
    }

    [Fact]
    public async Task SubmitIncident_EndToEndFlow_CreatesIncidentWithAuditTrail()
    {
        // Arrange
        var request = new SubmitIncidentRequest
        {
            IsAnonymous = false,
            ReporterId = _fixture.TestUserId,
            Severity = IncidentSeverity.High,
            IncidentDate = DateTime.UtcNow.AddHours(-1),
            Location = "Integration Test Location",
            Description = "Integration test incident description with sufficient length for validation",
            ContactEmail = "test@example.com",
            RequestFollowUp = true
        };

        // Act
        var result = await _safetyService.SubmitIncidentAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.ReferenceNumber.StartsWith("SAF-"));

        // Verify database state
        var incident = await _context.SafetyIncidents
            .Include(i => i.AuditLogs)
            .FirstAsync(i => i.ReferenceNumber == result.Value.ReferenceNumber);

        Assert.Equal(request.Severity, incident.Severity);
        Assert.Equal(request.Location, incident.Location);
        Assert.False(incident.IsAnonymous);
        Assert.True(incident.EncryptedDescription.Length > 0);
        Assert.True(incident.EncryptedContactEmail.Length > 0);
        Assert.Single(incident.AuditLogs);
        Assert.Equal("Created", incident.AuditLogs.First().ActionType);
    }

    [Fact]
    public async Task GetIncidentDetail_WithEncryptedData_ReturnsDecryptedContent()
    {
        // Arrange
        var incident = await CreateTestIncidentAsync();
        var safetyTeamUser = await CreateSafetyTeamUserAsync();

        // Act
        var result = await _safetyService.GetIncidentDetailAsync(incident.Id, safetyTeamUser.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Test incident description", result.Value.Description);
        Assert.Equal("test@example.com", result.Value.ContactEmail);
        
        // Verify audit log was created for access
        var auditLogs = await _context.IncidentAuditLogs
            .Where(a => a.IncidentId == incident.Id && a.ActionType == "Viewed")
            .ToListAsync();
        
        Assert.Single(auditLogs);
        Assert.Equal(safetyTeamUser.Id, auditLogs.First().UserId);
    }

    [Fact]
    public async Task SearchIncidents_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        await CreateMultipleTestIncidentsAsync();
        var safetyTeamUser = await CreateSafetyTeamUserAsync();
        
        var searchRequest = new SearchIncidentsRequest
        {
            Status = IncidentStatus.New,
            MinSeverity = IncidentSeverity.High,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _safetyService.SearchIncidentsAsync(searchRequest, safetyTeamUser.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Items.All(i => i.Status == IncidentStatus.New));
        Assert.True(result.Value.Items.All(i => i.Severity >= IncidentSeverity.High));
        Assert.True(result.Value.TotalCount > 0);
    }

    private async Task<SafetyIncident> CreateTestIncidentAsync()
    {
        var encryptionService = new EncryptionService(_fixture.Configuration, Mock.Of<ILogger<EncryptionService>>());
        
        var incident = new SafetyIncident
        {
            ReferenceNumber = "SAF-TEST-0001",
            Severity = IncidentSeverity.Medium,
            IncidentDate = DateTime.UtcNow.AddHours(-2),
            Location = "Test Location",
            EncryptedDescription = await encryptionService.EncryptAsync("Test incident description"),
            EncryptedContactEmail = await encryptionService.EncryptAsync("test@example.com"),
            IsAnonymous = false,
            Status = IncidentStatus.New
        };

        _context.SafetyIncidents.Add(incident);
        await _context.SaveChangesAsync();
        
        return incident;
    }

    private async Task<ApplicationUser> CreateSafetyTeamUserAsync()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "safety@test.com",
            Email = "safety@test.com",
            SceneName = "SafetyTeamMember",
            Role = "SafetyTeam",
            EmailConfirmed = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        return user;
    }
}
```

### API Endpoint Tests
```csharp
[Collection("API Integration Tests")]
public class SafetyEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SafetyEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task SubmitIncident_ValidAnonymousRequest_ReturnsSuccessWithTrackingNumber()
    {
        // Arrange
        var request = new SubmitIncidentRequest
        {
            IsAnonymous = true,
            Severity = IncidentSeverity.High,
            IncidentDate = DateTime.UtcNow.AddHours(-1),
            Location = "API Test Location",
            Description = "API test incident description with sufficient length for validation requirements"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/safety/incidents", request);

        // Assert
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<SubmissionResponse>();
        Assert.NotNull(result);
        Assert.True(result.ReferenceNumber.StartsWith("SAF-"));
        Assert.True(result.TrackingUrl.Contains(result.ReferenceNumber));
    }

    [Fact]
    public async Task GetIncidentStatus_ValidReferenceNumber_ReturnsStatus()
    {
        // Arrange
        var incident = await CreateTestIncidentViaApiAsync();

        // Act
        var response = await _client.GetAsync($"/api/safety/incidents/{incident.ReferenceNumber}/status");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<IncidentStatusResponse>();
        Assert.NotNull(result);
        Assert.Equal(incident.ReferenceNumber, result.ReferenceNumber);
        Assert.Equal("New", result.Status);
    }

    [Fact]
    public async Task GetIncidentDetail_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var incidentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/safety/admin/incidents/{incidentId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetIncidentDetail_WithSafetyTeamAuth_ReturnsIncidentDetails()
    {
        // Arrange
        var safetyTeamToken = await GetSafetyTeamTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", safetyTeamToken);
        
        var incident = await CreateTestIncidentViaApiAsync();

        // Act
        var response = await _client.GetAsync($"/api/safety/admin/incidents/{incident.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<IncidentResponse>();
        Assert.NotNull(result);
        Assert.Equal(incident.Id, result.Id);
        Assert.NotNull(result.Description); // Should be decrypted
    }

    [Fact]
    public async Task UpdateIncident_WithValidUpdate_ReturnsUpdatedIncident()
    {
        // Arrange
        var safetyTeamToken = await GetSafetyTeamTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", safetyTeamToken);
        
        var incident = await CreateTestIncidentViaApiAsync();
        var updateRequest = new UpdateIncidentRequest
        {
            Status = IncidentStatus.InProgress,
            Notes = "Investigation started - reviewing evidence and witness statements"
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/safety/admin/incidents/{incident.Id}", updateRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<IncidentResponse>();
        Assert.NotNull(result);
        Assert.Equal(IncidentStatus.InProgress, result.Status);
    }

    [Fact]
    public async Task SubmitIncident_InvalidData_ReturnsValidationErrors()
    {
        // Arrange
        var invalidRequest = new SubmitIncidentRequest
        {
            // Missing required fields
            Location = "X", // Too short
            Description = "Too short" // Too short
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/safety/incidents", invalidRequest);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.True(problemDetails.Errors.ContainsKey("Location"));
        Assert.True(problemDetails.Errors.ContainsKey("Description"));
    }

    private async Task<SubmissionResponse> CreateTestIncidentViaApiAsync()
    {
        var request = new SubmitIncidentRequest
        {
            IsAnonymous = false,
            Severity = IncidentSeverity.Medium,
            IncidentDate = DateTime.UtcNow.AddHours(-1),
            Location = "Test Location for API",
            Description = "Test incident description with sufficient length for validation requirements",
            ContactEmail = "test@example.com"
        };

        var response = await _client.PostAsJsonAsync("/api/safety/incidents", request);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<SubmissionResponse>()
            ?? throw new InvalidOperationException("Failed to create test incident");
    }

    private async Task<string> GetSafetyTeamTokenAsync()
    {
        // Implementation to get authentication token for safety team user
        // This would use the existing authentication system
        var loginRequest = new LoginRequest
        {
            Email = "safety@test.com",
            Password = "TestPassword123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();
        
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return loginResponse?.Token ?? throw new InvalidOperationException("Failed to get auth token");
    }
}
```

## Service Registration

### Dependency Injection Configuration
```csharp
// SafetyServiceExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Features.Safety.Validation;
using FluentValidation;

namespace WitchCityRope.Api.Features.Safety.Extensions;

/// <summary>
/// Dependency injection registration for Safety feature
/// </summary>
public static class SafetyServiceExtensions
{
    /// <summary>
    /// Register all Safety feature services
    /// </summary>
    public static IServiceCollection AddSafetyServices(this IServiceCollection services)
    {
        // Core safety services
        services.AddScoped<ISafetyService, SafetyService>();
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INotificationService, NotificationService>();

        // Validators
        services.AddScoped<IValidator<SubmitIncidentRequest>, SubmitIncidentValidator>();
        services.AddScoped<IValidator<UpdateIncidentRequest>, UpdateIncidentValidator>();
        services.AddScoped<IValidator<SearchIncidentsRequest>, SearchIncidentsValidator>();

        // Configuration
        services.Configure<EncryptionOptions>(options =>
        {
            // Encryption configuration will be bound from appsettings
        });

        return services;
    }
}

// Program.cs registration
builder.Services.AddSafetyServices();

// Register endpoints
app.MapSafetyEndpoints();
```

## Implementation Roadmap

### Phase 1: Core Infrastructure (Week 1)
**Priority: CRITICAL - Foundation**

1. **Database Setup**
   - [ ] Create SafetyIncident, IncidentAuditLog, IncidentNotification entities
   - [ ] Generate and apply EF Core migration
   - [ ] Create PostgreSQL reference number generation function
   - [ ] Implement database seeding for test data

2. **Encryption Service**
   - [ ] Implement IEncryptionService and EncryptionService
   - [ ] Add encryption configuration to appsettings
   - [ ] Create unit tests for encryption/decryption
   - [ ] Validate performance with large text blocks

3. **Basic Service Layer**
   - [ ] Implement ISafetyService interface
   - [ ] Create basic SafetyService with incident submission
   - [ ] Implement Result<T> pattern error handling
   - [ ] Add structured logging throughout

**Deliverables:**
- Database schema deployed and tested
- Encryption service with performance validation
- Basic incident submission working end-to-end
- Unit tests achieving >90% coverage

### Phase 2: Core Business Logic (Week 2)
**Priority: HIGH - Core Features**

1. **Incident Management**
   - [ ] Complete SafetyService implementation
   - [ ] Add anonymous incident tracking
   - [ ] Implement incident detail retrieval with decryption
   - [ ] Create incident search and filtering

2. **Audit System**
   - [ ] Implement IAuditService and AuditService
   - [ ] Add comprehensive audit logging
   - [ ] Create audit trail retrieval
   - [ ] Implement IP address exclusion for anonymous reports

3. **Validation Layer**
   - [ ] Implement FluentValidation validators
   - [ ] Add business rule validation
   - [ ] Create comprehensive validation error handling
   - [ ] Test all validation scenarios

**Deliverables:**
- Complete incident lifecycle management
- Anonymous reporting with privacy protection
- Comprehensive audit trails
- Full validation with clear error messages

### Phase 3: API Endpoints (Week 3)
**Priority: HIGH - External Interface**

1. **Minimal API Implementation**
   - [ ] Create SafetyEndpoints with all required endpoints
   - [ ] Implement proper authorization policies
   - [ ] Add OpenAPI documentation
   - [ ] Configure request/response models

2. **Authentication Integration**
   - [ ] Integrate with existing cookie authentication
   - [ ] Implement role-based authorization
   - [ ] Add safety team permission checks
   - [ ] Test anonymous access patterns

3. **Error Handling**
   - [ ] Implement consistent API error responses
   - [ ] Add proper HTTP status codes
   - [ ] Create detailed error logging
   - [ ] Test error scenarios thoroughly

**Deliverables:**
- Complete API endpoint suite
- Integrated authentication and authorization
- Comprehensive error handling
- OpenAPI documentation

### Phase 4: Notification & Dashboard (Week 4)
**Priority: MEDIUM - Enhanced Features**

1. **Notification Service**
   - [ ] Implement INotificationService
   - [ ] Create email notification templates
   - [ ] Add severity-based notification rules
   - [ ] Implement notification tracking and retry logic

2. **Dashboard Features**
   - [ ] Implement safety dashboard data aggregation
   - [ ] Create incident statistics calculation
   - [ ] Add pending actions identification
   - [ ] Optimize dashboard queries for performance

3. **Search & Filtering**
   - [ ] Implement advanced incident search
   - [ ] Add pagination support
   - [ ] Create filter combinations
   - [ ] Optimize search queries with proper indexing

**Deliverables:**
- Email notification system
- Safety team dashboard
- Advanced search and filtering
- Performance optimization

### Phase 5: Testing & Security (Week 5)
**Priority: CRITICAL - Quality Assurance**

1. **Comprehensive Testing**
   - [ ] Complete unit test suite (>95% coverage)
   - [ ] Integration tests with TestContainers
   - [ ] API endpoint tests
   - [ ] Performance tests with large datasets

2. **Security Hardening**
   - [ ] Security audit of encryption implementation
   - [ ] Anonymous access verification
   - [ ] Authorization policy testing
   - [ ] Input validation security testing

3. **Performance Testing**
   - [ ] Load testing with 100+ concurrent users
   - [ ] Database performance optimization
   - [ ] Query execution plan analysis
   - [ ] Encryption performance validation

**Deliverables:**
- Comprehensive test suite
- Security audit completion
- Performance benchmarks
- Production readiness verification

### Phase 6: Documentation & Deployment (Week 6)
**Priority: HIGH - Production Preparation**

1. **Documentation**
   - [ ] API documentation completion
   - [ ] Service deployment guide
   - [ ] Security configuration guide
   - [ ] Operational runbook creation

2. **Production Deployment**
   - [ ] Environment configuration
   - [ ] Database migration deployment
   - [ ] Service monitoring setup
   - [ ] Backup and recovery procedures

3. **Training Materials**
   - [ ] Safety team user guide
   - [ ] Admin interface documentation
   - [ ] Incident management procedures
   - [ ] Emergency response protocols

**Deliverables:**
- Complete documentation suite
- Production deployment
- Monitoring and alerting
- Training materials

## Risk Mitigation

### High-Risk Areas

1. **Data Security**
   - **Risk**: Encryption key compromise or weak encryption
   - **Mitigation**: Use AES-256 with secure key management, regular key rotation
   - **Monitoring**: Encryption failure alerts, audit access to encrypted data

2. **Anonymous Privacy**
   - **Risk**: Accidental linking of anonymous reports to users
   - **Mitigation**: Strict IP address exclusion, no session tracking for anonymous reports
   - **Validation**: Regular privacy audit, anonymization verification

3. **Performance Impact**
   - **Risk**: Encryption/decryption causing performance degradation
   - **Mitigation**: Async encryption, query optimization, strategic caching
   - **Monitoring**: Response time metrics, database performance tracking

4. **Legal Compliance**
   - **Risk**: Inadequate audit trails or data retention
   - **Mitigation**: Permanent data retention, comprehensive audit logging
   - **Validation**: Legal compliance review, audit trail completeness testing

### Contingency Plans

1. **Encryption Service Failure**
   - **Immediate**: Disable incident submission, alert safety team
   - **Backup**: Manual process for critical incidents
   - **Recovery**: Rollback to last known good configuration

2. **Database Performance Issues**
   - **Immediate**: Enable query caching, disable non-critical features
   - **Backup**: Read-only mode for tracking, manual incident management
   - **Recovery**: Database optimization, index rebuilding

3. **Authentication System Failure**
   - **Immediate**: Maintain anonymous reporting, disable admin features
   - **Backup**: Emergency safety team access via alternate authentication
   - **Recovery**: Coordinate with authentication service team

## Quality Gates

### Functional Requirements
- [ ] Anonymous incident reporting fully functional
- [ ] Reference number tracking system working
- [ ] Safety team dashboard operational
- [ ] Email notifications sent per severity rules
- [ ] Status workflow enforced correctly
- [ ] Data encryption/decryption working
- [ ] Audit trail logging all actions
- [ ] Role-based access control implemented

### Security Requirements
- [ ] Sensitive data encrypted at rest
- [ ] Anonymous protection verified (no IP logging)
- [ ] No unauthorized access to encrypted data
- [ ] SQL injection protection tested
- [ ] XSS protection implemented
- [ ] Input validation comprehensive
- [ ] Authorization policies enforced

### Performance Requirements
- [ ] Incident submission <2 seconds
- [ ] Dashboard loads <1 second
- [ ] Email notifications sent <30 seconds
- [ ] Database queries optimized (execution plans reviewed)
- [ ] 100 concurrent users supported
- [ ] Search operations <3 seconds

### Legal Compliance Requirements
- [ ] Data retention policies implemented (permanent for incidents)
- [ ] Privacy protection mechanisms tested
- [ ] Audit trail completeness verified
- [ ] Access control authorization tested
- [ ] Community safety standards met
- [ ] Legal review completed

## Success Metrics

### Technical Metrics
- **Response Time**: 95% of API calls under 1 second
- **Availability**: 99.9% uptime for incident submission
- **Security**: Zero data breaches or privacy violations
- **Performance**: Support 100 concurrent incident submissions

### Business Metrics
- **Adoption**: 90% of incidents reported through system within 3 months
- **Efficiency**: Safety team response time <2 hours for critical incidents
- **Compliance**: 100% of incidents have complete audit trails
- **User Satisfaction**: >95% successful form submissions

### Operational Metrics
- **Monitoring**: 100% of security events logged and alerted
- **Recovery**: <15 minutes recovery time for service failures
- **Maintenance**: <2 hours downtime per month for updates
- **Documentation**: 100% of features documented with examples

This comprehensive technical design provides the complete architecture and implementation roadmap for the Safety System, ensuring legal compliance, community safety, and operational excellence while maintaining the highest standards for privacy and security.