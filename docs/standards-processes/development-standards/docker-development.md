# Docker Development Standards

## Overview

This document defines mandatory Docker development practices for the WitchCityRope project. Following these standards prevents common build failures and ensures consistent development environments.

## 🚨 CRITICAL: Development vs Production Builds

### The Most Common Mistake

**Developers repeatedly use `docker-compose up` which uses PRODUCTION build target and FAILS!**

The default `docker-compose.yml` uses `target: ${BUILD_TARGET:-final}` which builds production images that try to run `dotnet watch` on compiled assemblies. This ALWAYS FAILS.

### ✅ CORRECT Commands

**ALWAYS use development build commands:**

```bash
# Option 1: Use docker-compose with dev override
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# Option 2: Use the helper script (RECOMMENDED)
./dev.sh
```

### ❌ WRONG Commands

**NEVER use these commands for development:**

```bash
# ❌ WRONG - Uses production build, dotnet watch FAILS
docker-compose up

# ❌ WRONG - Missing dev compose file
docker-compose up -d

# ❌ WRONG - Only use with dev compose
docker-compose logs -f web
```

## Development Environment Setup

### Using the Dev Script (Recommended)

The `./dev.sh` script provides an interactive menu:

```bash
./dev.sh

# Options:
# 1. Start all services (development mode)
# 2. Stop all services
# 3. View logs (all services)
# 4. View web service logs
# 5. View API service logs
# 6. View database logs
# 7. Rebuild and restart
# 8. Run database migrations
# 9. Access web container shell
# 10. Access database shell
```

### Manual Commands

If not using the dev script, always include both compose files:

```bash
# Start services
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# View logs
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f

# View specific service logs
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f web
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f api
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f postgres

# Stop services
docker-compose down

# Execute commands in containers
docker-compose exec web bash
docker-compose exec api bash
docker-compose exec postgres psql -U postgres -d witchcityrope_db
```

## Hot Reload Issues and Solutions

### Known Issue

.NET 9 Blazor Server hot reload in Docker containers is notoriously unreliable, especially for:
- Authentication changes
- Layout modifications
- Render mode changes
- Dependency injection modifications
- Route configuration changes

### Solutions

#### Quick Restart
When changes aren't being picked up:

```bash
# Option 1: Use the restart script
./restart-web.sh

# Option 2: Manual restart
docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart web
```

#### Full Rebuild
For major changes:

```bash
# Option 1: Use dev.sh menu option 7
./dev.sh
# Select option 7: Rebuild and restart

# Option 2: Manual rebuild
docker-compose down
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d --build
```

## Container Architecture

### Service Configuration

| Service | Internal Port | External Port | Purpose |
|---------|---------------|---------------|---------|
| Web | 8080 (HTTP) | 5651 | Blazor Server UI |
| API | 8080 (HTTP) | 5653 | Minimal API |
| PostgreSQL | 5432 | 5433 | Database |

### Development vs Production Differences

#### Development Build
- Mounts source code as volume
- Runs `dotnet watch`
- Enables hot reload
- Debug logging enabled
- No SSL/HTTPS

#### Production Build
- Copies compiled assemblies
- Runs compiled DLL
- No source code mounting
- Optimized for performance
- SSL/HTTPS enabled

## Database Management

### Running Migrations

```bash
# Using dev.sh
./dev.sh
# Select option 8: Run database migrations

# Manual command
docker-compose exec web dotnet ef database update
```

### Database Access

```bash
# PostgreSQL shell
docker-compose exec postgres psql -U postgres -d witchcityrope_db

# Check database tables
docker exec witchcity-postgres psql -U postgres -d witchcityrope_db -c "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public';"
```

## Troubleshooting Guide

### Container Won't Start

1. **Check if ports are in use:**
   ```bash
   sudo lsof -i :5651
   sudo lsof -i :5653
   sudo lsof -i :5433
   ```

2. **Clean up existing containers:**
   ```bash
   docker-compose down
   docker ps -a | grep witchcity | awk '{print $1}' | xargs -r docker rm -f
   ```

3. **Check Docker logs:**
   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs web
   ```

### Changes Not Reflected

**FIRST ACTION: Always restart containers before assuming code issues**

```bash
docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart web
```

### Build Failures

1. **Check compilation in container:**
   ```bash
   docker exec witchcity-web dotnet build --verbosity minimal
   ```

2. **Clear Docker cache and rebuild:**
   ```bash
   docker-compose down
   docker system prune -f
   docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d --build
   ```

## Container Health Monitoring

### Check Container Status

```bash
# View all containers
docker-compose ps

# Check health status
docker inspect witchcity-web --format='{{.State.Health.Status}}'

# View resource usage
docker stats
```

### Health Check Endpoints

- Web: `http://localhost:5651/health`
- API: `http://localhost:5653/health`

## Best Practices

### 1. Always Use Development Compose Files
Never run `docker-compose up` without the dev override file.

### 2. Restart After Critical Changes
Always restart containers after modifying:
- Authentication/authorization
- Layout components
- Render modes
- Service registration
- Route configuration

### 3. Use Helper Scripts
The project provides scripts to handle common issues:
- `./dev.sh` - Full development menu
- `./restart-web.sh` - Quick web restart

### 4. Monitor Logs
Keep logs open in a terminal to catch issues early:
```bash
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f web
```

### 5. Clean Up Resources
Periodically clean unused Docker resources:
```bash
docker system prune -a --volumes
```

## Common Error Messages and Solutions

### "dotnet watch exited with code 143"
**Cause**: Using production build instead of development
**Solution**: Use correct docker-compose command with dev override

### "Cannot find the fallback endpoint"
**Cause**: Application failed to start properly
**Solution**: Check logs and restart container

### "Port already allocated"
**Cause**: Previous containers still running or other services using ports
**Solution**: Run `docker-compose down` and check for processes using ports

### "No such service"
**Cause**: Wrong docker-compose file or service name
**Solution**: Ensure using both compose files in command

## Additional Resources

- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [.NET Docker Images](https://hub.docker.com/_/microsoft-dotnet)
- [Docker Development Guide](./DOCKER_DEV_GUIDE.md)