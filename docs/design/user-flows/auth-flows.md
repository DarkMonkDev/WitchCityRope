# Authentication & Authorization Flows

## 1. Account Creation Flow

```mermaid
flowchart TD
    Start([User clicks Sign Up]) --> Method{Choose method}
    Method -->|Email| EmailForm[Enter email/password]
    Method -->|Google| GoogleOAuth[Google OAuth flow]
    
    EmailForm --> Validate{Valid?}
    Validate -->|No| ShowErrors[Show validation errors]
    ShowErrors --> EmailForm
    Validate -->|Yes| CreateAccount[Create account]
    
    GoogleOAuth --> GoogleConsent[Google consent screen]
    GoogleConsent --> GoogleReturn[Return with profile]
    GoogleReturn --> CreateAccount
    
    CreateAccount --> SendVerify[Send verification email]
    SendVerify --> ShowVerify[Show verification prompt]
    
    ShowVerify --> UserVerifies{User verifies?}
    UserVerifies -->|No| ResendOption[Option to resend]
    UserVerifies -->|Yes| Setup2FA[Setup 2FA screen]
    
    Setup2FA --> Choose2FA{2FA method}
    Choose2FA -->|TOTP| ShowQR[Display QR code]
    Choose2FA -->|SMS| EnterPhone[Enter phone number]
    
    ShowQR --> ScanApp[User scans with app]
    ScanApp --> EnterCode[Enter verification code]
    
    EnterPhone --> SendSMS[Send SMS code]
    SendSMS --> EnterCode
    
    EnterCode --> Verify2FA{Code valid?}
    Verify2FA -->|No| RetryCode[Try again]
    RetryCode --> EnterCode
    Verify2FA -->|Yes| SaveBackup[Show backup codes]
    
    SaveBackup --> Complete[Account created]
    Complete --> Dashboard[Redirect to dashboard]
```

## 2. Login Flow

```mermaid
flowchart TD
    Start([User clicks Login]) --> LoginForm[Login page]
    LoginForm --> Method{Auth method}
    
    Method -->|Email| EnterCreds[Enter email/password]
    Method -->|Google| GoogleFlow[Google OAuth]
    
    EnterCreds --> CheckCreds{Valid credentials?}
    CheckCreds -->|No| ShowError[Invalid login error]
    ShowError --> ForgotPw[Show forgot password]
    CheckCreds -->|Yes| Check2FA{2FA enabled?}
    
    GoogleFlow --> GoogleAuth[Google authentication]
    GoogleAuth --> Check2FA
    
    Check2FA -->|No| LoginSuccess[Login successful]
    Check2FA -->|Yes| Request2FA[Request 2FA code]
    
    Request2FA --> Enter2FA[Enter 2FA code]
    Enter2FA --> Verify2FA{Valid code?}
    
    Verify2FA -->|No| Retry{Attempts left?}
    Retry -->|Yes| Enter2FA
    Retry -->|No| LockAccount[Temporary lock]
    
    Verify2FA -->|Yes| LoginSuccess
    LoginSuccess --> CheckRole{Check user role}
    
    CheckRole -->|Guest| GuestDash[Guest dashboard]
    CheckRole -->|Member| MemberDash[Member dashboard]
    CheckRole -->|Admin| AdminDash[Admin dashboard]
    CheckRole -->|Staff| StaffDash[Staff dashboard]
```

## 3. Password Reset Flow

```mermaid
flowchart TD
    Start([Click Forgot Password]) --> EmailForm[Enter email address]
    EmailForm --> Submit[Submit form]
    
    Submit --> CheckEmail{Email exists?}
    CheckEmail -->|No| SameMessage[Show success message]
    CheckEmail -->|Yes| SendReset[Send reset email]
    SendReset --> SameMessage
    
    SameMessage --> UserChecks[User checks email]
    UserChecks --> ClickLink{Clicks reset link?}
    
    ClickLink -->|No| Expire[Link expires 24hr]
    ClickLink -->|Yes| ValidateToken{Token valid?}
    
    ValidateToken -->|No| ShowExpired[Show expired message]
    ShowExpired --> StartOver[Link to restart]
    ValidateToken -->|Yes| ResetForm[Password reset form]
    
    ResetForm --> NewPassword[Enter new password]
    NewPassword --> Confirm[Confirm password]
    
    Confirm --> Validate{Passwords valid?}
    Validate -->|No| ShowReqs[Show requirements]
    ShowReqs --> NewPassword
    Validate -->|Yes| UpdatePw[Update password]
    
    UpdatePw --> InvalidateSessions[Logout other sessions]
    InvalidateSessions --> Success[Show success]
    Success --> RedirectLogin[Redirect to login]
```

## 4. Two-Factor Authentication Management

```mermaid
flowchart TD
    Start([User in settings]) --> 2FASection[2FA settings section]
    2FASection --> Current{2FA enabled?}
    
    Current -->|No| Enable[Click Enable 2FA]
    Current -->|Yes| Manage[Manage 2FA options]
    
    Enable --> VerifyPw[Verify password]
    VerifyPw --> ChooseMethod[Choose 2FA method]
    ChooseMethod --> Setup[Setup chosen method]
    Setup --> TestCode[Test with code]
    TestCode --> SaveBackups[Save backup codes]
    
    Manage --> Options{Select option}
    Options -->|Change| ChangeMethod[Change method]
    Options -->|Disable| Disable2FA[Disable 2FA]
    Options -->|Backup| NewBackups[Generate new backups]
    
    ChangeMethod --> VerifyPw
    Disable2FA --> ConfirmDisable[Confirm disable]
    ConfirmDisable --> VerifyPw
    NewBackups --> VerifyPw
```

## 5. Session Management Flow

```mermaid
flowchart TD
    Start([User logs in]) --> CreateSession[Create session]
    CreateSession --> SetCookie[Set session cookie]
    SetCookie --> Track[Track session:<br/>- IP address<br/>- User agent<br/>- Login time]
    
    Track --> Active[User active]
    Active --> Check{Check session}
    
    Check -->|Valid| Continue[Continue using site]
    Check -->|Expired| Redirect[Redirect to login]
    Check -->|Suspicious| Challenge[Security challenge]
    
    Continue --> Activity{User activity?}
    Activity -->|Active| RefreshSession[Refresh session]
    Activity -->|Idle 30min| WarnTimeout[Show timeout warning]
    Activity -->|Idle 60min| Logout[Auto logout]
    
    Challenge --> Verify2FA[Require 2FA]
    Verify2FA --> Valid{Valid?}
    Valid -->|Yes| Continue
    Valid -->|No| Logout
```

## Role-Based Access Control (RBAC)

```mermaid
flowchart TD
    Start([User authenticated]) --> GetRole[Get user role]
    GetRole --> Route{Requested route}
    
    Route -->|/admin/*| CheckAdmin{Is admin?}
    Route -->|/checkin/*| CheckStaff{Is staff/admin?}
    Route -->|/dashboard/*| CheckMember{Is member?}
    Route -->|/events/meetup/*| CheckAuth{Is authenticated?}
    
    CheckAdmin -->|Yes| AllowAdmin[Allow access]
    CheckAdmin -->|No| Deny403[403 Forbidden]
    
    CheckStaff -->|Yes| AllowStaff[Allow access]
    CheckStaff -->|No| Deny403
    
    CheckMember -->|Yes| AllowMember[Allow access]
    CheckMember -->|No| GuestView[Show guest view]
    
    CheckAuth -->|Yes| AllowAuth[Allow access]
    CheckAuth -->|No| RedirectLogin[Redirect to login]
```

## Security Features

### Password Requirements
- Minimum 12 characters
- Mix of upper/lowercase
- At least one number
- At least one special character
- Not in common password list
- Different from last 5 passwords

### 2FA Options
- TOTP (Google Authenticator, Authy)
- SMS backup (optional)
- Backup codes (10 single-use)
- Recovery email option

### Session Security
- Secure, httpOnly cookies
- CSRF protection
- Session timeout (60 min idle)
- IP address validation
- Concurrent session limits

### Account Security
- Email verification required
- 2FA mandatory for all users
- Account lockout after 5 failed attempts
- Password reset rate limiting
- Login anomaly detection