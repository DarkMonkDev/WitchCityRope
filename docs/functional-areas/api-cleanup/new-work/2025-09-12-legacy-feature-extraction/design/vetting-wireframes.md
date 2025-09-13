# Vetting System UI Wireframes
<!-- Created: 2025-09-13 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Complete Wireframes -->

## Overview

These wireframes demonstrate the visual layout and user flow for the WitchCityRope Vetting System, designed with privacy-first principles and mobile-responsive patterns using Mantine v7 components.

## Design Principles Applied

- **WitchCityRope Design System v7**: Burgundy (#880124), rose-gold (#B76D75), amber (#FFBF00)
- **Floating Labels**: ALL form inputs use floating label animation pattern
- **Corner Morphing**: Signature asymmetric button styling (12px 6px 12px 6px)
- **Mobile-First**: Touch-optimized with 44px+ targets, thumb-zone actions
- **Privacy Indicators**: 🔒 icons for sensitive data, clear privacy notices

## User Flow Overview

```
1. Application Submission
   Guest → Application Form (5 steps) → Confirmation → Email Notice

2. Application Review  
   Reviewer → Dashboard → Application Detail → Decision → Notification

3. Status Checking
   Applicant → Login → Status Page → Additional Info (if needed)

4. Administration
   Admin → Analytics Dashboard → Bulk Operations → Reports
```

## Wireframe 1: Member Application Form

### Desktop Layout (1200px)
```
+----------------------------------------------------------------+
|  Header: WitchCityRope Logo    |    "Join Our Community"       |
+----------------------------------------------------------------+
| Progress: [●]—[○]—[○]—[○]—[○]  Step 1 of 5: Personal Info    |
+----------------------------------------------------------------+
|                                                                |
| ┌─────────────────────┐  ┌─────────────────────────────────┐ |
| │ Form Fields         │  │ Privacy & Security Notice       │ |
| │                     │  │                                 │ |
| │ [Full Name____]     │  │ 🔒 Your information is encrypted │ |
| │ [Scene Name___]     │  │ and only accessible to approved │ |
| │ [Pronouns_____]     │  │ vetting team members.           │ |
| │ [Email________]     │  │                                 │ |
| │ [Phone________]     │  │ All data follows strict privacy │ |
| │                     │  │ protection guidelines.          │ |
| │ Required fields *   │  │                                 │ |
| └─────────────────────┘  └─────────────────────────────────┘ |
|                                                                |
| Help Text: We use your scene name for all community           |
| interactions to protect your privacy and respect your         |
| chosen identity within the rope bondage community.            |
|                                                                |
+----------------------------------------------------------------+
| [< Back]                                    [Next Step >]     |
+----------------------------------------------------------------+
```

### Mobile Layout (375px)
```
+-----------------------------------+
| ☰  WitchCityRope  Join Community  |
+-----------------------------------+
| ●—○—○—○—○  Step 1 of 5            |
+-----------------------------------+
|                                   |
| Personal Information              |
|                                   |
| [Full Name_________________]      |
| [Scene Name________________]      |
| [Pronouns__________________]      |
| [Email_____________________]      |
| [Phone_____________________]      |
|                                   |
| 🔒 Privacy Notice                 |
| Your information is encrypted     |
| and secure. Only approved team    |
| members can access your data.     |
|                                   |
| * Required fields                 |
|                                   |
+-----------------------------------+
| [Back]           [Next Step]      |
+-----------------------------------+
```

## Wireframe 2: Application Review Dashboard

### Desktop Layout (1200px)
```
+----------------------------------------------------------------+
| Header: Vetting Dashboard          [Search___] [Filter ▼]     |
+----------------------------------------------------------------+
| Stats: [Pending: 12] [In Review: 5] [Approved: 8] [Total: 25] |
+----------------------------------------------------------------+
|                                                                |
| Application Cards Grid                                         |
| ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐  |
| │ Sarah Mitchell  │ │ Alex Rodriguez  │ │ Jamie Chen      │  |
| │ [NEW]           │ │ [IN REVIEW]     │ │ [PENDING INFO]  │  |
| │ 3 days ago      │ │ 1 day ago       │ │ 5 days ago      │  |
| │ ──────────────  │ │ ──────────────  │ │ ──────────────  │  |
| │ Beginner        │ │ Intermediate    │ │ Advanced        │  |
| │ 2/2 References  │ │ 2/2 References  │ │ 1/2 References  │  |
| │ [👁 View] [📝]   │ │ [Continue]      │ │ [Follow Up]     │  |
| └─────────────────┘ └─────────────────┘ └─────────────────┘  |
|                                                                |
| ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐  |
| │ Morgan Taylor   │ │ Casey Johnson   │ │ Riley Kim       │  |
| │ [INTERVIEW]     │ │ [NEW]           │ │ [IN REVIEW]     │  |
| │ 2 days ago      │ │ 4 hours ago     │ │ 1 day ago       │  |
| │ ──────────────  │ │ ──────────────  │ │ ──────────────  │ |
| │ Expert          │ │ Beginner        │ │ Intermediate    │  |
| │ Scheduled       │ │ 2/2 References  │ │ 2/2 References  │  |
| │ [📅 Schedule]    │ │ [👁 View] [📝]   │ │ [Continue]      │  |
| └─────────────────┘ └─────────────────┘ └─────────────────┘  |
|                                                                |
+----------------------------------------------------------------+
| Pagination: [<] 1 2 3 [>]                    Items per page: 6|
+----------------------------------------------------------------+
```

### Mobile Layout (375px)
```
+-----------------------------------+
| ☰ Vetting Dashboard      [🔍] [⚙] |
+-----------------------------------+
| [Pending: 12] [Review: 5]         |
| [Approved: 8] [Total: 25]         |
+-----------------------------------+
|                                   |
| Sarah Mitchell           [NEW]    |
| Beginner • 3 days ago             |
| References: 2/2 ✓                 |
| [View] [Review]                   |
| --------------------------------- |
|                                   |
| Alex Rodriguez      [IN REVIEW]   |
| Intermediate • 1 day ago          |
| References: 2/2 ✓                 |
| [Continue Review]                 |
| --------------------------------- |
|                                   |
| Jamie Chen       [PENDING INFO]   |
| Advanced • 5 days ago             |
| References: 1/2 ⚠                 |
| [Follow Up]                       |
| --------------------------------- |
|                                   |
| ⋯ Show More                       |
|                                   |
+-----------------------------------+
| [< Prev]              [Next >]    |
+-----------------------------------+
```

## Wireframe 3: Application Detail View

### Desktop Layout (1200px)
```
+----------------------------------------------------------------+
|  ← Back to Dashboard        Application Review: Sarah Mitchell |
+----------------------------------------------------------------+
| Left Panel: Application Details    | Right Panel: Review Tools|
| ┌────────────────────────────────┐ | ┌─────────────────────────┐ |
| │ Personal Information           │ | │ Review Notes            │ |
| │ Full Name: Sarah Mitchell      │ | │ ┌─────────────────────┐ │ |
| │ Scene Name: Sasha              │ | │ │ [Add review notes...] │ │ |
| │ Pronouns: She/Her              │ | │ │                     │ │ |
| │ Email: s.mitchell@email.com    │ | │ │                     │ │ |
| │ Phone: (555) 123-4567          │ | │ │                     │ │ |
| │                                │ | │ │                     │ │ |
| │ Experience Details             │ | │ └─────────────────────┘ │ |
| │ Level: Beginner (2 years)      │ | │                         │ |
| │ 🔒 Safety Knowledge:           │ | │ Previous Reviews        │ |
| │ "Understands SSC principles... │ | │ [None]                  │ |
| │                                │ | │                         │ |
| │ 🔒 Consent Understanding:      │ | │ Decision Actions        │ |
| │ "Consent is ongoing and can... │ | │ ┌─────────────────────┐ │ |
| │                                │ | │ │ ✓ [Approve]         │ │ |
| │ References Status              │ | │ │ ℹ [Request More Info] │ │ |
| │ ┌────────────────────────────┐ │ | │ │ 📅 [Schedule Interview]│ │ |
| │ │ 1. Lisa Chen ✓ Completed  │ │ | │ │ ✗ [Deny Application] │ │ |
| │ │    Safety instructor       │ │ | │ └─────────────────────┘ │ |
| │ │ 2. Mark Thompson ✓ Done   │ │ | │                         │ |
| │ │    Community member (3yr)  │ │ | │ Required Score: 7/10    │ |
| │ └────────────────────────────┘ │ | │ Current Score: [8/10]   │ |
| └────────────────────────────────┘ | └─────────────────────────┘ |
+----------------------------------------------------------------+
| Audit Trail Timeline                                           |
| ● Application Submitted (3 days ago)                          |
| ● References Contacted (2 days ago)                           |
| ○ Review In Progress (1 day ago)                              |
| ○ Decision Pending                                             |
+----------------------------------------------------------------+
```

### Mobile Layout (375px)
```
+-----------------------------------+
| ← Dashboard   Sarah Mitchell      |
+-----------------------------------+
| Personal Info            [Expand] |
| Sarah Mitchell (Sasha)            |
| She/Her • s.mitchell@email.com    |
|                                   |
| Experience               [Expand] |
| Beginner • 2 years                |
| 🔒 Safety & Consent responses     |
|                                   |
| References ✓             [Expand] |
| 1. Lisa Chen (Instructor) ✓       |
| 2. Mark Thompson (Member) ✓       |
|                                   |
| Review Notes             [Expand] |
| [Add your review notes here...]   |
|                                   |
| Score: [8]/10                     |
|                                   |
| Decision Actions                  |
| [✓ Approve] [ℹ Request Info]      |
| [📅 Interview] [✗ Deny]           |
|                                   |
+-----------------------------------+
| Timeline                 [Expand] |
| ● Submitted (3d) ● Refs (2d)      |
| ○ Review (1d) ○ Decision          |
+-----------------------------------+
```

## Wireframe 4: Applicant Status Page

### Desktop Layout (1200px)
```
+----------------------------------------------------------------+
|  WitchCityRope Logo                            [Login] [Help]  |
+----------------------------------------------------------------+
|                     Application Status                         |
|                                                                |
| ┌────────────────────────────────────────────────────────────┐ |
| │ Status Timeline                                            │ |
| │                                                            │ |
| │ [●]────[●]────[●]────[○]────[○]                           │ |
| │  ↓      ↓      ↓      ↓      ↓                            │ |
| │ Submit  Refs   Review Interview Decision                   │ |
| │ 5 days  3 days  1 day  Pending  Pending                   │ |
| │                                                            │ |
| └────────────────────────────────────────────────────────────┘ |
|                                                                |
| ┌────────────────────────────────────────────────────────────┐ |
| │ Current Status: Under Review                               │ |
| │                                                            │ |
| │ Your application is being reviewed by our vetting team.   │ |
| │ We have received responses from both of your references    │ |
| │ and are now evaluating your application.                  │ |
| │                                                            │ |
| │ Next Steps:                                                │ |
| │ • We'll contact you within 2-3 business days              │ |
| │ • You may be invited for a brief interview                │ |
| │ • Final decision within 5-7 business days                 │ |
| │                                                            │ |
| │ Questions? Contact our vetting team: vetting@wcr.com      │ |
| └────────────────────────────────────────────────────────────┘ |
|                                                                |
| Additional Information Requested              [Not Required]   |
| ┌────────────────────────────────────────────────────────────┐ |
| │ No additional information needed at this time.            │ |
| └────────────────────────────────────────────────────────────┘ |
|                                                                |
+----------------------------------------------------------------+
```

### Mobile Layout (375px)
```
+-----------------------------------+
| WitchCityRope         [Login] [?] |
+-----------------------------------+
|        Application Status         |
|                                   |
| Timeline                          |
| ●—●—●—○—○                         |
| Submit → Refs → Review →          |
| Interview → Decision              |
|                                   |
| Current Status                    |
| ┌─────────────────────────────┐   |
| │ Under Review                │   |
| │                             │   |
| │ Your application is being   │   |
| │ reviewed by our team.       │   |
| │                             │   |
| │ References: ✓ Complete      │   |
| │ Review: In Progress         │   |
| │                             │   |
| │ Next: Interview (possible)  │   |
| │ Decision: 2-3 business days │   |
| │                             │   |
| │ Questions?                  │   |
| │ vetting@wcr.com             │   |
| └─────────────────────────────┘   |
|                                   |
| Additional Info                   |
| [None requested]                  |
|                                   |
+-----------------------------------+
```

## Wireframe 5: Admin Analytics Dashboard

### Desktop Layout (1200px)
```
+----------------------------------------------------------------+
|  Admin Vetting Dashboard              [Export] [Settings] [?] |
+----------------------------------------------------------------+
| Date Range: [Last 30 Days ▼]                    [Refresh]     |
+----------------------------------------------------------------+
|                                                                |
| Statistics Overview                                            |
| ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ |
| │ Total Apps  │ │ Avg Time    │ │ Approval    │ │ Pending     │ |
| │    247      │ │   5.2 days  │ │    78%      │ │     12      │ |
| │ +12 vs last │ │ -0.3 vs last│ │ +2% vs last │ │ -3 vs last  │ |
| │    week     │ │    month    │ │    month    │ │    week     │ |
| └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘ |
|                                                                |
| Charts & Trends                                                |
| ┌─────────────────────────────────┐ ┌────────────────────────┐ |
| │ Application Volume (30 days)    │ │ Status Distribution    │ |
| │                                 │ │                        │ |
| │ 20 ┌─╮                          │ │      [Pie Chart]       │ |
| │ 15 │ │╭─╮                       │ │                        │ |
| │ 10 ├─┤│ │╭─╮                    │ │ ● New (25%)            │ |
| │  5 │ ││ ││ │╭─╮                 │ │ ● Review (35%)         │ |
| │  0 └─┴┴─┴┴─┴┴─╯                 │ │ ● Pending (20%)        │ |
| │    W1 W2 W3 W4                  │ │ ● Approved (15%)       │ |
| │                                 │ │ ● Denied (5%)          │ |
| └─────────────────────────────────┘ └────────────────────────┘ |
|                                                                |
| Quick Actions                                                  |
| [📊 Full Report] [📧 Send Reminders] [👥 Reviewer Workload]   |
| [📋 Bulk Actions] [⚙ Settings] [📞 Contact Applicants]        |
|                                                                |
+----------------------------------------------------------------+
```

### Mobile Layout (375px)
```
+-----------------------------------+
| ☰ Admin Dashboard    [⚙] [📤] [?] |
+-----------------------------------+
| Last 30 Days              [🔄]   |
+-----------------------------------+
|                                   |
| Stats                             |
| ┌─────────────┐ ┌─────────────┐   |
| │ Total: 247  │ │ Pending: 12 │   |
| │ +12 week    │ │ -3 week     │   |
| └─────────────┘ └─────────────┘   |
| ┌─────────────┐ ┌─────────────┐   |
| │ Time: 5.2d  │ │ Rate: 78%   │   |
| │ -0.3 month  │ │ +2% month   │   |
| └─────────────┘ └─────────────┘   |
|                                   |
| Volume Trend                      |
| ┌─────────────────────────────┐   |
| │ [Mini Line Chart]           │   |
| │                             │   |
| │ Week 1: 15 applications     │   |
| │ Week 2: 18 applications     │   |
| │ Week 3: 12 applications     │   |
| │ Week 4: 22 applications     │   |
| └─────────────────────────────┘   |
|                                   |
| Quick Actions                     |
| [📊 Report] [📧 Reminders]        |
| [👥 Workload] [📋 Bulk]           |
|                                   |
+-----------------------------------+
```

## Interaction Specifications

### Button Styling (Mantine v7 + Design System)
```jsx
// Primary Action Button (Amber)
<Button
  size="lg"
  style={{
    background: 'linear-gradient(135deg, #FFBF00 0%, #DAA520 100%)',
    borderRadius: '12px 6px 12px 6px',
    fontFamily: 'Montserrat, sans-serif',
    fontWeight: 600,
    textTransform: 'uppercase',
    letterSpacing: '1px',
    transition: 'all 0.3s ease',
    border: 'none',
    color: '#1A1A2E'
  }}
  onMouseEnter={(e) => {
    e.target.style.borderRadius = '6px 12px 6px 12px';
    e.target.style.background = 'linear-gradient(135deg, #DAA520 0%, #FFBF00 100%)';
  }}
  onMouseLeave={(e) => {
    e.target.style.borderRadius = '12px 6px 12px 6px';
    e.target.style.background = 'linear-gradient(135deg, #FFBF00 0%, #DAA520 100%)';
  }}
>
  Submit Application
</Button>

// Secondary Action Button (Burgundy)
<Button
  size="lg"
  variant="outline"
  style={{
    borderColor: '#880124',
    color: '#880124',
    borderRadius: '12px 6px 12px 6px',
    fontFamily: 'Montserrat, sans-serif',
    fontWeight: 600,
    textTransform: 'uppercase',
    letterSpacing: '1px',
    transition: 'all 0.3s ease'
  }}
>
  Save Draft
</Button>
```

### Floating Label Implementation
```jsx
// Floating Label Text Input
<Box sx={{ position: 'relative', marginBottom: 24 }}>
  <TextInput
    size="lg"
    placeholder=" "
    styles={(theme) => ({
      input: {
        padding: '16px 12px 6px 12px',
        borderColor: '#B76D75', // rose-gold
        borderRadius: 12,
        backgroundColor: '#FAF6F2', // cream
        fontSize: 16, // Prevents iOS zoom
        '&:focus': {
          borderColor: '#880124', // burgundy
          transform: 'translateY(-2px)',
          boxShadow: '0 4px 12px rgba(183, 109, 117, 0.25)',
          outline: '3px solid #FFBF00', // amber focus ring
          outlineOffset: '2px'
        },
        '&:not(:placeholder-shown) + label, &:focus + label': {
          top: '-2px',
          transform: 'translateY(-50%) scale(0.8)',
          color: '#880124',
          backgroundColor: '#FAF6F2'
        }
      },
      label: {
        position: 'absolute',
        left: 12,
        top: '50%',
        transform: 'translateY(-50%)',
        transition: 'all 0.3s ease',
        backgroundColor: '#FAF6F2',
        padding: '0 4px',
        color: '#8B8680',
        fontFamily: 'Montserrat, sans-serif',
        fontWeight: 500,
        pointerEvents: 'none'
      }
    })}
  />
</Box>
```

### Status Badge Components
```jsx
// Status Badge with WCR Colors
const StatusBadge = ({ status, children }) => {
  const statusConfig = {
    'new': { color: '#FFBF00', bg: 'rgba(255, 191, 0, 0.1)' },
    'in-review': { color: '#9D4EDD', bg: 'rgba(157, 78, 221, 0.1)' },
    'pending-info': { color: '#DAA520', bg: 'rgba(218, 165, 32, 0.1)' },
    'approved': { color: '#228B22', bg: 'rgba(34, 139, 34, 0.1)' },
    'denied': { color: '#DC143C', bg: 'rgba(220, 20, 60, 0.1)' }
  };
  
  const config = statusConfig[status] || statusConfig['new'];
  
  return (
    <Badge
      style={{
        backgroundColor: config.bg,
        color: config.color,
        borderRadius: '12px 6px 12px 6px',
        fontFamily: 'Montserrat, sans-serif',
        fontWeight: 600,
        textTransform: 'uppercase',
        letterSpacing: '0.5px',
        border: `1px solid ${config.color}`
      }}
    >
      {children}
    </Badge>
  );
};
```

### Privacy Indicator Pattern
```jsx
// Privacy Notice Component
<Alert
  icon={<IconShieldCheck />}
  color="blue"
  title="Privacy Protection Active"
  style={{
    backgroundColor: 'rgba(183, 109, 117, 0.05)',
    borderColor: '#B76D75',
    marginBottom: 16
  }}
>
  <Group spacing="xs">
    <IconLock size={14} color="#880124" />
    <Text size="sm">
      This information is encrypted and only visible to approved vetting team members.
    </Text>
  </Group>
</Alert>
```

## Mobile Responsive Patterns

### Responsive Grid Layout
```jsx
// Application Cards Grid
<SimpleGrid
  cols={3}
  spacing="lg"
  breakpoints={[
    { maxWidth: 'md', cols: 2, spacing: 'md' },
    { maxWidth: 'sm', cols: 1, spacing: 'sm' }
  ]}
>
  {applications.map(app => (
    <ApplicationCard key={app.id} application={app} />
  ))}
</SimpleGrid>
```

### Mobile Navigation Pattern
```jsx
// Mobile Drawer Navigation
<AppShell
  navbar={{
    width: 300,
    breakpoint: 'sm',
    collapsed: { mobile: !navOpened }
  }}
  header={{ height: 60 }}
>
  <AppShell.Header>
    <Group h="100%" px="md" position="apart">
      <Group>
        <Burger
          opened={navOpened}
          onClick={toggleNav}
          hiddenFrom="sm"
          size="sm"
        />
        <Text
          size="lg"
          weight={700}
          style={{ fontFamily: 'Montserrat, sans-serif' }}
        >
          Vetting Dashboard
        </Text>
      </Group>
      <Group>
        <ActionIcon variant="light" color="wcr.7">
          <IconBell size={18} />
        </ActionIcon>
      </Group>
    </Group>
  </AppShell.Header>
</AppShell>
```

## Accessibility Implementation

### Focus Management
```jsx
// Focus trap for modal dialogs
<Modal
  opened={opened}
  onClose={close}
  title="Application Review"
  trapFocus
  returnFocus
  styles={{
    title: {
      fontFamily: 'Montserrat, sans-serif',
      fontWeight: 700,
      color: '#880124'
    }
  }}
>
  <form onSubmit={handleSubmit}>
    <TextInput
      label="Review Notes"
      required
      aria-describedby="notes-description"
      styles={{
        input: {
          '&:focus': {
            outline: '3px solid #FFBF00',
            outlineOffset: '2px'
          }
        }
      }}
    />
    <Text
      id="notes-description"
      size="sm"
      color="dimmed"
      style={{ marginTop: 4 }}
    >
      Provide detailed feedback for the applicant
    </Text>
  </form>
</Modal>
```

### Screen Reader Support
```jsx
// Accessible status announcements
<div
  role="status"
  aria-live="polite"
  aria-label="Application status update"
  style={{ position: 'absolute', left: '-10000px' }}
>
  {statusMessage}
</div>

// Accessible form validation
<TextInput
  error={errors.email}
  aria-invalid={!!errors.email}
  aria-describedby={errors.email ? 'email-error' : undefined}
/>
{errors.email && (
  <Text
    id="email-error"
    role="alert"
    size="sm"
    color="red"
    style={{ marginTop: 4 }}
  >
    {errors.email}
  </Text>
)}
```

---

These wireframes provide comprehensive visual guidance for implementing the WitchCityRope Vetting System with proper Design System v7 compliance, mobile-first responsiveness, privacy protection, and accessibility standards. Each component follows established patterns while maintaining the community-focused, safety-first approach essential for this sensitive application process.