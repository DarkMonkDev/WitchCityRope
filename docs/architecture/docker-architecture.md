# Docker Architecture Documentation

## Overview

WitchCityRope uses Docker containerization for both development and production environments. This document provides a comprehensive guide to the Docker architecture, deployment strategies, and best practices.

## Architecture Overview

### Container Strategy

The application follows a microservices-inspired architecture with the following containers:

1. **Web Application Container** (`witchcityrope-web`)
   - ASP.NET Core 9.0 Blazor Server application
   - Handles all HTTP requests and SignalR connections
   - Communicates with database via Entity Framework Core

2. **Database Container** (`witchcityrope-db`)
   - PostgreSQL 16 Alpine (lightweight Linux distribution)
   - Stores all application data
   - Isolated from external network in production

3. **Reverse Proxy Container** (`witchcityrope-proxy`) - Production only
   - Nginx Alpine
   - Handles SSL termination
   - Load balancing and request routing
   - Static file serving optimization

## Container Specifications

### Web Application Container

```yaml
Container Name: witchcityrope-web
Base Image: mcr.microsoft.com/dotnet/aspnet:9.0
Build Image: mcr.microsoft.com/dotnet/sdk:9.0
Exposed Ports: 8080 (HTTP), 8443 (HTTPS)
Health Check: HTTP GET /health
Resource Limits:
  - Memory: 2GB (production)
  - CPU: 1.5 cores (production)
```

### Database Container

```yaml
Container Name: witchcityrope-db
Image: postgres:16-alpine
Exposed Port: 5432
Health Check: pg_isready command
Resource Limits:
  - Memory: 1GB (production)
  - CPU: 1 core (production)
Storage:
  - Volume: postgres_data
  - Mount: /var/lib/postgresql/data
```

### Reverse Proxy Container

```yaml
Container Name: witchcityrope-proxy
Image: nginx:alpine
Exposed Ports: 80, 443
Health Check: HTTP GET /health
Resource Limits:
  - Memory: 512MB
  - CPU: 0.5 cores
```

## Network Architecture

### Development Network

```
┌─────────────────────────────────────────────────────────┐
│                    Host Machine                          │
│                                                          │
│  ┌─────────────────────────────────────────────────┐    │
│  │           Docker Bridge Network                  │    │
│  │           (witchcityrope-network)               │    │
│  │                                                 │    │
│  │  ┌──────────────┐       ┌──────────────┐      │    │
│  │  │  Web App     │       │  PostgreSQL  │      │    │
│  │  │  Container   │◄─────►│  Container   │      │    │
│  │  │  Port: 8080  │       │  Port: 5432  │      │    │
│  │  └──────────────┘       └──────────────┘      │    │
│  │         ▲                                      │    │
│  └─────────┼──────────────────────────────────────┘    │
│            │                                            │
│  ┌─────────▼──────────┐                               │
│  │  Host Ports:       │                               │
│  │  - 5000 → 8080     │                               │
│  │  - 5432 → 5432     │                               │
│  └────────────────────┘                               │
└─────────────────────────────────────────────────────────┘
```

### Production Network

```
┌─────────────────────────────────────────────────────────┐
│                    Internet                              │
└────────────────────────┬─────────────────────────────────┘
                         │
                    ┌────▼────┐
                    │  Nginx  │
                    │  Proxy  │
                    │ 80, 443 │
                    └────┬────┘
                         │
┌─────────────────────────┼─────────────────────────────────┐
│                  Frontend Network                          │
│                         │                                  │
│              ┌──────────▼──────────┐                      │
│              │    Web App          │                      │
│              │    Container        │                      │
│              │    Port: 8080       │                      │
│              └──────────┬──────────┘                      │
│                         │                                  │
└─────────────────────────┼─────────────────────────────────┘
                          │
┌─────────────────────────┼─────────────────────────────────┐
│                  Backend Network (Internal)                │
│                         │                                  │
│              ┌──────────▼──────────┐                      │
│              │    PostgreSQL       │                      │
│              │    Container        │                      │
│              │    Port: 5432       │                      │
│              └─────────────────────┘                      │
└───────────────────────────────────────────────────────────┘
```

## Volume Management

### Persistent Volumes

1. **Database Volume** (`postgres_data`)
   - Purpose: Persist PostgreSQL data
   - Type: Named volume
   - Backup: Daily automated backups
   - Location: Docker volume directory

2. **Application Logs** (`app_logs`)
   - Purpose: Persist application logs
   - Type: Named volume
   - Retention: 30 days rolling
   - Location: `/app/logs` in container

3. **User Uploads** (`uploads`)
   - Purpose: Store user-uploaded files
   - Type: Named volume
   - Backup: Daily to cloud storage
   - Location: `/app/wwwroot/uploads` in container

### Development Bind Mounts

```yaml
volumes:
  - ./src:/src:cached              # Source code for hot reload
  - ./appsettings.Development.json:/app/appsettings.Development.json
```

## Environment Configuration

### Development Environment Variables

```bash
# Application
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080;https://+:8443
DOTNET_USE_POLLING_FILE_WATCHER=true

# Database
POSTGRES_USER=postgres
POSTGRES_PASSWORD=dev_password_123
POSTGRES_DB=witchcityrope

# Connection String
ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=witchcityrope;Username=postgres;Password=dev_password_123

# Services
Syncfusion__LicenseKey=YOUR_DEV_LICENSE_KEY
Email__SmtpServer=smtp.mailtrap.io
Email__SmtpPort=2525
```

### Production Environment Variables

```bash
# Application
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Database
POSTGRES_USER=postgres
POSTGRES_PASSWORD=${SECURE_GENERATED_PASSWORD}
POSTGRES_DB=witchcityrope

# Connection String (uses Docker secrets in production)
ConnectionStrings__DefaultConnection=/run/secrets/db_connection_string

# Services
Syncfusion__LicenseKey=/run/secrets/syncfusion_license
Email__SmtpServer=${SMTP_SERVER}
Email__SmtpPort=${SMTP_PORT}
Email__Username=/run/secrets/smtp_username
Email__Password=/run/secrets/smtp_password
```

## Build Process

### Multi-Stage Dockerfile

```dockerfile
# Build stage - compiles the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
# Copy project files and restore dependencies
COPY ["src/WitchCityRope.Web/WitchCityRope.Web.csproj", "WitchCityRope.Web/"]
COPY ["src/WitchCityRope.Core/WitchCityRope.Core.csproj", "WitchCityRope.Core/"]
COPY ["src/WitchCityRope.Infrastructure/WitchCityRope.Infrastructure.csproj", "WitchCityRope.Infrastructure/"]
RUN dotnet restore "WitchCityRope.Web/WitchCityRope.Web.csproj"

# Copy source code and build
COPY src/ .
WORKDIR "/src/WitchCityRope.Web"
RUN dotnet build "WitchCityRope.Web.csproj" -c Release -o /app/build

# Publish stage - creates deployment artifacts
FROM build AS publish
RUN dotnet publish "WitchCityRope.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Development stage - includes SDK for hot reload
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS development
WORKDIR /app
EXPOSE 8080 8443
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENTRYPOINT ["dotnet", "watch", "run", "--project", "/src/WitchCityRope.Web/WitchCityRope.Web.csproj"]

# Final production stage - minimal runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080 8443

# Create non-root user
RUN adduser --disabled-password --home /app --gecos '' appuser && chown -R appuser /app
USER appuser

# Copy published artifacts
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "WitchCityRope.Web.dll"]
```

### Build Optimization

1. **Layer Caching**
   - Project files copied separately for better caching
   - Dependencies restored before source copy
   - Multi-stage build reduces final image size

2. **Security Hardening**
   - Non-root user in production
   - Minimal base image (Alpine where possible)
   - No development tools in production image

## Deployment Workflows

### Development Workflow

```bash
# 1. Clone repository
git clone https://github.com/DarkMonkDev/WitchCityRope.git
cd WitchCityRope

# 2. Setup environment
cp .env.example .env
# Edit .env with your values

# 3. Build and start
docker-compose up -d --build

# 4. Run migrations
docker-compose exec web dotnet ef database update

# 5. Verify health
docker-compose ps
docker-compose logs -f web

# 6. Development cycle
# - Make code changes
# - Hot reload automatically applies changes
# - View logs for errors

# 7. Run tests
docker-compose exec web dotnet test

# 8. Stop when done
docker-compose down
```

### Production Deployment

```bash
# 1. Build production image
docker build -t witchcityrope:latest -f Dockerfile --target final .

# 2. Tag for registry
docker tag witchcityrope:latest registry.example.com/witchcityrope:latest

# 3. Push to registry
docker push registry.example.com/witchcityrope:latest

# 4. Deploy to production
ssh production-server
cd /opt/witchcityrope
docker-compose -f docker-compose.prod.yml pull
docker-compose -f docker-compose.prod.yml up -d

# 5. Run migrations
docker-compose -f docker-compose.prod.yml exec web dotnet ef database update

# 6. Verify deployment
docker-compose -f docker-compose.prod.yml ps
docker-compose -f docker-compose.prod.yml logs -f web
```

## Health Monitoring

### Application Health Checks

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>()
    .AddCheck("Syncfusion", () => 
        !string.IsNullOrEmpty(configuration["Syncfusion:LicenseKey"]) 
            ? HealthCheckResult.Healthy() 
            : HealthCheckResult.Unhealthy())
    .AddSmtpHealthCheck(options =>
    {
        options.Host = configuration["Email:SmtpServer"];
        options.Port = int.Parse(configuration["Email:SmtpPort"]);
    });

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

### Container Health Monitoring

1. **Docker Health Checks**
   ```yaml
   healthcheck:
     test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
     interval: 30s
     timeout: 3s
     retries: 3
   ```

2. **Monitoring Stack** (Optional)
   - Prometheus for metrics collection
   - Grafana for visualization
   - AlertManager for notifications

## Backup and Recovery

### Database Backup Strategy

```bash
# Automated daily backup script
#!/bin/bash
BACKUP_DIR="/backups/postgres"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/witchcityrope_$TIMESTAMP.sql"

# Create backup
docker exec witchcityrope-db pg_dump -U postgres witchcityrope > $BACKUP_FILE

# Compress backup
gzip $BACKUP_FILE

# Upload to S3 (example)
aws s3 cp $BACKUP_FILE.gz s3://witchcityrope-backups/

# Clean old backups (keep 30 days)
find $BACKUP_DIR -name "*.gz" -mtime +30 -delete
```

### Disaster Recovery

1. **Database Recovery**
   ```bash
   # Restore from backup
   gunzip < backup.sql.gz | docker exec -i witchcityrope-db psql -U postgres witchcityrope
   ```

2. **Volume Recovery**
   ```bash
   # Restore volumes from backup
   docker run --rm -v witchcityrope_uploads:/data -v /backup:/backup alpine tar -xzf /backup/uploads.tar.gz -C /data
   ```

## Security Best Practices

### Container Security

1. **Image Security**
   - Regular base image updates
   - Vulnerability scanning with Trivy
   - Signed images in production
   - No secrets in images

2. **Runtime Security**
   - Non-root user execution
   - Read-only root filesystem where possible
   - Limited capabilities
   - Network segmentation

3. **Secret Management**
   ```yaml
   # Docker Swarm secrets
   secrets:
     db_connection_string:
       external: true
     syncfusion_license:
       external: true
   ```

### Network Security

1. **Firewall Rules**
   - Only ports 80/443 exposed externally
   - Database isolated in internal network
   - Service-to-service communication only

2. **SSL/TLS Configuration**
   - Strong cipher suites
   - HSTS enabled
   - Certificate auto-renewal with Let's Encrypt

## Performance Optimization

### Container Optimization

1. **Resource Limits**
   ```yaml
   deploy:
     resources:
       limits:
         cpus: '2'
         memory: 2G
       reservations:
         cpus: '1'
         memory: 1G
   ```

2. **Caching Strategy**
   - Build cache optimization
   - Application-level caching
   - CDN for static assets

### Database Optimization

1. **Connection Pooling**
   ```csharp
   services.AddDbContext<ApplicationDbContext>(options =>
       options.UseNpgsql(connectionString, npgsqlOptions =>
       {
           npgsqlOptions.EnableRetryOnFailure();
       }));
   ```

2. **Query Optimization**
   - Proper indexing
   - Query monitoring
   - Connection pool tuning

## Troubleshooting

### Common Issues

1. **Container Won't Start**
   ```bash
   # Check logs
   docker logs witchcityrope-web
   
   # Inspect container
   docker inspect witchcityrope-web
   
   # Check events
   docker events --filter container=witchcityrope-web
   ```

2. **Database Connection Issues**
   ```bash
   # Test connectivity
   docker exec witchcityrope-web ping witchcityrope-db
   
   # Check DNS resolution
   docker exec witchcityrope-web nslookup witchcityrope-db
   ```

3. **Performance Issues**
   ```bash
   # Check resource usage
   docker stats
   
   # Inspect specific container
   docker top witchcityrope-web
   ```

### Debug Commands

```bash
# Shell into container
docker exec -it witchcityrope-web bash

# View real-time logs
docker logs -f witchcityrope-web

# Copy files from container
docker cp witchcityrope-web:/app/logs/app.log ./

# Network debugging
docker network inspect witchcityrope-network
```

## Maintenance

### Regular Maintenance Tasks

1. **Weekly**
   - Review container logs
   - Check disk usage
   - Verify backups

2. **Monthly**
   - Update base images
   - Security patches
   - Performance review

3. **Quarterly**
   - Full disaster recovery test
   - Security audit
   - Capacity planning

### Update Procedures

```bash
# 1. Pull latest changes
git pull

# 2. Build new image
docker-compose build

# 3. Test in staging
docker-compose -f docker-compose.staging.yml up -d

# 4. Deploy to production (with zero downtime)
docker-compose -f docker-compose.prod.yml up -d --no-deps --scale web=2 web
docker-compose -f docker-compose.prod.yml up -d --no-deps web
```

## Appendix

### Docker Compose Reference

See the main CLAUDE.md file for complete docker-compose.yml examples.

### Environment Variables Reference

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| ASPNETCORE_ENVIRONMENT | Runtime environment | Development | Yes |
| ConnectionStrings__DefaultConnection | Database connection | - | Yes |
| Syncfusion__LicenseKey | Syncfusion license | - | Yes |
| Email__SmtpServer | SMTP server address | - | No |
| Email__SmtpPort | SMTP server port | 587 | No |

### Useful Resources

- [Docker Documentation](https://docs.docker.com/)
- [ASP.NET Core Docker Documentation](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/)
- [PostgreSQL Docker Documentation](https://hub.docker.com/_/postgres)
- [Docker Compose Documentation](https://docs.docker.com/compose/)