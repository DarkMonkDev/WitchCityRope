# DigitalOcean Deployment - Script Documentation
<!-- Last Updated: 2025-09-15 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Overview

This document provides detailed documentation for all setup scripts used in the DigitalOcean deployment process. Each script is designed to be idempotent and includes comprehensive error handling.

## Script Location

All setup scripts are located at:
```
/docs/functional-areas/deployment/2025-01-13-digitalocean-deployment/implementation/setup-scripts/
```

## Script Execution Order

**CRITICAL**: Scripts must be executed in the exact order specified:

1. `01-initial-droplet-setup.sh`
2. `02-install-dependencies.sh`
3. `03-database-setup.sh`
4. `04-ssl-setup.sh`
5. `05-deploy-application.sh`
6. `06-monitoring-setup.sh`
7. `07-backup-setup.sh`

## Script 01: Initial Droplet Setup

### Purpose
Configures a fresh DigitalOcean Ubuntu 24.04 droplet with basic security and user setup.

### Execution Context
- **Run as**: `root` user
- **Prerequisites**: SSH access to fresh droplet
- **Time**: 5-10 minutes
- **Network**: Requires internet access for package updates

### What It Does

#### User Management
- Creates `witchcity` user with sudo privileges
- Configures SSH key authentication for new user
- Disables root login for security
- Sets up proper file permissions

#### Security Configuration
- Configures UFW (Uncomplicated Firewall)
- Opens only necessary ports: 22 (SSH), 80 (HTTP), 443 (HTTPS)
- Disables password authentication
- Configures fail2ban for SSH protection

#### System Updates
- Updates all Ubuntu packages to latest versions
- Installs essential security updates
- Configures automatic security updates

#### Directory Structure
- Creates `/opt/witchcityrope/` directory tree
- Sets up logging directories
- Creates backup directories with proper permissions

### Required Inputs
```bash
# Script will prompt for:
SSH_PUBLIC_KEY="your public key content"
ADMIN_EMAIL="admin@example.com"
```

### Expected Outputs
```bash
# Success indicators:
✅ User 'witchcity' created successfully
✅ SSH key configured for witchcity user
✅ Firewall configured and enabled
✅ System updates completed
✅ Directory structure created
```

### Verification Commands
```bash
# After running script:
ssh witchcity@YOUR_DROPLET_IP  # Should work without password
sudo ufw status                # Should show active firewall
ls -la /opt/witchcityrope/     # Should show directory structure
```

### Common Issues
- **SSH key format**: Ensure public key is in correct OpenSSH format
- **Sudo permissions**: If sudo fails, re-run user creation portion
- **Firewall conflicts**: UFW may conflict with DigitalOcean firewall rules

### Script Location
`./setup-scripts/01-initial-droplet-setup.sh`

---

## Script 02: Install Dependencies

### Purpose
Installs all required software dependencies for running WitchCityRope application.

### Execution Context
- **Run as**: `witchcity` user
- **Prerequisites**: Script 01 completed successfully
- **Time**: 10-15 minutes
- **Network**: Requires internet for package downloads

### What It Does

#### Docker Installation
- Installs Docker CE (latest stable version)
- Installs Docker Compose V2
- Adds `witchcity` user to docker group
- Configures Docker daemon for production use
- Sets up Docker log rotation

#### Web Server Installation
- Installs Nginx web server
- Configures basic Nginx security settings
- Sets up SSL-ready virtual host templates
- Configures gzip compression
- Sets up security headers

#### System Utilities
- Installs `curl`, `wget`, `unzip`, `git`
- Installs `htop`, `iotop`, `nethogs` for monitoring
- Installs `certbot` for SSL certificate management
- Installs Redis server for caching

#### Monitoring Tools
- Installs `netdata` for real-time monitoring
- Configures log rotation for applications
- Sets up system health check scripts

### Required Inputs
```bash
# No interactive inputs required
# Script runs automatically with safe defaults
```

### Expected Outputs
```bash
# Success indicators:
✅ Docker installed and configured
✅ Docker Compose installed
✅ Nginx installed and running
✅ Redis installed and running
✅ Monitoring tools installed
✅ User added to docker group
```

### Verification Commands
```bash
# Test Docker installation:
docker --version
docker-compose --version
docker run hello-world

# Test Nginx:
sudo systemctl status nginx
curl http://localhost

# Test Redis:
redis-cli ping
```

### Common Issues
- **Docker permissions**: May need to log out/in for group membership
- **Port conflicts**: Redis default port 6379 may conflict with existing services
- **Nginx startup**: Check for configuration syntax errors

### Script Location
`./setup-scripts/02-install-dependencies.sh`

---

## Script 03: Database Setup

### Purpose
Configures connection to DigitalOcean managed PostgreSQL database and creates application databases.

### Execution Context
- **Run as**: `witchcity` user
- **Prerequisites**: Scripts 01-02 completed, DigitalOcean PostgreSQL created
- **Time**: 5-10 minutes
- **Network**: Requires connection to DigitalOcean database

### What It Does

#### Database Connection Configuration
- Tests connection to managed PostgreSQL instance
- Creates production database (`witchcityrope_prod`)
- Creates staging database (`witchcityrope_staging`)
- Configures separate database users for each environment

#### Environment Configuration
- Creates `.env.production` with database connection strings
- Creates `.env.staging` with database connection strings
- Configures connection pooling settings
- Sets up database-specific logging

#### Backup Scripts
- Creates automated database backup scripts
- Configures encryption for backup files
- Sets up retention policies (30 days)
- Schedules daily backups via cron

#### Migration Support
- Prepares database for Entity Framework migrations
- Creates migration scripts for both environments
- Sets up database schema validation

### Required Inputs
```bash
# Script will prompt for:
DATABASE_HOST="your-db-cluster.db.ondigitalocean.com"
DATABASE_PORT="25060"
DATABASE_USER="doadmin"
DATABASE_PASSWORD="your_database_password"
```

### Expected Outputs
```bash
# Success indicators:
✅ Connected to managed PostgreSQL successfully
✅ Production database 'witchcityrope_prod' created
✅ Staging database 'witchcityrope_staging' created
✅ Environment files created
✅ Backup scripts configured
✅ Database migrations ready
```

### Verification Commands
```bash
# Test database connections:
/opt/witchcityrope/test-database.sh

# Check environment files:
ls -la /opt/witchcityrope/*/.*env*

# Test backup script:
/opt/witchcityrope/backup-database-test.sh
```

### Common Issues
- **Connection failures**: Verify database credentials from DigitalOcean console
- **Network access**: Ensure droplet can reach managed database
- **Permission errors**: Check database user privileges

### Script Location
`./setup-scripts/03-database-setup.sh`

---

## Script 04: SSL Setup

### Purpose
Configures SSL certificates using Let's Encrypt and sets up HTTPS for both production and staging environments.

### Execution Context
- **Run as**: `witchcity` user with sudo
- **Prerequisites**: Scripts 01-03 completed, DNS pointing to droplet
- **Time**: 10-15 minutes
- **Network**: Requires internet access to Let's Encrypt servers

### What It Does

#### Let's Encrypt Configuration
- Installs and configures Certbot
- Obtains SSL certificates for production domain
- Obtains SSL certificates for staging domain
- Sets up automatic certificate renewal

#### Nginx HTTPS Configuration
- Configures SSL virtual hosts for both environments
- Sets up HTTP to HTTPS redirects
- Configures security headers (HSTS, CSP, etc.)
- Enables OCSP stapling for performance

#### Security Enhancements
- Configures strong SSL cipher suites
- Sets up perfect forward secrecy
- Disables vulnerable SSL/TLS versions
- Configures security headers

#### Monitoring Setup
- Creates SSL certificate monitoring scripts
- Sets up expiration alerts
- Configures health checks for HTTPS endpoints

### Required Inputs
```bash
# Script will prompt for:
PRODUCTION_DOMAIN="witchcityrope.com"
STAGING_DOMAIN="staging.witchcityrope.com"
ADMIN_EMAIL="admin@example.com"
```

### Expected Outputs
```bash
# Success indicators:
✅ SSL certificate obtained for witchcityrope.com
✅ SSL certificate obtained for staging.witchcityrope.com
✅ Nginx configured for HTTPS
✅ HTTP to HTTPS redirects active
✅ Automatic renewal configured
✅ SSL security headers configured
```

### Verification Commands
```bash
# Test SSL certificates:
/opt/witchcityrope/check-ssl.sh

# Test HTTPS connectivity:
curl -I https://witchcityrope.com
curl -I https://staging.witchcityrope.com

# Check certificate expiration:
openssl s_client -connect witchcityrope.com:443 -servername witchcityrope.com < /dev/null | openssl x509 -noout -dates
```

### Common Issues
- **DNS not propagated**: Wait 30-60 minutes after DNS changes
- **Rate limits**: Let's Encrypt has rate limits, use staging first for testing
- **Firewall blocking**: Ensure ports 80 and 443 are open

### Script Location
`./setup-scripts/04-ssl-setup.sh`

---

## Script 05: Deploy Application

### Purpose
Deploys the WitchCityRope application containers for both production and staging environments.

### Execution Context
- **Run as**: `witchcity` user
- **Prerequisites**: Scripts 01-04 completed successfully
- **Time**: 15-30 minutes (depending on image download)
- **Network**: Requires access to DigitalOcean Container Registry

### What It Does

#### Docker Compose Configuration
- Creates production docker-compose.yml
- Creates staging docker-compose.yml
- Configures environment-specific settings
- Sets up container networking

#### Application Deployment
- Builds or pulls React frontend images
- Builds or pulls .NET API images
- Deploys Redis cache containers
- Configures inter-service communication

#### Nginx Reverse Proxy
- Configures production proxy rules (port 443)
- Configures staging proxy rules (port 8443)
- Sets up static file serving
- Configures API routing

#### Health Checks
- Implements container health checks
- Sets up application monitoring endpoints
- Configures service dependency handling
- Creates restart policies

### Required Inputs
```bash
# Script will prompt for:
REGISTRY_NAME="witchcityrope"
API_IMAGE_TAG="latest"
WEB_IMAGE_TAG="latest"
```

### Expected Outputs
```bash
# Success indicators:
✅ Production environment deployed
✅ Staging environment deployed
✅ All containers healthy
✅ Nginx proxy configured
✅ Health checks passing
✅ Inter-service communication working
```

### Verification Commands
```bash
# Check deployment status:
/opt/witchcityrope/status.sh

# Test application health:
curl https://witchcityrope.com/api/health
curl https://staging.witchcityrope.com/api/health

# Check container logs:
/opt/witchcityrope/logs.sh production api
/opt/witchcityrope/logs.sh staging web
```

### Common Issues
- **Image pull failures**: Verify DigitalOcean Container Registry access
- **Container startup failures**: Check environment variables and logs
- **Proxy configuration**: Verify Nginx configuration syntax

### Script Location
`./setup-scripts/05-deploy-application.sh`

---

## Script 06: Monitoring Setup

### Purpose
Sets up comprehensive monitoring, alerting, and observability for the deployed application.

### Execution Context
- **Run as**: `witchcity` user
- **Prerequisites**: Scripts 01-05 completed successfully
- **Time**: 10-15 minutes
- **Network**: May require internet for monitoring tool installation

### What It Does

#### System Monitoring
- Configures Netdata for real-time system metrics
- Sets up CPU, memory, disk, and network monitoring
- Configures performance baselines and thresholds
- Creates monitoring dashboards

#### Application Monitoring
- Sets up health check endpoints monitoring
- Configures API response time tracking
- Monitors container resource usage
- Tracks application-specific metrics

#### Log Management
- Configures centralized logging
- Sets up log rotation and retention
- Creates log analysis scripts
- Implements structured logging

#### Alerting System
- Configures email alerts for critical issues
- Sets up Slack notifications (optional)
- Creates escalation procedures
- Implements alert throttling

### Required Inputs
```bash
# Script will prompt for:
ADMIN_EMAIL="admin@example.com"
SLACK_WEBHOOK_URL="https://hooks.slack.com/..." # Optional
ALERT_THRESHOLDS="cpu:80,memory:90,disk:85"
```

### Expected Outputs
```bash
# Success indicators:
✅ Netdata monitoring active
✅ Application health checks configured
✅ Log management setup complete
✅ Alert system configured
✅ Monitoring dashboards accessible
✅ Performance baselines established
```

### Verification Commands
```bash
# Check monitoring status:
/opt/witchcityrope/monitoring/dashboard.sh

# Test alerts:
/opt/witchcityrope/monitoring/test-alerts.sh

# View logs:
/opt/witchcityrope/logs.sh production all --tail 50
```

### Common Issues
- **Netdata not accessible**: Check firewall rules, verify installation
- **Alert delivery failures**: Verify email configuration, test SMTP
- **High resource usage**: Monitoring tools consuming too many resources

### Script Location
`./setup-scripts/06-monitoring-setup.sh`

---

## Script 07: Backup Setup

### Purpose
Configures comprehensive backup systems for databases, application data, and system configurations.

### Execution Context
- **Run as**: `witchcity` user with sudo
- **Prerequisites**: Scripts 01-06 completed successfully
- **Time**: 10-20 minutes
- **Network**: May require DigitalOcean Spaces configuration

### What It Does

#### Database Backup
- Creates automated PostgreSQL backup scripts
- Configures encrypted backup storage
- Sets up backup rotation (30-day retention)
- Implements backup verification procedures

#### System Configuration Backup
- Backs up Nginx configurations
- Backs up SSL certificates and keys
- Backs up Docker Compose configurations
- Backs up environment files (encrypted)

#### Application Data Backup
- Backs up Docker volumes
- Backs up uploaded files and media
- Backs up log files (compressed)
- Backs up monitoring data

#### Remote Backup Storage
- Configures DigitalOcean Spaces integration (optional)
- Sets up encrypted remote storage
- Implements backup synchronization
- Creates disaster recovery procedures

### Required Inputs
```bash
# Script will prompt for:
ENABLE_REMOTE_BACKUP="yes/no"
SPACES_ACCESS_KEY="your_spaces_key"      # If remote backup enabled
SPACES_SECRET_KEY="your_spaces_secret"   # If remote backup enabled
BACKUP_ENCRYPTION_KEY="strong_password"
```

### Expected Outputs
```bash
# Success indicators:
✅ Database backup scripts created
✅ System configuration backup configured
✅ Application data backup setup
✅ Remote backup configured (if enabled)
✅ Backup scheduling active
✅ Restore procedures documented
```

### Verification Commands
```bash
# Test database backup:
/opt/witchcityrope/backup-full-database.sh

# Test system backup:
/opt/witchcityrope/backup-system-config.sh

# List available backups:
/opt/witchcityrope/backup-manage.sh list

# Test restore (non-destructive):
/opt/witchcityrope/backup-manage.sh verify latest
```

### Common Issues
- **Disk space**: Ensure sufficient space for backup storage
- **Permissions**: Backup scripts need proper file permissions
- **Encryption**: Strong encryption keys required for security

### Script Location
`./setup-scripts/07-backup-setup.sh`

---

## Additional Configuration Files

### Docker Compose Files

#### production configuration
**File**: `docker-compose.production.yml`
**Purpose**: Production environment container orchestration

**Key Features**:
- Production-optimized resource limits
- Health checks for all services
- Restart policies for high availability
- Security-focused networking

**Services Defined**:
- `witchcity-web-prod`: React frontend (port 3001)
- `witchcity-api-prod`: .NET API (port 5001)
- `witchcity-redis-prod`: Redis cache (port 6379)

#### Staging Configuration
**File**: `docker-compose.staging.yml`
**Purpose**: Staging environment container orchestration

**Key Features**:
- Resource-constrained for cost optimization
- Separate port ranges (3002, 5002)
- Shared database with production
- Simplified health checks

### Management Scripts

#### Status Script
**File**: `/opt/witchcityrope/status.sh`
**Purpose**: Check status of all services across environments

**Usage**:
```bash
/opt/witchcityrope/status.sh              # All environments
/opt/witchcityrope/status.sh production   # Production only
/opt/witchcityrope/status.sh staging      # Staging only
```

#### Logs Script
**File**: `/opt/witchcityrope/logs.sh`
**Purpose**: View logs from any service in any environment

**Usage**:
```bash
/opt/witchcityrope/logs.sh production api     # Production API logs
/opt/witchcityrope/logs.sh staging web --tail 50  # Last 50 staging web logs
/opt/witchcityrope/logs.sh production all     # All production logs
```

#### Restart Script
**File**: `/opt/witchcityrope/restart.sh`
**Purpose**: Restart services with zero-downtime procedures

**Usage**:
```bash
/opt/witchcityrope/restart.sh production api  # Restart production API
/opt/witchcityrope/restart.sh staging all     # Restart all staging services
/opt/witchcityrope/restart.sh all             # Restart everything
```

### Health Check Scripts

#### Comprehensive Health Check
**File**: `/opt/witchcityrope/health-check.sh`
**Purpose**: Complete system health validation

**What It Checks**:
- Container health status
- Database connectivity
- SSL certificate validity
- Disk space and system resources
- Network connectivity
- Application endpoint responses

**Usage**:
```bash
/opt/witchcityrope/health-check.sh                # Standard check
/opt/witchcityrope/health-check.sh --comprehensive # Detailed check
/opt/witchcityrope/health-check.sh --emergency     # Fast critical check
```

## Script Execution Best Practices

### Pre-Execution Checklist
- [ ] Verify all prerequisites are met
- [ ] Take system snapshot (if possible)
- [ ] Ensure stable internet connection
- [ ] Have all required information gathered
- [ ] Test SSH connectivity

### During Execution
- [ ] Monitor script output carefully
- [ ] Don't interrupt scripts mid-execution
- [ ] Save output logs for troubleshooting
- [ ] Verify each step completes successfully
- [ ] Run verification commands after each script

### Post-Execution
- [ ] Run comprehensive health check
- [ ] Document any issues encountered
- [ ] Update team on completion status
- [ ] Schedule follow-up monitoring
- [ ] Create backup of current state

## Troubleshooting Common Script Issues

### Script Permission Errors
```bash
# Fix script permissions:
chmod +x /tmp/setup-scripts/*.sh

# Fix directory permissions:
sudo chown -R witchcity:witchcity /opt/witchcityrope/
```

### Docker Permission Issues
```bash
# Add user to docker group:
sudo usermod -aG docker witchcity

# Log out and back in, then test:
docker run hello-world
```

### Network Connectivity Issues
```bash
# Test DNS resolution:
nslookup witchcityrope.com

# Test DigitalOcean API access:
curl -X GET "https://api.digitalocean.com/v2/account" \
  -H "Authorization: Bearer $DIGITALOCEAN_ACCESS_TOKEN"
```

### Database Connection Issues
```bash
# Test database connectivity:
psql -h your-db-host -p 25060 -U doadmin -d postgres

# Check database logs:
docker logs witchcity-postgres-prod
```

## Script Maintenance

### Regular Updates
- Review scripts quarterly for security updates
- Update Docker images to latest stable versions
- Review and update SSL certificate procedures
- Update monitoring and backup retention policies

### Documentation Updates
- Keep script documentation synchronized with code changes
- Update troubleshooting procedures based on real experiences
- Document new features or configuration changes
- Maintain version history for all scripts

---

**Script Package Version**: 1.0
**Last Tested**: September 2025
**Compatibility**: Ubuntu 24.04 LTS, Docker 24.x, PostgreSQL 16
**Support**: See [TROUBLESHOOTING-GUIDE.md](./TROUBLESHOOTING-GUIDE.md) for issue resolution

*All scripts have been tested in production environments and include comprehensive error handling and rollback procedures.*