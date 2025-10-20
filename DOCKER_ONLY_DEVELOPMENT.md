# Docker-Only Development Environment

## 🚨 CRITICAL: DOCKER CONTAINERS ONLY

This project has been configured to **ONLY** use Docker containers for development. Local dev servers are **DISABLED** to prevent confusion and ensure consistent development environments.

## Why Docker-Only?

### Problems with Mixed Environments:
- ❌ Multiple Vite servers running (Docker on 5173, local on 5174/5175)
- ❌ Tests sometimes run against local servers instead of Docker
- ❌ Code changes work in tests but not in Docker
- ❌ Massive confusion about which environment is being used
- ❌ Wasted time debugging environment issues

### Docker-Only Benefits:
- ✅ Single source of truth for development
- ✅ Tests ALWAYS run against the same environment as production
- ✅ No port conflicts or confusion
- ✅ Consistent development across all machines
- ✅ Easy to identify and fix environment issues

## How It Works

### 1. Package.json Scripts Modified
```json
{
  "scripts": {
    "dev": "echo '❌ ERROR: Use ./dev.sh to start Docker containers. Local dev servers are disabled.' && exit 1",
    "dev:docker-only": "vite"  // Only used inside Docker containers
  }
}
```

### 2. Vite Config Enforces Port 5173
```typescript
server: {
  strictPort: true, // ENFORCE port 5173 - fail if unavailable
  port: 5173
}
```

### 3. Tests Only Accept Docker Environment
- Playwright config hardcoded to `http://localhost:5173`
- Global setup verifies Docker containers are running
- Tests FAIL if containers not available

## Usage Instructions

### ✅ CORRECT - Start Development Environment
```bash
# Option 1: Use the interactive dev script (RECOMMENDED)
./dev.sh

# Option 2: Direct docker-compose command
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# Check all services are running
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

### ❌ WRONG - These Commands Will Fail
```bash
# These will show error messages and exit
npm run dev
cd apps/web && npm run dev
turbo run dev
```

### 🧹 Cleanup Local Processes
If you suspect local dev servers are running:
```bash
# Kill any local dev servers
./scripts/kill-local-dev-servers.sh

# Then start Docker
./dev.sh
```

## Development Workflow

### 1. Start Development
```bash
./dev.sh
# Wait for "ready" messages in logs
```

### 2. Access Services
- **React App**: http://localhost:5173
- **API**: http://localhost:5655
- **Database**: **localhost:5434** (postgres/devpass123) - **DEDICATED PORT** for WitchCityRope

### 3. Make Code Changes
- React: Hot reload works automatically
- API: `dotnet watch` restarts within 3-5 seconds
- Database: Migrations applied automatically

### 4. Run Tests
```bash
# E2E tests (will verify Docker environment first)
npm run test:e2e:playwright

# Unit tests
npm run test

# All tests
npm run test:all
```

### 5. Stop Development
```bash
docker-compose down
```

## Troubleshooting

### Problem: Tests Fail with "Connection Refused"
**Solution**: Docker containers aren't running
```bash
./dev.sh
# Wait for services to start, then run tests
```

### Problem: Port 5173 Already in Use
**Solution**: Kill local processes
```bash
./scripts/kill-local-dev-servers.sh
docker-compose down
./dev.sh
```

### Problem: "npm run dev" Shows Error Message
**Solution**: This is CORRECT behavior. Use Docker instead.
```bash
./dev.sh
```

### Problem: Changes Not Reflecting
**Solution**: Restart the specific container
```bash
# For React changes
docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart web

# For API changes
docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart api
```

### Problem: Container Won't Start
**Solution**: Check logs and rebuild
```bash
# Check logs
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs web

# Rebuild if needed
docker-compose down
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d --build
```

## File Locations

### Configuration Files
- `/package.json` - Root package.json with disabled dev script
- `/apps/web/package.json` - Web package.json with disabled dev script
- `/apps/web/vite.config.ts` - Vite config with strictPort enforcement
- `/playwright.config.ts` - Test config hardcoded to Docker ports

### Scripts
- `/scripts/kill-local-dev-servers.sh` - Cleanup script for local processes
- `/dev.sh` - Interactive Docker development script

### Test Setup
- `/tests/e2e/global-setup.ts` - Verifies Docker environment before tests

## Architecture

```
┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐
│   React Web App    │────│     .NET API       │────│   PostgreSQL DB    │
│   localhost:5173   │    │   localhost:5655   │    │   localhost:5433   │
│  (Docker Container) │    │  (Docker Container) │    │  (Docker Container) │
└─────────────────────┘    └─────────────────────┘    └─────────────────────┘
           │                           │                           │
           └───────────────────────────┼───────────────────────────┘
                                       │
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Docker Network                                   │
│                         witchcity-net                                      │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Important Notes

### ⚠️ Never Bypass Docker
- Don't modify package.json to re-enable local dev scripts
- Don't change Playwright config to use different ports
- Don't try to run local servers alongside Docker

### ✅ Extending the System
If you need additional development features:
1. Add them to the Docker containers
2. Update docker-compose.dev.yml
3. Test in the Docker environment
4. Document in this file

### 🔧 Emergency Override
If you absolutely must run local servers for debugging:
1. Temporarily rename `dev:docker-only` back to `dev`
2. Stop Docker containers first: `docker-compose down`
3. Remember to revert changes and restart Docker when done

## Enforcement Mechanisms

1. **Package.json scripts**: Show error messages
2. **Vite config**: `strictPort: true` prevents port switching
3. **Test setup**: Verifies Docker before running tests
4. **Documentation**: This file clearly explains the system
5. **Scripts**: Cleanup tools to prevent conflicts

This system ensures **zero confusion** about which environment is being used.