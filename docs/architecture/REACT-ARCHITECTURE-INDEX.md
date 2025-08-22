# React Architecture Index - WitchCityRope
<!-- Last Updated: 2025-08-22 -->
<!-- Last Validated: 2025-08-22 -->
<!-- Version: 1.0 -->
<!-- Primary Maintainer: React-Developer Agent -->
<!-- Structure Owner: Librarian Agent -->
<!-- Status: Active -->

## 🎯 Purpose

This index provides **react-developer agents** and all development team members with a comprehensive guide to React architecture documentation in the WitchCityRope project. All React architecture resources are centrally catalogued here to prevent confusion and ensure agents can quickly locate the information they need.

## 📝 Shared Ownership & Maintenance Model

**PRIMARY MAINTAINER**: **React-Developer Agent** - Uses daily, finds broken links first  
**STRUCTURE OWNER**: **Librarian Agent** - Maintains organization and standards compliance  
**VALIDATION**: React-Developer updates "Last Validated" date when verifying links  

### **🔧 IMMEDIATE REPAIR AUTHORITY**
**React-Developer Agents**: ✅ **FIX BROKEN LINKS IMMEDIATELY** - Don't wait for delegation  
**React-Developer Agents**: ✅ **UPDATE Last Validated date** when checking links  
**React-Developer Agents**: ✅ **ADD missing architecture resources** discovered during work  

### **📋 SHARED RESPONSIBILITIES**
**React-Developer Primary Tasks:**
- 🔧 **Fix broken links immediately** (don't wait for Librarian)
- 📅 **Update "Last Validated" date** when using the index
- ➕ **Add missing resources** discovered during development work
- 🚨 **Report structural issues** to Librarian Agent

**Librarian Agent Tasks:**
- 🏗️ **Major reorganizations** when functional areas change
- 📏 **Standards compliance** enforcement
- 🗂️ **File registry updates** for index modifications
- 🔍 **Structural validation** of index organization

### **🚨 CRITICAL: No Permission Required for Link Fixes**
**React-Developer**: If you find a broken link → FIX IT IMMEDIATELY  
**React-Developer**: Update "Last Validated" date after any link verification  
**React-Developer**: This index MUST work when you need it

## 🚨 CRITICAL FOR REACT DEVELOPERS

### **Primary Architecture Documents (READ FIRST)**

1. **📋 Main Project Architecture**: `/ARCHITECTURE.md`
   - **Overview**: Web+API microservices architecture with React frontend
   - **Critical Info**: Port configuration, service communication, authentication flow
   - **React Context**: React + TypeScript + Vite + Mantine UI Framework
   - **Database Access**: React → HTTP API calls (NEVER direct database access)

2. **🏗️ React Architecture Research**: `/docs/architecture/react-migration/react-architecture.md`
   - **Overview**: Comprehensive React patterns, state management, routing analysis
   - **Technology Stack**: Zustand + React Query + React Router v7 + Vite
   - **Component Architecture**: Feature-based organization patterns
   - **Performance**: Code splitting, optimization patterns

3. **🛠️ React Developer API Guide**: `/docs/guides-setup/ai-agents/react-developer-api-changes-guide.md`
   - **Overview**: API architecture changes and frontend integration
   - **Critical Info**: Minimal frontend impact, consistent response formats
   - **Type Generation**: NSwag integration improvements
   - **Migration Checklist**: Step-by-step update procedures

---

## 📁 Complete React Architecture Resource Map

### **🏛️ Core Architecture Documents**

| Document | Location | Purpose | Last Updated |
|----------|----------|---------|--------------|
| **Main Architecture** | `/ARCHITECTURE.md` | Primary system architecture - Web+API pattern | 2025-08-22 |
| **React Architecture Research** | `/docs/architecture/react-migration/react-architecture.md` | State management, routing, component patterns | 2025-08-13 |
| **API Integration Guide** | `/docs/architecture/react-migration/api-integration.md` | Frontend-API communication patterns | Active |
| **Technical Context** | `/docs/architecture/react-migration/technical-context.md` | Migration technical background | Active |

### **📋 Architecture Decision Records (ADRs)**

| ADR | Location | Decision | Date | Status |
|-----|----------|----------|------|--------|
| **ADR-004** | `/docs/architecture/decisions/adr-004-ui-framework-mantine.md` | **Mantine v7** UI framework selection | Current | **ACTIVE** |
| **ADR-002** | `/docs/architecture/decisions/adr-002-authentication-api-pattern.md` | Cookie-based authentication pattern | Current | **ACTIVE** |
| **ADR-003** | `/docs/architecture/decisions/adr-003-playwright-e2e-testing.md` | Playwright for E2E testing | Current | **ACTIVE** |

### **🔧 Implementation Guides**

| Guide | Location | Purpose | Target Audience |
|-------|----------|---------|-----------------|
| **React Developer API Guide** | `/docs/guides-setup/ai-agents/react-developer-api-changes-guide.md` | API changes coordination | React Developers |
| **Migration Implementation Plan** | `/docs/architecture/react-migration/detailed-implementation-plan.md` | Step-by-step migration | All Developers |
| **Authentication Integration** | `/docs/architecture/react-migration/authentication-research.md` | Auth implementation patterns | React Developers |
| **UI Components Research** | `/docs/architecture/react-migration/ui-components-research.md` | Component library decisions | React Developers |

### **📊 Migration Documentation**

| Document | Location | Purpose | Status |
|----------|----------|---------|--------|
| **Migration Plan** | `/docs/architecture/react-migration/migration-plan.md` | Overall migration strategy | Complete |
| **Strategy Recommendation** | `/docs/architecture/react-migration/strategy-recommendation.md` | Rebuild vs migrate analysis | Complete |
| **Progress Tracking** | `/docs/architecture/react-migration/progress.md` | Migration status | Active |
| **Migration Checklist** | `/docs/architecture/react-migration/migration-checklist.md` | Implementation steps | Active |

### **🎨 UI/UX Architecture**

| Document | Location | Purpose | Framework |
|----------|----------|---------|-----------|
| **UI Framework Research** | `/docs/architecture/react-migration/ui-framework-research.md` | Framework comparison analysis | Mantine v7 |
| **Component Patterns** | `/docs/architecture/react-migration/react-architecture.md` | React component organization | Features-based |
| **Validation Patterns** | `/docs/architecture/react-migration/validation-research.md` | Form validation strategies | React Hook Form + Zod |

### **🔐 Authentication Architecture**

| Document | Location | Purpose | Pattern |
|----------|----------|---------|---------|
| **Authentication Decision** | `/docs/architecture/react-migration/AUTHENTICATION-DECISION-FINAL.md` | Final auth strategy | Cookie-based |
| **Auth Strategies Comparison** | `/docs/architecture/react-migration/authentication-strategies-comparison.md` | Options analysis | HTTP-only cookies |
| **Auth Implementation** | `/docs/architecture/react-migration/authentication-research.md` | Technical implementation | JWT service-to-service |

### **🏗️ Development Architecture**

| Document | Location | Purpose | Technology |
|----------|----------|---------|------------|
| **Build Tools** | `/docs/architecture/react-migration/react-architecture.md` | Build system analysis | Vite |
| **State Management** | `/docs/architecture/react-migration/react-architecture.md` | State architecture | Zustand + React Query |
| **Routing Strategy** | `/docs/architecture/react-migration/react-architecture.md` | Navigation patterns | React Router v7 |

---

## 🛠️ Technology Stack Summary

### **Frontend Architecture**
- **Framework**: React + TypeScript + Vite
- **UI Library**: **Mantine v7** (ADR-004) - TypeScript-first, WCAG compliant
- **State Management**: **Zustand** (global) + **React Query** (server state)
- **Routing**: **React Router v7** (stable, proven choice)
- **Forms**: **React Hook Form** + **Zod validation**
- **Build Tool**: **Vite** (lightning-fast HMR)

### **API Integration**
- **Pattern**: React → HTTP calls → .NET Minimal API
- **Authentication**: HTTP-only cookies (XSS prevention)
- **Type Generation**: **NSwag** (OpenAPI → TypeScript)
- **Error Handling**: Standardized Problem Details format
- **Data Fetching**: React Query with caching

### **Development Tools**
- **Testing**: Vitest + Testing Library (unit) + Playwright (E2E)
- **Linting**: ESLint + Prettier
- **DevTools**: React DevTools + Query DevTools

---

## 🎯 Quick Access for Common Tasks

### **For react-developer Agent Starting Work**
1. **Read First**: `/ARCHITECTURE.md` (system overview)
2. **Architecture Patterns**: `/docs/architecture/react-migration/react-architecture.md`
3. **API Integration**: `/docs/guides-setup/ai-agents/react-developer-api-changes-guide.md`
4. **UI Framework**: `/docs/architecture/decisions/adr-004-ui-framework-mantine.md`

### **For New Feature Implementation**
1. **Component Patterns**: `/docs/architecture/react-migration/react-architecture.md` (Component Architecture section)
2. **State Management**: `/docs/architecture/react-migration/react-architecture.md` (State Management section)
3. **API Integration**: `/docs/architecture/react-migration/api-integration.md`
4. **Form Validation**: `/docs/architecture/react-migration/validation-research.md`

### **For Authentication Work**
1. **Auth Decision**: `/docs/architecture/react-migration/AUTHENTICATION-DECISION-FINAL.md`
2. **Implementation**: `/docs/architecture/decisions/adr-002-authentication-api-pattern.md`
3. **API Guide**: `/docs/guides-setup/ai-agents/react-developer-api-changes-guide.md` (Authentication section)

### **For Testing Integration**
1. **E2E Testing**: `/docs/architecture/decisions/adr-003-playwright-e2e-testing.md`
2. **Component Testing**: `/docs/architecture/react-migration/react-architecture.md` (Development Tools section)

---

## 📚 Lessons Learned Resources

| File | Location | Purpose |
|------|----------|---------|
| **React Developer Lessons** | `/docs/lessons-learned/react-developer-lessons-learned.md` | Agent-specific patterns and solutions |
| **Frontend Lessons** | `/docs/lessons-learned/ui-developers.md` | UI development patterns |

---

## 🔍 Migration Status & Progress

### **✅ Completed Architecture Decisions**
- ✅ **UI Framework**: Mantine v7 selected (ADR-004)
- ✅ **Authentication**: Cookie-based pattern finalized
- ✅ **Testing**: Playwright for E2E confirmed
- ✅ **State Management**: Zustand + React Query established
- ✅ **Build Tools**: Vite confirmed for development

### **🔄 Active Architecture Work**
- **API Integration**: Ongoing coordination with backend vertical slice migration
- **Component Library**: Mantine components implementation
- **Type Generation**: NSwag pipeline improvements
- **Performance Optimization**: Code splitting and caching strategies

---

## 🚨 Architecture Compliance Requirements

### **MANDATORY Patterns (react-developer MUST Follow)**

1. **UI Framework**: ✅ **ONLY Mantine v7** components (ADR-004)
   - ❌ NO Chakra UI, Material-UI, or other frameworks
   - ✅ Use Mantine's TypeScript-first approach

2. **Authentication**: ✅ **HTTP-only cookies ONLY**
   - ❌ NO localStorage token storage (XSS risk)
   - ✅ Use `/api/auth/login`, `/api/auth/logout` endpoints

3. **API Communication**: ✅ **HTTP calls to API service**
   - ❌ NO direct database access from React
   - ✅ Use `credentials: 'include'` for cookies

4. **State Management**: ✅ **Zustand + React Query pattern**
   - ✅ Zustand for global app state
   - ✅ React Query for server state
   - ✅ useState for local component state

5. **Routing**: ✅ **React Router v7**
   - ✅ Protected routes with role-based access
   - ✅ Feature-based route organization

### **File Creation Rules**
- ✅ **ALWAYS** check functional-area-master-index.md first
- ✅ **USE** `/docs/functional-areas/` for feature work
- ✅ **UPDATE** file registry for ALL operations
- ❌ **NEVER** create files in `/docs/` root

---

## 🔧 Troubleshooting Quick Reference

### **Common React Architecture Issues**

1. **Type Generation Failures**
   - **Check**: `/docs/guides-setup/ai-agents/react-developer-api-changes-guide.md` (Troubleshooting section)
   - **Solution**: Verify API running, regenerate types

2. **Authentication Issues**
   - **Check**: `/docs/architecture/decisions/adr-002-authentication-api-pattern.md`
   - **Solution**: Ensure `credentials: 'include'` in fetch calls

3. **UI Component Problems**
   - **Check**: `/docs/architecture/decisions/adr-004-ui-framework-mantine.md`
   - **Solution**: Use only Mantine v7 components

4. **State Management Confusion**
   - **Check**: `/docs/architecture/react-migration/react-architecture.md` (State Management section)
   - **Solution**: Follow Zustand + React Query hybrid pattern

---

## 🎯 Success Metrics

### **Architecture Quality Indicators**
- ✅ **100% Mantine Components**: No mixed UI frameworks
- ✅ **Zero XSS Vulnerabilities**: HTTP-only cookie authentication
- ✅ **< 200ms API Response**: Efficient backend integration
- ✅ **100% TypeScript Coverage**: Strong type safety
- ✅ **Clean Architecture**: Feature-based organization

### **Developer Experience Metrics**
- ✅ **< 2 seconds Dev Server Start**: Vite performance
- ✅ **< 300ms Hot Reload**: Development efficiency
- ✅ **Zero Breaking Changes**: API compatibility maintained
- ✅ **Consistent Patterns**: Architecture compliance

---

## 📞 Getting Help

### **For react-developer Agents**
1. **Architecture Questions**: Check this index first
2. **API Integration Issues**: `/docs/guides-setup/ai-agents/react-developer-api-changes-guide.md`
3. **Pattern Clarification**: `/docs/architecture/react-migration/react-architecture.md`
4. **Lessons Learned**: `/docs/lessons-learned/react-developer-lessons-learned.md`

### **Documentation Updates**
- **File Registry**: Update `/docs/architecture/file-registry.md` for ALL changes
- **Master Index**: Check `/docs/architecture/functional-area-master-index.md`
- **Architecture Changes**: Update this index when new patterns emerge

---

*This index uses **SHARED OWNERSHIP**: React-Developer Agent maintains links and content, Librarian Agent maintains structure and organization.*

**For React-Developer Agents**:
- ✅ **USE** this index as your primary architecture resource
- ✅ **FIX BROKEN LINKS IMMEDIATELY** - no permission required  
- ✅ **UPDATE "Last Validated" date** when verifying links
- ✅ **ADD missing resources** you discover during development
- ✅ **REPORT structural issues** to Librarian Agent for major changes
- ❌ **DO NOT** create duplicate architecture documentation