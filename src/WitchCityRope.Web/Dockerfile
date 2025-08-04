# Development stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS development
WORKDIR /src

# Install Node.js for potential frontend tooling
RUN apt-get update && \
    apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs && \
    rm -rf /var/lib/apt/lists/*

# Set up environment for hot reload
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:8080;https://+:8081
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Expose ports for development
EXPOSE 8080 8081

# Keep container running and use dotnet watch
ENTRYPOINT ["dotnet", "watch", "run", "--project", "/src/WitchCityRope.Web/WitchCityRope.Web.csproj", "--no-launch-profile", "--urls", "http://0.0.0.0:8080"]

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file if it exists
COPY ["*.sln", "./"]

# Copy csproj files and restore dependencies
# This assumes the docker build context is at the repository root
COPY ["src/WitchCityRope.Web/WitchCityRope.Web.csproj", "src/WitchCityRope.Web/"]
COPY ["src/WitchCityRope.Core/WitchCityRope.Core.csproj", "src/WitchCityRope.Core/"]
COPY ["src/WitchCityRope.Infrastructure/WitchCityRope.Infrastructure.csproj", "src/WitchCityRope.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/WitchCityRope.Web/WitchCityRope.Web.csproj"

# Copy everything else and build
COPY ["src/", "src/"]
RUN dotnet build "src/WitchCityRope.Web/WitchCityRope.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "src/WitchCityRope.Web/WitchCityRope.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN useradd -m -u 1000 -s /bin/bash appuser

# Set up directories with proper permissions
RUN mkdir -p /app/data && \
    mkdir -p /app/logs && \
    chown -R appuser:appuser /app

# Copy published files
COPY --from=publish --chown=appuser:appuser /app/publish .

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "WitchCityRope.Web.dll"]