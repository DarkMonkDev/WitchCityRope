# UI Designer Lessons Learned

## 🚨 MANDATORY STARTUP PROCEDURE 🚨

### 🚨 ULTRA CRITICAL UI/UX DOCUMENTS (MUST READ): 🚨
1. **Design System v7** - **CURRENT DESIGN STANDARDS**
`docs/design/current/design-system-v7.md`

2. **Button Style Guide** - **COMPLETE BUTTON IMPLEMENTATION GUIDE**
`docs/design/current/button-style-guide.md`

3. **UI Implementation Standards** - **COMPONENT PATTERNS**
`docs/standards-processes/ui-implementation-standards.md`

4. **React Patterns** - **REACT COMPONENT STANDARDS**
`docs/standards-processes/development-standards/react-patterns.md`

5. **Project Architecture** - **MANTINE UI FRAMEWORK**
`ARCHITECTURE.md`

### 📚 DOCUMENT DISCOVERY RESOURCES:
- **File Registry** - `docs/architecture/file-registry.md` - Find any document
- **Functional Areas Index** - `docs/architecture/functional-area-master-index.md` - Navigate features
- **Key Documents List** - `docs/standards-processes/KEY-PROJECT-DOCUMENTS.md` - Critical docs

### 📖 ADDITIONAL IMPORTANT DOCUMENTS:
- **Forms Standardization** - `docs/standards-processes/forms-standardization.md` - Form patterns
- **Workflow Process** - `docs/standards-processes/workflow-orchestration-process.md` - Handoff procedures
- **Agent Boundaries** - `docs/standards-processes/agent-boundaries.md` - What each agent does

### Validation Gates (MUST COMPLETE):
- [ ] **Read Design System v7 FIRST** - Current design language and components
- [ ] **Read Button Style Guide** - Complete button implementation patterns
- [ ] Review UI Implementation Standards for component patterns
- [ ] Check React Patterns for React-specific guidelines
- [ ] Verify Mantine v7 component library usage
- [ ] Understand mobile-first responsive design approach

## 🚨 MANDATORY: Agent Handoff Documentation Process 🚨

**CRITICAL**: This is NOT optional - handoff documentation is REQUIRED for workflow continuity.

### 📋 WHEN TO CREATE HANDOFF DOCUMENTS
- **END of UI design phase** - BEFORE implementation begins
- **COMPLETION of wireframes** - Document design decisions
- **APPROVAL from stakeholders** - Document approved designs
- **DISCOVERY of UX constraints** - Share immediately

### 📁 WHERE TO SAVE HANDOFFS
**Location**: `docs/functional-areas/[feature]/handoffs/`
**Naming**: `ui-designer-YYYY-MM-DD-handoff.md`
**Template**: `docs/standards-processes/agent-handoff-template.md`

### 📝 WHAT TO INCLUDE (TOP 5 CRITICAL)
1. **Design Decisions**: Component choices and patterns
2. **Wireframe Locations**: Paths to all mockups
3. **Mantine Components**: Specific components to use
4. **Interaction Patterns**: User flows and behaviors
5. **Responsive Breakpoints**: Mobile/tablet/desktop specs

### 🤝 WHO NEEDS YOUR HANDOFFS
- **React Developers**: Component specifications
- **Functional Spec Agents**: Design requirements
- **Test Developers**: UI test scenarios
- **Other UI Designers**: Design system consistency

### ⚠️ MANDATORY READING BEFORE STARTING
**ALWAYS READ EXISTING HANDOFFS FIRST**:
1. Check `docs/functional-areas/[feature]/handoffs/` for requirements
2. Read business requirements handoff FIRST
3. Review existing wireframes and patterns
4. Maintain design system consistency

### 🚨 FAILURE TO CREATE HANDOFFS = IMPLEMENTATION FAILURES
**Why this matters**:
- Developers implement wrong designs
- Components don't match wireframes
- UX patterns become inconsistent
- Mobile experience breaks

**NO EXCEPTIONS**: Create handoff documents or workflow WILL fail.

---

## CMS In-Place Editing Pattern - October 2025

### CRITICAL: Always-Visible Edit Controls (Admin-Only)
**Problem**: Admin users need clear, discoverable way to edit static content pages
**Solution**: Edit button ALWAYS visible to admins (not hidden/hover-only)

**Desktop Pattern**:
```tsx
// Sticky edit button, top-right corner
<Button
  className="btn btn-secondary"
  style={{
    position: 'sticky',
    top: '80px',
    right: '40px',
    zIndex: 10
  }}
  onClick={handleEdit}
  aria-label="Edit page content"
>
  🖊 Edit Page
</Button>
```

**Mobile Pattern (Recommended - Floating Action Button)**:
```tsx
// FAB in bottom-right corner, thumb-friendly
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

**Rationale**:
- Stakeholder requirement (Phase 1 review)
- Maximum discoverability for admin users
- Hover states don't work on mobile (touch devices)
- Accessible to screen readers (always in DOM)

**Alternative (Mobile Header Button)**:
- Sticky button below page title
- Full-width on mobile
- Scrolls with page (less thumb-friendly)
- Acceptable if FAB feels too modern

---

### Optimistic UI Updates for Content Saving
**Pattern**: Update UI immediately on save, rollback on error

**Implementation**:
```tsx
const mutation = useMutation({
  mutationFn: (data) => cmsApi.updatePage(pageId, data),

  onMutate: async (newData) => {
    // Cancel outgoing queries
    await queryClient.cancelQueries({ queryKey: ['cms-page', slug] });

    // Snapshot previous
    const previousData = queryClient.getQueryData(['cms-page', slug]);

    // Optimistic update - instant UI change
    queryClient.setQueryData(['cms-page', slug], (old) => ({
      ...old,
      content: newData.content,
      title: newData.title,
      updatedAt: new Date().toISOString()
    }));

    return { previousData };
  },

  onError: (err, newData, context) => {
    // Rollback to previous content
    queryClient.setQueryData(['cms-page', slug], context?.previousData);

    // Show error notification
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

**Benefits**:
- <16ms perceived save time (instant feedback)
- Critical for mobile users on 3G/4G connections
- Automatic rollback preserves user edits on failure
- Industry standard (Gmail, Notion, etc.)

---

### Unsaved Changes Protection Pattern
**Problem**: Users navigate away with unsaved edits, lose work
**Solution**: Browser warning on navigation attempt with dirty state

**Implementation**:
```tsx
const [isDirty, setIsDirty] = useState(false);
const [isEditing, setIsEditing] = useState(false);

// Track dirty state on content changes
const handleContentChange = (html: string) => {
  setEditableContent(html);
  setIsDirty(true);
};

// Browser beforeunload event
useEffect(() => {
  const handleBeforeUnload = (e: BeforeUnloadEvent) => {
    if (isDirty && isEditing) {
      e.preventDefault();
      e.returnValue = ''; // Chrome requires returnValue
    }
  };

  window.addEventListener('beforeunload', handleBeforeUnload);
  return () => window.removeEventListener('beforeunload', handleBeforeUnload);
}, [isDirty, isEditing]);

// Cancel confirmation
const handleCancel = () => {
  if (isDirty && !window.confirm('Discard unsaved changes?')) {
    return; // User clicked "Cancel" in confirm dialog
  }
  setIsEditing(false);
  setIsDirty(false);
};
```

**Alternative (Better UX - Mantine Modal)**:
```tsx
<Modal
  opened={showCancelModal}
  onClose={() => setShowCancelModal(false)}
  title="Discard unsaved changes?"
  centered
>
  <Text size="sm" mb="md">
    You have made changes to the page content. If you cancel now,
    these changes will be permanently lost.
  </Text>

  <Group justify="flex-end" gap="md">
    <Button variant="outline" onClick={() => setShowCancelModal(false)}>
      Keep Editing
    </Button>
    <Button color="red" onClick={handleConfirmDiscard}>
      Discard Changes
    </Button>
  </Group>
</Modal>
```

---

### Admin Dashboard Revision History UI
**Pattern**: Separate admin page for viewing content change history

**Route**: `/admin/cms/revisions`

**Layout**:
```tsx
// Main page: Table of all CMS pages
<Table striped highlightOnHover>
  <thead>
    <tr>
      <th>Page Name</th>
      <th>Total Revisions</th>
      <th>Last Edited</th>
    </tr>
  </thead>
  <tbody>
    {pages.map(page => (
      <tr key={page.id} onClick={() => showRevisions(page.id)}>
        <td>{page.title} ▸</td>
        <td>{page.revisionCount}</td>
        <td>{formatDistanceToNow(page.updatedAt, { addSuffix: true })}</td>
      </tr>
    ))}
  </tbody>
</Table>

// Revision detail modal
<Modal opened={showModal} title={`Revision History: ${selectedPage.title}`}>
  <Stack>
    {revisions.map(revision => (
      <Paper
        key={revision.id}
        shadow="sm"
        p="md"
        radius="md"
        withBorder
        style={{ borderLeft: '4px solid #880124' }}
      >
        <Text size="md" weight={600}>
          {format(revision.createdAt, 'MMMM dd, yyyy at h:mm a')}
        </Text>
        <Text size="sm" color="dimmed">
          By: {revision.createdBy}
        </Text>
        <Text size="sm" italic>
          {revision.changeDescription}
        </Text>
        <Code block>{revision.contentPreview}</Code>

        <Group mt="md">
          <Button variant="outline" onClick={() => viewFullContent(revision)}>
            View Full Content
          </Button>
          <Button variant="outline" color="red" disabled>
            Restore (Future)
          </Button>
        </Group>
      </Paper>
    ))}
  </Stack>
</Modal>
```

**Rationale**:
- Stakeholder requirement (Phase 1 review)
- Separate from editing interface (cleaner UX)
- Admin-only feature in admin area
- Scalable for filters, search, exports later

---

### TipTap Editor Integration for CMS
**Pattern**: Use existing `MantineTiptapEditor.tsx` with text-only configuration

**Configuration**:
```tsx
<MantineTiptapEditor
  value={editableContent}
  onChange={setEditableContent}
  minRows={15} // 450px minimum height
  placeholder="Enter page content..."
  // No variable insertion for static pages
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
- Variable insertion (event-specific feature, not needed for static content)
- Image upload (MVP text-only, future enhancement with DigitalOcean Spaces)

**Mobile Toolbar Optimization**:
- Collapse less-used controls into "More" dropdown
- Show essential controls by default (Bold, Italic, Lists, Links)
- 44×44px touch targets for all toolbar buttons

---

### Error Recovery UI Patterns
**Pattern**: Clear, actionable error messages with retry capability

**Network Failure**:
```tsx
<Alert
  color="red"
  title="Failed to save content"
  icon={<IconAlertTriangle />}
  variant="filled"
>
  <Text size="sm" mb="md">
    Network error. Check your connection and try again.
  </Text>
  <Button color="red" size="sm" onClick={handleRetry}>
    Retry Save
  </Button>
</Alert>
```

**Error Messages by Type**:
| Error | User Message | Technical Reason |
|-------|--------------|------------------|
| Network failure | "Network error. Check your connection and try again." | `err.code === 'NETWORK_ERROR'` |
| 401 Unauthorized | "Session expired. Please log in and try again." | `err.status === 401` |
| 403 Forbidden | "You don't have permission to edit this page." | `err.status === 403` |
| 500 Server error | "Server error. Please try again later or contact support." | `err.status === 500` |
| Unknown | "Failed to save content. Please try again." | Default fallback |

**Key Principles**:
- Edits preserved in editor (no data loss)
- Clear explanation of what went wrong
- Actionable next steps (retry, check connection, log in)
- User-friendly language (not technical jargon)

---

### Mobile-First Touch Target Standards
**Minimum Sizes**:
- **Primary actions** (Save, Edit FAB): 48×48px
- **Secondary actions** (Cancel): 44×44px
- **TipTap toolbar buttons**: 44×44px
- **Clickable rows/cards**: 56px minimum height

**Example**:
```tsx
// Desktop button
<Button className="btn btn-primary">Save</Button>

// Mobile button (larger)
<Button className="btn btn-primary btn-large">Save</Button>

// Edit FAB (mobile)
<Button
  className="btn btn-primary btn-large"
  style={{
    width: '56px',
    height: '56px',
    borderRadius: '50%'
  }}
>
  🖊
</Button>
```

**Rationale**:
- iOS accessibility guidelines: 44×44px minimum
- WitchCityRope users often on phones
- Admins may edit content urgently while mobile
- Thumb-friendly = better UX

---

### Responsive Breakpoint: 768px
**Pattern**: Mobile-first, enhance for desktop

**Mobile (<768px)**:
- Content container: Full width, 20px padding
- Edit button: FAB bottom-right OR sticky header button
- Save/Cancel buttons: Stacked vertically, full-width
- TipTap toolbar: Simplified with "More" dropdown
- Revision table: Card layout (stacked)

**Desktop (≥769px)**:
- Content container: max-width 1200px, centered, 40px padding
- Edit button: Sticky top-right, `position: sticky; top: 80px; right: 40px;`
- Save/Cancel buttons: Flex row, right-aligned
- TipTap toolbar: Full toolbar visible
- Revision table: 3 columns (Name, Revisions, Last Edited)

**Implementation**:
```tsx
// Responsive button layout
<Group
  gap="md"
  justify={{ base: 'stretch', sm: 'flex-end' }}
  direction={{ base: 'column', sm: 'row' }}
>
  <Button className="btn btn-primary">Save</Button>
  <Button className="btn btn-secondary">Cancel</Button>
</Group>
```

---

### Accessibility for Admin Workflows
**ARIA Labels**:
```tsx
<Button aria-label="Edit page content">Edit Page</Button>
<Button aria-label="Save changes to page content" aria-busy={isSaving}>
  {isSaving ? 'Saving...' : 'Save'}
</Button>
<Button aria-label="Cancel editing and discard changes">Cancel</Button>
```

**Keyboard Navigation**:
- Tab order: Title input → Editor → Toolbar buttons → Save → Cancel
- Enter/Space: Activate buttons
- Escape: Close modals
- Ctrl+B/I/Z/Y: TipTap shortcuts (Bold, Italic, Undo, Redo)

**Focus Management**:
```tsx
// Focus editor when entering edit mode
useEffect(() => {
  if (isEditing && editorRef.current) {
    editorRef.current.focus();
  }
}, [isEditing]);
```

**Screen Reader Announcements**:
- Edit mode: "Edit mode active. Make changes to page content."
- Save success: "Content saved successfully."
- Save error: "Failed to save content. Please try again."

---

## Common Mistakes to Avoid (CMS-Specific)

45. **DON'T** hide edit button until hover - always visible for admins
46. **DON'T** skip unsaved changes warning - users lose work
47. **DON'T** forget optimistic updates - slow UX on mobile
48. **DON'T** use pessimistic updates - network wait feels unresponsive
49. **DON'T** ignore dirty state tracking - can't warn on navigation
50. **DON'T** enable variable insertion for static pages - not needed
51. **DON'T** design image upload for MVP - text-only scope
52. **DON'T** forget revision history admin page - stakeholder requirement
53. **DON'T** design inline revision history - clutters editing interface
54. **DON'T** skip FAB option for mobile edit button - best thumb reach
55. **DON'T** use small touch targets on mobile - 44px minimum
56. **DON'T** forget TipTap toolbar mobile optimization - collapsible groups
57. **DON'T** skip browser beforeunload event - native protection
58. **DON'T** forget error recovery UI - network failures happen
59. **DON'T** expose technical error details to users - user-friendly messages
60. **DON'T** lose user edits on error - preserve in editor for retry

---

This comprehensive approach ensures in-place content editing is discoverable, mobile-friendly, accessible, and follows Design System v7 patterns while protecting user work through optimistic updates and unsaved changes warnings.
