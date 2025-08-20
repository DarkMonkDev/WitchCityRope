# Witch City Rope Website Development Roadmap

## Overview
This roadmap is optimized for a single developer, focusing on MVP features first and leveraging Claude Code as your AI development assistant throughout the process.

## Phase 1: Discovery & Requirements (1 week)

### 1.1 Core Business Requirements

**MVP Features (Launch First):**
- User registration and authentication (Google + email/password with 2FA)
- Basic member vetting application
- Simple event creation (classes and meetups)
- Ticket purchase and RSVP
- Basic check-in functionality with caching for performance
- Essential admin dashboard

**Post-MVP Features (Add Later):**
- Recurring events
- Advanced waitlist management
- Detailed financial reporting
- Interview scheduling system
- Automated email campaigns

**Claude Can Help With:**
- Generate comprehensive user stories with acceptance criteria
- Create detailed feature specifications from your descriptions
- Identify edge cases and security considerations
- Write privacy policy and terms of service templates
- Generate email notification templates

**Deliverables:**
- ✅ MVP feature list with priorities (see requirements.md)
- ✅ User stories document
- ✅ Basic security and privacy requirements

### 1.2 Quick Competitive Research

**Claude Tasks:**
- Ask Claude to analyze event platforms for best practices
- Request UX patterns for membership-based communities
- Get suggestions for check-in workflow optimizations

**Time-Saving Tip:** Give Claude screenshots of competitor sites and ask for specific feature analysis

## Phase 2: Design & Architecture (2-3 weeks) - IN PROGRESS

### 2.1 Simple Site Architecture

**Core Pages (MVP):**
- Public: Home, Event List, Event Detail, Join Page
- Member: Dashboard, Event List (full), My Events
- Admin: Dashboard, Event Management, Member Applications

**Claude Can Generate:**
- Complete sitemap in visual format
- User flow diagrams as code (Mermaid format)
- Blazor page structure and routing setup
- Responsive navigation component code

### 2.2 Database Design

**Updated Decision:** Using SQLite instead of PostgreSQL for simplicity and cost-effectiveness

**Claude Tasks:**
```
"Generate Entity Framework models for:
- Users (with roles: Guest, Member, Admin, EventStaff)
- Events (with types: Class, Meetup)
- Registrations (Tickets/RSVPs)
- VettingApplications
- Payments
- IncidentReports"
```

**Claude Will Provide:**
- Complete entity classes with relationships
- DbContext configuration
- Migration scripts
- Seed data for testing

### 2.3 UI Design (Simplified Approach)

**Strategy:** Use Syncfusion Blazor components (subscription available) for faster development

**Claude Can Help:**
- Generate component layouts using Syncfusion Blazor
- Create responsive grid systems
- Write CSS for custom styling needs
- Generate dark mode CSS variables

**Completed Wireframes:**
- ✅ Landing page
- ✅ Event list (public/member views)
- ✅ Event detail page
- ✅ Event creation (admin)
- ✅ Vetting application form
- ✅ User dashboard (4 member states)
- ✅ Admin vetting queue
- ✅ Admin vetting review
- ✅ Event check-in interface
- ✅ Check-in modal
- ✅ Admin events management

**Remaining Wireframes:**
- 🔲 Login/Register page
- 🔲 2FA setup and entry screens
- 🔲 Password reset flow
- 🔲 My Tickets page
- 🔲 User Profile/Settings
- 🔲 Error pages (404, 403, 500)
- 🔲 Anonymous incident report
- 🔲 Admin member management
- 🔲 Admin financial reports
- 🔲 Admin safety/incidents

**Additional Design Tasks:**
- 🔲 Finalize landing page copy
- 🔲 Create component mapping to Syncfusion
- 🔲 Document color scheme and typography
- 🔲 Create mobile navigation patterns

## Phase 3: Development Environment Setup (2-3 days) - PENDING DESIGN COMPLETION

### 3.1 Project Structure

**Updated Structure (Vertical Slice Architecture):**
```
/src
  /WitchCityRope.Web (Blazor Server entry point)
  /Features
    /Vetting (application, review, approval)
    /Events (create, register, manage)
    /CheckIn (event day operations)
    /Payments (PayPal integration)
    /Safety (incident reporting)
  /Data (EF Core context and entities)
  /Pages (Blazor UI components)
/tests
  /Features (mirrors src structure)
```

### 3.2 Initial Configuration

**Claude Tasks:**
- Generate appsettings.json structure
- Create authentication setup code
- PayPal SDK integration boilerplate
- Email service configuration (SendGrid)

## Phase 4: MVP Implementation (6-8 weeks) - STARTS AFTER DESIGN COMPLETE

### Sprint 1: Foundation (Weeks 1-2)
**Features:**
- User authentication (Google OAuth + local with 2FA)
- Basic user profiles and roles
- Database setup and migrations

**Claude Assistance:**
- Generate complete authentication flow code
- Create user management services
- Write unit tests for auth logic
- Generate authorization policies

### Sprint 2: Vetting System (Weeks 3-4)
**Features:**
- Vetting application form
- Admin review dashboard
- Approval/denial workflow
- Email notifications

**Claude Can Generate:**
- Application form with validation
- Admin review interface
- Notification service (using SendGrid)
- Application status tracking

### Sprint 3: Events Core (Weeks 5-6)
**Features:**
- Event CRUD operations
- Event listing and detail pages
- Registration/RSVP functionality
- Basic admin event management

**Claude Can Generate:**
- Event service layer with business logic
- Blazor components for event display
- Registration workflow
- Capacity and waitlist management with cached availability

### Sprint 4: Payments & Check-in (Weeks 7-8)
**Features:**
- PayPal integration
- Ticket purchase flow
- Check-in interface with IMemoryCache for attendee lists
- Basic reporting

**Claude Tasks:**
- PayPal integration service code
- Payment status tracking
- Mobile-optimized check-in UI
- Transaction logging

## Phase 5: Testing & Polish (1-2 weeks)

### 5.1 Testing Strategy (Simplified)
**Focus Areas:**
- Critical path testing (registration → vetting → event booking → check-in)
- Payment flow testing
- Admin functionality verification
- Mobile responsiveness testing

**Claude Can Generate:**
- Unit test templates for services
- Integration test scenarios
- Manual testing checklists
- Bug report templates

### 5.2 Pre-Launch Tasks
**Claude Assistance:**
- Generate deployment scripts
- Create user documentation
- Write admin guide
- Generate FAQ content

## Phase 6: Launch & Iteration (Ongoing)

### 6.1 Soft Launch Strategy
1. Deploy to production (low-cost VPS)
2. Invite 10-20 trusted members for beta testing
3. Gather feedback and fix critical issues
4. Gradual rollout to full membership

### 6.2 Post-MVP Features Priority
1. **Waitlist Management** (Month 2)
2. **Recurring Events** (Month 2-3)
3. **Advanced Reporting** (Month 3)
4. **Teacher Profiles** (Month 3-4)
5. **Interview Scheduling** (Month 4+)

## Development Workflow with Claude

### Daily Development Pattern

**Morning Planning:**
- Ask Claude to break down today's feature into tasks
- Generate test cases for what you'll build

**Coding Sessions:**
Use Claude for:
- Writing service methods
- Creating Blazor components
- Solving specific technical problems
- Refactoring suggestions

**End of Day:**
- Have Claude review your code for improvements
- Generate documentation for completed features
- Create tomorrow's task list

### Efficient Claude Prompts

**For Architecture:**
```
"Create a service class for [feature] following vertical slice architecture with these methods: [list]. Include error handling and logging."
```

**For UI Components:**
```
"Create a Blazor component using Syncfusion for [feature]. Include loading states, error handling, and mobile responsiveness."
```

**For Business Logic:**
```
"Write the business logic for [process] including validation rules: [rules]. Consider these edge cases: [cases]."
```

**For Testing:**
```
"Generate xUnit tests for [class/method] covering happy path and these edge cases: [list]."
```

## Technical Decisions for Solo Development

### Recommended Stack (Updated)
- **Framework:** ASP.NET 9 with Blazor Server
- **Database:** SQLite (simpler than PostgreSQL for MVP)
- **UI Library:** Syncfusion Blazor components
- **Payments:** PayPal Checkout SDK
- **Logging:** Built-in ILogger (simpler than Serilog for MVP)
- **Caching:** IMemoryCache for session data and frequently accessed items
- **Validation:** Data Annotations + custom validators
- **Architecture:** Simplified vertical slice with direct service injection
- **Email:** SendGrid for transactional emails and notifications

### Security Essentials
**Claude Can Generate:**
- OWASP security checklist adapted for your app
- Input validation rules
- XSS prevention code
- SQL injection prevention patterns
- Secure payment handling guidelines

## Time-Saving Tips

1. **Template Everything:** Have Claude create templates for common patterns (features, components, tests)

2. **Batch Similar Tasks:** Do all CRUD operations at once, all forms at once, etc.

3. **Use Claude for Boilerplate:**
   - Entity configurations
   - Validation rules
   - Email templates
   - Test fixtures

4. **Progressive Enhancement:** Launch with basic features and add complexity based on user feedback

5. **Documentation as You Go:** Have Claude generate documentation from your code comments

## Success Metrics (Simplified)

- **MVP Launch:** 8-10 weeks from start
- **Core Features Working:** Auth, Vetting, Events, Payments, Check-in
- **Initial User Feedback:** Positive from 20+ beta users
- **Technical Debt:** Keep under 20% of codebase
- **Performance:** Page loads under 2 seconds

## Next Steps

- ✅ **Completed:** Create GitHub repo and initial project structure
- ✅ **Completed:** Reorganize design documents and create comprehensive user flows
- **Current Week:** Complete remaining wireframes (authentication, member pages, admin screens)
- **Next Week:** Finalize all design documents and copy
- **Following Week:** Set up development environment with Syncfusion and begin Sprint 1

## Claude Prompt Library for This Project

Save these prompts for repeated use:

1. "Generate a complete Blazor component for [feature] with Syncfusion, including form validation and error handling"

2. "Create an Entity Framework entity for [model] with appropriate relationships, indexes, and configurations for SQLite"

3. "Write a service class for [feature] with methods, validation, and tests"

4. "Generate unit tests for [class] covering all public methods and edge cases"

5. "Create a database migration script for [feature] with rollback capability"

6. "Review this code for SOLID principles and suggest improvements: [code]"

7. "Generate API documentation for this endpoint: [endpoint details]"

8. "Create a mobile-responsive check-in interface using Syncfusion Blazor"

## Architecture Changes from Original Plan

1. **Database:** SQLite for MVP (simpler than PostgreSQL, no hosting costs, can migrate later if needed)
2. **Architecture:** Vertical slice instead of layered (better feature cohesion)
3. **Deployment:** Single container on low-cost VPS instead of complex cloud setup
4. **Patterns:** Direct DbContext usage instead of repository pattern (simpler for small app)

## Risk Mitigation

- **PayPal Integration:** Start with sandbox early in Sprint 3
- **Performance:** SQLite is fine for 600 users, monitor concurrent writes
- **Caching:** Use IMemoryCache to reduce database queries for frequently accessed data
- **Security:** 2FA mandatory, encrypted PII, regular backups
- **Scalability:** Can migrate to PostgreSQL later if needed