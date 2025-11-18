# Test Developer Handoff - Update Email Tests for Hardcoded Variables

**Date**: 2025-11-17
**Agent**: test-developer
**Estimated Time**: 1-2 hours
**Priority**: Medium (runs after backend changes complete)

## Context

We're removing static variables (`{{support_email}}`, `{{contact_email}}`, `{{organizer_email}}`, `{{system_url}}`) from email templates. Services no longer populate these in variable dictionaries, and templates contain hardcoded values instead.

Tests that mock `SendTemplatedEmailAsync()` or verify email sending need updates to match the new implementation.

## Your Tasks

### Task 1: Update Unit Tests - Remove Static Variables from Mocks

**Files to Update**:

1. **AuthenticationServiceTests.cs** (`tests/unit/api/Features/Auth/AuthenticationServiceTests.cs`)

**Search for lines around 945, 965, 998, 1049, 1066, 1103, 1116** that verify `SendTemplatedEmailAsync` calls.

**Current Test Code** (example):
```csharp
await _emailService.Received(1).SendTemplatedEmailAsync(
    email,
    user.SceneName ?? email,
    EmailCategory.Admin,
    "PasswordReset",
    Arg.Is<Dictionary<string, string>>(vars =>
        vars.ContainsKey("user_name") &&
        vars.ContainsKey("reset_url") &&
        vars.ContainsKey("support_email")), // REMOVE THIS ASSERTION
    Arg.Any<CancellationToken>());
```

**Updated Test Code**:
```csharp
await _emailService.Received(1).SendTemplatedEmailAsync(
    email,
    user.SceneName ?? email,
    EmailCategory.Admin,
    "PasswordReset",
    Arg.Is<Dictionary<string, string>>(vars =>
        vars.ContainsKey("user_name") &&
        vars.ContainsKey("reset_url") &&
        !vars.ContainsKey("support_email")), // VERIFY IT'S NOT THERE
    Arg.Any<CancellationToken>());
```

**What to Do**:
- Find all tests that verify `SendTemplatedEmailAsync` was called with specific variables
- Remove assertions for `support_email`, `contact_email`, `organizer_email`, `system_url`
- Optionally add assertions that these variables are NOT in the dictionary

2. **RefundServiceEmailTests.cs** (`tests/unit/api/Features/Payments/Services/RefundServiceEmailTests.cs`)

Search for `SendTemplatedEmailAsync` verifications and remove `support_email` assertions.

### Task 2: Add New Tests - Verify Templates Have Hardcoded Values

**File**: Create new test file or add to existing `EmailTemplateEndpointsTests.cs`

**Purpose**: Verify that seeded templates contain hardcoded values (not variable placeholders)

**New Test Example**:
```csharp
[Fact]
public async Task SeedEmailTemplates_VettingTemplates_ShouldContainHardcodedContactEmail()
{
    // Arrange
    var seeder = new EmailTemplateSeeder(_context, _logger);

    // Act
    await seeder.SeedAsync();

    // Assert
    var vettingTemplate = await _context.GlobalEmailTemplates
        .FirstOrDefaultAsync(t =>
            t.Category == EmailCategory.Vetting &&
            t.TemplateType == "ApplicationReceived");

    vettingTemplate.Should().NotBeNull();

    // Verify hardcoded email in HTML body
    vettingTemplate!.HtmlBody.Should().Contain("info@witchcityrope.com");
    vettingTemplate.HtmlBody.Should().NotContain("{{contact_email}}");

    // Verify hardcoded email in plain text body
    vettingTemplate.PlainTextBody.Should().Contain("info@witchcityrope.com");
    vettingTemplate.PlainTextBody.Should().NotContain("{{contact_email}}");

    // Verify Variables field doesn't include contact_email
    var variables = JsonSerializer.Deserialize<string[]>(vettingTemplate.Variables);
    variables.Should().NotContain("{{contact_email}}");
}

[Fact]
public async Task SeedEmailTemplates_AdminTemplates_ShouldContainHardcodedSupportEmail()
{
    // Arrange
    var seeder = new EmailTemplateSeeder(_context, _logger);

    // Act
    await seeder.SeedAsync();

    // Assert
    var adminTemplate = await _context.GlobalEmailTemplates
        .FirstOrDefaultAsync(t =>
            t.Category == EmailCategory.Admin &&
            t.TemplateType == "PasswordReset");

    adminTemplate.Should().NotBeNull();

    // Verify hardcoded email
    adminTemplate!.HtmlBody.Should().Contain("support@witchcityrope.com");
    adminTemplate.HtmlBody.Should().NotContain("{{support_email}}");

    adminTemplate.PlainTextBody.Should().Contain("support@witchcityrope.com");
    adminTemplate.PlainTextBody.Should().NotContain("{{support_email}}");

    // Verify Variables field doesn't include support_email
    var variables = JsonSerializer.Deserialize<string[]>(adminTemplate.Variables);
    variables.Should().NotContain("{{support_email}}");
}

[Fact]
public async Task SeedEmailTemplates_EventsTemplates_ShouldContainHardcodedOrganizerEmail()
{
    // Arrange
    var seeder = new EmailTemplateSeeder(_context, _logger);

    // Act
    await seeder.SeedAsync();

    // Assert
    var eventsTemplate = await _context.GlobalEmailTemplates
        .FirstOrDefaultAsync(t =>
            t.Category == EmailCategory.Events &&
            t.TemplateType == "Confirmation");

    eventsTemplate.Should().NotBeNull();

    // Verify hardcoded email
    eventsTemplate!.HtmlBody.Should().Contain("events@witchcityrope.com");
    eventsTemplate.HtmlBody.Should().NotContain("{{organizer_email}}");

    eventsTemplate.PlainTextBody.Should().Contain("events@witchcityrope.com");
    eventsTemplate.PlainTextBody.Should().NotContain("{{organizer_email}}");

    // Verify Variables field doesn't include organizer_email
    var variables = JsonSerializer.Deserialize<string[]>(eventsTemplate.Variables);
    variables.Should().NotContain("{{organizer_email}}");
}
```

### Task 3: Update Integration Tests

**File**: `tests/integration/api/Features/EmailTemplates/EmailTemplateEndpointsIntegrationTests.cs`

**What to Check**:
- Tests that create/update templates
- Tests that verify template content
- Tests that send emails using templates

**Expected Changes**:
- Variable substitution tests should only test dynamic variables
- Template content assertions should expect hardcoded values
- No tests should populate static variables in dictionaries

### Task 4: Search for All Email-Related Test Assertions

Run comprehensive search for tests that might be affected:

```bash
# From project root
grep -rn "support_email" tests/ --include="*.cs"
grep -rn "contact_email" tests/ --include="*.cs"
grep -rn "organizer_email" tests/ --include="*.cs"
grep -rn "system_url" tests/ --include="*.cs"

# Find all SendTemplatedEmailAsync verifications
grep -rn "SendTemplatedEmailAsync" tests/ --include="*.cs" -A 10
```

Update any tests found to not expect static variables in dictionaries.

## Testing Your Changes

**Run All Tests**:
```bash
# Run unit tests
dotnet test tests/unit/api/

# Run integration tests (ensure database is clean)
dotnet test tests/integration/api/

# Check for failures
```

**Verify Test Coverage**:
- All email-sending services have tests updated
- Template seeding has tests verifying hardcoded values
- No tests fail due to missing static variables

## Success Criteria

- ✅ All unit tests pass
- ✅ All integration tests pass
- ✅ No test assertions check for `support_email`, `contact_email`, `organizer_email`, `system_url` in variables dictionaries
- ✅ New tests verify templates contain hardcoded email addresses
- ✅ New tests verify Variables field excludes static variables

## Files You'll Modify

**Definitely**:
- `tests/unit/api/Features/Auth/AuthenticationServiceTests.cs` (multiple test methods)
- `tests/unit/api/Features/Payments/Services/RefundServiceEmailTests.cs`

**Possibly**:
- `tests/unit/api/Features/EmailTemplates/EmailTemplateEndpointsTests.cs` (add new tests)
- `tests/integration/api/Features/EmailTemplates/EmailTemplateEndpointsIntegrationTests.cs`
- Any other test files found via grep

**New** (if creating):
- Could create `tests/unit/api/Services/Seeding/EmailTemplateSeederTests.cs` for comprehensive template validation

## Common Test Patterns to Update

**Before**:
```csharp
// Verify static variable was included
vars.ContainsKey("support_email").Should().BeTrue();
vars["support_email"].Should().Be("support@witchcityrope.com");
```

**After**:
```csharp
// Verify static variable is NOT included
vars.ContainsKey("support_email").Should().BeFalse();

// Or use negative assertion in Arg.Is:
Arg.Is<Dictionary<string, string>>(vars =>
    !vars.ContainsKey("support_email") &&
    !vars.ContainsKey("contact_email"))
```

## Edge Cases to Consider

1. **Mock setup**: If tests use `_emailService.SendTemplatedEmailAsync().Returns(Result.Success())`, no changes needed
2. **Variable count assertions**: If tests assert exact variable count, update the expected count
3. **Template content tests**: If tests verify template HTML/text, update expectations to include hardcoded values

## Questions or Issues?

If you encounter:
- **Tests failing for unclear reasons**: Check if test data setup includes static variables
- **Mocking issues**: Ensure mocks don't validate static variables in dictionaries
- **Integration test failures**: Verify database migrations ran successfully

## Next Steps After Completion

1. Run full test suite and verify 100% pass rate
2. Report test coverage metrics
3. Document any new tests added
4. Commit test updates with descriptive message

**Implementation Plan**: `/docs/functional-areas/email-templates/new-work/2025-11-17-hardcode-static-variables/implementation-plan.md`
