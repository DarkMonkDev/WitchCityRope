# DevOps Lessons Learned

## 🚨 MANDATORY STARTUP PROCEDURE 🚨

### 🚨 ULTRA CRITICAL DEVOPS DOCUMENTS (MUST READ): 🚨
1. **🛑 DOCKER DEV GUIDE** - **DOCKER-ONLY DEVELOPMENT**
`/DOCKER_DEV_GUIDE.md` and `/DOCKER_ONLY_DEVELOPMENT.md`

2. **GitHub Push Instructions** - **GIT OPERATIONS**
`/docs/standards-processes/GITHUB-PUSH-INSTRUCTIONS.md`

3. **Architecture Overview** - **SYSTEM STRUCTURE**
`/ARCHITECTURE.md`

4. **CI/CD Pipeline** - **DEPLOYMENT PROCEDURES**
`/docs/guides-setup/ci-cd-setup.md`

### 📚 DOCUMENT DISCOVERY RESOURCES:
- **File Registry** - `/docs/architecture/file-registry.md` - Find any document
- **Functional Areas Index** - `/docs/architecture/functional-area-master-index.md` - Navigate features
- **Key Documents List** - `/docs/standards-processes/KEY-PROJECT-DOCUMENTS.md` - Critical docs

### 📖 ADDITIONAL IMPORTANT DOCUMENTS:
- **Development Guide** - `/docs/guides-setup/development-guide.md` - Dev workflow
- **Deployment Guide** - `/docs/guides-setup/deployment-guide.md` - Production deploy
- **Environment Config** - `/docs/guides-setup/environment-setup.md` - Environment vars
- **Monitoring Setup** - `/docs/guides-setup/monitoring-setup.md` - App monitoring

### Validation Gates (MUST COMPLETE):
- [ ] Read Docker development guides
- [ ] Understand git push procedures
- [ ] Check current deployment status
- [ ] Verify Docker containers running
- [ ] Review CI/CD pipeline configuration
- [ ] Create DevOps handoff document when complete

### DevOps Specific Rules:
- **ALWAYS use Docker for development (./dev.sh)**
- **NEVER commit build artifacts (bin/, obj/, test-results/)**
- **USE HEREDOC for complex commit messages**
- **CHECK container health before deployments**
- **MONITOR memory usage and performance**

## Prevention Pattern: Test Artifact Management

**Problem**: Hundreds of test artifacts (playwright reports, screenshots, videos, session work) clutter git status and accidentally get committed.

**Solution**: Maintain comprehensive .gitignore entries for test artifacts and perform cleanup after testing sessions.

**Required .gitignore Entries**:
```
# Playwright test artifacts
playwright-report/
**/playwright-report/data/
test-results/
**/test-results/
apps/web/test-results/
*.png
*.webm

# Temporary session work
session-work/

# Temporary test files in root
/test-*.spec.ts
```

**Cleanup Procedure After Testing**:
1. Review uncommitted changes: `git status` and `git diff`
2. Delete test artifacts: `rm -rf apps/web/playwright-report/data/*`, `rm -rf test-results/*`
3. Delete session work: `rm -rf session-work/[date]/`
4. Delete temporary specs: `rm -f test-*.spec.ts`, `rm -f tests/playwright/temp-*.spec.ts`
5. Update .gitignore if new patterns emerge
6. Stage cleanup: `git add -u && git add .gitignore`
7. Commit: `git commit -m "chore: Clean up test artifacts and update .gitignore"`

**Example Cleanup Session (Oct 8, 2025)**:
- Deleted 200+ Playwright videos (.webm) and screenshots (.png)
- Removed test-results directories from Oct 6-8
- Cleaned temporary test specs in root
- Updated .gitignore to prevent recurrence
- Result: Clean working tree with 55 files removed

## Prevention Pattern: Silent Fallback Data Anti-Pattern

**Problem**: Code using fallback/mock data that silently masks API failures, allowing bugs to persist undetected.
**Solution**: NEVER use silent fallback data. Always make API failures visible to users and developers. Use proper error handling with meaningful messages instead of hiding problems with fake data.

**Critical Issues Caused**:
- API authentication failures hidden by fallback data
- Navigation broken due to sequential IDs (1-8) vs real GUIDs
- Users see fake data instead of real system state
- Debugging impossible when real errors are masked
- Production deployments fail unexpectedly

**Implementation**:
- Remove all hardcoded fallback datasets from API hooks
- Set `throwOnError: true` in React Query
- Add comprehensive logging for API calls and responses
- Implement proper HTTP status code error handling
- Show meaningful error messages to users

## Prevention Pattern: Build Artifact Exclusion

**Problem**: Build artifacts (bin/, obj/, test-results/) accidentally committed to repository.
**Solution**: Always verify staging with `git diff --staged --name-only | grep -E "(bin/|obj/|test-results/)"` before commit. Use selective staging with specific file paths instead of `git add -A`.

## Prevention Pattern: Docker Container Health Monitoring

**Problem**: API container crashes with exit code 137 (memory/kill signal).
**Solution**: Monitor container memory usage, check Docker logs for OOM issues, ensure adequate memory allocation for containers, implement health checks.

## Prevention Pattern: Authentication Architecture Alignment

**Problem**: Frontend expects JWT tokens but backend uses httpOnly cookies.
**Solution**: Ensure frontend uses `withCredentials: true` for API calls and removes all localStorage token handling. Backend should handle authentication entirely through httpOnly cookies.

## Prevention Pattern: Entity Framework Navigation Properties

**Problem**: Related data not persisting due to missing navigation properties.
**Solution**: Always define navigation properties in entity models, configure relationships in DbContext, and use Include() statements in queries to load related data.

## Prevention Pattern: Memory Leak from Debug Logging

**Problem**: Console.log statements retain object references preventing garbage collection.
**Solution**: Remove all debug console.log statements before commit, especially those logging large objects or API responses. Set appropriate memory monitoring thresholds for application size.

## Prevention Pattern: Git Commit Message Structure

**Problem**: Unclear or incomplete commit messages make history hard to understand.
**Solution**: Use HEREDOC pattern for detailed commit messages with sections for technical details, issues resolved, and verification results. Always include file registry updates.

## Prevention Pattern: Middleware Conflict Resolution

**Problem**: Custom middleware intercepting requests before proper controllers.
**Solution**: Review middleware pipeline order in Program.cs, ensure authentication middleware runs before custom middleware, remove conflicting middleware that intercepts auth endpoints.

## Prevention Pattern: Form State Persistence

**Problem**: React forms lose state on component re-mounting.
**Solution**: Use `hasInitialized` ref pattern to prevent form re-initialization, ensure proper component lifecycle management, invalidate query client appropriately after updates.

## Prevention Pattern: Selective File Staging

**Problem**: Staging too many files or wrong files in commits.
**Solution**: Always stage files individually by path, never use `git add -A` for code commits, verify staged files with `git diff --staged --name-only` before commit.

## Prevention Pattern: Documentation Maintenance

**Problem**: File registry and documentation becoming outdated.
**Solution**: Update file registry in every commit that touches files, maintain cleanup schedule for temporary files, archive old session work regularly.
