# Archived Docker Documentation from Blazor Implementation

<!-- Last Updated: 2025-08-17 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian -->
<!-- Status: Archived -->

## Purpose

This directory contains Docker documentation from the previous Blazor Server implementation of WitchCityRope. These files have been archived because:

1. **Technology Migration**: Project migrated from Blazor Server to React + TypeScript
2. **Architecture Changes**: New stack is React (Vite) + .NET Minimal API + PostgreSQL
3. **Updated Implementation**: New Docker approach needed for React containerization

## Archived Files

| File | Original Purpose | Archival Date | Reason |
|------|------------------|---------------|--------|
| DOCKER_DEV_GUIDE.md | Blazor development Docker guide | 2025-08-17 | Blazor-specific, replaced by React guide |
| DOCKER_QUICK_REFERENCE.md | Quick Docker commands for Blazor | 2025-08-17 | Blazor-specific commands and ports |
| DOCKER_SETUP.md | Comprehensive Blazor Docker setup | 2025-08-17 | Blazor architecture, replaced by React setup |
| PORT_UPDATE_SUMMARY.md | Port configuration changes for Blazor | 2025-08-17 | Blazor port mappings, new ports for React |
| docker-development.md | Blazor Docker development workflow | 2025-08-17 | Blazor Server patterns, replaced by React patterns |

## Relevant Knowledge Extracted

The valuable Docker patterns from these files have been extracted and consolidated into:

**New Location**: `/docs/functional-areas/docker-authentication/requirements/existing-docker-knowledge.md`

**Extracted Knowledge**:
- Port configuration strategies
- PostgreSQL container setup
- Service-to-service communication patterns
- Environment variable management
- Hot reload configuration
- Health check implementations
- Troubleshooting patterns
- Security considerations

## What Was Not Migrated

### Blazor-Specific Content
- Blazor Server configuration
- ASP.NET Core Blazor patterns
- Blazor circuit handling
- Server-side rendering specifics
- Blazor development workflows

### Obsolete Information
- Old port mappings (5651-5654 range)
- Blazor-specific environment variables
- Server-side authentication patterns
- Blazor hot reload mechanisms

## Technology Mapping

| Blazor Implementation | React Implementation |
|----------------------|---------------------|
| Blazor Server | React + Vite |
| ASP.NET Core Web | .NET Minimal API |
| Server-side rendering | Client-side SPA |
| Blazor circuits | HTTP API calls |
| SignalR connections | REST/HTTP communication |
| Blazor components | React components |
| Razor pages | TypeScript + JSX |

## Reference Value

These archived files remain valuable for:

1. **Historical Reference**: Understanding previous implementation decisions
2. **Pattern Learning**: Docker patterns that worked well with .NET applications
3. **Troubleshooting**: Solutions to problems that may still apply
4. **Port Management**: Understanding how port conflicts were resolved
5. **PostgreSQL Setup**: Database container configuration that still applies

## Current Implementation

For current Docker implementation guidance, see:
- **Active Project**: `/docs/functional-areas/docker-authentication/`
- **Requirements**: `/docs/functional-areas/docker-authentication/requirements/existing-docker-knowledge.md`
- **Progress**: `/docs/functional-areas/docker-authentication/progress.md`

## Cleanup Policy

These files will be retained indefinitely as:
- They contain valuable implementation history
- Docker patterns may be referenced for future projects
- Troubleshooting solutions remain relevant
- No storage constraints require deletion

---

*Archived on 2025-08-17 during React migration - Docker authentication containerization preparation*