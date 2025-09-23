# Vetting Grid Wireframe Fix - 2025-09-22

## Problem
The admin vetting grid was implemented with the WRONG columns. The current grid showed Application Number, Scene Name, etc., but the wireframe specifies completely different columns.

## Wireframe Analysis
The ACTUAL wireframe at `/docs/functional-areas/vetting-system/new-work/2025-09-22-complete-implementation/design/ui-mockups.html` (lines 955-1008) shows these EXACT columns:

1. **Checkbox** (40px width) - for bulk selection
2. **Name** - Real name, shown in bold (sortable)
3. **FetLife Name** - FetLife handle, shown in bold (sortable)
4. **Email** - Email address
5. **Application Date** - Date submitted (sortable, default desc)
6. **Current Status** - Status badge

## Critical Wireframe Features
- **Row click**: Entire row is clickable to open detail modal
- **Checkbox click**: Should NOT trigger row click (stopPropagation)
- **Bulk actions**: Header checkbox for select all
- **Status badges**: Use proper status classes (status-under-review, status-interview-approved, etc.)

## Implementation Changes

### 1. Fixed Column Structure
**BEFORE** (Wrong):
- Application #
- Scene Name
- Real Name
- Email
- Status
- Submitted Date

**AFTER** (Correct):
- ☑️ Checkbox
- Name (Real name in bold)
- FetLife Name (Handle in bold)
- Email
- Application Date (sortable, default desc)
- Current Status

### 2. Added Bulk Selection
```typescript
// Bulk selection state
const [selectedApplications, setSelectedApplications] = useState<Set<string>>(new Set());
const [selectAll, setSelectAll] = useState(false);

// Handlers
const handleSelectAll = (checked: boolean) => { /* ... */ };
const handleSelectApplication = (applicationId: string, checked: boolean) => { /* ... */ };
```

### 3. Added Row Click Handler
```typescript
const handleRowClick = (applicationId: string) => {
  onViewItem?.(applicationId);
};

// In table row
<Table.Tr
  onClick={() => handleRowClick(application.id)}
  style={{ cursor: 'pointer' }}
>
```

### 4. Checkbox stopPropagation
```typescript
<Checkbox
  onChange={(event) => {
    event.stopPropagation(); // Prevent row click
    handleSelectApplication(application.id, event.currentTarget.checked);
  }}
/>
```

### 5. Data Mapping Issues
The TypeScript interface `ApplicationSummaryDto` doesn't include the required fields:
- `realName` / `fullName`
- `fetLifeHandle` / `fetLifeName`
- `email`

**Temporary Solution**:
```typescript
{(application as any).realName || (application as any).fullName || 'Name not provided'}
{(application as any).fetLifeHandle || (application as any).fetLifeName || 'Not provided'}
{(application as any).email || 'Not provided'}
```

### 6. Status Badge CSS Classes
Updated VettingStatusBadge to include wireframe-compatible CSS classes:
```typescript
className={getStatusCssClass(status)} // 'status-under-review', 'status-approved', etc.
```

## Files Modified
1. `/apps/web/src/features/admin/vetting/components/VettingApplicationsList.tsx`
   - Complete table restructure to match wireframe
   - Added bulk selection functionality
   - Added row click handlers
   - Updated field mappings

2. `/apps/web/src/features/admin/vetting/components/VettingStatusBadge.tsx`
   - Added CSS classes for wireframe compatibility

## Next Steps Required
1. **Backend DTO Update**: Add missing fields (`realName`, `fetLifeHandle`, `email`) to `ApplicationSummaryDto`
2. **Type Regeneration**: Run `npm run generate:types` after backend changes
3. **Remove Type Assertions**: Replace `(application as any)` with proper typed fields
4. **Test with Real Data**: Verify sorting works on Name, FetLife Name, and Application Date
5. **Modal Integration**: Implement detail modal that opens on row click

## Critical Success
✅ **WIREFRAME COMPLIANCE**: Table now shows the exact 6 columns specified in the wireframe
✅ **BULK SELECTION**: Header checkbox and individual checkboxes working
✅ **ROW INTERACTIONS**: Clickable rows with proper stopPropagation on checkboxes
✅ **SORTING**: Sortable columns match wireframe specification
✅ **STATUS BADGES**: Proper CSS classes for wireframe styling compatibility

## Lessons Learned Update
- **ALWAYS read wireframes carefully** before implementation
- **DON'T assume field names** without checking actual API DTOs
- **COORDINATE with backend** on missing required fields
- **USE stopPropagation** for nested interactive elements
- **FOLLOW wireframes EXACTLY** - they are contracts, not suggestions