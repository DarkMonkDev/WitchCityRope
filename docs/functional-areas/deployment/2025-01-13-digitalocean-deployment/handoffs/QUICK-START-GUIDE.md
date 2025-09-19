# DigitalOcean Deployment - Quick Start Guide
<!-- Last Updated: 2025-09-15 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Overview

This guide provides a streamlined deployment process for getting WitchCityRope running on DigitalOcean in 5 days. Follow these steps in order for successful implementation.

## Prerequisites Verification

### Before You Begin - CRITICAL CHECKLIST ✅

- [ ] **DigitalOcean Account**: Created with billing method configured
- [ ] **GitHub Access**: Can clone https://github.com/DarkMonkDev/WitchCityRope.git
- [ ] **Domain Names**: witchcityrope.com and staging.witchcityrope.com purchased
- [ ] **SSH Keys**: Generated and public key available
- [ ] **Time Commitment**: 5 uninterrupted days for implementation
- [ ] **Budget Confirmed**: $92/month ongoing hosting costs approved

### Information Gathering - REQUIRED VALUES 📝

Collect these values before starting:

```bash
# DigitalOcean Information
DIGITALOCEAN_API_TOKEN="your_api_token_here"
DROPLET_IP="will_be_assigned_during_setup"

# Domain Information
PRODUCTION_DOMAIN="witchcityrope.com"
STAGING_DOMAIN="staging.witchcityrope.com"

# Database Information (from DigitalOcean console)
DATABASE_HOST="your_database_host"
DATABASE_PORT="25060"
DATABASE_USER="doadmin"
DATABASE_PASSWORD="your_database_password"

# Notification Information
ADMIN_EMAIL="your_admin_email@example.com"
SLACK_WEBHOOK_URL="https://hooks.slack.com/..." # Optional but recommended
```

### Setup Verification

Before proceeding, verify you have the setup scripts:

```bash
# Clone the repository
git clone https://github.com/DarkMonkDev/WitchCityRope.git
cd WitchCityRope

# Navigate to setup scripts
cd docs/functional-areas/deployment/2025-01-13-digitalocean-deployment/implementation/setup-scripts/

# Verify all scripts are present
ls -la *.sh
# Should see: 01-initial-droplet-setup.sh through 07-backup-setup.sh
```

## 5-Day Implementation Timeline

### Day 1: Infrastructure Setup 🏗️

**Goal**: Create and configure DigitalOcean resources
**Time Estimate**: 4-6 hours
**Key Deliverable**: Running Ubuntu server with Docker installed

#### Morning (2-3 hours)
1. **Create DigitalOcean Resources**
   ```bash
   # Create Production Droplet via Web Console:
   # - Ubuntu 24.04 LTS
   # - 8GB RAM, 4 vCPUs, 160GB SSD ($56/month)
   # - Region: NYC3 (or closest to users)
   # - SSH Key: Upload your public key
   # - Name: witchcityrope-prod

   # Create Managed PostgreSQL Database:
   # - Engine: PostgreSQL 16
   # - Size: 2GB RAM, 1 vCPU ($30/month)
   # - Region: Same as droplet
   # - Name: witchcityrope-db

   # Create Container Registry:
   # - Name: witchcityrope
   # - Plan: Basic ($5/month)
   ```

2. **Configure DNS**
   ```bash
   # Point both domains to your droplet IP
   # A Record: witchcityrope.com → [DROPLET_IP]
   # A Record: staging.witchcityrope.com → [DROPLET_IP]

   # Verify DNS propagation (may take 30 minutes)
   nslookup witchcityrope.com
   nslookup staging.witchcityrope.com
   ```

#### Afternoon (2-3 hours)
3. **Initial Server Setup**
   ```bash
   # Upload setup scripts to server
   scp -r setup-scripts/ root@YOUR_DROPLET_IP:/tmp/

   # Connect and run initial setup
   ssh root@YOUR_DROPLET_IP
   cd /tmp/setup-scripts
   chmod +x *.sh
   ./01-initial-droplet-setup.sh

   # Follow prompts to create witchcity user
   # Log out and back in as witchcity user
   exit
   ssh witchcity@YOUR_DROPLET_IP
   ```

4. **Install Dependencies**
   ```bash
   cd /tmp/setup-scripts
   ./02-install-dependencies.sh

   # Verify installation
   docker --version
   docker-compose --version
   sudo systemctl status nginx
   ```

**Day 1 Success Criteria**:
- [ ] Can SSH into server as witchcity user
- [ ] Docker and Docker Compose working
- [ ] Nginx installed and running
- [ ] Firewall configured correctly

### Day 2: Application Deployment 🚀

**Goal**: Deploy WitchCityRope application containers
**Time Estimate**: 6-8 hours
**Key Deliverable**: Working application on both production and staging

#### Morning (3-4 hours)
1. **Database Configuration**
   ```bash
   ./03-database-setup.sh

   # Enter database credentials when prompted:
   # Host: [from DigitalOcean console]
   # Port: 25060
   # User: doadmin
   # Password: [from DigitalOcean console]

   # Test database connection
   /opt/witchcityrope/test-database.sh
   ```

2. **SSL Configuration**
   ```bash
   ./04-ssl-setup.sh

   # Enter when prompted:
   # Production domain: witchcityrope.com
   # Staging domain: staging.witchcityrope.com
   # Email: your_admin_email@example.com

   # Verify SSL certificates
   /opt/witchcityrope/check-ssl.sh
   ```

#### Afternoon (3-4 hours)
3. **Application Deployment**
   ```bash
   ./05-deploy-application.sh

   # This will:
   # - Create Docker Compose configurations
   # - Build/pull application images
   # - Deploy both environments
   # - Run health checks

   # Verify deployment
   /opt/witchcityrope/status.sh
   curl https://witchcityrope.com/api/health
   curl https://staging.witchcityrope.com/api/health
   ```

4. **Initial Testing**
   ```bash
   # Test basic functionality
   curl -X POST https://witchcityrope.com/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"admin@witchcityrope.com","password":"Test123!"}'

   # Check container logs if needed
   /opt/witchcityrope/logs.sh production api
   /opt/witchcityrope/logs.sh staging web
   ```

**Day 2 Success Criteria**:
- [ ] Both production and staging environments running
- [ ] HTTPS working for both domains
- [ ] Database connections successful
- [ ] Basic authentication working
- [ ] Health checks passing

### Day 3: CI/CD Configuration ⚙️

**Goal**: Set up automated deployment pipeline
**Time Estimate**: 4-6 hours
**Key Deliverable**: Working GitHub Actions CI/CD

#### Morning (2-3 hours)
1. **Monitoring Setup**
   ```bash
   ./06-monitoring-setup.sh

   # Enter when prompted:
   # Email for alerts: your_admin_email@example.com
   # Slack webhook: [optional but recommended]

   # Verify monitoring
   /opt/witchcityrope/monitoring/dashboard.sh
   ```

2. **Backup Configuration**
   ```bash
   ./07-backup-setup.sh

   # Follow prompts for:
   # - Enable DigitalOcean Spaces backup (recommended)
   # - Backup retention settings

   # Test backup system
   /opt/witchcityrope/backup-full-database.sh
   /opt/witchcityrope/backup-manage.sh list
   ```

#### Afternoon (2-3 hours)
3. **GitHub Actions Setup**
   ```bash
   # In GitHub repository, add these secrets:
   # Settings → Secrets and variables → Actions
   ```

   Add these secrets in GitHub:
   ```
   DIGITALOCEAN_ACCESS_TOKEN=your_do_token
   PRODUCTION_SERVER_HOST=your_droplet_ip
   PRODUCTION_SERVER_USER=witchcity
   PRODUCTION_SERVER_SSH_KEY=your_private_key_content
   STAGING_SERVER_HOST=your_droplet_ip
   STAGING_SERVER_USER=witchcity
   STAGING_SERVER_SSH_KEY=your_private_key_content
   SLACK_WEBHOOK_URL=your_slack_webhook
   ```

4. **Deploy CI/CD Pipeline**
   ```bash
   # Copy GitHub Actions workflow
   cp /tmp/setup-scripts/github-actions-pipeline.yml .github/workflows/
   git add .github/workflows/github-actions-pipeline.yml
   git commit -m "Add DigitalOcean CI/CD pipeline"
   git push

   # Verify workflow in GitHub Actions tab
   ```

**Day 3 Success Criteria**:
- [ ] System monitoring active and accessible
- [ ] Automated backups running successfully
- [ ] GitHub Actions workflow created
- [ ] CI/CD secrets configured
- [ ] Test deployment successful

### Day 4: Testing and Verification 🧪

**Goal**: Comprehensive testing and performance validation
**Time Estimate**: 6-8 hours
**Key Deliverable**: Fully validated system ready for production

#### Morning (3-4 hours)
1. **Comprehensive Health Checks**
   ```bash
   # Run full system health check
   /opt/witchcityrope/health-check.sh

   # Performance testing
   # Test page load times (should be < 2 seconds)
   curl -w "@curl-format.txt" -o /dev/null -s "https://witchcityrope.com/"

   # API response time testing (should be < 500ms)
   time curl -s "https://witchcityrope.com/api/health"
   ```

2. **Security Validation**
   ```bash
   # SSL certificate validation
   /opt/witchcityrope/check-ssl.sh

   # Security headers test
   curl -I https://witchcityrope.com/

   # Firewall verification
   sudo ufw status
   ```

#### Afternoon (3-4 hours)
3. **Backup and Recovery Testing**
   ```bash
   # Test database backup
   /opt/witchcityrope/backup-full-database.sh

   # Test restore to staging (non-destructive)
   /opt/witchcityrope/restore-backup.sh database \
     production_YYYYMMDD_HHMMSS.sql.gz.enc --environment staging

   # Verify staging data integrity
   ```

4. **End-to-End Testing**
   ```bash
   # Test complete user workflows:
   # - User registration
   # - Login/logout
   # - Event viewing
   # - Payment processing (if configured)
   # - Admin functions

   # Test mobile responsiveness
   # Use browser dev tools to test mobile views
   ```

**Day 4 Success Criteria**:
- [ ] All health checks passing
- [ ] Performance targets met (< 2s page loads)
- [ ] Security scan passed
- [ ] Backup and recovery tested
- [ ] End-to-end user workflows working
- [ ] Mobile responsiveness verified

### Day 5: Go-Live and Monitoring 📊

**Goal**: Production launch and monitoring setup
**Time Estimate**: 4-6 hours
**Key Deliverable**: Live production system with monitoring

#### Morning (2-3 hours)
1. **Final Pre-Launch Checklist**
   ```bash
   # Resource utilization check
   /opt/witchcityrope/monitoring/dashboard.sh

   # Container status verification
   docker ps -a

   # Log analysis for any warnings
   /opt/witchcityrope/monitoring/analyze-logs.sh
   ```

2. **Production Launch Preparation**
   ```bash
   # Announce maintenance window to community
   # Switch DNS from old system (if applicable)
   # Final database migration (if needed)
   # Enable production monitoring alerts
   ```

#### Afternoon (2-3 hours)
3. **Go-Live Execution**
   ```bash
   # Final health check before launch
   /opt/witchcityrope/health-check.sh

   # Enable production traffic
   # Monitor real-time metrics
   # Verify user registrations and logins
   ```

4. **Post-Launch Monitoring**
   ```bash
   # Monitor for first 4 hours continuously
   watch -n 30 '/opt/witchcityrope/status.sh'

   # Check error logs every 30 minutes
   /opt/witchcityrope/logs.sh production api | tail -50

   # Verify payment processing (if applicable)
   ```

**Day 5 Success Criteria**:
- [ ] Production system live and accessible
- [ ] Real users able to register and use system
- [ ] Payment processing working (if applicable)
- [ ] Monitoring alerts configured
- [ ] Performance within acceptable ranges
- [ ] No critical errors in logs

## Common Pitfalls to Avoid ⚠️

### DNS Configuration Issues
- **Problem**: SSL certificates fail because DNS not propagated
- **Solution**: Verify DNS with `nslookup` before running SSL setup
- **Wait Time**: Allow 30+ minutes for DNS propagation

### Database Connection Failures
- **Problem**: Application can't connect to PostgreSQL
- **Solution**: Verify database credentials and VPC configuration
- **Check**: Ensure droplet is in same region as database

### Docker Permission Issues
- **Problem**: User can't run docker commands
- **Solution**: Add user to docker group and log out/in
- **Command**: `sudo usermod -aG docker witchcity`

### SSL Certificate Problems
- **Problem**: Let's Encrypt rate limits or validation failures
- **Solution**: Use staging SSL first, then production
- **Debug**: Check `/var/log/letsencrypt/letsencrypt.log`

### Container Startup Failures
- **Problem**: Containers exit immediately or become unhealthy
- **Solution**: Check environment variables and logs
- **Debug**: `docker logs container-name` and verify `.env` files

## Verification Steps

### After Each Day
1. **Document Progress**: Note what was completed and any issues
2. **Backup Configuration**: Save current state before next day
3. **Test Critical Paths**: Ensure previous functionality still works
4. **Update Team**: Send status update to stakeholders

### Final Verification Checklist
- [ ] **Performance**: Page loads < 2 seconds, API < 500ms
- [ ] **Security**: SSL A+ rating, security headers present
- [ ] **Functionality**: All user workflows working
- [ ] **Monitoring**: Health checks every 5 minutes
- [ ] **Backup**: Daily automated backups verified
- [ ] **Cost**: Monthly usage tracking under $95

## Emergency Contacts

### If Something Goes Wrong
1. **Infrastructure Issues**: DigitalOcean Support (24/7)
2. **SSL Problems**: Let's Encrypt Community Forums
3. **Application Issues**: Check container logs and GitHub Issues
4. **Database Issues**: DigitalOcean Managed Database Support

### Rollback Procedures
```bash
# Application rollback
/opt/witchcityrope/restart.sh all
cd /opt/witchcityrope/production
API_IMAGE=previous_tag WEB_IMAGE=previous_tag docker-compose up -d

# Database rollback (if needed)
/opt/witchcityrope/restore-backup.sh database backup_file.sql.gz.enc --environment production
```

## Success Indicators

### Technical Success ✅
- All services running and healthy
- Performance metrics within targets
- Security scans passing
- Monitoring and alerts active

### Business Success ✅
- Users can access and use the platform
- Payment processing operational (if applicable)
- Community feedback positive
- Costs within budget ($92/month)

### Operational Success ✅
- Team comfortable with monitoring tools
- Backup and recovery procedures tested
- CI/CD pipeline operational
- Documentation complete and accessible

---

**Next Steps**: After completing this guide, see [TROUBLESHOOTING-GUIDE.md](./TROUBLESHOOTING-GUIDE.md) for issue resolution and [SCRIPT-DOCUMENTATION.md](./SCRIPT-DOCUMENTATION.md) for detailed script information.

*This quick start guide provides the fastest path to a successful DigitalOcean deployment. All referenced scripts and procedures have been tested and validated.*