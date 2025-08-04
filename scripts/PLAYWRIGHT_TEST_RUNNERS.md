# Playwright Test Runner Scripts

This directory contains three specialized test runner scripts for Playwright tests:

## 1. run-playwright-tests.sh

The main test runner with comprehensive options for running Playwright tests.

### Usage
```bash
./scripts/run-playwright-tests.sh [OPTIONS]
```

### Options
- `-s, --suite <name>` - Run specific test suite (auth, events, admin, all)
- `-b, --browser <name>` - Browser to use (chromium, firefox, webkit, all)
- `-h, --headed` - Run in headed mode (show browser)
- `-u, --url <url>` - Base URL (default: http://localhost:5651)
- `-w, --workers <num>` - Number of parallel workers
- `-r, --retries <num>` - Number of retries for failed tests
- `--no-report` - Disable HTML report generation
- `--help` - Display help message

### Examples
```bash
# Run auth tests in headed mode
./scripts/run-playwright-tests.sh -s auth -h

# Run events tests in Firefox
./scripts/run-playwright-tests.sh -s events -b firefox

# Run all tests in all browsers with 4 workers
./scripts/run-playwright-tests.sh -b all --workers 4

# Run admin tests without report
./scripts/run-playwright-tests.sh -s admin --no-report
```

## 2. run-parallel-migration.sh

Runs both Puppeteer and Playwright tests in parallel to compare results during migration.

### Usage
```bash
./scripts/run-parallel-migration.sh [OPTIONS]
```

### Options
- `-s, --suite <name>` - Run specific test suite (auth, events, admin, all)
- `-h, --headed` - Run in headed mode (show browser)
- `-u, --url <url>` - Base URL (default: http://localhost:5651)
- `--puppeteer-only` - Run only Puppeteer tests
- `--playwright-only` - Run only Playwright tests
- `--no-comparison` - Skip comparison report
- `--help` - Display help message

### Examples
```bash
# Compare auth tests in both frameworks
./scripts/run-parallel-migration.sh -s auth

# Compare events tests in headed mode
./scripts/run-parallel-migration.sh -h --suite events

# Run only Puppeteer tests
./scripts/run-parallel-migration.sh --puppeteer-only
```

### Output
- Saves results to `test-results/migration-comparison/[timestamp]/`
- Generates a detailed comparison report
- Includes test outputs, summaries, and migration notes

## 3. playwright-ci-local.sh

Simulates CI environment locally for testing, with comprehensive pre-flight checks and artifact collection.

### Usage
```bash
./scripts/playwright-ci-local.sh [OPTIONS]
```

### Options
- `-s, --suite <name>` - Run specific test suite (auth, events, admin, all)
- `-b, --browser <name>` - Browser to use (chromium, firefox, webkit, all)
- `--skip-deps` - Skip dependency installation
- `--skip-build` - Skip application build
- `--keep-artifacts` - Keep all test artifacts (screenshots, videos, traces)
- `--parallel <num>` - Number of parallel workers (default: 1 for CI)
- `--docker` - Run tests against Docker environment
- `--help` - Display help message

### Examples
```bash
# Run all tests in CI mode
./scripts/playwright-ci-local.sh

# Run auth tests in Firefox
./scripts/playwright-ci-local.sh -s auth -b firefox

# Run in Docker with 4 workers
./scripts/playwright-ci-local.sh --docker --parallel 4
```

### Features
- System requirements check
- CI environment setup
- Dependency installation
- Application startup verification
- Comprehensive artifact collection
- Multiple report formats (JSON, JUnit XML, HTML)
- Detailed execution logs

### Output
- Saves results to `test-results/ci-simulation/[timestamp]/`
- Collects screenshots, videos, and traces
- Generates multiple report formats
- Creates environment information file

## Prerequisites

All scripts require:
1. Node.js and npm installed
2. Application running (or use Docker option)
3. Playwright dependencies (`npm install`)

## Tips

1. **First-time setup**: The scripts will automatically install Playwright if not present
2. **Headed mode**: Use `-h` flag to see browser interactions (useful for debugging)
3. **Parallel execution**: Increase workers for faster execution (be mindful of system resources)
4. **CI simulation**: Use `playwright-ci-local.sh` before pushing to ensure tests pass in CI
5. **Migration**: Use `run-parallel-migration.sh` to ensure Playwright tests match Puppeteer behavior

## Troubleshooting

1. **Application not running**: Ensure the application is accessible at the base URL
2. **Permission denied**: Make sure scripts are executable (`chmod +x`)
3. **Browser not found**: Run `npx playwright install` to install browsers
4. **Out of memory**: Reduce parallel workers or run tests in smaller suites