# Vetting System Technical Architecture
<!-- Created: 2025-09-13 -->
<!-- Version: 1.0 -->
<!-- Owner: Backend Developer Agent -->
<!-- Status: Complete Technical Architecture -->

## Executive Summary

This document provides the comprehensive technical architecture for the WitchCityRope Vetting System implementation using Vertical Slice Architecture patterns. The design emphasizes privacy-first security, scalable performance, and maintainable code structure while aligning with established WitchCityRope development standards.

## System Architecture Overview

### Component Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          React Frontend (Vite)                                   │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐                 │
│  │ Application     │  │ Review          │  │ Status          │                 │
│  │ Form            │  │ Dashboard       │  │ Tracking        │                 │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘                 │
└─────────────────────────────────────────────────────────────────────────────────┘
                                    ↓ HTTPS/Cookie Auth
┌─────────────────────────────────────────────────────────────────────────────────┐
│                            .NET 9 Minimal API                                    │
│  ┌─────────────────────────────────────────────────────────────────────────────┐ │
│  │                    /apps/api/Features/Vetting/                               │ │
│  │                                                                             │ │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐           │ │
│  │  │ Endpoints/      │  │ Services/       │  │ Models/         │           │ │
│  │  │ - Application   │  │ - VettingService│  │ - Request/      │           │ │
│  │  │ - Reference     │  │ - ReferenceServ │  │   Response DTOs │           │ │
│  │  │ - Review        │  │ - ReviewerServ  │  │ - Entities      │           │ │
│  │  │ - Status        │  │ - NotificationS │  │ - Validations   │           │ │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────┘           │ │
│  │                                                                             │ │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐           │ │
│  │  │ Security/       │  │ Validation/     │  │ Extensions/     │           │ │
│  │  │ - Encryption    │  │ - FluentValid   │  │ - Service       │           │ │
│  │  │ - Auth Guards   │  │ - Validators    │  │   Registration  │           │ │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────┘           │ │
│  └─────────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────────┘
                                    ↓ Entity Framework Core 9
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              PostgreSQL Database                                 │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐               │
│  │ VettingApplication │ VettingReviewer │  │ VettingReference│               │
│  │ (AES-256 PII)    │  │ (Role-based)    │  │ (Secure Tokens) │               │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘               │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐               │
│  │ VettingDecision │  │ Audit Logs      │  │ Notifications   │               │
│  │ (Tracked)       │  │ (Compliance)    │  │ (Email Queue)   │               │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘               │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Technology Stack

| Layer | Technology | Version | Purpose |
|-------|------------|---------|---------|
| **Frontend** | React + TypeScript + Vite | React 18, TS 5.x | User interface and interactions |
| **API** | .NET 9 Minimal API | .NET 9.0 | RESTful API endpoints |
| **Authentication** | ASP.NET Core Identity + Cookies | .NET 9.0 | Cookie-based authentication |
| **Data Layer** | Entity Framework Core | EF 9.0 | ORM and database operations |
| **Database** | PostgreSQL | 16.x | Primary data storage |
| **Caching** | IMemoryCache | .NET 9.0 | In-memory application caching |
| **Encryption** | AES-256-GCM | .NET 9.0 | PII encryption at rest |
| **Validation** | FluentValidation | 11.x | Input validation and business rules |
| **Logging** | ILogger + Serilog | .NET 9.0 | Structured logging and audit trails |

## Service Layer Architecture

### 1. Core Service Design

Following WitchCityRope's Vertical Slice Architecture with direct EF Core usage (no MediatR, no Repository pattern):

```csharp
namespace WitchCityRope.Api.Features.Vetting.Services;

/// <summary>
/// Main vetting application service using direct Entity Framework access
/// Implements privacy-first architecture with AES-256 encryption
/// </summary>
public class VettingService : IVettingService
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly INotificationService _notificationService;
    private readonly IValidator<CreateApplicationRequest> _createValidator;
    private readonly IValidator<ReviewDecisionRequest> _reviewValidator;
    private readonly ILogger<VettingService> _logger;
    private readonly IMemoryCache _cache;

    // Constructor and service implementations...
}
```

### 2. Service Responsibilities

#### VettingService (Primary Orchestrator)
- **Application Lifecycle**: Create, submit, update applications
- **Status Management**: Track application progress through workflow
- **Business Rules**: Enforce approval criteria and constraints
- **Data Integration**: Coordinate with user management and role assignment

#### ReferenceService
- **Reference Collection**: Manage reference contact and verification
- **Email Automation**: Send requests, reminders, and follow-ups  
- **Response Processing**: Validate and store reference feedback
- **Privacy Protection**: Maintain reference confidentiality

#### ReviewerService
- **Workload Management**: Automatic application assignment
- **Performance Tracking**: Monitor reviewer metrics and efficiency
- **Capacity Planning**: Balance assignments across team members
- **Specialization Matching**: Route applications based on expertise

#### EncryptionService
- **Data Protection**: AES-256-GCM encryption for PII fields
- **Key Management**: Secure key rotation and storage
- **Audit Compliance**: Encryption operation logging
- **Performance Optimization**: Batched encryption/decryption

#### NotificationService Integration
- **Email Delivery**: Template-based notification system
- **Delivery Tracking**: Monitor bounce rates and engagement
- **Escalation Management**: Handle failed deliveries
- **Compliance Tracking**: Audit all communications

### 3. Result Pattern Implementation

All service methods return `Result<T>` for consistent error handling:

```csharp
/// <summary>
/// Submit new vetting application with comprehensive validation
/// </summary>
public async Task<Result<ApplicationSubmissionResponse>> SubmitApplicationAsync(
    CreateApplicationRequest request,
    CancellationToken cancellationToken = default)
{
    try
    {
        // Input validation
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<ApplicationSubmissionResponse>.Failure(
                validationResult.Errors.First().ErrorMessage);
        }

        // Check for duplicate scene name
        var existingApplication = await _context.VettingApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => 
                EF.Functions.ILike(a.EncryptedSceneName, await _encryptionService.EncryptAsync(request.SceneName)), 
                cancellationToken);

        if (existingApplication != null)
        {
            return Result<ApplicationSubmissionResponse>.Failure(
                "Scene name already in use by another application");
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        // Create application with encrypted PII
        var application = new VettingApplication
        {
            ApplicationNumber = await GenerateApplicationNumberAsync(cancellationToken),
            EncryptedFullName = await _encryptionService.EncryptAsync(request.FullName),
            EncryptedSceneName = await _encryptionService.EncryptAsync(request.SceneName),
            EncryptedEmail = await _encryptionService.EncryptAsync(request.Email),
            // ... other encrypted fields
            Status = ApplicationStatus.Submitted,
            IsAnonymous = request.IsAnonymous,
            Priority = DeterminePriority(request)
        };

        _context.VettingApplications.Add(application);

        // Create references with secure tokens
        foreach (var refRequest in request.References)
        {
            var reference = new VettingReference
            {
                ApplicationId = application.Id,
                EncryptedName = await _encryptionService.EncryptAsync(refRequest.Name),
                EncryptedEmail = await _encryptionService.EncryptAsync(refRequest.Email),
                EncryptedRelationship = await _encryptionService.EncryptAsync(refRequest.Relationship),
                ResponseToken = GenerateSecureToken()
            };
            _context.VettingReferences.Add(reference);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Send confirmation email
        await _notificationService.SendApplicationConfirmationAsync(
            application.Id, 
            request.Email, 
            application.StatusToken,
            cancellationToken);

        // Schedule reference contacts
        await _notificationService.ScheduleReferenceContactsAsync(
            application.Id,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        var response = new ApplicationSubmissionResponse
        {
            ApplicationId = application.Id,
            ApplicationNumber = application.ApplicationNumber,
            StatusTrackingUrl = $"/vetting/status/{application.StatusToken}",
            SubmittedAt = application.CreatedAt
        };

        _logger.LogInformation(
            "Vetting application submitted: {ApplicationNumber}, Anonymous: {IsAnonymous}",
            application.ApplicationNumber, application.IsAnonymous);

        return Result<ApplicationSubmissionResponse>.Success(response);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to submit vetting application");
        return Result<ApplicationSubmissionResponse>.Failure(
            "Unable to submit application at this time");
    }
}
```

## API Endpoint Architecture

### 1. Endpoint Structure

Using .NET 9 Minimal API patterns with endpoint groups:

```csharp
namespace WitchCityRope.Api.Features.Vetting.Endpoints;

public static class VettingEndpoints
{
    public static IEndpointRouteBuilder MapVettingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/vetting")
            .WithTags("Vetting")
            .WithOpenApi();

        // Public endpoints (no authentication required)
        group.MapPost("/applications", SubmitApplication)
            .AllowAnonymous()
            .WithSummary("Submit new vetting application")
            .WithDescription("Public endpoint for community members to submit vetting applications");

        group.MapGet("/status/{token}", GetApplicationStatus)
            .AllowAnonymous()
            .WithSummary("Check application status")
            .WithDescription("Public status tracking using secure token from confirmation email");

        // Reference endpoints (secure token based)
        group.MapGet("/references/{token}/form", GetReferenceForm)
            .AllowAnonymous()
            .WithSummary("Display reference form")
            .WithDescription("Secure reference form access via email token");

        group.MapPost("/references/{token}/response", SubmitReferenceResponse)
            .AllowAnonymous()
            .WithSummary("Submit reference response")
            .WithDescription("Reference submission with validation and audit trail");

        // Reviewer endpoints (authentication required)
        group.MapGet("/applications", GetApplicationsForReview)
            .RequireAuthorization("VettingReviewer")
            .WithSummary("Get applications for review")
            .WithDescription("Paginated list of applications with filtering");

        group.MapGet("/applications/{id}", GetApplicationDetail)
            .RequireAuthorization("VettingReviewer")
            .WithSummary("Get application details")
            .WithDescription("Full application details with decrypted data for reviewers");

        group.MapPost("/applications/{id}/decision", SubmitReviewDecision)
            .RequireAuthorization("VettingReviewer")
            .WithSummary("Submit review decision")
            .WithDescription("Approve, deny, or request additional information");

        // Admin endpoints (elevated permissions)
        group.MapGet("/analytics/dashboard", GetAnalyticsDashboard)
            .RequireAuthorization("VettingAdmin")
            .WithSummary("Get analytics dashboard")
            .WithDescription("Comprehensive vetting system metrics and trends");

        group.MapPost("/notifications/send", SendManualNotification)
            .RequireAuthorization("VettingAdmin")
            .WithSummary("Send manual notification")
            .WithDescription("Send custom notifications to applicants or references");

        return app;
    }

    private static async Task<IResult> SubmitApplication(
        CreateApplicationRequest request,
        IVettingService vettingService,
        CancellationToken cancellationToken)
    {
        var result = await vettingService.SubmitApplicationAsync(request, cancellationToken);
        return result.IsSuccess 
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> GetApplicationsForReview(
        IVettingService vettingService,
        HttpContext context,
        string? status = null,
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var reviewerId = GetUserIdFromContext(context);
        if (reviewerId == null)
        {
            return Results.Unauthorized();
        }

        var filter = new ApplicationFilterRequest
        {
            Status = status,
            SearchTerm = search,
            Page = page,
            PageSize = Math.Min(pageSize, 100) // Limit page size
        };

        var result = await vettingService.GetApplicationsForReviewAsync(
            reviewerId.Value, filter, cancellationToken);

        return result.IsSuccess 
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    // Additional endpoint implementations...
}
```

### 2. Request/Response Models

#### Application Submission Models

```csharp
namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Complete application submission request with validation attributes
/// </summary>
public record CreateApplicationRequest
{
    // Personal Information
    public required string FullName { get; init; }
    public required string SceneName { get; init; }
    public string? Pronouns { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }

    // Experience Information
    public required ExperienceLevel ExperienceLevel { get; init; }
    public required int YearsExperience { get; init; }
    public required string ExperienceDescription { get; init; }
    public required string SafetyKnowledge { get; init; }
    public required string ConsentUnderstanding { get; init; }

    // Community Information
    public required string WhyJoinCommunity { get; init; }
    public required List<string> SkillsInterests { get; init; }
    public required string ExpectationsGoals { get; init; }
    public required bool AgreesToGuidelines { get; init; }

    // References
    public required List<ReferenceRequest> References { get; init; }

    // Privacy Settings
    public required bool IsAnonymous { get; init; }
    public required bool AgreesToTerms { get; init; }
    public bool ConsentToContact { get; init; } = true;
}

public record ReferenceRequest
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string Relationship { get; init; }
}

public record ApplicationSubmissionResponse
{
    public required Guid ApplicationId { get; init; }
    public required string ApplicationNumber { get; init; }
    public required string StatusTrackingUrl { get; init; }
    public required DateTime SubmittedAt { get; init; }
}
```

#### Review and Status Models

```csharp
/// <summary>
/// Application details for reviewer interface with decrypted data
/// </summary>
public record ApplicationDetailResponse
{
    public required Guid Id { get; init; }
    public required string ApplicationNumber { get; init; }
    public required ApplicationStatus Status { get; init; }
    public required DateTime SubmittedAt { get; init; }

    // Decrypted Personal Information (reviewers only)
    public required string FullName { get; init; }
    public required string SceneName { get; init; }
    public string? Pronouns { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }

    // Experience Information
    public required ExperienceLevel ExperienceLevel { get; init; }
    public required int YearsExperience { get; init; }
    public required string ExperienceDescription { get; init; }
    public required string SafetyKnowledge { get; init; }
    public required string ConsentUnderstanding { get; init; }

    // Community Information
    public required string WhyJoinCommunity { get; init; }
    public required List<string> SkillsInterests { get; init; }
    public required string ExpectationsGoals { get; init; }

    // Privacy and Workflow
    public required bool IsAnonymous { get; init; }
    public required ApplicationPriority Priority { get; init; }
    public Guid? AssignedReviewerId { get; init; }
    public string? AssignedReviewerName { get; init; }

    // References with status
    public required List<ReferenceDetailResponse> References { get; init; }

    // Review History
    public required List<ReviewNoteResponse> Notes { get; init; }
    public required List<DecisionHistoryResponse> DecisionHistory { get; init; }
}

public record ReviewDecisionRequest
{
    public required DecisionType DecisionType { get; init; }
    public required string Reasoning { get; init; }
    public int? Score { get; init; } // 1-10 scoring if used
    public string? AdditionalInfoRequested { get; init; }
    public DateTime? ProposedInterviewTime { get; init; }
    public bool IsFinalDecision { get; init; }
}
```

## Security Architecture

### 1. Authentication & Authorization

#### Cookie-Based Authentication
- **HttpOnly Cookies**: Secure token storage, XSS protection
- **SameSite=Strict**: CSRF protection
- **Secure Flag**: HTTPS-only transmission
- **Rolling Expiration**: 30-day sliding window

#### Role-Based Authorization
```csharp
// Authorization policies configuration
public static class VettingAuthorizationPolicies
{
    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy("VettingReviewer", policy =>
            policy.RequireRole("VettingReviewer", "VettingAdmin", "Admin"));

        options.AddPolicy("VettingAdmin", policy =>
            policy.RequireRole("VettingAdmin", "Admin"));

        options.AddPolicy("CanAccessVettingAnalytics", policy =>
            policy.RequireRole("VettingAdmin", "Admin")
                  .RequireClaim("analytics", "read"));
    }
}
```

### 2. Data Encryption Strategy

#### AES-256-GCM Encryption Implementation
```csharp
namespace WitchCityRope.Api.Features.Vetting.Security;

/// <summary>
/// AES-256-GCM encryption service for PII protection
/// Implements key rotation and performance optimization
/// </summary>
public class VettingEncryptionService : IEncryptionService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<VettingEncryptionService> _logger;
    private readonly IMemoryCache _keyCache;

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _encryptionSemaphores = new();

    public async Task<string> EncryptAsync(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            return string.Empty;

        try
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.GCM;
            
            var key = GetEncryptionKey();
            var nonce = new byte[12]; // 96-bit nonce for GCM
            RandomNumberGenerator.Fill(nonce);

            var tag = new byte[16]; // 128-bit authentication tag
            var ciphertext = new byte[Encoding.UTF8.GetByteCount(plaintext)];

            using var encryptor = aes.CreateEncryptor(key, nonce);
            encryptor.TransformBlock(
                Encoding.UTF8.GetBytes(plaintext), 0, 
                Encoding.UTF8.GetByteCount(plaintext),
                ciphertext, 0);

            // Return Base64-encoded: nonce + tag + ciphertext
            var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encryption failed for data length: {Length}", plaintext.Length);
            throw new EncryptionException("Data encryption failed", ex);
        }
    }

    public async Task<string> DecryptAsync(string encryptedData)
    {
        if (string.IsNullOrEmpty(encryptedData))
            return string.Empty;

        try
        {
            var data = Convert.FromBase64String(encryptedData);
            
            var nonce = new byte[12];
            var tag = new byte[16];
            var ciphertext = new byte[data.Length - 28];

            Buffer.BlockCopy(data, 0, nonce, 0, 12);
            Buffer.BlockCopy(data, 12, tag, 0, 16);
            Buffer.BlockCopy(data, 28, ciphertext, 0, ciphertext.Length);

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.GCM;

            var key = GetEncryptionKey();
            var plaintext = new byte[ciphertext.Length];

            using var decryptor = aes.CreateDecryptor(key, nonce);
            decryptor.TransformBlock(ciphertext, 0, ciphertext.Length, plaintext, 0);

            // Verify authentication tag
            if (!tag.SequenceEqual(((GcmAuthenticationTag)decryptor).Tag))
            {
                throw new CryptographicException("Authentication tag verification failed");
            }

            return Encoding.UTF8.GetString(plaintext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decryption failed for data length: {Length}", encryptedData.Length);
            throw new EncryptionException("Data decryption failed", ex);
        }
    }

    private byte[] GetEncryptionKey()
    {
        const string cacheKey = "vetting_encryption_key";
        
        if (_keyCache.TryGetValue(cacheKey, out byte[]? cachedKey) && cachedKey != null)
        {
            return cachedKey;
        }

        var keyString = _configuration.GetConnectionString("EncryptionKey") 
            ?? throw new InvalidOperationException("Encryption key not configured");

        var key = Convert.FromBase64String(keyString);
        
        _keyCache.Set(cacheKey, key, TimeSpan.FromHours(1)); // Cache for 1 hour
        return key;
    }
}
```

#### Encrypted Fields Strategy
- **Personal Information**: Full name, scene name, email, phone
- **Application Content**: Experience descriptions, community understanding
- **Reference Data**: Reference names, emails, relationships, responses
- **Communication Logs**: Email content, contact attempts

### 3. Token Security

#### Secure Token Generation
```csharp
/// <summary>
/// Generate cryptographically secure tokens for status tracking and reference forms
/// </summary>
public static class SecureTokenGenerator
{
    public static string GenerateStatusToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32]; // 256-bit token
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    public static string GenerateReferenceToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[48]; // 384-bit token
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
```

## Performance Architecture

### 1. Caching Strategy

#### Multi-Level Caching Implementation
```csharp
/// <summary>
/// Vetting system caching service with intelligent invalidation
/// </summary>
public class VettingCacheService : IVettingCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<VettingCacheService> _logger;
    
    // Cache keys and TTL configuration
    private static readonly Dictionary<string, TimeSpan> CacheTtlConfig = new()
    {
        ["reviewer_dashboard"] = TimeSpan.FromMinutes(5),
        ["application_summary"] = TimeSpan.FromMinutes(10),
        ["analytics_data"] = TimeSpan.FromMinutes(15),
        ["reviewer_workload"] = TimeSpan.FromMinutes(2)
    };

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            return _memoryCache.Get<T>(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache retrieval failed for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, string cacheType) where T : class
    {
        if (!CacheTtlConfig.TryGetValue(cacheType, out var ttl))
        {
            ttl = TimeSpan.FromMinutes(5); // Default TTL
        }

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl,
            SlidingExpiration = TimeSpan.FromMinutes(ttl.TotalMinutes / 2),
            Priority = CacheItemPriority.Normal
        };

        _memoryCache.Set(key, value, options);
    }

    public async Task InvalidatePatternAsync(string pattern)
    {
        // Invalidate all cache entries matching pattern
        if (_memoryCache is MemoryCache mc)
        {
            var field = typeof(MemoryCache).GetField("_coherentState", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (field?.GetValue(mc) is IDictionary dictionary)
            {
                var keysToRemove = dictionary.Keys.Cast<object>()
                    .Where(key => key.ToString()?.Contains(pattern) == true)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _memoryCache.Remove(key);
                }
            }
        }
    }
}
```

### 2. Database Query Optimization

#### Strategic Indexing for Performance
```sql
-- Primary reviewer dashboard query optimization
CREATE INDEX "IX_VettingApplications_ReviewerDashboard" 
ON "VettingApplications" ("Status", "Priority", "AssignedReviewerId", "CreatedAt" DESC)
WHERE "DeletedAt" IS NULL;

-- Status tracking optimization  
CREATE INDEX "IX_VettingApplications_StatusToken_Lookup" 
ON "VettingApplications" ("StatusToken")
WHERE "DeletedAt" IS NULL;

-- Reference processing optimization
CREATE INDEX "IX_VettingReferences_StatusProcessing" 
ON "VettingReferences" ("Status", "ContactedAt", "FormExpiresAt")
WHERE "Status" IN (1, 2, 3); -- NotContacted, Contacted, ReminderSent

-- Analytics and reporting optimization
CREATE INDEX "IX_VettingApplications_Analytics" 
ON "VettingApplications" ("Status", "ExperienceLevel", "IsAnonymous", "CreatedAt")
WHERE "DeletedAt" IS NULL;

-- JSONB search for skills/interests
CREATE INDEX "IX_VettingApplications_SkillsInterests" 
ON "VettingApplications" USING GIN ("SkillsInterests");
```

#### Query Performance Patterns
```csharp
/// <summary>
/// Optimized reviewer dashboard query with projection and filtering
/// </summary>
public async Task<Result<PagedResult<ApplicationSummaryDto>>> GetApplicationsForReviewAsync(
    Guid reviewerId,
    ApplicationFilterRequest filter,
    CancellationToken cancellationToken = default)
{
    try
    {
        // Build optimized query with selective loading
        var query = _context.VettingApplications
            .AsNoTracking() // Read-only optimization
            .Where(a => a.DeletedAt == null); // Use indexed filter

        // Apply status filter (uses index)
        if (!string.IsNullOrEmpty(filter.Status))
        {
            var statusEnum = Enum.Parse<ApplicationStatus>(filter.Status, true);
            query = query.Where(a => a.Status == statusEnum);
        }

        // Apply reviewer assignment filter
        query = query.Where(a => 
            a.AssignedReviewerId == reviewerId || 
            a.AssignedReviewerId == null);

        // Apply search with encrypted field handling
        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var encryptedSearchTerms = await EncryptSearchTermsAsync(filter.SearchTerm);
            query = query.Where(a => 
                encryptedSearchTerms.Any(term => 
                    a.EncryptedSceneName.Contains(term) ||
                    a.EncryptedEmail.Contains(term)));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply ordering and pagination (uses composite index)
        var applications = await query
            .OrderBy(a => a.Priority) // Primary sort
            .ThenBy(a => a.CreatedAt)  // Secondary sort
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(a => new ApplicationSummaryDto // Project to minimize data transfer
            {
                Id = a.Id,
                ApplicationNumber = a.ApplicationNumber,
                Status = a.Status,
                Priority = a.Priority,
                SubmittedAt = a.CreatedAt,
                // Encrypted fields will be decrypted in service layer
                EncryptedSceneName = a.EncryptedSceneName,
                ExperienceLevel = a.ExperienceLevel,
                IsAnonymous = a.IsAnonymous
            })
            .ToListAsync(cancellationToken);

        // Decrypt sensitive fields for authorized reviewers
        var decryptedApplications = new List<ApplicationSummaryDto>();
        foreach (var app in applications)
        {
            decryptedApplications.Add(app with
            {
                SceneName = await _encryptionService.DecryptAsync(app.EncryptedSceneName)
            });
        }

        var result = new PagedResult<ApplicationSummaryDto>
        {
            Items = decryptedApplications,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
        };

        return Result<PagedResult<ApplicationSummaryDto>>.Success(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to retrieve applications for reviewer {ReviewerId}", reviewerId);
        return Result<PagedResult<ApplicationSummaryDto>>.Failure(
            "Failed to retrieve applications");
    }
}
```

### 3. Background Processing

#### Notification Queue Processing
```csharp
/// <summary>
/// Background service for processing notification queue
/// Handles email delivery, retries, and failure escalation
/// </summary>
public class VettingNotificationProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VettingNotificationProcessor> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingNotificationsAsync(stoppingToken);
                await ProcessFailedNotificationsAsync(stoppingToken);
                await ProcessReferenceRemindersAsync(stoppingToken);
                
                await Task.Delay(_processingInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification processing loop");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Back off on error
            }
        }
    }

    private async Task ProcessPendingNotificationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var pendingNotifications = await context.VettingNotifications
            .Where(n => n.Status == NotificationStatus.Pending)
            .Where(n => n.CreatedAt <= DateTime.UtcNow.AddMinutes(-1)) // 1 minute delay for batching
            .OrderBy(n => n.CreatedAt)
            .Take(50) // Process in batches
            .ToListAsync(cancellationToken);

        foreach (var notification in pendingNotifications)
        {
            try
            {
                await notificationService.SendNotificationAsync(notification, cancellationToken);
                
                notification.Status = NotificationStatus.Sent;
                notification.SentAt = DateTime.UtcNow;
                notification.UpdatedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to send notification {NotificationId}", notification.Id);
                
                notification.Status = NotificationStatus.Failed;
                notification.RetryCount++;
                notification.ErrorMessage = ex.Message;
                notification.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, notification.RetryCount)); // Exponential backoff
                notification.UpdatedAt = DateTime.UtcNow;
            }
        }

        if (pendingNotifications.Any())
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessReferenceRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        // Find references needing reminders
        var referencesNeedingReminders = await context.VettingReferences
            .Include(r => r.Application)
            .Where(r => r.Status == ReferenceStatus.Contacted)
            .Where(r => r.ContactedAt <= DateTime.UtcNow.AddDays(-3)) // 3 days since contact
            .Where(r => r.FirstReminderSentAt == null) // No reminder sent yet
            .ToListAsync(cancellationToken);

        foreach (var reference in referencesNeedingReminders)
        {
            try
            {
                await notificationService.SendReferenceReminderAsync(reference, cancellationToken);
                
                reference.FirstReminderSentAt = DateTime.UtcNow;
                reference.UpdatedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to send reference reminder for {ReferenceId}", reference.Id);
            }
        }

        if (referencesNeedingReminders.Any())
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
```

## Error Handling Strategy

### 1. Comprehensive Error Classification

```csharp
namespace WitchCityRope.Api.Features.Vetting.Exceptions;

/// <summary>
/// Base exception for all vetting system errors
/// </summary>
public abstract class VettingException : Exception
{
    public string ErrorCode { get; }
    public Dictionary<string, object> Context { get; }

    protected VettingException(string errorCode, string message, Exception? innerException = null) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Context = new Dictionary<string, object>();
    }
}

/// <summary>
/// Business rule validation failures
/// </summary>
public class VettingBusinessRuleException : VettingException
{
    public VettingBusinessRuleException(string rule, string message) 
        : base($"BUSINESS_RULE_{rule}", message)
    {
        Context["Rule"] = rule;
    }
}

/// <summary>
/// Data encryption/decryption failures
/// </summary>
public class VettingEncryptionException : VettingException
{
    public VettingEncryptionException(string operation, string message, Exception? innerException = null) 
        : base($"ENCRYPTION_{operation}", message, innerException)
    {
        Context["Operation"] = operation;
    }
}

/// <summary>
/// Notification delivery failures
/// </summary>
public class VettingNotificationException : VettingException
{
    public VettingNotificationException(string type, string recipient, string message, Exception? innerException = null) 
        : base($"NOTIFICATION_{type}", message, innerException)
    {
        Context["Type"] = type;
        Context["Recipient"] = recipient;
    }
}
```

### 2. Global Exception Handling

```csharp
/// <summary>
/// Global exception handling middleware for vetting endpoints
/// </summary>
public class VettingExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<VettingExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = ex switch
        {
            VettingBusinessRuleException businessEx => new
            {
                error = businessEx.Message,
                code = businessEx.ErrorCode,
                type = "business_rule_violation",
                context = businessEx.Context
            },
            VettingEncryptionException encryptionEx => new
            {
                error = "Data security operation failed",
                code = encryptionEx.ErrorCode,
                type = "security_error"
                // Don't expose encryption details
            },
            ValidationException validationEx => new
            {
                error = "Input validation failed",
                code = "VALIDATION_FAILED",
                type = "validation_error",
                details = validationEx.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            },
            UnauthorizedAccessException _ => new
            {
                error = "Access denied",
                code = "UNAUTHORIZED",
                type = "authorization_error"
            },
            _ => new
            {
                error = "An unexpected error occurred",
                code = "INTERNAL_ERROR",
                type = "system_error"
            }
        };

        response.StatusCode = ex switch
        {
            VettingBusinessRuleException => 400,
            ValidationException => 400,
            UnauthorizedAccessException => 401,
            VettingEncryptionException => 500,
            _ => 500
        };

        _logger.LogError(ex, 
            "Vetting system error: {ErrorType} - {Message}", 
            ex.GetType().Name, ex.Message);

        await response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
}
```

## Testing Strategy

### 1. Unit Testing Architecture

```csharp
namespace WitchCityRope.Api.Tests.Features.Vetting.Services;

/// <summary>
/// Comprehensive unit tests for VettingService business logic
/// Uses mocked dependencies and focused test scenarios
/// </summary>
public class VettingServiceTests
{
    private readonly Mock<ApplicationDbContext> _mockContext;
    private readonly Mock<IEncryptionService> _mockEncryption;
    private readonly Mock<INotificationService> _mockNotification;
    private readonly Mock<ILogger<VettingService>> _mockLogger;
    private readonly VettingService _service;

    public VettingServiceTests()
    {
        _mockContext = new Mock<ApplicationDbContext>();
        _mockEncryption = new Mock<IEncryptionService>();
        _mockNotification = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<VettingService>>();

        _service = new VettingService(
            _mockContext.Object,
            _mockEncryption.Object,
            _mockNotification.Object,
            Mock.Of<IValidator<CreateApplicationRequest>>(),
            Mock.Of<IValidator<ReviewDecisionRequest>>(),
            _mockLogger.Object,
            Mock.Of<IMemoryCache>());
    }

    [Fact]
    public async Task SubmitApplicationAsync_WithValidRequest_ShouldCreateApplicationSuccessfully()
    {
        // Arrange
        var request = CreateValidApplicationRequest();
        
        _mockEncryption.Setup(e => e.EncryptAsync(It.IsAny<string>()))
                      .ReturnsAsync((string input) => $"encrypted_{input}");

        var mockDbSet = CreateMockDbSet<VettingApplication>();
        _mockContext.Setup(c => c.VettingApplications).Returns(mockDbSet.Object);

        // Act
        var result = await _service.SubmitApplicationAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value.ApplicationNumber);
        
        mockDbSet.Verify(d => d.Add(It.IsAny<VettingApplication>()), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockNotification.Verify(n => n.SendApplicationConfirmationAsync(
            It.IsAny<Guid>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitApplicationAsync_WithDuplicateSceneName_ShouldReturnFailure()
    {
        // Arrange
        var request = CreateValidApplicationRequest();
        
        var existingApplication = new VettingApplication 
        { 
            EncryptedSceneName = "encrypted_TestUser" 
        };

        _mockEncryption.Setup(e => e.EncryptAsync(request.SceneName))
                      .ReturnsAsync("encrypted_TestUser");

        var mockDbSet = CreateMockDbSet<VettingApplication>(new[] { existingApplication });
        _mockContext.Setup(c => c.VettingApplications).Returns(mockDbSet.Object);

        // Act
        var result = await _service.SubmitApplicationAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Scene name already in use", result.Error);
    }

    private CreateApplicationRequest CreateValidApplicationRequest()
    {
        return new CreateApplicationRequest
        {
            FullName = $"Test User {Guid.NewGuid():N}",
            SceneName = $"TestUser_{Guid.NewGuid():N}",
            Email = $"test_{Guid.NewGuid():N}@example.com",
            ExperienceLevel = ExperienceLevel.Intermediate,
            YearsExperience = 3,
            ExperienceDescription = "Sample experience description that meets minimum length requirements",
            SafetyKnowledge = "Understanding of rope safety and risk awareness",
            ConsentUnderstanding = "Clear understanding of consent principles",
            WhyJoinCommunity = "Seeking to learn and grow in rope bondage community",
            SkillsInterests = new List<string> { "rope-bondage", "safety" },
            ExpectationsGoals = "Looking forward to learning from experienced practitioners",
            AgreesToGuidelines = true,
            References = new List<ReferenceRequest>
            {
                new() { Name = "Reference 1", Email = "ref1@example.com", Relationship = "Mentor" },
                new() { Name = "Reference 2", Email = "ref2@example.com", Relationship = "Partner" }
            },
            IsAnonymous = false,
            AgreesToTerms = true
        };
    }
}
```

### 2. Integration Testing with TestContainers

```csharp
namespace WitchCityRope.Api.Tests.Features.Vetting.Integration;

/// <summary>
/// Integration tests using real PostgreSQL via TestContainers
/// Tests complete workflows including database operations and encryption
/// </summary>
[Collection("PostgreSQL Integration Tests")]
public class VettingWorkflowIntegrationTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;
    private readonly HttpClient _client;

    public VettingWorkflowIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
        _client = _fixture.CreateClient();
    }

    [Fact]
    public async Task CompleteVettingWorkflow_FromSubmissionToApproval_ShouldWorkEndToEnd()
    {
        // Arrange
        var applicationRequest = CreateValidApplicationRequest();

        // Act 1: Submit application
        var submitResponse = await _client.PostAsJsonAsync("/api/vetting/applications", applicationRequest);
        
        // Assert 1: Application submitted successfully
        submitResponse.EnsureSuccessStatusCode();
        var submissionResult = await submitResponse.Content.ReadFromJsonAsync<ApplicationSubmissionResponse>();
        Assert.NotNull(submissionResult);

        // Act 2: Reviewer retrieves applications
        await _fixture.AuthenticateAsVettingReviewer(_client);
        var applicationsResponse = await _client.GetAsync("/api/vetting/applications?status=submitted");
        
        // Assert 2: Application appears in reviewer queue
        applicationsResponse.EnsureSuccessStatusCode();
        var applications = await applicationsResponse.Content.ReadFromJsonAsync<PagedResult<ApplicationSummaryDto>>();
        Assert.NotNull(applications);
        Assert.Contains(applications.Items, a => a.ApplicationNumber == submissionResult.ApplicationNumber);

        // Act 3: Submit references
        var application = applications.Items.First(a => a.ApplicationNumber == submissionResult.ApplicationNumber);
        var references = await GetApplicationReferences(application.Id);
        
        foreach (var reference in references)
        {
            var referenceResponse = CreateValidReferenceResponse();
            var referenceSubmitResponse = await _client.PostAsJsonAsync(
                $"/api/vetting/references/{reference.ResponseToken}/response", 
                referenceResponse);
            
            referenceSubmitResponse.EnsureSuccessStatusCode();
        }

        // Act 4: Reviewer makes approval decision
        var reviewDecision = new ReviewDecisionRequest
        {
            DecisionType = DecisionType.Approve,
            Reasoning = "Application meets all criteria for community membership",
            IsFinalDecision = true
        };

        var decisionResponse = await _client.PostAsJsonAsync(
            $"/api/vetting/applications/{application.Id}/decision", 
            reviewDecision);

        // Assert 4: Decision recorded successfully
        decisionResponse.EnsureSuccessStatusCode();

        // Act 5: Check final status
        var statusResponse = await _client.GetAsync($"/api/vetting/status/{submissionResult.StatusToken}");
        var finalStatus = await statusResponse.Content.ReadFromJsonAsync<ApplicationStatusResponse>();

        // Assert 5: Application approved
        Assert.Equal("approved", finalStatus?.Status);
    }

    [Fact]
    public async Task EncryptionDecryption_WithRealDatabase_ShouldMaintainDataIntegrity()
    {
        // Arrange
        var originalData = "Sensitive personal information that must be protected";
        
        using var scope = _fixture.ServiceProvider.CreateScope();
        var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act 1: Encrypt and store data
        var encryptedData = await encryptionService.EncryptAsync(originalData);
        
        var testApplication = new VettingApplication
        {
            ApplicationNumber = $"TEST-{Guid.NewGuid():N}",
            EncryptedFullName = encryptedData,
            Status = ApplicationStatus.Draft
        };

        context.VettingApplications.Add(testApplication);
        await context.SaveChangesAsync();

        // Act 2: Retrieve and decrypt data
        var retrievedApplication = await context.VettingApplications
            .FirstAsync(a => a.Id == testApplication.Id);
        
        var decryptedData = await encryptionService.DecryptAsync(retrievedApplication.EncryptedFullName);

        // Assert: Data integrity maintained through encryption/decryption cycle
        Assert.Equal(originalData, decryptedData);
        Assert.NotEqual(originalData, encryptedData); // Ensure actually encrypted
        Assert.True(encryptedData.Length > originalData.Length); // Includes nonce + tag
    }
}
```

## Implementation Phases

### Phase 1: Core Infrastructure (Weeks 1-2)
1. **Database Schema**: Create all vetting tables with encryption fields
2. **Core Services**: Implement VettingService with basic CRUD operations
3. **Security Foundation**: AES-256 encryption service and secure token generation
4. **Basic Endpoints**: Application submission and status checking
5. **Authentication**: Role-based authorization for vetting team

### Phase 2: Application Workflow (Weeks 3-4)
1. **Reference Management**: ReferenceService with automated email handling
2. **Review Dashboard**: Reviewer interface with application assignment
3. **Decision Processing**: Approval/denial workflow with audit trails
4. **Notification System**: Email templates and delivery tracking
5. **Status Tracking**: Public status page with secure token access

### Phase 3: Advanced Features (Weeks 5-6)
1. **Analytics Dashboard**: Comprehensive reporting for administrators
2. **Batch Operations**: Bulk assignment and notification capabilities
3. **Performance Optimization**: Caching, indexing, and query optimization
4. **Background Processing**: Automated reminder and cleanup jobs
5. **Integration Testing**: Complete end-to-end workflow validation

### Phase 4: Production Readiness (Week 7)
1. **Security Audit**: Penetration testing and vulnerability assessment
2. **Performance Testing**: Load testing and optimization
3. **Documentation**: API documentation and deployment guides
4. **Monitoring**: Application insights and health checks
5. **Deployment**: Production deployment with monitoring

## Success Metrics

### Technical Performance Targets
- **API Response Times**: < 200ms for GET requests, < 1000ms for POST/PUT
- **Database Queries**: < 50ms for simple queries, < 200ms for complex queries
- **Encryption Operations**: < 100ms for individual field encryption/decryption
- **Background Processing**: < 30 seconds for notification delivery
- **System Availability**: > 99.5% uptime during business hours

### Security Compliance Goals
- **Zero Data Breaches**: No exposure of unencrypted PII
- **Audit Trail Coverage**: 100% of sensitive operations logged
- **Access Control**: Role-based permissions enforced at all levels
- **Encryption Standards**: AES-256-GCM for all PII fields
- **Token Security**: Cryptographically secure tokens for all public access

### Business Process Efficiency
- **Application Processing**: 95% of decisions within 14 business days
- **Reference Response**: 85% of references respond within 7 days  
- **Reviewer Satisfaction**: > 4.2/5.0 rating for review interface
- **Automation Rate**: 80% of notifications sent automatically
- **Error Rate**: < 1% application submission failures

This technical architecture provides a comprehensive foundation for implementing the WitchCityRope Vetting System with emphasis on security, performance, and maintainability while following established project patterns and standards.