# UI Designer Lessons Learned
<!-- Last Updated: 2025-08-16 -->
<!-- Next Review: 2025-09-16 -->

## Overview
This document captures UI design lessons learned for the UI Designer agent role, including wireframe standards, design patterns, component specifications, and accessibility considerations. These lessons apply to design work that supports React component development and modern web applications.

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
- Loading states
- Error states
- Success messages
- Validation errors

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

### Validation Checklist
Before submitting wireframes:
- [ ] Mobile view included
- [ ] All states represented
- [ ] Data-testid attributes added
- [ ] Accessibility considered
- [ ] Consistent with existing patterns
- [ ] File properly named and located

## Common Mistakes to Avoid

1. **Creating in isolation** - Always check existing components first
2. **Desktop-only thinking** - Mobile users are significant
3. **Perfect pixel designs** - Focus on layout and flow
4. **Missing edge cases** - What if list is empty? Text is long?
5. **Ignoring accessibility** - Consider keyboard navigation, screen readers

## Handoff Process

### What Developers Need
1. **Layout structure** - How components are arranged
2. **Interaction notes** - What happens on click/hover
3. **Data requirements** - What information is displayed
4. **State variations** - Different views based on conditions

### Documentation Format
Create a companion `.md` file explaining:
- User flow through the screens
- Business logic notes
- Questions or assumptions
- Links to related requirements

---

*Remember: Wireframes are communication tools. Clear annotations and consistency are more valuable than pixel perfection.*