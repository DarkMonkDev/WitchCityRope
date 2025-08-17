# Vertical Slice Home Page Tests

This test suite validates the React + API + PostgreSQL stack proof-of-concept implementation.

## 🎯 Purpose

These tests prove that the **complete technical stack works end-to-end**:
- React frontend can fetch data from API
- API can return data from PostgreSQL (with fallback)  
- Database integration works correctly
- Error handling works at all layers

## 🧪 Test Coverage

### 1. API Unit Tests (`/tests/unit/api/`)
- **File**: `EventsController.test.cs`
- **Purpose**: Tests the API controller with mocked dependencies
- **Proves**: API layer returns correct EventDto structure

### 2. React Component Tests (`/tests/unit/web/`)  
- **File**: `EventsList.test.tsx`
- **Purpose**: Tests React components with mocked fetch
- **Proves**: React layer handles API responses correctly

### 3. E2E Tests (`/tests/e2e/`)
- **File**: `home-page.spec.ts`  
- **Purpose**: Browser automation testing complete stack
- **Proves**: Complete browser → React → API → PostgreSQL flow works

## 🚀 Quick Start

### Prerequisites
```bash
# Start React dev server
cd apps/web && npm run dev

# Start API server
cd apps/api && dotnet run
```

### Run All Tests
```bash
# Automated test runner (recommended)
./tests/run-vertical-slice-tests.sh
```

### Run Individual Test Types
```bash
# API Unit Tests
cd tests/unit/api && dotnet test

# React Component Tests  
cd apps/web && npm test

# E2E Tests (requires servers running)
cd tests/e2e && npm test
```

## 📋 Test Results

When successful, you'll see:
- ✅ **API Unit Tests** - EventsController returns EventDto array
- ✅ **React Component Tests** - EventsList fetches and displays events  
- ✅ **E2E Tests** - Complete React + API + PostgreSQL stack integration

## 🔧 Test Infrastructure

### API Tests (.NET)
- **Framework**: xUnit with FluentAssertions
- **Mocking**: Moq for IEventService
- **Project**: `WitchCityRope.Api.Tests.csproj`

### React Tests (TypeScript)
- **Framework**: Vitest with React Testing Library
- **Mocking**: vi.mock() for fetch and components
- **Config**: `vitest.config.ts`

### E2E Tests (Playwright)
- **Framework**: Playwright with TypeScript
- **Browsers**: Chrome, Firefox, Safari, Mobile
- **Config**: `playwright.config.ts`

## 🎯 Key Test Cases

### API Layer Validation
- Database events return correctly
- Fallback events work when database is empty
- Error handling returns fallback on exceptions
- UTC DateTime compatibility with PostgreSQL

### React Layer Validation  
- Loading states display properly
- Events render when data loads
- Error messages show when API fails
- Empty states handle no data gracefully

### E2E Stack Validation
- Page loads without errors
- API calls complete successfully
- Responsive design works across devices
- Network failures handled gracefully

## 📝 Notes

- **Simple & Focused**: These are proof-of-concept tests, not production-ready
- **Throwaway Code**: Designed to validate the stack works, then can be replaced
- **Stack Validation**: Primary goal is proving React + API + PostgreSQL integration
- **Error Handling**: All layers include proper error handling and fallbacks

## 🔗 Documentation

See `/docs/standards-processes/testing/TEST_CATALOG.md` for complete test inventory and maintenance details.