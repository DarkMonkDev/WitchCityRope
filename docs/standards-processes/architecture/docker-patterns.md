# Docker Development Patterns

**Purpose**: Docker containerization patterns, development workflow, and container management for WitchCityRope.
**When to Read**: When working with Docker containers, docker-compose, or local development environment.
**Related**: [Microservices Patterns](./microservices-patterns.md), [Docker Development Guide](/docs/standards-processes/development-standards/docker-development.md)

## Primary References

**Comprehensive Guides**:
- [Docker Architecture](/docs/architecture/docker-architecture.md) - Complete architecture overview
- [Docker Development Guide](/docs/standards-processes/development-standards/docker-development.md) - Development workflows
- [Docker Dev Guide (Root)](/DOCKER_DEV_GUIDE.md) - Quick start guide

This document provides quick patterns. For detailed architecture and workflows, see the guides above.

## Quick Start

### Starting Development Environment
```bash
# ✅ CORRECT: Use container-restart skill
# The skill handles proper startup with dev overrides

# OR manually:
./dev.sh
```

### Stopping Services
```bash
# Stop all services
docker-compose down

# Stop and remove volumes (fresh start)
docker-compose down -v
```

## Service Ports

```
Web Service (React):   localhost:5173 → container:3000
API Service (.NET):    localhost:5655 → container:8080
PostgreSQL:            localhost:5434 → container:5432
```

## Container Health Checks

### Check Container Status
```bash
# View all container status
docker-compose ps

# View logs for specific service
docker-compose logs web
docker-compose logs api
docker-compose logs postgres

# Follow logs in real-time
docker-compose logs -f api
```

### Health Check Endpoints
```bash
# API health
curl http://localhost:5655/health

# Web service (Vite dev server)
curl http://localhost:5173
```

## Hot Reload

Both services support hot reload in development:

**React (Web)**:
- Vite HMR (Hot Module Replacement)
- Changes reflect immediately
- Volume mounted: `./packages/witchcityrope-web:/app`

**API (.NET)**:
- `dotnet watch` enabled
- Changes trigger automatic rebuild
- Volume mounted: `./src:/app/src`

## Database Access

### Connection Strings
```bash
# From host machine
Host=localhost;Port=5434;Database=witchcityrope;Username=postgres;Password=postgres

# From API container (Docker DNS)
Host=postgres;Port=5432;Database=witchcityrope;Username=postgres;Password=postgres
```

### Running Migrations
```bash
# From host (if .NET SDK installed)
cd src/WitchCityRope.Api
dotnet ef database update

# From container
docker-compose exec api dotnet ef database update
```

## Container Restart

### When to Restart
- After backend code changes (if hot reload fails)
- After Docker configuration changes
- When containers are unhealthy
- Before running E2E tests

### Restart Commands
```bash
# Restart specific service
docker-compose restart api

# Restart all services
docker-compose restart

# Full rebuild (after dependency changes)
docker-compose down
docker-compose build
docker-compose up -d
```

## Environment Variables

### Development .env
```env
# Web Service
VITE_API_URL=http://localhost:5655
VITE_ENV=development

# API Service
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432...
JWT_SECRET=your-secret-key-here
```

## Troubleshooting

### Container Won't Start
```bash
# Check logs
docker-compose logs <service-name>

# Rebuild container
docker-compose build <service-name>
docker-compose up -d <service-name>
```

### Port Already in Use
```bash
# Find process using port
lsof -i :5173
lsof -i :5655
lsof -i :5434

# Kill process or change port in docker-compose.yml
```

### Database Connection Issues
```bash
# Verify PostgreSQL is running
docker-compose ps postgres

# Check PostgreSQL logs
docker-compose logs postgres

# Connect to database
docker-compose exec postgres psql -U postgres -d witchcityrope
```

## Docker Compose Profiles

```bash
# Start only specific services
docker-compose --profile api up -d  # Only API + dependencies

# Start all services
docker-compose up -d  # Web + API + PostgreSQL
```

## Container Shell Access

```bash
# API container shell
docker-compose exec api /bin/bash

# PostgreSQL container shell
docker-compose exec postgres /bin/bash

# Web container shell
docker-compose exec web /bin/sh
```

## Cleanup Commands

```bash
# Remove stopped containers
docker-compose down

# Remove containers and volumes (fresh start)
docker-compose down -v

# Remove ALL Docker resources (CAREFUL!)
docker system prune -a --volumes
```

## Standards Maintenance

For detailed Docker patterns:
- Read [Docker Architecture](/docs/architecture/docker-architecture.md)
- Read [Docker Development Guide](/docs/standards-processes/development-standards/docker-development.md)
- Use `container-restart` skill for automated restart workflows

---

*This document is maintained by the Backend Developer and React Developer agents.*
