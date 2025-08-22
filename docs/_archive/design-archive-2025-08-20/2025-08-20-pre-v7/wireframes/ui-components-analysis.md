# Comprehensive UI Components Analysis for Witch City Rope

## Overview
This document provides a comprehensive analysis of all UI components found in the Witch City Rope wireframes, organized by category. Each component includes usage context and special styling requirements to facilitate mapping to Syncfusion Blazor components.

## Component Categories

### 1. Navigation Components

#### 1.1 Utility Bar
- **Usage**: Top-most navigation bar across all pages
- **Components**:
  - Links (Report Incident, Private Lessons, Contact)
  - Hover effects with underline animation
- **Styling Requirements**:
  - Dark background (#1A1A2E)
  - Uppercase text with letter spacing
  - Rose gold hover color (#B76D75)
  - Fixed position at top

#### 1.2 Main Navigation Header
- **Usage**: Primary site navigation below utility bar
- **Components**:
  - Logo (text-based, clickable)
  - Navigation menu items
  - CTA button (Login/User menu)
  - Mobile menu toggle (hamburger)
- **Styling Requirements**:
  - Sticky positioning with scroll effects
  - Backdrop blur on scroll
  - Shadow elevation changes
  - Responsive collapse to mobile menu

#### 1.3 Breadcrumb Navigation
- **Usage**: Hierarchical navigation path
- **Components**:
  - Linked segments
  - Separator icons (›)
- **Styling Requirements**:
  - Burgundy colored links
  - Gray separators

#### 1.4 Tab Navigation
- **Usage**: Content switching within pages (Login/Register, Event filters)
- **Components**:
  - Tab buttons
  - Active indicator (underline or background)
- **Styling Requirements**:
  - Smooth transitions
  - Clear active state
  - Horizontal layout

#### 1.5 Sidebar Navigation (Admin)
- **Usage**: Vertical navigation for admin dashboard
- **Components**:
  - Menu items with icons
  - Active state indicator
  - Notification badges
  - Collapsible sections
- **Styling Requirements**:
  - Left border for active state
  - Icon + text layout
  - Badge for counts

### 2. Form Components

#### 2.1 Text Input Fields
- **Usage**: User data entry
- **Variants**:
  - Standard text input
  - Email input
  - Password input
  - Number input
  - Search input
- **Styling Requirements**:
  - 2px border with rounded corners
  - Focus state with color change and shadow
  - Placeholder text styling
  - Error state styling

#### 2.2 Textarea Fields
- **Usage**: Multi-line text entry
- **Features**:
  - Resizable vertically
  - Character count display
- **Styling Requirements**:
  - Same as text inputs
  - Min-height constraint

#### 2.3 Select Dropdowns
- **Usage**: Single option selection
- **Styling Requirements**:
  - Custom styled to match inputs
  - Dropdown arrow indicator

#### 2.4 Checkboxes
- **Usage**: Multiple option selection, agreements
- **Variants**:
  - Standard checkbox
  - Checkbox with label
  - Checkbox groups
- **Styling Requirements**:
  - Custom accent color
  - Label alignment

#### 2.5 Radio Buttons
- **Usage**: Single option selection from group
- **Styling Requirements**:
  - Custom accent color
  - Group layout options

#### 2.6 Toggle Switches
- **Usage**: Binary on/off settings
- **Features**:
  - Animated transition
  - Label support
- **Styling Requirements**:
  - iOS-style sliding toggle
  - Color change on state

#### 2.7 Range Sliders
- **Usage**: Numeric value selection (sliding scale pricing)
- **Features**:
  - Value display
  - Min/max labels
- **Styling Requirements**:
  - Custom thumb styling
  - Track color

#### 2.8 File Upload
- **Usage**: Document/image uploads
- **Features**:
  - Drag and drop support
  - Progress indication
- **Styling Requirements**:
  - Dashed border for drop zone

### 3. Button Components

#### 3.1 Primary Button
- **Usage**: Main CTAs
- **Features**:
  - Gradient background
  - Hover animation (shine effect)
  - Loading state
  - Disabled state
- **Styling Requirements**:
  - Amber gradient (#FFBF00 to #E6AC00)
  - Shadow elevation
  - Uppercase text
  - Letter spacing

#### 3.2 Secondary Button
- **Usage**: Secondary actions
- **Features**:
  - Border style
  - Fill animation on hover
- **Styling Requirements**:
  - Transparent background
  - Burgundy border and text
  - White text on hover

#### 3.3 Button Sizes
- **Variants**:
  - Small
  - Default
  - Large

#### 3.4 Special Buttons
- **OAuth Button** (Google login)
- **PayPal Button**
- **Icon Buttons**

### 4. Data Display Components

#### 4.1 Cards
- **Usage**: Content containers
- **Variants**:
  - Event cards
  - Instructor cards
  - Registration cards
  - Admin dashboard cards
- **Features**:
  - Hover effects
  - Shadow elevation
  - Border highlighting
- **Styling Requirements**:
  - Rounded corners
  - White background
  - Subtle shadows

#### 4.2 Tables
- **Usage**: Data lists (admin)
- **Features**:
  - Sortable columns
  - Hover rows
  - Action buttons
- **Styling Requirements**:
  - Alternating row colors
  - Header styling
  - Sort indicators

#### 4.3 Badges/Pills
- **Usage**: Status indicators, categories
- **Variants**:
  - Event type badges
  - Member badges
  - Status badges
- **Styling Requirements**:
  - Rounded pill shape
  - Color coding by type
  - Small uppercase text

#### 4.4 Progress Indicators
- **Usage**: Multi-step processes
- **Types**:
  - Step circles with connecting lines
  - Progress bars
  - Capacity bars
- **Styling Requirements**:
  - Active/completed/pending states
  - Smooth transitions

#### 4.5 Lists
- **Usage**: Information display
- **Variants**:
  - Bulleted lists
  - Numbered lists
  - Definition lists
- **Styling Requirements**:
  - Custom bullet styles
  - Proper spacing

### 5. Feedback Components

#### 5.1 Alert/Notice Boxes
- **Usage**: Important information display
- **Variants**:
  - Info (blue/purple)
  - Warning (yellow)
  - Error (red)
  - Success (green)
- **Styling Requirements**:
  - Left border accent
  - Light background
  - Icon support

#### 5.2 Tooltips
- **Usage**: Contextual help
- **Styling Requirements**:
  - Dark background
  - Arrow pointer

#### 5.3 Loading Indicators
- **Usage**: Async operations
- **Types**:
  - Spinning circles
  - Progress bars
  - Skeleton screens
- **Styling Requirements**:
  - Brand color scheme
  - Smooth animations

#### 5.4 Success States
- **Usage**: Completion confirmation
- **Features**:
  - Large icon
  - Success message
  - Next actions
- **Styling Requirements**:
  - Centered layout
  - Green color scheme

### 6. Modal/Overlay Components

#### 6.1 Modal Dialogs
- **Usage**: Focused interactions
- **Features**:
  - Backdrop overlay
  - Close button
  - Action buttons
- **Styling Requirements**:
  - Centered positioning
  - Shadow elevation
  - Smooth transitions

#### 6.2 Dropdown Menus
- **Usage**: User menu, options
- **Styling Requirements**:
  - Shadow box
  - Hover states

### 7. Layout Components

#### 7.1 Grid Systems
- **Usage**: Responsive layouts
- **Types**:
  - Event grids
  - Card grids
  - Form grids
- **Features**:
  - Responsive breakpoints
  - Gap control

#### 7.2 Containers
- **Usage**: Content width constraints
- **Variants**:
  - Full width
  - Max-width centered
  - Fluid

#### 7.3 Sections
- **Usage**: Page organization
- **Features**:
  - Background variations
  - Padding control
- **Styling Requirements**:
  - Consistent spacing

### 8. Interactive Components

#### 8.1 Accordion/Collapsible
- **Usage**: FAQ, expandable content
- **Features**:
  - Smooth expand/collapse
  - Icon indicators

#### 8.2 Calendar Integration
- **Usage**: Date/time selection
- **Features**:
  - Date picker
  - Time picker
  - Combined datetime picker

#### 8.3 Search Components
- **Usage**: Content filtering
- **Features**:
  - Instant search
  - Search icon
  - Clear button
- **Styling Requirements**:
  - Integrated icon
  - Focus states

### 9. Specialized Components

#### 9.1 Pricing Components
- **Sliding scale selector**
- **Ticket quantity selector**
- **Price display formatting**

#### 9.2 Event-Specific Components
- **Capacity indicators**
- **Event type badges**
- **Calendar links**
- **RSVP buttons**

#### 9.3 Authentication Components
- **OAuth integration buttons**
- **2FA setup flows**
- **Password strength indicators**

#### 9.4 Admin Components
- **Data visualization charts**
- **Quick action buttons**
- **Bulk selection controls**
- **Export options**

## Typography Requirements

### Font Families
- **Display**: 'Bodoni Moda' (serif) - Headers, prices
- **Headings**: 'Montserrat' (sans-serif) - Section titles
- **Body**: 'Source Sans 3' (sans-serif) - General text
- **Accent**: 'Satisfy' (cursive) - Special callouts

### Font Sizes
- **Display**: 36-48px
- **H1**: 32-36px
- **H2**: 24-28px
- **H3**: 20px
- **Body**: 16-18px
- **Small**: 12-14px

### Font Weights
- Regular (400)
- Medium (500)
- Semibold (600)
- Bold (700)
- Extra Bold (800)

## Color System Requirements

### Primary Colors
- **Burgundy**: #880124 (primary brand)
- **Amber**: #FFBF00 (CTA accent)
- **Plum**: #614B79 (secondary)

### Neutral Colors
- **Midnight**: #1A1A2E
- **Charcoal**: #2B2B2B
- **Gray**: #666666
- **Stone**: #8B8680
- **Ivory**: #FFF8F0
- **Cream**: #FAF6F2

### Status Colors
- **Success**: #228B22
- **Warning**: #DAA520
- **Error**: #DC143C
- **Info**: #614B79

## Responsive Considerations

### Breakpoints
- **Mobile**: < 768px
- **Tablet**: 768px - 1024px
- **Desktop**: > 1024px

### Mobile Adaptations
- Collapsible navigation
- Stack layouts
- Touch-friendly tap targets (min 44px)
- Simplified tables
- Full-width forms

## Animation Requirements

### Transitions
- **Default duration**: 0.3s
- **Easing**: ease or ease-in-out
- **Properties**: color, background, transform, opacity

### Hover Effects
- Scale transforms
- Color transitions
- Shadow elevation changes
- Underline animations

### Loading States
- Spin animations
- Fade transitions
- Skeleton loading

## Accessibility Requirements

### ARIA Support
- Proper labeling
- Role attributes
- State indicators

### Keyboard Navigation
- Tab order
- Focus indicators
- Skip links

### Screen Reader Support
- Semantic HTML
- Alternative text
- Descriptive labels

## Syncfusion Component Mapping Recommendations

Based on the analysis, here are the key Syncfusion Blazor components that would be needed:

1. **Navigation**: AppBar, Menu, Breadcrumb, Tab
2. **Forms**: TextBox, NumericTextBox, DropDownList, CheckBox, RadioButton, Switch, Slider, Uploader
3. **Buttons**: Button component with custom styling
4. **Data Display**: Card, Grid, Badge, ProgressBar, ListView
5. **Feedback**: Message, Tooltip, Spinner, Dialog
6. **Layout**: Dashboard Layout, Splitter
7. **Calendar**: DatePicker, DateTimePicker, Calendar
8. **Rich Editors**: RichTextEditor for content areas

Each Syncfusion component would need custom theming to match the Witch City Rope design system, particularly the color scheme, typography, and spacing requirements.