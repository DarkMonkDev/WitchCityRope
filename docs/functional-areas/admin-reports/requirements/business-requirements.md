# Admin Reports & Analytics - Business Requirements

**Date**: 2026-03-17
**Status**: Draft - Awaiting Approval
**Author**: Orchestrator + Business Requirements Agent
**Stakeholder**: Chad (Admin/Owner)

---

## 1. Overview

Add a Reports section to the admin area that provides analytics dashboards, transaction charts, and user statistics. The reports section uses a left-hand navigation layout with a dashboard home and individual report pages. The existing Payment Analytics functionality moves from the admin dashboard into this new section.

## 2. Business Goals

1. **Centralized reporting** — One place for all admin analytics instead of scattered cards
2. **User visibility** — At-a-glance summary of community membership health (vetted, verified, total)
3. **Transaction monitoring** — Daily transaction volume with failed CC and incomplete payment visibility
4. **Extensibility** — Layout that easily accommodates future reports without redesign

## 3. User Stories

### US-1: Reports Card on Admin Dashboard
**As an** admin, **I want** a "Reports" card on the `/admin` page **so that** I can navigate to the reports section from the main admin dashboard.

**Acceptance Criteria:**
- Card styled identically to existing admin cards (Email Templates, Content Management, etc.)
- Card title: "Reports"
- Uses `IconChartBar` icon (reuse from current Payment Analytics card)
- Links to `/admin/reports`
- The current "Payment Analytics" card is **removed** from the admin dashboard (replaced by this card)

### US-2: Reports Layout with Left Navigation
**As an** admin, **I want** a reports section with a left-hand navigation menu **so that** I can switch between different reports without leaving the section.

**Acceptance Criteria:**
- Left nav menu with items: Dashboard, Payments, Transactions
- Active nav item is visually highlighted
- Right side shows the selected report content
- Left nav collapses to a top bar or hamburger menu on mobile
- URL updates when switching reports (e.g., `/admin/reports`, `/admin/reports/payments`, `/admin/reports/transactions`)
- Design is extensible — adding a new nav item requires only adding a route and menu entry

### US-3: Dashboard Home (Default View)
**As an** admin, **I want** the default reports view to show a dashboard home with user summary statistics **so that** I can see community health at a glance.

**Acceptance Criteria:**
- **User Summary Section** with the following stat cards:
  - **Total Users**: Count of all users with profiles on the site
  - **Verified Users**: Users where `EmailConfirmed = true`
  - **Unverified Users**: Users where `EmailConfirmed = false`
  - **Vetted Members**: Users where `VettingStatus = 3` (Approved)
  - **Vetting Applications Submitted**: Users where `HasVettingApplication = true`
- Stats are **not time-based** — they show current totals
- Each stat displayed as a card/tile with a number and label
- Styling consistent with existing admin patterns (ivory background, burgundy accents)

### US-4: Payments Report
**As an** admin, **I want** the existing payment analytics interface available under Reports → Payments **so that** all reporting is centralized.

**Acceptance Criteria:**
- **Identical functionality** to current `/admin/analytics/payments` page
- Same filter bar (search, date range, payment methods, statuses)
- Same sortable table with all columns
- Same refund capability
- Same pagination
- Old route (`/admin/analytics/payments`) redirects to `/admin/reports/payments`

### US-5: Transactions Report (Chart + Detail Table)
**As an** admin, **I want** to see daily transaction volume, failed credit cards, and incomplete payments on a chart **so that** I can monitor payment health over time.

**Acceptance Criteria:**
- **Chart** (top half of page):
  - Bar or line chart showing daily data over a selectable date range
  - Default date range: last 30 days
  - Three data series on the chart:
    1. **Completed transactions** per day (count)
    2. **Failed credit card attempts** per day (count)
    3. **Incomplete/pending payments** per day (count — `PaymentStatus = Pending` older than a reasonable threshold, or `PaymentStatus = Failed`)
  - Date range picker to adjust the time window
- **Detail Table** (bottom half of page):
  - When a data point on the chart is clicked, a table appears below showing the individual transactions for that day
  - Table columns: Date, User, Event, Payment Method, Amount, Status
  - Sortable and paginated
  - If no data point is selected, show a prompt: "Click a day on the chart to see transaction details"
- **Charting Library**: @mantine/charts (wraps Recharts, native Mantine integration)

## 4. Data Requirements

### New Backend API Endpoints Needed

#### GET /api/admin/reports/user-summary
Returns current user counts for the dashboard home.

**Response:**
```json
{
  "totalUsers": 245,
  "verifiedUsers": 198,
  "unverifiedUsers": 47,
  "vettedMembers": 132,
  "vettingApplicationsSubmitted": 156
}
```

#### GET /api/admin/reports/daily-transactions
Returns daily transaction aggregates for a date range.

**Query Parameters:**
- `dateFrom` (DateOnly, required)
- `dateTo` (DateOnly, required)

**Response:**
```json
{
  "days": [
    {
      "date": "2026-03-15",
      "completedCount": 12,
      "completedAmount": 450.00,
      "failedCount": 2,
      "incompleteCount": 1
    },
    {
      "date": "2026-03-16",
      "completedCount": 8,
      "completedAmount": 320.00,
      "failedCount": 0,
      "incompleteCount": 0
    }
  ]
}
```

**Data Sources:**
- `completedCount` / `completedAmount`: `TicketPurchase` where `PaymentStatus` in (Completed, Confirmed), grouped by `PurchaseDate` date
- `failedCount`: `TicketPurchase` where `PaymentStatus = Failed`, grouped by `PurchaseDate` date. Additionally cross-reference `DailyLogSummary` where `Category = 'cc_failure'` for CC-specific failures that may not have created a TicketPurchase record
- `incompleteCount`: `TicketPurchase` where `PaymentStatus = Pending`, grouped by `PurchaseDate` date

### Existing Endpoints (Reused)
- `GET /api/admin/payments` — existing payment list with filtering (used by Payments report page)

## 5. Technical Approach

### Charting Library
- **@mantine/charts** + **recharts@^2**
- Native Mantine theming, zero additional CSS systems
- Click-to-drill-down via recharts `barProps.onClick` passthrough
- Install: `npm install @mantine/charts recharts@^2`
- CSS: Add `import '@mantine/charts/styles.css'` to app entry point

### Frontend Architecture
- New route group: `/admin/reports/*`
- Layout component: `AdminReportsLayout` with left nav + content area
- Pages:
  - `AdminReportsPage` (dashboard home) at `/admin/reports`
  - Existing `AdminPaymentsPage` reused at `/admin/reports/payments`
  - `AdminTransactionsReportPage` at `/admin/reports/transactions`
- Feature folder: `apps/web/src/features/admin/reports/`

### Left Navigation Pattern
- Mantine `NavLink` component for nav items
- `AppShell` or simple `Grid` layout (2-column: nav 250px fixed + content fluid)
- Mobile: nav collapses to horizontal tabs or hamburger drawer
- Active state derived from current route via `useLocation()`

### Styling
- Reuse existing design tokens: burgundy `#880124`, ivory `#FFF8F0`, plum `#614B79`
- Stat cards: `Paper` component matching admin card patterns
- Tables: match existing `BaseEventsTable` / payment table patterns
- No new colors, no new fonts, no new component libraries

## 6. Scope Boundaries

### In Scope
- Reports card on admin dashboard (replacing Payment Analytics card)
- Reports layout with left navigation
- Dashboard home with user summary stats
- Payments report (moved existing page)
- Transactions report with chart and drill-down table
- Two new backend API endpoints
- @mantine/charts installation and setup

### Out of Scope (Future)
- Revenue/financial reports
- Event attendance analytics
- Vetting pipeline funnel analytics
- Export to CSV/PDF
- Automated email reports
- Date-based user growth trends
- Any reports beyond what is listed above

## 7. Dependencies

- @mantine/charts package (new dependency)
- recharts@^2 package (peer dependency of @mantine/charts)
- Existing payment hooks and components
- Existing admin authentication loader (`adminLoader`)

## 8. Implementation Phases

### Phase 1: Infrastructure & Dashboard
1. Install @mantine/charts + recharts
2. Create `AdminReportsLayout` with left nav
3. Create dashboard home page with user summary stats
4. Add "Reports" card to admin dashboard, remove "Payment Analytics" card
5. Create new backend endpoint: `GET /api/admin/reports/user-summary`
6. Set up routing: `/admin/reports`, `/admin/reports/payments`, `/admin/reports/transactions`

### Phase 2: Payments Migration
1. Move `AdminPaymentsPage` to render inside `AdminReportsLayout`
2. Add redirect from `/admin/analytics/payments` to `/admin/reports/payments`
3. Verify all existing payment functionality works in new location

### Phase 3: Transactions Report
1. Create new backend endpoint: `GET /api/admin/reports/daily-transactions`
2. Build the transactions chart using @mantine/charts BarChart
3. Build click-to-drill-down detail table
4. Date range picker for chart time window
