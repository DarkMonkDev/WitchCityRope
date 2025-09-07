# UI Implementation Standards - WitchCityRope
<!-- Last Updated: 2025-08-25 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Executive Summary
This document establishes the critical UI implementation standards for WitchCityRope React development, emphasizing **wireframes as primary source of truth** and **TinyMCE as the mandatory rich text editor**.

## 🚨 CRITICAL: Wireframes as Primary Source of Truth

### Wireframe Authority
- **PRIMARY SOURCE**: `/docs/design/wireframes/` directory contains THE definitive UI specifications
- **EXTENSIVE INVESTMENT**: Significant time spent refining wireframes to near-final quality
- **NO DEVIATIONS**: Wireframes must be followed exactly unless explicitly approved otherwise
- **IMPLEMENTATION PRIORITY**: Wireframes override all other UI guidance

### Wireframe-First Implementation Process
1. **Locate wireframe** for the page/component being implemented
2. **Study layout** and all interactive elements shown
3. **Match component placement** exactly as designed
4. **Implement all functionality** visible in wireframes
5. **Follow interaction patterns** as demonstrated
6. **Reference wireframe filename** in implementation comments

### Available Wireframes
Located in `/docs/design/wireframes/`:

#### Authentication Flows
- `auth-login-register-visual.html` - Login and registration forms
- `auth-password-reset-visual.html` - Password reset flow
- `auth-2fa-setup-visual.html` - Two-factor authentication setup
- `auth-2fa-entry-visual.html` - Two-factor authentication entry

#### Event Management
- `event-list-visual.html` - Event listing page
- `event-detail-visual.html` - Individual event details
- `event-creation.html` - Event creation form
- `event-checkin-visual.html` - Event check-in interface

#### Admin Pages
- `admin-events-visual.html` - Admin event management
- `admin-vetting-queue.html` - Member vetting queue
- `admin-vetting-review.html` - Individual vetting review

#### User Dashboard
- `user-dashboard-visual.html` - Main user dashboard
- `member-profile-settings-visual.html` - Profile settings
- `member-security-settings-visual.html` - Security settings
- `member-my-tickets-visual.html` - User's event tickets

#### Error Pages
- `error-404-visual.html` - 404 not found page
- `error-403.html` - Forbidden access page
- `error-500.html` - Server error page

### Wireframe Implementation Checklist
- [ ] Wireframe located and reviewed
- [ ] Layout matches wireframe exactly
- [ ] All UI components match placement
- [ ] Interactive elements implemented as shown
- [ ] Visual hierarchy preserved
- [ ] Responsive behavior considered
- [ ] Wireframe filename referenced in code comments

## 🚨 CRITICAL: Rich Text Editor Requirement

### TinyMCE Mandatory Standard
- **EDITOR REQUIRED**: TinyMCE is the ONLY approved rich text editor
- **NO ALTERNATIVES**: Do not use @mantine/tiptap, TipTap, or any other rich text solution
- **ADMIN PAGES ONLY**: Rich text editing is for admin content management
- **BUNDLE SIZE IRRELEVANT**: Admin pages don't have bundle size constraints
- **BASIC VERSION SUFFICIENT**: No premium features needed

### TinyMCE Implementation Requirements
```typescript
// ✅ CORRECT Implementation
import { Editor } from '@tinymce/tinymce-react';

const RichTextEditor = ({ value, onChange }) => {
  return (
    <Editor
      apiKey="your-api-key-here"
      value={value}
      onEditorChange={onChange}
      init={{
        height: 300,
        menubar: false,
        plugins: [
          'link', 'lists', 'textcolor', 'colorpicker', 'fontsize'
        ],
        toolbar: 'bold italic underline | link | bullist numlist | forecolor backcolor | fontsize',
        branding: false,
        statusbar: false
      }}
    />
  );
};

// ❌ FORBIDDEN - Do not use these
// import { RichTextEditor } from '@mantine/tiptap';
// import { useEditor } from '@tiptap/react';
```

### TinyMCE Features Available
- **Basic Formatting**: Bold, italic, underline
- **Lists**: Bulleted and numbered lists
- **Links**: URL link insertion and editing
- **Colors**: Text and background color selection
- **Font Sizes**: Text size adjustment
- **Clean HTML Output**: Semantic markup generation

### Why TinyMCE Over Alternatives
1. **Industry Standard**: Used by 1.5M+ developers globally
2. **Enterprise Adoption**: Trusted by Atlassian, Medium, Evernote
3. **Feature Complete**: 35+ plugins available out of the box
4. **Professional Grade**: What major companies use for content management
5. **Proven Reliability**: Enterprise-grade stability and performance
6. **Admin Context Perfect**: Ideal for admin content management workflows

## Component Consistency Standards

### Mantine Component Usage
- **USE standardized components** from project's component library first
- **CHECK existing patterns** before creating new components
- **FOLLOW design system** colors and spacing defined in Design System v7
- **AVOID inline styles** - use CSS classes and Mantine's styling system

### Form Component Standards
- **USE animated form components** from `/apps/web/src/components/forms/MantineFormInputs.tsx`
- **ENABLE tapered underlines** with `taperedUnderline={true}` prop
- **APPLY Design System v7 colors** using CSS variables
- **FOLLOW form validation patterns** established in the codebase

### Button Standards
- **USE CSS classes** `btn`, `btn-primary`, `btn-secondary`, `btn-primary-alt`
- **AVOID inline styles** on buttons
- **NO Mantine Button** with custom styles - use native HTML with CSS classes
- **MAINTAIN consistency** across all button implementations

## File Organization Standards

### Component Location Rules
- **React Components**: `/apps/web/src/components/`
- **Pages**: `/apps/web/src/pages/`
- **Dashboard Pages**: `/apps/web/src/pages/dashboard/`
- **Shared Types**: Use `@witchcityrope/shared-types` package
- **Component Organization**: Group by feature in subdirectories

### Naming Conventions
- **Component Files**: `ComponentName.tsx` (PascalCase)
- **Page Files**: `page-name.tsx` (kebab-case)
- **Type Files**: `types.ts` or use generated types
- **Test Files**: `Component.test.tsx`
- **Style Files**: `Component.module.css`

## Quality Assurance

### Implementation Validation
- [ ] Wireframe requirements met 100%
- [ ] TinyMCE used for rich text (if applicable)
- [ ] Mantine components used correctly
- [ ] CSS classes used instead of inline styles
- [ ] TypeScript interfaces properly typed
- [ ] Responsive design considerations applied
- [ ] Accessibility standards met
- [ ] Component reusability maintained

### Review Checklist
- [ ] Matches wireframe exactly
- [ ] Uses approved components and patterns
- [ ] Follows naming conventions
- [ ] No inline styling violations
- [ ] TypeScript compilation clean
- [ ] No console errors
- [ ] Mobile responsive
- [ ] Keyboard accessible

## Enforcement

### Violation Consequences
- **Wireframe deviations**: Implementation must be corrected to match wireframes
- **Wrong rich text editor**: Must replace with TinyMCE implementation
- **Inline style violations**: Must convert to CSS classes
- **Component location errors**: Must move files to correct locations

### Approval Process
- **Wireframe changes**: Require explicit approval before deviating
- **New component patterns**: Review existing components first
- **Rich text alternatives**: Not permitted - TinyMCE only
- **Styling approaches**: Must follow established CSS class patterns

## Resources

### Documentation References
- **Wireframes**: `/docs/design/wireframes/`
- **Design System**: `/docs/design/current/design-system-v7.md`
- **Component Library**: `/apps/web/src/components/`
- **React Architecture**: `/docs/architecture/REACT-ARCHITECTURE-INDEX.md`

### Implementation Guides
- **Forms**: `/docs/lessons-learned/react-developer-lessons-learned.md`
- **Authentication**: `/docs/functional-areas/authentication/`
- **API Integration**: `/docs/guides-setup/ai-agents/react-developer-api-changes-guide.md`

---

**Last Updated**: 2025-08-25  
**Maintained By**: Librarian Agent  
**Enforced By**: All React Development Agents  
**Review Schedule**: Updated when standards change