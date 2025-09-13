# CI/CD Testing Guide

**Date**: 2025-09-12  
**Project**: WitchCityRope React Migration  
**Phase**: 3 - CI/CD Integration  
**Status**: Production Ready

## Overview

This guide covers the containerized testing infrastructure implementation for CI/CD pipelines, building on the enhanced TestContainers infrastructure from Phases 1 and 2.

## 🚀 Quick Start

### Running Tests Locally

```bash
# Run all tests with containerized infrastructure
npm run test:containerized

# Run specific test types
npm run test:unit                 # Fast unit tests
npm run test:integration         # Integration tests with containers
npm run test:e2e                 # End-to-end tests
npm run test:performance         # Performance tests

# Run E2E tests with Playwright
npm run test:e2e:playwright
```

### Manual Script Execution

```bash
# Integration tests with custom options
./scripts/run-integration-tests.sh --ci --verbose

# E2E tests with specific browser
./scripts/run-e2e-tests.sh --browser chromium --ci

# Performance tests
./scripts/run-performance-tests.sh --ci
```

## 🔧 CI/CD Workflows

### 1. Main Containerized Test Pipeline

**File**: `.github/workflows/test-containerized.yml`

**Purpose**: Comprehensive testing pipeline with PostgreSQL containers

**Features**:
- Fast unit tests (no containers)
- Integration tests with PostgreSQL service containers
- E2E tests with full application stack
- Container cleanup verification
- Performance monitoring

**Triggers**:
- Push to `main` or `develop`
- Pull requests to `main` or `develop`
- Manual dispatch

### 2. Integration Tests (TestContainers)

**File**: `.github/workflows/integration-tests.yml`

**Purpose**: Focused integration testing with TestContainers v4.7.0

**Features**:
- Matrix testing across multiple test projects
- Dynamic container configuration
- Performance monitoring
- Resource usage tracking
- Automatic cleanup verification

**Triggers**:
- Code changes in `src/**` or `tests/**`
- Workflow file changes
- Manual dispatch with performance test option

### 3. E2E Tests (Containerized)

**File**: `.github/workflows/e2e-tests-containerized.yml`

**Purpose**: End-to-end testing with full application stack

**Features**:
- Cross-browser testing (Chromium, Firefox, WebKit)
- Containerized PostgreSQL database
- API and Web application startup
- Screenshot capture on failure
- Comprehensive environment validation

**Triggers**:
- Changes in `apps/**`, `tests/e2e/**`, or `packages/**`
- Manual dispatch with browser selection

### 4. Container Cleanup Verification

**File**: `.github/workflows/cleanup-check.yml`

**Purpose**: Monitor and clean up orphaned containers

**Features**:
- Automatic orphaned container detection
- Resource usage monitoring
- Slack notifications for alerts
- Scheduled cleanup runs (every 6 hours)
- Force cleanup on demand

**Triggers**:
- After other test workflows complete
- Scheduled runs every 6 hours
- Manual dispatch for force cleanup

## 🐳 Container Infrastructure

### PostgreSQL Configuration

```yaml
services:
  postgres:
    image: postgres:16-alpine
    env:
      POSTGRES_DB: witchcityrope_test
      POSTGRES_USER: test_user
      POSTGRES_PASSWORD: Test123!
    options: >-
      --health-cmd pg_isready
      --health-interval 10s
      --health-timeout 5s
      --health-retries 5
      --label cleanup=automatic
      --label project=witchcityrope
      --label purpose=testing
    ports:
      - 5432:5432
```

### Container Labeling Strategy

All test containers use consistent labeling for identification and cleanup:

```yaml
labels:
  project: witchcityrope          # Project identifier
  purpose: testing                # Container purpose
  cleanup: automatic              # Cleanup strategy
  test-type: integration|e2e      # Test type
  browser: chromium|firefox       # Browser (E2E only)
```

### Resource Limits

```yaml
options: >-
  --memory=512m
  --cpus=0.5
  --health-cmd pg_isready
  --health-interval 10s
  --health-timeout 5s
  --health-retries 5
```

## 🔍 Monitoring and Debugging

### Container Status Verification

Each workflow includes container status checks:

```bash
# Check for orphaned containers
docker ps -a --filter "label=project=witchcityrope"

# Monitor resource usage
docker stats --no-stream

# Verify cleanup
ORPHANED=$(docker ps -a --filter "label=project=witchcityrope" --filter "status=exited" -q | wc -l)
```

### Debugging Failed Tests

1. **Check workflow logs**: Review GitHub Actions logs for specific failures
2. **Download artifacts**: Test results, screenshots, and container logs
3. **Review container status**: Look for orphaned or failed containers
4. **Verify environment**: Check database connections and API health

### Available Artifacts

- **Test Results**: TRX files and coverage reports
- **Screenshots**: PNG files for E2E test failures
- **Container Logs**: Docker container output for debugging
- **Performance Reports**: Timing and resource usage metrics

## ⚡ Performance Optimization

### Container Pooling

Integration tests use container pooling for improved performance:

```csharp
// Pre-warm containers for 80% faster startup
var pool = ContainerPool.GetInstance();
var container = await pool.GetContainerAsync();
```

### Parallel Execution

Tests run in parallel with dynamic port allocation:

```yaml
strategy:
  matrix:
    test-project: 
      - 'tests/integration/WitchCityRope.IntegrationTests'
      - 'tests/WitchCityRope.Core.Tests'
  fail-fast: false
```

### Caching Strategy

```yaml
- name: Cache NuGet packages
  uses: actions/cache@v4
  with:
    path: ${{ env.NUGET_PACKAGES }}
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}

- name: Cache Node modules
  uses: actions/cache@v4
  with:
    path: ~/.npm
    key: ${{ runner.os }}-node-${{ hashFiles('**/package-lock.json') }}
```

## 🚨 Troubleshooting

### Common Issues

#### 1. Orphaned Containers

**Symptoms**: Tests fail with port conflicts or "container already exists" errors

**Solution**:
```bash
# Manual cleanup
docker container prune -f --filter "label=project=witchcityrope"

# Check cleanup workflow
gh workflow run cleanup-check.yml
```

#### 2. Port Conflicts

**Symptoms**: "Port already in use" errors

**Solution**:
- Use dynamic port allocation (port: 0)
- Ensure proper cleanup between tests
- Check for running background processes

#### 3. Database Connection Issues

**Symptoms**: Connection timeout or authentication errors

**Solution**:
```bash
# Verify PostgreSQL container
docker logs <container_id>

# Test connection manually
PGPASSWORD=Test123! psql -h localhost -p 5432 -U test_user -d witchcityrope_test -c "SELECT 1;"
```

#### 4. Resource Exhaustion

**Symptoms**: Container startup failures or timeouts

**Solution**:
- Check available disk space: `df -h`
- Monitor memory usage: `free -h`
- Clean up Docker resources: `docker system prune -f`

### Performance Issues

#### Slow Test Startup

**Causes**:
- Container image pull time
- Database migration time
- Cold container start

**Solutions**:
- Pre-warm images in workflow
- Use container pooling
- Cache migration state

#### High Resource Usage

**Monitoring**:
```bash
# Check container resource usage
docker stats --no-stream

# Monitor disk usage
docker system df
```

**Optimization**:
- Reduce container memory limits
- Use Alpine-based images
- Implement container reuse

## 📊 Metrics and Reporting

### Success Metrics

- **Zero orphaned containers** after test runs
- **<5 second** container startup time (with pooling)
- **<30 second** total cleanup time
- **95%+ test success rate** in CI

### Performance Targets

| Metric | Target | Current |
|--------|--------|---------|
| Unit test execution | <30s | ~15s |
| Integration test startup | <5s | ~3s (pooled) |
| E2E test execution | <10min | ~8min |
| Container cleanup | <30s | ~15s |

### Monitoring Dashboards

Access workflow metrics:
1. GitHub Actions dashboard
2. Test result artifacts
3. Performance reports
4. Container cleanup reports

## 🔧 Configuration

### Environment Variables

```yaml
env:
  DOTNET_VERSION: '9.0.x'
  NODE_VERSION: '20'
  ConnectionStrings__DefaultConnection: Host=localhost;Port=5432;Database=witchcityrope_test;Username=test_user;Password=Test123!
  TestContainers__Enabled: "true"
  TestContainers__ReuseContainers: "true"
  ASPNETCORE_ENVIRONMENT: Testing
```

### Test Configuration

Create `appsettings.Testing.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Testcontainers": "Debug"
    }
  },
  "TestContainers": {
    "PostgreSQL": {
      "Image": "postgres:16-alpine",
      "Port": 0,
      "Cleanup": true
    }
  }
}
```

### Workflow Customization

Customize workflows through manual dispatch inputs:

```yaml
workflow_dispatch:
  inputs:
    browser:
      description: 'Browser to test'
      required: false
      default: 'chromium'
      type: choice
      options: ['chromium', 'firefox', 'webkit', 'all']
```

## 🔄 Maintenance

### Regular Tasks

1. **Monitor cleanup workflows**: Check for orphaned containers weekly
2. **Update container images**: Keep PostgreSQL image current
3. **Review performance metrics**: Optimize based on timing data
4. **Clean up artifacts**: Remove old test result artifacts

### Upgrade Procedures

When updating TestContainers or PostgreSQL versions:

1. Test changes in development environment
2. Update container images in workflows
3. Verify compatibility with existing tests
4. Monitor first production runs carefully

### Backup and Recovery

Test infrastructure is fully reproducible from source code. No persistent data backup required.

Critical configurations to track:
- Workflow files in `.github/workflows/`
- Test scripts in `scripts/`
- Container configuration in test fixtures

## 📚 References

- [TestContainers Documentation](https://testcontainers.com/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [PostgreSQL Docker Hub](https://hub.docker.com/_/postgres)
- [Playwright Documentation](https://playwright.dev/)

## 🤝 Support

For issues with CI/CD testing infrastructure:

1. Check this guide for troubleshooting steps
2. Review workflow logs in GitHub Actions
3. Create issue with workflow run URL and error details
4. Include container status and resource usage information

---

*Last updated: 2025-09-12*  
*Version: 1.0*  
*Phase: 3 - CI/CD Integration*