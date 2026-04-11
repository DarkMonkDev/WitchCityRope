---
name: run-test-suite
description: Run .NET unit/integration tests (host-based dotnet test) or Playwright E2E tests for WitchCityRope. Single source of truth for test execution. Use --mode unit for fast .NET runs, --mode e2e for browser tests (delegates to test-environment skill), --mode all for both.
---

# run-test-suite Skill

**Purpose**: Run the WitchCityRope test suite. This is the **single source of truth** for how tests are executed in this repo.

## Why This Skill Exists

The previous `test-environment` skill tried to run .NET tests inside the `witchcity-api-test` container via `docker-compose exec api dotnet test`. That was architecturally broken: the api container's test stage only copies `apps/api/` — no test projects ever existed inside it, so `dotnet test` had nothing to run. It silently produced zero results on every invocation from 2025-11-27 until this skill replaced it.

This skill is ported from `inventory-purchasing-workflow/.claude/skills/run-test-suite/` (a sibling repo using the same architecture stack). That implementation is battle-tested with hard-won safety nets — see comments in `execute.sh` for details.

## Architecture

**.NET tests run ON THE HOST**, not in a container:
- `tests/unit/api/WitchCityRope.Api.Tests.csproj` — API endpoint/service unit tests (vertical slice)
- `tests/WitchCityRope.Core.Tests/WitchCityRope.Core.Tests.csproj` — Core/domain tests
- `tests/integration/WitchCityRope.IntegrationTests.csproj` — Integration tests (WebApplicationFactory)
- `tests/WitchCityRope.SystemTests/WitchCityRope.SystemTests.csproj` — Pre-flight health checks for the dev environment ([Trait("Category", "HealthCheck")]). These hit dev URLs (React :5173, API :5655, postgres :5434), not test container URLs. They'll fail if dev containers aren't running — that's intentional, they're meant as a "is the dev environment actually up?" sanity check. If you're running the .NET suite without dev containers up and want to skip them, filter them out with `--filter "Category!=HealthCheck"`.

All three use `Testcontainers.PostgreSql`, which spins up its own per-test postgres container on demand. They don't need the long-running witchcity-test-* containers to be up — each test run manages its own database lifecycle.

**Prerequisites**:
- `dotnet` 10.0+ on the host PATH (`/home/chad/.dotnet/dotnet` — already installed)
- Docker running (for Testcontainers to spin up postgres)

**E2E tests run INSIDE the test-runner container** via the existing `test-environment` skill (unchanged). This skill delegates to it for `--mode e2e` and the E2E portion of `--mode all`.

## Usage

```bash
# Run all .NET unit/integration tests (fast, no E2E)
bash .claude/skills/run-test-suite/execute.sh --mode unit

# Run a filtered subset of .NET tests (dotnet test --filter)
bash .claude/skills/run-test-suite/execute.sh --mode unit --filter VettingService

# Run verbose for debugging
bash .claude/skills/run-test-suite/execute.sh --mode unit --verbose

# Run Playwright E2E tests only (delegates to test-environment skill)
bash .claude/skills/run-test-suite/execute.sh --mode e2e

# Run everything (.NET + E2E)
bash .claude/skills/run-test-suite/execute.sh --mode all
```

## Options

| Option | Description |
|---|---|
| `--mode unit` | Run .NET unit/integration tests only (fastest, skips E2E) |
| `--mode e2e` | Run Playwright E2E tests only (delegates to test-environment skill) |
| `--mode all` | Run both .NET and E2E tests |
| `--verbose` | Detailed dotnet test output (verbosity=detailed) |
| `--filter PATTERN` | dotnet test `--filter` pattern (unit mode only) |

## What The Skill Is NOT

- ❌ **Does not write tests.** It only runs them. For writing tests, see `docs/standards-processes/testing/TEST-CREATION-GUIDE.md`.
- ❌ **Does not manage dev containers.** Use `restart-dev-containers` for that.
- ❌ **Does not manage E2E test containers directly.** Delegates that to `test-environment`.

## Safety Nets (Important)

The skill has three defensive checks that catch common silent failures:

1. **Bash arrays over `eval`** — Earlier versions of the inventory skill built a command as a string and ran it via `eval`. The `--logger "console;verbosity=normal"` argument got split on the semicolon, so `dotnet test` ran without the logger AND a stray `verbosity=normal` shell assignment set `$?` to 0. Tests reported PASSED even when the build was broken. The bash-array form here prevents that.

2. **Zero-counter detection** — If `dotnet test` exits 0 but every counter (passed, failed, skipped, total) is 0, the skill forces FAIL. This catches silent test-discovery failures (e.g., a broken regex in the output parser, or a `--filter` that matches nothing, which is almost always a typo).

3. **Compile-error detection** — If the output contains `error CS\d+` anywhere, the skill forces FAIL. `dotnet test` sometimes exits 0 on compile errors in certain edge cases; this catches those.

**If you "improve" the skill by converting the bash arrays back to a string + eval, you will re-introduce the false-PASSED bug.** Don't.

## Related Skills

- **`test-environment`** — E2E container management and Playwright execution. `run-test-suite --mode e2e` delegates here.
- **`restart-test-containers`** — Container restart (called internally by `test-environment`).
- **`restart-dev-containers`** — For dev environment, not testing.

## Exit Codes

- `0` — All requested test suites passed
- `1` — One or more test suites failed, or the skill itself errored

## Output

Pass/fail summary for each of the three .NET test projects, aggregated totals, and a structured `=== SKILL_RESULT ===` JSON block at the end for programmatic parsing.

---

**Maintained by**: Test Team
**Ported from**: `inventory-purchasing-workflow` (sibling repo)
