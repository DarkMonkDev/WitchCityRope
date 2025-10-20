# Incident Dashboard Redesign - Card-Based Grid Pattern
<!-- Last Updated: 2025-10-18 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Design Proposal -->

## Design Overview

**Objective**: Redesign the Admin Incident Dashboard to match the vetting applications page pattern EXACTLY.

**Current Implementation**: Table-based display with statistics row
**Target Pattern**: Card-based grid view (similar to vetting applications)

## Key Changes Required

### 1. REMOVE - Statistics Cards Row
❌ **REMOVE ENTIRELY** - Lines 184-238 in current implementation
- No "Unassigned Count" card
- No "In Progress" card
- No "Closed Count" card
- Statistics row does not appear in vetting page pattern

### 2. REMOVE - Recent Incidents Section
❌ **REMOVE ENTIRELY** - Lines 241-300 in current implementation
- "Recent Incidents" card with last 5 items
- This duplicates the main grid content below
- Not present in vetting page pattern

### 3. REMOVE - Time-Based Filter
❌ **REMOVE from IncidentFilters** - Lines 150-165
- Date range filter ("Last 7 Days", "Last 30 Days", etc.)
- Not present in vetting page filters
- Keep only: Search, Status, Clear Filters

### 4. TRANSFORM - Table to Card Grid
✅ **CHANGE** - Lines 311-318 in current implementation
- Replace `IncidentTable` component with card-based grid
- Use Mantine `SimpleGrid` component
- Display incidents as individual cards in responsive grid

## Vetting Pattern Analysis

Based on vetting applications page pattern (typical admin review page):

### Layout Structure
```
+--------------------------------------------------+
|  Page Title                                       |
+--------------------------------------------------+
|  Search & Filters (Horizontal)                    |
|  [Search] [Status Filter] [Clear (N)]             |
+--------------------------------------------------+
|  Card Grid (Responsive)                           |
|  +------------+  +------------+  +------------+   |
|  | Card 1     |  | Card 2     |  | Card 3     |   |
|  +------------+  +------------+  +------------+   |
|  +------------+  +------------+  +------------+   |
|  | Card 4     |  | Card 5     |  | Card 6     |   |
|  +------------+  +------------+  +------------+   |
+--------------------------------------------------+
|  Pagination                                       |
+--------------------------------------------------+
```

### Card Design Pattern
Each incident displays as a card with:
- **Reference number** (top-left, burgundy accent)
- **Location** (gray subtitle)
- **Status badge** (color-coded)
- **Reported date** (gray text)
- **Assigned coordinator** (or "Unassigned" badge)
- **Hover state** (subtle elevation)
- **Click handler** (entire card clickable)

## Component Specifications

### 1. Page Header
```tsx
<Container size="xl" py="xl">
  <Stack gap="lg">
    <Title order={1}>Incident Dashboard</Title>
```

**Mantine Components**:
- `Container` with `size="xl"` for max-width constraint
- `Stack` with `gap="lg"` for vertical spacing
- `Title` with `order={1}` for h1 semantic

### 2. Filters Section
```tsx
<IncidentFilters
  filters={filters}
  onFilterChange={handleFilterChange}
  onClearFilters={handleClearFilters}
  isLoading={isLoadingIncidents}
/>
```

**Filter Options** (simplified to match vetting):
- Search input (reference number, location, coordinator)
- Status dropdown (single select)
- Clear filters button with count

**Remove**:
- Date range filter
- Assignment filter (redundant with search)

### 3. Incident Card Grid
```tsx
<SimpleGrid
  cols={{ base: 1, sm: 2, lg: 3 }}
  spacing="md"
  data-testid="incidents-grid"
>
  {incidents.map((incident) => (
    <IncidentCard
      key={incident.id}
      incident={incident}
      onClick={() => handleIncidentClick(incident.id)}
    />
  ))}
</SimpleGrid>
```

**Mantine Components**:
- `SimpleGrid` for responsive column layout
- Responsive breakpoints: 1 column (mobile), 2 columns (tablet), 3 columns (desktop)
- `spacing="md"` for consistent gap

### 4. Individual Incident Card
```tsx
<Card
  shadow="sm"
  padding="lg"
  radius="md"
  withBorder
  style={{
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    background: '#FAF6F2'
  }}
  styles={{
    root: {
      '&:hover': {
        transform: 'translateY(-2px)',
        boxShadow: '0 8px 16px rgba(0,0,0,0.08)'
      }
    }
  }}
  onClick={() => onClick(incident.id)}
>
  <Stack gap="sm">
    {/* Reference Number */}
    <Group justify="space-between" align="flex-start">
      <Text size="lg" fw={600} c="wcr.7">
        {incident.referenceNumber}
      </Text>
      <Badge color={getStatusColor(incident.status)} size="sm">
        {formatStatus(incident.status)}
      </Badge>
    </Group>

    {/* Location */}
    <Text size="sm" c="dimmed">
      {incident.location}
    </Text>

    {/* Metadata Row */}
    <Group justify="space-between" mt="xs">
      <Text size="xs" c="dimmed">
        {formatDate(incident.reportedAt)}
      </Text>
      {incident.assignedUserName ? (
        <Text size="xs" fw={500}>
          {incident.assignedUserName}
        </Text>
      ) : (
        <Badge color="gray" size="xs">
          Unassigned
        </Badge>
      )}
    </Group>
  </Stack>
</Card>
```

**Card Styling**:
- `shadow="sm"` for subtle elevation
- `withBorder` for definition
- `background: '#FAF6F2'` (cream, Design System v7)
- Hover animation: translateY(-2px) with enhanced shadow
- Click target: entire card (44px minimum height for accessibility)

### 5. Empty State
```tsx
{incidents.length === 0 && !isLoading && (
  <Box ta="center" py="xl">
    <IconFileAlert size={64} color="#B76D75" />
    <Text size="lg" fw={600} mt="md" c="dimmed">
      No incidents found
    </Text>
    <Text size="sm" c="dimmed">
      {hasActiveFilters
        ? 'Try adjusting your filters or search query.'
        : 'No incidents have been reported yet.'
      }
    </Text>
    {hasActiveFilters && (
      <Button
        variant="outline"
        color="wcr"
        mt="md"
        onClick={handleClearFilters}
      >
        Clear All Filters
      </Button>
    )}
  </Box>
)}
```

**Empty State Components**:
- Icon placeholder (64px, rose gold color)
- Primary message (large, bold)
- Secondary message (contextual help)
- Clear filters action (if filters active)

### 6. Pagination
```tsx
<Group justify="space-between" mt="lg">
  <Text size="sm" c="dimmed">
    Showing {incidents.length} of {total} incidents
  </Text>
  <Group gap="xs">
    <Button
      variant="subtle"
      size="sm"
      disabled={page === 1}
      onClick={() => handlePageChange(page - 1)}
    >
      Previous
    </Button>
    <Text size="sm" c="dimmed">
      Page {page} of {totalPages}
    </Text>
    <Button
      variant="subtle"
      size="sm"
      disabled={page >= totalPages}
      onClick={() => handlePageChange(page + 1)}
    >
      Next
    </Button>
  </Group>
</Group>
```

## Status Color Mapping

```typescript
const getStatusColor = (status: string): string => {
  const statusMap: Record<string, string> = {
    'ReportSubmitted': 'blue',      // New report
    'InformationGathering': 'yellow', // In progress
    'ReviewingFinalReport': 'orange', // Review stage
    'OnHold': 'gray',                // Paused
    'Closed': 'green'                // Resolved
  };
  return statusMap[status] || 'gray';
};

const formatStatus = (status: string): string => {
  return status
    .replace(/([A-Z])/g, ' $1')
    .trim();
};
```

## Responsive Breakpoints

Following Mantine v7 standards and Design System v7:

| Breakpoint | Columns | Container | Padding |
|------------|---------|-----------|---------|
| xs (0-575px) | 1 | Full width | 20px |
| sm (576-767px) | 2 | 720px | 20px |
| md (768-991px) | 2 | 960px | 40px |
| lg (992-1199px) | 3 | 1140px | 40px |
| xl (1200px+) | 3 | 1200px | 40px |

## Accessibility Requirements

### WCAG 2.1 AA Compliance
- ✅ All cards have minimum 44x44px click target
- ✅ Color contrast ratio 4.5:1 for all text
- ✅ Focus indicators on keyboard navigation
- ✅ Screen reader labels for status badges
- ✅ Semantic HTML structure (Card as article, Title as h2)

### Keyboard Navigation
- Tab: Navigate between cards
- Enter/Space: Open card details
- Arrow keys: Navigate grid (optional enhancement)

### Screen Reader Support
```tsx
<Card
  role="article"
  aria-label={`Incident ${incident.referenceNumber} at ${incident.location}`}
  tabIndex={0}
  onKeyDown={(e) => {
    if (e.key === 'Enter' || e.key === ' ') {
      onClick(incident.id);
    }
  }}
>
```

## Loading States

### Skeleton Cards
```tsx
{isLoading && (
  <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }} spacing="md">
    {Array.from({ length: 6 }).map((_, index) => (
      <Card key={index} padding="lg" radius="md">
        <Stack gap="sm">
          <Skeleton height={24} width="60%" />
          <Skeleton height={16} width="80%" />
          <Skeleton height={16} width="40%" />
        </Stack>
      </Card>
    ))}
  </SimpleGrid>
)}
```

## Implementation Checklist

- [ ] Remove statistics cards row (lines 184-238)
- [ ] Remove recent incidents section (lines 241-300)
- [ ] Remove date range filter from IncidentFilters component
- [ ] Create new `IncidentCard.tsx` component
- [ ] Replace `IncidentTable` with `SimpleGrid` + `IncidentCard`
- [ ] Update filter state to remove `dateRange` property
- [ ] Add status color mapping utility function
- [ ] Implement card hover animations (Design System v7)
- [ ] Add empty state with contextual messaging
- [ ] Test responsive behavior (1, 2, 3 column layouts)
- [ ] Verify WCAG 2.1 AA compliance
- [ ] Test keyboard navigation
- [ ] Test screen reader announcements

## Design System v7 Integration

### Colors Used
- Background: `#FAF6F2` (cream)
- Text Primary: `#2B2B2B` (charcoal)
- Text Dimmed: `#8B8680` (stone)
- Accent: `#880124` (burgundy) - reference numbers
- Hover: Elevated shadow with `-2px` translateY

### Typography
- Card title (reference number): 18px, 600 weight, burgundy
- Location: 14px, dimmed color
- Date/metadata: 12px, dimmed color
- Status badge: 12px, uppercase

### Animations
- Card hover: `transform: translateY(-2px)` with `box-shadow` enhancement
- Transition: `all 0.2s ease`
- NO corner morphing (reserved for buttons only)

## Testing Strategy

### Visual Regression Tests
- Screenshot comparison with vetting applications page
- Verify identical filter layout
- Verify identical card grid structure
- Verify identical responsive behavior

### Functional Tests
- Filter interactions (search, status, clear)
- Card click navigation
- Pagination controls
- Empty state display
- Loading state display

### Accessibility Tests
- Keyboard navigation flow
- Screen reader announcements
- Focus indicators visibility
- Color contrast validation

## Success Criteria

✅ Page matches vetting applications layout EXACTLY
✅ No statistics row at top
✅ No recent incidents section
✅ No date range filter
✅ Card-based grid (NOT table)
✅ Responsive 1/2/3 column layout
✅ Consistent with Design System v7
✅ WCAG 2.1 AA compliant
✅ All interactions smooth and intuitive

---

**Next Steps**: Implement redesigned component and create side-by-side comparison screenshots.
