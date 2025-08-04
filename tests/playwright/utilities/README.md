# Playwright Utility Scripts

This directory contains utility scripts and tools that support Playwright testing but are not part of the main test suite.

## Scripts

### analyze-login-logs.js
- **Purpose**: Analyzes login test results from monitoring runs
- **Usage**: `node analyze-login-logs.js`
- **Description**: Created during login functionality debugging. Parses test results and provides analysis of login success/failure patterns.

### test-homepage.js
- **Purpose**: Quick smoke test for homepage functionality
- **Usage**: `node test-homepage.js`
- **Description**: Simple Playwright script that loads the homepage and verifies basic elements are present. Useful for quick health checks.

## Guidelines

- Scripts in this directory are utilities, not part of the main test suite
- They should be documented with clear usage instructions
- Keep scripts focused on single purposes
- Consider converting useful utilities into proper test specs if they're used frequently

## Note

These scripts were moved from the root directory during cleanup on 2025-08-04 to better organize the project structure.