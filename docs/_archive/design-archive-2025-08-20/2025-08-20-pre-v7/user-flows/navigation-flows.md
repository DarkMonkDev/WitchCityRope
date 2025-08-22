# Navigation & Information Architecture

## Primary Navigation Structure

### Public Navigation (Unauthenticated)
```
┌─────────────────────────────────────────────────────────┐
│ Logo                                      Login | Sign Up │
├─────────────────────────────────────────────────────────┤
│ Home | Events | How to Join | About | Contact           │
└─────────────────────────────────────────────────────────┘
```

### Member Navigation (Authenticated)
```
┌─────────────────────────────────────────────────────────┐
│ Logo                          👤 River Moon | Logout     │
├─────────────────────────────────────────────────────────┤
│ Dashboard | Events | My Tickets | Profile               │
└─────────────────────────────────────────────────────────┘
```

### Admin Navigation
```
┌─────────────────────────────────────────────────────────┐
│ Logo [ADMIN]                    👤 River Moon | Logout  │
├─────────────────────────────────────────────────────────┤
│ Admin Dashboard | Events | Members | Reports | Safety   │
└─────────────────────────────────────────────────────────┘
```

## Mobile Navigation Pattern

### Bottom Navigation (Members)
```
┌─────────────────────────────────────┐
│                                     │
│         Main Content Area           │
│                                     │
├─────────────────────────────────────┤
│  🏠      📅      🎫      ☰        │
│ Home   Events  Tickets   More      │
└─────────────────────────────────────┘
```

### Hamburger Menu Contents
```
More Menu:
├── My Profile
├── Settings
├── Safety Resources
├── Report Incident
├── Help
└── Logout
```

## Information Scent & Wayfinding

### Breadcrumb Pattern
```
Home > Events > Classes > Rope Basics Workshop
```

### Page Headers
Each page includes:
- Clear page title
- Contextual actions (right-aligned)
- Status indicators where relevant

Example:
```
┌─────────────────────────────────────────────────────┐
│ Rope Basics Workshop              [Register] button │
│ Saturday, March 15 at 2:00 PM                      │
└─────────────────────────────────────────────────────┘
```

## Progressive Disclosure Examples

### Event List Card States

**Guest View (Not Logged In):**
```
┌─────────────────────────────────────┐
│ 📅 Monthly Rope Jam                 │
│ Friday, March 21 • 7:00 PM          │
│ Member-only social event...         │
│ [🔒 Login to see details]           │
└─────────────────────────────────────┘
```

**Member View (Logged In):**
```
┌─────────────────────────────────────┐
│ 📅 Monthly Rope Jam                 │
│ Friday, March 21 • 7:00 PM          │
│ 📍 The Dungeon Space               │
│ 💵 $10-20 sliding scale            │
│ 👥 18/30 spots filled              │
│ [RSVP] [Get Tickets]               │
└─────────────────────────────────────┘
```

## Admin Sidebar Navigation

```
Admin Dashboard
├── 🏠 Dashboard
├── 📅 Events
│   ├── All Events
│   ├── Create Event
│   └── Past Events
├── 🎓 Vetting Queue (3)
├── ⚠️ Incident Reports (1)
├── 👥 Members
│   ├── All Members
│   ├── Pending
│   └── Suspended/Banned
├── 👨‍🏫 Teachers
├── 💰 Financial Reports
│   ├── Revenue Summary
│   ├── Event Performance
│   └── Refund History
└── ⚙️ Settings
```

## Contextual Navigation

### Event Management Actions
When viewing an event as admin:
```
[Edit] [Duplicate] [Email Attendees] [Cancel Event]
```

### Member Profile Actions
When viewing own profile:
```
[Edit Profile] [Change Password] [Privacy Settings] [Download Data]
```

### Vetting Application Actions
When reviewing application:
```
[Approve] [Request More Info] [Deny] [Add Note]
```

## Quick Actions & Shortcuts

### Admin Dashboard
```
Quick Actions:
┌─────────┬─────────┬─────────┬─────────┐
│ Create  │ Check   │  View   │  Send   │
│ Event   │ Queue   │ Reports │ Email   │
└─────────┴─────────┴─────────┴─────────┘
```

### Member Dashboard
```
┌─────────────────────────────────────────┐
│ Welcome back, River!                    │
├─────────────────────────────────────────┤
│ Your next event:                        │
│ Rope Basics - Tomorrow at 2 PM          │
│ [View Details] [Get Directions]         │
└─────────────────────────────────────────┘
```

## Search Patterns

### Global Search (Admin)
- Search across events, members, reports
- Filter by type
- Recent searches
- Quick filters

### Event Search (All Users)
- Filter by date
- Filter by type (Class/Meetup)
- Filter by teacher
- Sort options

### Member Search (Admin)
- Search by name (scene or legal)
- Filter by status
- Filter by join date
- Bulk actions

## Error & Empty States

### 404 Page
```
Oops! That page doesn't exist.
[Go to Homepage] [Browse Events]
```

### Empty Event List
```
No upcoming events match your filters.
[Clear Filters] [View Past Events]
```

### Access Denied
```
You don't have permission to view this page.
[Go to Dashboard] [Contact Support]
```

## Responsive Breakpoints

### Desktop (>1024px)
- Full navigation bar
- Sidebar navigation (admin)
- Multi-column layouts
- Hover states

### Tablet (768-1024px)
- Condensed navigation
- Collapsible sidebar
- Touch-optimized buttons
- Stack some columns

### Mobile (<768px)
- Bottom navigation
- Full-width cards
- Vertical stacking
- Large touch targets
- Simplified tables