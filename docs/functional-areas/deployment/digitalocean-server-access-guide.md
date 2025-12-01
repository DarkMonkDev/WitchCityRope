# DigitalOcean Server Access Guide

**Purpose**: Complete guide for accessing and configuring the WitchCityRope DigitalOcean server for staging and production environments.

**Last Updated**: 2025-11-21

---

## Server Information

**Droplet Details**:
- **Name**: witchcityrope-prod
- **IP Address**: 104.131.165.14
- **Droplet ID**: 523718929
- **System User**: witchcity (SSH key-only access)
- **Operating System**: Ubuntu (DigitalOcean managed)

**Application Directories**:
- **Staging**: `/opt/witchcityrope/staging/`
- **Production**: `/opt/witchcityrope/production/`

---

## SSH Access

### ✅ Correct Method - SSH as witchcity User

```bash
# SSH to the server
ssh -i /home/chad/.ssh/id_ed25519_witchcityrope witchcity@104.131.165.14

# Quick test
ssh -i /home/chad/.ssh/id_ed25519_witchcityrope witchcity@104.131.165.14 "whoami"
```

**Why this works**:
- The `witchcity` user has SSH key-only access (no password - this is secure)
- SSH key location: `/home/chad/.ssh/id_ed25519_witchcityrope`
- Passwordless sudo configured for specific administrative commands

### ❌ Wrong Method - Don't Try Root SSH

```bash
# This FAILS - root doesn't have SSH key configured
ssh root@104.131.165.14
```

**Why it fails**: Root user SSH is not configured. The witchcity user is the proper access method.

---

## Passwordless Sudo Configuration

Administrative tasks require sudo, which is configured for specific commands only:

**Configuration File**: `/etc/sudoers.d/witchcity-automation`

**Allowed Commands**:
```bash
witchcity ALL=(ALL) NOPASSWD: /usr/sbin/nginx, /usr/bin/certbot, /bin/systemctl, /bin/cp, /bin/ln
```

**Usage Example**:
```bash
# These work without password
sudo nginx -t
sudo certbot --nginx -d staging.notfai.com
sudo systemctl reload nginx
sudo cp /tmp/config.conf /etc/nginx/sites-available/
```

**Security Note**: Only specific commands are allowed via passwordless sudo. This maintains security while enabling automation.

---

## Credentials Storage

All DigitalOcean credentials are stored in **.NET User Secrets** on the local development machine.

**View Stored Credentials**:
```bash
cd /home/chad/repos/witchcityrope/apps/api
dotnet user-secrets list | grep DigitalOcean
```

**Stored Credentials Include**:
- `DigitalOcean:Token` - API token for doctl and API operations
- `DigitalOcean:DropletHost` - Server IP (104.131.165.14)
- `DigitalOcean:DropletUser` - SSH user (witchcity)
- `DigitalOcean:Spaces:AccessKey:WitchCityRope` - DO Spaces access key
- `DigitalOcean:Spaces:SecretKey:WitchCityRope` - DO Spaces secret key
- `DigitalOcean:Spaces:BucketName:WitchCityRope` - witchcityrope
- `DigitalOcean:Spaces:AccessKey:Accounting` - Accounting project spaces
- `DigitalOcean:Spaces:SecretKey:Accounting` - Accounting project spaces
- `DigitalOcean:Spaces:BucketName:Accounting` - accounting-backups

**Note**: Credentials are stored locally and never committed to version control.

---

## PostgreSQL Connection (via PgBouncer)

**CRITICAL**: All database connections go through PgBouncer connection pooler to prevent connection exhaustion.

### PgBouncer Configuration (Implemented 2025-11-29)

| Pool | Port | Size | Mode | Database | User |
|------|------|------|------|----------|------|
| pgbouncer-staging | 25061 | 10 | transaction | witchcityrope_staging | doadmin |
| pgbouncer-production | 25061 | 12 | transaction | witchcityrope_production | witchcity_production |

**Port Reference**:
- **25061** = PgBouncer (recommended - all app connections)
- **25060** = Direct database (only for admin tasks if needed)

### Connection String Format

**CRITICAL**: Connection strings MUST use keyword-value format for Npgsql/Hangfire compatibility.

**✅ CORRECT Format** (PgBouncer via port 25061):
```
Host=server.com;Port=25061;Database=pgbouncer-staging;Username=user;Password=pass;SSL Mode=Require;Trust Server Certificate=true
```

**❌ WRONG Format** (URI format - causes Hangfire failures):
```
postgresql://user:password@server.com:25061/pgbouncer-staging?sslmode=require
```

**Why PgBouncer**:
- DigitalOcean basic tier only allows ~25 connections
- Hangfire alone consumes ~13 connections per worker
- PgBouncer multiplexes many app connections through few DB connections
- Transaction mode releases connections back to pool after each transaction

**Research Document**: `/docs/architecture/postgresql-connection-pool-exhaustion-research.md`

### Managing PgBouncer Pools

```bash
# List pools
doctl databases pool list <cluster-id>

# Create new pool
doctl databases pool create <cluster-id> <pool-name> --db <database> --size <N> --mode transaction --user <user>

# Delete pool
doctl databases pool delete <cluster-id> <pool-name> --force
```

**Verification**: Both staging and production `.env` files should use port 25061 and pgbouncer-* database names.

---

## Common Administrative Tasks

### Nginx Configuration

**Update Nginx Configuration**:
```bash
# 1. Upload config to /tmp/
scp -i /home/chad/.ssh/id_ed25519_witchcityrope nginx-config.conf witchcity@104.131.165.14:/tmp/

# 2. SSH to server
ssh -i /home/chad/.ssh/id_ed25519_witchcityrope witchcity@104.131.165.14

# 3. Copy to sites-available
sudo cp /tmp/nginx-config.conf /etc/nginx/sites-available/witchcityrope-production

# 4. Enable site
sudo ln -sf /etc/nginx/sites-available/witchcityrope-production /etc/nginx/sites-enabled/

# 5. Test configuration
sudo nginx -t

# 6. Reload Nginx
sudo systemctl reload nginx
```

**Nginx Configuration Locations**:
- Staging: `/etc/nginx/sites-available/notfai-staging`
- Production: `/etc/nginx/sites-available/witchcityrope-production`

### SSL Certificate Management

**Install SSL Certificates** (certbot):
```bash
# SSH to server
ssh -i /home/chad/.ssh/id_ed25519_witchcityrope witchcity@104.131.165.14

# Run certbot
sudo certbot --nginx -d prod.notfai.com -d prod.witchcityrope.com

# Certbot handles:
# - Domain verification
# - Certificate installation
# - Nginx configuration updates
# - Auto-renewal setup
```

**Renew Certificates**:
```bash
# Certbot auto-renewal is configured
# Manual renewal if needed:
sudo certbot renew
```

**SSL Certificate Status**:
- **Staging**: staging.notfai.com (active)
- **Production**: prod.notfai.com, prod.witchcityrope.com (pending DNS setup)

### Application Deployment

**Standard Deployment** (use skills):
```bash
# Use staging-deploy or production-deploy skills
# These handle: build, push to registry, SSH, docker-compose commands
```

**Manual Deployment** (if needed):
- Use `staging-deploy` skill for staging deployments
- Use `production-deploy` skill for production deployments
- Use `restart-dev-containers` skill for container management
- See deployment guides for manual procedures if skills fail

---

## DigitalOcean CLI (doctl)

**doctl** is pre-configured with API token for DigitalOcean management.

**Configuration**: `/home/chad/.config/doctl/config.yaml`

**Common Commands**:
```bash
# List droplets
doctl compute droplet list --format ID,Name,PublicIPv4,Status

# SSH via droplet ID
doctl compute ssh 523718929

# Container registry login
doctl registry login

# List container images
doctl registry repository list-v2

# Manage DNS (if using DO DNS)
doctl compute domain records list witchcityrope.com
```

---

## Troubleshooting

### SSH Connection Issues

**Problem**: Can't connect via SSH

**Solutions**:
```bash
# 1. Verify SSH key exists
ls -la /home/chad/.ssh/id_ed25519_witchcityrope

# 2. Check SSH key permissions (should be 600)
chmod 600 /home/chad/.ssh/id_ed25519_witchcityrope

# 3. Test SSH connection
ssh -v -i /home/chad/.ssh/id_ed25519_witchcityrope witchcity@104.131.165.14 "whoami"

# 4. Check droplet status via doctl
doctl compute droplet list --format ID,Name,Status

# 5. Use DigitalOcean console access
# Go to: DigitalOcean dashboard > Droplets > witchcityrope-prod > Access > Console
```

### Sudo Issues

**Problem**: Sudo requires password or command not allowed

**Check passwordless sudo configuration**:
```bash
ssh -i /home/chad/.ssh/id_ed25519_witchcityrope witchcity@104.131.165.14 "sudo -n -l"
```

**Reconfigure** (requires root access via DO console):
```bash
# Log in as root via DigitalOcean console, then:
echo "witchcity ALL=(ALL) NOPASSWD: /usr/sbin/nginx, /usr/bin/certbot, /bin/systemctl, /bin/cp, /bin/ln" | tee /etc/sudoers.d/witchcity-automation
chmod 0440 /etc/sudoers.d/witchcity-automation

# Verify
sudo -u witchcity sudo -n nginx -t
```

### Container Issues

**Problem**: Containers not starting or behaving unexpectedly

**Debugging Steps**:
- Use `restart-dev-containers` skill for container management and troubleshooting
- Skill handles: status checks, log viewing, container restarts
- See restart-dev-containers skill documentation for specific commands

### Database Connection Issues

**Problem**: Application can't connect to database

**Debugging**:
- SSH to server and check .env file for DB_CONNECTION configuration
- Test API health endpoint: `curl https://staging.notfai.com/api/health/database`
- Use `restart-dev-containers` skill to check API logs for connection errors
- Verify connection string format (keyword-value format, not URI format - see above)

---

## Security Best Practices

### SSH Key Management
- ✅ **DO**: Use SSH keys only (no passwords)
- ✅ **DO**: Keep SSH private key secure with 600 permissions
- ❌ **DON'T**: Share SSH private keys
- ❌ **DON'T**: Commit SSH keys to version control

### Sudo Configuration
- ✅ **DO**: Limit passwordless sudo to specific commands only
- ✅ **DO**: Document which commands are allowed
- ❌ **DON'T**: Grant blanket NOPASSWD sudo access
- ❌ **DON'T**: Add commands without security review

### Credentials Management
- ✅ **DO**: Store credentials in .NET User Secrets locally
- ✅ **DO**: Use environment variables on server
- ✅ **DO**: Rotate API tokens periodically
- ❌ **DON'T**: Hardcode credentials in code
- ❌ **DON'T**: Commit .env files to version control

### Server Access
- ✅ **DO**: Use non-root user (witchcity) for operations
- ✅ **DO**: Keep server software updated
- ✅ **DO**: Monitor server logs regularly
- ❌ **DON'T**: Run services as root
- ❌ **DON'T**: Expose unnecessary ports

---

## Quick Reference

### SSH Access
```bash
ssh -i /home/chad/.ssh/id_ed25519_witchcityrope witchcity@104.131.165.14
```

### View Credentials
```bash
cd /home/chad/repos/witchcityrope/apps/api && dotnet user-secrets list | grep DigitalOcean
```

### Staging Directory
```bash
/opt/witchcityrope/staging/
```

### Production Directory
```bash
/opt/witchcityrope/production/
```

### Common Sudo Commands
```bash
sudo nginx -t
sudo systemctl reload nginx
sudo certbot --nginx -d example.com
```

### Container Management
```bash
# Use restart-dev-containers skill for all container operations
```

---

## Related Documentation

- **Staging Deployment**: `/docs/functional-areas/deployment/staging-deployment-guide.md`
- **Production Deployment**: `/docs/functional-areas/deployment/production-deployment-guide.md`
- **Staging Deploy Skill**: `/.claude/skills/staging-deploy/SKILL.md`
- **Production Deploy Skill**: `/.claude/skills/production-deploy/SKILL.md`
- **Secrets Management**: `/docs/guides-setup/secrets-management-guide-2025-10-24.md`

---

**Note**: This guide focuses on server access and configuration. For application deployment procedures, use the deployment skills and guides listed above.
