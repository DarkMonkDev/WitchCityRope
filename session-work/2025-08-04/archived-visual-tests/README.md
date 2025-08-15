# Archived Visual Tests and UI Monitoring System
<!-- Archived: 2025-08-04 -->
<!-- Reason: Superseded by official Playwright visual regression tests -->

## Overview
This directory contains the archived visual tests and UI monitoring system that was previously located in:
- `/visual-tests/` - Visual regression test setup
- `/tools/` - UI monitoring scripts

## Why These Were Archived
1. **Duplicate functionality**: The project already has comprehensive visual regression tests in `/tests/playwright/visual-regression/`
2. **Not integrated**: This system was separate from the main Playwright test suite
3. **Outdated**: Uses old test credentials and selectors
4. **Superseded**: The official test suite provides better coverage and integration

## Archived Files

### From `/visual-tests/`:
- `playwright.config.js` - Playwright configuration for visual tests (port 5652)
- `witch-city-rope.spec.js` - Comprehensive visual test suite (301 lines)

### From `/tools/`:
- `ui-monitor.js` - UI monitoring script that watches for file changes
- `ui-test-scenarios.js` - Test scenario definitions
- `Start-UIMonitoring.ps1` - PowerShell launcher for UI monitoring
- `start-ui-monitoring.sh` - Bash launcher for UI monitoring

## Current Visual Testing Location
All visual regression testing is now handled by the official Playwright test suite:
- **Location**: `/tests/playwright/`
- **Visual tests**: `/tests/playwright/visual-regression/`
- **Example test**: `/tests/playwright/specs/visual/example.visual.spec.ts`
- **Screenshots**: `/tests/playwright/visual-regression/__screenshots__/`

The official suite includes:
- Homepage visual tests (full page, header, mobile, tablet, dark mode)
- Authentication page visual tests
- Admin dashboard visual tests
- Event display visual tests
- Cross-browser testing (Chromium, Firefox, WebKit)
- Color scheme testing (light, dark, high-contrast)

## MCP Documentation
This system was referenced in MCP (Model Context Protocol) documentation:
- `/docs/standards-processes/MCP/MCP_VISUAL_VERIFICATION_SETUP.md`
- `/docs/standards-processes/MCP/MCP_QUICK_REFERENCE.md`

These docs should be updated to reference the actual visual regression tests.

## Note
These files are archived for historical reference. All new visual testing should use the official Playwright test suite structure.