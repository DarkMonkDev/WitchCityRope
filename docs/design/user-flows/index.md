# Witch City Rope - User Flows Documentation

This directory contains detailed user flows and information architecture documentation for the Witch City Rope platform. Each document focuses on a specific feature area, making it easier to understand and implement the functionality.

## Documents Overview

### 📍 [Site Map](./site-map.md)
Complete hierarchical structure of all pages and their relationships. Use this to understand the overall site organization and URL structure.

### 🔐 [Authentication Flows](./auth-flows.md)
- Account creation (email and Google OAuth)
- Login process with 2FA
- Password reset
- Session management
- Role-based access control (RBAC)

### 🎓 [Vetting System Flows](./vetting-flows.md)
- Guest application process
- Admin review workflow
- Collaborative review features
- Member status management
- Application state transitions

### 📅 [Event Management Flows](./event-flows.md)
- Event creation (admin)
- Event registration (members)
- Check-in process (staff)
- Event management (edit, cancel, communicate)
- Refund processing
- Event lifecycle states

### 💳 [Payment Flows](./payment-flows.md)
- Fixed price ticket purchase
- Sliding scale implementation
- Free event RSVP
- Refund processing
- Financial reporting
- Volunteer ticket allocation

### 🛡️ [Safety & Incident Flows](./safety-flows.md)
- Anonymous incident reporting
- Safety team review process
- Member status changes (warning, suspension, ban)
- Waiver management
- Check-in safety verification

### 🧭 [Navigation Flows](./navigation-flows.md)
- Navigation structures by user role
- Mobile navigation patterns
- Information architecture
- Search patterns
- Empty states and errors

## How to Use These Documents

### For Developers
1. Start with the [Site Map](./site-map.md) to understand overall structure
2. Reference specific flow documents when implementing features
3. Use the mermaid diagrams to understand state transitions
4. Follow the UI patterns described in navigation flows

### For Designers
1. Use flows to ensure all states are designed
2. Reference navigation patterns for consistency
3. Check empty states and error conditions
4. Validate progressive disclosure patterns

### For Product Owners
1. Review flows to ensure business logic is correct
2. Use state diagrams to understand system behavior
3. Verify all edge cases are covered
4. Check that safety and privacy requirements are met

## Key Design Principles

### 1. Progressive Disclosure
- Public users see limited event information
- Full details revealed after authentication
- Admin features hidden from regular members

### 2. Privacy First
- Scene names used publicly
- Legal names only visible to admins
- Venue addresses hidden until registered
- Anonymous incident reporting

### 3. Mobile Optimization
- Check-in interface is mobile-first
- Bottom navigation for key actions
- Large touch targets throughout
- Simplified layouts on small screens

### 4. Safety Focus
- Incident reporting easily accessible
- Waiver tracking integrated
- Member status affects access
- Clear audit trails

## Related Documentation

- [Wireframes](../wireframes/) - Visual designs for each page
- [Requirements](../requirements.md) - Business requirements
- [Architecture](../../architecture/) - Technical decisions

## Maintenance Notes

When updating these flows:
1. Keep mermaid diagrams in sync with text descriptions
2. Update the site map when adding new pages
3. Document all user paths, including error cases
4. Note any changes to state transitions
5. Update related wireframes if UI changes