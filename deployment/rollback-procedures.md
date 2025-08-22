# WitchCityRope Rollback Procedures

This document outlines the procedures for rolling back a deployment in case of critical issues.

## Table of Contents

1. [Overview](#overview)
2. [When to Rollback](#when-to-rollback)
3. [Pre-Rollback Checklist](#pre-rollback-checklist)
4. [Rollback Procedures](#rollback-procedures)
   - [Docker Deployment Rollback](#docker-deployment-rollback)
   - [Linux VPS Rollback](#linux-vps-rollback)
   - [Windows VPS Rollback](#windows-vps-rollback)
5. [Database Rollback](#database-rollback)
6. [Post-Rollback Verification](#post-rollback-verification)
7. [Emergency Procedures](#emergency-procedures)
8. [Rollback Scripts](#rollback-scripts)

## Overview

Rollback procedures are critical for maintaining service availability when deployments fail. This guide provides step-by-step instructions for different deployment scenarios.

### Key Principles

1. **Speed**: Minimize downtime by having pre-planned procedures
2. **Safety**: Preserve data integrity during rollback
3. **Verification**: Confirm system health after rollback
4. **Documentation**: Record what went wrong for future prevention

## When to Rollback

Initiate rollback procedures when:

- [ ] Critical functionality is broken in production
- [ ] Database corruption or data loss is detected
- [ ] Performance degradation affects user experience
- [ ] Security vulnerabilities are exposed
- [ ] Multiple services fail health checks
- [ ] Payment processing is compromised
- [ ] Authentication system fails

### Severity Levels

| Level | Description | Action | Timeframe |
|-------|-------------|--------|-----------|
| **Critical** | Complete service outage | Immediate rollback | < 15 minutes |
| **High** | Major feature broken | Assess and rollback | < 30 minutes |
| **Medium** | Partial functionality affected | Hotfix or rollback | < 2 hours |
| **Low** | Minor issues | Schedule fix | Next maintenance |

## Pre-Rollback Checklist

Before initiating rollback:

1. **Communication**
   - [ ] Notify team lead/manager
   - [ ] Alert support team
   - [ ] Prepare user communication
   - [ ] Update status page

2. **Assessment**
   - [ ] Identify exact issue
   - [ ] Determine affected components
   - [ ] Check if hotfix is possible
   - [ ] Verify backup availability

3. **Preparation**
   - [ ] Locate recent backup
   - [ ] Ensure rollback scripts are ready
   - [ ] Have monitoring dashboards open
   - [ ] Clear browser caches

## Rollback Procedures

### Docker Deployment Rollback

#### Step 1: Stop Current Deployment
```bash
cd /opt/witchcityrope
docker-compose down
```

#### Step 2: Restore Previous Images
```bash
# List available images
docker images | grep witchcityrope

# Tag previous version as latest
docker tag witchcityrope/api:v1.2.3 witchcityrope/api:latest
docker tag witchcityrope/web:v1.2.3 witchcityrope/web:latest
```

#### Step 3: Restore Database Backup
```bash
# Stop containers
docker-compose down

# Restore database
cp /var/backups/witchcityrope/backup-20240127-020000/data/witchcityrope.db \
   ./data/witchcityrope.db

# Set permissions
chmod 666 ./data/witchcityrope.db
```

#### Step 4: Start Previous Version
```bash
# Use previous docker-compose file if changed
cp /var/backups/witchcityrope/backup-20240127-020000/docker-compose.yml .

# Start services
docker-compose up -d

# Check status
docker-compose ps
docker-compose logs --tail=50
```

### Linux VPS Rollback

#### Step 1: Stop Services
```bash
# If using systemd
sudo systemctl stop witchcityrope

# If using Docker
cd /opt/witchcityrope
docker-compose down

# If using PM2
pm2 stop all
```

#### Step 2: Restore Application Files
```bash
# Identify backup
BACKUP_DIR="/var/backups/witchcityrope/backup-20240127-020000"

# Create rollback directory
sudo mv /opt/witchcityrope /opt/witchcityrope.failed
sudo cp -r $BACKUP_DIR /opt/witchcityrope

# Restore permissions
sudo chown -R www-data:www-data /opt/witchcityrope/data
sudo chown -R www-data:www-data /opt/witchcityrope/logs
sudo chown -R www-data:www-data /opt/witchcityrope/uploads
```

#### Step 3: Restore Configuration
```bash
# Restore environment configuration
cp $BACKUP_DIR/.env /opt/witchcityrope/.env

# Restore nginx configuration
sudo cp $BACKUP_DIR/nginx-config /etc/nginx/sites-available/witchcityrope
sudo nginx -t
sudo systemctl reload nginx
```

#### Step 4: Restart Services
```bash
# Start application
cd /opt/witchcityrope
docker-compose up -d

# Or with systemd
sudo systemctl start witchcityrope

# Verify
curl -f http://localhost:5653/health
```

### Windows VPS Rollback

#### Step 1: Stop IIS Services
```powershell
# Stop app pools
Stop-WebAppPool -Name "WitchCityRope-API"
Stop-WebAppPool -Name "WitchCityRope-Web"

# Stop websites
Stop-Website -Name "WitchCityRope-API"
Stop-Website -Name "WitchCityRope-Web"
```

#### Step 2: Restore Application Files
```powershell
# Set paths
$BackupPath = "C:\Backups\WitchCityRope\backup-20240127-020000"
$DeploymentPath = "C:\inetpub\WitchCityRope"

# Rename current deployment
Rename-Item $DeploymentPath "$DeploymentPath.failed"

# Restore from backup
Copy-Item -Path $BackupPath -Destination $DeploymentPath -Recurse -Force

# Restore permissions
$acl = Get-Acl $DeploymentPath
$permission = "IIS_IUSRS","FullControl","ContainerInherit,ObjectInherit","None","Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
$acl.SetAccessRule($accessRule)
Set-Acl $DeploymentPath $acl
```

#### Step 3: Restore Database
```powershell
# Restore SQLite database
Copy-Item -Path "$BackupPath\database\witchcityrope.db" `
          -Destination "$DeploymentPath\data\witchcityrope.db" -Force
```

#### Step 4: Start Services
```powershell
# Start app pools
Start-WebAppPool -Name "WitchCityRope-API"
Start-WebAppPool -Name "WitchCityRope-Web"

# Start websites
Start-Website -Name "WitchCityRope-API"
Start-Website -Name "WitchCityRope-Web"

# Verify
Invoke-WebRequest -Uri "http://localhost:5653/health" -UseBasicParsing
```

## Database Rollback

### SQLite Database Rollback

#### Option 1: Full Database Restore
```bash
# Stop application
docker-compose down

# Backup current (failed) database
cp ./data/witchcityrope.db ./data/witchcityrope.db.failed

# Restore from backup
cp /var/backups/witchcityrope/db-backup-20240127-020000.db ./data/witchcityrope.db

# Verify integrity
sqlite3 ./data/witchcityrope.db "PRAGMA integrity_check;"

# Restart application
docker-compose up -d
```

#### Option 2: Reverse Migrations
```bash
# If using Entity Framework Core
cd /opt/witchcityrope/api

# List migrations
dotnet ef migrations list

# Revert to specific migration
dotnet ef database update PreviousMigrationName

# Or revert last migration
dotnet ef database update LastGoodMigration
```

### Data Recovery Procedures

If data corruption occurred:

1. **Immediate Actions**
   ```bash
   # Create corruption backup
   cp ./data/witchcityrope.db ./data/witchcityrope.db.corrupted
   
   # Attempt repair
   sqlite3 ./data/witchcityrope.db ".recover" | sqlite3 ./data/witchcityrope.recovered.db
   ```

2. **Partial Recovery**
   ```sql
   -- Export specific tables
   sqlite3 witchcityrope.db.corrupted ".dump Users" > users.sql
   sqlite3 witchcityrope.db.corrupted ".dump Events" > events.sql
   
   -- Import to new database
   sqlite3 witchcityrope.db.new < users.sql
   sqlite3 witchcityrope.db.new < events.sql
   ```

## Post-Rollback Verification

### Automated Health Checks
```bash
# Run health check script
./deployment/post-deployment-health-check.sh

# Check specific endpoints
curl -f http://localhost:5653/health
curl -f http://localhost:5651/
curl -f http://localhost:5653/api/events
```

### Manual Verification Checklist

1. **Core Functionality**
   - [ ] User login works
   - [ ] Event listing displays
   - [ ] Event registration functions
   - [ ] Payment processing works
   - [ ] Admin panel accessible

2. **Data Integrity**
   - [ ] User accounts intact
   - [ ] Event data correct
   - [ ] Registration records present
   - [ ] Payment history accurate

3. **Performance**
   - [ ] Page load times normal
   - [ ] API response times acceptable
   - [ ] No timeout errors
   - [ ] Database queries fast

4. **Integration**
   - [ ] Email sending works
   - [ ] PayPal integration functional
   - [ ] File uploads working
   - [ ] Search functionality operational

## Emergency Procedures

### Complete Service Failure

If rollback fails:

1. **Activate Maintenance Mode**
   ```nginx
   # Add to nginx configuration
   location / {
       return 503;
       error_page 503 @maintenance;
   }
   
   location @maintenance {
       root /var/www/maintenance;
       rewrite ^.*$ /index.html break;
   }
   ```

2. **Deploy Static Maintenance Page**
   ```html
   <!DOCTYPE html>
   <html>
   <head>
       <title>Maintenance - WitchCityRope</title>
       <style>
           body { font-family: Arial; text-align: center; padding: 50px; }
           .container { max-width: 600px; margin: 0 auto; }
       </style>
   </head>
   <body>
       <div class="container">
           <h1>Scheduled Maintenance</h1>
           <p>We're performing maintenance to improve your experience.</p>
           <p>Expected completion: <strong>[TIME]</strong></p>
           <p>For urgent matters, contact: support@witchcityrope.com</p>
       </div>
   </body>
   </html>
   ```

3. **Escalation Path**
   - Level 1: Development team lead
   - Level 2: Infrastructure team
   - Level 3: External consultants
   - Level 4: Hosting provider support

### Data Loss Prevention

If data loss is suspected:

1. **Immediate freeze**
   ```bash
   # Make database read-only
   chmod 444 ./data/witchcityrope.db
   
   # Create immediate backup
   cp ./data/witchcityrope.db ./data/witchcityrope.db.$(date +%Y%m%d-%H%M%S)
   ```

2. **Forensic copy**
   ```bash
   # Create forensic image
   dd if=./data/witchcityrope.db of=./data/forensic-copy.db bs=4096
   ```

## Rollback Scripts

### Automated Rollback Script

Create `rollback.sh`:

```bash
#!/bin/bash
set -euo pipefail

# Configuration
DEPLOYMENT_PATH="/opt/witchcityrope"
BACKUP_PATH="/var/backups/witchcityrope"
SERVICE_NAME="witchcityrope"

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${YELLOW}WitchCityRope Emergency Rollback${NC}"
echo "===================================="

# Find latest backup
LATEST_BACKUP=$(ls -t $BACKUP_PATH | grep "backup-" | head -1)

if [[ -z "$LATEST_BACKUP" ]]; then
    echo -e "${RED}No backup found!${NC}"
    exit 1
fi

echo "Latest backup: $LATEST_BACKUP"
read -p "Proceed with rollback? (yes/no): " confirm

if [[ "$confirm" != "yes" ]]; then
    echo "Rollback cancelled"
    exit 0
fi

# Stop services
echo "Stopping services..."
cd $DEPLOYMENT_PATH
docker-compose down || true

# Backup current failed deployment
echo "Backing up failed deployment..."
mv $DEPLOYMENT_PATH "$DEPLOYMENT_PATH.failed.$(date +%Y%m%d-%H%M%S)"

# Restore from backup
echo "Restoring from backup..."
cp -r "$BACKUP_PATH/$LATEST_BACKUP" $DEPLOYMENT_PATH

# Restore database
echo "Restoring database..."
cp "$BACKUP_PATH/$LATEST_BACKUP/database/"*.db "$DEPLOYMENT_PATH/data/"

# Start services
echo "Starting services..."
cd $DEPLOYMENT_PATH
docker-compose up -d

# Wait for services
echo "Waiting for services to start..."
sleep 10

# Verify
echo "Verifying deployment..."
if curl -f http://localhost:5653/health; then
    echo -e "${GREEN}Rollback completed successfully!${NC}"
else
    echo -e "${RED}Rollback verification failed!${NC}"
    exit 1
fi
```

### Quick Rollback Commands

Add to `.bashrc` or `.zshrc`:

```bash
# WitchCityRope rollback aliases
alias wcr-rollback='cd /opt/witchcityrope && ./deployment/rollback.sh'
alias wcr-status='docker-compose ps && curl -s http://localhost:5653/health | jq'
alias wcr-logs='docker-compose logs --tail=100 -f'
alias wcr-restart='docker-compose restart'
```

## Post-Mortem Process

After successful rollback:

1. **Document the Incident**
   - What triggered the rollback?
   - What was the root cause?
   - How long was the downtime?
   - What data was affected?

2. **Update Procedures**
   - Add new checks to deployment process
   - Update rollback procedures if needed
   - Create automated tests for the issue

3. **Communication**
   - Notify users of resolution
   - Update status page
   - Send internal report

4. **Prevention**
   - Add monitoring for the issue
   - Update deployment checklist
   - Schedule retrospective meeting

Remember: A successful rollback is one that restores service quickly with minimal data loss. Always prioritize data integrity and user experience over deployment schedules.