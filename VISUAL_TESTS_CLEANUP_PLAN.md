# Visual Tests Cleanup Plan
<!-- Created: 2025-08-04 -->
<!-- Purpose: Plan for cleaning up visual-tests directory in root -->

## Summary
Found a `visual-tests` directory in the root containing 2 Playwright scripts that appear to be an older/duplicate visual regression testing setup. The project already has visual regression tests in the proper location: `/tests/playwright/visual-regression/`.

## Files Found
```
/visual-tests/
├── playwright.config.js    # Playwright config for visual tests
└── witch-city-rope.spec.js # Visual regression test suite
```

## Analysis

### 1. **playwright.config.js**
- Configured for visual regression testing
- Uses different port (5652) than main tests
- Has visual comparison options (threshold, animations)
- Tests multiple browsers and viewports

### 2. **witch-city-rope.spec.js**
- Comprehensive visual test suite (301 lines)
- Tests core pages, authenticated pages, admin pages
- Includes responsive, dark theme, and accessibility tests
- References `../tools/ui-test-scenarios.js` which EXISTS
- Uses outdated test credentials and selectors

### UPDATE: Related UI Monitoring System Found
The visual-tests directory appears to be part of a larger UI monitoring system that includes:
- `/tools/ui-monitor.js` - UI monitoring script
- `/tools/ui-test-scenarios.js` - Test scenarios definition
- `/tools/Start-UIMonitoring.ps1` / `start-ui-monitoring.sh` - Monitoring launchers
- Referenced in MCP documentation as a visual verification system

### 3. **Already Have Visual Tests**
The official Playwright test suite already has visual regression tests at:
- `/tests/playwright/visual-regression/`
- Contains actual screenshots and test specs
- Properly integrated with main test suite

## Documentation References
These files are referenced in:
- `/docs/standards-processes/MCP/MCP_VISUAL_VERIFICATION_SETUP.md`
- `/docs/standards-processes/MCP/MCP_QUICK_REFERENCE.md`
- `/docs/history/PROJECT_COMPLETION_SUMMARY.md`
- `/docs/functional-areas/user-dashboard/mcp-visual-testing-guide.md`
- `/docs/functional-areas/user-dashboard/testing-strategy.md`

## Recommendation
**Archive these files** because:
1. Visual regression tests already exist in proper location (`/tests/playwright/visual-regression/`)
2. This appears to be part of a separate UI monitoring system (MCP-based)
3. Uses outdated selectors and test credentials
4. Not integrated with main Playwright test suite
5. The UI monitoring system in `/tools/` appears to be a different approach that may not be actively used

However, since this is part of a larger UI monitoring system documented in MCP guides, we should verify if this system is still needed before archiving.

## Action Plan
1. Create archive directory: `/session-work/2025-08-04/archived-visual-tests/`
2. Move both files to archive
3. Add README explaining why they were archived
4. Update file registry
5. Note: Documentation references can remain as they describe the concept

## Questions
1. Were these files part of an MCP (Model Context Protocol) visual testing setup that's still needed?
2. Is the UI monitoring system in `/tools/` (ui-monitor.js, ui-test-scenarios.js) still actively used?
3. Should we update the MCP documentation to point to the actual visual regression tests in `/tests/playwright/visual-regression/`?
4. Should we keep the visual-tests directory as part of the UI monitoring system, or is it safe to archive?