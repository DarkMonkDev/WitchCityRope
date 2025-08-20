# Vetting System User Flows

## 1. Guest Application Flow

```mermaid
flowchart TD
    Start([Guest visits site]) --> ViewJoin[Views 'How to Join' info]
    ViewJoin --> ClickApply[Clicks 'Apply Now']
    
    ClickApply --> HasAccount{Has account?}
    HasAccount -->|No| CreateAccount[Create account]
    HasAccount -->|Yes| Login[Login]
    
    CreateAccount --> ChooseAuth{Auth method?}
    ChooseAuth -->|Email| EmailReg[Enter email/password]
    ChooseAuth -->|Google| GoogleAuth[Google OAuth]
    
    EmailReg --> VerifyEmail[Verify email]
    VerifyEmail --> Setup2FA[Setup 2FA]
    GoogleAuth --> Setup2FA
    
    Login --> Setup2FA
    Setup2FA --> AppForm[Vetting application form]
    
    AppForm --> FillForm[Fill multi-step form:<br/>- Scene name<br/>- Pronouns<br/>- Experience<br/>- References<br/>- Agreement]
    
    FillForm --> Submit[Submit application]
    Submit --> Confirm[Show confirmation]
    Confirm --> Email[Send confirmation email]
    Email --> CreateTicket[Create admin notification]
    
    CreateTicket --> Wait[Status: Pending Review]
```

## 2. Admin Review Flow

```mermaid
flowchart TD
    Start([Admin logs in]) --> Dashboard[Admin dashboard]
    Dashboard --> VettingQueue[View vetting queue]
    
    VettingQueue --> Filter[Apply filters:<br/>- Status<br/>- Date range<br/>- Assigned to]
    
    Filter --> SelectApp[Select application]
    SelectApp --> ReviewPage[Application review page]
    
    ReviewPage --> ViewDetails[View application details:<br/>- Answers<br/>- Account info<br/>- Previous notes]
    
    ViewDetails --> AddNotes[Add reviewer notes]
    AddNotes --> Decision{Make decision}
    
    Decision -->|Approve| Approve[Set status: Approved]
    Decision -->|Deny| Deny[Set status: Denied]
    Decision -->|More Info| RequestInfo[Request additional info]
    
    Approve --> EmailApprove[Send approval email]
    Deny --> EmailDeny[Send denial email]
    RequestInfo --> EmailInfo[Send info request email]
    
    EmailApprove --> UpdateMember[Update user to Member status]
    EmailDeny --> UpdateGuest[Keep as Guest status]
    EmailInfo --> UpdatePending[Status: Awaiting Info]
    
    UpdateMember --> End([Application complete])
    UpdateGuest --> End
    UpdatePending --> End
```

## 3. Collaborative Review Flow

```mermaid
flowchart TD
    Start([Multiple reviewers]) --> Reviewer1[Reviewer 1 adds notes]
    Start --> Reviewer2[Reviewer 2 adds notes]
    
    Reviewer1 --> SharedView[Shared application view]
    Reviewer2 --> SharedView
    
    SharedView --> Timeline[View note timeline:<br/>- Who reviewed<br/>- When reviewed<br/>- Comments added]
    
    Timeline --> Discuss{Need discussion?}
    Discuss -->|Yes| Flag[Flag for meeting]
    Discuss -->|No| FinalReview[Final reviewer decides]
    
    Flag --> Meeting[Discuss in meeting]
    Meeting --> FinalReview
    
    FinalReview --> Decision[Make final decision]
```

## 4. Member Status Management Flow

```mermaid
flowchart TD
    Start([Admin views member]) --> Profile[Member profile page]
    
    Profile --> CurrentStatus[View current status:<br/>- Guest<br/>- Pending<br/>- Member<br/>- Suspended<br/>- Banned]
    
    CurrentStatus --> ChangeStatus{Change status?}
    ChangeStatus -->|Yes| SelectNew[Select new status]
    ChangeStatus -->|No| End([No change])
    
    SelectNew --> AddReason[Add reason/notes]
    AddReason --> Confirm[Confirm change]
    
    Confirm --> UpdateDB[Update database]
    UpdateDB --> Email[Send status change email]
    Email --> Log[Log status change]
    Log --> End
```

## State Diagram: Application Status

```mermaid
stateDiagram-v2
    [*] --> Draft: Start application
    Draft --> Submitted: Submit form
    Submitted --> UnderReview: Admin opens
    UnderReview --> AwaitingInfo: Request more info
    AwaitingInfo --> UnderReview: Info provided
    UnderReview --> Approved: Admin approves
    UnderReview --> Denied: Admin denies
    Approved --> [*]
    Denied --> [*]
    AwaitingInfo --> Expired: No response 30 days
    Expired --> [*]
```

## Key Features

### Application Form
- Multi-step process with progress indicator
- Save draft functionality
- Field validation
- Required fields clearly marked
- Help text for complex questions

### Admin Queue
- Sortable columns
- Bulk actions
- Assignment to reviewers
- Priority flagging
- Search functionality

### Review Interface
- Full application display
- Inline note taking
- Status history
- Quick actions
- Email preview

### Notifications
- Application received (to applicant)
- New application (to admins)
- Status changes (to applicant)
- Notes added (to assigned reviewers)
- Reminder for pending reviews