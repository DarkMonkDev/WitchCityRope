---
name: test-environment
description: Run Playwright E2E tests in isolated containers separate from dev environment. Handles build, container startup, health checks, test execution, and cleanup. For .NET tests, use run-test-suite skill instead.
---

# test-environment Skill

**Purpose**: Run Playwright E2E tests in isolated containers separate from dev environment.

**What this skill runs**:
- ✅ Playwright E2E tests (`tests/e2e/*.spec.ts`) inside `witchcity-test-runner` container

**What this skill does NOT run**:
- ❌ .NET unit tests — use `run-test-suite` skill (`--mode unit`)
- ❌ .NET integration tests — use `run-test-suite` skill (`--mode unit` — integration is in `tests/integration/` and runs with the other .NET tests)
- ❌ Both .NET + E2E together — use `run-test-suite` skill (`--mode all`)

**Why the split**: See `run-test-suite/SKILL.md` "Why This Skill Exists" — the dead `--mode dotnet|unit|integration|all` paths that used to live here tried to `docker-compose exec api dotnet test` into a container whose test stage only copied `apps/api/` (no test projects), so they silently produced zero results from 2025-11-27 until they were removed on 2026-04-10. The replacement runs .NET tests from the host, which is how WCR's test architecture was designed (TestContainers.PostgreSql spins up per-test postgres containers on demand).

**When to Use**:
- Before running an E2E test suite
- When dev containers are busy with other agents and you need isolation
- For complete test isolation from development work

**Single Source**: This skill is the ONLY way to run E2E tests in isolated containers.

## Features

✅ **Complete Isolation**
- Separate test database (`witchcityrope_test`)
- Separate API and Web containers
- No interference with dev environment

✅ **Fresh Environment**
- Builds from current codebase
- Fresh database each run
- Clean state for every test

✅ **Automatic Cleanup**
- Removes containers after tests
- Removes images (prevents orphans)
- Prunes dangling images
- Optional keep flags for debugging

## Quick Start

```bash
# Run E2E tests (default)
bash .claude/skills/test-environment/execute.sh

# Run specific E2E test file
bash .claude/skills/test-environment/execute.sh --mode e2e --filter "admin-events-dashboard"

# Keep containers for debugging
bash .claude/skills/test-environment/execute.sh --mode e2e --keep-containers
```

For .NET tests or for running everything (.NET + E2E):
```bash
bash .claude/skills/run-test-suite/execute.sh --mode unit   # .NET only
bash .claude/skills/run-test-suite/execute.sh --mode all    # .NET + E2E
```

## Options

| Option | Description |
|--------|-------------|
| `--mode MODE` | Test mode: `e2e` (default) or `failed-only` |
| `--filter PATTERN` | Filter E2E tests by filename pattern |
| `--coverage` | Generate coverage reports (not yet implemented) |
| `--keep-images` | Keep built images for faster reruns |
| `--keep-containers` | Keep containers running for debugging |
| `--skip-confirm` | Skip confirmation prompts (for automation) |

## Test Modes

### `--mode e2e` (default)
Runs Playwright E2E tests against test containers.

### `--mode failed-only`
Reruns previously failed E2E tests (not yet implemented).

## Integration with Agents

### test-executor
```markdown
BEFORE running ANY tests:
1. Use test-environment skill to start isolated containers
2. Skill handles build, start, health checks automatically
3. Run tests in isolation
```

### test-developer
```markdown
When writing/debugging tests:
1. Use test-environment skill with --keep-containers
2. Inspect running containers for debugging
3. Containers remain running until manual cleanup
```

## How It Works

1. **Build** - Creates fresh images from current codebase
2. **Start** - Launches test containers (API, Web, DB)
3. **Health Check** - Verifies compilation and database seeded
4. **Test** - Executes requested tests
5. **Results** - Saves to /test-results/
6. **Cleanup** - Removes containers/images (unless --keep-* flags)

## Files

```
.claude/skills/test-environment/
├── SKILL.md                     # This file
├── execute.sh                   # Main entry point
├── lib/
│   ├── build-containers.sh     # Build test images
│   ├── start-containers.sh     # Start test containers
│   ├── run-tests.sh            # Execute tests
│   ├── health-checks.sh        # Verify environment
│   ├── cleanup.sh              # Remove containers/images
│   └── failed-tests-tracker.sh # Track failures (TODO)
└── config/
    └── test-modes.json         # Test mode configurations
```

## Troubleshooting

**Build fails**: Check for compilation errors in source code

**Health check fails**: Review container logs with `docker logs`

**Tests fail**: Results saved to `/test-results/`

**Orphaned images**: Skill automatically prunes on cleanup

## Future Enhancements

- [ ] Failed test tracking and reruns
- [ ] Coverage report generation
- [ ] Performance baseline tracking
- [ ] Parallel test execution optimization

---

**Last Updated**: 2025-12-02
**Maintained by**: Test Team
**Related**: test-executor-lessons-learned.md, test-developer-lessons-learned.md
