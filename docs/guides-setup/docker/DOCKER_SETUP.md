# Docker Setup Guide for WitchCityRope

This guide provides comprehensive instructions for setting up and running WitchCityRope using Docker Compose.

## Prerequisites

1. **Docker Desktop** (Windows/Mac) or **Docker Engine** (Linux)
   - Download from: https://www.docker.com/products/docker-desktop/
   - Ensure Docker is running before proceeding

2. **Docker Compose**
   - Usually included with Docker Desktop
   - Verify installation: `docker-compose --version`

3. **Git** (to clone the repository)

## Quick Start

```bash
# Clone the repository
git clone https://github.com/DarkMonkDev/WitchCityRope.git
cd WitchCityRope

# Copy environment variables
cp .env.example .env

# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Access the application
# Web: https://localhost:5652
# API: https://localhost:5654
```

## Service Architecture

The application consists of three main services:

### 1. PostgreSQL Database (`postgres`)
- **Image**: postgres:16-alpine
- **Port**: 5433 (mapped from container's 5432)
- **Database**: witchcityrope_db
- **Username**: postgres
- **Password**: WitchCity2024! (configurable via .env)

### 2. API Service (`api`)
- **Port**: 5653 (HTTP), 5654 (HTTPS)
- **Depends on**: PostgreSQL
- **Environment**: Development/Staging/Production

### 3. Web Service (`web`) - React Application
- **Port**: 5651 (HTTP), 5652 (HTTPS)
- **Depends on**: API for backend services
- **Default URL**: https://localhost:5652

### 4. pgAdmin (Optional)
- **Port**: 5050
- **Profile**: tools (not started by default)
- **Usage**: `docker-compose --profile tools up -d`

## Development Workflow

### Starting the Application

```bash
# Start all services (excluding optional tools)
docker-compose up -d

# Start with specific profile (includes pgAdmin)
docker-compose --profile tools up -d

# View real-time logs
docker-compose logs -f

# View logs for specific service
docker-compose logs -f web
docker-compose logs -f api
docker-compose logs -f postgres
```

### Stopping the Application

```bash
# Stop all containers (preserves data)
docker-compose down

# Stop and remove all data (fresh start)
docker-compose down -v

# Stop specific service
docker-compose stop web
```

### Database Management

```bash
# Run database migrations
docker-compose exec api dotnet ef database update

# Access PostgreSQL shell
docker-compose exec postgres psql -U postgres -d witchcityrope_db

# Create database backup
docker-compose exec postgres pg_dump -U postgres witchcityrope_db > backup.sql

# Restore database backup
docker-compose exec -T postgres psql -U postgres witchcityrope_db < backup.sql

# Check database health
docker-compose exec postgres pg_isready -U postgres
```

### Development Commands

```bash
# Rebuild containers after code changes
docker-compose up -d --build

# Rebuild specific service
docker-compose up -d --build web

# Execute commands in running container
docker-compose exec web dotnet test
docker-compose exec api dotnet ef migrations add MigrationName

# View container resource usage
docker stats

# Clean up unused Docker resources
docker system prune -a
```

## Environment Configuration

### Essential Environment Variables

Create a `.env` file in the project root:

```env
# Database Configuration
POSTGRES_USER=postgres
POSTGRES_PASSWORD=WitchCity2024!
POSTGRES_DB=witchcityrope_db

# Application Environment
ASPNETCORE_ENVIRONMENT=Development

# JWT Configuration
JWT_SECRET=YourSuperSecretKeyForDevelopmentPurposesOnly!123

# Syncfusion License
SYNCFUSION_LICENSE_KEY=your_license_key_here

# Email Configuration (SendGrid)
SENDGRID_API_KEY=your_sendgrid_key_here

# PayPal Configuration
PAYPAL_CLIENT_ID=your_paypal_client_id
PAYPAL_CLIENT_SECRET=your_paypal_secret
PAYPAL_MODE=sandbox

# Google OAuth
GOOGLE_CLIENT_ID=your_google_client_id
GOOGLE_CLIENT_SECRET=your_google_client_secret

# pgAdmin (optional)
PGADMIN_EMAIL=admin@example.com
PGADMIN_PASSWORD=admin
```

## Connection Strings

### From Docker Containers (Internal)

The application automatically uses the correct connection strings when running in Docker:

```
Host=postgres;Port=5432;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!
```

### From Host Machine (External)

When connecting from your host machine (e.g., for database tools):

```
Host=localhost;Port=5433;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!
```

### From WSL2

If using WSL2, you may need to use the Docker host IP:

```
Host=host.docker.internal;Port=5433;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!
```

## Troubleshooting

### Port Conflicts

If you encounter port conflicts:

1. **Check what's using the port**:
   ```bash
   # Linux/Mac/WSL
   sudo lsof -i :5652
   
   # Windows PowerShell
   netstat -ano | findstr :5652
   ```

2. **Change ports in docker-compose.yml**:
   ```yaml
   services:
     web:
       ports:
         - "8080:8080"  # Change 5651 to 8080
         - "8443:8081"  # Change 5652 to 8443
   ```

### Database Connection Issues

1. **Verify PostgreSQL is running**:
   ```bash
   docker-compose ps
   docker-compose logs postgres
   ```

2. **Test database connection**:
   ```bash
   docker-compose exec postgres pg_isready -U postgres
   ```

3. **Check connection from API**:
   ```bash
   docker-compose exec api ping postgres
   ```

### Container Won't Start

1. **Check logs**:
   ```bash
   docker-compose logs [service_name]
   ```

2. **Verify Docker resources**:
   ```bash
   docker system df
   docker system prune -a  # Clean up if low on space
   ```

3. **Rebuild from scratch**:
   ```bash
   docker-compose down -v
   docker-compose build --no-cache
   docker-compose up -d
   ```

### SSL Certificate Issues

For local development, the application uses self-signed certificates. To trust them:

1. **Export the certificate**:
   ```bash
   docker-compose exec web cat /https/aspnetapp.pfx > cert.pfx
   ```

2. **Install in your certificate store** (varies by OS)

3. **Or disable SSL validation** for local development only

### Performance Issues

1. **Allocate more resources to Docker**:
   - Docker Desktop > Settings > Resources
   - Increase CPU and Memory limits

2. **Use production builds**:
   ```bash
   BUILD_TARGET=final docker-compose up -d
   ```

3. **Enable BuildKit**:
   ```bash
   export DOCKER_BUILDKIT=1
   docker-compose build
   ```

## Health Checks

All services include health checks:

```bash
# Check all service health
docker-compose ps

# Detailed health status
docker inspect witchcity-web --format='{{.State.Health.Status}}'
docker inspect witchcity-api --format='{{.State.Health.Status}}'
docker inspect witchcity-postgres --format='{{.State.Health.Status}}'

# Manual health check endpoints
curl http://localhost:5651/health
curl http://localhost:5653/health
```

## Production Deployment

For production deployment:

1. **Use docker-compose.prod.yml** (if available)
2. **Set appropriate environment variables**
3. **Use secrets management** for sensitive data
4. **Enable SSL/TLS** with proper certificates
5. **Set up reverse proxy** (nginx, traefik)
6. **Configure backups** for PostgreSQL

```bash
# Production deployment example
ASPNETCORE_ENVIRONMENT=Production docker-compose -f docker-compose.prod.yml up -d
```

## Useful Docker Commands

```bash
# View running containers
docker ps

# View all containers (including stopped)
docker ps -a

# Remove all stopped containers
docker container prune

# View Docker networks
docker network ls

# Inspect network
docker network inspect witchcity-network

# View volumes
docker volume ls

# Inspect volume
docker volume inspect witchcityrope_postgres_data

# Follow logs with timestamps
docker-compose logs -f -t

# Export container logs
docker-compose logs > logs.txt
```

## Getting Help

If you encounter issues:

1. Check the logs: `docker-compose logs -f [service_name]`
2. Verify prerequisites are installed correctly
3. Ensure ports are not in use by other applications
4. Check the [GitHub Issues](https://github.com/DarkMonkDev/WitchCityRope/issues)
5. Review the [main documentation](README.md)