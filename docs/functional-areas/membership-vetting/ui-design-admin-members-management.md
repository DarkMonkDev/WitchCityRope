# Admin Members Management - UI Design Specification

## Design Overview
This document provides detailed UI specifications for the Admin Members Management feature, including mockups, component specifications, and interaction patterns.

## 1. Members List Page (`/admin/members`)

### 1.1 Page Layout Structure
```
┌─────────────────────────────────────────────────────────────────────┐
│ Admin Header (existing)                                              │
├─────────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Page Title: "Member Management"                                  │ │
│ │ Subtitle: "Manage all community members and their information"   │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Stats Cards (4 columns)                                          │ │
│ │ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐                │ │
│ │ │ Total   │ │ Vetted  │ │ Active  │ │ New This│                │ │
│ │ │ Members │ │ Members │ │ Today   │ │ Month   │                │ │
│ │ │ 1,234   │ │ 987     │ │ 45      │ │ 156     │                │ │
│ │ └─────────┘ └─────────┘ └─────────┘ └─────────┘                │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Filters and Search Bar                                           │ │
│ │ ┌──────────────┐ ┌─────────────────────────┐ ┌────────────────┐ │ │
│ │ │ Vetting: ▼   │ │ 🔍 Search members...    │ │ Items/Page: ▼  │ │ │
│ │ │ [Vetted]     │ │                         │ │ [100]          │ │ │
│ │ └──────────────┘ └─────────────────────────┘ └────────────────┘ │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Syncfusion DataGrid                                              │ │
│ │ ┌─────────────────────────────────────────────────────────────┐ │ │
│ │ │Scene Name↓│Real Name │FetLife │Email     │Joined  │Events│Status│ │
│ │ ├───────────┼──────────┼────────┼──────────┼────────┼──────┼──────┤ │
│ │ │AceRope    │John Doe  │aceknots│ace@e.com │01/15/24│  12  │Vetted│ │
│ │ ├───────────┼──────────┼────────┼──────────┼────────┼──────┼──────┤ │
│ │ │BunnyTied  │Jane Smith│bunnyt  │bunny@e.co│02/20/24│   5  │Member│ │
│ │ └───────────┴──────────┴────────┴──────────┴────────┴──────┴──────┘ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Pagination Controls                                              │ │
│ │ [First] [Prev] Page 1 of 13 [Next] [Last]  Showing 1-100 of 1234│ │
│ └─────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

### 1.2 Component Specifications

#### Stats Cards
- **Styling**: Use existing admin card pattern with icon and metric
- **Colors**: 
  - Total: Primary burgundy (#880124)
  - Vetted: Success green
  - Active: Info blue
  - New: Secondary plum (#614B79)
- **Update**: Real-time via SignalR or refresh on filter change

#### Filter Controls
- **Vetting Status Dropdown**:
  - Options: All, Vetted, Unvetted, Pending
  - Default: Vetted
  - Width: 150px
  
- **Search Box**:
  - Placeholder: "Search members..."
  - Debounce: 300ms
  - Searches: Scene name, real name, FetLife name
  - Icon: Magnifying glass
  - Clear button when text present

- **Items Per Page**:
  - Options: 10, 50, 100, 500
  - Default: 100
  - Triggers grid refresh

#### Syncfusion DataGrid Configuration
```razor
<SfGrid DataSource="@Members" 
        AllowPaging="true" 
        AllowSorting="true"
        AllowFiltering="false"
        PageSettings="@(new PageSettings { PageSize = PageSize })"
        @onclick="@OnRowClick">
    <GridEvents TValue="MemberListDto" RowSelected="OnRowSelected" />
    <GridColumns>
        <GridColumn Field="@nameof(MemberListDto.SceneName)" 
                    HeaderText="Scene Name" 
                    Width="150"
                    AllowSorting="true" />
        <GridColumn Field="@nameof(MemberListDto.RealName)" 
                    HeaderText="Real Name" 
                    Width="150" />
        <GridColumn Field="@nameof(MemberListDto.FetLifeName)" 
                    HeaderText="FetLife" 
                    Width="120" />
        <GridColumn Field="@nameof(MemberListDto.Email)" 
                    HeaderText="Email" 
                    Width="200" />
        <GridColumn Field="@nameof(MemberListDto.DateJoined)" 
                    HeaderText="Joined" 
                    Width="100" 
                    Format="d" />
        <GridColumn Field="@nameof(MemberListDto.EventsAttended)" 
                    HeaderText="Events" 
                    Width="80" 
                    TextAlign="TextAlign.Center" />
        <GridColumn Field="@nameof(MemberListDto.Status)" 
                    HeaderText="Status" 
                    Width="100">
            <Template>
                @{
                    var member = (context as MemberListDto);
                    <span class="badge badge-@GetStatusClass(member.Status)">
                        @member.Status
                    </span>
                }
            </Template>
        </GridColumn>
    </GridColumns>
</SfGrid>
```

#### Row Styling
```css
/* Alternating row colors */
.e-grid .e-row:nth-child(even) {
    background-color: var(--color-neutral-50); /* Light gray from theme */
}

.e-grid .e-row:hover {
    background-color: var(--color-primary-100); /* Light burgundy */
    cursor: pointer;
}

/* Status badges */
.badge-vetted { background-color: var(--color-success); }
.badge-member { background-color: var(--color-info); }
.badge-pending { background-color: var(--color-warning); }
.badge-unvetted { background-color: var(--color-neutral-400); }
```

## 2. Member Detail Page (`/admin/members/{id}`)

### 2.1 Page Layout Structure
```
┌─────────────────────────────────────────────────────────────────────┐
│ Admin Header (existing)                                              │
├─────────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ < Back to Members | Member: AceRope (John Doe)                  │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Tab Navigation                                                   │ │
│ │ [Overview] [Events] [Notes] [Incidents] [Account Settings]      │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Overview Tab                                                     │ │
│ │ ┌─────────────────┬─────────────────────────────────────────┐  │ │
│ │ │ Profile Info    │ Account Status                          │  │ │
│ │ │ Scene: AceRope  │ Role: Member                           │  │ │
│ │ │ Real: John Doe  │ Vetted: ✓ Yes                         │  │ │
│ │ │ FetLife: aceknot│ Active: ✓ Yes                         │  │ │
│ │ │ Email: ace@e.com│ Joined: 01/15/2024                     │  │ │
│ │ │ DOB: 03/15/1990 │ Last Login: Today at 2:45 PM          │  │ │
│ │ │ Pronouns: He/Him│ [Change Status] [Reset Password]       │  │ │
│ │ └─────────────────┴─────────────────────────────────────────┘  │ │
│ └─────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

### 2.2 Tab Content Specifications

#### Overview Tab
- **Left Panel**: Editable profile information
- **Right Panel**: Account status and actions
- **Edit Mode**: Inline editing with save/cancel buttons
- **Validation**: Real-time field validation

#### Events Tab
```
┌─────────────────────────────────────────────────────────────────────┐
│ Events History (Total: 12 events)                      [Export CSV] │
├─────────────────────────────────────────────────────────────────────┤
│ Date       │ Event Name              │ Type     │ Status   │ Actions│
├───────────┼─────────────────────────┼──────────┼──────────┼────────┤
│ 06/15/2024│ Rope Basics Workshop    │ Workshop │ Attended │ [View] │
│ 06/01/2024│ June Social             │ Social   │ No-show  │ [View] │
│ 05/20/2024│ Advanced Suspensions    │ Workshop │ Refunded │ [View] │
└───────────┴─────────────────────────┴──────────┴──────────┴────────┘

Legend: ✓ Attended | ⚠ No-show | 💰 Refunded | 📝 RSVP Only
```

#### Notes Tab
```
┌─────────────────────────────────────────────────────────────────────┐
│ Member Notes                                           [Add Note]   │
├─────────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ 📝 Vetting Note - Added by Jane Admin on 01/20/2024            │ │
│ │ Completed vetting interview. Very knowledgeable about safety... │ │
│ │ [Edit] [Delete]                                                 │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ 📌 General Note - Added by John Mod on 03/15/2024              │ │
│ │ Member volunteered to help with workshop setup. Very helpful... │ │
│ │ [Edit] [Delete]                                                 │ │
│ └─────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

#### Incidents Tab
```
┌─────────────────────────────────────────────────────────────────────┐
│ Incident Reports                                                    │
├─────────────────────────────────────────────────────────────────────┤
│ Date       │ Type              │ Severity │ Status    │ Actions    │
├───────────┼───────────────────┼──────────┼───────────┼────────────┤
│ 04/10/2024│ Safety Concern    │ Low      │ Resolved  │ [View]     │
└───────────┴───────────────────┴──────────┴───────────┴────────────┘
│ No incidents linked to this member                                  │
└─────────────────────────────────────────────────────────────────────┘
```

### 2.3 Interaction Patterns

#### Search Behavior
1. User types in search box
2. After 300ms pause, API call triggered
3. Loading indicator appears in grid
4. Results update with matching rows
5. Match highlighting on found terms
6. Result count updates

#### Sort Behavior
1. Click column header
2. Sort indicator appears (↑ or ↓)
3. Grid re-renders with sorted data
4. Pagination resets to page 1

#### Row Click Navigation
1. Entire row is clickable (cursor: pointer)
2. Click navigates to `/admin/members/{id}`
3. Smooth transition with loading state

#### Note Management
1. **Add Note**: Modal dialog with rich text editor
2. **Edit Note**: Inline editing with save/cancel
3. **Delete Note**: Confirmation dialog, soft delete

## 3. Responsive Design

### Breakpoints
- **Desktop**: 1200px+ (full grid view)
- **Tablet**: 768px-1199px (condensed grid, hide some columns)
- **Mobile**: <768px (card view instead of grid)

### Mobile Adaptations
- Stack filter controls vertically
- Convert grid to card layout
- Prioritize key fields (Scene Name, Status)
- Use accordions for detail sections

## 4. Accessibility

### WCAG 2.1 AA Compliance
- All interactive elements keyboard accessible
- Proper ARIA labels on controls
- Color contrast ratios meet standards
- Screen reader announcements for updates
- Focus indicators on all interactive elements

### Keyboard Navigation
- Tab through all controls
- Enter to select/activate
- Escape to close modals
- Arrow keys for grid navigation

## 5. Loading States

### Initial Load
```
┌─────────────────────────────────────────────────────────────────────┐
│                                                                      │
│                    [Spinner Icon]                                    │
│                    Loading members...                                │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### Search/Filter Updates
- Overlay spinner on grid
- Disable controls during load
- Show "No results found" for empty results

## 6. Error States

### API Errors
```
┌─────────────────────────────────────────────────────────────────────┐
│ ⚠️ Unable to load members                                           │
│ Please try again or contact support if the problem persists.       │
│ [Retry] [Dismiss]                                                   │
└─────────────────────────────────────────────────────────────────────┘
```

### Validation Errors
- Inline field validation messages
- Red border on invalid fields
- Clear error messages

## 7. Performance Considerations

### Optimization Strategies
1. Virtual scrolling for large datasets
2. Lazy loading for detail page tabs
3. Debounced search input
4. Server-side pagination and sorting
5. Caching for repeated queries

### Target Metrics
- Initial page load: <2 seconds
- Search response: <500ms
- Sort operation: <300ms
- Navigation to detail: <1 second