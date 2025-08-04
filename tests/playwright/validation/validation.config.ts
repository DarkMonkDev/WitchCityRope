/**
 * Validation Test Configuration
 * Specific configuration for validation test suite
 */

import { PlaywrightTestConfig } from '@playwright/test';

const validationConfig: PlaywrightTestConfig = {
  testDir: '.',
  timeout: 60000, // 60 seconds per test
  expect: {
    timeout: 10000 // 10 seconds for assertions
  },
  fullyParallel: false, // Run validation tests sequentially to avoid conflicts
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [
    ['html', { outputFolder: '../../../test-results/validation-html-report' }],
    ['json', { outputFile: '../../../test-results/validation-results.json' }],
    ['list']
  ],
  use: {
    baseURL: process.env.BASE_URL || 'http://localhost:5651',
    trace: 'on-first-retry',
    screenshot: {
      mode: 'only-on-failure',
      fullPage: true
    },
    video: process.env.CI ? 'retain-on-failure' : 'off'
  },
  
  projects: [
    {
      name: 'validation-chrome',
      use: {
        browserName: 'chromium',
        viewport: { width: 1280, height: 800 }
      }
    }
  ],

  outputDir: '../../../test-results/validation-tests'
};

export default validationConfig;

/**
 * Validation test patterns and selectors
 */
export const validationPatterns = {
  // WCR Component Selectors
  wcrComponents: {
    inputs: 'wcr-input-text, wcr-input-email, wcr-input-password, [class*="wcr-input"]',
    validationMessages: '.wcr-field-validation, wcr-validation-message',
    validationSummary: '.wcr-validation-summary, wcr-validation-summary',
    passwordRequirements: '.wcr-password-requirements, .wcr-password-requirement',
    errorStyling: '.wcr-input-error, .wcr-validation-error'
  },

  // Standard Form Selectors
  standardSelectors: {
    inputs: 'input[type="text"], input[type="email"], input[type="password"], input[type="number"]',
    submitButtons: 'button[type="submit"], input[type="submit"], .sign-in-btn, .register-btn',
    validationErrors: '.text-danger, .field-validation-error, .validation-message',
    validationSummary: '.validation-summary-errors, .wcr-validation-summary'
  },

  // Validation Messages
  commonMessages: {
    required: /required|cannot be empty|must be provided/i,
    email: /valid email|email format|invalid email/i,
    password: /password must|at least \d+ characters|uppercase|lowercase|number|special/i,
    mismatch: /passwords? do not match|must match|confirmation/i,
    range: /must be between|greater than|less than|minimum|maximum/i
  }
};

/**
 * Test data for validation scenarios
 */
export const validationTestData = {
  invalid: {
    email: ['notanemail', 'missing@', '@example.com', 'spaces in@email.com'],
    password: ['short', '12345678', 'NoNumbers!', 'no-uppercase-123!', 'NO-LOWERCASE-123!'],
    phone: ['123', 'abcdefghij', '123-456-789', '(123) 456-789'],
    age: ['-1', '0', '150', 'abc', '17'] // Assuming 18+ requirement
  },
  
  valid: {
    email: 'test@example.com',
    password: 'ValidPass123!',
    confirmPassword: 'ValidPass123!',
    phone: '(123) 456-7890',
    age: '25',
    sceneName: 'TestUser123'
  },

  edge: {
    email: 'test+tag@subdomain.example.co.uk',
    password: 'P@ssw0rd!123456789012345', // Long but valid
    phone: '(999) 999-9999',
    age: '100'
  }
};

/**
 * Expected validation behavior configurations
 */
export const validationBehavior = {
  realTimeValidation: {
    onBlur: true,
    debounceMs: 300,
    showPasswordStrength: true
  },
  
  submitValidation: {
    scrollToFirstError: true,
    focusFirstError: true,
    showSummary: true
  },

  styling: {
    errorClass: 'wcr-input-error',
    validClass: 'wcr-input-valid',
    touchedClass: 'wcr-input-touched'
  }
};

/**
 * List of validation test files
 */
export const validationTests = [
  'all-migrated-forms.spec.ts',
  'validation-components.spec.ts',
  'validation-diagnostics.spec.ts',
  'performance-validation.spec.ts',
  'standardization-tests.spec.ts',
  'public-forms-validation.spec.ts',
  'field-specific-validation.spec.ts',
  'login-validation.spec.ts',
  'validation-suite.spec.ts',
  'input-validation.spec.ts'
];