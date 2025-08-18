# CI/CD Comprehensive Guide - WitchCityRope React
<!-- Last Updated: 2025-08-17 -->
<!-- Version: 1.0 -->
<!-- Owner: DevOps Team -->
<!-- Status: Active -->

## Overview

This comprehensive guide covers all CI/CD practices for the WitchCityRope React + .NET API project. This consolidates all CI/CD knowledge into a single source of truth.

> **📋 Quick Reference**: For day-to-day CI/CD commands, see [CI/CD Quick Reference](./CI_CD_QUICK_REFERENCE.md)

## Technology Stack

### Current Architecture (React Migration)
- **Frontend**: React + TypeScript + Vite
- **Backend**: .NET 9 Minimal API
- **Database**: PostgreSQL
- **Container**: Docker + Docker Compose
- **Testing**: Playwright (E2E) + Vitest (Unit) + xUnit (.NET)

### CI/CD Pipeline Technology
- **Primary Platform**: GitHub Actions
- **Alternative**: Azure DevOps (enterprise scenarios)
- **Local Testing**: Docker Compose
- **Quality Gates**: Automated testing + Performance monitoring

## GitHub Actions Workflows

### Primary Workflow: `playwright-ci-enhanced.yml`

#### Triggers
- **Push** to main/develop: Standard test suite
- **Pull Request**: Smoke + changed area tests  
- **Schedule**: Nightly comprehensive testing
- **Manual**: Workflow dispatch with options

#### Workflow Jobs

##### 1. Setup and Environment
```yaml
strategy:
  matrix:
    os: [ubuntu-latest]
    node-version: [18, 20]
    dotnet-version: [9.0.x]
```

##### 2. Service Startup
- PostgreSQL database container
- .NET API service build and startup
- React application build and serve
- Health check validation

##### 3. Test Execution Categories
| Category | Description | Duration | Priority |
|----------|-------------|----------|----------|
| `smoke` | Critical path tests | ~5 min | Critical |
| `auth` | Authentication/authorization | ~8 min | High |
| `events` | Event management | ~10 min | High |
| `admin` | Admin functionality | ~12 min | Medium |
| `validation` | Form validation | ~6 min | Medium |
| `rsvp` | RSVP/membership flows | ~10 min | Medium |
| `ui` | UI/navigation | ~5 min | Low |
| `api` | API endpoints | ~8 min | Medium |
| `visual` | Visual regression | ~15 min | Low |
| `performance` | Load/performance | ~20 min | Low |

##### 4. Quality Gates
- **Code Coverage**: .NET (80%+), React (70%+)
- **Performance**: Lighthouse scores (70+ performance, 90+ accessibility)
- **Security**: Dependency vulnerability scanning
- **Code Quality**: ESLint, Prettier, SonarQube analysis

### Legacy Workflow: `e2e-playwright-js.yml`
- **Status**: Deprecated - maintained for compatibility
- **Migration Plan**: Phase out after React migration complete

## Local Development CI/CD

### Quick Start Commands
```bash
# Run smoke tests (fastest verification)
npm run test:smoke

# Test authentication flows
npm run test:auth

# Debug mode with browser visible
npm run test:debug

# List all available test categories
npm run test:categories

# Run full test suite
npm run test:all
```

### Docker Environment Setup
```bash
# Standard environment
docker compose up -d --wait

# CI-optimized environment  
docker compose -f docker-compose.ci.yml up -d --wait

# Check service health
curl http://localhost:5173/health  # React
curl http://localhost:5655/health  # API
```

## Configuration Files

### Primary Configuration Matrix
| File | Purpose | Technology |
|------|---------|------------|
| `playwright.config.ci.ts` | CI-optimized Playwright config | E2E Testing |
| `vitest.config.ts` | React unit test configuration | Unit Testing |
| `.env.ci` | CI environment variables | Environment |
| `docker-compose.ci.yml` | CI-optimized Docker setup | Containerization |
| `test-categories.json` | Test categorization rules | Test Organization |
| `.lighthouserc.js` | Performance testing config | Performance |

### Environment Variables

#### CI Environment Variables
```bash
export BASE_URL=http://localhost:5173
export VITE_API_BASE_URL=http://localhost:5655
export CI=true
export PLAYWRIGHT_BROWSERS_PATH=0
export HEADLESS=true
export NODE_ENV=test
```

#### Local Development
```bash
export BASE_URL=http://localhost:5173
export VITE_API_BASE_URL=http://localhost:5655
export CI=false
export DEBUG=true
export HEADED=true
export NODE_ENV=development
```

## Testing Strategy

### Test Pyramid Architecture

#### Unit Tests (70% of test suite)
- **React Components**: Vitest + React Testing Library
- **.NET API**: xUnit + Moq
- **Location**: `apps/web/src/test/` and `apps/api/tests/`
- **Execution Time**: <30 seconds total
- **Coverage Target**: 80%

#### Integration Tests (20% of test suite)  
- **API Integration**: Real database + HTTP calls
- **Component Integration**: React components with API mocking
- **Location**: `tests/integration/`
- **Execution Time**: <2 minutes total
- **Coverage Target**: Key user flows

#### E2E Tests (10% of test suite)
- **Technology**: Playwright
- **Coverage**: Critical user journeys
- **Location**: `tests/playwright/`
- **Execution Time**: 5-20 minutes depending on category
- **Coverage Target**: Happy path + critical error scenarios

### Performance Testing

#### Lighthouse CI Integration
```javascript
// .lighthouserc.js
module.exports = {
  ci: {
    collect: {
      url: ['http://localhost:5173/', 'http://localhost:5173/events'],
      numberOfRuns: 3
    },
    assert: {
      assertions: {
        'categories:performance': ['error', {minScore: 0.7}],
        'categories:accessibility': ['error', {minScore: 0.9}],
        'categories:best-practices': ['error', {minScore: 0.8}],
        'categories:seo': ['warn', {minScore: 0.7}]
      }
    }
  }
};
```

#### Performance Thresholds
- **Performance**: ≥70 (error), ≥90 (strict mode)
- **Accessibility**: ≥90 (error), ≥95 (strict mode)  
- **Best Practices**: ≥80 (error)
- **SEO**: ≥70 (warning)

## Quality Gates and Success Criteria

### Automated Quality Gates

#### Test Success Criteria  
- **Smoke Tests**: 100% pass rate required
- **Standard Suite**: ≥95% pass rate required
- **Full Suite**: ≥90% pass rate acceptable
- **Performance**: <4s LCP, <2s FCP, <0.1 CLS

#### Code Quality Gates
- **Code Coverage**: .NET 80%+, React 70%+
- **Code Quality**: SonarQube Quality Gate passed
- **Security**: No critical/high severity vulnerabilities
- **Dependencies**: All dependencies up to date, no known vulnerabilities

#### Performance Gates
- **Bundle Size**: React bundle <500KB gzipped
- **API Response**: 95th percentile <500ms
- **Database**: Query execution <100ms average
- **Memory**: Container memory usage <80% of allocated

### Manual Review Gates

#### Required Approvals
- **Security Changes**: Security team review
- **Database Migrations**: DBA review  
- **Performance Impact**: Performance team review
- **Breaking Changes**: Architecture team review

## Deployment Pipeline

### Staging Deployment (Automatic)
```yaml
# Triggers: Push to develop branch
stages:
  - build: Build React + .NET containers
  - test: Run test suite
  - deploy: Deploy to staging environment
  - verify: Run smoke tests against staging
  - notify: Slack/Teams notification
```

### Production Deployment (Manual Approval)
```yaml
# Triggers: Manual trigger from staging
stages:
  - approval: Manual approval required
  - backup: Database backup
  - build: Production container build
  - deploy: Blue-green deployment
  - verify: Production smoke tests
  - monitor: 24-hour monitoring alert
```

### Rollback Procedures
```bash
# Automatic rollback triggers
- Health check failures (>5 minutes)
- Error rate >5% (>2 minutes)
- Performance degradation >50% (>3 minutes)

# Manual rollback
./scripts/rollback-production.sh <previous-version>
```

## Security and Compliance

### Security Scanning Integration

#### Container Security
```yaml
- name: Container Security Scan
  run: |
    docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
      aquasec/trivy image witchcityrope/api:latest
    docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
      aquasec/trivy image witchcityrope/web:latest
```

#### Dependency Scanning
```yaml
- name: Dependency Security Audit
  run: |
    npm audit --audit-level high
    dotnet list package --vulnerable --include-transitive
```

#### Secret Scanning
```yaml
- name: Secret Detection
  uses: trufflesecurity/trufflehog@main
  with:
    path: ./
    base: main
    head: HEAD
```

### Compliance Requirements

#### SOC 2 Compliance
- All code changes tracked in Git
- All deployments logged and auditable
- Access controls on production deployments
- Automated security scanning

#### Data Protection
- No sensitive data in logs
- Encrypted secrets management
- GDPR compliance validation
- Data retention policy enforcement

## Monitoring and Alerting

### Application Performance Monitoring

#### Metrics Collection
- **Application**: OpenTelemetry + Application Insights
- **Infrastructure**: Prometheus + Grafana
- **Logs**: Structured logging + ELK Stack
- **User Experience**: Real User Monitoring (RUM)

#### Alert Configurations
```yaml
alerts:
  - name: "High Error Rate"
    condition: "error_rate > 5%"
    duration: "2 minutes"
    severity: "critical"
    
  - name: "Response Time Degradation"
    condition: "p95_response_time > 2s"
    duration: "5 minutes"
    severity: "warning"
    
  - name: "Database Connection Issues"
    condition: "db_connections_failed > 10"
    duration: "1 minute"
    severity: "critical"
```

### CI/CD Pipeline Monitoring

#### Build Metrics
- **Build Time**: Target <5 minutes for PR builds
- **Test Execution**: Target <15 minutes for full suite
- **Deployment Time**: Target <3 minutes for staging
- **Success Rate**: Target >95% pipeline success

#### Pipeline Health Alerts
- Build failures >2 consecutive
- Test success rate <90%
- Deployment failures
- Performance regression >20%

## Troubleshooting Guide

### Common CI/CD Issues

#### Authentication Failures
```bash
# Check test data seeding
docker compose exec postgres psql -U postgres -d witchcityrope_test -c "SELECT email FROM \"AspNetUsers\";"

# Reset authentication states
rm -rf tests/playwright/.auth/*

# Regenerate test user data
npm run test:seed-users
```

#### Service Startup Issues
```bash
# Clean Docker environment
docker compose down -v
docker system prune -f
docker compose up -d --build --wait

# Check service logs
docker compose logs api
docker compose logs web
docker compose logs postgres
```

#### Browser/Playwright Issues
```bash
# Reinstall browsers
npx playwright install --with-deps --force

# Check browser installation
npx playwright doctor

# Update Playwright
npm install @playwright/test@latest
```

#### Performance Issues
```bash
# Monitor resource usage
docker stats --no-stream

# Check memory leaks
docker compose exec api dotnet-counters monitor --process-id 1

# Profile database queries
docker compose exec postgres psql -U postgres -d witchcityrope_test -c "SELECT * FROM pg_stat_activity;"
```

### Resource Constraints (CI)

#### Memory Optimization
- Reduce parallel workers (set to 1 in CI)
- Skip visual regression tests on resource-limited runners
- Use Docker resource limits in `docker-compose.ci.yml`
- Enable swap file for memory-intensive operations

#### Build Time Optimization
- Layer caching for Docker builds
- NPM/NuGet package caching
- Parallel test execution (when resources allow)
- Incremental builds for unchanged components

## Best Practices

### Code Quality Standards

#### Git Workflow
```bash
# Branch naming convention
feature/WCR-123-authentication-improvements
bugfix/WCR-456-login-form-validation
hotfix/WCR-789-security-patch

# Commit message format
feat(auth): add JWT token refresh mechanism

- Implement automatic token refresh
- Add refresh token storage
- Update authentication interceptor
- Closes #123
```

#### Pull Request Requirements
- [ ] All tests passing
- [ ] Code coverage maintained
- [ ] Performance impact assessed
- [ ] Security review completed (if applicable)
- [ ] Documentation updated
- [ ] Migration scripts provided (if needed)

### Testing Best Practices

#### Test Organization
```
tests/
├── unit/                 # Fast, isolated tests
│   ├── components/      # React component tests
│   └── services/        # Business logic tests
├── integration/         # Multi-component tests
│   ├── api/            # API integration tests
│   └── workflows/      # User workflow tests
└── e2e/                # End-to-end tests
    ├── critical/       # Must-pass tests
    ├── regression/     # Bug prevention tests
    └── performance/    # Load and performance tests
```

#### Test Data Management
- Use factories for test data creation
- Isolate test data between tests
- Use realistic but anonymized data
- Clean up test data after execution

### Security Best Practices

#### Secret Management
- Never commit secrets to Git
- Use GitHub Secrets for CI/CD
- Rotate secrets regularly
- Use least-privilege access

#### Container Security
- Use official base images
- Scan for vulnerabilities regularly
- Run containers as non-root users
- Implement security contexts

## Migration from Blazor CI/CD

### Completed Migration Tasks
- [x] Playwright E2E testing framework
- [x] React component testing with Vitest
- [x] Docker containerization
- [x] GitHub Actions workflow migration
- [x] Performance testing integration

### Remaining Legacy Elements
- [ ] Azure DevOps pipeline (optional migration)
- [ ] Some Blazor-specific test helpers (archived)
- [ ] Legacy deployment scripts (archived)

### Migration Notes
- All Blazor-specific CI/CD documentation archived to `/docs/_archive/blazor-legacy/`
- Current pipelines fully React-compatible
- No breaking changes to existing workflows
- Performance improvements: 40% faster build times, 60% more reliable tests

## Additional Resources

### Documentation
- [CI/CD Quick Reference](./CI_CD_QUICK_REFERENCE.md) - Daily commands and troubleshooting
- [Docker Operations Guide](/docs/guides-setup/docker-operations-guide.md) - Container management
- [Testing Standards](/docs/standards-processes/testing/) - Comprehensive testing guidelines
- [Performance Monitoring](/docs/standards-processes/performance/) - Performance standards

### External Resources
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Playwright Documentation](https://playwright.dev/docs)
- [Docker Compose Reference](https://docs.docker.com/compose/)
- [Vitest Documentation](https://vitest.dev/)

### Support Contacts
- **CI/CD Issues**: Check GitHub Actions workflow logs
- **Test Failures**: Review Playwright HTML report  
- **Infrastructure**: Verify Docker service health
- **Authentication**: Validate test user credentials
- **Performance**: Review Lighthouse CI reports

---

**Last Updated**: 2025-08-17 - Consolidated all CI/CD documentation into single source of truth
**Version**: 1.0 - Post React migration comprehensive guide