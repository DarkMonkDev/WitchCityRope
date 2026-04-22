---
name: run-test-suite
description: Run WitchCityRope tests. Single source of truth for test execution. Modes -- `--mode unit` (.NET unit/integration via host dotnet test), `--mode react` (React/vitest unit tests via host npx vitest), `--mode e2e` (Playwright via test-environment skill), `--mode all` (everything).
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

**React/vitest tests run ON THE HOST** via `npx vitest run` from `apps/web/`. Vitest's config (`apps/web/vitest.config.ts`) points at `tests/unit/web/**/*.test.{ts,tsx}` and uses jsdom. No containers needed. Added 2026-04-22 after the first attempt to run React unit tests during a vetting admin feature was blocked by `block-manual-test-runs.py` with no skill alternative.

**Prerequisites**:
- `dotnet` 10.0+ on the host PATH (`/home/chad/.dotnet/dotnet` — already installed) — for `--mode unit`
- Docker running (for Testcontainers to spin up postgres) — for `--mode unit`
- `node` + `npx` on the host PATH — for `--mode react`

**E2E tests run INSIDE the test-runner container** via the existing `test-environment` skill (unchanged). This skill delegates to it for `--mode e2e` and the E2E portion of `--mode all`.

## Usage

```bash
# Run all .NET unit/integration tests (fast, no E2E)
bash .claude/skills/run-test-suite/execute.sh --mode unit

# Run a filtered subset of .NET tests (dotnet test --filter)
bash .claude/skills/run-test-suite/execute.sh --mode unit --filter VettingService

# Run all React/vitest unit tests
bash .claude/skills/run-test-suite/execute.sh --mode react

# Run a filtered subset of React tests (vitest positional pattern; matches
# both file paths and test names)
bash .claude/skills/run-test-suite/execute.sh --mode react --filter VettingApplicationsList

# Run verbose for debugging
bash .claude/skills/run-test-suite/execute.sh --mode unit --verbose
bash .claude/skills/run-test-suite/execute.sh --mode react --verbose

# Run Playwright E2E tests only (delegates to test-environment skill)
bash .claude/skills/run-test-suite/execute.sh --mode e2e

# Run everything (.NET + React + E2E)
bash .claude/skills/run-test-suite/execute.sh --mode all
```

## Options

| Option | Description |
|---|---|
| `--mode unit` | Run .NET unit/integration tests only (host dotnet test) |
| `--mode react` | Run React/vitest unit tests only (host npx vitest from apps/web) |
| `--mode e2e` | Run Playwright E2E tests only (delegates to test-environment skill) |
| `--mode all` | Run all three (.NET + React + E2E) |
| `--verbose` | Detailed test output (verbosity=detailed for dotnet, reporter=verbose for vitest) |
| `--filter PATTERN` | unit mode: dotnet `--filter` expression. react mode: vitest positional pattern (matches file paths AND test names). Ignored in e2e mode. |

## What The Skill Is NOT

- ❌ **Does not write tests.** It only runs them. For writing tests, see `docs/standards-processes/testing/TEST-CREATION-GUIDE.md`.
- ❌ **Does not manage dev containers.** Use `restart-dev-containers` for that.
- ❌ **Does not manage E2E test containers directly.** Delegates that to `test-environment`.

## Safety Nets (Important)

The skill has three defensive checks that catch common silent failures, applied to BOTH `--mode unit` (dotnet) and `--mode react` (vitest):

1. **Bash arrays over `eval`** — Earlier versions of the inventory skill built a command as a string and ran it via `eval`. The `--logger "console;verbosity=normal"` argument got split on the semicolon, so `dotnet test` ran without the logger AND a stray `verbosity=normal` shell assignment set `$?` to 0. Tests reported PASSED even when the build was broken. The bash-array form here prevents that. The same array form is used for the vitest invocation.

2. **Zero-counter detection** — If the runner exits 0 but every counter (passed, failed, skipped, total) is 0, the skill forces FAIL. This catches silent test-discovery failures (e.g., a broken regex in the output parser, or a `--filter` that matches nothing, which is almost always a typo).

3. **Compile-error detection** — If the dotnet output contains `error CS\d+` OR the vitest output contains `Transform failed` / `Cannot find module` / `SyntaxError`, the skill forces FAIL. Both runners can exit 0 on certain edge-case compile/transform errors; these catch them.

**If you "improve" the skill by converting the bash arrays back to a string + eval, you will re-introduce the false-PASSED bug.** Don't.

## Related Skills

- **`test-environment`** — E2E container management and Playwright execution. `run-test-suite --mode e2e` delegates here.
- **`restart-test-containers`** — Container restart (called internally by `test-environment`).
- **`restart-dev-containers`** — For dev environment, not testing.

## Exit Codes

- `0` — All requested test suites passed
- `1` — One or more test suites failed, or the skill itself errored

## Output

Pass/fail summary for each runner that was activated (per-project for the .NET projects, single roll-up for vitest, status line for E2E), aggregated totals, and a structured `=== SKILL_RESULT ===` JSON block at the end for programmatic parsing. The JSON includes separate `dotnet`, `react`, and `e2e` sub-objects.

---

**Maintained by**: Test Team
**Ported from**: `inventory-purchasing-workflow` (sibling repo)
