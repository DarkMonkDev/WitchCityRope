# Docker Container Deployment Instructions

## Overview

This guide covers deploying WitchCityRope using Docker containers for a consistent and scalable production environment.

## Docker Architecture

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Nginx Proxy   │────▶│  Web Container  │────▶│   PostgreSQL    │
│   (Port 80/443) │     │   (Port 5651)   │     │   (Port 5432)   │
└─────────────────┘     └─────────────────┘     └─────────────────┘
                               │                          │
                               ▼                          ▼
                        ┌─────────────────┐     ┌─────────────────┐
                        │  Redis Cache    │     │   File Storage  │
                        │  (Port 6379)    │     │   (S3/Local)    │
                        └─────────────────┘     └─────────────────┘
```

## Docker Files

### Production Dockerfile

```dockerfile
# Dockerfile.prod
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["WitchCityRope.Web/WitchCityRope.Web.csproj", "WitchCityRope.Web/"]
COPY ["WitchCityRope.Core/WitchCityRope.Core.csproj", "WitchCityRope.Core/"]
COPY ["WitchCityRope.Infrastructure/WitchCityRope.Infrastructure.csproj", "WitchCityRope.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "WitchCityRope.Web/WitchCityRope.Web.csproj"

# Copy source code
COPY . .

# Build application
WORKDIR "/src/WitchCityRope.Web"
RUN dotnet build "WitchCityRope.Web.csproj" -c Release -o /app/build

# Publish application
FROM build AS publish
RUN dotnet publish "WitchCityRope.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install dependencies
RUN apt-get update && apt-get install -y \
    curl \
    wget \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app

# Copy published files
COPY --from=publish --chown=appuser:appuser /app/publish .

# Set environment
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Switch to non-root user
USER appuser

EXPOSE 8080
ENTRYPOINT ["dotnet", "WitchCityRope.Web.dll"]
```

### Docker Compose Configuration

```yaml
# docker-compose.prod.yml
version: '3.8'

services:
  web:
    build:
      context: .
      dockerfile: Dockerfile.prod
    container_name: witchcityrope-web
    restart: always
    ports:
      - "5651:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=db;Database=WitchCityRope;Username=witchcity;Password=${DB_PASSWORD}
      - Redis__ConnectionString=redis:6379
    depends_on:
      db:
        condition: service_healthy
      redis:
        condition: service_healthy
    volumes:
      - ./wwwroot:/app/wwwroot
      - logs:/app/logs
    networks:
      - witchcity-network
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 2G
        reservations:
          cpus: '1.0'
          memory: 1G

  db:
    image: postgres:15-alpine
    container_name: witchcityrope-db
    restart: always
    environment:
      - POSTGRES_DB=WitchCityRope
      - POSTGRES_USER=witchcity
      - POSTGRES_PASSWORD=${DB_PASSWORD}
      - PGDATA=/var/lib/postgresql/data/pgdata
    volumes:
      - postgres-data:/var/lib/postgresql/data
      - ./init-scripts:/docker-entrypoint-initdb.d
    ports:
      - "5432:5432"
    networks:
      - witchcity-network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U witchcity -d WitchCityRope"]
      interval: 10s
      timeout: 5s
      retries: 5
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 1G

  redis:
    image: redis:7-alpine
    container_name: witchcityrope-redis
    restart: always
    command: redis-server --appendonly yes --maxmemory 256mb --maxmemory-policy allkeys-lru
    volumes:
      - redis-data:/data
    ports:
      - "6379:6379"
    networks:
      - witchcity-network
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5
    deploy:
      resources:
        limits:
          cpus: '0.5'
          memory: 512M

  nginx:
    image: nginx:alpine
    container_name: witchcityrope-nginx
    restart: always
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./nginx/conf.d:/etc/nginx/conf.d:ro
      - ./nginx/ssl:/etc/nginx/ssl:ro
      - ./wwwroot:/usr/share/nginx/html:ro
      - nginx-cache:/var/cache/nginx
    depends_on:
      - web
    networks:
      - witchcity-network
    deploy:
      resources:
        limits:
          cpus: '0.5'
          memory: 256M

volumes:
  postgres-data:
    driver: local
  redis-data:
    driver: local
  logs:
    driver: local
  nginx-cache:
    driver: local

networks:
  witchcity-network:
    driver: bridge
    ipam:
      config:
        - subnet: 172.20.0.0/16
```

## Deployment Process

### 1. Prepare Environment

```bash
# Create deployment directory
mkdir -p /opt/witchcityrope
cd /opt/witchcityrope

# Clone repository
git clone https://github.com/yourusername/WitchCityRope.git .

# Create environment file
cat > .env.production << EOF
DB_PASSWORD=your-secure-database-password
JWT_KEY=your-very-long-jwt-key
STRIPE_SECRET_KEY=your-stripe-secret-key
S3_ACCESS_KEY=your-s3-access-key
S3_SECRET_KEY=your-s3-secret-key
EOF

# Set permissions
chmod 600 .env.production
```

### 2. Build Images

```bash
# Build all images
docker-compose -f docker-compose.prod.yml build

# Or build specific service
docker-compose -f docker-compose.prod.yml build web

# Build with no cache
docker-compose -f docker-compose.prod.yml build --no-cache
```

### 3. Database Initialization

```bash
# Start only database service
docker-compose -f docker-compose.prod.yml up -d db

# Wait for database to be ready
sleep 10

# Run migrations
docker-compose -f docker-compose.prod.yml run --rm web dotnet ef database update

# Create initial admin user
docker-compose -f docker-compose.prod.yml run --rm web dotnet WitchCityRope.Web.dll seed-admin
```

### 4. Start All Services

```bash
# Start all services in detached mode
docker-compose -f docker-compose.prod.yml up -d

# View logs
docker-compose -f docker-compose.prod.yml logs -f

# View specific service logs
docker-compose -f docker-compose.prod.yml logs -f web
```

### 5. Verify Deployment

```bash
# Check all services are running
docker-compose -f docker-compose.prod.yml ps

# Test health endpoints
curl http://localhost:5651/health
curl http://localhost:5651/health/ready

# Check resource usage
docker stats
```

## Container Management

### Service Control

```bash
# Stop all services
docker-compose -f docker-compose.prod.yml stop

# Start all services
docker-compose -f docker-compose.prod.yml start

# Restart specific service
docker-compose -f docker-compose.prod.yml restart web

# Remove all containers (preserves volumes)
docker-compose -f docker-compose.prod.yml down

# Remove all containers and volumes
docker-compose -f docker-compose.prod.yml down -v
```

### Scaling Services

```bash
# Scale web service
docker-compose -f docker-compose.prod.yml up -d --scale web=3

# Update HAProxy configuration for load balancing
```

### Container Maintenance

```bash
# Execute commands in running container
docker-compose -f docker-compose.prod.yml exec web bash

# Run one-off commands
docker-compose -f docker-compose.prod.yml run --rm web dotnet ef migrations add NewMigration

# Copy files from container
docker cp witchcityrope-web:/app/logs ./backup-logs

# Clean up unused resources
docker system prune -a --volumes
```

## Nginx Configuration

### Production Nginx Config

```nginx
# nginx/conf.d/witchcityrope.conf
upstream witchcity_backend {
    server web:8080;
}

server {
    listen 80;
    server_name yourdomain.com www.yourdomain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name yourdomain.com www.yourdomain.com;

    # SSL Configuration
    ssl_certificate /etc/nginx/ssl/cert.pem;
    ssl_certificate_key /etc/nginx/ssl/key.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;

    # Security Headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;

    # Proxy Configuration
    location / {
        proxy_pass http://witchcity_backend;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Real-IP $remote_addr;
        
        # Timeouts
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }

    # Static files
    location ~* \.(jpg|jpeg|png|gif|ico|css|js|pdf|txt)$ {
        root /usr/share/nginx/html;
        expires 30d;
        add_header Cache-Control "public, immutable";
    }

    # WebSocket support
    location /hubs/ {
        proxy_pass http://witchcity_backend;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }
}
```

## Monitoring Configuration

### Docker Health Checks

```yaml
# Health check for web service
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

### Container Logging

```yaml
# Logging configuration
logging:
  driver: "json-file"
  options:
    max-size: "10m"
    max-file: "3"
    labels: "service"
    env: "ENVIRONMENT,SERVICE_NAME"
```

### Resource Monitoring

```bash
# Monitor container resources
docker stats --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}"

# View container processes
docker-compose -f docker-compose.prod.yml top

# Inspect container
docker inspect witchcityrope-web
```

## Backup Procedures

### Database Backup

```bash
#!/bin/bash
# scripts/backup-database.sh

BACKUP_DIR="/backups/postgres"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/witchcityrope_$TIMESTAMP.sql"

# Create backup
docker-compose -f docker-compose.prod.yml exec -T db pg_dump -U witchcity WitchCityRope > $BACKUP_FILE

# Compress backup
gzip $BACKUP_FILE

# Upload to S3
aws s3 cp $BACKUP_FILE.gz s3://witchcityrope-backups/database/

# Clean old backups
find $BACKUP_DIR -name "*.gz" -mtime +30 -delete
```

### Volume Backup

```bash
#!/bin/bash
# scripts/backup-volumes.sh

BACKUP_DIR="/backups/volumes"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

# Stop containers
docker-compose -f docker-compose.prod.yml stop

# Backup volumes
docker run --rm \
  -v witchcityrope_postgres-data:/data/postgres \
  -v witchcityrope_redis-data:/data/redis \
  -v $BACKUP_DIR:/backup \
  alpine tar czf /backup/volumes_$TIMESTAMP.tar.gz /data

# Start containers
docker-compose -f docker-compose.prod.yml start
```

## Troubleshooting

### Common Issues

1. **Container won't start**
```bash
# Check logs
docker logs witchcityrope-web

# Check events
docker events --filter container=witchcityrope-web
```

2. **Database connection issues**
```bash
# Test database connection
docker-compose -f docker-compose.prod.yml exec web nc -zv db 5432

# Check database logs
docker-compose -f docker-compose.prod.yml logs db
```

3. **Memory issues**
```bash
# Check memory usage
docker system df

# Clean up
docker system prune -a
```

### Debug Mode

```yaml
# docker-compose.debug.yml
version: '3.8'

services:
  web:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - Logging__LogLevel__Default=Debug
    ports:
      - "5651:8080"
      - "5652:8081"  # Debug port
```

## Security Best Practices

1. **Use specific image versions**
```yaml
image: mcr.microsoft.com/dotnet/aspnet:8.0.1-alpine3.18
```

2. **Run as non-root user**
```dockerfile
RUN useradd -m -u 1000 appuser
USER appuser
```

3. **Limit container capabilities**
```yaml
security_opt:
  - no-new-privileges:true
cap_drop:
  - ALL
cap_add:
  - NET_BIND_SERVICE
```

4. **Use secrets management**
```yaml
secrets:
  db_password:
    external: true
  jwt_key:
    external: true
```

5. **Network isolation**
```yaml
networks:
  frontend:
    internal: false
  backend:
    internal: true
```