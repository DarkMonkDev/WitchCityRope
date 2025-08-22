# CI/CD Setup Guide

This guide provides detailed information about the CI/CD setup for the WitchCityRope project.

## Overview

The project uses a multi-platform CI/CD approach to ensure maximum compatibility and reliability:

1. **GitHub Actions** - Primary CI/CD platform
2. **Azure DevOps** - Enterprise-grade pipeline with advanced reporting
3. **GitLab CI** - Docker-based pipeline with container support
4. **Pre-commit Hooks** - Local validation before commits

## GitHub Actions

### Configuration

The GitHub Actions workflow is defined in `.github/workflows/test.yml`.

### Features

1. **Automatic Triggers**
   - Push to main/develop branches
   - Pull requests to main/develop
   - Manual workflow dispatch

2. **Build Matrix**
   - OS: Ubuntu Latest
   - .NET Version: 9.0.x
   - Database: SQL Server 2022

3. **Jobs**
   - **Test and Coverage**: Unit tests with coverage reporting
   - **Integration Tests**: Tests with real SQL Server instance
   - **Code Quality**: Format checking and static analysis
   - **Build Status**: Final status check

4. **Caching**
   - NuGet packages cached for faster builds
   - Cache key based on project files hash

5. **Reporting**
   - Test results in TRX format
   - Coverage reports to Codecov
   - PR comments with coverage details
   - Build status badges

### Secrets Required

Configure these secrets in your GitHub repository:
- `CODECOV_TOKEN` - For coverage reporting (optional but recommended)

## Azure DevOps

### Setup Instructions

1. Create a new project in Azure DevOps
2. Import the repository
3. Create a new pipeline:
   - Select "Existing Azure Pipelines YAML file"
   - Choose `/azure-pipelines.yml`
4. Save and run

### Pipeline Stages

1. **Build Stage**
   - Restore, build, and unit test
   - Publish test results and coverage

2. **Integration Tests Stage**
   - Spin up SQL Server container
   - Run integration tests
   - Publish results

3. **Code Quality Stage**
   - Format validation
   - Static code analysis

4. **Package Stage** (main branch only)
   - Create deployment artifacts
   - Package Web and API projects

### Service Connections

No special service connections required for basic functionality.

## GitLab CI

### Configuration

The GitLab CI pipeline is defined in `.gitlab-ci.yml`.

### Features

1. **Docker Support**
   - Uses official .NET SDK images
   - Docker-in-Docker for container builds

2. **Parallel Execution**
   - Unit tests and integration tests run in parallel
   - Code quality checks in separate jobs

3. **GitLab Pages**
   - Coverage reports published to GitLab Pages
   - Accessible at `https://yourusername.gitlab.io/witchcityrope`

4. **Container Registry**
   - Automatic Docker image builds
   - Tagged with commit SHA and latest

### Variables to Configure

In GitLab CI/CD settings:
- `CI_REGISTRY_USER` - Docker registry username
- `CI_REGISTRY_PASSWORD` - Docker registry password

## Pre-commit Hooks

### Installation

```bash
# Install pre-commit framework
pip install pre-commit

# Install hooks in your repository
pre-commit install

# Install commit-msg hook (optional)
pre-commit install --hook-type commit-msg

# Update hooks to latest versions
pre-commit autoupdate
```

### Manual Usage

```bash
# Run all hooks on all files
pre-commit run --all-files

# Run specific hook
pre-commit run dotnet-format --all-files

# Skip hooks temporarily
git commit --no-verify
```

### Hooks Overview

1. **dotnet-format** - Formats C# code
2. **build-validation** - Ensures solution builds
3. **run-unit-tests** - Runs unit tests before commit
4. **check-test-coverage** - Checks coverage before push
5. **check-nuget-vulnerabilities** - Scans for vulnerable packages
6. **verify-no-secrets** - Prevents committing secrets

## Local Testing

### Running CI/CD Locally

You can simulate CI/CD pipelines locally using act (GitHub Actions) or gitlab-runner.

#### GitHub Actions with act

```bash
# Install act
# macOS: brew install act
# Windows: choco install act-cli
# Linux: See https://github.com/nektos/act

# List available workflows
act -l

# Run the test workflow
act -j test

# Run with specific event
act pull_request
```

#### GitLab CI with gitlab-runner

```bash
# Install gitlab-runner
# See: https://docs.gitlab.com/runner/install/

# Execute pipeline locally
gitlab-runner exec docker unit-tests
```

## Troubleshooting

### Common Issues

1. **Coverage Not Meeting Threshold**
   - Current threshold: 60%
   - Add more tests or adjust threshold in pipeline files

2. **Integration Tests Failing**
   - Ensure SQL Server connection string is correct
   - Check if migrations are up to date
   - Verify test database permissions

3. **Pre-commit Hooks Not Running**
   - Ensure hooks are installed: `pre-commit install`
   - Check Python and pip are installed
   - Verify .git/hooks directory contains scripts

4. **Docker Build Failures**
   - Ensure Docker daemon is running
   - Check Dockerfile syntax
   - Verify base image availability

### Performance Optimization

1. **Cache Usage**
   - NuGet packages are cached
   - Consider caching node_modules if adding frontend build

2. **Parallel Execution**
   - Tests run in parallel where possible
   - Integration tests isolated from unit tests

3. **Conditional Steps**
   - Packaging only on main branch
   - Coverage only on test jobs

## Best Practices

1. **Branch Protection**
   - Require CI/CD passes before merge
   - Enforce linear history
   - Require up-to-date branches

2. **Secret Management**
   - Never commit secrets
   - Use repository/organization secrets
   - Rotate credentials regularly

3. **Test Organization**
   - Keep unit tests fast (<1 second each)
   - Mock external dependencies
   - Use test categories/traits

4. **Coverage Goals**
   - Aim for 80%+ coverage
   - Focus on business logic
   - Don't test framework code

## Monitoring

### Build Status

Monitor build status through:
- GitHub Actions tab
- Azure DevOps Pipelines
- GitLab CI/CD Pipelines
- README badges

### Metrics to Track

1. **Build Time**
   - Target: <5 minutes for PR builds
   - Target: <10 minutes for full pipeline

2. **Test Execution Time**
   - Unit tests: <30 seconds
   - Integration tests: <2 minutes

3. **Coverage Trends**
   - Monitor coverage over time
   - Identify areas needing tests

4. **Failure Rate**
   - Track flaky tests
   - Monitor infrastructure issues

## Maintenance

### Regular Tasks

1. **Weekly**
   - Review failed builds
   - Update pre-commit hooks
   - Check for security alerts

2. **Monthly**
   - Update CI/CD tool versions
   - Review and optimize slow tests
   - Clean up old artifacts

3. **Quarterly**
   - Audit secret usage
   - Review coverage reports
   - Update documentation

## Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure Pipelines Documentation](https://docs.microsoft.com/en-us/azure/devops/pipelines)
- [GitLab CI Documentation](https://docs.gitlab.com/ee/ci/)
- [pre-commit Documentation](https://pre-commit.com/)