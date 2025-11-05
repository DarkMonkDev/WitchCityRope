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
`docs/standards-processes/frontend/react-patterns.md`

5. **Project Architecture** - **MANTINE UI FRAMEWORK**
`ARCHITECTURE.md`

### 📚 DOCUMENT DISCOVERY RESOURCES:
- **File Registry** - `docs/architecture/file-registry.md` - Find any document
- **Functional Areas Index** - `docs/architecture/functional-area-master-index.md` - Navigate features
- **Key Documents List** - `docs/standards-processes/KEY-PROJECT-DOCUMENTS.md` - Critical docs
- **Standards Index** - `docs/standards-processes/STANDARDS-INDEX.md` - Task-based standards discovery (NEW)

### 📖 ADDITIONAL IMPORTANT DOCUMENTS:
- **Forms Standardization** - `docs/standards-processes/forms-standardization.md` - Form patterns
- **Workflow Process** - `docs/standards-processes/workflow-orchestration-process.md` - Handoff procedures
- **Agent Boundaries** - `docs/standards-processes/agent-boundaries.md` - What each agent does

### 🎨 UI/UX-SPECIFIC STANDARDS (Just-In-Time Loading):
**Reference**: `docs/standards-processes/STANDARDS-INDEX.md` for complete frontend standards list

**Quick UI/UX Standards** (read when needed):
- **Mantine UI Standards** - `docs/standards-processes/frontend/mantine-ui-standards.md` - Mantine v7 component usage
- **React Patterns** - `docs/standards-processes/frontend/react-patterns.md` - Component patterns, hooks
- **TypeScript Patterns** - `docs/standards-processes/frontend/typescript-patterns.md` - Type safety, DTO alignment
- **Routing Patterns** - `docs/standards-processes/frontend/routing-patterns.md` - React Router v7 navigation
- **State Management** - `docs/standards-processes/frontend/state-management-patterns.md` - Zustand, React Query

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

**Skills Usage**: See `/.claude/skills/HOW-TO-USE-SKILLS.md` for complete guide on when/how to use skills

---

## Admin Settings Card Pattern - November 2025

### CRITICAL: Consistent Card Header Design
**Problem**: Admin settings cards need visual consistency across features
**Solution**: Reusable gradient header pattern with icon + title

**Header Pattern**:
```tsx
<Box
  style={{
    background: 'linear-gradient(135deg, var(--color-burgundy) 0%, var(--color-plum) 100%)',
    padding: 'var(--space-lg) var(--space-xl)',
    borderBottom: '1px solid var(--color-taupe)',
  }}
>
  <Group gap="sm">
    <IconClock size={24} color="var(--color-ivory)" />
    <Title
      order={3}
      style={{
        fontFamily: 'var(--font-heading)',
        fontSize: '20px',
        fontWeight: 700,
        color: 'var(--color-ivory)',
      }}
    >
      Card Title
    </Title>
  </Group>
</Box>
```

**Body Pattern**:
```tsx
<Box style={{ padding: 'var(--space-xl)' }}>
  <Stack gap="lg">
    {/* Card content */}
  </Stack>
</Box>
```

**Rationale**:
- Burgundy/plum gradient matches brand (Design System v7)
- Ivory text provides excellent contrast (AAA compliant)
- Icon + title pattern aids scanability
- Consistent spacing creates visual rhythm
- Border-bottom separates header from body

**Example Usage**: Time Zone Settings card, Venue Management card

---

### Conditional Form Visibility Pattern
**Pattern**: Show form only when user makes selection in dropdown

**Implementation**:
```tsx
const [selectedId, setSelectedId] = useState<number | 'new' | null>(null);

// Render
<Select
  data={options}
  value={selectedId?.toString() ?? null}
  onChange={(value) => setSelectedId(value === 'new' ? 'new' : parseInt(value))}
/>

{selectedId !== null && (
  <Box mt="md">
    {/* Form appears here */}
  </Box>
)}
```

**Benefits**:
- Cleaner initial UI (no visual clutter)
- Progressive disclosure (show complexity only when needed)
- Clear affordance (dropdown drives form visibility)
- Reduces cognitive load for users

**Rationale**: Venue management dropdown has 3 states (no selection, create mode, edit mode). Showing form only on selection keeps UI clean and guides user through workflow.

---

### Dropdown Options with Visual Hierarchy
**Pattern**: Group options with separators and visual indicators

**Implementation**:
```tsx
const venueOptions = [
  { value: 'default', label: 'Select or Add New', disabled: true },
  { value: 'new', label: 'Add New' },
  // Divider component between sections
  ...activeVenues.map(v => ({
    value: v.id.toString(),
    label: v.name
  })),
  ...inactiveVenues.map(v => ({
    value: v.id.toString(),
    label: `${v.name} (Inactive)`,
    style: { color: 'var(--color-stone)' } // Gray text
  })),
];

<Select
  data={venueOptions}
  searchable
  placeholder="Select venue"
/>
```

**Visual Hierarchy**:
1. **Default option** (disabled): Placeholder text, cannot be reselected
2. **"Add New" option**: Bold or distinct to draw attention
3. **Divider**: Visual separator between actions and data
4. **Active items**: Normal text (charcoal)
5. **Inactive items**: Gray text with "(Inactive)" suffix

**Benefits**:
- Clear separation between actions and data
- Visual indication of item status
- Prevents user confusion about available vs. archived items
- Follows native UI patterns (common in OS file pickers)

---

### Form Mode Detection Pattern
**Pattern**: Different button layouts for create vs. edit modes

**Implementation**:
```tsx
const isCreateMode = selectedId === 'new';
const isEditMode = selectedId !== null && selectedId !== 'new';

// Button rendering
{isCreateMode && (
  <Group justify="flex-end">
    <Button className="btn btn-primary" onClick={handleCreate}>
      Create Venue
    </Button>
  </Group>
)}

{isEditMode && (
  <Group justify="flex-end" gap="sm">
    <Button className="btn btn-secondary" onClick={handleDelete}>
      Delete Venue
    </Button>
    <Button className="btn btn-primary" onClick={handleUpdate}>
      Update Venue
    </Button>
  </Group>
)}
```

**Mode Indicators**:
- **Create mode**: Single "Create" button (primary CTA)
- **Edit mode**: "Delete" (secondary) + "Update" (primary) buttons
- **Button order**: Destructive action on left, primary action on right

**Rationale**:
- Clear visual indicator of current mode
- Prevents accidental deletions (requires two clicks: select + confirm)
- Follows standard form patterns (cancel left, save right)
- Primary action always right-aligned for consistency

---

### Input Field Label Styling
**Pattern**: Uppercase labels with letter-spacing for professional admin UI

**Implementation**:
```tsx
<Text
  component="label"
  style={{
    display: 'block',
    fontFamily: 'var(--font-heading)',
    fontSize: '14px',
    fontWeight: 600,
    color: 'var(--color-smoke)',
    marginBottom: 'var(--space-xs)',
    textTransform: 'uppercase',
    letterSpacing: '0.5px',
  }}
>
  Venue Name
</Text>
<TextInput {...props} />
```

**Visual Effect**:
- Labels stand out from input values
- Uppercase creates visual hierarchy
- Letter-spacing improves readability at small sizes
- Consistent with Design System v7 navigation style

**Rationale**: Admin interfaces benefit from stronger visual hierarchy than public-facing forms. Uppercase labels with letter-spacing create professional, scannable layouts without overwhelming users.

---

### Textarea Auto-Sizing Pattern
**Pattern**: Fixed row count with vertical resize for long-form content

**Implementation**:
```tsx
<Textarea
  label="Directions"
  rows={4}
  minRows={3}
  maxRows={10}
  autosize={false} // Disable auto-sizing for consistent layout
  style={{ resize: 'vertical' }} // Allow manual resize
  maxLength={500}
  description={`${directions.length}/500 characters`}
/>
```

**Desktop**:
- 4 rows visible (100px height)
- Manual resize via drag handle
- Character counter below

**Mobile**:
- 3 rows visible (smaller screen)
- Manual resize still available
- Character counter wraps below

**Rationale**:
- Fixed initial height maintains layout consistency
- Manual resize gives users control
- Character counter prevents validation errors
- 4 rows accommodates most venue directions without scrolling

---

### Validation Error Display Pattern
**Pattern**: Inline errors below inputs with icon and clear message

**Implementation**:
```tsx
<TextInput
  label="Venue Name"
  required
  error={nameError}
  styles={{
    input: {
      borderColor: nameError ? 'var(--color-error)' : 'var(--color-taupe)',
    },
  }}
/>

{nameError && (
  <Text size="xs" color="red" mt="xs">
    <Group gap="xs">
      <IconAlertCircle size={16} />
      <span>{nameError}</span>
    </Group>
  </Text>
)}
```

**Error Types**:
- **Required field**: "Venue name is required"
- **Max length**: "Must be 100 characters or less (currently 123)"
- **Unique constraint**: "Venue name must be unique"
- **Network error**: "Failed to validate. Please try again."

**Visual Indicators**:
- Red border on input (error state)
- Red text below input
- Alert icon before error message
- Save button disabled while errors exist

**Rationale**:
- Inline errors appear exactly where problem occurred
- Icon draws attention without being alarming
- Specific messages help user fix issue
- Disabled save button prevents invalid submissions

---

### Button Alignment in Forms
**Pattern**: Right-aligned buttons in admin forms, full-width on mobile

**Desktop** (≥769px):
```tsx
<Group justify="flex-end" gap="sm" mt="md">
  <Button className="btn btn-secondary">Cancel</Button>
  <Button className="btn btn-primary">Save</Button>
</Group>
```

**Mobile** (<768px):
```tsx
<Stack gap="sm" mt="md">
  <Button className="btn btn-primary" fullWidth>Save</Button>
  <Button className="btn btn-secondary" fullWidth>Cancel</Button>
</Stack>
```

**Responsive Implementation**:
```tsx
<Group
  justify={{ base: 'stretch', sm: 'flex-end' }}
  gap="sm"
  mt="md"
  style={{
    flexDirection: window.innerWidth < 768 ? 'column-reverse' : 'row'
  }}
>
  <Button className="btn btn-secondary">Cancel</Button>
  <Button className="btn btn-primary">Save</Button>
</Group>
```

**Rationale**:
- Desktop: Right-aligned follows Western reading patterns (action at end)
- Mobile: Full-width increases touch target size
- Primary action on top (mobile) or right (desktop) for thumb reach
- Stack reverses on mobile so primary action appears first

---

### Soft Delete Confirmation Modal
**Pattern**: Explain consequences before destructive action

**Implementation**:
```tsx
<Modal
  opened={showDeleteModal}
  onClose={() => setShowDeleteModal(false)}
  title="Deactivate Venue?"
  centered
  size="md"
>
  <Text size="sm" mb="md">
    This will set "{venueName}" to inactive. The venue will no longer appear in event
    forms, but existing events will keep this venue information.
  </Text>

  <Group justify="flex-end" gap="sm">
    <Button className="btn btn-secondary" onClick={() => setShowDeleteModal(false)}>
      Cancel
    </Button>
    <Button className="btn btn-primary" onClick={handleConfirmDelete}>
      Deactivate
    </Button>
  </Group>
</Modal>
```

**Key Elements**:
- **Title as question**: "Deactivate Venue?" (not "Warning" or "Confirm")
- **Explain soft delete**: User understands data is preserved
- **Name the item**: Shows specific venue name for clarity
- **Explain impact**: "won't appear in event forms" + "existing events keep info"
- **Centered modal**: Draws focus to important decision
- **Two buttons**: Cancel (escape route) + Deactivate (confirm action)

**Rationale**:
- Soft delete is less scary than "Delete" (data preserved)
- Explaining consequences reduces user anxiety
- Naming the item prevents accidental deletions
- Cancel button provides clear escape route
- Modal pattern standard for destructive actions

---

### Success Notification Timing
**Pattern**: Auto-close success toasts, persist error toasts longer

**Implementation**:
```tsx
// Success notification
notifications.show({
  color: 'green',
  title: 'Success',
  message: 'Venue created successfully',
  icon: <IconCheck />,
  autoClose: 3000, // 3 seconds
  position: 'top-right',
});

// Error notification
notifications.show({
  color: 'red',
  title: 'Error',
  message: 'Failed to save venue. Please try again.',
  icon: <IconAlertCircle />,
  autoClose: 5000, // 5 seconds (longer for errors)
  position: 'top-right',
});
```

**Timing Rationale**:
- **Success (3s)**: User saw action succeed, doesn't need long confirmation
- **Error (5s)**: User needs time to read error and plan next action
- **Position**: Top-right doesn't obscure form content
- **Icons**: Visual reinforcement of message type

**Alternative for Critical Errors**:
```tsx
// No auto-close for critical errors
notifications.show({
  color: 'red',
  title: 'Network Error',
  message: 'Failed to save. Check your connection and try again.',
  icon: <IconAlertCircle />,
  autoClose: false, // User must dismiss manually
  withCloseButton: true,
});
```

---

### Grid Layout for Two-Column Admin Cards
**Pattern**: Two-column grid with responsive stacking

**Implementation**:
```tsx
<Grid gutter="xl">
  {/* Left Column - Time Zone Settings */}
  <Grid.Col span={{ base: 12, md: 6 }}>
    <Box style={{ height: '100%' }}>
      {/* Time Zone Card */}
    </Box>
  </Grid.Col>

  {/* Right Column - Venue Management */}
  <Grid.Col span={{ base: 12, md: 6 }}>
    <Box style={{ height: '100%' }}>
      {/* Venue Management Card */}
    </Box>
  </Grid.Col>
</Grid>
```

**Responsive Behavior**:
- **Desktop (≥768px)**: Two columns side-by-side
- **Mobile (<768px)**: Stack vertically (left card on top)
- **Height matching**: `height: '100%'` on both cards creates equal heights
- **Gutter**: `xl` spacing (40px) between cards

**Rationale**:
- Two-column layout maximizes screen real estate
- Cards of equal height create visual balance
- Stacking on mobile prevents horizontal scrolling
- Consistent gutter spacing maintains rhythm

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

## Common Mistakes to Avoid

### Admin Settings UI (November 2025)
61. **DON'T** use different header gradients across admin cards - always burgundy/plum
62. **DON'T** show form before user makes dropdown selection - progressive disclosure
63. **DON'T** use same buttons for create vs edit modes - mode detection prevents confusion
64. **DON'T** forget inactive item styling - gray text with "(Inactive)" suffix
65. **DON'T** skip dropdown separators - visual hierarchy aids scanability
66. **DON'T** auto-close success toasts too fast - 3s minimum for user to read
67. **DON'T** auto-close error toasts too fast - 5s minimum, critical errors never auto-close
68. **DON'T** left-align form buttons on desktop - right-aligned follows reading patterns
69. **DON'T** use fixed widths on mobile - full-width buttons increase touch targets
70. **DON'T** forget soft delete explanations - users need to know data is preserved

### CMS-Specific (October 2025)
71. **DON'T** hide edit button until hover - always visible for admins
72. **DON'T** skip unsaved changes warning - users lose work
73. **DON'T** forget optimistic updates - slow UX on mobile
74. **DON'T** use pessimistic updates - network wait feels unresponsive
75. **DON'T** ignore dirty state tracking - can't warn on navigation
76. **DON'T** enable variable insertion for static pages - not needed
77. **DON'T** design image upload for MVP - text-only scope
78. **DON'T** forget revision history admin page - stakeholder requirement
79. **DON'T** design inline revision history - clutters editing interface
80. **DON'T** skip FAB option for mobile edit button - best thumb reach
81. **DON'T** use small touch targets on mobile - 44px minimum
82. **DON'T** forget TipTap toolbar mobile optimization - collapsible groups
83. **DON'T** skip browser beforeunload event - native protection
84. **DON'T** forget error recovery UI - network failures happen
85. **DON'T** expose technical error details to users - user-friendly messages
86. **DON'T** lose user edits on error - preserve in editor for retry

---

This comprehensive approach ensures admin interfaces are consistent, accessible, and follow Design System v7 patterns while providing excellent UX through progressive disclosure, clear mode indicators, and responsive layouts.
