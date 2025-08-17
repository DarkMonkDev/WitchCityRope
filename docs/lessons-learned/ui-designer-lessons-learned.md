# UI Designer Lessons Learned
<!-- Last Updated: 2025-08-17 -->
<!-- Next Review: 2025-09-17 -->

## Overview
This document captures UI design lessons learned for the UI Designer agent role, including wireframe standards, design patterns, component specifications, and accessibility considerations. These lessons apply to design work that supports React component development and modern web applications.

## CRITICAL: UI Design First in Phase 2
**Issue**: UI design was happening after other technical designs
**Solution**: UI design must happen FIRST in Phase 2
- **Sequence**: UI Design → Human Review → Other Designs
- **Rationale**: Design changes can influence technical requirements
- **Impact**: Functional specifications may need updates based on UI
- **Human Review**: MANDATORY after UI design completion
- **Implementation**: ALWAYS create clickable mockups when possible
**Applies to**: All Phase 2 design work in orchestrated workflows

## Wireframe Standards

### File Organization
**Issue**: Wireframes scattered across multiple folders  
**Solution**: Keep wireframes with their functional area
```
functional-areas/
└── events-management/
    ├── wireframes/           # HTML wireframe files
    │   ├── event-list.html
    │   └── event-detail.html
    └── current-state/
        └── wireframes.md     # Documentation about wireframes
```
**Applies to**: All new wireframe creation

### Naming Conventions
**Issue**: Inconsistent file names making them hard to find  
**Solution**: Use descriptive, hyphenated names
```
✅ CORRECT:
- user-dashboard-overview.html
- event-creation-form.html
- admin-vetting-review.html

❌ WRONG:
- dashboard.html
- new_event.html
- AdminVetting.html
```
**Applies to**: All wireframe files

## Design Patterns

### Mobile-First Approach
**Issue**: Wireframes only showing desktop view  
**Solution**: Always design mobile view first
```html
<!-- Include viewport meta tag -->
<meta name="viewport" content="width=device-width, initial-scale=1.0">

<!-- Use responsive classes -->
<div class="container">
  <div class="row">
    <div class="col-12 col-md-6">
      <!-- Content -->
    </div>
  </div>
</div>
```
**Applies to**: All new wireframes

### Component Consistency
**Issue**: Same UI element designed differently across pages  
**Solution**: Reference the component library
- Check `standards-processes/ui-components/` for existing components
- Use consistent button styles, form layouts, etc.
- Don't reinvent existing patterns
- **Phase 2 Priority**: Complete UI design BEFORE other technical designs
- **Human Review**: MUST pause after UI completion for approval

### Accessibility Considerations
**Issue**: Wireframes missing accessibility features  
**Solution**: Include accessibility annotations
```html
<!-- Add aria labels and roles -->
<button aria-label="Close dialog" role="button">×</button>

<!-- Include skip navigation -->
<a href="#main-content" class="skip-link">Skip to main content</a>

<!-- Annotate color contrast requirements -->
<!-- Note: Text must have 4.5:1 contrast ratio -->
```
**Applies to**: All interactive elements

## OAuth and Authentication UX Patterns

### OAuth Provider Selection (Research: Aug 2025)
**Issue**: Need to choose OAuth provider for small community sites  
**Solution**: Clerk is optimal for WitchCityRope's needs
- **Best for small communities**: Clerk ($550/month for 10k MAU)
- **Quick implementation**: Under 5 minutes for basic setup
- **Professional appearance**: Beautiful pre-built components
- **Avoid**: Custom OAuth (security complexity), Auth0 (too expensive)

### OAuth Button Design
**Pattern**: Follow 2024-2025 UX best practices
```jsx
// OAuth Button Layout
<VStack spacing={3}>
  <Button
    width="100%"
    leftIcon={<FaGoogle />}
    onClick={() => signInWithOAuth('google')}
    colorScheme="red"
    variant="outline"
  >
    Continue with Google
  </Button>
  <Divider />
  <Text fontSize="sm" color="gray.500">OR</Text>
  <Input placeholder="Email" />
  <Input type="password" placeholder="Password" />
</VStack>
```

### Mobile OAuth Considerations
**Issue**: OAuth flows need mobile optimization  
**Solution**: Thumb zone optimization
- Place OAuth buttons in bottom 1/3 of screen
- Use full-width buttons on mobile
- Implement one-handed use patterns
- 53% of users abandon sites taking >3 seconds to load

### Age Verification for Adult Sites
**Critical**: COPPA compliance requirements (effective June 2025)  
**Solution**: Cannot collect personal info before age determination
```jsx
const handleOAuthSuccess = async (userData) => {
  // Check if OAuth provider includes age/birthdate
  if (userData.ageVerified && userData.age >= 18) {
    await registerUser(userData);
  } else {
    router.push('/verify-age');
  }
};
```

### Session Management UX
**Pattern**: "Remember me" with refresh tokens
- Access tokens: 15 minutes
- Refresh tokens: 30 days  
- Remember me: 90 days
- Always validate server-side

## Docker Architecture Design Patterns (August 2025)

### Container Architecture Diagrams
**Issue**: Complex Docker setups need clear visual representation  
**Solution**: Use ASCII art with container communication flows
```
┌─────────────────┐  HTTP Cookies   ┌─────────────────┐   JWT Bearer    ┌─────────────────┐
│  React SPA      │ ─────────────► │  Web Service    │ ─────────────► │  API Service    │
│  (Port 5173)    │ ◄───────────── │  (Port 5655)    │ ◄───────────── │  (Port 8080)    │
└─────────────────┘                └─────────────────┘                └─────────────────┘
```
**Best Practice**: Include port mappings, container names, and communication protocols

### Multi-Environment Strategy Visualization
**Issue**: Complex docker-compose override patterns hard to understand  
**Solution**: Layer-based documentation with inheritance diagrams
```
Base (docker-compose.yml)
├── Development (docker-compose.dev.yml)
├── Test (docker-compose.test.yml)
└── Production (docker-compose.prod.yml)
```
**Pattern**: Show configuration matrix comparing environments

### Service Communication Flow Design
**Issue**: Container networking can be confusing for developers  
**Solution**: Sequence diagrams showing authentication flows
- Show external browser → container communication
- Highlight internal container-to-container calls
- Document service discovery patterns
- Include health check dependencies

### Developer Workflow Visualization
**Issue**: Complex Docker development setup intimidates developers  
**Solution**: Step-by-step workflow diagrams
- Daily development workflow from startup to commit
- Hot reload flow showing file changes → container updates
- Debugging workflow with IDE → container connections
- Testing workflow with container orchestration

### Docker Design Documentation Standards
**File Structure**: Place Docker design docs in functional area
```
/docs/functional-areas/docker-authentication/design/
├── docker-architecture-diagram.md
├── environment-strategy.md
├── service-communication-design.md
└── developer-workflow.md
```

**Content Requirements**:
- ASCII diagrams for all architectures
- Step-by-step sequences with timing estimates
- Environment comparison matrices
- Troubleshooting decision trees
- Configuration examples with explanations

## WitchCityRope Specific Patterns

### Event Display Pattern
**Standard layout for events**:
1. Event image/banner (optional)
2. Title and scene name
3. Date, time, location
4. Price and ticket availability
5. Description
6. RSVP/Buy button

### User Dashboard Sections
**Standard organization**:
1. Welcome message with user name
2. Upcoming events (RSVPs and tickets)
3. Quick actions (browse events, update profile)
4. Recent activity
5. Settings link

### Form Layouts
**Consistent form structure**:
1. Clear heading
2. Required field indicators (*)
3. Field grouping with sections
4. Help text under fields
5. Clear primary action button
6. Cancel/back option

### Authentication Flow for Adult Community
**Privacy-first approach**:
1. OAuth login with age verification
2. Default profile to private
3. Explicit consent for photo sharing
4. Role assignment (start as 'guest')
5. Clear privacy controls

## Integration with Development

### Data Attributes
**Issue**: Developers can't identify elements from wireframes  
**Solution**: Add data-testid attributes
```html
<button data-testid="submit-event">Create Event</button>
<div data-testid="event-list">
  <div data-testid="event-card">...</div>
</div>
```
**Applies to**: All interactive elements

### State Representations
**Issue**: Only showing happy path  
**Solution**: Design all states
- Empty states (no data)
- Loading states (critical for OAuth flows)
- Error states with recovery options
- Success messages
- Validation errors

### Error Handling for OAuth
**Pattern**: Clear error communication with recovery
```jsx
<Alert status="error">
  <AlertIcon />
  <AlertTitle>Google Login Failed</AlertTitle>
  <AlertDescription>
    Please check your Google account permissions and try again.
    <Link href="/login/email">Use email/password instead</Link>
  </AlertDescription>
</Alert>
```

### Responsive Breakpoints
**Issue**: Unclear how design adapts  
**Solution**: Document breakpoints
```html
<!-- Add comments for breakpoints -->
<!-- Mobile: < 768px -->
<!-- Tablet: 768px - 1024px -->
<!-- Desktop: > 1024px -->
```

## Tools and Resources

### Recommended Tools
- **HTML/CSS**: For interactive wireframes
- **Comments**: Explain interactions and flows
- **Browser DevTools**: Test responsive design

### Style References
- Brand colors: See `standards-processes/ui-components/design-tokens.json`
- Typography: Check style guide
- Spacing: Use consistent 8px grid system

### OAuth Design Resources
- **Clerk**: Pre-built components with Salem theme customization
- **Loading states**: Chakra UI Spinner, Skeleton components
- **Error patterns**: Alert, Toast components for OAuth failures

### Validation Checklist
Before submitting wireframes:
- [ ] Mobile view included
- [ ] All states represented (loading, error, success)
- [ ] OAuth flow documented
- [ ] Age verification considered
- [ ] Data-testid attributes added
- [ ] Accessibility considered
- [ ] Consistent with existing patterns
- [ ] File properly named and located
- [ ] **PHASE 2 SEQUENCING**: UI design completed FIRST (before other designs)
- [ ] **HUMAN REVIEW**: Ready for mandatory UI approval checkpoint
- [ ] **CLICKABLE MOCKUPS**: Created when possible for stakeholder review

## Common Mistakes to Avoid

1. **Creating in isolation** - Always check existing components first
2. **Desktop-only thinking** - Mobile users are significant
3. **Perfect pixel designs** - Focus on layout and flow
4. **Missing edge cases** - What if OAuth fails? Network issues?
5. **Ignoring accessibility** - Consider keyboard navigation, screen readers
6. **Skipping loading states** - OAuth flows have significant wait times
7. **Custom OAuth implementation** - Use proven providers like Clerk
8. **Complex Docker diagrams** - Keep ASCII art simple and focused
9. **Missing container communication** - Show internal vs external traffic
10. **Incomplete environment coverage** - Document dev, test, and production

## Handoff Process

### What Developers Need
1. **Layout structure** - How components are arranged
2. **Interaction notes** - What happens on click/hover/OAuth flow
3. **Data requirements** - What information is displayed
4. **State variations** - Different views based on conditions
5. **OAuth flow details** - Provider configuration, error handling
6. **Age verification** - Compliance requirements and UX flow
7. **Container architecture** - Service communication patterns
8. **Environment configuration** - Multi-environment setup requirements

### Documentation Format
Create a companion `.md` file explaining:
- User flow through the screens
- OAuth provider integration notes
- Business logic notes
- Age verification requirements
- Questions or assumptions
- Links to related requirements
- Docker deployment considerations
- Container communication requirements

---

*Remember: Wireframes are communication tools. Clear annotations and consistency are more valuable than pixel perfection. For OAuth flows, prioritize security and compliance over complexity. For Docker designs, focus on developer experience and clear service communication patterns.*