# SSL Certificate Setup and Configuration

## Overview

This guide covers SSL/TLS certificate setup and configuration for securing WitchCityRope with HTTPS.

## Certificate Options

### 1. Let's Encrypt (Recommended)
- **Cost**: Free
- **Automation**: Full automation with Certbot
- **Validity**: 90 days (auto-renewable)
- **Domain Validation**: Required

### 2. Commercial Certificates
- **Providers**: DigiCert, Comodo, GoDaddy
- **Cost**: $50-300/year
- **Validity**: 1-2 years
- **Support**: Extended validation options

### 3. Self-Signed Certificates
- **Use Case**: Development/testing only
- **Cost**: Free
- **Warning**: Browsers will show security warnings

## Let's Encrypt Setup

### Prerequisites

```bash
# Install Certbot
sudo apt update
sudo apt install certbot python3-certbot-nginx

# Ensure domain points to your server
dig yourdomain.com +short
# Should return your server's IP address
```

### Obtain Certificate

```bash
# Stop nginx if running
sudo systemctl stop nginx

# Obtain certificate (standalone mode)
sudo certbot certonly --standalone -d yourdomain.com -d www.yourdomain.com

# Or with nginx plugin (if nginx is configured)
sudo certbot --nginx -d yourdomain.com -d www.yourdomain.com

# For wildcard certificates
sudo certbot certonly --manual --preferred-challenges dns -d "*.yourdomain.com" -d yourdomain.com
```

### Certificate Files

```bash
# Certificate locations
/etc/letsencrypt/live/yourdomain.com/fullchain.pem  # Certificate chain
/etc/letsencrypt/live/yourdomain.com/privkey.pem    # Private key
/etc/letsencrypt/live/yourdomain.com/cert.pem       # Certificate only
/etc/letsencrypt/live/yourdomain.com/chain.pem      # Intermediate certificates
```

## Nginx SSL Configuration

### Strong SSL Configuration

```nginx
# /etc/nginx/sites-available/witchcityrope-ssl
server {
    listen 80;
    server_name yourdomain.com www.yourdomain.com;
    
    # Redirect all HTTP to HTTPS
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name yourdomain.com www.yourdomain.com;

    # SSL Certificate
    ssl_certificate /etc/letsencrypt/live/yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/yourdomain.com/privkey.pem;
    
    # SSL Configuration
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_prefer_server_ciphers off;
    ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384:ECDHE-ECDSA-CHACHA20-POLY1305:ECDHE-RSA-CHACHA20-POLY1305:DHE-RSA-AES128-GCM-SHA256:DHE-RSA-AES256-GCM-SHA384;
    
    # SSL Session
    ssl_session_timeout 1d;
    ssl_session_cache shared:SSL:50m;
    ssl_session_tickets off;
    
    # OCSP Stapling
    ssl_stapling on;
    ssl_stapling_verify on;
    ssl_trusted_certificate /etc/letsencrypt/live/yourdomain.com/chain.pem;
    
    # Security Headers
    add_header Strict-Transport-Security "max-age=63072000; includeSubDomains; preload" always;
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;
    add_header Content-Security-Policy "default-src 'self' https:; script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://code.jquery.com; style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; img-src 'self' data: https:; connect-src 'self' https://api.stripe.com;" always;
    
    # Proxy to application
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Real-IP $remote_addr;
    }
    
    # Static files with caching
    location ~* \.(jpg|jpeg|png|gif|ico|css|js|pdf|txt|woff|woff2|ttf|svg|eot)$ {
        root /opt/witchcityrope/wwwroot;
        expires 30d;
        add_header Cache-Control "public, immutable";
        add_header X-Content-Type-Options "nosniff" always;
    }
}
```

### Enable Configuration

```bash
# Enable site
sudo ln -s /etc/nginx/sites-available/witchcityrope-ssl /etc/nginx/sites-enabled/

# Test configuration
sudo nginx -t

# Reload nginx
sudo systemctl reload nginx
```

## Docker SSL Configuration

### Docker Compose with SSL

```yaml
# docker-compose.ssl.yml
version: '3.8'

services:
  nginx:
    image: nginx:alpine
    container_name: witchcityrope-nginx
    restart: always
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/conf.d:/etc/nginx/conf.d:ro
      - /etc/letsencrypt:/etc/letsencrypt:ro
      - ./nginx/ssl-params.conf:/etc/nginx/ssl-params.conf:ro
      - certbot-webroot:/var/www/certbot:ro
    depends_on:
      - web
    networks:
      - witchcity-network

  certbot:
    image: certbot/certbot
    container_name: witchcityrope-certbot
    volumes:
      - /etc/letsencrypt:/etc/letsencrypt
      - certbot-webroot:/var/www/certbot
    entrypoint: "/bin/sh -c 'trap exit TERM; while :; do certbot renew; sleep 12h & wait $${!}; done;'"

volumes:
  certbot-webroot:
```

### Nginx Config for Docker

```nginx
# nginx/conf.d/ssl.conf
server {
    listen 80;
    server_name yourdomain.com www.yourdomain.com;
    
    location /.well-known/acme-challenge/ {
        root /var/www/certbot;
    }
    
    location / {
        return 301 https://$server_name$request_uri;
    }
}

server {
    listen 443 ssl http2;
    server_name yourdomain.com www.yourdomain.com;
    
    include /etc/nginx/ssl-params.conf;
    
    ssl_certificate /etc/letsencrypt/live/yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/yourdomain.com/privkey.pem;
    
    location / {
        proxy_pass http://web:5000;
        include /etc/nginx/proxy-params.conf;
    }
}
```

## Certificate Renewal

### Automatic Renewal

```bash
# Test renewal
sudo certbot renew --dry-run

# Setup auto-renewal with cron
sudo crontab -e

# Add renewal job
0 0,12 * * * /usr/bin/certbot renew --quiet --post-hook "systemctl reload nginx"
```

### Manual Renewal

```bash
# Renew all certificates
sudo certbot renew

# Renew specific certificate
sudo certbot renew --cert-name yourdomain.com

# Force renewal
sudo certbot renew --force-renewal
```

### Renewal Hooks

```bash
# Create renewal hook script
sudo nano /etc/letsencrypt/renewal-hooks/deploy/reload-services.sh

#!/bin/bash
# Reload nginx
systemctl reload nginx

# Restart Docker containers if needed
cd /opt/witchcityrope && docker-compose restart nginx

# Make executable
sudo chmod +x /etc/letsencrypt/renewal-hooks/deploy/reload-services.sh
```

## SSL Testing and Validation

### Online Tools

1. **SSL Labs Test**
   - URL: https://www.ssllabs.com/ssltest/
   - Tests configuration strength
   - Provides detailed report

2. **Security Headers**
   - URL: https://securityheaders.com/
   - Tests security headers
   - Recommendations for improvements

### Command Line Testing

```bash
# Test SSL handshake
openssl s_client -connect yourdomain.com:443 -servername yourdomain.com

# Check certificate details
openssl x509 -in /etc/letsencrypt/live/yourdomain.com/cert.pem -text -noout

# Test TLS versions
# TLS 1.2
openssl s_client -connect yourdomain.com:443 -tls1_2

# TLS 1.3
openssl s_client -connect yourdomain.com:443 -tls1_3

# Check certificate expiration
echo | openssl s_client -servername yourdomain.com -connect yourdomain.com:443 2>/dev/null | openssl x509 -noout -dates
```

### Curl Testing

```bash
# Test HTTPS connection
curl -I https://yourdomain.com

# Verbose SSL information
curl -Ivs https://yourdomain.com

# Test specific TLS version
curl --tlsv1.2 -I https://yourdomain.com
```

## Advanced SSL Configuration

### HTTP/2 Configuration

```nginx
# Enable HTTP/2
listen 443 ssl http2;

# HTTP/2 Push
http2_push /css/style.css;
http2_push /js/app.js;
```

### Perfect Forward Secrecy

```nginx
# Generate DH parameters
sudo openssl dhparam -out /etc/nginx/dhparam.pem 2048

# Add to nginx config
ssl_dhparam /etc/nginx/dhparam.pem;
ssl_ecdh_curve secp384r1;
```

### OCSP Must-Staple

```bash
# Request certificate with OCSP Must-Staple
sudo certbot certonly --standalone -d yourdomain.com --must-staple
```

### CAA Records

```bash
# Add CAA DNS record
yourdomain.com. CAA 0 issue "letsencrypt.org"
yourdomain.com. CAA 0 issuewild "letsencrypt.org"
```

## Troubleshooting

### Common Issues

1. **Certificate not trusted**
```bash
# Check certificate chain
openssl s_client -connect yourdomain.com:443 -showcerts

# Verify intermediate certificates
cat /etc/letsencrypt/live/yourdomain.com/chain.pem
```

2. **Mixed content warnings**
```bash
# Find HTTP resources
grep -r "http://" /opt/witchcityrope/wwwroot/

# Update application settings
ASPNETCORE_HTTPS_PORT=443
ASPNETCORE_URLS=https://+:443;http://+:80
```

3. **Renewal failures**
```bash
# Check certbot logs
sudo cat /var/log/letsencrypt/letsencrypt.log

# Test renewal with verbose output
sudo certbot renew --dry-run -v
```

### Debug SSL Issues

```bash
# Enable nginx debug logging
error_log /var/log/nginx/error.log debug;

# Test specific cipher
openssl s_client -connect yourdomain.com:443 -cipher ECDHE-RSA-AES128-GCM-SHA256

# Check SSL protocols
nmap --script ssl-enum-ciphers -p 443 yourdomain.com
```

## Security Best Practices

### 1. Strong Configuration
- Use TLS 1.2 and 1.3 only
- Disable weak ciphers
- Enable HSTS
- Use OCSP stapling

### 2. Regular Monitoring
- Monitor certificate expiration
- Test SSL configuration monthly
- Keep up with security advisories

### 3. Backup Certificates
```bash
# Backup Let's Encrypt directory
sudo tar -czf letsencrypt-backup.tar.gz /etc/letsencrypt/

# Store securely
aws s3 cp letsencrypt-backup.tar.gz s3://witchcityrope-backups/ssl/
```

### 4. Certificate Pinning
```nginx
# Add public key pins (use with caution)
add_header Public-Key-Pins 'pin-sha256="base64+primary=="; pin-sha256="base64+backup=="; max-age=5184000; includeSubDomains';
```

### 5. Monitor CT Logs
- Subscribe to Certificate Transparency monitoring
- Get alerts for new certificates issued for your domain

## Application-Level SSL

### ASP.NET Core Configuration

```csharp
// Program.cs
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
    options.HttpsPort = 443;
});

builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
    options.ExcludedHosts.Add("localhost");
});

// In Configure method
app.UseHttpsRedirection();
app.UseHsts();
```

### Kestrel HTTPS Configuration

```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://+:443",
        "Certificate": {
          "Path": "/etc/letsencrypt/live/yourdomain.com/fullchain.pem",
          "KeyPath": "/etc/letsencrypt/live/yourdomain.com/privkey.pem"
        }
      }
    }
  }
}
```