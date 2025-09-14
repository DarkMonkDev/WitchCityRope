# Archived Legacy API
<!-- Last Updated: 2025-09-13 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Archived -->

## Archive Information
- **Date Archived**: 2025-09-13
- **Reason**: All features successfully migrated to modern API at `/apps/api/`
- **Migration Status**: COMPLETE - All valuable features extracted and implemented

## ⚠️ DO NOT USE - ARCHIVED CODE ⚠️

**This code is archived for historical reference only.**
**All active development uses the modern API at `/apps/api/`.**

## Archived Projects

### WitchCityRope.Api/
Legacy ASP.NET Core Web API project with complex enterprise patterns.
- **Port**: Was configured for port 5001 (INACTIVE)
- **Status**: SUPERSEDED by simplified modern API
- **Architecture**: MediatR/CQRS pattern (DEPRECATED)

### WitchCityRope.Core/
Legacy business logic layer.
- **Status**: Business logic migrated to modern API services
- **Patterns**: Domain models and interfaces (SUPERSEDED)

### WitchCityRope.Infrastructure/
Legacy data access layer.
- **Status**: Data patterns migrated to modern API with Entity Framework
- **Database**: PostgreSQL connection patterns preserved in modern API

## Migration Timeline
- **August 2025**: React migration began, modern API created
- **September 2025**: Feature extraction completed
- **2025-09-13**: Legacy API officially archived

## Modern API Location
**All new development must use: `/apps/api/`**
- **Port**: 5655 (ACTIVE)
- **Architecture**: Vertical Slice Architecture
- **Performance**: 49ms average response times
- **Features**: All legacy features migrated and enhanced

## Value Extracted
✅ Safety System - Incident reporting with privacy protection
✅ CheckIn System - Event attendee management
✅ Vetting System - Member approval workflow  
✅ Payment System - PayPal/Venmo integration with sliding scale
✅ Dashboard System - User profile and statistics
✅ Authentication - Secure BFF pattern with httpOnly cookies
✅ Events Management - Event creation, RSVP, ticketing

## For Historical Reference Only
This archive is maintained for:
- Code archaeology and learning
- Understanding previous architectural decisions
- Compliance and audit requirements
- Debugging any missed edge cases during migration

**Under NO circumstances should this code be executed, deployed, or developed.**