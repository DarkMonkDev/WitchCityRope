# Validation Tests

This directory contains Playwright tests for form validation in the WitchCityRope application. These tests have been converted from the original Puppeteer tests and focus on testing WCR validation components and form behavior.

## Test Files

### Core Validation Tests

#### 1. `all-migrated-forms.spec.ts`
Comprehensive test suite that validates all migrated forms in the application:
- **Identity Forms**: Login, Register, Password Management, Profile Management
- **Application Forms**: Event Registration, Vetting, Incident Reporting, Event Management
- Tests empty form submission, field-specific validation, and successful form submission
- Generates detailed test reports with screenshots

#### 2. `validation-diagnostics.spec.ts`
Diagnostic tool that analyzes the validation implementation status across all forms:
- Checks for presence of WCR validation components
- Tests validation behavior (empty form, real-time validation)
- Categorizes forms as: fully migrated, partially migrated, or missing validation
- Generates an HTML diagnostic report with detailed analysis

#### 3. `validation-components.spec.ts`
Tests WCR validation components in isolation using a dedicated test page:
- Empty form validation
- Individual field validation (email, password, number, checkbox)
- Real-time validation behavior
- CSS validation styling
- Async validation (email uniqueness)
- Keyboard navigation and accessibility

### Specialized Validation Tests

#### 4. `performance-validation.spec.ts`
Tests the performance characteristics of validation components:
- Page load performance with validation components
- Form interaction latency measurements
- Memory usage profiling
- Comparison of original vs standardized forms
- Blazor circuit latency measurements

#### 5. `standardization-tests.spec.ts`
Tests the standardized validation implementation across different page types:
- Identity pages (Login, Register, Forgot Password)
- Profile management pages
- Event management forms
- User management interfaces
- Form component standardization

#### 6. `public-forms-validation.spec.ts`
Tests validation on publicly accessible forms:
- Contact form validation
- Newsletter subscription validation
- Public incident report form
- Vetting application form
- Tests both client-side and server-side validation

#### 7. `field-specific-validation.spec.ts`
Tests validation for specific field types:
- Email field validation (format, uniqueness)
- Password field validation (strength, requirements, matching)
- Phone number format validation
- Date/DateTime validation
- Number/range validation
- Text length validation

#### 8. `login-validation.spec.ts`
Focused tests for login page validation:
- Empty form submission
- Invalid email format
- Missing password
- Invalid credentials
- Password visibility toggle
- Scene name login support
- Remember me functionality
- Form accessibility
- Real-time validation

#### 9. `validation-suite.spec.ts`
Comprehensive suite testing all identity page validations:
- Forgot Password validation
- Reset Password validation
- Change Password validation
- Manage Email validation
- Manage Profile validation
- Delete Personal Data validation
- Login with 2FA validation

#### 10. `input-validation.spec.ts`
Tests validation for different input types:
- Text input validation (required, min/max length, pattern)
- Email input validation (format, domain)
- Password input validation (strength, requirements, visibility)
- Number input validation (range, step, integer-only)
- Checkbox validation (required, groups)
- Select/dropdown validation
- Textarea validation (length, max length)

## Running the Tests

### Run all validation tests:
```bash
npx playwright test validation/
```

### Run specific test file:
```bash
npx playwright test validation/all-migrated-forms.spec.ts
npx playwright test validation/validation-diagnostics.spec.ts
npx playwright test validation/validation-components.spec.ts
```

### Run with UI mode for debugging:
```bash
npx playwright test validation/ --ui
```

### Run with headed browser:
```bash
npx playwright test validation/ --headed
```

## Test Output

- **Screenshots**: Saved to `test-results/validation-tests/` or `test-results/validation-diagnostics/`
- **Test Reports**: JSON and HTML reports generated in the screenshot directories
- **Console Output**: Detailed progress and results logged during test execution

## Key Features

### WCR Validation Components
The tests validate the custom WCR (WitchCityRope) validation components:
- `wcr-input-text`, `wcr-input-email`, `wcr-input-password`
- `wcr-field-validation`, `wcr-validation-message`
- `wcr-validation-summary`
- `wcr-password-requirements`

### Validation Behaviors Tested
- Required field validation
- Format validation (email, phone, etc.)
- Password strength requirements
- Real-time validation on blur
- Validation summary display
- Error styling (CSS classes)
- Clearing validation on valid input

### Authentication-Required Forms
Some forms require authentication. The tests handle this by:
- Using the `AuthHelpers.login()` helper function
- Maintaining session state across test steps
- Handling redirects to login page

## Debugging Tips

1. **Use Page Screenshots**: Tests capture screenshots at key points, especially on failures
2. **Check HTML Reports**: The diagnostic tests generate detailed HTML reports
3. **Use Playwright Inspector**: Run with `--debug` flag to step through tests
4. **Check Console Logs**: Tests include detailed console logging of progress

## Notes

- Tests use the test configuration from `../helpers/test.config.ts`
- Blazor initialization is handled by `BlazorHelpers.waitForBlazorReady()`
- Tests are designed to be resilient to timing issues with appropriate waits
- Skipped tests indicate forms that require special conditions (e.g., 2FA setup)