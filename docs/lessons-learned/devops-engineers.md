# Lessons Learned - DevOps Engineers
<!-- Last Updated: 2025-08-04 -->
<!-- Next Review: 2025-09-04 -->

## Docker Development

### Hot Reload Issues
**Issue**: Code changes not reflected in running containers  
**Solution**: Know when to restart containers
```bash
# Quick restart for code changes
docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart web

# Full rebuild when packages change
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d --build
```
**When to restart**:
- Program.cs changes
- Package additions/updates
- Configuration changes
- Authentication logic changes
- _Imports.razor modifications

### Container Communication
**Issue**: Services can't reach each other  
**Solution**: Use container names, not localhost
```yaml
# docker-compose.yml
services:
  web:
    environment:
      - ApiUrl=http://api:8080  # NOT http://localhost:5653
  api:
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;...
```
**Applies to**: All inter-service communication

### Volume Mounting
**Issue**: File permission problems in containers  
**Solution**: Proper volume configuration
```yaml
services:
  web:
    volumes:
      - ./src/WitchCityRope.Web:/app
      - /app/obj        # Exclude obj folder
      - /app/bin        # Exclude bin folder
```
**Applies to**: Development containers only

## PostgreSQL Specific

### Database Initialization
**Issue**: Migrations not running on fresh setup  
**Solution**: Proper initialization order
```bash
# 1. Start database first
docker-compose up -d postgres

# 2. Wait for it to be ready
docker exec witchcity-postgres pg_isready

# 3. Run migrations
dotnet ef database update

# 4. Start other services
docker-compose up -d
```

### Connection Pooling
**Issue**: "Too many connections" errors  
**Solution**: Configure connection pooling
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;...;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;"
  }
}
```

### Backup Strategy
**Issue**: No database backups  
**Solution**: Automated backup script
```bash
#!/bin/bash
docker exec witchcity-postgres pg_dump -U postgres witchcityrope_db > backup_$(date +%Y%m%d_%H%M%S).sql
```

## CI/CD Pipeline

### GitHub Actions
**Issue**: Tests failing in CI but passing locally  
**Solution**: Match CI environment exactly
```yaml
- name: Start PostgreSQL
  run: |
    docker run -d \
      -e POSTGRES_PASSWORD=postgres \
      -p 5432:5432 \
      postgres:16-alpine

- name: Wait for PostgreSQL
  run: |
    until docker exec $(docker ps -q) pg_isready; do
      sleep 1
    done
```

### Environment Variables
**Issue**: Secrets exposed in logs  
**Solution**: Use GitHub secrets properly
```yaml
env:
  ConnectionStrings__DefaultConnection: ${{ secrets.DB_CONNECTION }}
  Syncfusion__LicenseKey: ${{ secrets.SYNCFUSION_LICENSE }}
```

### Build Optimization
**Issue**: Slow CI builds  
**Solution**: Layer caching and parallel jobs
```yaml
- uses: docker/setup-buildx-action@v2
- uses: docker/build-push-action@v4
  with:
    cache-from: type=gha
    cache-to: type=gha,mode=max
```

## Monitoring & Logging

### Container Logs
**Issue**: Missing important error information  
**Solution**: Centralized log viewing
```bash
# View all logs
docker-compose logs -f

# View specific service
docker-compose logs -f web

# Save logs for analysis
docker-compose logs > logs_$(date +%Y%m%d).txt
```

### Health Checks
**Issue**: Containers marked healthy but not working  
**Solution**: Proper health check configuration
```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

## Performance Optimization

### Container Resources
**Issue**: Containers running out of memory  
**Solution**: Set resource limits
```yaml
services:
  web:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 2G
        reservations:
          memory: 1G
```

### Build Performance
**Issue**: Slow rebuilds during development  
**Solution**: Multi-stage builds with proper layering
```dockerfile
# Cache NuGet packages
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["*.csproj", "./"]
RUN dotnet restore

# Then copy source
COPY . .
RUN dotnet build
```

## Security Best Practices

### Secrets Management
**Issue**: Credentials in configuration files  
**Solution**: Use environment variables
```bash
# .env file (git ignored)
DB_PASSWORD=super_secret
SENDGRID_API_KEY=secret_key

# docker-compose.yml
env_file:
  - .env
```

### Container Security
**Issue**: Running as root in containers  
**Solution**: Use non-root user
```dockerfile
# Create app user
RUN adduser -u 5678 --disabled-password --gecos "" appuser
USER appuser
```

## Troubleshooting Guide

### Common Issues and Solutions

1. **Port already in use**
   ```bash
   # Find process using port
   lsof -i :5651
   # Kill it
   kill -9 [PID]
   ```

2. **Container won't start**
   ```bash
   # Check logs
   docker logs witchcity-web
   # Inspect container
   docker inspect witchcity-web
   ```

3. **Database connection failures**
   ```bash
   # Test connection
   docker exec witchcity-postgres psql -U postgres -c "SELECT 1"
   # Check network
   docker network inspect witchcityrope_default
   ```

## Deployment Checklist

Before deploying:
- [ ] All environment variables configured
- [ ] Database migrations tested
- [ ] Health checks passing
- [ ] Resource limits set
- [ ] Secrets properly managed
- [ ] Backup strategy in place
- [ ] Monitoring configured
- [ ] Rollback plan ready

---

*Remember: Development should mirror production as closely as possible. If it works in Docker locally, it should work in production.*