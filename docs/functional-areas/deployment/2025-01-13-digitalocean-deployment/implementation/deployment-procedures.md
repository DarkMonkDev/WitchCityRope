# WitchCityRope DigitalOcean Deployment Procedures
<!-- Last Updated: 2025-09-13 -->
<!-- Version: 1.0 -->
<!-- Owner: Backend Developer -->
<!-- Status: Implementation Ready -->

## Overview

This document provides step-by-step procedures for deploying WitchCityRope to DigitalOcean within a $100/month budget constraint. The deployment uses a cost-optimized architecture with shared resources between production and staging environments.

## Prerequisites

### Required Accounts and Access

1. **DigitalOcean Account** with billing configured
2. **GitHub Account** with repository access
3. **Domain Registration** (e.g., witchcityrope.com and staging.witchcityrope.com)
4. **SSH Key Pair** for server access

### Required Information

- [ ] DigitalOcean API token
- [ ] GitHub repository URL
- [ ] Domain names (production and staging)
- [ ] Email address for SSL certificates and notifications
- [ ] SSH public key content

### Budget Verification

Ensure the following monthly costs fit within $92 budget:
- Production Droplet (8GB RAM): $56/month
- Shared PostgreSQL (2GB): $30/month
- Container Registry: $5/month
- Total: $91/month (under $100 target)

## Phase 1: Infrastructure Setup

### Step 1.1: Create DigitalOcean Resources

1. **Create Production Droplet**
   ```bash
   # Via DigitalOcean CLI (optional)
   doctl compute droplet create witchcityrope-prod \
     --size s-4vcpu-8gb \
     --image ubuntu-24-04-x64 \
     --region nyc3 \
     --ssh-keys YOUR_SSH_KEY_ID
   ```

   **Via Web Console:**
   - Navigate to DigitalOcean Console
   - Create Droplet → Ubuntu 24.04 LTS
   - Size: 8GB RAM, 4 vCPUs, 160GB SSD ($56/month)
   - Region: NYC3 (or closest to your users)
   - SSH Keys: Add your public key
   - Name: `witchcityrope-prod`

2. **Create Managed PostgreSQL Database**
   - Navigate to Databases → Create Database
   - Engine: PostgreSQL 16
   - Size: 2GB RAM, 1 vCPU ($30/month)
   - Region: Same as droplet
   - Name: `witchcityrope-db`

3. **Create Container Registry**
   - Navigate to Container Registry → Create
   - Name: `witchcityrope`
   - Plan: Basic ($5/month)

### Step 1.2: Configure DNS

1. **Point Domains to Droplet**
   ```
   A Record: witchcityrope.com → [DROPLET_IP]
   A Record: staging.witchcityrope.com → [DROPLET_IP]
   ```

2. **Verify DNS Resolution**
   ```bash
   nslookup witchcityrope.com
   nslookup staging.witchcityrope.com
   ```

### Step 1.3: Initial Server Setup

1. **Connect to Server**
   ```bash
   ssh root@YOUR_DROPLET_IP
   ```

2. **Upload Setup Scripts**
   ```bash
   # On your local machine
   scp -r setup-scripts/ root@YOUR_DROPLET_IP:/tmp/
   ```

3. **Run Initial Setup**
   ```bash
   # On the server
   cd /tmp/setup-scripts
   chmod +x *.sh

   # Upload your SSH public key first
   # Then run initial setup
   ./01-initial-droplet-setup.sh
   ```

4. **Switch to Deploy User**
   ```bash
   # Log out and back in as witchcity user
   ssh witchcity@YOUR_DROPLET_IP
   ```

## Phase 2: System Dependencies

### Step 2.1: Install Docker and Dependencies

```bash
cd /tmp/setup-scripts
./02-install-dependencies.sh
```

**Expected Results:**
- Docker and Docker Compose installed
- Nginx installed and configured
- Redis installed
- Monitoring tools (htop, etc.) installed
- User added to docker group

### Step 2.2: Verify Installation

```bash
# Test Docker (may need to log out/in first)
docker --version
docker-compose --version

# Test Nginx
sudo systemctl status nginx

# Test basic health check
/opt/witchcityrope/health-check.sh
```

## Phase 3: Database Configuration

### Step 3.1: Configure Database Connection

```bash
./03-database-setup.sh
```

**Prompts will ask for:**
- Database host (from DigitalOcean console)
- Database port (usually 25060)
- Database user (usually doadmin)
- Database password (from DigitalOcean console)

**This script will:**
- Create production and staging databases
- Generate environment configuration files
- Set up backup scripts
- Configure automated daily backups

### Step 3.2: Verify Database Setup

```bash
# Test database connections
/opt/witchcityrope/test-database.sh

# Check environment files
ls -la /opt/witchcityrope/*/.*env*
```

## Phase 4: SSL Configuration

### Step 4.1: Configure HTTPS

```bash
./04-ssl-setup.sh
```

**Prompts will ask for:**
- Production domain (e.g., witchcityrope.com)
- Staging domain (e.g., staging.witchcityrope.com)
- Email for Let's Encrypt notifications

**This script will:**
- Create Nginx configurations for both environments
- Obtain SSL certificates via Let's Encrypt
- Configure automatic certificate renewal
- Update firewall rules

### Step 4.2: Verify SSL Setup

```bash
# Check SSL certificates
/opt/witchcityrope/check-ssl.sh

# Test HTTPS connectivity
curl -I https://witchcityrope.com
curl -I https://staging.witchcityrope.com
```

## Phase 5: Application Deployment

### Step 5.1: Deploy Application Containers

```bash
./05-deploy-application.sh
```

**This script will:**
- Create Docker Compose configurations
- Build or pull Docker images
- Deploy production and staging environments
- Create management scripts
- Run health checks

### Step 5.2: Verify Application Deployment

```bash
# Check application status
/opt/witchcityrope/status.sh

# Test API endpoints
curl https://witchcityrope.com/api/health
curl https://staging.witchcityrope.com/api/health

# View container logs
/opt/witchcityrope/logs.sh production api
```

## Phase 6: Monitoring Setup

### Step 6.1: Configure Monitoring

```bash
./06-monitoring-setup.sh
```

**Prompts will ask for:**
- Email for alerts (optional)
- Slack webhook URL (optional)

**This script will:**
- Install Netdata monitoring
- Set up system monitoring scripts
- Configure log analysis
- Create monitoring dashboard
- Set up automated reports

### Step 6.2: Verify Monitoring

```bash
# View monitoring dashboard
/opt/witchcityrope/monitoring/dashboard.sh

# Check Netdata (local access only)
curl http://localhost:19999

# View monitoring logs
tail -f /var/log/witchcityrope/monitoring/system-monitor.log
```

## Phase 7: Backup Configuration

### Step 7.1: Setup Backup System

```bash
./07-backup-setup.sh
```

**Prompts will ask for:**
- Enable remote backup to DigitalOcean Spaces? (optional)
- Spaces configuration if enabled

**This script will:**
- Create comprehensive backup scripts
- Set up automated backup schedules
- Configure backup retention policies
- Create restore procedures
- Test backup system

### Step 7.2: Verify Backup System

```bash
# Check backup status
/opt/witchcityrope/backup-manage.sh status

# Run test backup
/opt/witchcityrope/backup-full-database.sh

# List available backups
/opt/witchcityrope/backup-manage.sh list
```

## Phase 8: CI/CD Pipeline Setup

### Step 8.1: Configure GitHub Secrets

In your GitHub repository, add these secrets:

```
DIGITALOCEAN_ACCESS_TOKEN=your_do_token
PRODUCTION_SERVER_HOST=your_droplet_ip
PRODUCTION_SERVER_USER=witchcity
PRODUCTION_SERVER_SSH_KEY=your_private_key
STAGING_SERVER_HOST=your_droplet_ip
STAGING_SERVER_USER=witchcity
STAGING_SERVER_SSH_KEY=your_private_key
SLACK_WEBHOOK_URL=your_slack_webhook (optional)
```

### Step 8.2: Deploy GitHub Actions Workflow

```bash
# Copy the GitHub Actions workflow to your repository
cp /tmp/setup-scripts/github-actions-pipeline.yml .github/workflows/
git add .github/workflows/github-actions-pipeline.yml
git commit -m "Add CI/CD pipeline"
git push
```

## Phase 9: Testing and Validation

### Step 9.1: Complete System Test

```bash
# Run comprehensive health check
/opt/witchcityrope/health-check.sh

# Test application functionality
curl -X POST https://witchcityrope.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@witchcityrope.com","password":"Test123!"}'

# Test file uploads, user registration, etc.
```

### Step 9.2: Performance Validation

```bash
# Check resource usage
/opt/witchcityrope/monitoring/dashboard.sh

# Monitor for 24 hours to ensure stability
# Check metrics in Netdata dashboard
```

### Step 9.3: Backup and Recovery Test

```bash
# Test database backup and restore
/opt/witchcityrope/backup-full-database.sh
/opt/witchcityrope/restore-backup.sh database production_YYYYMMDD_HHMMSS.sql.gz.enc --environment staging
```

## Post-Deployment Checklist

### Security Validation
- [ ] SSH password authentication disabled
- [ ] Root login disabled
- [ ] Firewall properly configured
- [ ] SSL certificates valid and auto-renewing
- [ ] Database connections encrypted
- [ ] Application security headers configured

### Performance Validation
- [ ] Page load times under 2 seconds
- [ ] API response times under 500ms
- [ ] Memory usage under 85%
- [ ] CPU usage reasonable under load
- [ ] Database query performance optimized

### Monitoring Validation
- [ ] System monitoring active
- [ ] Application health checks working
- [ ] Log aggregation functioning
- [ ] Alerts configured and tested
- [ ] Backup system operational

### Operational Readiness
- [ ] Documentation updated
- [ ] Team access configured
- [ ] Emergency procedures documented
- [ ] Monitoring dashboards accessible
- [ ] Backup and recovery procedures tested

## Troubleshooting Guide

### Common Issues

#### 1. Container Startup Failures

**Symptoms**: Containers fail to start or become unhealthy
**Investigation**:
```bash
docker ps -a
docker logs witchcity-api-prod
docker logs witchcity-web-prod
```

**Common Causes**:
- Database connection issues
- Environment variable errors
- Resource constraints
- Port conflicts

**Solutions**:
- Check database connectivity
- Verify environment files
- Increase resource limits
- Check port mappings

#### 2. SSL Certificate Issues

**Symptoms**: HTTPS not working, certificate errors
**Investigation**:
```bash
/opt/witchcityrope/check-ssl.sh
sudo certbot certificates
nginx -t
```

**Common Causes**:
- DNS not pointing to server
- Firewall blocking port 80
- Domain validation failures

**Solutions**:
- Verify DNS records
- Check firewall rules
- Re-run SSL setup script

#### 3. Database Connection Problems

**Symptoms**: API returns 500 errors, connection timeouts
**Investigation**:
```bash
/opt/witchcityrope/test-database.sh
docker logs witchcity-api-prod | grep -i database
```

**Common Causes**:
- Incorrect connection strings
- Database server unreachable
- Authentication failures

**Solutions**:
- Verify database credentials
- Check DigitalOcean database status
- Update connection strings

#### 4. Performance Issues

**Symptoms**: Slow response times, high resource usage
**Investigation**:
```bash
/opt/witchcityrope/monitoring/dashboard.sh
docker stats
htop
```

**Common Causes**:
- Insufficient resources
- Database query performance
- Memory leaks

**Solutions**:
- Scale droplet resources
- Optimize database queries
- Restart containers
- Review application logs

### Emergency Procedures

#### Application Rollback

```bash
# Stop current containers
/opt/witchcityrope/restart.sh all

# Deploy previous version
cd /opt/witchcityrope/production
API_IMAGE=previous_api_tag WEB_IMAGE=previous_web_tag docker-compose up -d

# Verify rollback
/opt/witchcityrope/status.sh
```

#### Database Restore

```bash
# Stop application first
docker stop witchcity-api-prod witchcity-api-staging

# Restore from backup
/opt/witchcityrope/restore-backup.sh database backup_file.sql.gz.enc --environment production

# Restart applications
/opt/witchcityrope/restart.sh all
```

#### Full System Recovery

```bash
# From backup server or local backup
scp backup_files/ witchcity@server:/opt/backups/witchcityrope/

# Restore system configuration
/opt/witchcityrope/restore-backup.sh system system_config_YYYYMMDD.tar.gz

# Restore application
/opt/witchcityrope/restore-backup.sh application docker_volumes_YYYYMMDD.tar.gz

# Restore database
/opt/witchcityrope/restore-backup.sh database production_YYYYMMDD.sql.gz.enc --environment production
```

## Maintenance Procedures

### Weekly Maintenance

```bash
# Check system status
/opt/witchcityrope/health-check.sh

# Review monitoring reports
/opt/witchcityrope/monitoring-report.sh

# Update packages
sudo apt update && sudo apt upgrade

# Clean up old backups
/opt/witchcityrope/backup-manage.sh cleanup --older-than 30
```

### Monthly Maintenance

```bash
# Review logs for issues
/opt/witchcityrope/monitoring/analyze-logs.sh

# Check SSL certificate expiration
/opt/witchcityrope/check-ssl.sh

# Review resource usage trends
/opt/witchcityrope/monitoring/dashboard.sh

# Test backup and restore procedures
```

### Scaling Procedures

#### Vertical Scaling (Increase Resources)

1. **Resize Droplet**
   - Power down droplet in DigitalOcean console
   - Resize to larger plan
   - Power on and verify functionality

2. **Scale Database**
   - Resize managed database in console
   - Update connection pool settings if needed

#### Horizontal Scaling (Future)

When approaching resource limits:

1. **Add Dedicated Staging Droplet**
2. **Implement Load Balancing**
3. **Consider Database Read Replicas**
4. **Optimize Caching Strategy**

## Success Metrics

### Performance Targets
- **Page Load Time**: < 2 seconds
- **API Response Time**: < 500ms average
- **Uptime**: > 99.9%
- **Database Query Time**: < 100ms average

### Cost Targets
- **Monthly Cost**: < $100 (current: $92)
- **Cost per Active User**: < $0.15
- **Cost Efficiency**: 60% savings vs managed platform

### Operational Targets
- **Recovery Time Objective (RTO)**: < 30 minutes
- **Recovery Point Objective (RPO)**: < 6 hours
- **Backup Success Rate**: > 99%
- **Alert Response Time**: < 15 minutes

## Conclusion

This deployment provides a production-ready, cost-effective hosting solution for WitchCityRope that:

1. **Meets Budget Requirements**: $92/month vs $100 target
2. **Provides Scalability**: Clear path to grow with community
3. **Ensures Reliability**: Comprehensive backup and monitoring
4. **Maintains Security**: SSL, firewalls, and security headers
5. **Enables CI/CD**: Automated testing and deployment

The architecture balances cost optimization with reliability and provides a sustainable foundation for the WitchCityRope community platform.