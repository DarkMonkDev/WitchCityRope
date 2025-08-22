# Component Inventory - Witch City Rope Wireframes

## Overview
Comprehensive inventory of all UI components found across 22 wireframe files, documenting variations, inconsistencies, and standardization opportunities.

## 1. Buttons

### Primary Buttons
- **Classes**: `.btn-primary`, `.primary-btn`, `.button-primary`
- **Colors**: `#8B4513` background, white text
- **Padding**: `10px 20px`, `12px 24px`, `16px 32px` (size variants)
- **Files**: All files

### Secondary Buttons  
- **Classes**: `.btn-secondary`, `.secondary-btn`, `.button-secondary`
- **Styles**: 
  - Variant 1: Transparent bg, `#8B4513` border (landing, event pages)
  - Variant 2: Gray bg `#f5f5f5` (dashboard, admin)
- **Need**: Consolidation to single style

### Warning Buttons
- **Classes**: `.btn-warning`, `.button-warning`
- **Colors**: Multiple oranges (`#ffc107`, `#ff9800`, `#f57c00`)
- **Usage**: Dashboard alerts, admin actions

### Danger Buttons
- **Classes**: `.btn-danger`, `.danger-button`
- **Colors**: `#d32f2f`, `#dc3545` (Bootstrap red)
- **Usage**: Delete actions, withdrawals

### Special Buttons
- **Google OAuth**: Custom styled with Google colors
- **Calendar buttons**: Icon + text combinations
- **Action buttons**: Various one-off styles

### Issues Found
- Inconsistent class naming (btn vs button)
- Multiple padding schemes
- Missing disabled states
- Incomplete hover states

## 2. Form Components

### Input Fields
- **Classes**: `.form-input`, `.input-field`, `input[type="text"]`
- **Border**: Mix of 1px and 2px borders
- **Padding**: `10px 14px`, `12px`, `12px 16px`
- **Focus**: Some have `border-color: #8B4513`, others missing

### Textareas
- **Classes**: `.form-textarea`, `textarea`
- **Min-height**: 100px, 120px, 150px (inconsistent)
- **Resize**: Some vertical, some both

### Select Dropdowns
- **Classes**: `.form-select`, `select`
- **Styling**: Inconsistent arrow customization

### Checkboxes & Radio Buttons
- **Default HTML styling** in most places
- **Custom styled** in vetting application
- **Toggle switches** in settings pages

### Form Groups
- **Classes**: `.form-group`, `.input-group`
- **Spacing**: `margin-bottom: 16px`, `20px`, `24px`
- **Label styling**: Inconsistent weight and spacing

## 3. Cards

### Event Cards
- **Structure**: Image + content + footer
- **Padding**: 20px or 24px
- **Border-radius**: 8px, 10px, 12px
- **Shadow**: Various depths

### Ticket Cards
- **Unique gradient header**
- **Couples ticket variation**
- **Status badges integrated**

### Member Cards
- **Avatar + info layout**
- **Horizontal on desktop, vertical on mobile**

### Info Cards
- **Simple box with content**
- **Used in dashboards and admin**

## 4. Navigation Components

### Main Navigation
- **Guest nav**: Logo + nav items + CTA button
- **Member nav**: Adds user menu dropdown
- **Mobile**: Hamburger menu (only on landing)

### Admin Sidebar
- **Fixed sidebar with icons**
- **Active state highlighting**
- **Collapsible sections**

### Tabs
- **Classes**: `.tabs`, `.tab-button`
- **Active indicator**: Border bottom or background
- **Used in**: Auth pages, settings, events

### Breadcrumbs
- **Only in admin pages**
- **Inconsistent separator (/, >, →)**

## 5. Badges & Pills

### Status Badges
- **Success**: Green bg/text
- **Warning**: Orange variations
- **Error**: Red variations
- **Info**: Blue consistent

### Event Type Badges
- **Public/Private**
- **Class/Meetup**
- **Capacity indicators**

### Count Badges
- **Circular number indicators**
- **Used in navigation and tabs**

## 6. Tables

### Data Tables
- **Admin only**: Member lists, event management
- **Features**: Sortable headers, action columns
- **Styling**: Striped rows, hover states

### Issues
- No responsive table solution
- Inconsistent padding
- Missing empty states

## 7. Modals

### Standard Modal
- **Structure**: Header + body + footer
- **Width**: 500px, 600px variants
- **Overlay**: `rgba(0,0,0,0.5)`

### Specialized Modals
- **Check-in modal**: Custom layout
- **Assignment modal**: Form-focused
- **Confirmation modals**: Warning styles

## 8. Alerts & Messages

### Alert Types
- Success, Warning, Error, Info
- **Inconsistent styling** across pages
- Some have icons, others don't
- Close button not standardized

### Toast/Flash Messages
- **Only in profile settings**
- **Position**: Top right
- **Animation**: Slide in

## 9. Progress Indicators

### Step Progress
- **Vetting application**: Numbered steps
- **Event creation**: Wizard style
- **2FA setup**: Linear progress

### Capacity Bars
- **Event capacity**: Percentage filled
- **Visual**: Green → yellow → red
- **Text**: "X/Y spots filled"

## 10. Empty States

### Current Implementation
- **My Events**: Has empty state
- **Most others**: Missing
- **Need**: Consistent pattern

## 11. Loading States

### Current
- **Spinner in buttons**: Auth pages only
- **Missing**: Page loading, data fetching
- **Need**: Skeleton screens, consistent spinners

## Priority Components for Standardization

### High Priority (Core Components)
1. **Buttons** - Consolidate 15+ variations
2. **Form Controls** - Unify padding and borders
3. **Cards** - Standardize the 6 types
4. **Navigation** - Fix mobile inconsistencies

### Medium Priority (Frequently Used)
5. **Badges** - Consolidate color variations
6. **Modals** - Create reusable structure
7. **Tables** - Add responsive behavior
8. **Alerts** - Unify styling and behavior

### Low Priority (Less Common)
9. **Progress indicators** - Mostly consistent
10. **Empty states** - Create template
11. **Loading states** - Define patterns

## Component Naming Recommendations

### Current Issues
- Mix of naming conventions (BEM-like, functional, semantic)
- Inconsistent prefixes (btn- vs button-)
- State classes vary (.active, .selected, .is-active)

### Proposed Convention
```css
/* Component */
.component-name

/* Component variant */
.component-name--variant

/* Component state */
.component-name.is-state

/* Component child */
.component-name__child
```

### Examples
```css
.btn
.btn--primary
.btn--large
.btn.is-disabled
.btn__icon

.card
.card--event
.card__header
.card__body
```

## Next Steps

1. **Create base component CSS** with all variants
2. **Document each component** with examples
3. **Build style guide page** showing all components
4. **Update files systematically** to use new components
5. **Add missing states** (focus, disabled, loading)
6. **Test accessibility** for all interactive components