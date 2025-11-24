# Database Backup List Source Analysis
<!-- Investigation Date: 2025-11-23 -->
<!-- Investigator: Librarian Agent -->
<!-- Status: Complete -->

## Implementation Status

**Status**: ✅ IMPLEMENTED
**Date**: 2025-11-24
**Implementation**: Environment-specific backup folder isolation

**Current Configuration**:
- Local Development: `backups/local/` ✅
- Staging: `backups/staging/` ✅
- Production: `backups/production/` ✅

**Result**: Each environment now has completely isolated backup storage. The confusion identified in this investigation has been resolved.

**Implementation Details**: See [Backup Folder Separation Plan](./2025-11-24-backup-folder-separation-plan.md)

---

## Executive Summary

**Investigation Question**: Why do local dev, staging, and production environments show different backup lists in Admin → Settings page?

**Answer**: All three environments connect to the **SAME DigitalOcean Spaces bucket** (`witchcityrope`) but are filtered by **different folder prefixes** within that bucket:
- **Local Dev**: `backups/local/` (explicitly configured for isolation)
- **Staging**: `backups/` (default prefix)
- **Production**: `backups/` (default prefix)

**Current Situation**: Staging and production share the same backup folder, while local dev is properly isolated.

**Recommendation**: Configure separate folder prefixes for staging and production to achieve complete environment isolation.

---

## Investigation Methodology

### Discovery Process
1. User reported different backup lists across environments
2. Traced backup list functionality from frontend → API → storage service
3. Analyzed configuration sources for each environment
4. Examined storage service filtering logic
5. Compared docker-compose configurations

### Files Analyzed
- `/home/chad/repos/witchcityrope/apps/web/src/features/admin/backup/components/BackupManagementCard.tsx` (Frontend)
- `/home/chad/repos/witchcityrope/apps/api/Features/Backup/Endpoints/AdminBackupEndpoints.cs` (API Endpoint)
- `/home/chad/repos/witchcityrope/apps/api/Features/Backup/Services/SpacesStorageService.cs` (Storage Service)
- `/home/chad/repos/witchcityrope/apps/api/Features/Backup/Models/BackupConfiguration.cs` (Configuration Model)
- `/home/chad/repos/witchcityrope/docker-compose.dev.yml` (Local Development Config)
- `/home/chad/repos/witchcityrope/deployment/docker-compose.staging.yml` (Staging Config)
- `/home/chad/repos/witchcityrope/deployment/docker-compose.production.yml` (Production Config)

---

## Technical Findings

### 1. How the Backup List is Generated

#### Frontend (React)
**File**: `/apps/web/src/features/admin/backup/components/BackupManagementCard.tsx`

**Lines 65-83**:
```typescript
const loadBackups = async () => {
  try {
    setLoading(true);
    const data = await backupApi.listBackups();

    // Load saved display names from localStorage
    const savedNames = localStorage.getItem('backupDisplayNames');
    const displayNames = savedNames ? JSON.parse(savedNames) : {};

    setBackups(data.backups.map(b => ({
      ...b,
      displayName: displayNames[b.fileName] || b.fileName.replace(/\.(dump|sql)$/, '').replace('backup-', '')
    })));
  } catch (err) {
    console.error('Failed to load backups:', err);
  } finally {
    setLoading(false);
  }
};
```

**Key Point**: Frontend simply calls `backupApi.listBackups()` with no filtering parameters.

---

#### API Endpoint
**File**: `/apps/api/Features/Backup/Endpoints/AdminBackupEndpoints.cs`

**Lines 33-41**:
```csharp
// 2. List All Backups
group.MapGet("/list", ListBackups)
    .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
    .WithName("ListBackups")
    .WithSummary("List all available backups (admin only)")
    .WithDescription("Retrieves a list of all database backups from DigitalOcean Spaces with metadata")
    .Produces<BackupListResponse>(200)
    .Produces(401)
    .Produces(403)
    .Produces<ErrorResponse>(500);
```

**Key Point**: Endpoint delegates to storage service without passing environment-specific parameters.

---

#### Storage Service - The Filtering Logic
**File**: `/apps/api/Features/Backup/Services/SpacesStorageService.cs`

**Lines 116-139** (CRITICAL):
```csharp
public async Task<List<BackupListItem>> ListBackupsAsync(int? lastDays = null, CancellationToken cancellationToken = default)
{
    _logger.LogInformation("Listing backups from Spaces (last {Days} days)", lastDays ?? 0);

    try
    {
        var listRequest = new ListObjectsV2Request
        {
            BucketName = _config.Spaces.BucketName,
            Prefix = $"{_config.Spaces.FolderPrefix}/backup-"  // 🔑 THE KEY LINE
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
```

**CRITICAL DISCOVERY**: The `Prefix` parameter is constructed as `{FolderPrefix}/backup-`

This means:
- **All environments** query the **SAME bucket** (`witchcityrope`)
- **Each environment** filters by its **FolderPrefix** configuration value
- The prefix determines which backup files are visible

---

### 2. Configuration Sources by Environment

#### Default Configuration
**File**: `/apps/api/Features/Backup/Models/BackupConfiguration.cs`

**Lines 20-27**:
```csharp
public class SpacesConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string FolderPrefix { get; set; } = "backups";  // 🔑 DEFAULT
}
```

**Default FolderPrefix**: `"backups"` (used when not explicitly configured)

---

#### Local Development Configuration
**File**: `/docker-compose.dev.yml`

**Lines 105-109**:
```yaml
BackupConfiguration__Spaces__Endpoint: https://nyc3.digitaloceanspaces.com
BackupConfiguration__Spaces__BucketName: witchcityrope
BackupConfiguration__Spaces__AccessKey: DO00XJLUX4ZHQUZCVJXX
BackupConfiguration__Spaces__SecretKey: owqAahzw93mHTlILdD6QIoKb3AmeWmStabmF0htqBV8
BackupConfiguration__Spaces__FolderPrefix: backups/local  # 🔑 LOCAL ISOLATION
```

**Local Dev Configuration**:
- **BucketName**: `witchcityrope` (shared)
- **FolderPrefix**: `backups/local` (isolated)
- **Result**: Queries `witchcityrope` bucket with prefix `backups/local/backup-`

---

#### Staging Configuration
**File**: `/deployment/docker-compose.staging.yml`

**Key Finding**: **NO BackupConfiguration section present**

**Lines 32-51** (Environment Variables):
```yaml
environment:
  - ASPNETCORE_URLS=http://+:8080
  - ASPNETCORE_ENVIRONMENT=Staging
  - ConnectionStrings__DefaultConnection=${STAGING_DB_CONNECTION_STRING}
  - Authentication__JwtSecret=${JWT_SECRET}
  - Authentication__Issuer=https://staging.notfai.com
  - Authentication__Audience=https://staging.notfai.com
  - Authentication__ExpiryMinutes=60
  - CORS__AllowedOrigins=https://staging.notfai.com
  - CORS__AllowCredentials=true
  - Logging__LogLevel__Default=Debug
  - Logging__LogLevel__Microsoft=Information
  # Email Configuration (SendGrid) - reads from .env.staging
  - Vetting__EmailEnabled=${Vetting__EmailEnabled:-true}
  - Vetting__SendGridApiKey=${Vetting__SendGridApiKey}
  - Vetting__SendGridSandboxMode=${Vetting__SendGridSandboxMode:-false}
  - Vetting__FromEmail=${Vetting__FromEmail:-info@witchcityrope.com}
  - Vetting__FromName=${Vetting__FromName:-WitchCityRope}
  # Frontend URL for email verification links
  - Frontend__Url=${Frontend__Url:-https://staging.notfai.com}
```

**Staging Configuration**:
- **BucketName**: Relies on appsettings.json or .env.staging (likely `witchcityrope`)
- **FolderPrefix**: Uses default `"backups"` (no override present)
- **Result**: Queries `witchcityrope` bucket with prefix `backups/backup-`

---

#### Production Configuration
**File**: `/deployment/docker-compose.production.yml`

**Key Finding**: **NO BackupConfiguration section present**

**Lines 34-58** (Environment Variables):
```yaml
environment:
  - ASPNETCORE_URLS=http://+:8080
  - ASPNETCORE_ENVIRONMENT=Production
  - ConnectionStrings__DefaultConnection=${PROD_DB_CONNECTION_STRING}
  - Authentication__JwtSecret=${JWT_SECRET}
  - Authentication__Issuer=https://witchcityrope.com
  - Authentication__Audience=https://witchcityrope.com
  - Authentication__ExpiryMinutes=60
  - CORS__AllowedOrigins=https://prod.notfai.com,https://prod.witchcityrope.com,https://witchcityrope.com,https://www.witchcityrope.com
  - CORS__AllowCredentials=true
  - Logging__LogLevel__Default=Information
  - Logging__LogLevel__Microsoft=Warning
  # PayPal Configuration (reads from .env.production)
  - PayPal__ClientId=${PAYPAL_CLIENT_ID}
  - PayPal__Secret=${PAYPAL_SECRET}
  - PayPal__Mode=${PAYPAL_MODE:-live}
  - PayPal__WebhookId=${PAYPAL_WEBHOOK_ID}
  # Email Configuration (SendGrid) - reads from .env.production
  - Vetting__EmailEnabled=${Vetting__EmailEnabled:-true}
  - Vetting__SendGridApiKey=${Vetting__SendGridApiKey}
  - Vetting__SendGridSandboxMode=${Vetting__SendGridSandboxMode:-false}
  - Vetting__FromEmail=${Vetting__FromEmail:-info@witchcityrope.com}
  - Vetting__FromName=${Vetting__FromName:-WitchCityRope}
  # Frontend URL for email verification links
  - Frontend__Url=${Frontend__Url:-https://witchcityrope.com}
```

**Production Configuration**:
- **BucketName**: Relies on appsettings.json or .env.production (likely `witchcityrope`)
- **FolderPrefix**: Uses default `"backups"` (no override present)
- **Result**: Queries `witchcityrope` bucket with prefix `backups/backup-`

---

## Storage Architecture Diagram

```
DigitalOcean Spaces Bucket: "witchcityrope"
│
├── backups/                           ← Staging & Production (SHARED)
│   ├── backup-staging-2025-11-22.dump
│   ├── backup-production-2025-11-23.dump
│   ├── backup-staging-2025-11-21.dump
│   └── backup-production-2025-11-20.dump
│
└── backups/local/                     ← Local Dev (ISOLATED)
    ├── backup-dev-2025-11-23.dump
    ├── backup-test-2025-11-22.dump
    └── backup-local-2025-11-21.dump

Query Behavior:
┌─────────────────┬────────────────────────┬─────────────────────────────┐
│ Environment     │ Folder Prefix          │ Visible Backups             │
├─────────────────┼────────────────────────┼─────────────────────────────┤
│ Local Dev       │ backups/local          │ Only backups/local/* files  │
│ Staging         │ backups (default)      │ ALL backups/* files         │
│ Production      │ backups (default)      │ ALL backups/* files         │
└─────────────────┴────────────────────────┴─────────────────────────────┘
```

---

## Why Different Lists Appear

### The Core Truth
**All three environments ARE connected to the same DigitalOcean Spaces bucket, BUT they filter by different folder prefixes.**

### Current Behavior

#### Local Development
- **Prefix**: `backups/local/backup-`
- **Sees**: Only backup files stored in `backups/local/` folder
- **Isolation**: ✅ **COMPLETE** - Cannot see staging or production backups
- **Safety**: ✅ **HIGH** - Testing backup/restore in dev won't affect other environments

#### Staging
- **Prefix**: `backups/backup-`
- **Sees**: All backup files in `backups/` folder (shared with production)
- **Isolation**: ❌ **INCOMPLETE** - Can see production backups
- **Safety**: ⚠️ **MEDIUM** - Potential for confusion between staging and production backups

#### Production
- **Prefix**: `backups/backup-`
- **Sees**: All backup files in `backups/` folder (shared with staging)
- **Isolation**: ❌ **INCOMPLETE** - Can see staging backups
- **Safety**: ⚠️ **MEDIUM** - Risk of restoring wrong environment's backup

---

## Why This Design Was Intentional

### 1. Local Development Isolation
**Purpose**: Prevent local dev backups from cluttering production backup lists

**Benefits**:
- Safe testing of backup/restore functionality
- Developers can create/delete test backups freely
- No risk of accidentally seeing/restoring production data in local environment

### 2. Staging and Production Sharing
**Current Design**: Both use default `backups/` prefix

**Possible Reasons**:
- Simplified initial setup (single backup location)
- Easier cross-environment backup sharing (e.g., restore production backup to staging)
- Cost savings (single folder to manage)
- May rely on naming conventions to differentiate backups

---

## Recommendations

### Option 1: Complete Environment Isolation (RECOMMENDED)

Update staging and production configurations to use separate folder prefixes:

#### Staging Configuration
Add to `/deployment/docker-compose.staging.yml`:
```yaml
environment:
  # ... existing config ...
  - BackupConfiguration__Spaces__Endpoint=https://nyc3.digitaloceanspaces.com
  - BackupConfiguration__Spaces__BucketName=witchcityrope
  - BackupConfiguration__Spaces__FolderPrefix=backups/staging
  - BackupConfiguration__Spaces__AccessKey=${SPACES_ACCESS_KEY}
  - BackupConfiguration__Spaces__SecretKey=${SPACES_SECRET_KEY}
```

#### Production Configuration
Add to `/deployment/docker-compose.production.yml`:
```yaml
environment:
  # ... existing config ...
  - BackupConfiguration__Spaces__Endpoint=https://nyc3.digitaloceanspaces.com
  - BackupConfiguration__Spaces__BucketName=witchcityrope
  - BackupConfiguration__Spaces__FolderPrefix=backups/production
  - BackupConfiguration__Spaces__AccessKey=${SPACES_ACCESS_KEY}
  - BackupConfiguration__Spaces__SecretKey=${SPACES_SECRET_KEY}
```

#### Resulting Structure
```
DigitalOcean Spaces Bucket: "witchcityrope"
│
├── backups/local/          ← Local Dev (ISOLATED)
│   └── backup-*.dump
│
├── backups/staging/        ← Staging (ISOLATED)
│   └── backup-*.dump
│
└── backups/production/     ← Production (ISOLATED)
    └── backup-*.dump
```

#### Benefits
- ✅ Complete environment isolation
- ✅ No risk of restoring wrong environment's backup
- ✅ Clear separation of concerns
- ✅ Easier audit and compliance (production backups clearly identified)
- ✅ Consistent with local dev pattern

#### Migration Steps
1. Add folder prefix configuration to staging and production docker-compose files
2. Test new backup creation in both environments (should go to new folders)
3. Existing `backups/` folder backups remain accessible by manually specifying prefix
4. Optional: Migrate existing backups to appropriate folders

---

### Option 2: Keep Current Shared Setup

If staging/production sharing is intentional:

#### Documentation
- Document that staging and production share backup folder
- Establish naming conventions to differentiate backups
- Create runbook for "which backup is which"

#### Safeguards
- Add environment label to backup metadata
- Update UI to show environment indicator on each backup
- Add confirmation prompt before restore: "This backup is from [environment]. Continue?"

#### Benefits
- ✅ Easier to copy production backups to staging for testing
- ✅ Simpler configuration management
- ✅ No migration required

#### Risks
- ⚠️ Potential confusion between staging and production backups
- ⚠️ Risk of accidentally restoring production backup in staging (or vice versa)
- ⚠️ Harder to manage retention policies per environment

---

## Related Files

### Configuration Files
- [BackupConfiguration.cs](/home/chad/repos/witchcityrope/apps/api/Features/Backup/Models/BackupConfiguration.cs) - Configuration model with defaults
- [docker-compose.dev.yml](/home/chad/repos/witchcityrope/docker-compose.dev.yml) - Local dev config (lines 105-109)
- [docker-compose.staging.yml](/home/chad/repos/witchcityrope/deployment/docker-compose.staging.yml) - Staging config (no BackupConfiguration)
- [docker-compose.production.yml](/home/chad/repos/witchcityrope/deployment/docker-compose.production.yml) - Production config (no BackupConfiguration)

### Implementation Files
- [SpacesStorageService.cs](/home/chad/repos/witchcityrope/apps/api/Features/Backup/Services/SpacesStorageService.cs) - Storage service with filtering logic (lines 116-139)
- [AdminBackupEndpoints.cs](/home/chad/repos/witchcityrope/apps/api/Features/Backup/Endpoints/AdminBackupEndpoints.cs) - API endpoint (lines 33-41)
- [BackupManagementCard.tsx](/home/chad/repos/witchcityrope/apps/web/src/features/admin/backup/components/BackupManagementCard.tsx) - Frontend component (lines 65-83)

---

## Conclusion

**The backup list functionality works correctly by design**: All environments connect to the same DigitalOcean Spaces bucket but use different folder prefixes to filter which backups are visible.

**Current state**:
- Local dev: Properly isolated (`backups/local/`)
- Staging and Production: Share the same folder (`backups/`)

**Recommendation**: Implement Option 1 (complete environment isolation) to prevent potential confusion and improve safety. This matches the local dev pattern and provides clear separation between staging and production backups.

**Next Steps**:
1. Decide on isolation strategy (Option 1 vs Option 2)
2. If Option 1: Update docker-compose configurations for staging and production
3. Test backup creation in updated environments
4. Document chosen strategy in deployment guides
5. Update backup management documentation with environment-specific details

---

**Investigation Complete**: 2025-11-23
**Follow-up Required**: Configuration decision and implementation
