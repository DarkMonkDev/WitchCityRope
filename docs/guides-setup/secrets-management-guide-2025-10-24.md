# Secrets Management Guide - WitchCityRope
<!-- Last Updated: 2025-10-24 -->
<!-- Version: 1.0 -->
<!-- Owner: DevOps/Security Team -->
<!-- Status: Active -->

## Purpose
This guide provides comprehensive documentation for managing secrets across all WitchCityRope environments: local development, Docker development, and production deployment. Understanding and properly implementing these patterns is critical for security and operational success.

---

## Table of Contents
1. [Overview](#overview)
2. [Local Development - .NET User Secrets](#local-development---net-user-secrets)
3. [Docker Development - Environment Variables](#docker-development---environment-variables)
4. [Production - DigitalOcean Encrypted Environment Variables](#production---digitalocean-encrypted-environment-variables)
5. [Migration Path](#migration-path)
6. [Troubleshooting](#troubleshooting)

---

## Overview

### Three Secrets Management Approaches

WitchCityRope uses three distinct secrets management approaches depending on the environment:

| Environment | Approach | Security Level | Use Case |
|-------------|----------|----------------|----------|
| **Local Development** | .NET User Secrets | Medium | Individual developer machines |
| **Docker Development** | Environment Variables | Low | Shared development containers |
| **Production** | DigitalOcean Encrypted Variables | High | Production deployment |

### When to Use Each Approach

#### .NET User Secrets (Local Development)
- **Use when**: Developing directly on your local machine without Docker
- **Pros**:
  - Secrets stored outside project directory
  - Not checked into git
  - Per-developer configuration
  - Easy to manage with `dotnet` CLI
- **Cons**:
  - Only works for .NET projects
  - Not available in containerized environments
  - Must be configured on each developer machine

#### Docker Environment Variables (Docker Development)
- **Use when**: Running the full stack with `./dev.sh` or `docker-compose`
- **Pros**:
  - Works across all services
  - Easy to share team-wide defaults
  - Consistent across developer machines
  - Simple configuration in `docker-compose.dev.yml`
- **Cons**:
  - **DEVELOPMENT ONLY** - never use these values in production
  - Visible in docker-compose files
  - Lower security (intentionally, for convenience)

#### DigitalOcean Encrypted Variables (Production)
- **Use when**: Deploying to production
- **Pros**:
  - Encrypted at rest
  - Access-controlled
  - Environment-isolated
  - Audit trails
- **Cons**:
  - Platform-specific
  - Requires DigitalOcean App Platform
  - More complex to update

### Security Best Practices

#### Critical Rules
1. ✅ **NEVER** commit secrets to git (including `.env` files)
2. ✅ **NEVER** use development secrets in production
3. ✅ **ALWAYS** generate new secure keys for production
4. ✅ **ALWAYS** use encrypted/protected storage for production secrets
5. ✅ **NEVER** share production secrets via email/Slack/chat

#### Security Levels by Environment
- **Local Development**: Medium security - secrets on local disk, not in git
- **Docker Development**: Low security - **INTENTIONALLY INSECURE FOR CONVENIENCE**
- **Production**: High security - encrypted, access-controlled, audited

---

## Local Development - .NET User Secrets

### What Are .NET User Secrets?

.NET User Secrets is a development-time secret storage system that:
- Stores secrets **outside your project directory**
- Uses a unique project identifier (UserSecretsId)
- Never gets checked into source control
- Works seamlessly with .NET configuration system

### How They Work

**Location on Linux**: `~/.microsoft/usersecrets/51c30e23-eda7-47d3-b7ba-81004187bc0d/secrets.json`

The `UserSecretsId` is configured in `/apps/api/WitchCityRope.Api.csproj`:
```xml
<PropertyGroup>
  <UserSecretsId>51c30e23-eda7-47d3-b7ba-81004187bc0d</UserSecretsId>
</PropertyGroup>
```

### Setting Up User Secrets

#### Step 1: Verify User Secrets Are Initialized
```bash
# Navigate to API project
cd /home/chad/repos/witchcityrope/apps/api

# Verify UserSecretsId is configured
grep UserSecretsId WitchCityRope.Api.csproj
```

#### Step 2: Set Required Secrets

```bash
# JWT Secret Key (minimum 32 characters)
dotnet user-secrets set "Jwt:SecretKey" "your-secure-jwt-key-for-local-development-make-it-long-enough"

# Safety Encryption Key (base64-encoded 32-byte key)
dotnet user-secrets set "Safety:EncryptionKey" "NAQntjSAGLxD4SAO59B9VwRm7gUfv31+1F1R2F51zDg="

# Database Connection (if not using Docker)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5434;Database=witchcityrope_dev;Username=postgres;Password=devpass123"

# PayPal Sandbox Credentials (optional - for payment testing)
dotnet user-secrets set "PayPal:ClientId" "your-paypal-sandbox-client-id"
dotnet user-secrets set "PayPal:Secret" "your-paypal-sandbox-secret"

# SendGrid API Key (optional - for email testing)
dotnet user-secrets set "SendGrid:ApiKey" "your-sendgrid-api-key"
```

#### Step 3: Verify Secrets Are Set

```bash
# List all secrets for this project
dotnet user-secrets list

# View the actual secrets.json file
cat ~/.microsoft/usersecrets/51c30e23-eda7-47d3-b7ba-81004187bc0d/secrets.json
```

### Complete Setup Script

For convenience, here's a complete setup script:

```bash
#!/bin/bash
# File: setup-user-secrets.sh
# Sets up all required user secrets for local development

cd /home/chad/repos/witchcityrope/apps/api

echo "Setting up .NET User Secrets for WitchCityRope..."

# Core Authentication
dotnet user-secrets set "Jwt:SecretKey" "dev-jwt-secret-for-local-testing-make-it-long-enough"
dotnet user-secrets set "Jwt:Issuer" "WitchCityRope-API"
dotnet user-secrets set "Jwt:Audience" "WitchCityRope-Services"
dotnet user-secrets set "Jwt:ExpirationMinutes" "480"

# Safety System
dotnet user-secrets set "Safety:EncryptionKey" "NAQntjSAGLxD4SAO59B9VwRm7gUfv31+1F1R2F51zDg="

# Database (if not using Docker)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5434;Database=witchcityrope_dev;Username=postgres;Password=devpass123"

echo "User secrets configured successfully!"
echo "View secrets with: dotnet user-secrets list"
```

### Important Notes

#### User Secrets Are NOT Checked Into Git
The secrets are stored in your home directory, completely separate from the project:
- ✅ **Safe**: `~/.microsoft/usersecrets/[UserSecretsId]/secrets.json`
- ❌ **Never**: `/home/chad/repos/witchcityrope/secrets.json`

#### User Secrets Only Work in Development
The User Secrets system is **automatically disabled** in production (when `ASPNETCORE_ENVIRONMENT` is not "Development").

#### UserSecretsId Is Project-Specific
The `UserSecretsId` in the `.csproj` file links the project to the secrets location. **Never change this ID** unless you want to create a completely new secrets store.

---

## Docker Development - Environment Variables

### How Docker Environment Variables Work

When running WitchCityRope in Docker (via `./dev.sh` or `docker-compose`), secrets are provided via environment variables defined in `docker-compose.dev.yml`.

### Current Docker Development Secrets

From `/docker-compose.dev.yml` (lines 77-96):

```yaml
environment:
  # Development authentication settings
  Jwt__SecretKey: dev-jwt-secret-for-local-testing-make-it-long-enough
  Jwt__Issuer: WitchCityRope-API
  Jwt__Audience: WitchCityRope-Services
  Jwt__ExpirationMinutes: 480  # 8 hours for development convenience

  # Safety encryption key for development
  Safety__EncryptionKey: "NAQntjSAGLxD4SAO59B9VwRm7gUfv31+1F1R2F51zDg="

  # Development database connection
  ConnectionStrings__DefaultConnection: "Host=postgres;Port=5432;Database=witchcityrope_dev;Username=postgres;Password=devpass123;Include Error Detail=true"
```

### Critical Warnings

#### ⚠️ DEVELOPMENT ONLY - NEVER USE IN PRODUCTION
These secrets are **INTENTIONALLY INSECURE** for development convenience:
- Simple, easy-to-remember values
- Longer token lifetimes (8 hours vs 15 minutes)
- Visible in docker-compose files
- Same across all developer machines

#### ⚠️ DO NOT COMMIT REAL SECRETS
If you need to test with real API keys (PayPal, SendGrid), use a `.env.local` file:

```bash
# Create .env.local (already in .gitignore)
cat > docker-compose.env.local <<EOF
PAYPAL_CLIENT_ID=your-real-sandbox-client-id
PAYPAL_SECRET=your-real-sandbox-secret
SENDGRID_API_KEY=your-real-sendgrid-api-key
EOF

# Reference in docker-compose (DO NOT COMMIT THIS CHANGE)
services:
  api:
    env_file:
      - docker-compose.env.local
```

### Environment Variable Naming Convention

Docker uses double underscores (`__`) instead of colons (`:`) for nested configuration:

| Configuration Key | Docker Environment Variable |
|-------------------|----------------------------|
| `Jwt:SecretKey` | `Jwt__SecretKey` |
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` |
| `Safety:EncryptionKey` | `Safety__EncryptionKey` |

### Adding New Development Secrets

To add a new secret to the Docker development environment:

1. **Edit** `/docker-compose.dev.yml`
2. **Add** to the `api.environment` section:
   ```yaml
   services:
     api:
       environment:
         YourService__YourSecret: "development-value-here"
   ```
3. **Restart** containers:
   ```bash
   ./dev.sh
   ```

### Verifying Docker Secrets Are Loaded

```bash
# Check environment variables in running container
docker exec -it witchcity-api env | grep -E "Jwt|Safety|ConnectionStrings"

# Expected output:
# Jwt__SecretKey=dev-jwt-secret-for-local-testing-make-it-long-enough
# Safety__EncryptionKey=NAQntjSAGLxD4SAO59B9VwRm7gUfv31+1F1R2F51zDg=
# ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;...
```

---

## Production - DigitalOcean Encrypted Environment Variables

### How DigitalOcean App Platform Secrets Work

DigitalOcean App Platform provides encrypted environment variable storage:
- **Encrypted at rest** using platform encryption
- **Access-controlled** via team permissions
- **Environment-isolated** (staging vs production)
- **Audit trail** of changes
- **Encrypted in transit** to your application

### Comparison to .NET User Secrets

| Feature | .NET User Secrets | DigitalOcean Variables |
|---------|-------------------|------------------------|
| **Encryption** | OS-level file protection | Platform-encrypted at rest |
| **Access Control** | File system permissions | Team-based RBAC |
| **Audit Trail** | None | Full change history |
| **Availability** | Local development only | Production runtime |
| **Team Sharing** | Manual (not recommended) | Built-in with permissions |
| **Secrets Rotation** | Manual file edit | UI-based with versioning |

**Key Difference**: DigitalOcean variables are **production-grade encrypted secrets management**, while User Secrets are **convenience-focused development storage**.

### Step-by-Step Configuration Guide

#### 1. Access DigitalOcean App Platform
```
1. Log in to DigitalOcean
2. Navigate to App Platform
3. Select your WitchCityRope app
4. Go to Settings → App-Level Environment Variables
```

#### 2. Configure Authentication Secrets

**Generate Secure JWT Key** (minimum 64 characters for production):
```bash
# Generate a cryptographically secure random key
openssl rand -base64 64

# Example output (use your own!):
# xK8pL2mN9qR5sT7vY1zA3bC4dE6fG8hJ0kM2nP4qS6tU8vX0yZ2aB4cD6eF8gH0jK2lM4nP6qR8sT0uV2wX4yZ6a
```

**Configure in DigitalOcean**:
```
Key: Jwt__SecretKey
Value: [your-generated-64-character-key]
Type: Secret (encrypted)
Scope: All components
```

```
Key: Jwt__Issuer
Value: WitchCityRope-API
Type: Environment Variable
Scope: API component
```

```
Key: Jwt__Audience
Value: WitchCityRope-Services
Type: Environment Variable
Scope: API component
```

```
Key: Jwt__ExpirationMinutes
Value: 15
Type: Environment Variable
Scope: API component
```

#### 3. Configure Safety Encryption Key

**Generate Secure Encryption Key**:
```bash
# Generate a 256-bit (32-byte) key and base64 encode it
openssl rand -base64 32

# Example output (use your own!):
# YmJ3VzRxNjhQOXZCTWJFN0xzUjNGSDJjVUd6SzRwTWQ=
```

**Configure in DigitalOcean**:
```
Key: Safety__EncryptionKey
Value: [your-generated-base64-key]
Type: Secret (encrypted)
Scope: API component
```

#### 4. Configure Database Connection

**Note**: DigitalOcean Managed PostgreSQL provides connection strings automatically.

**If using Managed PostgreSQL**:
```
Key: ConnectionStrings__DefaultConnection
Value: ${db.DATABASE_URL}  # DigitalOcean auto-populates this
Type: Secret (encrypted)
Scope: API component
```

**If using external database**:
```
Key: ConnectionStrings__DefaultConnection
Value: Host=your-db.postgresql.database;Port=5432;Database=witchcityrope;Username=app_user;Password=[secure-password];SSL Mode=Require
Type: Secret (encrypted)
Scope: API component
```

#### 5. Configure PayPal Integration (Optional)

**Get PayPal Production Credentials**:
1. Log in to PayPal Developer Portal
2. Switch to **Live** mode (not Sandbox)
3. Create a new REST API app
4. Copy Client ID and Secret

**Configure in DigitalOcean**:
```
Key: PayPal__ClientId
Value: [your-live-paypal-client-id]
Type: Secret (encrypted)
Scope: API component
```

```
Key: PayPal__Secret
Value: [your-live-paypal-secret]
Type: Secret (encrypted)
Scope: API component
```

```
Key: PayPal__Mode
Value: Live
Type: Environment Variable
Scope: API component
```

#### 6. Configure SendGrid Email (Optional)

**Get SendGrid API Key**:
1. Log in to SendGrid
2. Settings → API Keys
3. Create API Key with "Full Access"
4. Copy the key (only shown once!)

**Configure in DigitalOcean**:
```
Key: SendGrid__ApiKey
Value: [your-sendgrid-api-key]
Type: Secret (encrypted)
Scope: API component
```

```
Key: SendGrid__FromEmail
Value: noreply@witchcityrope.com
Type: Environment Variable
Scope: API component
```

```
Key: SendGrid__FromName
Value: WitchCityRope
Type: Environment Variable
Scope: API component
```

### Security Considerations and Limitations

#### What DigitalOcean Provides
✅ **Encryption at rest** - Secrets encrypted in DigitalOcean's database
✅ **Encryption in transit** - TLS for API calls and container delivery
✅ **Access control** - Team-based permissions
✅ **Audit trail** - Change history logged
✅ **Secrets isolation** - Per-environment separation

#### What DigitalOcean Does NOT Provide
❌ **Hardware Security Modules (HSM)** - Not available
❌ **Secret rotation automation** - Manual process
❌ **Secret versioning with rollback** - Limited history
❌ **Fine-grained secret access per service** - Component-level only
❌ **Integration with external vaults** - DigitalOcean-only

#### Comparing to Enterprise Solutions

| Feature | DigitalOcean | AWS Secrets Manager | Azure Key Vault | HashiCorp Vault |
|---------|-------------|---------------------|-----------------|-----------------|
| Encryption at rest | ✅ | ✅ | ✅ | ✅ |
| Automatic rotation | ❌ | ✅ | ✅ | ✅ |
| HSM-backed | ❌ | ✅ (Premium) | ✅ (Premium) | ✅ |
| Versioning | Limited | ✅ | ✅ | ✅ |
| Cost (monthly) | Free | ~$0.40/secret | ~$0.03/secret | Self-hosted |
| Complexity | Low | Medium | Medium | High |

**For WitchCityRope**: DigitalOcean's approach is **sufficient for our scale and security requirements**.

### Best Practices for Team Access Control

#### Role-Based Access
```
Production Secrets:
- View: Senior Developers, DevOps, CTO
- Edit: DevOps Lead, CTO only
- Deploy: CI/CD system only

Staging Secrets:
- View: All Developers
- Edit: Senior Developers, DevOps
- Deploy: CI/CD system, Developers
```

#### Audit Trail Review
```
Monthly Review Checklist:
1. Review all secret changes in audit log
2. Verify no unauthorized access
3. Check for secrets that should be rotated
4. Confirm team access levels are appropriate
```

#### Emergency Access
```
Break-Glass Procedure:
1. Document emergency in incident log
2. Access secrets via owner account
3. Complete emergency work
4. Rotate all accessed secrets within 24 hours
5. Document in post-mortem
```

---

## Migration Path

### Development to Production Checklist

When deploying to production for the first time:

#### Phase 1: Generate New Production Secrets
```bash
# Generate JWT Secret (64+ characters)
openssl rand -base64 64 > production-jwt-secret.txt

# Generate Safety Encryption Key (32 bytes)
openssl rand -base64 32 > production-safety-key.txt

# NEVER commit these files to git
# Store securely (password manager, encrypted drive)
```

#### Phase 2: Configure DigitalOcean Secrets
```
□ Log in to DigitalOcean App Platform
□ Navigate to WitchCityRope app settings
□ Add Jwt__SecretKey (from production-jwt-secret.txt)
□ Add Safety__EncryptionKey (from production-safety-key.txt)
□ Add ConnectionStrings__DefaultConnection (from managed database)
□ Add PayPal__ClientId and PayPal__Secret (if using PayPal)
□ Add SendGrid__ApiKey (if using SendGrid)
□ Verify all secrets marked as "Secret" type (encrypted)
□ Save changes and trigger deployment
```

#### Phase 3: Verify Production Secrets
```bash
# After deployment, verify API health check
curl https://your-app.ondigitalocean.app/health

# Expected response: { "status": "Healthy", ... }

# Verify authentication works
curl -X POST https://your-app.ondigitalocean.app/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@witchcityrope.com","password":"Test123!"}'

# Expected: JWT token returned
```

#### Phase 4: Secure Cleanup
```bash
# Delete local production secret files
shred -u production-jwt-secret.txt
shred -u production-safety-key.txt

# Store secrets in password manager
# Format: "WitchCityRope Production - [Secret Name]"
# Include: Secret value, date created, who generated it
```

### Staging Environment (Recommended)

Before deploying to production, test with a staging environment:

```
Staging Environment Secrets:
- Use DIFFERENT secrets from development and production
- Use PayPal Sandbox (not Live)
- Use SendGrid test mode
- Use separate database
- Document as "Staging - NOT PRODUCTION"
```

**Benefits**:
- Test secret configuration without production risk
- Verify deployment process works
- Validate secret rotation procedures
- Practice emergency access procedures

---

## Troubleshooting

### Common Issues and Solutions

#### Issue 1: "Secret key not found" Error

**Symptoms**:
```
System.InvalidOperationException: Secret key for JWT generation not configured
```

**Causes**:
1. User secrets not initialized
2. Environment variable not set in Docker
3. DigitalOcean secret not configured

**Solutions**:

**Local Development**:
```bash
# Verify user secrets exist
dotnet user-secrets list --project /home/chad/repos/witchcityrope/apps/api

# If empty, set the secret
dotnet user-secrets set "Jwt:SecretKey" "dev-jwt-secret-for-local-testing-make-it-long-enough" \
  --project /home/chad/repos/witchcityrope/apps/api
```

**Docker Development**:
```bash
# Check docker-compose.dev.yml has the secret
grep "Jwt__SecretKey" docker-compose.dev.yml

# Expected output:
# Jwt__SecretKey: dev-jwt-secret-for-local-testing-make-it-long-enough

# If missing, add it and restart
./dev.sh
```

**Production**:
```
1. Log in to DigitalOcean
2. App Platform → Settings → Environment Variables
3. Verify "Jwt__SecretKey" exists and is type "Secret"
4. If missing, add it and redeploy
```

#### Issue 2: "Invalid connection string" Database Error

**Symptoms**:
```
Npgsql.NpgsqlException: Connection refused
```

**Causes**:
1. Database not running (Docker)
2. Wrong port in connection string
3. Wrong credentials

**Solutions**:

**Docker Development**:
```bash
# Verify database container is running
docker ps | grep postgres

# Expected: witchcity-postgres container running

# If not running, start containers
./dev.sh

# Verify connection string in docker-compose.dev.yml
grep "ConnectionStrings__DefaultConnection" docker-compose.dev.yml

# Expected: Host=postgres;Port=5432;Database=witchcityrope_dev;Username=postgres;Password=devpass123
```

**Local Development**:
```bash
# Verify PostgreSQL is running on correct port
pg_isready -h localhost -p 5434

# Expected: localhost:5434 - accepting connections

# If wrong port, update user secret
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5434;Database=witchcityrope_dev;Username=postgres;Password=devpass123" \
  --project /home/chad/repos/witchcityrope/apps/api
```

**Production**:
```
1. DigitalOcean managed database should auto-configure
2. Verify "DATABASE_URL" component variable exists
3. Connection string should reference ${db.DATABASE_URL}
4. Check database is in same region as app
5. Verify database is running in DigitalOcean console
```

#### Issue 3: PayPal Webhook Failures

**Symptoms**:
```
PayPal webhook verification failed: Invalid credentials
```

**Causes**:
1. Using Sandbox credentials in production
2. Using Production credentials in development
3. Wrong client ID or secret

**Solutions**:

**Docker Development**:
```yaml
# In docker-compose.dev.yml, verify Sandbox mode
PayPal__Mode: Sandbox
PayPal__ClientId: [your-sandbox-client-id]
PayPal__Secret: [your-sandbox-secret]
```

**Production**:
```
In DigitalOcean:
1. PayPal__Mode = "Live" (NOT "Sandbox")
2. PayPal__ClientId = [production client ID]
3. PayPal__Secret = [production secret]
4. Verify credentials in PayPal Live dashboard
```

#### Issue 4: SendGrid Email Not Sending

**Symptoms**:
```
SendGrid API returned 401 Unauthorized
```

**Causes**:
1. Invalid API key
2. API key permissions insufficient
3. API key expired or revoked

**Solutions**:

**Verify API Key**:
```bash
# Test API key with curl (development)
curl --request POST \
  --url https://api.sendgrid.com/v3/mail/send \
  --header "Authorization: Bearer YOUR_API_KEY" \
  --header 'Content-Type: application/json' \
  --data '{
    "personalizations": [{"to": [{"email": "test@example.com"}]}],
    "from": {"email": "noreply@witchcityrope.com"},
    "subject": "Test",
    "content": [{"type": "text/plain", "value": "Test"}]
  }'

# Expected: 202 Accepted
# If 401: API key is invalid
```

**Regenerate API Key**:
```
1. Log in to SendGrid
2. Settings → API Keys
3. Delete old key
4. Create new key with "Full Access"
5. Update secret in environment (user secrets, Docker, or DigitalOcean)
6. Restart application
```

#### Issue 5: Secrets Not Loaded After Configuration

**Symptoms**:
- Secrets configured but application still shows "not found"
- Environment variables empty in container

**Diagnostic Steps**:

**1. Verify Environment Variable Names**:
```bash
# Common mistake: Using colons instead of double underscores in Docker
# WRONG:  Jwt:SecretKey
# CORRECT: Jwt__SecretKey
```

**2. Check Configuration Binding**:
```bash
# In running container, verify environment
docker exec -it witchcity-api printenv | grep -i jwt

# Should show: Jwt__SecretKey=your-secret-here
```

**3. Verify Startup Logs**:
```bash
# Check for configuration errors
docker logs witchcity-api | grep -i error

# Look for: "Configuration 'Jwt:SecretKey' not found"
```

**4. Production DigitalOcean Verification**:
```
1. App Platform → Runtime Logs
2. Search for "Configuration" errors
3. Verify secret scope is correct (app-level vs component-level)
4. Check secret was saved (not just previewed)
5. Verify deployment was triggered after saving secrets
```

### How to Verify Secrets Are Loaded Correctly

#### Local Development Verification

```bash
# 1. Check user secrets exist
cd /home/chad/repos/witchcityrope/apps/api
dotnet user-secrets list

# Expected output:
# Jwt:SecretKey = dev-jwt-secret-for-local-testing-make-it-long-enough
# Safety:EncryptionKey = NAQntjSAGLxD4SAO59B9VwRm7gUfv31+1F1R2F51zDg=
# ConnectionStrings:DefaultConnection = Host=localhost;Port=5434;...

# 2. Run API locally and check health endpoint
dotnet run
curl http://localhost:5655/health

# Expected: {"status": "Healthy", ...}
```

#### Docker Development Verification

```bash
# 1. Start containers
./dev.sh

# 2. Check environment variables in container
docker exec -it witchcity-api env | grep -E "Jwt|Safety|ConnectionStrings"

# Expected output:
# Jwt__SecretKey=dev-jwt-secret-for-local-testing-make-it-long-enough
# Safety__EncryptionKey=NAQntjSAGLxD4SAO59B9VwRm7gUfv31+1F1R2F51zDg=
# ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;...

# 3. Check API health
curl http://localhost:5655/health

# Expected: {"status": "Healthy", ...}
```

#### Production Verification

```bash
# 1. Check health endpoint
curl https://your-app.ondigitalocean.app/health

# Expected: {"status": "Healthy", ...}

# 2. Verify authentication works (tests JWT secret)
curl -X POST https://your-app.ondigitalocean.app/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@witchcityrope.com","password":"Test123!"}'

# Expected: {"token": "eyJ...", "user": {...}}

# 3. Check DigitalOcean runtime logs
# Navigate to: App Platform → Runtime Logs
# Search for: "Configuration" or "Secret"
# Should NOT see: "not found" or "missing" errors
```

### What to Do If Secrets Are Not Working

#### Escalation Path

1. **Level 1: Self-Service Checks** (5 minutes)
   - Verify correct environment (local/Docker/production)
   - Check secret names for typos
   - Verify container/app restarted after changes

2. **Level 2: Documentation Review** (15 minutes)
   - Re-read relevant section of this guide
   - Check troubleshooting section
   - Review application logs for specific errors

3. **Level 3: Team Assistance** (30 minutes)
   - Share error messages with team
   - Provide environment details
   - Document steps already attempted

4. **Level 4: Emergency Access** (1 hour)
   - Use break-glass procedure for production
   - Document in incident log
   - Schedule post-mortem review

---

## Appendix: Quick Reference

### Environment Variable Naming Reference

| Configuration Path | User Secrets | Docker Environment |
|-------------------|-------------|-------------------|
| `Jwt:SecretKey` | `Jwt:SecretKey` | `Jwt__SecretKey` |
| `Jwt:Issuer` | `Jwt:Issuer` | `Jwt__Issuer` |
| `Jwt:Audience` | `Jwt:Audience` | `Jwt__Audience` |
| `Safety:EncryptionKey` | `Safety:EncryptionKey` | `Safety__EncryptionKey` |
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` |
| `PayPal:ClientId` | `PayPal:ClientId` | `PayPal__ClientId` |
| `SendGrid:ApiKey` | `SendGrid:ApiKey` | `SendGrid__ApiKey` |

### Secret Generation Commands

```bash
# JWT Secret (64 characters minimum for production)
openssl rand -base64 64

# Safety Encryption Key (32 bytes = 256 bits)
openssl rand -base64 32

# Random password (32 characters)
openssl rand -base64 32 | tr -d '/+=' | cut -c1-32

# UUID (for database identifiers)
uuidgen
```

### Useful Commands

```bash
# List all user secrets
dotnet user-secrets list --project /home/chad/repos/witchcityrope/apps/api

# Remove a user secret
dotnet user-secrets remove "Jwt:SecretKey" --project /home/chad/repos/witchcityrope/apps/api

# Clear all user secrets (CAREFUL!)
dotnet user-secrets clear --project /home/chad/repos/witchcityrope/apps/api

# Check environment in running Docker container
docker exec -it witchcity-api printenv

# View Docker container logs
docker logs witchcity-api

# Restart Docker containers
./dev.sh
```

---

## Related Documentation

- **Docker Development Guide**: `/DOCKER_DEV_GUIDE.md`
- **Database Migrations Guide**: `/docs/standards-processes/backend/database-migrations-guide.md`
- **Deployment Guide**: `/docs/functional-areas/deployment/staging-deployment-guide.md`
- **Authentication Documentation**: `/docs/functional-areas/authentication/`
- **PayPal Integration**: `/docs/functional-areas/payment-paypal-venmo/`

---

## Document Change Log

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2025-10-24 | 1.0 | Initial creation - comprehensive secrets management guide covering local, Docker, and production | Librarian Agent |

