# Component Specifications: Incident Reporting System
<!-- Last Updated: 2025-10-17 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Draft - Awaiting Approval -->

## Overview

This document provides detailed component specifications for all UI components in the Incident Reporting System. Each specification includes props, styling, behavior, and Mantine v7 integration details.

## Component Hierarchy

```
IncidentReportingApp
├── PublicIncidentReportForm (existing wireframe - updated)
├── AdminIncidentDashboard (NEW)
│   ├── IncidentFilters
│   ├── UnassignedQueueAlert
│   ├── IncidentTable
│   │   ├── SeverityBadge (4 variants)
│   │   ├── IncidentStatusBadge (5 variants)
│   │   └── IncidentActionsMenu
│   └── IncidentPagination
├── IncidentDetailPage (NEW)
│   ├── IncidentDetailHeader
│   ├── IncidentDetailsCard
│   ├── PeopleInvolvedCard
│   ├── IncidentNotesList
│   │   ├── AddNoteForm
│   │   └── NoteItem (system vs manual)
│   └── GoogleDriveSection
├── MyReportsPage (NEW)
│   ├── MyReportCard
│   └── MyReportDetailView (limited)
└── Modals
    ├── StageGuidanceModal (5 variants)
    ├── CoordinatorAssignmentModal
    ├── OnHoldModal (adapted from vetting)
    └── CloseIncidentModal
```

---

## Core Badge Components

### 1. SeverityBadge

**File**: `/apps/web/src/features/safety/components/SeverityBadge.tsx`

#### Props Interface

```typescript
interface SeverityBadgeProps {
  severity: 'Low' | 'Medium' | 'High' | 'Critical';
  size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
  'data-testid'?: string;
}
```

#### Severity Configuration

```typescript
const severityConfig = {
  Critical: {
    backgroundColor: '#AA0130',    // Bright red
    color: 'white',
    label: 'CRITICAL',
    icon: '🔴',
    ariaLabel: 'Critical severity - immediate attention required'
  },
  High: {
    backgroundColor: '#FF8C00',    // Orange
    color: 'white',
    label: 'HIGH',
    icon: '🟠',
    ariaLabel: 'High severity - urgent action needed'
  },
  Medium: {
    backgroundColor: '#FFBF00',    // Amber/gold
    color: '#1A1A2E',              // Dark text for contrast
    label: 'MEDIUM',
    icon: '🟡',
    ariaLabel: 'Medium severity - requires monitoring'
  },
  Low: {
    backgroundColor: '#4A5C3A',    // Forest green
    color: 'white',
    label: 'LOW',
    icon: '🟢',
    ariaLabel: 'Low severity - routine handling'
  }
};
```

#### Implementation

```tsx
import React from 'react';
import { Badge } from '@mantine/core';

export const SeverityBadge: React.FC<SeverityBadgeProps> = ({
  severity,
  size = 'sm',
  'data-testid': dataTestId
}) => {
  const config = severityConfig[severity];

  const getFontSize = (size: string) => {
    switch (size) {
      case 'xs': return '10px';
      case 'sm': return '12px';
      case 'md': return '14px';
      case 'lg': return '16px';
      case 'xl': return '18px';
      default: return '12px';
    }
  };

  return (
    <Badge
      size={size}
      data-testid={dataTestId}
      aria-label={config.ariaLabel}
      style={{
        backgroundColor: config.backgroundColor,
        color: config.color,
        borderRadius: '12px',
        fontWeight: 600,
        fontSize: getFontSize(size),
        textTransform: 'uppercase',
        letterSpacing: '0.5px',
        border: 'none',
        padding: size === 'lg' ? '8px 16px' : size === 'xl' ? '10px 20px' : undefined
      }}
    >
      {config.label}
    </Badge>
  );
};
```

#### Usage Examples

```tsx
// Small badge (default)
<SeverityBadge severity="Critical" />

// Large badge in header
<SeverityBadge severity="High" size="lg" />

// With test ID
<SeverityBadge
  severity="Medium"
  size="md"
  data-testid="incident-severity"
/>
```

#### Visual States

- **Default**: Solid background with configured color
- **Hover**: No hover effect (display-only component)
- **Focus**: Not focusable (display-only)

#### Accessibility

- ARIA label describes severity level and meaning
- High contrast ratios: Critical/High/Low (white on color), Medium (dark on gold)
- No interactive behavior (no keyboard navigation needed)

---

### 2. IncidentStatusBadge

**File**: `/apps/web/src/features/safety/components/IncidentStatusBadge.tsx`

**Pattern Source**: `VettingStatusBadge.tsx`

#### Props Interface

```typescript
interface IncidentStatusBadgeProps {
  status: IncidentStatus;  // NEW 5-stage enum
  size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
  'data-testid'?: string;
}

enum IncidentStatus {
  ReportSubmitted = 1,
  InformationGathering = 2,
  ReviewingFinalReport = 3,
  OnHold = 4,
  Closed = 5
}
```

#### Status Configuration

```typescript
const statusConfig = {
  ReportSubmitted: {
    backgroundColor: '#614B79',    // Plum
    color: 'white',
    label: 'Report Submitted',
    shortLabel: 'Submitted',
    ariaLabel: 'Report submitted, awaiting assignment'
  },
  InformationGathering: {
    backgroundColor: '#7B2CBF',    // Electric purple
    color: 'white',
    label: 'Information Gathering',
    shortLabel: 'Investigating',
    ariaLabel: 'Information gathering in progress'
  },
  ReviewingFinalReport: {
    backgroundColor: '#E6AC00',    // Dark amber
    color: 'white',
    label: 'Reviewing Final Report',
    shortLabel: 'Final Review',
    ariaLabel: 'Reviewing final report before closure'
  },
  OnHold: {
    backgroundColor: '#FFBF00',    // Bright amber
    color: '#1A1A2E',              // Dark text
    label: 'On Hold',
    shortLabel: 'On Hold',
    ariaLabel: 'Investigation on hold pending information'
  },
  Closed: {
    backgroundColor: '#4A5C3A',    // Forest green
    color: 'white',
    label: 'Closed',
    shortLabel: 'Closed',
    ariaLabel: 'Incident closed and archived'
  }
};
```

#### Implementation

```tsx
import React from 'react';
import { Badge } from '@mantine/core';

export const IncidentStatusBadge: React.FC<IncidentStatusBadgeProps> = ({
  status,
  size = 'sm',
  'data-testid': dataTestId
}) => {
  const config = statusConfig[status];

  const getFontSize = (size: string) => {
    switch (size) {
      case 'xs': return '10px';
      case 'sm': return '12px';
      case 'md': return '14px';
      case 'lg': return '16px';
      case 'xl': return '18px';
      default: return '12px';
    }
  };

  // Use short label for xs and sm sizes
  const displayLabel = (size === 'xs' || size === 'sm')
    ? config.shortLabel
    : config.label;

  return (
    <Badge
      size={size}
      data-testid={dataTestId}
      aria-label={config.ariaLabel}
      className={`status-${status.toLowerCase()}`}
      style={{
        backgroundColor: config.backgroundColor,
        color: config.color,
        borderRadius: '12px',
        fontWeight: 600,
        fontSize: getFontSize(size),
        textTransform: 'uppercase',
        letterSpacing: '0.5px',
        border: 'none',
        padding: size === 'lg' ? '8px 16px' : size === 'xl' ? '10px 20px' : undefined
      }}
    >
      {displayLabel}
    </Badge>
  );
};
```

#### Usage Examples

```tsx
// Small badge in table
<IncidentStatusBadge status="ReportSubmitted" size="sm" />

// Large badge in header
<IncidentStatusBadge status="InformationGathering" size="lg" />

// With test ID
<IncidentStatusBadge
  status="Closed"
  size="md"
  data-testid="incident-status"
/>
```

#### Visual States

- **Default**: Solid background with configured color
- **Responsive Labels**: Short labels on xs/sm, full labels on md+
- **Hover**: No hover effect (display-only)

#### Accessibility

- ARIA label describes current status and meaning
- High contrast ratios for all variants
- CSS class for targeting specific statuses

---

## Dashboard Components

### 3. IncidentFilters

**File**: `/apps/web/src/features/safety/components/IncidentFilters.tsx`

**Pattern Source**: VettingReviewGrid.tsx filter section

#### Props Interface

```typescript
interface IncidentFiltersProps {
  filters: IncidentFilterRequest;
  onFilterChange: (filters: IncidentFilterRequest) => void;
  onClearFilters: () => void;
  isLoading?: boolean;
}

interface IncidentFilterRequest {
  searchQuery?: string;
  statusFilters?: string[];
  severityFilters?: string[];
  assignedToFilters?: string[];
  dateRange?: 'last7days' | 'last30days' | 'last90days' | 'all';
  sortBy?: 'UpdatedAt' | 'ReportedAt' | 'Severity' | 'Status';
  sortDirection?: 'Asc' | 'Desc';
  page: number;
  pageSize: number;
}
```

#### Implementation

```tsx
import React, { useMemo, useCallback } from 'react';
import {
  Paper,
  Stack,
  Group,
  TextInput,
  Select,
  Button,
  Badge,
  rem
} from '@mantine/core';
import {
  IconSearch,
  IconFilter,
  IconX
} from '@tabler/icons-react';

export const IncidentFilters: React.FC<IncidentFiltersProps> = ({
  filters,
  onFilterChange,
  onClearFilters,
  isLoading = false
}) => {
  // Status options
  const statusOptions = useMemo(() => [
    { value: '', label: 'All Statuses' },
    { value: 'ReportSubmitted', label: 'Report Submitted' },
    { value: 'InformationGathering', label: 'Information Gathering' },
    { value: 'ReviewingFinalReport', label: 'Reviewing Final Report' },
    { value: 'OnHold', label: 'On Hold' },
    { value: 'Closed', label: 'Closed' }
  ], []);

  // Severity options
  const severityOptions = useMemo(() => [
    { value: '', label: 'All Severities' },
    { value: 'Critical', label: 'Critical' },
    { value: 'High', label: 'High' },
    { value: 'Medium', label: 'Medium' },
    { value: 'Low', label: 'Low' }
  ], []);

  // Date range options
  const dateRangeOptions = useMemo(() => [
    { value: 'all', label: 'All Time' },
    { value: 'last7days', label: 'Last 7 Days' },
    { value: 'last30days', label: 'Last 30 Days' },
    { value: 'last90days', label: 'Last 90 Days' }
  ], []);

  // Calculate active filter count
  const activeFilterCount = useMemo(() => {
    let count = 0;
    if (filters.searchQuery) count++;
    if (filters.statusFilters && filters.statusFilters.length > 0) count++;
    if (filters.severityFilters && filters.severityFilters.length > 0) count++;
    if (filters.assignedToFilters && filters.assignedToFilters.length > 0) count++;
    if (filters.dateRange && filters.dateRange !== 'all') count++;
    return count;
  }, [filters]);

  // Search handler
  const handleSearchChange = useCallback((value: string) => {
    onFilterChange({
      ...filters,
      searchQuery: value,
      page: 1
    });
  }, [filters, onFilterChange]);

  // Status filter handler
  const handleStatusChange = useCallback((value: string | null) => {
    onFilterChange({
      ...filters,
      statusFilters: value && value !== '' ? [value] : [],
      page: 1
    });
  }, [filters, onFilterChange]);

  // Severity filter handler
  const handleSeverityChange = useCallback((value: string | null) => {
    onFilterChange({
      ...filters,
      severityFilters: value && value !== '' ? [value] : [],
      page: 1
    });
  }, [filters, onFilterChange]);

  // Date range handler
  const handleDateRangeChange = useCallback((value: string | null) => {
    onFilterChange({
      ...filters,
      dateRange: (value as any) || 'all',
      page: 1
    });
  }, [filters, onFilterChange]);

  return (
    <Paper p="md" radius="md" style={{ background: '#FAF6F2' }}>
      <Stack gap="md">
        {/* Primary Filters Row */}
        <Group gap="md" align="flex-end" wrap="wrap">
          {/* Search */}
          <TextInput
            placeholder="Search by reference number, location, or coordinator..."
            leftSection={<IconSearch size={16} />}
            value={filters.searchQuery || ''}
            onChange={(e) => handleSearchChange(e.currentTarget.value)}
            disabled={isLoading}
            style={{ flex: 1, minWidth: rem(300) }}
            styles={{
              input: {
                fontSize: '16px',
                height: '42px',
                borderRadius: '8px'
              }
            }}
          />

          {/* Status Filter */}
          <Select
            placeholder="Filter by status"
            data={statusOptions}
            value={filters.statusFilters?.[0] || ''}
            onChange={handleStatusChange}
            leftSection={<IconFilter size={16} />}
            disabled={isLoading}
            clearable
            style={{ minWidth: rem(200) }}
            styles={{
              input: {
                fontSize: '16px',
                height: '42px',
                borderRadius: '8px'
              }
            }}
          />

          {/* Severity Filter */}
          <Select
            placeholder="Filter by severity"
            data={severityOptions}
            value={filters.severityFilters?.[0] || ''}
            onChange={handleSeverityChange}
            leftSection={<IconFilter size={16} />}
            disabled={isLoading}
            clearable
            style={{ minWidth: rem(180) }}
            styles={{
              input: {
                fontSize: '16px',
                height: '42px',
                borderRadius: '8px'
              }
            }}
          />

          {/* Date Range */}
          <Select
            placeholder="Date range"
            data={dateRangeOptions}
            value={filters.dateRange || 'all'}
            onChange={handleDateRangeChange}
            disabled={isLoading}
            style={{ minWidth: rem(150) }}
            styles={{
              input: {
                fontSize: '16px',
                height: '42px',
                borderRadius: '8px'
              }
            }}
          />

          {/* Clear Filters */}
          {activeFilterCount > 0 && (
            <Button
              variant="subtle"
              color="gray"
              onClick={onClearFilters}
              leftSection={<IconX size={16} />}
              disabled={isLoading}
            >
              Clear Filters ({activeFilterCount})
            </Button>
          )}
        </Group>

        {/* Active Filter Badges */}
        {activeFilterCount > 0 && (
          <Group gap="xs">
            {filters.searchQuery && (
              <Badge
                color="burgundy"
                rightSection={
                  <IconX
                    size={12}
                    style={{ cursor: 'pointer' }}
                    onClick={() => handleSearchChange('')}
                  />
                }
              >
                Search: {filters.searchQuery}
              </Badge>
            )}
            {filters.statusFilters && filters.statusFilters.length > 0 && (
              <Badge
                color="burgundy"
                rightSection={
                  <IconX
                    size={12}
                    style={{ cursor: 'pointer' }}
                    onClick={() => handleStatusChange(null)}
                  />
                }
              >
                Status: {filters.statusFilters[0]}
              </Badge>
            )}
            {filters.severityFilters && filters.severityFilters.length > 0 && (
              <Badge
                color="burgundy"
                rightSection={
                  <IconX
                    size={12}
                    style={{ cursor: 'pointer' }}
                    onClick={() => handleSeverityChange(null)}
                  />
                }
              >
                Severity: {filters.severityFilters[0]}
              </Badge>
            )}
          </Group>
        )}
      </Stack>
    </Paper>
  );
};
```

#### Responsive Behavior

**Desktop (≥768px)**:
- Filters in single horizontal row
- All filters visible simultaneously
- Clear Filters button inline

**Mobile (<768px)**:
```tsx
<Stack gap="md">
  <TextInput fullWidth />
  <Group grow>
    <Select />
    <Select />
  </Group>
  <Select fullWidth />
  {activeFilterCount > 0 && <Button fullWidth />}
</Stack>
```

---

### 4. IncidentTable

**File**: `/apps/web/src/features/safety/components/IncidentTable.tsx`

**Pattern Source**: VettingReviewGrid.tsx table section

#### Props Interface

```typescript
interface IncidentTableProps {
  incidents: SafetyIncident[];
  isLoading?: boolean;
  onRowClick?: (incidentId: string) => void;
  onAssign?: (incidentId: string) => void;
  onPutOnHold?: (incidentId: string) => void;
  onClose?: (incidentId: string) => void;
}
```

#### Implementation

```tsx
import React, { useCallback } from 'react';
import {
  Table,
  Text,
  Group,
  ActionIcon,
  Menu,
  Skeleton
} from '@mantine/core';
import {
  IconDots,
  IconUserPlus,
  IconEye,
  IconClock,
  IconCheck
} from '@tabler/icons-react';
import { SeverityBadge } from './SeverityBadge';
import { IncidentStatusBadge } from './IncidentStatusBadge';
import { formatRelativeTime } from '../utils/dateUtils';

export const IncidentTable: React.FC<IncidentTableProps> = ({
  incidents,
  isLoading = false,
  onRowClick,
  onAssign,
  onPutOnHold,
  onClose
}) => {
  // Calculate days since update for aging indicator
  const getDaysOld = useCallback((updatedAt: string) => {
    const days = Math.floor(
      (new Date().getTime() - new Date(updatedAt).getTime()) / (1000 * 60 * 60 * 24)
    );
    return days;
  }, []);

  // Get row background based on age
  const getRowBackground = useCallback((daysOld: number) => {
    if (daysOld > 7) return 'rgba(220, 53, 69, 0.05)';  // Red tint
    if (daysOld > 3) return 'rgba(255, 193, 7, 0.05)';  // Yellow tint
    return 'transparent';
  }, []);

  if (isLoading) {
    return (
      <>
        {Array.from({ length: 5 }).map((_, i) => (
          <Skeleton key={i} height={60} mb="sm" />
        ))}
      </>
    );
  }

  return (
    <Table striped highlightOnHover>
      {/* Header */}
      <Table.Thead style={{ backgroundColor: '#880124' }}>
        <Table.Tr>
          <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
            <Text fw={600} size="sm" c="white" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
              Reference
            </Text>
          </Table.Th>
          <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
            <Text fw={600} size="sm" c="white" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
              Severity
            </Text>
          </Table.Th>
          <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
            <Text fw={600} size="sm" c="white" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
              Status
            </Text>
          </Table.Th>
          <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
            <Text fw={600} size="sm" c="white" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
              Assigned To
            </Text>
          </Table.Th>
          <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
            <Text fw={600} size="sm" c="white" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
              Last Updated
            </Text>
          </Table.Th>
          <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none', width: 100 }}>
            <Text fw={600} size="sm" c="white" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
              Actions
            </Text>
          </Table.Th>
        </Table.Tr>
      </Table.Thead>

      {/* Body */}
      <Table.Tbody>
        {incidents.map((incident) => {
          const daysOld = getDaysOld(incident.updatedAt);
          const rowBg = getRowBackground(daysOld);

          return (
            <Table.Tr
              key={incident.id}
              onClick={() => onRowClick?.(incident.id!)}
              style={{
                cursor: 'pointer',
                backgroundColor: rowBg
              }}
            >
              {/* Reference Number */}
              <Table.Td>
                <Text size="sm" fw={600} style={{ color: '#880124' }}>
                  {incident.referenceNumber}
                </Text>
              </Table.Td>

              {/* Severity Badge */}
              <Table.Td>
                <SeverityBadge severity={incident.severity} size="sm" />
              </Table.Td>

              {/* Status Badge */}
              <Table.Td>
                <IncidentStatusBadge status={incident.status} size="sm" />
              </Table.Td>

              {/* Assigned To */}
              <Table.Td>
                <Text size="sm" style={{ color: '#2B2B2B' }}>
                  {incident.assignedUser?.sceneName || (
                    <Text c="dimmed" fw={600}>Unassigned</Text>
                  )}
                </Text>
              </Table.Td>

              {/* Last Updated */}
              <Table.Td>
                <Text
                  size="sm"
                  style={{
                    color: daysOld > 7 ? '#AA0130' : daysOld > 3 ? '#E6AC00' : '#2B2B2B'
                  }}
                >
                  {formatRelativeTime(incident.updatedAt)}
                </Text>
              </Table.Td>

              {/* Actions Menu */}
              <Table.Td onClick={(e) => e.stopPropagation()}>
                <Menu shadow="md" width={200}>
                  <Menu.Target>
                    <ActionIcon variant="subtle" color="gray">
                      <IconDots size={16} />
                    </ActionIcon>
                  </Menu.Target>

                  <Menu.Dropdown>
                    <Menu.Label>Quick Actions</Menu.Label>
                    <Menu.Item
                      leftSection={<IconUserPlus size={14} />}
                      onClick={() => onAssign?.(incident.id!)}
                    >
                      {incident.assignedTo ? 'Reassign' : 'Assign'} Coordinator
                    </Menu.Item>
                    <Menu.Item
                      leftSection={<IconEye size={14} />}
                      onClick={() => onRowClick?.(incident.id!)}
                    >
                      View Details
                    </Menu.Item>
                    <Menu.Divider />
                    <Menu.Item
                      leftSection={<IconClock size={14} />}
                      onClick={() => onPutOnHold?.(incident.id!)}
                    >
                      Put On Hold
                    </Menu.Item>
                    <Menu.Item
                      leftSection={<IconCheck size={14} />}
                      onClick={() => onClose?.(incident.id!)}
                      color="green"
                    >
                      Close Incident
                    </Menu.Item>
                  </Menu.Dropdown>
                </Menu>
              </Table.Td>
            </Table.Tr>
          );
        })}
      </Table.Tbody>
    </Table>
  );
};
```

#### Aging Indicators

| Age | Background Color | Text Color | Visual Cue |
|-----|-----------------|------------|-----------|
| 0-3 days | Transparent | Default (#2B2B2B) | Normal |
| 4-7 days | Yellow tint (rgba(255, 193, 7, 0.05)) | Amber (#E6AC00) | Warning |
| 8+ days | Red tint (rgba(220, 53, 69, 0.05)) | Red (#AA0130) | Alert |

---

## Notes Components

### 5. IncidentNotesList

**File**: `/apps/web/src/features/safety/components/IncidentNotesList.tsx`

**Pattern Source**: VettingApplicationDetail.tsx lines 501-579

#### Props Interface

```typescript
interface IncidentNotesListProps {
  incidentId: string;
  notes: IncidentNote[];
  onAddNote: (content: string) => Promise<void>;
  isAddingNote?: boolean;
  canAddNotes?: boolean;
}

interface IncidentNote {
  id: string;
  incidentId: string;
  authorId: string;
  authorName: string;
  content: string;
  isSystemGenerated: boolean;
  createdAt: string;
  updatedAt: string | null;
  editedAt: string | null;
}
```

#### Implementation

```tsx
import React, { useState } from 'react';
import {
  Card,
  Stack,
  Paper,
  Group,
  Text,
  Textarea,
  Badge,
  Title
} from '@mantine/core';
import { IconNotes } from '@tabler/icons-react';
import { formatTime } from '../utils/dateUtils';

export const IncidentNotesList: React.FC<IncidentNotesListProps> = ({
  incidentId,
  notes,
  onAddNote,
  isAddingNote = false,
  canAddNotes = true
}) => {
  const [newNote, setNewNote] = useState('');

  const handleSaveNote = async () => {
    if (!newNote.trim()) return;

    await onAddNote(newNote.trim());
    setNewNote('');
  };

  return (
    <Card p="xl">
      <Group justify="space-between" align="center" mb="md">
        <Title order={3} style={{ color: '#880124' }}>
          Notes
        </Title>
        {canAddNotes && (
          <button
            className={newNote.trim() ? "btn btn-primary" : "btn"}
            onClick={handleSaveNote}
            disabled={!newNote.trim() || isAddingNote}
            data-testid="save-note-button"
            type="button"
          >
            {isAddingNote ? 'Saving...' : 'Save Note'}
          </button>
        )}
      </Group>

      <Stack gap="md">
        {/* Add Note Text Area */}
        {canAddNotes && (
          <Textarea
            placeholder="Add a note about this incident..."
            value={newNote}
            onChange={(e) => setNewNote(e.currentTarget.value)}
            minRows={4}
            disabled={isAddingNote}
            data-testid="add-note-textarea"
            styles={{
              input: {
                borderRadius: '8px',
                border: '1px solid #E0E0E0',
              }
            }}
          />
        )}

        {/* Notes List */}
        {notes.length > 0 ? (
          <Stack gap="sm">
            {notes.map((note) => {
              const isSystem = note.isSystemGenerated;

              return (
                <Paper
                  key={note.id}
                  p="md"
                  style={{
                    background: isSystem ? '#F0EDFF' : '#F5F5F5',
                    borderRadius: '8px',
                    borderLeft: isSystem ? '4px solid #7B2CBF' : 'none'
                  }}
                  data-testid={isSystem ? "system-note" : "manual-note"}
                >
                  <Group justify="space-between" mb="xs">
                    <Group gap="xs">
                      {/* System badge or note icon */}
                      {isSystem ? (
                        <Badge color="purple" size="sm">SYSTEM</Badge>
                      ) : (
                        <IconNotes size={16} style={{ color: '#880124' }} />
                      )}
                      <Text fw={600} size="sm">{note.authorName}</Text>
                    </Group>
                    <Text size="sm" c="dimmed">
                      {formatTime(note.createdAt)}
                      {note.editedAt && <> • Edited</>}
                    </Text>
                  </Group>
                  <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
                    {note.content}
                  </Text>
                </Paper>
              );
            })}
          </Stack>
        ) : (
          <Text c="dimmed" size="sm" ta="center" py="md">
            No notes added yet
          </Text>
        )}
      </Stack>
    </Card>
  );
};
```

#### Note Type Styling

**System-Generated Notes**:
- Background: Light purple (#F0EDFF)
- Left border: 4px solid electric purple (#7B2CBF)
- Badge: Purple with "SYSTEM" label
- Icon: None (badge replaces icon)
- Cannot be edited or deleted

**Manual Notes**:
- Background: Light gray (#F5F5F5)
- Left border: None
- Badge: None
- Icon: IconNotes (burgundy #880124)
- Editable within 15 minutes (shows "Edited" indicator)

#### System Note Examples

```
"Status changed from Report Submitted to Information Gathering"
"Assigned to JaneRigger by AdminUser"
"HOLD: Awaiting police report - Expected resume: 10/31/2025"
"CLOSED: Mediation completed, no further action required"
```

---

## Modal Components

### 6. StageGuidanceModal (Base Component)

**File**: `/apps/web/src/features/safety/components/modals/StageGuidanceModal.tsx`

#### Props Interface

```typescript
interface StageGuidanceModalProps {
  opened: boolean;
  onClose: () => void;
  onConfirm: (data: StageTransitionData) => Promise<void>;
  variant: 'information-gathering' | 'final-review' | 'on-hold' | 'resume' | 'close';
  currentStatus?: IncidentStatus;
  incidentReference?: string;
}

interface StageTransitionData {
  note?: string;
  googleDriveLink?: string;
  holdReason?: string;
  resumeToStatus?: IncidentStatus;
  finalSummary?: string;
}
```

#### Modal Configurations

```typescript
const modalConfigs = {
  'information-gathering': {
    title: 'Moving to Information Gathering',
    guidanceText: 'The coordinator will now begin gathering additional information about this incident. Initial review should be complete before proceeding.',
    checklist: [
      'Coordinator has been assigned',
      'Initial review of incident details complete',
      'Google Drive folder created (manual link below)'
    ],
    showGoogleDriveInput: true,
    showNoteField: true,
    confirmButtonText: 'Begin Information Gathering',
    confirmButtonColor: 'purple'
  },
  'final-review': {
    title: 'Moving to Reviewing Final Report',
    guidanceText: 'The investigation is complete and the coordinator is preparing the final report. All information gathering should be finished before this stage.',
    checklist: [
      'Investigation complete',
      'All relevant parties contacted',
      'Draft resolution prepared in Google Drive'
    ],
    showGoogleDriveInput: true,
    showNoteField: true,
    confirmButtonText: 'Move to Final Review',
    confirmButtonColor: 'purple'
  },
  'on-hold': {
    title: 'Putting Report On Hold',
    guidanceText: 'This incident will remain on hold until additional information is available or external processes complete.',
    checklist: [
      'Document reason for hold in notes',
      'Notify relevant parties if necessary'
    ],
    showHoldReasonField: true,
    showNoteField: false,
    confirmButtonText: 'Put On Hold',
    confirmButtonColor: 'orange'
  },
  'resume': {
    title: 'Resuming Investigation',
    guidanceText: 'Investigation will resume. Review notes since hold to ensure all blockers are resolved.',
    checklist: [
      'Review notes since hold',
      'Verify all blockers resolved'
    ],
    showResumeToDropdown: true,
    showNoteField: true,
    confirmButtonText: 'Resume Investigation',
    confirmButtonColor: 'purple'
  },
  'close': {
    title: 'Closing Incident Report',
    guidanceText: 'Once closed, this incident will move to archived status. Ensure all documentation is complete before closing.',
    checklist: [
      'Final report documented in notes',
      'All relevant notes added',
      'Reporter notified (if identified)',
      'Google Drive upload complete (Phase 1 manual)'
    ],
    showFinalSummaryField: true,
    showGoogleDriveInput: true,
    showNoteField: false,
    confirmButtonText: 'Close Incident',
    confirmButtonColor: 'green'
  }
};
```

#### Base Implementation

```tsx
import React, { useState } from 'react';
import {
  Modal,
  Stack,
  Text,
  Textarea,
  TextInput,
  Select,
  Button,
  Group,
  Title,
  Checkbox,
  Alert
} from '@mantine/core';
import { IconInfoCircle } from '@tabler/icons-react';

export const StageGuidanceModal: React.FC<StageGuidanceModalProps> = ({
  opened,
  onClose,
  onConfirm,
  variant,
  currentStatus,
  incidentReference
}) => {
  const config = modalConfigs[variant];
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [note, setNote] = useState('');
  const [googleDriveLink, setGoogleDriveLink] = useState('');
  const [holdReason, setHoldReason] = useState('');
  const [resumeToStatus, setResumeToStatus] = useState<IncidentStatus | undefined>();
  const [finalSummary, setFinalSummary] = useState('');

  const handleConfirm = async () => {
    setIsSubmitting(true);
    try {
      await onConfirm({
        note: note.trim() || undefined,
        googleDriveLink: googleDriveLink.trim() || undefined,
        holdReason: holdReason.trim() || undefined,
        resumeToStatus,
        finalSummary: finalSummary.trim() || undefined
      });
      handleClose();
    } catch (error) {
      // Error handled by parent
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    if (!isSubmitting) {
      setNote('');
      setGoogleDriveLink('');
      setHoldReason('');
      setResumeToStatus(undefined);
      setFinalSummary('');
      onClose();
    }
  };

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Title order={3} style={{ color: '#880124' }}>
          {config.title}
        </Title>
      }
      centered
      size="md"
    >
      <Stack gap="md">
        {/* Guidance Text */}
        <Text size="sm">{config.guidanceText}</Text>

        {/* Checklist (NOT enforced - user can proceed without checking) */}
        <Stack gap="xs">
          <Text size="sm" fw={600}>Recommended Actions:</Text>
          {config.checklist.map((item, index) => (
            <Group gap="xs" key={index}>
              <Checkbox size="xs" />
              <Text size="sm">{item}</Text>
            </Group>
          ))}
        </Stack>

        {/* Alert: Checklist is not enforced */}
        <Alert color="blue" icon={<IconInfoCircle />}>
          <Text size="xs">
            These recommendations are guidance only. You may proceed without completing all items.
          </Text>
        </Alert>

        {/* Google Drive Link Field */}
        {config.showGoogleDriveInput && (
          <TextInput
            label="Google Drive Link (Optional)"
            placeholder="https://drive.google.com/..."
            value={googleDriveLink}
            onChange={(e) => setGoogleDriveLink(e.currentTarget.value)}
          />
        )}

        {/* Hold Reason Field */}
        {config.showHoldReasonField && (
          <Textarea
            label="Reason for Hold (Optional)"
            placeholder="Enter the reason for putting this incident on hold..."
            value={holdReason}
            onChange={(e) => setHoldReason(e.currentTarget.value)}
            minRows={3}
          />
        )}

        {/* Resume To Status Dropdown */}
        {config.showResumeToDropdown && (
          <Select
            label="Resume To Stage"
            placeholder="Select stage to resume to..."
            data={[
              { value: 'InformationGathering', label: 'Information Gathering' },
              { value: 'ReviewingFinalReport', label: 'Reviewing Final Report' }
            ]}
            value={resumeToStatus}
            onChange={(value) => setResumeToStatus(value as IncidentStatus)}
            required
          />
        )}

        {/* Final Summary Field */}
        {config.showFinalSummaryField && (
          <Textarea
            label="Final Summary (Required)"
            placeholder="Provide a final summary of the incident resolution..."
            value={finalSummary}
            onChange={(e) => setFinalSummary(e.currentTarget.value)}
            minRows={4}
            required
          />
        )}

        {/* Optional Note Field */}
        {config.showNoteField && (
          <Textarea
            label="Note (Optional)"
            placeholder="Add a note about this transition..."
            value={note}
            onChange={(e) => setNote(e.currentTarget.value)}
            minRows={3}
          />
        )}

        {/* Actions */}
        <Group justify="flex-end" gap="md" mt="md">
          <Button
            variant="light"
            onClick={handleClose}
            disabled={isSubmitting}
          >
            Cancel
          </Button>
          <Button
            color={config.confirmButtonColor}
            onClick={handleConfirm}
            loading={isSubmitting}
            disabled={
              (variant === 'resume' && !resumeToStatus) ||
              (variant === 'close' && !finalSummary.trim())
            }
          >
            {config.confirmButtonText}
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};
```

---

### 7. CoordinatorAssignmentModal

**File**: `/apps/web/src/features/safety/components/modals/CoordinatorAssignmentModal.tsx`

#### Props Interface

```typescript
interface CoordinatorAssignmentModalProps {
  opened: boolean;
  onClose: () => void;
  onAssign: (userId: string) => Promise<void>;
  incidentId: string;
  currentCoordinator?: {
    id: string;
    sceneName: string;
  };
  allUsers: UserForAssignment[];
}

interface UserForAssignment {
  id: string;
  sceneName: string;
  realName: string;
  role: string;
  activeIncidentCount: number;
}
```

#### Implementation

```tsx
import React, { useState, useMemo } from 'react';
import {
  Modal,
  Stack,
  Paper,
  Text,
  Select,
  Button,
  Group,
  Title,
  Alert
} from '@mantine/core';
import { IconUserPlus, IconInfoCircle } from '@tabler/icons-react';

export const CoordinatorAssignmentModal: React.FC<CoordinatorAssignmentModalProps> = ({
  opened,
  onClose,
  onAssign,
  incidentId,
  currentCoordinator,
  allUsers
}) => {
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Format users for dropdown
  const userOptions = useMemo(() => {
    return allUsers.map(user => ({
      value: user.id,
      label: `${user.sceneName} (${user.realName})`,
      description: `${user.activeIncidentCount || 0} active incidents`
    }));
  }, [allUsers]);

  // Selected user details
  const selectedUser = useMemo(() => {
    return allUsers.find(u => u.id === selectedUserId);
  }, [allUsers, selectedUserId]);

  const handleAssign = async () => {
    if (!selectedUserId) return;

    setIsSubmitting(true);
    try {
      await onAssign(selectedUserId);
      handleClose();
    } catch (error) {
      // Error handled by parent
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    if (!isSubmitting) {
      setSelectedUserId(null);
      onClose();
    }
  };

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Title order={3} style={{ color: '#880124' }}>
          Assign Incident Coordinator
        </Title>
      }
      centered
      size="md"
    >
      <Stack gap="md">
        {/* Current Coordinator Alert */}
        {currentCoordinator && (
          <Alert color="blue" icon={<IconInfoCircle />}>
            Current Coordinator: <strong>{currentCoordinator.sceneName}</strong>
          </Alert>
        )}

        {/* Instruction Text */}
        <Text size="sm">
          Select a user to coordinate this incident. Any user can be assigned as coordinator,
          not just administrators.
        </Text>

        {/* User Search/Select */}
        <Select
          label="Coordinator"
          placeholder="Search by scene name or real name..."
          data={userOptions}
          value={selectedUserId}
          onChange={setSelectedUserId}
          searchable
          required
          leftSection={<IconUserPlus size={16} />}
          styles={{
            input: {
              fontSize: '16px',
              height: '42px',
              borderRadius: '8px'
            }
          }}
        />

        {/* Selected User Details */}
        {selectedUser && (
          <Paper p="md" style={{ background: '#F5F5F5', borderRadius: '8px' }}>
            <Stack gap="xs">
              <Text fw={600}>{selectedUser.sceneName}</Text>
              <Text size="sm" c="dimmed">Real Name: {selectedUser.realName}</Text>
              <Text size="sm" c="dimmed">
                Current Active Incidents: {selectedUser.activeIncidentCount || 0}
              </Text>
              <Text size="sm" c="dimmed">
                Role: {selectedUser.role}
              </Text>
            </Stack>
          </Paper>
        )}

        {/* Guidance Alert */}
        <Alert color="purple" icon={<IconInfoCircle />}>
          <Text size="sm">
            This user will be responsible for managing this incident through resolution.
            They will receive email notification upon assignment.
          </Text>
        </Alert>

        {/* Actions */}
        <Group justify="flex-end" gap="md" mt="md">
          <Button
            variant="light"
            onClick={handleClose}
            disabled={isSubmitting}
          >
            Cancel
          </Button>
          <Button
            color="purple"
            onClick={handleAssign}
            loading={isSubmitting}
            disabled={!selectedUserId}
          >
            Assign Coordinator
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};
```

---

## Summary

### Components Created
1. ✅ SeverityBadge (4 variants)
2. ✅ IncidentStatusBadge (5 variants)
3. ✅ IncidentFilters
4. ✅ IncidentTable
5. ✅ IncidentNotesList
6. ✅ StageGuidanceModal (5 modal variants)
7. ✅ CoordinatorAssignmentModal

### Mantine v7 Usage
- Table component with custom styling
- Badge component for status/severity
- Modal component for all dialogs
- TextInput, Textarea, Select for forms
- Paper, Card for containers
- Stack, Group for layout
- ActionIcon, Menu for actions
- Alert for guidance/warnings

### Design System Compliance
- ✅ Color palette from Design System v7
- ✅ Typography hierarchy from Design System v7
- ✅ Spacing system from Design System v7
- ✅ Accessibility standards (ARIA, keyboard nav)
- ✅ Responsive design patterns
- ✅ Mantine theming integration

---

**Created**: 2025-10-17
**Author**: UI Designer Agent
**Version**: 1.0
**Status**: Draft - Awaiting Approval
