# Playwright Diagnostic Tests

This directory contains diagnostic and utility tests that help debug issues with the WitchCityRope application. These tests are focused on identifying problems rather than asserting specific behaviors.

## Test Files

### diagnose-event-form.test.ts
Analyzes the event form structure, validates form fields, and attempts submission to identify any issues with:
- Form structure and Blazor bindings
- Required fields and validation setup
- Submit button functionality
- Form submission process

### check-console-errors.test.ts
Monitors console output across multiple pages to capture:
- JavaScript errors
- Console warnings
- Page errors
- Failed resource requests (404s)

### check-console-warnings.test.ts
Specifically focuses on:
- Console warnings
- Deprecation notices
- Blazor initialization issues
- Script loading problems

### check-form-inputs.test.ts
Analyzes form structure and inputs, particularly useful for:
- Identifying form field attributes
- Checking validation setup
- Understanding form patterns (ASP.NET vs Blazor)
- Verifying authentication forms

### check-script-loading.test.ts
Monitors script loading and jQuery setup:
- Tracks all script requests
- Identifies failed script loads
- Verifies jQuery and validation plugin loading
- Checks for console errors related to scripts

### visual-screenshot.test.ts
Captures screenshots and analyzes styling:
- Home page visual capture
- Login page screenshot
- Event form datetime picker screenshot
- CSS and styling analysis

## Running Diagnostic Tests

Run all diagnostic tests:
```bash
npm test -- tests/playwright/diagnostics/
```

Run a specific diagnostic test:
```bash
npm test -- tests/playwright/diagnostics/check-console-errors.test.ts
```

Run with visible browser (useful for debugging):
```bash
npm test -- tests/playwright/diagnostics/ --headed
```

## Test Helpers

The `test-helpers.ts` file provides common utilities:
- Login functionality
- Configuration constants
- Logging with color coding
- Wait/delay helpers

## Output

- Console output provides detailed diagnostic information
- Screenshots are saved to the `screenshots/` directory
- Failed assertions indicate critical issues found

## When to Use These Tests

Use these diagnostic tests when:
- Debugging new issues in the application
- After major code changes
- When visual regression is suspected
- To verify proper script loading
- To check for console errors/warnings before deployment