# VettingReviewGrid Component

## Overview

The `VettingReviewGrid` is the main admin interface for reviewing and managing vetting applications. It provides a comprehensive data grid with filtering, sorting, search, and quick actions.

## Location

`/apps/web/src/features/admin/vetting/components/VettingReviewGrid.tsx`

## Features

### Data Display
- **Table Layout**: Clean, striped table with hover effects
- **Columns**: Scene Name, Email, Status, Submitted Date, Actions
- **Status Badges**: Color-coded status indicators using `VettingStatusBadge`
- **Pagination**: 25 items per page with navigation controls
- **Responsive**: Adapts to different screen sizes

### Filtering & Search
- **Search**: Real-time search by scene name or email (debounced)
- **Status Filter**: Dropdown to filter by application status
- **Clear Filters**: Button to reset all filters (shows count of active filters)
- **URL State**: Filter state preserved in URL query params (future enhancement)

### Sorting
- **Default Sort**: Submitted date (newest first)
- **Sortable Columns**: Status, Submitted Date
- **Visual Indicators**: Arrow icons show current sort direction

### Quick Actions
Each row has an actions menu with:
- **Approve**: Approve the application
- **Deny**: Deny the application
- **Put On Hold**: Place application on hold
- **Schedule Interview**: Schedule an interview
- **Send Reminder**: Send a reminder email

> **Note**: Quick action handlers are currently placeholders. Modals need to be implemented.

### Loading & Error States
- **Initial Load**: Skeleton loading for table rows
- **Error State**: Alert with retry button
- **Empty States**:
  - No applications yet
  - No results from filters

## Usage

### Basic Usage

```tsx
import { VettingReviewGrid } from '../components/VettingReviewGrid';

export const VettingPage = () => {
  return <VettingReviewGrid />;
};
```

### With Custom Handlers

```tsx
import { VettingReviewGrid } from '../components/VettingReviewGrid';

export const VettingPage = () => {
  const handleViewDetails = (applicationId: string) => {
    // Custom navigation or modal logic
    console.log('View details:', applicationId);
  };

  const handleActionComplete = () => {
    // Refresh data or show notification
    console.log('Action completed');
  };

  return (
    <VettingReviewGrid
      onViewDetails={handleViewDetails}
      onActionComplete={handleActionComplete}
    />
  );
};
```

## Props

| Prop | Type | Required | Description |
|------|------|----------|-------------|
| `onViewDetails` | `(applicationId: string) => void` | No | Custom handler for row clicks. If not provided, navigates to detail page. |
| `onActionComplete` | `() => void` | No | Callback after quick actions complete. |

## Data Flow

1. **Component mounts** → Fetches applications with default filters
2. **User changes filters** → Resets to page 1, refetches data
3. **User clicks row** → Navigates to detail page or calls `onViewDetails`
4. **User clicks action** → Calls action handler (TODO: open modal)

## Dependencies

### React Query
- Uses `useVettingApplications` hook for data fetching
- Automatic caching and refetching
- Error handling and retry logic

### Mantine Components
- `Table`: Main data grid
- `TextInput`: Search field
- `Select`: Status filter
- `Button`: Action buttons
- `Paper`: Container styling
- `Badge`: Status indicators (via `VettingStatusBadge`)
- `Menu`: Quick actions dropdown
- `Skeleton`: Loading states
- `Alert`: Error states
- `Pagination`: Page navigation

### Icons (Tabler Icons)
- `IconSearch`: Search input
- `IconFilter`: Status filter
- `IconX`: Clear filters
- `IconRefresh`: Retry button
- `IconDots`: Actions menu
- `IconCheck`, `IconCancel`, `IconClock`, `IconCalendar`, `IconMail`: Menu items

## TypeScript Types

### Filter Request
```typescript
interface ApplicationFilterRequest {
  page: number;
  pageSize: number;
  statusFilters: string[];
  searchQuery?: string;
  sortBy: string;
  sortDirection: string;
}
```

### Application Summary
```typescript
interface ApplicationSummaryDto {
  id: string;
  applicationNumber: string;
  status: string;
  submittedAt: string;
  sceneName: string;
  // ... additional fields
}
```

## Styling

### Design System Colors
- **Primary Burgundy**: `#880124` (header background, pagination active)
- **Ivory Background**: `#FFF8F0` (filter section)
- **Charcoal Text**: `#2B2B2B` (table text)

### Custom Styles
- **Header**: White text on burgundy background with uppercase letters
- **Rows**: Striped with hover highlight
- **Filters**: Ivory background with 16px inputs
- **Pagination**: Burgundy active state

## Performance

### Optimizations
- **Memoized Callbacks**: All event handlers use `useCallback`
- **Memoized Values**: Status options and filter counts use `useMemo`
- **Debounced Search**: 300ms delay (handled by React Query)
- **Efficient Renders**: Only re-renders when data or filters change

### React Query Configuration
- **Stale Time**: 5 minutes
- **Refetch on Window Focus**: Disabled
- **Placeholder Data**: Previous data shown during refetch

## Accessibility

### Keyboard Navigation
- Tab through filters, table rows, and pagination
- Enter key on row opens detail page
- Escape key closes action menus

### Screen Readers
- Semantic table structure
- ARIA labels on interactive elements
- Status badges with readable text

### WCAG Compliance
- Color contrast ratios meet AA standards
- Focus indicators on all interactive elements
- Clear error messaging

## Testing Checklist

- [ ] Grid loads and displays data
- [ ] Status badges show correct colors
- [ ] Sorting works for columns
- [ ] Filtering by status works
- [ ] Search filters by scene name and email
- [ ] Pagination works correctly
- [ ] Actions menu opens and closes
- [ ] Loading states show during data fetch
- [ ] Error states display properly
- [ ] Empty states show appropriate messages
- [ ] Row click navigates to detail page
- [ ] Quick actions log to console (until modals implemented)

## Future Enhancements

### TODO: Implement Quick Action Modals
```typescript
// Example: Approve Modal
const handleApprove = useCallback((applicationId: string) => {
  openApprovalModal({
    applicationId,
    onConfirm: async (reasoning: string) => {
      await vettingAdminApi.approveApplication(applicationId, reasoning);
      refetch();
      onActionComplete?.();
    }
  });
}, [refetch, onActionComplete]);
```

### TODO: Add Bulk Operations
- Select multiple applications with checkboxes
- Bulk status changes
- Bulk reminder sending

### TODO: Add More Filters
- Date range picker
- Priority filter
- Experience level filter
- Skills/interests filter

### TODO: Export Functionality
- Export to CSV
- Export to PDF
- Print view

## Related Components

- **VettingStatusBadge**: Status indicator component
- **VettingApplicationDetail**: Detail page component
- **VettingApplicationsList**: Legacy list component (can be deprecated)

## API Integration

### Endpoint
```
POST /api/vetting/reviewer/applications
```

### Request
```json
{
  "page": 1,
  "pageSize": 25,
  "statusFilters": ["UnderReview"],
  "searchQuery": "john",
  "sortBy": "SubmittedAt",
  "sortDirection": "Desc"
}
```

### Response
```json
{
  "success": true,
  "data": {
    "items": [...],
    "totalCount": 156,
    "page": 1,
    "pageSize": 25,
    "totalPages": 7,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

## Migration Notes

### From VettingApplicationsList
The `VettingReviewGrid` is a new component that:
- ✅ Uses simpler Mantine Table (no external datatable library)
- ✅ Follows exact wireframe specifications
- ✅ Has cleaner, more maintainable code
- ✅ Better TypeScript type safety
- ✅ Improved accessibility
- ✅ More consistent with design system

### Breaking Changes
None - this is a new component. `VettingApplicationsList` can coexist or be deprecated.

## Support

For issues or questions about this component:
1. Check the [UI/UX Specification](/docs/functional-areas/vetting-system/new-work/2025-10-04-complete-vetting-workflow/design/ui-ux-specification.md)
2. Review [React Patterns](/docs/standards-processes/development-standards/react-patterns.md)
3. Consult the [React Developer Lessons Learned](/docs/lessons-learned/react-developer-lessons-learned.md)
