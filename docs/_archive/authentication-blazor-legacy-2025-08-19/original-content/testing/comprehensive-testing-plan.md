# Comprehensive Testing Plan - Authentication Flow

## Overview
This document outlines the testing strategy for the minimal authentication flow, validating our ability to test the new React architecture with TanStack Query, Zustand, and React Router v7.

## Testing Pyramid Strategy

### Level 1: Unit Tests (Fast, Isolated)
**Tools**: Vitest, React Testing Library, MSW
**Focus**: Individual components and functions in isolation

### Level 2: Integration Tests (Medium Speed, Component Interaction)
**Tools**: Vitest, React Testing Library, MSW
**Focus**: Multiple components working together

### Level 3: E2E Tests (Slower, Full User Journey)
**Tools**: Playwright
**Focus**: Complete user workflows through the browser

## Test Implementation Plan

### Phase 1: Unit Tests

#### 1.1 Zustand Store Tests
**File**: `/apps/web/src/stores/__tests__/authStore.test.ts` (ALREADY EXISTS)
- ✅ Test login action updates state
- ✅ Test logout clears state
- ✅ Test permission calculations
- ✅ Test persistence to sessionStorage

#### 1.2 TanStack Query Hook Tests
**File**: `/apps/web/src/features/auth/api/__tests__/mutations.test.tsx`
- Test useLogin mutation success/failure
- Test useLogout mutation
- Test loading states
- Test error handling
- Mock API with MSW

**Implementation Pattern**:
```typescript
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useLogin } from '../mutations'
import { server } from '@/test/mocks/server'

describe('useLogin', () => {
  it('should login successfully', async () => {
    const { result } = renderHook(() => useLogin(), {
      wrapper: createWrapper()
    })
    
    act(() => {
      result.current.mutate({
        email: 'admin@witchcityrope.com',
        password: 'Test123!'
      })
    })
    
    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })
  })
})
```

#### 1.3 Component Tests
**File**: `/apps/web/src/pages/__tests__/LoginPage.test.tsx`
- Test form renders correctly
- Test validation errors display
- Test form submission
- Test loading states
- Test error message display

### Phase 2: Integration Tests

#### 2.1 Auth Flow Integration
**File**: `/apps/web/src/test/integration/auth-flow.test.tsx`
- Test complete login flow (form → API → store → navigation)
- Test logout flow
- Test protected route redirection
- Test session persistence

**Implementation Pattern**:
```typescript
import { render, screen, waitFor, userEvent } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

describe('Authentication Flow', () => {
  it('should complete login and navigate to dashboard', async () => {
    const router = createMemoryRouter(routes, {
      initialEntries: ['/login']
    })
    
    render(
      <QueryClientProvider client={queryClient}>
        <RouterProvider router={router} />
      </QueryClientProvider>
    )
    
    // Fill form
    await userEvent.type(screen.getByLabelText(/email/i), 'admin@witchcityrope.com')
    await userEvent.type(screen.getByLabelText(/password/i), 'Test123!')
    
    // Submit
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }))
    
    // Verify navigation
    await waitFor(() => {
      expect(router.state.location.pathname).toBe('/dashboard')
    })
  })
})
```

#### 2.2 Protected Route Integration
**File**: `/apps/web/src/test/integration/protected-routes.test.tsx`
- Test unauthenticated access redirects to login
- Test authenticated access allows entry
- Test logout redirects to login
- Test returnTo parameter works

### Phase 3: E2E Tests with Playwright

#### 3.1 Setup Playwright
**File**: `/playwright.config.ts`
```typescript
import { defineConfig } from '@playwright/test'

export default defineConfig({
  testDir: './tests/e2e',
  use: {
    baseURL: 'http://localhost:5173',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'firefox', use: { ...devices['Desktop Firefox'] } },
    { name: 'webkit', use: { ...devices['Desktop Safari'] } },
  ],
})
```

#### 3.2 Auth E2E Tests
**File**: `/tests/e2e/auth.spec.ts`
```typescript
import { test, expect } from '@playwright/test'

test.describe('Authentication', () => {
  test('should login successfully', async ({ page }) => {
    // Navigate to login
    await page.goto('/login')
    
    // Fill form
    await page.fill('input[name="email"]', 'admin@witchcityrope.com')
    await page.fill('input[name="password"]', 'Test123!')
    
    // Submit
    await page.click('button[type="submit"]')
    
    // Verify navigation
    await expect(page).toHaveURL('/dashboard')
    
    // Verify user info displays
    await expect(page.locator('text=Welcome')).toBeVisible()
  })
  
  test('should protect dashboard route', async ({ page }) => {
    // Try to access protected route
    await page.goto('/dashboard')
    
    // Should redirect to login
    await expect(page).toHaveURL('/login?returnTo=%2Fdashboard')
  })
  
  test('should handle invalid credentials', async ({ page }) => {
    await page.goto('/login')
    
    await page.fill('input[name="email"]', 'invalid@test.com')
    await page.fill('input[name="password"]', 'wrong')
    
    await page.click('button[type="submit"]')
    
    // Should show error
    await expect(page.locator('text=Invalid email or password')).toBeVisible()
  })
})
```

## Test Data Management

### Mock Users
```typescript
export const testUsers = {
  admin: {
    email: 'admin@witchcityrope.com',
    password: 'Test123!',
    user: {
      id: '1',
      email: 'admin@witchcityrope.com',
      firstName: 'Admin',
      lastName: 'User',
      roles: ['Administrator']
    }
  },
  member: {
    email: 'member@witchcityrope.com',
    password: 'Test123!',
    user: {
      id: '2',
      email: 'member@witchcityrope.com',
      firstName: 'Member',
      lastName: 'User',
      roles: ['GeneralMember']
    }
  }
}
```

### MSW Handlers
```typescript
import { http, HttpResponse } from 'msw'

export const authHandlers = [
  http.post('/api/auth/login', async ({ request }) => {
    const body = await request.json()
    const user = testUsers[body.email]
    
    if (user && body.password === user.password) {
      return HttpResponse.json({ user: user.user })
    }
    
    return new HttpResponse('Invalid credentials', { status: 401 })
  }),
  
  http.post('/api/auth/logout', () => {
    return new HttpResponse(null, { status: 200 })
  })
]
```

## Test Commands

```json
{
  "scripts": {
    "test": "vitest",
    "test:unit": "vitest run --dir src",
    "test:integration": "vitest run --dir src/test/integration",
    "test:e2e": "playwright test",
    "test:e2e:ui": "playwright test --ui",
    "test:coverage": "vitest run --coverage",
    "test:all": "npm run test:unit && npm run test:integration && npm run test:e2e"
  }
}
```

## Coverage Requirements

### Minimum Coverage Targets
- **Statements**: 80%
- **Branches**: 75%
- **Functions**: 80%
- **Lines**: 80%

### Critical Path Coverage
- **Login flow**: 100%
- **Logout flow**: 100%
- **Protected routes**: 100%
- **Error handling**: 90%

## CI/CD Integration

### GitHub Actions Workflow
```yaml
name: Test

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
      - run: npm ci
      - run: npm run test:unit
      - run: npm run test:integration
      - run: npx playwright install
      - run: npm run test:e2e
      - run: npm run test:coverage
      - uses: codecov/codecov-action@v3
```

## Testing Best Practices

### 1. Test Structure
- **Arrange**: Set up test data and environment
- **Act**: Perform the action being tested
- **Assert**: Verify the expected outcome

### 2. Test Isolation
- Each test should be independent
- Clean up after each test
- Use fresh test data

### 3. Test Naming
- Use descriptive names
- Follow pattern: "should [expected behavior] when [condition]"

### 4. Mocking Strategy
- Mock external dependencies (API, localStorage)
- Use MSW for API mocking
- Keep mocks close to reality

### 5. Performance
- Unit tests: < 100ms each
- Integration tests: < 500ms each
- E2E tests: < 5s each

## Validation Checklist

- [ ] Unit tests run successfully with `npm test`
- [ ] Integration tests validate component interactions
- [ ] E2E tests work with real browser
- [ ] MSW mocks API calls properly
- [ ] Coverage meets minimum requirements
- [ ] Tests run in CI/CD pipeline
- [ ] Test patterns documented for reuse

## Next Steps

1. Implement unit tests for auth components
2. Create integration tests for full auth flow
3. Set up Playwright and write E2E tests
4. Configure coverage reporting
5. Document patterns for other functional areas
6. Create test templates for future features

## References
- [Vitest Documentation](https://vitest.dev/)
- [React Testing Library](https://testing-library.com/react)
- [MSW Documentation](https://mswjs.io/)
- [Playwright Documentation](https://playwright.dev/)
- [Testing Validated Patterns](/docs/guides-setup/testing-patterns-guide.md)