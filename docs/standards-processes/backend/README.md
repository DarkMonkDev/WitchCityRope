# Backend Coding Standards

## Purpose
This folder contains comprehensive backend API coding standards, implementation guides, and architectural patterns for the WitchCityRope project.

## Organization

### Implementation Guides
- **vertical-slice-implementation-guide.md**: Complete guide for Simple Vertical Slice Architecture pattern used in WitchCityRope API

### Future Standards (To Be Added)
- Error handling patterns and standards
- Database query optimization guidelines
- Caching strategies and implementation
- Logging standards and best practices
- Testing patterns for backend services
- Security implementation guidelines
- Performance optimization techniques
- API documentation standards

## Current Architecture

**WitchCityRope uses Simple Vertical Slice Architecture**:
- Direct Entity Framework services (NO repositories)
- Minimal API endpoints (NO MediatR/CQRS)
- Feature-based folder organization
- Simple error handling with Result pattern

**Reference Implementation**: `/apps/api/Features/Health/`

## Key Principles

1. **SIMPLICITY ABOVE ALL**: No unnecessary complexity
2. **Direct Database Access**: Use DbContext directly, no repository layers
3. **Feature Organization**: Group related functionality together
4. **Minimal API**: Simple endpoint registration with direct service calls
5. **Type Safety**: NSwag-generated TypeScript types from C# DTOs

## Related Documentation

- **API Architecture Modernization**: `/docs/functional-areas/api-architecture-modernization/`
- **DTO Alignment Strategy**: `/docs/functional-areas/api-data-alignment/`
- **Lessons Learned**: `/docs/lessons-learned/backend-developer-lessons-learned.md`
- **Agent Boundaries**: `/docs/standards-processes/agent-boundaries.md`

## Audit and Validation

Current standards are being validated through comprehensive audit:
- **Audit Location**: `/docs/standards-processes/backend-api-audit-2025-10-23/`
- **Start Date**: 2025-10-23
- **Purpose**: Validate implementation against latest .NET 9 best practices
- **Primary Sources**: Milan Jovanovic, Microsoft official documentation

## Maintenance

Backend standards are actively maintained and updated based on:
- Latest .NET releases and features
- Industry best practices evolution
- Team feedback and lessons learned
- Performance optimization discoveries
- Security updates and improvements

---
**Created**: 2025-10-23
**Owner**: Backend Development Team
**Status**: Active
**Last Updated**: 2025-10-23
