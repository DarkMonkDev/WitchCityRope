# Event Management User Flows

## 1. Event Creation Flow (Admin)

```mermaid
flowchart TD
    Start([Admin clicks Create Event]) --> EventType{Select type}
    EventType -->|Class| ClassSettings[Public event settings]
    EventType -->|Meetup| MeetupSettings[Member-only settings]
    
    ClassSettings --> BasicInfo[Fill Basic Info:<br/>- Title<br/>- Description<br/>- Teacher<br/>- Image]
    MeetupSettings --> BasicInfo
    
    BasicInfo --> Schedule[Set Schedule:<br/>- Date & time<br/>- Duration<br/>- Venue address]
    
    Schedule --> Capacity[Set Capacity:<br/>- Max attendees<br/>- Registration dates]
    
    Capacity --> Tickets[Configure Tickets:<br/>- Pricing type<br/>- Individual/Couples<br/>- Refund policy]
    
    Tickets --> Emails[Setup Emails:<br/>- Confirmation<br/>- Reminder<br/>- Custom templates]
    
    Emails --> Volunteers[Add Volunteers:<br/>- Define tasks<br/>- Assign people<br/>- Set times]
    
    Volunteers --> Review[Review all tabs]
    Review --> Publish{Ready?}
    
    Publish -->|No| EditTabs[Edit tabs]
    EditTabs --> Review
    
    Publish -->|Yes| CreateEvent[Create event]
    CreateEvent --> Live[Event goes live]
    Live --> Notify[Notify subscribers]
```

## 2. Event Registration Flow (Member)

```mermaid
flowchart TD
    Start([Member browses events]) --> ViewList[View event list]
    ViewList --> Filter[Apply filters:<br/>- Type<br/>- Date<br/>- Teacher]
    
    Filter --> SelectEvent[Click event]
    SelectEvent --> DetailPage[View event details]
    
    DetailPage --> CheckCap{Spots available?}
    CheckCap -->|No| Waitlist[Join waitlist]
    CheckCap -->|Yes| Register[Click Register]
    
    Register --> TicketType{Select ticket}
    TicketType -->|Individual| IndPrice[Individual pricing]
    TicketType -->|Couple| CouplePrice[Couple pricing]
    
    IndPrice --> PriceType{Pricing type?}
    CouplePrice --> PriceType
    
    PriceType -->|Fixed| ShowPrice[Show fixed price]
    PriceType -->|Sliding| SelectPrice[Select price in range]
    
    ShowPrice --> Checkout[PayPal checkout]
    SelectPrice --> Checkout
    
    Checkout --> Payment{Payment result}
    Payment -->|Success| Confirm[Registration confirmed]
    Payment -->|Failed| Retry[Show error]
    
    Confirm --> Email[Send confirmation]
    Email --> ShowVenue[Display venue details]
    ShowVenue --> TicketConfirm[Show ticket confirmation]
    
    Waitlist --> WaitConfirm[Waitlist confirmed]
    WaitConfirm --> WaitEmail[Send waitlist email]
```

## 3. Event Check-in Flow (Staff)

```mermaid
flowchart TD
    Start([Staff opens check-in]) --> SelectEvent[Select today's event]
    SelectEvent --> CheckinPage[Check-in interface]
    
    CheckinPage --> Search{Search method?}
    Search -->|Name| TypeName[Type attendee name]
    Search -->|List| ScrollList[Scroll attendee list]
    
    TypeName --> Results[Show matching results]
    ScrollList --> Results
    Results --> Select[Select attendee]
    
    Select --> Modal[Open check-in modal]
    Modal --> Display[Display:<br/>- Photo<br/>- Payment status<br/>- Waiver status<br/>- Notes]
    
    Display --> Verify{All OK?}
    Verify -->|No| HandleIssue[Handle issue:<br/>- Payment<br/>- Waiver<br/>- Other]
    Verify -->|Yes| CheckIn[Confirm check-in]
    
    HandleIssue --> Override{Override?}
    Override -->|Yes| CheckIn
    Override -->|No| Resolve[Resolve issue]
    Resolve --> CheckIn
    
    CheckIn --> Update[Update:<br/>- Status<br/>- Timestamp<br/>- Counter]
    Update --> Next[Ready for next]
```

## 4. Event Management Flow (Admin)

```mermaid
flowchart TD
    Start([Admin views events]) --> EventList[Event management page]
    EventList --> Actions{Select action}
    
    Actions -->|Edit| EditEvent[Edit event details]
    Actions -->|Duplicate| DupEvent[Duplicate for new date]
    Actions -->|Email| EmailAttend[Email attendees]
    Actions -->|Cancel| CancelEvent[Cancel event]
    Actions -->|View| ViewAttend[View attendees]
    
    EditEvent --> SaveChanges[Save changes]
    SaveChanges --> NotifyChanges[Notify affected users]
    
    DupEvent --> ModifyDetails[Modify date/details]
    ModifyDetails --> CreateNew[Create as new event]
    
    EmailAttend --> ComposeEmail[Compose message]
    ComposeEmail --> SelectRecip[Select recipients:<br/>- All registered<br/>- Waitlist<br/>- Specific]
    SelectRecip --> SendEmail[Send email]
    
    CancelEvent --> ConfirmCancel{Confirm?}
    ConfirmCancel -->|Yes| ProcessCancel[Process cancellation:<br/>- Refund payments<br/>- Email attendees<br/>- Mark cancelled]
    
    ViewAttend --> AttendList[Attendee list:<br/>- Name<br/>- Status<br/>- Payment<br/>- Check-in]
```

## 5. Refund Flow

```mermaid
flowchart TD
    Start([Member views ticket]) --> CheckWindow{Within refund window?}
    CheckWindow -->|No| NoRefund[Display: No refunds after X]
    CheckWindow -->|Yes| RequestRefund[Click Request Refund]
    
    RequestRefund --> Confirm{Confirm refund?}
    Confirm -->|No| Cancel[Cancel request]
    Confirm -->|Yes| Process[Process refund]
    
    Process --> PayPalRefund[PayPal API refund]
    PayPalRefund --> Success{Refund successful?}
    
    Success -->|No| Error[Show error message]
    Success -->|Yes| Update[Update registration]
    
    Update --> RemoveAttendee[Remove from attendees]
    RemoveAttendee --> Email[Send confirmation email]
    Email --> CheckWaitlist{Has waitlist?}
    
    CheckWaitlist -->|No| Done[Complete]
    CheckWaitlist -->|Yes| NotifyWaitlist[Notify first on waitlist]
    NotifyWaitlist --> Timer[24hr claim timer]
```

## State Diagram: Event Lifecycle

```mermaid
stateDiagram-v2
    [*] --> Draft: Create event
    Draft --> Published: Publish
    Published --> Open: Registration opens
    Open --> Full: Capacity reached
    Full --> Open: Cancellation/refund
    Open --> Closed: Registration closes
    Full --> Closed: Registration closes
    Closed --> InProgress: Event starts
    InProgress --> Completed: Event ends
    Published --> Cancelled: Admin cancels
    Open --> Cancelled: Admin cancels
    Full --> Cancelled: Admin cancels
    Closed --> Cancelled: Admin cancels
    Completed --> [*]
    Cancelled --> [*]
```

## Key Features

### Event Creation
- Multi-tab interface
- Save draft functionality
- Preview before publish
- Template system for repeated events
- Rich text editor for descriptions

### Registration
- Real-time capacity updates
- Sliding scale honor system
- Couples ticket option
- Automatic waitlist management
- PayPal integration

### Check-in
- Mobile-optimized interface
- Offline capability
- Quick search
- Override permissions
- Real-time attendance tracking

### Communication
- Email template system
- Bulk email to attendees
- Automated reminders
- Custom email composition
- Delivery tracking