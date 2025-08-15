# PostgreSQL Configuration Reference for WitchCityRope

## ⚠️ CRITICAL: DO NOT MODIFY THESE SETTINGS

This document serves as the authoritative reference for PostgreSQL configuration in the WitchCityRope project. Any deviation from these settings will cause authentication failures and break the application.

## Official PostgreSQL Credentials

### Password and Authentication
- **Password**: `WitchCity2024!`
- **Username**: `postgres`
- **Database Name**: `witchcityrope_db`

**IMPORTANT**: This password is hardcoded in multiple places and must NEVER be changed:
- Docker Compose environment variables
- Application connection strings

## Container Configurations by Environment

### 1. Docker Compose

**Command**: `docker-compose up -d`

- **Container Name**: `witchcityrope-db`
- **Port**: `5433`
- **Volume**: `witchcityrope_postgres_dev_data`
- **Connection String**: `Host=witchcityrope-db;Port=5432;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!`

### 2. Direct PostgreSQL Installation

- **Port**: `5432` (PostgreSQL default)
- **Connection String**: `Host=localhost;Port=5432;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!`

## Common Issues and Solutions

### Issue: "password authentication failed for user 'postgres'"

This error occurs when:
1. An old PostgreSQL volume exists with a different password
2. The container was previously initialized with different credentials
3. Multiple PostgreSQL instances are running on conflicting ports

### Solution: Clean Slate Approach

```bash
# 1. Stop all PostgreSQL containers
docker ps -a | grep postgres | awk '{print $1}' | xargs -r docker stop
docker ps -a | grep postgres | awk '{print $1}' | xargs -r docker rm

# 2. Remove Docker Compose PostgreSQL volume (if needed)
docker volume rm witchcityrope_postgres_dev_data

# 3. Restart Docker Compose
docker-compose up -d
```

## Port Conflict Resolution

### Check What's Using PostgreSQL Ports

```bash
# Check port 5432 (standard PostgreSQL)
sudo lsof -i :5432

# Check port 5433 (Docker Compose)
sudo lsof -i :5433

```

### Kill Conflicting Processes

```bash
# Find and kill process by port
sudo kill -9 $(sudo lsof -t -i:5432)
```

## Database Connection Testing

### Test Docker Compose PostgreSQL Connection
```bash
# From WSL/Linux
psql -h localhost -p 5433 -U postgres -d witchcityrope_db
# Password: WitchCity2024!
```

### Test from Application Container
```bash
# Exec into the web container
docker exec -it witchcityrope-web bash

# Test connection from inside container
psql -h witchcityrope-db -p 5432 -U postgres -d witchcityrope_db
```


## Important Notes for Future Development

1. **NEVER** modify the PostgreSQL password in any configuration file
2. **DO NOT** run multiple PostgreSQL instances simultaneously
3. **CHECK** for existing volumes before reporting authentication errors

## Connection String Priority in Application

The application uses the `ConnectionStrings:DefaultConnection` configuration for database connections.

## Volume Persistence

- **Docker Compose**: Uses named volume `witchcityrope_postgres_dev_data`
- **Data Persistence**: All data is preserved between container restarts
- **Clean Start**: Remove volumes to start with a fresh database

## Last Updated

This configuration was last verified and updated on: December 30, 2024

**Maintained by**: Development Team
**Authority**: This document supersedes any conflicting PostgreSQL configuration found elsewhere in the codebase.