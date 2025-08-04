# Events Management - Wireframes & UI Design
<!-- Last Updated: 2025-08-04 -->
<!-- Version: 2.0 -->
<!-- Owner: Events Team -->
<!-- Status: Active -->

## Overview
This document references the wireframes and visual designs for the events management system. All wireframes are HTML files that can be viewed in a browser for interactive exploration.

## Wireframe Inventory

### Public Event Pages

#### 1. Event List Page
**File**: `/docs/functional-areas/events/public-events/event-list.html`
**Visual Version**: `/docs/functional-areas/events/public-events/event-list-visual.html`

**Key Features**:
- Grid/list view toggle
- Filter sidebar (date, type, instructor)
- Event cards with key info
- Capacity indicators
- Responsive design

**Components**:
- Event card component
- Filter panel
- Sort dropdown
- Pagination controls

#### 2. Event Detail Page
**File**: `/docs/functional-areas/events/public-events/event-detail.html`
**Visual Version**: `/docs/functional-areas/events/public-events/event-detail-visual.html`

**Key Features**:
- Hero image section
- Event information panels
- Registration/RSVP buttons
- Instructor bio
- Location/venue details
- Related events

**Components**:
- Hero banner
- Info cards
- CTA buttons (context-aware)
- Map integration
- Share functionality

### Admin Event Management

#### 3. Admin Events Dashboard
**File**: `/docs/functional-areas/events/admin-events-management/admin-events.html`
**Visual Version**: `/docs/functional-areas/events/admin-events-management/admin-events-visual.html`

**Key Features**:
- Event list with actions
- Quick stats cards
- Bulk operations
- Search and filters
- Create event button

**Components**:
- Data table with actions
- Stat cards
- Bulk action toolbar
- Status badges

#### 4. Event Creation/Edit Form
**File**: `/docs/functional-areas/events/admin-events-management/event-creation.html`

**Key Features**:
- Multi-tab interface
- Form validation
- Rich text editor
- Image upload
- Preview mode

**Tabs**:
1. **Basic Info** - Core event details
2. **Tickets & Pricing** - Registration options
3. **Emails** - Communication templates
4. **Volunteers** - Staff assignments

### Event Operations

#### 5. Event Check-in Interface
**File**: `/docs/functional-areas/events/events-checkin/event-checkin.html`
**Visual Version**: `/docs/functional-areas/events/events-checkin/event-checkin-visual.html`

**Key Features**:
- Search/scan interface
- Attendee list
- Check-in modal
- Real-time stats
- Mobile-optimized

**Components**:
- Search bar with filters
- Attendee cards
- QR scanner trigger
- Progress indicators
- Quick actions

## Design System Integration

### Color Palette
```css
/* Primary Colors */
--primary: #8B0000;        /* Dark red */
--primary-hover: #A52A2A;  /* Lighter red */

/* Status Colors */
--success: #28a745;        /* Green - confirmed */
--warning: #ffc107;        /* Yellow - waitlist */
--danger: #dc3545;         /* Red - cancelled */
--info: #17a2b8;          /* Blue - information */

/* Neutral Colors */
--dark: #212529;          /* Text */
--light: #f8f9fa;         /* Backgrounds */
--gray: #6c757d;          /* Secondary text */
```

### Typography
```css
/* Headings */
h1: 2.5rem (40px) - Page titles
h2: 2rem (32px) - Section headers  
h3: 1.5rem (24px) - Card titles
h4: 1.25rem (20px) - Subsections

/* Body Text */
Body: 1rem (16px) - Regular text
Small: 0.875rem (14px) - Helper text
```

### Component Patterns

#### Event Cards
- Image aspect ratio: 16:9
- Border radius: 8px
- Shadow on hover
- Status badge positioning
- Truncated descriptions

#### Form Elements
- Label above input
- Helper text below
- Error messages in red
- Required field indicators
- Consistent spacing (1rem)

#### Buttons
- Primary: Dark red background
- Secondary: Outlined
- Danger: Red for destructive
- Disabled: Reduced opacity
- Loading: Spinner icon

## Responsive Breakpoints

```css
/* Mobile First Design */
- Mobile: < 576px
- Tablet: 576px - 768px
- Desktop: > 768px
- Large: > 1200px
```

### Mobile Adaptations
- Stack columns vertically
- Hamburger menu
- Touch-friendly targets (44px min)
- Swipe gestures for actions
- Simplified navigation

## Accessibility Considerations

### WCAG 2.1 AA Compliance
- Color contrast ratios > 4.5:1
- Keyboard navigation support
- Screen reader labels
- Focus indicators
- Alt text for images

### ARIA Labels
```html
<!-- Event card example -->
<article role="article" aria-label="Event: Introduction to Rope">
  <h3 id="event-title">Introduction to Rope</h3>
  <button aria-label="Register for Introduction to Rope">
    Register
  </button>
</article>
```

## Interactive Elements

### Loading States
- Skeleton screens for lists
- Spinner for actions
- Progress bars for uploads
- Optimistic UI updates

### Error Handling
- Inline validation messages
- Toast notifications
- Error boundaries
- Fallback UI states

### Success Feedback
- Green checkmarks
- Success toasts
- Confirmation modals
- Updated UI state

## Implementation Notes

### Syncfusion Components Used
- SfGrid - Event lists
- SfDatePicker - Date selection
- SfDropDownList - Filters
- SfTextBox - Form inputs
- SfRichTextEditor - Descriptions
- SfChip - Tags and badges
- SfToast - Notifications

### Custom Components
- EventCard.razor
- RegistrationButton.razor
- CapacityIndicator.razor
- PriceDisplay.razor
- VettingBadge.razor

## Wireframe Viewing Instructions

1. **Local Viewing**:
   ```bash
   # Navigate to docs folder
   cd docs/functional-areas/events
   
   # Open in browser
   google-chrome public-events/event-list.html
   ```

2. **Features to Explore**:
   - Hover states
   - Click interactions
   - Responsive behavior (resize window)
   - Form validations
   - Modal dialogs

3. **Visual vs Functional**:
   - Base files: Functional, grayscale
   - Visual files: With branding and colors

## Future Enhancements

### Planned UI Updates
- [ ] Dark mode support
- [ ] Animation library
- [ ] Advanced filtering
- [ ] Drag-drop for admin
- [ ] Calendar widget
- [ ] Live capacity updates

### Mobile App Considerations
- Native app designs pending
- PWA capabilities planned
- Offline mode for check-in
- Push notifications

---

*Original wireframe files created by UX team. For implementation examples, see the Components folder in the Web project.*