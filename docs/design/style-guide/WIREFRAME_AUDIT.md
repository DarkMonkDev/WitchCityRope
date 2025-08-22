# Wireframe Inconsistency Audit

## Summary
This audit identifies all styling inconsistencies found across the 18+ HTML wireframes that need to be standardized.

## Critical Issues to Address

### 1. Navigation Inconsistencies
- **Landing/Event List**: Uses `.nav` with mix of buttons and nav-items
- **Dashboard/Member Pages**: Different structure with user menu
- **Admin Pages**: Completely different sidebar navigation
- **Auth Pages**: Minimal navigation
- **Error Pages**: Simplified navigation

**Resolution**: Create 3 standard navigation patterns:
1. Guest navigation (landing, event list, auth pages)
2. Member navigation (dashboard, profile, my events)
3. Admin navigation (sidebar pattern)

### 2. Button Style Variations
Currently using multiple button styles:
- `.btn-primary`: `#8B4513` (brown)
- `.btn-secondary`: Two different implementations
- `.btn-warning`: Only on dashboard
- Various one-off styles in forms

**Resolution**: Standardize to 4 button types:
- Primary (brown)
- Secondary (outline)
- Warning (orange)
- Danger (red)

### 3. Color Duplication
Multiple values for same semantic colors:
- Orange/Warning: `#ffc107`, `#f57c00`, `#e65100`, `#FF9800`
- Red/Error: `#d32f2f`, `#ff6b6b`
- Success: Consistent at `#2e7d32`

**Resolution**: Define single values for each semantic color

### 4. Typography Scale Issues
- Page titles: 28px, 30px, 32px (inconsistent)
- Section titles: 20px, 22px, 24px (varies)
- No consistent heading hierarchy

**Resolution**: Define strict typography scale

### 5. Spacing Inconsistencies
- Section padding: 60px vs 30-40px
- Card padding: 20px vs 24px
- Button padding variations
- No consistent spacing system

**Resolution**: Implement 4px-based spacing scale

### 6. Border Radius Variations
- Cards: 8px, 10px, 12px
- Buttons: 4px, 6px
- Inputs: 4px, 6px, 8px

**Resolution**: Standardize to 3 sizes: 4px, 6px, 12px

### 7. Form Element Inconsistencies
- Input padding varies
- Border width: 1px vs 2px
- Different focus states

**Resolution**: Create single form control system

## Files Requiring Major Updates

### High Priority (Most Inconsistent):
1. `landing.html` - Non-standard navigation, custom styles
2. `user-dashboard.html` - Mixed button styles
3. `admin-events.html` - Different component patterns
4. Auth pages - Need navigation added

### Medium Priority:
1. Event pages - Card style variations
2. Profile/Settings - Form inconsistencies
3. Error pages - Need full navigation

### Low Priority:
1. Check-in pages - Mostly consistent
2. Vetting pages - Well structured

## Component Inventory

### Components to Standardize:
1. **Navigation Bar** (3 variants)
2. **Buttons** (4 types, 3 sizes)
3. **Cards** (event, member, info)
4. **Forms** (inputs, textareas, selects)
5. **Badges/Pills** (status, event type)
6. **Tables** (admin data display)
7. **Alerts** (success, warning, error, info)
8. **Modals** (standard pattern needed)

### New Components Needed:
1. Loading states
2. Empty states
3. Tooltips
4. Dropdown menus
5. Progress indicators (beyond capacity bars)

## Accessibility Concerns
1. Focus states not defined consistently
2. Color contrast needs verification
3. Form labels and error messages need review
4. Mobile navigation needs improvement

## Responsive Design Gaps
1. Only one breakpoint (768px) currently used
2. Tablet layouts not considered
3. Some components not mobile-optimized

## Next Steps
1. Create design token system
2. Build component library CSS
3. Update each wireframe systematically
4. Test for consistency and accessibility