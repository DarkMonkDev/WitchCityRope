# Backend Developer Handoff - Backup Folder Separation
**Date**: 2025-11-24
**From**: Orchestrator
**To**: backend-developer agent
**Task**: Implement environment-specific backup storage configuration

## Objective
Update staging and production Docker Compose files to use isolated backup storage folders instead of the shared `backups/` folder.

## Background
Read the implementation plan first:
- **Implementation Plan**: `/home/chad/repos/witchcityrope/docs/functional-areas/database-backup-restore/investigation/2025-11-24-backup-folder-separation-plan.md`

## Required Changes

### File 1: Staging Configuration
**File**: `/home/chad/repos/witchcityrope/deployment/docker-compose.staging.yml`
**Location**: After line 51 (after `Frontend__Url` environment variable)
**Action**: Add backup configuration environment variables

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

### File 2: Production Configuration
**File**: `/home/chad/repos/witchcityrope/deployment/docker-compose.production.yml`
**Location**: After line 58 (after `Frontend__Url` environment variable)
**Action**: Add backup configuration environment variables

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

## Critical Requirements

1. **Exact Indentation**: Match existing environment variable indentation (6 spaces for variable names after the `- `)
2. **Variable Format**: Use double underscore `__` for nested configuration (ASP.NET Core convention)
3. **Folder Prefixes**:
   - Staging MUST use: `backups/staging`
   - Production MUST use: `backups/production`
4. **Placement**: Insert after `Frontend__Url` variable, before `depends_on` section
5. **Comment**: Include the comment line to document purpose

## Environment Variables
The configuration references these environment variables (already exist in .env files, no changes needed):
- `DIGITALOCEAN_SPACES_ACCESS_KEY`
- `DIGITALOCEAN_SPACES_SECRET_KEY`

## Validation Checklist
After making changes:
- [ ] YAML syntax is valid (proper indentation)
- [ ] No duplicate environment variable definitions
- [ ] Folder prefix is correct for each environment
- [ ] Comment header included for clarity
- [ ] File still follows docker-compose v3.8 specification

## What NOT to Do
- ❌ Do NOT modify any other environment variables
- ❌ Do NOT change database configuration
- ❌ Do NOT modify the local development configuration (docker-compose.dev.yml)
- ❌ Do NOT add these variables to appsettings.json (handled via env vars)
- ❌ Do NOT modify any C# code
- ❌ Do NOT create new files

## Expected Outcome
After your changes:
1. `deployment/docker-compose.staging.yml` has BackupConfiguration environment variables
2. `deployment/docker-compose.production.yml` has BackupConfiguration environment variables
3. Both files maintain valid YAML syntax
4. Staging uses `backups/staging` folder prefix
5. Production uses `backups/production` folder prefix

## Testing (Not Required - Information Only)
These changes will be tested during deployment:
- Staging backup trigger will create backup in `backups/staging/` folder
- Production backup trigger will create backup in `backups/production/` folder
- Local dev continues using `backups/local/` folder (unchanged)

## Report Back
When complete, report:
1. Confirmation both files updated
2. Line numbers where configuration was added
3. Any issues encountered
4. Confirmation YAML syntax validated

## Related Files
- Implementation Plan: `/home/chad/repos/witchcityrope/docs/functional-areas/database-backup-restore/investigation/2025-11-24-backup-folder-separation-plan.md`
- Current Staging Config: `/home/chad/repos/witchcityrope/deployment/docker-compose.staging.yml`
- Current Production Config: `/home/chad/repos/witchcityrope/deployment/docker-compose.production.yml`
- Local Dev Config (reference only): `/home/chad/repos/witchcityrope/docker-compose.dev.yml` (lines 98-111)
