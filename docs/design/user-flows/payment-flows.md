# Payment & Financial Flows

## 1. Ticket Purchase Flow (Fixed Price)

```mermaid
flowchart TD
    Start([User clicks Register]) --> CheckAuth{Authenticated?}
    CheckAuth -->|No| Login[Redirect to login]
    CheckAuth -->|Yes| TicketSelect[Select ticket type]
    
    Login --> TicketSelect
    TicketSelect --> Type{Ticket type?}
    
    Type -->|Individual| IndQty[Select quantity: 1-4]
    Type -->|Couple| CoupleQty[Select quantity: 1-2]
    
    IndQty --> CalcTotal[Calculate total]
    CoupleQty --> CalcTotal
    
    CalcTotal --> ShowSummary[Show order summary:<br/>- Event details<br/>- Tickets<br/>- Total price]
    
    ShowSummary --> ProceedPay[Proceed to payment]
    ProceedPay --> PayPalRedirect[Redirect to PayPal]
    
    PayPalRedirect --> PayPalLogin[User logs into PayPal]
    PayPalLogin --> PayPalConfirm[Confirm payment]
    
    PayPalConfirm --> Return{Payment status}
    Return -->|Success| ProcessSuccess[Process successful payment]
    Return -->|Cancelled| BackToEvent[Return to event page]
    Return -->|Failed| ShowError[Show error message]
    
    ProcessSuccess --> SaveOrder[Save order:<br/>- Transaction ID<br/>- Amount<br/>- Timestamp]
    SaveOrder --> UpdateAttendees[Add to attendee list]
    UpdateAttendees --> SendConfirm[Send confirmation email]
    SendConfirm --> ShowSuccess[Show success page]
```

## 2. Sliding Scale Payment Flow

```mermaid
flowchart TD
    Start([User selects sliding scale event]) --> ShowRange[Display price range:<br/>$35 - $65]
    ShowRange --> Explain[Show explanation:<br/>'Pay what you can afford']
    
    Explain --> SelectAmount[Price selector slider]
    SelectAmount --> ShowSelected[Display selected: $XX]
    
    ShowSelected --> Confirm{Confirm amount?}
    Confirm -->|No| SelectAmount
    Confirm -->|Yes| ProceedCheckout[Proceed to checkout]
    
    ProceedCheckout --> PayPalFlow[Standard PayPal flow]
    PayPalFlow --> Process[Process at selected amount]
    
    Process --> Track[Track for analytics:<br/>- Amount selected<br/>- vs suggested price<br/>- Anonymous stats]
```

## 3. Free Event RSVP Flow

```mermaid
flowchart TD
    Start([Member views free meetup]) --> RSVPButton[Click RSVP]
    RSVPButton --> CheckCap{Capacity check}
    
    CheckCap -->|Full| Waitlist[Add to waitlist]
    CheckCap -->|Available| ConfirmRSVP[Confirm RSVP modal]
    
    ConfirmRSVP --> Agreement[Agree to:<br/>- Attendance policy<br/>- Code of conduct]
    
    Agreement --> SaveRSVP[Save RSVP]
    SaveRSVP --> UpdateCount[Update attendee count]
    UpdateCount --> SendEmail[Send confirmation email]
    SendEmail --> ShowConfirm[Show confirmation]
    
    Waitlist --> WaitConfirm[Confirm waitlist]
    WaitConfirm --> NotifyAvail[Will notify if spot opens]
```

## 4. Refund Processing Flow

```mermaid
flowchart TD
    Start([User requests refund]) --> CheckPolicy{Within refund window?}
    
    CheckPolicy -->|No| DenyRefund[Show policy:<br/>'No refunds after X hours']
    CheckPolicy -->|Yes| ConfirmRefund[Confirm refund request]
    
    ConfirmRefund --> InitiateRefund[Initiate PayPal refund]
    InitiateRefund --> PayPalAPI[PayPal refund API]
    
    PayPalAPI --> RefundStatus{Refund successful?}
    RefundStatus -->|No| ManualReview[Flag for manual review]
    RefundStatus -->|Yes| UpdateSystem[Update system]
    
    UpdateSystem --> RemoveAttendee[Remove from attendees]
    RemoveAttendee --> UpdateFinancials[Update financial records]
    UpdateFinancials --> EmailRefund[Send refund confirmation]
    
    EmailRefund --> CheckWaitlist{Check waitlist}
    CheckWaitlist -->|Empty| Complete[Refund complete]
    CheckWaitlist -->|Has people| NotifyNext[Notify next person]
    
    NotifyNext --> StartTimer[24hr claim timer]
    StartTimer --> Complete
```

## 5. Financial Reporting Flow (Admin)

```mermaid
flowchart TD
    Start([Admin accesses reports]) --> SelectReport{Report type}
    
    SelectReport -->|Event| EventReport[Event financial report]
    SelectReport -->|Period| PeriodReport[Period summary]
    SelectReport -->|Sliding| SlidingAnalysis[Sliding scale analysis]
    
    EventReport --> SelectEvent[Select specific event]
    SelectEvent --> ShowMetrics[Display:<br/>- Total revenue<br/>- Tickets sold<br/>- Refunds<br/>- Net amount]
    
    PeriodReport --> SelectDates[Select date range]
    SelectDates --> ShowSummary[Display:<br/>- Events held<br/>- Total revenue<br/>- Avg per event<br/>- Trends]
    
    SlidingAnalysis --> ShowStats[Display:<br/>- Avg amount paid<br/>- Distribution curve<br/>- vs suggested price]
    
    ShowMetrics --> Export{Export?}
    ShowSummary --> Export
    ShowStats --> Export
    
    Export -->|Yes| Format{Choose format}
    Format -->|CSV| GenerateCSV[Generate CSV]
    Format -->|PDF| GeneratePDF[Generate PDF]
    
    GenerateCSV --> Download[Download file]
    GeneratePDF --> Download
```

## 6. Volunteer Ticket Flow

```mermaid
flowchart TD
    Start([Admin creates event]) --> VolunteerTab[Go to Volunteers tab]
    VolunteerTab --> AddTasks[Create volunteer tasks]
    
    AddTasks --> AssignVol[Volunteers sign up]
    AssignVol --> TicketsTab[Go to Tickets tab]
    
    TicketsTab --> ShowVols[Display volunteers]
    ShowVols --> SetPrice{Set ticket price}
    
    SetPrice -->|Free| FreeTicket[Create comp ticket]
    SetPrice -->|Discount| DiscountTicket[Set discount price]
    
    FreeTicket --> AddTicket[Add ticket to volunteer]
    DiscountTicket --> AddTicket
    
    AddTicket --> NotifyVol[Email volunteer]
    NotifyVol --> VolConfirm[Volunteer confirms]
```

## Payment Security Features

### PCI Compliance
- No credit card data stored
- All payments through PayPal
- Secure redirect flow
- Transaction IDs only

### Fraud Prevention
- Rate limiting on purchases
- Duplicate payment detection
- Refund abuse monitoring
- IP address tracking

### Financial Controls
- Admin-only refund approval
- Audit trail for all transactions
- Daily reconciliation option
- Automated receipts

## Sliding Scale Guidelines

### Implementation
- Honor system based
- No verification required
- Anonymous tracking only
- Suggested midpoint shown

### Reporting
- Distribution analytics
- No individual tracking
- Aggregate data only
- Helps set future pricing