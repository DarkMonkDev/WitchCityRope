# WitchCityRope Performance Tests

This project contains comprehensive performance tests for the WitchCityRope application, including load tests, stress tests, and performance benchmarks.

## Overview

The performance test suite includes:

- **NBomber Tests** (.NET-based load testing)
- **k6 Tests** (JavaScript-based load testing)
- **Load Test Scenarios** (normal usage patterns)
- **Stress Test Scenarios** (breaking point analysis)
- **Performance Benchmarks** (response time and throughput targets)

## Test Categories

### Load Tests
- **Authentication Load Tests**: Login, registration, and token refresh scenarios
- **Event Load Tests**: Event listing, registration, and flash sale scenarios
- **Mixed Load Tests**: Realistic user behavior patterns

### Stress Tests
- **Breaking Point Test**: Gradually increases load to find system limits
- **Recovery Test**: Tests system recovery after high load
- **Memory Leak Test**: Long-running tests to detect memory issues
- **Connection Pool Test**: Tests database connection exhaustion

### Performance Benchmarks
- Response time thresholds (P50, P75, P95, P99)
- Throughput requirements (requests/second)
- Error rate limits (<1% for normal load)
- Resource utilization targets

## Running Tests

### NBomber Tests

```bash
# Run all performance tests
dotnet test

# Run specific test category
dotnet test --filter "Category=LoadTest"
dotnet test --filter "Category=StressTest"

# Run with specific configuration
dotnet test -e ASPNETCORE_ENVIRONMENT=Performance
```

### k6 Tests

```bash
# Install k6 (if not already installed)
# Windows: choco install k6
# Mac: brew install k6
# Linux: See https://k6.io/docs/getting-started/installation/

# Run authentication load test
k6 run k6/auth-load-test.js

# Run events load test with custom parameters
k6 run -e BASE_URL=https://staging.witchcityrope.com k6/events-load-test.js

# Run stress test
k6 run k6/stress-test.js

# Run with HTML report
k6 run --out json=results.json k6/events-load-test.js
```

## Configuration

### appsettings.json
Configure test parameters including:
- Base URL
- Test duration
- Load scenarios (Light, Normal, Heavy, Stress)
- Performance thresholds
- Test user settings

### Environment-Specific Settings
- `appsettings.Performance.json`: Production-like settings
- Environment variables can override any setting

## Test Scenarios

### Authentication Scenarios
1. **Login Load Test**: Simulates concurrent user logins
2. **Registration Surge**: Tests registration during high-traffic events
3. **Token Refresh**: Continuous token refresh operations

### Event Scenarios
1. **Event Browsing**: Users browsing and searching events
2. **Featured Events**: Homepage featured events (cached)
3. **Flash Sale**: Sudden spike in registrations
4. **Mixed Operations**: Realistic user behavior

### Stress Scenarios
1. **Breaking Point**: Find maximum sustainable load
2. **Spike Test**: Sudden traffic increase and recovery
3. **Soak Test**: Extended duration for memory leaks
4. **Cascading Failure**: Service degradation patterns

## Performance Targets

### Response Time Thresholds
- P50: < 100ms
- P75: < 200ms
- P95: < 500ms
- P99: < 1000ms

### Throughput Requirements
- Minimum: 50 requests/second
- Target: 200 requests/second
- Peak: 500 requests/second (flash sales)

### Error Rate Limits
- Normal Load: < 1%
- Peak Load: < 5%
- Stress Conditions: < 10%

## Reports

Performance test results are saved in the `reports/` directory with:
- HTML reports for visual analysis
- JSON reports for programmatic processing
- CSV exports for further analysis
- Markdown summaries for documentation

### Report Contents
- Summary statistics
- Response time percentiles
- Throughput metrics
- Error analysis
- Custom metrics
- Performance regression detection

## CI/CD Integration

The performance tests are integrated into the CI/CD pipeline:

1. **Nightly Runs**: Automated performance tests run every night
2. **PR Validation**: Basic performance tests on pull requests
3. **Release Testing**: Full suite before production deployments
4. **Regression Detection**: Automatic comparison with baselines

### GitHub Actions Workflow
```yaml
# Run performance tests manually
gh workflow run performance-tests.yml -f test_type=load -f environment=staging

# View results
gh run list --workflow=performance-tests.yml
```

## Monitoring and Alerting

Performance metrics are monitored for:
- Response time degradation
- Throughput reduction
- Error rate increases
- Resource exhaustion

Alerts are sent when:
- Performance thresholds are exceeded
- Regressions are detected
- Tests fail to complete

## Best Practices

1. **Warm-up Period**: Always include warm-up to avoid cold start issues
2. **Realistic Data**: Use production-like data volumes
3. **Network Conditions**: Test with realistic network latency
4. **Resource Monitoring**: Monitor CPU, memory, and I/O during tests
5. **Baseline Comparison**: Always compare against established baselines

## Troubleshooting

### Common Issues

1. **Connection Refused**
   - Ensure the application is running
   - Check the BASE_URL configuration
   - Verify firewall settings

2. **High Error Rates**
   - Check application logs
   - Verify database connections
   - Monitor resource usage

3. **Inconsistent Results**
   - Ensure consistent test environment
   - Check for background processes
   - Verify network stability

## Contributing

When adding new performance tests:
1. Follow existing patterns and naming conventions
2. Include appropriate assertions and thresholds
3. Document test scenarios and expected outcomes
4. Update this README with new test information