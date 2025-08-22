# Docker Development Guide for Witch City Rope

## Quick Start

1. **Prerequisites**
   - Docker Desktop installed and running
   - .NET 9 SDK (for local development)
   - Git

2. **Initial Setup**
   ```bash
   # Clone the repository
   git clone <repository-url>
   cd WitchCityRope

   # Copy environment variables
   cp .env.example .env
   # Edit .env with your configuration values

   # Make the development script executable
   chmod +x docker-dev.sh

   # Start the development environment
   ./docker-dev.sh
   # Select option 1 to start the basic development environment
   ```

3. **Access the Applications**
   - Web (Blazor): http://localhost:5651
   - Web (HTTPS): https://localhost:5652
   - API: http://localhost:5653
   - API (HTTPS): https://localhost:5654

## Architecture

The Docker setup consists of:

- **API Service**: ASP.NET Core Web API running on ports 5653/5654
- **Web Service**: Blazor Server application running on ports 5651/5652
- **SQLite Database**: Persistent volume for data storage
- **Development Tools** (optional): Mailcatcher, SQLite viewer

## Development Workflow

### Starting the Environment

```bash
# Start basic development environment
./docker-dev.sh
# Select option 1

# Or start with development tools
./docker-dev.sh
# Select option 2
```

### Hot Reload

Both the API and Web projects support hot reload in development mode:

1. Edit any C# file in the `src/` directory
2. Save the file
3. The application will automatically recompile and restart
4. Refresh your browser to see changes

### Database Management

```bash
# Run migrations
./docker-dev.sh
# Select option 5

# Access SQLite database (when debug profile is active)
docker-compose exec sqlite-viewer sqlite3 /data/witchcityrope-dev.db
```

### Viewing Logs

```bash
# View all logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f api
docker-compose logs -f web
```

### Running Tests

```bash
./docker-dev.sh
# Select option 6
# Then choose which test suite to run
```

## Environment Variables

Key environment variables in `.env`:

- `JWT_SECRET`: Secret key for JWT tokens (min 32 characters)
- `SENDGRID_API_KEY`: For email functionality
- `PAYPAL_CLIENT_ID/SECRET`: For payment processing
- `GOOGLE_CLIENT_ID/SECRET`: For Google OAuth

## Troubleshooting

### Port Conflicts

If you get port binding errors:
```bash
# Check what's using the ports
lsof -i :5651
lsof -i :5653

# Stop conflicting services or change ports in docker-compose.yml
```

### Certificate Issues

If HTTPS isn't working:
```bash
# Regenerate development certificates
rm -rf ~/.aspnet/https
./docker-dev.sh
# The script will regenerate certificates automatically
```

### Database Issues

If the database is corrupted or you need a fresh start:
```bash
# Warning: This will delete all data!
./docker-dev.sh
# Select option 7 (Clean up)
```

### Container Shell Access

For debugging, you can access container shells:
```bash
# API container
docker-compose exec api /bin/bash

# Web container
docker-compose exec web /bin/bash
```

## Docker Commands Reference

```bash
# Start services
docker-compose up -d

# Stop services
docker-compose down

# Rebuild images
docker-compose build --no-cache

# View running containers
docker-compose ps

# Remove all data (careful!)
docker-compose down -v

# Start with specific profile
COMPOSE_PROFILES=dev-tools docker-compose up -d
```

## Performance Tips

1. **Windows/WSL2**: Place the project in the WSL2 filesystem for better performance
2. **macOS**: Use `:cached` or `:delegated` volume options (already configured)
3. **Resource Allocation**: Ensure Docker Desktop has sufficient CPU/RAM allocated

## Security Notes

- The default JWT secret in `.env.example` is for development only
- Never commit `.env` files with real credentials
- Use proper certificates for production deployments
- The development setup uses relaxed CORS settings

## Additional Services

### Mailcatcher (Development Email)

When running with dev tools profile:
- SMTP Server: localhost:1025
- Web Interface: http://localhost:1080

### SQLite Viewer

Available with debug profile:
```bash
COMPOSE_PROFILES=debug docker-compose up -d
docker-compose exec sqlite-viewer sqlite3 /data/witchcityrope-dev.db
```

## Next Steps

1. Configure your `.env` file with appropriate values
2. Run database migrations
3. Create a test user account
4. Start developing!

For production deployment, see the separate production Docker documentation.