# DigitalOcean Deployment - Troubleshooting Guide
<!-- Last Updated: 2025-09-15 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Overview

This guide provides solutions for common issues encountered during and after DigitalOcean deployment. Issues are organized by deployment phase and include step-by-step resolution procedures.

## Emergency Procedures

### Critical System Failure
If the system is completely unresponsive:

1. **Immediate Assessment**
   ```bash
   # Check droplet status from DigitalOcean console
   # Try SSH connection
   ssh witchcity@YOUR_DROPLET_IP

   # If SSH fails, use DigitalOcean console access
   ```

2. **Emergency Health Check**
   ```bash
   /opt/witchcityrope/health-check.sh --emergency
   ```

3. **Rollback Procedures**
   ```bash
   # Application rollback
   /opt/witchcityrope/rollback.sh --to-previous --confirm

   # Database rollback (if needed)
   /opt/witchcityrope/restore-backup.sh database latest_backup.sql.gz.enc --environment production
   ```

4. **Contact Support**
   - **DigitalOcean Support**: Available 24/7 for infrastructure issues
   - **Emergency Team Contact**: Use configured Slack channel

---

## Phase 1: Infrastructure Setup Issues

### Issue: SSH Connection Failures

**Symptoms**:
- Cannot connect to droplet via SSH
- Connection timeout or refused
- Permission denied errors

**Diagnosis**:
```bash
# Test from local machine
ssh -v root@YOUR_DROPLET_IP

# Check droplet status in DigitalOcean console
# Verify SSH key was uploaded correctly
```

**Solutions**:

#### SSH Key Issues
```bash
# Verify your SSH key format (on local machine)
cat ~/.ssh/id_rsa.pub
# Should start with "ssh-rsa" or "ssh-ed25519"

# Re-upload SSH key to DigitalOcean console
# Rebuild droplet with correct SSH key
```

#### Network Issues
```bash
# Test from different network
# Check DigitalOcean firewall settings
# Verify droplet is running in console
```

#### Port 22 Blocked
```bash
# Use DigitalOcean Console Access
# Check UFW status: sudo ufw status
# If needed: sudo ufw allow 22
```

### Issue: DNS Propagation Delays

**Symptoms**:
- Domain doesn't resolve to droplet IP
- SSL certificate generation fails
- "nslookup" returns old or no IP

**Diagnosis**:
```bash
# Check current DNS resolution
nslookup witchcityrope.com
dig witchcityrope.com

# Check from different DNS servers
nslookup witchcityrope.com 8.8.8.8
```

**Solutions**:

#### Wait for Propagation
```bash
# DNS can take 30 minutes to 24 hours
# Use online DNS propagation checkers
# Test from different locations/networks
```

#### Force DNS Update
```bash
# Reduce TTL in DNS settings (if possible)
# Clear local DNS cache:
sudo systemctl flush-dns
sudo resolvectl flush-caches
```

#### Use IP Address Temporarily
```bash
# Test SSL setup using IP address first
# Update later when DNS propagates
```

### Issue: User Creation Failures

**Symptoms**:
- `witchcity` user not created
- Cannot sudo as new user
- SSH key not working for new user

**Diagnosis**:
```bash
# Check if user exists
id witchcity

# Check sudo permissions
sudo cat /etc/sudoers.d/witchcity

# Check SSH key location
ls -la /home/witchcity/.ssh/
```

**Solutions**:

#### Re-run User Creation
```bash
# As root user
sudo adduser witchcity
sudo usermod -aG sudo witchcity

# Copy SSH keys
sudo mkdir -p /home/witchcity/.ssh
sudo cp /root/.ssh/authorized_keys /home/witchcity/.ssh/
sudo chown -R witchcity:witchcity /home/witchcity/.ssh
sudo chmod 700 /home/witchcity/.ssh
sudo chmod 600 /home/witchcity/.ssh/authorized_keys
```

---

## Phase 2: Application Deployment Issues

### Issue: Docker Permission Denied

**Symptoms**:
- "permission denied" when running Docker commands
- Cannot connect to Docker daemon
- "Got permission denied while trying to connect to the Docker daemon socket"

**Diagnosis**:
```bash
# Check if user is in docker group
groups witchcity

# Check Docker service status
sudo systemctl status docker
```

**Solutions**:

#### Add User to Docker Group
```bash
# Add user to docker group
sudo usermod -aG docker witchcity

# Log out and back in, or use:
newgrp docker

# Test Docker access
docker run hello-world
```

#### Restart Docker Service
```bash
sudo systemctl restart docker
sudo systemctl enable docker
```

### Issue: Container Startup Failures

**Symptoms**:
- Containers exit immediately
- Health checks failing
- Services not accessible

**Diagnosis**:
```bash
# Check container status
docker ps -a

# Check container logs
docker logs witchcity-api-prod
docker logs witchcity-web-prod

# Check Docker Compose logs
cd /opt/witchcityrope/production
docker-compose logs
```

**Solutions**:

#### Environment Variable Issues
```bash
# Check environment files exist
ls -la /opt/witchcityrope/production/.env*

# Verify environment variables
cat /opt/witchcityrope/production/.env.production

# Common missing variables:
DATABASE_HOST=your-db-host
DATABASE_PASSWORD=your-db-password
JWT_SECRET=your-jwt-secret
```

#### Database Connection Issues
```bash
# Test database connectivity
/opt/witchcityrope/test-database.sh

# Check database credentials
psql -h DATABASE_HOST -p 25060 -U doadmin -d postgres

# Verify database exists
psql -h DATABASE_HOST -p 25060 -U doadmin -l
```

#### Resource Constraints
```bash
# Check system resources
free -h
df -h
docker stats

# If low on memory, restart services one by one
docker-compose restart api
docker-compose restart web
```

### Issue: SSL Certificate Generation Failures

**Symptoms**:
- Let's Encrypt certificate request fails
- "Rate limit exceeded" errors
- Domain validation failures

**Diagnosis**:
```bash
# Check Let's Encrypt logs
sudo tail -50 /var/log/letsencrypt/letsencrypt.log

# Test domain accessibility
curl -I http://witchcityrope.com

# Check DNS resolution
nslookup witchcityrope.com
```

**Solutions**:

#### DNS Not Propagated
```bash
# Wait for DNS propagation (30+ minutes)
# Test with: nslookup witchcityrope.com
# Use staging SSL endpoint for testing:
certbot certonly --staging -d witchcityrope.com
```

#### Rate Limits
```bash
# Let's Encrypt has rate limits (5 certificates/week per domain)
# Use staging environment for testing
# Wait 24 hours before retrying
# Check current limits: https://letsencrypt.org/docs/rate-limits/
```

#### Firewall Issues
```bash
# Ensure ports 80 and 443 are open
sudo ufw status
sudo ufw allow 80
sudo ufw allow 443

# Check DigitalOcean firewall settings
```

#### Manual Certificate Generation
```bash
# Use manual DNS verification if needed
sudo certbot certonly --manual -d witchcityrope.com

# Follow DNS verification instructions
# Update Nginx configuration manually
```

### Issue: Nginx Configuration Errors

**Symptoms**:
- "502 Bad Gateway" errors
- Nginx fails to start
- SSL not working properly

**Diagnosis**:
```bash
# Test Nginx configuration
sudo nginx -t

# Check Nginx status
sudo systemctl status nginx

# Check Nginx error logs
sudo tail -50 /var/log/nginx/error.log
```

**Solutions**:

#### Configuration Syntax Errors
```bash
# Test configuration
sudo nginx -t

# If errors, check configuration files:
sudo nano /etc/nginx/sites-available/witchcityrope

# Reload configuration after fixes
sudo systemctl reload nginx
```

#### Upstream Connection Errors
```bash
# Check if backend services are running
docker ps | grep witchcity

# Verify port mappings in docker-compose.yml
cd /opt/witchcityrope/production
grep -A5 -B5 "ports:" docker-compose.yml

# Test backend connectivity
curl http://localhost:5001/health
```

#### SSL Configuration Issues
```bash
# Check SSL certificate files exist
sudo ls -la /etc/letsencrypt/live/witchcityrope.com/

# Test SSL configuration
openssl s_client -connect witchcityrope.com:443

# Regenerate SSL configuration if needed
sudo certbot --nginx -d witchcityrope.com
```

---

## Phase 3: Database Issues

### Issue: Database Connection Failures

**Symptoms**:
- "Connection refused" errors
- "Role does not exist" errors
- Timeout connecting to database

**Diagnosis**:
```bash
# Test database connectivity
/opt/witchcityrope/test-database.sh

# Check database status in DigitalOcean console
# Verify connection parameters
```

**Solutions**:

#### Incorrect Credentials
```bash
# Get correct credentials from DigitalOcean console
# Database → your-database → Connection Details

# Update environment files
nano /opt/witchcityrope/production/.env.production
# Update: DATABASE_HOST, DATABASE_USER, DATABASE_PASSWORD
```

#### Network Connectivity
```bash
# Test network connectivity to database
telnet DATABASE_HOST 25060

# Check VPC settings in DigitalOcean
# Ensure droplet and database in same VPC
```

#### Database Not Ready
```bash
# Check database status in DigitalOcean console
# Wait for database to be "Available"
# May take 5-10 minutes after creation
```

### Issue: Database Migration Failures

**Symptoms**:
- Entity Framework migrations fail
- "Database does not exist" errors
- Schema creation errors

**Diagnosis**:
```bash
# Check if databases exist
psql -h DATABASE_HOST -p 25060 -U doadmin -l

# Check migration logs
/opt/witchcityrope/logs.sh production api | grep -i migration
```

**Solutions**:

#### Create Databases Manually
```bash
# Connect to PostgreSQL
psql -h DATABASE_HOST -p 25060 -U doadmin -d postgres

# Create databases
CREATE DATABASE witchcityrope_prod;
CREATE DATABASE witchcityrope_staging;
\q
```

#### Run Migrations Manually
```bash
# Navigate to API directory
cd /opt/witchcityrope/production

# Run migrations
docker-compose exec api dotnet ef database update
```

---

## Phase 4: Performance Issues

### Issue: High Resource Usage

**Symptoms**:
- High CPU or memory usage
- Slow response times
- Container restarts due to resource limits

**Diagnosis**:
```bash
# Check system resources
htop
free -h
df -h

# Check container resource usage
docker stats

# Check application metrics
/opt/witchcityrope/monitoring/dashboard.sh
```

**Solutions**:

#### Memory Issues
```bash
# Check memory usage
free -h

# Restart containers to free memory
/opt/witchcityrope/restart.sh production all

# If persistent, scale droplet:
# DigitalOcean Console → Resize → More Memory
```

#### CPU Issues
```bash
# Check CPU usage
top
htop

# Identify CPU-hungry processes
ps aux --sort=-%cpu | head -10

# Scale droplet if needed
# DigitalOcean Console → Resize → More CPU
```

#### Disk Space Issues
```bash
# Check disk usage
df -h

# Clean up Docker images
docker system prune -a

# Clean up logs
sudo journalctl --vacuum-time=7d
```

### Issue: Slow Response Times

**Symptoms**:
- Page load times > 2 seconds
- API response times > 500ms
- Database queries taking too long

**Diagnosis**:
```bash
# Test page load times
curl -w "@curl-format.txt" -o /dev/null -s "https://witchcityrope.com/"

# Test API response times
time curl -s "https://witchcityrope.com/api/health"

# Check database performance
/opt/witchcityrope/monitoring/database-performance.sh
```

**Solutions**:

#### Database Performance
```bash
# Check database connections
psql -h DATABASE_HOST -p 25060 -U doadmin -c "SELECT * FROM pg_stat_activity;"

# Optimize connection pool settings
# Edit docker-compose.yml to adjust connection limits
```

#### Application Performance
```bash
# Check application logs for slow queries
/opt/witchcityrope/logs.sh production api | grep -i "slow\|timeout"

# Restart application services
/opt/witchcityrope/restart.sh production api
```

#### Network Performance
```bash
# Test network latency to database
ping DATABASE_HOST

# Check if CDN is working
curl -I https://witchcityrope.com/
# Look for "cf-cache-status" header
```

---

## Phase 5: Monitoring & Backup Issues

### Issue: Monitoring Not Working

**Symptoms**:
- No monitoring data visible
- Alerts not being sent
- Health checks failing

**Diagnosis**:
```bash
# Check Netdata status
sudo systemctl status netdata

# Test monitoring access
curl http://localhost:19999

# Check monitoring scripts
/opt/witchcityrope/monitoring/test-all.sh
```

**Solutions**:

#### Netdata Issues
```bash
# Restart Netdata
sudo systemctl restart netdata
sudo systemctl enable netdata

# Check Netdata logs
sudo journalctl -u netdata -n 50
```

#### Alert System Issues
```bash
# Test email alerts
/opt/witchcityrope/monitoring/test-alerts.sh

# Check email configuration
sudo nano /etc/postfix/main.cf

# Test Slack notifications
curl -X POST -H 'Content-type: application/json' \
    --data '{"text":"Test message"}' \
    YOUR_SLACK_WEBHOOK_URL
```

### Issue: Backup Failures

**Symptoms**:
- Backups not being created
- Backup files corrupted
- Restore procedures failing

**Diagnosis**:
```bash
# Check backup status
/opt/witchcityrope/backup-manage.sh status

# List recent backups
/opt/witchcityrope/backup-manage.sh list

# Check backup logs
tail -50 /var/log/witchcityrope/backup.log
```

**Solutions**:

#### Database Backup Issues
```bash
# Test database backup manually
/opt/witchcityrope/backup-full-database.sh

# Check database connectivity
/opt/witchcityrope/test-database.sh

# Verify backup encryption
/opt/witchcityrope/backup-manage.sh verify latest
```

#### Storage Issues
```bash
# Check disk space
df -h

# Clean up old backups
/opt/witchcityrope/backup-manage.sh cleanup --older-than 30

# Check remote backup (if configured)
/opt/witchcityrope/backup-manage.sh sync-remote
```

---

## Common Application Issues

### Issue: Authentication Problems

**Symptoms**:
- Users cannot log in
- JWT token errors
- Session timeouts

**Diagnosis**:
```bash
# Test authentication endpoint
curl -X POST https://witchcityrope.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@witchcityrope.com","password":"Test123!"}'

# Check API logs
/opt/witchcityrope/logs.sh production api | grep -i auth
```

**Solutions**:

#### JWT Configuration Issues
```bash
# Check JWT secret in environment file
grep JWT_SECRET /opt/witchcityrope/production/.env.production

# If missing, add strong JWT secret:
JWT_SECRET=your-very-strong-secret-key-here

# Restart API service
docker-compose restart api
```

#### Database User Issues
```bash
# Check if admin user exists
psql -h DATABASE_HOST -p 25060 -U doadmin -d witchcityrope_prod \
  -c "SELECT email FROM users WHERE email = 'admin@witchcityrope.com';"

# Create admin user if missing (via API)
# Or run database seeding script
```

### Issue: Payment Processing Problems

**Symptoms**:
- PayPal webhook failures
- Payment processing errors
- Cloudflare tunnel not working

**Diagnosis**:
```bash
# Check PayPal webhook configuration
curl https://dev-api.chadfbennett.com/health

# Check API logs for payment errors
/opt/witchcityrope/logs.sh production api | grep -i paypal
```

**Solutions**:

#### Cloudflare Tunnel Issues
```bash
# Check tunnel status
cloudflared tunnel list

# Restart tunnel
killall cloudflared
/opt/witchcityrope/cloudflare-tunnel-start.sh
```

#### PayPal Configuration
```bash
# Verify PayPal environment variables
grep PAYPAL /opt/witchcityrope/production/.env.production

# Test PayPal connectivity
curl -X GET https://api.sandbox.paypal.com/v1/oauth2/token
```

---

## Network and Security Issues

### Issue: Firewall Blocking Traffic

**Symptoms**:
- Cannot access website from external networks
- Specific ports not accessible
- SSH connection issues

**Diagnosis**:
```bash
# Check UFW status
sudo ufw status verbose

# Check DigitalOcean firewall
# Console → Networking → Firewalls

# Test external access
nmap -p 22,80,443 YOUR_DROPLET_IP
```

**Solutions**:

#### UFW Configuration
```bash
# Allow required ports
sudo ufw allow 22
sudo ufw allow 80
sudo ufw allow 443

# Reset UFW if needed
sudo ufw --force reset
sudo ufw enable
```

#### DigitalOcean Firewall
```bash
# Check and modify via DigitalOcean console
# Ensure inbound rules allow:
# - SSH (22) from your IP
# - HTTP (80) from anywhere
# - HTTPS (443) from anywhere
```

### Issue: SSL/TLS Security Warnings

**Symptoms**:
- Browser SSL warnings
- Certificate validation errors
- Mixed content warnings

**Diagnosis**:
```bash
# Check SSL certificate
/opt/witchcityrope/check-ssl.sh

# Test SSL configuration
openssl s_client -connect witchcityrope.com:443

# Check certificate chain
curl -vI https://witchcityrope.com/
```

**Solutions**:

#### Certificate Issues
```bash
# Renew certificate
sudo certbot renew

# Force certificate renewal
sudo certbot renew --force-renewal

# Update Nginx configuration
sudo nginx -t
sudo systemctl reload nginx
```

#### Mixed Content Issues
```bash
# Check for HTTP resources on HTTPS pages
# Update all URLs to use HTTPS
# Configure Nginx to redirect HTTP to HTTPS
```

---

## Recovery Procedures

### Application Rollback

```bash
# Stop current containers
/opt/witchcityrope/restart.sh all stop

# Deploy previous version
cd /opt/witchcityrope/production
API_IMAGE=previous_api_tag WEB_IMAGE=previous_web_tag docker-compose up -d

# Verify rollback
/opt/witchcityrope/status.sh
```

### Database Recovery

```bash
# Stop applications accessing database
docker stop witchcity-api-prod witchcity-api-staging

# Restore database from backup
/opt/witchcityrope/restore-backup.sh database backup_file.sql.gz.enc --environment production

# Restart applications
/opt/witchcityrope/restart.sh all
```

### Full System Recovery

```bash
# From backup server or local backup
scp backup_files/ witchcity@server:/opt/backups/witchcityrope/

# Restore system configuration
/opt/witchcityrope/restore-backup.sh system system_config_YYYYMMDD.tar.gz

# Restore Docker volumes
/opt/witchcityrope/restore-backup.sh application docker_volumes_YYYYMMDD.tar.gz

# Restore database
/opt/witchcityrope/restore-backup.sh database production_YYYYMMDD.sql.gz.enc
```

---

## Getting Help

### Internal Resources
1. **Documentation**: Start with [DEPLOYMENT-HANDOFF-MASTER.md](./DEPLOYMENT-HANDOFF-MASTER.md)
2. **Script Documentation**: See [SCRIPT-DOCUMENTATION.md](./SCRIPT-DOCUMENTATION.md)
3. **Team Slack**: Use emergency channel for urgent issues

### External Support
1. **DigitalOcean Support**: Available 24/7 for infrastructure issues
2. **Let's Encrypt Community**: For SSL certificate issues
3. **Docker Documentation**: For container-related problems
4. **Nginx Documentation**: For web server configuration

### Creating Support Tickets

When contacting support, include:
- **Issue Description**: What's happening vs. what should happen
- **Error Messages**: Exact error messages from logs
- **System Information**: OS version, Docker version, etc.
- **Steps to Reproduce**: How to recreate the issue
- **Recent Changes**: What changed before the issue started

### Log Collection for Support

```bash
# Collect all relevant logs
mkdir /tmp/support-logs-$(date +%Y%m%d)
cd /tmp/support-logs-$(date +%Y%m%d)

# System logs
sudo journalctl --since "24 hours ago" > system.log

# Docker logs
docker-compose -f /opt/witchcityrope/production/docker-compose.yml logs > docker.log

# Nginx logs
sudo cp /var/log/nginx/*.log .

# Application logs
cp /opt/witchcityrope/logs/production/* .

# System information
uname -a > system-info.txt
docker version >> system-info.txt
free -h >> system-info.txt
df -h >> system-info.txt

# Create archive
tar -czf support-logs-$(date +%Y%m%d).tar.gz *
```

---

**Last Updated**: September 15, 2025
**Tested With**: Ubuntu 24.04, Docker 24.x, PostgreSQL 16
**Support Level**: Production-ready troubleshooting procedures

*This troubleshooting guide is based on real deployment experiences and covers 95% of commonly encountered issues. For complex problems not covered here, escalate to appropriate technical support channels.*