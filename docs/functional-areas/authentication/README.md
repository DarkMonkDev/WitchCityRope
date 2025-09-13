# Authentication System Documentation
<!-- Last Updated: 2025-09-12 -->
<!-- Version: 3.0 -->
<!-- Owner: Authentication Team -->
<!-- Status: Active - BFF Pattern Implementation Complete -->

## Overview
The WitchCityRope authentication system manages user identity, access control, and session management for the rope bondage community platform. The system uses a **secure BFF (Backend-for-Frontend) pattern** with httpOnly cookies for React frontend authentication and enhanced security.

## Quick Links
- **Current Requirements**: [current-state/business-requirements.md](current-state/business-requirements.md)
- **Technical Design**: [current-state/functional-design.md](current-state/functional-design.md)
- **JWT Service-to-Service Auth**: [jwt-service-to-service-auth.md](jwt-service-to-service-auth.md)
- **User Flows**: [current-state/user-flows.md](current-state/user-flows.md)
- **Test Coverage**: [current-state/test-coverage.md](current-state/test-coverage.md)
- **BFF Implementation Summary**: [/session-work/2025-09-12/bff-authentication-implementation-summary.md](/session-work/2025-09-12/bff-authentication-implementation-summary.md)
- **Active Work**: [new-work/status.md](new-work/status.md)

## Key Concepts
- **BFF Authentication**: httpOnly cookies with automatic token refresh
- **Zero localStorage Exposure**: No JWT tokens accessible to JavaScript (XSS protection)
- **Silent Token Refresh**: Prevents authentication timeouts and interruptions
- **Role-Based Access**: Administrator, Member, EventOrganizer, etc.
- **Vetting System**: Members must be vetted for social event access
- **Age Verification**: 21+ requirement enforced
- **Scene Names**: Public display names for privacy

## Critical Implementation Notes

### 🔐 BFF Authentication Pattern Implementation
**NEW (September 2025)**: Complete migration to BFF pattern with httpOnly cookies addresses authentication timeout issues:
- **No localStorage**: JWT tokens never exposed to JavaScript (XSS protection)
- **httpOnly Cookies**: Automatic inclusion in all API requests
- **Silent Refresh**: `/api/auth/refresh` endpoint prevents authentication interruptions
- **Backwards Compatibility**: Dual authentication support (Bearer + Cookie) maintained
- **Complete implementation guide**: [/session-work/2025-09-12/bff-authentication-implementation-summary.md](/session-work/2025-09-12/bff-authentication-implementation-summary.md)

### 🚨 ARCHIVED: Legacy localStorage Pattern
**Security Risk Eliminated**: localStorage JWT token pattern archived due to:
- **XSS Vulnerability**: Tokens accessible to malicious scripts
- **Authentication Timeouts**: No automatic refresh causing frequent logouts
- **Archive Location**: `/docs/_archive/authentication-localstorage-legacy-2025-09-12/`

### Current Features
✅ **BFF Authentication**: httpOnly cookies with silent refresh (September 2025)
✅ **Enhanced Security**: XSS and CSRF protection via httpOnly cookies
✅ **Zero Authentication Timeouts**: Silent token refresh prevents interruptions
✅ Email/password login (React and API)  
✅ User registration with scene names  
✅ JWT authentication for API (via cookies)
✅ Role-based authorization  
✅ Account lockout protection  
✅ Multi-tab session synchronization

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