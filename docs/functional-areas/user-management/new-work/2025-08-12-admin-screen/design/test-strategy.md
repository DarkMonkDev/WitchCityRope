# Test Strategy: User Management Admin Screen Redesign
<!-- Last Updated: 2025-08-12 -->
<!-- Version: 1.0 -->
<!-- Owner: Test Automation Engineer -->
<!-- Status: Draft -->

## 1. Test Scope & Objectives

### Primary Objectives
- **Validate vetting workflow** with admin notes and status tracking
- **Ensure role-based access** (Admin vs Event Organizer permissions)
- **Verify performance requirements** (2-second load time, 1000+ user capacity)
- **Test admin notes functionality** (5000 char limit, persistence, audit)
- **Validate audit trail logging** for all administrative actions
- **Confirm SendGrid email notifications** trigger correctly

### Success Criteria
- **Functional**: 100% critical scenarios pass
- **Performance**: Page loads in <2s with 1000+ users
- **Security**: All role-based restrictions enforced
- **Data Integrity**: No data loss during vetting state transitions
- **Audit Coverage**: All admin actions logged with proper details

### Out of Scope
- Third-party integrations beyond SendGrid
- Legacy user data migration testing
- Mobile-specific responsive testing (covered in separate suite)
- Load testing beyond 1000 concurrent users

## 2. Critical Test Scenarios (Top 15)

### Vetting Workflow (Priority 1)
1. **VET-001**: Complete vetting workflow from NotStarted → Approved
2. **VET-002**: Reject vetting application with required admin note
3. **VET-003**: Transition vetting status with audit trail validation
4. **VET-004**: Bulk vetting operations (approve/reject multiple users)
5. **VET-005**: Vetting status email notifications via SendGrid

### Admin Notes Management (Priority 1)
6. **NOTE-001**: Create admin note with 5000 character limit validation
7. **NOTE-002**: Edit existing admin note with audit trail
8. **NOTE-003**: Admin notes persistence across page refreshes
9. **NOTE-004**: Admin notes visibility based on role permissions

### Role-Based Access (Priority 1)
10. **RBAC-001**: Administrator access to all user management features
11. **RBAC-002**: Event Organizer restricted access validation
12. **RBAC-003**: Unauthorized role access prevention

### Performance & Scale (Priority 1)
13. **PERF-001**: Page load performance with 1000+ users displayed
14. **PERF-002**: Search/filter response time under load
15. **PERF-003**: Real-time updates without performance degradation

## 3. Test Types & Tools

### Unit Tests (xUnit + Moq + FluentAssertions)
**Location**: `/tests/WitchCityRope.Api.Tests/Features/Admin/Users/`

```csharp
// Example Test Structure
public class AdminUserManagementServiceTests
{
    private readonly Mock<IUserRepository> _userRepo;
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<IAuditService> _auditService;
    private readonly AdminUserManagementService _sut;

    [Fact]
    public async Task UpdateVettingStatus_WithValidData_CreatesAuditEntry()
    {
        // Arrange
        var request = new UpdateVettingStatusRequest
        {
            UserId = Guid.NewGuid(),
            NewStatus = VettingStatus.Approved,
            AdminNote = "User meets all requirements"
        };

        // Act
        var result = await _sut.UpdateVettingStatusAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditService.Verify(x => x.LogAsync(
            It.Is<AuditEntry>(a => a.Action == "VettingStatusUpdate" && 
                                  a.Details.Contains("Approved")), 
            default), Times.Once);
    }
}
```

**Coverage Areas**:
- Vetting workflow business logic
- Admin notes validation and persistence
- Role-based permission checking
- Email notification triggering
- Audit trail creation

### Integration Tests (ASP.NET Core TestHost)
**Location**: `/tests/WitchCityRope.IntegrationTests/Admin/UserManagement/`

```csharp
public class AdminUserManagementIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetUsers_AsAdmin_ReturnsFullUserDetails()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        
        // Act
        var response = await _client.GetAsync("/api/admin/users?include=notes,vetting");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await DeserializeAsync<PagedResult<AdminUserDto>>(response);
        users.Items.Should().AllSatisfy(u => 
        {
            u.AdminNotes.Should().NotBeNull();
            u.VettingHistory.Should().NotBeNull();
        });
    }
}
```

**Coverage Areas**:
- API endpoint functionality
- Database integration
- Authentication/authorization
- Cross-service communication

### Component Tests (bUnit)
**Location**: `/tests/WitchCityRope.Web.ComponentTests/Admin/Users/`

```csharp
public class UserManagementGridComponentTests : TestContext
{
    [Fact]
    public void VettingStatusDropdown_OnChange_TriggersUpdateWithNote()
    {
        // Arrange
        Services.AddMockUserManagementService();
        var component = RenderComponent<UserManagementGrid>();
        
        // Act
        var dropdown = component.Find("select[data-testid='vetting-status']");
        dropdown.Change(VettingStatus.Approved.ToString());
        
        var noteModal = component.Find("[data-testid='admin-note-modal']");
        var noteInput = noteModal.Find("textarea");
        noteInput.Change("User demonstrates good understanding of safety");
        
        var saveButton = noteModal.Find("button[data-testid='save-note']");
        saveButton.Click();
        
        // Assert
        MockUserService.Verify(x => x.UpdateVettingStatusAsync(
            It.Is<UpdateVettingStatusRequest>(r => 
                r.NewStatus == VettingStatus.Approved &&
                r.AdminNote.Contains("safety")), 
            default), Times.Once);
    }
}
```

**Coverage Areas**:
- Blazor component interactions
- Form validation
- Real-time UI updates
- Modal dialogs and workflows

### E2E Tests (Playwright)
**Location**: `/tests/playwright/admin/user-management/`

```typescript
// user-management.spec.ts
import { test, expect } from '@playwright/test';
import { AdminPage } from '../../pages/admin.page';
import { UserManagementPage } from '../../pages/admin/user-management.page';

test.describe('Admin User Management', () => {
  let adminPage: AdminPage;
  let userManagement: UserManagementPage;

  test.beforeEach(async ({ page }) => {
    adminPage = new AdminPage(page);
    userManagement = new UserManagementPage(page);
    
    await adminPage.loginAsAdmin();
    await userManagement.navigate();
  });

  test('complete vetting workflow with admin notes', async ({ page }) => {
    // Find user pending vetting
    await userManagement.searchUser('test-user@example.com');
    
    // Update vetting status
    await userManagement.updateVettingStatus('Approved');
    
    // Add required admin note
    await userManagement.addAdminNote(
      'User completed interview successfully. Strong understanding of consent and safety protocols.'
    );
    
    // Verify status updated
    await expect(userManagement.getVettingStatus('test-user@example.com'))
      .toHaveText('Approved');
    
    // Verify audit trail
    await userManagement.viewAuditTrail('test-user@example.com');
    await expect(page.locator('[data-testid="audit-entry"]').first())
      .toContainText('Vetting status updated to Approved');
  });

  test('performance: load 1000+ users under 2 seconds', async ({ page }) => {
    const startTime = Date.now();
    
    await userManagement.loadAllUsers();
    
    const loadTime = Date.now() - startTime;
    expect(loadTime).toBeLessThan(2000);
    
    // Verify user count
    const userCount = await userManagement.getUserCount();
    expect(userCount).toBeGreaterThan(1000);
  });
});
```

**Coverage Areas**:
- End-to-end user workflows
- Browser compatibility
- Performance validation
- Visual regression testing

## 4. Test Data Strategy

### Test Data Requirements
```csharp
// Test Data Builder Pattern
public class AdminUserTestDataBuilder
{
    private VettingStatus _vettingStatus = VettingStatus.NotStarted;
    private List<AdminNote> _notes = new();
    private MembershipLevel _membership = MembershipLevel.Member;

    public AdminUserTestDataBuilder WithVettingStatus(VettingStatus status)
    {
        _vettingStatus = status;
        return this;
    }

    public AdminUserTestDataBuilder WithAdminNote(string content, string adminId = null)
    {
        _notes.Add(new AdminNote 
        { 
            Content = content, 
            CreatedBy = adminId ?? "test-admin-id",
            CreatedAt = DateTime.UtcNow 
        });
        return this;
    }

    public User Build()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = $"test-{Guid.NewGuid()}@example.com",
            UserExtended = new UserExtended
            {
                VettingStatus = _vettingStatus,
                MembershipLevel = _membership,
                AdminNotes = _notes
            }
        };
    }
}
```

### Data Scenarios
- **Small Dataset**: 10 users for basic functionality
- **Medium Dataset**: 100 users for search/filter testing  
- **Large Dataset**: 1000+ users for performance testing
- **Edge Cases**: Users with max-length notes, multiple vetting transitions
- **Role Variations**: Admin, Event Organizer, different permission levels

### Database Setup
```csharp
// Docker test database for integration tests
public class TestDatabaseFixture
{
    private readonly PostgreSqlContainer _container;
    
    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("witchcityrope_test")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .Build();
            
        await _container.StartAsync();
        await SeedTestDataAsync();
    }
}
```

## 5. Performance Criteria

### Response Time Requirements
| Operation | Target | Maximum |
|-----------|---------|---------|
| Page Load (50 users/page) | <1.0s | <1.5s |
| User Search/Filter | <300ms | <500ms |
| Status Update | <200ms | <400ms |
| Admin Notes Save | <150ms | <300ms |
| Edit User Modal Open | <100ms | <200ms |
| Pagination Navigation | <200ms | <400ms |

### Load Testing Scenarios
```csharp
[Fact]
public async Task UserGrid_WithThousandUsers_LoadsWithinTimeLimit()
{
    // Arrange
    var users = new AdminUserTestDataBuilder().Build(1000);
    await SeedDatabaseAsync(users);
    
    var stopwatch = Stopwatch.StartNew();
    
    // Act
    var result = await _client.GetAsync("/api/admin/users");
    
    // Assert
    stopwatch.Stop();
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000);
    result.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

### Memory Usage Limits
- **Client-side**: Max 50MB for user grid component
- **Server-side**: Max 100MB for user management operations
- **Database**: Efficient queries with proper indexing

## 6. Security Test Cases

### Authentication Tests
- **AUTH-001**: Unauthenticated access denied to admin endpoints
- **AUTH-002**: Session timeout redirects to login
- **AUTH-003**: JWT token validation and refresh

### Authorization Tests  
- **AUTHZ-001**: Admin role can access all user management features
- **AUTHZ-002**: Event Organizer role restricted to read-only operations
- **AUTHZ-003**: Regular users cannot access admin endpoints
- **AUTHZ-004**: Cross-user data access prevention

### Input Validation Tests
```csharp
[Theory]
[InlineData("<script>alert('xss')</script>")]
[InlineData("'; DROP TABLE users; --")]
[InlineData(null)]
[InlineData("")]
public async Task AdminNote_WithInvalidInput_ReturnsValidationError(string maliciousInput)
{
    // Arrange & Act
    var result = await _service.AddAdminNoteAsync(userId, maliciousInput);
    
    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Error.Should().Contain("validation");
}
```

### Data Protection Tests
- **DATA-001**: Admin notes encrypted at rest
- **DATA-002**: Audit trail data immutable after creation
- **DATA-003**: PII handled according to privacy requirements
- **DATA-004**: Secure deletion of sensitive data

## 7. Coverage Targets

### Code Coverage Requirements
- **Unit Tests**: 90% line coverage for business logic
- **Integration Tests**: 80% API endpoint coverage
- **Component Tests**: 85% UI component coverage
- **E2E Tests**: 100% critical user journey coverage

### Functional Coverage
```yaml
Vetting Workflow:
  - Status transitions: 100%
  - Email notifications: 100%
  - Admin note requirements: 100%
  
Admin Notes:
  - CRUD operations: 100%
  - Character limits: 100%
  - Permission checks: 100%
  
Role-Based Access:
  - Admin permissions: 100%
  - Event Organizer restrictions: 100%
  - Unauthorized access prevention: 100%
```

### Test Execution Matrix
| Test Type | Development | CI/CD | Release |
|-----------|-------------|-------|---------|
| Unit | All | All | All |
| Integration | Smoke | All | All |
| Component | Changed | All | All |
| E2E Critical | Manual | Automated | Automated |
| E2E Full | Weekly | Release | Release |
| Performance | Manual | Nightly | Release |

## 8. Risk Areas

### High Risk Areas
1. **Vetting State Transitions**
   - Risk: Data corruption during status updates
   - Mitigation: Database transactions and validation
   - Tests: State transition matrix validation

2. **Admin Notes Persistence**
   - Risk: Note loss during concurrent edits
   - Mitigation: Optimistic concurrency control
   - Tests: Concurrent edit scenarios

3. **Performance with Large Datasets**
   - Risk: Page timeouts with 1000+ users
   - Mitigation: Pagination and lazy loading
   - Tests: Load testing with realistic data volumes

4. **Role-Based Security**
   - Risk: Privilege escalation vulnerabilities
   - Mitigation: Server-side permission checks
   - Tests: Comprehensive authorization test matrix

### Medium Risk Areas
1. **Email Notification Delivery**
   - Risk: SendGrid integration failures
   - Mitigation: Retry logic and fallback options
   - Tests: Integration tests with SendGrid sandbox

2. **Audit Trail Integrity**
   - Risk: Missing or corrupted audit entries
   - Mitigation: Database constraints and validation
   - Tests: Audit coverage verification

3. **Browser Compatibility**
   - Risk: Feature inconsistencies across browsers
   - Mitigation: Cross-browser E2E testing
   - Tests: Playwright multi-browser execution

### Low Risk Areas
1. **UI Visual Consistency**
   - Risk: Minor styling inconsistencies
   - Mitigation: Component-based architecture
   - Tests: Visual regression testing

2. **Search Performance**
   - Risk: Slow search with complex filters
   - Mitigation: Database indexing optimization
   - Tests: Search response time validation

## Test Execution Plan

### Development Phase
```bash
# Run unit tests during development
dotnet test tests/WitchCityRope.Api.Tests/ --filter "Category=Unit"

# Run component tests for UI changes
dotnet test tests/WitchCityRope.Web.ComponentTests/ 

# Run integration tests for API changes
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=UserManagement"
```

### CI/CD Pipeline
```yaml
test_pipeline:
  stages:
    - unit_tests: All unit tests
    - integration_tests: All integration tests  
    - component_tests: All component tests
    - e2e_critical: Critical user journeys only
    - performance_smoke: Basic performance validation
```

### Pre-Release Validation
```bash
# Full test suite execution
npm run test:e2e:playwright -- --config=production
dotnet test --collect:"XPlat Code Coverage" --results-directory:./coverage
```

### Monitoring & Metrics
- **Test Execution Time**: Target <10 minutes for full suite
- **Flaky Test Rate**: <2% failure rate due to test instability
- **Coverage Trends**: Weekly coverage reports and trend analysis
- **Performance Benchmarks**: Automated performance regression detection

## Test Environment Configuration

### Local Development
```json
{
  "TestDatabase": "Host=localhost;Port=5433;Database=witchcityrope_test",
  "EmailService": "Mock",
  "AuditService": "InMemory",
  "UserDataSize": "Small"
}
```

### CI/CD Environment  
```json
{
  "TestDatabase": "Docker PostgreSQL Container",
  "EmailService": "SendGrid Sandbox",
  "AuditService": "Database",
  "UserDataSize": "Medium"
}
```

### Performance Testing
```json
{
  "TestDatabase": "Dedicated PostgreSQL Instance", 
  "EmailService": "Mock",
  "AuditService": "Database",
  "UserDataSize": "Large"
}
```

This comprehensive test strategy ensures thorough validation of the User Management Admin Screen Redesign while maintaining focus on the critical business requirements and performance expectations.