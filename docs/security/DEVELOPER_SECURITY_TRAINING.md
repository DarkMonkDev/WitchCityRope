# Developer Security Training Guide

This guide provides security training materials and best practices for developers working on the WitchCityRope platform.

## Table of Contents
1. [Security Fundamentals](#security-fundamentals)
2. [Secure Coding Practices](#secure-coding-practices)
3. [Common Vulnerabilities](#common-vulnerabilities)
4. [Security Testing](#security-testing)
5. [Security Resources](#security-resources)

## Security Fundamentals

### The CIA Triad

**Confidentiality**: Ensuring data is only accessible to authorized users
- Implement proper authentication and authorization
- Encrypt sensitive data at rest and in transit
- Follow the principle of least privilege

**Integrity**: Ensuring data remains accurate and unmodified
- Use checksums and digital signatures
- Implement audit trails
- Validate all inputs

**Availability**: Ensuring systems and data are accessible when needed
- Implement proper error handling
- Plan for scalability
- Have disaster recovery procedures

### Defense in Depth

Never rely on a single security control. Implement multiple layers:

```
┌─────────────────────────────────────┐
│         Perimeter Security          │ ← Firewalls, WAF
├─────────────────────────────────────┤
│        Network Security             │ ← VPNs, Segmentation
├─────────────────────────────────────┤
│      Application Security           │ ← Authentication, Authorization
├─────────────────────────────────────┤
│         Data Security               │ ← Encryption, Access Controls
└─────────────────────────────────────┘
```

## Secure Coding Practices

### 1. Never Trust User Input

**Bad Example:**
```csharp
// NEVER DO THIS - SQL Injection vulnerability
public async Task<User> GetUserAsync(string email)
{
    var query = $"SELECT * FROM Users WHERE Email = '{email}'";
    return await _db.ExecuteQueryAsync<User>(query);
}
```

**Good Example:**
```csharp
// ALWAYS use parameterized queries
public async Task<User> GetUserAsync(string email)
{
    var query = "SELECT * FROM Users WHERE Email = @Email";
    var parameters = new { Email = email };
    return await _db.QuerySingleOrDefaultAsync<User>(query, parameters);
}
```

### 2. Validate Everything

**Input Validation Checklist:**
```csharp
public class UserInputValidator
{
    public ValidationResult ValidateUserInput(UserInput input)
    {
        var errors = new List<string>();
        
        // Type validation
        if (!IsValidEmail(input.Email))
            errors.Add("Invalid email format");
            
        // Length validation
        if (input.Username?.Length > 50)
            errors.Add("Username too long");
            
        // Range validation
        if (input.Age < 21 || input.Age > 120)
            errors.Add("Age must be between 21 and 120");
            
        // Format validation
        if (!Regex.IsMatch(input.Phone, @"^\d{3}-\d{3}-\d{4}$"))
            errors.Add("Phone must be in format XXX-XXX-XXXX");
            
        // Business logic validation
        if (IsRestrictedUsername(input.Username))
            errors.Add("Username not allowed");
            
        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
```

### 3. Handle Errors Securely

**Bad Example:**
```csharp
// NEVER expose internal details
catch (Exception ex)
{
    return BadRequest($"Error: {ex.ToString()}"); // Exposes stack trace!
}
```

**Good Example:**
```csharp
// Log details internally, return generic message
catch (SqlException ex)
{
    _logger.LogError(ex, "Database error occurred");
    return StatusCode(500, "An error occurred processing your request");
}
catch (ValidationException ex)
{
    return BadRequest(new { errors = ex.Errors }); // Only safe, expected errors
}
```

### 4. Secure Password Handling

**Password Requirements:**
```csharp
public class PasswordPolicy
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 128;
    public const bool RequireUppercase = true;
    public const bool RequireLowercase = true;
    public const bool RequireDigit = true;
    public const bool RequireSpecialCharacter = true;
    
    public static bool IsValidPassword(string password)
    {
        if (string.IsNullOrEmpty(password)) return false;
        if (password.Length < MinimumLength) return false;
        if (password.Length > MaximumLength) return false;
        
        if (RequireUppercase && !password.Any(char.IsUpper)) return false;
        if (RequireLowercase && !password.Any(char.IsLower)) return false;
        if (RequireDigit && !password.Any(char.IsDigit)) return false;
        if (RequireSpecialCharacter && !Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>]")) return false;
        
        // Check against common passwords
        if (CommonPasswords.Contains(password.ToLower())) return false;
        
        return true;
    }
}
```

### 5. Implement Proper Authorization

**Role-Based Authorization:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class EventController : ControllerBase
{
    // Anyone can view events
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetEvents()
    {
        // Implementation
    }
    
    // Only authenticated users can register
    [HttpPost("{id}/register")]
    [Authorize]
    public async Task<IActionResult> RegisterForEvent(Guid id)
    {
        // Implementation
    }
    
    // Only organizers can create events
    [HttpPost]
    [Authorize(Policy = "RequireOrganizer")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        // Additional check - user can only create their own events
        var userId = User.GetUserId();
        if (request.OrganizerId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }
        
        // Implementation
    }
    
    // Only admins can delete events
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteEvent(Guid id)
    {
        // Implementation
    }
}
```

## Common Vulnerabilities

### OWASP Top 10 for WitchCityRope

#### 1. Injection
**Risk**: Attackers can execute malicious code through input fields
**Prevention**:
- Use parameterized queries
- Validate all inputs
- Use ORMs properly
- Escape special characters

#### 2. Broken Authentication
**Risk**: Attackers can compromise user accounts
**Prevention**:
- Implement strong password policies
- Use secure session management
- Enable MFA
- Implement account lockout

#### 3. Sensitive Data Exposure
**Risk**: Confidential data could be exposed
**Prevention**:
- Encrypt data at rest and in transit
- Use HTTPS everywhere
- Don't log sensitive data
- Implement proper access controls

#### 4. XML External Entities (XXE)
**Risk**: XML processors can be exploited
**Prevention**:
- Disable XML external entity processing
- Use JSON instead of XML where possible
- Validate XML schemas

#### 5. Broken Access Control
**Risk**: Users can access unauthorized resources
**Prevention**:
- Implement proper authorization checks
- Use role-based access control
- Validate object-level permissions
- Default to deny

#### 6. Security Misconfiguration
**Risk**: Default or weak configurations
**Prevention**:
- Remove default accounts
- Disable unnecessary features
- Keep software updated
- Use security headers

#### 7. Cross-Site Scripting (XSS)
**Risk**: Malicious scripts in user browsers
**Prevention**:
- Encode all outputs
- Use Content Security Policy
- Validate inputs
- Use modern frameworks

#### 8. Insecure Deserialization
**Risk**: Malicious object deserialization
**Prevention**:
- Don't deserialize untrusted data
- Use simple data formats
- Implement integrity checks
- Isolate deserialization

#### 9. Using Components with Known Vulnerabilities
**Risk**: Exploitable third-party components
**Prevention**:
- Keep dependencies updated
- Monitor security advisories
- Use dependency scanning
- Remove unused dependencies

#### 10. Insufficient Logging & Monitoring
**Risk**: Attacks go undetected
**Prevention**:
- Log security events
- Monitor for anomalies
- Implement alerting
- Regular log review

## Security Testing

### Unit Testing Security

```csharp
[TestClass]
public class SecurityTests
{
    [TestMethod]
    [DataRow("admin")]
    [DataRow("root")]
    [DataRow("administrator")]
    public void Username_ShouldRejectReservedNames(string username)
    {
        var result = UserValidator.IsValidUsername(username);
        Assert.IsFalse(result);
    }
    
    [TestMethod]
    [DataRow("<script>alert('xss')</script>")]
    [DataRow("'; DROP TABLE Users; --")]
    [DataRow("../../../etc/passwd")]
    public void Input_ShouldBeSanitized(string maliciousInput)
    {
        var sanitized = InputSanitizer.Sanitize(maliciousInput);
        Assert.IsFalse(sanitized.Contains("<script>"));
        Assert.IsFalse(sanitized.Contains("DROP TABLE"));
        Assert.IsFalse(sanitized.Contains("../"));
    }
    
    [TestMethod]
    public async Task Authorization_ShouldEnforceRoles()
    {
        // Arrange
        var controller = new EventController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Role, "Attendee")
                }))
            }
        };
        
        // Act
        var result = await controller.CreateEvent(new CreateEventRequest());
        
        // Assert
        Assert.IsInstanceOfType(result, typeof(ForbidResult));
    }
}
```

### Integration Testing Security

```csharp
[TestClass]
public class SecurityIntegrationTests
{
    [TestMethod]
    public async Task Api_ShouldRequireAuthentication()
    {
        // Arrange
        using var client = _factory.CreateClient();
        
        // Act
        var response = await client.GetAsync("/api/users/profile");
        
        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    [TestMethod]
    public async Task Api_ShouldEnforceRateLimiting()
    {
        // Arrange
        using var client = _factory.CreateClient();
        
        // Act - Make many requests quickly
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 150; i++)
        {
            tasks.Add(client.GetAsync("/api/events"));
        }
        
        var responses = await Task.WhenAll(tasks);
        
        // Assert - Some should be rate limited
        Assert.IsTrue(responses.Any(r => r.StatusCode == HttpStatusCode.TooManyRequests));
    }
}
```

### Security Code Review Checklist

Before submitting a pull request, review:

- [ ] **Authentication & Authorization**
  - [ ] All endpoints have appropriate authorization
  - [ ] User permissions are checked at object level
  - [ ] No hardcoded credentials

- [ ] **Input Validation**
  - [ ] All inputs are validated
  - [ ] No dynamic SQL construction
  - [ ] File uploads are restricted

- [ ] **Output Encoding**
  - [ ] All outputs are encoded
  - [ ] No use of Html.Raw with user input
  - [ ] JSON responses are properly formatted

- [ ] **Error Handling**
  - [ ] No sensitive data in error messages
  - [ ] All exceptions are caught and logged
  - [ ] Generic error messages for users

- [ ] **Cryptography**
  - [ ] Strong algorithms used
  - [ ] No custom crypto implementations
  - [ ] Keys stored securely

- [ ] **Dependencies**
  - [ ] All packages are up to date
  - [ ] No known vulnerabilities
  - [ ] Minimal dependencies used

## Security Resources

### Essential Reading
1. [OWASP Top 10](https://owasp.org/www-project-top-ten/)
2. [OWASP Cheat Sheet Series](https://cheatsheetseries.owasp.org/)
3. [Microsoft Security Documentation](https://docs.microsoft.com/en-us/aspnet/core/security/)

### Security Tools
- **Static Analysis**: SonarQube, Security Code Scan
- **Dependency Scanning**: OWASP Dependency Check, Snyk
- **Dynamic Testing**: OWASP ZAP, Burp Suite
- **Secrets Scanning**: GitLeaks, TruffleHog

### Security Training Platforms
- [OWASP WebGoat](https://owasp.org/www-project-webgoat/)
- [Hack The Box](https://www.hackthebox.eu/)
- [Security Shepherd](https://owasp.org/www-project-security-shepherd/)

### Reporting Security Issues

If you discover a security vulnerability:
1. **DO NOT** create a public GitHub issue
2. Email security@witchcityrope.com
3. Include:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if any)

### Security Champions Program

Join our Security Champions to:
- Get advanced security training
- Review security-critical code
- Help shape security policies
- Mentor other developers

Contact: security-champions@witchcityrope.com

---

## Quick Reference Card

### Secure Coding Do's and Don'ts

**DO:**
- ✅ Validate all inputs
- ✅ Use parameterized queries
- ✅ Encode all outputs
- ✅ Use HTTPS everywhere
- ✅ Hash passwords with BCrypt
- ✅ Implement proper logging
- ✅ Keep dependencies updated
- ✅ Follow least privilege principle

**DON'T:**
- ❌ Trust user input
- ❌ Store passwords in plain text
- ❌ Use MD5 or SHA1 for passwords
- ❌ Expose sensitive data in logs
- ❌ Use eval() or similar functions
- ❌ Hardcode secrets
- ❌ Ignore security warnings
- ❌ Roll your own crypto

---

*Remember: Security is everyone's responsibility. When in doubt, ask the security team!*