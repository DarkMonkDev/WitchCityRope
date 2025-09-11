# Port Configuration Management Standard

## Overview

This document establishes the standard for managing port configurations across all WitchCityRope environments to prevent deployment failures and testing inconsistencies.

## 🚨 CRITICAL PRINCIPLES

### 1. NO HARD-CODED PORTS
- **NEVER** use hard-coded localhost URLs in any file
- **ALWAYS** use environment variables for port configuration
- **MUST** use centralized configuration utilities

### 2. SINGLE SOURCE OF TRUTH
- **API Configuration**: `/apps/web/src/config/api.ts`
- **Test Configuration**: `/apps/web/src/test/config/testConfig.ts`
- **Environment Variables**: `.env.development`, `.env.staging`, `.env.production`

## Environment Variable Structure

### Required Environment Variables

```bash
# =============================================================================
# PORT CONFIGURATION - REQUIRED FOR ALL ENVIRONMENTS
# =============================================================================
VITE_PORT=5173                              # React dev server port
VITE_API_BASE_URL=http://localhost:5655     # API base URL (environment-specific)
VITE_API_INTERNAL_PORT=8080                 # API container internal port
VITE_DB_PORT=5433                          # Database external port (dev only)
VITE_TEST_SERVER_PORT=8080                 # Test server port

# =============================================================================
# HMR CONFIGURATION - DEVELOPMENT ONLY
# =============================================================================
VITE_HMR_PORT=24679                        # Hot Module Replacement port
VITE_HMR_HOST=0.0.0.0                     # HMR host
VITE_HMR_CLIENT_PORT=24679                 # HMR client port

# =============================================================================
# DOCKER CONFIGURATION
# =============================================================================
DOCKER_ENV=false                           # Set to true in Docker environments
VITE_API_CONTAINER_URL=http://api:8080     # Container-to-container API URL
```

### Environment-Specific Configurations

| Environment | API Base URL | Docker | Analytics | Debug |
|-------------|--------------|--------|-----------|-------|
| Development | `http://localhost:5655` | false | false | true |
| Staging | `https://api-staging.witchcityrope.com` | true | true | false |
| Production | `https://api.witchcityrope.com` | true | true | false |

## Development Patterns

### ✅ CORRECT Patterns

#### API Requests
```typescript
// Use centralized API configuration
import { apiRequest, apiConfig } from '../config/api'

// Simple request
const response = await apiRequest(apiConfig.endpoints.events.list)

// Request with options
const response = await apiRequest(apiConfig.endpoints.auth.login, {
  method: 'POST',
  body: JSON.stringify(credentials),
})
```

#### Test Files
```typescript
// Use test configuration
import { testConfig, testCredentials } from '../test/config/testConfig'

// Navigate to pages
await page.goto(testConfig.urls.web.login)

// API calls in tests
const response = await page.request.post(testConfig.urls.api.auth.login, {
  data: testCredentials.admin,
})
```

#### Components
```typescript
// Use centralized API utilities
import { getApiBaseUrl, getApiUrl } from '../config/api'

const apiBaseUrl = getApiBaseUrl()
const eventsUrl = getApiUrl('/api/events')
```

### ❌ INCORRECT Patterns

```typescript
// DON'T: Hard-coded URLs
const response = await fetch('http://localhost:5655/api/events')
await page.goto('http://localhost:5173/login')

// DON'T: Mixed port usage
const apiUrl = 'http://localhost:5653/api/events'  // Wrong port!

// DON'T: Environment-specific hard-coding
const baseUrl = process.env.NODE_ENV === 'production' 
  ? 'https://api.witchcityrope.com' 
  : 'http://localhost:5655'  // Use VITE_API_BASE_URL instead
```

## File Organization

### Configuration Files
```
apps/web/
├── .env.template              # Template for all environments
├── .env.development          # Development configuration
├── .env.staging             # Staging configuration  
├── .env.production         # Production configuration
├── src/
│   ├── config/
│   │   └── api.ts          # Centralized API configuration
│   └── test/
│       └── config/
│           └── testConfig.ts # Test environment configuration
└── vite.config.ts          # Build tool configuration
```

### Configuration Hierarchy
1. **Environment Variables** (`.env.{environment}`)
2. **API Configuration** (`src/config/api.ts`)
3. **Test Configuration** (`src/test/config/testConfig.ts`)
4. **Component Usage** (Individual components)

## Migration Guide

### For Existing Files with Hard-coded Ports

#### Step 1: Identify Hard-coded URLs
```bash
# Search for hard-coded localhost references
grep -r "localhost:[0-9]" --include="*.ts" --include="*.tsx" src/
```

#### Step 2: Update API Calls
```typescript
// BEFORE
const response = await fetch('http://localhost:5655/api/events')

// AFTER
import { apiRequest, apiConfig } from '../config/api'
const response = await apiRequest(apiConfig.endpoints.events.list)
```

#### Step 3: Update Test Files
```typescript
// BEFORE
await page.goto('http://localhost:5173/login')

// AFTER
import { testConfig } from '../test/config/testConfig'
await page.goto(testConfig.urls.web.login)
```

#### Step 4: Update Playwright Tests
```typescript
// BEFORE
const API_BASE_URL = 'http://localhost:5655'

// AFTER
import { testConfig } from '../../src/test/config/testConfig'
// Use testConfig.api.baseUrl throughout
```

## Docker Configuration

### Development Docker Compose
```yaml
# docker-compose.dev.yml
services:
  web:
    environment:
      VITE_API_BASE_URL: http://localhost:5655  # External browser access
      DOCKER_ENV: "true"
      VITE_API_CONTAINER_URL: http://api:8080   # Container communication
```

### Vite Proxy Configuration
```typescript
// vite.config.ts
proxy: {
  '/api': {
    target: process.env.DOCKER_ENV === 'true' 
      ? (process.env.VITE_API_CONTAINER_URL || 'http://api:8080')
      : (process.env.VITE_API_BASE_URL || 'http://localhost:5655'),
    changeOrigin: true,
    secure: false,
  },
}
```

## Testing Standards

### Unit Tests
- Use `testConfig.urls` for all URL references
- Import test credentials from `testConfig.testCredentials`
- Use environment-aware timeouts with `testConfig.getTimeouts()`

### Integration Tests
- Use `apiRequest` for API calls in tests
- Use `testConfig.urls.api` for direct API testing
- Use `testConfig.urls.web` for page navigation

### E2E Tests (Playwright)
- Import and use `testConfig` throughout
- No hard-coded URLs allowed
- Use environment variables for different test environments

## Validation and Compliance

### Pre-commit Checks
```bash
# Check for hard-coded localhost references
if grep -r "localhost:[0-9]" --include="*.ts" --include="*.tsx" src/; then
  echo "❌ Hard-coded localhost URLs found. Use environment variables."
  exit 1
fi
```

### Code Review Checklist
- [ ] No hard-coded localhost URLs
- [ ] Uses `apiRequest` or `getApiUrl` for API calls
- [ ] Test files use `testConfig.urls`
- [ ] Environment variables defined in appropriate `.env` files
- [ ] Docker configuration uses environment variables

## Troubleshooting

### Common Issues

1. **Wrong API Port**: Check `VITE_API_BASE_URL` in environment file
2. **Docker Proxy Errors**: Verify `DOCKER_ENV` and container URLs
3. **Test Failures**: Ensure test files use `testConfig.urls`
4. **HMR Not Working**: Check `VITE_HMR_*` configuration

### Debug Commands
```bash
# Check current environment variables
npm run env

# Verify API connectivity
curl $(grep VITE_API_BASE_URL .env.development | cut -d= -f2)/api/health

# Test port availability
netstat -tuln | grep :5655
```

## Updates and Maintenance

### When Adding New Environments
1. Create new `.env.{environment}` file
2. Update `apiConfig` if new endpoints needed
3. Update `testConfig` for environment-specific URLs
4. Update Docker configurations
5. Update this documentation

### When Adding New Services
1. Add port configuration to environment variables
2. Update `apiConfig.endpoints` with new service endpoints
3. Update `testConfig.urls` with new service URLs
4. Update documentation with new service patterns

---

**This standard is mandatory for all development work. Violations will cause build and deployment failures.**