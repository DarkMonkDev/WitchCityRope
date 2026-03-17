# Authentication Patterns and Best Practices
<!-- Last Updated: 2026-03-17 -->
<!-- Version: 4.0 -->
<!-- Owner: Authentication Team -->
<!-- Status: Redirect - Blazor-era content archived -->

## ARCHIVED: Blazor-Era Authentication Patterns

This file previously contained Blazor Server authentication patterns (SignInManager, Blazor components, cookie sharing between containers, etc.). The application migrated from Blazor Server to React + TypeScript in August 2025, making all Blazor-specific authentication patterns obsolete.

**Archived**: 2026-03-17

## Current Authentication Documentation

The WitchCityRope authentication system now uses a BFF (Backend-For-Frontend) pattern with dual httpOnly cookies, refresh token rotation, and a React frontend. See:

- **Architecture overview**: `/ARCHITECTURE.md` (Authentication and Security section)
- **Frontend patterns**: `/docs/standards-processes/frontend/authentication-pattern-guide.md`
- **BFF implementation guide**: `/docs/functional-areas/authentication/bff-authentication-implementation-guide.md`
- **Auth feature hub**: `/docs/functional-areas/authentication/README.md`
