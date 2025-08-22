# Legacy Blazor User Dashboard Designs - REFERENCE ONLY
<!-- Last Updated: 2025-08-22 -->
<!-- Status: LEGACY REFERENCE -->
<!-- Owner: Librarian -->

## ⚠️ IMPORTANT NOTICE

This folder contains **LEGACY BLAZOR SERVER DESIGNS** for historical reference only.

**DO NOT USE THESE DESIGNS FOR REACT DEVELOPMENT**

## Current Active Work

**React User Dashboard Redesign**: `/docs/functional-areas/user-dashboard/new-work/2025-08-22-user-dashboard-redesign/`

## Purpose of This Archive

These legacy designs are preserved for:
- **Historical Reference**: Understanding previous UI decisions
- **Feature Requirements**: Identifying functionality that should be migrated
- **User Flow Insights**: Understanding user expectations from current system
- **Content Organization**: Reference for information architecture

## Migration Status

- ✅ **New Workflow Structure Created**: React-focused development structure established
- 🔄 **Requirements Analysis**: In progress using Design System v7
- ⏳ **UI Redesign**: Planned with modern React patterns
- ⏳ **Implementation**: Using Mantine v7 + TanStack Query + Zustand

## Legacy Files in This Folder

| File | Original Purpose | Migration Notes |
|------|------------------|----------------|
| `member-membership-settings.html` | Membership settings UI | Migrate to React with Mantine forms |
| `member-my-tickets-visual.html` | Event tickets display | Key focus for new dashboard events section |
| `member-profile-settings-visual.html` | Profile management | Reference for user profile patterns |
| `member-security-settings-visual.html` | Security settings | Reference for security section |
| `user-dashboard-visual.html` | Main dashboard layout | **PRIMARY REFERENCE** for redesign |
| `user-menu-wireframes.md` | Navigation wireframes | Reference for left-hand navigation design |

## Key Design Elements to Consider

### From Legacy Designs
1. **Navigation Structure**: Current menu organization patterns
2. **Event Display**: How tickets/RSVPs are currently shown
3. **User Information**: Profile data presentation patterns
4. **Color Scheme**: Existing color choices (to be updated with v7)
5. **Information Hierarchy**: Current content prioritization

### For React Redesign
1. **Design System v7**: Modern color palette and typography
2. **Component Architecture**: Mantine v7 component patterns
3. **Responsive Design**: Mobile-first approach
4. **Performance**: Fast loading with TanStack Query
5. **Accessibility**: WCAG 2.1 AA compliance

## DO NOT

❌ Copy HTML/CSS from these files directly
❌ Use Blazor-specific patterns in React
❌ Assume these designs meet current accessibility standards
❌ Use outdated color schemes or typography
❌ Implement without consulting Design System v7

## DO

✅ Reference for functionality requirements
✅ Use for understanding user workflows
✅ Extract content organization patterns
✅ Identify key features to preserve
✅ Consult for user expectations context

## Questions About Legacy Designs?

For questions about these legacy designs or migration planning:

1. **Technical Questions**: Check with React Developer agents
2. **Design Questions**: Consult UI Designer with v7 system
3. **Business Logic**: Review with Business Requirements agent
4. **Architecture Questions**: Reference authentication milestone patterns

---

**Remember**: This is REFERENCE ONLY. All new development should follow the React workflow in the `new-work/2025-08-22-user-dashboard-redesign/` folder.