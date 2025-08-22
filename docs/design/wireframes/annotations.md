# Wireframe Annotations

## Landing Page (landing-page.html)

### Page Structure

1. **Utility Bar** (Top)
   - Report an Incident link (red, high visibility)
   - Contact Us link
   - Dark background (#1a1a1a)

2. **Header/Navigation**
   - Logo: "Witch City Rope" (brown #8B4513)
   - Primary Navigation:
     - Events & Classes (primary button)
     - How to Join (text link)
     - Resources (text link)
     - Login (secondary button)
   - Mobile hamburger menu for responsive

3. **Hero Section**
   - Gradient background (dark brown to saddle brown)
   - Main headline: "Salem's Premier Rope Education Community"
   - Subheading: Member count (600+) and safety focus
   - Two CTAs:
     - View Events & Classes (primary)
     - Learn How to Join (secondary)

4. **Events Preview**
   - 3-column grid (responsive)
   - Event cards show:
     - Type badge (CLASS = green, MEETUP = orange)
     - Title, date/time
     - Description (blurred for member-only events)
     - Price (sliding scale or fixed)
     - Registration link
   - "View All Events" button below

5. **Features Section** ("What We Offer")
   - 4-column grid
   - Icons (placeholder divs)
   - Feature titles and descriptions:
     - Educational Classes
     - Practice Events
     - Safe Community
     - Inclusive Environment

6. **CTA Section**
   - Gradient background matching hero
   - "Ready to Start Your Rope Journey?"
   - Apply for Membership button

7. **Footer**
   - 4-column layout:
     - Learn (classes, resources, equipment)
     - Community (join, conduct, instructors)
     - Support (contact, FAQ, incident report)
     - Connect (Discord, newsletter, FetLife)
   - Copyright and legal links

### Design Decisions

**Colors:**
- Primary: Saddle Brown (#8B4513)
- Dark: #1a1a1a
- Success/Class: Green (#2e7d32)
- Warning/Meetup: Orange (#e65100)
- Error/Incident: Red (#ff6b6b)

**Typography:**
- System fonts for fast loading
- Hero headline: 48px (32px mobile)
- Section titles: 36px (28px mobile)
- Body text: 16px

**Responsive Breakpoints:**
- Mobile: < 768px
- Desktop: 768px+

**Accessibility Notes:**
- High contrast text
- Clear visual hierarchy
- Mobile-first approach
- Semantic HTML structure

### Implementation Notes for Blazor

1. **Components Needed:**
   - NavBar.razor (with mobile toggle)
   - EventCard.razor (reusable)
   - FeatureCard.razor
   - Footer.razor

2. **Dynamic Elements:**
   - Events loaded from database
   - Member-only content blur/reveal
   - Login state changes nav

3. **Routes:**
   - `/` - Landing page
   - `/events` - Full event listing
   - `/join` - Vetting application
   - `/login` - Authentication page

### Business Logic Visible

1. **Event Types:**
   - Classes: Public, paid, advance registration
   - Meetups: Members only, optional payment

2. **Pricing:**
   - Sliding scale shown ($35-$55)
   - Fixed prices shown ($65)
   - Honor system implied

3. **Safety Focus:**
   - Incident reporting prominent
   - Vetted membership mentioned
   - Code of conduct referenced

4. **Community Size:**
   - "600+ members" establishes scale
   - "Premier" and "best instructors" positioning

---

## Vetting Application (vetting-application.html)

### Page Structure

1. **Progress Indicator**
   - 3-step visual progress bar
   - Steps: Create Account → Application Form → Review
   - Shows current step (Application Form)
   - Completed steps marked with checkmark

2. **Application Form Container**
   - White background with shadow
   - 800px max width for readability
   - Clear section breaks

3. **Form Sections**

   **Header & Info Box:**
   - Clear title: "Apply to Join Witch City Rope"
   - Timeline expectation (1-2 weeks)
   - Info box explaining vetting purpose
   - Warm, welcoming tone

   **Basic Information:**
   - Scene Name (required) - with explanation
   - FetLife Handle (optional) - for verification
   - Pronouns (optional)
   - Email (pre-filled, readonly)

   **Additional Information:**
   - Other names/handles - for safety vetting
   - About yourself (required) - 1000 char limit
   - How they found WCR (required)
   - Character counter for long text

   **Agreement Section:**
   - Community standards clearly stated
   - Age verification (21+)
   - Consent and boundary acknowledgment
   - Code of conduct agreement
   - Single checkbox for all agreements

4. **Form Actions**
   - Save as draft option
   - Submit button (large, prominent)
   - Clear visual hierarchy

5. **Success State**
   - Confirmation message
   - Timeline reminder
   - Email confirmation
   - Return to homepage button

### Design Patterns

**Form UX:**
- Required fields marked with red asterisk
- Help text under fields
- Placeholder text with examples
- Focus states on inputs (#8B4513 border)
- Disabled state for readonly fields

**Visual Hierarchy:**
- Gray background (#f5f5f5)
- White form containers
- Info box with brown accent
- Agreement box with light background

**Mobile Considerations:**
- Stack form actions vertically
- Maintain readability on small screens
- Touch-friendly input sizes

### Implementation Notes for Blazor

1. **Components Needed:**
   - ProgressIndicator.razor
   - VettingForm.razor
   - FormField.razor (reusable)
   - AgreementSection.razor

2. **Form Handling:**
   - Client-side validation
   - Save draft to local storage
   - Character counting in real-time
   - Disable submit until agreement checked

3. **Data Model:**
   ```csharp
   public class VettingApplication
   {
       public string SceneName { get; set; }
       public string? FetLifeHandle { get; set; }
       public string? Pronouns { get; set; }
       public string Email { get; set; }
       public string? OtherNames { get; set; }
       public string AboutYourself { get; set; }
       public string HowYouFound { get; set; }
       public bool AgreesToTerms { get; set; }
       public DateTime SubmittedAt { get; set; }
   }
   ```

4. **Validation Rules:**
   - Scene Name: Required, 2-50 chars
   - About Yourself: Required, 50-1000 chars
   - How You Found: Required
   - Agreement: Must be checked
   - Age verification through agreement

### Business Logic Visible

1. **Vetting Process:**
   - Not instant - 1-2 week review
   - Manual review process implied
   - Focus on safety and community fit

2. **Requirements:**
   - 21+ age requirement (strict)
   - Comfort with adult content
   - Understanding of consent/boundaries
   - Agreement to code of conduct

3. **Privacy Considerations:**
   - Optional fields clearly marked
   - FetLife handle for verification only
   - Other names for safety vetting

4. **User Experience:**
   - Save draft functionality
   - Clear progress indication
   - Welcoming tone throughout
   - No experience required messaging

---

## User Dashboard (user-dashboard.html)

### Page Structure

1. **Navigation Changes**
   - User menu dropdown replaces Login button
   - Shows scene name ("Alex Rivers")
   - Breadcrumb navigation added

2. **Main Dashboard Layout**
   - Page title: "My Upcoming Events"
   - Quick access buttons: Discord & Browse Events
   - Events grid (responsive)
   - Secondary actions section below

3. **Membership States (Commented Examples)**

   **State 1: Not Vetted, No Application**
   - Special card with dashed border
   - "Join Our Community" prompt
   - Apply for Membership CTA
   - No events shown

   **State 2: Application Submitted, Awaiting Interview**
   - "Schedule Your Vetting Call" card
   - Indicates application reviewed
   - Schedule call CTA
   - No events shown

   **State 3: Interview Scheduled**
   - Blue-bordered call info card
   - Shows date, time, interviewer
   - Join Video Call button
   - Reschedule option
   - No events shown yet

   **State 4: Fully Vetted Member (Active View)**
   - Full event cards displayed
   - Mix of classes and meetups
   - Payment status visible

4. **Event Card Types**

   **Registered & Paid:**
   - Green checkmark status
   - Ticket number shown
   - Refund request option
   - Calendar add option

   **RSVP'd but Unpaid:**
   - Yellow warning background
   - Payment reminder
   - Purchase Ticket button (warning style)
   - Sliding scale amount shown

5. **Secondary Actions**
   - View Past Events
   - Update Profile
   - Waiver Status
   - Our Policies

### Design Patterns

**Visual States:**
- Special cards: Dashed borders for prompts
- Warning states: Yellow backgrounds
- Success states: Green indicators
- Info states: Blue backgrounds

**Information Architecture:**
- Progressive disclosure based on membership status
- Clear visual hierarchy
- Action-oriented CTAs

**Mobile Behavior:**
- Single column event grid
- Stacked page header
- Maintained readability

### Implementation Notes for Blazor

1. **Components Needed:**
   - UserDashboard.razor (main container)
   - EventCard.razor (reusable)
   - MembershipStatusCard.razor
   - UserMenu.razor (dropdown)

2. **State Management:**
   ```csharp
   public enum MembershipStatus
   {
       Guest,                    // Not vetted, no application
       ApplicationSubmitted,     // Awaiting review
       InterviewScheduled,       // Call scheduled
       Member,                   // Fully vetted
       Suspended,               // Access revoked
       Banned                   // Permanent ban
   }
   ```

3. **Conditional Rendering:**
   - Show appropriate card based on membership status
   - Filter events based on user access level
   - Hide Discord button for non-members

4. **Event Data Model:**
   ```csharp
   public class UserEventRegistration
   {
       public Event Event { get; set; }
       public bool IsPaid { get; set; }
       public decimal? AmountPaid { get; set; }
       public string TicketNumber { get; set; }
       public DateTime RegisteredAt { get; set; }
   }
   ```

### Business Logic Visible

1. **Membership Journey:**
   - Clear progression path shown
   - Application → Interview → Membership
   - Each step has specific UI state

2. **Payment States:**
   - Can RSVP without payment (meetups)
   - Payment required warning
   - Sliding scale amounts tracked
   - Ticket numbers for reconciliation

3. **Access Control:**
   - Discord access for members only
   - Member-only events hidden from guests
   - Refund options available

4. **User Actions:**
   - Quick calendar integration
   - Easy refund requests
   - Profile management
   - Waiver tracking

### Key Features

1. **Smart Dashboard:**
   - Adapts to membership status
   - Shows next required action
   - Progressive enhancement

2. **Event Management:**
   - Clear payment status
   - One-click actions
   - Calendar integration

3. **Community Integration:**
   - Discord access prominent
   - Event browsing encouraged
   - Policy transparency

---

## Event Check-in Interface (event-checkin.html)

### Page Structure

1. **Header Bar**
   - Event name and type prominently displayed
   - Event date and time information
   - Exit Check-in button (returns to normal view)
   - Brown theme (#8B4513) for brand consistency

2. **Statistics Bar**
   - Real-time attendee counts
   - Key metrics at a glance:
     - Not Arrived count
     - Total Registered
     - Need Waiver (critical info)
     - Countdown timer to event start

3. **Controls Section**
   - Search box for quick name lookup
   - Filter tabs:
     - All attendees
     - Not Arrived (default focus)
     - Need Waiver (priority issues)
   - Counts shown in each tab

4. **Attendee Table**
   - Sortable columns (all directions)
   - Key information visible:
     - Name with pronouns
     - Payment status (Paid/Unpaid/Comp)
     - Waiver status (Signed/Not Signed)
     - Check-in action/status

### Design Patterns

**Visual Status Indicators:**
- Payment: Green (paid), Red (unpaid), Blue (comp)
- Waiver: Green (signed), Red (not signed)
- Check-in: Button (not arrived), Green text (checked in)

**Interaction Patterns:**
- One-click check-in process
- Real-time status updates
- Search-as-you-type functionality
- Column sorting for flexibility

**Mobile Optimization:**
- Responsive table with horizontal scroll
- Touch-friendly buttons
- Maintained readability on phones

### Implementation Notes for Blazor

1. **Components Needed:**
   - CheckInDashboard.razor (main container)
   - AttendeeTable.razor
   - StatsBar.razor
   - CountdownTimer.razor

2. **Real-time Updates:**
   - SignalR for live check-in updates
   - Auto-refresh attendee counts
   - Synchronized across devices

3. **Data Models:**
   ```csharp
   public class CheckInAttendee
   {
       public int UserId { get; set; }
       public string SceneName { get; set; }
       public string Pronouns { get; set; }
       public PaymentStatus Payment { get; set; }
       public bool WaiverSigned { get; set; }
       public DateTime? CheckedInAt { get; set; }
   }
   
   public enum PaymentStatus
   {
       Paid,
       Unpaid,
       Complimentary
   }
   ```

4. **Search Implementation:**
   - Client-side filtering for speed
   - Search by scene name
   - Highlight matched results

### Business Logic Visible

1. **Check-in Requirements:**
   - Payment verification shown
   - Waiver status critical
   - No blocking of check-in (trust staff judgment)

2. **Event Day Operations:**
   - Quick identification of issues
   - Priority on missing waivers
   - Complimentary tickets tracked

3. **Staff Workflow:**
   - Search by name for check-in
   - Manual check-in process
   - Visual confirmation of status

4. **Time Awareness:**
   - Countdown creates urgency
   - Helps staff prioritize
   - Know when to start event

### Key Features

1. **Efficiency Focus:**
   - <5 second check-in goal
   - Minimal clicks required
   - Clear visual feedback

2. **Problem Identification:**
   - Red badges draw attention
   - Filter by issues
   - Waiver count prominent

3. **Mobile-First Design:**
   - Works on phones/tablets
   - Large touch targets
   - Essential info visible

4. **Staff-Friendly:**
   - No complex workflows
   - Clear status indicators
   - Forgiveness for mistakes

### Performance Considerations

1. **Speed Requirements:**
   - Instant search results
   - Sub-second check-in
   - No loading delays

2. **Offline Capability:**
   - Cache attendee list
   - Queue check-ins if offline
   - Sync when connected

3. **Concurrent Use:**
   - Multiple staff checking in
   - Prevent double check-ins
   - Real-time sync

### Security Notes

1. **Access Control:**
   - Staff-only interface
   - No PII displayed (legal names hidden)
   - Scene names only

2. **Audit Trail:**
   - Log who checked in whom
   - Timestamp all actions
   - For incident resolution

---

## Check-in Modal (checkin-modal.html)

### Modal Structure

1. **Header**
   - "Check In Attendee" title
   - Close button (X)
   - Brown brand color (#8B4513)

2. **Attendee Info**
   - Scene name prominently displayed
   - Pronouns shown for respectful interaction
   - Gray background for visual separation

3. **Requirements Section (Critical)**
   
   **Waiver Status:**
   - Two states: NOT SIGNED (red) or Signed (green)
   - Action button to mark as signed
   - Shows last signed date when verified
   - Must be signed to enable check-in

   **COVID Test:**
   - Two states: NOT VERIFIED (red) or Verified (green)
   - Action button to verify negative test
   - Shows test date/time when verified
   - Must be verified to enable check-in

4. **Payment Section**
   - Warning message if unpaid
   - Amount input (sliding scale support)
   - Payment method toggle (Cash/Venmo)
   - Read-only if already paid online

5. **Notes Section**
   - Optional text area
   - For special circumstances
   - Audit trail purposes

6. **Footer Actions**
   - Cancel button
   - Complete Check-in button (disabled until requirements met)

### Business Logic & States

**Check-in Button States:**
1. **Disabled** (gray) - Missing waiver OR COVID verification
2. **Enabled** (brown) - Both requirements satisfied

**Requirement Verification Flow:**
1. Staff sees red status → clicks action button
2. Button transforms to green status badge
3. Once both green → check-in enabled
4. Staff completes check-in

**Payment Handling:**
- Not blocking for check-in
- Warning shown if unpaid
- Staff can record door payment
- Amount tracked for reconciliation

### Design Patterns

**Visual Hierarchy:**
- Red for critical missing items
- Green for verified/completed
- Yellow warning for payment
- Gray for disabled states

**Progressive Disclosure:**
- Action buttons only when needed
- Status badges replace buttons
- Clear next action always visible

**Trust-Based System:**
- Staff can override requirements
- Payment not blocking
- Notes for edge cases

### Implementation Notes for Blazor

1. **Component Structure:**
   ```csharp
   public class CheckInModal : ComponentBase
   {
       [Parameter] public CheckInAttendee Attendee { get; set; }
       [Parameter] public EventCallback<CheckInResult> OnCheckIn { get; set; }
       
       private bool WaiverSigned { get; set; }
       private bool CovidVerified { get; set; }
       private decimal? PaymentAmount { get; set; }
       private PaymentMethod? Method { get; set; }
       private string Notes { get; set; }
       
       private bool CanCheckIn => WaiverSigned && CovidVerified;
   }
   ```

2. **State Management:**
   - Track requirement states locally
   - Update database on confirmation
   - Log all verifications with timestamp

3. **Validation Rules:**
   - Both requirements must be true
   - Payment amount optional
   - Notes optional

### Key Features

1. **Safety First:**
   - COVID verification prominent
   - Waiver requirement enforced
   - Cannot bypass safety checks

2. **Flexible Payment:**
   - Record door payments
   - Track sliding scale amounts
   - Non-blocking for entry

3. **Audit Trail:**
   - Who verified what
   - Timestamp all actions
   - Notes for context

4. **Mobile Friendly:**
   - Large touch targets
   - Clear visual states
   - Single column layout

### Security Considerations

1. **Verification Authority:**
   - Only staff can verify
   - Actions logged with user ID
   - Cannot be undone easily

2. **Data Protection:**
   - No medical details stored
   - Just yes/no verification
   - Scene names only

### Edge Cases Handled

1. **Lost COVID test result**
   - Staff can verify verbally
   - Note in comments

2. **Waiver on paper**
   - Mark as signed
   - Physical copy filed

3. **Payment disputes**
   - Record what's paid
   - Note circumstances
   - Handle later

### Performance Notes

- Modal loads instantly
- State changes immediate
- No server round-trips until submit
- Optimistic UI updates

---

## Admin Events Management (admin-events.html)

### Page Structure

1. **Admin Header**
   - Dark theme (#1a1a1a) differentiates admin area
   - "ADMIN" badge for clear role indication
   - User name with logout option
   - No public navigation items

2. **Sidebar Navigation**
   - Fixed 250px width
   - Two sections: main nav and utilities
   - Active state with brown accent
   - Badge notification for vetting queue (3)
   - Icons for visual scanning

3. **Main Content Area**
   - Page title: "Event Management"
   - Search and Create Event in header
   - Events table with filtering

4. **Events Table**
   - Sortable columns (all fields)
   - Default sort by date (ascending)
   - Visual capacity indicators
   - Revenue tracking
   - Duplicate action for each event

### Design Patterns

**Visual Hierarchy:**
- Dark admin header for role clarity
- White content areas
- Brown accents for active/primary
- Green for revenue (positive)
- Red notification badge

**Information Display:**
- Type badges (CLASS/MEETUP)
- Capacity bars with percentages
- Date/time prominently shown
- Revenue in green
- Clickable event names

**Admin Controls:**
- Quick search functionality
- Create Event button prominent
- Duplicate for easy event copying
- Filter tabs (All/Upcoming/Past)

### Implementation Notes for Blazor

1. **Component Structure:**
   ```csharp
   public class AdminLayout : LayoutComponentBase
   {
       // Shared admin navigation
       // Role verification
       // Active page tracking
   }
   
   public class EventsManagement : ComponentBase
   {
       private List<Event> Events { get; set; }
       private string SearchTerm { get; set; }
       private EventFilter Filter { get; set; }
       private SortColumn CurrentSort { get; set; }
   }
   ```

2. **Security Requirements:**
   - Admin role required
   - Audit log for all actions
   - Cannot delete events (only cancel)

3. **Table Features:**
   - Client-side sorting
   - Real-time search filtering
   - Capacity calculations
   - Revenue summaries

### Business Logic Visible

1. **Event Management:**
   - All events visible (not just upcoming)
   - Revenue tracking important
   - Capacity monitoring
   - No delete option (audit trail)

2. **Admin Workflow:**
   - Quick duplication for recurring events
   - Search across all event fields
   - Filter by time period
   - Direct link to event details

3. **Financial Visibility:**
   - Revenue per event shown
   - Total revenue implied (not shown)
   - Helps with planning decisions

4. **Capacity Management:**
   - Visual bars for quick scanning
   - Exact numbers shown
   - Helps identify popular events

### Key Features

1. **Efficient Event Creation:**
   - Duplicate button for similar events
   - Saves time on recurring events
   - Preserves settings/pricing

2. **At-a-Glance Metrics:**
   - Registration fill rates
   - Revenue per event
   - Time-based filtering

3. **Admin Navigation:**
   - All key functions accessible
   - Notification badges
   - Clear active states

4. **Search & Sort:**
   - Real-time search
   - Multi-column sorting
   - Persistent sort preferences

### Sidebar Navigation Items

1. **Main Navigation:**
   - Dashboard - Overview stats
   - Events - Current page
   - Vetting Queue - Shows pending count
   - Incident Reports - Safety management
   - Members - User management
   - Teachers - Instructor profiles

2. **Utility Navigation:**
   - Financial Reports - Revenue details
   - Settings - System configuration

### Performance Considerations

1. **Data Loading:**
   - Paginate if >100 events
   - Lazy load past events
   - Cache event data

2. **Real-time Updates:**
   - SignalR for registration changes
   - Update capacity bars live
   - Refresh revenue on changes

3. **Search Optimization:**
   - Debounce search input
   - Index searchable fields
   - Client-side for <500 events

### Security Notes

1. **Access Control:**
   - Admin role verified on load
   - Actions logged with admin ID
   - No destructive operations

2. **Data Protection:**
   - Revenue data admin-only
   - No attendee PII shown
   - Audit trail for duplicates

---

## Event Detail Page (event-detail.html)

### Page Structure

1. **Two-Column Layout**
   - Left: Event details (responsive width)
   - Right: Registration sidebar (400px fixed)
   - Sticky sidebar on desktop

2. **Event Header**
   - Class badge (green for visibility)
   - Large title (36px)
   - Key metadata with icons:
     - Date and day
     - Time and duration
     - Capacity limit

3. **Content Sections**
   - About This Class
   - What You'll Learn (bulleted)
   - What to Bring
   - Your Instructor (with profile link)
   - Prerequisites
   - Important Policies

4. **Registration Sidebar**
   - Price range prominently displayed
   - Capacity visualization
   - Ticket type selection
   - Sliding scale selector
   - Contact info collection
   - Continue to Payment CTA

### Key Features

**Sliding Scale Implementation:**
- Visual slider for price selection
- Range adjusts based on ticket type
- Clear min/max labels
- Explanatory help text
- Honor system messaging

**Venue Security:**
- Location details hidden
- Lock icon for clarity
- Explanation provided
- Revealed after registration

**Instructor Profile:**
- Photo placeholder
- Bio snippet
- Link to full profile
- Builds trust/credibility

**Registration Flow:**
1. Select ticket type (individual/couple)
2. Choose price on sliding scale
3. Enter contact info
4. Continue to payment
5. Success confirmation

### Design Patterns

**Visual Hierarchy:**
- Price in green (positive)
- Capacity bar visual
- Warning box for policies
- Sticky sidebar for conversion

**User Experience:**
- Breadcrumb navigation
- Sticky registration form
- Clear refund deadline
- Mobile responsive

**Trust Building:**
- Instructor credentials
- Clear policies
- Safety emphasis
- Professional presentation

### Implementation Notes for Blazor

1. **Component Structure:**
   ```csharp
   public class EventDetail : ComponentBase
   {
       [Parameter] public int EventId { get; set; }
       public Event Event { get; set; }
       public int SpotsAvailable { get; set; }
       public decimal SelectedPrice { get; set; }
       public TicketType SelectedTicket { get; set; }
   }
   ```

2. **Dynamic Elements:**
   - Slider updates price in real-time
   - Ticket type changes slider range
   - Capacity updates from database
   - Success state after payment

3. **Payment Flow:**
   - Validate selections
   - Create registration record
   - Redirect to PayPal
   - Show venue on return

### Business Logic Visible

1. **Sliding Scale Pricing:**
   - Individual: $35-$55
   - Couple: $60-$100
   - User selects within range
   - No verification of amount

2. **Capacity Management:**
   - Shows exact availability
   - Visual bar for scanning
   - Updates in real-time

3. **Venue Protection:**
   - Address hidden until paid
   - Security for private venues
   - Clear explanation

4. **Refund Policy:**
   - 48-hour cutoff clearly stated
   - Specific deadline shown
   - No partial refunds

### Content Strategy

1. **Class Description:**
   - Beginner-friendly language
   - Clear learning outcomes
   - Safety emphasis
   - Practical details

2. **What to Bring:**
   - Rope specifications
   - Partner policy (solo OK)
   - Practical items

3. **Prerequisites:**
   - "None!" for beginners
   - Welcoming tone
   - Inclusive messaging

4. **Policies:**
   - Age requirement (21+)
   - Code of conduct link
   - Refund terms clear

### Mobile Considerations

- Single column layout
- Registration form at bottom
- Maintain readability
- Touch-friendly slider

### Conversion Optimization

1. **Sticky Sidebar:**
   - Always visible CTA
   - Price prominent
   - Capacity creates urgency

2. **Trust Elements:**
   - Instructor profile
   - Clear policies
   - Professional design

3. **Friction Reduction:**
   - Minimal form fields
   - Clear pricing
   - Simple ticket selection

### Security Notes

- No payment data collected
- PayPal handles transactions
- Venue address protected
- Email validation only

---

## Admin Vetting Review (admin-vetting-review.html)

### Page Structure

1. **Admin Layout Reuse**
   - Same header and sidebar as admin events page
   - Maintains consistent admin experience
   - Back link to vetting queue

2. **Application Header**
   - Applicant name with pronouns
   - Application status badge (PENDING REVIEW)
   - Days since application (7 days ago)
   - Interview status notification (blue info bar)
   - Action buttons: Flag for Review, Deny, Approve

3. **Two-Column Layout**
   - Left: Application details (wide)
   - Right: Admin notes section (450px)
   - Clear visual separation

4. **Application Details Sections**
   - About Themselves (full response)
   - How They Found WCR
   - FetLife Profile (linked)
   - Contact Information
   - Other Handles (for vetting)
   - Confirmations (21+, Code of Conduct)

5. **Admin Notes System**
   - Add note form at top
   - Chronological note history
   - Author and timestamp
   - Scrollable list (max-height: 600px)
   - Collaborative vetting visible

### Design Patterns

**Status Indicators:**
- PENDING REVIEW: Orange badge (#e65100)
- Interview scheduled: Blue info bar
- Confirmations: Green checkmarks
- Flag button: Red icon

**Information Architecture:**
- All application data on one page
- No tab switching needed
- Notes provide context/history
- Clear next actions

**Collaborative Features:**
- Multiple admin notes visible
- Author attribution
- Timestamp tracking
- Cross-reference capability

### Implementation Notes for Blazor

1. **Component Structure:**
   ```csharp
   public class VettingReview : ComponentBase
   {
       [Parameter] public int ApplicationId { get; set; }
       public VettingApplication Application { get; set; }
       public List<AdminNote> Notes { get; set; }
       public Interview? ScheduledInterview { get; set; }
       
       private string NewNoteContent { get; set; }
       private bool ShowApproveModal { get; set; }
       private bool ShowDenyModal { get; set; }
   }
   ```

2. **Note Management:**
   - Real-time note updates
   - Auto-refresh on new notes
   - Character limit on notes
   - Markdown support possible

3. **Modal Dialogs:**
   - Approve: Optional note, immediate membership
   - Deny: Required reason, email applicant
   - Both require confirmation

4. **Data Models:**
   ```csharp
   public class AdminNote
   {
       public int Id { get; set; }
       public string AuthorName { get; set; }
       public string Content { get; set; }
       public DateTime CreatedAt { get; set; }
       public int ApplicationId { get; set; }
   }
   
   public class Interview
   {
       public DateTime ScheduledAt { get; set; }
       public string InterviewerName { get; set; }
       public string Status { get; set; }
   }
   ```

### Business Logic Visible

1. **Vetting Process:**
   - Application → Review → Interview → Decision
   - Multiple admins collaborate
   - Reference checking evident
   - 7+ day timeline shown

2. **Interview Scheduling:**
   - Shows scheduled interviews
   - Interviewer assigned (Sky Chen)
   - Date/time visible
   - Not blocking approval

3. **Reference Verification:**
   - FetLife profile checked
   - Instagram mentioned
   - Community references (Sage, Luna)
   - Cross-event verification (RopeCraft)

4. **Decision Making:**
   - Can approve without interview
   - Deny requires reason
   - Flag for additional review
   - Collaborative decision process

### Key Features

1. **Efficient Review:**
   - All info on one screen
   - No navigation needed
   - Quick action buttons
   - Keyboard shortcuts possible

2. **Collaboration Tools:**
   - Shared notes system
   - See other admin actions
   - Build on previous work
   - Avoid duplicate effort

3. **Safety Focus:**
   - Multiple verification points
   - Community reference checks
   - Red flag system
   - Thorough documentation

4. **Audit Trail:**
   - All notes preserved
   - Timestamps on everything
   - Author attribution
   - Decision reasoning

### Admin Workflow

1. **Initial Review:**
   - Read application
   - Check references
   - Add initial notes
   - Schedule interview or flag

2. **Reference Checking:**
   - Click FetLife link
   - Verify profile authentic
   - Check other handles
   - Note findings

3. **Collaborative Review:**
   - Read other admin notes
   - Add own observations
   - Build consensus
   - Make decision

4. **Final Decision:**
   - Review all notes
   - Click Approve/Deny
   - Add final note
   - System sends email

### Security Considerations

1. **Access Control:**
   - Admin-only page
   - No applicant PII visible
   - Scene names used
   - Email protected

2. **Data Protection:**
   - Notes are permanent
   - No delete function
   - Audit trail complete
   - Legal name encrypted

3. **Decision Authority:**
   - Any admin can approve/deny
   - Actions logged
   - Email notifications
   - Can't be undone

### Edge Cases Handled

1. **Missing References:**
   - Note in admin section
   - Can still approve
   - Trust admin judgment

2. **Interview No-Show:**
   - Flag for follow-up
   - Reschedule option
   - Note circumstances

3. **Conflicting Opinions:**
   - All views visible
   - Discussion in notes
   - Senior admin decides

4. **Rush Approvals:**
   - Flag option available
   - Note urgency
   - Skip interview if needed

### Performance Notes

- Load all notes at once
- Real-time updates via SignalR
- Cache application data
- Lazy load profile images

### Quality Indicators

1. **Thorough Vetting:**
   - Multiple touchpoints
   - Community verification
   - Reference checking
   - Interview option

2. **Transparent Process:**
   - All notes visible
   - Clear timeline
   - Decision reasoning
   - Collaborative approach

3. **Safety Priority:**
   - Multiple admins involved
   - External references
   - Flag system
   - No rush to approve

---

## Admin Vetting Queue List (admin-vetting-queue.html)

### Page Structure

1. **Standard Admin Layout**
   - Same header and sidebar navigation
   - Active state on Vetting Queue (with badge showing count)
   - Consistent admin experience

2. **Page Header**
   - "Vetting Queue" title
   - Search box for finding applicants
   - Real-time search functionality

3. **Applications Table Section**
   - White card container
   - "Pending Applications" header
   - Time filter tabs (Last Month, 3 Months, Year, All)
   - Sortable table columns

4. **Table Columns**
   - Status (visual badges or empty)
   - Applicant (name + pronouns)
   - Applied (date + relative time)
   - Why They Want to Join (excerpt)
   - FetLife (linked profile or "Not provided")

5. **Status Types Shown**
   - Empty/Pending (no badge)
   - INTERVIEW (blue badge)
   - FLAGGED (orange badge)
   - Missing: APPROVED/DENIED (filtered out)

### Design Patterns

**Visual Status System:**
- No badge = Pending review
- Blue = Interview scheduled
- Orange = Flagged for attention
- Consistent with other admin pages

**Information Density:**
- Excerpt of application reason
- Two-line display for dates
- Pronouns with names
- Direct FetLife links

**Sorting & Filtering:**
- Default sort by date (newest first)
- All columns sortable
- Time-based filtering
- Search across all fields

### Implementation Notes for Blazor

1. **Component Structure:**
   ```csharp
   public class VettingQueue : ComponentBase
   {
       private List<VettingApplication> Applications { get; set; }
       private string SearchTerm { get; set; }
       private TimeFilter CurrentFilter { get; set; } = TimeFilter.LastMonth;
       private SortColumn SortBy { get; set; } = SortColumn.DateApplied;
       private bool SortAscending { get; set; } = false;
   }
   
   public enum TimeFilter
   {
       LastMonth,
       LastThreeMonths,
       LastYear,
       AllTime
   }
   ```

2. **Real-time Features:**
   - Live search as-you-type
   - Auto-refresh for new applications
   - Badge count updates
   - Sort state persistence

3. **Data Loading:**
   - Only show pending/interview/flagged
   - Exclude approved/denied
   - Paginate if >50 applications
   - Cache for performance

4. **Application Status Enum:**
   ```csharp
   public enum ApplicationStatus
   {
       Pending,        // No badge shown
       Interview,      // Blue badge
       Flagged,        // Orange badge
       Approved,       // Not shown in queue
       Denied          // Not shown in queue
   }
   ```

### Business Logic Visible

1. **Queue Management:**
   - 3 pending items shown in badge
   - Only active applications displayed
   - Relative time display (X days ago)
   - Most recent first by default

2. **Application Preview:**
   - First ~100 chars of "why" response
   - Truncated with ellipsis
   - Gives quick context
   - Click name for full review

3. **Status Workflow:**
   - Pending → Review needed
   - Interview → Scheduled, awaiting
   - Flagged → Needs attention
   - Not shown: Completed applications

4. **Reference Checking:**
   - FetLife profiles directly linked
   - "Not provided" clearly marked
   - Quick verification ability

### Key Features

1. **Efficient Triage:**
   - See all pending at once
   - Quick status identification
   - Sort by urgency/date
   - Search for specific people

2. **Time-based Views:**
   - Default to last month
   - Historical views available
   - Helps manage workload
   - Track application trends

3. **Quick Actions:**
   - Click name to review
   - Direct FetLife links
   - Search functionality
   - Sort any column

4. **Visual Scanning:**
   - Status badges stand out
   - Pronouns visible
   - Relative dates helpful
   - Excerpts provide context

### Admin Workflow

1. **Daily Review:**
   - Check badge count
   - Sort by date (newest)
   - Click through pending
   - Process oldest first

2. **Search Scenarios:**
   - Find specific applicant
   - Search by FetLife name
   - Look for keywords
   - Check duplicates

3. **Priority Handling:**
   - Flagged items first
   - Interview reminders
   - Oldest pending next
   - Systematic approach

### Performance Considerations

1. **Data Loading:**
   - Load visible columns only
   - Lazy load full applications
   - Client-side filtering
   - Debounced search

2. **Real-time Updates:**
   - SignalR for new applications
   - Update badge counts
   - Refresh table on changes
   - Optimistic UI

3. **Caching Strategy:**
   - Cache applicant list
   - Refresh on filter change
   - Store sort preferences
   - Minimize API calls

### Security Notes

1. **Access Control:**
   - Admin role required
   - No PII in list view
   - Scene names only
   - Audit trail for views

2. **Data Protection:**
   - FetLife links open externally
   - No sensitive data in excerpts
   - Email addresses hidden
   - Legal names not shown

### Edge Cases

1. **Long Applications:**
   - Excerpt truncation handled
   - Full text on detail page
   - Consistent display

2. **Missing Data:**
   - "Not provided" for FetLife
   - Still reviewable
   - Note in detail view

3. **Duplicate Applications:**
   - Search helps identify
   - Admin notes on previous
   - Manual handling

### Mobile Considerations

- Table scrolls horizontally
- Essential columns visible
- Search full width
- Sidebar hidden
- Touch-friendly links

### Quality Indicators

1. **Organized Workflow:**
   - Clear status system
   - Sortable by priority
   - Time-based filtering
   - Efficient navigation

2. **Information Balance:**
   - Enough context to triage
   - Not overwhelming
   - Quick access to details
   - FetLife verification easy

3. **Admin Efficiency:**
   - Batch review possible
   - Quick status checks
   - Search capabilities
   - Clear next actions

---

## Event List (event-list.html)

### Page Structure

1. **Standard Public Header**
   - Same as landing page
   - Events & Classes button active
   - Login/Logout based on auth state

2. **Compact Page Hero**
   - Smaller than landing page hero
   - "Events & Classes" title
   - Subtitle about learning and community
   - Brown gradient background

3. **Filter Section**
   - White card container
   - Filter by event type (All, Classes, Member Events)
   - "View Past Events" link
   - Responsive layout

4. **Events List Layout**
   - Grouped by date sections
   - Date headers with borders
   - Event cards in chronological order
   - Clear visual hierarchy

5. **Event Card Design**
   - Type badge (CLASS/MEMBER EVENT)
   - Title, time, instructor/type
   - Full description for classes
   - Limited preview for member events (guests)
   - Pricing and capacity info
   - Action buttons

### Design Patterns

**Access Control Visualization:**
- Classes: Full details visible to all
- Member Events: Preview only for guests
- Login prompt with dashed border
- Member view example shown

**Visual Type System:**
- Green badge = Classes (public)
- Orange badge = Member events
- Consistent with other pages

**Information Hierarchy:**
- Date grouping for scanning
- Type badges for quick identification
- Capacity bars for urgency
- Price prominently displayed

### Implementation Notes for Blazor

1. **Component Structure:**
   ```csharp
   public class EventList : ComponentBase
   {
       [Inject] public IEventService Events { get; set; }
       [Inject] public AuthenticationStateProvider Auth { get; set; }
       
       private List<Event> AllEvents { get; set; }
       private EventFilter CurrentFilter { get; set; } = EventFilter.All;
       private bool ShowPastEvents { get; set; } = false;
       private bool IsMember { get; set; }
   }
   
   public enum EventFilter
   {
       All,
       ClassesOnly,
       MemberEventsOnly
   }
   ```

2. **Event Card Component:**
   - Reusable for both views
   - Conditional rendering based on auth
   - Props for guest/member mode
   - Handles capacity visualization

3. **Data Loading:**
   - Load upcoming events by default
   - Group by date in code
   - Filter client-side for speed
   - Lazy load past events

4. **Authentication Integration:**
   - Check auth state on load
   - Show appropriate content
   - Different CTAs based on status

### Business Logic Visible

1. **Event Types:**
   - Classes: Open to public, paid
   - Member Events: Vetted members only
   - Different pricing models shown

2. **Pricing Models:**
   - Sliding scale ($35-$55)
   - Fixed pricing ($65)
   - Couple discounts ($120)
   - Free RSVP + optional door payment

3. **Capacity Management:**
   - Visual bars show fill rate
   - "Only X spots left" warnings
   - Red styling for urgency
   - Exact numbers shown

4. **Member Benefits:**
   - Full event details
   - RSVP without payment option
   - Access to member-only events
   - Choice of payment timing

### Key Features

1. **Progressive Disclosure:**
   - Guests see limited info
   - Prompt to join/login
   - Members see everything
   - Clear value proposition

2. **Event Discovery:**
   - Chronological listing
   - Type filtering
   - Date grouping
   - Instructor names shown

3. **Registration Flow:**
   - Single CTA for classes
   - Dual options for members
   - Clear pricing display
   - Capacity creates urgency

4. **Visual Scanning:**
   - Date headers break up list
   - Type badges for quick ID
   - Capacity bars at a glance
   - Consistent card layout

### Guest vs Member Experience

**Guest View:**
- Class details fully visible
- Member events show preview only
- Login/apply prompts
- Limited description with ellipsis

**Member View:**
- All event details visible
- RSVP and payment options
- No login prompts
- Border highlight on example

### Mobile Considerations

- Single column layout
- Filters stack vertically
- Horizontal scroll for filters
- Full-width event cards
- Centered action buttons

### Conversion Optimization

1. **Clear Value Prop:**
   - Show what members get
   - Tease member events
   - Multiple login prompts
   - Apply option visible

2. **Urgency Indicators:**
   - Capacity visualization
   - "Only X left" messaging
   - Red styling for low spots
   - Real numbers shown

3. **Trust Building:**
   - Instructor names
   - Professional descriptions
   - Clear prerequisites
   - Organized presentation

### Performance Notes

- Initial load: Next 30 days
- Pagination for past events
- Client-side filtering
- Lazy load descriptions
- Cache event data

### Edge Cases

1. **No Upcoming Events:**
   - Show helpful message
   - Link to past events
   - Suggest joining waitlist

2. **Sold Out Events:**
   - Still show in list
   - "SOLD OUT" badge
   - Waitlist option

3. **Long Descriptions:**
   - Truncate appropriately
   - Full text on detail page
   - Consistent card heights

### Quality Indicators

1. **Information Architecture:**
   - Clear date grouping
   - Type-based filtering
   - Logical sort order
   - Easy scanning

2. **Accessibility:**
   - Semantic HTML
   - Clear visual hierarchy
   - Color not sole indicator
   - Mobile friendly

3. **User Journey:**
   - Browse → Filter → Select → Register
   - Multiple entry points
   - Clear next actions
   - Conversion focused