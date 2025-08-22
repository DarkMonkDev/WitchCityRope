# Docker Quick Reference for WitchCityRope

## Quick Start
```bash
# Start everything
./start-docker.sh
# or
docker-compose up -d

# Access the application
https://localhost:5652
```

## Common Commands

### Service Management
```bash
# Start all services
docker-compose up -d

# Stop all services
docker-compose down

# Restart a specific service
docker-compose restart web

# View service status
docker-compose ps
```

### Logs
```bash
# View all logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f web
docker-compose logs -f api
docker-compose logs -f postgres
```

### Database Operations
```bash
# Run migrations
docker-compose exec api dotnet ef database update

# Access PostgreSQL shell
docker-compose exec postgres psql -U postgres -d witchcityrope_db

# Backup database
docker-compose exec postgres pg_dump -U postgres witchcityrope_db > backup.sql

# Restore database
docker-compose exec -T postgres psql -U postgres witchcityrope_db < backup.sql
```

### Development
```bash
# Rebuild after code changes
docker-compose up -d --build

# Run tests
docker-compose exec web dotnet test

# Access container shell
docker-compose exec web bash
docker-compose exec api bash
```

### Cleanup
```bash
# Stop and remove containers
docker-compose down

# Remove everything including volumes (fresh start)
docker-compose down -v

# Clean up Docker system
docker system prune -a
```

## Service URLs

- **Web Application**: https://localhost:5652
- **API Service**: https://localhost:5654
- **PostgreSQL**: localhost:5433
- **pgAdmin** (if enabled): http://localhost:5050

## Troubleshooting

### Check if services are healthy
```bash
docker-compose ps
docker inspect witchcity-web --format='{{.State.Health.Status}}'
```

### If a service won't start
```bash
# Check logs for errors
docker-compose logs [service_name]

# Rebuild the service
docker-compose up -d --build [service_name]
```

### Port conflicts
```bash
# Find what's using a port (Linux/WSL)
sudo lsof -i :5652

# Change ports in docker-compose.yml if needed
```

### Database connection issues
```bash
# Test database connection
docker-compose exec postgres pg_isready -U postgres

# Check if migrations are needed
docker-compose exec api dotnet ef migrations list
```