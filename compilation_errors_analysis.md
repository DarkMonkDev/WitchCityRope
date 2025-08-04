# Compilation Errors Analysis

## Summary
- **Total Errors**: 118 (59 unique × 2)
- **Projects Affected**: 4

### Errors by Project:
1. **WitchCityRope.E2E.Tests**: 62 errors (31 unique)
2. **WitchCityRope.Web.Tests**: 42 errors (21 unique)
3. **WitchCityRope.Api.Tests**: 8 errors (4 unique)
4. **WitchCityRope.Infrastructure.Tests**: 6 errors (3 unique)

## Detailed Error Breakdown

### 1. WitchCityRope.E2E.Tests (31 unique errors)

#### A. TestDataManager.cs - Property Assignment Errors (10 errors)
- **Error Type**: CS0200 - Property cannot be assigned to -- it is read only
- **Affected Properties**:
  - `WitchCityRopeUser.SceneNameValue`
  - `WitchCityRopeUser.SceneName`
  - `WitchCityRopeUser.EncryptedLegalName`
  - `WitchCityRopeUser.EmailAddress`
  - `WitchCityRopeUser.DateOfBirth`
  - `WitchCityRopeUser.Role`
  - `WitchCityRopeUser.IsActive`
  - `WitchCityRopeUser.CreatedAt`
  - `WitchCityRopeUser.UpdatedAt`
- **Error**: CS0117 - `WitchCityRopeUser.Roles` does not exist

#### B. Playwright Test Errors - StringComparison Arguments (11 errors)
- **Error Type**: CS1503 - Cannot convert from 'System.StringComparison' to 'string'
- **Files Affected**:
  - AuthenticationFlowTests.cs (6 errors)
  - TwoFactorAuthTests.cs (2 errors)
  - VettingApplicationFlowTests.cs (4 errors)
  - UserRegistrationFlowTests.cs (1 error)
  - EventRegistrationFlowTests.cs (1 error)

#### C. Playwright Test Errors - String Array Arguments (3 errors)
- **Error Type**: CS1503 - Cannot convert from 'string[]' to 'string'
- **Files**: AuthenticationFlowTests.cs, TwoFactorAuthTests.cs

#### D. Other E2E Test Errors
- TestDataManager.cs:
  - CS1503 - Argument conversion errors for Price/decimal/string
  - CS1061 - 'Event' does not contain 'Price' property
- DatabaseFixture.cs:
  - CS0029 - Cannot convert 'string[]' to 'Respawn.Graph.Table[]'
- PlaywrightSetup.cs:
  - CS0117 - 'Program' does not contain 'Main'

### 2. WitchCityRope.Web.Tests (21 unique errors)

#### A. TestDataBuilder.cs - EventDto Property Errors (7 errors)
- **Error Type**: CS0117 - EventDto missing properties:
  - `Title`
  - `StartDate`
  - `EndDate`
  - `InstructorName`
  - `RegistrationStatus`
  - `TicketId`
- **Error**: CS0029 - Cannot convert EventType to string

#### B. ServiceMockHelpers.cs - Moq Lambda Errors (4 errors)
- CS0029 & CS1662 - Lambda expression conversion issues with Moq

#### C. Component Test Errors
- LoginComponentTests.cs:
  - CS1061 - 'string' does not contain 'Value' (2 errors)
- EventCardComponentTests.cs:
  - CS0103 - 'EventCallback' does not exist (2 errors)
- MainNavTests.cs:
  - CS0246 - Missing 'IUserService' and 'INotificationService'
- MainLayoutTests.cs:
  - CS0246 - Missing 'INotificationService'
  - CS1503 - Argument conversion errors for timer
  - CS0103 - 'StateHasChanged' does not exist

### 3. WitchCityRope.Api.Tests (4 unique errors)

#### LoginCommandTests.cs
- CS0246 - 'JwtToken' type not found
- CS0234 - 'UserWithAuth' namespace issue
- CS0535 - MockTokenService interface implementation issue

#### AuthServiceTests.cs
- CS0234 - 'UserWithAuth' namespace issue

### 4. WitchCityRope.Infrastructure.Tests (3 unique errors)

#### PayPalServiceTests.cs
- CS1503 - Cannot convert 'Ticket' to 'Registration' (2 errors)
- CS0117 - 'PaymentMetadata' does not contain 'TicketId'

## Most Common Error Patterns

1. **Property Access Issues** (17 errors)
   - Read-only properties being assigned in TestDataManager
   - Missing properties on DTOs and entities

2. **Type Conversion Issues** (23 errors)
   - StringComparison to string conversions in Playwright tests
   - String array to string conversions
   - Type mismatches between Ticket/Registration

3. **Missing Types/Namespaces** (6 errors)
   - INotificationService, IUserService, JwtToken, UserWithAuth

4. **Moq/Testing Framework Issues** (6 errors)
   - Lambda expression conversions
   - EventCallback missing in component tests

## Recommended Fix Order

1. **Fix Infrastructure.Tests first** (3 errors) - Smallest scope
   - Update PayPalService to use Registration instead of Ticket
   - Remove or update PaymentMetadata.TicketId

2. **Fix Api.Tests** (4 errors)
   - Add missing UserWithAuth type or update references
   - Add missing JwtToken type
   - Fix MockTokenService interface implementation

3. **Fix Web.Tests** (21 errors)
   - Update EventDto properties to match current model
   - Fix Moq lambda expressions in ServiceMockHelpers
   - Add missing service interfaces
   - Fix EventCallback references

4. **Fix E2E.Tests** (31 errors)
   - Make WitchCityRopeUser properties settable or use constructor
   - Fix Playwright string comparison method calls
   - Update Respawn configuration
   - Fix Price property references