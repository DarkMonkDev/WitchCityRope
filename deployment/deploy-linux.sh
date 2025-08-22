#!/bin/bash

# WitchCityRope Linux Deployment Script
# This script deploys WitchCityRope to a Linux VPS environment
# Requirements: Ubuntu 20.04+ or Debian 11+, Docker, Docker Compose, nginx

set -euo pipefail

# Script configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOG_FILE="deployment-$(date +%Y%m%d-%H%M%S).log"
START_TIME=$(date +%s)

# Color output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

log_success() {
    echo -e "${GREEN}✓${NC} $1" | tee -a "$LOG_FILE"
}

log_info() {
    echo -e "${BLUE}ℹ${NC} $1" | tee -a "$LOG_FILE"
}

log_warning() {
    echo -e "${YELLOW}⚠${NC} $1" | tee -a "$LOG_FILE"
}

log_error() {
    echo -e "${RED}✗${NC} $1" | tee -a "$LOG_FILE"
}

# Usage function
usage() {
    cat << EOF
Usage: $0 [OPTIONS]

Deploy WitchCityRope to Linux VPS

OPTIONS:
    -e, --environment ENV      Environment to deploy (required: staging|production)
    -p, --path PATH           Deployment path (default: /opt/witchcityrope)
    -b, --backup-path PATH    Backup path (default: /var/backups/witchcityrope)
    -c, --config FILE         Configuration file (default: ./deployment-config.json)
    --skip-backup             Skip backup step
    --skip-health-check       Skip health check after deployment
    --force                   Force deployment even if checks fail
    -h, --help               Show this help message

EXAMPLES:
    $0 -e production
    $0 -e staging -p /home/deploy/witchcityrope --skip-backup
    $0 -e production --force
EOF
}

# Parse command line arguments
ENVIRONMENT=""
DEPLOYMENT_PATH="/opt/witchcityrope"
BACKUP_PATH="/var/backups/witchcityrope"
CONFIG_FILE="./deployment-config.json"
SKIP_BACKUP=false
SKIP_HEALTH_CHECK=false
FORCE=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -p|--path)
            DEPLOYMENT_PATH="$2"
            shift 2
            ;;
        -b|--backup-path)
            BACKUP_PATH="$2"
            shift 2
            ;;
        -c|--config)
            CONFIG_FILE="$2"
            shift 2
            ;;
        --skip-backup)
            SKIP_BACKUP=true
            shift
            ;;
        --skip-health-check)
            SKIP_HEALTH_CHECK=true
            shift
            ;;
        --force)
            FORCE=true
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            log_error "Unknown option: $1"
            usage
            exit 1
            ;;
    esac
done

# Validate required arguments
if [[ -z "$ENVIRONMENT" ]]; then
    log_error "Environment is required"
    usage
    exit 1
fi

if [[ "$ENVIRONMENT" != "staging" && "$ENVIRONMENT" != "production" ]]; then
    log_error "Invalid environment: $ENVIRONMENT (must be staging or production)"
    exit 1
fi

# Load configuration
load_config() {
    log_info "Loading deployment configuration from $CONFIG_FILE"
    
    if [[ ! -f "$CONFIG_FILE" ]]; then
        log_error "Configuration file not found: $CONFIG_FILE"
        exit 1
    fi
    
    # Parse JSON config using jq
    if ! command -v jq &> /dev/null; then
        log_error "jq is required but not installed. Please install it first."
        exit 1
    fi
    
    # Load configuration values
    APP_NAME=$(jq -r '.AppName' "$CONFIG_FILE")
    API_PORT=$(jq -r '.ApiPort' "$CONFIG_FILE")
    WEB_PORT=$(jq -r '.WebPort' "$CONFIG_FILE")
    DOMAIN=$(jq -r ".Environments.$ENVIRONMENT.Domain" "$CONFIG_FILE")
    SSL_EMAIL=$(jq -r ".Environments.$ENVIRONMENT.SSLEmail" "$CONFIG_FILE")
    
    log_success "Configuration loaded successfully"
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    local prereqs=(
        "docker:Docker"
        "docker-compose:Docker Compose"
        "nginx:Nginx"
        "git:Git"
        "jq:JSON processor"
        "certbot:Certbot (for SSL)"
    )
    
    local failed=false
    
    for prereq in "${prereqs[@]}"; do
        IFS=':' read -r cmd name <<< "$prereq"
        printf "Checking %s... " "$name"
        if command -v "$cmd" &> /dev/null; then
            echo -e "${GREEN}OK${NC}"
        else
            echo -e "${RED}MISSING${NC}"
            failed=true
        fi
    done
    
    # Check Docker daemon
    printf "Checking Docker daemon... "
    if docker info &> /dev/null; then
        echo -e "${GREEN}OK${NC}"
    else
        echo -e "${RED}NOT RUNNING${NC}"
        failed=true
    fi
    
    if [[ "$failed" == "true" ]] && [[ "$FORCE" != "true" ]]; then
        log_error "Prerequisites check failed. Install missing components or use --force"
        exit 1
    fi
    
    log_success "Prerequisites check completed"
}

# Create backup
create_backup() {
    if [[ "$SKIP_BACKUP" == "true" ]]; then
        log_warning "Skipping backup as requested"
        return
    fi
    
    log_info "Creating backup of current deployment..."
    
    if [[ ! -d "$DEPLOYMENT_PATH" ]]; then
        log_info "No existing deployment found, skipping backup"
        return
    fi
    
    local backup_dir="$BACKUP_PATH/backup-$(date +%Y%m%d-%H%M%S)"
    mkdir -p "$backup_dir"
    
    # Backup application files
    log_info "Backing up application files..."
    cp -r "$DEPLOYMENT_PATH"/* "$backup_dir/" 2>/dev/null || true
    
    # Backup docker volumes
    log_info "Backing up Docker volumes..."
    local volumes_backup="$backup_dir/volumes"
    mkdir -p "$volumes_backup"
    
    # Export volume data
    docker run --rm \
        -v witchcityrope_data:/data:ro \
        -v "$volumes_backup":/backup \
        alpine tar czf /backup/data.tar.gz -C /data . 2>/dev/null || true
    
    # Backup nginx configuration
    if [[ -f "/etc/nginx/sites-available/witchcityrope" ]]; then
        cp /etc/nginx/sites-available/witchcityrope "$backup_dir/nginx-config" || true
    fi
    
    # Create backup manifest
    cat > "$backup_dir/manifest.json" << EOF
{
    "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
    "environment": "$ENVIRONMENT",
    "deployment_path": "$DEPLOYMENT_PATH",
    "backup_path": "$backup_dir",
    "docker_images": $(docker images --format '{{json .}}' | jq -s .)
}
EOF
    
    log_success "Backup created at: $backup_dir"
}

# Stop application
stop_application() {
    log_info "Stopping application..."
    
    cd "$DEPLOYMENT_PATH" 2>/dev/null || true
    
    # Stop Docker containers
    if [[ -f "docker-compose.yml" ]]; then
        log_info "Stopping Docker containers..."
        docker-compose down --remove-orphans || true
    fi
    
    # Stop any systemd services
    if systemctl is-active --quiet witchcityrope-api; then
        log_info "Stopping witchcityrope-api service..."
        sudo systemctl stop witchcityrope-api
    fi
    
    if systemctl is-active --quiet witchcityrope-web; then
        log_info "Stopping witchcityrope-web service..."
        sudo systemctl stop witchcityrope-web
    fi
    
    log_success "Application stopped"
}

# Deploy application
deploy_application() {
    log_info "Deploying application files..."
    
    # Create deployment directory
    sudo mkdir -p "$DEPLOYMENT_PATH"
    sudo chown -R "$USER:$USER" "$DEPLOYMENT_PATH"
    
    # Clone or update repository
    if [[ -d "$DEPLOYMENT_PATH/.git" ]]; then
        log_info "Updating existing repository..."
        cd "$DEPLOYMENT_PATH"
        git fetch origin
        git checkout "$ENVIRONMENT"
        git pull origin "$ENVIRONMENT"
    else
        log_info "Cloning repository..."
        git clone --branch "$ENVIRONMENT" https://github.com/yourusername/WitchCityRope.git "$DEPLOYMENT_PATH"
        cd "$DEPLOYMENT_PATH"
    fi
    
    # Copy environment configuration
    log_info "Applying environment configuration..."
    if [[ -f "./configs/$ENVIRONMENT/.env" ]]; then
        cp "./configs/$ENVIRONMENT/.env" .env
    fi
    
    # Create required directories
    mkdir -p data logs uploads
    
    # Set permissions
    sudo chown -R www-data:www-data data logs uploads
    sudo chmod -R 755 data logs uploads
    
    log_success "Application files deployed"
}

# Build and deploy with Docker
deploy_docker() {
    log_info "Building and deploying with Docker..."
    
    cd "$DEPLOYMENT_PATH"
    
    # Create docker-compose override for environment
    cat > "docker-compose.$ENVIRONMENT.yml" << EOF
version: '3.8'

services:
  api:
    environment:
      - ASPNETCORE_ENVIRONMENT=$ENVIRONMENT
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/witchcityrope.db
    restart: always
    
  web:
    environment:
      - ASPNETCORE_ENVIRONMENT=$ENVIRONMENT
      - ApiBaseUrl=http://api:8080/
      - ApiBaseUrlExternal=https://$DOMAIN/api/
    restart: always
    
  nginx:
    image: nginx:alpine
    container_name: witchcity-nginx
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./nginx/conf.d:/etc/nginx/conf.d:ro
      - /etc/letsencrypt:/etc/letsencrypt:ro
      - ./logs/nginx:/var/log/nginx
    depends_on:
      - api
      - web
    networks:
      - witchcity-network
    restart: always
EOF
    
    # Build images
    log_info "Building Docker images..."
    docker-compose -f docker-compose.yml -f "docker-compose.$ENVIRONMENT.yml" build
    
    # Run database migrations
    log_info "Running database migrations..."
    docker-compose -f docker-compose.yml -f "docker-compose.$ENVIRONMENT.yml" run --rm api dotnet ef database update
    
    # Start services
    log_info "Starting Docker services..."
    docker-compose -f docker-compose.yml -f "docker-compose.$ENVIRONMENT.yml" up -d
    
    log_success "Docker deployment completed"
}

# Configure nginx
configure_nginx() {
    log_info "Configuring nginx..."
    
    # Create nginx configuration directory
    sudo mkdir -p "$DEPLOYMENT_PATH/nginx/conf.d"
    
    # Create nginx configuration
    cat > "$DEPLOYMENT_PATH/nginx/conf.d/witchcityrope.conf" << EOF
upstream api_backend {
    server api:8080;
}

upstream web_backend {
    server web:8080;
}

server {
    listen 80;
    server_name $DOMAIN www.$DOMAIN;
    
    # Redirect to HTTPS
    location / {
        return 301 https://\$server_name\$request_uri;
    }
    
    # Let's Encrypt challenge
    location /.well-known/acme-challenge/ {
        root /var/www/certbot;
    }
}

server {
    listen 443 ssl http2;
    server_name $DOMAIN www.$DOMAIN;
    
    # SSL configuration
    ssl_certificate /etc/letsencrypt/live/$DOMAIN/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/$DOMAIN/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;
    
    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;
    add_header Content-Security-Policy "default-src 'self' https:; script-src 'self' 'unsafe-inline' 'unsafe-eval' https:; style-src 'self' 'unsafe-inline' https:;" always;
    
    # API proxy
    location /api/ {
        proxy_pass http://api_backend/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
        
        # Timeouts
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }
    
    # Web app proxy
    location / {
        proxy_pass http://web_backend;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
        
        # WebSocket support
        proxy_set_header Connection "upgrade";
    }
    
    # Static files
    location /uploads/ {
        alias /app/uploads/;
        expires 30d;
        add_header Cache-Control "public, immutable";
    }
    
    # Compression
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_types text/plain text/css text/xml text/javascript application/javascript application/xml+rss application/json;
}
EOF
    
    # Create main nginx configuration
    cat > "$DEPLOYMENT_PATH/nginx/nginx.conf" << 'EOF'
user nginx;
worker_processes auto;
error_log /var/log/nginx/error.log warn;
pid /var/run/nginx.pid;

events {
    worker_connections 4096;
    use epoll;
    multi_accept on;
}

http {
    include /etc/nginx/mime.types;
    default_type application/octet-stream;
    
    log_format main '$remote_addr - $remote_user [$time_local] "$request" '
                    '$status $body_bytes_sent "$http_referer" '
                    '"$http_user_agent" "$http_x_forwarded_for"';
    
    access_log /var/log/nginx/access.log main;
    
    sendfile on;
    tcp_nopush on;
    tcp_nodelay on;
    keepalive_timeout 65;
    types_hash_max_size 2048;
    client_max_body_size 50M;
    
    # Rate limiting
    limit_req_zone $binary_remote_addr zone=general:10m rate=10r/s;
    limit_req_zone $binary_remote_addr zone=api:10m rate=30r/s;
    
    include /etc/nginx/conf.d/*.conf;
}
EOF
    
    log_success "Nginx configuration completed"
}

# Setup SSL certificate
setup_ssl() {
    log_info "Setting up SSL certificate..."
    
    # Check if certificate already exists
    if [[ -f "/etc/letsencrypt/live/$DOMAIN/fullchain.pem" ]]; then
        log_info "SSL certificate already exists, skipping..."
        return
    fi
    
    # Install certbot if not present
    if ! command -v certbot &> /dev/null; then
        log_info "Installing certbot..."
        sudo apt-get update
        sudo apt-get install -y certbot
    fi
    
    # Stop nginx temporarily
    docker-compose -f docker-compose.yml -f "docker-compose.$ENVIRONMENT.yml" stop nginx 2>/dev/null || true
    
    # Get certificate
    sudo certbot certonly \
        --standalone \
        --non-interactive \
        --agree-tos \
        --email "$SSL_EMAIL" \
        -d "$DOMAIN" \
        -d "www.$DOMAIN"
    
    # Restart nginx
    docker-compose -f docker-compose.yml -f "docker-compose.$ENVIRONMENT.yml" start nginx
    
    # Setup auto-renewal
    setup_ssl_renewal
    
    log_success "SSL certificate setup completed"
}

# Setup SSL auto-renewal
setup_ssl_renewal() {
    log_info "Setting up SSL auto-renewal..."
    
    # Create renewal script
    cat > "$DEPLOYMENT_PATH/renew-ssl.sh" << 'EOF'
#!/bin/bash
certbot renew --quiet --no-self-upgrade --post-hook "docker-compose -f /opt/witchcityrope/docker-compose.yml -f /opt/witchcityrope/docker-compose.production.yml restart nginx"
EOF
    
    chmod +x "$DEPLOYMENT_PATH/renew-ssl.sh"
    
    # Add cron job
    (crontab -l 2>/dev/null; echo "0 2 * * * $DEPLOYMENT_PATH/renew-ssl.sh >> /var/log/letsencrypt-renewal.log 2>&1") | crontab -
    
    log_success "SSL auto-renewal configured"
}

# Run health checks
run_health_checks() {
    if [[ "$SKIP_HEALTH_CHECK" == "true" ]]; then
        log_warning "Skipping health checks as requested"
        return
    fi
    
    log_info "Running deployment health checks..."
    
    # Wait for services to start
    sleep 10
    
    # Define health check endpoints
    declare -A checks=(
        ["API Health"]="http://localhost:$API_PORT/health"
        ["Web Health"]="http://localhost:$WEB_PORT/health"
        ["API Swagger"]="http://localhost:$API_PORT/swagger"
    )
    
    local failed=false
    
    for check_name in "${!checks[@]}"; do
        local url="${checks[$check_name]}"
        printf "Checking %s... " "$check_name"
        
        if curl -sf "$url" > /dev/null; then
            echo -e "${GREEN}OK${NC}"
        else
            echo -e "${RED}FAILED${NC}"
            failed=true
        fi
    done
    
    # Check Docker containers
    printf "Checking Docker containers... "
    local unhealthy=$(docker ps --filter "health=unhealthy" --format "{{.Names}}" | grep witchcity || true)
    if [[ -z "$unhealthy" ]]; then
        echo -e "${GREEN}OK${NC}"
    else
        echo -e "${RED}UNHEALTHY: $unhealthy${NC}"
        failed=true
    fi
    
    if [[ "$failed" == "true" ]] && [[ "$FORCE" != "true" ]]; then
        log_error "Health checks failed! Use --force to ignore."
        exit 1
    fi
    
    log_success "Health checks completed"
}

# Create systemd service (optional)
create_systemd_service() {
    log_info "Creating systemd service..."
    
    sudo tee /etc/systemd/system/witchcityrope.service > /dev/null << EOF
[Unit]
Description=WitchCityRope Application
After=docker.service
Requires=docker.service

[Service]
Type=oneshot
RemainAfterExit=yes
WorkingDirectory=$DEPLOYMENT_PATH
ExecStart=/usr/local/bin/docker-compose -f docker-compose.yml -f docker-compose.$ENVIRONMENT.yml up -d
ExecStop=/usr/local/bin/docker-compose -f docker-compose.yml -f docker-compose.$ENVIRONMENT.yml down
ExecReload=/usr/local/bin/docker-compose -f docker-compose.yml -f docker-compose.$ENVIRONMENT.yml restart
StandardOutput=journal

[Install]
WantedBy=multi-user.target
EOF
    
    sudo systemctl daemon-reload
    sudo systemctl enable witchcityrope.service
    
    log_success "Systemd service created"
}

# Main deployment function
main() {
    log_info "Starting WitchCityRope deployment for environment: $ENVIRONMENT"
    log_info "Deployment path: $DEPLOYMENT_PATH"
    
    # Load configuration
    load_config
    
    # Check prerequisites
    check_prerequisites
    
    # Create backup
    create_backup
    
    # Stop application
    stop_application
    
    # Deploy application
    deploy_application
    
    # Configure nginx
    configure_nginx
    
    # Deploy with Docker
    deploy_docker
    
    # Setup SSL
    if [[ "$ENVIRONMENT" == "production" ]]; then
        setup_ssl
    fi
    
    # Create systemd service
    create_systemd_service
    
    # Run health checks
    run_health_checks
    
    # Calculate duration
    local end_time=$(date +%s)
    local duration=$((end_time - START_TIME))
    local minutes=$((duration / 60))
    local seconds=$((duration % 60))
    
    log_success "Deployment completed successfully in ${minutes}m ${seconds}s"
    
    # Display summary
    echo -e "\n${YELLOW}Deployment Summary:${NC}"
    echo "Environment: $ENVIRONMENT"
    echo "Domain: $DOMAIN"
    echo "API URL: http://localhost:$API_PORT"
    echo "Web URL: http://localhost:$WEB_PORT"
    echo "Log file: $LOG_FILE"
    
    if [[ "$ENVIRONMENT" == "production" ]]; then
        echo "Production URL: https://$DOMAIN"
    fi
}

# Run main function
main