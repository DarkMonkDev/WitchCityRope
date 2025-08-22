# Docker Development Guide

This guide covers Docker setup and development workflow for the WitchCityRope project.

## 🚨 CRITICAL: Always Use Development Build

> **NEVER run `docker-compose up` directly!** This uses production build and WILL FAIL.
> 
> The default docker-compose.yml uses `target: ${BUILD_TARGET:-final}` which builds production images that try to run `dotnet watch` on compiled assemblies. This ALWAYS FAILS.

## Prerequisites

- Docker and Docker Compose installed
- Basic understanding of container concepts
- Access to project repository

## Quick Start

### 1. Use the Development Helper Script (Recommended)

```bash
# Start development environment with interactive menu
./dev.sh

# Select option 1 to start all services
```

### 2. Manual Docker Commands

```bash
# ✅ CORRECT: Start all services with development build
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# ❌ WRONG: Never use this!
# docker-compose up -d  # This uses production build and FAILS!
```

## Development Workflow

### Starting Services

```bash
# Using helper script (RECOMMENDED)
./dev.sh
# Then select:
# 1 - Start all services
# 2 - Stop all services
# 3 - View logs
# 4 - Restart web service
# 5 - Run database migrations
# 6 - Access web container shell
# 7 - Rebuild and restart all services
# 8 - Clean up (remove containers and volumes)

# Or manually:
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

### Viewing Logs

```bash
# View logs for all services
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f

# View logs for specific service
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f web
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f api
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f postgres
```

### Stopping Services

```bash
# Stop all services (keeps data)
docker-compose down

# Stop and remove volumes (WARNING: Deletes all data!)
docker-compose down -v
```

## Service Architecture

### Services Running

| Service | Port | URL | Description |
|---------|------|-----|-------------|
| Web Application | 5651 | http://localhost:5651 | Blazor Server UI |
| API Service | 5653 | http://localhost:5653 | Minimal API |
| PostgreSQL Database | 5433 | localhost:5433 | Database (internal: 5432) |

**Note**: HTTPS is disabled in Docker development. Use HTTP URLs only.

## Hot Reload Issues and Solutions

> ⚠️ **IMPORTANT**: .NET 9 Blazor Server hot reload in Docker containers is notoriously unreliable, especially for authentication/layout changes.

### When Hot Reload Fails

1. **Quick Restart** (preserves data):
   ```bash
   ./restart-web.sh
   ```

2. **Full Service Restart**:
   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart web
   ```

3. **Complete Rebuild** (for major changes):
   ```bash
   ./dev.sh
   # Select option 7 - Rebuild and restart
   ```

### Changes That Require Container Restart

Always restart containers after:
- Authentication/authorization changes
- Layout component modifications
- Render mode changes
- Dependency injection modifications
- Route configuration changes
- Program.cs modifications
- Middleware pipeline changes

## Container Management

### Accessing Containers

```bash
# Access web container shell
docker-compose exec web bash

# Access API container shell
docker-compose exec api bash

# Access database
docker-compose exec postgres psql -U postgres -d witchcityrope_db
```

### Running Commands in Containers

```bash
# Run database migrations
docker-compose exec web dotnet ef database update

# Build the application
docker exec witchcity-web dotnet build

# Run tests inside container
docker-compose exec web dotnet test
```

### Container Health Checks

```bash
# Check container status
docker ps

# Check container health
docker inspect witchcity-web --format='{{.State.Health.Status}}'

# View container resource usage
docker stats
```

## Common Docker Commands

### Development Commands

```bash
# View running containers
docker ps

# View all containers (including stopped)
docker ps -a

# Remove specific container
docker rm container_name

# View container logs
docker logs container_name

# Follow container logs
docker logs -f container_name

# Execute command in running container
docker exec -it container_name command
```

### Cleanup Commands

```bash
# Remove all stopped containers
docker container prune

# Remove unused images
docker image prune

# Remove unused volumes
docker volume prune

# Complete system cleanup (WARNING: Removes everything unused!)
docker system prune -a
```

## Troubleshooting

### Container Won't Start

1. **Check logs**:
   ```bash
   docker-compose logs web
   ```

2. **Verify build target**:
   ```bash
   # Check docker-compose.yml for correct target
   grep -A5 "target:" docker-compose*.yml
   ```

3. **Clean rebuild**:
   ```bash
   docker-compose down
   docker-compose -f docker-compose.yml -f docker-compose.dev.yml build --no-cache
   docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
   ```

### Port Already in Use

```bash
# Find process using port
sudo lsof -i :5651

# Kill process
kill -9 <PID>

# Or change port in docker-compose.yml
```

### "Headers are read-only" Error

This indicates hot reload failure. Solution:
```bash
./restart-web.sh
```

### Database Connection Issues

```bash
# Verify database is running
docker ps | grep postgres

# Check database logs
docker logs witchcity-postgres

# Test connection
docker exec witchcity-postgres psql -U postgres -c "SELECT 1;"
```

### Out of Disk Space

```bash
# Check disk usage
df -h

# Clean up Docker resources
docker system prune -a --volumes
```

## Best Practices

### 1. Always Use Development Compose Files
```bash
# ✅ CORRECT
docker-compose -f docker-compose.yml -f docker-compose.dev.yml [command]

# ❌ WRONG
docker-compose [command]
```

### 2. Use Helper Scripts
- `./dev.sh` - Main development menu
- `./restart-web.sh` - Quick restart for hot reload issues

### 3. Monitor Container Health
```bash
# Regular health check
docker ps --format "table {{.Names}}\t{{.Status}}"
```

### 4. Clean Up Regularly
```bash
# Weekly cleanup (when containers are stopped)
docker system prune -a
```

### 5. Document Container Changes
Always update docker-compose files when:
- Adding new services
- Changing ports
- Modifying environment variables
- Adding volumes

## Environment Variables

### Development Environment
```yaml
ASPNETCORE_ENVIRONMENT: Development
ASPNETCORE_URLS: http://+:8080;https://+:8443
ConnectionStrings__DefaultConnection: Host=postgres;Port=5432;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!
```

### Adding New Environment Variables

1. Update `docker-compose.yml`:
   ```yaml
   environment:
     - NEW_VARIABLE=${NEW_VARIABLE}
   ```

2. Create `.env` file (if not exists):
   ```bash
   cp .env.example .env
   ```

3. Add variable to `.env`:
   ```
   NEW_VARIABLE=value
   ```

## Volume Management

### Persistent Volumes
- `postgres_data` - Database data
- `app_logs` - Application logs
- `uploads` - User uploaded files

### Backup Volumes
```bash
# Backup database volume
docker run --rm -v witchcityrope_postgres_data:/data -v $(pwd):/backup alpine tar czf /backup/postgres_backup.tar.gz -C /data .
```

### Restore Volumes
```bash
# Restore database volume
docker run --rm -v witchcityrope_postgres_data:/data -v $(pwd):/backup alpine tar xzf /backup/postgres_backup.tar.gz -C /data
```

## Security Considerations

1. **Never commit `.env` files** - Use `.env.example` as template
2. **Use secrets for production** - Don't hardcode passwords
3. **Limit port exposure** - Only expose necessary ports
4. **Regular updates** - Keep base images updated
5. **Non-root containers** - Run as non-privileged user when possible

## Related Documentation

- [Database Setup Guide](./database-setup.md) - PostgreSQL configuration
- [Development Standards](../standards-processes/development-standards.md) - Coding standards
- [Docker Architecture](../../docs/architecture/DOCKER_ARCHITECTURE.md) - Container architecture
- [Quick Start Guide](./developer-quick-start.md) - Getting started