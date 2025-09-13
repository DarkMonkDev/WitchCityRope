#!/bin/bash
# SSL Setup Script - Configure HTTPS with Let's Encrypt
# Run this script as the witchcity user
# Usage: ./04-ssl-setup.sh

set -euo pipefail

echo "🔒 Setting up SSL certificates with Let's Encrypt for WitchCityRope..."
echo "📅 Started at: $(date)"

# Configuration - UPDATE THESE WITH YOUR ACTUAL DOMAINS
PRODUCTION_DOMAIN=""
STAGING_DOMAIN=""
EMAIL=""

# Check if running as correct user
if [ "$USER" = "root" ]; then
    echo "❌ This script should not be run as root"
    echo "   Please run as witchcity user: ./04-ssl-setup.sh"
    exit 1
fi

# Function to prompt for domain information
prompt_for_domains() {
    echo "🌐 Please provide your domain information:"
    echo ""

    # Prompt for production domain
    read -p "Enter Production Domain (e.g., witchcityrope.com): " PRODUCTION_DOMAIN
    if [ -z "$PRODUCTION_DOMAIN" ]; then
        echo "❌ Production domain is required"
        exit 1
    fi

    # Prompt for staging domain
    read -p "Enter Staging Domain (e.g., staging.witchcityrope.com): " STAGING_DOMAIN
    if [ -z "$STAGING_DOMAIN" ]; then
        echo "❌ Staging domain is required"
        exit 1
    fi

    # Prompt for email
    read -p "Enter Email for Let's Encrypt notifications: " EMAIL
    if [ -z "$EMAIL" ]; then
        echo "❌ Email is required for Let's Encrypt"
        exit 1
    fi

    echo "✅ Domain information collected:"
    echo "   Production: $PRODUCTION_DOMAIN"
    echo "   Staging: $STAGING_DOMAIN"
    echo "   Email: $EMAIL"
}

# Function to check DNS resolution
check_dns() {
    local domain=$1
    echo "🔍 Checking DNS resolution for $domain..."

    if nslookup "$domain" > /dev/null 2>&1; then
        echo "✅ DNS resolution: $domain resolves correctly"
        return 0
    else
        echo "❌ DNS resolution: $domain does not resolve"
        return 1
    fi
}

# Get domain information from user
prompt_for_domains

# Verify DNS resolution
echo "🔍 Verifying DNS resolution..."
DNS_ISSUES=false

if ! check_dns "$PRODUCTION_DOMAIN"; then
    DNS_ISSUES=true
fi

if ! check_dns "$STAGING_DOMAIN"; then
    DNS_ISSUES=true
fi

if [ "$DNS_ISSUES" = true ]; then
    echo ""
    echo "⚠️  WARNING: DNS resolution issues detected"
    echo "   SSL certificate generation may fail if domains don't resolve to this server"
    echo "   Make sure your domains point to this server's IP address:"
    echo "   Server IP: $(curl -s ifconfig.me 2>/dev/null || echo "Unable to detect")"
    echo ""
    read -p "Continue anyway? (yes/no): " continue_anyway

    if [ "$continue_anyway" != "yes" ]; then
        echo "Setup cancelled. Please configure DNS first."
        exit 1
    fi
fi

# Create Nginx configuration for production
echo "🌐 Creating Nginx configuration for production ($PRODUCTION_DOMAIN)..."
sudo tee /etc/nginx/sites-available/witchcityrope-production > /dev/null << EOF
# WitchCityRope Production Configuration
# Domain: $PRODUCTION_DOMAIN

# HTTP server block - redirects to HTTPS
server {
    listen 80;
    listen [::]:80;
    server_name $PRODUCTION_DOMAIN;

    # Let's Encrypt challenge directory
    location /.well-known/acme-challenge/ {
        root /var/www/html;
    }

    # Redirect all other HTTP requests to HTTPS
    location / {
        return 301 https://\$server_name\$request_uri;
    }
}

# HTTPS server block - main production configuration
server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name $PRODUCTION_DOMAIN;

    # SSL Configuration (certificates will be configured by Certbot)
    ssl_certificate /etc/letsencrypt/live/$PRODUCTION_DOMAIN/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/$PRODUCTION_DOMAIN/privkey.pem;

    # SSL Security Settings
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES256-GCM-SHA384:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-RSA-AES256-SHA384:ECDHE-RSA-AES128-SHA256;
    ssl_prefer_server_ciphers off;
    ssl_session_timeout 1d;
    ssl_session_cache shared:SSL:50m;
    ssl_stapling on;
    ssl_stapling_verify on;

    # Security Headers
    add_header Strict-Transport-Security "max-age=63072000" always;
    add_header X-Frame-Options DENY always;
    add_header X-Content-Type-Options nosniff always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;

    # Logging
    access_log /var/log/nginx/witchcityrope/production-access.log;
    error_log /var/log/nginx/witchcityrope/production-error.log;

    # Gzip compression
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml text/javascript;

    # Static files caching
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)\$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }

    # Health check endpoint
    location /health {
        access_log off;
        return 200 "healthy\n";
        add_header Content-Type text/plain;
    }

    # API proxy to backend container
    location /api/ {
        proxy_pass http://127.0.0.1:5001/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
        proxy_read_timeout 300;
        proxy_connect_timeout 300;
        proxy_send_timeout 300;
    }

    # React app proxy to frontend container
    location / {
        proxy_pass http://127.0.0.1:3001;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
        proxy_read_timeout 300;
        proxy_connect_timeout 300;
        proxy_send_timeout 300;

        # Handle React Router (SPA fallback)
        try_files \$uri \$uri/ /index.html;
    }
}
EOF

# Create Nginx configuration for staging
echo "🌐 Creating Nginx configuration for staging ($STAGING_DOMAIN)..."
sudo tee /etc/nginx/sites-available/witchcityrope-staging > /dev/null << EOF
# WitchCityRope Staging Configuration
# Domain: $STAGING_DOMAIN

# HTTP server block - redirects to HTTPS
server {
    listen 80;
    listen [::]:80;
    server_name $STAGING_DOMAIN;

    # Let's Encrypt challenge directory
    location /.well-known/acme-challenge/ {
        root /var/www/html;
    }

    # Redirect all other HTTP requests to HTTPS
    location / {
        return 301 https://\$server_name\$request_uri;
    }
}

# HTTPS server block - staging configuration
server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name $STAGING_DOMAIN;

    # SSL Configuration (certificates will be configured by Certbot)
    ssl_certificate /etc/letsencrypt/live/$STAGING_DOMAIN/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/$STAGING_DOMAIN/privkey.pem;

    # SSL Security Settings
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES256-GCM-SHA384:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-RSA-AES256-SHA384:ECDHE-RSA-AES128-SHA256;
    ssl_prefer_server_ciphers off;
    ssl_session_timeout 1d;
    ssl_session_cache shared:SSL:50m;
    ssl_stapling on;
    ssl_stapling_verify on;

    # Security Headers (slightly relaxed for staging)
    add_header Strict-Transport-Security "max-age=3600" always;  # Shorter for staging
    add_header X-Frame-Options SAMEORIGIN always;  # Less restrictive for testing
    add_header X-Content-Type-Options nosniff always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;

    # Staging environment indicator
    add_header X-Environment "staging" always;

    # Logging
    access_log /var/log/nginx/witchcityrope/staging-access.log;
    error_log /var/log/nginx/witchcityrope/staging-error.log;

    # Gzip compression
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml text/javascript;

    # Static files caching (shorter for staging)
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)\$ {
        expires 1h;  # Shorter cache for staging
        add_header Cache-Control "public";
    }

    # Health check endpoint
    location /health {
        access_log off;
        return 200 "staging-healthy\n";
        add_header Content-Type text/plain;
    }

    # API proxy to staging backend container
    location /api/ {
        proxy_pass http://127.0.0.1:5002/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
        proxy_read_timeout 300;
        proxy_connect_timeout 300;
        proxy_send_timeout 300;
    }

    # React app proxy to staging frontend container
    location / {
        proxy_pass http://127.0.0.1:3002;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
        proxy_read_timeout 300;
        proxy_connect_timeout 300;
        proxy_send_timeout 300;

        # Handle React Router (SPA fallback)
        try_files \$uri \$uri/ /index.html;
    }

    # Basic auth for staging (optional security)
    # Uncomment and configure if you want password protection for staging
    # auth_basic "Staging Environment";
    # auth_basic_user_file /etc/nginx/.htpasswd;
}
EOF

# Enable the new sites
echo "🔗 Enabling Nginx sites..."
sudo ln -sf /etc/nginx/sites-available/witchcityrope-production /etc/nginx/sites-enabled/
sudo ln -sf /etc/nginx/sites-available/witchcityrope-staging /etc/nginx/sites-enabled/

# Disable default site to avoid conflicts
sudo rm -f /etc/nginx/sites-enabled/default

# Test Nginx configuration
echo "🧪 Testing Nginx configuration..."
sudo nginx -t

if [ $? -eq 0 ]; then
    echo "✅ Nginx configuration test passed"
else
    echo "❌ Nginx configuration test failed"
    exit 1
fi

# Reload Nginx to apply new configuration
sudo systemctl reload nginx

# Create the Let's Encrypt webroot directory
sudo mkdir -p /var/www/html/.well-known/acme-challenge
sudo chown -R www-data:www-data /var/www/html

# Function to obtain SSL certificate
obtain_certificate() {
    local domain=$1
    local env_type=$2

    echo "🔒 Obtaining SSL certificate for $domain ($env_type)..."

    # Use certbot to obtain certificate
    sudo certbot certonly \
        --webroot \
        --webroot-path=/var/www/html \
        --email "$EMAIL" \
        --agree-tos \
        --non-interactive \
        --domains "$domain"

    if [ $? -eq 0 ]; then
        echo "✅ SSL certificate obtained for $domain"
        return 0
    else
        echo "❌ Failed to obtain SSL certificate for $domain"
        return 1
    fi
}

# Obtain SSL certificates
CERT_ERRORS=false

if ! obtain_certificate "$PRODUCTION_DOMAIN" "production"; then
    CERT_ERRORS=true
fi

if ! obtain_certificate "$STAGING_DOMAIN" "staging"; then
    CERT_ERRORS=true
fi

if [ "$CERT_ERRORS" = true ]; then
    echo ""
    echo "⚠️  Some SSL certificates could not be obtained"
    echo "   This is likely due to DNS configuration issues"
    echo "   You can retry SSL setup later once DNS is properly configured"
    echo ""
    read -p "Continue with setup anyway? (yes/no): " continue_ssl

    if [ "$continue_ssl" != "yes" ]; then
        echo "SSL setup cancelled"
        exit 1
    fi
fi

# Test and reload Nginx with SSL
echo "🔄 Reloading Nginx with SSL configuration..."
sudo nginx -t && sudo systemctl reload nginx

# Set up automatic certificate renewal
echo "🔄 Setting up automatic SSL certificate renewal..."

# Create renewal script
cat > /opt/witchcityrope/renew-certificates.sh << 'EOF'
#!/bin/bash
# SSL Certificate Renewal Script for WitchCityRope
# Renews Let's Encrypt certificates and reloads Nginx

set -euo pipefail

LOG_FILE="/var/log/witchcityrope/ssl-renewal.log"
exec 1> >(tee -a "$LOG_FILE")
exec 2>&1

echo "🔒 SSL Certificate Renewal Started - $(date)"
echo "================================================"

# Renew certificates
echo "🔄 Checking for certificate renewals..."
if certbot renew --quiet; then
    echo "✅ Certificate renewal check completed"

    # Test nginx configuration
    if nginx -t; then
        echo "✅ Nginx configuration test passed"

        # Reload nginx to use new certificates
        systemctl reload nginx
        echo "✅ Nginx reloaded successfully"
    else
        echo "❌ Nginx configuration test failed after renewal"
    fi
else
    echo "❌ Certificate renewal failed"
fi

echo "================================================"
echo "SSL Certificate Renewal Completed - $(date)"
echo ""
EOF

chmod +x /opt/witchcityrope/renew-certificates.sh

# Add certificate renewal to cron (twice daily)
echo "⏰ Setting up automated certificate renewal..."
(crontab -l 2>/dev/null; echo "0 2,14 * * * /opt/witchcityrope/renew-certificates.sh") | crontab -

# Create SSL status check script
cat > /opt/witchcityrope/check-ssl.sh << 'EOF'
#!/bin/bash
# SSL Status Check Script for WitchCityRope
# Checks SSL certificate status and expiration

set -euo pipefail

echo "🔒 SSL Certificate Status Check - $(date)"
echo "==============================================="

# Function to check certificate
check_cert() {
    local domain=$1
    local env_type=$2

    echo "🔍 Checking SSL certificate for $domain ($env_type)..."

    if [ -f "/etc/letsencrypt/live/$domain/fullchain.pem" ]; then
        # Get certificate expiration date
        expiry=$(openssl x509 -enddate -noout -in "/etc/letsencrypt/live/$domain/fullchain.pem" | cut -d= -f2)
        expiry_epoch=$(date -d "$expiry" +%s)
        current_epoch=$(date +%s)
        days_until_expiry=$(( (expiry_epoch - current_epoch) / 86400 ))

        echo "✅ Certificate exists for $domain"
        echo "   Expires: $expiry"
        echo "   Days until expiry: $days_until_expiry"

        if [ $days_until_expiry -lt 30 ]; then
            echo "⚠️  Certificate expires in less than 30 days!"
        fi

        # Test SSL connectivity
        if timeout 10 openssl s_client -connect "$domain:443" -servername "$domain" < /dev/null > /dev/null 2>&1; then
            echo "✅ SSL connectivity test passed"
        else
            echo "❌ SSL connectivity test failed"
        fi
    else
        echo "❌ No certificate found for $domain"
    fi
    echo ""
}

# Load domain information from nginx configs
PRODUCTION_DOMAIN=$(grep -h "server_name" /etc/nginx/sites-available/witchcityrope-production | head -1 | awk '{print $2}' | sed 's/;//g')
STAGING_DOMAIN=$(grep -h "server_name" /etc/nginx/sites-available/witchcityrope-staging | head -1 | awk '{print $2}' | sed 's/;//g')

# Check certificates
check_cert "$PRODUCTION_DOMAIN" "production"
check_cert "$STAGING_DOMAIN" "staging"

echo "==============================================="
echo "SSL Certificate Status Check Completed - $(date)"
EOF

chmod +x /opt/witchcityrope/check-ssl.sh

# Run initial SSL status check
echo "🔍 Running initial SSL status check..."
/opt/witchcityrope/check-ssl.sh

# Create firewall rules for HTTPS
echo "🔥 Updating firewall rules for HTTPS..."
sudo ufw allow 'Nginx Full'
sudo ufw reload

# Update environment files with HTTPS URLs
echo "📝 Updating environment files with HTTPS URLs..."

# Update production environment
sudo sed -i "s|CORS__AllowedOrigins=.*|CORS__AllowedOrigins=https://$PRODUCTION_DOMAIN|g" /opt/witchcityrope/production/.env.production
sudo sed -i "s|Security__AllowedHosts=.*|Security__AllowedHosts=$PRODUCTION_DOMAIN|g" /opt/witchcityrope/production/.env.production

# Update staging environment
sudo sed -i "s|CORS__AllowedOrigins=.*|CORS__AllowedOrigins=https://$STAGING_DOMAIN|g" /opt/witchcityrope/staging/.env.staging
sudo sed -i "s|Security__AllowedHosts=.*|Security__AllowedHosts=$STAGING_DOMAIN|g" /opt/witchcityrope/staging/.env.staging

# Final summary
echo ""
echo "✅ SSL setup completed successfully!"
echo ""
echo "📋 Setup Summary:"
echo "   • Production domain: $PRODUCTION_DOMAIN"
echo "   • Staging domain: $STAGING_DOMAIN"
echo "   • SSL certificates: $([ "$CERT_ERRORS" = false ] && echo "✅ Obtained" || echo "⚠️  Some failed")"
echo "   • Nginx configurations: Created and enabled"
echo "   • Automatic renewal: Configured (twice daily)"
echo "   • Firewall: Updated for HTTPS"
echo "   • Environment files: Updated with HTTPS URLs"
echo ""
echo "📁 Important Files:"
echo "   • Production config: /etc/nginx/sites-available/witchcityrope-production"
echo "   • Staging config: /etc/nginx/sites-available/witchcityrope-staging"
echo "   • SSL renewal script: /opt/witchcityrope/renew-certificates.sh"
echo "   • SSL check script: /opt/witchcityrope/check-ssl.sh"
echo ""
echo "🔧 Useful Commands:"
echo "   • Check SSL status: /opt/witchcityrope/check-ssl.sh"
echo "   • Renew certificates: /opt/witchcityrope/renew-certificates.sh"
echo "   • Test Nginx: sudo nginx -t"
echo "   • Reload Nginx: sudo systemctl reload nginx"
echo ""
echo "🌐 Your sites should be available at:"
echo "   • Production: https://$PRODUCTION_DOMAIN"
echo "   • Staging: https://$STAGING_DOMAIN"
echo ""
echo "🚨 NEXT STEPS:"
echo "   1. Verify your domains resolve to this server IP: $(curl -s ifconfig.me 2>/dev/null || echo "Unable to detect")"
echo "   2. Test HTTPS connectivity to both domains"
echo "   3. Run next script: 05-deploy-application.sh"
echo ""
echo "📅 Completed at: $(date)"