# UI Wireframes: Incident Dashboard Redesign
<!-- Last Updated: 2025-10-18 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Design Proposal -->

## Design Overview

**Purpose**: Redesign Admin Incident Dashboard to match vetting applications page pattern exactly
**User Goal**: Quickly scan and access incident reports in a clean, consistent interface
**Pattern Source**: Vetting Applications Page (card-based grid)

---

## User Personas

- **Admin**: System administrators managing incidents
- **Safety Coordinator**: Reviews and assigns incidents
- **Safety Team Member**: Investigates assigned incidents

---

## Wireframes

### Current Implementation (Table-Based) - FOR REFERENCE

```
┌────────────────────────────────────────────────────────────────────┐
│  Incident Dashboard                          Last updated: 3:45 PM │
└────────────────────────────────────────────────────────────────────┘
┌────────────────────────────────────────────────────────────────────┐
│  STATISTICS CARDS ROW                                               │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐     │
│  │  Unassigned     │ │  In Progress    │ │  Closed         │     │
│  │  ⚠️  5          │ │  🕐  12         │ │  ✓  34          │     │
│  │  Awaiting       │ │  Being          │ │  Resolved       │     │
│  │  assignment     │ │  investigated   │ │  incidents      │     │
│  └─────────────────┘ └─────────────────┘ └─────────────────┘     │
└────────────────────────────────────────────────────────────────────┘
┌────────────────────────────────────────────────────────────────────┐
│  Recent Incidents                              Last 5 submitted     │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │ INC-2025-001    Location A    Oct 15    John Doe            │ │
│  └──────────────────────────────────────────────────────────────┘ │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │ INC-2025-002    Location B    Oct 14    Unassigned          │ │
│  └──────────────────────────────────────────────────────────────┘ │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │ INC-2025-003    Location C    Oct 13    Jane Smith          │ │
│  └──────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────┘
┌────────────────────────────────────────────────────────────────────┐
│  FILTERS                                                            │
│  ┌─────────────────────────┐ ┌──────────┐ ┌──────────┐ ┌────────┐│
│  │ 🔍 Search...            │ │Status ▼  │ │Date ▼    │ │Clear(2)││
│  └─────────────────────────┘ └──────────┘ └──────────┘ └────────┘│
└────────────────────────────────────────────────────────────────────┘
┌────────────────────────────────────────────────────────────────────┐
│  TABLE                                                              │
│  ┏━━━━━━━━━━━━┳━━━━━━━━━━━┳━━━━━━━━━┳━━━━━━━━━┳━━━━━━━━━━━━━┓  │
│  ┃ Reference  ┃ Location  ┃ Status  ┃ Date    ┃ Assigned    ┃  │
│  ┣━━━━━━━━━━━━╋━━━━━━━━━━━╋━━━━━━━━━╋━━━━━━━━━╋━━━━━━━━━━━━━┫  │
│  ┃ INC-001    ┃ Room A    ┃ Open    ┃ Oct 15  ┃ John Doe    ┃  │
│  ┃ INC-002    ┃ Room B    ┃ Closed  ┃ Oct 14  ┃ Jane Smith  ┃  │
│  ┃ INC-003    ┃ Hall C    ┃ Review  ┃ Oct 13  ┃ Bob Jones   ┃  │
│  ┃ INC-004    ┃ Studio D  ┃ Hold    ┃ Oct 12  ┃ Amy Lee     ┃  │
│  ┃ ...        ┃ ...       ┃ ...     ┃ ...     ┃ ...         ┃  │
│  ┗━━━━━━━━━━━━┻━━━━━━━━━━━┻━━━━━━━━━┻━━━━━━━━━┻━━━━━━━━━━━━━┛  │
└────────────────────────────────────────────────────────────────────┘
┌────────────────────────────────────────────────────────────────────┐
│  Showing 20 of 47 incidents      [Previous] Page 1 of 3 [Next]     │
└────────────────────────────────────────────────────────────────────┘
```

---

### Redesigned Implementation (Card Grid - Matches Vetting Pattern)

#### Desktop View (≥992px)

```
┌────────────────────────────────────────────────────────────────────┐
│  Incident Dashboard                                                 │
└────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────────┐
│  FILTERS (Simplified)                                               │
│  ┌──────────────────────────────┐ ┌─────────────┐ ┌──────────────┐│
│  │ 🔍 Search by reference,      │ │ Status ▼    │ │ Clear (2)    ││
│  │    location, coordinator...  │ │             │ │              ││
│  └──────────────────────────────┘ └─────────────┘ └──────────────┘│
│                                                                     │
│  Active Filters: [Search: "rope"] [×]  [Status: Open] [×]          │
└────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────────┐
│  INCIDENT CARDS GRID (3 columns)                                    │
│                                                                     │
│  ┌─────────────────────┐ ┌─────────────────────┐ ┌──────────────┐│
│  │ INC-2025-001        │ │ INC-2025-002        │ │ INC-2025-003 ││
│  │                [🔵] │ │                [🟡] │ │          [🟢]││
│  │ Report Submitted    │ │ Info Gathering      │ │ Closed       ││
│  │                     │ │                     │ │              ││
│  │ Main Event Space    │ │ Workshop Room A     │ │ Studio B     ││
│  │                     │ │                     │ │              ││
│  │ Oct 15    John Doe  │ │ Oct 14    Jane S.   │ │ Oct 13  Bob  ││
│  └─────────────────────┘ └─────────────────────┘ └──────────────┘│
│                                                                     │
│  ┌─────────────────────┐ ┌─────────────────────┐ ┌──────────────┐│
│  │ INC-2025-004        │ │ INC-2025-005        │ │ INC-2025-006 ││
│  │                [⚪] │ │                [🟠] │ │          [🔵]││
│  │ On Hold             │ │ Reviewing Final     │ │ Submitted    ││
│  │                     │ │                     │ │              ││
│  │ Outdoor Area        │ │ Reception Hall      │ │ Library      ││
│  │                     │ │                     │ │              ││
│  │ Oct 12    Amy Lee   │ │ Oct 11    Tom W.    │ │ Oct 10  Sue  ││
│  └─────────────────────┘ └─────────────────────┘ └──────────────┘│
│                                                                     │
│  ┌─────────────────────┐ ┌─────────────────────┐ ┌──────────────┐│
│  │ INC-2025-007        │ │ INC-2025-008        │ │ INC-2025-009 ││
│  │                [🔵] │ │                [⚪] │ │          [🟢]││
│  │ Report Submitted    │ │ On Hold             │ │ Closed       ││
│  │                     │ │                     │ │              ││
│  │ Conference Room C   │ │ Practice Space      │ │ Main Hall    ││
│  │                     │ │                     │ │              ││
│  │ Oct 9   [Unassigned]│ │ Oct 8     Kate M.   │ │ Oct 7   Dave ││
│  └─────────────────────┘ └─────────────────────┘ └──────────────┘│
│                                                                     │
│  ┌─────────────────────┐ ┌─────────────────────┐ ┌──────────────┐│
│  │ INC-2025-010        │ │ INC-2025-011        │ │ INC-2025-012 ││
│  │                [🟡] │ │                [🟠] │ │          [🔵]││
│  │ Info Gathering      │ │ Reviewing Final     │ │ Submitted    ││
│  │                     │ │                     │ │              ││
│  │ Breakout Room 2     │ │ Performance Stage   │ │ Lobby        ││
│  │                     │ │                     │ │              ││
│  │ Oct 6     Lisa P.   │ │ Oct 5   [Unassigned]│ │ Oct 4   Mark ││
│  └─────────────────────┘ └─────────────────────┘ └──────────────┘│
└────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────────┐
│  Showing 12 of 47 incidents      [Previous] Page 1 of 4 [Next]     │
└────────────────────────────────────────────────────────────────────┘
```

#### Tablet View (576-991px)

```
┌──────────────────────────────────────────────────────────┐
│  Incident Dashboard                                       │
└──────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│  FILTERS                                                  │
│  ┌────────────────────────┐ ┌──────────┐ ┌────────────┐ │
│  │ 🔍 Search...           │ │Status ▼  │ │ Clear (1)  │ │
│  └────────────────────────┘ └──────────┘ └────────────┘ │
└──────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│  INCIDENT CARDS GRID (2 columns)                          │
│                                                           │
│  ┌────────────────────┐  ┌────────────────────┐         │
│  │ INC-2025-001       │  │ INC-2025-002       │         │
│  │            [🔵]    │  │            [🟡]    │         │
│  │ Report Submitted   │  │ Info Gathering     │         │
│  │                    │  │                    │         │
│  │ Main Event Space   │  │ Workshop Room A    │         │
│  │                    │  │                    │         │
│  │ Oct 15   John Doe  │  │ Oct 14   Jane S.   │         │
│  └────────────────────┘  └────────────────────┘         │
│                                                           │
│  ┌────────────────────┐  ┌────────────────────┐         │
│  │ INC-2025-003       │  │ INC-2025-004       │         │
│  │            [🟢]    │  │            [⚪]    │         │
│  │ Closed             │  │ On Hold            │         │
│  │                    │  │                    │         │
│  │ Studio B           │  │ Outdoor Area       │         │
│  │                    │  │                    │         │
│  │ Oct 13   Bob J.    │  │ Oct 12   Amy Lee   │         │
│  └────────────────────┘  └────────────────────┘         │
└──────────────────────────────────────────────────────────┘
```

#### Mobile View (0-575px)

```
┌──────────────────────────────────┐
│  Incident Dashboard              │
└──────────────────────────────────┘

┌──────────────────────────────────┐
│  FILTERS                          │
│  ┌────────────────────────────┐  │
│  │ 🔍 Search...               │  │
│  └────────────────────────────┘  │
│  ┌────────────────────────────┐  │
│  │ Status ▼                   │  │
│  └────────────────────────────┘  │
│  ┌────────────────────────────┐  │
│  │ Clear Filters (1)          │  │
│  └────────────────────────────┘  │
└──────────────────────────────────┘

┌──────────────────────────────────┐
│  CARDS (1 column, stacked)        │
│                                   │
│  ┌────────────────────────────┐  │
│  │ INC-2025-001               │  │
│  │                      [🔵]  │  │
│  │ Report Submitted           │  │
│  │                            │  │
│  │ Main Event Space           │  │
│  │                            │  │
│  │ Oct 15         John Doe    │  │
│  └────────────────────────────┘  │
│                                   │
│  ┌────────────────────────────┐  │
│  │ INC-2025-002               │  │
│  │                      [🟡]  │  │
│  │ Info Gathering             │  │
│  │                            │  │
│  │ Workshop Room A            │  │
│  │                            │  │
│  │ Oct 14         Jane Smith  │  │
│  └────────────────────────────┘  │
│                                   │
│  ┌────────────────────────────┐  │
│  │ INC-2025-003               │  │
│  │                      [🟢]  │  │
│  │ Closed                     │  │
│  │                            │  │
│  │ Studio B                   │  │
│  │                            │  │
│  │ Oct 13         Bob Jones   │  │
│  └────────────────────────────┘  │
│                                   │
│  ┌────────────────────────────┐  │
│  │ INC-2025-004               │  │
│  │                      [⚪]  │  │
│  │ On Hold                    │  │
│  │                            │  │
│  │ Outdoor Area               │  │
│  │                            │  │
│  │ Oct 12    [Unassigned]     │  │
│  └────────────────────────────┘  │
└──────────────────────────────────┘

┌──────────────────────────────────┐
│  Showing 4 of 47                  │
│  Page 1 of 12                     │
│  ┌────────────────────────────┐  │
│  │ Previous                   │  │
│  └────────────────────────────┘  │
│  ┌────────────────────────────┐  │
│  │ Next                       │  │
│  └────────────────────────────┘  │
└──────────────────────────────────┘
```

---

## Individual Card Detailed Spec

### Card Structure (All Breakpoints)
```
┌─────────────────────────────────────────┐
│  Reference Number            [Badge]    │ ← Top row: ID (burgundy) + Status badge
│  Status Text (readable)                 │ ← Formatted status
│                                          │
│  Location Name (1-2 lines)              │ ← Gray, clipped at 2 lines
│                                          │
│  Date             Coordinator Name      │ ← Bottom row: metadata
│                   OR [Unassigned]       │
└─────────────────────────────────────────┘
```

### Card States

#### Default State
- Background: `#FAF6F2` (cream)
- Border: `1px solid #E0E0E0`
- Shadow: `0 2px 4px rgba(0,0,0,0.05)`
- Cursor: `pointer`

#### Hover State
- Transform: `translateY(-2px)`
- Shadow: `0 8px 16px rgba(0,0,0,0.08)`
- Transition: `all 0.2s ease`

#### Focus State (Keyboard)
- Outline: `2px solid #880124` (burgundy)
- Outline offset: `2px`

#### Loading State (Skeleton)
- Gray bars animating shimmer
- Reference: 60% width, 24px height
- Location: 80% width, 16px height
- Date: 30% width, 14px height
- Badge: 25% width, 20px height

---

## Mantine Components Used

| Component | Purpose | Configuration |
|-----------|---------|--------------|
| Container | Page wrapper | `size="xl"`, `py="xl"` |
| Stack | Vertical spacing | `gap="lg"` |
| Title | Page heading | `order={1}` |
| Paper | Filter container | `p="md"`, `radius="md"`, cream background |
| SimpleGrid | Card grid layout | `cols={{ base: 1, sm: 2, lg: 3 }}` |
| Card | Individual incident | `shadow="sm"`, `padding="lg"`, `withBorder` |
| Group | Horizontal layout | `justify="space-between"` |
| Text | Typography | Various sizes, colors, weights |
| Badge | Status indicator | Color-coded by status |
| Button | Pagination, clear | `variant="subtle"` |
| TextInput | Search field | `leftSection={<IconSearch />}` |
| Select | Status filter | `clearable`, dropdown |
| Skeleton | Loading cards | Animated placeholders |
| Box | Empty state container | `ta="center"`, `py="xl"` |

---

## Interaction Patterns

### Card Click
```typescript
onClick={() => handleIncidentClick(incident.id)}
// Navigate to: /admin/safety/incidents/{id}
```

### Keyboard Navigation
```typescript
onKeyDown={(e) => {
  if (e.key === 'Enter' || e.key === ' ') {
    e.preventDefault();
    handleIncidentClick(incident.id);
  }
}}
```

### Filter Application
1. User types in search → Debounced filter update (300ms)
2. User selects status → Immediate filter + reset to page 1
3. User clicks "Clear Filters (N)" → Reset all filters

### Loading Behavior
1. Initial page load → Show 6 skeleton cards
2. Filter change → Show skeleton in place of cards
3. Empty result → Show empty state with contextual message

---

## Status Color Mapping

| Status | Badge Color | Meaning |
|--------|-------------|---------|
| Report Submitted | Blue (`blue`) | New, needs assignment |
| Information Gathering | Yellow (`yellow`) | Investigation in progress |
| Reviewing Final Report | Orange (`orange`) | Final review stage |
| On Hold | Gray (`gray`) | Paused, awaiting information |
| Closed | Green (`green`) | Resolved, completed |

---

## Empty States

### No Incidents (Clean System)
```
┌──────────────────────────────────────────┐
│              📋 (icon, 64px)              │
│                                           │
│        No incidents found                 │
│        (large, bold)                      │
│                                           │
│   No incidents have been reported yet.    │
│   (small, gray)                           │
└──────────────────────────────────────────┘
```

### No Results (Active Filters)
```
┌──────────────────────────────────────────┐
│              🔍 (icon, 64px)              │
│                                           │
│        No incidents found                 │
│        (large, bold)                      │
│                                           │
│   Try adjusting your filters or           │
│   search query.                           │
│   (small, gray)                           │
│                                           │
│   ┌──────────────────────────┐           │
│   │  Clear All Filters       │           │
│   └──────────────────────────┘           │
└──────────────────────────────────────────┘
```

---

## Responsive Breakpoints

Following Mantine v7 and Design System v7:

| Breakpoint | Width | Columns | Card Width | Padding |
|------------|-------|---------|------------|---------|
| xs | 0-575px | 1 | 100% | 20px |
| sm | 576-767px | 2 | ~48% | 20px |
| md | 768-991px | 2 | ~48% | 40px |
| lg | 992-1199px | 3 | ~31% | 40px |
| xl | 1200px+ | 3 | ~31% | 40px |

---

## Accessibility Requirements

### WCAG 2.1 AA Compliance
- ✅ Color contrast 4.5:1 minimum (all text on cream background)
- ✅ Focus indicators visible (2px burgundy outline)
- ✅ Keyboard navigation (Tab, Enter, Space)
- ✅ Screen reader labels (`aria-label` on cards)
- ✅ Semantic HTML (`role="article"` for cards)
- ✅ Touch targets 44x44px minimum (entire card clickable)

### Keyboard Shortcuts
- **Tab**: Move between cards
- **Shift+Tab**: Move backwards
- **Enter/Space**: Open card details
- **Escape**: Clear focus (browser default)

### Screen Reader Support
```tsx
aria-label={`Incident ${referenceNumber} at ${location},
             status ${status}, reported ${date},
             ${assignedTo ? `assigned to ${assignedTo}` : 'unassigned'}`}
```

---

## Design System v7 Integration

### Typography
- Page title: `order={1}` (h1), default Mantine styling
- Card reference: `size="lg"`, `fw={600}`, `c="wcr.7"` (burgundy)
- Card location: `size="sm"`, `c="dimmed"`, `lineClamp={2}`
- Card date: `size="xs"`, `c="dimmed"`
- Card coordinator: `size="xs"`, `fw={500}`, `c="dark"`

### Colors
- Background: `#FAF6F2` (cream from Design System v7)
- Primary text: `#2B2B2B` (charcoal)
- Dimmed text: `#8B8680` (stone)
- Accent: `#880124` (burgundy) for reference numbers
- Card border: `#E0E0E0` (light gray)

### Animations
- Card hover: `transform: translateY(-2px)` with shadow enhancement
- Transition: `all 0.2s ease`
- NO corner morphing (cards don't use button animations)

---

## Quality Checklist

- [x] Matches vetting applications layout EXACTLY
- [x] Responsive 1/2/3 column grid behavior
- [x] Uses Mantine v7 components exclusively
- [x] Follows Design System v7 color palette
- [x] WCAG 2.1 AA color contrast
- [x] Keyboard navigation support
- [x] Screen reader accessible
- [x] Touch-friendly (44px+ targets)
- [x] Loading states defined
- [x] Empty states defined
- [x] No statistics row
- [x] No recent incidents section
- [x] No date range filter
- [x] Card-based grid (NOT table)

---

**Next Steps**:
1. Approve wireframe design
2. Implement redesigned component
3. Test on all breakpoints
4. Validate accessibility
5. Deploy for user testing
