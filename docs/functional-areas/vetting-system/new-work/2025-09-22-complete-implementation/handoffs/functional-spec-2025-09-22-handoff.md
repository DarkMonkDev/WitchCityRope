# Functional Specification Handoff Document
<!-- Created: 2025-09-22 -->
<!-- Author: Business Requirements Agent -->
<!-- Target: Development Teams -->
<!-- Phase: Requirements → Implementation -->

## Handoff Summary

**Document Created**: Complete functional specification for WitchCityRope Vetting System
**Location**: `/docs/functional-areas/vetting-system/new-work/2025-09-22-complete-implementation/requirements/functional-specification.md`
**Status**: Ready for implementation phase
**Quality Gate**: 95% requirements implementation target established

## Critical Implementation Guidance

### 🚨 MANDATORY READING FOR DEVELOPERS

**Architecture Dependencies**:
1. **READ FIRST**: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md` - Prevents TypeScript errors
2. **NSwag Auto-Generation**: All TypeScript types MUST come from NSwag, never manual interfaces
3. **Existing Patterns**: Follow authentication and API patterns from Events Management implementation

### Component Architecture Summary

**Frontend Structure**: 15 React components specified with complete props interfaces
**API Structure**: 12 endpoints with full request/response models
**Database Integration**: Entity Framework patterns following existing User management
**Email Integration**: SendGrid service with template management

## Key Technical Decisions Made

### 1. React Component Hierarchy
- **Primary Components**: 6 main components (Form, Grid, Detail, Templates, Dashboard, Bulk)
- **Shared Components**: 3 reusable components (StatusBadge, NotesTimeline, EmailEditor)
- **Technology Stack**: React 18 + TypeScript + TanStack Query v5 + Mantine v7

### 2. API Design Patterns
- **Minimal API**: Following established `/apps/api/Features/` structure
- **Authentication**: Existing httpOnly cookie system
- **Response Models**: Comprehensive DTOs for NSwag generation
- **Error Handling**: Structured error responses with proper HTTP codes

### 3. Database Schema Extensions
- **New Entities**: VettingApplication, VettingEmailTemplate, VettingAuditLog
- **Relationships**: Foreign key to existing User entity
- **Audit Trail**: Complete status change history with admin tracking

### 4. Email Service Integration
- **SendGrid Integration**: Templates stored in database, SendGrid for delivery
- **Template Variables**: Standardized replacement system
- **Delivery Tracking**: Full audit trail of email attempts and results

## Critical Business Rules for Implementation

### Application Submission Rules
1. **One Application Per User**: Enforce at database and API level
2. **Single Session Submission**: No draft functionality required
3. **Immediate Email Confirmation**: Must send within 30 seconds
4. **Scene Name Uniqueness**: Validate against existing approved members

### Status Transition Rules
```
Valid Transitions:
Submitted → Under Review
Under Review → Interview Approved | On Hold | Denied
Interview Approved → Interview Scheduled | Approved | On Hold | Denied
Interview Scheduled → Approved | On Hold | Denied
Approved → On Hold (admin correction only)
On Hold → Under Review | Interview Approved | Denied
Denied → [Final State]
```

### Admin Access Rules
- **Admin Role Required**: All review and management functions
- **Audit Logging**: Every admin action must be logged
- **Email Triggers**: Every status change sends appropriate template

## UI/UX Implementation Notes

### Design System Compliance
- **Design System**: v7 with burgundy/plum color scheme established
- **Component Patterns**: Floating labels, signature corner morphing buttons
- **Responsive Design**: Mobile-first approach with grid breakpoints
- **Accessibility**: WCAG 2.1 AA compliance required

### Approved UI Mockups
- **Location**: `/docs/functional-areas/vetting-system/new-work/2025-09-22-complete-implementation/design/ui-mockups.html`
- **Components**: 6 complete screen mockups with interactions
- **Mobile Responsive**: Established patterns for mobile breakpoints

## Performance and Security Requirements

### Performance Targets
- **Application Submission**: < 3 seconds
- **Admin Grid Loading**: < 2 seconds
- **Status Updates**: < 1 second
- **Bulk Operations**: < 10 seconds for 100 applications

### Security Requirements
- **PII Encryption**: Real names and contact info encrypted at rest
- **Role-Based Access**: Strict enforcement at API level
- **Audit Trail**: Permanent retention of all admin actions
- **Session Management**: Standard platform authentication patterns

## Testing Requirements

### Test Coverage Targets
- **Unit Tests**: > 80% coverage required
- **Integration Tests**: All API endpoints with TestContainers
- **E2E Tests**: Complete user workflows with Playwright
- **Performance Tests**: Load testing for bulk operations

### Critical Test Scenarios
1. **Application Submission Flow**: End-to-end with email confirmation
2. **Admin Status Change Workflow**: All valid transitions
3. **Bulk Operations**: Reminder emails and status changes
4. **Email Template Management**: CRUD operations
5. **Error Handling**: Network failures, validation errors, permission denials

## Integration Points for Developers

### Frontend Integration
- **Authentication**: Use existing Zustand auth store
- **Navigation**: Integrate with React Router v7 patterns
- **Dashboard**: Add widget to existing dashboard layout
- **Forms**: Follow Mantine + Zod validation patterns established

### Backend Integration
- **Database**: Extend existing Entity Framework context
- **Email Service**: Use established service injection patterns
- **Logging**: Follow existing structured logging approach
- **Validation**: Use FluentValidation patterns from Events API

## Implementation Phases

### Phase 1: Core Application Flow (Week 1)
- **Priority**: Application form + submission + email confirmation
- **Components**: VettingApplicationForm, basic API endpoints
- **Validation**: Form submission working end-to-end

### Phase 2: Admin Management (Week 2)
- **Priority**: Admin review grid + status changes + email templates
- **Components**: VettingAdminGrid, VettingApplicationDetail, VettingEmailTemplates
- **Validation**: Complete admin workflow operational

### Phase 3: Advanced Features (Week 3)
- **Priority**: Bulk operations + dashboard integration + polish
- **Components**: VettingBulkOperations, VettingDashboardWidget
- **Validation**: All performance targets met, mobile responsive

## Known Risks and Mitigation

### High Risk Items
1. **Database Schema Sync**: Existing migration issues must be resolved first
   - **Mitigation**: Prioritize schema alignment before implementation starts
2. **SendGrid Integration**: Email delivery critical for user experience
   - **Mitigation**: Implement mock service for testing, graceful error handling

### Medium Risk Items
1. **Bulk Operation Performance**: Large datasets may impact UI responsiveness
   - **Mitigation**: Implement progress indicators, async processing
2. **NSwag Type Generation**: Complex DTOs may require iteration
   - **Mitigation**: Start with simple models, iterate based on frontend needs

## Quality Gates for Implementation

### Week 1 Gate (Core Functionality)
- [ ] Application form loads and validates correctly
- [ ] API endpoints accept and process submissions
- [ ] Email confirmation sends successfully
- [ ] Dashboard widget displays application status

### Week 2 Gate (Admin Workflow)
- [ ] Admin grid loads applications with proper filtering
- [ ] Status changes trigger email notifications
- [ ] Email templates can be edited and saved
- [ ] Audit trail records all admin actions

### Week 3 Gate (Production Readiness)
- [ ] Bulk operations complete successfully
- [ ] Mobile responsiveness verified
- [ ] Performance targets met under load
- [ ] Security requirements validated

## Questions for Implementation Teams

### Frontend Development
- [ ] Any conflicts with existing Mantine v7 patterns?
- [ ] TanStack Query cache strategy aligned with Events implementation?
- [ ] Mobile responsiveness patterns consistent with dashboard?

### Backend Development
- [ ] Entity Framework migration strategy for new tables?
- [ ] SendGrid API key configuration approach?
- [ ] Bulk operation performance optimization needs?

### Testing Team
- [ ] TestContainers setup for vetting-specific tests?
- [ ] Email testing strategy for SendGrid integration?
- [ ] Performance testing tools available for bulk operations?

## Success Criteria

### Functional Success
- **Member Experience**: Intuitive application process with clear status updates
- **Admin Efficiency**: 50% reduction in manual communication tasks
- **Process Consistency**: 100% audit trail for all vetting decisions
- **Email Reliability**: > 95% delivery success rate for notifications

### Technical Success
- **Performance**: All response time targets met
- **Reliability**: < 1% error rate for normal operations
- **Security**: Zero access control violations
- **Maintainability**: Clear code structure following established patterns

## Next Steps

1. **Development Team Review**: Review functional specification and raise questions
2. **Architecture Validation**: Confirm patterns align with existing codebase
3. **Implementation Planning**: Break work into specific development tasks
4. **Testing Strategy**: Finalize test approach for integration testing

## Handoff Checklist

- [x] Complete functional specification created
- [x] All business requirements addressed
- [x] UI mockups referenced and specified
- [x] API endpoints fully defined
- [x] Database schema requirements documented
- [x] Performance targets established
- [x] Security requirements specified
- [x] Testing requirements defined
- [x] Implementation phases outlined
- [x] Risk mitigation strategies provided

**Status**: Ready for implementation phase handoff
**Next Review**: After Phase 1 completion for architecture validation