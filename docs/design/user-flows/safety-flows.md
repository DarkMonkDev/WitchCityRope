# Safety & Incident Management Flows

## 1. Anonymous Incident Report Flow

```mermaid
flowchart TD
    Start([Anyone clicks Report Incident]) --> AnonForm[Anonymous form loads]
    AnonForm --> NoLogin[No login required]
    
    NoLogin --> FillForm[Fill incident form:<br/>- Type of incident<br/>- Date/time<br/>- Location<br/>- Description<br/>- People involved]
    
    FillForm --> OptionalContact{Provide contact?}
    OptionalContact -->|Yes| AddContact[Add email/phone]
    OptionalContact -->|No| StayAnon[Remain anonymous]
    
    AddContact --> Submit[Submit report]
    StayAnon --> Submit
    
    Submit --> Encrypt[Encrypt sensitive data]
    Encrypt --> GenID[Generate report ID]
    GenID --> Store[Store in database]
    
    Store --> Notify[Notify safety team:<br/>- Email alert<br/>- Dashboard flag]
    
    Notify --> ShowConfirm[Show confirmation:<br/>'Report #12345 received']
    ShowConfirm --> SaveID[User saves ID for reference]
```

## 2. Safety Team Review Flow

```mermaid
flowchart TD
    Start([Safety team notified]) --> Login[Team member logs in]
    Login --> Dashboard[Safety dashboard]
    
    Dashboard --> ViewQueue[View incident queue:<br/>- New reports<br/>- In progress<br/>- Resolved]
    
    ViewQueue --> SelectReport[Select report]
    SelectReport --> ReviewDetails[Review details:<br/>- Encrypted data<br/>- Severity assessment<br/>- Similar reports]
    
    ReviewDetails --> InitialAssess{Initial assessment}
    
    InitialAssess -->|Emergency| Immediate[Immediate action:<br/>- Contact authorities<br/>- Emergency meeting]
    InitialAssess -->|High| HighPri[High priority:<br/>- Team discussion<br/>- Quick investigation]
    InitialAssess -->|Standard| Standard[Standard process:<br/>- Investigate<br/>- Document]
    
    Immediate --> TakeAction[Take required action]
    HighPri --> Investigate[Begin investigation]
    Standard --> Investigate
    
    Investigate --> GatherInfo[Gather information:<br/>- Interview witnesses<br/>- Review evidence<br/>- Check history]
    
    GatherInfo --> TeamMeeting[Team meeting:<br/>- Review findings<br/>- Discuss actions]
    
    TeamMeeting --> Decision{Determine action}
    Decision -->|NoAction| Document[Document decision]
    Decision -->|Warning| IssueWarning[Issue warning]
    Decision -->|Suspension| Suspend[Suspend member]
    Decision -->|Ban| BanMember[Ban member]
    
    Document --> CloseReport[Close report]
    IssueWarning --> FollowUp[Schedule follow-up]
    Suspend --> SetDuration[Set suspension period]
    BanMember --> UpdateStatus[Update member status]
    
    FollowUp --> CloseReport
    SetDuration --> CloseReport
    UpdateStatus --> CloseReport
```

## 3. Member Status Change Flow

```mermaid
flowchart TD
    Start([Safety decision made]) --> Action{Action type}
    
    Action -->|Warning| WarnProcess[Warning process]
    Action -->|Suspend| SuspendProcess[Suspension process]
    Action -->|Ban| BanProcess[Ban process]
    
    WarnProcess --> CreateWarning[Create warning record]
    CreateWarning --> EmailWarning[Email member:<br/>- Reason<br/>- Expectations<br/>- Consequences]
    
    SuspendProcess --> SetSuspension[Set suspension:<br/>- Start date<br/>- End date<br/>- Restrictions]
    SetSuspension --> UpdateAccess[Update access:<br/>- Block event registration<br/>- Hide member content]
    UpdateAccess --> EmailSuspend[Email notification]
    
    BanProcess --> PermanentBan[Set permanent ban]
    PermanentBan --> RevokeAll[Revoke all access:<br/>- Disable login<br/>- Remove from events<br/>- Block registration]
    RevokeAll --> EmailBan[Send final notice]
    
    EmailWarning --> LogAction[Log in member record]
    EmailSuspend --> LogAction
    EmailBan --> LogAction
    
    LogAction --> UpdateReport[Update incident report]
    UpdateReport --> NotifyReporter{Reporter contact?}
    
    NotifyReporter -->|Yes| SendUpdate[Send status update]
    NotifyReporter -->|No| Complete[Process complete]
    SendUpdate --> Complete
```

## 4. Waiver Management Flow

```mermaid
flowchart TD
    Start([Member registers for event]) --> CheckWaiver{Has valid waiver?}
    
    CheckWaiver -->|No| ShowWaiver[Display waiver form]
    CheckWaiver -->|Yes| CheckExpiry{Check expiration}
    
    CheckExpiry -->|Expired| ShowWaiver
    CheckExpiry -->|Valid| ProceedReg[Continue registration]
    
    ShowWaiver --> ReadWaiver[Member reads waiver]
    ReadWaiver --> AgreeTerms{Agree to terms?}
    
    AgreeTerms -->|No| CancelReg[Cancel registration]
    AgreeTerms -->|Yes| SignWaiver[Electronic signature]
    
    SignWaiver --> SaveWaiver[Save waiver:<br/>- Member ID<br/>- Timestamp<br/>- IP address<br/>- Waiver version]
    
    SaveWaiver --> SetExpiry[Set expiration:<br/>No expiration currently]
    SetExpiry --> ProceedReg
    
    ProceedReg --> CompleteReg[Complete registration]
```

## 5. Check-in Safety Verification

```mermaid
flowchart TD
    Start([Staff checking in attendee]) --> ScanAttendee[Scan/search attendee]
    ScanAttendee --> CheckStatus{Check safety status}
    
    CheckStatus -->|Flagged| ShowAlert[Display alert:<br/>- Reason<br/>- Instructions<br/>- Contact safety team]
    CheckStatus -->|Suspended| BlockEntry[Block check-in:<br/>'Suspended until X']
    CheckStatus -->|Banned| BlockPerm[Block check-in:<br/>'Not permitted']
    CheckStatus -->|Clear| CheckWaiver{Waiver signed?}
    
    ShowAlert --> ContactSafety[Contact safety team]
    ContactSafety --> Decision{Team decision}
    Decision -->|Allow| Override[Override with note]
    Decision -->|Deny| RefuseEntry[Refuse entry]
    
    BlockEntry --> RefuseEntry
    BlockPerm --> RefuseEntry
    
    CheckWaiver -->|No| RequireWaiver[Require waiver signing]
    CheckWaiver -->|Yes| AllowEntry[Allow check-in]
    
    RequireWaiver --> SignOnSpot[Sign on device]
    SignOnSpot --> AllowEntry
    
    Override --> AllowEntry
    AllowEntry --> LogEntry[Log check-in]
    RefuseEntry --> LogRefusal[Log refusal]
```

## Safety System Components

### Incident Types
- Consent violations
- Safety concerns
- Code of conduct violations
- Medical emergencies
- Property damage
- Other concerns

### Response Levels
1. **Information Only** - Logged but no action
2. **Monitor** - Watch for patterns
3. **Warning** - Formal warning issued
4. **Suspension** - Temporary removal
5. **Ban** - Permanent removal

### Documentation Requirements
- All reports documented
- All actions logged
- Decision rationale recorded
- Follow-up scheduled
- Patterns tracked

### Privacy Protection
- Report encryption
- Limited access (safety team only)
- Anonymous option
- Secure communication
- Data retention policy

### Communication Templates
- Initial report acknowledgment
- Investigation updates
- Warning notices
- Suspension notifications
- Ban notifications
- Reinstatement (if applicable)