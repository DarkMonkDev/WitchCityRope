# Phase 5 UI Design Specifications: User Dashboard & Member Features
<!-- Last Updated: 2025-09-08 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Draft -->

## Design Overview

Phase 5 focuses on creating comprehensive member dashboard functionality that serves different user roles within the WitchCityRope community. The design emphasizes personalized experiences while maintaining the established Design System v7 aesthetic with burgundy/rose gold theming.

## User Personas & Access Levels

### Primary Users
- **Admin**: Full platform management access
- **Teacher**: Event creation and management capabilities  
- **Vetted Member**: Full community access and features
- **General Member**: Basic access with limited features
- **Guest**: Limited visibility and event registration

### Design Considerations by Role
- **Progressive disclosure**: Show relevant features based on role
- **Vetting workflow**: Clear status indicators and next steps
- **Teacher tools**: Streamlined event management interface
- **Admin controls**: Comprehensive platform oversight

## Core Dashboard Components

### 1. Member Dashboard Layout

#### Desktop Layout (1200px+)
```
+--------------------------------------------------------+
| Header: Logo | Nav | User Menu                        |
+--------+-----------------------------------------------+
| Side   |  Main Dashboard Content Area                  |
| Nav    |  +------------------------------------------+ |
| [50px] |  | Welcome Section + Quick Stats            | |
|        |  +------------------------------------------+ |
| • Dash |  | Dashboard Cards Grid (2x2 or 3x2)       | |
| • Prof |  | +-------+ +-------+ +-------+            | |
| • Even |  | | Events| | Regs  | | Prof  |            | |
| • Sett |  | +-------+ +-------+ +-------+            | |
| • Help |  | +-------+ +-------+ +-------+            | |
|        |  | | Vent  | | Msgs  | | More  |            | |
|        |  | +-------+ +-------+ +-------+            | |
+--------+--+------------------------------------------+ |
| Footer Information                                   | |
+------------------------------------------------------+
```

#### Mobile Layout (375px-768px)
```
+---------------------------+
| ☰ WCR Logo    [Profile]   |
+---------------------------+
| Welcome, [Name]           |
| Status: [Vetting Level]   |
+---------------------------+
| Quick Actions            |
| [Register Event] [Profile]|
+---------------------------+
| Dashboard Cards Stack    |
| +---------------------+  |
| | Upcoming Events     |  |
| | Next: Workshop      |  |
| +---------------------+  |
| | Registration History|  |
| | 5 events attended   |  |
| +---------------------+  |
| | Profile Status      |  |
| | 75% Complete        |  |
| +---------------------+  |
+---------------------------+
```

### 2. Navigation Structure

#### Primary Navigation (Desktop Sidebar)
- **Dashboard** (Home icon) - Overview and quick access
- **Profile** (User icon) - Personal information and settings  
- **Events** (Calendar icon) - Registration history and upcoming
- **Settings** (Gear icon) - Privacy, notifications, security
- **Help** (Question icon) - Support and resources

#### Mobile Navigation (Drawer)
- Collapsible hamburger menu
- Same structure as desktop but in vertical list
- User profile summary at top of drawer

### 3. Dashboard Cards System

#### Card Specifications
**Mantine Components Used**:
- `Card` with shadow and hover effects
- `Text` for typography hierarchy
- `Badge` for status indicators
- `Button` for primary actions
- `Group` for layout alignment

#### Standard Card Template
```jsx
<Card shadow="sm" padding="lg" radius="md" withBorder>
  <Card.Section>
    <Group justify="space-between">
      <Text size="lg" fw={600}>[Card Title]</Text>
      <Badge color="wcr">[Status]</Badge>
    </Group>
  </Card.Section>
  
  <Text size="sm" c="dimmed" mt="sm">
    [Description content]
  </Text>
  
  <Button variant="light" color="wcr" fullWidth mt="md">
    [Primary Action]
  </Button>
</Card>
```

#### Dashboard Card Types

##### Events Card
- **Title**: "Upcoming Events"
- **Content**: Next 2-3 events with dates
- **Status Badge**: Registration status
- **Action**: "View All Events"
- **Icon**: Calendar icon

##### Registration History Card  
- **Title**: "Registration History"
- **Content**: Total events attended, recent activity
- **Status Badge**: Attendance rate or member level
- **Action**: "View Full History"
- **Icon**: History icon

##### Profile Status Card
- **Title**: "Profile Status"  
- **Content**: Completion percentage, required fields
- **Status Badge**: Profile completeness
- **Action**: "Update Profile"
- **Icon**: User icon

##### Vetting Status Card (General Members)
- **Title**: "Membership Status"
- **Content**: Current vetting stage, next steps
- **Status Badge**: Current status (Pending, In Progress, Complete)
- **Action**: "Complete Vetting"
- **Icon**: Shield icon

##### Teacher Tools Card (Teachers Only)
- **Title**: "Event Management"
- **Content**: Upcoming classes, registrations
- **Status Badge**: Active events count
- **Action**: "Manage Events"
- **Icon**: Teaching icon

##### Messages Card
- **Title**: "Messages"
- **Content**: Unread count, recent message preview
- **Status Badge**: Unread count
- **Action**: "View Messages"
- **Icon**: Mail icon

## Profile Management Interface

### Profile Page Layout
```
+--------------------------------------------------------+
| Breadcrumb: Dashboard > Profile                        |
+--------------------------------------------------------+
| Profile Header                                         |
| [Avatar] [Name] [Member Since] [Status Badge]         |
+--------------------------------------------------------+
| Tab Navigation                                         |
| [Personal Info] [Privacy] [Preferences] [Documents]   |
+--------------------------------------------------------+
| Tab Content Area                                       |
| +--------------------------------------------------+  |
| | Form Fields with Mantine Components             |  |
| | - TextInput for names, email                   |  |
| | - Textarea for bio                             |  |
| | - FileInput for photos/documents               |  |
| | - Switch for privacy settings                  |  |
| | - Select for preferences                       |  |
| +--------------------------------------------------+  |
| [Cancel] [Save Changes]                              |
+--------------------------------------------------------+
```

### Personal Information Tab
**Mantine Form Components**:
- `TextInput` - Name, email, phone (with validation)
- `Textarea` - Bio/introduction (character limit display)
- `FileInput` - Profile photo upload
- `DatePicker` - Birthdate (age verification)
- `Select` - Preferred pronouns, experience level
- `MultiSelect` - Interests, availability

### Privacy Tab
**Privacy Controls**:
- `Switch` components for visibility settings
- Profile photo visibility (Members only, Vetted only, Private)
- Contact information sharing
- Event history visibility
- Directory listing inclusion

### Preferences Tab
**User Preferences**:
- `Switch` for email notifications
- `Select` for communication preferences
- `Checkbox.Group` for event types of interest
- `Slider` for notification frequency
- Theme preference toggle (future dark mode)

### Documents Tab (Vetting Process)
**Document Management**:
- `FileInput` for required documents
- Document status indicators using `Badge`
- Upload progress with `Progress`
- Document verification status

## Event Registration History

### History Page Layout
```
+--------------------------------------------------------+
| Breadcrumb: Dashboard > Event History                 |
+--------------------------------------------------------+
| Filter Section                                         |
| [Status] [Date Range] [Event Type] [Clear Filters]    |
+--------------------------------------------------------+
| Statistics Row                                         |
| Total: 12 | Completed: 10 | Cancelled: 2 | Upcoming: 3|
+--------------------------------------------------------+
| Events Table/Cards                                     |
| Desktop: Table with sorting                           |
| Mobile: Cards with key information                    |
+--------------------------------------------------------+
| Pagination                                            |
+--------------------------------------------------------+
```

### Event History Table (Desktop)
**Mantine Table Component**:
```jsx
<Table>
  <Table.Thead>
    <Table.Tr>
      <Table.Th>Event</Table.Th>
      <Table.Th>Date</Table.Th>
      <Table.Th>Status</Table.Th>
      <Table.Th>Payment</Table.Th>
      <Table.Th>Actions</Table.Th>
    </Table.Tr>
  </Table.Thead>
  <Table.Tbody>
    {/* Event rows with Badge for status */}
  </Table.Tbody>
</Table>
```

### Event History Cards (Mobile)
- Stacked card layout using `Stack`
- Key information: Event name, date, status
- Quick actions: View details, cancel (if allowed)

## Settings and Preferences Screens

### Settings Page Layout
```
+--------------------------------------------------------+
| Settings Navigation (Tabs or Accordion)               |
| • Account Settings                                     |
| • Privacy & Security                                   |
| • Communication Preferences                            |
| • Accessibility                                        |
+--------------------------------------------------------+
| Settings Content                                       |
| Form fields appropriate to selected section           |
+--------------------------------------------------------+
```

### Account Settings
- Password change form with strength indicator
- Email change with verification
- Two-factor authentication toggle
- Account deletion option (with confirmation modal)

### Privacy & Security
- Login history table
- Active sessions management
- Data sharing preferences
- Account visibility controls

### Communication Preferences
- Email notification settings by category
- SMS preferences (if enabled)
- Push notification settings
- Frequency controls

## Mobile-Responsive Considerations

### Breakpoint Strategy (Mantine's system)
- **xs (0-575px)**: Single column, drawer navigation
- **sm (576-767px)**: Single column, some two-column forms
- **md (768-991px)**: Sidebar appears, card grid layout
- **lg (992-1199px)**: Full desktop experience
- **xl (1200px+)**: Wider content, more cards per row

### Mobile-Specific Patterns
- **Bottom Navigation**: Consider adding for frequent actions
- **Swipe Gestures**: For navigating between dashboard tabs
- **Pull-to-Refresh**: On main dashboard and event history
- **Touch Targets**: Minimum 44px for all interactive elements
- **Thumb Zone**: Important actions in lower third of screen

### Mobile Dashboard Modifications
- Reduce dashboard cards from 3x2 grid to 2x3 on tablet, 1x6 on mobile
- Larger touch targets for card actions
- Simplified card content with key information only
- Bottom sheet modals instead of full-page modals

## Design System Integration

### WitchCityRope Color Palette (Design System v7)
```jsx
const wcrTheme = createTheme({
  colors: {
    wcr: [
      '#f8f4e6', // ivory (lightest)
      '#e8ddd4', // warm cream
      '#d4a5a5', // dusty rose  
      '#c48b8b', // rose
      '#b47171', // deeper rose
      '#a45757', // rose gold
      '#9b4a75', // plum
      '#880124', // burgundy (primary)
      '#6b0119', // dark burgundy
      '#2c2c2c'  // charcoal (darkest)
    ]
  },
  primaryColor: 'wcr'
});
```

### Typography Hierarchy
- **Headings**: Bodoni Moda (serif) for elegance
- **Body Text**: Source Sans 3 (sans-serif) for readability
- **UI Elements**: Source Sans 3 for consistency

### Component Styling Patterns

#### Dashboard Cards
```jsx
// Card styling with WCR theme
<Card 
  shadow="sm" 
  padding="lg" 
  radius="md" 
  withBorder
  style={{
    background: 'linear-gradient(135deg, var(--mantine-color-wcr-0), var(--mantine-color-wcr-1))',
    borderColor: 'var(--mantine-color-wcr-3)'
  }}
>
```

#### Buttons
```jsx
// Primary action button
<Button 
  color="wcr" 
  variant="filled"
  size="md"
  leftSection={<IconCalendar size={16} />}
>
  Register for Event
</Button>

// Secondary action button  
<Button 
  color="wcr" 
  variant="outline"
  size="md"
>
  View Details
</Button>
```

#### Form Components
- All form inputs use `color="wcr"` for focus states
- Error states use built-in Mantine error styling
- Success states use green accent color
- Form validation with Zod integration

## Interaction Patterns

### Loading States
- **Dashboard**: Skeleton cards while loading data
- **Forms**: Button loading state during submission  
- **File Uploads**: Progress indicators with percentage
- **Page Transitions**: Smooth loading indicators

### Error Handling
- **Form Validation**: Inline error messages below inputs
- **API Errors**: Toast notifications using Mantine `Notifications`
- **Network Issues**: Retry buttons with clear messaging
- **Graceful Degradation**: Fallback content when features unavailable

### Success Feedback
- **Form Submission**: Success toast with confirmation message
- **Profile Updates**: Inline success indicators
- **File Uploads**: Checkmark animations
- **Settings Changes**: Immediate visual feedback

### Confirmation Patterns
- **Destructive Actions**: Modal confirmation dialogs
- **Form Abandonment**: Unsaved changes warning
- **Account Changes**: Email verification flows
- **Privacy Changes**: Clear impact explanations

## Accessibility Requirements

### WCAG 2.1 AA Compliance
- **Color Contrast**: 4.5:1 minimum for all text
- **Keyboard Navigation**: Full tab order through all interactive elements
- **Screen Reader Support**: Proper ARIA labels and landmarks
- **Focus Indicators**: Visible focus rings on all interactive elements

### Mantine Accessibility Features
- Built-in ARIA attributes
- Keyboard navigation support
- Screen reader announcements
- High contrast mode support

### Community-Specific Accessibility
- **Content Warnings**: For mature content
- **Consent Indicators**: Clear opt-in/opt-out controls
- **Privacy Levels**: Multiple visibility options
- **Inclusive Language**: Gender-neutral options

## Performance Considerations

### Loading Optimization
- **Lazy Loading**: Dashboard cards load progressively
- **Image Optimization**: Profile photos with responsive sizing
- **Code Splitting**: Route-based chunks for dashboard sections
- **Caching**: User data cached for offline viewing

### Mobile Performance
- **Touch Delays**: Remove 300ms click delays
- **Viewport Optimization**: Prevent layout shift
- **Resource Loading**: Critical CSS inline, non-critical deferred
- **Image Formats**: WebP with fallbacks

## Implementation Roadmap

### Phase 5.1: Core Dashboard (Week 11)
- [ ] Dashboard layout with navigation
- [ ] Basic dashboard cards implementation
- [ ] Mobile responsive navigation
- [ ] User profile basic information

### Phase 5.2: Profile Management (Week 11-12)
- [ ] Complete profile editing interface
- [ ] File upload functionality
- [ ] Privacy controls implementation
- [ ] Settings and preferences

### Phase 5.3: Event History & Polish (Week 12)
- [ ] Event registration history
- [ ] Advanced filtering and search
- [ ] Performance optimization
- [ ] Accessibility audit and fixes

### Quality Assurance Checkpoints
- [ ] Mobile responsiveness testing across devices
- [ ] Accessibility testing with screen readers
- [ ] Performance testing with Lighthouse
- [ ] Cross-browser compatibility testing
- [ ] User experience testing with role-based scenarios

## Future Enhancements

### Advanced Features (Post-Phase 5)
- **Dark Mode**: Toggle between light and dark themes
- **Advanced Analytics**: Personal usage statistics
- **Social Features**: Member directory and messaging
- **Customization**: Dashboard layout preferences
- **Notifications**: Real-time updates and alerts

### Integration Opportunities
- **Calendar Integration**: Personal calendar syncing
- **Payment History**: Financial transaction records
- **Feedback Systems**: Event rating and review features
- **Community Features**: Forums and discussion integration

---

## Technical Implementation Notes

### React Component Architecture
```jsx
// Dashboard page structure
<Dashboard>
  <DashboardHeader />
  <DashboardNavigation />
  <DashboardContent>
    <WelcomeSection />
    <DashboardCards />
  </DashboardContent>
</Dashboard>
```

### State Management
- **Zustand**: User profile and dashboard state
- **TanStack Query**: API data fetching and caching
- **Mantine Form**: Form state and validation
- **React Router**: Navigation state

### API Integration Points
- `GET /api/user/dashboard` - Dashboard summary data
- `GET /api/user/profile` - User profile information
- `PUT /api/user/profile` - Profile updates
- `GET /api/user/events` - Registration history
- `GET /api/user/settings` - User preferences

This comprehensive design specification provides the foundation for implementing Phase 5 of the WitchCityRope platform, focusing on creating an intuitive and accessible member dashboard experience that serves the diverse needs of the Salem rope bondage community.