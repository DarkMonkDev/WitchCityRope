# Docker Development Setup Summary

## Overview

The Docker development environment for WitchCityRope has been configured with a comprehensive setup that supports hot reload, database persistence, and development tools.

## What Was Configured

### 1. Docker Files Structure

```
WitchCityRope/
├── .dockerignore                    # NEW: Excludes unnecessary files from Docker builds
├── .env.example                     # Existing: Template for environment variables
├── docker-compose.yml               # Existing: Main compose configuration
├── docker-compose.override.yml      # Existing: Development overrides
├── docker-dev.sh                    # Existing: Interactive development helper
├── docker-quick.sh                  # NEW: Quick command shortcuts
├── DOCKER_DEV_GUIDE.md             # NEW: Comprehensive development guide
├── src/
│   ├── WitchCityRope.Api/
│   │   └── Dockerfile              # UPDATED: Multi-stage build with dev support
│   └── WitchCityRope.Web/
│       └── Dockerfile              # UPDATED: Multi-stage build with dev support
```

### 2. Key Features

#### Hot Reload Support
- Both API and Web projects support hot reload in development mode
- File changes are automatically detected and trigger rebuilds
- No need to restart containers for code changes

#### Database Management
- SQLite database with persistent volume
- Automatic database initialization
- Database migrations support via Entity Framework

#### Development Tools
- **Mailcatcher**: Captures emails in development (port 1080)
- **SQLite Viewer**: Direct database access for debugging
- **Health Checks**: Both services expose `/health` endpoints

#### Security Features
- HTTPS support with development certificates
- Non-root user execution in production
- Secure environment variable handling

### 3. Port Mappings

| Service | HTTP Port | HTTPS Port | Description |
|---------|-----------|------------|-------------|
| Web (Blazor) | 5651 | 5652 | Frontend application |
| API | 5653 | 5654 | Backend API |
| Mailcatcher | 1080 | - | Email viewer (dev tools) |

### 4. Volume Mappings

- `./data`: SQLite database files
- `./logs`: Application logs
- `./uploads`: File uploads
- `./src`: Source code (development only)
- `~/.aspnet/https`: HTTPS certificates

## Quick Start Commands

### Using docker-dev.sh (Interactive)
```bash
# Make script executable
chmod +x docker-dev.sh

# Run interactive menu
./docker-dev.sh
```

### Using docker-quick.sh (Direct Commands)
```bash
# Make script executable
chmod +x docker-quick.sh

# Start services
./docker-quick.sh up

# View logs
./docker-quick.sh logs api

# Run migrations
./docker-quick.sh migrate

# Access container shell
./docker-quick.sh shell api
```

### Using Docker Compose Directly
```bash
# Start development environment
docker-compose up -d

# Start with dev tools
COMPOSE_PROFILES=dev-tools docker-compose up -d

# Stop everything
docker-compose down

# Clean everything (including data)
docker-compose down -v
```

## Environment Variables

Key variables to configure in `.env`:

```env
# Core Settings
ASPNETCORE_ENVIRONMENT=Development
BUILD_TARGET=development

# Security
JWT_SECRET=YourSuperSecretKeyForDevelopmentPurposesOnly!ThisShouldBeAtLeast32Characters

# External Services
SENDGRID_API_KEY=your_sendgrid_api_key
PAYPAL_CLIENT_ID=your_paypal_client_id
PAYPAL_CLIENT_SECRET=your_paypal_client_secret
GOOGLE_CLIENT_ID=your_google_client_id
GOOGLE_CLIENT_SECRET=your_google_client_secret
SYNCFUSION_LICENSE_KEY=your_syncfusion_license_key
```

## Development Workflow

1. **Initial Setup**
   ```bash
   cp .env.example .env
   # Edit .env with your values
   ./docker-dev.sh
   ```

2. **Daily Development**
   ```bash
   # Start services
   ./docker-quick.sh up
   
   # Make code changes - hot reload will handle them
   
   # View logs if needed
   ./docker-quick.sh logs api
   ```

3. **Database Changes**
   ```bash
   # After modifying entities
   ./docker-quick.sh migrate
   ```

4. **Testing**
   ```bash
   # Run tests
   ./docker-quick.sh test
   ```

## Architecture Notes

### Multi-Stage Dockerfiles
- **development**: Includes SDK, hot reload support, and dev tools
- **build**: Compiles the application
- **publish**: Creates release artifacts
- **final**: Minimal runtime image for production

### Network Architecture
- All services communicate on `witchcity-network`
- API is accessible to Web via `http://api:8080`
- External access via mapped ports

### Build Context
- Docker build context is at repository root
- Allows access to all project dependencies
- Proper .dockerignore prevents unnecessary file copying

## Troubleshooting

### Common Issues

1. **Port Already in Use**
   - Change ports in docker-compose.yml
   - Or stop conflicting services

2. **Hot Reload Not Working**
   - Ensure `BUILD_TARGET=development` in .env
   - Check volume mappings in docker-compose.override.yml

3. **Database Connection Issues**
   - Verify database file exists: `ls ./data/`
   - Check permissions on data directory

4. **Certificate Errors**
   - Delete `~/.aspnet/https` directory
   - Restart services (certificates regenerate automatically)

## Best Practices

1. **Development**
   - Always use `.env` for configuration (never commit it)
   - Use docker-compose.override.yml for local customizations
   - Keep containers running for hot reload

2. **Performance**
   - On Windows/WSL2: Keep code in WSL2 filesystem
   - Use `:cached` volume options (already configured)
   - Allocate sufficient Docker resources

3. **Security**
   - Never use development secrets in production
   - Regenerate all keys/secrets for production
   - Use proper SSL certificates in production

## Next Steps

1. Configure your `.env` file with appropriate values
2. Run `./docker-dev.sh` and select option 1
3. Access the application at http://localhost:5000
4. Start developing with hot reload!

For production deployment, create separate Dockerfiles without development dependencies and use proper orchestration (Kubernetes, Docker Swarm, etc.).