# Multi-stage Dockerfile for WitchCityRope .NET API
# Based on design: /docs/functional-areas/docker-authentication/design/api-container-design.md

# Development stage - Full SDK with hot reload support
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS development

# Create app directory
WORKDIR /app

# Install additional tools for debugging and database connectivity
# Install PostgreSQL 16 client to match server version
RUN apt-get update && apt-get install -y \
    curl \
    vim \
    wget \
    gnupg \
    lsb-release \
    && wget --quiet -O - https://www.postgresql.org/media/keys/ACCC4CF8.asc | gpg --dearmor -o /usr/share/keyrings/postgresql-keyring.gpg \
    && echo "deb [signed-by=/usr/share/keyrings/postgresql-keyring.gpg] http://apt.postgresql.org/pub/repos/apt $(lsb_release -cs)-pgdg main" > /etc/apt/sources.list.d/pgdg.list \
    && apt-get update \
    && apt-get install -y postgresql-client-16 \
    && rm -rf /var/lib/apt/lists/*

# Copy API project files
COPY apps/api/ ./

# Restore dependencies
RUN dotnet restore --verbosity normal

# Expose ports for API (8080 internal) and debugging (40000)
EXPOSE 8080
EXPOSE 40000

# Configure hot reload environment
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true
ENV ASPNETCORE_ENVIRONMENT=Development

# Run with hot reload and specific binding
CMD ["dotnet", "watch", "run", "--urls", "http://0.0.0.0:8080", "--no-restore"]

# Production build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

# Copy API project files
COPY apps/api/ ./

# Restore dependencies
RUN dotnet restore

# Build the application
RUN dotnet build -c Release --no-restore

# Publish application
RUN dotnet publish -c Release -o /app/publish --no-restore

# Production runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS production

# Create non-root user for security
RUN adduser -D -u 1001 -g 1001 -s /bin/sh appuser

# Set working directory
WORKDIR /app

# Install runtime dependencies
# Install PostgreSQL 16 client to match server version
RUN apk add --no-cache \
    curl \
    postgresql16-client \
    --repository=https://dl-cdn.alpinelinux.org/alpine/edge/main \
    && rm -rf /var/cache/apk/*

# Copy published application
COPY --from=build /app/publish .

# Change ownership to app user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port (8080 internal, mapped to 5655 external)
EXPOSE 8080

# Configure production environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Health check configuration
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Run application
ENTRYPOINT ["dotnet", "WitchCityRope.Api.dll"]