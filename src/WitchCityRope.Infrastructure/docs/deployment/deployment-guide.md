# WitchCityRope Production Deployment Guide

## Overview

This guide provides step-by-step instructions for deploying WitchCityRope to a production VPS environment.

## Prerequisites

- VPS with Ubuntu 22.04 LTS or later
- Minimum 2GB RAM, 2 CPU cores
- Domain name pointing to your VPS IP
- SSH access to the VPS
- Docker and Docker Compose installed
- SSL certificate (Let's Encrypt recommended)

## Deployment Steps

### 1. Initial Server Setup

```bash
# Update system packages
sudo apt update && sudo apt upgrade -y

# Install required dependencies
sudo apt install -y curl git nginx certbot python3-certbot-nginx

# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Add current user to docker group
sudo usermod -aG docker $USER
newgrp docker
```

### 2. Clone Repository

```bash
# Create application directory
sudo mkdir -p /opt/witchcityrope
sudo chown $USER:$USER /opt/witchcityrope

# Clone repository
cd /opt/witchcityrope
git clone https://github.com/yourusername/WitchCityRope.git .
```

### 3. Configure Environment

```bash
# Copy and configure production environment file
cp .env.example .env.production
nano .env.production
```

Required environment variables:
```
# Application
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000

# Database
ConnectionStrings__DefaultConnection=Server=db;Database=WitchCityRope;User=witchcity;Password=your-secure-password

# Email
Email__SmtpServer=smtp.example.com
Email__SmtpPort=587
Email__SmtpUsername=noreply@witchcityrope.com
Email__SmtpPassword=your-smtp-password
Email__FromEmail=noreply@witchcityrope.com
Email__FromName=Witch City Rope

# Security
Authentication__JwtKey=your-very-long-secure-jwt-key
Authentication__JwtIssuer=https://yourdomain.com
Authentication__JwtAudience=https://yourdomain.com

# Storage
Storage__Provider=S3
Storage__S3__BucketName=witchcityrope-assets
Storage__S3__AccessKey=your-s3-access-key
Storage__S3__SecretKey=your-s3-secret-key
Storage__S3__Region=us-east-1
```

### 4. Build and Deploy with Docker Compose

```bash
# Build images
docker-compose -f docker-compose.prod.yml build

# Start services
docker-compose -f docker-compose.prod.yml up -d

# Check logs
docker-compose -f docker-compose.prod.yml logs -f
```

### 5. Configure Nginx Reverse Proxy

Create Nginx configuration:
```bash
sudo nano /etc/nginx/sites-available/witchcityrope
```

```nginx
server {
    listen 80;
    server_name yourdomain.com www.yourdomain.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Static files
    location /static/ {
        alias /opt/witchcityrope/wwwroot/;
        expires 30d;
        add_header Cache-Control "public, immutable";
    }

    # Health check endpoint
    location /health {
        access_log off;
        proxy_pass http://localhost:5000/health;
    }
}
```

Enable site:
```bash
sudo ln -s /etc/nginx/sites-available/witchcityrope /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### 6. Setup SSL Certificate

```bash
# Install SSL certificate with Let's Encrypt
sudo certbot --nginx -d yourdomain.com -d www.yourdomain.com

# Test automatic renewal
sudo certbot renew --dry-run
```

### 7. Database Migrations

```bash
# Run database migrations
docker-compose -f docker-compose.prod.yml exec web dotnet ef database update
```

### 8. Create Admin User

```bash
# Connect to application container
docker-compose -f docker-compose.prod.yml exec web bash

# Create admin user
dotnet WitchCityRope.dll seed-admin --email admin@witchcityrope.com --password YourSecurePassword
```

### 9. Setup Monitoring

```bash
# Install monitoring stack
docker-compose -f docker-compose.monitoring.yml up -d

# Configure alerts in Grafana
# Access at http://yourdomain.com:3000
```

### 10. Configure Backups

```bash
# Setup automated backups
sudo crontab -e

# Add backup jobs
0 2 * * * /opt/witchcityrope/scripts/backup-database.sh
0 3 * * * /opt/witchcityrope/scripts/backup-uploads.sh
```

## Post-Deployment Checklist

- [ ] Verify all services are running: `docker-compose ps`
- [ ] Test website accessibility via HTTPS
- [ ] Verify SSL certificate is valid
- [ ] Test user registration and login
- [ ] Verify email sending functionality
- [ ] Check image upload functionality
- [ ] Test payment processing (if applicable)
- [ ] Verify backup scripts are working
- [ ] Setup monitoring alerts
- [ ] Configure firewall rules
- [ ] Enable automatic security updates

## Troubleshooting

### Service Won't Start
```bash
# Check logs
docker-compose -f docker-compose.prod.yml logs web

# Restart services
docker-compose -f docker-compose.prod.yml restart
```

### Database Connection Issues
```bash
# Verify database is running
docker-compose -f docker-compose.prod.yml ps db

# Test connection
docker-compose -f docker-compose.prod.yml exec db psql -U witchcity -d WitchCityRope
```

### SSL Certificate Issues
```bash
# Renew certificate manually
sudo certbot renew --force-renewal

# Check certificate status
sudo certbot certificates
```

## Maintenance Commands

```bash
# View logs
docker-compose -f docker-compose.prod.yml logs -f --tail=100

# Restart application
docker-compose -f docker-compose.prod.yml restart web

# Update application
git pull origin main
docker-compose -f docker-compose.prod.yml build
docker-compose -f docker-compose.prod.yml up -d

# Clean up old images
docker system prune -a
```