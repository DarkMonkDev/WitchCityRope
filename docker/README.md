# Docker Development Setup for Witch City Rope

This guide explains how to use Docker for local development of the Witch City Rope application.

## Prerequisites

- Docker Desktop (Windows/Mac) or Docker Engine (Linux)
- Docker Compose v2.0+
- .NET 9.0 SDK (optional, for IDE support)
- Visual Studio 2022 or VS Code (recommended)

## Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/WitchCityRope.git
   cd WitchCityRope
   ```

2. **Copy environment variables**
   ```bash
   cp .env.example .env
   ```

3. **Edit `.env` file** with your configuration:
   - Configure SendGrid API key (or use mailcatcher for development)
   - Set up PayPal sandbox credentials
   - Configure Google OAuth credentials
   - Set PostgreSQL connection string (auto-configured in development)

4. **Start the development environment**
   ```bash
   # Start all services with hot reload
   docker-compose up -d
   
   # Or start with development tools (mailcatcher, sqlite viewer)
   docker-compose --profile dev-tools up -d
   ```

5. **Access the applications**
   - Web (React): http://localhost:5173
   - API: http://localhost:5653 or https://localhost:5654
   - Mailcatcher (if enabled): http://localhost:1080

## Development Workflow

### Hot Reload
Both the API and Web projects are configured for hot reload. Any changes to C# files in the API or TypeScript/React files in the web app will automatically trigger a rebuild and browser refresh via Vite.

### Database Management

**View PostgreSQL database:**
```bash
# Access the database
docker-compose exec postgres psql -U witchcityrope -d witchcityrope_dev

# Or use a database GUI tool
# Connect to: localhost:5433, user: witchcityrope, database: witchcityrope_dev
```

**Run migrations:**
```bash
# API migrations
docker-compose exec api dotnet ef database update --project /src/WitchCityRope.Infrastructure

# Or from host (if .NET SDK installed)
dotnet ef database update --project src/WitchCityRope.Infrastructure --startup-project src/WitchCityRope.Api
```

### Debugging

**View logs:**
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f api
docker-compose logs -f web
```

**Access container shell:**
```bash
docker-compose exec api /bin/bash
docker-compose exec web /bin/bash
```

### Testing

**Run tests in containers:**
```bash
# Unit tests
docker-compose exec api dotnet test /workspace/tests/WitchCityRope.Api.Tests
docker-compose exec web dotnet test /workspace/tests/WitchCityRope.Web.Tests

# Integration tests
docker-compose exec api dotnet test /workspace/tests/WitchCityRope.IntegrationTests
```

## Configuration

### Environment Variables

Key environment variables in `docker-compose.yml`:

- `ASPNETCORE_ENVIRONMENT`: Set to Development for detailed errors
- `ApiBaseUrl`: Internal API URL for container communication
- `ApiBaseUrlExternal`: External API URL for browser access
- `ConnectionStrings__DefaultConnection`: PostgreSQL connection string
- Database auto-initialization enabled in development
- `Email__SendGrid__ApiKey`: SendGrid API key

### Volumes

- `postgres_data`: PostgreSQL database files
- `./logs`: Application logs
- `./uploads`: User uploads
- `./apps`: Source code (mounted for hot reload)
- `./packages`: Shared TypeScript types

### Ports

- 5173: React Web application (Vite dev server)
- 5653/5654: API (HTTP/HTTPS)
- 5433: PostgreSQL database
- 1080: Mailcatcher web interface (dev-tools profile)
- 1025: Mailcatcher SMTP (dev-tools profile)

## Production Build

To build production images:

```bash
# Build with production target
docker-compose build --build-arg BUILD_TARGET=final

# Or use the production compose file
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

## Troubleshooting

### Certificate Issues
If you encounter HTTPS certificate errors:

```bash
# Generate development certificates
dotnet dev-certs https --clean
dotnet dev-certs https --trust
dotnet dev-certs https -ep ~/.aspnet/https/aspnetapp.pfx -p password
```

### Permission Issues
If you encounter permission issues with PostgreSQL:

```bash
# Reset PostgreSQL data
docker-compose down -v
docker-compose up -d postgres
```

### Hot Reload Not Working
1. Ensure `DOTNET_USE_POLLING_FILE_WATCHER=true` is set
2. Check that volumes are mounted correctly
3. Restart the containers: `docker-compose restart`

### Database Connection Issues
If PostgreSQL connection fails:
1. Stop all containers: `docker-compose down`
2. Remove volumes: `docker-compose down -v`
3. Start containers: `docker-compose up -d`
4. Wait for auto-initialization to complete

## Clean Up

```bash
# Stop all containers
docker-compose down

# Remove volumes (WARNING: deletes data)
docker-compose down -v

# Clean up everything
docker system prune -a --volumes
```

## Tips

1. **Use Docker Compose profiles** to enable optional services:
   ```bash
   COMPOSE_PROFILES=dev-tools,debug docker-compose up -d
   ```

2. **Override configurations** without modifying docker-compose.yml:
   - Create `docker-compose.override.yml` (already included)
   - Add custom environment variables to `.env`

3. **Performance on Windows/Mac**:
   - Use delegated mounts for better performance
   - Consider using WSL2 on Windows
   - Allocate sufficient resources in Docker Desktop

4. **VS Code Integration**:
   - Install Docker and C# extensions
   - Use Remote-Containers for development inside containers
   - Debug directly in containers with launch.json configuration

## Related Documentation

- [Architecture Overview](../docs/architecture/CURRENT-ARCHITECTURE-SUMMARY.md)
- [Development Quick Start](../docs/architecture/quick-start-guide.md)
- [Technical Stack](../docs/architecture/technical-stack.md)