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