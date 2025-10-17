# UI Designer Handoff: CMS Implementation
<!-- Last Updated: 2025-10-17 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Complete - Ready for Human Review -->

## Handoff Summary

**From**: UI Designer Agent
**To**: React Developer, Functional Spec Agent, Test Developer
**Date**: October 17, 2025
**Phase**: Phase 2 - Design & Architecture (UI Design Complete)
**Next Phase**: Human Review → Functional Specification → Technical Design

---

## What Was Delivered

### 1. Complete UI Design Document
**Location**: `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/design/ui-design.md`

**Contents**:
- 7 UI states wireframed (desktop + mobile)
- 4 complete interaction flows
- Component specifications for all states
- Mantine component mapping
- Responsive behavior (768px breakpoint)
- Accessibility requirements (WCAG 2.1 AA)
- Design decisions with rationale
- Edge case handling

---

## Critical Design Decisions

### 1. Edit Button Always Visible (Not Hidden/Hover)
**Decision**: Edit button is always visible to admin users, prominently displayed.

**Implementation**:
- **Desktop**: Sticky top-right position, burgundy outline button
- **Mobile**: Floating action button (FAB) bottom-right, gold gradient, 56×56px

**Rationale**:
- Stakeholder requirement from Phase 1 review
- Maximum discoverability for admins
- Mobile-friendly (hover doesn't work on touch)

**React Components**:
```tsx
// Desktop
<Button
  className="btn btn-secondary"
  style={{ position: 'sticky', top: '80px', right: '40px', zIndex: 10 }}
  onClick={handleEdit}
  aria-label="Edit page content"
>
  🖊 Edit Page
</Button>

// Mobile (FAB)
<Button
  className="btn btn-primary btn-large"
  style={{
    position: 'fixed',
    bottom: '24px',
    right: '16px',
    width: '56px',
    height: '56px',
    borderRadius: '50%',
    zIndex: 100
  }}
  onClick={handleEdit}
  aria-label="Edit page content"
>
  🖊
</Button>
```

---

### 2. Optimistic Updates (<16ms Perceived Save Time)
**Decision**: Use TanStack Query optimistic updates for instant UI feedback.

**Implementation**:
- Save clicked → UI updates immediately
- Network request happens in background
- Rollback on error
- Success toast after server confirms

**React Pattern**:
```tsx
const mutation = useMutation({
  mutationFn: (data) => cmsApi.updatePage(pageId, data),

  onMutate: async (newData) => {
    // Cancel outgoing queries
    await queryClient.cancelQueries({ queryKey: ['cms-page', slug] });

    // Snapshot previous
    const previousData = queryClient.getQueryData(['cms-page', slug]);

    // Optimistic update
    queryClient.setQueryData(['cms-page', slug], (old) => ({
      ...old,
      content: newData.content,
      title: newData.title,
      updatedAt: new Date().toISOString()
    }));

    return { previousData };
  },

  onError: (err, newData, context) => {
    // Rollback
    queryClient.setQueryData(['cms-page', slug], context?.previousData);
    notifications.show({
      color: 'red',
      title: 'Error',
      message: 'Failed to save content. Please try again.'
    });
  },

  onSuccess: () => {
    notifications.show({
      color: 'green',
      title: 'Success',
      message: 'Content saved successfully',
      autoClose: 3000
    });
  }
});
```

---

### 3. Admin Dashboard for Revision History
**Decision**: Separate admin page at `/admin/cms/revisions` for viewing history.

**Routes Required**:
```tsx
// App.tsx
<Route path="/admin/cms/revisions" element={<CmsRevisionsPage />} />
```

**API Endpoints**:
- `GET /api/cms/pages` - List all CMS pages with revision counts
- `GET /api/cms/pages/{id}/revisions` - Get revision history for a page

**React Components**:
- `CmsRevisionsPage.tsx` - Main page with table of pages
- `RevisionHistoryModal.tsx` - Modal showing revision list
- `RevisionCard.tsx` - Individual revision display

---

### 4. TipTap Editor Integration
**Decision**: Use existing `MantineTiptapEditor.tsx` component with text-only configuration.

**Configuration**:
```tsx
<MantineTiptapEditor
  value={editableContent}
  onChange={setEditableContent}
  minRows={15}
  placeholder="Enter page content..."
  // No variable insertion for CMS pages
/>
```

**Features Enabled**:
- Text Formatting: Bold, Italic, Underline, Strikethrough
- Headings: H1, H2, H3, H4
- Lists: Bullet, Ordered, Blockquote
- Links: Insert, Edit, Remove
- Alignment: Left, Center, Right, Justify
- History: Undo, Redo

**Features Disabled**:
- Variable insertion (event-specific feature, not needed for static pages)

---

### 5. Mobile-First Design
**Decision**: Design for mobile first, enhance for desktop.

**Breakpoint**: 768px
- **Mobile**: <768px - Single column, FAB, simplified toolbar
- **Desktop**: ≥769px - Multi-column, sticky edit button, full toolbar

**Touch Targets**:
- Primary actions (Save, Edit FAB): 48×48px minimum
- Secondary actions (Cancel): 44×44px minimum
- TipTap toolbar buttons: 44×44px

---

## Mantine Component Specifications

### Primary Components

| Component | Purpose | Props | Class |
|-----------|---------|-------|-------|
| **Container** | Page wrapper | `size="lg"`, `px="xl"` | - |
| **TextInput** | Page title | `label="Page Title"`, `required`, `maxLength={200}` | Floating label animation |
| **MantineTiptapEditor** | Content editor | `minRows={15}`, `value`, `onChange` | Existing component |
| **Button (Save)** | Primary action | `className="btn btn-primary"`, `loading={isSaving}`, `disabled={!isDirty}` | Gold gradient |
| **Button (Cancel)** | Secondary action | `className="btn btn-secondary"`, `disabled={isSaving}` | Burgundy outline |
| **Button (Edit)** | Edit trigger | `className="btn btn-secondary"` (desktop) or `"btn btn-primary btn-large"` (mobile FAB) | Always visible |
| **Modal** | Cancel confirmation | `centered`, `title="Discard unsaved changes?"` | - |
| **Alert** | Error messages | `color="red"`, `icon={<IconAlertTriangle />}`, `variant="filled"` | - |
| **Table** | Revision history list | `striped`, `highlightOnHover` | - |
| **Paper** | Revision cards | `shadow="sm"`, `p="md"`, `radius="md"`, `withBorder` | - |

### Notifications (Toasts)

**Success**:
```tsx
notifications.show({
  color: 'green',
  title: 'Success',
  message: 'Content saved successfully',
  icon: <IconCheck />,
  autoClose: 3000
});
```

**Error**:
```tsx
notifications.show({
  color: 'red',
  title: 'Error',
  message: 'Failed to save content. Please try again.',
  icon: <IconX />,
  autoClose: 5000
});
```

**Loading**:
```tsx
notifications.show({
  id: 'saving',
  loading: true,
  title: 'Saving',
  message: 'Saving changes...',
  autoClose: false
});
```

---

## Component Structure

### CmsPage.tsx (Main Component)

**File**: `/apps/web/src/pages/cms/CmsPage.tsx`

**State Management**:
```tsx
const [isEditing, setIsEditing] = useState(false);
const [editableContent, setEditableContent] = useState('');
const [editableTitle, setEditableTitle] = useState('');
const [isDirty, setIsDirty] = useState(false);
```

**Hooks**:
```tsx
const { content, isLoading, save, isSaving } = useCmsPage(slug);
const { isAdmin } = useAuth();
```

**Key Functions**:
- `handleEdit()` - Enter edit mode
- `handleSave()` - Save with optimistic update
- `handleCancel()` - Exit edit mode with confirmation
- `handleContentChange(html)` - Track dirty state
- `handleTitleChange(value)` - Track dirty state

**Browser Warning**:
```tsx
useEffect(() => {
  const handleBeforeUnload = (e: BeforeUnloadEvent) => {
    if (isDirty && isEditing) {
      e.preventDefault();
      e.returnValue = '';
    }
  };
  window.addEventListener('beforeunload', handleBeforeUnload);
  return () => window.removeEventListener('beforeunload', handleBeforeUnload);
}, [isDirty, isEditing]);
```

---

### useCmsPage.ts (Custom Hook)

**File**: `/apps/web/src/hooks/useCmsPage.ts`

**Responsibilities**:
- Fetch page content by slug
- Save page content with optimistic updates
- Error handling and rollback
- Cache management

**API Calls**:
```tsx
const query = useQuery({
  queryKey: ['cms-page', slug],
  queryFn: () => cmsApi.getPageBySlug(slug),
  staleTime: 5 * 60 * 1000 // 5 minutes
});

const mutation = useMutation({
  mutationFn: (data) => cmsApi.updatePage(query.data!.id, data),
  onMutate: /* optimistic update */,
  onError: /* rollback */,
  onSuccess: /* success toast */,
  onSettled: /* invalidate cache */
});
```

---

### CmsRevisionsPage.tsx (Admin Dashboard)

**File**: `/apps/web/src/pages/admin/CmsRevisionsPage.tsx`

**Layout**:
- Table of all CMS pages with revision counts
- Click row to open revision history modal
- Relative time display ("1 hour ago", "3 days ago")

**Components**:
- `RevisionHistoryModal` - Shows list of revisions for selected page
- `RevisionCard` - Individual revision display with metadata

---

## Responsive Behavior

### Desktop (≥769px)

**Layout**:
- Content container: `max-width: 1200px`, centered
- Edit button: Sticky top-right, `position: sticky; top: 80px; right: 40px;`
- Save/Cancel buttons: Flex row, right-aligned, `justify="flex-end"`
- TipTap toolbar: Full toolbar visible

**Spacing**:
- Container padding: `40px`
- Gap between buttons: `24px` (--space-md)

---

### Mobile (<768px)

**Layout**:
- Content container: Full width, `20px` padding
- Edit button: **Floating action button** (FAB), `position: fixed; bottom: 24px; right: 16px;`
- Save/Cancel buttons: Flex column, full-width, stacked
- TipTap toolbar: Simplified with "More" dropdown

**Touch Optimization**:
- Save button height: `48px`
- Cancel button height: `48px`
- Edit FAB size: `56×56px`
- All toolbar buttons: `44×44px` minimum

---

## Accessibility Implementation

### Keyboard Navigation

**Tab Order**:
1. Page Title input (edit mode)
2. TipTap editor content area
3. TipTap toolbar buttons
4. Save button
5. Cancel button

**ARIA Labels**:
```tsx
<Button aria-label="Edit page content">Edit Page</Button>
<Button aria-label="Save changes to page content" aria-busy={isSaving}>
  {isSaving ? 'Saving...' : 'Save'}
</Button>
<Button aria-label="Cancel editing and discard changes">Cancel</Button>
```

**Focus Management**:
```tsx
// Focus editor when entering edit mode
useEffect(() => {
  if (isEditing && editorRef.current) {
    editorRef.current.focus();
  }
}, [isEditing]);
```

---

### Screen Reader Support

**Live Regions**:
```tsx
// Notifications (Mantine handles this)
<div aria-live="polite" aria-atomic="true">
  {/* Notification content */}
</div>

// Error alerts
<Alert role="alert" aria-live="assertive">
  {/* Error message */}
</Alert>
```

**Status Announcements**:
- Edit mode: "Edit mode active. Make changes to page content."
- Save success: "Content saved successfully."
- Save error: "Failed to save content. Please try again."

---

## Edge Cases to Handle

### 1. Network Failure During Save
- Optimistic update rolls back
- Error toast: "Network error. Check your connection and try again."
- Editor remains open with user's edits preserved
- Retry button available

### 2. Very Long Content (>10,000 characters)
- TipTap editor: `max-height: 600px`, `overflow-y: auto`
- Revision previews: Limited to first 100 characters
- Full content only loaded when "View Full Content" clicked

### 3. Very Short Content (<50 characters)
- TipTap editor: `min-height: 450px`
- Placeholder text: "Enter page content..."
- Save button disabled if title or content empty

### 4. Unsaved Changes + Browser Navigation
- Browser `beforeunload` event fires
- Native browser warning: "Leave site? Changes you made may not be saved."
- User chooses to stay or leave

### 5. Concurrent Editing (Future)
- Current: Last-write-wins (no conflict detection)
- Future: Show warning if page recently edited by another user

---

## Design System Compliance

### Colors Used
- **Burgundy**: `#880124` (buttons, borders)
- **Rose Gold**: `#B76D75` (input borders, highlights)
- **Gold Gradient**: `linear-gradient(135deg, #FFBF00 0%, #FF8C00 100%)` (Save button, Edit FAB)
- **Cream**: `#FAF6F2` (page background)
- **Ivory**: `#FFF8F0` (card backgrounds, editor background)
- **Success**: `#228B22` (save success)
- **Error**: `#DC143C` (save failure)

### Typography
- **Headings/Labels**: `'Montserrat', sans-serif`, 600 weight, uppercase, 1.5px letter-spacing
- **Body Text**: `'Source Sans 3', sans-serif`, 400 weight, 1.7 line-height

### Button Styles
**All buttons use signature corner morphing**:
- Default: `border-radius: 12px 6px 12px 6px`
- Hover: `border-radius: 6px 12px 6px 12px`
- Transition: `all 0.3s ease`
- **NO vertical movement** (no translateY)

**Button Classes**:
- Save: `.btn .btn-primary` (gold gradient)
- Cancel: `.btn .btn-secondary` (burgundy outline)
- Edit (desktop): `.btn .btn-secondary`
- Edit (mobile FAB): `.btn .btn-primary .btn-large`

---

## Testing Requirements

### Visual Testing
- [ ] Edit button visible on desktop (sticky top-right)
- [ ] Edit button visible on mobile (FAB bottom-right)
- [ ] TipTap editor loads with correct toolbar
- [ ] Save/Cancel buttons styled correctly
- [ ] Toast notifications appear in correct position
- [ ] Error alerts display with red styling

### Interaction Testing
- [ ] Click Edit → Editor appears
- [ ] Make changes → isDirty becomes true
- [ ] Click Save → Optimistic update, content view appears
- [ ] Server success → Success toast appears
- [ ] Server failure → Error alert, rollback to edit mode
- [ ] Click Cancel with changes → Confirmation modal
- [ ] Navigate away with changes → Browser warning

### Accessibility Testing
- [ ] Keyboard navigation works (Tab, Enter, Escape)
- [ ] ARIA labels present on all buttons
- [ ] Focus indicators visible (2px burgundy outline)
- [ ] Screen reader announces state changes
- [ ] Color contrast meets WCAG 2.1 AA

### Responsive Testing
- [ ] Mobile layout (<768px): FAB, stacked buttons, simplified toolbar
- [ ] Desktop layout (≥769px): Sticky edit button, row buttons, full toolbar
- [ ] Touch targets 44×44px minimum on mobile

---

## Questions for Implementation Team

### For React Developer:
1. **Edit Button Placement**: Prefer FAB or sticky header button on mobile?
2. **Cancel Confirmation**: Browser `window.confirm()` or Mantine Modal?
3. **Revision History**: Modal or accordion for detail view?
4. **Loading States**: Mantine Loader or custom spinner?

### For Backend Developer:
1. **Revision Preview**: Should backend return first 100 characters, or should frontend truncate?
2. **Error Codes**: What HTTP status codes will API return? (401, 403, 500?)
3. **Validation**: What validation errors might occur? (title too long, content empty?)

### For Test Developer:
1. **E2E Priority**: Which flow should be tested first? (Edit → Save → View?)
2. **Error Scenarios**: Which error cases need E2E tests? (Network failure, validation error?)
3. **Mobile Testing**: Test on real devices or emulator?

---

## Next Phase Prerequisites

**Before Functional Specification**:
- [ ] Human review approval of UI design
- [ ] Stakeholder confirmation on mobile edit button placement
- [ ] Decision on cancel confirmation approach

**Before Implementation**:
- [ ] Functional specification complete
- [ ] Technical design approved
- [ ] Database schema designed
- [ ] API endpoints specified

---

## Files Created

| File | Purpose | Status |
|------|---------|--------|
| `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/design/ui-design.md` | Complete UI design with wireframes | Complete ✅ |
| `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/handoffs/ui-designer-2025-10-17-handoff.md` | This handoff document | Complete ✅ |

---

## References

**Must Read**:
- Business Requirements: `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/requirements/business-requirements.md`
- Technology Research: `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/research/cms-architecture-research.md`
- Phase 1 Review: `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/reviews/phase1-requirements-review.md`

**Design System**:
- Design System v7: `/docs/design/current/design-system-v7.md`
- Button Style Guide: `/docs/design/current/button-style-guide.md`

**Existing Components**:
- TipTap Editor: `/apps/web/src/components/forms/MantineTiptapEditor.tsx`

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-10-17 | UI Designer Agent | Initial handoff with complete UI design specifications, component mapping, and implementation guidance |

**Status**: **Complete - Ready for Human Review** ✅
**Next Review**: Mandatory human review before Phase 2 continues
**Handoff To**: React Developer (after approval), Functional Spec Agent, Test Developer
