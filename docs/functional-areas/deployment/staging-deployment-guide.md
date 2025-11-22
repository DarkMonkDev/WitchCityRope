# Staging Deployment Guide

## Overview

This guide covers deploying WitchCityRope to the staging environment on DigitalOcean. The staging environment uses DigitalOcean Container Registry for Docker images and a managed PostgreSQL database.

**Current Staging Environment:**
- **Server**: DigitalOcean Droplet at 104.131.165.14
- **Domain**: https://staging.notfai.com
- **User**: witchcity
- **Registry**: registry.digitalocean.com/witchcityrope
- **Database**: DigitalOcean Managed PostgreSQL
- **SSH Key**: `/home/chad/.ssh/id_ed25519_witchcityrope`

## 🚨 CRITICAL: Container Registry Structure

**NEW ORGANIZATION (2025-11-22):**

WitchCityRope uses environment-specific repository naming in the `witchcityrope` registry:

**Staging repositories:**
- `registry.digitalocean.com/witchcityrope/staging-api-witchcityrope`
- `registry.digitalocean.com/witchcityrope/staging-web-witchcityrope`

**Production repositories:**
- `registry.digitalocean.com/witchcityrope/production-api-witchcityrope`
- `registry.digitalocean.com/witchcityrope/production-web-witchcityrope`

**Accounting project (separate registry):**
- `registry.digitalocean.com/accounting/accounting-api`
- `registry.digitalocean.com/accounting/accounting-web`

This structure ensures:
- Clear environment separation (staging vs production)
- No accidental deployment to wrong environment
- Accounting project completely isolated

## ⚠️ CRITICAL: Shared Server Warning

**IMPORTANT**: The staging server hosts multiple applications. When deploying:
- Only touch WitchCityRope containers (witchcity-api-staging, witchcity-web-staging, witchcity-redis-staging)
- Never run `docker stop $(docker ps -q)` or similar commands that affect all containers
- Always use the specific compose file: `docker-compose -f docker-compose.staging.yml`

## Prerequisites

- SSH access to DigitalOcean droplet (witchcity user)
- SSH key configured: `/home/chad/.ssh/id_ed25519_witchcityrope`
- Docker and Docker Compose installed locally
- DigitalOcean Container Registry access (credentials in local `~/.docker/config.json`)
- Access to `.env.staging` file on server at `/opt/witchcityrope/staging/`

## Standard Deployment

**Use the `staging-deploy` skill for all standard deployments.**

The skill is located at `/.claude/skills/staging-deploy.md` and fully automates:
- Building production Docker images locally
- Pushing images to DigitalOcean Container Registry
- SSH to server and pulling latest images
- Restarting containers with age verification
- Health checks and smoke tests

**When to deploy:**
- After completing and testing new features locally
- After bug fixes that need staging verification
- After Phase 5 validation passes in workflow orchestration

**The skill will:**
1. Build API and Web images with `-staging` suffix and `:latest` tag
2. Push to DigitalOcean Container Registry
3. Connect to staging server
4. Pull latest images
5. Restart containers
6. Verify containers are newly created (< 120 seconds old)
7. Wait for services to stabilize
8. Run health checks (web, API, database)
9. Run smoke tests (homepage, events API)
10. Report deployment status

**If deployment skill fails:**
1. Check error message from skill for specific issue
2. Use `container-restart` skill to inspect container logs
3. Verify health endpoints are accessible
4. If database issues, see database management guide below

## 🚨 CRITICAL: Docker Image Tagging Convention

**STAGING USES `-staging` REPOSITORY SUFFIX WITH `:latest` TAG**

The `staging-deploy` skill automatically uses:
- Repository names: `staging-api-witchcityrope` and `staging-web-witchcityrope`
- Tag: `:latest` for rollout + `:git-sha` for traceability

**Why this structure:**
- Prevents accidental staging→production or production→staging deployments
- Repository name enforces environment isolation
- `:latest` tag maintains consistent deployment pattern
- Git SHA tags provide rollback capability

**The skill handles this automatically** - no manual tag management needed.

## Database Management

**For database operations, see:** `/docs/guides-setup/database-setup.md` (Staging Database Management section)

### Quick Reference:

**Full schema reset** (schema changes, migrations):
- Use `database-reset-staging` skill
- Drops BOTH `public` and `cms` schemas
- Migrations rebuild automatically

**Selective data reseed** (fresh data only):
- See database-setup.md for manual DELETE FROM procedure
- Use when schema is fine, just need fresh seed data

**⚠️ CRITICAL**: If you need to drop schemas manually, you MUST drop BOTH `public` AND `cms` schemas. Leftover CMS tables will cause migration failures with "relation already exists" errors.

## DigitalOcean Container Registry Authentication

The server already has registry credentials configured at `/home/witchcity/.docker/config.json`.

The `staging-deploy` skill handles authentication automatically via local Docker config.

**If local authentication needed**: Copy working credentials from server to `~/.docker/config.json` or generate new token via DigitalOcean Console → API → Container Registry.

## Monitoring and Troubleshooting

### Container Logs and Restart

**Use the `container-restart` skill** (located at `/.claude/skills/container-restart.md`) to:
- View container status
- Inspect error logs
- Restart containers if needed
- Monitor initialization logs

### Health Checks

**Manual health verification:**
```bash
# Web service
curl https://staging.notfai.com/

# API service
curl https://staging.notfai.com/api/health | jq .

# Expected response: {"status": "Healthy", "databaseConnected": true, ...}
```

### Common Issues

**Issue: Deployment appears successful but containers not restarted**
- **Cause**: This was a historical issue, now fixed in skill
- **Solution**: Skill now verifies container age (< 120 seconds)
- **Manual check**: Use `container-restart` skill to verify container creation time

**Issue: Health checks fail after deployment**
- **Cause**: Services need more time to start
- **Solution**: Wait longer (skill waits 30 seconds), use `container-restart` skill to check logs

**Issue: API shows database connection errors**
- **Cause**: Database credentials or network issue
- **Solution**: Check `.env.staging` on server, verify database is accessible

**Issue: Migrations fail on startup**
- **Cause**: Schema conflicts or leftover tables
- **Solution**: Use `database-reset-staging` skill for clean slate

**Issue: Wrong image repository being used**
- **Cause**: docker-compose.yml not updated with new `-staging` repositories
- **Solution**: Update docker-compose.staging.yml to use `-staging` repositories

## Deployment Workflow

**Standard workflow for new features:**

1. **Develop and test locally**
   - Use `./dev.sh` for local Docker environment
   - Run tests: `npm test` (frontend), `dotnet test` (backend)
   - Verify changes work locally

2. **Commit changes**
   - Follow commit message standards
   - Push to GitHub main branch

3. **Deploy to staging**
   - Use `staging-deploy` skill
   - Monitor output for any errors
   - Skill will report success/failure

4. **Verify deployment**
   - Test critical user flows manually
   - Check for console errors
   - Verify new features work as expected

5. **Monitor for issues**
   - Use `container-restart` skill if needed
   - Check API logs for errors
   - Verify database operations

## Skills Reference

**Primary deployment automation:**
- `staging-deploy` - Full deployment automation
- `database-reset-staging` - Full database schema reset
- `container-restart` - Container management and log inspection

**Location**: All skills in `/.claude/skills/` directory

## Related Documentation

- [Database Setup Guide](../../guides-setup/database-setup.md) - Database management (includes staging section)
- [Secrets Management Guide](../../guides-setup/secrets-management-guide-2025-10-24.md) - Credential management
- [Docker Development Guide](../../DOCKER_DEV_GUIDE.md) - Local development environment
- [Architecture Guide](../../ARCHITECTURE.md) - System architecture

## Legacy Documentation

Legacy self-hosted deployment procedures have been archived to:
`/docs/_archive/staging-deployment-guide-legacy-2025-11-05.md`

The current staging environment uses DigitalOcean infrastructure as described in this guide.

---

**Last Updated**: 2025-11-22
**Deployment Method**: Automated via `staging-deploy` skill
**Database Resets**: Automated via `database-reset-staging` skill
**Registry Structure**: Environment-specific repositories (-staging, -production)
