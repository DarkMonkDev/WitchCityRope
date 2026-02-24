# Exposed Secrets Inventory - WitchCityRope

**Date:** 2026-02-23
**Purpose:** Track all secrets found committed in repository files for rotation planning
**Repository:** Private (DarkMonkDev/WitchCityRope)

## Risk Assessment

- **Repository visibility:** Private
- **Git history:** Secrets exist in commit history even after removal from current files
- **Immediate risk:** Low (private repo, no public exposure)
- **Recommended action:** Rotate secrets at your convenience; prioritize database and JWT secrets

## Secrets Found in Repository Files

### Database Credentials

| Secret | File(s) | Service | Rotated? | Priority |
|--------|---------|---------|----------|----------|
| PostgreSQL password `WitchCity2024!` | `apps/api/Program.cs` (4 locations, now removed), `.env.development`, `.env.staging` | PostgreSQL database | No | **HIGH** - Database access |
| PostgreSQL connection strings | `.env.development`, `.env.staging`, `.env` | PostgreSQL | No | HIGH |

### Authentication Secrets

| Secret | File(s) | Service | Rotated? | Priority |
|--------|---------|---------|----------|----------|
| JWT secret key `DevSecret-JWT-WitchCityRope-AuthTest-2024-32CharMinimum!` | `apps/api/Program.cs` (now removed), `.env.development` | JWT token signing | No | **HIGH** - Token forgery risk |

### Third-Party API Keys

| Secret | File(s) | Service | Rotated? | Priority |
|--------|---------|---------|----------|----------|
| SendGrid API key | `.env.staging`, `.env` | Email delivery | No | MEDIUM |
| PayPal client ID/secret | `.env.staging`, `.env` | Payment processing | No | MEDIUM |
| DigitalOcean Spaces keys | `.env.staging`, `.env` | Object storage (backups) | No | MEDIUM |
| Authorize.net API login/transaction key | `.env.staging`, `.env` | Payment processing | No | MEDIUM |

### Infrastructure Secrets

| Secret | File(s) | Service | Rotated? | Priority |
|--------|---------|---------|----------|----------|
| Hangfire connection string | Was hardcoded in `Program.cs` (now removed) | Background jobs | No | LOW (same as DB) |

## What Was Fixed in This Audit

The hardcoded fallback secrets in `apps/api/Program.cs` have been replaced with fail-fast exceptions (`throw new InvalidOperationException`). The application now **requires** proper configuration through environment variables or user secrets - it will not silently fall back to development defaults.

## Rotation Instructions

### Database Password
1. Update PostgreSQL password on the server
2. Update `ConnectionStrings:DefaultConnection` in environment variables / Docker compose
3. Restart all containers

### JWT Secret Key
1. Generate new 32+ character secret: `openssl rand -base64 48`
2. Update `Jwt:SecretKey` in environment variables / Docker compose
3. Restart API container
4. **Note:** All existing tokens will be invalidated (users will need to re-login)

### Third-Party API Keys
1. Rotate in each provider's dashboard (SendGrid, PayPal, DigitalOcean, Authorize.net)
2. Update corresponding environment variables
3. Restart affected containers

## Rotation Schedule

| Priority | When to Rotate | Items |
|----------|---------------|-------|
| **HIGH** | Before next production deployment | Database password, JWT secret |
| **MEDIUM** | Within 30 days | SendGrid, PayPal, DO Spaces, Authorize.net keys |
| **LOW** | At convenience | Any remaining development-only secrets |
