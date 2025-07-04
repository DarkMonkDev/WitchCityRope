# Docker PostgreSQL Setup for WitchCityRope

This Docker Compose configuration sets up the WitchCityRope application with PostgreSQL as the database backend.

## ⚠️ CRITICAL: PostgreSQL Configuration

**DO NOT CHANGE THE POSTGRESQL PASSWORD!** The password `WitchCity2024!` is hardcoded in multiple places and changing it will break authentication. See `/docs/database/POSTGRESQL_CONFIGURATION.md` for the authoritative configuration reference.

## Quick Start

1. **Copy the environment file:**
   ```bash
   cp .env.example .env
   ```

2. **Edit `.env` file** with your configuration values (especially API keys)

3. **Start the services:**
   ```bash
   docker-compose up -d
   ```

4. **Access the application:**
   - Web UI: http://localhost:5651
   - API: http://localhost:5653
   - pgAdmin (optional): http://localhost:5050

## Services

### PostgreSQL Database
- **Image**: postgres:16-alpine (latest stable)
- **Port**: 5433 (Docker Compose) / 5432 (Direct)
- **Database**: witchcityrope_db
- **Username**: postgres
- **Password**: WitchCity2024! (DO NOT CHANGE - hardcoded in application)
- **Health checks**: Configured with pg_isready

### API Service
- ASP.NET Core API
- Automatically connects to PostgreSQL
- Runs database migrations on startup
- Port: 5653 (HTTP), 5654 (HTTPS)

### Web Service
- Blazor Server application
- Connects to API service
- Port: 5651 (HTTP), 5652 (HTTPS)

### pgAdmin (Optional)
- Database management tool
- Port: 5050
- Default login: admin@witchcityrope.local / admin
- Enable with: `docker-compose --profile tools up -d`

## Development Mode

The `docker-compose.override.yml` file automatically configures development settings:
- Hot reload enabled
- Separate development database (witchcityrope_dev)
- Debug logging
- Source code mounting for live updates

## Common Commands

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down

# Stop and remove volumes (careful - deletes data!)
docker-compose down -v

# Start with pgAdmin
docker-compose --profile tools up -d

# Connect to PostgreSQL
docker exec -it witchcity-postgres psql -U postgres -d witchcityrope_db

# Run migrations manually
docker exec -it witchcity-api dotnet ef database update

# View API logs
docker logs -f witchcity-api

# View Web logs
docker logs -f witchcity-web
```

## Database Connection Strings

### From Host Machine (Docker Compose)
```
Host=localhost;Port=5433;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!
```


### From Docker Containers
```
Host=postgres;Port=5432;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!
```


## Troubleshooting

### Database Connection Issues
1. Ensure PostgreSQL is healthy: `docker-compose ps`
2. Check logs: `docker logs witchcity-postgres`
3. Verify connection: `docker exec -it witchcity-postgres pg_isready`

### Migration Issues
1. Check API logs: `docker logs witchcity-api`
2. Run migrations manually: `docker exec -it witchcity-api dotnet ef database update`

### Port Conflicts
If ports are already in use, modify the port mappings in docker-compose.yml:
```yaml
ports:
  - "15432:5432"  # Change 15432 to any free port
```

## Production Considerations

1. **Password Management**: The PostgreSQL password is currently hardcoded. For production, implement proper secrets management without changing the application code that expects this specific password.
2. **Use secrets management** for sensitive data
3. **Enable SSL/TLS** for database connections
4. **Configure backups** for PostgreSQL data
5. **Use external PostgreSQL** service for better performance
6. **Set up monitoring** for all services

**NOTE**: If you need to change the PostgreSQL password for production, you must update it in multiple places in the codebase, not just in environment variables.

## Data Persistence

PostgreSQL data is persisted in Docker volumes:
- Production: `postgres_data` volume
- Development: `postgres_dev_data` volume

To backup PostgreSQL data:
```bash
docker exec witchcity-postgres pg_dump -U postgres witchcityrope_db > backup.sql
```

To restore from backup:
```bash
docker exec -i witchcity-postgres psql -U postgres witchcityrope_db < backup.sql
```