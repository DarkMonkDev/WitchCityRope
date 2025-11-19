<!-- Last Updated: 2025-11-18 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Complete -->

# UI Design: Send Ad-Hoc Email Feature

**Feature**: Email Admin Enhancement - Send emails to user segments
**Location**: Email Templates Admin Page > Ad Hoc Tab
**Design Date**: 2025-11-18
**Designer**: UI Designer Agent

## Design Overview

This document specifies the UI design for adding "Send Ad-Hoc Email" functionality to the existing Email Templates Admin page. The feature allows administrators to compose and send custom emails to specific user segments (e.g., all vetted members, email not verified users, etc.).

### Design Goals
1. **Seamless Integration**: Blend with existing Email Templates Admin UI patterns
2. **Clear User Flow**: Guide admin through recipient selection → content creation → confirmation → send
3. **Error Prevention**: Show recipient preview, variable validation, confirmation dialog
4. **Consistent Styling**: Use Mantine v7 components and Design System v7 patterns
5. **Mobile-Responsive**: Work on all devices with touch-friendly targets

---

## Current State Analysis

### Existing Email Templates Admin Page
- **Location**: `/apps/web/src/pages/admin/EmailTemplatesAdminPage.tsx`
- **Pattern**: Tabbed interface with 5 tabs (Vetting, Events, Admin, Incident, Ad Hoc)
- **Component**: Uses `EmailCategoryPanel` for each tab
- **Styling**: Burgundy color scheme (#880124), Montserrat headings, clean card layouts
- **Editor**: Uses `MantineTiptapEditor` for rich text email content

### Ad Hoc Tab Current Functionality
- Shows template cards for Ad Hoc category
- Allows editing existing Ad Hoc templates
- Does NOT currently support sending emails

### Design Patterns to Follow
From existing `EmailCategoryPanel.tsx`:
- **Template Cards**: Horizontal card layout with click-to-edit
- **Editor Panel**: Paper component with shadow, appears below cards when template selected
- **Variable Display**: Light burgundy box showing available variables
- **Validation**: Real-time variable validation with yellow alerts
- **Buttons**: Cancel (light) + Save (primary) right-aligned

---

## Wireframes

### Desktop Layout (≥769px)

```
┌─ Ad Hoc Tab ─────────────────────────────────────────────────────────────────────┐
│                                                                                    │
│  [Existing Template Cards - Horizontal Scrollable]                                │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐                                        │
│  │ Template │  │ Template │  │ Template │                                        │
│  │    1     │  │    2     │  │    3     │                                        │
│  └──────────┘  └──────────┘  └──────────┘                                        │
│                                                                                    │
│  [Editor Panel - Appears when template selected]                                  │
│  (Existing EmailCategoryPanel edit functionality)                                 │
│                                                                                    │
│ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ │
│                                                                                    │
│  📧 Send Ad-Hoc Email                                                             │
│  Send custom emails to specific user groups                                       │
│                                                                                    │
│  ┌─ Recipient Selection ─────────────────────────────────────────────────────┐   │
│  │                                                                             │   │
│  │  Select Recipients: *                                                       │   │
│  │  ┌────────────────────────────────────────────────────────────────────┐   │   │
│  │  │ Email Not Verified (142 users)                                ▼   │   │   │
│  │  └────────────────────────────────────────────────────────────────────┘   │   │
│  │                                                                             │   │
│  │  📊 Selected: 142 recipients                                               │   │
│  │                                                                             │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                    │
│  ┌─ Email Content ─────────────────────────────────────────────────────────────┐ │
│  │                                                                               │ │
│  │  Subject Line: *                                                              │ │
│  │  ┌─────────────────────────────────────────────────────────────────────┐    │ │
│  │  │ Welcome Back - Set Your Password                                    │    │ │
│  │  └─────────────────────────────────────────────────────────────────────┘    │ │
│  │                                                                               │ │
│  │  Available Variables:                                                         │ │
│  │  {{user_name}}, {{reset_url}}, {{verification_url}}                          │ │
│  │                                                                               │ │
│  │  Email Content (HTML): *                                                      │ │
│  │  ┌─────────────────────────────────────────────────────────────────────┐    │ │
│  │  │ [Rich Text Editor - MantineTiptapEditor]                            │    │ │
│  │  │                                                                       │    │ │
│  │  │ Hello {{user_name}},                                                 │    │ │
│  │  │                                                                       │    │ │
│  │  │ We've noticed your email isn't verified yet. Please click below...  │    │ │
│  │  │                                                                       │    │ │
│  │  │                                                                       │    │ │
│  │  │                                                                       │    │ │
│  │  └─────────────────────────────────────────────────────────────────────┘    │ │
│  │                                                                               │ │
│  └───────────────────────────────────────────────────────────────────────────────┘ │
│                                                                                    │
│  ┌─ Preview Recipients ─────────────────────────────────────────────────────────┐ │
│  │                                                                               │ │
│  │  First 10 of 142 recipients:                                                 │ │
│  │  • user1@example.com (SceneName1)                                            │ │
│  │  • user2@example.com (SceneName2)                                            │ │
│  │  • user3@example.com (SceneName3)                                            │ │
│  │  ... and 132 more                                                            │ │
│  │                                                                               │ │
│  └───────────────────────────────────────────────────────────────────────────────┘ │
│                                                                                    │
│  [ Cancel ]                                                   [ Send Email → ]   │
│                                                                                    │
└────────────────────────────────────────────────────────────────────────────────────┘
```

### Mobile Layout (<768px)

```
┌─ Ad Hoc Tab ───────────────────────┐
│                                     │
│ [Template Cards - Vertical Stack]  │
│ ┌─────────────────────────────────┐ │
│ │ Template 1                      │ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ Template 2                      │ │
│ └─────────────────────────────────┘ │
│                                     │
│ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ │
│                                     │
│ 📧 Send Ad-Hoc Email                │
│ Send emails to user groups          │
│                                     │
│ ┌─ Recipients ─────────────────┐   │
│ │ Select Recipients: *          │   │
│ │ ┌───────────────────────────┐ │   │
│ │ │ Email Not Verified (142) ▼│ │   │
│ │ └───────────────────────────┘ │   │
│ │                               │   │
│ │ 📊 Selected: 142 recipients   │   │
│ └───────────────────────────────┘   │
│                                     │
│ ┌─ Email ──────────────────────┐   │
│ │ Subject: *                    │   │
│ │ ┌───────────────────────────┐ │   │
│ │ │ Welcome Back...           │ │   │
│ │ └───────────────────────────┘ │   │
│ │                               │   │
│ │ Variables:                    │   │
│ │ {{user_name}}, {{reset_url}}  │   │
│ │                               │   │
│ │ Content: *                    │   │
│ │ ┌───────────────────────────┐ │   │
│ │ │ [Rich Text Editor]        │ │   │
│ │ │ (Smaller toolbar on mobile)│ │   │
│ │ │                           │ │   │
│ │ └───────────────────────────┘ │   │
│ └───────────────────────────────┘   │
│                                     │
│ ┌─ Preview ─────────────────────┐   │
│ │ First 10 of 142:              │   │
│ │ • user1@example.com (Name1)   │   │
│ │ • user2@example.com (Name2)   │   │
│ │ ... and 132 more              │   │
│ └───────────────────────────────┘   │
│                                     │
│ ┌───────────────────────────────┐   │
│ │    Send Email →               │   │
│ └───────────────────────────────┘   │
│ ┌───────────────────────────────┐   │
│ │    Cancel                     │   │
│ └───────────────────────────────┘   │
│                                     │
└─────────────────────────────────────┘
```

### Confirmation Dialog (Both Desktop & Mobile)

```
┌──────────────────────────────────────┐
│  ⚠ Confirm Send                      │
│  ─────────────────────────────────   │
│                                       │
│  You are about to send this email    │
│  to 142 recipients. This cannot be   │
│  undone.                              │
│                                       │
│  Segment: Email Not Verified          │
│  Recipients: 142 users                │
│                                       │
│  Subject: Welcome Back - Set Your     │
│           Password                    │
│                                       │
│  ────────────────────────────────    │
│                                       │
│  [ Cancel ]          [ Send Now ]    │
│                                       │
└──────────────────────────────────────┘
```

---

## Component Breakdown

### 1. SendAdHocEmailSection (New Component)

**Purpose**: Main container for send ad-hoc email functionality
**Location**: Added to `EmailCategoryPanel.tsx` when `category === 'AdHoc'`

**Structure**:
```tsx
<Stack gap="xl" mt="3xl">
  <Divider size="md" />

  {/* Section Header */}
  <Box>
    <Title order={2}>📧 Send Ad-Hoc Email</Title>
    <Text size="sm" c="dimmed">Send custom emails to specific user groups</Text>
  </Box>

  {/* Recipient Selector */}
  <RecipientSelector />

  {/* Email Content Editor */}
  <EmailContentEditor />

  {/* Recipient Preview */}
  <RecipientPreview />

  {/* Action Buttons */}
  <SendActions />

  {/* Confirmation Modal */}
  <SendConfirmationModal />
</Stack>
```

---

### 2. RecipientSelector

**Props**:
```tsx
interface RecipientSelectorProps {
  selectedSegment: UserSegment | null;
  onSegmentChange: (segment: UserSegment | null) => void;
  recipientCount: number;
}
```

**Visual Spec**:
- **Component**: Mantine `Select`
- **Label**: "Select Recipients:" with red asterisk (required)
- **Placeholder**: "Choose a user group..."
- **Options**: 8 segment types with recipient counts
- **Display**: Count badge next to each option
- **Selected Display**: Large text showing "📊 Selected: X recipients"

**Segment Options**:
```tsx
const segmentOptions = [
  { value: 'AllVettedMembers', label: 'All Vetted Members (142 users)' },
  { value: 'AllPreVettedMembers', label: 'All Pre-Vetted Members (158 users)' },
  { value: 'AllTeachers', label: 'All Teachers (15 users)' },
  { value: 'AllDMs', label: 'All DMs (8 users)' },
  { value: 'AllSafetyTeam', label: 'All Safety Team (6 users)' },
  { value: 'AllAdmins', label: 'All Admins (5 users)' },
  { value: 'EmailNotVerified', label: 'Email Not Verified (142 users)' },
  { value: 'VettingPending', label: 'Vetting Pending (23 users)' },
];
```

**State Management**:
```tsx
const [selectedSegment, setSelectedSegment] = useState<UserSegment | null>(null);
const [recipientCount, setRecipientCount] = useState(0);

// Fetch segment counts on mount
const { data: segmentCounts } = useQuery({
  queryKey: ['email-segments'],
  queryFn: () => emailTemplatesApi.getSegments(),
});
```

**Styling**:
```tsx
<Paper p="md" withBorder style={{ borderColor: 'rgba(136, 1, 36, 0.1)' }}>
  <Stack gap="md">
    <Select
      label="Select Recipients:"
      required
      data={segmentOptions}
      value={selectedSegment}
      onChange={setSelectedSegment}
      searchable
      maxDropdownHeight={300}
      styles={{
        label: {
          fontFamily: 'Montserrat, sans-serif',
          fontWeight: 600,
          fontSize: '14px',
          color: '#880124',
          marginBottom: '8px',
        },
      }}
    />

    {selectedSegment && (
      <Text size="lg" fw={600} c="burgundy">
        📊 Selected: {recipientCount} recipients
      </Text>
    )}
  </Stack>
</Paper>
```

---

### 3. EmailContentEditor

**Props**:
```tsx
interface EmailContentEditorProps {
  subject: string;
  htmlBody: string;
  onSubjectChange: (value: string) => void;
  onBodyChange: (value: string) => void;
  availableVariables: string[];
  invalidVariables: string[];
}
```

**Visual Spec**:
- **Subject Input**: Mantine `TextInput`, max 200 chars, required
- **Variable Display**: Same pattern as existing template editor (burgundy info box)
- **Rich Text Editor**: `MantineTiptapEditor` component (already used in EmailCategoryPanel)
- **Validation**: Real-time variable validation with yellow alert

**Available Variables**:
```tsx
const AD_HOC_VARIABLES = [
  '{{user_name}}',      // User's scene name
  '{{reset_url}}',      // Password reset link
  '{{verification_url}}' // Email verification link
];
```

**Component Structure**:
```tsx
<Paper p="xl" withBorder style={{ borderColor: 'rgba(136, 1, 36, 0.1)' }}>
  <Stack gap="md">
    {/* Subject Line */}
    <TextInput
      label="Subject Line"
      required
      value={subject}
      onChange={(e) => setSubject(e.currentTarget.value)}
      maxLength={200}
      placeholder="Enter email subject..."
      styles={{
        label: {
          fontFamily: 'Montserrat, sans-serif',
          fontWeight: 600,
          fontSize: '14px',
          color: '#880124',
        },
      }}
    />

    {/* Available Variables */}
    <Box
      p="sm"
      style={{
        background: 'rgba(136, 1, 36, 0.05)',
        borderRadius: '6px',
        border: '1px solid rgba(136, 1, 36, 0.1)',
      }}
    >
      <Text size="xs" fw={600} c="burgundy" mb={4}>
        Available Variables:
      </Text>
      <Text size="xs" c="dimmed">
        {AD_HOC_VARIABLES.join(', ')}
      </Text>
    </Box>

    {/* Rich Text Editor */}
    <div>
      <Text size="sm" fw={500} mb={4}>
        Email Content (HTML) *
      </Text>
      <MantineTiptapEditor
        value={htmlBody}
        onChange={setHtmlBody}
        placeholder="Compose your email message..."
        minRows={12}
      />
    </div>

    {/* Variable Validation Warning */}
    {invalidVariables.length > 0 && (
      <Alert
        icon={<IconAlertCircle />}
        color="yellow"
        variant="light"
        title="Unknown Variables Detected"
      >
        <Text size="sm">
          These variables are not in the allowed list: {invalidVariables.join(', ')}
        </Text>
        <Text size="xs" mt="xs" c="dimmed">
          Available variables: {AD_HOC_VARIABLES.join(', ')}
        </Text>
      </Alert>
    )}
  </Stack>
</Paper>
```

---

### 4. RecipientPreview

**Props**:
```tsx
interface RecipientPreviewProps {
  segment: UserSegment | null;
  previewRecipients: PreviewRecipient[];
  totalCount: number;
}

interface PreviewRecipient {
  email: string;
  sceneName: string;
}
```

**Visual Spec**:
- **Component**: Mantine `Paper` with light border
- **Display**: First 10 recipients as bulleted list
- **Format**: `email (SceneName)` per line
- **Overflow**: "... and X more" text at bottom

**Component Structure**:
```tsx
<Paper p="md" withBorder style={{ borderColor: 'rgba(136, 1, 36, 0.1)' }}>
  <Stack gap="xs">
    <Text size="sm" fw={600} c="burgundy">
      Preview Recipients
    </Text>

    {previewRecipients.length > 0 ? (
      <>
        <Text size="xs" c="dimmed" mb="xs">
          First {Math.min(10, totalCount)} of {totalCount} recipients:
        </Text>

        <Stack gap={4}>
          {previewRecipients.slice(0, 10).map((recipient, index) => (
            <Text key={index} size="sm" c="charcoal">
              • {recipient.email} ({recipient.sceneName})
            </Text>
          ))}
        </Stack>

        {totalCount > 10 && (
          <Text size="sm" c="dimmed" style={{ fontStyle: 'italic' }}>
            ... and {totalCount - 10} more
          </Text>
        )}
      </>
    ) : (
      <Text size="sm" c="dimmed" style={{ fontStyle: 'italic' }}>
        Select a recipient segment to see preview
      </Text>
    )}
  </Stack>
</Paper>
```

**API Integration**:
```tsx
// Fetch preview when segment changes
const { data: previewRecipients } = useQuery({
  queryKey: ['email-segment-preview', selectedSegment],
  queryFn: () =>
    selectedSegment
      ? emailTemplatesApi.getSegmentPreview(selectedSegment)
      : Promise.resolve([]),
  enabled: selectedSegment !== null,
});
```

---

### 5. SendActions

**Props**:
```tsx
interface SendActionsProps {
  onCancel: () => void;
  onSend: () => void;
  isSending: boolean;
  isValid: boolean;
}
```

**Visual Spec**:
- **Desktop**: Right-aligned horizontal group
- **Mobile**: Full-width stacked buttons (Send on top, Cancel below)
- **Disabled State**: Send button disabled if form invalid or sending in progress

**Validation Rules**:
```tsx
const isValid =
  selectedSegment !== null &&
  subject.trim().length > 0 &&
  htmlBody.trim().length > 0 &&
  invalidVariables.length === 0;
```

**Component Structure**:
```tsx
{/* Desktop */}
<Group justify="flex-end" gap="sm" visibleFrom="sm">
  <Button
    variant="light"
    onClick={handleCancel}
    disabled={isSending}
  >
    Cancel
  </Button>

  <Button
    onClick={handleSendClick}
    loading={isSending}
    disabled={!isValid || isSending}
    rightSection={<IconSend size={16} />}
    styles={{
      root: {
        fontWeight: 600,
        height: '44px',
        fontSize: '14px',
      },
    }}
  >
    Send Email
  </Button>
</Group>

{/* Mobile */}
<Stack gap="sm" hiddenFrom="sm">
  <Button
    onClick={handleSendClick}
    loading={isSending}
    disabled={!isValid || isSending}
    fullWidth
    rightSection={<IconSend size={16} />}
    styles={{
      root: {
        fontWeight: 600,
        height: '48px',
        fontSize: '16px',
      },
    }}
  >
    Send Email
  </Button>

  <Button
    variant="light"
    onClick={handleCancel}
    disabled={isSending}
    fullWidth
  >
    Cancel
  </Button>
</Stack>
```

---

### 6. SendConfirmationModal

**Props**:
```tsx
interface SendConfirmationModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  segment: UserSegment;
  recipientCount: number;
  subject: string;
}
```

**Visual Spec**:
- **Component**: Mantine `Modal`, centered
- **Title**: "⚠ Confirm Send"
- **Size**: `md` (500px)
- **Content**: Warning text, segment details, subject preview
- **Actions**: Cancel (light) + Send Now (primary)

**Component Structure**:
```tsx
<Modal
  opened={isOpen}
  onClose={onClose}
  title="⚠ Confirm Send"
  centered
  size="md"
  styles={{
    title: {
      fontFamily: 'Montserrat, sans-serif',
      fontWeight: 700,
      fontSize: '20px',
      color: '#880124',
    },
  }}
>
  <Stack gap="md">
    {/* Warning Message */}
    <Alert color="yellow" variant="light" icon={<IconAlertCircle />}>
      <Text size="sm" fw={600}>
        You are about to send this email to {recipientCount} recipients.
        This action cannot be undone.
      </Text>
    </Alert>

    {/* Send Details */}
    <Paper p="sm" withBorder style={{ borderColor: 'rgba(136, 1, 36, 0.1)' }}>
      <Stack gap="xs">
        <Group gap="xs">
          <Text size="sm" fw={600} c="burgundy">Segment:</Text>
          <Text size="sm">{segmentDisplayName}</Text>
        </Group>

        <Group gap="xs">
          <Text size="sm" fw={600} c="burgundy">Recipients:</Text>
          <Text size="sm">{recipientCount} users</Text>
        </Group>

        <Box>
          <Text size="sm" fw={600} c="burgundy" mb={4}>Subject:</Text>
          <Text size="sm" c="dimmed">{subject}</Text>
        </Box>
      </Stack>
    </Paper>

    {/* Action Buttons */}
    <Group justify="flex-end" gap="sm" mt="md">
      <Button variant="light" onClick={onClose}>
        Cancel
      </Button>

      <Button
        onClick={onConfirm}
        color="burgundy"
        styles={{
          root: {
            fontWeight: 600,
            height: '44px',
          },
        }}
      >
        Send Now
      </Button>
    </Group>
  </Stack>
</Modal>
```

---

## State Management

### Component State

```tsx
const SendAdHocEmailSection: React.FC = () => {
  // Segment selection
  const [selectedSegment, setSelectedSegment] = useState<UserSegment | null>(null);

  // Email content
  const [subject, setSubject] = useState('');
  const [htmlBody, setHtmlBody] = useState('');

  // Validation
  const [invalidVariables, setInvalidVariables] = useState<string[]>([]);

  // Modal state
  const [showConfirmModal, setShowConfirmModal] = useState(false);

  // React Query hooks
  const queryClient = useQueryClient();

  // Fetch segment counts
  const { data: segments } = useQuery({
    queryKey: ['email-segments'],
    queryFn: () => emailTemplatesApi.getSegments(),
  });

  // Fetch preview recipients
  const { data: previewRecipients } = useQuery({
    queryKey: ['email-segment-preview', selectedSegment],
    queryFn: () =>
      selectedSegment
        ? emailTemplatesApi.getSegmentPreview(selectedSegment)
        : Promise.resolve([]),
    enabled: selectedSegment !== null,
  });

  // Send email mutation
  const sendMutation = useMutation({
    mutationFn: (data: SendAdHocEmailRequest) =>
      emailTemplatesApi.sendAdHocEmail(data),
    onSuccess: () => {
      notifications.show({
        message: `Email sent to ${segments?.find(s => s.name === selectedSegment)?.count} recipients successfully`,
        color: 'green',
        icon: <IconCheck />,
      });
      handleReset();
    },
    onError: (error: any) => {
      notifications.show({
        message: error.message || 'Failed to send email',
        color: 'red',
        icon: <IconAlertCircle />,
      });
    },
  });

  // Handlers
  const handleSendClick = () => setShowConfirmModal(true);

  const handleConfirmSend = () => {
    sendMutation.mutate({
      segment: selectedSegment!,
      subject,
      htmlBody,
    });
    setShowConfirmModal(false);
  };

  const handleCancel = () => {
    if (subject || htmlBody) {
      if (window.confirm('Discard unsaved email?')) {
        handleReset();
      }
    } else {
      handleReset();
    }
  };

  const handleReset = () => {
    setSelectedSegment(null);
    setSubject('');
    setHtmlBody('');
    setInvalidVariables([]);
  };

  // Real-time variable validation
  useEffect(() => {
    // Same pattern as EmailCategoryPanel variable validation
    // Extract variables from subject + htmlBody
    // Compare with AD_HOC_VARIABLES
    // Set invalidVariables state
  }, [subject, htmlBody]);

  // Computed validation
  const isValid =
    selectedSegment !== null &&
    subject.trim().length > 0 &&
    htmlBody.trim().length > 0 &&
    invalidVariables.length === 0;

  return (
    // Component render
  );
};
```

---

## API Integration Points

### 1. GET /api/email-templates/segments

**Purpose**: Fetch all user segments with recipient counts

**Response**:
```typescript
interface UserSegmentDto {
  name: UserSegment;
  displayName: string;
  count: number;
}

// Example:
[
  { name: 'AllVettedMembers', displayName: 'All Vetted Members', count: 142 },
  { name: 'EmailNotVerified', displayName: 'Email Not Verified', count: 142 },
  // ... other segments
]
```

**Usage**:
```tsx
const { data: segments } = useQuery({
  queryKey: ['email-segments'],
  queryFn: () => emailTemplatesApi.getSegments(),
});
```

---

### 2. GET /api/email-templates/segments/{name}/preview

**Purpose**: Fetch first 10 recipients from segment for preview

**Parameters**:
- `name`: UserSegment enum value

**Response**:
```typescript
interface PreviewRecipientDto {
  email: string;
  sceneName: string;
}

// Example:
[
  { email: 'user1@example.com', sceneName: 'SceneName1' },
  { email: 'user2@example.com', sceneName: 'SceneName2' },
  // ... up to 10 recipients
]
```

**Usage**:
```tsx
const { data: previewRecipients } = useQuery({
  queryKey: ['email-segment-preview', selectedSegment],
  queryFn: () => emailTemplatesApi.getSegmentPreview(selectedSegment!),
  enabled: selectedSegment !== null,
});
```

---

### 3. POST /api/email-templates/ad-hoc/send

**Purpose**: Send ad-hoc email to selected segment

**Request**:
```typescript
interface SendAdHocEmailRequest {
  segment: UserSegment;
  subject: string;
  htmlBody: string;
}
```

**Response**:
```typescript
interface SendAdHocEmailResponse {
  success: boolean;
  sentCount: number;
  failedCount: number;
  errors: string[];
}
```

**Usage**:
```tsx
const sendMutation = useMutation({
  mutationFn: (data: SendAdHocEmailRequest) =>
    emailTemplatesApi.sendAdHocEmail(data),
  onSuccess: (response) => {
    notifications.show({
      message: `Email sent to ${response.sentCount} recipients successfully`,
      color: 'green',
    });
  },
  onError: (error: any) => {
    notifications.show({
      message: error.message || 'Failed to send email',
      color: 'red',
    });
  },
});
```

---

## User Flows

### Happy Path: Send Email to Email Not Verified Users

1. **Navigate**: Admin goes to Email Templates Admin > Ad Hoc tab
2. **Scroll**: Scrolls past existing template editing section
3. **See Section**: "Send Ad-Hoc Email" section visible below divider
4. **Select Segment**: Clicks "Select Recipients" dropdown
5. **Choose**: Selects "Email Not Verified (142 users)"
6. **Count Updates**: Sees "📊 Selected: 142 recipients"
7. **Preview Loads**: Preview section shows first 10 recipients automatically
8. **Enter Subject**: Types "Welcome Back - Set Your Password"
9. **Compose Body**: Uses rich text editor to compose message
10. **Add Variables**: Inserts `{{user_name}}` and `{{reset_url}}` variables
11. **Validation**: No yellow warning (variables are valid)
12. **Click Send**: Clicks "Send Email" button
13. **Confirmation**: Modal appears: "You are about to send this email to 142 recipients..."
14. **Review**: Reviews segment name, count, and subject in modal
15. **Confirm**: Clicks "Send Now" button
16. **Loading**: Button shows loading spinner
17. **Success**: Green notification appears: "Email sent to 142 recipients successfully"
18. **Form Reset**: All fields clear automatically

---

### Error Scenario 1: Invalid Variable Used

1. Admin selects segment
2. Admin enters subject and body
3. Admin types `{{event_name}}` variable (not allowed in ad-hoc emails)
4. **Yellow Alert Appears**: "Unknown Variables Detected"
5. Alert shows: "These variables are not in the allowed list: {{event_name}}"
6. Alert shows: "Available variables: {{user_name}}, {{reset_url}}, {{verification_url}}"
7. **Send Button Disabled**: Cannot send until fixed
8. Admin removes invalid variable
9. Alert disappears
10. Send button becomes enabled

---

### Error Scenario 2: Network Failure During Send

1. Admin completes form and clicks "Send Email"
2. Confirmation modal appears
3. Admin clicks "Send Now"
4. **Network request fails** (timeout, 500 error, etc.)
5. **Red Notification Appears**: "Failed to send email: Network error"
6. Modal closes
7. **Form preserves content** (subject and body still filled)
8. Admin can retry or fix issue

---

### Error Scenario 3: Empty Form Submission Prevented

1. Admin clicks "Select Recipients" but doesn't choose segment
2. Admin clicks "Send Email" button
3. **Button is Disabled** (validation prevents click)
4. **Visual Cue**: Button has reduced opacity, cursor shows not-allowed
5. Admin selects segment
6. Admin enters subject
7. Admin enters body content
8. **Button Becomes Enabled** (all validation passes)
9. Admin can now send

---

### Cancellation Flow

1. Admin fills out form partially
2. Admin clicks "Cancel" button
3. **Confirmation Dialog**: Browser confirm dialog appears: "Discard unsaved email?"
4. **User Chooses**:
   - If "Cancel" (keep editing): Returns to form with content preserved
   - If "OK" (discard): Form resets, all fields clear
5. Form returns to empty state

---

## Responsive Behavior

### Desktop (≥769px)

**Layout**:
- Two-column grid NOT used (full width like existing editor panel)
- Horizontal button group (Cancel left, Send right)
- Rich text editor full toolbar visible
- Preview recipients list shows 10 items

**Spacing**:
- Section padding: `xl` (40px)
- Card gaps: `md` (24px)
- Button gap: `sm` (16px)

---

### Tablet (768px)

**Layout**:
- Same as desktop (full width)
- Buttons remain horizontal
- Toolbar may wrap to 2 lines

---

### Mobile (<768px)

**Layout**:
- Full width sections (20px padding)
- Stacked button layout (Send on top, Cancel below)
- Rich text editor simplified toolbar (collapsed groups)
- Preview recipients shows 5 items (instead of 10)

**Touch Targets**:
- All buttons: 48×48px minimum
- Dropdown: 44px height
- Input fields: 44px height

**Font Sizes**:
- Section title: 24px (reduced from 32px)
- Body text: 14px
- Button text: 16px

---

## Accessibility Requirements

### WCAG 2.1 AA Compliance

**Color Contrast**:
- Text on white: 4.5:1 minimum (charcoal #2B2B2B on white)
- Burgundy text: 8.5:1 (exceeds AAA)
- Disabled button: 4.8:1 (meets AA)

**Keyboard Navigation**:
- Tab order: Segment dropdown → Subject input → Rich text editor → Preview (skip) → Cancel → Send
- Enter/Space: Activate buttons and submit forms
- Escape: Close modal
- Arrow keys: Navigate dropdown options

**Screen Reader Support**:
```tsx
<Select
  aria-label="Select recipient segment"
  aria-required="true"
  aria-describedby="segment-help-text"
/>

<TextInput
  aria-label="Email subject line"
  aria-required="true"
  aria-invalid={subject.length === 0}
/>

<Button
  aria-label="Send email to selected recipients"
  aria-busy={isSending}
  aria-disabled={!isValid}
/>
```

**Focus Management**:
- Visible focus rings on all interactive elements
- Focus trap in confirmation modal
- Auto-focus subject input when segment selected

**Error Announcements**:
```tsx
<div role="alert" aria-live="polite">
  {invalidVariables.length > 0 && (
    <Text>Unknown variables detected: {invalidVariables.join(', ')}</Text>
  )}
</div>
```

---

## Design Tokens

### Colors (Design System v7)

```css
/* Primary Brand */
--color-burgundy: #880124;
--color-burgundy-light: #9F1D35;
--color-burgundy-dark: #660018;

/* Neutral */
--color-charcoal: #2B2B2B;
--color-smoke: #4A4A4A;
--color-stone: #8B8680;
--color-taupe: #B8B0A8;
--color-ivory: #FFF8F0;
--color-cream: #FAF6F2;

/* Status */
--color-success: #228B22;
--color-warning: #DAA520;
--color-error: #DC143C;
```

### Spacing

```css
--space-xs: 8px;
--space-sm: 16px;
--space-md: 24px;
--space-lg: 32px;
--space-xl: 40px;
--space-2xl: 48px;
--space-3xl: 64px;
```

### Typography

```css
/* Font Families */
--font-heading: 'Montserrat', sans-serif;
--font-body: 'Source Sans 3', sans-serif;

/* Sizes */
--text-xs: 12px;
--text-sm: 14px;
--text-md: 16px;
--text-lg: 18px;
--text-xl: 20px;
--text-2xl: 24px;
--text-3xl: 32px;

/* Weights */
--font-regular: 400;
--font-medium: 500;
--font-semibold: 600;
--font-bold: 700;
--font-extrabold: 800;
```

### Border Radius

```css
--radius-sm: 6px;
--radius-md: 8px;
--radius-lg: 12px;
--radius-xl: 16px;
```

### Shadows

```css
--shadow-sm: 0 1px 3px rgba(0, 0, 0, 0.1);
--shadow-md: 0 4px 6px rgba(0, 0, 0, 0.1);
--shadow-lg: 0 10px 15px rgba(0, 0, 0, 0.1);
```

---

## Mantine v7 Components Used

| Component | Purpose | Configuration |
|-----------|---------|--------------|
| `Stack` | Vertical layout | gap="xl", gap="md" |
| `Group` | Horizontal layout | justify="flex-end", justify="space-between" |
| `Paper` | Card containers | withBorder, p="md", p="xl" |
| `Box` | Generic container | p="sm", style props |
| `Title` | Section headings | order={2}, order={3} |
| `Text` | Body text | size="sm", c="dimmed", fw={600} |
| `Divider` | Section separator | size="md" |
| `Select` | Segment dropdown | searchable, required, maxDropdownHeight |
| `TextInput` | Subject input | maxLength={200}, required |
| `Button` | Actions | variant="light", loading, disabled, rightSection |
| `Alert` | Warnings/errors | color="yellow", variant="light", icon |
| `Modal` | Confirmation dialog | centered, size="md", opened, onClose |
| `Loader` | Loading spinner | size="lg" |
| `MantineTiptapEditor` | Rich text editor | Custom component, minRows={12} |

---

## Error Handling

### Validation Errors

**Empty Segment**:
- Send button disabled
- Visual: Reduced opacity, not-allowed cursor
- No error message shown (required field)

**Empty Subject**:
- Send button disabled
- Visual: Reduced opacity
- No error message until blur (native HTML5 validation)

**Empty Body**:
- Send button disabled
- Visual: Reduced opacity

**Invalid Variables**:
- Yellow alert shown below editor
- Lists invalid variables
- Shows available variables
- Send button disabled
- Example: "Unknown variables: {{event_name}}"

---

### Network Errors

**Segment Fetch Failed**:
```tsx
{error && (
  <Alert icon={<IconAlertCircle />} color="red" title="Error Loading Segments">
    <Text size="sm">Failed to load user segments. Please refresh the page.</Text>
    <Button size="xs" variant="light" onClick={() => queryClient.invalidateQueries(['email-segments'])}>
      Retry
    </Button>
  </Alert>
)}
```

**Preview Fetch Failed**:
```tsx
{previewError && (
  <Text size="sm" c="red">
    Failed to load recipient preview. You can still send the email.
  </Text>
)}
```

**Send Failed**:
```tsx
// Notification shown
notifications.show({
  message: error.message || 'Failed to send email. Please try again.',
  color: 'red',
  icon: <IconAlertCircle />,
  autoClose: 5000,
});

// Form NOT reset (user can retry)
// Modal closes automatically
```

---

### Success States

**Email Sent Successfully**:
```tsx
notifications.show({
  message: `Email sent to ${sentCount} recipients successfully`,
  color: 'green',
  icon: <IconCheck />,
  autoClose: 3000,
});

// Form resets automatically
setSelectedSegment(null);
setSubject('');
setHtmlBody('');
```

---

## Integration with Existing Code

### Modify EmailCategoryPanel.tsx

**Add conditional rendering for Ad Hoc category**:

```tsx
export const EmailCategoryPanel: React.FC<EmailCategoryPanelProps> = ({ category }) => {
  // ... existing code ...

  return (
    <Stack gap="xl">
      {/* Existing Template Cards */}
      {/* ... existing template card rendering ... */}

      {/* Existing Editor Panel */}
      {/* ... existing editor panel ... */}

      {/* NEW: Send Ad-Hoc Email Section (only for Ad Hoc category) */}
      {category === 'AdHoc' && (
        <>
          <Divider size="md" my="xl" />
          <SendAdHocEmailSection />
        </>
      )}
    </Stack>
  );
};
```

---

### Create New Component File

**Location**: `/apps/web/src/components/email-templates/SendAdHocEmailSection.tsx`

**Import Structure**:
```tsx
import React, { useState, useEffect } from 'react';
import {
  Stack,
  Group,
  Box,
  Paper,
  Title,
  Text,
  Select,
  TextInput,
  Button,
  Modal,
  Alert,
  Divider,
} from '@mantine/core';
import { IconAlertCircle, IconCheck, IconSend } from '@tabler/icons-react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { MantineTiptapEditor } from '../forms/MantineTiptapEditor';
import { emailTemplatesApi } from '../../services/emailTemplates.api';
import type { UserSegment } from '@witchcityrope/shared-types';
```

---

## Testing Scenarios

### Manual Testing Checklist

**Component Rendering**:
- [ ] Section appears below template editor on Ad Hoc tab
- [ ] Section does NOT appear on other tabs (Vetting, Events, Admin, Incident)
- [ ] All sub-sections visible: Recipients, Email, Preview, Buttons
- [ ] Divider separates template editor from send section

**Segment Selection**:
- [ ] Dropdown shows all 8 segment options
- [ ] Each option shows recipient count
- [ ] Selected count displays prominently
- [ ] Preview loads automatically when segment selected
- [ ] Preview shows first 10 recipients with email and scene name
- [ ] Preview shows "... and X more" when total > 10

**Email Composition**:
- [ ] Subject input accepts text up to 200 chars
- [ ] Rich text editor loads and is functional
- [ ] Variable info box shows correct variables
- [ ] Can insert variables via typing
- [ ] Invalid variables trigger yellow alert
- [ ] Valid variables do not trigger alert

**Form Validation**:
- [ ] Send button disabled when segment not selected
- [ ] Send button disabled when subject empty
- [ ] Send button disabled when body empty
- [ ] Send button disabled when invalid variables present
- [ ] Send button enabled when all validation passes

**Send Flow**:
- [ ] Click "Send Email" opens confirmation modal
- [ ] Modal shows correct segment name, count, subject
- [ ] "Cancel" in modal closes without sending
- [ ] "Send Now" in modal triggers API call
- [ ] Loading spinner shows during send
- [ ] Success notification appears on success
- [ ] Form resets after successful send
- [ ] Error notification appears on failure
- [ ] Form preserves content after failure (can retry)

**Responsive Behavior**:
- [ ] Desktop: Buttons horizontal, full toolbar
- [ ] Mobile: Buttons stacked, simplified toolbar
- [ ] Mobile: Touch targets 48×48px minimum
- [ ] Mobile: Preview shows 5 items instead of 10

**Accessibility**:
- [ ] Can tab through all interactive elements
- [ ] Focus rings visible on all elements
- [ ] Enter/Space activates buttons
- [ ] Escape closes modal
- [ ] Screen reader announces validation errors
- [ ] ARIA labels present on all inputs

---

## File Locations

### New Files to Create

1. `/apps/web/src/components/email-templates/SendAdHocEmailSection.tsx`
   - Main send ad-hoc email component

2. `/apps/web/src/services/emailTemplates.api.ts` (enhance existing)
   - Add `getSegments()` method
   - Add `getSegmentPreview(segment)` method
   - Add `sendAdHocEmail(request)` method

---

### Files to Modify

1. `/apps/web/src/components/email-templates/EmailCategoryPanel.tsx`
   - Add conditional rendering for `category === 'AdHoc'`
   - Import and render `SendAdHocEmailSection`

2. `/apps/web/src/pages/admin/EmailTemplatesAdminPage.tsx`
   - No changes needed (already renders EmailCategoryPanel)

---

## Design Decisions & Rationale

### Why Divider Separator?

**Decision**: Use `<Divider size="md" />` to separate template editing from send section

**Rationale**:
- Clear visual separation between two distinct workflows
- Matches existing pattern in WitchCityRope admin pages
- Prevents confusion about which form is active
- Maintains clean visual hierarchy

---

### Why Show Preview Recipients?

**Decision**: Automatically show first 10 recipients when segment selected

**Rationale**:
- Prevents accidental sends to wrong group
- Builds admin confidence before sending
- Allows quick sanity check of segment logic
- Common pattern in email marketing tools (Mailchimp, SendGrid)

---

### Why Confirmation Modal?

**Decision**: Require explicit confirmation before send

**Rationale**:
- Email sends are irreversible (cannot be undone)
- Large recipient counts increase impact of mistakes
- Stakeholder requirement for safety-critical actions
- Industry standard for bulk email tools

---

### Why Real-Time Variable Validation?

**Decision**: Validate variables as user types, not on submit

**Rationale**:
- Immediate feedback prevents frustration
- Matches existing template editor pattern
- Users can fix errors before attempting send
- Reduces failed submissions

---

### Why Reset Form After Success?

**Decision**: Clear all fields automatically after successful send

**Rationale**:
- Prevents accidental duplicate sends
- Clean slate for next email
- Clear signal that action completed
- User can start fresh composition

---

### Why Preserve Form on Failure?

**Decision**: Keep subject and body filled if send fails

**Rationale**:
- User doesn't lose work due to network error
- Can retry immediately without re-composing
- Frustrating to lose content on transient failures
- Standard UX pattern for form submissions

---

## Future Enhancements (Out of Scope)

These features are NOT part of current design but could be added later:

1. **Email Preview Modal**: Show rendered HTML in modal before send
2. **Schedule Send**: Pick date/time for delayed send
3. **Email History**: Log of all ad-hoc emails sent with timestamps
4. **Attachment Support**: Upload files to include in emails
5. **A/B Testing**: Send two variants to segment subsets
6. **Template Library**: Save ad-hoc emails as reusable templates
7. **Send Test Email**: Send preview to admin's email before bulk send
8. **Progress Indicator**: Real-time progress bar during send
9. **Recipient Import**: Upload CSV of custom recipient list
10. **Advanced Filters**: Combine segments with AND/OR logic

---

## Validation Checklist

**Design Complete**:
- [x] Wireframes created (desktop + mobile)
- [x] Component breakdown documented
- [x] Props interfaces defined
- [x] State management specified
- [x] API integration points documented
- [x] User flows mapped (happy path + errors)
- [x] Responsive behavior specified
- [x] Accessibility requirements defined
- [x] Design tokens documented
- [x] Mantine components listed
- [x] Error handling designed
- [x] Integration approach documented

**Consistency**:
- [x] Matches Design System v7 colors
- [x] Follows Button Style Guide patterns
- [x] Uses Mantine v7 components correctly
- [x] Aligns with existing EmailCategoryPanel patterns
- [x] Maintains burgundy/cream color scheme
- [x] Uses Montserrat headings, Source Sans body

**Completeness**:
- [x] All user scenarios covered
- [x] All error states designed
- [x] All validation rules specified
- [x] All responsive breakpoints defined
- [x] All accessibility requirements met
- [x] All design tokens documented

---

## Next Steps

**For React Developer (Phase 3B)**:

1. Read this design document thoroughly
2. Review existing `EmailCategoryPanel.tsx` implementation patterns
3. Create `SendAdHocEmailSection.tsx` component
4. Implement all sub-components per specifications
5. Integrate with `EmailCategoryPanel.tsx`
6. Add API service methods to `emailTemplates.api.ts`
7. Test all user flows manually
8. Verify responsive behavior on all breakpoints
9. Run accessibility tests (keyboard navigation, screen reader)
10. Update handoff document when complete

---

**Design Status**: ✅ COMPLETE
**Ready for Implementation**: YES
**Handoff Document**: `/docs/functional-areas/member-import/handoffs/ui-designer-2025-11-18-handoff.md`
