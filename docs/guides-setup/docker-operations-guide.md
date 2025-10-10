# Docker Operations Guide - WitchCityRope
<!-- Last Updated: 2025-08-17 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Quick Start Commands

### Helper Scripts (Recommended)
```bash
./scripts/docker-dev.sh              # Start development with health checks
./scripts/docker-stop.sh             # Stop all services
./scripts/docker-logs.sh [service]   # View service logs with filtering
./scripts/docker-health.sh           # Check comprehensive health
./scripts/docker-rebuild.sh [service] # Rebuild specific services
./scripts/docker-clean.sh [options]  # Clean up containers/volumes/images
```

### Essential Commands (Direct Docker Compose)
```bash
./dev.sh                             # Quick development start
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
docker-compose down                  # Stop all services
docker-compose logs -f               # View all service logs
docker-compose ps                    # Check service status
```

### Quick Health Check
```bash
./scripts/docker-health.sh           # Comprehensive health check

# Manual verification
curl -f http://localhost:5173 && echo "✅ React app ready"
curl -f http://localhost:5655/health && echo "✅ API healthy"
docker-compose exec postgres pg_isready -U postgres && echo "✅ Database ready"
```

## Helper Scripts Reference

### docker-dev.sh - Development Environment Starter
```bash
./scripts/docker-dev.sh                # Start development environment
./scripts/docker-dev.sh --build        # Force rebuild before starting
./scripts/docker-dev.sh --logs         # Start and follow logs
./scripts/docker-dev.sh --clean        # Clean rebuild (no cache)
```
Features: Pre-flight checks, image building, health monitoring, status display, optional log following.

### docker-stop.sh - Environment Stopper
```bash
./scripts/docker-stop.sh               # Graceful stop
./scripts/docker-stop.sh --force       # Force stop containers
./scripts/docker-stop.sh --volumes     # Stop and remove volumes (⚠️ DATA LOSS)
./scripts/docker-stop.sh --all         # Stop all environments
```

### docker-logs.sh - Log Viewer with Filtering
```bash
./scripts/docker-logs.sh               # All service logs
./scripts/docker-logs.sh api           # API logs only
./scripts/docker-logs.sh web --follow  # Follow React app logs
./scripts/docker-logs.sh --errors      # Show only errors
./scripts/docker-logs.sh api --auth --follow  # Follow authentication logs
./scripts/docker-logs.sh postgres --since "1 hour ago"  # Time-based filtering
./scripts/docker-logs.sh --filter "JWT" # Custom grep pattern
```

### docker-health.sh - Comprehensive Health Checker
```bash
./scripts/docker-health.sh             # Standard health check
./scripts/docker-health.sh --verbose   # Detailed output
./scripts/docker-health.sh --quick     # Basic connectivity only
./scripts/docker-health.sh --fix       # Attempt to fix issues
./scripts/docker-health.sh --api-only  # Test API health only
```
Features: Container status, database connectivity, API health, authentication flow, service communication, performance metrics, auto-fix capabilities.

### docker-rebuild.sh - Service Rebuilder
```bash
./scripts/docker-rebuild.sh api        # Rebuild API service
./scripts/docker-rebuild.sh all --no-cache  # Rebuild all without cache
./scripts/docker-rebuild.sh web --restart   # Rebuild and restart React
./scripts/docker-rebuild.sh --parallel      # Parallel build
```

### docker-clean.sh - Cleanup Tool
```bash
./scripts/docker-clean.sh              # Interactive cleanup
./scripts/docker-clean.sh --containers # Remove containers only
./scripts/docker-clean.sh --volumes    # Remove volumes (⚠️ DATA LOSS)
./scripts/docker-clean.sh --all        # Remove everything
./scripts/docker-clean.sh --dry-run    # Show what would be removed
```

### docker-migrate.sh - Database Migration Tool
```bash
./scripts/docker-migrate.sh                     # Apply pending migrations
./scripts/docker-migrate.sh --create "AddUserProfiles"  # Create new migration
./scripts/docker-migrate.sh --rollback          # Rollback last migration
./scripts/docker-migrate.sh --reset             # Reset database (⚠️ DATA LOSS)
./scripts/docker-migrate.sh --env production    # Run against production
./scripts/docker-migrate.sh --backup            # Backup before migration
```

## Container Management

### Starting Services

#### Fresh Start
```bash
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build -d
sleep 15
./docker/scripts/health-check.sh
```

#### Regular Startup
```bash
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
docker-compose ps
```

#### Start Specific Services
```bash
docker-compose up -d postgres        # Start only database
docker-compose up -d api             # Start API after database
docker-compose up -d web             # Start React app
```

### Stopping Services

```bash
docker-compose down                  # Graceful shutdown
docker-compose down -v               # Stop and remove volumes
docker-compose kill                  # Force stop all
docker-compose stop web              # Stop specific service
```

### Restarting Services

```bash
docker-compose restart               # Restart all services
docker-compose restart api           # Restart API
docker-compose down && docker-compose up --build -d  # Restart with rebuild
```

### Health Checking

```bash
docker-compose ps                    # Check all container status
docker-compose exec postgres pg_isready -U postgres -d witchcityrope_dev
curl -f http://localhost:5655/health
docker stats                         # Container resource usage

# Dependency checking
docker-compose exec web wget -qO- http://api:8080/health
docker-compose exec api pg_isready -h postgres -p 5432 -U postgres
```

### Viewing Logs

```bash
docker-compose logs -f web           # React app logs
docker-compose logs -f api           # API logs
docker-compose logs -f               # All services
docker-compose logs --tail=100       # Last 100 lines
docker-compose logs api | grep -i error  # Search for errors
```

## Development Workflows

### Hot Reload Testing

#### React Hot Reload
```bash
docker-compose up -d
open http://localhost:5173
echo "console.log('Hot reload test');" >> apps/web/src/App.tsx
# Verify change appears within 1 second
docker-compose logs -f web | grep -i "hmr\|reload"
```

#### .NET API Hot Reload
```bash
docker-compose up -d
docker-compose logs -f api &
echo "Console.WriteLine(\"Hot reload test - $(date)\");" >> apps/api/Program.cs
# Verify restart within 5 seconds
curl -f http://localhost:5655/health
```

### Log Monitoring

```bash
# Terminal 1: Monitor all services
docker-compose logs -f

# Watch for errors
docker-compose logs -f | grep -i error

# Watch for authentication issues
docker-compose logs -f | grep -E "(auth|login|jwt|cookie)"

# Performance monitoring
docker stats --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}"
```

### Performance Validation

```bash
# Test load times
curl -w "React Load: %{time_total}s\n" -o /dev/null -s http://localhost:5173
curl -w "API Response: %{time_total}s\n" -o /dev/null -s http://localhost:5655/health

# Resource usage (expected)
# - React: ~100-200MB RAM, <10% CPU
# - API: ~200-400MB RAM, <15% CPU
# - PostgreSQL: ~100-300MB RAM, <10% CPU
docker stats --no-stream
```

## Troubleshooting

### Common Issues

#### Container Won't Start
```bash
netstat -tulpn | grep -E "(5173|5655|5433)"  # Check port conflicts
sudo lsof -ti:5173 | xargs sudo kill -9     # Kill processes
docker version                               # Check Docker daemon
```

#### Service Communication Issues
```bash
docker network inspect witchcityrope-dev    # Verify network
docker-compose exec web nslookup api        # Test DNS
docker-compose exec web ping api            # Test connectivity
```

#### Volume Mount Problems
```bash
docker-compose exec web ls -la /app/        # Verify mounts
sudo chown -R $(id -u):$(id -g) apps/       # Fix permissions (Linux/macOS)
```

### Service Communication

#### React to API Issues
```bash
docker-compose exec web curl -f http://api:8080/health
docker-compose exec web env | grep VITE_API_BASE_URL

# Test CORS
curl -H "Origin: http://localhost:5173" \
     -H "Access-Control-Request-Method: POST" \
     -X OPTIONS http://localhost:5655/api/auth/login
```

#### API to Database Issues
```bash
docker-compose exec api pg_isready -h postgres -p 5432 -U postgres
docker-compose exec api env | grep ConnectionStrings
docker-compose exec postgres psql -U postgres -d witchcityrope_dev -c "SELECT version();"
```

#### Authentication Flow Issues
```bash
# Test complete flow
curl -X POST http://localhost:5655/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"debug@test.com","password":"Test1234","sceneName":"DebugUser"}' -v

curl -X POST http://localhost:5655/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"debug@test.com","password":"Test1234"}' -c cookies.txt -v

curl http://localhost:5655/api/protected/welcome -b cookies.txt -v
```

## Database Operations and Migrations

### Essential Migration Workflow
```bash
# Check migration status
docker-compose exec api dotnet ef migrations list

# Apply migrations
docker-compose exec api dotnet ef database update

# Verify tables
docker-compose exec postgres psql -U postgres -d witchcityrope_dev -c "\dt"

# Check migration history
docker-compose exec postgres psql -U postgres -d witchcityrope_dev -c \
  "SELECT * FROM __EFMigrationsHistory ORDER BY migration_id DESC LIMIT 5;"
```

### Connection String Configuration
```bash
# Development (container-to-container)
ConnectionStrings__DefaultConnection="Host=postgres;Port=5432;Database=witchcityrope_dev;Username=postgres;Password=postgres"

# Production (with SSL)
ConnectionStrings__DefaultConnection="Host=postgres;Port=5432;Database=witchcityrope_prod;Username=postgres;Password=${DB_PASSWORD};SSL Mode=Require;Connection Lifetime=30;Minimum Pool Size=1;Maximum Pool Size=50"

# Host access (debugging)
ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=witchcityrope_dev;Username=postgres;Password=postgres"
```

### Container Networking
```bash
docker-compose exec api nslookup postgres   # Verify DNS
docker-compose exec web curl -f http://api:8080/health  # Internal
curl -f http://localhost:5655/health        # External
docker network inspect witchcityrope-dev | jq '.[0].Containers'
```

### Database Issues
```bash
# Check connection
docker-compose exec postgres pg_isready -U postgres

# Test database queries
docker-compose exec postgres psql -U postgres -d witchcityrope_dev -c "\l"

# Check user data
docker-compose exec postgres psql -U postgres -d witchcityrope_dev -c \
  "SELECT email FROM AspNetUsers LIMIT 5;"

# Backup/Restore
docker-compose exec postgres pg_dump -U postgres witchcityrope_dev > backup.sql
cat backup.sql | docker-compose exec -T postgres psql -U postgres -d witchcityrope_dev
```

## Agent-Specific Sections

### For Test Executor

#### Container Testing Setup
```bash
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
./docker/scripts/health-check.sh
npm run test:e2e:playwright

# Monitor during tests
docker-compose logs -f web &
docker-compose logs -f api &
```

#### Test Environment Cleanup
```bash
# Clean test data
docker-compose exec postgres psql -U postgres -d witchcityrope_dev -c \
  "TRUNCATE TABLE AspNetUsers CASCADE;"

# Reset containers
docker-compose down
docker-compose up -d
./docker/scripts/health-check.sh
```

### For Backend Developer

#### .NET API Development
```bash
docker-compose logs -f api                  # Monitor logs
docker-compose exec api bash                # Access container
docker-compose exec api env | grep -E "(ASPNETCORE|ConnectionStrings|JWT)"

# Test endpoints
curl -v http://localhost:5655/health
curl -v http://localhost:5655/api/auth/health
```

#### Hot Reload Workflow
```bash
docker-compose up -d
docker-compose logs -f api                  # Monitor in separate terminal
# Make code changes - watch for dotnet watch restart
curl http://localhost:5655/health           # Test changes
```

#### Database Operations
```bash
docker-compose exec api dotnet ef migrations add NewMigration
docker-compose exec api dotnet ef database update
docker-compose exec postgres psql -U postgres -d witchcityrope_dev -c "\d"
```

#### Authentication Testing
```bash
# Test service token generation
curl -X POST http://localhost:5655/api/auth/service-token \
  -H "Content-Type: application/json" \
  -H "X-Service-Secret: DevSecret-WitchCityRope-ServiceToService-Auth-2024" \
  -d '{"userId": "test-user", "email": "test@example.com"}'
```

### For React Developer

#### Frontend Development
```bash
docker-compose logs -f web                  # Monitor React logs
docker-compose exec web sh                  # Access container
docker-compose exec web curl http://api:8080/health  # Test API connectivity
```

#### Hot Reload Optimization
```bash
echo "export const testChange = new Date();" >> apps/web/src/test.ts
# Should see immediate browser update
docker-compose logs web | grep -i hmr       # Verify HMR working
```

#### Build Operations
```bash
# Development build with hot reload
docker build --target development -t witchcityrope/web:dev ./apps/web

# Production build
docker build --target production \
  --build-arg VITE_API_BASE_URL=http://localhost:5655 \
  -t witchcityrope/web:prod ./apps/web

# Check image size
docker images | grep witchcityrope/web
```

#### Troubleshooting
```bash
# Debug HMR issues
docker-compose exec web ps aux | grep node
docker-compose logs web | grep -E "(error|warn|fail)"

# Test volume mounts
docker-compose exec web touch /app/src/test-permissions
ls -la apps/web/src/test-permissions  # Should exist

# Debug API connectivity
docker-compose exec web curl -v http://api:8080/health
docker-compose exec web nslookup api
```

## Advanced Operations

### Container Maintenance
```bash
docker system prune -f                      # Clean up unused resources
docker-compose down -v --rmi all            # Remove all project resources
docker-compose build --no-cache             # Rebuild from scratch
```

### Performance Optimization
```bash
# Enable BuildKit in ~/.docker/daemon.json:
{
  "experimental": true,
  "features": { "buildkit": true }
}

DOCKER_BUILDKIT=1 docker-compose build      # Faster builds
```

### Security Validation
```bash
docker scan $(docker-compose config --services)  # Scan for vulnerabilities
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  aquasec/trivy image postgres:16-alpine
```

## Production Deployment

### Server Requirements
```bash
# Minimum Production Specs:
# - CPU: 2 cores (4 recommended)
# - RAM: 4GB (8GB recommended)
# - Storage: 20GB SSD (50GB recommended)
# - Network: 100Mbps (1Gbps recommended)
# - Docker: 20.10+ with BuildKit
# - OS: Ubuntu 22.04 LTS
```

### SSL Configuration
```yaml
# docker-compose.prod.yml - SSL Setup
services:
  nginx:
    image: nginx:alpine
    ports: ["80:80", "443:443"]
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./ssl/certs:/etc/ssl/certs:ro
    environment:
      - DOMAIN_NAME=${DOMAIN_NAME}
    depends_on: [web, api]

  api:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Authentication__SecureCookies=true
      - Authentication__CookieDomain=${DOMAIN_NAME}
```

### Production Monitoring
```bash
# Health monitoring
monitor_production() {
    API_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://$DOMAIN_NAME/api/health)
    [ "$API_STATUS" != "200" ] && echo "🚨 API health check failed: $API_STATUS"

    DB_CONNECTIONS=$(docker-compose exec postgres psql -U postgres -d witchcityrope_prod -t -c \
      "SELECT count(*) FROM pg_stat_activity WHERE state = 'active';")
    [ "$(echo $DB_CONNECTIONS | tr -d ' \n')" -gt "50" ] && echo "🚨 High DB connections: $DB_CONNECTIONS"

    echo "✅ Monitoring check completed"
}
```

### Backup Procedures
```bash
backup_production() {
    BACKUP_DATE=$(date +%Y%m%d_%H%M%S)
    BACKUP_DIR="/backups/witchcityrope/$BACKUP_DATE"
    mkdir -p "$BACKUP_DIR"

    # Database backup
    docker-compose exec postgres pg_dump -U postgres witchcityrope_prod | \
      gzip > "$BACKUP_DIR/database.sql.gz"

    # Configuration backup
    tar -czf "$BACKUP_DIR/config.tar.gz" docker-compose*.yml .env.prod nginx/

    # Verify backup
    gunzip -t "$BACKUP_DIR/database.sql.gz" || exit 1

    # Cleanup old backups (keep 30 days)
    find /backups/witchcityrope -type d -mtime +30 -exec rm -rf {} +
}
```

### Multi-Environment Strategy
```bash
# Environment-specific deployment
deploy_environment() {
    ENV=$1
    [ ! -f ".env.$ENV" ] && echo "❌ Environment file .env.$ENV not found" && exit 1

    export $(cat .env.$ENV | xargs)
    docker-compose -f docker-compose.yml -f docker-compose.$ENV.yml up -d
    ./scripts/health-check-$ENV.sh
}

# Deploy to staging
deploy_environment staging

# Deploy to production
deploy_environment production
```

### Performance Metrics

From vertical slice validation testing:
```bash
# Measured Performance (Docker environment)
# React Load: 0.8s (Target: <2.0s) ✅
# API Health: 0.05s (Target: <0.5s) ✅
# Database Connection: 0.02s (Target: <0.1s) ✅
# JWT Generation: 0.1s (Target: <0.2s) ✅
# Hot Reload React: 0.3s (Target: <1.0s) ✅
# Hot Reload API: 3.2s (Target: <5.0s) ✅
# Full Auth Flow: 0.8s (Target: <2.0s) ✅

# Resource Usage
docker stats --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}"
# Expected: web 5-8% 150-200MB, api 3-6% 180-250MB, postgres 2-4% 80-120MB
```

---

This Docker operations guide provides comprehensive procedures for containerized WitchCityRope development with real-world implementation learnings, troubleshooting solutions, and production-ready patterns. For strategic Docker architecture, see [Docker Architecture](/docs/architecture/docker-architecture.md).
