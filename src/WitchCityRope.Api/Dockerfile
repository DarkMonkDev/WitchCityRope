# Development stage for hot reload
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS development
WORKDIR /src

# Install Entity Framework Core tools
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

# Set up environment for hot reload
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:8080;https://+:8081
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Expose ports for development
EXPOSE 8080 8081

# Keep container running and use dotnet watch
ENTRYPOINT ["dotnet", "watch", "run", "--project", "/src/WitchCityRope.Api/WitchCityRope.Api.csproj", "--no-launch-profile", "--urls", "http://0.0.0.0:8080"]

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file if it exists
COPY ["*.sln", "./"]

# Copy csproj files and restore dependencies
# This assumes the docker build context is at the repository root
COPY ["src/WitchCityRope.Api/WitchCityRope.Api.csproj", "src/WitchCityRope.Api/"]
COPY ["src/WitchCityRope.Core/WitchCityRope.Core.csproj", "src/WitchCityRope.Core/"]
COPY ["src/WitchCityRope.Infrastructure/WitchCityRope.Infrastructure.csproj", "src/WitchCityRope.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/WitchCityRope.Api/WitchCityRope.Api.csproj"

# Copy everything else and build
COPY ["src/", "src/"]
RUN dotnet build "src/WitchCityRope.Api/WitchCityRope.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "src/WitchCityRope.Api/WitchCityRope.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN useradd -m -u 1001 appuser

# Set up directories with proper permissions
RUN mkdir -p /app/data && \
    mkdir -p /app/logs && \
    mkdir -p /app/uploads && \
    chown -R appuser:appuser /app

# Copy published app from publish stage
COPY --from=publish --chown=appuser:appuser /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Entry point
ENTRYPOINT ["dotnet", "WitchCityRope.Api.dll"]