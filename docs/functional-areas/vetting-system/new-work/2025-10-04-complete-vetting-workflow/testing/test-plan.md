# Complete Vetting Workflow - Comprehensive Testing Plan

<!-- Last Updated: 2025-10-04 -->
<!-- Version: 1.0 -->
<!-- Owner: Test Developer -->
<!-- Status: READY FOR IMPLEMENTATION -->

## Executive Summary

This document provides a comprehensive testing strategy for the complete vetting workflow implementation, covering:
- **Backend Services**: VettingAccessControlService, VettingEmailService, VettingService
- **Database**: VettingEmailTemplate, VettingEmailLog, VettingAuditLog tables
- **API Integration**: RSVP/ticket access control in ParticipationEndpoints
- **Frontend**: VettingReviewGrid, VettingApplicationDetail components
- **Email Integration**: SendGrid with mock mode, automated notifications

**Testing Levels**:
1. **Unit Tests (Backend)**: 60+ test cases across 3 services
2. **Integration Tests**: 25+ test cases for API endpoints and database
3. **E2E Tests**: 10+ comprehensive workflow tests

**Estimated Timeline**: 12-16 hours for complete implementation

---

## Table of Contents

1. [Testing Strategy Overview](#testing-strategy-overview)
2. [Unit Tests - Backend Services](#unit-tests---backend-services)
3. [Integration Tests - API & Database](#integration-tests---api--database)
4. [E2E Tests - Frontend Workflows](#e2e-tests---frontend-workflows)
5. [Test Data Requirements](#test-data-requirements)
6. [Test Execution Order](#test-execution-order)
7. [CI/CD Integration](#cicd-integration)
8. [Coverage Targets](#coverage-targets)
9. [Known Risks & Mitigation](#known-risks--mitigation)

---

## Testing Strategy Overview

### Testing Pyramid Approach

```
         /\
        /  \  E2E Tests (10+ tests)
       /----\  Comprehensive workflows
      /      \
     /--------\ Integration Tests (25+ tests)
    /          \ API endpoints + Database
   /------------\
  /--------------\ Unit Tests (60+ tests)
 /                \ Service layer logic
/------------------\
```

### Test Coverage Matrix

| Component | Unit | Integration | E2E | Priority |
|-----------|------|-------------|-----|----------|
| VettingAccessControlService | ✅ 15 tests | ✅ 8 tests | ✅ 3 tests | CRITICAL |
| VettingEmailService | ✅ 20 tests | ✅ 5 tests | - | CRITICAL |
| VettingService (Status) | ✅ 25 tests | ✅ 7 tests | ✅ 5 tests | CRITICAL |
| RSVP Access Control | - | ✅ 5 tests | ✅ 2 tests | HIGH |
| Frontend Components | - | - | ✅ 10+ tests | HIGH |

### Quality Gates

**Unit Tests**:
- ✅ 80% code coverage minimum
- ✅ All business logic paths tested
- ✅ Edge cases and error handling covered
- ✅ Fast execution (<100ms per test)

**Integration Tests**:
- ✅ All API endpoints tested
- ✅ Database transactions verified
- ✅ Error scenarios validated
- ✅ Real PostgreSQL with TestContainers

**E2E Tests**:
- ✅ Critical user journeys tested
- ✅ Cross-component workflows validated
- ✅ Docker-only environment (port 5173)
- ✅ Visual verification with screenshots

---

## Unit Tests - Backend Services

### 1. VettingAccessControlService Tests

**Location**: `/tests/WitchCityRope.Api.Tests/Features/Vetting/Services/VettingAccessControlServiceTests.cs`

**Test Class Structure**:
```csharp
public class VettingAccessControlServiceTests
{
    private readonly Mock<ApplicationDbContext> _mockContext;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<ILogger<VettingAccessControlService>> _mockLogger;
    private readonly VettingAccessControlService _sut;

    public VettingAccessControlServiceTests()
    {
        _mockContext = new Mock<ApplicationDbContext>();
        _mockCache = new Mock<IMemoryCache>();
        _mockLogger = new Mock<ILogger<VettingAccessControlService>>();
        _sut = new VettingAccessControlService(_mockContext.Object, _mockLogger.Object, _mockCache.Object);
    }
}
```

#### Test Suite 1: CanUserRsvpAsync - Status-Based Access Control

**Test Cases** (8 tests):

1. **CanUserRsvpAsync_WithNoApplication_ReturnsAllowed**
   - **Arrange**: User with no vetting application in database
   - **Act**: Call CanUserRsvpAsync(userId, eventId)
   - **Assert**:
     - Result.IsSuccess = true
     - AccessControlResult.IsAllowed = true
     - No denial reason
   - **Business Rule**: General members can RSVP for public events

2. **CanUserRsvpAsync_WithSubmittedStatus_ReturnsAllowed**
   - **Arrange**: User with application in Submitted status
   - **Act**: Call CanUserRsvpAsync(userId, eventId)
   - **Assert**: AccessControlResult.IsAllowed = true
   - **Business Rule**: New applications don't block access

3. **CanUserRsvpAsync_WithUnderReviewStatus_ReturnsAllowed**
   - **Arrange**: User with application in UnderReview status
   - **Act**: Call CanUserRsvpAsync(userId, eventId)
   - **Assert**: AccessControlResult.IsAllowed = true
   - **Business Rule**: Review process doesn't block access

4. **CanUserRsvpAsync_WithApprovedStatus_ReturnsAllowed**
   - **Arrange**: User with application in Approved status
   - **Act**: Call CanUserRsvpAsync(userId, eventId)
   - **Assert**: AccessControlResult.IsAllowed = true
   - **Business Rule**: Approved users have full access

5. **CanUserRsvpAsync_WithOnHoldStatus_ReturnsDenied**
   - **Arrange**: User with application in OnHold status
   - **Act**: Call CanUserRsvpAsync(userId, eventId)
   - **Assert**:
     - AccessControlResult.IsAllowed = false
     - DenialReason contains "on hold"
     - UserMessage contains support email
   - **Business Rule**: OnHold blocks RSVP access
   - **Audit**: Verifies audit log created

6. **CanUserRsvpAsync_WithDeniedStatus_ReturnsDenied**
   - **Arrange**: User with application in Denied status
   - **Act**: Call CanUserRsvpAsync(userId, eventId)
   - **Assert**:
     - AccessControlResult.IsAllowed = false
     - DenialReason = "Vetting application denied"
     - UserMessage explains denial
   - **Business Rule**: Denied blocks RSVP permanently
   - **Audit**: Verifies audit log created

7. **CanUserRsvpAsync_WithWithdrawnStatus_ReturnsDenied**
   - **Arrange**: User with application in Withdrawn status
   - **Act**: Call CanUserRsvpAsync(userId, eventId)
   - **Assert**:
     - AccessControlResult.IsAllowed = false
     - DenialReason = "Vetting application withdrawn"
     - UserMessage suggests reapplication
   - **Business Rule**: Withdrawn blocks access
   - **Audit**: Verifies audit log created

8. **CanUserRsvpAsync_WithInterviewScheduledStatus_ReturnsAllowed**
   - **Arrange**: User with application in InterviewScheduled status
   - **Act**: Call CanUserRsvpAsync(userId, eventId)
   - **Assert**: AccessControlResult.IsAllowed = true
   - **Business Rule**: Interview process allows RSVP

#### Test Suite 2: CanUserPurchaseTicketAsync - Ticket Purchase Control

**Test Cases** (8 tests - same scenarios as RSVP):

1. **CanUserPurchaseTicketAsync_WithNoApplication_ReturnsAllowed**
2. **CanUserPurchaseTicketAsync_WithSubmittedStatus_ReturnsAllowed**
3. **CanUserPurchaseTicketAsync_WithUnderReviewStatus_ReturnsAllowed**
4. **CanUserPurchaseTicketAsync_WithApprovedStatus_ReturnsAllowed**
5. **CanUserPurchaseTicketAsync_WithOnHoldStatus_ReturnsDenied**
6. **CanUserPurchaseTicketAsync_WithDeniedStatus_ReturnsDenied**
7. **CanUserPurchaseTicketAsync_WithWithdrawnStatus_ReturnsDenied**
8. **CanUserPurchaseTicketAsync_WithInterviewScheduledStatus_ReturnsAllowed**

**Note**: Same test structure as RSVP tests, validates consistent access control logic

#### Test Suite 3: Caching Behavior

**Test Cases** (3 tests):

1. **CanUserRsvpAsync_WithCachedStatus_UsesCache**
   - **Arrange**: Mock cache returns cached VettingStatusInfo
   - **Act**: Call CanUserRsvpAsync(userId, eventId)
   - **Assert**:
     - Database query NOT executed
     - Cache.TryGetValue called
     - Result uses cached data
   - **Performance**: Validates caching optimization

2. **CanUserRsvpAsync_WithoutCachedStatus_QueriesDatabase**
   - **Arrange**: Cache returns false (no cached data)
   - **Act**: Call CanUserRsvpAsync(userId, eventId)
   - **Assert**:
     - Database query executed
     - Cache.Set called with 5-minute duration
   - **Performance**: Validates cache miss behavior

3. **GetUserVettingStatusAsync_ReturnsStatusInfo**
   - **Arrange**: User with Approved status
   - **Act**: Call GetUserVettingStatusAsync(userId)
   - **Assert**:
     - StatusInfo.HasApplication = true
     - StatusInfo.Status = Approved
     - StatusInfo.ApplicationId matches
   - **Data**: Validates status information retrieval

#### Test Suite 4: Audit Logging

**Test Cases** (2 tests):

1. **CanUserRsvpAsync_WhenDenied_CreatesAuditLog**
   - **Arrange**: User with Denied status
   - **Act**: Call CanUserRsvpAsync(userId, eventId)
   - **Assert**:
     - VettingAuditLog created
     - Log contains user ID, event ID, status
     - Log timestamp is UTC
   - **Compliance**: Validates audit trail

2. **CanUserRsvpAsync_WhenAllowed_DoesNotCreateAuditLog**
   - **Arrange**: User with Approved status
   - **Act**: Call CanUserRsvpAsync(userId, eventId)
   - **Assert**: No audit log created
   - **Performance**: Avoids unnecessary logging

#### Test Suite 5: Error Handling

**Test Cases** (2 tests):

1. **CanUserRsvpAsync_WhenDatabaseFails_ReturnsFailureResult**
   - **Arrange**: Mock database throws exception
   - **Act**: Call CanUserRsvpAsync(userId, eventId)
   - **Assert**:
     - Result.IsSuccess = false
     - Error message is user-friendly
     - Exception logged
   - **Resilience**: Validates error handling

2. **CanUserRsvpAsync_WithInvalidUserId_HandlesGracefully**
   - **Arrange**: Empty Guid.Empty userId
   - **Act**: Call CanUserRsvpAsync(userId, eventId)
   - **Assert**: Result.IsSuccess (allows access for invalid ID)
   - **Business Rule**: Unknown users treated as general members

**Total VettingAccessControlService Tests**: **23 tests**

---

### 2. VettingEmailService Tests

**Location**: `/tests/WitchCityRope.Api.Tests/Features/Vetting/Services/VettingEmailServiceTests.cs`

**Test Class Structure**:
```csharp
public class VettingEmailServiceTests
{
    private readonly Mock<ApplicationDbContext> _mockContext;
    private readonly Mock<ILogger<VettingEmailService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly VettingEmailService _sut;

    public VettingEmailServiceTests()
    {
        _mockContext = new Mock<ApplicationDbContext>();
        _mockLogger = new Mock<ILogger<VettingEmailService>>();
        _mockConfig = SetupMockConfiguration(emailEnabled: false); // Mock mode by default
        _sut = new VettingEmailService(_mockContext.Object, _mockLogger.Object, _mockConfig.Object);
    }

    private Mock<IConfiguration> SetupMockConfiguration(bool emailEnabled, string? apiKey = null)
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Vetting:EmailEnabled"]).Returns(emailEnabled.ToString());
        config.Setup(c => c["Vetting:SendGridApiKey"]).Returns(apiKey ?? "");
        config.Setup(c => c["Vetting:FromEmail"]).Returns("noreply@witchcityrope.com");
        config.Setup(c => c["Vetting:FromName"]).Returns("WitchCityRope");
        return config;
    }
}
```

#### Test Suite 1: SendApplicationConfirmationAsync

**Test Cases** (5 tests):

1. **SendApplicationConfirmationAsync_InMockMode_LogsEmailContent**
   - **Arrange**:
     - EmailEnabled = false (mock mode)
     - VettingApplication with ApplicationNumber
   - **Act**: Call SendApplicationConfirmationAsync(application, email, name)
   - **Assert**:
     - Result.IsSuccess = true
     - Logger contains email content
     - VettingEmailLog created with DeliveryStatus.Sent
     - SendGridMessageId is null
   - **Mock Mode**: Validates logging behavior

2. **SendApplicationConfirmationAsync_WithTemplate_RendersVariables**
   - **Arrange**:
     - VettingEmailTemplate in database with variables
     - Template contains {{applicant_name}}, {{application_number}}
   - **Act**: Call SendApplicationConfirmationAsync(application, email, name)
   - **Assert**:
     - Variables replaced with actual values
     - Subject contains application number
     - Body contains applicant name
   - **Template**: Validates variable substitution

3. **SendApplicationConfirmationAsync_WithoutTemplate_UsesFallback**
   - **Arrange**: No email template in database
   - **Act**: Call SendApplicationConfirmationAsync(application, email, name)
   - **Assert**:
     - Default HTML template used
     - Default plain text template used
     - Contains application details
   - **Fallback**: Validates default template

4. **SendApplicationConfirmationAsync_InProductionMode_CallsSendGrid**
   - **Arrange**:
     - EmailEnabled = true
     - SendGridApiKey = "test-key"
     - Mock ISendGridClient
   - **Act**: Call SendApplicationConfirmationAsync(application, email, name)
   - **Assert**:
     - SendGrid.SendEmailAsync called
     - Email addresses correct
     - Subject and body populated
   - **Production**: Validates SendGrid integration
   - **Note**: Use mock SendGrid client, not real API

5. **SendApplicationConfirmationAsync_WhenSendGridFails_LogsError**
   - **Arrange**:
     - EmailEnabled = true
     - Mock SendGrid returns failure status
   - **Act**: Call SendApplicationConfirmationAsync(application, email, name)
   - **Assert**:
     - Result.IsSuccess = false
     - VettingEmailLog.DeliveryStatus = Failed
     - ErrorMessage contains SendGrid error
   - **Error Handling**: Validates failure logging

#### Test Suite 2: SendStatusUpdateAsync

**Test Cases** (6 tests):

1. **SendStatusUpdateAsync_WithApprovedStatus_SendsApprovedTemplate**
   - **Arrange**:
     - Application status = Approved
     - Approved email template in database
   - **Act**: Call SendStatusUpdateAsync(application, email, name, Approved)
   - **Assert**:
     - Correct template type selected
     - Email sent successfully
     - Log created with Approved template type
   - **Status**: Validates approval notification

2. **SendStatusUpdateAsync_WithDeniedStatus_SendsDeniedTemplate**
   - **Arrange**: Application status = Denied
   - **Act**: Call SendStatusUpdateAsync(application, email, name, Denied)
   - **Assert**: Template type = Denied
   - **Status**: Validates denial notification

3. **SendStatusUpdateAsync_WithOnHoldStatus_SendsOnHoldTemplate**
   - **Arrange**: Application status = OnHold
   - **Act**: Call SendStatusUpdateAsync(application, email, name, OnHold)
   - **Assert**: Template type = OnHold
   - **Status**: Validates on-hold notification

4. **SendStatusUpdateAsync_WithInterviewScheduledStatus_SendsTemplate**
   - **Arrange**: Application status = InterviewScheduled
   - **Act**: Call SendStatusUpdateAsync(application, email, name, InterviewScheduled)
   - **Assert**: Template type = InterviewScheduled
   - **Status**: Validates interview notification

5. **SendStatusUpdateAsync_WithSubmittedStatus_NoEmailSent**
   - **Arrange**: Application status = Submitted
   - **Act**: Call SendStatusUpdateAsync(application, email, name, Submitted)
   - **Assert**: Result.IsSuccess = true, but no email sent
   - **Business Rule**: Not all status changes trigger emails

6. **SendStatusUpdateAsync_WithDefaultTemplate_UsesStatusDescription**
   - **Arrange**: No template in database for status
   - **Act**: Call SendStatusUpdateAsync(application, email, name, Approved)
   - **Assert**:
     - Default template contains status description
     - Next steps message included
   - **Fallback**: Validates default status update template

#### Test Suite 3: SendReminderAsync

**Test Cases** (4 tests):

1. **SendReminderAsync_WithCustomMessage_IncludesMessage**
   - **Arrange**: customMessage = "Please complete your interview scheduling"
   - **Act**: Call SendReminderAsync(application, email, name, customMessage)
   - **Assert**:
     - Email body contains custom message
     - Template variables replaced
   - **Customization**: Validates custom message insertion

2. **SendReminderAsync_WithoutCustomMessage_SendsStandardReminder**
   - **Arrange**: customMessage = null
   - **Act**: Call SendReminderAsync(application, email, name, null)
   - **Assert**: Standard reminder template used
   - **Standard**: Validates default reminder

3. **SendReminderAsync_WithTemplate_RendersCorrectly**
   - **Arrange**: InterviewReminder template in database
   - **Act**: Call SendReminderAsync(application, email, name, "Custom")
   - **Assert**: Template variables replaced with application data
   - **Template**: Validates reminder template

4. **SendReminderAsync_InMockMode_LogsReminder**
   - **Arrange**: EmailEnabled = false
   - **Act**: Call SendReminderAsync(application, email, name, "Test")
   - **Assert**:
     - Logger contains reminder content
     - VettingEmailLog created
   - **Mock Mode**: Validates logging

#### Test Suite 4: Email Logging

**Test Cases** (3 tests):

1. **SendEmailAsync_AlwaysCreatesEmailLog**
   - **Arrange**: Any email send operation
   - **Act**: Call SendApplicationConfirmationAsync
   - **Assert**:
     - VettingEmailLog created
     - ApplicationId matches
     - RecipientEmail correct
     - SentAt timestamp is UTC
   - **Audit**: Validates all emails logged

2. **SendEmailAsync_InMockMode_SetsNullMessageId**
   - **Arrange**: EmailEnabled = false
   - **Act**: Send any email
   - **Assert**:
     - VettingEmailLog.SendGridMessageId = null
     - DeliveryStatus = Sent
   - **Mock**: Validates mock mode logging

3. **SendEmailAsync_InProductionMode_StoresSendGridMessageId**
   - **Arrange**:
     - EmailEnabled = true
     - Mock SendGrid returns X-Message-Id header
   - **Act**: Send any email
   - **Assert**:
     - VettingEmailLog.SendGridMessageId matches header
     - DeliveryStatus = Sent
   - **Production**: Validates SendGrid message ID capture

#### Test Suite 5: Error Handling

**Test Cases** (2 tests):

1. **SendEmailAsync_WhenDatabaseFails_LogsError**
   - **Arrange**: Mock DbContext.SaveChangesAsync throws exception
   - **Act**: Send email
   - **Assert**:
     - Result.IsSuccess = false
     - Error logged
     - Email log attempted
   - **Resilience**: Validates error handling

2. **SendEmailAsync_WhenTemplateRenderFails_UsesFallback**
   - **Arrange**: Template with invalid syntax
   - **Act**: Send email
   - **Assert**: Fallback template used
   - **Resilience**: Validates template fallback

**Total VettingEmailService Tests**: **20 tests**

---

### 3. VettingService Status Change Tests

**Location**: `/tests/WitchCityRope.Api.Tests/Features/Vetting/Services/VettingServiceTests.cs`

**Test Class Structure**:
```csharp
public class VettingServiceStatusChangeTests
{
    private readonly Mock<ApplicationDbContext> _mockContext;
    private readonly Mock<ILogger<VettingService>> _mockLogger;
    private readonly Mock<IVettingEmailService> _mockEmailService;
    private readonly VettingService _sut;

    public VettingServiceStatusChangeTests()
    {
        _mockContext = new Mock<ApplicationDbContext>();
        _mockLogger = new Mock<ILogger<VettingService>>();
        _mockEmailService = new Mock<IVettingEmailService>();
        _sut = new VettingService(_mockContext.Object, _mockLogger.Object, _mockEmailService.Object);
    }
}
```

#### Test Suite 1: UpdateApplicationStatusAsync - Valid Transitions

**Test Cases** (8 tests):

1. **UpdateApplicationStatusAsync_FromSubmittedToUnderReview_Succeeds**
   - **Arrange**:
     - Application with status = Submitted
     - Admin user authorized
   - **Act**: UpdateApplicationStatusAsync(appId, UnderReview, notes, adminId)
   - **Assert**:
     - Application.Status = UnderReview
     - Application.ReviewStartedAt set
     - Audit log created
     - Email sent
   - **Business Rule**: Valid transition

2. **UpdateApplicationStatusAsync_FromUnderReviewToInterviewApproved_Succeeds**
   - **Arrange**: Application status = UnderReview
   - **Act**: UpdateApplicationStatusAsync(appId, InterviewApproved, notes, adminId)
   - **Assert**:
     - Status changed
     - Email notification sent
     - Audit log with correct values
   - **Business Rule**: Valid transition

3. **UpdateApplicationStatusAsync_FromInterviewScheduledToApproved_Succeeds**
   - **Arrange**: Application status = InterviewScheduled
   - **Act**: UpdateApplicationStatusAsync(appId, Approved, notes, adminId)
   - **Assert**:
     - Status = Approved
     - DecisionMadeAt timestamp set
     - User role updated to VettedMember (if UserId exists)
     - Audit log created
   - **Business Rule**: Approval workflow
   - **Critical**: Validates role grant

4. **UpdateApplicationStatusAsync_FromUnderReviewToOnHold_RequiresNotes**
   - **Arrange**: Application status = UnderReview
   - **Act**: UpdateApplicationStatusAsync(appId, OnHold, null, adminId)
   - **Assert**:
     - Result.IsSuccess = false
     - Error message requires admin notes
   - **Business Rule**: OnHold requires explanation

5. **UpdateApplicationStatusAsync_FromUnderReviewToDenied_RequiresNotes**
   - **Arrange**: Application status = UnderReview
   - **Act**: UpdateApplicationStatusAsync(appId, Denied, null, adminId)
   - **Assert**:
     - Result.IsSuccess = false
     - Error requires denial reason
   - **Business Rule**: Denied requires explanation

6. **UpdateApplicationStatusAsync_WithNotes_AddsToAdminNotes**
   - **Arrange**:
     - Application with existing admin notes
     - New notes = "Additional review needed"
   - **Act**: UpdateApplicationStatusAsync(appId, OnHold, notes, adminId)
   - **Assert**:
     - Application.AdminNotes contains old notes
     - Application.AdminNotes contains new notes with timestamp
     - Format: "[2025-10-04 12:30] Status change to OnHold: {notes}"
   - **Data**: Validates note appending

7. **UpdateApplicationStatusAsync_FromOnHoldToUnderReview_Succeeds**
   - **Arrange**: Application status = OnHold
   - **Act**: UpdateApplicationStatusAsync(appId, UnderReview, notes, adminId)
   - **Assert**: Status changed successfully
   - **Business Rule**: Can resume from OnHold

8. **UpdateApplicationStatusAsync_CreatesAuditLog_WithCorrectData**
   - **Arrange**: Any valid status transition
   - **Act**: UpdateApplicationStatusAsync(appId, newStatus, notes, adminId)
   - **Assert**:
     - VettingAuditLog created
     - OldValue = old status
     - NewValue = new status
     - PerformedBy = adminId
     - PerformedAt is UTC
     - Notes = admin notes
   - **Audit**: Validates audit trail

#### Test Suite 2: UpdateApplicationStatusAsync - Invalid Transitions

**Test Cases** (5 tests):

1. **UpdateApplicationStatusAsync_FromApprovedToAnyStatus_Fails**
   - **Arrange**: Application status = Approved (terminal)
   - **Act**: UpdateApplicationStatusAsync(appId, UnderReview, notes, adminId)
   - **Assert**:
     - Result.IsSuccess = false
     - Error = "Cannot modify terminal state"
   - **Business Rule**: Approved is final

2. **UpdateApplicationStatusAsync_FromDeniedToAnyStatus_Fails**
   - **Arrange**: Application status = Denied (terminal)
   - **Act**: UpdateApplicationStatusAsync(appId, UnderReview, notes, adminId)
   - **Assert**:
     - Result.IsSuccess = false
     - Error = "Cannot modify terminal state"
   - **Business Rule**: Denied is final

3. **UpdateApplicationStatusAsync_FromSubmittedToApproved_Fails**
   - **Arrange**: Application status = Submitted
   - **Act**: UpdateApplicationStatusAsync(appId, Approved, notes, adminId)
   - **Assert**:
     - Result.IsSuccess = false
     - Error contains "Invalid transition"
   - **Business Rule**: Must go through review process

4. **UpdateApplicationStatusAsync_ByNonAdmin_Fails**
   - **Arrange**:
     - User with role = "Member" (not admin)
     - Valid status transition
   - **Act**: UpdateApplicationStatusAsync(appId, UnderReview, notes, userId)
   - **Assert**:
     - Result.IsSuccess = false
     - Error = "Only administrators can change status"
   - **Authorization**: Validates admin-only access

5. **UpdateApplicationStatusAsync_WithInvalidApplicationId_Fails**
   - **Arrange**: Non-existent application ID
   - **Act**: UpdateApplicationStatusAsync(invalidId, UnderReview, notes, adminId)
   - **Assert**:
     - Result.IsSuccess = false
     - Error = "Application not found"
   - **Validation**: Validates application exists

#### Test Suite 3: Specialized Status Change Methods

**Test Cases** (6 tests):

1. **ScheduleInterviewAsync_WithValidDate_SetsInterviewScheduledFor**
   - **Arrange**:
     - Application in InterviewApproved status
     - Future interview date
   - **Act**: ScheduleInterviewAsync(appId, interviewDate, location, adminId)
   - **Assert**:
     - Application.InterviewScheduledFor = interviewDate
     - Status = InterviewScheduled
     - Admin notes include date and location
     - Audit log created
   - **Business Rule**: Interview scheduling

2. **ScheduleInterviewAsync_WithPastDate_Fails**
   - **Arrange**: Interview date in the past
   - **Act**: ScheduleInterviewAsync(appId, pastDate, location, adminId)
   - **Assert**:
     - Result.IsSuccess = false
     - Error = "Interview date must be in the future"
   - **Validation**: Date validation

3. **ScheduleInterviewAsync_WithoutLocation_Fails**
   - **Arrange**: Empty or null location
   - **Act**: ScheduleInterviewAsync(appId, date, "", adminId)
   - **Assert**:
     - Result.IsSuccess = false
     - Error requires location
   - **Validation**: Location required

4. **PutOnHoldAsync_WithReasonAndActions_UpdatesStatus**
   - **Arrange**: Application in UnderReview status
   - **Act**: PutOnHoldAsync(appId, reason, requiredActions, adminId)
   - **Assert**:
     - Status = OnHold
     - Admin notes contain reason AND required actions
     - Email sent to applicant
   - **Business Rule**: OnHold workflow

5. **ApproveApplicationAsync_GrantsVettedMemberRole**
   - **Arrange**:
     - Application in InterviewScheduled status
     - User linked to application (UserId set)
   - **Act**: ApproveApplicationAsync(appId, adminId, notes)
   - **Assert**:
     - Status = Approved
     - User.Role = "VettedMember"
     - DecisionMadeAt timestamp set
     - Email sent
   - **CRITICAL**: Validates role grant for vetting approval

6. **DenyApplicationAsync_WithReason_UpdatesStatus**
   - **Arrange**: Application in InterviewScheduled status
   - **Act**: DenyApplicationAsync(appId, reason, adminId)
   - **Assert**:
     - Status = Denied
     - Admin notes contain denial reason
     - DecisionMadeAt timestamp set
     - Email sent
   - **Business Rule**: Denial workflow

#### Test Suite 4: Email Integration

**Test Cases** (3 tests):

1. **UpdateApplicationStatusAsync_ToApproved_SendsEmail**
   - **Arrange**: Status changing to Approved
   - **Act**: UpdateApplicationStatusAsync(appId, Approved, notes, adminId)
   - **Assert**:
     - IVettingEmailService.SendStatusUpdateAsync called
     - Email parameters correct
   - **Integration**: Validates email trigger

2. **UpdateApplicationStatusAsync_EmailFailure_DoesNotBlockStatusChange**
   - **Arrange**:
     - Mock email service throws exception
     - Valid status transition
   - **Act**: UpdateApplicationStatusAsync(appId, Approved, notes, adminId)
   - **Assert**:
     - Status changed successfully
     - Error logged
     - Transaction committed
   - **Resilience**: Email failure doesn't prevent status change

3. **UpdateApplicationStatusAsync_ToSubmitted_DoesNotSendEmail**
   - **Arrange**: Status changing to Submitted
   - **Act**: UpdateApplicationStatusAsync(appId, Submitted, notes, adminId)
   - **Assert**: Email service NOT called
   - **Business Rule**: Not all statuses trigger emails

#### Test Suite 5: Transaction and Error Handling

**Test Cases** (3 tests):

1. **UpdateApplicationStatusAsync_OnDatabaseError_RollsBackTransaction**
   - **Arrange**:
     - Mock SaveChangesAsync throws exception
     - Valid status transition
   - **Act**: UpdateApplicationStatusAsync(appId, UnderReview, notes, adminId)
   - **Assert**:
     - Transaction rolled back
     - Result.IsSuccess = false
     - Error message logged
   - **Resilience**: Validates transaction rollback

2. **UpdateApplicationStatusAsync_WithConcurrentUpdate_HandlesCorrectly**
   - **Arrange**:
     - Simulate DbUpdateConcurrencyException
     - Two admins updating same application
   - **Act**: UpdateApplicationStatusAsync(appId, UnderReview, notes, adminId)
   - **Assert**:
     - Result.IsSuccess = false
     - Error message appropriate
   - **Concurrency**: Validates conflict handling

3. **ScheduleInterviewAsync_OnError_RollsBackTransaction**
   - **Arrange**: Mock database throws exception during save
   - **Act**: ScheduleInterviewAsync(appId, date, location, adminId)
   - **Assert**:
     - Transaction rolled back
     - Result.IsSuccess = false
   - **Resilience**: Validates error handling

**Total VettingService Status Change Tests**: **25 tests**

---

## Integration Tests - API & Database

### Test Environment Setup

**CRITICAL**: All integration tests MUST use real PostgreSQL with TestContainers.

**Base Test Class**:
```csharp
public class VettingIntegrationTestBase : IAsyncLifetime
{
    protected readonly PostgreSqlContainer _postgresContainer;
    protected ApplicationDbContext _dbContext;
    protected HttpClient _client;

    public VettingIntegrationTestBase()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithDatabase("vetting_test")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var connectionString = _postgresContainer.GetConnectionString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        _dbContext = new ApplicationDbContext(options);
        await _dbContext.Database.MigrateAsync();

        // Seed test data
        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }
}
```

---

### 1. ParticipationEndpoints Integration Tests

**Location**: `/tests/WitchCityRope.IntegrationTests/Features/Participation/ParticipationEndpointsTests.cs`

**Purpose**: Test RSVP and ticket purchase endpoints with vetting access control integration

#### Test Suite 1: RSVP Endpoint Access Control

**Test Cases** (5 tests):

1. **POST_Rsvp_WithNoVettingApplication_Succeeds**
   - **Arrange**:
     - User with no vetting application
     - Public event in database
     - Authenticated user JWT token
   - **Act**: POST /api/events/{eventId}/rsvp
   - **Assert**:
     - HTTP 200 OK
     - RSVP created in database
     - CurrentRSVPs incremented
   - **Business Rule**: General members can RSVP

2. **POST_Rsvp_WithApprovedApplication_Succeeds**
   - **Arrange**:
     - User with Approved vetting application
     - Event in database
     - JWT token
   - **Act**: POST /api/events/{eventId}/rsvp
   - **Assert**:
     - HTTP 200 OK
     - RSVP created
   - **Business Rule**: Vetted members can RSVP

3. **POST_Rsvp_WithDeniedApplication_Returns403**
   - **Arrange**:
     - User with Denied vetting application
     - Event in database
     - JWT token
   - **Act**: POST /api/events/{eventId}/rsvp
   - **Assert**:
     - HTTP 403 Forbidden
     - Error message contains denial reason
     - No RSVP created
     - VettingAuditLog created with access denial
   - **Business Rule**: Denied users blocked from RSVP

4. **POST_Rsvp_WithOnHoldApplication_Returns403**
   - **Arrange**:
     - User with OnHold vetting application
     - Event in database
     - JWT token
   - **Act**: POST /api/events/{eventId}/rsvp
   - **Assert**:
     - HTTP 403 Forbidden
     - Error message contains support email
     - No RSVP created
   - **Business Rule**: OnHold users blocked from RSVP

5. **POST_Rsvp_WithWithdrawnApplication_Returns403**
   - **Arrange**:
     - User with Withdrawn vetting application
     - Event in database
     - JWT token
   - **Act**: POST /api/events/{eventId}/rsvp
   - **Assert**:
     - HTTP 403 Forbidden
     - Error suggests reapplication
     - No RSVP created
   - **Business Rule**: Withdrawn users blocked from RSVP

#### Test Suite 2: Ticket Purchase Endpoint Access Control

**Test Cases** (5 tests - same scenarios as RSVP):

1. **POST_PurchaseTicket_WithNoVettingApplication_Succeeds**
2. **POST_PurchaseTicket_WithApprovedApplication_Succeeds**
3. **POST_PurchaseTicket_WithDeniedApplication_Returns403**
4. **POST_PurchaseTicket_WithOnHoldApplication_Returns403**
5. **POST_PurchaseTicket_WithWithdrawnApplication_Returns403**

**Note**: Same test structure as RSVP, but for ticket purchase endpoint

---

### 2. VettingEndpoints Integration Tests

**Location**: `/tests/WitchCityRope.IntegrationTests/Features/Vetting/VettingEndpointsTests.cs`

**Purpose**: Test vetting status change API endpoints with database transactions

#### Test Suite 1: Status Update Endpoints

**Test Cases** (7 tests):

1. **PUT_ApplicationStatus_FromSubmittedToUnderReview_Succeeds**
   - **Arrange**:
     - Application in database with Submitted status
     - Admin user JWT token
   - **Act**: PUT /api/vetting/applications/{id}/status
   - **Request Body**: `{ "newStatus": "UnderReview", "notes": "Starting review" }`
   - **Assert**:
     - HTTP 200 OK
     - Database application status updated
     - Audit log created
     - Response contains updated application
   - **Database**: Validates transaction committed

2. **PUT_ApplicationStatus_InvalidTransition_Returns400**
   - **Arrange**: Application with Submitted status
   - **Act**: PUT /api/vetting/applications/{id}/status
   - **Request Body**: `{ "newStatus": "Approved" }` (invalid - skips review)
   - **Assert**:
     - HTTP 400 Bad Request
     - Error message explains invalid transition
     - Database NOT updated
   - **Validation**: Validates business rules enforced

3. **PUT_ApplicationStatus_ToTerminalState_Succeeds**
   - **Arrange**: Application in InterviewScheduled status
   - **Act**: PUT /api/vetting/applications/{id}/status
   - **Request Body**: `{ "newStatus": "Approved", "notes": "Interview passed" }`
   - **Assert**:
     - HTTP 200 OK
     - Status = Approved
     - DecisionMadeAt timestamp set
     - User role = "VettedMember"
   - **Business Rule**: Validates approval workflow

4. **PUT_ApplicationStatus_ByNonAdmin_Returns403**
   - **Arrange**:
     - Application in database
     - Regular user JWT token (not admin)
   - **Act**: PUT /api/vetting/applications/{id}/status
   - **Assert**:
     - HTTP 403 Forbidden
     - Error = "Administrator role required"
   - **Authorization**: Validates admin-only access

5. **POST_ScheduleInterview_WithValidData_Succeeds**
   - **Arrange**: Application in InterviewApproved status
   - **Act**: POST /api/vetting/applications/{id}/interview
   - **Request Body**: `{ "interviewDate": "2025-11-15T18:00:00Z", "location": "Community Center" }`
   - **Assert**:
     - HTTP 200 OK
     - InterviewScheduledFor set
     - Status = InterviewScheduled
     - Admin notes contain date and location
   - **Business Rule**: Interview scheduling

6. **POST_PutOnHold_WithReasonAndActions_Succeeds**
   - **Arrange**: Application in UnderReview status
   - **Act**: POST /api/vetting/applications/{id}/hold
   - **Request Body**: `{ "reason": "Missing references", "requiredActions": "Submit 2 references" }`
   - **Assert**:
     - HTTP 200 OK
     - Status = OnHold
     - Admin notes contain reason and actions
     - Email sent to applicant
   - **Business Rule**: OnHold workflow

7. **PUT_ApplicationStatus_OnDatabaseError_Returns500AndRollback**
   - **Arrange**:
     - Application in database
     - Simulate database constraint violation
   - **Act**: PUT /api/vetting/applications/{id}/status
   - **Assert**:
     - HTTP 500 Internal Server Error
     - Database rolled back (no changes committed)
     - Error logged
   - **Resilience**: Validates transaction rollback

#### Test Suite 2: Email Integration in Status Changes

**Test Cases** (3 tests):

1. **PUT_ApplicationStatus_ToApproved_SendsEmail**
   - **Arrange**:
     - Application in InterviewScheduled status
     - SendGrid configured (mock mode)
   - **Act**: PUT /api/vetting/applications/{id}/status
   - **Request Body**: `{ "newStatus": "Approved" }`
   - **Assert**:
     - Status updated
     - VettingEmailLog created with TemplateType.Approved
     - Email logged to console (mock mode)
   - **Integration**: Validates email trigger

2. **PUT_ApplicationStatus_EmailFails_StatusStillUpdates**
   - **Arrange**:
     - Application status change
     - Mock email service configured to fail
   - **Act**: PUT /api/vetting/applications/{id}/status
   - **Assert**:
     - Status updated successfully
     - VettingEmailLog created with DeliveryStatus.Failed
     - Transaction committed
   - **Resilience**: Email failure doesn't block status change

3. **POST_PutOnHold_SendsOnHoldEmail**
   - **Arrange**: Application in UnderReview
   - **Act**: POST /api/vetting/applications/{id}/hold
   - **Assert**:
     - VettingEmailLog created with TemplateType.OnHold
     - Email contains reason and required actions
   - **Integration**: Validates OnHold email

#### Test Suite 3: Audit Logging in Endpoints

**Test Cases** (2 tests):

1. **PUT_ApplicationStatus_CreatesAuditLog**
   - **Arrange**: Any valid status transition
   - **Act**: PUT /api/vetting/applications/{id}/status
   - **Assert**:
     - VettingAuditLog created
     - OldValue and NewValue correct
     - PerformedBy = admin user ID
     - Notes from request included
   - **Audit**: Validates audit trail

2. **POST_ScheduleInterview_CreatesAuditLog**
   - **Arrange**: Valid interview scheduling
   - **Act**: POST /api/vetting/applications/{id}/interview
   - **Assert**:
     - VettingAuditLog.Action = "Interview Scheduled"
     - Notes contain date and location
   - **Audit**: Validates interview audit

**Total Integration Tests**: **25 tests**

---

## E2E Tests - Frontend Workflows

**CRITICAL**: All E2E tests MUST run against Docker containers on port 5173 exclusively.

### Pre-Test Verification (MANDATORY)

**Before ANY E2E test execution**:
```bash
# 1. Verify Docker containers running
docker ps --format "table {{.Names}}\t{{.Ports}}" | grep witchcity

# 2. Kill any rogue local dev servers
./scripts/kill-local-dev-servers.sh

# 3. Verify React app on correct port
curl -f http://localhost:5173/ | grep -q "Witch City Rope" && echo "✅ Docker environment ready"
```

### Test Configuration

**Playwright Config** (`/apps/web/playwright.config.ts`):
```typescript
export default defineConfig({
  testDir: './tests/playwright',
  use: {
    baseURL: 'http://localhost:5173', // Docker port ONLY
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
```

---

### 1. Admin Vetting Workflow E2E Tests

**Location**: `/apps/web/tests/e2e/admin/vetting-workflow.spec.ts`

**Purpose**: Complete admin workflows for vetting application management

#### Test Suite 1: Admin Login and Navigation

**Test Cases** (2 tests):

1. **Admin_Login_And_Navigate_To_Vetting_Grid**
   - **Arrange**:
     - Docker containers running on port 5173
     - Admin user credentials
   - **Act**:
     - Navigate to http://localhost:5173
     - Click "Sign In"
     - Fill email: admin@witchcityrope.com
     - Fill password: Test123! (NO escaping)
     - Click Sign In button
     - Navigate to /admin/vetting
   - **Assert**:
     - URL = /admin/vetting
     - Page title contains "Vetting Applications"
     - Grid visible with applications
   - **Screenshot**: Capture vetting grid on success

2. **Non_Admin_Cannot_Access_Vetting_Grid**
   - **Arrange**: Regular member credentials
   - **Act**:
     - Login as member@witchcityrope.com
     - Attempt to navigate to /admin/vetting
   - **Assert**:
     - Redirected to /dashboard or /unauthorized
     - Error message displayed
   - **Authorization**: Validates admin-only access

#### Test Suite 2: Grid Display and Filtering

**Test Cases** (3 tests):

1. **Vetting_Grid_Displays_All_Applications**
   - **Arrange**:
     - Logged in as admin
     - Multiple applications in database
   - **Act**: Navigate to /admin/vetting
   - **Assert**:
     - Grid displays applications
     - Columns: Application #, Scene Name, Real Name, Email, Status, Submitted Date
     - Row count matches database
   - **UI**: Validates grid rendering

2. **Filter_Applications_By_Status**
   - **Arrange**:
     - Admin on vetting grid
     - Applications with different statuses
   - **Act**:
     - Click status filter dropdown
     - Select "UnderReview"
     - Apply filter
   - **Assert**:
     - Grid updates to show only UnderReview applications
     - Filter badge shows "Status: UnderReview"
   - **Functionality**: Validates filtering

3. **Search_Applications_By_Scene_Name**
   - **Arrange**: Admin on vetting grid
   - **Act**:
     - Type "TestUser" in search box
     - Press Enter
   - **Assert**:
     - Grid displays only matching applications
     - Search term highlighted
   - **Functionality**: Validates search

#### Test Suite 3: Application Detail View

**Test Cases** (2 tests):

1. **Click_Application_Row_Opens_Detail_View**
   - **Arrange**: Admin on vetting grid
   - **Act**:
     - Click first application row
     - Wait for navigation
   - **Assert**:
     - URL = /admin/vetting/applications/{id}
     - Application details displayed
     - Scene name, email, status visible
     - Action buttons visible
   - **Navigation**: Validates detail view

2. **Detail_View_Shows_Audit_History**
   - **Arrange**:
     - Admin on application detail page
     - Application with status changes
   - **Act**: Scroll to workflow history section
   - **Assert**:
     - Audit log entries displayed
     - Chronological order (newest first)
     - Each entry shows action, date, performer
   - **Data**: Validates audit history display

#### Test Suite 4: Status Change Modals

**Test Cases** (4 tests):

1. **Approve_Application_Modal_Workflow**
   - **Arrange**:
     - Admin on application detail
     - Application in InterviewScheduled status
   - **Act**:
     - Click "Approve" button
     - Modal opens
     - Fill optional notes: "Interview successful"
     - Click "Approve Application" button
   - **Assert**:
     - Modal closes
     - Status badge updates to "Approved"
     - Success toast notification
     - Audit log updated
   - **Workflow**: Validates approval modal
   - **Screenshot**: Capture on success

2. **Deny_Application_Modal_Workflow**
   - **Arrange**:
     - Admin on application detail
     - Application in InterviewScheduled status
   - **Act**:
     - Click "Deny" button
     - Modal opens
     - Fill required reason: "Did not meet safety requirements"
     - Click "Deny Application" button
   - **Assert**:
     - Status badge updates to "Denied"
     - Success notification
     - Reason stored in audit log
   - **Workflow**: Validates denial modal
   - **Validation**: Reason required

3. **Put_On_Hold_Modal_Workflow**
   - **Arrange**:
     - Admin on application detail
     - Application in UnderReview status
   - **Act**:
     - Click "Put On Hold" button
     - Modal opens
     - Fill reason: "Missing references"
     - Fill required actions: "Submit 2 references by email"
     - Click "Put On Hold" button
   - **Assert**:
     - Status badge updates to "OnHold"
     - Success notification
     - Reason and actions stored
   - **Workflow**: Validates on-hold modal
   - **Validation**: Both fields required

4. **Send_Reminder_Modal_Workflow**
   - **Arrange**:
     - Admin on application detail
     - Application in any status
   - **Act**:
     - Click "Send Reminder" button
     - Modal opens
     - Fill custom message: "Please complete your interview scheduling"
     - Click "Send Reminder" button
   - **Assert**:
     - Modal closes
     - Success notification: "Reminder sent"
     - Email log created
   - **Workflow**: Validates reminder modal

#### Test Suite 5: Sorting and Pagination

**Test Cases** (2 tests):

1. **Sort_Applications_By_Submitted_Date**
   - **Arrange**: Admin on vetting grid with multiple applications
   - **Act**:
     - Click "Submitted Date" column header
     - Click again to reverse sort
   - **Assert**:
     - Applications reordered
     - Newest first or oldest first
   - **Functionality**: Validates sorting

2. **Paginate_Through_Applications**
   - **Arrange**:
     - Admin on vetting grid
     - More than 25 applications (page size)
   - **Act**:
     - Scroll to pagination controls
     - Click "Next Page" button
   - **Assert**:
     - Page 2 applications displayed
     - Pagination shows "Page 2 of X"
   - **Functionality**: Validates pagination

#### Test Suite 6: Error Handling

**Test Cases** (1 test):

1. **Handle_Status_Change_Error_Gracefully**
   - **Arrange**:
     - Admin on application detail
     - Simulate API error (disconnect network or invalid transition)
   - **Act**: Attempt to approve application
   - **Assert**:
     - Error toast notification displayed
     - Status NOT changed
     - User can retry
   - **Resilience**: Validates error handling

**Total Admin Vetting E2E Tests**: **14 tests**

---

### 2. Access Control E2E Tests

**Location**: `/apps/web/tests/e2e/access-control/vetting-restrictions.spec.ts`

**Purpose**: Validate access restrictions for users with blocked vetting statuses

#### Test Suite 1: RSVP Blocking for Denied Users

**Test Cases** (2 tests):

1. **Denied_User_Cannot_RSVP_For_Event**
   - **Arrange**:
     - Create test user with Denied vetting application
     - Public event available
   - **Act**:
     - Login as denied user
     - Navigate to event detail page
     - Click "RSVP" button
   - **Assert**:
     - HTTP 403 Forbidden response
     - Error message: "Your vetting application was denied..."
     - RSVP button disabled or hidden
     - No RSVP created in database
   - **Business Rule**: Denied users blocked from RSVP

2. **Denied_User_Sees_Informative_Error_Message**
   - **Arrange**: Same as above
   - **Act**: Attempt RSVP
   - **Assert**:
     - Modal or toast with user-friendly message
     - Message explains denial
     - No technical error details exposed
   - **UX**: Validates user communication

#### Test Suite 2: Ticket Purchase Blocking

**Test Cases** (2 tests):

1. **OnHold_User_Cannot_Purchase_Ticket**
   - **Arrange**:
     - User with OnHold vetting application
     - Paid event available
   - **Act**:
     - Login as on-hold user
     - Navigate to event detail
     - Click "Purchase Ticket" button
   - **Assert**:
     - HTTP 403 Forbidden
     - Error message contains support email
     - Ticket purchase blocked
   - **Business Rule**: OnHold users blocked

2. **Withdrawn_User_Cannot_Purchase_Ticket**
   - **Arrange**: User with Withdrawn vetting application
   - **Act**: Attempt ticket purchase
   - **Assert**:
     - HTTP 403 Forbidden
     - Error suggests reapplication
   - **Business Rule**: Withdrawn users blocked

**Total Access Control E2E Tests**: **4 tests**

---

## Test Data Requirements

### Database Seeding for Tests

**Test Data Sets Required**:

1. **Users**:
   - Admin user: admin@witchcityrope.com (Administrator role)
   - Vetted member: vetted@witchcityrope.com (VettedMember role, Approved application)
   - Denied user: denied@witchcityrope.com (Member role, Denied application)
   - OnHold user: onhold@witchcityrope.com (Member role, OnHold application)
   - Withdrawn user: withdrawn@witchcityrope.com (Member role, Withdrawn application)
   - General member: member@witchcityrope.com (Member role, no application)
   - Under review user: reviewing@witchcityrope.com (Member role, UnderReview application)

2. **Vetting Applications**:
   - Application for vetted user: Status = Approved, SubmittedAt = 30 days ago
   - Application for denied user: Status = Denied, DecisionMadeAt = 15 days ago
   - Application for on-hold user: Status = OnHold, AdminNotes with reason
   - Application for withdrawn user: Status = Withdrawn
   - Application for reviewing user: Status = UnderReview, ReviewStartedAt = 5 days ago
   - Test applications in various statuses for grid testing (10+ applications)

3. **Events**:
   - Public social event: Capacity 30, CurrentRSVPs < capacity
   - Paid class event: Capacity 20, CurrentTickets < capacity
   - Past event: For validation testing (cannot modify)
   - Sold out event: Capacity = CurrentAttendees

4. **Email Templates** (Optional - fallback templates work):
   - ApplicationReceived template (Active, Version 1)
   - Approved template (Active, Version 1)
   - Denied template (Active, Version 1)
   - OnHold template (Active, Version 1)
   - InterviewReminder template (Active, Version 1)

### Test Data Builders

**C# Test Data Builders**:

```csharp
public class VettingApplicationTestBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid? _userId = null;
    private string _sceneName = "TestUser";
    private string _email = "test@example.com";
    private VettingStatus _status = VettingStatus.Submitted;
    private DateTime _submittedAt = DateTime.UtcNow;

    public VettingApplicationTestBuilder WithStatus(VettingStatus status)
    {
        _status = status;
        return this;
    }

    public VettingApplicationTestBuilder WithUser(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public VettingApplicationTestBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public VettingApplication Build()
    {
        return new VettingApplication
        {
            Id = _id,
            UserId = _userId,
            SceneName = _sceneName,
            Email = _email,
            Status = _status,
            SubmittedAt = _submittedAt,
            ApplicationNumber = $"VET-{DateTime.UtcNow:yyyyMMdd}-{_id.ToString()[..8].ToUpper()}",
            StatusToken = Guid.NewGuid().ToString("N")
        };
    }
}
```

**TypeScript Test Helpers** (E2E):

```typescript
export class TestDataHelper {
  static async createUserWithVettingStatus(
    email: string,
    sceneName: string,
    status: VettingStatus
  ): Promise<{ userId: string; applicationId: string }> {
    // Create user via API
    // Create vetting application via API
    // Return IDs for cleanup
  }

  static async cleanupTestData(userId: string): Promise<void> {
    // Delete user and related vetting application
    // Delete any RSVPs or tickets created during test
  }
}
```

---

## Test Execution Order

### Recommended Execution Sequence

**Phase 1: Unit Tests** (Fastest - Run First)
1. VettingAccessControlService tests (23 tests) - ~2 seconds
2. VettingEmailService tests (20 tests) - ~1.5 seconds
3. VettingService status change tests (25 tests) - ~2.5 seconds

**Total Unit Test Time**: ~6 seconds

**Phase 2: Integration Tests** (Medium Speed)
1. ParticipationEndpoints RSVP tests (5 tests) - ~15 seconds
2. ParticipationEndpoints Ticket tests (5 tests) - ~15 seconds
3. VettingEndpoints status update tests (7 tests) - ~20 seconds
4. VettingEndpoints email integration tests (3 tests) - ~10 seconds
5. VettingEndpoints audit logging tests (2 tests) - ~8 seconds

**Total Integration Test Time**: ~68 seconds (~1.1 minutes)

**Phase 3: E2E Tests** (Slowest - Run Last)
1. Admin login and navigation (2 tests) - ~15 seconds
2. Grid display and filtering (3 tests) - ~20 seconds
3. Application detail view (2 tests) - ~12 seconds
4. Status change modals (4 tests) - ~40 seconds
5. Sorting and pagination (2 tests) - ~10 seconds
6. Error handling (1 test) - ~8 seconds
7. Access control RSVP blocking (2 tests) - ~15 seconds
8. Access control ticket blocking (2 tests) - ~15 seconds

**Total E2E Test Time**: ~135 seconds (~2.25 minutes)

**GRAND TOTAL TEST TIME**: ~3.5 minutes for complete test suite

### Parallel Execution

**Unit Tests**: Can run in parallel (all are isolated)
**Integration Tests**: Can run in parallel with unique database names
**E2E Tests**: Sequential execution recommended (avoid race conditions on Docker)

### CI/CD Test Commands

```bash
# Run all tests in order
npm run test:all

# Run unit tests only (fast feedback)
dotnet test --filter "Category=Unit"

# Run integration tests (requires Docker)
dotnet test --filter "Category=Integration"

# Run E2E tests (requires Docker on port 5173)
cd apps/web && npm run test:e2e
```

---

## CI/CD Integration

### GitHub Actions Workflow

**File**: `.github/workflows/vetting-tests.yml`

```yaml
name: Vetting Workflow Tests

on:
  pull_request:
    paths:
      - 'apps/api/Features/Vetting/**'
      - 'apps/web/src/features/vetting/**'
      - 'apps/web/src/features/admin/vetting/**'
  push:
    branches:
      - main

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'

      - name: Run Unit Tests
        run: dotnet test --filter "Category=Unit" --logger "trx"

      - name: Upload Test Results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: unit-test-results
          path: '**/TestResults/*.trx'

  integration-tests:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_PASSWORD: postgres
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'

      - name: Run Integration Tests
        run: dotnet test --filter "Category=Integration" --logger "trx"
        env:
          ConnectionStrings__DefaultConnection: "Host=postgres;Database=vetting_test;Username=postgres;Password=postgres"

      - name: Upload Test Results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: integration-test-results
          path: '**/TestResults/*.trx'

  e2e-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup Docker Compose
        run: |
          docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
          sleep 30 # Wait for containers to be healthy

      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '20'

      - name: Install Dependencies
        run: |
          cd apps/web
          npm ci

      - name: Install Playwright Browsers
        run: |
          cd apps/web
          npx playwright install chromium

      - name: Run E2E Tests
        run: |
          cd apps/web
          npm run test:e2e

      - name: Upload Test Results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: e2e-test-results
          path: apps/web/test-results/

      - name: Upload Screenshots
        if: failure()
        uses: actions/upload-artifact@v3
        with:
          name: e2e-screenshots
          path: apps/web/test-results/**/*.png
```

### Required Environment Variables (CI)

```bash
# appsettings.Test.json
{
  "Vetting": {
    "EmailEnabled": false, // Mock mode for tests
    "SendGridApiKey": "", // Empty for mock mode
    "FromEmail": "noreply@witchcityrope.com",
    "FromName": "WitchCityRope"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=vetting_test;Username=postgres;Password=postgres"
  }
}
```

---

## Coverage Targets

### Code Coverage Goals

**Unit Tests**:
- **Target**: 80% code coverage minimum
- **Critical Paths**: 100% coverage for:
  - VettingAccessControlService.CanUserRsvpAsync
  - VettingAccessControlService.CanUserPurchaseTicketAsync
  - VettingService.UpdateApplicationStatusAsync
  - VettingService.ApproveApplicationAsync (role grant logic)
  - VettingEmailService.SendStatusUpdateAsync

**Integration Tests**:
- **Target**: All API endpoints tested
- **Critical Endpoints**:
  - POST /api/events/{id}/rsvp (with vetting check)
  - POST /api/events/{id}/tickets (with vetting check)
  - PUT /api/vetting/applications/{id}/status
  - POST /api/vetting/applications/{id}/interview
  - POST /api/vetting/applications/{id}/hold

**E2E Tests**:
- **Target**: Critical user journeys covered
- **Critical Workflows**:
  - Admin approve application → User gains VettedMember role
  - Denied user attempts RSVP → Blocked with error
  - Admin puts application on hold → Email sent to applicant
  - Admin schedules interview → Status updates correctly

### Coverage Reporting

```bash
# Generate coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Install report generator
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator -reports:"**/coverage.opencover.xml" -targetdir:"coverage"

# View report
open coverage/index.html
```

---

## Known Risks & Mitigation

### Risk 1: Email Service Failures

**Risk**: SendGrid failures could block status changes if not handled properly
**Mitigation**:
- Email failures do NOT block status changes (transaction commits even if email fails)
- Unit tests validate this behavior
- Integration tests simulate SendGrid failures
- Email logs capture all failures for manual follow-up

### Risk 2: Concurrent Status Updates

**Risk**: Two admins updating same application simultaneously
**Mitigation**:
- Database concurrency tokens prevent lost updates
- Integration tests simulate DbUpdateConcurrencyException
- Error messages guide admins to refresh and retry

### Risk 3: Role Grant Failures

**Risk**: Application approved but VettedMember role not granted
**Mitigation**:
- ApproveApplicationAsync validates user exists before granting role
- Unit tests specifically validate role grant logic
- Integration tests verify User.Role updated in database
- Audit log tracks all approval actions

### Risk 4: Cache Staleness

**Risk**: Cached vetting status out of sync with database
**Mitigation**:
- 5-minute cache expiration
- Cache invalidation on status changes (TODO: implement)
- Access control uses database for admin operations
- E2E tests validate fresh data after status changes

### Risk 5: Docker Environment Issues

**Risk**: E2E tests fail due to port conflicts or container issues
**Mitigation**:
- Mandatory pre-flight checklist before E2E tests
- kill-local-dev-servers.sh script
- Docker health checks in CI/CD
- Comprehensive error messages guide troubleshooting

---

## Implementation Checklist

### Phase 1: Unit Tests (Priority: CRITICAL)
- [ ] Create VettingAccessControlServiceTests.cs (23 tests)
- [ ] Create VettingEmailServiceTests.cs (20 tests)
- [ ] Create VettingServiceStatusChangeTests.cs (25 tests)
- [ ] Verify 80% code coverage achieved
- [ ] All tests pass in <10 seconds

### Phase 2: Integration Tests (Priority: HIGH)
- [ ] Create ParticipationEndpointsTests.cs (10 tests)
- [ ] Create VettingEndpointsTests.cs (12 tests)
- [ ] Setup TestContainers with PostgreSQL
- [ ] Seed test data (users, applications, events)
- [ ] All tests pass with real database

### Phase 3: E2E Tests (Priority: HIGH)
- [ ] Create admin/vetting-workflow.spec.ts (14 tests)
- [ ] Create access-control/vetting-restrictions.spec.ts (4 tests)
- [ ] Verify Docker-only environment (port 5173)
- [ ] Implement test data helpers
- [ ] Capture screenshots on failures

### Phase 4: CI/CD Integration (Priority: MEDIUM)
- [ ] Create GitHub Actions workflow
- [ ] Configure test environment variables
- [ ] Setup coverage reporting
- [ ] Verify all tests pass in CI

### Phase 5: Documentation (Priority: MEDIUM)
- [ ] Update TEST_CATALOG.md with new tests
- [ ] Document test data requirements
- [ ] Create troubleshooting guide
- [ ] Add lessons learned to test-developer-lessons-learned-2.md

---

## Success Criteria

**Definition of Done**:
- ✅ All 93 tests implemented and passing
- ✅ 80% code coverage achieved
- ✅ All tests run in CI/CD successfully
- ✅ No flaky tests (100% pass rate over 10 runs)
- ✅ Documentation updated in TEST_CATALOG.md
- ✅ Handoff document created for test-executor

**Quality Gates**:
- ✅ Unit tests execute in <10 seconds
- ✅ Integration tests execute in <2 minutes
- ✅ E2E tests execute in <3 minutes
- ✅ All critical workflows validated end-to-end
- ✅ Access control properly enforced and tested

---

## Next Steps

1. **Review this test plan** with stakeholders
2. **Begin implementation** with Phase 1 (Unit Tests)
3. **Create handoff document** for test-executor after implementation
4. **Update TEST_CATALOG.md** with new test inventory
5. **Monitor CI/CD** for any test failures

---

**Document Status**: READY FOR IMPLEMENTATION
**Estimated Implementation Time**: 12-16 hours
**Test Count**: 93 comprehensive tests across all levels
**Coverage**: Complete vetting workflow from access control to email notifications

**Test Developer**: Ready to begin implementation following this plan.
