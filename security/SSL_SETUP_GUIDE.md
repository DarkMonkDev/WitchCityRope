# SSL Certificate Setup Guide for Production

## Table of Contents
1. [SSL/TLS Overview](#ssltls-overview)
2. [Certificate Types](#certificate-types)
3. [Let's Encrypt Setup](#lets-encrypt-setup)
4. [Commercial SSL Setup](#commercial-ssl-setup)
5. [Nginx Configuration](#nginx-configuration)
6. [Apache Configuration](#apache-configuration)
7. [Certificate Renewal](#certificate-renewal)
8. [Security Best Practices](#security-best-practices)
9. [Troubleshooting](#troubleshooting)

## SSL/TLS Overview

SSL/TLS certificates encrypt data in transit between your server and users, ensuring:
- Data confidentiality
- Data integrity
- Server authentication
- SEO benefits
- User trust

## Certificate Types

### 1. Domain Validation (DV)
- Basic encryption
- Verifies domain ownership
- Quick issuance (minutes)
- Suitable for most websites

### 2. Organization Validation (OV)
- Includes company information
- More rigorous verification
- 1-3 days issuance
- Better for business sites

### 3. Extended Validation (EV)
- Highest level of validation
- Green address bar (legacy)
- Extensive verification process
- Best for e-commerce/financial sites

### 4. Wildcard Certificates
- Covers main domain + subdomains
- Example: *.example.com
- Cost-effective for multiple subdomains

## Let's Encrypt Setup

### Prerequisites
```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install Certbot
sudo apt install certbot python3-certbot-nginx -y
```

### Nginx Setup
```bash
# Obtain certificate
sudo certbot --nginx -d example.com -d www.example.com

# Test automatic renewal
sudo certbot renew --dry-run
```

### Apache Setup
```bash
# Install Apache plugin
sudo apt install python3-certbot-apache -y

# Obtain certificate
sudo certbot --apache -d example.com -d www.example.com
```

### Standalone Mode (No Web Server)
```bash
# Stop web server temporarily
sudo systemctl stop nginx

# Obtain certificate
sudo certbot certonly --standalone -d example.com -d www.example.com

# Start web server
sudo systemctl start nginx
```

### DNS Challenge (Wildcard Certificates)
```bash
# Manual DNS challenge
sudo certbot certonly --manual --preferred-challenges dns -d example.com -d *.example.com

# Follow prompts to add TXT records to DNS
```

## Commercial SSL Setup

### 1. Generate CSR (Certificate Signing Request)
```bash
# Generate private key
openssl genrsa -out private.key 2048

# Generate CSR
openssl req -new -key private.key -out certificate.csr

# CSR Information Example:
# Country Name: US
# State: Massachusetts
# Locality: Salem
# Organization: Witch City Rope
# Organizational Unit: IT
# Common Name: www.witchcityrope.com
# Email: admin@witchcityrope.com
```

### 2. Submit CSR to Certificate Authority
- Purchase SSL from provider (DigiCert, Comodo, GoDaddy, etc.)
- Submit CSR during purchase process
- Complete validation process

### 3. Install Certificate
```bash
# Download certificate files
# - certificate.crt (your domain certificate)
# - intermediate.crt (CA bundle)
# - root.crt (root certificate)

# Combine certificates (for Nginx)
cat certificate.crt intermediate.crt > fullchain.pem
```

## Nginx Configuration

### Basic SSL Configuration
```nginx
server {
    listen 80;
    server_name example.com www.example.com;
    
    # Redirect HTTP to HTTPS
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name example.com www.example.com;
    
    # SSL Certificate
    ssl_certificate /etc/letsencrypt/live/example.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/example.com/privkey.pem;
    
    # SSL Configuration
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES128-GCM-SHA256:ECDHE:ECDH:AES:HIGH:!NULL:!aNULL:!MD5:!ADH:!RC4;
    ssl_prefer_server_ciphers on;
    
    # OCSP Stapling
    ssl_stapling on;
    ssl_stapling_verify on;
    ssl_trusted_certificate /etc/letsencrypt/live/example.com/chain.pem;
    
    # Session Configuration
    ssl_session_cache shared:SSL:10m;
    ssl_session_timeout 10m;
    
    # Security Headers
    add_header Strict-Transport-Security "max-age=63072000; includeSubDomains; preload" always;
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    
    # Your application configuration
    root /var/www/html;
    index index.html index.htm;
    
    location / {
        try_files $uri $uri/ =404;
    }
}
```

### Advanced Security Configuration
```nginx
# DH Parameters (generate with: openssl dhparam -out /etc/nginx/dhparam.pem 4096)
ssl_dhparam /etc/nginx/dhparam.pem;

# HSTS Preloading
add_header Strict-Transport-Security "max-age=63072000; includeSubDomains; preload" always;

# Content Security Policy
add_header Content-Security-Policy "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdnjs.cloudflare.com; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; img-src 'self' data: https:; connect-src 'self' https://api.example.com;" always;

# Referrer Policy
add_header Referrer-Policy "strict-origin-when-cross-origin" always;

# Feature Policy
add_header Feature-Policy "geolocation 'none'; microphone 'none'; camera 'none'" always;
```

## Apache Configuration

### Basic SSL Configuration
```apache
<VirtualHost *:80>
    ServerName example.com
    ServerAlias www.example.com
    
    # Redirect to HTTPS
    RewriteEngine On
    RewriteCond %{HTTPS} off
    RewriteRule ^(.*)$ https://%{HTTP_HOST}$1 [R=301,L]
</VirtualHost>

<VirtualHost *:443>
    ServerName example.com
    ServerAlias www.example.com
    
    # Enable SSL
    SSLEngine on
    
    # Certificate Files
    SSLCertificateFile /etc/letsencrypt/live/example.com/cert.pem
    SSLCertificateKeyFile /etc/letsencrypt/live/example.com/privkey.pem
    SSLCertificateChainFile /etc/letsencrypt/live/example.com/chain.pem
    
    # SSL Protocol
    SSLProtocol -all +TLSv1.2 +TLSv1.3
    
    # Cipher Suite
    SSLCipherSuite ECDHE-RSA-AES128-GCM-SHA256:ECDHE:ECDH:AES:HIGH:!NULL:!aNULL:!MD5:!ADH:!RC4
    SSLHonorCipherOrder on
    
    # HSTS
    Header always set Strict-Transport-Security "max-age=63072000; includeSubDomains; preload"
    
    # Security Headers
    Header always set X-Frame-Options "SAMEORIGIN"
    Header always set X-Content-Type-Options "nosniff"
    Header always set X-XSS-Protection "1; mode=block"
    
    # Document Root
    DocumentRoot /var/www/html
    
    <Directory /var/www/html>
        Options -Indexes +FollowSymLinks
        AllowOverride All
        Require all granted
    </Directory>
</VirtualHost>
```

## Certificate Renewal

### Let's Encrypt Auto-Renewal

#### Systemd Timer (Recommended)
```bash
# Check timer status
sudo systemctl status certbot.timer

# Enable timer
sudo systemctl enable certbot.timer

# Manual renewal
sudo certbot renew
```

#### Cron Job
```bash
# Edit crontab
sudo crontab -e

# Add renewal job (daily at 2:30 AM)
30 2 * * * /usr/bin/certbot renew --quiet --post-hook "systemctl reload nginx"
```

### Commercial Certificate Renewal
1. Generate new CSR (optional, can reuse)
2. Purchase renewal from CA
3. Download new certificate
4. Replace old certificate files
5. Reload web server

```bash
# Backup old certificates
sudo cp /etc/ssl/certs/example.com.crt /etc/ssl/certs/example.com.crt.backup

# Install new certificate
sudo cp new_certificate.crt /etc/ssl/certs/example.com.crt

# Reload web server
sudo systemctl reload nginx
```

## Security Best Practices

### 1. Strong Configuration
- Use TLS 1.2 and 1.3 only
- Disable weak ciphers
- Enable OCSP stapling
- Implement HSTS

### 2. Certificate Management
```bash
# Check certificate expiration
openssl x509 -noout -dates -in /path/to/certificate.crt

# Verify certificate chain
openssl verify -CAfile chain.pem certificate.crt

# Test SSL configuration
openssl s_client -connect example.com:443 -servername example.com
```

### 3. Security Headers
```nginx
# Comprehensive security headers
add_header Strict-Transport-Security "max-age=63072000; includeSubDomains; preload" always;
add_header X-Frame-Options "DENY" always;
add_header X-Content-Type-Options "nosniff" always;
add_header X-XSS-Protection "1; mode=block" always;
add_header Referrer-Policy "strict-origin-when-cross-origin" always;
add_header Content-Security-Policy "default-src 'self'; frame-ancestors 'none';" always;
add_header Permissions-Policy "geolocation=(), microphone=(), camera=()" always;
```

### 4. Monitor Certificate Health
```bash
# Certificate expiration check script
#!/bin/bash
DOMAIN="example.com"
EXPIRY=$(echo | openssl s_client -servername $DOMAIN -connect $DOMAIN:443 2>/dev/null | openssl x509 -noout -enddate | cut -d= -f2)
EXPIRY_EPOCH=$(date -d "$EXPIRY" +%s)
NOW_EPOCH=$(date +%s)
DAYS_LEFT=$(( ($EXPIRY_EPOCH - $NOW_EPOCH) / 86400 ))

if [ $DAYS_LEFT -lt 30 ]; then
    echo "WARNING: Certificate expires in $DAYS_LEFT days!"
fi
```

## Troubleshooting

### Common Issues

#### 1. Certificate Chain Issues
```bash
# Check certificate chain
openssl s_client -connect example.com:443 -showcerts

# Verify intermediate certificates
openssl verify -untrusted intermediate.crt certificate.crt
```

#### 2. Mixed Content Warnings
- Ensure all resources use HTTPS
- Update hardcoded HTTP URLs
- Use protocol-relative URLs: //example.com/resource

#### 3. Certificate Not Trusted
- Verify correct certificate installation
- Check intermediate certificates
- Ensure certificate matches domain

#### 4. Renewal Failures
```bash
# Test renewal
sudo certbot renew --dry-run

# Check logs
sudo journalctl -u certbot

# Manual renewal with debug
sudo certbot renew --force-renewal --debug
```

### SSL Testing Tools

1. **SSL Labs (Online)**
   - https://www.ssllabs.com/ssltest/
   - Comprehensive SSL/TLS analysis
   - Grades configuration

2. **testssl.sh (Command Line)**
```bash
# Install
git clone https://github.com/drwetter/testssl.sh.git
cd testssl.sh

# Run test
./testssl.sh example.com
```

3. **OpenSSL Commands**
```bash
# Check certificate details
openssl x509 -in certificate.crt -text -noout

# Test connection
openssl s_client -connect example.com:443 -tls1_2

# Check supported ciphers
nmap --script ssl-enum-ciphers -p 443 example.com
```

## Automation Scripts

### Certificate Expiration Monitor
```bash
#!/bin/bash
# Save as: /usr/local/bin/check-ssl-expiry.sh

DOMAINS=("example.com" "www.example.com" "api.example.com")
ALERT_DAYS=30
EMAIL="admin@example.com"

for DOMAIN in "${DOMAINS[@]}"; do
    EXPIRY=$(echo | openssl s_client -servername $DOMAIN -connect $DOMAIN:443 2>/dev/null | openssl x509 -noout -enddate | cut -d= -f2)
    EXPIRY_EPOCH=$(date -d "$EXPIRY" +%s)
    NOW_EPOCH=$(date +%s)
    DAYS_LEFT=$(( ($EXPIRY_EPOCH - $NOW_EPOCH) / 86400 ))
    
    if [ $DAYS_LEFT -lt $ALERT_DAYS ]; then
        echo "Certificate for $DOMAIN expires in $DAYS_LEFT days" | mail -s "SSL Certificate Expiration Warning" $EMAIL
    fi
done
```

### Automated SSL Deployment
```bash
#!/bin/bash
# Save as: /usr/local/bin/deploy-ssl.sh

DOMAIN=$1
EMAIL=$2

if [ -z "$DOMAIN" ] || [ -z "$EMAIL" ]; then
    echo "Usage: $0 <domain> <email>"
    exit 1
fi

# Obtain certificate
certbot certonly --nginx -d $DOMAIN -d www.$DOMAIN --email $EMAIL --agree-tos --non-interactive

# Configure Nginx
cat > /etc/nginx/sites-available/$DOMAIN-ssl << EOF
server {
    listen 443 ssl http2;
    server_name $DOMAIN www.$DOMAIN;
    
    ssl_certificate /etc/letsencrypt/live/$DOMAIN/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/$DOMAIN/privkey.pem;
    
    # ... rest of configuration
}
EOF

# Enable site
ln -s /etc/nginx/sites-available/$DOMAIN-ssl /etc/nginx/sites-enabled/
nginx -t && systemctl reload nginx
```

## Conclusion

Proper SSL/TLS implementation is crucial for:
- Protecting user data
- Building trust
- SEO benefits
- Regulatory compliance

Regular monitoring and updates ensure continued security and availability.