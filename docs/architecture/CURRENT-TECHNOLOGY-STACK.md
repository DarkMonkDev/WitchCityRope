# Current Technology Stack

**Last Updated**: 2025-10-08
**Status**: Active
**Owner**: Architecture Team

## Purpose

This document serves as the single source of truth for all technologies currently in use by WitchCityRope. It is maintained by the architecture team and updated with every technology addition, upgrade, or replacement.

## Frontend Technologies

### React Application
- **Framework**: React 18.3.1 + TypeScript 5.2.2
- **Build Tool**: Vite 5.3.1
- **UI Framework**: Mantine v7 (ADR-004)
- **Rich Text Editor**: @mantine/tiptap (Tiptap v2 wrapper)
- **State Management**: Zustand 5.0.7 + TanStack Query 5.85.3
- **Routing**: React Router 7.8.1
- **Forms**: Mantine use-form + Zod 4.0.17

### Rich Text Editor Details

**Current**: @mantine/tiptap
**Migration Date**: October 8, 2025
**Replaced**: TinyMCE (retired due to API key and quota issues)
**Documentation**: `/docs/functional-areas/html-editor-migration/`

**Key Features**:
- Client-side only (no external dependencies)
- Variable insertion support ({{fieldName}} syntax)
- Full Mantine theme integration
- ~155KB bundle size (vs ~500KB+ for TinyMCE)
- Zero usage quotas or API keys

**Component Location**: `/apps/web/src/components/forms/MantineTiptapEditor.tsx`
**Usage**: EventForm rich text fields, email templates

### Development Tools
- **Package Manager**: npm
- **Dev Server**: Vite dev server with HMR
- **TypeScript**: 5.2.2 with strict mode
- **Linting**: ESLint + Prettier
- **Testing**: Vitest + React Testing Library

## Backend Technologies

### API Service
- **Framework**: ASP.NET Core Minimal API (.NET 9)
- **ORM**: Entity Framework Core 9.0
- **Database**: PostgreSQL 16
- **Authentication**: ASP.NET Core Identity with JWT + HttpOnly Cookies
- **API Documentation**: Swagger/OpenAPI

### Database
- **Server**: PostgreSQL 16
- **Migration Tool**: Entity Framework Core Migrations
- **Seed Data**: Automated via DatabaseInitializationService
- **Connection Pooling**: Npgsql connection pooling enabled

### Authentication Architecture
- **Pattern**: BFF (Backend-for-Frontend) with httpOnly cookies
- **Token Type**: JWT (JSON Web Tokens)
- **Security**: httpOnly cookies, XSS protection, CSRF protection
- **Session Management**: Silent token refresh
- **Documentation**: `/docs/functional-areas/authentication/`

## Testing Technologies

### End-to-End Testing
- **Framework**: Playwright
- **Test Location**: `/tests/playwright/`
- **Run Command**: `npm run test:e2e:playwright`
- **Status**: 100% pass rate on launch-critical tests (as of 2025-10-08)

### React Unit Testing
- **Framework**: Vitest + React Testing Library
- **Test Location**: `/apps/web/src/**/*.test.tsx`
- **Run Command**: `npm run test`

### API Testing
- **Framework**: xUnit + FluentAssertions
- **Test Location**: `/tests/WitchCityRope.Core.Tests/`
- **Run Command**: `dotnet test`

### Integration Testing
- **Framework**: xUnit + TestContainers
- **Database**: Real PostgreSQL via TestContainers
- **Test Location**: `/tests/WitchCityRope.IntegrationTests/`
- **Run Command**: `dotnet test tests/WitchCityRope.IntegrationTests/`

## Infrastructure Technologies

### Container Platform
- **Platform**: Docker + Docker Compose
- **Development**: docker-compose.dev.yml for hot reload
- **Production**: docker-compose.yml
- **Helper Script**: `./dev.sh` for development operations

### Version Control
- **System**: Git
- **Platform**: GitHub
- **Repository**: https://github.com/DarkMonkDev/WitchCityRope.git

### CI/CD (Planned)
- **Platform**: GitHub Actions
- **Documentation**: `/docs/functional-areas/deployment/`

## Development Environment

### Required Software
- **Docker**: Latest stable version
- **Docker Compose**: Latest stable version
- **.NET SDK**: .NET 9
- **Node.js**: v18+ (for React development)
- **npm**: v9+

### Optional Tools
- **pgAdmin**: Database management (runs in Docker)
- **VS Code**: Recommended IDE
- **Visual Studio**: Optional for .NET development

## External Services

### Payment Processing
- **Provider**: PayPal
- **Integration**: PayPal REST API + Webhooks
- **Webhook Infrastructure**: Cloudflare tunnel
- **Documentation**: `/docs/functional-areas/payment-paypal-venmo/`
- **Status**: Production ready (as of 2025-09-14)

## Deprecated Technologies

### Replaced - October 8, 2025
- **TinyMCE**: Replaced by @mantine/tiptap
  - **Reason**: API key management, testing quotas, external service dependency
  - **Migration**: Complete on October 8, 2025
  - **Archive**: `/docs/_archive/tinymce-decision-2025-08-24/`
  - **Impact**: No breaking changes, feature parity maintained

### Replaced - August 2025
- **Blazor Server**: Replaced by React + TypeScript
  - **Reason**: React migration for better developer experience and ecosystem
  - **Migration**: Complete on September 14, 2025
  - **Archive**: `/docs/_archive/` (various Blazor documentation)
  - **Impact**: Complete frontend rewrite

## Version Compatibility Matrix

| Frontend | Backend | Database | Status |
|----------|---------|----------|--------|
| React 18.3.1 | .NET 9 | PostgreSQL 16 | ✅ Production |
| TypeScript 5.2.2 | EF Core 9.0 | Npgsql 8.x | ✅ Production |
| Mantine v7 | - | - | ✅ Production |
| @mantine/tiptap 7.x | - | - | ✅ Production |

## Package Version Pinning Policy

### Frontend (package.json)
- **Major versions**: Pinned (e.g., `7.x.x` for Mantine)
- **Minor versions**: Flexible for patches
- **Review cycle**: Quarterly for major updates

### Backend (.csproj)
- **Framework**: Pin to LTS versions (.NET 9)
- **Packages**: Allow minor version updates
- **Review cycle**: With .NET release schedule

## Technology Decision Process

All technology decisions must:
1. Be documented in an ADR (Architecture Decision Record)
2. Include migration path from existing technology
3. Address security, performance, and maintenance concerns
4. Have approval from architecture team
5. Update this document immediately upon approval

**ADR Location**: `/docs/architecture/decisions/`

## Related Documentation

- **Architecture Overview**: `/ARCHITECTURE.md`
- **React Migration Plan**: `/docs/architecture/react-migration/migration-plan.md`
- **UI Framework Decision**: `/docs/architecture/decisions/adr-004-ui-framework-mantine.md`
- **HTML Editor Migration**: `/docs/functional-areas/html-editor-migration/`
- **Authentication Implementation**: `/docs/functional-areas/authentication/`
- **Database Initialization**: `/docs/functional-areas/database-initialization/`

## Change Log

| Date | Change | Reason | Impact |
|------|--------|--------|--------|
| 2025-10-08 | @mantine/tiptap replaced TinyMCE | API key management, testing quotas | No breaking changes |
| 2025-09-14 | React migration complete | Frontend modernization | Complete UI rewrite |
| 2025-09-14 | PayPal integration complete | Payment processing | New payment capabilities |
| 2025-08-22 | Database auto-initialization | Developer experience | 95% faster setup |
| 2025-08-17 | Mantine v7 selected | UI framework standardization | New component library |

---

**Maintenance**: This document should be updated within 24 hours of any technology change, upgrade, or deprecation.
