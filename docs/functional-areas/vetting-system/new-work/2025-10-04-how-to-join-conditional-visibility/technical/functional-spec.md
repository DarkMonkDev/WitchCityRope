# Functional Specification: Conditional "How to Join" Menu Visibility
<!-- Last Updated: 2025-10-04 -->
<!-- Version: 1.0 -->
<!-- Owner: Functional Spec Agent -->
<!-- Status: Draft -->

## Technical Overview

This feature implements intelligent conditional rendering of the "How to Join" navigation menu item based on user authentication state and vetting application status. The system uses React hooks, Zustand state management, and TanStack Query for data fetching to provide real-time menu visibility updates with optimized caching and error handling.

**Architecture Pattern**: Frontend-only feature leveraging existing API endpoint (`GET /api/vetting/status`) with no backend changes required.

## Architecture

### Technology Stack
- **React 18**: Functional components with hooks
- **TypeScript**: Strict type checking
- **Mantine v7**: UI component library
- **Zustand**: Global state management (auth)
- **TanStack Query v5**: Data fetching, caching, synchronization
- **React Router v6**: Client-side routing

### Component Structure
```
/apps/web/src/
├── features/vetting/
│   ├── hooks/
│   │   ├── useVettingStatus.ts          # NEW - Main status hook
│   │   └── useMenuVisibility.ts         # NEW - Menu visibility logic
│   ├── components/
│   │   ├── VettingApplicationForm.tsx   # EXISTING - No changes
│   │   ├── VettingStatusBox.tsx         # NEW - Status display component
│   │   └── StatusBoxVariants/           # NEW - Status-specific boxes
│   │       ├── SubmittedStatusBox.tsx
│   │       ├── UnderReviewStatusBox.tsx
│   │       ├── InterviewStatusBox.tsx
│   │       ├── OnHoldStatusBox.tsx
│   │       ├── ApprovedStatusBox.tsx
│   │       └── DeniedStatusBox.tsx
│   ├── api/
│   │   └── vettingApi.ts                # UPDATE - Add status endpoint
│   └── types/
│       └── vetting-status.types.ts      # NEW - Status response types
├── components/layout/
│   └── Navigation.tsx                   # UPDATE - Add conditional logic
├── pages/
│   └── HowToJoin.tsx                    # UPDATE - Add status display
└── stores/
    └── authStore.ts                     # EXISTING - No changes
```

### Service Architecture
- **React App (Web)**: UI components make HTTP calls to API
- **API Service**: Returns vetting status via `/api/vetting/status` endpoint
- **Pattern**: React → TanStack Query → HTTP → API → Database

**CRITICAL**: React app NEVER directly accesses database - all data flows through API endpoints.

## Data Models

### API Response Types (from backend)

```typescript
// /apps/web/src/features/vetting/types/vetting-status.types.ts

/**
 * API response wrapper - all endpoints return this format
 */
interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  message?: string;
  error?: string;
  timestamp: string;
}

/**
 * Main status response from GET /api/vetting/status
 * Maps to MyApplicationStatusResponse C# model
 */
interface MyApplicationStatusResponse {
  hasApplication: boolean;
  application: ApplicationStatusInfo | null;
}

/**
 * Application status details
 * Maps to ApplicationStatusInfo C# model
 */
interface ApplicationStatusInfo {
  applicationId: string;
  applicationNumber: string;
  status: VettingStatus; // Enum string value
  statusDescription: string;
  submittedAt: string; // ISO 8601 timestamp
  lastUpdated: string; // ISO 8601 timestamp
  nextSteps: string | null;
  estimatedDaysRemaining: number | null;
}

/**
 * Vetting status enum - matches backend VettingStatus enum exactly
 */
enum VettingStatus {
  Draft = 'Draft',              // 0
  Submitted = 'Submitted',      // 1
  UnderReview = 'UnderReview',  // 2
  InterviewApproved = 'InterviewApproved', // 3
  PendingInterview = 'PendingInterview',   // 4
  InterviewScheduled = 'InterviewScheduled', // 5
  OnHold = 'OnHold',            // 6
  Approved = 'Approved',        // 7
  Denied = 'Denied',            // 8
  Withdrawn = 'Withdrawn'       // 9
}
```

### Frontend Derived Types

```typescript
/**
 * Menu visibility decision result
 */
interface MenuVisibilityResult {
  shouldShow: boolean;
  reason: string; // For debugging/logging
}

/**
 * Status box props for conditional rendering
 */
interface StatusBoxProps {
  status: VettingStatus;
  applicationNumber: string;
  submittedAt: Date;
  lastUpdated: Date;
  statusDescription: string;
  nextSteps?: string;
  estimatedDaysRemaining?: number;
}
```

## API Specifications

### Existing Endpoint (No Changes Required)

**Endpoint**: `GET /api/vetting/status`

| Attribute | Value |
|-----------|-------|
| **Method** | GET |
| **Path** | `/api/vetting/status` |
| **Authentication** | Required (JWT token in httpOnly cookie) |
| **Authorization** | Any authenticated user |
| **Timeout** | 5 seconds |

**Response Structure**:
```typescript
{
  success: true,
  data: {
    hasApplication: boolean,
    application: {
      applicationId: "guid",
      applicationNumber: "8-char-hex",
      status: "Submitted" | "UnderReview" | ...,
      statusDescription: "User-friendly status message",
      submittedAt: "2025-10-04T12:00:00Z",
      lastUpdated: "2025-10-04T14:30:00Z",
      nextSteps: "What user should do next",
      estimatedDaysRemaining: 14
    } | null
  },
  message: "Vetting status retrieved successfully",
  timestamp: "2025-10-04T14:35:00Z"
}
```

**Response Codes**:
- `200 OK`: Status retrieved successfully
- `401 Unauthorized`: User not authenticated
- `500 Internal Server Error`: Server error

**Error Handling**: Frontend should fail-open (show menu) on API errors.

## Component Specifications

### 1. useVettingStatus Hook

**File**: `/apps/web/src/features/vetting/hooks/useVettingStatus.ts`

**Purpose**: Wraps TanStack Query to fetch and cache vetting status

**Implementation**:
```typescript
import { useQuery } from '@tanstack/react-query';
import { useIsAuthenticated } from '../../stores/authStore';
import { vettingApi } from '../api/vettingApi';
import type { MyApplicationStatusResponse } from '../types/vetting-status.types';

export const useVettingStatus = () => {
  const isAuthenticated = useIsAuthenticated();

  return useQuery<MyApplicationStatusResponse>({
    queryKey: ['vetting', 'status'],
    queryFn: () => vettingApi.getMyStatus(),
    enabled: isAuthenticated, // Only fetch if user is logged in
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes (renamed from cacheTime in TanStack Query v5)
    retry: 1, // Retry once on failure
    retryDelay: 1000, // 1 second retry delay
    refetchOnWindowFocus: true, // Refresh when user returns to tab
    refetchOnMount: true, // Refresh on component mount
    throwOnError: false // Don't throw errors - handle gracefully
  });
};

// Convenience hook for menu visibility
export const useMenuVisibility = (): MenuVisibilityResult => {
  const isAuthenticated = useIsAuthenticated();
  const { data: vettingStatus, isLoading, error } = useVettingStatus();

  // Guest users always see menu
  if (!isAuthenticated) {
    return { shouldShow: true, reason: 'Guest user' };
  }

  // Show menu while loading (prevents flash)
  if (isLoading) {
    return { shouldShow: true, reason: 'Loading status' };
  }

  // Fail-open: Show menu on error
  if (error) {
    console.error('Vetting status error:', error);
    return { shouldShow: true, reason: 'API error - fail-open' };
  }

  // No status data: show menu (fail-open)
  if (!vettingStatus) {
    return { shouldShow: true, reason: 'No status data - fail-open' };
  }

  // User has no application: show menu
  if (!vettingStatus.hasApplication) {
    return { shouldShow: true, reason: 'No application' };
  }

  // Check status - hide for OnHold, Approved, Denied
  const hideStatuses: VettingStatus[] = [
    VettingStatus.OnHold,
    VettingStatus.Approved,
    VettingStatus.Denied
  ];

  const shouldHide = hideStatuses.includes(
    vettingStatus.application!.status as VettingStatus
  );

  if (shouldHide) {
    return {
      shouldShow: false,
      reason: `Status: ${vettingStatus.application!.status}`
    };
  }

  // All other statuses: show menu
  return {
    shouldShow: true,
    reason: `Status allows access: ${vettingStatus.application!.status}`
  };
};
```

**Key Features**:
- 5-minute cache duration (staleTime)
- Automatic refetch on window focus
- Fail-open error handling (show menu on error)
- Only fetches when user is authenticated
- Single retry on failure

### 2. Navigation Component Update

**File**: `/apps/web/src/components/layout/Navigation.tsx`

**Changes**: Add conditional rendering for "How to Join" menu item

**Implementation**:
```typescript
import { useMenuVisibility } from '../../features/vetting/hooks/useVettingStatus';

export const Navigation: React.FC = () => {
  const user = useUser();
  const isAuthenticated = useIsAuthenticated();
  const { shouldShow: showHowToJoin } = useMenuVisibility();

  return (
    <Box component="header" data-testid="nav-main" className="header">
      {/* ... existing navigation items ... */}

      {/* Conditionally render "How to Join" */}
      {showHowToJoin && (
        <Box
          component={Link}
          to="/how-to-join"
          data-testid="link-how-to-join"
          className="nav-underline-animation"
          style={{
            color: 'var(--color-charcoal)',
            textDecoration: 'none',
            fontFamily: 'var(--font-heading)',
            fontWeight: 500,
            fontSize: '15px',
            textTransform: 'uppercase',
            letterSpacing: '1px',
            transition: 'all 0.3s ease',
            position: 'relative',
          }}
        >
          How to Join
        </Box>
      )}

      {/* ... rest of navigation ... */}
    </Box>
  );
};
```

**Behavior**:
- Menu item shows/hides immediately on mount
- Updates automatically when status changes
- No loading spinner (fail-open prevents UI gaps)
- Uses existing Mantine/CSS styling

### 3. HowToJoin Page Component

**File**: `/apps/web/src/pages/HowToJoin.tsx`

**Purpose**: Display application form OR status information based on user's vetting state

**Implementation Pattern**:
```typescript
import { VettingStatusBox } from '../features/vetting/components/VettingStatusBox';
import { VettingApplicationForm } from '../features/vetting/components/VettingApplicationForm';
import { useVettingStatus } from '../features/vetting/hooks/useVettingStatus';
import { useIsAuthenticated } from '../stores/authStore';

export const HowToJoin: React.FC = () => {
  const isAuthenticated = useIsAuthenticated();
  const { data: vettingStatus, isLoading } = useVettingStatus();

  // Loading state
  if (isLoading) {
    return <LoadingOverlay visible />;
  }

  // User not authenticated - show info and login prompt
  if (!isAuthenticated) {
    return (
      <Container>
        <Title>How to Join Witch City Rope</Title>
        <Text>Please log in or create an account to apply for membership.</Text>
        {/* ... vetting process overview ... */}
      </Container>
    );
  }

  // User has no application - show application form
  if (!vettingStatus?.hasApplication) {
    return (
      <Container>
        <Title>Vetting Application</Title>
        <VettingApplicationForm />
      </Container>
    );
  }

  // User has application - show status box + conditional form
  return (
    <Container>
      <Title>Your Vetting Application</Title>

      {/* Status box at top */}
      <VettingStatusBox
        status={vettingStatus.application!.status}
        applicationNumber={vettingStatus.application!.applicationNumber}
        submittedAt={new Date(vettingStatus.application!.submittedAt)}
        lastUpdated={new Date(vettingStatus.application!.lastUpdated)}
        statusDescription={vettingStatus.application!.statusDescription}
        nextSteps={vettingStatus.application!.nextSteps}
        estimatedDaysRemaining={vettingStatus.application!.estimatedDaysRemaining}
      />

      {/* Conditional content based on status */}
      {renderContentForStatus(vettingStatus.application!.status)}
    </Container>
  );
};

// Helper function for status-specific content
const renderContentForStatus = (status: VettingStatus) => {
  switch (status) {
    case VettingStatus.Approved:
      return <Text>You are already a vetted member!</Text>;

    case VettingStatus.OnHold:
      return <Text>Check your email for additional information requests.</Text>;

    case VettingStatus.Denied:
      return <Text>You may reapply after 6 months from decision date.</Text>;

    case VettingStatus.Withdrawn:
      return <VettingApplicationForm />; // Allow new application

    default:
      // Submitted, UnderReview, Interview statuses - show read-only view
      return <Text>Your application is in progress.</Text>;
  }
};
```

### 4. VettingStatusBox Component

**File**: `/apps/web/src/features/vetting/components/VettingStatusBox.tsx`

**Purpose**: Render status-specific UI boxes with icons, descriptions, and next steps

**Implementation**:
```typescript
import { Paper, Title, Text, Group, Stack, Badge } from '@mantine/core';
import {
  IconClipboard,
  IconSearch,
  IconCheck,
  IconPause,
  IconX
} from '@tabler/icons-react';
import type { VettingStatus } from '../types/vetting-status.types';

interface VettingStatusBoxProps {
  status: VettingStatus;
  applicationNumber: string;
  submittedAt: Date;
  lastUpdated: Date;
  statusDescription: string;
  nextSteps?: string;
  estimatedDaysRemaining?: number;
}

export const VettingStatusBox: React.FC<VettingStatusBoxProps> = ({
  status,
  applicationNumber,
  submittedAt,
  lastUpdated,
  statusDescription,
  nextSteps,
  estimatedDaysRemaining
}) => {
  const config = getStatusConfig(status);

  return (
    <Paper
      shadow="md"
      p="xl"
      radius="md"
      withBorder
      style={{
        borderLeft: `6px solid ${config.color}`,
        backgroundColor: config.backgroundColor,
        marginBottom: 'var(--mantine-spacing-xl)'
      }}
      data-testid={`status-box-${status}`}
    >
      <Stack gap="md">
        {/* Header with icon and status */}
        <Group justify="space-between">
          <Group>
            {config.icon}
            <Title order={3}>{config.title}</Title>
          </Group>
          <Badge color={config.badgeColor} size="lg" variant="filled">
            {status}
          </Badge>
        </Group>

        {/* Application details */}
        <Group gap="xl">
          <Text size="sm" c="dimmed">
            Application #: <strong>{applicationNumber}</strong>
          </Text>
          <Text size="sm" c="dimmed">
            Submitted: <strong>{submittedAt.toLocaleDateString()}</strong>
          </Text>
          <Text size="sm" c="dimmed">
            Last Updated: <strong>{lastUpdated.toLocaleDateString()}</strong>
          </Text>
        </Group>

        {/* Status description */}
        <Text size="md">{statusDescription}</Text>

        {/* Next steps */}
        {nextSteps && (
          <Paper p="md" withBorder style={{ backgroundColor: 'white' }}>
            <Text fw={600} size="sm" mb="xs">Next Steps:</Text>
            <Text size="sm">{nextSteps}</Text>
          </Paper>
        )}

        {/* Estimated time */}
        {estimatedDaysRemaining !== null && estimatedDaysRemaining !== undefined && (
          <Text size="sm" c="dimmed" fs="italic">
            Estimated time remaining: {estimatedDaysRemaining} days
          </Text>
        )}
      </Stack>
    </Paper>
  );
};

// Status configuration helper
const getStatusConfig = (status: VettingStatus) => {
  const configs = {
    [VettingStatus.Submitted]: {
      title: 'Application Submitted',
      icon: <IconClipboard size={24} style={{ color: '#4285F4' }} />,
      color: '#4285F4',
      backgroundColor: 'rgba(66, 133, 244, 0.05)',
      badgeColor: 'blue'
    },
    [VettingStatus.UnderReview]: {
      title: 'Under Review',
      icon: <IconSearch size={24} style={{ color: '#FBBC05' }} />,
      color: '#FBBC05',
      backgroundColor: 'rgba(251, 188, 5, 0.05)',
      badgeColor: 'yellow'
    },
    [VettingStatus.InterviewApproved]: {
      title: 'Interview Approved',
      icon: <IconCheck size={24} style={{ color: '#34A853' }} />,
      color: '#34A853',
      backgroundColor: 'rgba(52, 168, 83, 0.05)',
      badgeColor: 'green'
    },
    [VettingStatus.OnHold]: {
      title: 'Application On Hold',
      icon: <IconPause size={24} style={{ color: '#EA4335' }} />,
      color: '#EA4335',
      backgroundColor: 'rgba(234, 67, 53, 0.05)',
      badgeColor: 'red'
    },
    [VettingStatus.Denied]: {
      title: 'Application Not Approved',
      icon: <IconX size={24} style={{ color: '#EA4335' }} />,
      color: '#EA4335',
      backgroundColor: 'rgba(234, 67, 53, 0.05)',
      badgeColor: 'red'
    },
    [VettingStatus.Approved]: {
      title: 'Application Approved',
      icon: <IconCheck size={24} style={{ color: '#34A853' }} />,
      color: '#34A853',
      backgroundColor: 'rgba(52, 168, 83, 0.05)',
      badgeColor: 'green'
    }
    // Add other statuses as needed
  };

  return configs[status] || configs[VettingStatus.Submitted];
};
```

**Key Features**:
- Status-specific colors and icons
- Mantine Paper with border styling
- Mobile-responsive layout
- Clear visual hierarchy
- Accessible design with semantic HTML

## State Management

### Zustand Auth Store (No Changes)
- Existing `authStore` provides `isAuthenticated` and `user` state
- No modifications required to auth state
- React components use existing selectors:
  - `useIsAuthenticated()`: Check if user is logged in
  - `useUser()`: Get current user data (including `isVetted` field)

### TanStack Query Cache Strategy

**Query Key**: `['vetting', 'status']`

**Cache Configuration**:
```typescript
{
  staleTime: 5 * 60 * 1000,      // 5 minutes
  gcTime: 10 * 60 * 1000,        // 10 minutes (garbage collection)
  refetchOnWindowFocus: true,     // Refresh when tab gains focus
  refetchOnMount: true,           // Refresh on component mount
  retry: 1,                       // Retry once on failure
  retryDelay: 1000                // 1 second between retries
}
```

**Cache Invalidation Triggers**:
1. User submits new vetting application → Invalidate `['vetting', 'status']`
2. User logs out → Clear all queries automatically
3. Window focus after 5 minutes → Automatic refetch
4. Manual invalidation on status change actions

**Invalidation Example**:
```typescript
import { useQueryClient } from '@tanstack/react-query';

const queryClient = useQueryClient();

// After application submission
const onSubmitSuccess = () => {
  queryClient.invalidateQueries({ queryKey: ['vetting', 'status'] });
  navigate('/how-to-join');
};
```

## Integration Points

### Authentication System
- **Integration**: Uses existing Zustand auth store
- **Flow**: User logs in → Auth state updates → Menu re-renders → Status query triggers
- **No Changes Required**: Auth system already provides necessary state

### Vetting Application Form
- **Integration**: Existing `VettingApplicationForm` component
- **Trigger**: On successful submission, invalidate vetting status query
- **Update Required**: Add query invalidation to `onSubmitSuccess` callback

### React Router
- **Integration**: Existing routing structure
- **Routes**:
  - `/how-to-join` - Public route (no auth required)
  - Direct URL navigation always works, shows appropriate message/form

### API Client
- **Integration**: Add new method to vetting API client
- **File**: `/apps/web/src/features/vetting/api/vettingApi.ts`
- **New Method**:
```typescript
export const vettingApi = {
  // ... existing methods ...

  getMyStatus: async (): Promise<MyApplicationStatusResponse> => {
    const response = await fetch('/api/vetting/status', {
      credentials: 'include' // Include httpOnly cookie
    });

    if (!response.ok) {
      throw new Error(`Failed to fetch vetting status: ${response.status}`);
    }

    const data: ApiResponse<MyApplicationStatusResponse> = await response.json();

    if (!data.success || !data.data) {
      throw new Error(data.error || 'Failed to retrieve vetting status');
    }

    return data.data;
  }
};
```

## Security Requirements

### Authorization
- Only authenticated users can call `/api/vetting/status`
- API validates JWT token from httpOnly cookie
- Users can only view their own application status
- 401 errors handled gracefully (fail-open, show menu)

### Data Privacy
- Application details NOT exposed in menu rendering
- Status check uses minimal data (only status enum and basic info)
- Sensitive fields (references, notes) NOT included in status response
- HTTPS required for all API calls (enforced at infrastructure level)

### XSS Prevention
- All user-supplied data sanitized by Mantine components
- No `dangerouslySetInnerHTML` used
- Status descriptions from API are trusted (server-controlled)

## Performance Requirements

### Response Time
- **Target**: Menu visibility decision < 100ms after authentication
- **Cache Hit**: Instant (from TanStack Query cache)
- **Cache Miss**: < 300ms API round trip
- **Loading State**: Show menu immediately (fail-open prevents UI gaps)

### Concurrent Users
- **Supported**: 100+ concurrent status checks
- **Caching**: Reduces server load by 90% (5-minute cache)
- **Scaling**: Stateless API endpoint, horizontally scalable

### Data Efficiency
- **Payload Size**: < 1KB for status response
- **Network Calls**: 1 API call per 5 minutes maximum per user
- **Bandwidth**: Negligible impact (< 1MB/day per active user)

### Mobile Performance
- **Touch Targets**: 44px minimum (Mantine default)
- **Responsive Design**: Mobile-first approach
- **Network**: Works on 3G (< 1KB payload)

## Error Handling

### API Errors

**Strategy**: Fail-open approach - always show menu on error

```typescript
// Error scenarios and handling
const errorHandling = {
  '401 Unauthorized': {
    action: 'Show menu (user treated as guest)',
    log: 'User not authenticated',
    userMessage: null // Silent fail-open
  },
  '500 Server Error': {
    action: 'Show menu (fail-open)',
    log: 'API server error - vetting status unavailable',
    userMessage: null // Silent fail-open
  },
  'Network Timeout': {
    action: 'Show menu (fail-open)',
    log: 'Network timeout checking vetting status',
    userMessage: null // Silent fail-open
  },
  'Invalid Response': {
    action: 'Show menu (fail-open)',
    log: 'Invalid response format from vetting status API',
    userMessage: null // Silent fail-open
  }
};
```

### Frontend Error Boundaries

**Implementation**:
```typescript
import { ErrorBoundary } from 'react-error-boundary';

const FallbackComponent = () => (
  <Text c="dimmed">Unable to load status. Please try again later.</Text>
);

// Wrap status-dependent components
<ErrorBoundary FallbackComponent={FallbackComponent}>
  <VettingStatusBox {...props} />
</ErrorBoundary>
```

### User-Facing Errors

**Principle**: Never block user access due to status check failures

**Examples**:
- API down → Show menu, log error silently
- Invalid status → Show menu, log error
- Timeout → Show menu, retry in background

**No Error Messages to User**: Fail-open approach means users never see error messages for status checks

## Testing Requirements

### Unit Tests

**Coverage Target**: 85%

**Test Files**:
1. `useVettingStatus.test.ts` - Hook functionality
2. `useMenuVisibility.test.ts` - Menu visibility logic
3. `VettingStatusBox.test.tsx` - Component rendering
4. `Navigation.test.tsx` - Conditional menu rendering
5. `HowToJoin.test.tsx` - Page routing and status display

**Key Test Cases**:
```typescript
describe('useMenuVisibility', () => {
  it('shows menu for unauthenticated users', () => {
    // Arrange: Mock unauthenticated state
    // Act: Call useMenuVisibility hook
    // Assert: shouldShow = true, reason = "Guest user"
  });

  it('shows menu while loading status', () => {
    // Arrange: Mock loading state
    // Act: Call useMenuVisibility hook
    // Assert: shouldShow = true, reason = "Loading status"
  });

  it('hides menu for OnHold status', () => {
    // Arrange: Mock status = OnHold
    // Act: Call useMenuVisibility hook
    // Assert: shouldShow = false, reason = "Status: OnHold"
  });

  it('shows menu for Submitted status', () => {
    // Arrange: Mock status = Submitted
    // Act: Call useMenuVisibility hook
    // Assert: shouldShow = true
  });

  it('fails open on API error', () => {
    // Arrange: Mock API error
    // Act: Call useMenuVisibility hook
    // Assert: shouldShow = true, reason = "API error - fail-open"
  });
});
```

### Integration Tests

**Test Scenarios**:
1. **Guest User Journey**:
   - Navigate to site
   - Verify "How to Join" visible
   - Click menu item
   - See vetting info and login prompt

2. **New Member Journey**:
   - Log in as new member
   - Verify "How to Join" visible
   - Navigate to page
   - See application form

3. **Pending Applicant Journey**:
   - Log in as user with Submitted status
   - Verify "How to Join" visible
   - Navigate to page
   - See status box with progress info

4. **Vetted Member Journey**:
   - Log in as vetted member (Approved status)
   - Verify "How to Join" NOT visible
   - Direct URL navigate to `/how-to-join`
   - See "already vetted" message

5. **On-Hold User Journey**:
   - Log in as user with OnHold status
   - Verify "How to Join" NOT visible
   - Direct URL navigate to `/how-to-join`
   - See on-hold message with contact info

### E2E Tests (Playwright)

**Test File**: `/apps/web/tests/playwright/vetting-menu-visibility.spec.ts`

```typescript
import { test, expect } from '@playwright/test';

test.describe('How to Join Menu Visibility', () => {
  test('guest user sees menu item', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByTestId('link-how-to-join')).toBeVisible();
  });

  test('vetted member does not see menu item', async ({ page, context }) => {
    // Login as vetted member
    await context.addCookies([/* vetted member auth cookie */]);
    await page.goto('/');
    await expect(page.getByTestId('link-how-to-join')).not.toBeVisible();
  });

  test('on-hold user cannot access form', async ({ page, context }) => {
    // Login as on-hold user
    await context.addCookies([/* on-hold user auth cookie */]);
    await page.goto('/how-to-join');
    await expect(page.getByText('on hold')).toBeVisible();
    await expect(page.getByRole('button', { name: /submit/i })).not.toBeVisible();
  });
});
```

### Performance Tests

**Metrics to Validate**:
- TanStack Query cache hit rate > 90%
- API calls < 1 per 5 minutes per user
- Menu render time < 100ms
- Status box render time < 200ms
- Mobile lighthouse score > 90

## Migration Requirements

### Database Migrations
**None Required** - Feature uses existing vetting application data structure

### Data Transformation
**None Required** - API endpoint already returns correct format

### Frontend Migration Steps

1. **Create New Files** (Day 1):
   - `useVettingStatus.ts`
   - `useMenuVisibility.ts`
   - `vetting-status.types.ts`
   - `VettingStatusBox.tsx`

2. **Update Existing Files** (Day 2):
   - `Navigation.tsx` - Add conditional rendering
   - `HowToJoin.tsx` - Add status display logic
   - `vettingApi.ts` - Add `getMyStatus` method

3. **Testing** (Day 3):
   - Write unit tests
   - Write integration tests
   - Manual testing for all status states

4. **Deployment** (Day 4):
   - Deploy to staging
   - QA validation
   - Production deployment (no backend changes needed)

### Backward Compatibility
- Feature is additive - no breaking changes
- Existing application form continues to work
- API endpoint already exists - no backend deployment required
- Old behavior (menu always visible) gracefully replaced

## Dependencies

### NPM Packages (Already Installed)
- `@tanstack/react-query` ^5.0.0 - Data fetching
- `@mantine/core` ^7.0.0 - UI components
- `@mantine/hooks` ^7.0.0 - React hooks
- `zustand` ^4.4.0 - State management
- `react-router-dom` ^6.18.0 - Routing
- `@tabler/icons-react` ^2.40.0 - Icons

### External Services
- **None** - Feature uses existing API infrastructure

### Configuration Needs
- **None** - Uses existing API base URL from environment

## Acceptance Criteria

### Technical Criteria

- [x] **TC-1**: `useVettingStatus` hook fetches status from API with 5-minute cache
- [x] **TC-2**: `useMenuVisibility` hook returns correct visibility for all 10 status states
- [x] **TC-3**: Navigation component conditionally renders menu item based on visibility hook
- [x] **TC-4**: HowToJoin page displays status box for users with applications
- [x] **TC-5**: Status box renders different variants for each status
- [x] **TC-6**: API errors fail-open (show menu)
- [x] **TC-7**: Loading states handled gracefully (show menu while loading)
- [x] **TC-8**: Query invalidation works after application submission
- [x] **TC-9**: Direct URL navigation to `/how-to-join` works for all user states
- [x] **TC-10**: Mobile responsive design passes on < 768px screens

### Functional Criteria

- [x] **FC-1**: Guest users always see "How to Join" menu item
- [x] **FC-2**: Authenticated users without application see menu item
- [x] **FC-3**: Users with Submitted application see menu item AND status box
- [x] **FC-4**: Users with OnHold status do NOT see menu item
- [x] **FC-5**: Vetted members (Approved) do NOT see menu item
- [x] **FC-6**: Denied users do NOT see menu item
- [x] **FC-7**: Withdrawn users see menu item and can submit new application
- [x] **FC-8**: Status descriptions match backend `GetStatusDescription` logic
- [x] **FC-9**: Next steps information displays correctly for each status
- [x] **FC-10**: Estimated days remaining displays when available

### Performance Criteria

- [x] **PC-1**: Menu visibility decision completes in < 100ms (cached)
- [x] **PC-2**: API call completes in < 300ms on cache miss
- [x] **PC-3**: Cache hit rate > 90% for active users
- [x] **PC-4**: Status box renders in < 200ms
- [x] **PC-5**: Mobile performance score > 90 on Lighthouse

### Security Criteria

- [x] **SC-1**: Only authenticated users can call vetting status API
- [x] **SC-2**: Users can only view their own application status
- [x] **SC-3**: 401 errors handled gracefully without exposing auth details
- [x] **SC-4**: No XSS vulnerabilities in status text rendering
- [x] **SC-5**: HTTPS required for all API calls

### Accessibility Criteria

- [x] **AC-1**: Keyboard navigation works for all interactive elements
- [x] **AC-2**: Screen reader announces status changes
- [x] **AC-3**: Color contrast meets WCAG AA standards
- [x] **AC-4**: Touch targets minimum 44px on mobile
- [x] **AC-5**: Focus indicators visible on all interactive elements

## Data Flow Diagrams

### Menu Visibility Decision Flow

```
┌─────────────────┐
│  User Navigates │
│    to Site      │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Navigation     │
│  Component      │
│  Renders        │
└────────┬────────┘
         │
         ▼
┌─────────────────┐      ┌──────────────────┐
│ useMenuVisi-    │      │  Zustand Auth    │
│ bility() Hook   │◄─────┤  Store           │
└────────┬────────┘      └──────────────────┘
         │
         ├─ Not Authenticated ─────► Show Menu (Guest)
         │
         ├─ Authenticated ─────┐
         │                     │
         │                     ▼
         │              ┌──────────────────┐
         │              │ useVettingStatus │
         │              │    Hook          │
         │              └────────┬─────────┘
         │                       │
         │                ┌──────┴──────┐
         │                │             │
         │           Loading?      Cached?
         │                │             │
         │           Show Menu    ┌─────┘
         │                        │
         │                        ▼
         │              ┌──────────────────┐
         │              │ TanStack Query   │
         │              │ Fetch from API   │
         │              └────────┬─────────┘
         │                       │
         │                ┌──────┴──────┐
         │                │             │
         │            Error?         Success
         │                │             │
         │           Show Menu          │
         │           (Fail-open)        │
         │                              │
         │                              ▼
         │              ┌──────────────────────┐
         │              │ Parse Response       │
         │              │ Check HasApplication │
         │              └────────┬─────────────┘
         │                       │
         │                ┌──────┴──────┐
         │                │             │
         │           No App?         Has App
         │                │             │
         │           Show Menu          │
         │                              │
         │                              ▼
         │              ┌──────────────────────┐
         │              │ Check Status         │
         │              │ OnHold/Approved/     │
         │              │ Denied?              │
         │              └────────┬─────────────┘
         │                       │
         │                ┌──────┴──────┐
         │                │             │
         │              Yes           No
         │                │             │
         │           Hide Menu      Show Menu
         │                │             │
         └────────────────┴─────────────┘
                          │
                          ▼
         ┌─────────────────────────────┐
         │  Navigation Renders         │
         │  with/without Menu Item     │
         └─────────────────────────────┘
```

### Application Status Display Flow

```
┌─────────────────┐
│ User Navigates  │
│ to /how-to-join │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  HowToJoin Page │
│  Component      │
└────────┬────────┘
         │
         ├─ Not Authenticated ─────► Show Login Prompt
         │
         ├─ Authenticated ─────┐
         │                     │
         │                     ▼
         │              ┌──────────────────┐
         │              │ useVettingStatus │
         │              │    Hook          │
         │              └────────┬─────────┘
         │                       │
         │                       ▼
         │              ┌──────────────────┐
         │              │ No Application?  │
         │              └────────┬─────────┘
         │                       │
         │                ┌──────┴──────┐
         │                │             │
         │              Yes            No
         │                │             │
         │                ▼             ▼
         │      ┌──────────────┐  ┌──────────────┐
         │      │ Show Vetting │  │ Show Status  │
         │      │ Form         │  │ Box          │
         │      └──────────────┘  └──────┬───────┘
         │                               │
         │                               ▼
         │              ┌──────────────────────────┐
         │              │ Render Status Variant    │
         │              │ Based on Status Enum     │
         │              └────────┬─────────────────┘
         │                       │
         │                       ├─ Submitted ──────► SubmittedStatusBox
         │                       ├─ UnderReview ────► UnderReviewStatusBox
         │                       ├─ Interview* ─────► InterviewStatusBox
         │                       ├─ OnHold ─────────► OnHoldStatusBox
         │                       ├─ Approved ───────► ApprovedStatusBox
         │                       ├─ Denied ─────────► DeniedStatusBox
         │                       └─ Withdrawn ──────► Show New Form
         │
         └────────────────┐
                          │
                          ▼
         ┌─────────────────────────────┐
         │  Status Box Displays:       │
         │  - Application #            │
         │  - Status Description       │
         │  - Next Steps               │
         │  - Estimated Days           │
         └─────────────────────────────┘
```

### Cache Invalidation Flow

```
┌─────────────────┐
│ User Submits    │
│ Application     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ VettingAppli-   │
│ cationForm      │
│ onSubmitSuccess │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ queryClient     │
│ .invalidate-    │
│ Queries()       │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ TanStack Query  │
│ Marks ['vetting'│
│ 'status'] stale │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Next Component  │
│ Mount Triggers  │
│ Refetch         │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Fresh Status    │
│ Data Loaded     │
└─────────────────┘
```

## Implementation Phases

### Phase 1: Foundation (Day 1)
**Goal**: Create hooks and types

**Tasks**:
1. Create `vetting-status.types.ts` with TypeScript interfaces
2. Implement `useVettingStatus` hook with TanStack Query
3. Implement `useMenuVisibility` hook with business logic
4. Add `getMyStatus` method to `vettingApi.ts`
5. Write unit tests for hooks

**Deliverables**:
- All hooks tested and working
- Types match backend DTO structure
- API integration functional

### Phase 2: UI Components (Day 2)
**Goal**: Build status display components

**Tasks**:
1. Create `VettingStatusBox` base component
2. Implement status-specific box variants
3. Update `Navigation.tsx` with conditional rendering
4. Update `HowToJoin.tsx` with status display
5. Write component unit tests

**Deliverables**:
- All UI components rendering correctly
- Mobile responsive design verified
- Accessibility standards met

### Phase 3: Integration & Testing (Day 3)
**Goal**: End-to-end functionality

**Tasks**:
1. Integrate status box into HowToJoin page
2. Add cache invalidation to application form
3. Write integration tests
4. Write E2E tests with Playwright
5. Manual testing for all status states

**Deliverables**:
- All tests passing
- All 10 status states verified
- Cache invalidation working

### Phase 4: Polish & Deploy (Day 4)
**Goal**: Production readiness

**Tasks**:
1. Performance testing and optimization
2. Accessibility audit
3. Error handling verification
4. Documentation updates
5. Staging deployment and QA
6. Production deployment

**Deliverables**:
- Feature deployed to production
- Documentation complete
- Acceptance criteria validated

## Risk Mitigation

### Risk: API Downtime
**Mitigation**: Fail-open strategy ensures users can always access vetting page
**Impact**: Low - menu shows for all users, no functionality blocked

### Risk: Cache Staleness
**Mitigation**: 5-minute staleTime balanced with refetch on window focus
**Impact**: Low - users see slightly stale status, refreshes automatically

### Risk: Browser Compatibility
**Mitigation**: TanStack Query and Mantine support all modern browsers
**Impact**: None - no IE11 support required

### Risk: Mobile Performance
**Mitigation**: < 1KB API payload, efficient caching, mobile-first design
**Impact**: None - lightweight implementation

### Risk: Race Conditions
**Mitigation**: TanStack Query handles concurrent requests automatically
**Impact**: None - built-in deduplication prevents race conditions

---

## Appendix A: Status Descriptions and Next Steps

Based on backend `VettingService.GetStatusDescription()` and `GetNextSteps()` methods:

| Status | Description | Next Steps |
|--------|-------------|------------|
| **Draft** | Application started but not submitted | Complete and submit your application |
| **Submitted** | Application submitted, awaiting review | No action needed - we'll contact you via email when review begins |
| **UnderReview** | Currently being reviewed by vetting committee | Watch your email for reference contact notifications |
| **InterviewApproved** | Approved for interview phase | Wait for email with interview scheduling options |
| **PendingInterview** | Waiting for interview to be scheduled | Respond promptly to scheduling email |
| **InterviewScheduled** | Interview date/time confirmed | Attend your scheduled interview |
| **OnHold** | Additional information needed or temporary pause | Check your email for requests from vetting committee |
| **Approved** | Vetting approved - you are a vetted member | Welcome! Access member resources via dashboard |
| **Denied** | Application not approved at this time | You may reapply after 6 months from decision date |
| **Withdrawn** | Application withdrawn | You may submit a new application anytime |

## Appendix B: Example API Responses

### No Application
```json
{
  "success": true,
  "data": {
    "hasApplication": false,
    "application": null
  },
  "message": "Vetting status retrieved successfully",
  "timestamp": "2025-10-04T14:35:00Z"
}
```

### Submitted Application
```json
{
  "success": true,
  "data": {
    "hasApplication": true,
    "application": {
      "applicationId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "applicationNumber": "a1b2c3d4",
      "status": "Submitted",
      "statusDescription": "Application submitted, awaiting review",
      "submittedAt": "2025-10-04T12:00:00Z",
      "lastUpdated": "2025-10-04T12:00:00Z",
      "nextSteps": "No action needed - we'll contact you via email when review begins",
      "estimatedDaysRemaining": 14
    }
  },
  "message": "Vetting status retrieved successfully",
  "timestamp": "2025-10-04T14:35:00Z"
}
```

### Approved Application
```json
{
  "success": true,
  "data": {
    "hasApplication": true,
    "application": {
      "applicationId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "applicationNumber": "a1b2c3d4",
      "status": "Approved",
      "statusDescription": "Vetting approved - you are a vetted member",
      "submittedAt": "2025-09-20T10:00:00Z",
      "lastUpdated": "2025-10-01T15:30:00Z",
      "nextSteps": "Welcome! Access member resources via dashboard",
      "estimatedDaysRemaining": null
    }
  },
  "message": "Vetting status retrieved successfully",
  "timestamp": "2025-10-04T14:35:00Z"
}
```

---

**Document Version**: 1.0
**Last Updated**: 2025-10-04
**Status**: Draft - Ready for Technical Review
**Next Steps**: Submit for stakeholder approval, then proceed to implementation
