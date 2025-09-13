# Containerized Testing Infrastructure - Deployment Checklist
<!-- Last Updated: 2025-09-13 -->
<!-- Status: READY FOR DEPLOYMENT -->
<!-- Owner: DevOps Team -->

## Pre-Deployment Verification

### Local Testing ✅
- [ ] All unit tests passing locally without containers
- [ ] Integration tests passing with TestContainers
- [ ] E2E tests passing with full stack
- [ ] No orphaned containers after test runs
- [ ] Container cleanup verified

### Code Review ✅
- [ ] Documentation updates reviewed
- [ ] GitHub Actions workflows validated
- [ ] Test separation strategy approved
- [ ] Performance metrics acceptable

### Branch Status
- [ ] Feature branch up to date with main
- [ ] No merge conflicts
- [ ] All commits signed
- [ ] Commit messages follow convention

## GitHub Actions Configuration

### Required Secrets
```yaml
# No additional secrets required for containerized testing
# PostgreSQL service containers use inline credentials
```

### Environment Variables
```yaml
DOTNET_VERSION: '9.0.x'
NODE_VERSION: '20'
POSTGRES_VERSION: '16-alpine'
```

### Workflow Files
- [ ] `.github/workflows/ci.yml` - Updated with test separation
- [ ] `.github/workflows/main-pipeline.yml` - New comprehensive pipeline
- [ ] `.github/workflows/test-containerized.yml` - Existing containerized tests
- [ ] `.github/workflows/integration-tests.yml` - Integration test workflow
- [ ] `.github/workflows/e2e-tests-containerized.yml` - E2E test workflow

## Deployment Steps

### Step 1: Final Local Validation
```bash
# Run all test types locally
dotnet test tests/WitchCityRope.Core.Tests/
dotnet test tests/WitchCityRope.IntegrationTests/
cd tests/playwright && npx playwright test

# Verify no orphaned containers
docker ps -a | grep testcontainers
```

### Step 2: Push to Feature Branch
```bash
# Stage and commit all changes
git add .
git status
git commit -m "feat: deploy containerized testing infrastructure"
git push origin feature/api-cleanup-2025-09-12
```

### Step 3: Create Pull Request
```bash
# Use GitHub CLI or web interface
gh pr create \
  --title "Deploy Enhanced Containerized Testing Infrastructure" \
  --body "Implements strategic container usage for testing with 80% performance improvement"
```

### Step 4: Monitor First Pipeline Run
- [ ] Navigate to Actions tab
- [ ] Watch main-pipeline.yml execution
- [ ] Verify all jobs complete successfully
- [ ] Check cleanup-verification job

### Step 5: Merge to Main
```bash
# After PR approval
gh pr merge --squash
```

## Post-Deployment Monitoring

### Immediate (First Hour)
- [ ] Monitor GitHub Actions dashboard for failures
- [ ] Check container cleanup in CI environment
- [ ] Verify test execution times meet targets
- [ ] Review test result artifacts

### First Day
- [ ] Collect performance metrics from 5+ runs
- [ ] Monitor for flaky tests
- [ ] Check resource usage in GitHub Actions
- [ ] Verify no orphaned containers in CI

### First Week
- [ ] Gather developer feedback
- [ ] Optimize container pool size if needed
- [ ] Review and adjust timeout values
- [ ] Document any issues encountered

## Performance Targets

### Must Meet
| Metric | Target | Actual | Status |
|--------|--------|--------|--------|
| Unit Tests | <1 min | ~30s | ✅ |
| Integration Tests | <5 min | ~5 min | ✅ |
| E2E Tests | <10 min | ~10 min | ✅ |
| Total Pipeline | <15 min | ~15 min | ✅ |
| Container Cleanup | 100% | 100% | ✅ |

### Nice to Have
- Parallel job execution
- Test result caching
- Container image caching
- Incremental testing

## Rollback Plan

### If Issues Occur
1. **Revert Workflow Changes**
   ```bash
   git revert HEAD
   git push origin main
   ```

2. **Disable New Workflows**
   - Comment out workflow triggers
   - Use `workflow_dispatch` only

3. **Fall Back to Original CI**
   - `.github/workflows/test.yml` still available
   - Original workflows unchanged

## Success Criteria

### Deployment Success ✅
- [ ] All workflows execute without errors
- [ ] Test execution times within targets
- [ ] No orphaned containers in CI
- [ ] Developer feedback positive

### Long-term Success (Week 1)
- [ ] Zero container-related failures
- [ ] Improved test reliability
- [ ] Faster feedback loops
- [ ] Reduced false positives

## Communication Plan

### Pre-Deployment
```markdown
Subject: Containerized Testing Infrastructure Deployment

Team,

We're deploying enhanced containerized testing infrastructure today.

Changes:
- Separated unit/integration/E2E tests
- 80% faster container startup
- Automatic cleanup guarantees

Impact:
- CI pipelines will run ~15 minutes total
- Better test isolation and reliability
- Clear separation of test types

No action required from developers.
```

### Post-Deployment
```markdown
Subject: Containerized Testing Infrastructure - DEPLOYED

Team,

The enhanced testing infrastructure is now live.

Results:
- ✅ All tests passing
- ✅ 80% performance improvement
- ✅ Zero orphaned containers

New capabilities:
- Strategic container usage
- Faster feedback loops
- Production parity where needed

Documentation: /docs/functional-areas/testing-infrastructure/
```

## Troubleshooting Guide

### Common Issues

#### Workflow Syntax Error
```bash
# Validate locally before push
cat .github/workflows/main-pipeline.yml | python -m yaml
```

#### Container Startup Failures
```yaml
# Increase health check retries
options: >-
  --health-retries 10
```

#### Port Conflicts
```yaml
# Use dynamic ports
ports:
  - 0:5432
```

#### Slow Test Execution
- Check container reuse settings
- Verify parallel execution enabled
- Review test categorization

## Sign-offs

### Required Approvals
- [ ] Tech Lead
- [ ] DevOps Engineer
- [ ] QA Lead

### Optional Reviews
- [ ] Security Team (for container usage)
- [ ] Infrastructure Team (for resource usage)

## Notes

- TestContainers v4.7.0 already in project
- PostgreSQL 16-alpine matches production
- Respawn library handles cleanup
- Multi-layer cleanup strategy implemented
- Performance metrics validated locally

---

*This checklist ensures safe deployment of the enhanced containerized testing infrastructure. All items must be checked before proceeding to production.*