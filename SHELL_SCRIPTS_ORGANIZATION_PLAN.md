# Shell Scripts Organization Plan
<!-- Created: 2025-08-04 -->
<!-- Purpose: Plan for organizing shell scripts in root directory -->

## Current Situation
- **30 shell scripts** in root directory
- Existing `/scripts/` directory with database, testing, and deployment scripts
- Key scripts (`dev.sh`, `restart-web.sh`) referenced in CLAUDE.md
- Mix of active tools, legacy scripts, and debugging utilities

## Proposed Organization

### 1. Scripts to KEEP in Root (High-frequency, documented use)
These scripts are referenced in documentation and used frequently by developers:

| Script | Purpose | References |
|--------|---------|------------|
| `dev.sh` | Main development menu | CLAUDE.md (primary dev tool) |
| `restart-web.sh` | Quick restart when hot reload fails | CLAUDE.md |
| `check-dev-tools-status.sh` | Development environment verification | Active utility |

### 2. Scripts to MOVE to Existing `/scripts/` Subdirectories

#### `/scripts/` (existing directory - add these)
- `run-tests.sh` → Main test runner
- `run-all-integration-tests.sh` → Integration test runner
- `run-performance-tests.sh` → Performance test runner
- `run-tests-coverage.sh` → Test coverage runner

#### `/scripts/docker/` (create new)
- `docker-quick.sh` → Docker quick start
- `docker-dev.sh` → Docker development setup
- `start-docker.sh` → Docker startup
- `open-docker-site.sh` → Open browser to Docker site
- `run-local.sh` → Local environment runner

#### `/scripts/diagnostics/` (create new)
- `run-diagnostic.sh` → Run diagnostics
- `check-mcp-status.sh` → MCP server status check
- `check-vulnerabilities.sh` → Security vulnerability scan

#### `/scripts/setup/` (create new)
- `install-dev-tools.sh` → Install development tools
- `claude-start.sh` → Start Claude with MCP servers
- `push-to-github.sh` → GitHub remote setup helper

#### `/scripts/database/` (add to existing)
- `run-migration.sh` → Database migration runner

### 3. Scripts to ARCHIVE

#### `/scripts/_archive/login-tests/` (create new)
These are debugging scripts from authentication troubleshooting:
- `login-test.sh`
- `test-login.sh`
- `test_login_detailed.sh`
- `test_login_curl.sh`
- `simple-login-test.sh`

#### `/scripts/_archive/fixes/` (create new)
One-time fix scripts that are likely no longer needed:
- `fix-navigation.sh` → Added forceLoad to navigation
- `fix-render-modes.sh` → Fixed render modes

#### `/scripts/_archive/utilities/` (create new)
- `start-chrome-debug.sh` → Chrome debugging (check if still needed)
- `test-internal-api.sh` → Internal API testing
- `test-endpoints.sh` → Endpoint testing
- `test-all-tools.sh` → Tool verification

### 4. Scripts to REVIEW for Removal
- `run.sh` → Just calls `scripts/dev-start.sh` (redundant)

## Implementation Plan

### Phase 1: Create Directory Structure
```bash
mkdir -p scripts/docker
mkdir -p scripts/diagnostics
mkdir -p scripts/setup
mkdir -p scripts/_archive/login-tests
mkdir -p scripts/_archive/fixes
mkdir -p scripts/_archive/utilities
```

### Phase 2: Move Scripts
1. Move scripts according to the plan above
2. Test key workflows (dev.sh, restart-web.sh remain functional)
3. Verify no broken references

### Phase 3: Update Documentation
1. Create `/scripts/README.md` documenting the structure
2. Update any references in documentation
3. Add to file registry

### Phase 4: Create Developer Guidelines
Document where new scripts should go:
- **Frequently used dev tools** → Root directory (with approval)
- **Docker utilities** → `/scripts/docker/`
- **Test runners** → `/scripts/`
- **Database scripts** → `/scripts/database/`
- **Setup/installation** → `/scripts/setup/`
- **Diagnostics** → `/scripts/diagnostics/`
- **One-time fixes** → `/scripts/_archive/fixes/`

## Benefits
1. **Cleaner root directory** - Only essential scripts remain
2. **Logical organization** - Scripts grouped by function
3. **Easy discovery** - Developers can find scripts by category
4. **Clear lifecycle** - Archive for outdated scripts
5. **Maintains compatibility** - Key scripts stay in root

## Risks & Mitigation
1. **Risk**: Breaking references to moved scripts
   - **Mitigation**: Keep frequently-used scripts in root, test after moves
   
2. **Risk**: Developers can't find scripts
   - **Mitigation**: Clear documentation, intuitive organization
   
3. **Risk**: CI/CD breakage
   - **Mitigation**: Check GitHub Actions for script references

## Questions for Approval
1. Should we keep `check-dev-tools-status.sh` in root or move to diagnostics?
2. Are there any other scripts that should remain in root for quick access?
3. Should archived scripts be deleted after a certain time period?