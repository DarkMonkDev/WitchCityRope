# Backup Folder Separation Implementation Plan
<!-- Created: 2025-11-24 -->
<!-- Author: Librarian Agent -->
<!-- Status: Ready for Implementation -->
<!-- Related: 2025-11-23-backup-list-source-analysis.md -->

## Executive Summary

**Objective**: Separate staging and production backup storage to eliminate confusion and improve safety.

**Current State**:
- Local dev: `backups/local/` (properly isolated)
- Staging: `backups/` (default prefix - shared with production)
- Production: `backups/` (default prefix - shared with staging)

**Target State**:
- Local dev: `backups/local/` (unchanged)
- Staging: `backups/staging/` (isolated)
- Production: `backups/production/` (isolated)

**Impact**:
- Risk Level: **LOW** (configuration only, no code changes)
- Database Impact: **NONE** (no migrations required)
- User Impact: **NONE** (admin-only feature)
- Reversible: **YES** (simple config change)

**Deployment Required**:
- Staging: Yes (redeploy with new configuration)
- Production: Yes (redeploy with new configuration)

---

## Implementation Scope

### Files to Modify

#### 1. Staging Configuration
**File**: `/home/chad/repos/witchcityrope/deployment/docker-compose.staging.yml`
- **Location**: After line 51 (after Frontend__Url)
- **Change Type**: Add new environment variables
- **Configuration**: BackupConfiguration section with staging-specific folder prefix

#### 2. Production Configuration
**File**: `/home/chad/repos/witchcityrope/deployment/docker-compose.production.yml`
- **Location**: After line 58 (after Frontend__Url)
- **Change Type**: Add new environment variables
- **Configuration**: BackupConfiguration section with production-specific folder prefix

#### 3. Environment Variable Templates (Optional)
**Files** (if they exist):
- `deployment/.env.staging.template`
- `deployment/.env.production.template`

**Purpose**: Document required environment variables for backup configuration

### Files to Update (Documentation)

#### 1. Investigation Document
**File**: `/home/chad/repos/witchcityrope/docs/functional-areas/database-backup-restore/investigation/2025-11-23-backup-list-source-analysis.md`
- **Action**: Add "IMPLEMENTED" status and date
- **Content**: Update findings to reflect new configuration

#### 2. Backup Feature README
**File**: `/home/chad/repos/witchcityrope/docs/functional-areas/database-backup-restore/README.md`
- **Action**: Update to document environment-specific backup locations
- **Content**: Add section on backup storage structure

#### 3. Staging Deployment Guide
**File**: `/home/chad/repos/witchcityrope/docs/functional-areas/deployment/staging-deployment-guide.md`
- **Action**: Add note about backup storage location
- **Content**: Document staging backup folder prefix

---

## Detailed Changes

### Staging Configuration Changes

**File**: `/home/chad/repos/witchcityrope/deployment/docker-compose.staging.yml`

**Insert After Line 51** (after `Frontend__Url` environment variable):

```yaml
      # Backup Configuration - Staging Isolated Storage
      - BackupConfiguration__Spaces__Endpoint=https://nyc3.digitaloceanspaces.com
      - BackupConfiguration__Spaces__BucketName=witchcityrope
      - BackupConfiguration__Spaces__AccessKey=${DIGITALOCEAN_SPACES_ACCESS_KEY}
      - BackupConfiguration__Spaces__SecretKey=${DIGITALOCEAN_SPACES_SECRET_KEY}
      - BackupConfiguration__Spaces__FolderPrefix=backups/staging
      - BackupConfiguration__BackupOptions__TempDirectory=/tmp
      - BackupConfiguration__BackupOptions__TimeoutSeconds=300
```

**Explanation**:
- `Endpoint`: DigitalOcean NYC3 region (same as current)
- `BucketName`: `witchcityrope` (shared bucket, different folder)
- `AccessKey` & `SecretKey`: From .env.staging file (existing credentials)
- `FolderPrefix`: **`backups/staging`** (NEW - isolates staging backups)
- `TempDirectory`: `/tmp` (standard temp location in container)
- `TimeoutSeconds`: `300` (5 minutes for large backups)

---

### Production Configuration Changes

**File**: `/home/chad/repos/witchcityrope/deployment/docker-compose.production.yml`

**Insert After Line 58** (after `Frontend__Url` environment variable):

```yaml
      # Backup Configuration - Production Isolated Storage
      - BackupConfiguration__Spaces__Endpoint=https://nyc3.digitaloceanspaces.com
      - BackupConfiguration__Spaces__BucketName=witchcityrope
      - BackupConfiguration__Spaces__AccessKey=${DIGITALOCEAN_SPACES_ACCESS_KEY}
      - BackupConfiguration__Spaces__SecretKey=${DIGITALOCEAN_SPACES_SECRET_KEY}
      - BackupConfiguration__Spaces__FolderPrefix=backups/production
      - BackupConfiguration__BackupOptions__TempDirectory=/tmp
      - BackupConfiguration__BackupOptions__TimeoutSeconds=300
```

**Explanation**:
- `Endpoint`: DigitalOcean NYC3 region (same as current)
- `BucketName`: `witchcityrope` (shared bucket, different folder)
- `AccessKey` & `SecretKey`: From .env.production file (existing credentials)
- `FolderPrefix`: **`backups/production`** (NEW - isolates production backups)
- `TempDirectory`: `/tmp` (standard temp location in container)
- `TimeoutSeconds`: `300` (5 minutes for large backups)

---

## Environment Variables Required

Both staging and production `.env` files need the following variables:

```bash
# DigitalOcean Spaces Configuration for Backups
DIGITALOCEAN_SPACES_ACCESS_KEY=DO00XJLUX4ZHQUZCVJXX
DIGITALOCEAN_SPACES_SECRET_KEY=owqAahzw93mHTlILdD6QIoKb3AmeWmStabmF0htqBV8
```

**Note**: These credentials are already in use by local dev (visible in `docker-compose.dev.yml`). Verify they exist in:
- `/opt/witchcityrope/staging/.env.staging`
- `/opt/witchcityrope/production/.env.production`

**Security Note**: These credentials are hardcoded in `docker-compose.dev.yml` for local development convenience. In staging/production, they should come from environment variable files for security.

---

## Migration Considerations

### Existing Backups

**Current Location**: `backups/` folder in DigitalOcean Spaces

**What Happens**:
- Existing backups in `backups/` folder **remain accessible**
- They won't automatically move to new folders
- New backups will be created in environment-specific folders

**Options for Existing Backups**:

#### Option 1: Leave in Place (RECOMMENDED)
- **Action**: No action required
- **Result**: Existing backups become read-only archive
- **Pros**: Simple, no risk, preserves history
- **Cons**: Mixed folder structure (old in `backups/`, new in `backups/staging/` or `backups/production/`)

#### Option 2: Manual Migration
- **Action**: Copy/move existing backups to appropriate folders
- **Steps**:
  1. Review backup filenames to determine environment of origin
  2. Use DigitalOcean Spaces console or S3 tools to move files
  3. Move staging backups to `backups/staging/`
  4. Move production backups to `backups/production/`
- **Pros**: Clean folder structure
- **Cons**: Risk of misidentifying backups, time-consuming, potential mistakes

#### Recommendation
**Use Option 1**: Leave existing backups in place. They remain accessible if needed, and new backups will automatically use the correct folders after deployment.

---

## Testing Plan

### Pre-Deployment Testing

**Environment**: Local development (optional)

1. **Test Configuration Syntax**:
   ```bash
   # Validate YAML syntax
   cd /home/chad/repos/witchcityrope/deployment
   docker-compose -f docker-compose.staging.yml config
   docker-compose -f docker-compose.production.yml config
   ```

2. **Review Environment Variables**:
   - Verify DigitalOcean Spaces credentials exist in `.env.staging`
   - Verify DigitalOcean Spaces credentials exist in `.env.production`

---

### Staging Deployment Testing

**Steps**:

1. **Deploy Staging with New Configuration**:
   - Use `staging-deploy` skill or manual deployment
   - New API container starts with `BackupConfiguration__Spaces__FolderPrefix=backups/staging`

2. **Verify Configuration Loaded**:
   ```bash
   # SSH to staging server
   ssh root@staging.notfai.com

   # Check container environment variables
   docker exec witchcity-api-staging env | grep BackupConfiguration
   ```

   **Expected Output**:
   ```
   BackupConfiguration__Spaces__FolderPrefix=backups/staging
   BackupConfiguration__Spaces__BucketName=witchcityrope
   BackupConfiguration__Spaces__Endpoint=https://nyc3.digitaloceanspaces.com
   ```

3. **Trigger Manual Backup in Staging**:
   - Login to staging admin: https://staging.notfai.com
   - Navigate to: Admin → Settings → Database Backup
   - Click "Create Backup Now"
   - Wait for success message

4. **Verify Backup in Staging Folder**:
   - Check backup list in admin UI
   - Verify new backup appears
   - Check DigitalOcean Spaces console:
     - Navigate to `witchcityrope` bucket
     - Look for `backups/staging/backup-*.dump` file
   - **CRITICAL**: Verify NO new backup in `backups/` folder (old location)

5. **Verify Production Unaffected**:
   - Login to production: https://prod.notfai.com or https://witchcityrope.com
   - Navigate to: Admin → Settings → Database Backup
   - Verify backup list shows ONLY production backups (old `backups/` folder)
   - Verify staging backup does NOT appear in production list

---

### Production Deployment Testing

**Steps**:

1. **Deploy Production with New Configuration**:
   - Use `production-deploy` skill or manual deployment
   - New API container starts with `BackupConfiguration__Spaces__FolderPrefix=backups/production`

2. **Verify Configuration Loaded**:
   ```bash
   # SSH to production server
   ssh root@prod.notfai.com

   # Check container environment variables
   docker exec witchcity-api-prod env | grep BackupConfiguration
   ```

   **Expected Output**:
   ```
   BackupConfiguration__Spaces__FolderPrefix=backups/production
   BackupConfiguration__Spaces__BucketName=witchcityrope
   BackupConfiguration__Spaces__Endpoint=https://nyc3.digitaloceanspaces.com
   ```

3. **Trigger Manual Backup in Production**:
   - Login to production admin
   - Navigate to: Admin → Settings → Database Backup
   - Click "Create Backup Now"
   - Wait for success message

4. **Verify Backup in Production Folder**:
   - Check backup list in admin UI
   - Verify new backup appears
   - Check DigitalOcean Spaces console:
     - Navigate to `witchcityrope` bucket
     - Look for `backups/production/backup-*.dump` file
   - **CRITICAL**: Verify NO new backup in `backups/` folder (old location)

5. **Verify Staging Shows Only Staging Backups**:
   - Login to staging
   - Navigate to: Admin → Settings → Database Backup
   - Verify backup list shows ONLY staging backups (from `backups/staging/`)
   - Verify production backup does NOT appear in staging list

---

### Final Verification

**Expected State After Both Deployments**:

| Environment | Folder Prefix | Visible Backups | Isolation |
|-------------|---------------|-----------------|-----------|
| Local Dev | `backups/local` | Only `backups/local/*` files | ✅ Complete |
| Staging | `backups/staging` | Only `backups/staging/*` files | ✅ Complete |
| Production | `backups/production` | Only `backups/production/*` files | ✅ Complete |

**DigitalOcean Spaces Bucket Structure**:
```
witchcityrope/
├── backups/                    ← OLD - Contains mixed staging/production backups (archived)
│   ├── backup-*.dump          (read-only archive - no longer written to)
│
├── backups/local/             ← Local Dev (unchanged)
│   └── backup-*.dump
│
├── backups/staging/           ← Staging (NEW - isolated)
│   └── backup-*.dump
│
└── backups/production/        ← Production (NEW - isolated)
    └── backup-*.dump
```

---

## Rollback Plan

**If Issues Occur**:

### Immediate Rollback Steps

1. **Remove BackupConfiguration from docker-compose file**:
   ```bash
   # On staging server
   cd /opt/witchcityrope/staging
   # Edit docker-compose.staging.yml
   # Remove the 8 BackupConfiguration lines

   # Or on production server
   cd /opt/witchcityrope/production
   # Edit docker-compose.production.yml
   # Remove the 8 BackupConfiguration lines
   ```

2. **Redeploy Affected Environment**:
   ```bash
   docker-compose -f docker-compose.[staging|production].yml pull
   docker-compose -f docker-compose.[staging|production].yml up -d
   ```

3. **Result**:
   - Environment reverts to default `backups/` folder prefix
   - All backups in `backups/` folder become visible again
   - Any backups created during test period remain in `backups/staging/` or `backups/production/` folders but won't be visible

### Rollback Impact

- **Risk**: LOW (no data loss, backups are never deleted)
- **Recovery Time**: ~5 minutes (config edit + redeploy)
- **User Impact**: NONE (brief API restart, backup feature briefly unavailable)

---

## Risk Assessment

### Risk Level: **LOW**

**Why Low Risk**:
1. **Configuration Only**: No code changes, no database migrations
2. **Read-Only Operation**: Backup list filtering doesn't affect existing backups
3. **Backward Compatible**: Old backups in `backups/` folder remain accessible
4. **Easily Reversible**: Simple config change to rollback
5. **Admin-Only Feature**: Only administrators use backup management
6. **No Data Loss**: Backups are never deleted, only filtered by prefix

### Change Type: **Configuration**

**What Changes**:
- Environment variables in docker-compose files
- Folder prefix used for backup storage filtering

**What Doesn't Change**:
- Application code
- Database schema
- API endpoints
- Frontend UI
- Existing backup files

### Database Impact: **NONE**

- No migrations required
- No schema changes
- No data modifications

### User Impact: **NONE**

- Feature is admin-only
- Brief API restart during deployment (standard)
- No user-facing functionality affected
- No downtime for public users

### Reversibility: **YES**

- Simple config removal to rollback
- No irreversible changes
- Backups remain in multiple locations (safe)

---

## Success Criteria

**After implementation is complete, verify ALL of the following**:

### Environment Isolation

- [ ] **Staging creates backups in `backups/staging/` folder**
  - Trigger manual backup in staging admin
  - Check DigitalOcean Spaces console for new file in `backups/staging/`

- [ ] **Production creates backups in `backups/production/` folder**
  - Trigger manual backup in production admin
  - Check DigitalOcean Spaces console for new file in `backups/production/`

- [ ] **Local dev continues using `backups/local/` folder**
  - Trigger manual backup in local dev
  - Check DigitalOcean Spaces console for new file in `backups/local/`

### Backup List Filtering

- [ ] **Staging shows only staging backups**
  - Login to staging admin → Settings → Database Backup
  - Verify ONLY backups from `backups/staging/` folder appear
  - Verify NO production backups in list

- [ ] **Production shows only production backups**
  - Login to production admin → Settings → Database Backup
  - Verify ONLY backups from `backups/production/` folder appear
  - Verify NO staging backups in list

- [ ] **Local dev shows only local backups**
  - Login to local admin → Settings → Database Backup
  - Verify ONLY backups from `backups/local/` folder appear
  - Verify NO staging or production backups in list

### Cross-Environment Safety

- [ ] **No cross-contamination between environments**
  - Create backup in staging
  - Verify it does NOT appear in production or local dev
  - Create backup in production
  - Verify it does NOT appear in staging or local dev

### Configuration Verification

- [ ] **Staging environment variables loaded correctly**
  - SSH to staging server
  - Run: `docker exec witchcity-api-staging env | grep BackupConfiguration`
  - Verify `FolderPrefix=backups/staging`

- [ ] **Production environment variables loaded correctly**
  - SSH to production server
  - Run: `docker exec witchcity-api-prod env | grep BackupConfiguration`
  - Verify `FolderPrefix=backups/production`

---

## Post-Implementation

### Documentation Updates

**Required Updates**:

1. **Investigation Document**:
   - **File**: `/docs/functional-areas/database-backup-restore/investigation/2025-11-23-backup-list-source-analysis.md`
   - **Action**: Add implementation status header:
     ```markdown
     <!-- Status: IMPLEMENTED 2025-11-24 -->
     <!-- Implementation: backup-folder-separation-plan.md -->
     ```

2. **Backup Feature README**:
   - **File**: `/docs/functional-areas/database-backup-restore/README.md`
   - **Action**: Add section documenting backup storage structure:
     ```markdown
     ## Backup Storage Structure

     Backups are stored in DigitalOcean Spaces bucket `witchcityrope` with environment-specific folder prefixes:

     - Local Dev: `backups/local/`
     - Staging: `backups/staging/`
     - Production: `backups/production/`

     This ensures complete isolation between environments.
     ```

3. **Staging Deployment Guide**:
   - **File**: `/docs/functional-areas/deployment/staging-deployment-guide.md`
   - **Action**: Add note about backup storage location in deployment notes

4. **File Registry**:
   - **File**: `/docs/architecture/file-registry.md`
   - **Action**: Log all modified files with implementation details

### File Registry Entry

**Add to file registry**:

```markdown
| 2025-11-24 | /home/chad/repos/witchcityrope/deployment/docker-compose.staging.yml | MODIFIED | Added BackupConfiguration environment variables (lines 52-59) with FolderPrefix=backups/staging for complete environment isolation. Implements backup folder separation plan. | Backup Folder Separation | ACTIVE | Never |
| 2025-11-24 | /home/chad/repos/witchcityrope/deployment/docker-compose.production.yml | MODIFIED | Added BackupConfiguration environment variables (lines 59-66) with FolderPrefix=backups/production for complete environment isolation. Implements backup folder separation plan. | Backup Folder Separation | ACTIVE | Never |
| 2025-11-24 | /home/chad/repos/witchcityrope/docs/functional-areas/database-backup-restore/investigation/2025-11-24-backup-folder-separation-plan.md | CREATED | Comprehensive implementation plan for separating staging and production backup storage areas. Documents executive summary (objective, current state, target state, impact, deployment requirements), implementation scope (files to modify, documentation updates), detailed configuration changes (staging/production docker-compose additions with exact line numbers and YAML), environment variables required, migration considerations (existing backups options), complete testing plan (pre-deployment, staging, production, final verification), rollback plan, risk assessment (LOW risk - configuration only), success criteria (environment isolation, backup list filtering, cross-environment safety, configuration verification), post-implementation tasks. 900+ lines production-ready implementation guide. | Backup Folder Separation Implementation | ACTIVE | Never |
```

### Stakeholder Notification

**Notify**:
- **Who**: Project maintainers, DevOps team
- **What**: New backup storage structure implemented
- **Why**: Improved safety and environment isolation
- **Impact**: None (transparent change to admins)
- **Action Required**: None (automatic after deployment)

**Sample Notification**:
```
Subject: Backup Storage Structure Updated - Complete Environment Isolation

Summary:
Database backups are now stored in environment-specific folders in DigitalOcean Spaces:
- Staging: backups/staging/
- Production: backups/production/
- Local Dev: backups/local/ (unchanged)

Benefits:
- Complete environment isolation
- No risk of restoring wrong environment's backup
- Clear separation of concerns

Impact:
- Admin-only feature
- No user impact
- Existing backups remain accessible in old location

Deployed:
- Staging: [date]
- Production: [date]

Questions? See: /docs/functional-areas/database-backup-restore/investigation/2025-11-24-backup-folder-separation-plan.md
```

### Runbook Updates

**If deployment runbooks exist**, update them to reference new backup storage locations:

- Backup procedures: Mention environment-specific folders
- Restore procedures: Note folder prefix when specifying backup
- Disaster recovery: Update backup location references

---

## Implementation Checklist

**Use this checklist during implementation**:

### Pre-Implementation
- [ ] Read complete implementation plan
- [ ] Verify DigitalOcean Spaces credentials in `.env.staging`
- [ ] Verify DigitalOcean Spaces credentials in `.env.production`
- [ ] Validate docker-compose YAML syntax (staging)
- [ ] Validate docker-compose YAML syntax (production)
- [ ] Review rollback plan
- [ ] Notify stakeholders of planned change

### Staging Implementation
- [ ] Update `/deployment/docker-compose.staging.yml` with BackupConfiguration
- [ ] Commit changes to git
- [ ] Deploy to staging using `staging-deploy` skill
- [ ] Verify configuration loaded (`docker exec witchcity-api-staging env`)
- [ ] Trigger manual backup in staging admin
- [ ] Verify backup appears in `backups/staging/` folder
- [ ] Verify backup list shows only staging backups
- [ ] Verify production unaffected

### Production Implementation
- [ ] Update `/deployment/docker-compose.production.yml` with BackupConfiguration
- [ ] Commit changes to git
- [ ] Deploy to production using `production-deploy` skill
- [ ] Verify configuration loaded (`docker exec witchcity-api-prod env`)
- [ ] Trigger manual backup in production admin
- [ ] Verify backup appears in `backups/production/` folder
- [ ] Verify backup list shows only production backups
- [ ] Verify staging isolation maintained

### Post-Implementation
- [ ] Run through complete success criteria checklist
- [ ] Update investigation document status
- [ ] Update backup feature README
- [ ] Update staging deployment guide
- [ ] Update file registry
- [ ] Notify stakeholders of completion
- [ ] Document any issues encountered
- [ ] Archive this implementation plan

---

## Related Documentation

### Investigation & Analysis
- [2025-11-23 Backup List Source Analysis](/home/chad/repos/witchcityrope/docs/functional-areas/database-backup-restore/investigation/2025-11-23-backup-list-source-analysis.md) - Original investigation explaining current behavior

### Implementation Files
- [BackupConfiguration.cs](/home/chad/repos/witchcityrope/apps/api/Features/Backup/Models/BackupConfiguration.cs) - Configuration model with defaults
- [SpacesStorageService.cs](/home/chad/repos/witchcityrope/apps/api/Features/Backup/Services/SpacesStorageService.cs) - Storage service with filtering logic

### Configuration Files
- [docker-compose.staging.yml](/home/chad/repos/witchcityrope/deployment/docker-compose.staging.yml) - Staging configuration (to be modified)
- [docker-compose.production.yml](/home/chad/repos/witchcityrope/deployment/docker-compose.production.yml) - Production configuration (to be modified)
- [docker-compose.dev.yml](/home/chad/repos/witchcityrope/docker-compose.dev.yml) - Local dev configuration (reference)

### Deployment
- [Staging Deployment Guide](/home/chad/repos/witchcityrope/docs/functional-areas/deployment/staging-deployment-guide.md) - Staging deployment procedures
- [staging-deploy Skill](/.claude/skills/staging-deploy/) - Automated staging deployment
- [production-deploy Skill](/.claude/skills/production-deploy/) - Automated production deployment

---

## Appendix: Configuration Reference

### Complete Backup Configuration Options

**All available BackupConfiguration settings**:

```yaml
# DigitalOcean Spaces Configuration
- BackupConfiguration__Spaces__Endpoint=https://nyc3.digitaloceanspaces.com
- BackupConfiguration__Spaces__BucketName=witchcityrope
- BackupConfiguration__Spaces__AccessKey=${DIGITALOCEAN_SPACES_ACCESS_KEY}
- BackupConfiguration__Spaces__SecretKey=${DIGITALOCEAN_SPACES_SECRET_KEY}
- BackupConfiguration__Spaces__FolderPrefix=backups/[environment]

# Backup Operation Configuration
- BackupConfiguration__BackupOptions__TempDirectory=/tmp
- BackupConfiguration__BackupOptions__TimeoutSeconds=300
- BackupConfiguration__BackupOptions__CompressionLevel=6  # (Optional: 0-9, default 6)
- BackupConfiguration__BackupOptions__BufferSizeKB=8192   # (Optional: default 8192)
```

### Default Values

**From BackupConfiguration.cs**:
- `FolderPrefix`: `"backups"` (default if not specified)
- `TempDirectory`: `/tmp` (standard Linux temp)
- `TimeoutSeconds`: `300` (5 minutes)
- `CompressionLevel`: `6` (balanced speed/size)
- `BufferSizeKB`: `8192` (8MB buffer)

---

**Implementation Plan Complete**: 2025-11-24
**Status**: Ready for Implementation
**Next Step**: Review plan → Update configuration files → Deploy → Test
