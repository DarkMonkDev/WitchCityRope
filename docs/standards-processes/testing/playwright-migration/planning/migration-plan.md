# Puppeteer to Playwright Migration Plan

## Executive Summary
This document outlines the comprehensive plan to migrate 180+ Puppeteer E2E tests to Playwright for the WitchCityRope Blazor Server application. The migration will improve test reliability, performance, and CI/CD integration while maintaining complete test coverage.

## Migration Timeline
**Total Duration**: 8-10 weeks  
**Start Date**: January 21, 2025  
**Target Completion**: March 25, 2025

## Phase Overview

### Phase 1: Foundation & Proof of Concept (Week 1-2)
- Set up Playwright infrastructure
- Create migration tooling and helpers
- Convert one complete test as POC
- Establish patterns and best practices
- Get stakeholder approval

### Phase 2: Core Test Migration (Week 3-5)
- Convert authentication tests (High priority)
- Convert admin management tests
- Convert event flow tests
- Maintain parallel Puppeteer tests

### Phase 3: Extended Test Migration (Week 6-7)
- Convert RSVP and ticket tests
- Convert validation tests
- Convert diagnostic utilities
- Add missing high-priority tests

### Phase 4: Cutover & Optimization (Week 8-9)
- Complete remaining migrations
- Remove Puppeteer dependencies
- Optimize test execution
- Update CI/CD pipelines

### Phase 5: Documentation & Training (Week 10)
- Complete documentation
- Create training materials
- Knowledge transfer sessions
- Final cleanup

## Test Storage Location

### Centralized Test Structure
All Playwright tests will be stored in a single, well-organized location:
- **Primary Location**: `/home/chad/repos/witchcityrope/WitchCityRope/tests/playwright/`
- **Current Puppeteer Location**: `/tests/e2e/tests/e2e/` (to be migrated)

The centralized structure ensures:
- Easy discovery of all tests
- Consistent organization patterns
- Simplified CI/CD configuration
- Clear separation from legacy Puppeteer tests

## Technical Migration Strategy

### 1. Infrastructure Setup
```typescript
// Playwright Configuration
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/e2e',
  timeout: 30000,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  
  use: {
    baseURL: 'http://localhost:5651',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    trace: 'on-first-retry',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },
  ],

  webServer: {
    command: 'docker-compose -f docker-compose.yml -f docker-compose.dev.yml up',
    port: 5651,
    reuseExistingServer: !process.env.CI,
  },
});
```

### 2. Test Conversion Pattern

#### Puppeteer Test Example:
```javascript
const puppeteer = require('puppeteer');

describe('Login Test', () => {
  let browser;
  let page;

  beforeEach(async () => {
    browser = await puppeteer.launch();
    page = await browser.newPage();
    await page.goto('http://localhost:5651');
  });

  afterEach(async () => {
    await browser.close();
  });

  it('should login successfully', async () => {
    await page.waitForSelector('#email');
    await page.type('#email', 'admin@witchcityrope.com');
    await page.type('#password', 'Test123!');
    await page.click('button[type="submit"]');
    await page.waitForNavigation();
    const title = await page.title();
    expect(title).toContain('Dashboard');
  });
});
```

#### Converted Playwright Test:
```typescript
import { test, expect } from '@playwright/test';
import { LoginPage } from './pages/login.page';

test.describe('Login Test', () => {
  test('should login successfully', async ({ page }) => {
    const loginPage = new LoginPage(page);
    
    await loginPage.goto();
    await loginPage.login('admin@witchcityrope.com', 'Test123!');
    
    // Wait for Blazor Server to complete navigation
    await page.waitForSelector('[data-testid="dashboard"]', { 
      state: 'visible',
      timeout: 10000 
    });
    
    await expect(page).toHaveTitle(/Dashboard/);
  });
});
```

### 3. Page Object Model Implementation
```typescript
// pages/login.page.ts
export class LoginPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/login');
    // Wait for Blazor Server to initialize
    await this.page.waitForSelector('[data-testid="login-form"]');
  }

  async login(email: string, password: string) {
    await this.page.fill('[data-testid="email-input"]', email);
    await this.page.fill('[data-testid="password-input"]', password);
    await this.page.click('[data-testid="login-button"]');
  }
}
```

### 4. Blazor Server Specific Helpers
```typescript
// helpers/blazor.helpers.ts
export class BlazorHelpers {
  static async waitForBlazorReady(page: Page) {
    // Wait for SignalR connection
    await page.waitForFunction(() => 
      window.Blazor && window.Blazor._internal.navigationManager
    );
  }

  static async waitForComponentUpdate(page: Page, selector: string) {
    // Wait for specific component to render
    await page.waitForSelector(selector, { state: 'visible' });
    // Additional wait for any pending renders
    await page.waitForTimeout(100);
  }

  static async handleAuthenticationState(page: Page) {
    // Store auth state for reuse
    const storageState = await page.context().storageState();
    return storageState;
  }
}
```

## CI/CD Integration Plan

### 1. GitHub Actions Configuration
```yaml
name: E2E Tests
on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    container:
      image: mcr.microsoft.com/playwright:v1.40.0-focal
    
    services:
      postgres:
        image: postgres:16-alpine
        env:
          POSTGRES_PASSWORD: WitchCity2024!
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Build application
      run: |
        cd src/WitchCityRope.Web
        dotnet build
    
    - name: Install Playwright
      run: npx playwright install --with-deps
    
    - name: Run E2E tests
      run: npx playwright test
      env:
        DATABASE_URL: postgres://postgres:WitchCity2024!@postgres:5432/witchcityrope_test
    
    - uses: actions/upload-artifact@v3
      if: always()
      with:
        name: playwright-report
        path: playwright-report/
        retention-days: 30
```

### 2. Docker Integration
```dockerfile
# Dockerfile.playwright
FROM mcr.microsoft.com/playwright:v1.40.0-focal

WORKDIR /app

# Copy test files
COPY tests/e2e ./tests/e2e
COPY playwright.config.ts ./
COPY package*.json ./

# Install dependencies
RUN npm ci

# Run tests
CMD ["npx", "playwright", "test"]
```

## Migration Checklist

### Pre-Migration Tasks
- [ ] Set up Playwright project structure
- [ ] Install Playwright dependencies
- [ ] Create base configuration
- [ ] Set up Page Object Model structure
- [ ] Create Blazor-specific helpers
- [ ] Configure test data management
- [ ] Set up parallel execution strategy

### Per-Test Migration Steps
1. [ ] Identify test dependencies and helpers
2. [ ] Create/update Page Object for test
3. [ ] Convert test syntax from Puppeteer to Playwright
4. [ ] Add Blazor-specific waits and assertions
5. [ ] Add data-testid attributes to components
6. [ ] Run test in isolation
7. [ ] Run test in full suite
8. [ ] Update test documentation
9. [ ] Remove Puppeteer version

### Post-Migration Tasks
- [ ] Remove Puppeteer from package.json
- [ ] Update all test documentation
- [ ] Update CI/CD pipelines
- [ ] Create migration guide for future tests
- [ ] Archive Puppeteer test history
- [ ] Performance benchmarking
- [ ] Team training sessions

## Risk Mitigation

### Identified Risks
1. **Test Coverage Gaps**
   - Mitigation: Run both suites in parallel during migration
   - Verification: Coverage reports before/after

2. **SignalR Timing Issues**
   - Mitigation: Implement robust wait strategies
   - Use Blazor-specific helpers

3. **CI/CD Disruption**
   - Mitigation: Gradual rollout with feature flags
   - Maintain both pipelines temporarily

4. **Team Learning Curve**
   - Mitigation: Comprehensive documentation
   - Pair programming sessions

5. **Test Data Dependencies**
   - Mitigation: Implement test isolation
   - Use database transactions/cleanup

## Success Metrics

### Quantitative Metrics
- **Test Execution Time**: Target 30-50% reduction
- **Test Flakiness**: <2% flaky test rate
- **Coverage**: Maintain 100% feature coverage
- **CI Pipeline Time**: <15 minutes for full suite

### Qualitative Metrics
- Developer satisfaction with test writing
- Reduced maintenance burden
- Improved debugging capabilities
- Better cross-browser coverage

## Resource Requirements

### Human Resources
- 1 Senior Developer (full-time): 10 weeks
- 1 QA Engineer (part-time): Review and validation
- 1 DevOps Engineer (part-time): CI/CD updates

### Technical Resources
- Playwright licenses (included in npm)
- Additional CI/CD runners for parallel execution
- Test environment infrastructure

## Communication Plan

### Stakeholder Updates
- Weekly progress reports
- Bi-weekly demos of migrated tests
- Immediate escalation for blockers

### Team Communication
- Daily standup updates during active migration
- Slack channel: #playwright-migration
- Wiki documentation updates

## Rollback Plan

If critical issues arise:
1. Maintain Puppeteer tests in separate branch
2. Feature flag for test runner selection
3. Gradual rollback procedure documented
4. Maximum rollback time: 4 hours

## Next Steps

1. **Immediate Actions**:
   - Get stakeholder approval for plan
   - Set up Playwright infrastructure
   - Convert first test as POC

2. **Week 1 Deliverables**:
   - POC test running successfully
   - CI/CD pipeline prototype
   - Team training materials drafted

3. **Success Criteria for POC**:
   - Test runs reliably 10/10 times
   - Execution time improved
   - Clear migration pattern established