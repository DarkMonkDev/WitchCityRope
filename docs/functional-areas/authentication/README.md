# Authentication System Documentation
<!-- Last Updated: 2025-08-04 -->
<!-- Version: 2.0 -->
<!-- Owner: Authentication Team -->
<!-- Status: Active -->

## Overview
The WitchCityRope authentication system manages user identity, access control, and session management for the rope bondage community platform. The system uses a hybrid approach with cookie-based authentication for the Blazor Server web application and JWT tokens for the REST API.

## Quick Links
- **Current Requirements**: [current-state/business-requirements.md](current-state/business-requirements.md)
- **Technical Design**: [current-state/functional-design.md](current-state/functional-design.md)
- **User Flows**: [current-state/user-flows.md](current-state/user-flows.md)
- **Test Coverage**: [current-state/test-coverage.md](current-state/test-coverage.md)
- **Active Work**: [new-work/status.md](new-work/status.md)

## Key Concepts
- **Hybrid Authentication**: Cookies for web, JWT for API
- **Role-Based Access**: Administrator, Member, EventOrganizer, etc.
- **Vetting System**: Members must be vetted for social event access
- **Age Verification**: 21+ requirement enforced
- **Scene Names**: Public display names for privacy

## Critical Implementation Notes

### ⚠️ Blazor Server Authentication Pattern
**NEVER** use SignInManager directly in Blazor components. This causes "Headers are read-only" errors. Instead:
- Blazor components redirect to Razor Pages for authentication operations
- `/Identity/Account/Login` handles actual sign-in
- See [functional-design.md](current-state/functional-design.md) for details

### Current Features
✅ Email/password login (Web and API)  
✅ User registration with scene names  
✅ Cookie authentication for Blazor  
✅ JWT authentication for API  
✅ Role-based authorization  
✅ Account lockout protection  

### Not Yet Implemented
❌ Two-factor authentication (infrastructure exists)  
❌ OAuth/Social login  
❌ Password reset flow  
❌ Email verification enforcement  

## Related Areas
- **Events Management**: Uses authentication for RSVP/ticket access
- **Membership Vetting**: Determines social event access
- **User Dashboard**: Displays user-specific content

## Contact
- Technical Owner: Authentication Team
- Business Owner: Community Safety Team
- Last Major Update: 2025-08-04 (Consolidation from 19 documents)