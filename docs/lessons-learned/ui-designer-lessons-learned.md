# UI Designer Lessons Learned

## 🚨 MANDATORY STARTUP PROCEDURE 🚨

### 🚨 ULTRA CRITICAL UI/UX DOCUMENTS (MUST READ): 🚨
1. **Design System v7** - **CURRENT DESIGN STANDARDS**
`docs/design/current/design-system-v7.md`

2. **Button Style Guide** - **COMPLETE BUTTON IMPLEMENTATION GUIDE**
`docs/design/current/button-style-guide.md`

3. **UI Implementation Standards** - **COMPONENT PATTERNS**
`docs/standards-processes/ui-implementation-standards.md`

4. **React Patterns** - **REACT COMPONENT STANDARDS**
`docs/standards-processes/development-standards/react-patterns.md`

5. **Project Architecture** - **MANTINE UI FRAMEWORK**
`ARCHITECTURE.md`

### 📚 DOCUMENT DISCOVERY RESOURCES:
- **File Registry** - `docs/architecture/file-registry.md` - Find any document
- **Functional Areas Index** - `docs/architecture/functional-area-master-index.md` - Navigate features
- **Key Documents List** - `docs/standards-processes/KEY-PROJECT-DOCUMENTS.md` - Critical docs

### 📖 ADDITIONAL IMPORTANT DOCUMENTS:
- **Forms Standardization** - `docs/standards-processes/forms-standardization.md` - Form patterns
- **Workflow Process** - `docs/standards-processes/workflow-orchestration-process.md` - Handoff procedures
- **Agent Boundaries** - `docs/standards-processes/agent-boundaries.md` - What each agent does

### Validation Gates (MUST COMPLETE):
- [ ] **Read Design System v7 FIRST** - Current design language and components
- [ ] **Read Button Style Guide** - Complete button implementation patterns
- [ ] Review UI Implementation Standards for component patterns
- [ ] Check React Patterns for React-specific guidelines
- [ ] Verify Mantine v7 component library usage
- [ ] Understand mobile-first responsive design approach

## 🚨 MANDATORY: Agent Handoff Documentation Process 🚨

**CRITICAL**: This is NOT optional - handoff documentation is REQUIRED for workflow continuity.

### 📋 WHEN TO CREATE HANDOFF DOCUMENTS
- **END of UI design phase** - BEFORE implementation begins
- **COMPLETION of wireframes** - Document design decisions
- **APPROVAL from stakeholders** - Document approved designs
- **DISCOVERY of UX constraints** - Share immediately

### 📁 WHERE TO SAVE HANDOFFS
**Location**: `docs/functional-areas/[feature]/handoffs/`
**Naming**: `ui-designer-YYYY-MM-DD-handoff.md`
**Template**: `docs/standards-processes/agent-handoff-template.md`

### 📝 WHAT TO INCLUDE (TOP 5 CRITICAL)
1. **Design Decisions**: Component choices and patterns
2. **Wireframe Locations**: Paths to all mockups
3. **Mantine Components**: Specific components to use
4. **Interaction Patterns**: User flows and behaviors
5. **Responsive Breakpoints**: Mobile/tablet/desktop specs

### 🤝 WHO NEEDS YOUR HANDOFFS
- **React Developers**: Component specifications
- **Functional Spec Agents**: Design requirements
- **Test Developers**: UI test scenarios
- **Other UI Designers**: Design system consistency

### ⚠️ MANDATORY READING BEFORE STARTING
**ALWAYS READ EXISTING HANDOFFS FIRST**:
1. Check `docs/functional-areas/[feature]/handoffs/` for requirements
2. Read business requirements handoff FIRST
3. Review existing wireframes and patterns
4. Maintain design system consistency

### 🚨 FAILURE TO CREATE HANDOFFS = IMPLEMENTATION FAILURES
**Why this matters**:
- Developers implement wrong designs
- Components don't match wireframes
- UX patterns become inconsistent
- Mobile experience breaks

**NO EXCEPTIONS**: Create handoff documents or workflow WILL fail.

---

## Design System v7 Implementation - August 2025

### CRITICAL: Design System v7 Color Enforcement
**Problem**: Previous wireframes used incorrect dark color scheme instead of approved Design System v7
**Solution**:
- Use EXACT colors from approved design system:
  - Primary: #880124 (burgundy)
  - Accent: #B76D75 (rose-gold)
  - CTA Primary: #FFBF00 (amber/gold)
  - CTA Secondary: #9D4EDD (electric purple)
  - Background: #FAF6F2 (cream)
  - Cards: #FFF8F0 (ivory)
- Always copy header/footer exactly from `docs/design/current/homepage-template-v7.html`
- Apply signature animations consistently across all pages

### MANDATORY: Floating Label Animation for ALL Form Inputs
**Critical Requirement**: EVERY form input MUST use floating label animation
**Implementation**: Label starts inside input field, moves to above on focus/input
**Pattern**:
```css
.floating-input {
    padding: var(--space-md) var(--space-sm) var(--space-xs) var(--space-sm);
}
.floating-label {
    position: absolute;
    left: var(--space-sm);
    top: 50%;
    transform: translateY(-50%);
    transition: all 0.3s ease;
}
.floating-input:focus + .floating-label,
.floating-input:not(:placeholder-shown) + .floating-label,
.floating-input.has-value + .floating-label {
    top: -2px;
    transform: translateY(-50%) scale(0.8);
    color: var(--color-burgundy);
    background: var(--color-cream);
}
```
**Exception**: Only use standard inputs for dropdowns/selects that can't support floating labels

### Typography Implementation
**Rule**: Use exact font hierarchy from Design System v7:
- Headlines: 'Bodoni Moda', serif
- Titles/Nav: 'Montserrat', sans-serif
- Body: 'Source Sans 3', sans-serif
- Taglines: 'Satisfy', cursive

### Button Styling - Signature Corner Morphing (Updated October 2025)
**CRITICAL**: Primary CTA buttons use GOLD/AMBER gradient (like Dashboard button)
**Implementation**:
- Default: `border-radius: 12px 6px 12px 6px` (asymmetric)
- Hover: `border-radius: 6px 12px 6px 12px` (reverse)
- NO vertical movement (no translateY)
- **Primary CTA**: Gold/amber gradient (#FFBF00 → #FF8C00) with shimmer effect
- **Primary Alt CTA**: Electric purple gradient (#9D4EDD → #7B2CBF)
- **Secondary**: Burgundy outline with fill animation
- **Disabled**: Gray background (#8B8680), no animations, cursor: not-allowed

**Reference**: See complete button implementation guide at `docs/design/current/button-style-guide.md`

### Button States Documentation - October 2025
**Problem**: Discovered Primary CTA button (Dashboard button) was gold/amber gradient but not properly documented
**Solution**:
- Updated Design System v7 to v7.1 with correct Primary CTA description
- Added disabled button state specification to design system
- Created comprehensive Button Style Guide with all button types and states
- Added disabled state CSS to `/apps/web/src/index.css`

**Key Files Updated**:
- `docs/design/current/design-system-v7.md` - v7.1 with corrected button descriptions
- `docs/design/current/button-style-guide.md` - New comprehensive button guide
- `apps/web/src/index.css` - Added `.btn:disabled` styles (lines 627-648)

**Button Type Summary**:
1. **Primary CTA** (.btn-primary): Gold gradient, shimmer, Dashboard button
2. **Primary Alt** (.btn-primary-alt): Purple gradient, secondary CTAs
3. **Secondary** (.btn-secondary): Burgundy outline, fill animation
4. **Disabled** (:disabled): Gray, no animations, not-allowed cursor

**Always Reference**: Check Button Style Guide for visual examples, code snippets, and usage patterns

### Rich Text Editor Standard
**Required**: Use Tiptap v2 (not basic @mantine/tiptap)
**Features Must Include**:
- Bold, Italic, Headings
- Bullet/Numbered lists
- Tables support
- Image insertion
- Link insertion
- Source code view
- Quote blocks

### Navigation Animations - Center Outward Underline
**Implementation**:
```css
.nav-item::after {
    content: '';
    position: absolute;
    bottom: -4px;
    left: 50%;
    transform: translateX(-50%);
    width: 0;
    height: 2px;
    background: linear-gradient(90deg, transparent, var(--color-burgundy), transparent);
    transition: width 0.3s ease;
}
.nav-item:hover::after {
    width: 100%;
}
```

### Form Input Styling with Animations
**Standard Pattern**:
- Rose-gold border by default
- Focus: burgundy border + rose-gold glow + translateY(-2px)
- Rounded corners (12px)
- Cream background
- Stone placeholder text

### Preserving Existing Designs
**Rule**: When asked NOT to redesign (like check-in interface), preserve:
- Original functionality and structure
- All existing tabs/sections
- Layout patterns
- Only update colors, typography, and animations to match v7

### Mobile Responsiveness Standards
**Breakpoint**: 768px
**Pattern**:
- Hide desktop nav, show mobile-friendly layout
- Stack grid layouts to single column
- Adjust padding from 40px to 20px
- Scale fonts appropriately
- Maintain signature animations on mobile

## Navigation Updates Pattern - September 2025

### CRITICAL: Maintain ALL Existing Styling and Animations
**Problem**: When updating navigation components, risk of breaking existing visual patterns
**Solution**:
- NEVER modify existing CSS animations or styling patterns
- ADD new elements using existing patterns, don't change existing ones
- Preserve all existing class names and animation behaviors
- Maintain exact color values and spacing variables

### User State Integration in Navigation
**Pattern**: Navigation updates based on authentication state:
- **Logged Out**: Show "Login" button in main nav
- **Logged In (Member)**: Show "Dashboard" CTA button, user greeting in utility bar, logout link
- **Logged In (Admin)**: Add "Admin" nav link + all member features

**Implementation Strategy**:
```typescript
// Use existing auth store selectors
const user = useUser();
const isAuthenticated = useIsAuthenticated();
const { logout } = useAuthActions();

// Role-based conditional rendering
const isAdmin = user?.roles?.includes('Administrator') || false;
```

### Utility Bar Layout Pattern
**Solution**: Use space-between layout for user greeting + existing links:
```jsx
<Group justify="space-between" gap="var(--space-lg)">
  {/* Left: User greeting */}
  {isAuthenticated && user ? (
    <Box className="utility-bar-user-greeting">
      Welcome, {user.sceneName}
    </Box>
  ) : <Box />}

  {/* Right: Existing links + logout */}
  <Group gap="var(--space-lg)">
    {/* All existing utility bar links */}
    {isAuthenticated && <LogoutLink />}
  </Group>
</Group>
```

### Main Navigation Extension Pattern
**Solution**: Insert new elements maintaining existing order and styling:
- Logo (unchanged)
- Admin link (new - uses existing .nav-underline-animation)
- Events & Classes (unchanged)
- How to Join (unchanged)
- Resources (unchanged)
- Dashboard CTA (replaces Login button)

**Key Rule**: New navigation items MUST use existing animation classes:
```css
.nav-underline-animation /* For all nav links */
.btn-primary /* For CTA buttons */
.utility-bar-link /* For utility bar items */
```

### Mobile Responsive Preservation
**Rule**: Mobile navigation changes must preserve existing mobile patterns:
- Burger menu toggle behavior (unchanged)
- Desktop nav hiding at 768px (unchanged)
- Utility bar scaling and font adjustments (preserved)
- Touch target sizes maintained

### State Change Handling
**Pattern**: Authentication state changes trigger UI updates without page reload:
1. Login → Dashboard CTA appears, user greeting added, logout link added, admin link appears (if admin)
2. Logout → Reverts to login button, removes user greeting, removes admin link
3. Uses existing auth store actions and navigation routing

## Events Management Specific Patterns

### Event Cards - Hover Animation
**Implementation**: `translateY(-4px)` on hover with increased shadow
**Used in**: public-events-list.html, admin dashboard

### Check-in Interface - Status Indicators
**Pattern**: Use design system colors for status:
- Success: #228B22 (checked-in)
- Warning: #DAA520 (waitlist)
- Error: #DC143C (issues)

### Form Sections - Hierarchical Styling
**Pattern**:
- Section titles with rose-gold bottom border
- Grouped form elements in ivory containers
- Proper spacing hierarchy using CSS variables

### Event Session Matrix Pattern - Complex Ticket Management
**Problem**: Need to support multi-day events with different ticket types (series passes, individual days, etc.)
**Solution**: Event Session Matrix approach implemented in event-form.html
**Key Components**:

1. **Event Sessions Section**: Define individual sessions (Day 1, Day 2, etc.)
   - Session cards with date, time, capacity
   - Dynamic add/remove functionality
   - Editable session names (e.g., "Day 1: Fundamentals")

2. **Ticket Types Section**: Multiple ticket types referencing sessions
   - Series passes include multiple sessions
   - Individual session tickets
   - Clear session inclusion display (✓ Day 1 ✓ Day 2)
   - Price ranges, quantities, sale periods

3. **Capacity Overview Sidebar**: Real-time capacity visualization
   - Progress bars for each session
   - Color-coded warnings (burgundy → warning → error)
   - Clear capacity allocation display
   - Sticky positioning for reference

4. **Template Selection**: Quick setup patterns
   - Single Session Event
   - Multi-Day Series
   - Tiered Pricing

**CSS Classes**:
```css
.session-card, .ticket-type-card {
    background: white;
    border: 2px solid var(--color-rose-gold);
    border-radius: 12px;
    transition: all 0.3s ease;
}
.session-card:hover, .ticket-type-card:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(136, 1, 36, 0.15);
}
.capacity-overview {
    background: var(--color-ivory);
    position: sticky;
    top: var(--space-md);
}
```

**Layout Pattern**:
- Two-column layout: main content + sidebar
- Responsive: stack on mobile
- Grid template: `1fr 300px` → `1fr` on mobile

### Public-Facing Event Session Matrix - Customer Experience
**Problem**: Customers need to understand multi-session events and select appropriate tickets
**Solution**: Smart capacity display and ticket selection interface

**Key Patterns**:

1. **Event Details Page - Registration Card**:
   - **Capacity Warning**: Highlight bottleneck sessions prominently
   - **Session Availability Display**: Show per-session capacity with color coding
   - **Ticket Type Selection**: Interactive cards showing included sessions
   - **Smart Unavailability**: Gray out tickets when constituent sessions are full
   - **Dynamic Button Text**: Update based on selected ticket type

2. **Events List - Smart Spots Display**:
   - **Single Session**: Standard "8/20" format
   - **Multi-Session**: Show bottleneck with context "5/20 (Day 2)"
   - **Card Badges**: Visual indicators for series events
   - **Table View**: Include constraint info in spots column

**CSS Patterns**:
```css
.multi-session-spots {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
}
.spots-constraint {
    font-size: 12px;
    font-style: italic;
    color: var(--color-stone);
}
.ticket-type-option.unavailable {
    opacity: 0.6;
    cursor: not-allowed;
    background: var(--color-stone);
}
.capacity-warning {
    background: linear-gradient(135deg, rgba(220, 20, 60, 0.1), rgba(218, 165, 32, 0.1));
    border: 2px solid var(--color-warning);
}
```

**UX Benefits**:
- Clear understanding of what's included in each ticket
- Immediate visibility of capacity constraints
- Prevents confusion about which sessions are available
- Smart warnings guide purchase decisions

## Admin Dashboard Activation Pattern - September 2025

### CRITICAL: Minimal Viable Connection Strategy
**Problem**: Need to activate existing AdminEventsPage without building complex new systems
**Solution**:
- Create simple landing dashboard that connects to existing functionality
- Use existing components and patterns where possible
- Focus on navigation connections rather than new features
- Reuse DashboardLayout for consistency

### Admin Dashboard Design Principles
**Key Rules**:
- **Preserve existing AdminEventsPage exactly** - no changes to working functionality
- **Use established Design System v7 colors** from existing AdminEventsPage (#880124, #FAF6F2, #FFF8F0)
- **Follow existing typography patterns** (Montserrat headers, consistent sizing)
- **Reuse proven animation patterns** (translateY(-2px) card hovers, color transitions)
- **Maintain mobile responsiveness** at 768px breakpoint

### Component Reuse Strategy
**Pattern**: Extract common patterns from AdminEventsPage for dashboard components
```typescript
// Stats card styling matches AdminEventsPage Paper styling
const statsCardStyle = {
  background: '#FFF8F0',
  padding: '24px',
  borderLeft: '4px solid #880124',
  transition: 'all 0.3s ease'
};

// Action tile hover matches existing card hover
const actionTileHover = {
  transform: 'translateY(-2px)',
  boxShadow: '0 4px 12px rgba(0,0,0,0.1)'
};
```

### Layout Integration Options
**Option A (Recommended)**: Extend DashboardLayout with admin menu items
- Role-based menu rendering using existing auth store
- Single layout component, cleaner navigation experience
- Consistent user experience between member and admin functions

**Option B**: Separate AdminLayout
- Duplicate layout code but cleaner separation
- Admin-specific sidebar design
- More complex to maintain

### Navigation Flow Optimization
**Problem**: Admin link in Navigation leads nowhere
**Solution**:
1. Admin link → `/admin` dashboard (overview page)
2. Dashboard → action tiles lead to specific admin sections
3. Primary "Manage Events" tile → existing `/admin/events` page
4. Maintain all existing routing and functionality

### Mobile-First Admin Dashboard
**Implementation**:
- Stats cards: 3-column grid → 1-column stack on mobile
- Action tiles: 2x3 grid → 1-column stack on mobile
- Touch-friendly targets (48px minimum)
- Consistent with existing DashboardLayout mobile behavior
- No changes to existing AdminEventsPage mobile responsiveness

### Future-Proof Architecture
**Design for growth**:
- Action tiles can easily link to new admin sections
- Stats cards can be made dynamic with real API data
- Component structure supports additional admin functionality
- Maintains separation between dashboard overview and specific admin tools

## Safety System UI Design Patterns - September 2025

### CRITICAL: Legal Compliance Design Requirements
**Problem**: Safety incident reporting system requires strict privacy protection and legal compliance
**Solution**:
- Anonymous reporting must have NO user identification tracking
- Severity-based color coding and alert escalation
- Encryption requirements for all sensitive user content
- Complete audit trail logging for legal compliance

### Privacy-First Design Patterns
**Anonymous Reporting**:
- Radio button toggle between anonymous and identified reporting
- When anonymous selected, all contact fields disappear
- Clear privacy notices explaining data protection
- No session tracking or IP logging for anonymous reports

**Data Sensitivity Indicators**:
- 🔒 icons for encrypted fields
- Privacy notices prominently displayed
- Color-coded severity levels with specific meanings
- Clear explanation of who can access what information

### Severity-Based Color System
**Implementation**: Specific colors for incident severity (NOT generic status colors):
```css
.severity-low { color: #228B22; background: rgba(34, 139, 34, 0.1); }
.severity-medium { color: #DAA520; background: rgba(218, 165, 32, 0.1); }
.severity-high { color: #DC143C; background: rgba(220, 20, 60, 0.1); }
.severity-critical { color: #8B0000; background: rgba(139, 0, 0, 0.1); }
```

### Legal Compliance UI Elements
**Required Components**:
- Audit trail timeline with user attribution
- Encrypted data indicators (lock icons)
- Role-based access control visual feedback
- Reference number generation and display
- Status tracking with timestamp history

### Form Design for Sensitive Data
**Pattern**: Floating labels with encryption indicators
- ALL text inputs use floating label animation
- Sensitive fields marked with 🔒 icon
- Clear field descriptions explaining data use
- Optional vs required field distinction
- Privacy toggle controls prominently placed

### Admin Safety Dashboard Patterns
**Layout Strategy**:
- Statistics cards showing incident counts by severity
- Color-coded urgency indicators
- Real-time alert badges for critical incidents
- Filter controls for incident management
- Export controls with privacy protection

**Table Design**:
- Severity badges with color coding
- Encrypted data indicators
- Quick action buttons (View, Assign, Update)
- Sortable columns with proper data formatting
- Reference number prominence for tracking

### Mobile Safety Form Optimization
**Critical Requirements**:
- Single column layout on mobile
- Touch-friendly form controls (44px minimum)
- Emergency contact information easily accessible
- Severity selection with large, clear options
- Submit button always visible (sticky positioning)

### Error Handling for Sensitive Forms
**Pattern**: Non-revealing error messages
- Generic "submission failed" rather than specific validation errors
- Success confirmations with tracking numbers
- Clear retry instructions without exposing sensitive data
- Graceful degradation for network issues

**Implementation**:
```jsx
// Safe error messaging
const handleSubmissionError = (error) => {
  showNotification({
    title: 'Submission Issue',
    message: 'Unable to submit report. Please try again or contact safety team directly.',
    color: 'red'
  });
  // Log detailed error privately, don't expose to user
};
```

### Accessibility for Emergency Situations
**Requirements**: Enhanced accessibility for users in crisis
- High contrast mode available
- Large text options
- Voice-to-text support indicators
- Screen reader optimized form structure
- Keyboard navigation priority

**Crisis-Appropriate Design**:
- Minimal cognitive load in form design
- Clear progress indicators
- Emergency contact information always visible
- Submit button prominence with confirmation
- Success state with clear next steps

## CheckIn System Mobile-First Design Patterns - September 2025

### CRITICAL: Mobile-First Volunteer Interface Requirements
**Problem**: Event check-in performed by volunteer staff using phones/tablets at venue entrances with poor WiFi
**Solution**:
- Touch-optimized interface with 44px+ targets
- Offline capability with local data storage
- Simple volunteer workflow requiring minimal training
- Quick processing for lines of 30+ attendees

### Touch Optimization Standards
**Touch Target Requirements**:
- **Minimum Size**: 44px × 44px for all interactive elements
- **Button Spacing**: 8px minimum between touch targets
- **Primary Actions**: 48px height, full-width on mobile
- **Search Input**: 56px height for comfortable typing

**Gesture Support Patterns**:
```jsx
// Swipe-to-refresh for attendee list
const SwipeRefresh = () => (
  <div
    className="swipe-refresh"
    onTouchStart={handleTouchStart}
    onTouchMove={handleTouchMove}
    onTouchEnd={handleRefresh}
  >
    <AttendeeList />
  </div>
);

// Long-press for additional options
const LongPressActions = () => (
  <div
    onTouchStart={startLongPress}
    onTouchEnd={clearLongPress}
    onContextMenu={showActions}
  >
    <AttendeeCard />
  </div>
);
```

### Offline Capability Design Patterns
**Local Storage Strategy**:
```typescript
interface OfflineData {
  eventId: string;
  attendees: Attendee[];
  checkIns: CheckInRecord[];
  lastSync: timestamp;
  pendingActions: PendingAction[];
}

// Sync when connection restored
const syncPendingCheckIns = async () => {
  const pending = getPendingActions();
  for (const action of pending) {
    try {
      await submitCheckIn(action);
      removePendingAction(action.id);
    } catch (error) {
      markActionFailed(action.id);
    }
  }
};
```

**Offline UI Indicators**:
- Connection status prominent in header
- Pending actions badge showing unsynced check-ins
- Manual sync trigger when connection restored
- Data freshness timestamp display

### Volunteer-Friendly Interface Patterns
**Simplified Check-In Flow**:
1. Search attendee by name/email
2. Select from filtered list
3. Review basic attendee info
4. Single "CHECK IN" button
5. Success confirmation with visual feedback
6. Return to search automatically

**Error Prevention**:
- Large, clear status indicators
- Color-coded attendee states (checked-in, expected, waitlist)
- Confirmation dialogs for critical actions
- Undo capability for accidental check-ins

### Performance Optimization for Mobile
**Battery Life Considerations**:
- Screen wake lock during check-in periods
- Reduced animations option for battery saving
- Efficient polling intervals based on event activity
- Minimal background processing

**Memory Management**:
- Virtual scrolling for large attendee lists (100+ people)
- Lazy loading of attendee details
- Image compression for attendee photos
- Local cache management with size limits

### Responsive Breakpoint Strategy
**Mobile Portrait (375px)**:
- Single column layout
- Full-width buttons
- Compressed cards with essential info only
- Sticky header with event info
- Bottom actions in thumb-reach zone

**Mobile Landscape (667px)**:
- Two-column: search + attendee list
- Horizontal scrolling table view
- Compact header design
- Side panel for attendee details

**Tablet (768px+)**:
- Three-column: search, list, details/overview
- Full table view with all information
- Modal dialogs for confirmations
- Multi-select capabilities

### Visual Feedback Patterns
**Check-In Success Animation**:
```jsx
const CheckInSuccess = ({ attendee }) => (
  <motion.div
    initial={{ scale: 0.8, opacity: 0 }}
    animate={{ scale: 1, opacity: 1 }}
    transition={{ duration: 0.5, ease: "easeOut" }}
    style={{
      backgroundColor: '#228B22',
      borderRadius: '12px',
      padding: '24px',
      textAlign: 'center',
      color: 'white'
    }}
  >
    <motion.div
      animate={{ scale: [1, 1.2, 1] }}
      transition={{ duration: 0.6, times: [0, 0.5, 1] }}
    >
      ✅
    </motion.div>
    <Text size="xl" weight={600}>Check-In Successful</Text>
    <Text>{attendee.name}</Text>
  </motion.div>
);
```

**Status Badge System**:
```jsx
const StatusBadge = ({ status, count }) => {
  const statusConfig = {
    'checked-in': { color: '#228B22', icon: '✅', label: 'Checked In' },
    'expected': { color: '#DAA520', icon: '⏳', label: 'Expected' },
    'waitlist': { color: '#DC143C', icon: '⚠️', label: 'Waitlist' },
    'no-show': { color: '#8B8680', icon: '❌', label: 'No Show' }
  };

  const config = statusConfig[status];

  return (
    <Badge
      color={config.color}
      variant="filled"
      size="lg"
      style={{
        borderRadius: '12px 6px 12px 6px',
        fontFamily: 'Montserrat, sans-serif',
        fontWeight: 600,
        textTransform: 'uppercase',
        letterSpacing: '0.5px'
      }}
    >
      {config.icon} {config.label} {count && `(${count})`}
    </Badge>
  );
};
```

### Accessibility for Venue Conditions
**Outdoor/Variable Lighting**:
- High contrast mode toggle
- Large text options (150% scaling)
- Color-independent status indicators
- Dark mode for evening events

**Emergency Accessibility**:
- Voice commands for hands-free operation
- Screen reader optimization
- External switch support
- Quick emergency contact access

## Stakeholder Feedback Integration - September 2025

### CRITICAL: Design Simplification Based on Stakeholder Feedback
**Problem**: Initial Safety System design included unnecessary complexity that didn't match stakeholder needs
**Solution**: Streamline designs based on actual business requirements

### Key Feedback Patterns
**What Stakeholders Actually Need**:
- Simple severity categorization (not incident types)
- Email notifications only (no SMS complexity)
- Long-term database retention for legal compliance
- Focus on core functionality over feature richness

**What to Remove Immediately**:
- Complex categorization systems
- Multiple notification channels
- Overly detailed workflows
- Features mentioned but not actually needed

### Implementation of Feedback
**Before Stakeholder Review**:
- Incident type dropdowns and categorization
- SMS notification options
- Complex data retention policies
- Multiple user role definitions

**After Stakeholder Feedback**:
- Simple severity levels only (Low/Medium/High/Critical)
- Email notifications only
- Database records permanent, only temp data has shorter retention
- Focus on current roles (Teacher/Event Coordinator noted for future)

### Communication Strategy for Complex Features
**Lesson**: Present complex features incrementally with stakeholder validation
**Process**:
1. Start with minimal viable design
2. Show core functionality first
3. Present additional features as optional extensions
4. Get explicit approval before adding complexity
5. Document what was simplified and why

### Design Iteration Based on Feedback
**Pattern**: Rapid iteration with clear change documentation
```markdown
### Version 2.0 Changes
**Removed**: [List specific elements]
**Simplified**: [List simplified workflows]
**Updated**: [List clarified requirements]
```

### Future Stakeholder Review Process
**Lesson**: Regular check-ins prevent over-engineering
**Implementation**:
1. Present wireframes at 50% completion
2. Validate core workflows before details
3. Confirm feature scope before implementation
4. Document all feedback and changes
5. Update lessons learned immediately

## Critical Stakeholder Corrections Integration - September 2025

### CRITICAL: Immediate Response to Stakeholder Feedback
**Problem**: Stakeholders provide urgent corrections that must be applied immediately
**Solution**: Systematic correction process with clear documentation of changes

### Stakeholder Correction Process
**Lesson**: When stakeholders provide critical corrections, follow structured approach
**Process**:
1. **STOP ALL OTHER WORK** - Corrections are highest priority
2. **READ ALL CORRECTIONS** carefully before starting changes
3. **APPLY SYSTEMATICALLY** - Don't rush, check each correction thoroughly
4. **DOCUMENT CHANGES** clearly with before/after comparisons
5. **UPDATE VERSION NUMBERS** to indicate corrections applied
6. **MARK AS APPROVED** only after all corrections completed

### RSVP and Ticketing Corrections Applied (2025-09-19)

#### 1. **TERMINOLOGY CORRECTION**
**Before**: Mixed usage of "register" and "RSVP" throughout documents
**After**: ALL instances of "register" replaced with "RSVP"
**Applied to**: Component names, function names, UI text, all documentation
**Files Updated**: wireframe-analysis.md, ui-specifications.md, handoff document

#### 2. **RSVP FLOW CORRECTION**
**Before**: Section 3 showed "RSVP Only Flow" without ticket purchase option
**After**: Social events ALWAYS have option to purchase ticket after RSVP
**Critical Clarification**: There is NO "RSVP only" flow - ticket purchase is ALWAYS available
**Impact**: User flows, confirmation modals, success pages all updated

#### 3. **BUTTON CSS FIX**
**Before**: Custom Mantine Button styling with inline styles causing text cutoff
**After**: Standard CSS classes (`.btn .btn-primary`) used to prevent text cutoff
**Reference**: `/docs/lessons-learned/react-developer-lessons-learned.md`
**Technical Solution**: Use existing CSS classes in `/apps/web/src/index.css`

#### 4. **EVENT DETAIL PAGE CLARIFICATION**
**Before**: Unclear distinction between social events and classes
**After**:
- **Social Events**: Right panel shows "RSVP" button FIRST, THEN optional "Purchase Ticket"
- **Classes**: Right panel shows ticket price selector and "Purchase Ticket" button
**Clarification**: These are DIFFERENT UI components for each event type

### Correction Implementation Strategy
**Documentation Pattern**:
```markdown
**🚨 CRITICAL STAKEHOLDER CORRECTIONS APPLIED**:
- **TERMINOLOGY**: All instances of "register" replaced with "RSVP"
- **FLOW CORRECTION**: Social events show RSVP button FIRST, then optional ticket purchase
- **BUTTON FIX**: Standard Mantine Button components used to prevent text cutoff
- **UI CLARIFICATION**: Different components for social events (RSVP + ticket) vs classes (ticket purchase)
```

**Version Control Pattern**:
- Increment version numbers when corrections applied
- Update status from "Draft" to "APPROVED" after corrections
- Add correction summary at top of documents
- Maintain change history for future reference

### Communication Strategy for Corrections
**Immediate Response Required**:
- Acknowledge receipt of corrections immediately
- Provide timeline for completion (usually same day)
- Ask clarifying questions if any corrections are unclear
- Confirm completion with summary of changes made

**Documentation Requirements**:
- Document what was changed and why
- Include before/after examples where helpful
- Update lessons learned with new patterns discovered
- Share completed corrections with team for review

### Quality Assurance for Corrections
**Validation Checklist**:
- [ ] All requested terminology changes applied consistently
- [ ] Flow diagrams updated to match corrected requirements
- [ ] Technical implementations reference correct standards
- [ ] UI clarifications include specific component differences
- [ ] Version numbers and status updated
- [ ] Correction summary included in all updated documents

### Prevention Strategies
**Lesson**: Prevent need for corrections by better stakeholder communication
**Future Process**:
1. **Regular Check-ins**: Schedule stakeholder reviews at 25%, 50%, 75% completion
2. **Terminology Validation**: Confirm preferred terms early in design process
3. **Flow Validation**: Walk through user flows with stakeholders before detailed design
4. **Technical Validation**: Confirm implementation approaches before specifying

**Early Warning Signs**:
- Stakeholder questions about terminology choices
- Requests for clarification on user flows
- Technical implementation concerns raised
- Multiple interpretation possibilities for requirements

This systematic approach to stakeholder corrections ensures rapid response while maintaining quality and documentation standards.

## Quality Validation Checklist

### Pre-Delivery Validation
- [ ] All colors match Design System v7 exactly
- [ ] Header/footer copied from homepage template v7
- [ ] Signature animations implemented (nav underlines, button morphing)
- [ ] Typography uses correct font families
- [ ] ALL form inputs use floating label animation
- [ ] Rich text areas use Tiptap v2 with full toolbar
- [ ] Primary CTA buttons use gold/amber gradient (NOT purple)
- [ ] Secondary buttons use burgundy outline
- [ ] Disabled buttons show gray background with no animations
- [ ] Input styling matches approved patterns
- [ ] Mobile responsive at 768px breakpoint
- [ ] No vertical button movement (translateY)
- [ ] Rose-gold bottom border on header
- [ ] Consistent spacing using CSS variables
- [ ] Button Style Guide reviewed for all button implementations

### Animation Requirements
- [ ] Navigation center-outward underline animation
- [ ] Button asymmetric corner morphing (no translateY)
- [ ] Primary button shimmer effect on hover
- [ ] Secondary button fill animation on hover
- [ ] Card hover elevation (-4px translateY)
- [ ] Input focus animations (translateY + glow)
- [ ] Logo hover scale + underline
- [ ] Floating labels on ALL text inputs
- [ ] Disabled buttons have NO animations

### Complex Form Validation
- [ ] Event Session Matrix pattern correctly implemented
- [ ] Capacity overview shows real-time calculations
- [ ] Session-ticket relationships clearly displayed
- [ ] Template selection provides appropriate defaults
- [ ] Mobile responsiveness for complex layouts

### Public-Facing Session Matrix Validation
- [ ] Multi-session events show bottleneck sessions clearly
- [ ] Ticket selection interface is intuitive
- [ ] Unavailable tickets are properly disabled with explanations
- [ ] Capacity warnings are prominent and actionable
- [ ] Button text updates based on selection
- [ ] Mobile responsive ticket selection

### Navigation Update Validation
- [ ] All existing animations preserved exactly
- [ ] Authentication state changes trigger proper UI updates
- [ ] Role-based rendering works correctly (Admin link for administrators only)
- [ ] Mobile responsive behavior maintained
- [ ] Utility bar layout uses space-between pattern
- [ ] User greeting appears on LEFT side of utility bar
- [ ] Logout link appears on RIGHT side of utility bar (after Contact)
- [ ] Dashboard CTA replaces Login button when authenticated
- [ ] All existing CSS class names and behaviors unchanged

### Admin Dashboard Activation Validation
- [ ] Existing AdminEventsPage functionality completely preserved
- [ ] Navigation flow: Admin link → dashboard → events management works
- [ ] Colors and typography match existing AdminEventsPage patterns
- [ ] Mobile responsive at existing 768px breakpoint
- [ ] Action tiles use established hover animations
- [ ] Stats cards follow existing Paper styling patterns
- [ ] Layout integration maintains existing user experience
- [ ] No duplicate code or conflicting styles

### Safety System Design Validation
- [ ] Anonymous reporting path completely separate from identified reporting
- [ ] Severity levels use specific safety colors (not generic status colors)
- [ ] All sensitive data fields marked for encryption in designs
- [ ] Privacy notices prominent and legally compliant
- [ ] Audit trail design supports complete action logging
- [ ] Role-based access control visually indicated
- [ ] Mobile responsive safety forms with touch optimization
- [ ] Emergency accessibility features included
- [ ] Error handling preserves data privacy

### CheckIn System Mobile-First Validation
- [ ] Touch targets minimum 44px, preferred 48px for primary actions
- [ ] Offline capability designed with local storage and sync indicators
- [ ] Volunteer-friendly interface with minimal training requirements
- [ ] Quick processing flow optimized for lines of 30+ attendees
- [ ] Battery life considerations implemented
- [ ] Connection status clearly displayed
- [ ] Success animations provide clear feedback
- [ ] Status badges use consistent color coding
- [ ] Swipe gestures with button fallbacks
- [ ] High contrast mode for variable lighting conditions

### Stakeholder Feedback Integration Validation
- [ ] Simplified designs match actual business requirements
- [ ] Removed unnecessary complexity based on feedback
- [ ] Core functionality prioritized over feature richness
- [ ] Email notifications only (no SMS)
- [ ] Severity categorization only (no incident types)
- [ ] Long-term database retention clarified
- [ ] Change documentation complete and clear

### Critical Stakeholder Corrections Validation (Added 2025-09-19)
- [ ] ALL "register" terminology replaced with "RSVP" consistently
- [ ] Social events ALWAYS show ticket purchase option (no "RSVP only" flow)
- [ ] Button components use standard CSS classes to prevent text cutoff
- [ ] Clear distinction between social event UI (RSVP + ticket) vs class UI (ticket purchase)
- [ ] Version numbers updated to reflect corrections applied
- [ ] Documents marked as APPROVED after corrections
- [ ] Correction summary included in all updated documents

### Button Implementation Validation (Added 2025-10-08)
- [ ] Primary CTA uses gold/amber gradient (#FFBF00 → #FF8C00)
- [ ] Primary Alt uses electric purple gradient (#9D4EDD → #7B2CBF)
- [ ] Secondary uses burgundy outline with fill animation
- [ ] Disabled state shows gray background, no animations, not-allowed cursor
- [ ] All button states have correct corner morphing animation
- [ ] Button Style Guide referenced for implementation
- [ ] Visual examples match design system documentation

## File Organization

### Wireframe Storage Pattern
**Location**: `docs/functional-areas/[feature]/new-work/[date]/design/wireframes/`
**Naming**: Descriptive names like `admin-events-dashboard.html`
**Structure**: Each wireframe is complete standalone HTML with embedded CSS

### Design System Reference
**Source of Truth**: `docs/design/current/design-system-v7.md`
**Button Guide**: `docs/design/current/button-style-guide.md`
**Template**: `docs/design/current/homepage-template-v7.html`
**Always verify against these files before finalizing designs**

### Admin Dashboard Documentation
**Location**: `docs/functional-areas/events/admin-activation/`
**Naming**: Date-stamped design documents
**Include**: ASCII wireframes, component specs, navigation flows, implementation priorities

### Safety System Documentation
**Location**: `docs/functional-areas/api-cleanup/new-work/[date]/design/`
**Naming**: `safety-system-ui-design.md`
**Include**: Complete UI specifications, privacy requirements, legal compliance notes

### CheckIn System Documentation
**Location**: `docs/functional-areas/api-cleanup/new-work/[date]/design/`
**Naming**: `checkin-system-ui-design.md`
**Include**: Mobile-first wireframes, touch optimization, offline capability, volunteer workflow design

## Common Mistakes to Avoid

1. **DON'T** use dark themes - Design System v7 is light with cream backgrounds
2. **DON'T** create custom colors - use exact hex values from design system
3. **DON'T** add vertical button movement - buttons only morph corners
4. **DON'T** skip the utility bar and footer from template
5. **DON'T** forget mobile responsive breakpoints
6. **DON'T** use wrong typography - each element has specific font family
7. **DON'T** redesign when asked to preserve existing functionality
8. **DON'T** use purple for primary CTA buttons - gold/amber gradient only
9. **DON'T** skip floating label animations on form inputs
10. **DON'T** use basic text editors - require Tiptap v2 with full features
11. **DON'T** forget sticky positioning for capacity overview sidebars
12. **DON'T** neglect session-ticket relationship clarity in complex forms
13. **DON'T** show generic capacity for multi-session events - always show the constraint
14. **DON'T** allow ticket purchases when constituent sessions are full
15. **DON'T** modify existing animation CSS when adding navigation updates
16. **DON'T** change existing component structure - only ADD new elements
17. **DON'T** break mobile responsive patterns when updating navigation
18. **DON'T** forget to use existing auth store selectors for state management
19. **DON'T** rebuild working functionality - activate with connection points
20. **DON'T** create duplicate layout components when existing ones can be extended
21. **DON'T** change existing AdminEventsPage styling or behavior
22. **DON'T** ignore existing component patterns when building admin dashboard
23. **DON'T** mix anonymous and identified reporting data patterns
24. **DON'T** use generic status colors for safety incident severity levels
25. **DON'T** expose sensitive data in error messages or debugging
26. **DON'T** skip privacy notices and encryption indicators for sensitive forms
27. **DON'T** forget crisis-appropriate accessibility features for emergency forms
28. **DON'T** over-engineer designs without stakeholder validation
29. **DON'T** include incident type categorization (stakeholders don't use this)
30. **DON'T** design SMS notification systems (email only required)
31. **DON'T** assume 2-year data retention for all data (only temp data, database records permanent)
32. **DON'T** design touch targets smaller than 44px for mobile interfaces
33. **DON'T** ignore offline capability requirements for venue-based systems
34. **DON'T** create complex volunteer interfaces requiring extensive training
35. **DON'T** forget battery life optimization for mobile-first applications
36. **DON'T** skip connection status indicators for offline-capable apps
37. **DON'T** use complex gestures without button alternatives
38. **DON'T** ignore urgent stakeholder corrections - they are highest priority
39. **DON'T** mix "register" and "RSVP" terminology - always use "RSVP"
40. **DON'T** create "RSVP only" flows - ticket purchase must always be available
41. **DON'T** use custom Mantine Button styling - use standard CSS classes
42. **DON'T** assume event types have same UI - social events and classes are different
43. **DON'T** forget disabled button state - gray background, no animations
44. **DON'T** implement buttons without checking Button Style Guide first

## Stakeholder Communication

### Design Approval Process
**Lesson**: Stakeholders were frustrated with incorrect designs that didn't match approved system
**Solution**: Always validate against design system before presenting
**Process**:
1. Review design system documentation
2. Implement exact colors and patterns
3. Test animations and interactions
4. Validate mobile responsiveness
5. Present with confidence that it matches approved standards

### Design Preservation Communication
**Lesson**: When asked to preserve existing styling, stakeholders expect ZERO visual changes to existing elements
**Solution**: Clearly communicate what will be preserved vs what will be added
**Process**:
1. Identify all existing visual elements and animations
2. Document exactly what will be preserved
3. Show new elements using existing patterns
4. Demonstrate responsive behavior maintained
5. Confirm no existing user workflows disrupted

### Minimal Viable Design Communication
**Lesson**: Stakeholders prefer activation of existing functionality over complex new features
**Solution**: Focus on connection points rather than feature additions
**Process**:
1. Identify what already works and needs no changes
2. Design minimal connecting components using established patterns
3. Show clear navigation flow to existing functionality
4. Emphasize preservation of working systems
5. Present implementation as low-risk activation rather than new development

### Legal Compliance Communication
**Lesson**: Safety system design requires explicit legal compliance documentation
**Solution**: Document privacy protection and legal requirements prominently
**Process**:
1. Highlight anonymous reporting capabilities
2. Document encryption requirements clearly
3. Show audit trail and access control designs
4. Emphasize crisis-appropriate accessibility features
5. Present as legal risk mitigation rather than optional feature

### Feature Simplification Communication
**Lesson**: Stakeholders clarify actual needs vs assumed requirements during design review
**Solution**: Present simplified versions and get explicit approval for additional complexity
**Process**:
1. Start with minimal viable feature set
2. Present core functionality clearly
3. List potential additions as optional
4. Get stakeholder feedback on what's actually needed
5. Document what was simplified and why
6. Update designs immediately based on feedback

### Mobile-First Communication
**Lesson**: Volunteer-operated systems require different communication approach
**Solution**: Emphasize operational benefits and volunteer ease-of-use
**Process**:
1. Highlight touch optimization and large targets
2. Demonstrate offline capability for poor venue WiFi
3. Show quick processing workflow for event lines
4. Emphasize minimal training requirements
5. Present battery life and performance considerations
6. Document venue-specific accessibility features

### Critical Corrections Communication
**Lesson**: When stakeholders provide urgent corrections, immediate systematic response required
**Solution**: Structured correction process with clear documentation
**Process**:
1. Acknowledge receipt immediately
2. Stop all other work to address corrections
3. Apply corrections systematically with documentation
4. Confirm completion with before/after summary
5. Update version numbers and approval status
6. Follow up to ensure corrections meet expectations

### Documentation Standards Communication
**Lesson**: Design documentation must be accurate and up-to-date or developers implement wrong patterns
**Solution**: Regular documentation audits and immediate updates when discrepancies found
**Process**:
1. Review CSS implementation against design system documentation
2. Identify any mismatches between docs and actual code
3. Update documentation to reflect actual implementation
4. Create comprehensive guides for complex patterns (like Button Style Guide)
5. Cross-reference related documentation
6. Increment version numbers to indicate updates

This comprehensive approach ensures all future wireframes will be consistent with the approved Design System v7, meet stakeholder expectations, support critical legal compliance requirements for community safety, provide mobile-first experiences optimized for volunteer staff operations, respond effectively to stakeholder feedback and corrections, and maintain accurate documentation that matches actual implementation.
