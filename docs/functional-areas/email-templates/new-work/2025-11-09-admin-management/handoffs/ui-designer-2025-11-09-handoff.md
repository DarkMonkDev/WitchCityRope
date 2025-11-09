# AGENT HANDOFF DOCUMENT

## Phase: UI Design (Phase 2)
## Date: 2025-11-09
## Feature: Email Templates Admin Management

---

## 🎯 UI DESIGN COMPLETION STATUS

### ✅ Deliverables Completed

**1. UI Design Document Created**: `/docs/functional-areas/email-templates/new-work/2025-11-09-admin-management/design/ui-design.md`

**2. Wireframes Delivered** (ASCII format):
- Admin Dashboard with Email Templates card (4-column grid)
- Email Templates Admin Page main layout (tabbed interface)
- Events Tab Panel (7 event templates)
- Event Emails Tab integration (EventForm with badges)
- Ad Hoc Email Tab (composer + history table)
- Mobile/Tablet responsive layouts

**3. Component Specifications**:
- Admin Dashboard "Email Templates" Card
- EmailTemplatesAdminPage (tabbed interface)
- EmailCategoryPanel (reusable per category)
- Template Card component
- Editor Panel component
- Badge indicators ("✓ Customized" vs "(Default)")
- Reset Confirmation Modal

**4. Design System Integration**:
- Color palette mapped to Design System v7 (burgundy, plum, rose-gold)
- Typography specs (Montserrat headings, Source Sans 3 body)
- Spacing scale (var(--space-xs) through var(--space-2xl))
- Button styles (btn-primary, btn-secondary)
- Animation patterns (card hover, editor slide-in, tab transitions)

**5. Mantine v7 Component Mapping**:
- Complete list of 20+ Mantine components with exact configurations
- Responsive grid specifications (Grid.Col span props)
- Mantine styles API customizations for tabs, cards, buttons
- MantineTiptapEditor integration for rich text editing

**6. Accessibility Specifications**:
- ARIA labels for all interactive elements
- Keyboard navigation tab order
- Screen reader announcements (live regions)
- Color contrast ratios verified (WCAG 2.1 AA)
- Focus management patterns

---

## 🔑 KEY DESIGN DECISIONS

### Decision 1: Reuse EventForm Email Tab Pattern

**Rationale**: EventForm already has template card UI (lines 1228-1386 in EventForm.tsx). Reusing this pattern:
- ✅ Maintains UI consistency across platform
- ✅ Reduces development time (proven pattern)
- ✅ Users already familiar with template card layout
- ✅ Horizontal scrollable cards work well for variable template counts

**Implementation**: EmailCategoryPanel component will use same card design:
- Card with border, padding, click-to-edit
- Title (burgundy, 16px Montserrat 600)
- Description (stone, 14px Source Sans 3)
- Target info (italic, dimmed, 12px)
- Selected state: burgundy border + light burgundy background

---

### Decision 2: Tabbed Interface (5 Categories)

**Rationale**: Keeps UI clean, prevents scroll overload, aligns with mental model of email categories.

**Categories**:
1. **Vetting** (6 templates) - Default tab, pre-selected when coming from Vetting admin
2. **Events** (7 templates) - Most complex, multi-session targeting
3. **Admin** (4 templates) - System notifications
4. **Incident** (4 templates) - Safety coordinator communications
5. **Ad Hoc** (1 template) - Bulk email composer with history

**Tab Styling**:
- Active tab: burgundy underline (3px), bold text (700 weight), burgundy color
- Inactive tabs: smoke color (4A4A4A), 600 weight
- Hover: light burgundy background (rgba(136, 1, 36, 0.05))
- Uppercase, letter-spacing 1px, 15px font size

**URL Pattern**: `/admin/email-templates?tab=events` (shareable links, browser back/forward support)

---

### Decision 3: Copy-on-Edit Badge System

**Rationale**: Event organizers need clear visual indicator of customization status.

**Badge Design**:
- **"✓ Customized"**: Green background (#228B22), white text, top-right position
- **"(Default)"**: Gray background (#8B8680), white text, top-right position
- Size: small (12px font), uppercase, 0.5px letter-spacing

**Behavior**:
- Badge shows on template card in EventForm Emails tab
- Badge NOT shown in global admin page (all templates are global there)
- Badge updates immediately after save or reset

---

### Decision 4: Reset to Default with Confirmation

**Rationale**: Prevent accidental deletion of customizations, but make recovery easy.

**Reset Button**:
- Variant: Light (not destructive red - less scary)
- Size: Small
- Position: Next to "Currently Editing" title in editor panel
- Visible: Only when template status is "✓ Customized"

**Confirmation Modal**:
- Title: "Reset to Default Template?"
- Message: "This will delete your event-specific customization for the [Template Name] template. Future emails will use the global default template."
- Warning: "This action cannot be undone." (bold, red text)
- Buttons: Cancel (light) + Reset to Default (red)
- Centered modal for focus

**Post-Reset**:
- DELETE EventEmailTemplate record
- Re-fetch global template
- Update editor with global content
- Badge changes to "(Default)"
- Notification: "Template reset to default successfully"

---

### Decision 5: Variable Validation (Warnings, Not Errors)

**Rationale**: Don't block admins from saving, but warn about potential issues.

**Validation Logic**:
1. Extract all `{{variable_name}}` patterns from subject + HTML body
2. Compare against category-specific allowed variables
3. If unknown variables detected:
   - Show yellow Alert component (not error)
   - List unknown variables
   - Show available variables for reference
   - ALLOW saving (warning only, not blocking)

**Visual Treatment**:
```tsx
<Alert
  icon={<IconAlertCircle />}
  color="yellow"
  variant="light"
  title="Unknown Variables Detected"
>
  <Text size="sm">
    {invalidVariables.join(', ')}
  </Text>
  <Text size="xs" mt="xs">
    Available variables: {allowedVariables.join(', ')}
  </Text>
</Alert>
```

**Why Not Block?**: Admins might intentionally use placeholder text like `{{custom_field}}` for future expansion. Warnings inform without preventing flexibility.

---

### Decision 6: MantineTiptapEditor Integration

**Rationale**: Reuse existing rich text editor from CMS feature (proven, accessible, integrated).

**Configuration**:
```tsx
<MantineTiptapEditor
  value={htmlBody}
  onChange={setHtmlBody}
  placeholder="Enter email template content..."
  minRows={12}
  variables={categoryVariables}  // Pass category-specific variables
/>
```

**Features Enabled**:
- Text Formatting: Bold, Italic, Underline
- Headings: H1-H6
- Lists: Bullet, Ordered
- Links: Insert, Edit, Remove
- Insert Variable: Dropdown button showing available variables
- HTML sanitization: Built-in (prevents XSS)

**Features Disabled**:
- Image upload (email templates text-only for MVP)
- Tables (not needed for email templates)
- Code blocks (not needed for email templates)

**Mobile Optimization**:
- Toolbar simplified on tablets (<1200px)
- Less-used controls in "More" dropdown
- 44×44px touch targets for all toolbar buttons

---

## 📍 WHAT FUNCTIONAL SPEC NEEDS TO ADDRESS

### API Endpoint Requirements

Based on UI design, functional spec should define these endpoints:

**1. Global Templates**:
- `GET /api/email-templates?category={category}` → List all templates for category
- `GET /api/email-templates/{id}` → Get single global template
- `PUT /api/email-templates/{id}` → Update global template
  - Request: `{ subject, htmlBody, plainTextBody }`
  - Response: Updated template with incremented version number

**2. Event-Specific Templates**:
- `GET /api/events/{eventId}/email-templates` → Get all templates for event (global + overrides)
- `GET /api/events/{eventId}/email-templates/{type}` → Get specific template
- `PUT /api/events/{eventId}/email-templates/{type}` → Create/update event-specific template
  - Request: `{ subject, htmlBody, plainTextBody, targetSessions }`
  - Response: Created/updated EventEmailTemplate
- `DELETE /api/events/{eventId}/email-templates/{type}` → Reset to default (delete override)

**3. Ad-Hoc Emails**:
- `POST /api/email-templates/ad-hoc` → Send ad-hoc email
  - Request: `{ subject, htmlBody, recipientGroup, eventId? }`
  - Response: SentAdHocEmail with SendGrid message ID
- `GET /api/email-templates/ad-hoc/history?eventId={id}` → Get sent ad-hoc history

**4. Variable Sets**:
- `GET /api/email-templates/variables/{category}` → Get allowed variables for category
  - Response: `{ category: 'events', variables: ['{{attendee_name}}', '{{event_title}}', ...] }`

### DTO Structure Requirements

**GlobalEmailTemplateDto**:
```typescript
{
  id: string;
  category: 'vetting' | 'events' | 'admin' | 'incident' | 'adhoc';
  templateType: string;  // Enum value as string
  templateTypeName: string;  // Display name
  subject: string;
  htmlBody: string;
  plainTextBody: string;
  variables: string[];  // Allowed variables for this template
  version: number;
  isActive: boolean;
  lastModified: string;  // ISO date
  updatedBy: string;  // Admin name or ID
}
```

**EventEmailTemplateDto**:
```typescript
{
  id: string;
  eventId: string;
  globalTemplateId: string;
  templateType: string;
  subject: string;
  htmlBody: string;
  plainTextBody: string;
  targetSessions: string[];  // ['all'] or ['s1', 's2']
  isCustomized: boolean;  // Always true for event-specific
  lastModified: string;
  updatedBy: string;
}
```

**SentAdHocEmailDto**:
```typescript
{
  id: string;
  subject: string;
  htmlBody: string;
  recipientGroup: string;
  recipientCount: number;
  eventId?: string;
  eventTitle?: string;
  sendGridMessageId: string;
  deliveryStatus: 'pending' | 'sent' | 'delivered' | 'failed';
  sentAt: string;
  sentBy: string;
}
```

### Business Logic Requirements

**1. Template Fetching Logic**:
- When loading EventForm Emails tab:
  - Fetch global templates for Events category
  - Fetch event-specific templates for this event
  - Merge: If event-specific exists for a type, use it; otherwise use global
  - Determine badge: "✓ Customized" if event-specific, "(Default)" if global

**2. Variable Substitution Logic**:
- Replace `{{variable_name}}` with actual values at send-time
- HTML-escape variable values before insertion (XSS prevention)
- If variable value is null/undefined, replace with empty string (no errors)
- Case-sensitive matching: `{{attendee_name}}` !== `{{Attendee_Name}}`

**3. HTML Sanitization**:
- Strip dangerous tags: `<script>`, `<iframe>`, `<object>`, `<embed>`
- Allow safe tags: `<p>`, `<h1-h6>`, `<strong>`, `<em>`, `<a>`, `<ul>`, `<ol>`, `<li>`
- Sanitize on save (server-side, defense-in-depth with client-side editor)

---

## 📊 WHAT DATABASE DESIGN NEEDS TO SUPPORT

### Query Patterns

**1. Fetch Global Templates by Category**:
```sql
SELECT * FROM GlobalEmailTemplates
WHERE Category = @category AND IsActive = true
ORDER BY TemplateType;
```

**Performance**: Index on `(Category, IsActive)` for fast filtering

**2. Fetch Event-Specific Templates**:
```sql
SELECT * FROM EventEmailTemplates
WHERE EventId = @eventId;
```

**Performance**: Index on `EventId` (foreign key)

**3. Check if Event-Specific Template Exists**:
```sql
SELECT COUNT(*) FROM EventEmailTemplates
WHERE EventId = @eventId AND TemplateType = @templateType;
```

**Performance**: Unique constraint on `(EventId, TemplateType)` ensures fast lookup

**4. Fetch Ad-Hoc Email History**:
```sql
SELECT * FROM SentAdHocEmails
WHERE EventId = @eventId OR EventId IS NULL
ORDER BY SentAt DESC
LIMIT 50;
```

**Performance**: Index on `(EventId, SentAt)` for fast filtering + sorting

### Data Model Requirements

**GlobalEmailTemplates Table**:
- **Unique Constraint**: `(Category, TemplateType)` - One template per type per category
- **Foreign Key**: `UpdatedBy` → `ApplicationUser.Id`
- **JSONB Field**: `Variables` - Store allowed variables as JSON array
- **Soft Delete**: `IsActive` flag (never hard delete global templates)
- **Version Control**: `Version` integer increments on each update

**EventEmailTemplates Table**:
- **Unique Constraint**: `(EventId, TemplateType)` - One override per type per event
- **Foreign Keys**:
  - `EventId` → `Events.Id` (cascade delete when event deleted)
  - `UpdatedBy` → `ApplicationUser.Id`
  - `GlobalTemplateId` → `GlobalEmailTemplates.Id` (NO constraint - reference only)
- **Array Field**: `TargetSessions` - String array for multi-session targeting
- **NO Version Control**: Event-specific templates don't need versioning (single editor)

**SentAdHocEmails Table**:
- **NO Unique Constraints**: Can send multiple ad-hoc emails to same group
- **Foreign Keys**:
  - `SentBy` → `ApplicationUser.Id` (required)
  - `EventId` → `Events.Id` (nullable, not all ad-hoc emails event-related)
- **Array Field**: `RecipientEmails` - Store email addresses for audit trail
- **Never Delete**: Permanent audit trail (no soft delete, no cascade delete)

### Variable Sets Storage

**Option 1: JSONB in GlobalEmailTemplates** (Recommended):
```sql
Variables JSONB DEFAULT '[]'::jsonb
-- Example: '["{{scene_name}}", "{{application_number}}"]'::jsonb
```

**Option 2: Separate VariableSets Table** (Overkill for 5 categories):
```sql
CREATE TABLE VariableSets (
  Category TEXT PRIMARY KEY,
  Variables JSONB NOT NULL
);
```

**Recommendation**: Use JSONB in GlobalEmailTemplates. Each template knows its own variables. Easier to query, maintain, and evolve.

---

## 🎨 WHAT REACT-DEVELOPER NEEDS TO KNOW

### Mantine Components and Exact Props

**1. Container (Page Wrapper)**:
```tsx
<Container size="xl" py="xl">
  {/* Page content */}
</Container>
```

**2. Title (Page Title)**:
```tsx
<Title
  order={1}
  style={{
    fontFamily: "'Montserrat', sans-serif",
    fontSize: '32px',
    fontWeight: 800,
    color: '#880124',
    textTransform: 'uppercase',
    letterSpacing: '-0.5px',
  }}
>
  Email Templates Management
</Title>
```

**3. Tabs (Category Navigation)**:
```tsx
<Tabs
  value={activeTab}
  onChange={(value) => setSearchParams({ tab: value || 'vetting' })}
  styles={{
    tab: {
      fontFamily: "'Montserrat', sans-serif",
      fontSize: '15px',
      fontWeight: 600,
      textTransform: 'uppercase',
      letterSpacing: '1px',
      color: 'var(--color-smoke)',
      padding: 'var(--space-md) var(--space-lg)',
      borderBottom: '3px solid transparent',
      transition: 'all 0.3s ease',
      '&[data-active]': {
        color: 'var(--color-burgundy)',
        borderBottomColor: 'var(--color-burgundy)',
        fontWeight: 700
      }
    }
  }}
>
  <Tabs.List>
    <Tabs.Tab value="vetting">Vetting</Tabs.Tab>
    <Tabs.Tab value="events">Events</Tabs.Tab>
    {/* ... other tabs */}
  </Tabs.List>

  <Tabs.Panel value="vetting" pt="xl">
    <EmailCategoryPanel category="vetting" />
  </Tabs.Panel>
</Tabs>
```

**4. Template Card**:
```tsx
<Card
  withBorder
  p="md"
  style={{
    cursor: 'pointer',
    borderColor: isSelected ? 'var(--color-burgundy)' : 'var(--color-rose-gold)',
    backgroundColor: isSelected ? 'rgba(136, 1, 36, 0.05)' : 'white',
    minWidth: '220px',
    maxWidth: '300px',
    flex: 1,
    position: 'relative',
    transition: 'all 0.3s ease',
    borderRadius: '12px',
  }}
  onClick={() => handleSelectTemplate(template)}
>
  {/* Badge for EventForm only */}
  {isEventContext && (
    <Badge
      color={isCustomized ? 'green' : 'gray'}
      variant="filled"
      size="sm"
      style={{
        position: 'absolute',
        top: '8px',
        right: '8px',
      }}
    >
      {isCustomized ? '✓ Customized' : '(Default)'}
    </Badge>
  )}

  <Text fw={600} c="burgundy" mb={4}>
    {template.name}
  </Text>

  <Text size="sm" c="stone" mb="xs">
    {template.description}
  </Text>

  <Text size="xs" c="dimmed" style={{ fontStyle: 'italic' }}>
    {template.targetInfo}
  </Text>
</Card>
```

**5. Editor Panel**:
```tsx
<Paper shadow="sm" radius="md" p="xl" mt="xl">
  <Stack gap="md">
    {/* Header */}
    <Group justify="space-between">
      <Text fw={600} c="burgundy" size="lg">
        Currently Editing: {selectedTemplate.name}
      </Text>

      {isEventContext && isCustomized && (
        <Button
          variant="light"
          color="red"
          size="sm"
          onClick={handleResetToDefault}
        >
          Reset to Default
        </Button>
      )}
    </Group>

    {/* Subject Line */}
    <TextInput
      label="Subject Line"
      value={subject}
      onChange={(e) => setSubject(e.currentTarget.value)}
      required
      maxLength={200}
    />

    {/* HTML Editor */}
    <MantineTiptapEditor
      value={htmlBody}
      onChange={setHtmlBody}
      placeholder="Enter email template content..."
      minRows={12}
      variables={selectedTemplate.variables}
    />

    {/* Variable Validation */}
    {invalidVariables.length > 0 && (
      <Alert icon={<IconAlertCircle />} color="yellow">
        Unknown variables: {invalidVariables.join(', ')}
      </Alert>
    )}

    {/* Actions */}
    <Group justify="flex-end" gap="sm">
      <Button variant="light" onClick={handleCancel}>
        Cancel
      </Button>
      <Button className="btn btn-primary" loading={isSaving}>
        Save Template
      </Button>
    </Group>
  </Stack>
</Paper>
```

### State Management Approach

**Use React Query for API Integration**:
```typescript
// Fetch global templates
const { data: globalTemplates, isLoading } = useQuery({
  queryKey: ['email-templates', category],
  queryFn: () => emailTemplatesApi.getByCategory(category),
});

// Fetch event-specific templates
const { data: eventTemplates } = useQuery({
  queryKey: ['event-templates', eventId],
  queryFn: () => emailTemplatesApi.getForEvent(eventId),
  enabled: !!eventId,
});

// Save template mutation
const saveMutation = useMutation({
  mutationFn: (data: UpdateTemplateRequest) =>
    emailTemplatesApi.updateTemplate(templateId, data),
  onSuccess: () => {
    queryClient.invalidateQueries(['email-templates', category]);
    notifications.show({ message: 'Template saved successfully', color: 'green' });
    setSelectedTemplate(null);
  },
  onError: (error) => {
    notifications.show({ message: 'Failed to save template', color: 'red' });
  },
});

// Reset to default mutation
const resetMutation = useMutation({
  mutationFn: ({ eventId, type }: ResetRequest) =>
    emailTemplatesApi.deleteEventTemplate(eventId, type),
  onSuccess: () => {
    queryClient.invalidateQueries(['event-templates', eventId]);
    notifications.show({ message: 'Template reset to default', color: 'green' });
    setSelectedTemplate(null);
  },
});
```

**Local State Management**:
```typescript
const [selectedTemplate, setSelectedTemplate] = useState<Template | null>(null);
const [subject, setSubject] = useState('');
const [htmlBody, setHtmlBody] = useState('');
const [invalidVariables, setInvalidVariables] = useState<string[]>([]);

// Sync editor state when template selected
useEffect(() => {
  if (selectedTemplate) {
    setSubject(selectedTemplate.subject);
    setHtmlBody(selectedTemplate.htmlBody);
  }
}, [selectedTemplate]);

// Real-time variable validation
useEffect(() => {
  const extractedVars = extractVariables(htmlBody);
  const invalid = extractedVars.filter(v => !allowedVariables.includes(v));
  setInvalidVariables(invalid);
}, [htmlBody, allowedVariables]);
```

### Accessibility Requirements

**ARIA Labels**:
```tsx
// Template card
<Card
  role="button"
  tabIndex={0}
  aria-label={`Edit ${template.name} template`}
  aria-pressed={isSelected}
  onClick={handleSelectTemplate}
  onKeyDown={(e) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      handleSelectTemplate();
    }
  }}
>

// Save button
<Button
  aria-label="Save email template changes"
  aria-busy={isSaving}
>
  Save Template
</Button>
```

**Keyboard Navigation**:
- Tab order: Tabs → Template cards → Subject → Editor → Cancel → Save
- Enter/Space activates buttons and cards
- Escape closes editor panel and modals

**Screen Reader Announcements**:
```tsx
<div
  role="status"
  aria-live="polite"
  aria-atomic="true"
  style={{ position: 'absolute', left: '-9999px' }}
>
  {isSaving && 'Saving template...'}
  {saveSuccess && 'Template saved successfully'}
</div>
```

### Recommended Component Structure

```
/apps/web/src/features/admin/email-templates/
├── pages/
│   └── EmailTemplatesAdminPage.tsx
├── components/
│   ├── EmailCategoryPanel.tsx  (reusable per tab)
│   ├── TemplateCard.tsx
│   ├── TemplateEditor.tsx
│   └── ResetConfirmationModal.tsx
├── hooks/
│   ├── useEmailTemplates.ts  (React Query hook)
│   └── useVariableValidation.ts
├── services/
│   └── emailTemplates.api.ts
└── types/
    └── emailTemplates.types.ts

/apps/web/src/components/events/
└── EventForm.tsx (update Emails tab panel)
```

---

## 🚀 RECOMMENDED IMPLEMENTATION APPROACH

### Phase 1: Core Structure (4-5 hours)

**Priority 1**:
1. Create `EmailTemplatesAdminPage.tsx` with tabbed interface
2. Implement URL query parameter handling (`?tab=vetting`)
3. Add "Email Templates" card to Admin Dashboard
4. Test navigation: Dashboard → Email Templates page, tab selection persists in URL

**Priority 2**:
1. Create `EmailCategoryPanel.tsx` reusable component
2. Fetch global templates for selected category
3. Render template cards in horizontal scrollable Group
4. Implement card selection state (burgundy border, light background)

**Testing Milestone**: Can navigate to page, select tabs, see template cards, click cards to select.

---

### Phase 2: Editor Panel (6-8 hours)

**Priority 3**:
1. Integrate MantineTiptapEditor component
2. Add subject line TextInput
3. Show editor panel when card selected (slide-in animation)
4. Implement save/cancel buttons with loading states

**Priority 4**:
1. Add variable reference display (Available variables: {{...}})
2. Implement variable validation logic:
   - Extract `{{variable_name}}` patterns from subject + body
   - Compare to category-specific allowed variables
   - Show yellow Alert if unknown variables detected
3. Allow saving despite warnings (non-blocking)

**Testing Milestone**: Can edit templates, see variable warnings, save changes successfully.

---

### Phase 3: EventForm Integration (3-4 hours)

**Priority 5**:
1. Update EventForm Emails tab to fetch global + event-specific templates
2. Add badge indicators:
   - "✓ Customized" (green) if event-specific exists
   - "(Default)" (gray) if using global
3. Show "Reset to Default" button when customized

**Priority 6**:
1. Create ResetConfirmationModal component
2. Implement reset logic:
   - Show confirmation modal
   - On confirm: DELETE EventEmailTemplate record
   - Re-fetch templates
   - Update badges
   - Show success notification

**Testing Milestone**: Event organizers can customize templates, see badges, reset to default.

---

### Phase 4: Polish & Accessibility (2-3 hours)

**Priority 7**:
1. Add animations:
   - Card hover effect (translateY(-2px))
   - Editor panel slide-in (slideDown keyframe)
   - Tab active state transition
2. Add ARIA labels and keyboard navigation
3. Add error handling (API failures, network errors)
4. Add success/error notifications (Mantine notifications)

**Priority 8**:
1. Responsive testing (desktop, tablet)
2. Accessibility audit (keyboard nav, screen readers, color contrast)
3. Cross-browser testing (Chrome, Firefox, Safari)

**Testing Milestone**: All animations smooth, keyboard accessible, no accessibility warnings.

---

### Testing Strategy

**Unit Tests**:
- `extractVariables()` utility function
- `useVariableValidation` hook
- Template card click handlers

**Component Tests**:
- EmailTemplatesAdminPage renders tabs correctly
- EmailCategoryPanel fetches and displays templates
- TemplateEditor validates variables
- ResetConfirmationModal shows/hides correctly

**Integration Tests**:
- Full workflow: Navigate → Select tab → Select template → Edit → Save
- Full workflow: EventForm → Emails tab → Customize → Reset
- API integration: Mock API calls, verify request/response

**E2E Tests** (Playwright):
- Admin manages global Vetting template
- Admin manages global Event template
- Event organizer customizes event template
- Event organizer resets to default
- Variable validation warnings appear correctly

---

## ⚠️ KNOWN DESIGN CONSTRAINTS

### Constraint 1: MantineTiptapEditor Limitations

**Issue**: MantineTiptapEditor may not have built-in "Insert Variable" dropdown.

**Workaround Options**:
1. **Option A**: Add custom toolbar button that inserts `{{variable_name}}` at cursor
2. **Option B**: Show variable reference panel below editor, users copy/paste manually
3. **Option C**: Extend MantineTiptapEditor with custom extension for variable insertion

**Recommendation**: Start with Option B (simplest), upgrade to Option A if stakeholders request it.

---

### Constraint 2: Responsive Design Limitation

**Issue**: Admin UI design optimized for desktop/tablet (≥768px), not phone.

**Rationale**:
- Email template editing is admin task (typically desktop)
- Rich text editor poor UX on phone (<768px)
- Variable validation, multi-field editing requires larger screen

**Mobile Handling**: Show message "Email template management requires desktop or tablet device"

**Exception**: Event organizers might customize on phone (EventForm context). If needed:
- Stack cards vertically (no horizontal scroll)
- Full-width editor
- Simplified TipTap toolbar
- Buttons stack vertically (Save on top)

---

### Constraint 3: Variable Sets Data Source

**Issue**: Not yet decided if variable sets stored in database or hardcoded in frontend.

**Options**:
1. **Database**: JSONB field in GlobalEmailTemplates table (recommended)
2. **Frontend**: Hardcoded constants in React (easier for MVP, harder to maintain)
3. **Separate API**: `/api/email-templates/variables/{category}` endpoint

**Impact on UI**:
- If database: Fetch variables from API when loading template
- If frontend: Import constants from shared file

**Recommendation for UI**: Assume API endpoint exists. Fetch variables when template selected. If backend implements JSONB in GlobalEmailTemplates, frontend code stays same (just fetch from template DTO).

---

## 🔗 NEXT AGENT INSTRUCTIONS

### For Functional Spec Agent (Phase 2):

1. **FIRST**: Read UI Design document (this document references it)
2. **SECOND**: Review Business Requirements (comprehensive requirements)
3. **THIRD**: Design API specifications:
   - 10 endpoints listed in "What Functional Spec Needs to Address" section
   - Request/response DTOs
   - Error responses (400, 401, 403, 404, 500)
   - Validation rules (subject max 200 chars, HTML sanitization)
4. **FOURTH**: Define business logic services:
   - GlobalEmailTemplateService
   - EventEmailTemplateService
   - AdHocEmailService
   - VariableValidationService
5. **FIFTH**: Document variable sets per category (5 categories, complete lists)
6. **VALIDATE**: NSwag configuration generates TypeScript types correctly

---

### For Database Designer (Phase 2):

1. **FIRST**: Read UI Design document (query patterns in "What Database Design Needs to Support")
2. **SECOND**: Review Business Requirements handoff (data model decisions)
3. **THIRD**: Design database schema:
   - GlobalEmailTemplates table
   - EventEmailTemplates table
   - SentAdHocEmails table
   - Unique constraints, foreign keys, indexes
4. **FOURTH**: Create migration script:
   - Create 3 new tables
   - Seed 22 default templates (6 Vetting + 7 Events + 4 Admin + 4 Incident + 1 Ad Hoc)
   - Migrate existing VettingEmailTemplates → GlobalEmailTemplates (if needed)
5. **VALIDATE**: Query patterns perform efficiently (check execution plans)

---

### For React Developer (Phase 3):

1. **FIRST**: Read UI Design document (complete component specs)
2. **SECOND**: Review this handoff (state management, Mantine components)
3. **THIRD**: Implement recommended approach (Phase 1-4)
4. **FOURTH**: Use exact Mantine component configurations from UI design
5. **FIFTH**: Follow accessibility requirements (ARIA labels, keyboard nav)
6. **VALIDATE**: All wireframes match implemented UI

---

## 📝 TERMINOLOGY REFERENCE

| Term | Definition |
|------|------------|
| **Global Template** | Default email template in GlobalEmailTemplates table, used by all events unless overridden |
| **Event-Specific Template** | Customized email template in EventEmailTemplates table, overrides global for specific event |
| **Copy-on-Edit** | Pattern where EventEmailTemplate created only when user saves changes (not pre-created) |
| **Reset to Default** | Delete EventEmailTemplate record, revert to global template |
| **Badge Indicator** | "✓ Customized" (green) or "(Default)" (gray) shown on template cards in EventForm |
| **Variable Validation** | Check `{{variable_name}}` patterns against category-specific allowed variables, show warnings if unknown |
| **Category** | High-level email grouping: Vetting, Events, Admin, Incident, Ad Hoc |
| **Template Type** | Specific email purpose within category (e.g., EventTemplateType.Confirmation) |
| **Tab** | UI navigation element for switching between categories in admin page |
| **EmailCategoryPanel** | Reusable component showing template cards + editor for one category |

---

## 🤝 HANDOFF CONFIRMATION

**Previous Agent**: Business Requirements Agent
**Previous Phase Completed**: 2025-11-09 (Phase 1 - Requirements)

**Current Agent**: UI Designer Agent
**Current Phase Completed**: 2025-11-09 (Phase 2 - UI Design)

**Key UI Design Decisions**:
1. Reuse EventForm Email Tab pattern (template cards + editor)
2. Tabbed interface (5 categories)
3. Badge indicators for customization status ("✓ Customized" vs "(Default)")
4. Reset to Default with confirmation modal
5. Variable validation warnings (non-blocking)
6. MantineTiptapEditor integration for rich text editing
7. Desktop/tablet optimized (≥768px), mobile shows unsupported message

**Next Agent Should Be**: Functional Spec Agent OR Database Designer (parallel work possible)
**Next Phase**: Phase 2 - Design (continued)

**Estimated Effort for Implementation**:
- EmailTemplatesAdminPage: 4-5 hours
- EmailCategoryPanel: 6-8 hours
- EventForm Integration: 3-4 hours
- Polish & Accessibility: 2-3 hours
- **Total Phase 3 (Implementation)**: ~15-20 hours

---

**END OF HANDOFF DOCUMENT**
