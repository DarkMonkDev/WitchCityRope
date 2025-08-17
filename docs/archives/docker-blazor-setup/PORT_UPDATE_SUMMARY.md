# Port Configuration Update Summary

## Overview
All Docker configurations and documentation have been updated to reflect the new port assignments:

- **Web (HTTP)**: 5651 (previously 5000)
- **Web (HTTPS)**: 5652 (previously 5001)
- **API (HTTP)**: 5653 (previously 7000)
- **API (HTTPS)**: 5654 (previously 7001)

## Updated Files

### Docker Configuration Files
1. **docker-compose.yml** - Main compose file with updated port mappings
2. **docker-compose.override.yml** - Development overrides with CORS origins updated
3. **docker/docker-compose.yml** - Docker directory compose file updated
4. **docker/docker-compose.override.yml** - Docker directory override file updated

### Dockerfiles
1. **src/WitchCityRope.Web/Dockerfile** - Updated ASPNETCORE_URLS and EXPOSE directives

### Documentation
1. **README.md** - Updated access URL to https://localhost:5652
2. **DOCKER_SETUP_SUMMARY.md** - Updated port mappings table
3. **DOCKER_DEV_GUIDE.md** - Updated all port references
4. **docker/README.md** - Updated port references in documentation
5. **docker-dev.sh** - Updated port references in output messages
6. **docker-quick.sh** - Already had correct ports
7. **src/WitchCityRope.Infrastructure/docs/deployment/docker-deployment.md** - Updated all port references
8. **src/WitchCityRope.Infrastructure/docs/deployment/environment-setup.md** - Updated ASPNETCORE_URLS

### Configuration Files
1. **src/WitchCityRope.Api/appsettings.json** - CORS origins already updated
2. **src/WitchCityRope.Api/appsettings.Development.json** - CORS origins already updated
3. **src/WitchCityRope.Web/appsettings.Development.json** - API URLs and return URLs already updated
4. **tests/WitchCityRope.PerformanceTests/k6/config.json** - Base URL already updated
5. **tests/WitchCityRope.E2E.Tests/appsettings.CI.json** - Base URL already updated

## Notes
- All services maintain their internal container ports (8080/8081)
- Only the external mapped ports have been changed
- The nginx configurations remain unchanged as they reference internal container ports
- Monitoring stack ports (Grafana, Prometheus, etc.) remain unchanged as they are separate from the application ports

## Testing
After these changes, you can test the new configuration by:
1. Running `docker-compose down` to stop existing containers
2. Running `docker-compose up -d` to start with new port mappings
3. Accessing the web application at https://localhost:5652
4. Accessing the API at https://localhost:5654