# WitchCityRope Environment Setup Checklist

This checklist ensures all prerequisites and configurations are in place before deployment.

## Infrastructure Requirements

### Minimum Server Requirements
- [ ] **CPU**: 2+ cores (4+ recommended for production)
- [ ] **RAM**: 4GB minimum (8GB+ recommended for production)
- [ ] **Storage**: 20GB minimum (50GB+ recommended with backups)
- [ ] **OS**: Ubuntu 20.04+ / Windows Server 2019+ / Debian 11+
- [ ] **Network**: Static IP address
- [ ] **Firewall**: Ports 80, 443 open (additional ports as needed)

### Domain and DNS
- [ ] Domain name registered
- [ ] DNS A records configured for domain
- [ ] DNS A records configured for www subdomain
- [ ] DNS records propagated (check with `nslookup`)
- [ ] SSL certificate strategy decided (Let's Encrypt recommended)

## Software Prerequisites

### For Linux Deployment
- [ ] Docker Engine installed (20.10+)
- [ ] Docker Compose installed (v2.0+)
- [ ] Git installed
- [ ] Nginx installed (if not using Docker)
- [ ] Certbot installed (for SSL)
- [ ] jq installed (for JSON parsing)
- [ ] curl/wget installed
- [ ] System user created for application

### For Windows Deployment
- [ ] .NET 8.0 Runtime installed
- [ ] IIS installed with required modules:
  - [ ] ASP.NET Core Module
  - [ ] URL Rewrite Module
  - [ ] WebSocket Protocol
- [ ] Git installed
- [ ] PowerShell 5.1+
- [ ] Windows Firewall configured

### For Docker Deployment
- [ ] Docker daemon running
- [ ] Docker Compose available
- [ ] Docker Hub access (or private registry)
- [ ] Sufficient disk space for images

## Application Configuration

### Environment Variables
Create `.env` file with the following configured:

```bash
# Environment
ENVIRONMENT=production  # or staging

# Security
JWT_SECRET=<generate-secure-key-min-32-chars>
ENCRYPTION_KEY=<generate-32-char-key>

# Database
DATABASE_CONNECTION=Data Source=/app/data/witchcityrope.db

# Email (SendGrid)
SENDGRID_API_KEY=<your-sendgrid-api-key>
EMAIL_FROM=noreply@yourdomain.com

# Payment Processing
PAYPAL_CLIENT_ID=<your-paypal-client-id>
PAYPAL_CLIENT_SECRET=<your-paypal-client-secret>


# Redis (if using)
REDIS_PASSWORD=<generate-secure-password>

# Monitoring
GRAFANA_PASSWORD=<secure-admin-password>

# Domain
DOMAIN=witchcityrope.com
SSL_EMAIL=admin@witchcityrope.com
```

### API Keys and External Services
- [ ] SendGrid account created and API key generated
- [ ] PayPal account configured (sandbox for staging, live for production)
- [ ] Google OAuth credentials configured (if using)
- [ ] Monitoring services configured (optional)

### Security Configurations
- [ ] Strong passwords generated for all services
- [ ] JWT secret key generated (min 32 characters)
- [ ] Encryption key generated (32 characters)
- [ ] Database encryption configured
- [ ] SSL certificates ready or auto-provisioning configured
- [ ] Firewall rules configured
- [ ] Rate limiting configured
- [ ] CORS settings reviewed

## Pre-Deployment Checklist

### Code and Repository
- [ ] Latest code pulled from repository
- [ ] Correct branch checked out (main/production/staging)
- [ ] All migrations created and tested
- [ ] Configuration files reviewed
- [ ] Secrets not committed to repository

### Database
- [ ] Database backup strategy defined
- [ ] Initial database created
- [ ] Migrations tested in staging
- [ ] Backup of current database (if upgrading)
- [ ] Database permissions configured

### File System
- [ ] Required directories created:
  - [ ] `/app/data` (or configured data path)
  - [ ] `/app/logs` (or configured log path)
  - [ ] `/app/uploads` (or configured upload path)
- [ ] Correct permissions set on directories
- [ ] Sufficient disk space available

### Networking
- [ ] Load balancer configured (if applicable)
- [ ] Reverse proxy configured
- [ ] SSL termination configured
- [ ] Health check endpoints accessible
- [ ] WebSocket support configured

### Monitoring and Logging
- [ ] Log rotation configured
- [ ] Log aggregation setup (optional)
- [ ] Application monitoring configured (optional)
- [ ] Alerting rules defined (optional)
- [ ] Backup monitoring configured

## Deployment Configuration Files

Ensure these files are present and configured:

### Required Files
- [ ] `deployment-config.json` - Main deployment configuration
- [ ] `.env` - Environment variables
- [ ] `docker-compose.yml` - Docker composition (if using Docker)
- [ ] `nginx.conf` - Nginx configuration (if using Nginx)

### Windows Specific
- [ ] `web.config` - IIS configuration
- [ ] App pool configurations
- [ ] SSL bindings in IIS

### Linux Specific
- [ ] Systemd service files (optional)
- [ ] Logrotate configuration
- [ ] Cron jobs for backups

## Security Pre-Flight

### Application Security
- [ ] Admin credentials changed from defaults
- [ ] Two-factor authentication enabled for admins
- [ ] Session timeout configured
- [ ] HTTPS enforced
- [ ] Security headers configured

### Infrastructure Security
- [ ] SSH key authentication only (Linux)
- [ ] RDP restricted to specific IPs (Windows)
- [ ] Unnecessary ports closed
- [ ] Fail2ban or similar configured (Linux)
- [ ] Regular security updates scheduled

## Final Checks

### Testing
- [ ] Deployment tested in staging environment
- [ ] Rollback procedure tested
- [ ] Backup restoration tested
- [ ] Health checks verified
- [ ] Performance baseline established

### Documentation
- [ ] Deployment procedures documented
- [ ] Rollback procedures documented
- [ ] Emergency contacts listed
- [ ] Known issues documented
- [ ] Post-deployment verification steps listed

### Team Readiness
- [ ] Deployment team identified
- [ ] Communication channels established
- [ ] Deployment window scheduled
- [ ] Stakeholders notified
- [ ] Support team on standby

## Post-Deployment Verification

After deployment, verify:

- [ ] Application accessible via HTTPS
- [ ] All health endpoints responding
- [ ] Authentication working
- [ ] Database connections successful
- [ ] Email sending functional
- [ ] Payment processing operational
- [ ] File uploads working
- [ ] Logs being generated
- [ ] Monitoring dashboards active
- [ ] SSL certificate valid
- [ ] Performance metrics acceptable

## Emergency Contacts

| Role | Name | Contact | Notes |
|------|------|---------|-------|
| Lead Developer | | | |
| DevOps Engineer | | | |
| Database Admin | | | |
| Security Lead | | | |
| Project Manager | | | |

## Notes

- Always perform deployments during scheduled maintenance windows
- Keep this checklist updated with lessons learned
- Document any deviations from standard procedure
- Ensure all team members have access to necessary credentials
- Test the rollback procedure before starting deployment