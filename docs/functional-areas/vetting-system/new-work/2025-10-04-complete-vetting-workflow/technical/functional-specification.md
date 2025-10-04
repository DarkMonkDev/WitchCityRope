# Functional Specification: Complete WitchCityRope Vetting Workflow
<!-- Last Updated: 2025-10-04 -->
<!-- Version: 1.0 -->
<!-- Owner: Functional Spec Agent -->
<!-- Status: Draft -->

## Table of Contents
1. [Technical Overview](#technical-overview)
2. [Architecture](#architecture)
3. [System Components Overview](#system-components-overview)
4. [Backend API Specifications](#backend-api-specifications)
5. [Frontend Component Specifications](#frontend-component-specifications)
6. [Email System Integration](#email-system-integration)
7. [Database Schema Extensions](#database-schema-extensions)
8. [Access Control Logic](#access-control-logic)
9. [Testing Strategy](#testing-strategy)
10. [Documentation Requirements](#documentation-requirements)
11. [Implementation Order](#implementation-order)

---

## Technical Overview

### Project Context
WitchCityRope vetting system currently has a functioning public application form, vetting status display, and database schema. This specification completes the workflow with access control, email notifications, admin review interface, and bulk operations.

### Implementation Approach
**One Complete Vertical Slice** covering all components:
- **Weeks 1-2**: Backend infrastructure (email service, status logic, access control)
- **Weeks 3-4**: Admin interface (review grid, status management, templates)
- **Weeks 5-6**: Member experience & polish (access control UI, testing)

### Technology Stack
- **Backend**: ASP.NET Core 9, Minimal APIs, EF Core 9, PostgreSQL 16
- **Frontend**: React 18, TypeScript 5, Mantine v7, TanStack Query v5, Zustand
- **Email**: SendGrid API
- **Auth**: httpOnly cookies via existing auth system
- **Architecture**: Microservices Web+API pattern

### Key Technical Decisions
1. **SendGrid for Email**: Reliable, scalable, comprehensive delivery tracking
2. **Database-Stored Templates**: Admin-editable without code deployments
3. **Server-Side Validation**: All access checks validated at API level
4. **Real-Time Updates**: TanStack Query for optimistic updates and cache management
5. **Audit Trail**: Complete immutable logging for compliance

---

## Architecture

### Microservices Architecture
**CRITICAL**: This is a Web+API microservices architecture:
- **Web Service** (React): UI/Auth at http://localhost:5173 (Docker)
- **API Service** (Minimal API): Business logic at http://localhost:5655 (Docker)
- **Database** (PostgreSQL): localhost:5433 (Docker)
- **Pattern**: React → HTTP → API → Database (NEVER React → Database directly)

### Component Structure (API Service)
```
/apps/api/Features/Vetting/
├── Endpoints/
│   └── VettingEndpoints.cs          # Minimal API endpoint mappings
├── Services/
│   ├── IVettingService.cs           # Service interface
│   ├── VettingService.cs            # Core vetting business logic
│   ├── IEmailService.cs             # Email service interface
│   ├── EmailService.cs              # SendGrid integration
│   └── IAccessControlService.cs     # Access control validation
│       └── AccessControlService.cs  # RSVP/ticket access logic
├── Entities/
│   ├── VettingApplication.cs        # Main entity (exists)
│   ├── VettingAuditLog.cs          # Audit trail (exists)
│   ├── VettingEmailTemplate.cs     # Email templates (new)
│   └── VettingEmailLog.cs          # Email delivery logs (new)
├── Models/
│   ├── VettingDtos.cs              # Request/Response DTOs
│   ├── EmailDtos.cs                # Email-specific DTOs (new)
│   └── AccessControlDtos.cs        # Access control DTOs (new)
└── Validators/
    ├── VettingValidator.cs         # Application validation
    └── StatusTransitionValidator.cs # Status change validation (new)
```

### Component Structure (Web Service)
```
/apps/web/src/features/vetting/
├── pages/
│   ├── VettingAdminPage.tsx        # Admin review grid
│   └── VettingApplicationDetail.tsx # Detail view
├── components/
│   ├── VettingStatusBox.tsx        # Status display (exists)
│   ├── VettingApplicationForm.tsx  # Public form (exists)
│   ├── VettingAdminGrid.tsx        # Application grid component
│   ├── VettingDetailView.tsx       # Detail view component
│   ├── VettingStatusChange.tsx     # Status change modal
│   ├── VettingNotes.tsx            # Notes management
│   ├── VettingEmailTemplates.tsx   # Template editor
│   ├── VettingBulkOperations.tsx   # Bulk actions
│   └── VettingAccessControl.tsx    # Access blocked messaging
├── hooks/
│   ├── useVettingStatus.ts         # Status check hook (exists)
│   ├── useVettingApplications.ts   # Admin grid data
│   ├── useVettingDetail.ts         # Detail view data
│   ├── useStatusChange.ts          # Status mutation
│   ├── useEmailTemplates.ts        # Template management
│   └── useAccessControl.ts         # Access check hook
├── services/
│   └── vettingApiClient.ts         # API client
└── types/
    └── vetting.types.ts            # TypeScript interfaces
```

### Service Architecture
- **Web Service**: React components make HTTP calls to API
- **API Service**: Business logic with EF Core database access
- **No Direct Database Access**: Web service NEVER directly accesses database

### Integration Points
```
┌─────────────────────────────────────────────────────────────┐
│                         WEB SERVICE                          │
│  ┌───────────────────────────────────────────────────────┐ │
│  │ React Components                                       │ │
│  │ - VettingAdminGrid                                    │ │
│  │ - VettingDetailView                                   │ │
│  │ - VettingEmailTemplates                               │ │
│  │ - VettingAccessControl                                │ │
│  └───────────────────────────────────────────────────────┘ │
│                          │ HTTP                             │
└──────────────────────────┼──────────────────────────────────┘
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                         API SERVICE                          │
│  ┌───────────────────────────────────────────────────────┐ │
│  │ Vetting Endpoints                                      │ │
│  │ /api/vetting/applications                             │ │
│  │ /api/vetting/applications/{id}                        │ │
│  │ /api/vetting/status-change                            │ │
│  │ /api/vetting/access-control                           │ │
│  │ /api/vetting/email-templates                          │ │
│  │ /api/vetting/bulk-operations                          │ │
│  └───────────────┬───────────────────────────────────────┘ │
│                  │                                           │
│  ┌───────────────▼───────────────────────────────────────┐ │
│  │ Services                                               │ │
│  │ - VettingService (core logic)                         │ │
│  │ - EmailService (SendGrid)                             │ │
│  │ - AccessControlService (RSVP/tickets)                 │ │
│  └───────────────┬───────────────────────────────────────┘ │
│                  │                                           │
│  ┌───────────────▼───────────────────────────────────────┐ │
│  │ EF Core → PostgreSQL                                  │ │
│  │ - VettingApplications                                 │ │
│  │ - VettingEmailTemplates                               │ │
│  │ - VettingEmailLogs                                    │ │
│  │ - VettingAuditLogs                                    │ │
│  └───────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
                    ┌─────────────┐
                    │  SendGrid   │
                    │  Email API  │
                    └─────────────┘
```

---

## System Components Overview

### Phase 1: Access Control System (Weeks 1-2)
**Purpose**: Block denied/on-hold members from RSVP and ticket purchases

**Components**:
1. **AccessControlService**: Server-side validation logic
2. **Access Control Endpoints**: API endpoints for status checks
3. **Access Control Hooks**: React hooks for UI state
4. **Access Blocked UI**: Clear messaging components

**Integration Points**:
- RSVP system: `/api/events/rsvp`
- Ticket purchase: `/api/payments/create-payment-intent`

### Phase 2: Email Notification System (Weeks 2-3)
**Purpose**: Automated email notifications for status changes

**Components**:
1. **EmailService**: SendGrid integration
2. **EmailTemplate Entity**: Database-stored templates
3. **EmailLog Entity**: Delivery tracking
4. **Email Trigger Logic**: Automatic sending on status changes

**Integration Points**:
- Status change workflow: VettingService
- SendGrid API: External service

### Phase 3: Admin Review Interface (Weeks 3-5)
**Purpose**: Complete admin workflow for application review

**Components**:
1. **VettingAdminGrid**: Filterable, sortable application list
2. **VettingDetailView**: Complete application details
3. **VettingStatusChange**: Status transition management
4. **VettingNotes**: Admin notes and audit trail
5. **VettingEmailTemplates**: Template editor

**Integration Points**:
- Email system: Status changes trigger emails
- Access control: Status changes affect access
- Audit logging: All actions logged

### Phase 4: Bulk Operations (Week 5-6)
**Purpose**: Administrative efficiency for multiple applications

**Components**:
1. **VettingBulkOperations**: Bulk action interface
2. **Bulk Email Service**: Batch email sending
3. **Bulk Status Change**: Batch status updates
4. **Progress Tracking**: Real-time operation monitoring

**Integration Points**:
- Email system: Bulk email sending
- Status change: Bulk status updates
- Admin interface: Integrated into grid

---

## Backend API Specifications

### Data Transfer Objects (DTOs)

#### Access Control DTOs
```csharp
namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Access control check response
/// </summary>
public class VettingAccessCheckDto
{
    public bool CanRsvp { get; set; }
    public bool CanPurchaseTickets { get; set; }
    public VettingStatus VettingStatus { get; set; }
    public string? BlockReason { get; set; }
    public string? BlockMessage { get; set; }
}

/// <summary>
/// Access control check request
/// </summary>
public class AccessControlRequest
{
    public Guid UserId { get; set; }
    public Guid? EventId { get; set; } // Optional for event-specific checks
}
```

#### Email DTOs
```csharp
/// <summary>
/// Email template DTO for admin management
/// </summary>
public class EmailTemplateDto
{
    public Guid Id { get; set; }
    public EmailTemplateType TemplateType { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public List<string> AvailableVariables { get; set; } = new();
}

/// <summary>
/// Email template update request
/// </summary>
public class UpdateEmailTemplateRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Email log DTO for delivery tracking
/// </summary>
public class EmailLogDto
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public EmailTemplateType TemplateType { get; set; }
    public DateTime SentAt { get; set; }
    public EmailDeliveryStatus Status { get; set; }
    public string? SendGridMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}
```

#### Bulk Operation DTOs
```csharp
/// <summary>
/// Bulk reminder email request
/// </summary>
public class BulkReminderRequest
{
    public List<Guid> ApplicationIds { get; set; } = new();
    public int AgeThresholdDays { get; set; } = 7;
    public bool OverrideThreshold { get; set; } = false;
}

/// <summary>
/// Bulk status change request
/// </summary>
public class BulkStatusChangeRequest
{
    public List<Guid> ApplicationIds { get; set; } = new();
    public VettingStatus NewStatus { get; set; }
    public int AgeThresholdDays { get; set; } = 14;
    public string? BulkNote { get; set; }
}

/// <summary>
/// Bulk operation result
/// </summary>
public class BulkOperationResult
{
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<Guid> ProcessedApplicationIds { get; set; } = new();
    public long ExecutionTimeMs { get; set; }
}
```

### API Endpoints

#### Phase 1: Access Control Endpoints

```csharp
// File: /apps/api/Features/Vetting/Endpoints/AccessControlEndpoints.cs

/// <summary>
/// Access control endpoints for vetting system
/// </summary>
public static class AccessControlEndpoints
{
    public static void MapAccessControlEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/vetting/access-control")
            .WithTags("Vetting Access Control");

        // Check user's vetting access for RSVP and ticket purchases
        group.MapGet("/check/{userId:guid}", CheckVettingAccess)
            .WithName("CheckVettingAccess")
            .WithSummary("Check user's vetting status for access control")
            .Produces<ApiResponse<VettingAccessCheckDto>>()
            .Produces<ApiResponse<VettingAccessCheckDto>>(StatusCodes.Status403Forbidden);

        // Check specific event access
        group.MapPost("/check-event", CheckEventAccess)
            .WithName("CheckEventAccess")
            .WithSummary("Check user's access for specific event")
            .Produces<ApiResponse<VettingAccessCheckDto>>()
            .Produces<ApiResponse<VettingAccessCheckDto>>(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> CheckVettingAccess(
        Guid userId,
        IAccessControlService accessControlService,
        CancellationToken cancellationToken)
    {
        var result = await accessControlService.CheckAccessAsync(userId, cancellationToken);

        if (!result.Success)
        {
            return Results.Json(
                new ApiResponse<VettingAccessCheckDto>
                {
                    Success = false,
                    Message = result.Message,
                    Errors = result.Errors,
                    Timestamp = DateTime.UtcNow
                },
                statusCode: StatusCodes.Status403Forbidden);
        }

        return Results.Ok(new ApiResponse<VettingAccessCheckDto>
        {
            Success = true,
            Data = result.Value,
            Timestamp = DateTime.UtcNow
        });
    }

    private static async Task<IResult> CheckEventAccess(
        AccessControlRequest request,
        IAccessControlService accessControlService,
        CancellationToken cancellationToken)
    {
        var result = await accessControlService.CheckEventAccessAsync(
            request.UserId,
            request.EventId,
            cancellationToken);

        if (!result.Success)
        {
            return Results.Json(
                new ApiResponse<VettingAccessCheckDto>
                {
                    Success = false,
                    Message = result.Message,
                    Errors = result.Errors,
                    Timestamp = DateTime.UtcNow
                },
                statusCode: StatusCodes.Status403Forbidden);
        }

        return Results.Ok(new ApiResponse<VettingAccessCheckDto>
        {
            Success = true,
            Data = result.Value,
            Timestamp = DateTime.UtcNow
        });
    }
}
```

#### Phase 2: Email Template Endpoints

```csharp
// File: /apps/api/Features/Vetting/Endpoints/EmailTemplateEndpoints.cs

/// <summary>
/// Email template management endpoints
/// </summary>
public static class EmailTemplateEndpoints
{
    public static void MapEmailTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/vetting/email-templates")
            .WithTags("Vetting Email Templates")
            .RequireAuthorization("Administrator");

        // Get all email templates
        group.MapGet("/", GetAllTemplates)
            .WithName("GetAllEmailTemplates")
            .WithSummary("Get all vetting email templates")
            .Produces<ApiResponse<List<EmailTemplateDto>>>();

        // Get specific template
        group.MapGet("/{templateType}", GetTemplate)
            .WithName("GetEmailTemplate")
            .WithSummary("Get specific email template by type")
            .Produces<ApiResponse<EmailTemplateDto>>()
            .Produces(StatusCodes.Status404NotFound);

        // Update template
        group.MapPut("/{templateType}", UpdateTemplate)
            .WithName("UpdateEmailTemplate")
            .WithSummary("Update email template")
            .Produces<ApiResponse<EmailTemplateDto>>()
            .Produces(StatusCodes.Status404NotFound);

        // Reset template to default
        group.MapPost("/{templateType}/reset", ResetTemplate)
            .WithName("ResetEmailTemplate")
            .WithSummary("Reset template to default")
            .Produces<ApiResponse<EmailTemplateDto>>();

        // Preview template with sample data
        group.MapPost("/{templateType}/preview", PreviewTemplate)
            .WithName("PreviewEmailTemplate")
            .WithSummary("Preview template with sample data")
            .Produces<ApiResponse<string>>();
    }

    private static async Task<IResult> GetAllTemplates(
        IEmailService emailService,
        CancellationToken cancellationToken)
    {
        var result = await emailService.GetAllTemplatesAsync(cancellationToken);

        if (!result.Success)
        {
            return Results.BadRequest(new ApiResponse<List<EmailTemplateDto>>
            {
                Success = false,
                Message = result.Message,
                Errors = result.Errors,
                Timestamp = DateTime.UtcNow
            });
        }

        return Results.Ok(new ApiResponse<List<EmailTemplateDto>>
        {
            Success = true,
            Data = result.Value,
            Timestamp = DateTime.UtcNow
        });
    }

    // Additional endpoint implementations...
}
```

#### Phase 3: Admin Review Endpoints

```csharp
// File: /apps/api/Features/Vetting/Endpoints/VettingAdminEndpoints.cs

/// <summary>
/// Admin review endpoints (extends existing VettingEndpoints.cs)
/// </summary>
public static class VettingAdminEndpoints
{
    public static void MapVettingAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/vetting/admin")
            .WithTags("Vetting Admin")
            .RequireAuthorization("Administrator");

        // Get applications for review (with filtering)
        group.MapPost("/applications", GetApplicationsForReview)
            .WithName("GetApplicationsForReview")
            .WithSummary("Get paginated applications for admin review")
            .Produces<ApiResponse<PagedResult<ApplicationSummaryDto>>>();

        // Get application detail
        group.MapGet("/applications/{id:guid}", GetApplicationDetail)
            .WithName("GetApplicationDetail")
            .WithSummary("Get detailed application information")
            .Produces<ApiResponse<ApplicationDetailResponse>>()
            .Produces(StatusCodes.Status404NotFound);

        // Change application status
        group.MapPost("/applications/{id:guid}/status", ChangeApplicationStatus)
            .WithName("ChangeApplicationStatus")
            .WithSummary("Change application status with validation")
            .Produces<ApiResponse<ReviewDecisionResponse>>()
            .Produces(StatusCodes.Status400BadRequest);

        // Add admin note
        group.MapPost("/applications/{id:guid}/notes", AddApplicationNote)
            .WithName("AddApplicationNote")
            .WithSummary("Add admin note to application")
            .Produces<ApiResponse<NoteResponse>>();

        // Get valid status transitions
        group.MapGet("/applications/{id:guid}/valid-transitions", GetValidTransitions)
            .WithName("GetValidStatusTransitions")
            .WithSummary("Get valid status transitions for current status")
            .Produces<ApiResponse<List<VettingStatus>>>();
    }

    private static async Task<IResult> ChangeApplicationStatus(
        Guid id,
        ReviewDecisionRequest request,
        IVettingService vettingService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId(); // Extension method

        var result = await vettingService.SubmitReviewDecisionAsync(
            id,
            request,
            userId,
            cancellationToken);

        if (!result.Success)
        {
            return Results.BadRequest(new ApiResponse<ReviewDecisionResponse>
            {
                Success = false,
                Message = result.Message,
                Errors = result.Errors,
                Timestamp = DateTime.UtcNow
            });
        }

        return Results.Ok(new ApiResponse<ReviewDecisionResponse>
        {
            Success = true,
            Data = result.Value,
            Message = "Status changed successfully",
            Timestamp = DateTime.UtcNow
        });
    }

    // Additional endpoint implementations...
}
```

#### Phase 4: Bulk Operation Endpoints

```csharp
// File: /apps/api/Features/Vetting/Endpoints/BulkOperationEndpoints.cs

/// <summary>
/// Bulk operation endpoints for vetting system
/// </summary>
public static class BulkOperationEndpoints
{
    public static void MapBulkOperationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/vetting/bulk")
            .WithTags("Vetting Bulk Operations")
            .RequireAuthorization("Administrator");

        // Send bulk reminder emails
        group.MapPost("/reminders", SendBulkReminders)
            .WithName("SendBulkReminders")
            .WithSummary("Send reminder emails to interview-approved applications")
            .Produces<ApiResponse<BulkOperationResult>>();

        // Bulk status change
        group.MapPost("/status-change", BulkStatusChange)
            .WithName("BulkStatusChange")
            .WithSummary("Change status for multiple applications")
            .Produces<ApiResponse<BulkOperationResult>>();

        // Get eligible applications for bulk operations
        group.MapGet("/eligible-reminders", GetEligibleForReminders)
            .WithName("GetEligibleForReminders")
            .WithSummary("Get applications eligible for reminder emails")
            .Produces<ApiResponse<List<ApplicationSummaryDto>>>();

        group.MapGet("/eligible-on-hold", GetEligibleForOnHold)
            .WithName("GetEligibleForOnHold")
            .WithSummary("Get applications eligible for on-hold status")
            .Produces<ApiResponse<List<ApplicationSummaryDto>>>();
    }

    private static async Task<IResult> SendBulkReminders(
        BulkReminderRequest request,
        IBulkOperationService bulkService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();

        var result = await bulkService.SendBulkRemindersAsync(
            request,
            userId,
            cancellationToken);

        if (!result.Success)
        {
            return Results.BadRequest(new ApiResponse<BulkOperationResult>
            {
                Success = false,
                Message = result.Message,
                Errors = result.Errors,
                Timestamp = DateTime.UtcNow
            });
        }

        return Results.Ok(new ApiResponse<BulkOperationResult>
        {
            Success = true,
            Data = result.Value,
            Message = $"Bulk operation completed: {result.Value.SuccessCount} successful, {result.Value.FailureCount} failed",
            Timestamp = DateTime.UtcNow
        });
    }

    // Additional endpoint implementations...
}
```

### Service Interfaces and Implementations

#### AccessControlService

```csharp
// File: /apps/api/Features/Vetting/Services/IAccessControlService.cs

namespace WitchCityRope.Api.Features.Vetting.Services;

/// <summary>
/// Service for vetting-based access control
/// </summary>
public interface IAccessControlService
{
    /// <summary>
    /// Check user's general vetting access
    /// </summary>
    Task<Result<VettingAccessCheckDto>> CheckAccessAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check user's access for specific event
    /// </summary>
    Task<Result<VettingAccessCheckDto>> CheckEventAccessAsync(
        Guid userId,
        Guid? eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate RSVP attempt
    /// </summary>
    Task<Result<bool>> ValidateRsvpAccessAsync(
        Guid userId,
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate ticket purchase attempt
    /// </summary>
    Task<Result<bool>> ValidateTicketPurchaseAccessAsync(
        Guid userId,
        Guid eventId,
        CancellationToken cancellationToken = default);
}
```

```csharp
// File: /apps/api/Features/Vetting/Services/AccessControlService.cs

namespace WitchCityRope.Api.Features.Vetting.Services;

/// <summary>
/// Implementation of vetting-based access control
/// </summary>
public class AccessControlService : IAccessControlService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccessControlService> _logger;

    public AccessControlService(
        ApplicationDbContext context,
        ILogger<AccessControlService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<VettingAccessCheckDto>> CheckAccessAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get user's vetting application
            var application = await _context.VettingApplications
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.UserId == userId, cancellationToken);

            var vettingStatus = application?.Status ?? VettingStatus.Draft;

            // Determine access based on status
            var canRsvp = CanRsvp(vettingStatus);
            var canPurchaseTickets = CanPurchaseTickets(vettingStatus);

            var response = new VettingAccessCheckDto
            {
                CanRsvp = canRsvp,
                CanPurchaseTickets = canPurchaseTickets,
                VettingStatus = vettingStatus,
                BlockReason = GetBlockReason(vettingStatus),
                BlockMessage = GetBlockMessage(vettingStatus)
            };

            return Result<VettingAccessCheckDto>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking vetting access for user {UserId}", userId);
            return Result<VettingAccessCheckDto>.Failure(
                "Failed to check access", ex.Message);
        }
    }

    public async Task<Result<VettingAccessCheckDto>> CheckEventAccessAsync(
        Guid userId,
        Guid? eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get basic access check
            var accessResult = await CheckAccessAsync(userId, cancellationToken);
            if (!accessResult.Success)
                return accessResult;

            // If event specified, check event access level
            if (eventId.HasValue)
            {
                var eventEntity = await _context.Events
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == eventId.Value, cancellationToken);

                if (eventEntity != null)
                {
                    // Apply event access level logic
                    var accessCheck = accessResult.Value!;

                    // Example: Vetted Members Only events require Approved status
                    if (eventEntity.AccessLevel == "VettedMembersOnly")
                    {
                        if (accessCheck.VettingStatus != VettingStatus.Approved)
                        {
                            accessCheck.CanRsvp = false;
                            accessCheck.CanPurchaseTickets = false;
                            accessCheck.BlockReason = "Event requires vetted member status";
                            accessCheck.BlockMessage = "This event is only available to vetted members. " +
                                "Please complete the vetting process to access this event.";
                        }
                    }
                }
            }

            return accessResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking event access for user {UserId}", userId);
            return Result<VettingAccessCheckDto>.Failure(
                "Failed to check event access", ex.Message);
        }
    }

    public async Task<Result<bool>> ValidateRsvpAccessAsync(
        Guid userId,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var accessCheck = await CheckEventAccessAsync(userId, eventId, cancellationToken);

        if (!accessCheck.Success)
            return Result<bool>.Failure(accessCheck.Message, accessCheck.Errors);

        if (!accessCheck.Value!.CanRsvp)
        {
            _logger.LogWarning("RSVP access denied for user {UserId} to event {EventId}. Status: {Status}",
                userId, eventId, accessCheck.Value.VettingStatus);

            return Result<bool>.Failure(
                "Access Denied",
                accessCheck.Value.BlockMessage ?? "You cannot RSVP for this event at this time.");
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ValidateTicketPurchaseAccessAsync(
        Guid userId,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var accessCheck = await CheckEventAccessAsync(userId, eventId, cancellationToken);

        if (!accessCheck.Success)
            return Result<bool>.Failure(accessCheck.Message, accessCheck.Errors);

        if (!accessCheck.Value!.CanPurchaseTickets)
        {
            _logger.LogWarning("Ticket purchase access denied for user {UserId} to event {EventId}. Status: {Status}",
                userId, eventId, accessCheck.Value.VettingStatus);

            return Result<bool>.Failure(
                "Access Denied",
                accessCheck.Value.BlockMessage ?? "You cannot purchase tickets for this event at this time.");
        }

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Determine if user can RSVP based on vetting status
    /// </summary>
    private static bool CanRsvp(VettingStatus status)
    {
        return status switch
        {
            VettingStatus.OnHold => false,
            VettingStatus.Denied => false,
            VettingStatus.Withdrawn => false,
            _ => true // All other statuses can RSVP (subject to event access level)
        };
    }

    /// <summary>
    /// Determine if user can purchase tickets based on vetting status
    /// </summary>
    private static bool CanPurchaseTickets(VettingStatus status)
    {
        return status switch
        {
            VettingStatus.OnHold => false,
            VettingStatus.Denied => false,
            VettingStatus.Withdrawn => false,
            _ => true // All other statuses can purchase tickets (subject to event access level)
        };
    }

    /// <summary>
    /// Get block reason for audit logging
    /// </summary>
    private static string? GetBlockReason(VettingStatus status)
    {
        return status switch
        {
            VettingStatus.OnHold => "Application on hold",
            VettingStatus.Denied => "Application denied",
            VettingStatus.Withdrawn => "Application withdrawn",
            _ => null
        };
    }

    /// <summary>
    /// Get user-facing block message
    /// </summary>
    private static string? GetBlockMessage(VettingStatus status)
    {
        return status switch
        {
            VettingStatus.OnHold => "Your application is on hold. Please contact support@witchcityrope.com to provide additional information and reactivate your application.",
            VettingStatus.Denied => "Your vetting application was denied. You cannot participate in community events at this time. Decisions are final and there is no appeal process.",
            VettingStatus.Withdrawn => "You have withdrawn your application. Please submit a new application if you would like to join the community.",
            _ => null
        };
    }
}
```

#### EmailService

```csharp
// File: /apps/api/Features/Vetting/Services/IEmailService.cs

namespace WitchCityRope.Api.Features.Vetting.Services;

/// <summary>
/// Service for email operations
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send status change email
    /// </summary>
    Task<Result<EmailLogDto>> SendStatusChangeEmailAsync(
        Guid applicationId,
        VettingStatus newStatus,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send application submission confirmation
    /// </summary>
    Task<Result<EmailLogDto>> SendApplicationSubmittedEmailAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send reminder email
    /// </summary>
    Task<Result<EmailLogDto>> SendReminderEmailAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all email templates
    /// </summary>
    Task<Result<List<EmailTemplateDto>>> GetAllTemplatesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get specific template
    /// </summary>
    Task<Result<EmailTemplateDto>> GetTemplateAsync(
        EmailTemplateType templateType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update template
    /// </summary>
    Task<Result<EmailTemplateDto>> UpdateTemplateAsync(
        EmailTemplateType templateType,
        UpdateEmailTemplateRequest request,
        Guid updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset template to default
    /// </summary>
    Task<Result<EmailTemplateDto>> ResetTemplateAsync(
        EmailTemplateType templateType,
        Guid updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Preview template with sample data
    /// </summary>
    Task<Result<string>> PreviewTemplateAsync(
        EmailTemplateType templateType,
        CancellationToken cancellationToken = default);
}
```

```csharp
// File: /apps/api/Features/Vetting/Services/EmailService.cs

namespace WitchCityRope.Api.Features.Vetting.Services;

/// <summary>
/// Implementation of email service with SendGrid
/// </summary>
public class EmailService : IEmailService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;
    private readonly SendGridClient _sendGridClient;

    public EmailService(
        ApplicationDbContext context,
        ILogger<EmailService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;

        var apiKey = configuration["SendGrid:ApiKey"];
        _sendGridClient = new SendGridClient(apiKey);
    }

    public async Task<Result<EmailLogDto>> SendStatusChangeEmailAsync(
        Guid applicationId,
        VettingStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get application
            var application = await _context.VettingApplications
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == applicationId, cancellationToken);

            if (application == null)
            {
                return Result<EmailLogDto>.Failure(
                    "Application not found", $"No application found with ID {applicationId}");
            }

            // Get template based on status
            var templateType = GetTemplateTypeForStatus(newStatus);
            var template = await GetTemplateAsync(templateType, cancellationToken);

            if (!template.Success)
            {
                return Result<EmailLogDto>.Failure(
                    "Template not found", $"No template found for status {newStatus}");
            }

            // Replace variables in template
            var subject = ReplaceTemplateVariables(template.Value!.Subject, application);
            var body = ReplaceTemplateVariables(template.Value!.Body, application);

            // Send email via SendGrid
            var sendResult = await SendEmailViaSendGridAsync(
                application.Email,
                subject,
                body,
                cancellationToken);

            // Log email delivery
            var emailLog = new VettingEmailLog
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                RecipientEmail = application.Email,
                TemplateType = templateType,
                SentAt = DateTime.UtcNow,
                Status = sendResult.Success ? EmailDeliveryStatus.Success : EmailDeliveryStatus.Failed,
                SendGridMessageId = sendResult.MessageId,
                ErrorMessage = sendResult.Success ? null : sendResult.Error,
                RetryCount = 0
            };

            _context.VettingEmailLogs.Add(emailLog);
            await _context.SaveChangesAsync(cancellationToken);

            var emailLogDto = new EmailLogDto
            {
                Id = emailLog.Id,
                ApplicationId = emailLog.ApplicationId,
                RecipientEmail = emailLog.RecipientEmail,
                TemplateType = emailLog.TemplateType,
                SentAt = emailLog.SentAt,
                Status = emailLog.Status,
                SendGridMessageId = emailLog.SendGridMessageId,
                ErrorMessage = emailLog.ErrorMessage,
                RetryCount = emailLog.RetryCount
            };

            if (!sendResult.Success)
            {
                _logger.LogWarning("Email delivery failed for application {ApplicationId}: {Error}",
                    applicationId, sendResult.Error);

                return Result<EmailLogDto>.Failure(
                    "Email delivery failed", sendResult.Error);
            }

            _logger.LogInformation("Email sent successfully for application {ApplicationId}. MessageId: {MessageId}",
                applicationId, sendResult.MessageId);

            return Result<EmailLogDto>.Success(emailLogDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending status change email for application {ApplicationId}", applicationId);
            return Result<EmailLogDto>.Failure(
                "Failed to send email", ex.Message);
        }
    }

    private async Task<(bool Success, string? MessageId, string? Error)> SendEmailViaSendGridAsync(
        string toEmail,
        string subject,
        string htmlContent,
        CancellationToken cancellationToken)
    {
        try
        {
            var from = new EmailAddress(
                _configuration["SendGrid:FromEmail"] ?? "noreply@witchcityrope.com",
                "WitchCityRope");

            var to = new EmailAddress(toEmail);

            var msg = MailHelper.CreateSingleEmail(
                from,
                to,
                subject,
                plainTextContent: ConvertHtmlToPlainText(htmlContent),
                htmlContent);

            // Set reply-to
            msg.SetReplyTo(new EmailAddress(
                _configuration["SendGrid:ReplyToEmail"] ?? "support@witchcityrope.com",
                "WitchCityRope Support"));

            var response = await _sendGridClient.SendEmailAsync(msg, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var messageId = response.Headers.GetValues("X-Message-Id")?.FirstOrDefault();
                return (true, messageId, null);
            }
            else
            {
                var errorBody = await response.Body.ReadAsStringAsync(cancellationToken);
                _logger.LogError("SendGrid API error: {StatusCode} - {Error}",
                    response.StatusCode, errorBody);

                return (false, null, $"SendGrid error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending email via SendGrid to {Email}", toEmail);
            return (false, null, ex.Message);
        }
    }

    private static EmailTemplateType GetTemplateTypeForStatus(VettingStatus status)
    {
        return status switch
        {
            VettingStatus.InterviewApproved => EmailTemplateType.InterviewApproved,
            VettingStatus.Approved => EmailTemplateType.ApplicationApproved,
            VettingStatus.OnHold => EmailTemplateType.ApplicationOnHold,
            VettingStatus.Denied => EmailTemplateType.ApplicationDenied,
            _ => EmailTemplateType.ApplicationReceived
        };
    }

    private static string ReplaceTemplateVariables(string template, VettingApplication application)
    {
        return template
            .Replace("{{applicant_name}}", application.SceneName)
            .Replace("{{real_name}}", application.RealName)
            .Replace("{{application_number}}", application.ApplicationNumber)
            .Replace("{{submission_date}}", application.SubmittedAt.ToString("MMMM dd, yyyy"))
            .Replace("{{status_change_date}}", DateTime.UtcNow.ToString("MMMM dd, yyyy"))
            .Replace("{{contact_email}}", "support@witchcityrope.com")
            .Replace("{{current_year}}", DateTime.UtcNow.Year.ToString());
    }

    private static string ConvertHtmlToPlainText(string html)
    {
        // Simple HTML to plain text conversion
        // In production, use a library like HtmlAgilityPack
        return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
    }

    // Additional method implementations...
}
```

---

## Frontend Component Specifications

### React Hooks

#### useVettingAccessControl Hook

```typescript
// File: /apps/web/src/features/vetting/hooks/useVettingAccessControl.ts

import { useQuery } from '@tanstack/react-query';
import { vettingApiClient } from '../services/vettingApiClient';
import { useAuth } from '@/features/auth/hooks/useAuth';

export interface VettingAccessCheck {
  canRsvp: boolean;
  canPurchaseTickets: boolean;
  vettingStatus: string;
  blockReason?: string;
  blockMessage?: string;
}

/**
 * Hook for checking vetting-based access control
 */
export function useVettingAccessControl(eventId?: string) {
  const { user } = useAuth();

  return useQuery({
    queryKey: ['vetting', 'access-control', user?.id, eventId],
    queryFn: async () => {
      if (!user?.id) {
        return {
          canRsvp: false,
          canPurchaseTickets: false,
          vettingStatus: 'Draft',
          blockReason: 'Not authenticated',
          blockMessage: 'Please log in to access events.',
        };
      }

      if (eventId) {
        return vettingApiClient.checkEventAccess(user.id, eventId);
      }

      return vettingApiClient.checkAccess(user.id);
    },
    enabled: !!user,
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
  });
}
```

#### useVettingApplications Hook (Admin)

```typescript
// File: /apps/web/src/features/vetting/hooks/useVettingApplications.ts

import { useQuery } from '@tanstack/react-query';
import { vettingApiClient } from '../services/vettingApiClient';
import type { ApplicationFilterRequest, PagedResult, ApplicationSummaryDto } from '../types/vetting.types';

/**
 * Hook for fetching admin application list with filtering
 */
export function useVettingApplications(filters: ApplicationFilterRequest) {
  return useQuery<PagedResult<ApplicationSummaryDto>>({
    queryKey: ['vetting', 'applications', filters],
    queryFn: () => vettingApiClient.getApplicationsForReview(filters),
    staleTime: 30 * 1000, // 30 seconds
    gcTime: 5 * 60 * 1000, // 5 minutes
  });
}
```

#### useStatusChange Mutation

```typescript
// File: /apps/web/src/features/vetting/hooks/useStatusChange.ts

import { useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { vettingApiClient } from '../services/vettingApiClient';
import type { ReviewDecisionRequest, ReviewDecisionResponse } from '../types/vetting.types';

/**
 * Hook for changing application status
 */
export function useStatusChange() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      applicationId,
      request
    }: {
      applicationId: string;
      request: ReviewDecisionRequest;
    }) => {
      return vettingApiClient.changeApplicationStatus(applicationId, request);
    },
    onSuccess: (data, variables) => {
      // Invalidate queries to refresh data
      queryClient.invalidateQueries({ queryKey: ['vetting', 'applications'] });
      queryClient.invalidateQueries({
        queryKey: ['vetting', 'application', variables.applicationId]
      });

      // Show success notification
      notifications.show({
        title: 'Status Changed',
        message: data.confirmationMessage,
        color: 'green',
      });
    },
    onError: (error: Error) => {
      notifications.show({
        title: 'Status Change Failed',
        message: error.message || 'Failed to change application status',
        color: 'red',
      });
    },
  });
}
```

### Main Components

#### VettingAdminGrid Component

```typescript
// File: /apps/web/src/features/vetting/components/VettingAdminGrid.tsx

import React, { useState } from 'react';
import {
  Table,
  Badge,
  TextInput,
  Select,
  Group,
  Stack,
  Pagination,
  Text,
  ActionIcon,
  Checkbox,
} from '@mantine/core';
import { IconSearch, IconEye, IconMailForward } from '@tabler/icons-react';
import { useVettingApplications } from '../hooks/useVettingApplications';
import type { ApplicationFilterRequest } from '../types/vetting.types';

/**
 * Admin grid for reviewing vetting applications
 */
export function VettingAdminGrid() {
  const [filters, setFilters] = useState<ApplicationFilterRequest>({
    page: 1,
    pageSize: 25,
    sortBy: 'SubmittedAt',
    sortDirection: 'desc',
    statusFilters: [],
    searchQuery: '',
  });

  const [selectedIds, setSelectedIds] = useState<string[]>([]);

  const { data, isLoading, error } = useVettingApplications(filters);

  const handleSearch = (value: string) => {
    setFilters(prev => ({ ...prev, searchQuery: value, page: 1 }));
  };

  const handleStatusFilter = (value: string | null) => {
    setFilters(prev => ({
      ...prev,
      statusFilters: value ? [value] : [],
      page: 1,
    }));
  };

  const handleSort = (column: string) => {
    setFilters(prev => ({
      ...prev,
      sortBy: column,
      sortDirection: prev.sortBy === column && prev.sortDirection === 'asc'
        ? 'desc'
        : 'asc',
    }));
  };

  const handleSelectAll = () => {
    if (selectedIds.length === data?.items.length) {
      setSelectedIds([]);
    } else {
      setSelectedIds(data?.items.map(app => app.id) || []);
    }
  };

  const handleSelectRow = (id: string) => {
    setSelectedIds(prev =>
      prev.includes(id)
        ? prev.filter(selectedId => selectedId !== id)
        : [...prev, id]
    );
  };

  if (isLoading) {
    return <Text>Loading applications...</Text>;
  }

  if (error) {
    return <Text c="red">Error loading applications: {error.message}</Text>;
  }

  return (
    <Stack gap="md">
      {/* Filters */}
      <Group>
        <TextInput
          placeholder="Search by name, email, or ID"
          leftSection={<IconSearch size={16} />}
          value={filters.searchQuery}
          onChange={(e) => handleSearch(e.currentTarget.value)}
          style={{ flex: 1 }}
        />
        <Select
          placeholder="Filter by status"
          data={[
            { value: '', label: 'All Statuses' },
            { value: 'Submitted', label: 'Submitted' },
            { value: 'UnderReview', label: 'Under Review' },
            { value: 'InterviewApproved', label: 'Interview Approved' },
            { value: 'PendingInterview', label: 'Pending Interview' },
            { value: 'InterviewScheduled', label: 'Interview Scheduled' },
            { value: 'OnHold', label: 'On Hold' },
            { value: 'Approved', label: 'Approved' },
            { value: 'Denied', label: 'Denied' },
          ]}
          value={filters.statusFilters[0] || ''}
          onChange={handleStatusFilter}
          clearable
          w={200}
        />
      </Group>

      {/* Bulk Actions */}
      {selectedIds.length > 0 && (
        <Group>
          <Text size="sm" fw={500}>
            {selectedIds.length} application(s) selected
          </Text>
          <ActionIcon
            variant="light"
            color="blue"
            onClick={() => {/* Handle bulk reminder */}}
          >
            <IconMailForward size={16} />
          </ActionIcon>
        </Group>
      )}

      {/* Table */}
      <Table highlightOnHover>
        <Table.Thead>
          <Table.Tr>
            <Table.Th w={40}>
              <Checkbox
                checked={selectedIds.length === data?.items.length}
                indeterminate={
                  selectedIds.length > 0 && selectedIds.length < (data?.items.length || 0)
                }
                onChange={handleSelectAll}
              />
            </Table.Th>
            <Table.Th
              onClick={() => handleSort('SceneName')}
              style={{ cursor: 'pointer' }}
            >
              Scene Name
            </Table.Th>
            <Table.Th>Email</Table.Th>
            <Table.Th
              onClick={() => handleSort('Status')}
              style={{ cursor: 'pointer' }}
            >
              Status
            </Table.Th>
            <Table.Th
              onClick={() => handleSort('SubmittedAt')}
              style={{ cursor: 'pointer' }}
            >
              Submitted
            </Table.Th>
            <Table.Th>Days in Status</Table.Th>
            <Table.Th w={100}>Actions</Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {data?.items.map((application) => (
            <Table.Tr key={application.id}>
              <Table.Td>
                <Checkbox
                  checked={selectedIds.includes(application.id)}
                  onChange={() => handleSelectRow(application.id)}
                />
              </Table.Td>
              <Table.Td>{application.sceneName}</Table.Td>
              <Table.Td>{application.email}</Table.Td>
              <Table.Td>
                <Badge color={getStatusColor(application.status)}>
                  {application.status}
                </Badge>
              </Table.Td>
              <Table.Td>
                {new Date(application.submittedAt).toLocaleDateString()}
              </Table.Td>
              <Table.Td>{application.daysInCurrentStatus}</Table.Td>
              <Table.Td>
                <ActionIcon
                  variant="subtle"
                  onClick={() => {/* Navigate to detail view */}}
                >
                  <IconEye size={16} />
                </ActionIcon>
              </Table.Td>
            </Table.Tr>
          ))}
        </Table.Tbody>
      </Table>

      {/* Pagination */}
      <Group justify="space-between">
        <Text size="sm" c="dimmed">
          Showing {data?.items.length || 0} of {data?.totalCount || 0} applications
        </Text>
        <Pagination
          value={filters.page}
          onChange={(page) => setFilters(prev => ({ ...prev, page }))}
          total={Math.ceil((data?.totalCount || 0) / filters.pageSize)}
        />
      </Group>
    </Stack>
  );
}

function getStatusColor(status: string): string {
  const colorMap: Record<string, string> = {
    Draft: 'gray',
    Submitted: 'blue',
    UnderReview: 'cyan',
    InterviewApproved: 'teal',
    PendingInterview: 'indigo',
    InterviewScheduled: 'violet',
    OnHold: 'yellow',
    Approved: 'green',
    Denied: 'red',
    Withdrawn: 'gray',
  };

  return colorMap[status] || 'gray';
}
```

#### VettingAccessControl Component

```typescript
// File: /apps/web/src/features/vetting/components/VettingAccessControl.tsx

import React from 'react';
import { Alert, Button, Stack, Text } from '@mantine/core';
import { IconAlertCircle, IconMail } from '@tabler/icons-react';
import { useVettingAccessControl } from '../hooks/useVettingAccessControl';

interface VettingAccessControlProps {
  eventId?: string;
  actionType: 'rsvp' | 'ticket';
  children: (canAccess: boolean) => React.ReactNode;
}

/**
 * Access control wrapper for RSVP and ticket purchases
 */
export function VettingAccessControl({
  eventId,
  actionType,
  children,
}: VettingAccessControlProps) {
  const { data: accessCheck, isLoading } = useVettingAccessControl(eventId);

  if (isLoading) {
    return children(false);
  }

  const canAccess = actionType === 'rsvp'
    ? accessCheck?.canRsvp
    : accessCheck?.canPurchaseTickets;

  if (!canAccess && accessCheck?.blockMessage) {
    return (
      <Stack gap="md">
        <Alert
          icon={<IconAlertCircle size={16} />}
          title="Access Restricted"
          color="red"
        >
          <Text size="sm">{accessCheck.blockMessage}</Text>
        </Alert>

        {accessCheck.vettingStatus === 'OnHold' && (
          <Button
            leftSection={<IconMail size={16} />}
            variant="light"
            component="a"
            href="mailto:support@witchcityrope.com"
          >
            Contact Support
          </Button>
        )}
      </Stack>
    );
  }

  return <>{children(canAccess || false)}</>;
}
```

---

## Email System Integration

### SendGrid Configuration

#### Application Settings (appsettings.json)
```json
{
  "SendGrid": {
    "ApiKey": "${SENDGRID_API_KEY}", // Environment variable
    "FromEmail": "noreply@witchcityrope.com",
    "FromName": "WitchCityRope",
    "ReplyToEmail": "support@witchcityrope.com",
    "ReplyToName": "WitchCityRope Support",
    "RateLimitPerMinute": 100,
    "RetryAttempts": 3,
    "RetryDelayMs": 1000,
    "TimeoutSeconds": 30
  }
}
```

#### Service Registration
```csharp
// File: /apps/api/Program.cs

// Add SendGrid email service
builder.Services.AddSingleton<ISendGridClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var apiKey = configuration["SendGrid:ApiKey"];

    if (string.IsNullOrEmpty(apiKey))
    {
        throw new InvalidOperationException(
            "SendGrid API key is not configured. Set SENDGRID_API_KEY environment variable.");
    }

    return new SendGridClient(apiKey);
});

builder.Services.AddScoped<IEmailService, EmailService>();
```

### Email Templates

#### Template Variable System
```csharp
/// <summary>
/// Available template variables for email rendering
/// </summary>
public static class EmailTemplateVariables
{
    public const string ApplicantName = "{{applicant_name}}";
    public const string RealName = "{{real_name}}";
    public const string ApplicationNumber = "{{application_number}}";
    public const string SubmissionDate = "{{submission_date}}";
    public const string StatusChangeDate = "{{status_change_date}}";
    public const string ContactEmail = "{{contact_email}}";
    public const string CurrentYear = "{{current_year}}";
    public const string AdminName = "{{admin_name}}";
    public const string InterviewDate = "{{interview_date}}";

    /// <summary>
    /// Get all available variables for a template type
    /// </summary>
    public static List<string> GetAvailableVariables(EmailTemplateType templateType)
    {
        var commonVariables = new List<string>
        {
            ApplicantName,
            ApplicationNumber,
            ContactEmail,
            CurrentYear
        };

        return templateType switch
        {
            EmailTemplateType.ApplicationReceived => commonVariables
                .Concat(new[] { SubmissionDate })
                .ToList(),
            EmailTemplateType.InterviewApproved => commonVariables
                .Concat(new[] { StatusChangeDate, AdminName })
                .ToList(),
            EmailTemplateType.ApplicationApproved => commonVariables
                .Concat(new[] { StatusChangeDate, AdminName })
                .ToList(),
            EmailTemplateType.ApplicationOnHold => commonVariables
                .Concat(new[] { StatusChangeDate, AdminName })
                .ToList(),
            EmailTemplateType.ApplicationDenied => commonVariables
                .Concat(new[] { StatusChangeDate })
                .ToList(),
            EmailTemplateType.InterviewReminder => commonVariables
                .Concat(new[] { InterviewDate })
                .ToList(),
            _ => commonVariables
        };
    }
}
```

#### Default Email Templates

```csharp
/// <summary>
/// Default email templates for vetting system
/// </summary>
public static class DefaultEmailTemplates
{
    public static VettingEmailTemplate ApplicationReceived => new()
    {
        TemplateType = EmailTemplateType.ApplicationReceived,
        Subject = "Your WitchCityRope Vetting Application - Received",
        Body = @"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #880124;'>Application Received</h2>

        <p>Dear {{applicant_name}},</p>

        <p>Thank you for submitting your vetting application to WitchCityRope. We have received your application and it is now under review.</p>

        <p><strong>Application Number:</strong> {{application_number}}<br>
        <strong>Submission Date:</strong> {{submission_date}}</p>

        <h3 style='color: #880124;'>What Happens Next?</h3>
        <ol>
            <li>Your application will be reviewed by our vetting committee</li>
            <li>References will be contacted within 3-5 business days</li>
            <li>You'll receive an update on your application status within 14 days</li>
        </ol>

        <p>If you have any questions, please don't hesitate to contact us at <a href='mailto:{{contact_email}}'>{{contact_email}}</a>.</p>

        <p>Best regards,<br>
        The WitchCityRope Team</p>

        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
        <p style='font-size: 12px; color: #666;'>© {{current_year}} WitchCityRope. All rights reserved.</p>
    </div>
</body>
</html>",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public static VettingEmailTemplate InterviewApproved => new()
    {
        TemplateType = EmailTemplateType.InterviewApproved,
        Subject = "Your WitchCityRope Application - Interview Approved",
        Body = @"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #880124;'>Interview Approved!</h2>

        <p>Dear {{applicant_name}},</p>

        <p>Great news! Your vetting application has been approved for an interview.</p>

        <p><strong>Application Number:</strong> {{application_number}}</p>

        <h3 style='color: #880124;'>Next Steps</h3>
        <p>A member of our vetting team will contact you within 3-5 business days to schedule your interview. The interview typically takes 30-45 minutes and can be conducted in person or via video call.</p>

        <h3 style='color: #880124;'>What to Expect</h3>
        <ul>
            <li>Discussion of your experience and interests in rope bondage</li>
            <li>Review of community guidelines and expectations</li>
            <li>Opportunity to ask questions about WitchCityRope</li>
            <li>Understanding of consent and safety practices</li>
        </ul>

        <p>If you have any questions or need to provide additional information, please contact us at <a href='mailto:{{contact_email}}'>{{contact_email}}</a>.</p>

        <p>We look forward to speaking with you!</p>

        <p>Best regards,<br>
        The WitchCityRope Vetting Team</p>

        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
        <p style='font-size: 12px; color: #666;'>© {{current_year}} WitchCityRope. All rights reserved.</p>
    </div>
</body>
</html>",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public static VettingEmailTemplate ApplicationApproved => new()
    {
        TemplateType = EmailTemplateType.ApplicationApproved,
        Subject = "Welcome to WitchCityRope - Application Approved!",
        Body = @"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #880124;'>Welcome to WitchCityRope!</h2>

        <p>Dear {{applicant_name}},</p>

        <p>Congratulations! Your vetting application has been approved. Welcome to the WitchCityRope community!</p>

        <p><strong>Application Number:</strong> {{application_number}}<br>
        <strong>Approval Date:</strong> {{status_change_date}}</p>

        <h3 style='color: #880124;'>Your Member Benefits</h3>
        <ul>
            <li>Access to all member events and workshops</li>
            <li>Participation in skill-building classes</li>
            <li>Connection with experienced practitioners</li>
            <li>Access to equipment and practice spaces</li>
            <li>Invitation to community social events</li>
        </ul>

        <h3 style='color: #880124;'>Getting Started</h3>
        <ol>
            <li>Log in to your account to access the member dashboard</li>
            <li>Browse upcoming events and workshops</li>
            <li>Review our community guidelines</li>
            <li>Consider volunteering at an upcoming event</li>
        </ol>

        <p>We're excited to have you as part of our community. If you have any questions, please contact us at <a href='mailto:{{contact_email}}'>{{contact_email}}</a>.</p>

        <p>Welcome aboard!</p>

        <p>Best regards,<br>
        The WitchCityRope Team</p>

        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
        <p style='font-size: 12px; color: #666;'>© {{current_year}} WitchCityRope. All rights reserved.</p>
    </div>
</body>
</html>",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public static VettingEmailTemplate ApplicationOnHold => new()
    {
        TemplateType = EmailTemplateType.ApplicationOnHold,
        Subject = "Your WitchCityRope Application - On Hold",
        Body = @"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #DAA520;'>Application On Hold</h2>

        <p>Dear {{applicant_name}},</p>

        <p>Thank you for your interest in WitchCityRope. Your application ({{application_number}}) has been placed on hold pending additional information.</p>

        <h3 style='color: #DAA520;'>Next Steps</h3>
        <p>Please contact us at <a href='mailto:{{contact_email}}'>{{contact_email}}</a> to discuss your application and provide any additional information that may be needed.</p>

        <p>Common reasons for applications being placed on hold include:</p>
        <ul>
            <li>Incomplete reference information</li>
            <li>Need for additional clarification about experience</li>
            <li>Scheduling conflicts for interview</li>
            <li>Additional questions about community fit</li>
        </ul>

        <p>We're here to help and look forward to hearing from you soon.</p>

        <p>Best regards,<br>
        The WitchCityRope Vetting Team</p>

        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
        <p style='font-size: 12px; color: #666;'>© {{current_year}} WitchCityRope. All rights reserved.</p>
    </div>
</body>
</html>",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public static VettingEmailTemplate ApplicationDenied => new()
    {
        TemplateType = EmailTemplateType.ApplicationDenied,
        Subject = "Your WitchCityRope Application - Decision",
        Body = @"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #880124;'>Application Decision</h2>

        <p>Dear {{applicant_name}},</p>

        <p>Thank you for your interest in WitchCityRope. After careful consideration, we have decided not to approve your application ({{application_number}}) at this time.</p>

        <p>This decision was not made lightly, and we appreciate the time and effort you put into your application. Our vetting process is designed to ensure the safety and compatibility of all community members.</p>

        <h3 style='color: #880124;'>Future Opportunities</h3>
        <p>While your current application was not approved, you may be eligible to reapply in the future. We recommend:</p>
        <ul>
            <li>Gaining additional experience in rope bondage and BDSM communities</li>
            <li>Building connections with experienced practitioners</li>
            <li>Attending educational workshops and classes</li>
            <li>Considering reapplication after 6-12 months</li>
        </ul>

        <p>We wish you the best in your journey and growth within the rope bondage community.</p>

        <p>If you have questions about this decision, please contact us at <a href='mailto:{{contact_email}}'>{{contact_email}}</a>.</p>

        <p>Best regards,<br>
        The WitchCityRope Team</p>

        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
        <p style='font-size: 12px; color: #666;'>© {{current_year}} WitchCityRope. All rights reserved.</p>
    </div>
</body>
</html>",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public static VettingEmailTemplate InterviewReminder => new()
    {
        TemplateType = EmailTemplateType.InterviewReminder,
        Subject = "Reminder: Schedule Your WitchCityRope Interview",
        Body = @"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #880124;'>Interview Scheduling Reminder</h2>

        <p>Dear {{applicant_name}},</p>

        <p>This is a friendly reminder that your vetting application ({{application_number}}) was approved for an interview, but we haven't yet received your scheduling preferences.</p>

        <h3 style='color: #880124;'>Action Required</h3>
        <p>Please contact us at <a href='mailto:{{contact_email}}'>{{contact_email}}</a> to schedule your interview at your earliest convenience.</p>

        <p>The interview typically takes 30-45 minutes and can be conducted:</p>
        <ul>
            <li>In person at our regular meeting location</li>
            <li>Via video call (Zoom, Google Meet, etc.)</li>
            <li>By phone if other options aren't available</li>
        </ul>

        <p>We're looking forward to speaking with you and learning more about your interest in WitchCityRope!</p>

        <p>Best regards,<br>
        The WitchCityRope Vetting Team</p>

        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
        <p style='font-size: 12px; color: #666;'>© {{current_year}} WitchCityRope. All rights reserved.</p>
    </div>
</body>
</html>",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    /// <summary>
    /// Get all default templates for seeding
    /// </summary>
    public static List<VettingEmailTemplate> GetAllDefaults()
    {
        return new List<VettingEmailTemplate>
        {
            ApplicationReceived,
            InterviewApproved,
            ApplicationApproved,
            ApplicationOnHold,
            ApplicationDenied,
            InterviewReminder
        };
    }
}
```

### Email Delivery Tracking

#### Email Log Entity Configuration
```csharp
// File: /apps/api/Features/Vetting/Configurations/VettingEmailLogConfiguration.cs

namespace WitchCityRope.Api.Features.Vetting.Configurations;

public class VettingEmailLogConfiguration : IEntityTypeConfiguration<VettingEmailLog>
{
    public void Configure(EntityTypeBuilder<VettingEmailLog> builder)
    {
        builder.ToTable("VettingEmailLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RecipientEmail)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.SendGridMessageId)
            .HasMaxLength(255);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(e => e.SentAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        // Indexes for common queries
        builder.HasIndex(e => e.ApplicationId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.SentAt);
        builder.HasIndex(e => e.TemplateType);

        // Foreign key to VettingApplication
        builder.HasOne<VettingApplication>()
            .WithMany()
            .HasForeignKey(e => e.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

## Database Schema Extensions

### New Entities

#### VettingEmailTemplate Entity
```csharp
// File: /apps/api/Features/Vetting/Entities/VettingEmailTemplate.cs

namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Email template entity for vetting system
/// </summary>
public class VettingEmailTemplate
{
    public Guid Id { get; set; }
    public EmailTemplateType TemplateType { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; } // FK to ApplicationUser
}

/// <summary>
/// Email template types for vetting workflow
/// </summary>
public enum EmailTemplateType
{
    ApplicationReceived = 1,
    InterviewApproved = 2,
    ApplicationApproved = 3,
    ApplicationOnHold = 4,
    ApplicationDenied = 5,
    InterviewReminder = 6
}
```

#### VettingEmailLog Entity
```csharp
// File: /apps/api/Features/Vetting/Entities/VettingEmailLog.cs

namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Email delivery log entity
/// </summary>
public class VettingEmailLog
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; } // FK to VettingApplication
    public string RecipientEmail { get; set; } = string.Empty;
    public EmailTemplateType TemplateType { get; set; }
    public DateTime SentAt { get; set; }
    public EmailDeliveryStatus Status { get; set; }
    public string? SendGridMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
}

/// <summary>
/// Email delivery status
/// </summary>
public enum EmailDeliveryStatus
{
    Success = 1,
    Failed = 2,
    Pending = 3
}
```

### EF Core Migration Script

```csharp
// File: /apps/api/Migrations/YYYYMMDDHHMMSS_AddVettingEmailSystem.cs

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVettingEmailSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create VettingEmailTemplates table
            migrationBuilder.CreateTable(
                name: "VettingEmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateType = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingEmailTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VettingEmailTemplates_AspNetUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Create VettingEmailLogs table
            migrationBuilder.CreateTable(
                name: "VettingEmailLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TemplateType = table.Column<int>(type: "integer", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SendGridMessageId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingEmailLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VettingEmailLogs_VettingApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "VettingApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_VettingEmailTemplates_TemplateType",
                table: "VettingEmailTemplates",
                column: "TemplateType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VettingEmailTemplates_UpdatedBy",
                table: "VettingEmailTemplates",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VettingEmailLogs_ApplicationId",
                table: "VettingEmailLogs",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingEmailLogs_Status",
                table: "VettingEmailLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VettingEmailLogs_SentAt",
                table: "VettingEmailLogs",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_VettingEmailLogs_TemplateType",
                table: "VettingEmailLogs",
                column: "TemplateType");

            // Seed default email templates
            SeedDefaultTemplates(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "VettingEmailLogs");
            migrationBuilder.DropTable(name: "VettingEmailTemplates");
        }

        private void SeedDefaultTemplates(MigrationBuilder migrationBuilder)
        {
            var systemUserId = Guid.NewGuid(); // System user for initial templates
            var now = DateTime.UtcNow;

            migrationBuilder.InsertData(
                table: "VettingEmailTemplates",
                columns: new[] { "Id", "TemplateType", "Subject", "Body", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { Guid.NewGuid(), 1, "Your WitchCityRope Vetting Application - Received",
                      "<html><!-- Application Received template HTML --></html>",
                      true, now, now, systemUserId },
                    { Guid.NewGuid(), 2, "Your WitchCityRope Application - Interview Approved",
                      "<html><!-- Interview Approved template HTML --></html>",
                      true, now, now, systemUserId },
                    { Guid.NewGuid(), 3, "Welcome to WitchCityRope - Application Approved!",
                      "<html><!-- Application Approved template HTML --></html>",
                      true, now, now, systemUserId },
                    { Guid.NewGuid(), 4, "Your WitchCityRope Application - On Hold",
                      "<html><!-- Application OnHold template HTML --></html>",
                      true, now, now, systemUserId },
                    { Guid.NewGuid(), 5, "Your WitchCityRope Application - Decision",
                      "<html><!-- Application Denied template HTML --></html>",
                      true, now, now, systemUserId },
                    { Guid.NewGuid(), 6, "Reminder: Schedule Your WitchCityRope Interview",
                      "<html><!-- Interview Reminder template HTML --></html>",
                      true, now, now, systemUserId }
                });
        }
    }
}
```

---

## Access Control Logic

### Status Transition Matrix

```csharp
// File: /apps/api/Features/Vetting/Services/StatusTransitionValidator.cs

namespace WitchCityRope.Api.Features.Vetting.Services;

/// <summary>
/// Validates vetting status transitions
/// </summary>
public class StatusTransitionValidator
{
    /// <summary>
    /// Get valid transitions from current status
    /// </summary>
    public static List<VettingStatus> GetValidTransitions(VettingStatus currentStatus)
    {
        return currentStatus switch
        {
            VettingStatus.Draft => new List<VettingStatus>
            {
                VettingStatus.Submitted
            },
            VettingStatus.Submitted => new List<VettingStatus>
            {
                VettingStatus.UnderReview
            },
            VettingStatus.UnderReview => new List<VettingStatus>
            {
                VettingStatus.InterviewApproved,
                VettingStatus.OnHold,
                VettingStatus.Denied
            },
            VettingStatus.InterviewApproved => new List<VettingStatus>
            {
                VettingStatus.InterviewScheduled,
                VettingStatus.Approved,
                VettingStatus.OnHold,
                VettingStatus.Denied
            },
            VettingStatus.PendingInterview => new List<VettingStatus>
            {
                VettingStatus.InterviewScheduled,
                VettingStatus.Approved,
                VettingStatus.OnHold,
                VettingStatus.Denied
            },
            VettingStatus.InterviewScheduled => new List<VettingStatus>
            {
                VettingStatus.Approved,
                VettingStatus.OnHold,
                VettingStatus.Denied
            },
            VettingStatus.OnHold => new List<VettingStatus>
            {
                VettingStatus.UnderReview,
                VettingStatus.InterviewApproved,
                VettingStatus.Denied
            },
            VettingStatus.Approved => new List<VettingStatus>
            {
                VettingStatus.OnHold // Admin correction only
            },
            VettingStatus.Denied => new List<VettingStatus>(), // Final state
            VettingStatus.Withdrawn => new List<VettingStatus>(), // User action only
            _ => new List<VettingStatus>()
        };
    }

    /// <summary>
    /// Validate if transition is allowed
    /// </summary>
    public static bool IsValidTransition(VettingStatus currentStatus, VettingStatus newStatus)
    {
        var validTransitions = GetValidTransitions(currentStatus);
        return validTransitions.Contains(newStatus);
    }

    /// <summary>
    /// Get user-friendly transition error message
    /// </summary>
    public static string GetTransitionErrorMessage(VettingStatus currentStatus, VettingStatus newStatus)
    {
        return $"Cannot transition from {currentStatus} to {newStatus}. " +
               $"Valid transitions from {currentStatus}: {string.Join(", ", GetValidTransitions(currentStatus))}";
    }
}
```

### RSVP Integration

```csharp
// File: /apps/api/Features/Events/Endpoints/RsvpEndpoints.cs (modification)

/// <summary>
/// Create RSVP with vetting access control
/// </summary>
private static async Task<IResult> CreateRsvp(
    CreateRsvpRequest request,
    IRsvpService rsvpService,
    IAccessControlService accessControlService,
    HttpContext httpContext,
    CancellationToken cancellationToken)
{
    var userId = httpContext.User.GetUserId();

    // Check vetting access BEFORE creating RSVP
    var accessCheck = await accessControlService.ValidateRsvpAccessAsync(
        userId,
        request.EventId,
        cancellationToken);

    if (!accessCheck.Success)
    {
        return Results.Json(
            new ApiResponse<RsvpDto>
            {
                Success = false,
                Message = "Access Denied",
                Errors = new List<string> { accessCheck.Message },
                Timestamp = DateTime.UtcNow
            },
            statusCode: StatusCodes.Status403Forbidden);
    }

    // Proceed with RSVP creation
    var result = await rsvpService.CreateRsvpAsync(request, userId, cancellationToken);

    if (!result.Success)
    {
        return Results.BadRequest(new ApiResponse<RsvpDto>
        {
            Success = false,
            Message = result.Message,
            Errors = result.Errors,
            Timestamp = DateTime.UtcNow
        });
    }

    return Results.Ok(new ApiResponse<RsvpDto>
    {
        Success = true,
        Data = result.Value,
        Message = "RSVP created successfully",
        Timestamp = DateTime.UtcNow
    });
}
```

### Ticket Purchase Integration

```csharp
// File: /apps/api/Features/Payments/Endpoints/PaymentEndpoints.cs (modification)

/// <summary>
/// Create payment intent with vetting access control
/// </summary>
private static async Task<IResult> CreatePaymentIntent(
    CreatePaymentIntentRequest request,
    IPaymentService paymentService,
    IAccessControlService accessControlService,
    HttpContext httpContext,
    CancellationToken cancellationToken)
{
    var userId = httpContext.User.GetUserId();

    // Check vetting access BEFORE processing payment
    var accessCheck = await accessControlService.ValidateTicketPurchaseAccessAsync(
        userId,
        request.EventId,
        cancellationToken);

    if (!accessCheck.Success)
    {
        return Results.Json(
            new ApiResponse<PaymentIntentDto>
            {
                Success = false,
                Message = "Access Denied",
                Errors = new List<string> { accessCheck.Message },
                Timestamp = DateTime.UtcNow
            },
            statusCode: StatusCodes.Status403Forbidden);
    }

    // Proceed with payment processing
    var result = await paymentService.CreatePaymentIntentAsync(
        request,
        userId,
        cancellationToken);

    if (!result.Success)
    {
        return Results.BadRequest(new ApiResponse<PaymentIntentDto>
        {
            Success = false,
            Message = result.Message,
            Errors = result.Errors,
            Timestamp = DateTime.UtcNow
        });
    }

    return Results.Ok(new ApiResponse<PaymentIntentDto>
    {
        Success = true,
        Data = result.Value,
        Message = "Payment intent created successfully",
        Timestamp = DateTime.UtcNow
    });
}
```

---

## Testing Strategy

### Unit Tests (80% Coverage Required)

#### AccessControlService Tests
```csharp
// File: /apps/api/Features/Vetting/Services/AccessControlService.Tests.cs

namespace WitchCityRope.Api.Features.Vetting.Services.Tests;

public class AccessControlServiceTests
{
    [Theory]
    [InlineData(VettingStatus.OnHold, false, false)]
    [InlineData(VettingStatus.Denied, false, false)]
    [InlineData(VettingStatus.Withdrawn, false, false)]
    [InlineData(VettingStatus.Submitted, true, true)]
    [InlineData(VettingStatus.UnderReview, true, true)]
    [InlineData(VettingStatus.Approved, true, true)]
    public async Task CheckAccess_ReturnsCorrectAccessBasedOnStatus(
        VettingStatus status,
        bool expectedCanRsvp,
        bool expectedCanPurchaseTickets)
    {
        // Arrange
        var (context, service) = SetupService();
        var userId = Guid.NewGuid();

        await context.VettingApplications.AddAsync(new VettingApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = status,
            Email = "test@example.com",
            SceneName = "TestUser"
        });
        await context.SaveChangesAsync();

        // Act
        var result = await service.CheckAccessAsync(userId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(expectedCanRsvp, result.Value!.CanRsvp);
        Assert.Equal(expectedCanPurchaseTickets, result.Value!.CanPurchaseTickets);
    }

    [Fact]
    public async Task CheckAccess_UserWithoutApplication_AllowsAccess()
    {
        // Arrange
        var (context, service) = SetupService();
        var userId = Guid.NewGuid();

        // No application for user

        // Act
        var result = await service.CheckAccessAsync(userId);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Value!.CanRsvp);
        Assert.True(result.Value!.CanPurchaseTickets);
        Assert.Equal(VettingStatus.Draft, result.Value!.VettingStatus);
    }

    [Theory]
    [InlineData(VettingStatus.OnHold, "Your application is on hold")]
    [InlineData(VettingStatus.Denied, "Your vetting application was denied")]
    [InlineData(VettingStatus.Withdrawn, "You have withdrawn your application")]
    public async Task CheckAccess_BlockedStatus_ReturnsAppropriateMessage(
        VettingStatus status,
        string expectedMessagePart)
    {
        // Arrange
        var (context, service) = SetupService();
        var userId = Guid.NewGuid();

        await context.VettingApplications.AddAsync(new VettingApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = status,
            Email = "test@example.com",
            SceneName = "TestUser"
        });
        await context.SaveChangesAsync();

        // Act
        var result = await service.CheckAccessAsync(userId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value!.BlockMessage);
        Assert.Contains(expectedMessagePart, result.Value!.BlockMessage);
    }

    private (ApplicationDbContext context, AccessControlService service) SetupService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<AccessControlService>>().Object;
        var service = new AccessControlService(context, logger);

        return (context, service);
    }
}
```

#### EmailService Tests
```csharp
// File: /apps/api/Features/Vetting/Services/EmailService.Tests.cs

namespace WitchCityRope.Api.Features.Vetting.Services.Tests;

public class EmailServiceTests
{
    [Fact]
    public async Task SendStatusChangeEmail_ValidApplication_SendsEmail()
    {
        // Arrange
        var (context, service, mockSendGrid) = SetupService();
        var applicationId = Guid.NewGuid();

        await context.VettingApplications.AddAsync(new VettingApplication
        {
            Id = applicationId,
            Email = "test@example.com",
            SceneName = "TestUser",
            Status = VettingStatus.UnderReview
        });

        await context.VettingEmailTemplates.AddAsync(new VettingEmailTemplate
        {
            Id = Guid.NewGuid(),
            TemplateType = EmailTemplateType.InterviewApproved,
            Subject = "Interview Approved",
            Body = "Hello {{applicant_name}}",
            IsActive = true
        });

        await context.SaveChangesAsync();

        // Configure SendGrid mock to return success
        mockSendGrid.Setup(x => x.SendEmailAsync(
                It.IsAny<SendGridMessage>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Response(
                System.Net.HttpStatusCode.OK,
                null,
                new Dictionary<string, IEnumerable<string>>
                {
                    { "X-Message-Id", new[] { "test-message-id" } }
                }));

        // Act
        var result = await service.SendStatusChangeEmailAsync(
            applicationId,
            VettingStatus.InterviewApproved);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(EmailDeliveryStatus.Success, result.Value.Status);
        Assert.Equal("test-message-id", result.Value.SendGridMessageId);

        // Verify email log was created
        var emailLog = await context.VettingEmailLogs
            .FirstOrDefaultAsync(e => e.ApplicationId == applicationId);
        Assert.NotNull(emailLog);
        Assert.Equal(EmailTemplateType.InterviewApproved, emailLog.TemplateType);
    }

    [Fact]
    public async Task SendStatusChangeEmail_SendGridFailure_LogsError()
    {
        // Arrange
        var (context, service, mockSendGrid) = SetupService();
        var applicationId = Guid.NewGuid();

        await context.VettingApplications.AddAsync(new VettingApplication
        {
            Id = applicationId,
            Email = "test@example.com",
            SceneName = "TestUser",
            Status = VettingStatus.UnderReview
        });

        await context.VettingEmailTemplates.AddAsync(new VettingEmailTemplate
        {
            Id = Guid.NewGuid(),
            TemplateType = EmailTemplateType.InterviewApproved,
            Subject = "Interview Approved",
            Body = "Hello {{applicant_name}}",
            IsActive = true
        });

        await context.SaveChangesAsync();

        // Configure SendGrid mock to return failure
        mockSendGrid.Setup(x => x.SendEmailAsync(
                It.IsAny<SendGridMessage>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Response(
                System.Net.HttpStatusCode.BadRequest,
                new StringContent("SendGrid error"),
                null));

        // Act
        var result = await service.SendStatusChangeEmailAsync(
            applicationId,
            VettingStatus.InterviewApproved);

        // Assert
        Assert.False(result.Success);

        // Verify email log was created with failed status
        var emailLog = await context.VettingEmailLogs
            .FirstOrDefaultAsync(e => e.ApplicationId == applicationId);
        Assert.NotNull(emailLog);
        Assert.Equal(EmailDeliveryStatus.Failed, emailLog.Status);
        Assert.NotNull(emailLog.ErrorMessage);
    }

    [Theory]
    [InlineData("{{applicant_name}}", "TestUser")]
    [InlineData("{{application_number}}", "VET-")]
    [InlineData("{{contact_email}}", "support@witchcityrope.com")]
    public async Task ReplaceTemplateVariables_ReplacesCorrectly(
        string variable,
        string expectedValue)
    {
        // Test template variable replacement logic
        // Implementation details...
    }

    private (ApplicationDbContext context, EmailService service, Mock<ISendGridClient> mockSendGrid)
        SetupService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<EmailService>>().Object;
        var configuration = new Mock<IConfiguration>();

        configuration.Setup(c => c["SendGrid:ApiKey"]).Returns("test-api-key");
        configuration.Setup(c => c["SendGrid:FromEmail"]).Returns("noreply@witchcityrope.com");
        configuration.Setup(c => c["SendGrid:ReplyToEmail"]).Returns("support@witchcityrope.com");

        var mockSendGrid = new Mock<ISendGridClient>();

        var service = new EmailService(context, logger, configuration.Object);

        return (context, service, mockSendGrid);
    }
}
```

### Integration Tests

#### Access Control Integration Tests
```csharp
// File: /tests/WitchCityRope.IntegrationTests/Vetting/AccessControlIntegrationTests.cs

namespace WitchCityRope.IntegrationTests.Vetting;

public class AccessControlIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AccessControlIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("OnHold", false, false)]
    [InlineData("Denied", false, false)]
    [InlineData("Approved", true, true)]
    public async Task GetAccessControl_ReturnsCorrectAccess(
        string status,
        bool expectedCanRsvp,
        bool expectedCanPurchaseTickets)
    {
        // Arrange
        var client = _factory.CreateClient();
        var userId = await CreateTestUserWithVettingStatus(client, status);

        // Act
        var response = await client.GetAsync($"/api/vetting/access-control/check/{userId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<VettingAccessCheckDto>>();

        Assert.NotNull(content);
        Assert.True(content.Success);
        Assert.Equal(expectedCanRsvp, content.Data.CanRsvp);
        Assert.Equal(expectedCanPurchaseTickets, content.Data.CanPurchaseTickets);
    }

    [Fact]
    public async Task CreateRsvp_DeniedUser_Returns403()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userId = await CreateTestUserWithVettingStatus(client, "Denied");
        var eventId = await CreateTestEvent(client);

        // Act
        var response = await client.PostAsJsonAsync("/api/events/rsvp", new
        {
            EventId = eventId,
            GuestCount = 1
        });

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<Guid> CreateTestUserWithVettingStatus(
        HttpClient client,
        string status)
    {
        // Test helper implementation...
        return Guid.NewGuid();
    }

    private async Task<Guid> CreateTestEvent(HttpClient client)
    {
        // Test helper implementation...
        return Guid.NewGuid();
    }
}
```

### E2E Tests (Playwright)

```typescript
// File: /apps/web/tests/e2e/vetting-workflow.spec.ts

import { test, expect } from '@playwright/test';

test.describe('Vetting Workflow', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('admin can review application and change status', async ({ page }) => {
    // Login as admin
    await page.click('text=Login');
    await page.fill('input[name="email"]', 'admin@witchcityrope.com');
    await page.fill('input[name="password"]', 'Test123!');
    await page.click('button[type="submit"]');

    // Navigate to vetting admin
    await page.click('text=Admin');
    await page.click('text=Vetting Applications');

    // Wait for grid to load
    await expect(page.locator('table')).toBeVisible();

    // Click first application
    await page.click('table tbody tr:first-child');

    // Wait for detail view
    await expect(page.locator('h2:has-text("Application Detail")')).toBeVisible();

    // Change status
    await page.click('button:has-text("Approve for Interview")');
    await page.click('button:has-text("Confirm")');

    // Verify success notification
    await expect(page.locator('text=Status changed successfully')).toBeVisible();
  });

  test('denied user cannot RSVP for events', async ({ page }) => {
    // Login as denied user
    await page.click('text=Login');
    await page.fill('input[name="email"]', 'denied@witchcityrope.com');
    await page.fill('input[name="password"]', 'Test123!');
    await page.click('button[type="submit"]');

    // Navigate to events
    await page.click('text=Events & Classes');

    // Try to RSVP
    await page.click('a:has-text("Rope Basics Workshop")');

    // Verify access blocked message
    await expect(page.locator('text=Your vetting application was denied')).toBeVisible();
    await expect(page.locator('button:has-text("RSVP")')).toBeDisabled();
  });

  test('admin can send bulk reminder emails', async ({ page }) => {
    // Login as admin
    await page.click('text=Login');
    await page.fill('input[name="email"]', 'admin@witchcityrope.com');
    await page.fill('input[name="password"]', 'Test123!');
    await page.click('button[type="submit"]');

    // Navigate to vetting admin
    await page.click('text=Admin');
    await page.click('text=Vetting Applications');

    // Filter for InterviewApproved status
    await page.selectOption('select[name="statusFilter"]', 'InterviewApproved');

    // Select multiple applications
    await page.click('thead input[type="checkbox"]'); // Select all

    // Click bulk reminder button
    await page.click('button:has-text("Send Reminders")');

    // Confirm bulk operation
    await page.click('button:has-text("Send")');

    // Wait for progress modal
    await expect(page.locator('text=Sending reminders')).toBeVisible();

    // Wait for completion
    await expect(page.locator('text=successfully')).toBeVisible({ timeout: 30000 });
  });
});
```

---

## Documentation Requirements

### Inline Code Documentation

#### XML Documentation Standards
```csharp
/// <summary>
/// Service for vetting-based access control with complete audit logging
/// </summary>
/// <remarks>
/// This service validates user access for RSVP and ticket purchases based on vetting status.
/// All access checks are logged for audit purposes.
///
/// Business Rules:
/// - OnHold (6), Denied (8), Withdrawn (9) statuses block all access
/// - All other statuses allow access subject to event access level
/// - VettedMembersOnly events require Approved (7) status
/// - Public events exclude only OnHold, Denied, Withdrawn statuses
/// </remarks>
public class AccessControlService : IAccessControlService
{
    /// <summary>
    /// Check user's general vetting access for RSVP and ticket purchases
    /// </summary>
    /// <param name="userId">User ID to check access for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Access check result with flags and messaging</returns>
    /// <exception cref="ArgumentException">Thrown when userId is empty</exception>
    public async Task<Result<VettingAccessCheckDto>> CheckAccessAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Implementation...
    }
}
```

### API Documentation

#### OpenAPI/Swagger Configuration
```csharp
// File: /apps/api/Program.cs

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WitchCityRope API",
        Version = "v1",
        Description = "API for WitchCityRope membership and event management system",
        Contact = new OpenApiContact
        {
            Name = "WitchCityRope Support",
            Email = "support@witchcityrope.com"
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    // Add security definition
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
});
```

### Admin User Guide Topics

#### Table of Contents
1. **Getting Started with Vetting Admin**
   - Accessing the vetting admin section
   - Understanding the application grid
   - Navigating application details

2. **Reviewing Applications**
   - Reading application information
   - Reviewing experience and qualifications
   - Checking references
   - Adding admin notes

3. **Changing Application Status**
   - Understanding status workflow
   - Valid status transitions
   - Confirmation process
   - Email notifications

4. **Managing Email Templates**
   - Editing email content
   - Using template variables
   - Previewing templates
   - Resetting to defaults

5. **Bulk Operations**
   - Sending reminder emails
   - Bulk status changes
   - Configuring thresholds
   - Monitoring operation progress

6. **Access Control**
   - Understanding blocked statuses
   - Event access levels
   - Member access to events
   - Troubleshooting access issues

---

## Implementation Order

### Week 1-2: Backend Infrastructure
**Priority**: Access Control + Email Foundation

#### Days 1-3: Access Control Service
1. **Day 1**: AccessControlService implementation
   - Interface and service class
   - Status-based access logic
   - Block message generation
   - Unit tests (100% coverage)

2. **Day 2**: Access Control Endpoints
   - `/api/vetting/access-control/check/{userId}`
   - `/api/vetting/access-control/check-event`
   - Integration with RSVP endpoints
   - Integration with ticket purchase endpoints

3. **Day 3**: Access Control Testing
   - Integration tests for all endpoints
   - RSVP integration tests
   - Ticket purchase integration tests
   - Performance testing (<100ms requirement)

#### Days 4-7: Email Service Foundation
4. **Day 4**: Database Schema
   - VettingEmailTemplate entity
   - VettingEmailLog entity
   - EF Core configurations
   - Migration script
   - Seed default templates

5. **Day 5**: EmailService Implementation
   - SendGrid integration
   - Template variable replacement
   - Email delivery logic
   - Error handling and retries
   - Unit tests

6. **Day 6**: Email Template System
   - Default template creation
   - Template storage in database
   - Variable replacement testing
   - HTML to plain text conversion

7. **Day 7**: Email Delivery Tracking
   - EmailLog entity integration
   - Delivery status tracking
   - SendGrid webhook setup (optional)
   - Email delivery testing

#### Days 8-10: Status Change Integration
8. **Day 8**: Status Transition Validator
   - Transition matrix implementation
   - Validation logic
   - Error messaging
   - Unit tests

9. **Day 9**: VettingService Enhancement
   - Email trigger integration
   - Audit log improvements
   - Status change validation
   - Integration tests

10. **Day 10**: End-to-End Backend Testing
    - Complete workflow testing
    - Performance validation
    - Error scenario testing
    - Documentation updates

### Week 3-4: Admin Interface
**Priority**: Review Grid + Detail View + Status Management

#### Days 11-14: Admin Grid Component
11. **Day 11**: API Integration
    - useVettingApplications hook
    - API client implementation
    - Query configuration
    - Type generation

12. **Day 12**: VettingAdminGrid Component
    - Grid layout with Mantine Table
    - Filtering and sorting
    - Search functionality
    - Pagination

13. **Day 13**: Grid Features
    - Checkbox selection
    - Status badges with colors
    - Action buttons
    - Mobile responsiveness

14. **Day 14**: Grid Testing
    - Component tests
    - Filter testing
    - Sort testing
    - E2E grid tests

#### Days 15-18: Application Detail View
15. **Day 15**: Detail View API
    - useVettingDetail hook
    - Application detail query
    - Notes query
    - Audit log query

16. **Day 16**: VettingDetailView Component
    - Layout structure
    - Application information display
    - Status timeline
    - Notes display

17. **Day 17**: Status Change Component
    - VettingStatusChange modal
    - Valid transition logic
    - Confirmation dialog
    - useStatusChange mutation

18. **Day 18**: Detail View Testing
    - Component tests
    - Status change tests
    - Note addition tests
    - E2E detail view tests

#### Days 19-21: Email Template Management
19. **Day 19**: Email Template API
    - useEmailTemplates hook
    - Template CRUD operations
    - Preview functionality

20. **Day 20**: VettingEmailTemplates Component
    - Template list view
    - Template editor
    - Variable insertion
    - Preview modal

21. **Day 21**: Template Testing
    - Component tests
    - Edit/save testing
    - Preview testing
    - E2E template tests

#### Days 22-24: Admin Interface Polish
22. **Day 22**: Admin Notes System
    - VettingNotes component
    - Note addition
    - Note timeline
    - Note editing (within 1 hour)

23. **Day 23**: Audit Trail Display
    - Audit log component
    - Timeline visualization
    - Filter by action type
    - Export functionality

24. **Day 24**: Integration Testing
    - Complete admin workflow E2E
    - Performance testing
    - Mobile responsiveness
    - Documentation

### Week 5-6: Member Experience + Bulk Operations
**Priority**: Access Control UI + Bulk Operations + Polish

#### Days 25-28: Access Control UI
25. **Day 25**: Access Control Hook
    - useVettingAccessControl implementation
    - Event-specific access checks
    - Caching strategy

26. **Day 26**: VettingAccessControl Component
    - Wrapper component
    - Block message display
    - Contact support button
    - Conditional rendering

27. **Day 27**: RSVP Integration
    - RSVP page updates
    - Access check integration
    - Disabled state handling
    - User messaging

28. **Day 28**: Ticket Purchase Integration
    - Ticket purchase page updates
    - Access check integration
    - Payment flow protection
    - User messaging

#### Days 29-33: Bulk Operations
29. **Day 29**: Bulk Operation Service
    - IBulkOperationService interface
    - Bulk reminder logic
    - Bulk status change logic
    - Progress tracking

30. **Day 30**: Bulk Operation Endpoints
    - `/api/vetting/bulk/reminders`
    - `/api/vetting/bulk/status-change`
    - `/api/vetting/bulk/eligible-*` endpoints
    - Testing

31. **Day 31**: VettingBulkOperations Component
    - Bulk action toolbar
    - Reminder modal
    - Status change modal
    - Configuration settings

32. **Day 32**: Bulk Progress Tracking
    - Progress modal component
    - Real-time updates
    - Success/failure counts
    - Results download

33. **Day 33**: Bulk Operation Testing
    - Unit tests
    - Integration tests
    - E2E bulk tests
    - Performance testing (100 apps)

#### Days 34-38: Polish and Documentation
34. **Day 34**: Performance Optimization
    - Query optimization
    - Caching improvements
    - Bundle size reduction
    - Load time improvements

35. **Day 35**: Error Handling
    - Comprehensive error messages
    - Recovery procedures
    - Graceful degradation
    - User feedback

36. **Day 36**: Accessibility
    - WCAG 2.1 AA compliance
    - Screen reader testing
    - Keyboard navigation
    - Color contrast

37. **Day 37**: Documentation
    - Admin user guide
    - API documentation
    - Code documentation
    - Training materials

38. **Day 38**: Final Testing
    - Regression testing
    - User acceptance testing
    - Performance benchmarks
    - Production readiness review

### Days 39-42: Deployment and Monitoring
39. **Day 39**: Staging Deployment
    - Deploy to staging environment
    - SendGrid configuration
    - Database migration
    - Smoke testing

40. **Day 40**: UAT with Admins
    - Admin training session
    - Hands-on testing
    - Feedback collection
    - Issue resolution

41. **Day 41**: Production Deployment
    - Production database migration
    - SendGrid production configuration
    - Feature flag activation
    - Monitoring setup

42. **Day 42**: Post-Deployment
    - Monitor email delivery
    - Monitor access control
    - Monitor admin usage
    - Collect feedback

---

## Summary

### Key Technical Decisions Made

1. **SendGrid for Email Delivery**: Reliable, scalable, comprehensive API with delivery tracking
2. **Database-Stored Templates**: Admin-editable without code deployments, version controlled
3. **Server-Side Access Validation**: All access checks validated at API level, no client-side bypass
4. **TanStack Query for State**: Optimistic updates, automatic caching, real-time UI updates
5. **Vertical Slice Architecture**: All vetting logic contained in `/Features/Vetting/` directory
6. **Immutable Audit Trail**: Complete logging for compliance, no updates or deletes allowed

### Most Complex Components Identified

1. **VettingAdminGrid**: Filtering, sorting, pagination, bulk selection, real-time updates
2. **EmailService**: Template rendering, variable replacement, SendGrid integration, retry logic
3. **AccessControlService**: Status validation, event access level integration, audit logging
4. **VettingDetailView**: Multiple data sources, status timeline, notes, audit trail, status changes
5. **BulkOperationService**: Batch processing, progress tracking, partial failure handling

### Critical Dependencies

**External**:
- SendGrid API (email delivery)
- PostgreSQL 16 (database)
- Docker (development environment)

**Internal**:
- Existing vetting application entity ✅
- Existing event system (RSVP, ticket purchase)
- Existing auth system (role-based authorization)
- Mantine v7 UI components ✅
- TanStack Query v5 ✅

### Recommended Implementation Order

**Phase 1 (Weeks 1-2)**: Backend infrastructure
- **Why first**: Foundation for all other features, testable independently
- **Risk**: Low, well-defined requirements, proven patterns
- **Deliverable**: Complete access control + email system

**Phase 2 (Weeks 3-4)**: Admin interface
- **Why second**: Requires Phase 1 backend, provides complete workflow
- **Risk**: Medium, complex UI state management
- **Deliverable**: Complete admin review interface

**Phase 3 (Weeks 5-6)**: Member experience + bulk operations
- **Why third**: Polish and optimization, builds on Phases 1-2
- **Risk**: Low, straightforward UI integration
- **Deliverable**: Complete vetting workflow system

**Total Timeline**: 6 weeks (42 days)
**Risk Level**: Medium (external SendGrid dependency, UI complexity)
**Success Criteria**: All quality gates met, 80%+ test coverage, <2s response times

---

**Document Status**: Complete - Ready for implementation team review
**Next Steps**:
1. Review with development team
2. Create sprint planning artifacts
3. Set up SendGrid account and configuration
4. Begin Phase 1 implementation
5. Schedule UAT sessions with admins

**Estimated Effort**: 6 weeks (1 full-stack developer) or 4 weeks (2 developers in parallel)
**Critical Success Factor**: SendGrid configuration must be complete before Phase 2 testing
