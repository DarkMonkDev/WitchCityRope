# Script Inventory - WitchCityRope Project
<!-- Last Updated: 2025-08-04 -->
<!-- Version: 1.0 -->
<!-- Purpose: Central inventory of all shell scripts in the project -->

> **For Developers**: This is the central registry of all shell scripts. If you're looking for a script or creating a new one, check here first to avoid duplication.

## Quick Reference

### Essential Developer Tools (Root Directory)
These scripts remain in the root for quick access:

| Script | Purpose | Usage | Last Updated |
|--------|---------|-------|--------------|
| `dev.sh` | Main development menu system | `./dev.sh` | Active |
| `restart-web.sh` | Quick restart when hot reload fails | `./restart-web.sh` | Active |
| `check-dev-tools-status.sh` | Verify development environment | `./check-dev-tools-status.sh` | Active |

## Complete Script Inventory

### `/scripts/` - Test Runners
Main test execution scripts:

| Script | Purpose | Usage | Created | Status |
|--------|---------|-------|---------|--------|
| `run-tests.sh` | Run all tests with coverage | `./scripts/run-tests.sh` | Unknown | Active |
| `run-all-integration-tests.sh` | Run integration test suite | `./scripts/run-all-integration-tests.sh` | Unknown | Active |
| `run-performance-tests.sh` | Run performance tests | `./scripts/run-performance-tests.sh` | Unknown | Active |
| `run-tests-coverage.sh` | Generate test coverage report | `./scripts/run-tests-coverage.sh` | Unknown | Active |
| `run-playwright-tests.sh` | Run Playwright E2E tests | `./scripts/run-playwright-tests.sh` | Existing | Active |
| `run-playwright-categorized.sh` | Run categorized Playwright tests | `./scripts/run-playwright-categorized.sh --category auth` | Existing | Active |

### `/scripts/docker/` - Docker Utilities
Docker development environment management:

| Script | Purpose | Usage | Created | Status |
|--------|---------|-------|---------|--------|
| `docker-quick.sh` | Quick Docker startup | `./scripts/docker/docker-quick.sh` | Unknown | Review |
| `docker-dev.sh` | Docker development setup | `./scripts/docker/docker-dev.sh` | Unknown | Review |
| `start-docker.sh` | Start Docker containers | `./scripts/docker/start-docker.sh` | Unknown | Review |
| `open-docker-site.sh` | Open browser to Docker app | `./scripts/docker/open-docker-site.sh` | Unknown | Active |
| `run-local.sh` | Run local environment | `./scripts/docker/run-local.sh` | Unknown | Review |

### `/scripts/database/` - Database Management
Database operations and migrations:

| Script | Purpose | Usage | Created | Status |
|--------|---------|-------|---------|--------|
| `run-migration.sh` | Apply database migrations | `./scripts/database/run-migration.sh` | Unknown | Active |
| `seed-database.sh` | Seed test data | `./scripts/database/seed-database.sh` | Existing | Active |
| `apply-migrations.sh` | Apply EF Core migrations | `./scripts/database/apply-migrations.sh` | Existing | Active |
| `generate-migration.sh` | Create new migration | `./scripts/database/generate-migration.sh` | Existing | Active |

### `/scripts/diagnostics/` - Diagnostic Tools
Environment and health checks:

| Script | Purpose | Usage | Created | Status |
|--------|---------|-------|---------|--------|
| `run-diagnostic.sh` | Run diagnostic checks | `./scripts/diagnostics/run-diagnostic.sh` | Unknown | Active |
| `check-mcp-status.sh` | Check MCP server status | `./scripts/diagnostics/check-mcp-status.sh` | Unknown | Active |
| `check-vulnerabilities.sh` | Security vulnerability scan | `./scripts/diagnostics/check-vulnerabilities.sh` | Unknown | Active |

### `/scripts/setup/` - Setup and Installation
Initial setup and configuration:

| Script | Purpose | Usage | Created | Status |
|--------|---------|-------|---------|--------|
| `install-dev-tools.sh` | Install .NET SDK & PostgreSQL | `./scripts/setup/install-dev-tools.sh` | Unknown | Active |
| `claude-start.sh` | Start Claude with MCP servers | `./scripts/setup/claude-start.sh` | Unknown | Active |
| `push-to-github.sh` | Configure GitHub remote | `./scripts/setup/push-to-github.sh` | Unknown | Active |

### `/scripts/_archive/` - Archived Scripts
Scripts no longer in active use but preserved for reference:

#### `/scripts/_archive/login-tests/`
Login testing scripts from authentication debugging phase:

| Script | Purpose | Original Location | Archived Date |
|--------|---------|------------------|---------------|
| `login-test.sh` | Basic login test | Root | 2025-08-04 |
| `test-login.sh` | Login functionality test | Root | 2025-08-04 |
| `test_login_detailed.sh` | Detailed login test | Root | 2025-08-04 |
| `test_login_curl.sh` | cURL-based login test | Root | 2025-08-04 |
| `simple-login-test.sh` | Simple login verification | Root | 2025-08-04 |

#### `/scripts/_archive/fixes/`
One-time fix scripts:

| Script | Purpose | Original Location | Archived Date |
|--------|---------|------------------|---------------|
| `fix-navigation.sh` | Add forceLoad to navigation | Root | 2025-08-04 |
| `fix-render-modes.sh` | Fix Blazor render modes | Root | 2025-08-04 |

#### `/scripts/_archive/utilities/`
Miscellaneous utilities:

| Script | Purpose | Original Location | Archived Date |
|--------|---------|------------------|---------------|
| `start-chrome-debug.sh` | Chrome debugging setup | Root | 2025-08-04 |
| `test-internal-api.sh` | Internal API testing | Root | 2025-08-04 |
| `test-endpoints.sh` | Endpoint testing | Root | 2025-08-04 |
| `test-all-tools.sh` | Tool verification | Root | 2025-08-04 |
| `run.sh` | Redundant wrapper for dev-start.sh | Root | 2025-08-04 |

## Script Guidelines

### Where to Place New Scripts

1. **High-frequency developer tools** → Root directory (requires team approval)
   - Must be documented in CLAUDE.md
   - Should be used by multiple developers daily

2. **Docker/container management** → `/scripts/docker/`
3. **Test runners** → `/scripts/`
4. **Database operations** → `/scripts/database/`
5. **Diagnostics/health checks** → `/scripts/diagnostics/`
6. **Setup/installation** → `/scripts/setup/`
7. **One-time fixes** → `/scripts/_archive/fixes/`
8. **Deprecated scripts** → `/scripts/_archive/`

### Best Practices

1. **Before creating a new script**:
   - Check this inventory to avoid duplication
   - Consider if an existing script can be extended
   - Discuss with team if it should be in root directory

2. **When creating a new script**:
   - Add clear comments explaining purpose
   - Include usage examples
   - Update this inventory
   - Make script executable: `chmod +x script.sh`

3. **Script naming conventions**:
   - Use descriptive names: `run-integration-tests.sh` not `test.sh`
   - Use hyphens, not underscores: `check-status.sh` not `check_status.sh`
   - Include action verb: `seed-database.sh` not `database.sh`

4. **Documentation**:
   - Add entry to this inventory immediately
   - Include purpose, usage, and examples
   - Note any dependencies or prerequisites

## Maintenance

- Review archived scripts quarterly for removal
- Update status when scripts become deprecated
- Keep inventory current when adding new scripts

---

*This inventory is maintained by the DevOps team. For questions or updates, create a PR or contact the team.*