# Functional Specification: User Dashboard Redesign
<!-- Last Updated: 2025-10-09 -->
<!-- Version: 2.0 -->
<!-- Owner: Functional Spec Agent -->
<!-- Status: APPROVED FOR IMPLEMENTATION -->

## 📋 Document Overview

**Status**: ✅ **APPROVED FOR IMPLEMENTATION**
**Approved Wireframe**: `dashboard-wireframe-v4-iteration.html`
**Approval Date**: October 9, 2025
**Business Requirements**: v6.0 (APPROVED)

This functional specification translates the approved wireframe v4 and business requirements v6.0 into concrete technical implementation requirements for the WitchCityRope User Dashboard redesign.

---

## 🎯 Technical Overview

The User Dashboard is a **React-based, user-focused dashboard** that displays registered events and manages profile settings. This is NOT a public sales page - all sales elements (pricing, capacity, "Learn More") are removed in favor of user-specific event management.

### Key Design Principles
1. **User Dashboard Context**: Show only user's registered events
2. **Status-Based Display**: Registration status, not sales status
3. **Conditional Alerts**: Vetting status alerts for non-vetted users only
4. **Clean Interface**: Simplified filtering with "Show Past Events" checkbox
5. **Flexible Views**: Grid (cards) and Table (list) view options
6. **Mobile-First**: Responsive design for all devices

---

## 🏗️ Architecture

### Microservices Architecture
**CRITICAL**: This is a Web+API microservices architecture:
- **Web Service** (React + Vite): UI/Auth at http://localhost:5173
- **API Service** (Minimal API): Business logic at http://localhost:5655
- **Database** (PostgreSQL): localhost:5433
- **Pattern**: Web → HTTP → API → Database (NEVER Web → Database directly)

### Technology Stack
- **Frontend**: React 18 + TypeScript + Vite
- **UI Framework**: Mantine v7 (ADR-004)
- **State Management**: TanStack Query v5 + Zustand
- **Routing**: React Router v7
- **Type Generation**: NSwag (from API OpenAPI spec)
- **Backend**: .NET 9 Minimal API
- **Database**: PostgreSQL 16 with Entity Framework Core 9

### Component Structure
```
/apps/web/src/
├── features/
│   └── dashboard/
│       ├── pages/
│       │   ├── MyEventsPage.tsx
│       │   └── ProfileSettingsPage.tsx
│       ├── components/
│       │   ├── VettingAlertBox.tsx
│       │   ├── EventCard.tsx
│       │   ├── EventTable.tsx
│       │   ├── FilterBar.tsx
│       │   └── ProfileTabs.tsx
│       ├── hooks/
│       │   ├── useUserEvents.ts
│       │   ├── useVettingStatus.ts
│       │   └── useProfile.ts
│       ├── services/
│       │   └── dashboardService.ts
│       └── types/
│           └── dashboard.types.ts (from NSwag)
```

### Service Architecture
- **Web Service**: React components make HTTP calls to API (NO direct database access)
- **API Service**: Business logic with EF Core database access
- **Authentication**: BFF pattern with httpOnly cookies (existing auth system)

---

## 📊 Data Models

### Database Schema

#### User Events View (No New Tables - Uses Existing)
The dashboard uses existing tables:
- `Events` - Event information
- `Registrations` - User registrations (RSVP)
- `Orders` - Ticket purchases
- `Users` - User profile data (vetting status)

### DTOs and ViewModels

#### UserEventDto (API Response)
**CRITICAL**: This is NOT the same as `PublicEventDto` - different fields for dashboard context.

```csharp
/// <summary>
/// User's registered event information for dashboard display
/// </summary>
public class UserEventDto
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // "Social Event", "Workshop", "Class"

    /// <summary>
    /// Registration status: "RSVP Confirmed", "Ticket Purchased", "Attended"
    /// </summary>
    public string RegistrationStatus { get; set; } = string.Empty;

    public bool IsPastEvent { get; set; }

    // NO pricing fields
    // NO capacity fields
    // NO availability fields
}
```

#### VettingStatusDto (API Response)
```csharp
/// <summary>
/// User's vetting status for alert display
/// </summary>
public class VettingStatusDto
{
    public Guid UserId { get; set; }

    /// <summary>
    /// Vetting status: "Pending", "Approved", "On Hold", "Denied", "Vetted"
    /// </summary>
    public string VettingStatus { get; set; } = string.Empty;

    public DateTime VettingStatusUpdatedAt { get; set; }

    // Optional fields for specific statuses
    public string? InterviewScheduleUrl { get; set; } // For "Approved"
    public string? ReapplyInfoUrl { get; set; } // For "Denied"
}
```

#### UserProfileDto (Existing)
```csharp
/// <summary>
/// User profile information for settings page
/// </summary>
public class UserProfileDto
{
    public Guid UserId { get; set; }
    public string SceneName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Pronouns { get; set; }
    public string? Bio { get; set; }
    public string? FetLifeUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

#### UpdateProfileDto (Request)
```csharp
public class UpdateProfileDto
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string SceneName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Pronouns { get; set; }

    [StringLength(500)]
    public string? Bio { get; set; }

    [Url]
    public string? FetLifeUrl { get; set; }

    [Url]
    public string? InstagramUrl { get; set; }
}
```

#### ChangePasswordDto (Request)
```csharp
public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Password must contain uppercase, lowercase, and number")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

### TypeScript Interfaces (NSwag Generated)
**CRITICAL**: These will be auto-generated from C# DTOs via NSwag pipeline.

```typescript
// Generated from @witchcityrope/shared-types
export interface UserEventDto {
  eventId: string; // Guid → string
  title: string;
  startDate: string; // DateTime → ISO 8601 string
  startTime: string;
  location: string;
  eventType: string;
  registrationStatus: 'RSVP Confirmed' | 'Ticket Purchased' | 'Attended';
  isPastEvent: boolean;
}

export interface VettingStatusDto {
  userId: string;
  vettingStatus: 'Pending' | 'Approved' | 'On Hold' | 'Denied' | 'Vetted';
  vettingStatusUpdatedAt: string;
  interviewScheduleUrl?: string | null;
  reapplyInfoUrl?: string | null;
}

export interface UserProfileDto {
  userId: string;
  sceneName: string;
  email: string;
  pronouns?: string | null;
  bio?: string | null;
  fetLifeUrl?: string | null;
  instagramUrl?: string | null;
  createdAt: string;
  updatedAt: string;
}
```

---

## 🔌 API Specifications

### Endpoints

#### GET /api/users/{userId}/events
**Purpose**: Fetch user's registered events for dashboard display
**Authorization**: User must be authenticated and accessing own data
**Response**: `ApiResponse<List<UserEventDto>>`

**Query Parameters**:
- `includePast` (optional, boolean, default: false) - Include past events
- `search` (optional, string) - Filter by event title

**Example Request**:
```http
GET /api/users/123e4567-e89b-12d3-a456-426614174000/events?includePast=false
Authorization: Cookie (httpOnly JWT)
```

**Example Response**:
```json
{
  "success": true,
  "data": [
    {
      "eventId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "title": "Halloween Rope Social",
      "startDate": "2025-10-31T19:00:00Z",
      "startTime": "7:00 PM",
      "location": "Salem Community Center",
      "eventType": "Social Event",
      "registrationStatus": "RSVP Confirmed",
      "isPastEvent": false
    }
  ],
  "timestamp": "2025-10-09T12:34:56Z"
}
```

#### GET /api/users/{userId}/vetting-status
**Purpose**: Fetch user's vetting status for alert box
**Authorization**: User must be authenticated and accessing own data
**Response**: `ApiResponse<VettingStatusDto>`

**Example Request**:
```http
GET /api/users/123e4567-e89b-12d3-a456-426614174000/vetting-status
Authorization: Cookie (httpOnly JWT)
```

**Example Response** (Approved status):
```json
{
  "success": true,
  "data": {
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "vettingStatus": "Approved",
    "vettingStatusUpdatedAt": "2025-10-01T10:00:00Z",
    "interviewScheduleUrl": "/vetting/schedule-interview",
    "reapplyInfoUrl": null
  },
  "timestamp": "2025-10-09T12:34:56Z"
}
```

#### GET /api/users/{userId}/profile
**Purpose**: Fetch user profile for settings page
**Authorization**: User must be authenticated and accessing own data
**Response**: `ApiResponse<UserProfileDto>`

**Example Request**:
```http
GET /api/users/123e4567-e89b-12d3-a456-426614174000/profile
Authorization: Cookie (httpOnly JWT)
```

#### PUT /api/users/{userId}/profile
**Purpose**: Update user profile information
**Authorization**: User must be authenticated and accessing own data
**Request Body**: `UpdateProfileDto`
**Response**: `ApiResponse<UserProfileDto>`

**Example Request**:
```http
PUT /api/users/123e4567-e89b-12d3-a456-426614174000/profile
Content-Type: application/json
Authorization: Cookie (httpOnly JWT)

{
  "sceneName": "ShadowKnot",
  "email": "shadowknot@example.com",
  "pronouns": "they/them",
  "bio": "Rope enthusiast since 2023",
  "fetLifeUrl": "https://fetlife.com/users/shadowknot",
  "instagramUrl": null
}
```

#### POST /api/users/{userId}/change-password
**Purpose**: Change user password
**Authorization**: User must be authenticated and accessing own data
**Request Body**: `ChangePasswordDto`
**Response**: `ApiResponse<bool>`

**Example Request**:
```http
POST /api/users/123e4567-e89b-12d3-a456-426614174000/change-password
Content-Type: application/json
Authorization: Cookie (httpOnly JWT)

{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewPassword123!",
  "confirmPassword": "NewPassword123!"
}
```

### API Response Wrapper
**MANDATORY**: ALL endpoints MUST return consistent `ApiResponse<T>` format.

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

---

## 🎨 Component Specifications

### 1. DashboardLayout Component

**Path**: `/apps/web/src/layouts/DashboardLayout.tsx`
**Purpose**: Main layout wrapper for dashboard pages
**Render Mode**: React component with React Router Outlet

**Props**: None (uses existing auth context)

**Key Features**:
- Uses existing Navigation component
- Adds "Edit Profile" link to utility bar (before Logout)
- Page title format: `{firstName} Dashboard`
- "Edit Profile" button to right of title
- Responsive layout with Mantine Grid

**Implementation**:
```tsx
import { Container, Group, Title, Button } from '@mantine/core';
import { useAuth } from '@/contexts/AuthContext';
import { Navigation } from '@/components/Navigation';
import { Outlet } from 'react-router-dom';

export function DashboardLayout() {
  const { user } = useAuth();

  return (
    <>
      <Navigation /> {/* Updated with Edit Profile link */}

      <Container size="xl" py="xl">
        {/* Page Title Bar */}
        <Group justify="space-between" mb="lg">
          <Title order={1} tt="uppercase" c="burgundy">
            {user?.firstName} Dashboard
          </Title>
          <Button
            component={Link}
            to="/dashboard/profile-settings"
            variant="outline"
            color="rose-gold"
          >
            Edit Profile
          </Button>
        </Group>

        {/* Page Content */}
        <Outlet />
      </Container>
    </>
  );
}
```

### 2. MyEventsPage Component

**Path**: `/apps/web/src/features/dashboard/pages/MyEventsPage.tsx`
**Route**: `/dashboard`
**Authorization**: Authenticated users only

**State Management**:
```tsx
// Using TanStack Query for data fetching
const { data: events, isLoading, error } = useUserEvents();
const { data: vettingStatus } = useVettingStatus();

// Local state for filters
const [showPast, setShowPast] = useState(false);
const [viewMode, setViewMode] = useState<'grid' | 'table'>('grid');
const [searchQuery, setSearchQuery] = useState('');
```

**Component Structure**:
```tsx
export function MyEventsPage() {
  return (
    <>
      {/* Conditional Vetting Alert */}
      {vettingStatus && vettingStatus !== 'Vetted' && (
        <VettingAlertBox status={vettingStatus} />
      )}

      {/* Filter Bar */}
      <FilterBar
        showPast={showPast}
        onShowPastChange={setShowPast}
        viewMode={viewMode}
        onViewModeChange={setViewMode}
        searchQuery={searchQuery}
        onSearchChange={setSearchQuery}
      />

      {/* Event Display (Grid or Table) */}
      {viewMode === 'grid' ? (
        <EventGrid events={filteredEvents} />
      ) : (
        <EventTable events={filteredEvents} />
      )}
    </>
  );
}
```

### 3. VettingAlertBox Component

**Path**: `/apps/web/src/features/dashboard/components/VettingAlertBox.tsx`
**Purpose**: Display conditional vetting status alerts

**Props**:
```tsx
interface VettingAlertBoxProps {
  status: 'Pending' | 'Approved' | 'On Hold' | 'Denied';
}
```

**Alert Configurations**:
| Status | Icon | Color | Title | Message |
|--------|------|-------|-------|---------|
| Pending | ⏰ | blue | "Application Under Review" | "Your membership application is currently under review..." |
| Approved | ✅ | green | "Great News! Your Application Has Been Approved" | "Schedule your vetting interview here..." (with link) |
| On Hold | ⏸️ | yellow | "Membership On Hold" | "Your membership is currently on hold..." |
| Denied | ❌ | red | "Application Not Approved" | "Your membership application was not approved..." (with link) |

**Implementation**:
```tsx
import { Alert } from '@mantine/core';

const alertConfigs = {
  Pending: {
    icon: '⏰',
    color: 'blue',
    title: 'Application Under Review',
    message: "Your membership application is currently under review. We'll notify you via email once it's been reviewed."
  },
  Approved: {
    icon: '✅',
    color: 'green',
    title: 'Great News! Your Application Has Been Approved',
    message: (
      <>
        <Anchor href="/vetting/schedule-interview">Schedule your vetting interview here</Anchor>
        {' '}to complete your membership.
      </>
    )
  },
  // ... other configs
};

export function VettingAlertBox({ status }: VettingAlertBoxProps) {
  const config = alertConfigs[status];

  return (
    <Alert
      icon={config.icon}
      color={config.color}
      title={config.title}
      mb="lg"
      radius="md"
    >
      {config.message}
    </Alert>
  );
}
```

### 4. EventCard Component (User Dashboard Version)

**Path**: `/apps/web/src/features/dashboard/components/EventCard.tsx`
**Purpose**: Display event information in grid view

**CRITICAL DIFFERENCES from Public Event Card**:
- ❌ NO pricing information
- ❌ NO capacity/availability
- ❌ NO "Learn More" button
- ✅ USES "View Details" button
- ✅ Shows registration status badge

**Props**:
```tsx
interface EventCardProps {
  event: UserEventDto;
}
```

**Implementation**:
```tsx
import { Card, Text, Badge, Button, Group, Stack } from '@mantine/core';
import { Link } from 'react-router-dom';

export function EventCard({ event }: EventCardProps) {
  const statusColors = {
    'RSVP Confirmed': 'blue',
    'Ticket Purchased': 'green',
    'Attended': 'grape'
  };

  return (
    <Card
      shadow="sm"
      padding="lg"
      radius="md"
      withBorder
      className={event.isPastEvent ? 'past-event' : ''}
    >
      {/* Gradient Header */}
      <Card.Section
        h={100}
        style={{
          background: 'linear-gradient(135deg, #9b4a75 0%, #880124 100%)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center'
        }}
      >
        <Text c="white" fw={700} size="lg" ta="center" px="md">
          {event.title}
        </Text>
      </Card.Section>

      <Stack gap="sm" mt="md">
        {/* Date/Time */}
        <Text fw={700} c="burgundy" size="sm" tt="uppercase">
          {formatEventDate(event.startDate)} • {event.startTime}
        </Text>

        {/* Location */}
        <Text size="sm" c="dimmed">
          📍 {event.location}
        </Text>

        {/* Status Badge */}
        <Badge color={statusColors[event.registrationStatus]}>
          {event.registrationStatus}
        </Badge>

        {/* Action Button */}
        <Button
          component={Link}
          to={`/events/${event.eventId}`}
          variant="outline"
          color="rose-gold"
          fullWidth
        >
          View Details
        </Button>
      </Stack>
    </Card>
  );
}
```

### 5. EventTable Component (User Dashboard Version)

**Path**: `/apps/web/src/features/dashboard/components/EventTable.tsx`
**Purpose**: Display events in table/list view

**Columns**: Date | Time | Event Title | Status | Action

**Props**:
```tsx
interface EventTableProps {
  events: UserEventDto[];
}
```

**Implementation**:
```tsx
import { Table, Badge, Button } from '@mantine/core';
import { Link } from 'react-router-dom';

export function EventTable({ events }: EventTableProps) {
  return (
    <Table
      striped
      highlightOnHover
      withTableBorder
      styles={{
        thead: {
          backgroundColor: 'var(--color-burgundy)',
          color: 'white'
        }
      }}
    >
      <Table.Thead>
        <Table.Tr>
          <Table.Th>Date ↕</Table.Th>
          <Table.Th>Time</Table.Th>
          <Table.Th>Event Title</Table.Th>
          <Table.Th ta="center">Status</Table.Th>
          <Table.Th ta="center">Action</Table.Th>
        </Table.Tr>
      </Table.Thead>
      <Table.Tbody>
        {events.map((event) => (
          <Table.Tr
            key={event.eventId}
            onClick={() => navigate(`/events/${event.eventId}`)}
            style={{ cursor: 'pointer' }}
            className={event.isPastEvent ? 'past-event' : ''}
          >
            <Table.Td fw={700}>{formatShortDate(event.startDate)}</Table.Td>
            <Table.Td>{event.startTime}</Table.Td>
            <Table.Td c="burgundy" fw={600}>{event.title}</Table.Td>
            <Table.Td ta="center">
              <Badge color={getStatusColor(event.registrationStatus)}>
                {event.registrationStatus}
              </Badge>
            </Table.Td>
            <Table.Td ta="center">
              <Button
                component={Link}
                to={`/events/${event.eventId}`}
                variant="outline"
                size="xs"
                onClick={(e) => e.stopPropagation()}
              >
                View Details
              </Button>
            </Table.Td>
          </Table.Tr>
        ))}
      </Table.Tbody>
    </Table>
  );
}
```

### 6. FilterBar Component

**Path**: `/apps/web/src/features/dashboard/components/FilterBar.tsx`
**Purpose**: Event filtering and view controls

**Controls**:
- Show Past Events checkbox (unchecked by default)
- View toggle (Card View / List View)
- Search input

**Props**:
```tsx
interface FilterBarProps {
  showPast: boolean;
  onShowPastChange: (show: boolean) => void;
  viewMode: 'grid' | 'table';
  onViewModeChange: (mode: 'grid' | 'table') => void;
  searchQuery: string;
  onSearchChange: (query: string) => void;
}
```

**Implementation**:
```tsx
import { Group, Checkbox, SegmentedControl, TextInput } from '@mantine/core';
import { IconSearch } from '@tabler/icons-react';

export function FilterBar({
  showPast,
  onShowPastChange,
  viewMode,
  onViewModeChange,
  searchQuery,
  onSearchChange
}: FilterBarProps) {
  return (
    <Group
      justify="space-between"
      p="md"
      bg="var(--color-ivory)"
      style={{ borderRadius: 8, border: '1px solid var(--color-taupe)' }}
      mb="lg"
    >
      <Group>
        {/* Show Past Events Checkbox */}
        <Checkbox
          label="Show Past Events"
          checked={showPast}
          onChange={(e) => onShowPastChange(e.currentTarget.checked)}
        />

        {/* View Toggle */}
        <SegmentedControl
          value={viewMode}
          onChange={(value) => onViewModeChange(value as 'grid' | 'table')}
          data={[
            { label: 'Card View', value: 'grid' },
            { label: 'List View', value: 'table' }
          ]}
        />
      </Group>

      {/* Search Input */}
      <TextInput
        placeholder="Search events..."
        leftSection={<IconSearch size={16} />}
        value={searchQuery}
        onChange={(e) => onSearchChange(e.currentTarget.value)}
        styles={{ input: { borderRadius: 25 } }}
      />
    </Group>
  );
}
```

### 7. ProfileSettingsPage Component

**Path**: `/apps/web/src/features/dashboard/pages/ProfileSettingsPage.tsx`
**Route**: `/dashboard/profile-settings`
**Authorization**: Authenticated users only

**Tab Structure**:
1. Personal (default) - Scene name, email, pronouns, bio
2. Social - Social media links
3. Security - Password change
4. Vetting - Read-only vetting status

**Implementation**:
```tsx
import { Tabs } from '@mantine/core';

export function ProfileSettingsPage() {
  return (
    <Tabs defaultValue="personal">
      <Tabs.List>
        <Tabs.Tab value="personal">Personal</Tabs.Tab>
        <Tabs.Tab value="social">Social</Tabs.Tab>
        <Tabs.Tab value="security">Security</Tabs.Tab>
        <Tabs.Tab value="vetting">Vetting</Tabs.Tab>
      </Tabs.List>

      <Tabs.Panel value="personal" pt="md">
        <PersonalInfoForm />
      </Tabs.Panel>

      <Tabs.Panel value="social" pt="md">
        <SocialLinksForm />
      </Tabs.Panel>

      <Tabs.Panel value="security" pt="md">
        <ChangePasswordForm />
      </Tabs.Panel>

      <Tabs.Panel value="vetting" pt="md">
        <VettingStatusDisplay />
      </Tabs.Panel>
    </Tabs>
  );
}
```

---

## 🔄 State Management

### TanStack Query Hooks

#### useUserEvents Hook
```tsx
import { useQuery } from '@tanstack/react-query';
import { dashboardService } from '../services/dashboardService';

export function useUserEvents(includePast = false) {
  const { user } = useAuth();

  return useQuery({
    queryKey: ['user-events', user?.id, includePast],
    queryFn: () => dashboardService.getUserEvents(user!.id, includePast),
    enabled: !!user?.id,
    staleTime: 5 * 60 * 1000 // 5 minutes
  });
}
```

#### useVettingStatus Hook
```tsx
export function useVettingStatus() {
  const { user } = useAuth();

  return useQuery({
    queryKey: ['vetting-status', user?.id],
    queryFn: () => dashboardService.getVettingStatus(user!.id),
    enabled: !!user?.id,
    staleTime: 10 * 60 * 1000 // 10 minutes
  });
}
```

#### useProfile Hook
```tsx
export function useProfile() {
  const { user } = useAuth();

  return useQuery({
    queryKey: ['user-profile', user?.id],
    queryFn: () => dashboardService.getProfile(user!.id),
    enabled: !!user?.id
  });
}
```

### Zustand Store (Optional for Filter State)
```tsx
import { create } from 'zustand';

interface DashboardFilters {
  showPast: boolean;
  viewMode: 'grid' | 'table';
  searchQuery: string;
  setShowPast: (show: boolean) => void;
  setViewMode: (mode: 'grid' | 'table') => void;
  setSearchQuery: (query: string) => void;
}

export const useDashboardFilters = create<DashboardFilters>((set) => ({
  showPast: false,
  viewMode: 'grid',
  searchQuery: '',
  setShowPast: (showPast) => set({ showPast }),
  setViewMode: (viewMode) => set({ viewMode }),
  setSearchQuery: (searchQuery) => set({ searchQuery })
}));
```

---

## 🎨 UI/UX Specifications

### Design System v7 Integration

**Colors** (from CSS variables):
```css
:root {
  --color-burgundy: #880124;
  --color-rose-gold: #B76D75;
  --color-amber: #FFBF00;
  --color-electric: #9D4EDD;
  --color-cream: #FAF6F2;
  --color-ivory: #FFF8F0;
  --color-taupe: #D4C4B0;
}
```

**Typography**:
- Headlines: 'Montserrat', sans-serif
- Body: 'Source Sans 3', sans-serif

### Grid Layout Specifications

**Event Grid**:
```css
.events-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
  gap: 32px;
}
```

**Responsive Breakpoints**:
- Mobile: `< 768px` - Single column
- Tablet: `768px - 1024px` - Two columns
- Desktop: `> 1024px` - Three+ columns

### Button Specifications

**Edit Profile Button** (Secondary):
```tsx
<Button
  variant="outline"
  color="rose-gold"
  styles={{
    root: {
      borderRadius: '12px 6px 12px 6px',
      '&:hover': {
        borderRadius: '6px 12px 6px 12px'
      }
    }
  }}
>
  Edit Profile
</Button>
```

**View Details Button** (Secondary):
```tsx
<Button
  variant="outline"
  color="rose-gold"
  fullWidth
>
  View Details
</Button>
```

### Status Badge Colors
- **RSVP Confirmed**: Blue (#2196F3)
- **Ticket Purchased**: Green (#4CAF50)
- **Attended**: Purple (#9C27B0)

### Animation Specifications

**Card Hover**:
```css
.event-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 8px 24px rgba(0,0,0,0.1);
  transition: all 0.3s ease;
}
```

**Button Corner Morphing**:
```css
.btn {
  border-radius: 12px 6px 12px 6px;
  transition: all 0.3s ease;
}

.btn:hover {
  border-radius: 6px 12px 6px 12px;
}
```

---

## ✅ Acceptance Criteria

### 1. Navigation Updates
- [ ] "Edit Profile" link added to utility bar (before Logout)
- [ ] "Edit Profile" button positioned to right of dashboard title
- [ ] Both links navigate to `/dashboard/profile-settings`
- [ ] Utility bar styling matches existing links

### 2. Page Title
- [ ] Dashboard title format: `{firstName} Dashboard`
- [ ] First name pulled from auth context
- [ ] Title displays correctly for all users
- [ ] "Edit Profile" button uses secondary styling

### 3. Vetting Alert Box
- [ ] Alert displays for Pending status (blue, clock icon)
- [ ] Alert displays for Approved status (green, checkmark, interview link)
- [ ] Alert displays for On Hold status (yellow, pause icon)
- [ ] Alert displays for Denied status (red, X icon, reapply link)
- [ ] NO alert displays for Vetted status
- [ ] Links in alerts navigate to correct pages

### 4. Event Display - Grid View
- [ ] Events display in grid layout (`repeat(auto-fill, minmax(350px, 1fr))`)
- [ ] Gradient header with event title
- [ ] Date/time formatted as "Day, Month Date • Time"
- [ ] Location displayed with pin emoji
- [ ] Status badge shows correct registration status
- [ ] "View Details" button (NOT "Learn More")
- [ ] NO pricing information visible
- [ ] NO capacity information visible
- [ ] Card hover animation (-4px translateY)

### 5. Event Display - Table View
- [ ] Table columns: Date | Time | Title | Status | Action
- [ ] Date column sortable (↕ indicator)
- [ ] Rows clickable to navigate to event details
- [ ] Status badges match grid view colors
- [ ] "View Details" button in action column
- [ ] Header has burgundy background with white text
- [ ] Rows alternate cream/white backgrounds
- [ ] Row hover highlighting works

### 6. Filter Controls
- [ ] "Show Past Events" checkbox unchecked by default
- [ ] Checkbox toggle shows/hides past events (`.hidden` class)
- [ ] View toggle switches between grid and table
- [ ] Active view button highlighted
- [ ] Search input filters events in real-time
- [ ] All filters work in both views
- [ ] Filter bar responsive on mobile

### 7. Past Events Handling
- [ ] Past events have `.past-event` class
- [ ] Past events hidden by default (`.hidden` class applied)
- [ ] Past events show "Attended" status badge
- [ ] Checkbox toggle works correctly
- [ ] Past events filter persists during view toggle

### 8. Profile Settings Page
- [ ] Four tabs display: Personal, Social, Security, Vetting
- [ ] Personal tab is default active
- [ ] All form fields editable except vetting tab
- [ ] Separate save buttons per tab
- [ ] Success messages after save
- [ ] Validation errors display clearly
- [ ] Password requirements enforced

### 9. Responsive Design
- [ ] Mobile (< 768px): Single column grid, stacked filters
- [ ] Tablet (768px-1024px): Two column grid
- [ ] Desktop (> 1024px): Three+ column grid
- [ ] All touch targets minimum 44px on mobile
- [ ] Search input expands on focus
- [ ] Table scrolls horizontally on small screens if needed

### 10. Data Accuracy
- [ ] Only user's registered events displayed
- [ ] Registration status accurate for each event
- [ ] Past events correctly identified (date < current date)
- [ ] Vetting status fetched correctly
- [ ] User's first name displayed in title
- [ ] NO public/available events shown

---

## 🔒 Security Requirements

### Authentication & Authorization
- **Cookie-Based Auth**: Use existing httpOnly cookie authentication (BFF pattern)
- **User Isolation**: Users can only access their own events and profile data
- **API Authorization**: All endpoints verify user identity via JWT from cookie
- **Route Protection**: `/dashboard` and `/dashboard/profile-settings` require authentication

### Data Validation
- **Server-Side**: All profile updates validated on API
- **Client-Side**: Form validation using Mantine form hooks
- **Password Requirements**: 8+ chars, 1 uppercase, 1 lowercase, 1 number
- **XSS Prevention**: All user-generated content sanitized
- **SQL Injection**: Use parameterized queries only

### Privacy
- **PII Protection**: Profile data encrypted in transit (HTTPS)
- **Vetting Status**: Only visible to user and admins
- **Event Data**: User sees only their registrations
- **Password Change**: Requires current password verification

---

## ⚡ Performance Requirements

### Response Time Targets
- **Dashboard Load**: < 2 seconds
- **API Calls**: < 500ms per endpoint
- **Search Filter**: < 100ms response time
- **View Toggle**: Instant (client-side only)

### Optimization Strategies
- **Data Caching**: TanStack Query caches for 5-10 minutes
- **Pagination**: Not required initially (small event counts per user)
- **Lazy Loading**: Load profile data only when tab accessed
- **Debounced Search**: 300ms debounce on search input

### Bundle Size
- **Initial Load**: < 500KB (gzipped)
- **Code Splitting**: Dashboard routes lazy loaded
- **Mantine Tree-Shaking**: Import only used components

---

## 🧪 Testing Requirements

### Unit Tests (80% Coverage)
- **Components**: All dashboard components
- **Hooks**: useUserEvents, useVettingStatus, useProfile
- **Services**: dashboardService API calls
- **Utilities**: Date formatters, status helpers

### Integration Tests
- **API Endpoints**: Test actual API responses match DTOs
- **Authentication**: Verify cookie-based auth works
- **Data Flow**: Test Web → API → Database flow

### E2E Tests (Playwright)
```typescript
test('User sees only their registered events', async ({ page }) => {
  await login(page, 'member@witchcityrope.com', 'Test123!');
  await page.goto('/dashboard');

  // Verify no pricing/capacity information
  await expect(page.locator('.event-card')).not.toContainText('$');
  await expect(page.locator('.event-card')).not.toContainText('spots available');

  // Verify "View Details" button (not "Learn More")
  await expect(page.getByRole('button', { name: 'View Details' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Learn More' })).not.toBeVisible();
});

test('Vetting alert displays for non-vetted users', async ({ page }) => {
  await login(page, 'member@witchcityrope.com', 'Test123!'); // Non-vetted user
  await page.goto('/dashboard');

  // Verify alert box visible
  await expect(page.locator('.vetting-alert')).toBeVisible();

  // Verify correct status and message
  await expect(page.locator('.vetting-alert')).toContainText('Application Under Review');
});

test('Past events toggle works', async ({ page }) => {
  await login(page, 'vetted@witchcityrope.com', 'Test123!');
  await page.goto('/dashboard');

  // Past events hidden by default
  const pastEventCount = await page.locator('.past-event:visible').count();
  expect(pastEventCount).toBe(0);

  // Check "Show Past Events"
  await page.getByLabel('Show Past Events').check();

  // Past events now visible
  const visiblePastEvents = await page.locator('.past-event:visible').count();
  expect(visiblePastEvents).toBeGreaterThan(0);
});

test('View toggle switches between grid and table', async ({ page }) => {
  await login(page, 'member@witchcityrope.com', 'Test123!');
  await page.goto('/dashboard');

  // Default: Grid view visible
  await expect(page.locator('.events-grid')).toBeVisible();
  await expect(page.locator('.events-table')).not.toBeVisible();

  // Click "List View"
  await page.getByRole('button', { name: 'List View' }).click();

  // Table view visible, grid hidden
  await expect(page.locator('.events-grid')).not.toBeVisible();
  await expect(page.locator('.events-table')).toBeVisible();
});
```

---

## 🔄 Migration Requirements

### No Database Migrations Needed
This feature uses existing tables:
- `Events` - Already exists
- `Registrations` - Already exists
- `Orders` - Already exists
- `Users` - Already exists (has vetting status field)

### Data Transformation
**None required** - API endpoints query existing data and transform to UserEventDto format.

---

## 📦 Dependencies

### Frontend Dependencies
```json
{
  "@mantine/core": "^7.0.0",
  "@mantine/hooks": "^7.0.0",
  "@mantine/form": "^7.0.0",
  "@tanstack/react-query": "^5.0.0",
  "react-router-dom": "^7.0.0",
  "zustand": "^4.0.0",
  "@tabler/icons-react": "^3.0.0"
}
```

### Backend Dependencies
```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.0" />
```

### External Services
- **Authentication**: Existing BFF auth system (httpOnly cookies)
- **Database**: PostgreSQL 16
- **Type Generation**: NSwag pipeline (from API OpenAPI spec)

---

## 🚀 Implementation Checklist

### Phase 1: Backend API Development
- [ ] Create UserEventDto (NOT same as PublicEventDto)
- [ ] Create VettingStatusDto
- [ ] Implement GET /api/users/{userId}/events endpoint
- [ ] Implement GET /api/users/{userId}/vetting-status endpoint
- [ ] Implement GET /api/users/{userId}/profile endpoint
- [ ] Implement PUT /api/users/{userId}/profile endpoint
- [ ] Implement POST /api/users/{userId}/change-password endpoint
- [ ] Add authorization checks (user can only access own data)
- [ ] Return ApiResponse<T> wrapper for all endpoints
- [ ] Add OpenAPI annotations for NSwag type generation

### Phase 2: Type Generation
- [ ] Run NSwag pipeline to generate TypeScript types
- [ ] Verify UserEventDto interface in @witchcityrope/shared-types
- [ ] Verify VettingStatusDto interface generated
- [ ] Verify UserProfileDto interface generated
- [ ] Test import paths in React components

### Phase 3: React Components
- [ ] Update Navigation component (add Edit Profile link)
- [ ] Create DashboardLayout component
- [ ] Create MyEventsPage component
- [ ] Create VettingAlertBox component (4 status variants)
- [ ] Create EventCard component (user dashboard version)
- [ ] Create EventTable component (user dashboard version)
- [ ] Create FilterBar component
- [ ] Create ProfileSettingsPage component
- [ ] Create PersonalInfoForm component
- [ ] Create SocialLinksForm component
- [ ] Create ChangePasswordForm component
- [ ] Create VettingStatusDisplay component

### Phase 4: Services & Hooks
- [ ] Create dashboardService (API calls)
- [ ] Create useUserEvents hook
- [ ] Create useVettingStatus hook
- [ ] Create useProfile hook
- [ ] Create useDashboardFilters store (optional)

### Phase 5: Routing
- [ ] Add /dashboard route
- [ ] Add /dashboard/profile-settings route
- [ ] Add route protection (authentication required)
- [ ] Test navigation from all entry points

### Phase 6: Styling
- [ ] Apply Design System v7 colors
- [ ] Implement grid layout (`repeat(auto-fill, minmax(350px, 1fr))`)
- [ ] Add card hover animation (-4px translateY)
- [ ] Add button corner morphing animation
- [ ] Implement responsive breakpoints
- [ ] Add status badge colors
- [ ] Style vetting alerts with correct colors

### Phase 7: Testing
- [ ] Write unit tests for all components
- [ ] Write integration tests for API endpoints
- [ ] Write E2E tests for user workflows
- [ ] Test vetting alert for all 5 statuses
- [ ] Test past events toggle
- [ ] Test view toggle (grid ↔ table)
- [ ] Test search filter
- [ ] Test profile settings tabs
- [ ] Test password change workflow
- [ ] Test responsive layouts

### Phase 8: Documentation
- [ ] Update component documentation
- [ ] Document API endpoints in Swagger
- [ ] Update routing documentation
- [ ] Create user guide for dashboard features

---

## 📋 Validation Checklist

### Design Accuracy
- [ ] Matches approved wireframe v4 pixel-perfect
- [ ] Uses Design System v7 colors exactly
- [ ] Typography matches specifications
- [ ] Animations match approved design
- [ ] Mobile responsive behavior correct

### Sales Element Removal
- [ ] NO pricing information anywhere
- [ ] NO capacity/availability information
- [ ] NO "Learn More" buttons
- [ ] NO "Register Now" buttons
- [ ] NO sales-focused messaging
- [ ] Focus entirely on user's registered events

### Data Accuracy
- [ ] Shows only user's registered events (NOT public events)
- [ ] Registration status correct for each event
- [ ] Past events properly identified
- [ ] Vetting status fetched correctly
- [ ] User's first name displayed in title

### Functionality
- [ ] Tab switching works (My Events ↔ Profile Settings)
- [ ] Grid view displays correctly
- [ ] Table view displays correctly
- [ ] View toggle switches correctly
- [ ] Past events toggle works
- [ ] Search filters events correctly
- [ ] "View Details" navigates to event detail page
- [ ] "Edit Profile" navigates to Profile Settings
- [ ] All profile tabs functional
- [ ] Password change workflow works

### Performance
- [ ] Dashboard loads in < 2 seconds
- [ ] API calls respond in < 500ms
- [ ] Search filter responds in < 100ms
- [ ] View toggle is instant
- [ ] No unnecessary re-renders

### Security
- [ ] Authentication required for all routes
- [ ] Users can only access own data
- [ ] httpOnly cookies used (no localStorage)
- [ ] All inputs validated
- [ ] Password requirements enforced
- [ ] XSS protection in place

---

## 🎯 Success Criteria

Implementation is complete when:

1. ✅ All API endpoints functional and returning correct data
2. ✅ All React components render correctly
3. ✅ Validation checklist 100% complete
4. ✅ All E2E tests pass
5. ✅ Performance targets met
6. ✅ Security requirements satisfied
7. ✅ Design matches approved wireframe v4 exactly
8. ✅ No sales elements visible on dashboard
9. ✅ Only user's registered events displayed
10. ✅ Project owner approves final product

---

## 📚 Related Documentation

- **Approved Design**: `/docs/functional-areas/user-dashboard/new-work/2025-08-22-user-dashboard-redesign/APPROVED-DESIGN.md`
- **Business Requirements**: `/docs/functional-areas/user-dashboard/new-work/2025-08-22-user-dashboard-redesign/requirements/business-requirements.md`
- **Approved Wireframe**: `/docs/functional-areas/user-dashboard/new-work/2025-08-22-user-dashboard-redesign/design/dashboard-wireframe-v4-iteration.html`
- **Architecture**: `/ARCHITECTURE.md`
- **DTO Alignment Strategy**: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`
- **Mantine Documentation**: https://mantine.dev/
- **TanStack Query**: https://tanstack.com/query/latest
- **React Router v7**: https://reactrouter.com/

---

**This is a user dashboard, not a sales page. Keep focus on user's registered events and profile management. Use NSwag generated types for all API DTOs. Follow microservices architecture with Web → API → Database pattern.**

*Next Phase*: React Developer to implement frontend components, Backend Developer to implement API endpoints.*
