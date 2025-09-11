# UI Designer Lessons Learned

## Design System v7 Implementation - August 2025

### CRITICAL: Design System v7 Color Enforcement
**Problem**: Previous wireframes used incorrect dark color scheme instead of approved Design System v7
**Solution**: 
- Use EXACT colors from approved design system:
  - Primary: #880124 (burgundy)
  - Accent: #B76D75 (rose-gold)
  - CTA Primary: #FFBF00 (amber)
  - CTA Secondary: #9D4EDD (electric purple)
  - Background: #FAF6F2 (cream)
  - Cards: #FFF8F0 (ivory)
- Always copy header/footer exactly from `/docs/design/current/homepage-template-v7.html`
- Apply signature animations consistently across all pages

### MANDATORY: Floating Label Animation for ALL Form Inputs
**Critical Requirement**: EVERY form input MUST use floating label animation
**Implementation**: Label starts inside input field, moves to above on focus/input
**Pattern**:
```css
.floating-input {
    padding: var(--space-md) var(--space-sm) var(--space-xs) var(--space-sm);
}
.floating-label {
    position: absolute;
    left: var(--space-sm);
    top: 50%;
    transform: translateY(-50%);
    transition: all 0.3s ease;
}
.floating-input:focus + .floating-label,
.floating-input:not(:placeholder-shown) + .floating-label,
.floating-input.has-value + .floating-label {
    top: -2px;
    transform: translateY(-50%) scale(0.8);
    color: var(--color-burgundy);
    background: var(--color-cream);
}
```
**Exception**: Only use standard inputs for dropdowns/selects that can't support floating labels

### Typography Implementation
**Rule**: Use exact font hierarchy from Design System v7:
- Headlines: 'Bodoni Moda', serif
- Titles/Nav: 'Montserrat', sans-serif  
- Body: 'Source Sans 3', sans-serif
- Taglines: 'Satisfy', cursive

### Button Styling - Signature Corner Morphing
**Implementation**: 
- Default: `border-radius: 12px 6px 12px 6px` (asymmetric)
- Hover: `border-radius: 6px 12px 6px 12px` (reverse)
- NO vertical movement (no translateY)
- Primary buttons use amber gradient (#FFBF00 to #DAA520)
- Secondary buttons use burgundy border
- NO PURPLE BUTTONS - Only amber/burgundy colors allowed

### Rich Text Editor Standard
**Required**: Use Tiptap v2 (not basic @mantine/tiptap)
**Features Must Include**:
- Bold, Italic, Headings
- Bullet/Numbered lists
- Tables support
- Image insertion
- Link insertion  
- Source code view
- Quote blocks

### Navigation Animations - Center Outward Underline
**Implementation**:
```css
.nav-item::after {
    content: '';
    position: absolute;
    bottom: -4px;
    left: 50%;
    transform: translateX(-50%);
    width: 0;
    height: 2px;
    background: linear-gradient(90deg, transparent, var(--color-burgundy), transparent);
    transition: width 0.3s ease;
}
.nav-item:hover::after {
    width: 100%;
}
```

### Form Input Styling with Animations
**Standard Pattern**:
- Rose-gold border by default
- Focus: burgundy border + rose-gold glow + translateY(-2px)
- Rounded corners (12px)
- Cream background
- Stone placeholder text

### Preserving Existing Designs
**Rule**: When asked NOT to redesign (like check-in interface), preserve:
- Original functionality and structure
- All existing tabs/sections
- Layout patterns
- Only update colors, typography, and animations to match v7

### Mobile Responsiveness Standards
**Breakpoint**: 768px
**Pattern**:
- Hide desktop nav, show mobile-friendly layout
- Stack grid layouts to single column
- Adjust padding from 40px to 20px
- Scale fonts appropriately
- Maintain signature animations on mobile

## Events Management Specific Patterns

### Event Cards - Hover Animation
**Implementation**: `translateY(-4px)` on hover with increased shadow
**Used in**: public-events-list.html, admin dashboard

### Check-in Interface - Status Indicators
**Pattern**: Use design system colors for status:
- Success: #228B22 (checked-in)
- Warning: #DAA520 (waitlist)
- Error: #DC143C (issues)

### Form Sections - Hierarchical Styling
**Pattern**: 
- Section titles with rose-gold bottom border
- Grouped form elements in ivory containers
- Proper spacing hierarchy using CSS variables

### Event Session Matrix Pattern - Complex Ticket Management
**Problem**: Need to support multi-day events with different ticket types (series passes, individual days, etc.)
**Solution**: Event Session Matrix approach implemented in event-form.html
**Key Components**:

1. **Event Sessions Section**: Define individual sessions (Day 1, Day 2, etc.)
   - Session cards with date, time, capacity
   - Dynamic add/remove functionality
   - Editable session names (e.g., "Day 1: Fundamentals")

2. **Ticket Types Section**: Multiple ticket types referencing sessions
   - Series passes include multiple sessions
   - Individual session tickets
   - Clear session inclusion display (✓ Day 1 ✓ Day 2)
   - Price ranges, quantities, sale periods

3. **Capacity Overview Sidebar**: Real-time capacity visualization
   - Progress bars for each session
   - Color-coded warnings (burgundy → warning → error)
   - Clear capacity allocation display
   - Sticky positioning for reference

4. **Template Selection**: Quick setup patterns
   - Single Session Event
   - Multi-Day Series  
   - Tiered Pricing

**CSS Classes**:
```css
.session-card, .ticket-type-card {
    background: white;
    border: 2px solid var(--color-rose-gold);
    border-radius: 12px;
    transition: all 0.3s ease;
}
.session-card:hover, .ticket-type-card:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(136, 1, 36, 0.15);
}
.capacity-overview {
    background: var(--color-ivory);
    position: sticky;
    top: var(--space-md);
}
```

**Layout Pattern**:
- Two-column layout: main content + sidebar
- Responsive: stack on mobile
- Grid template: `1fr 300px` → `1fr` on mobile

### Public-Facing Event Session Matrix - Customer Experience
**Problem**: Customers need to understand multi-session events and select appropriate tickets
**Solution**: Smart capacity display and ticket selection interface

**Key Patterns**:

1. **Event Details Page - Registration Card**:
   - **Capacity Warning**: Highlight bottleneck sessions prominently
   - **Session Availability Display**: Show per-session capacity with color coding
   - **Ticket Type Selection**: Interactive cards showing included sessions
   - **Smart Unavailability**: Gray out tickets when constituent sessions are full
   - **Dynamic Button Text**: Update based on selected ticket type

2. **Events List - Smart Spots Display**:
   - **Single Session**: Standard "8/20" format
   - **Multi-Session**: Show bottleneck with context "5/20 (Day 2)"
   - **Card Badges**: Visual indicators for series events
   - **Table View**: Include constraint info in spots column

**CSS Patterns**:
```css
.multi-session-spots {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
}
.spots-constraint {
    font-size: 12px;
    font-style: italic;
    color: var(--color-stone);
}
.ticket-type-option.unavailable {
    opacity: 0.6;
    cursor: not-allowed;
    background: var(--color-stone);
}
.capacity-warning {
    background: linear-gradient(135deg, rgba(220, 20, 60, 0.1), rgba(218, 165, 32, 0.1));
    border: 2px solid var(--color-warning);
}
```

**UX Benefits**:
- Clear understanding of what's included in each ticket
- Immediate visibility of capacity constraints
- Prevents confusion about which sessions are available
- Smart warnings guide purchase decisions

## Quality Validation Checklist

### Pre-Delivery Validation
- [ ] All colors match Design System v7 exactly
- [ ] Header/footer copied from homepage template v7
- [ ] Signature animations implemented (nav underlines, button morphing)
- [ ] Typography uses correct font families
- [ ] ALL form inputs use floating label animation
- [ ] Rich text areas use Tiptap v2 with full toolbar
- [ ] NO purple buttons - only amber/burgundy
- [ ] Input styling matches approved patterns
- [ ] Mobile responsive at 768px breakpoint
- [ ] No vertical button movement (translateY)
- [ ] Rose-gold bottom border on header
- [ ] Consistent spacing using CSS variables

### Animation Requirements
- [ ] Navigation center-outward underline animation
- [ ] Button asymmetric corner morphing (no translateY)
- [ ] Card hover elevation (-4px translateY)
- [ ] Input focus animations (translateY + glow)
- [ ] Logo hover scale + underline
- [ ] Floating labels on ALL text inputs

### Complex Form Validation
- [ ] Event Session Matrix pattern correctly implemented
- [ ] Capacity overview shows real-time calculations
- [ ] Session-ticket relationships clearly displayed
- [ ] Template selection provides appropriate defaults
- [ ] Mobile responsiveness for complex layouts

### Public-Facing Session Matrix Validation
- [ ] Multi-session events show bottleneck sessions clearly
- [ ] Ticket selection interface is intuitive
- [ ] Unavailable tickets are properly disabled with explanations
- [ ] Capacity warnings are prominent and actionable
- [ ] Button text updates based on selection
- [ ] Mobile responsive ticket selection

## File Organization

### Wireframe Storage Pattern
**Location**: `/docs/functional-areas/[feature]/new-work/[date]/design/wireframes/`
**Naming**: Descriptive names like `admin-events-dashboard.html`
**Structure**: Each wireframe is complete standalone HTML with embedded CSS

### Design System Reference
**Source of Truth**: `/docs/design/current/design-system-v7.md`
**Template**: `/docs/design/current/homepage-template-v7.html`
**Always verify against these files before finalizing designs**

## Common Mistakes to Avoid

1. **DON'T** use dark themes - Design System v7 is light with cream backgrounds
2. **DON'T** create custom colors - use exact hex values from design system
3. **DON'T** add vertical button movement - buttons only morph corners
4. **DON'T** skip the utility bar and footer from template
5. **DON'T** forget mobile responsive breakpoints
6. **DON'T** use wrong typography - each element has specific font family
7. **DON'T** redesign when asked to preserve existing functionality
8. **DON'T** use purple buttons - amber and burgundy only
9. **DON'T** skip floating label animations on form inputs
10. **DON'T** use basic text editors - require Tiptap v2 with full features
11. **DON'T** forget sticky positioning for capacity overview sidebars
12. **DON'T** neglect session-ticket relationship clarity in complex forms
13. **DON'T** show generic capacity for multi-session events - always show the constraint
14. **DON'T** allow ticket purchases when constituent sessions are full

## Stakeholder Communication

### Design Approval Process
**Lesson**: Stakeholders were frustrated with incorrect designs that didn't match approved system
**Solution**: Always validate against design system before presenting
**Process**: 
1. Review design system documentation
2. Implement exact colors and patterns
3. Test animations and interactions
4. Validate mobile responsiveness
5. Present with confidence that it matches approved standards

This comprehensive approach ensures all future wireframes will be consistent with the approved Design System v7 and meet stakeholder expectations.