# PostgreSQL Migration Guide

This document outlines the successful migration from SQLite to PostgreSQL for the WitchCityRope application.

## Migration Summary

The WitchCityRope application has been successfully migrated from SQLite to PostgreSQL. This migration provides better performance, scalability, and production-readiness.

### Key Changes

1. **Database Engine**: SQLite → PostgreSQL 16 (Alpine)
2. **Connection Method**: File-based → Network-based (Docker container)
3. **Development Port**: 5433 (to avoid conflicts with other PostgreSQL instances)
4. **Production Port**: 5432 (standard PostgreSQL port)

## Architecture Overview

### Docker Container Setup

```yaml
services:
  postgres:
    image: postgres:16-alpine
    container_name: witchcity-postgres
    ports:
      - "5433:5432"  # Development
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: WitchCity2024!
      POSTGRES_DB: witchcityrope_dev
    volumes:
      - postgres_dev_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d witchcityrope_dev"]
      interval: 10s
      timeout: 5s
      retries: 5
```

## Connection Strings

### Development
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5433;Database=witchcityrope_dev;Username=postgres;Password=WitchCity2024!"
}
```

### Production (Docker)
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=postgres;Port=5432;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!"
}
```

## Migration Steps Completed

### 1. Updated NuGet Packages
- Replaced `Microsoft.EntityFrameworkCore.Sqlite` with `Npgsql.EntityFrameworkCore.PostgreSQL` (v9.0.2)
- All Entity Framework Core packages remain at v9.0.0

### 2. Updated Configuration Files
- Modified all DbContext configurations to use `UseNpgsql()`
- Updated connection strings in all appsettings.json files
- Configured Docker Compose for PostgreSQL

### 3. Created New Migrations
```bash
# Removed old SQLite migrations
rm -rf src/WitchCityRope.Infrastructure/Migrations

# Created new PostgreSQL migration
dotnet ef migrations add InitialPostgreSQLMigration \
  --project src/WitchCityRope.Infrastructure \
  --startup-project src/WitchCityRope.Api

# Applied migration to database
dotnet ef database update \
  --project src/WitchCityRope.Infrastructure \
  --startup-project src/WitchCityRope.Api
```

### 4. Seeded Test Data
Created a database seeder tool that populates the database with:
- 5 test users (Admin, Staff, Organizer, Member, Guest)
- 10 test events (various types)
- Authentication records for all users

Test accounts:
- admin@witchcityrope.com / Test123! (Administrator, Vetted)
- staff@witchcityrope.com / Test123! (Moderator, Vetted)
- organizer@witchcityrope.com / Test123! (Moderator/Organizer, Vetted)
- member@witchcityrope.com / Test123! (Member, Not Vetted)
- guest@witchcityrope.com / Test123! (Attendee, Not Vetted)

## Database Schema Changes

### PostgreSQL-Specific Optimizations
1. **Data Types**:
   - `TEXT` → `text` or `character varying(n)`
   - `INTEGER` (for booleans) → `boolean`
   - `REAL` → `double precision`
   - `BLOB` → `bytea`
   - DateTime → `timestamp with time zone`

2. **Indexes**: All performance indexes were recreated automatically by EF Core migrations

3. **JSON Support**: PricingTiers and References columns use PostgreSQL's native JSON support

## Running the Application

### Start PostgreSQL
```bash
docker-compose up -d postgres
```

### Run Database Seeder (if needed)
```bash
cd tools/DatabaseSeeder
dotnet run
```

### Start the Application
```bash
# Web application
cd src/WitchCityRope.Web
dotnet run

# API
cd src/WitchCityRope.Api
dotnet run
```


## Production Deployment

For production deployment:

1. Use environment variables for sensitive data:
   ```yaml
   environment:
     - POSTGRES_PASSWORD=${DB_PASSWORD}
     - ConnectionStrings__DefaultConnection=${CONNECTION_STRING}
   ```

2. Enable SSL/TLS for database connections:
   ```
   Host=postgres;Port=5432;Database=witchcityrope_db;Username=postgres;Password=xxx;SSL Mode=Require
   ```

3. Configure connection pooling:
   ```
   Maximum Pool Size=100;Connection Idle Lifetime=300
   ```

4. Set up automated backups using pg_dump

## Performance Improvements

The migration to PostgreSQL provides:
- Better concurrent user handling
- Improved query performance for complex joins
- Native JSON support for complex data types
- Better indexing capabilities
- Production-ready replication and backup options

## Monitoring

Monitor the PostgreSQL database using:
- pgAdmin (included in docker-compose with --profile tools)
- Docker logs: `docker logs witchcity-postgres`
- PostgreSQL logs in the container

## Troubleshooting

### Common Issues

1. **Port conflicts**: If port 5432 is in use, we're using 5433 for development
2. **Connection refused**: Ensure Docker container is running and healthy
3. **Migration errors**: Check connection string and database permissions

### Useful Commands

```bash
# Check PostgreSQL container status
docker ps | grep witchcity-postgres

# View PostgreSQL logs
docker logs witchcity-postgres

# Connect to PostgreSQL CLI
docker exec -it witchcity-postgres psql -U postgres -d witchcityrope_dev

# List tables
\dt

# Check data
SELECT COUNT(*) FROM "Users";
SELECT COUNT(*) FROM "Events";
```

## Next Steps

1. Configure automated backups
2. Set up monitoring and alerting
3. Implement connection retry logic
4. Configure read replicas for scaling
5. Set up proper SSL certificates for production