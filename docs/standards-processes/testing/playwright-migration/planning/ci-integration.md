# CI/CD Integration Plan for Playwright Migration

## Overview
This document details the integration plan for Playwright tests in the WitchCityRope CI/CD pipeline. The project uses GitHub Actions as the primary CI/CD platform with Docker containers and PostgreSQL database.

## Current State Analysis

### Existing CI/CD Infrastructure
1. **GitHub Actions**: Primary CI/CD with dedicated E2E test workflows
2. **GitLab CI**: Docker-based pipeline with coverage reports (optional/secondary)
3. **Docker Compose**: Local development and testing support with PostgreSQL

### Key Finding
The project already has a Playwright workflow (`e2e-tests.yml`) running .NET-based Playwright tests, which provides a foundation for JavaScript/TypeScript Playwright test integration.

## Test Folder Structure

### Centralized Test Organization
All Playwright tests will be stored in a centralized, well-organized folder structure:

```
/home/chad/repos/witchcityrope/WitchCityRope/tests/playwright/
├── auth/                    # Authentication tests
│   ├── login.spec.ts
│   ├── register.spec.ts
│   ├── logout.spec.ts
│   └── password-reset.spec.ts
├── events/                  # Event management tests
│   ├── create-event.spec.ts
│   ├── edit-event.spec.ts
│   ├── view-events.spec.ts
│   └── event-details.spec.ts
├── admin/                   # Admin functionality tests
│   ├── user-management.spec.ts
│   ├── event-management.spec.ts
│   ├── reports.spec.ts
│   └── settings.spec.ts
├── rsvp/                    # RSVP and ticketing tests
│   ├── rsvp-flow.spec.ts
│   ├── ticket-purchase.spec.ts
│   ├── waitlist.spec.ts
│   └── cancellation.spec.ts
├── validation/              # Form validation tests
│   ├── event-form.spec.ts
│   ├── registration-form.spec.ts
│   └── profile-form.spec.ts
├── helpers/                 # Shared test utilities
│   ├── blazor.helpers.ts
│   ├── test.config.ts
│   └── data-generators.ts
└── pages/                   # Page Object Models
    ├── login.page.ts
    ├── event.page.ts
    ├── admin.page.ts
    └── dashboard.page.ts
```

### Migration Path from Puppeteer
- **Current Puppeteer Tests**: `/tests/e2e/tests/e2e/`
- **New Playwright Tests**: `/tests/playwright/`
- Tests will be migrated category by category to maintain organization

## Integration Strategy

### Phase 1: Parallel Execution (Weeks 1-3)
Run Puppeteer and Playwright tests side-by-side to ensure no regression.

#### GitHub Actions Updates

**New Workflow: `.github/workflows/e2e-playwright-js.yml`**
```yaml
name: Playwright JavaScript Tests

on:
  push:
    branches: [ main, develop, feature/playwright-migration ]
  pull_request:
    branches: [ main ]
  workflow_dispatch:
    inputs:
      test-suite:
        description: 'Test suite to run'
        required: false
        default: 'all'
        type: choice
        options:
          - all
          - auth
          - events
          - admin
          - validation

jobs:
  playwright-tests:
    runs-on: ubuntu-latest
    timeout-minutes: 30
    
    services:
      postgres:
        image: postgres:16-alpine
        env:
          POSTGRES_USER: postgres
          POSTGRES_PASSWORD: WitchCity2024!
          POSTGRES_DB: witchcityrope_test
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'

    - name: Setup Node.js
      uses: actions/setup-node@v4
      with:
        node-version: '18.x'
        cache: 'npm'

    - name: Install dependencies
      run: |
        npm ci
        npx playwright install --with-deps chromium firefox webkit

    - name: Build application
      run: |
        dotnet build src/WitchCityRope.Web/WitchCityRope.Web.csproj

    - name: Start application
      run: |
        dotnet run --project src/WitchCityRope.Web/WitchCityRope.Web.csproj &
        echo "Waiting for application to start..."
        npx wait-on http://localhost:5651 --timeout 60000

    - name: Run Playwright tests
      run: |
        if [ "${{ github.event.inputs.test-suite }}" = "all" ] || [ -z "${{ github.event.inputs.test-suite }}" ]; then
          npx playwright test
        else
          npx playwright test tests/playwright/${{ github.event.inputs.test-suite }}
        fi
      env:
        BASE_URL: http://localhost:5651
        CI: true
        DATABASE_URL: postgresql://postgres:WitchCity2024!@localhost:5432/witchcityrope_test

    - name: Upload test results
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: playwright-report
        path: playwright-report/
        retention-days: 30

    - name: Upload test videos
      uses: actions/upload-artifact@v4
      if: failure()
      with:
        name: test-videos
        path: test-results/
        retention-days: 7
```

### Phase 2: Migration Period (Weeks 4-7)

#### Updated Package.json Scripts
```json
{
  "scripts": {
    "test:e2e:puppeteer": "node tests/e2e/test-suite-organizer.js",
    "test:e2e:playwright": "playwright test",
    "test:e2e:playwright:debug": "playwright test --debug",
    "test:e2e:playwright:ui": "playwright test --ui",
    "test:e2e:playwright:headed": "playwright test --headed",
    "test:e2e:all": "npm run test:e2e:puppeteer && npm run test:e2e:playwright",
    "test:e2e:playwright:ci": "playwright test --reporter=html,github,junit",
    "test:e2e:playwright:docker": "docker run --rm -v $(pwd):/work -w /work mcr.microsoft.com/playwright:v1.40.0-focal npm run test:e2e:playwright"
  }
}
```

#### GitLab CI Updates (Optional - if using GitLab)
```yaml
# Add to .gitlab-ci.yml
playwright-tests:
  stage: test
  image: mcr.microsoft.com/playwright:v1.40.0-focal
  services:
    - postgres:16-alpine
  variables:
    POSTGRES_USER: postgres
    POSTGRES_PASSWORD: WitchCity2024!
    POSTGRES_DB: witchcityrope_test
    DATABASE_URL: "Host=postgres;Database=witchcityrope_test;Username=postgres;Password=WitchCity2024!"
  before_script:
    - apt-get update && apt-get install -y wget
    - wget https://packages.microsoft.com/config/debian/11/packages-microsoft-prod.deb
    - dpkg -i packages-microsoft-prod.deb
    - apt-get update && apt-get install -y dotnet-sdk-9.0
    - npm ci
  script:
    - dotnet build src/WitchCityRope.Web/WitchCityRope.Web.csproj
    - dotnet run --project src/WitchCityRope.Web/WitchCityRope.Web.csproj &
    - npx wait-on http://localhost:5651
    - npx playwright test --reporter=junit,html
  artifacts:
    when: always
    reports:
      junit: test-results/results.xml
    paths:
      - playwright-report/
    expire_in: 1 week
  coverage: '/All files[^|]*\|[^|]*\s+([\d\.]+)/'
```

### Phase 3: Cutover (Weeks 8-9)

#### Deprecate Puppeteer Workflows
1. Add deprecation warnings to Puppeteer workflows
2. Update all references to use Playwright workflows
3. Archive Puppeteer test results for comparison

#### Final GitHub Actions Workflow
Rename `e2e-playwright-js.yml` to `e2e-tests.yml` and remove Puppeteer workflow.

## Docker Integration

### Development Docker Compose
```yaml
# docker-compose.playwright.yml
version: '3.8'

services:
  playwright-tests:
    image: mcr.microsoft.com/playwright:v1.40.0-focal
    volumes:
      - .:/work
    working_dir: /work
    environment:
      - BASE_URL=http://web:8080
      - DATABASE_URL=Host=postgres;Database=witchcityrope_test;Username=postgres;Password=WitchCity2024!
    depends_on:
      - web
      - postgres
    command: npx playwright test
    networks:
      - witchcity-network

  playwright-ui:
    image: mcr.microsoft.com/playwright:v1.40.0-focal
    volumes:
      - .:/work
    working_dir: /work
    ports:
      - "9323:9323"
    environment:
      - BASE_URL=http://web:8080
    depends_on:
      - web
    command: npx playwright test --ui-host=0.0.0.0 --ui-port=9323
    networks:
      - witchcity-network
```

### CI Docker Build
```dockerfile
# Dockerfile.playwright-ci
FROM mcr.microsoft.com/playwright:v1.40.0-focal

# Install .NET SDK
RUN wget https://packages.microsoft.com/config/debian/11/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y dotnet-sdk-9.0

WORKDIR /app

# Copy application files
COPY . .

# Install dependencies
RUN npm ci && \
    dotnet restore

# Build application
RUN dotnet build src/WitchCityRope.Web/WitchCityRope.Web.csproj

# Run tests
CMD ["sh", "-c", "dotnet run --project src/WitchCityRope.Web/WitchCityRope.Web.csproj & npx wait-on http://localhost:5651 && npx playwright test"]
```

## Performance Optimization

### 1. Test Sharding
```yaml
# GitHub Actions with sharding
strategy:
  matrix:
    shard: [1/4, 2/4, 3/4, 4/4]
steps:
  - name: Run Playwright tests
    run: npx playwright test --shard=${{ matrix.shard }}
```

### 2. Parallel Execution
```javascript
// playwright.config.js
export default {
  workers: process.env.CI ? 2 : undefined,
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
};
```

### 3. Browser Selection
- Development: All browsers (Chromium, Firefox, WebKit)
- PR Checks: Chromium only (fast feedback)
- Main Branch: All browsers (comprehensive testing)

## Monitoring & Reporting

### 1. Test Results Dashboard
- Integrate with GitHub Actions summary
- GitLab test reports (if using GitLab)

### 2. Performance Metrics
Track and compare:
- Total execution time
- Individual test duration
- Flaky test rate
- Pass/fail trends

### 3. Alerts
- Slack notifications for failures
- Email reports for test trends
- PR comments with test summaries

## Rollback Plan

### Triggers for Rollback
1. Test execution time increases >50%
2. Flaky test rate >10%
3. Critical test failures in production

### Rollback Steps
1. Revert to Puppeteer workflow files
2. Update package.json scripts
3. Notify team of rollback
4. Investigation and remediation

## Success Criteria

### Metrics
- ✅ All tests migrated successfully
- ✅ Execution time reduced by 30%+
- ✅ Flaky test rate <2%
- ✅ Cross-browser coverage implemented
- ✅ CI/CD pipeline time <15 minutes

### Validation
- Run both test suites in parallel for 1 week
- Compare results and timing
- Stakeholder sign-off

## Timeline

### Week 1-2: Foundation
- Set up Playwright infrastructure
- Create parallel workflows
- Migrate first test batch

### Week 3-5: Migration
- Migrate remaining tests
- Update CI/CD pipelines
- Performance optimization

### Week 6-7: Validation
- Parallel execution comparison
- Fix any issues
- Documentation update

### Week 8: Cutover
- Remove Puppeteer dependencies
- Archive old tests
- Team training

## Next Steps

1. **Immediate Actions**:
   - Create feature branch for Playwright migration
   - Set up initial Playwright configuration
   - Implement first workflow

2. **Communication**:
   - Notify team of parallel test execution
   - Create migration tracking dashboard
   - Schedule weekly sync meetings

3. **Risk Mitigation**:
   - Backup current test results
   - Document rollback procedures
   - Create troubleshooting guide