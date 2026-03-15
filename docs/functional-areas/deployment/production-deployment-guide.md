# WitchCityRope Production Deployment Guide

<!-- Last Updated: 2026-03-14 -->
<!-- Version: 2.0 -->
<!-- Owner: Infrastructure -->
<!-- Status: Active -->

## 1. Overview

WitchCityRope production runs on a DigitalOcean Droplet hosting three Docker containers (API, Web, Redis) behind system-level Nginx with SSL termination. The database is DigitalOcean Managed PostgreSQL (not self-hosted). Secrets are stored in HashiCorp Vault.

**Primary deployment method**: Use the `production-deploy` skill. This document is a reference guide for understanding the architecture and troubleshooting -- not a replacement for the skill.

## 2. Server Access

| Detail | Value |
|--------|-------|
| **Server** | DigitalOcean Droplet `104.131.165.14` |
| **User** | `witchcity` |
| **SSH Key** | `/home/chad/.ssh/id_ed25519_witchcityrope` |
| **Production Path** | `/opt/witchcityrope/production` |
| **Staging Path** | `/opt/witchcityrope/staging` |

```bash
ssh -i ~/.ssh/id_ed25519_witchcityrope witchcity@104.131.165.14
```

**Important**: This is a shared server hosting multiple apps. Always use environment-specific compose files and container names to avoid conflicts.

## 3. Architecture

### Container Layout

| Service | Container Name | Image | Port Mapping | Resources |
|---------|---------------|-------|--------------|-----------|
| **API** | `witchcity-api-prod` | `registry.digitalocean.com/witchcityrope/production-api-witchcityrope` | `5001:8080` | 2GB memory, 2 CPUs |
| **Web** | `witchcity-web-prod` | `registry.digitalocean.com/witchcityrope/production-web-witchcityrope` | `3001:80` | 512MB memory, 0.5 CPU |
| **Redis** | `witchcity-redis-prod` | `redis:7-alpine` | Internal only | 256MB memory, AOF persistence |

### Networking

```
Internet → Nginx (system, port 443) → localhost:3001 (Web container)
                                     → localhost:5001 (API via /api path)

API container → DigitalOcean Managed PostgreSQL (PgBouncer, port 25061)
API container → Redis container (internal Docker network)
```

- **Nginx**: System-level (NOT containerized), handles SSL termination and reverse proxy
- **Nginx config**: `/etc/nginx/sites-available/witchcityrope-production` on the server
- **SSL**: Let's Encrypt via Certbot with auto-renewal
- **Database**: DigitalOcean Managed PostgreSQL, connected through PgBouncer on port 25061

### Docker Compose File

The production compose file lives at `/home/chad/repos/witchcityrope/deployment/docker-compose.production.yml` in the repo. During deployment, it is SCP'd to `/opt/witchcityrope/production/` on the server.

Images are tagged with both `:latest` and `:<git-sha>` and pushed to `registry.digitalocean.com/witchcityrope/`.

### Secrets Management

- **Vault**: HashiCorp Vault at `https://vault.monksafterdark.com`
- **Production path**: `secret/projects/witchcityrope/production`
- **Vault helpers**: Use `vault-helpers.sh` shared skill for Vault operations
- Secrets are pulled from Vault during deployment and written to `.env.production` on the server

**⚠️ CRITICAL: Vault is the SINGLE SOURCE OF TRUTH for production environment variables.**
- The template file (`deployment/.env.production.template`) is a **reference only** — editing it does NOT change production.
- To change a production env var (e.g., `Frontend__Url`), you MUST update it in Vault:
  ```bash
  export VAULT_ADDR="https://vault.monksafterdark.com"
  ~/bin/vault kv patch secret/projects/witchcityrope/production KEY=VALUE
  ```
- Then redeploy using the `production-deploy` skill so the updated `.env.production` is written to the server.
- **Common mistake**: Editing the template file, redeploying, and wondering why nothing changed — because the deploy skill overwrites `.env.production` from Vault, not from the template.

## 4. Deployment Process

**Use the `production-deploy` skill.** It handles the full deployment pipeline:

1. Initialize Vault and pull environment variables
2. Pre-flight checks (clean git state, main branch, SSH connectivity, Docker login, registry access)
3. Build Docker images tagged with current git SHA
4. Push images to DigitalOcean Container Registry
5. SCP compose file and `.env.production` to server
6. Pull images on the server
7. Restart containers via `docker compose`
8. Run health checks (web, API, database connectivity)
9. Run smoke tests (homepage loads, events API responds)

**Do not run deployment steps manually.** The skill handles build ordering, tagging, health check retries, and rollback awareness.

## 5. Health Checks and Verification

### Built-in Container Health Checks

Each container has a Docker healthcheck defined in the compose file:

- **API**: `curl -f http://localhost:8080/health` (30s interval, 60s start period)
- **Web**: `curl -f http://localhost/` (30s interval, 30s start period)
- **Redis**: `redis-cli ping` (10s interval)

### Manual Verification

```bash
# On the server:
cd /opt/witchcityrope/production
docker compose -f docker-compose.production.yml ps
docker compose -f docker-compose.production.yml logs --tail=50 api
docker compose -f docker-compose.production.yml logs --tail=50 web

# From local machine (external):
curl -f https://witchcityrope.com
curl -f https://witchcityrope.com/api/health
```

## 6. Maintenance Mode

- **Enable**: Create flag file `/opt/witchcityrope/production/maintenance.flag` on the server
- **Disable**: Remove the flag file
- **Maintenance page**: Served from `/opt/witchcityrope/maintenance/maintenance.html`
- **Health check endpoints are exempt** from maintenance mode (monitoring continues working)

```bash
# Enable maintenance mode
ssh -i ~/.ssh/id_ed25519_witchcityrope witchcity@104.131.165.14 \
  "touch /opt/witchcityrope/production/maintenance.flag"

# Disable maintenance mode
ssh -i ~/.ssh/id_ed25519_witchcityrope witchcity@104.131.165.14 \
  "rm /opt/witchcityrope/production/maintenance.flag"
```

## 7. Backups

- **Storage**: DigitalOcean Spaces at `https://nyc3.digitaloceanspaces.com`, bucket `witchcityrope`
- **Production folder**: `backups/production/`
- **Method**: `pg_dump` executed via the API container (which has database connectivity)
- **Database backup config** is set via environment variables in the compose file (see `BackupConfiguration__*` vars)

Production backups are isolated from staging backups by folder prefix.

## 8. Troubleshooting

### Containers won't start

```bash
# Check logs for the failing container
docker compose -f docker-compose.production.yml logs api
docker compose -f docker-compose.production.yml logs web

# Common cause: .env.production missing or has wrong values
cat /opt/witchcityrope/production/.env.production | head -5
```

### API can't connect to database

- Verify the `PROD_DB_CONNECTION_STRING` in `.env.production` uses PgBouncer port `25061`
- Check DigitalOcean managed database dashboard for connection limits or maintenance windows
- Test connectivity from the API container: `docker exec witchcity-api-prod curl -v telnet://db-host:25061`

### Web container returns 502

- Usually means the API container is still starting (Web depends on API health check)
- Check API container health: `docker inspect witchcity-api-prod | grep -A5 Health`
- The API has a 60-second start period before health checks begin

### Redis connection issues

- Redis is internal-only (no port mapping to host)
- Containers reach Redis via Docker network name `redis` on default port `6379`
- Check Redis health: `docker exec witchcity-redis-prod redis-cli ping`

### SSL certificate renewal

- Certbot handles auto-renewal via systemd timer
- If renewal fails, check: `sudo certbot renew --dry-run` on the server
- Nginx reload may be needed after manual renewal: `sudo systemctl reload nginx`

### Rolling back

- Previous images are tagged with git SHA in the registry
- To rollback: set `IMAGE_TAG=<previous-sha>` in `.env.production`, then pull and restart
- Or redeploy from a previous commit using the `production-deploy` skill
