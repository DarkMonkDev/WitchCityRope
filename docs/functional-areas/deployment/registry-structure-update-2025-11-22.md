# Container Registry Structure Update - 2025-11-22

## Overview

This document describes the reorganization of DigitalOcean Container Registry structure to provide clear environment separation and prevent accidental deployments to the wrong environment.

## New Registry Structure

### WitchCityRope Registry (`registry.digitalocean.com/witchcityrope`)

**Staging repositories:**
- `registry.digitalocean.com/witchcityrope/witchcityrope-api-staging`
- `registry.digitalocean.com/witchcityrope/witchcityrope-web-staging`

**Production repositories:**
- `registry.digitalocean.com/witchcityrope/witchcityrope-api-production`
- `registry.digitalocean.com/witchcityrope/witchcityrope-web-production`

### Accounting Registry (`registry.digitalocean.com/accounting`)

**Accounting repositories (separate project):**
- `registry.digitalocean.com/accounting/accounting-api`
- `registry.digitalocean.com/accounting/accounting-web`

## Benefits

1. **Environment Isolation**: Repository names enforce staging vs production separation
2. **No Accidental Deployments**: Impossible to deploy staging images to production or vice versa
3. **Project Separation**: Accounting project completely isolated in separate registry
4. **Clear Intent**: Repository name immediately identifies the target environment

## Deployment Scripts Updated

The following deployment automation has been updated to use the new structure:

### Staging Deployment
**File**: `/.claude/skills/staging-deploy/execute.sh`

**Changes:**
- API images: `witchcityrope-api:latest` → `witchcityrope-api-staging:latest`
- Web images: `witchcityrope-web:latest` → `witchcityrope-web-staging:latest`
- Git SHA tags: `witchcityrope-api:$GIT_SHA` → `witchcityrope-api-staging:$GIT_SHA`

**Deployment**: Use `staging-deploy` skill for automated staging deployment with new repository structure.

### Production Deployment
**File**: `/.claude/skills/production-deploy/execute.sh`

**Changes:**
- API images: `witchcityrope-api:production` → `witchcityrope-api-production:latest`
- Web images: `witchcityrope-web:production` → `witchcityrope-web-production:latest`
- Git SHA tags: `witchcityrope-api:$GIT_SHA` → `witchcityrope-api-production:$GIT_SHA`

**Deployment**: Use `production-deploy` skill for automated production deployment with new repository structure.

## Server-Side Updates Required

### Staging Server (`/opt/witchcityrope/staging/docker-compose.staging.yml`)

Update image references:
```yaml
services:
  api:
    image: registry.digitalocean.com/witchcityrope/witchcityrope-api-staging:latest

  web:
    image: registry.digitalocean.com/witchcityrope/witchcityrope-web-staging:latest
```

### Production Server (`/opt/witchcityrope/production/docker-compose.production.yml`)

Update image references:
```yaml
services:
  api:
    image: registry.digitalocean.com/witchcityrope/witchcityrope-api-production:latest

  web:
    image: registry.digitalocean.com/witchcityrope/witchcityrope-web-production:latest
```

## Documentation Updated

1. **Staging Deployment Guide** (`/docs/functional-areas/deployment/staging-deployment-guide.md`)
   - Added registry structure section
   - Updated tagging convention documentation
   - Added troubleshooting for wrong repository usage

2. **Production Deployment Guide** (this document serves as update reference)
   - Document new registry structure
   - Update deployment procedures

## Migration Notes

### No Breaking Changes
- This is purely a naming convention change
- Existing images remain accessible
- New deployments will use new naming scheme

### Gradual Migration
1. Deploy updated skills (already complete)
2. Update server-side compose files
3. Next deployment will use new repository names
4. Old repositories can be cleaned up after verification

## Rollback Procedure

If issues arise with new registry structure:

1. **Update deployment scripts** to use previous naming:
   - Staging: `witchcityrope-api:latest`, `witchcityrope-web:latest`
   - Production: `witchcityrope-api:production`, `witchcityrope-web:production`

2. **Update server compose files** to reference old repositories

3. **Deploy** using updated configuration

## Verification Checklist

After first deployment with new structure:

- [ ] Staging deployment skill completes successfully
- [ ] Staging containers pull correct `-staging` images
- [ ] Production deployment skill completes successfully
- [ ] Production containers pull correct `-production` images
- [ ] Health checks pass on both environments
- [ ] No image confusion between staging and production

## Related Documentation

- [Staging Deployment Guide](./staging-deployment-guide.md)
- [Production Deployment Guide](./production-deployment-guide.md)
- [Staging Deploy Skill](/.claude/skills/staging-deploy/SKILL.md)
- [Production Deploy Skill](/.claude/skills/production-deploy/SKILL.md)

---

**Effective Date**: 2025-11-22
**Author**: Librarian Agent
**Status**: Active
**Impact**: Low (naming convention only, no functional changes)
