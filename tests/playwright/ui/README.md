# UI Tests

This directory contains tests for user interface components and interactions.

## Test Files

### user-dropdown.spec.ts
- Tests user dropdown menu functionality after login
- Verifies dropdown toggle behavior
- Tests mobile view compatibility
- Checks Blazor interactivity for dropdown components
- Includes diagnostic tests for troubleshooting dropdown issues

### navigation.spec.ts
- Tests navigation between different pages
- Verifies navigation menu items and links
- Tests direct URL navigation
- Checks navigation button functionality
- Validates admin page navigation

### button-interactivity.spec.ts
- Tests button click handling across the application
- Monitors API calls triggered by button clicks
- Verifies Blazor connection for interactive components
- Tests form submission buttons
- Checks for event handler attachments

## Running Tests

```bash
# Run all UI tests
npx playwright test ui/

# Run specific test file
npx playwright test ui/user-dropdown.spec.ts

# Run with UI mode for debugging
npx playwright test ui/ --ui
```

## Common Issues

- **Dropdown not working**: Check if Blazor interactivity is enabled for the component
- **Navigation failures**: Verify authentication state and route permissions
- **Button clicks not registering**: Check Blazor circuit connection status