# Claude Code Configuration Updates for React Repository

<!-- Last Updated: 2025-08-14 -->
<!-- Version: 1.0 -->
<!-- Owner: Architecture Team -->
<!-- Status: Active -->

## Overview

This document details all changes required to migrate the Claude Code configuration from the Blazor Server repository to the React repository. The goal is to preserve all AI workflow functionality while adapting the configuration for React/TypeScript development patterns.

## Complete CLAUDE.md Migration

### Section-by-Section Updates

#### Header and Project Description
```markdown
# Claude Code Project Configuration - WitchCityRope React

## 🤖 AI Workflow Orchestration Active

**Project**: WitchCityRope - A membership and event management platform for Salem's rope bondage community.
**Technology Stack**: React SPA with TypeScript, Node.js API, PostgreSQL database
**Repository**: WitchCityRope-React (migrated from Blazor Server)
```

#### Architecture Warnings Section
**COMPLETE REPLACEMENT** of Section 1:

```markdown
### 1. React SPA + API Architecture
- **Frontend**: React SPA with TypeScript at http://localhost:3000
- **API Service**: Node.js/Express API at http://localhost:5000  
- **Database**: PostgreSQL at localhost:5433
- **Pattern**: React → HTTP Client → API → Database
- **Build Tool**: Vite for development and production builds

### 2. Pure React SPA - NO Server-Side Rendering
**NEVER CREATE:**
- ❌ Server-side rendered components (use Next.js patterns only if explicitly required)
- ❌ Mixed React/jQuery applications
- ❌ Direct DOM manipulation in components

**ALWAYS USE:**
- ✅ Functional components with hooks
- ✅ TypeScript for all components and logic
- ✅ Client-side routing with React Router
- ✅ Declarative state management (Zustand/Redux)
```

**COMPLETE REPLACEMENT** of Section 3:

```markdown
### 3. Authentication Pattern
- ❌ **NEVER** store auth tokens in localStorage without security consideration
- ✅ **ALWAYS** use secure token storage (httpOnly cookies or secure storage)
- ✅ **PATTERN**: React → API Client → JWT → API endpoints
- ✅ **AUTH FLOW**: Login → Store token securely → API requests with token
```

**COMPLETE REPLACEMENT** of Section 4:

```markdown
### 4. E2E Testing - Playwright for React
- ✅ **Location**: `/tests/playwright/`
- ✅ **Run**: `npm run test:e2e`
- ✅ **Pattern**: Test React components and user interactions
- ❌ **NO DOM testing libraries** in E2E tests (use React Testing Library for unit tests)
```

**COMPLETE REPLACEMENT** of Section 5:

```markdown
### 5. Development Build
```bash
# ❌ WRONG - Docker not used for React development:
docker-compose up

# ✅ CORRECT - React development:
npm install
npm run dev
# Opens http://localhost:3000

# ✅ API Development:
cd api/
npm install
npm run dev
# Opens http://localhost:5000
```

#### Technology Stack Section
**ADD NEW SECTION** after Architecture Warnings:

```markdown
## 🔧 Technology Stack

### Frontend Stack
- **Framework**: React 18+ with TypeScript
- **Build Tool**: Vite (fast dev server and builds)
- **State Management**: Zustand (primary) or Redux Toolkit (complex state)
- **Routing**: React Router v6
- **HTTP Client**: Axios with interceptors
- **UI Library**: [To be determined - Material-UI, Ant Design, or Mantine]
- **Styling**: CSS Modules + styled-components
- **Testing**: Jest + React Testing Library + Playwright

### API Stack (Preserved)
- **Framework**: Node.js with Express or ASP.NET Core API
- **Database**: PostgreSQL with migrations
- **Authentication**: JWT tokens
- **Validation**: Joi or Yup
- **Documentation**: OpenAPI/Swagger

### Development Tools
- **IDE**: VS Code with React/TypeScript extensions
- **Linting**: ESLint + Prettier
- **Type Checking**: TypeScript strict mode
- **Git Hooks**: Husky + lint-staged
- **Package Manager**: npm (primary) or yarn
```

#### File Patterns Section
**ADD NEW SECTION**:

```markdown
## 📁 React File Patterns

### Component Files
- **Components**: `.tsx` files for React components
- **Utilities**: `.ts` files for utility functions
- **Tests**: `.test.tsx` or `.spec.ts` for tests
- **Styles**: `.module.css` or styled-components
- **Types**: `.types.ts` for TypeScript interfaces

### Directory Structure
```
src/
├── components/           # Reusable React components
├── pages/               # Page-level components
├── hooks/               # Custom React hooks
├── services/            # API client and business logic
├── stores/              # State management (Zustand stores)
├── types/               # TypeScript type definitions
├── utils/               # Utility functions
├── styles/              # Global styles and themes
└── __tests__/           # Test utilities and setup
```

### Test File Patterns
- **Unit Tests**: `*.test.tsx`, `*.test.ts`
- **Integration Tests**: `*.integration.test.ts`
- **E2E Tests**: `*.spec.ts` in `/tests/playwright/`
- **Test Utilities**: `*/__tests__/*`, `/tests/utils/`
```

#### Quick Commands Section
**COMPLETE REPLACEMENT**:

```markdown
### Quick Commands
```bash
# Development
npm run dev                    # Start React dev server
npm run api                    # Start API server (if Node.js)
npm run dev:full              # Start both frontend and API

# Testing
npm test                      # Run unit tests (Jest)
npm run test:watch           # Run tests in watch mode
npm run test:coverage        # Run tests with coverage
npm run test:e2e             # Run E2E tests (Playwright)

# Building
npm run build                # Production build
npm run preview              # Preview production build
npm run type-check           # TypeScript type checking

# Linting and Formatting
npm run lint                 # ESLint
npm run lint:fix            # Auto-fix ESLint issues
npm run format              # Prettier formatting

# Health Checks
npm run health              # Check all services
npm run test:api            # Test API connectivity
```

#### Common Pitfalls Section
**COMPLETE REPLACEMENT**:

```markdown
## ⚠️ Common Pitfalls
1. **State Management**: Use immutable updates, avoid direct state mutation
2. **Component Re-renders**: Optimize with React.memo, useMemo, useCallback
3. **TypeScript**: Enable strict mode, avoid `any` types
4. **API Calls**: Handle loading states, errors, and race conditions
5. **Testing**: Test user behavior, not implementation details
6. **Performance**: Code splitting, lazy loading, bundle analysis
7. **Accessibility**: ARIA labels, keyboard navigation, screen readers
```

#### Session Checklist Section
**UPDATE**:

```markdown
## 📋 Session Checklist
- [ ] Read [/docs/00-START-HERE.md](./docs/00-START-HERE.md)
- [ ] Check [PROGRESS.md](./PROGRESS.md) for current status
- [ ] **LOG ALL FILES** in [/docs/architecture/file-registry.md](./docs/architecture/file-registry.md)
- [ ] Use `npm run dev` for development
- [ ] Use TodoWrite for multi-step tasks
- [ ] Follow TypeScript strict mode patterns
- [ ] Run tests before committing (`npm test`)
- [ ] **CLEANUP FILES** at session end per file registry
```

#### Test Accounts Section
**PRESERVE AS-IS** - API authentication unchanged

#### Environment Section
**UPDATE**:

```markdown
### Environment
- **OS**: Ubuntu 24.04 (Native Linux - NOT WSL)
- **Project Path**: `/home/chad/repos/WitchCityRope-React`
- **Node Version**: 18+ (LTS recommended)
- **Package Manager**: npm (primary)
- **MCP Servers**: `/home/chad/mcp-servers/`
- **GitHub**: https://github.com/DarkMonkDev/WitchCityRope-React.git
```

## Agent Updates Summary

### Agents Requiring Major Updates

#### react-developer.md (NEW)
```markdown
---
name: react-developer
description: React component development specialist for TypeScript React applications. Handles component creation, state management, routing, and React-specific patterns.
tools: Read, Write, Edit, MultiEdit, Bash, TodoWrite
---

# React Developer Agent

## Specialization
- React functional components with hooks (useState, useEffect, useContext, etc.)
- TypeScript integration with strict mode compliance
- State management with Zustand or Redux Toolkit
- React Router for client-side navigation
- Component testing with React Testing Library
- Performance optimization (memoization, code splitting)
- Accessibility compliance (WCAG guidelines)
- Modern React patterns (custom hooks, error boundaries, suspense)

## CRITICAL RESTRICTIONS
- FORBIDDEN from ALL test files: *.test.*, *.spec.*, /tests/**
- MUST use TypeScript strict mode - no `any` types
- MUST follow React best practices and hooks rules
- MUST implement proper error boundaries for component errors
- MUST consider accessibility in all components

## Startup Procedure
1. Read /docs/lessons-learned/react-developers.md
2. Read /docs/standards-processes/code-standards/react-patterns.md
3. Read /docs/standards-processes/code-standards/typescript-standards.md
4. Verify current React and TypeScript versions
5. Check component library and design system standards

## Implementation Patterns

### Component Structure
```tsx
// Preferred component structure
import React, { useState, useEffect } from 'react';
import styles from './ComponentName.module.css';

interface ComponentNameProps {
  // TypeScript interface for props
  prop1: string;
  prop2?: number;
  children?: React.ReactNode;
}

export const ComponentName: React.FC<ComponentNameProps> = ({ 
  prop1, 
  prop2 = 0, 
  children 
}) => {
  const [state, setState] = useState<StateType>(initialState);
  
  useEffect(() => {
    // Side effects with proper dependencies
  }, [dependency]);

  return (
    <div className={styles.container}>
      {/* JSX content */}
    </div>
  );
};
```

### State Management
- Use `useState` for local component state
- Use Zustand stores for shared application state
- Use `useReducer` for complex state logic
- Use `useContext` for theme/config sharing

### Error Handling
- Implement error boundaries for component trees
- Handle async errors in components
- Provide user-friendly error messages
- Log errors for debugging

## File Patterns I Handle
- React components: `*.tsx`
- Custom hooks: `use*.ts`
- Component styles: `*.module.css`
- Type definitions: `*.types.ts`
- Utility functions: `*.utils.ts`

## File Patterns I CANNOT Touch
- Test files: `*.test.*`, `*.spec.*`
- Test directories: `/tests/**`, `/__tests__/**`
- E2E test configurations
- Jest setup files
```

#### typescript-developer.md (NEW)
```markdown
---
name: typescript-developer
description: TypeScript specialist for React applications. Handles type definitions, interface design, generic patterns, and TypeScript configuration.
tools: Read, Write, Edit, MultiEdit
---

# TypeScript Developer Agent

## Specialization
- TypeScript configuration and setup
- Type definitions and interface design
- Generic type patterns and utilities
- Type-safe API client development
- Advanced TypeScript features (mapped types, conditional types)
- Integration with React component props
- Type-safe state management patterns

## Startup Procedure
1. Read /docs/standards-processes/code-standards/typescript-standards.md
2. Review tsconfig.json configuration
3. Check current TypeScript version and compatibility
4. Review existing type definitions for consistency

## Type Definition Patterns
```typescript
// API Response Types
interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
  errors?: string[];
}

// Component Props with Generic Constraints
interface ListProps<T extends { id: string }> {
  items: T[];
  onSelect: (item: T) => void;
  renderItem: (item: T) => React.ReactNode;
}

// State Management Types
interface UserState {
  currentUser: User | null;
  isLoading: boolean;
  error: string | null;
}
```
```

#### api-developer.md (UPDATED from backend-developer)
```markdown
---
name: api-developer
description: API integration specialist for React applications. Handles HTTP client setup, API service layers, data fetching patterns, and integration with React components.
tools: Read, Write, Edit, MultiEdit, Bash
---

# API Developer Agent

## Specialization
- HTTP client configuration (Axios/Fetch)
- API service layer architecture
- React data fetching patterns (useEffect, TanStack Query)
- Error handling and retry logic
- Authentication token management
- Request/response interceptors
- API integration testing setup

## CRITICAL RESTRICTIONS
- FORBIDDEN from test files: *.test.*, *.spec.*, /tests/**
- MUST handle errors gracefully
- MUST implement proper loading states
- MUST use TypeScript for all API interfaces

## API Client Patterns
```typescript
// API Client Setup
import axios from 'axios';

const apiClient = axios.create({
  baseURL: process.env.REACT_APP_API_URL,
  timeout: 10000,
});

// Request interceptor for auth
apiClient.interceptors.request.use((config) => {
  const token = getAuthToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});
```

## React Integration Patterns
```tsx
// Custom hook for API calls
const useApiData = <T>(endpoint: string) => {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        const response = await apiClient.get<T>(endpoint);
        setData(response.data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unknown error');
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [endpoint]);

  return { data, loading, error };
};
```
```

#### test-executor.md (UPDATED)
**Key changes for React testing**:

```markdown
## Testing Commands for React
```bash
# Unit tests with Jest
npm test
npm run test:watch
npm run test:coverage

# E2E tests with Playwright
npm run test:e2e
npm run test:e2e:ui

# Type checking
npm run type-check

# Linting (affects test quality)
npm run lint
npm run lint:fix
```

## React Test Patterns
- Unit tests: Component rendering, props, user interactions
- Integration tests: API integration, state management
- E2E tests: Full user workflows in browser

## Test Environment Prerequisites
1. Node.js 18+ installed
2. All npm dependencies installed (`npm install`)
3. API server running for integration tests
4. Environment variables configured
5. Playwright browsers installed (`npx playwright install`)
```

#### orchestrator.md (UPDATED)
**Key changes**:

```markdown
## Available Sub-Agents (Updated for React)

### Implementation Phase
- `react-developer`: React components and client-side logic
- `typescript-developer`: Type definitions and TypeScript patterns  
- `api-developer`: API integration and HTTP client setup
- `ui-designer`: Component design and user experience (updated for React)

### Testing Phase
- `test-executor`: Test execution for React (Jest + Playwright)
- `test-developer`: Test creation with React Testing Library patterns
- `code-reviewer`: Code review for TypeScript/React patterns

## React-Specific Workflow Adaptations

### Build Commands
```bash
# Development
npm run dev

# Testing
npm test
npm run test:e2e

# Production
npm run build
npm run preview
```

### File Pattern Validation
```python
# React file patterns for delegation
react_patterns = [
    r".*\.tsx$",      # React components
    r".*\.ts$",       # TypeScript files
    r".*\.css$",      # Styles
    r".*\.json$",     # Config files
]

test_patterns = [
    r".*\.test\.(ts|tsx)$",
    r".*\.spec\.(ts|tsx)$", 
    r".*/tests/.*",
    r".*/__tests__/.*",
]
```
```

## New Agent Creation Requirements

### state-management-specialist.md (NEW)
```markdown
---
name: state-management-specialist
description: State management specialist for React applications. Handles Zustand stores, Redux setup, state architecture, and performance optimization.
tools: Read, Write, Edit, MultiEdit
---

# State Management Specialist

## Specialization
- Zustand store design and implementation
- Redux Toolkit setup and patterns (when needed)
- State normalization and structure
- Performance optimization for state updates
- State persistence and hydration
- State testing patterns

## Zustand Patterns
```typescript
import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface UserStore {
  user: User | null;
  setUser: (user: User | null) => void;
  updateUser: (updates: Partial<User>) => void;
  clearUser: () => void;
}

export const useUserStore = create<UserStore>()(
  persist(
    (set, get) => ({
      user: null,
      setUser: (user) => set({ user }),
      updateUser: (updates) => set((state) => ({ 
        user: state.user ? { ...state.user, ...updates } : null 
      })),
      clearUser: () => set({ user: null }),
    }),
    {
      name: 'user-store',
    }
  )
);
```
```

## Trigger Word Updates

**ORCHESTRATOR-TRIGGERS.md** requires no changes - all trigger words remain the same:
- "continue", "implement", "test", "fix", etc.
- React development follows same workflow patterns
- Human review gates preserved

## Critical Implementation Notes

### Day 1 Priority Items
1. **Update CLAUDE.md** - Complete technology stack section
2. **Test orchestrator** - Verify trigger word detection works
3. **Create react-developer agent** - Essential for React work
4. **Update test-executor** - React testing commands
5. **Validate file patterns** - TypeScript/React file detection

### Validation Checkpoints
1. **Trigger Detection**: "implement a React component" should invoke orchestrator
2. **Agent Delegation**: Orchestrator should delegate to react-developer  
3. **File Restrictions**: react-developer should be forbidden from test files
4. **Testing Workflow**: test-executor should run npm commands
5. **Human Reviews**: Approval gates should still function

### Risk Mitigation
- **Test orchestrator immediately** after CLAUDE.md update
- **Preserve all workflow logic** while updating technical context
- **Maintain agent boundaries** with updated file patterns
- **Keep human review gates** for quality control

## Conclusion

The Claude Code configuration migration preserves all AI workflow functionality while adapting the technical context for React development. The key changes focus on:

1. **Technology Stack Updates**: React/TypeScript instead of Blazor/C#
2. **Command Updates**: npm commands instead of dotnet commands  
3. **File Pattern Updates**: .tsx/.ts files instead of .razor/.cs files
4. **Agent Specialization**: React patterns instead of Blazor patterns
5. **Testing Updates**: Jest/Playwright instead of .NET testing

All orchestration logic, human review gates, and quality standards are preserved while adapting to the React development environment.