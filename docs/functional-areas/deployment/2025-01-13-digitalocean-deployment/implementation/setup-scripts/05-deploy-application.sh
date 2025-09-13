#!/bin/bash
# Application Deployment Script - Deploy WitchCityRope containers
# Run this script as the witchcity user
# Usage: ./05-deploy-application.sh

set -euo pipefail

echo "🚀 Deploying WitchCityRope application containers..."
echo "📅 Started at: $(date)"

# Check if running as correct user
if [ "$USER" = "root" ]; then
    echo "❌ This script should not be run as root"
    echo "   Please run as witchcity user: ./05-deploy-application.sh"
    exit 1
fi

# Configuration
DOCKER_REGISTRY="registry.digitalocean.com/witchcityrope"
API_IMAGE_TAG="latest"
WEB_IMAGE_TAG="latest"

# Check if Docker is running and accessible
echo "🐳 Checking Docker status..."
if ! docker ps > /dev/null 2>&1; then
    echo "❌ Docker is not accessible. Please ensure:"
    echo "   1. Docker service is running: sudo systemctl status docker"
    echo "   2. User is in docker group: groups $USER"
    echo "   3. You've logged out and back in after adding to docker group"
    exit 1
fi

echo "✅ Docker is accessible"

# Create Docker Compose files
echo "📝 Creating Docker Compose configurations..."

# Production Docker Compose
cat > /opt/witchcityrope/production/docker-compose.production.yml << 'EOF'
# WitchCityRope Production Docker Compose
# Multi-container deployment for production environment

version: '3.8'

networks:
  witchcity-production:
    driver: bridge
    ipam:
      config:
        - subnet: 172.26.0.0/16

volumes:
  api_uploads:
    driver: local
  web_dist:
    driver: local
  redis_data:
    driver: local

services:
  # Production API Service
  api:
    image: ${API_IMAGE:-registry.digitalocean.com/witchcityrope/witchcityrope-api:latest}
    container_name: witchcity-api-prod
    restart: unless-stopped
    networks:
      - witchcity-production
    ports:
      - "5001:8080"  # Internal port mapping for Nginx proxy
    volumes:
      - api_uploads:/app/uploads
      - /var/log/witchcityrope/production:/app/logs
    env_file:
      - .env.production
    environment:
      - ASPNETCORE_URLS=http://+:8080
      - ASPNETCORE_ENVIRONMENT=Production
    depends_on:
      - redis
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
    deploy:
      resources:
        limits:
          memory: 3G
          cpus: '2.0'
        reservations:
          memory: 1G
          cpus: '0.5'
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

  # Production Web Service
  web:
    image: ${WEB_IMAGE:-registry.digitalocean.com/witchcityrope/witchcityrope-web:latest}
    container_name: witchcity-web-prod
    restart: unless-stopped
    networks:
      - witchcity-production
    ports:
      - "3001:3000"  # Internal port mapping for Nginx proxy
    volumes:
      - web_dist:/app/dist
      - /var/log/witchcityrope/production:/app/logs
    environment:
      - NODE_ENV=production
      - VITE_API_BASE_URL=/api
      - VITE_APP_ENVIRONMENT=production
    depends_on:
      api:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:3000/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 30s
    deploy:
      resources:
        limits:
          memory: 1G
          cpus: '0.5'
        reservations:
          memory: 256M
          cpus: '0.1'
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

  # Redis Cache Service
  redis:
    image: redis:7-alpine
    container_name: witchcity-redis-prod
    restart: unless-stopped
    networks:
      - witchcity-production
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    command: [
      "redis-server",
      "--appendonly", "yes",
      "--maxmemory", "512mb",
      "--maxmemory-policy", "allkeys-lru"
    ]
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 3s
      retries: 3
    deploy:
      resources:
        limits:
          memory: 512M
          cpus: '0.25'
    logging:
      driver: "json-file"
      options:
        max-size: "5m"
        max-file: "2"
EOF

# Staging Docker Compose
cat > /opt/witchcityrope/staging/docker-compose.staging.yml << 'EOF'
# WitchCityRope Staging Docker Compose
# Multi-container deployment for staging environment

version: '3.8'

networks:
  witchcity-staging:
    driver: bridge
    ipam:
      config:
        - subnet: 172.27.0.0/16

volumes:
  api_uploads_staging:
    driver: local
  web_dist_staging:
    driver: local

services:
  # Staging API Service
  api:
    image: ${API_IMAGE:-registry.digitalocean.com/witchcityrope/witchcityrope-api:latest}
    container_name: witchcity-api-staging
    restart: unless-stopped
    networks:
      - witchcity-staging
    ports:
      - "5002:8080"  # Different port for staging
    volumes:
      - api_uploads_staging:/app/uploads
      - /var/log/witchcityrope/staging:/app/logs
    env_file:
      - .env.staging
    environment:
      - ASPNETCORE_URLS=http://+:8080
      - ASPNETCORE_ENVIRONMENT=Staging
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
    deploy:
      resources:
        limits:
          memory: 1G
          cpus: '0.5'
        reservations:
          memory: 256M
          cpus: '0.1'
    logging:
      driver: "json-file"
      options:
        max-size: "5m"
        max-file: "2"

  # Staging Web Service
  web:
    image: ${WEB_IMAGE:-registry.digitalocean.com/witchcityrope/witchcityrope-web:latest}
    container_name: witchcity-web-staging
    restart: unless-stopped
    networks:
      - witchcity-staging
    ports:
      - "3002:3000"  # Different port for staging
    volumes:
      - web_dist_staging:/app/dist
      - /var/log/witchcityrope/staging:/app/logs
    environment:
      - NODE_ENV=staging
      - VITE_API_BASE_URL=/api
      - VITE_APP_ENVIRONMENT=staging
    depends_on:
      api:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:3000/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 30s
    deploy:
      resources:
        limits:
          memory: 512M
          cpus: '0.25'
        reservations:
          memory: 128M
          cpus: '0.05'
    logging:
      driver: "json-file"
      options:
        max-size: "5m"
        max-file: "2"
EOF

echo "✅ Docker Compose files created"

# Function to build local images (for initial deployment)
build_local_images() {
    echo "🔨 Building local Docker images for initial deployment..."

    # Check if source code exists
    if [ ! -d "/opt/witchcityrope/source" ]; then
        echo "📥 Cloning WitchCityRope repository..."
        git clone https://github.com/DarkMonkDev/WitchCityRope.git /opt/witchcityrope/source
        cd /opt/witchcityrope/source
    else
        echo "📥 Updating WitchCityRope repository..."
        cd /opt/witchcityrope/source
        git pull origin main
    fi

    # Build API image
    echo "🔨 Building API image..."
    docker build -f apps/api/Dockerfile.production -t $DOCKER_REGISTRY/witchcityrope-api:$API_IMAGE_TAG .

    # Build Web image
    echo "🔨 Building Web image..."
    docker build -f apps/web/Dockerfile.production -t $DOCKER_REGISTRY/witchcityrope-web:$WEB_IMAGE_TAG ./apps/web

    echo "✅ Local images built successfully"
}

# Function to deploy environment
deploy_environment() {
    local env=$1
    local compose_file=$2

    echo "🚀 Deploying $env environment..."

    cd "/opt/witchcityrope/$env"

    # Pull latest images (or use local if registry not available)
    echo "📥 Pulling Docker images..."
    if docker pull "$DOCKER_REGISTRY/witchcityrope-api:$API_IMAGE_TAG" 2>/dev/null && \
       docker pull "$DOCKER_REGISTRY/witchcityrope-web:$WEB_IMAGE_TAG" 2>/dev/null; then
        echo "✅ Images pulled from registry"
    else
        echo "⚠️  Could not pull from registry, using local images"
        build_local_images
    fi

    # Deploy containers
    echo "🐳 Starting $env containers..."
    API_IMAGE="$DOCKER_REGISTRY/witchcityrope-api:$API_IMAGE_TAG" \
    WEB_IMAGE="$DOCKER_REGISTRY/witchcityrope-web:$WEB_IMAGE_TAG" \
    docker-compose -f "$compose_file" up -d

    # Wait for services to be healthy
    echo "⏳ Waiting for $env services to become healthy..."
    local max_wait=120
    local wait_time=0

    while [ $wait_time -lt $max_wait ]; do
        if docker-compose -f "$compose_file" ps --services --filter "status=running" | wc -l | grep -q "2"; then
            echo "✅ $env services are running"
            break
        fi
        sleep 5
        wait_time=$((wait_time + 5))
        echo "   Waiting... ($wait_time/$max_wait seconds)"
    done

    if [ $wait_time -ge $max_wait ]; then
        echo "❌ $env services failed to start within $max_wait seconds"
        echo "📋 Container status:"
        docker-compose -f "$compose_file" ps
        echo "📋 Container logs:"
        docker-compose -f "$compose_file" logs --tail=20
        return 1
    fi

    echo "✅ $env environment deployed successfully"
}

# Function to test deployment
test_deployment() {
    local env=$1
    local port=$2

    echo "🧪 Testing $env deployment..."

    # Test API health endpoint
    if curl -f "http://localhost:$port/health" > /dev/null 2>&1; then
        echo "✅ $env API health check passed"
    else
        echo "❌ $env API health check failed"
        return 1
    fi

    # Test container status
    local running_containers=$(docker ps --filter "name=witchcity-.*-$(echo $env | sed 's/staging/staging/' | sed 's/production/prod/')" --format "{{.Names}}" | wc -l)
    if [ "$running_containers" -ge 2 ]; then
        echo "✅ $env containers are running ($running_containers containers)"
    else
        echo "❌ $env containers not running properly"
        return 1
    fi
}

# Deploy staging environment first
echo "🎯 Deploying staging environment..."
deploy_environment "staging" "docker-compose.staging.yml"
test_deployment "staging" "5002"

# Deploy production environment
echo "🎯 Deploying production environment..."
deploy_environment "production" "docker-compose.production.yml"
test_deployment "production" "5001"

# Create management scripts
echo "🔧 Creating container management scripts..."

# Create status script
cat > /opt/witchcityrope/status.sh << 'EOF'
#!/bin/bash
# WitchCityRope Application Status Script
# Shows status of all containers and services

set -euo pipefail

echo "📊 WitchCityRope Application Status - $(date)"
echo "============================================"

# Docker service status
echo "🐳 Docker Service Status:"
if systemctl is-active --quiet docker; then
    echo "   ✅ Docker: Running"
else
    echo "   ❌ Docker: Not running"
fi

# Container status
echo ""
echo "📦 Container Status:"
docker ps --filter "name=witchcity-" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

# Health checks
echo ""
echo "🔍 Health Checks:"
for env in production staging; do
    port=$([ "$env" = "production" ] && echo "5001" || echo "5002")
    web_port=$([ "$env" = "production" ] && echo "3001" || echo "3002")

    echo "  $env environment:"

    # API health
    if curl -f -s "http://localhost:$port/health" > /dev/null 2>&1; then
        echo "    ✅ API ($port): Healthy"
    else
        echo "    ❌ API ($port): Unhealthy"
    fi

    # Web health (basic connectivity)
    if curl -f -s "http://localhost:$web_port" > /dev/null 2>&1; then
        echo "    ✅ Web ($web_port): Accessible"
    else
        echo "    ❌ Web ($web_port): Inaccessible"
    fi
done

# Resource usage
echo ""
echo "💻 Resource Usage:"
docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}" $(docker ps --filter "name=witchcity-" -q)

echo ""
echo "============================================"
echo "Status check completed at $(date)"
EOF

chmod +x /opt/witchcityrope/status.sh

# Create restart script
cat > /opt/witchcityrope/restart.sh << 'EOF'
#!/bin/bash
# WitchCityRope Application Restart Script
# Safely restarts application containers

set -euo pipefail

if [ $# -eq 0 ]; then
    echo "Usage: $0 <environment>"
    echo "  environment: production, staging, or all"
    exit 1
fi

ENVIRONMENT="$1"

restart_environment() {
    local env=$1
    echo "🔄 Restarting $env environment..."

    cd "/opt/witchcityrope/$env"

    # Graceful restart
    docker-compose -f "docker-compose.$env.yml" restart

    # Wait for health checks
    sleep 30

    # Verify restart
    port=$([ "$env" = "production" ] && echo "5001" || echo "5002")
    if curl -f "http://localhost:$port/health" > /dev/null 2>&1; then
        echo "✅ $env environment restarted successfully"
    else
        echo "❌ $env environment restart failed"
        return 1
    fi
}

case "$ENVIRONMENT" in
    "production")
        restart_environment "production"
        ;;
    "staging")
        restart_environment "staging"
        ;;
    "all")
        restart_environment "staging"
        restart_environment "production"
        ;;
    *)
        echo "❌ Invalid environment: $ENVIRONMENT"
        echo "   Valid options: production, staging, all"
        exit 1
        ;;
esac
EOF

chmod +x /opt/witchcityrope/restart.sh

# Create logs script
cat > /opt/witchcityrope/logs.sh << 'EOF'
#!/bin/bash
# WitchCityRope Application Logs Script
# View logs for application containers

set -euo pipefail

if [ $# -eq 0 ]; then
    echo "Usage: $0 <environment> [service] [options]"
    echo "  environment: production, staging"
    echo "  service: api, web, redis (optional, shows all if not specified)"
    echo "  options: -f (follow), --tail=N (show last N lines)"
    exit 1
fi

ENVIRONMENT="$1"
SERVICE="${2:-}"
OPTIONS="${@:3}"

if [ "$ENVIRONMENT" != "production" ] && [ "$ENVIRONMENT" != "staging" ]; then
    echo "❌ Invalid environment: $ENVIRONMENT"
    exit 1
fi

cd "/opt/witchcityrope/$ENVIRONMENT"

if [ -n "$SERVICE" ]; then
    echo "📋 Showing logs for $ENVIRONMENT $SERVICE..."
    docker-compose -f "docker-compose.$ENVIRONMENT.yml" logs $OPTIONS "$SERVICE"
else
    echo "📋 Showing logs for all $ENVIRONMENT services..."
    docker-compose -f "docker-compose.$ENVIRONMENT.yml" logs $OPTIONS
fi
EOF

chmod +x /opt/witchcityrope/logs.sh

# Run initial status check
echo "📊 Running initial status check..."
/opt/witchcityrope/status.sh

# Final summary
echo ""
echo "✅ Application deployment completed successfully!"
echo ""
echo "📋 Deployment Summary:"
echo "   • Production environment: Deployed on ports 5001 (API), 3001 (Web)"
echo "   • Staging environment: Deployed on ports 5002 (API), 3002 (Web)"
echo "   • Redis cache: Running on port 6379"
echo "   • Docker Compose configurations: Created"
echo "   • Management scripts: Created"
echo ""
echo "📁 Important Files:"
echo "   • Production compose: /opt/witchcityrope/production/docker-compose.production.yml"
echo "   • Staging compose: /opt/witchcityrope/staging/docker-compose.staging.yml"
echo "   • Status script: /opt/witchcityrope/status.sh"
echo "   • Restart script: /opt/witchcityrope/restart.sh"
echo "   • Logs script: /opt/witchcityrope/logs.sh"
echo ""
echo "🔧 Useful Commands:"
echo "   • Check status: /opt/witchcityrope/status.sh"
echo "   • Restart env: /opt/witchcityrope/restart.sh <production|staging|all>"
echo "   • View logs: /opt/witchcityrope/logs.sh <environment> [service] [-f]"
echo "   • Container stats: docker stats"
echo ""
echo "🌐 Application Access:"
echo "   • Production API: http://localhost:5001 (via Nginx: https://your-domain.com/api)"
echo "   • Production Web: http://localhost:3001 (via Nginx: https://your-domain.com)"
echo "   • Staging API: http://localhost:5002 (via Nginx: https://staging.your-domain.com/api)"
echo "   • Staging Web: http://localhost:3002 (via Nginx: https://staging.your-domain.com)"
echo ""
echo "🚨 NEXT STEPS:"
echo "   1. Verify applications are accessible through Nginx reverse proxy"
echo "   2. Test both production and staging environments"
echo "   3. Run next script: 06-monitoring-setup.sh"
echo ""
echo "📅 Completed at: $(date)"