# CI/CD Quick Reference Guide

## 🚀 Quick Start Commands

### Local Testing
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

### CI/CD Integration
```bash
# Setup CI environment
npm run ci:setup

# Run CI-optimized tests
npm run ci:test

# Fast CI verification 
npm run test:ci-fast

# Standard CI test suite
npm run test:ci-standard
```

## 📊 Test Categories

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

## 🐳 Docker Environment

### Start Services
```bash
# Standard environment
docker compose up -d --wait

# CI-optimized environment  
docker compose -f docker-compose.ci.yml up -d --wait

# Check service health
curl http://localhost:5651/health
curl http://localhost:5653/health
```

### Service URLs
- **Web Application**: http://localhost:5651
- **API Service**: http://localhost:5653  
- **PostgreSQL**: localhost:5432
- **pgAdmin** (optional): http://localhost:5050

## 🔧 Environment Configuration

### CI Environment Variables
```bash
export BASE_URL=http://localhost:5651
export CI=true
export PLAYWRIGHT_BROWSERS_PATH=0
export HEADLESS=true
```

### Local Development
```bash
export BASE_URL=http://localhost:5651
export CI=false
export DEBUG=true
export HEADED=true
```

## 📋 GitHub Actions Workflows

### Triggers
- **Push** to main/develop: Standard test suite
- **Pull Request**: Smoke + changed area tests  
- **Schedule**: Nightly comprehensive testing
- **Manual**: Workflow dispatch with options

### Workflow Files
- `.github/workflows/playwright-ci-enhanced.yml` - Main CI workflow
- `.github/workflows/e2e-playwright-js.yml` - Existing E2E tests
- `azure-pipelines.yml` - Azure DevOps (legacy)

## 🏗️ Configuration Files

| File | Purpose |
|------|---------|
| `playwright.config.ci.ts` | CI-optimized Playwright config |
| `.env.ci` | CI environment variables |
| `docker-compose.ci.yml` | CI-optimized Docker setup |
| `test-categories.json` | Test categorization rules |
| `.lighthouserc.js` | Performance testing config |

## 🔍 Debugging Failed Tests

### 1. Local Reproduction
```bash
# Run specific category in debug mode
./scripts/run-playwright-categorized.sh --category auth --debug

# Run with headed browser
./scripts/run-playwright-categorized.sh --category events --headed

# Run single test file
npx playwright test tests/playwright/auth/login-basic.spec.ts --debug
```

### 2. CI Artifact Analysis
1. Download artifacts from GitHub Actions
2. Extract `playwright-report` folder
3. Open `index.html` in browser
4. Review screenshots, traces, and videos

### 3. Service Debugging
```bash
# Check Docker service logs
docker compose logs web
docker compose logs api
docker compose logs postgres

# Check service health
curl -v http://localhost:5651/health
curl -v http://localhost:5653/health
```

## 📈 Performance and Quality Gates

### Lighthouse Thresholds
- **Performance**: ≥70 (error), ≥90 (strict mode)
- **Accessibility**: ≥90 (error), ≥95 (strict mode)  
- **Best Practices**: ≥80 (error)
- **SEO**: ≥70 (warning)

### Test Success Criteria  
- **Smoke Tests**: 100% pass rate required
- **Standard Suite**: ≥95% pass rate required
- **Full Suite**: ≥90% pass rate acceptable
- **Performance**: <4s LCP, <2s FCP, <0.1 CLS

## 🚨 Common Issues and Solutions

### Authentication Failures
```bash
# Check test data seeding
docker compose exec postgres psql -U postgres -d witchcityrope_test -c "SELECT email FROM \"AspNetUsers\";"

# Reset authentication states
rm -rf tests/playwright/.auth/*
```

### Service Startup Issues
```bash
# Clean Docker environment
docker compose down -v
docker system prune -f
docker compose up -d --build --wait
```

### Browser/Playwright Issues
```bash
# Reinstall browsers
npx playwright install --with-deps --force

# Check browser installation
npx playwright doctor
```

### Resource Constraints (CI)
- Reduce parallel workers (already set to 1 in CI)
- Skip visual regression tests on resource-limited runners
- Use Docker resource limits in `docker-compose.ci.yml`

## 📞 Getting Help

### Documentation
- [Full CI/CD Implementation Guide](docs/CI_CD_IMPLEMENTATION_GUIDE.md)
- [Playwright Documentation](https://playwright.dev/docs)
- [Docker Compose Reference](https://docs.docker.com/compose/)

### Logs and Monitoring
- GitHub Actions logs and artifacts
- Docker service logs: `docker compose logs [service]`
- Application logs: `./logs/web/` and `./logs/api/`
- Test results: `./test-results/` and `./playwright-report/`

### Support Contacts
- **CI/CD Issues**: Check GitHub Actions workflow logs
- **Test Failures**: Review Playwright HTML report
- **Infrastructure**: Verify Docker service health
- **Authentication**: Validate test user credentials

---

**Last Updated**: Based on authentication fixes and Docker environment setup
**Version**: 1.0 - Initial CI/CD implementation