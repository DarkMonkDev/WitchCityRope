# Endpoint Adaptation Guide - Account-Automation to WCR

**Date**: 2025-11-17
**Purpose**: Document EXACT changes needed to adapt account-automation backup endpoints to WCR standards
**Scope**: Minimal changes to maintain working code while following WCR patterns

---

## 📋 Changes Required

### 1. Add Role-Based Authorization

**Add to EVERY endpoint**:
```csharp
.RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
```

**Import required**:
```csharp
using WitchCityRope.Api.Features.Users.Constants;
```

**Note**: UserRole.Administrator is the enum, `.ToRoleString()` converts to "Admin" string

---

### 2. Add OpenAPI Documentation

**For each endpoint, add**:
```csharp
.WithSummary("Brief description")
.WithDescription("Detailed description")
.WithTags("Admin", "Backup")
.Produces<ResponseType>(200)
.Produces(401)  // If requires auth
.Produces(403)  // If requires auth
.Produces(500)  // If has error handling
```

---

### 3. Namespace Changes

**From**:
```csharp
namespace AccountingAutomation.Api.Endpoints;
```

**To**:
```csharp
namespace WitchCityRope.Api.Features.Backup.Endpoints;
```

---

### 4. Using Statements

**From**:
```csharp
using AccountingAutomation.Api.Models;
using AccountingAutomation.Api.Services;
using AccountingAutomation.Api.Jobs;
```

**To**:
```csharp
using WitchCityRope.Api.Features.Backup.Models;
using WitchCityRope.Api.Features.Backup.Services;
using WitchCityRope.Api.Features.Backup.Jobs;
using WitchCityRope.Api.Features.Users.Constants;
```

---

## 🔍 Endpoint-by-Endpoint Changes

### Endpoint 1: POST /api/admin/backup (Trigger Backup)

```csharp
// BEFORE (account-automation)
group.MapPost("", TriggerBackup)
    .WithName("TriggerBackup")
    .WithSummary("Trigger a manual database backup")
    .Produces<BackupJobResponse>(200)
    .Produces<ErrorResponse>(400)
    .Produces<ErrorResponse>(500);

// AFTER (WCR)
group.MapPost("", TriggerBackup)
    .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
    .WithName("TriggerBackup")
    .WithSummary("Trigger a manual database backup (admin only)")
    .WithDescription("Enqueues a Hangfire job to create a PostgreSQL backup via pg_dump and upload to DigitalOcean Spaces")
    .WithTags("Admin", "Backup")
    .Produces<BackupJobResponse>(200)
    .Produces(401)
    .Produces(403)
    .Produces<ErrorResponse>(400)
    .Produces<ErrorResponse>(500);
```

### Endpoint 2: GET /api/admin/backup/list (List Backups)

```csharp
// Add authorization
.RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
.WithTags("Admin", "Backup")
.Produces(401)
.Produces(403)
```

### Endpoint 3-8: Same Pattern

Apply same authorization and tags to:
- POST /api/admin/backup/restore
- DELETE /api/admin/backup/{fileName}
- GET /api/admin/backup/download/{fileName}
- GET /api/admin/backup/job/{jobId}
- GET /api/admin/backup/storage
- POST /api/admin/backup/upload-and-restore

---

## ✅ What to KEEP (No Changes)

### Error Handling Pattern - KEEP AS-IS
```csharp
// ✅ CORRECT - Account-automation pattern is fine
catch (Exception ex)
{
    logger.LogError(ex, "Failed to start backup");
    return Results.Problem(
        detail: ex.Message,
        statusCode: 500,
        title: "Failed to start backup");
}
```

**DO NOT change to tuple pattern** - this is cleaner and works perfectly.

### DTOs - KEEP AS-IS
```csharp
// ✅ CORRECT - Already using proper DTOs
public record BackupJobResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int EstimatedSeconds { get; set; }
}
```

**DO NOT convert to different pattern** - this follows WCR standards.

### Service Injection - KEEP AS-IS
```csharp
// ✅ CORRECT - Already using dependency injection
private static IResult TriggerBackup(
    IBackgroundJobClient backgroundJobs,
    ILogger<Program> logger)
```

**DO NOT change injection pattern** - this is standard.

---

## 📊 Summary

| Aspect | Account-Automation | WCR | Action Required |
|--------|-------------------|-----|-----------------|
| HTTP Methods | ✅ Correct | ✅ Same | **No change** |
| Status Codes | ✅ Correct | ✅ Same | **No change** |
| DTOs | ✅ Uses DTOs | ✅ Same | **No change** |
| Error Handling | ✅ `Results.Problem()` | ✅ Same | **No change** |
| Authorization | ❌ None | ✅ Required | **ADD** `.RequireAuthorization()` |
| OpenAPI Tags | ⚠️ Minimal | ✅ Detailed | **ADD** `.WithTags()` |
| Namespaces | ❌ AccountingAutomation | ✅ WitchCityRope | **CHANGE** namespaces |

---

## 🎯 Backend-Developer Checklist

When copying BackupEndpoints.cs:

- [ ] Change filename: `BackupEndpoints.cs` → `AdminBackupEndpoints.cs`
- [ ] Update namespace: → `WitchCityRope.Api.Features.Backup.Endpoints`
- [ ] Update using statements (4 changes)
- [ ] Add authorization to all 8 endpoints
- [ ] Add `.WithTags("Admin", "Backup")` to all 8 endpoints
- [ ] Add `.Produces(401)` and `.Produces(403)` to all 8 endpoints
- [ ] **DO NOT** change error handling pattern
- [ ] **DO NOT** change DTO structures
- [ ] **DO NOT** change service injection pattern
- [ ] Test that all endpoints compile

---

**Estimated Changes**: ~20 lines added, ~4 lines modified, **95% unchanged**

