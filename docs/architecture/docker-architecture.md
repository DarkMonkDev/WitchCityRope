# Docker Architecture - WitchCityRope
<!-- Last Updated: 2025-08-17 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Docker Strategy Overview

WitchCityRope employs a containerized microservices architecture for development environments that preserves the proven React + .NET API + PostgreSQL authentication system. The strategy focuses on maintaining 100% feature parity between native and containerized development while preparing for future deployment scenarios.

### Core Principles
- **Zero Code Changes**: Containerization through configuration, not code modification
- **Development Efficiency**: Hot reload and debugging capabilities preserved
- **Service Isolation**: Each service runs in dedicated containers with proper networking
- **Agent Accessibility**: All development agents have access to appropriate Docker documentation

## Service Architecture

### Container Services
```
┌─────────────────────────────────────────────────────────────┐
│                     Docker Compose Network                   │
│                         (bridge)                            │
├─────────────────┬─────────────────┬─────────────────────────┤
│   React App     │    .NET API     │      PostgreSQL         │
│   (Vite Dev)    │   (Minimal API) │     (postgres:16)       │
│                 │                 │                         │
│ Port: 5173:3000 │ Port: 5655:8080 │   Port: 5433:5432      │
│ Service: web    │ Service: api    │   Service: postgres     │
│                 │                 │                         │
│ - Hot Reload    │ - dotnet watch  │ - Data Persistence      │
│ - Volume Mount  │ - Volume Mount  │ - Health Checks         │
│ - Vite HMR      │ - JWT/Identity  │ - Auto Restart          │
└─────────────────┴─────────────────┴─────────────────────────┘
```

### Service Communication Pattern
- **External Access**: Host ports → Container ports (5173:3000, 5655:8080, 5433:5432)
- **Internal Communication**: Service names resolve via Docker DNS (web → api → postgres)
- **Authentication Flow**: React → API (JWT + HttpOnly Cookies) → PostgreSQL
- **Network Isolation**: Custom bridge network `witchcityrope-dev`

## Local Development Setup

### Current Implementation
- **Development Environment**: Docker Compose with development overrides
- **Hot Reload**: Both React (Vite HMR) and .NET (dotnet watch) functional
- **Authentication System**: Hybrid JWT + HttpOnly Cookies pattern preserved
- **Database**: PostgreSQL with persistent volumes and automatic migrations
- **Performance**: Meets or exceeds native development experience

### Development Workflow
1. **Startup**: `./dev.sh` launches complete environment
2. **Code Changes**: Hot reload triggers appropriate container updates
3. **Testing**: E2E tests run against containerized services
4. **Debugging**: Standard development tools work with containerized services

## Future Scaling Provisions

### Test Environment Preparations
- **CI/CD Integration**: Container architecture supports automated testing
- **Database Isolation**: Separate test databases via container orchestration
- **Service Scaling**: Horizontal scaling capability built into architecture
- **Environment Consistency**: Identical containers across development and testing

### Production Deployment Readiness
- **Multi-stage Dockerfiles**: Development and production builds supported
- **Security Configuration**: Production-ready secret management patterns
- **Resource Management**: Container resource limits and health checks
- **Load Balancing**: Architecture supports multiple container instances

## Links to All Docker-Related Guides

### Primary Documentation
- **Docker Operations Guide**: [/docs/guides-setup/docker-operations-guide.md](/docs/guides-setup/docker-operations-guide.md)
  - Complete container management procedures
  - Hot reload testing and validation
  - Troubleshooting and debugging
  - Development workflow optimization

### Project-Specific Documentation
- **Business Requirements**: [/docs/functional-areas/docker-authentication/requirements/business-requirements.md](/docs/functional-areas/docker-authentication/requirements/business-requirements.md)
- **Functional Specification**: [/docs/functional-areas/docker-authentication/requirements/functional-specification.md](/docs/functional-areas/docker-authentication/requirements/functional-specification.md)
- **Implementation Progress**: [/docs/functional-areas/docker-authentication/progress.md](/docs/functional-areas/docker-authentication/progress.md)

### Related Documentation
- **Main Project Setup**: [/DOCKER_DEV_GUIDE.md](/DOCKER_DEV_GUIDE.md)
- **Authentication Architecture**: [/docs/architecture/react-migration/AUTHENTICATION-DECISION-FINAL.md](/docs/architecture/react-migration/AUTHENTICATION-DECISION-FINAL.md)
- **Existing Docker Knowledge**: [/docs/functional-areas/docker-authentication/requirements/existing-docker-knowledge.md](/docs/functional-areas/docker-authentication/requirements/existing-docker-knowledge.md)

## Agent Instruction Matrix

### Which Agents Need Docker Information

| Agent Role | Primary Need | Docker Documentation Required | Direct Link Location |
|------------|--------------|-------------------------------|---------------------|
| **orchestrator** | Task delegation and progress tracking | Central architecture overview | This document |
| **test-executor** | Container testing and health validation | Operations guide + testing procedures | [Docker Operations Guide](/docs/guides-setup/docker-operations-guide.md) |
| **backend-developer** | .NET API containerization and debugging | Operations guide + hot reload testing | [Docker Operations Guide](/docs/guides-setup/docker-operations-guide.md) |
| **react-developer** | Frontend containerization and hot reload | Operations guide + Vite configuration | [Docker Operations Guide](/docs/guides-setup/docker-operations-guide.md) |
| **database-designer** | PostgreSQL container management | Operations guide + database operations | [Docker Operations Guide](/docs/guides-setup/docker-operations-guide.md) |
| **librarian** | Documentation organization and file management | Architecture overview for agent direction | This document |

### Agent Direction Patterns

#### When to Direct Agents to Docker Documentation
- **Development Tasks**: Any work involving containerized services
- **Testing Tasks**: E2E testing, integration testing, health checks
- **Debugging Tasks**: Service communication issues, hot reload problems
- **Setup Tasks**: Environment initialization, dependency management

#### Orchestrator Direction Strategies
```
IF task involves container operations:
  THEN direct to Docker Operations Guide
IF task involves Docker architecture decisions:
  THEN direct to Docker Architecture document
IF task involves agent coordination:
  THEN check agent lessons learned for Docker knowledge
```

## Agent Knowledge Integration

### Lessons Learned File Integration
Each development agent has Docker knowledge integrated into their role-specific lessons learned files:

- **test-executor-lessons-learned.md**: Container testing procedures and health checks
- **backend-developer-lessons-learned.md**: .NET API containerization and hot reload
- **react-developer-lessons-learned.md**: Frontend containerization and Vite configuration

### Knowledge Distribution Pattern
1. **Central Architecture** (this document): Strategic overview and agent direction
2. **Operations Guide**: Tactical procedures and troubleshooting
3. **Lessons Learned**: Role-specific applications and discoveries
4. **Project Documentation**: Specific implementation details

## Container Technology Stack

### Base Images
- **React Development**: `node:18-alpine`
- **.NET API Development**: `mcr.microsoft.com/dotnet/sdk:9.0`
- **PostgreSQL**: `postgres:16-alpine`
- **Production React**: `nginx:alpine` (future)
- **Production API**: `mcr.microsoft.com/dotnet/aspnet:9.0` (future)

### Development Tools Integration
- **Docker Desktop**: Primary container management platform
- **Docker Compose**: Multi-service orchestration
- **Volume Mounting**: Source code hot reload support
- **Custom Networks**: Service isolation and communication
- **Health Checks**: Automated service monitoring

## Success Metrics and Monitoring

### Development Environment Success Indicators
- **Startup Time**: Complete environment ready in <60 seconds
- **Hot Reload Performance**: React <1s, API restart <5s
- **Authentication Functionality**: 100% feature parity with native
- **Resource Usage**: Reasonable CPU and memory consumption
- **Developer Experience**: Equivalent or superior to native development

### Container Health Monitoring
- **PostgreSQL Health**: Database connectivity and response time
- **API Health**: Endpoint availability and authentication functionality
- **React Health**: Development server responsiveness and hot reload
- **Network Health**: Service-to-service communication validation

## Security Considerations

### Development Security
- **Container Isolation**: Services run in isolated containers
- **Network Security**: Custom bridge network with controlled access
- **Secret Management**: Development secrets via environment files
- **Volume Security**: Source code mounts with appropriate permissions

### Authentication Security Preservation
- **JWT Handling**: Service-to-service token validation
- **Cookie Security**: HttpOnly cookies across container network
- **CORS Configuration**: Proper origin handling for containers
- **Database Security**: Secure PostgreSQL authentication

## Troubleshooting Strategy

### Common Issue Categories
1. **Container Startup**: Service dependency and health check issues
2. **Hot Reload**: File watching and volume mount problems
3. **Service Communication**: Network connectivity and DNS resolution
4. **Authentication**: Token and cookie handling across containers
5. **Database**: Connection and migration issues

### Resolution Approach
1. **Check Health**: Use health check endpoints to validate service status
2. **Review Logs**: Container logs provide detailed error information
3. **Validate Configuration**: Environment variables and port mappings
4. **Test Communication**: Service-to-service connectivity validation
5. **Consult Operations Guide**: Comprehensive troubleshooting procedures

For detailed troubleshooting procedures, see the [Docker Operations Guide](/docs/guides-setup/docker-operations-guide.md).

---

This Docker architecture provides the strategic foundation for containerized development while maintaining the proven authentication system and preparing for future scaling needs. All development agents can reference this document for high-level Docker understanding and direction to appropriate operational documentation.