# Business Requirements Lessons Learned

## File Discovery and Validation

### Don't Try to Read Files That May Not Exist (CRITICAL)
**Date**: 2025-08-17
**Category**: File Management
**Severity**: Critical

#### Context
Attempting to read files that may not exist causes errors and breaks the workflow. Must validate file existence before attempting to read.

#### What We Learned
- Always check if files exist before attempting to read them
- Use file discovery patterns (Glob, LS) to verify file presence
- Don't assume files exist based on naming conventions or expectations
- Handle file-not-found gracefully rather than failing the entire workflow

#### Action Items
- [ ] ALWAYS use LS or Glob to check file existence before Read operations
- [ ] PROVIDE graceful fallbacks when expected files don't exist
- [ ] DOCUMENT when files are expected vs optional in requirements analysis
- [ ] CREATE files only when explicitly required, never speculatively

#### Impact
Prevents workflow failures and provides more robust requirements analysis process.

### Tags
#critical #file-management #error-handling #workflow-robustness

## Documentation Discovery Process

### Check Master Index First for Existing Requirements (CRITICAL)
**Date**: 2025-08-17
**Category**: Process
**Severity**: Critical

#### Context
Must check the master index at `/docs/architecture/functional-area-master-index.md` before starting new requirements work to avoid duplication.

#### What We Learned
- Master index contains authoritative list of all functional areas and their current states
- Existing requirements may already cover requested functionality
- Current work status prevents conflicts with ongoing development
- Historical work provides context for new requirements

#### Action Items
- [ ] ALWAYS check master index before starting new requirements analysis
- [ ] IDENTIFY existing functional areas that might contain relevant requirements
- [ ] REVIEW current work status to avoid conflicts with ongoing development
- [ ] REFERENCE existing requirements when building on previous work

#### Impact
Prevents duplicate work, ensures consistency with existing requirements, and builds on established patterns.

### Tags
#critical #process #master-index #duplication-prevention

## Domain-Specific Business Analysis

### Focus on Domain-Specific Business Analysis
**Date**: 2025-08-17
**Category**: Analysis
**Severity**: High

#### Context
Business requirements analysis must focus on the specific domain knowledge and business patterns rather than generic analysis.

#### What We Learned
- WitchCityRope has unique community dynamics and safety requirements
- Rope bondage community has specific consent, safety, and privacy needs
- Event management for workshops requires specialized pricing and capacity models
- Membership tiers affect access patterns and feature requirements

#### Action Items
- [ ] LEVERAGE domain expertise in rope bondage community dynamics
- [ ] CONSIDER safety and consent implications in all requirements
- [ ] APPLY community-specific patterns for membership and access
- [ ] REFERENCE existing successful patterns from the platform

#### Impact
Ensures requirements align with community needs and leverage domain-specific understanding for better business solutions.

### Tags
#high #domain-expertise #community-analysis #business-patterns

## Platform Context Understanding

### Platform Overview: WitchCityRope Membership and Event Management
**Date**: 2025-08-17
**Category**: Platform Knowledge
**Severity**: High

#### Context
Understanding the platform's core purpose and user base is essential for all requirements analysis.

#### What We Learned
- WitchCityRope is a membership and event management platform for Salem's rope bondage community
- Primary functions: workshops, performances, social events, member management
- User roles: Admin, Teacher, Vetted Member, General Member, Guest/Attendee
- Technology stack: React + TypeScript + Vite frontend, .NET API backend, PostgreSQL database
- Key patterns: sliding scale pricing, consent workflows, safety protocols, vetting processes

#### Action Items
- [ ] ALWAYS consider community safety and consent requirements
- [ ] APPLY appropriate user role restrictions in requirements
- [ ] LEVERAGE existing pricing and event management patterns
- [ ] ENSURE requirements align with React frontend capabilities

#### Impact
Provides essential context for all requirements analysis and ensures community-appropriate solutions.

### Tags
#high #platform-knowledge #community-focus #context

## Requirements Quality Patterns

### Business Requirements Must Include Complete User Role Analysis
**Date**: 2025-08-17
**Category**: Quality
**Severity**: Medium

#### Context
Requirements analysis must consider all user roles and their different needs and permissions within the platform.

#### What We Learned
- Each user role (Admin, Teacher, Vetted Member, General Member, Guest) has different capabilities
- Access patterns vary significantly between role types
- Safety and privacy requirements differ by user role
- Business rules must account for role-based restrictions

#### Action Items
- [ ] ANALYZE impact on all user roles for every requirement
- [ ] DOCUMENT role-specific business rules and restrictions
- [ ] CONSIDER privacy implications for each role interaction
- [ ] VALIDATE requirements against existing role permission patterns

#### Impact
Ensures comprehensive requirements that work for all platform users and maintain appropriate access controls.

### Tags
#medium #quality #user-roles #access-control

## Technology Alignment

### Requirements Must Align with React Frontend Architecture
**Date**: 2025-08-17
**Category**: Architecture
**Severity**: Medium

#### Context
All requirements must be feasible within the React + TypeScript frontend architecture and .NET API backend.

#### What We Learned
- React component patterns influence UI requirements
- API-driven architecture affects data flow requirements
- Authentication uses hybrid JWT + HttpOnly Cookies pattern
- Real-time features may require SignalR or similar technology

#### Action Items
- [ ] ENSURE UI requirements align with React component patterns
- [ ] CONSIDER API design implications in data requirements
- [ ] REFERENCE existing authentication patterns for security requirements
- [ ] VALIDATE technical feasibility with development team when uncertain

#### Impact
Ensures requirements can be implemented efficiently within the existing technology stack.

### Tags
#medium #architecture #technology-alignment #feasibility

---
*This file is maintained by the business-requirements agent. Add new lessons immediately when discovered, remove outdated entries as needed.*
*Last updated: 2025-08-17 - Initial creation with essential business analysis patterns and platform knowledge*