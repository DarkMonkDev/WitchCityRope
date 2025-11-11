# DevOps Lessons Learned

## 🚨 MANDATORY STARTUP PROCEDURE 🚨

### 🚨 ULTRA CRITICAL DEVOPS DOCUMENTS (MUST READ): 🚨
1. **🛑 DOCKER DEV GUIDE** - **DOCKER-ONLY DEVELOPMENT**
`/DOCKER_DEV_GUIDE.md` and `/DOCKER_ONLY_DEVELOPMENT.md`

2. **GitHub Push Instructions** - **GIT OPERATIONS**
`/home/chad/repos/witchcityrope/docs/standards-processes/GITHUB-PUSH-INSTRUCTIONS.md`

3. **Architecture Overview** - **SYSTEM STRUCTURE**
`/ARCHITECTURE.md`

4. **CI/CD Pipeline** - **DEPLOYMENT PROCEDURES**
`/home/chad/repos/witchcityrope/docs/guides-setup/ci-cd-setup.md`

### 📚 DOCUMENT DISCOVERY RESOURCES:
- **File Registry** - `/home/chad/repos/witchcityrope/docs/architecture/file-registry.md` - Find any document
- **Functional Areas Index** - `/home/chad/repos/witchcityrope/docs/architecture/functional-area-master-index.md` - Navigate features
- **Key Documents List** - `/home/chad/repos/witchcityrope/docs/standards-processes/KEY-PROJECT-DOCUMENTS.md` - Critical docs

### 📖 ADDITIONAL IMPORTANT DOCUMENTS:
- **Development Guide** - `/home/chad/repos/witchcityrope/docs/guides-setup/development-guide.md` - Dev workflow
- **Deployment Guide** - `/home/chad/repos/witchcityrope/docs/guides-setup/deployment-guide.md` - Production deploy
- **Environment Config** - `/home/chad/repos/witchcityrope/docs/guides-setup/environment-setup.md` - Environment vars
- **Monitoring Setup** - `/home/chad/repos/witchcityrope/docs/guides-setup/monitoring-setup.md` - App monitoring

### Validation Gates (MUST COMPLETE):
- [ ] Read Docker development guides
- [ ] Understand git push procedures
- [ ] Check current deployment status
- [ ] Verify Docker containers running
- [ ] Review CI/CD pipeline configuration
- [ ] Create DevOps handoff document when complete

### DevOps Specific Rules:
- **ALWAYS use Docker for development** - Use container-restart skill (see SKILLS-REGISTRY.md)
- **NEVER commit build artifacts (bin/, obj/, test-results/)**
- **USE HEREDOC for complex commit messages**
- **CHECK container health before deployments**
- **MONITOR memory usage and performance**

## Prevention Pattern: Test Artifact Management

**Problem**: Hundreds of test artifacts (playwright reports, screenshots, videos, session work) clutter git status and accidentally get committed.

**Solution**: Maintain comprehensive .gitignore entries so test artifacts are ignored by git but remain available for review.

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

**🚨 CRITICAL: DO NOT DELETE TEST ARTIFACTS 🚨**

**WHY**: Test artifacts (screenshots, videos, traces, logs) are needed to review and debug test failures.

**CORRECT WORKFLOW**:
1. ✅ Run tests → artifacts saved to test-results/
2. ✅ Review test results using artifacts in test-results/
3. ✅ Git automatically ignores test-results/ (via .gitignore)
4. ✅ Old artifacts naturally accumulate and can be manually cleaned up ONLY when disk space is a concern
5. ✅ **NEVER run cleanup commands from documentation** - they were dangerously wrong

**❌ WRONG ASSUMPTIONS**:
- "Clean up after testing sessions" - NO, you need those artifacts to review results!
- "rm -rf test-results/*" - CATASTROPHIC, deletes everything including important docs
- Test artifacts "clutter git status" - NO, they're gitignored so they don't appear in git status

**LESSON LEARNED (November 11, 2025)**:
- ❌ Documentation contained dangerous cleanup procedure that made NO SENSE
- ❌ Cleanup deleted critical anti-pattern analysis document (142 test issues documented)
- ❌ Document was unrecoverable (rm -rf doesn't use trash)
- ❌ The entire premise was wrong: test artifacts SHOULD remain for review
- ✅ Test-results/ is gitignored, so artifacts don't affect git operations
- ✅ Manual cleanup ONLY when disk space is a concern (not after every test session)
- ✅ Important analysis documents belong in /docs/ or /session-work/, NEVER in test-results/

**IF DISK SPACE BECOMES AN ISSUE** (rare):
1. Check disk usage: `du -sh test-results/`
2. Manually review what's there: `ls -lah test-results/`
3. Selectively delete OLD artifacts only (weeks/months old)
4. NEVER delete anything from the current session or recent sessions

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
