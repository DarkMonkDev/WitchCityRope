# Staging SSL Certificate Configuration

## Overview

This guide provides specific SSL/TLS configuration for the staging environment. The staging environment uses a subdomain structure and requires its own SSL certificates.

## Domain Structure

- Primary: `staging.witchcityrope.com`
- Alternative: `www.staging.witchcityrope.com`
- API: `api.staging.witchcityrope.com`

## Certificate Options for Staging

### 1. Let's Encrypt with Wildcard (Recommended)

Using a wildcard certificate simplifies management across all staging subdomains.

```bash
# Install Certbot
sudo apt update
sudo apt install certbot python3-certbot-nginx

# Request wildcard certificate
sudo certbot certonly --manual --preferred-challenges dns \
  -d "*.staging.witchcityrope.com" \
  -d "staging.witchcityrope.com" \
  --email admin@witchcityrope.com \
  --agree-tos
```

### 2. Let's Encrypt with Specific Domains

```bash
# Request certificates for specific domains
sudo certbot certonly --standalone \
  -d staging.witchcityrope.com \
  -d www.staging.witchcityrope.com \
  -d api.staging.witchcityrope.com \
  --email admin@witchcityrope.com \
  --agree-tos
```

### 3. Self-Signed Certificate (Development/Testing)

```bash
# Generate self-signed certificate for staging
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout /etc/ssl/private/staging.witchcityrope.key \
  -out /etc/ssl/certs/staging.witchcityrope.crt \
  -subj "/C=US/ST=MA/L=Salem/O=WitchCityRope/CN=*.staging.witchcityrope.com"

# Generate PFX for .NET applications
openssl pkcs12 -export \
  -out staging.witchcityrope.pfx \
  -inkey /etc/ssl/private/staging.witchcityrope.key \
  -in /etc/ssl/certs/staging.witchcityrope.crt \
  -password pass:staging-cert-password
```

## Nginx Configuration for Staging

### Create staging-specific Nginx configuration:

```nginx
# /etc/nginx/sites-available/staging.witchcityrope.com
server {
    listen 80;
    server_name staging.witchcityrope.com www.staging.witchcityrope.com api.staging.witchcityrope.com;
    
    # Redirect all HTTP to HTTPS
    return 301 https://$server_name$request_uri;
}

# Main staging site
server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name staging.witchcityrope.com www.staging.witchcityrope.com;

    # SSL Certificate
    ssl_certificate /etc/letsencrypt/live/staging.witchcityrope.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/staging.witchcityrope.com/privkey.pem;
    
    # SSL Configuration
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_prefer_server_ciphers off;
    ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384;
    
    # SSL Session
    ssl_session_timeout 1d;
    ssl_session_cache shared:SSL:50m;
    ssl_session_tickets off;
    
    # Security Headers (relaxed for staging)
    add_header Strict-Transport-Security "max-age=86400" always;
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header X-Environment "staging" always;
    
    # Logging
    access_log /var/log/nginx/staging.witchcityrope.access.log;
    error_log /var/log/nginx/staging.witchcityrope.error.log;
    
    # Proxy to web application
    location / {
        proxy_pass http://localhost:5651;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Environment "staging";
    }
    
    # Health check endpoint
    location /health {
        access_log off;
        return 200 "healthy\n";
        add_header Content-Type text/plain;
    }
}

# API subdomain
server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name api.staging.witchcityrope.com;

    # SSL Certificate (same wildcard cert)
    ssl_certificate /etc/letsencrypt/live/staging.witchcityrope.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/staging.witchcityrope.com/privkey.pem;
    
    # SSL Configuration (same as main site)
    include /etc/nginx/snippets/ssl-params.conf;
    
    # Security Headers
    add_header X-Environment "staging-api" always;
    
    # Logging
    access_log /var/log/nginx/api.staging.witchcityrope.access.log;
    error_log /var/log/nginx/api.staging.witchcityrope.error.log;
    
    # Proxy to API application
    location / {
        proxy_pass http://localhost:5653;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Environment "staging-api";
    }
}
```

## Docker Compose SSL Configuration

### docker-compose.staging.yml

```yaml
version: '3.8'

services:
  nginx:
    image: nginx:alpine
    container_name: witchcity-staging-nginx
    restart: always
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/staging:/etc/nginx/conf.d:ro
      - ./certs/staging:/etc/nginx/certs:ro
      - ./nginx/snippets:/etc/nginx/snippets:ro
    environment:
      - NGINX_ENVIRONMENT=staging
    depends_on:
      - web
      - api
    networks:
      - witchcity-staging-network

  api:
    build:
      context: .
      dockerfile: src/WitchCityRope.Api/Dockerfile
    container_name: witchcity-staging-api
    environment:
      - ASPNETCORE_ENVIRONMENT=Staging
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certs/staging.pfx
      - ASPNETCORE_Kestrel__Certificates__Default__Password=${STAGING_CERT_PASSWORD}
    volumes:
      - ./certs/staging:/app/certs:ro
      - ./data/staging:/app/data
    networks:
      - witchcity-staging-network

  web:
    build:
      context: .
      dockerfile: src/WitchCityRope.Web/Dockerfile
    container_name: witchcity-staging-web
    environment:
      - ASPNETCORE_ENVIRONMENT=Staging
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certs/staging.pfx
      - ASPNETCORE_Kestrel__Certificates__Default__Password=${STAGING_CERT_PASSWORD}
      - ApiBaseUrl=https://api.staging.witchcityrope.com/
    volumes:
      - ./certs/staging:/app/certs:ro
      - ./data/staging:/app/data:ro
    networks:
      - witchcity-staging-network

networks:
  witchcity-staging-network:
    driver: bridge
```

## Kestrel SSL Configuration

### appsettings.Staging.json additions:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://+:80"
      },
      "Https": {
        "Url": "https://+:443",
        "Certificate": {
          "Path": "/app/certs/staging.pfx",
          "Password": "${STAGING_CERT_PASSWORD}"
        }
      }
    },
    "Limits": {
      "MaxConcurrentConnections": 100,
      "MaxConcurrentUpgradedConnections": 100
    }
  }
}
```

## Certificate Management Scripts

### Auto-renewal script for staging:

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/renew-staging-cert.sh

DOMAIN="staging.witchcityrope.com"
CERT_PATH="/etc/letsencrypt/live/$DOMAIN"
PFX_PATH="/opt/witchcityrope/certs/staging/staging.pfx"
PFX_PASSWORD="${STAGING_CERT_PASSWORD:-staging-cert-password}"

# Renew certificate
certbot renew --cert-name $DOMAIN --quiet

# Convert to PFX if renewal succeeded
if [ $? -eq 0 ]; then
    openssl pkcs12 -export \
        -out $PFX_PATH \
        -inkey $CERT_PATH/privkey.pem \
        -in $CERT_PATH/fullchain.pem \
        -password pass:$PFX_PASSWORD
    
    # Restart services
    docker-compose -f docker-compose.staging.yml restart api web
    systemctl reload nginx
    
    echo "Staging certificate renewed successfully"
else
    echo "Certificate renewal failed" >&2
    exit 1
fi
```

### Certificate monitoring:

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/check-staging-cert.sh

DOMAIN="staging.witchcityrope.com"
DAYS_WARNING=30

# Check certificate expiration
EXPIRY=$(echo | openssl s_client -servername $DOMAIN -connect $DOMAIN:443 2>/dev/null | \
         openssl x509 -noout -enddate | cut -d= -f2)

EXPIRY_EPOCH=$(date -d "$EXPIRY" +%s)
CURRENT_EPOCH=$(date +%s)
DAYS_LEFT=$(( ($EXPIRY_EPOCH - $CURRENT_EPOCH) / 86400 ))

if [ $DAYS_LEFT -lt $DAYS_WARNING ]; then
    echo "WARNING: Staging SSL certificate expires in $DAYS_LEFT days"
    # Send alert email or notification
fi

echo "Staging certificate valid for $DAYS_LEFT more days"
```

## Security Considerations for Staging

1. **Relaxed HSTS**: Use shorter max-age (1 day) for easier testing
2. **Debug Headers**: Add environment indicators for troubleshooting
3. **Separate Certificates**: Don't reuse production certificates
4. **Access Restrictions**: Consider IP whitelisting for staging
5. **Test Accounts**: Use staging-specific test credentials
6. **Monitoring**: Enable verbose logging for debugging

## Testing SSL Configuration

### Verify staging SSL setup:

```bash
# Test HTTPS connection
curl -I https://staging.witchcityrope.com

# Check certificate details
openssl s_client -connect staging.witchcityrope.com:443 -servername staging.witchcityrope.com

# Test API endpoint
curl https://api.staging.witchcityrope.com/health

# Run SSL Labs test
# Visit: https://www.ssllabs.com/ssltest/analyze.html?d=staging.witchcityrope.com
```

### Troubleshooting Common Issues

1. **Certificate Name Mismatch**
   - Ensure certificate includes all staging subdomains
   - Use wildcard certificate for flexibility

2. **Mixed Content Warnings**
   - Update all internal links to use HTTPS
   - Check API base URLs in configuration

3. **CORS Issues**
   - Ensure CORS allows staging domains
   - Update allowed origins in appsettings.Staging.json

4. **Certificate Not Trusted**
   - For self-signed certs, add to trusted store
   - Ensure full certificate chain is included