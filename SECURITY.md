# Security Policy

## Overview

WitchCityRope takes security seriously. This document outlines our security policies, procedures, and best practices to ensure the safety and privacy of our community members' data.

## Reporting Security Vulnerabilities

### Responsible Disclosure

We appreciate the security research community's efforts in helping keep our platform secure. If you discover a security vulnerability, please follow responsible disclosure practices:

1. **DO NOT** disclose the vulnerability publicly until we have had a chance to address it
2. **DO NOT** exploit the vulnerability beyond what is necessary to demonstrate the issue
3. **DO NOT** access, modify, or delete other users' data

### How to Report

Please report security vulnerabilities by emailing: **security@witchcityrope.com**

Include the following information:
- Detailed description of the vulnerability
- Steps to reproduce the issue
- Potential impact assessment
- Any proof-of-concept code (if applicable)
- Your contact information for follow-up

### Our Commitment

- We will acknowledge receipt of your report within 48 hours
- We will provide regular updates on our progress
- We will notify you when the vulnerability has been fixed
- We will publicly acknowledge your contribution (unless you prefer to remain anonymous)

## Security Features Implemented

### Authentication & Authorization

#### JWT Token Management
- **Implementation**: JWT tokens with HMAC-SHA256 signature algorithm
- **Token Expiration**: Configurable expiration (default: 60 minutes)
- **Refresh Tokens**: Secure refresh token mechanism with 7-day expiration
- **Token Storage**: Tokens should be stored securely in the client (HttpOnly cookies recommended)

#### Password Security
- **Hashing**: BCrypt with salt rounds of 12
- **Minimum Requirements**: Enforced through validation
- **Password Reset**: Secure token-based password reset flow
- **Account Lockout**: Protection against brute force attacks (planned)

#### Two-Factor Authentication (2FA)
- **Support**: TOTP-based 2FA support (in development)
- **Backup Codes**: Recovery codes for account access
- **Enforcement**: Can be required for privileged accounts

### Data Protection

#### Encryption at Rest
- **PII Encryption**: Legal names are encrypted using AES-256-CBC
- **Database**: SQLite with encrypted fields for sensitive data
- **Key Management**: Encryption keys stored separately from data

#### Encryption in Transit
- **HTTPS**: All communications encrypted with TLS 1.2+
- **HSTS**: HTTP Strict Transport Security enabled in production
- **Certificate Pinning**: Planned for mobile applications

### Access Control

#### Role-Based Access Control (RBAC)
- **Roles**: Attendee, Member, Organizer, VettingTeam, SafetyTeam, Admin
- **Policies**: Fine-grained authorization policies
- **Principle of Least Privilege**: Users only have access to necessary resources

#### API Security
- **Authentication**: Bearer token authentication required
- **Rate Limiting**: Protection against abuse (configurable)
- **CORS**: Properly configured Cross-Origin Resource Sharing
- **Input Validation**: Comprehensive validation on all inputs

### Security Headers

The application implements the following security headers:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `X-XSS-Protection: 1; mode=block`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Content-Security-Policy` (planned)

### Audit & Monitoring

#### Logging
- **Authentication Events**: Login attempts, failures, and successes
- **Authorization Failures**: Unauthorized access attempts
- **Data Access**: Sensitive data access logging
- **Error Logging**: Secure error handling without exposing sensitive information

#### Incident Response
- **Incident Reports**: Anonymous incident reporting system
- **Safety Team Review**: Dedicated team for handling safety concerns
- **Audit Trail**: Comprehensive audit logging for administrative actions

## Known Security Considerations

### Current Limitations

1. **Email Verification**: Currently using simple token-based verification
   - Enhancement: Time-limited tokens with secure random generation

2. **Session Management**: JWT-based with refresh tokens
   - Enhancement: Consider implementing token revocation list

3. **Rate Limiting**: Basic implementation
   - Enhancement: Implement sophisticated rate limiting per endpoint

4. **Payment Security**: PCI compliance considerations
   - Using Stripe/PayPal for payment processing
   - No credit card data stored locally

### Planned Security Enhancements

1. **Security Scanning**
   - Regular dependency vulnerability scanning
   - Static code analysis integration
   - Dynamic security testing

2. **Infrastructure Security**
   - Web Application Firewall (WAF)
   - DDoS protection
   - Regular security audits

3. **Privacy Enhancements**
   - Data minimization practices
   - Right to erasure (GDPR compliance)
   - Privacy-preserving analytics

## Security Best Practices for Contributors

### Development Guidelines

1. **Never commit secrets**: Use environment variables for sensitive configuration
2. **Input Validation**: Always validate and sanitize user input
3. **Output Encoding**: Properly encode output to prevent XSS
4. **SQL Injection Prevention**: Use parameterized queries
5. **Authentication Checks**: Verify authentication and authorization on every request
6. **Error Handling**: Never expose sensitive information in error messages

### Code Review Checklist

Before merging code, ensure:
- [ ] No hardcoded secrets or credentials
- [ ] All user inputs are validated
- [ ] Authentication and authorization checks are in place
- [ ] Sensitive data is properly encrypted
- [ ] Error messages don't leak sensitive information
- [ ] Dependencies are up-to-date and vulnerability-free

### Testing Security

- Write security-focused unit tests
- Include negative test cases
- Test authorization boundaries
- Verify rate limiting works as expected
- Test input validation thoroughly

## Compliance

### GDPR Compliance
- Right to access personal data
- Right to rectification
- Right to erasure ("right to be forgotten")
- Data portability
- Privacy by design

### Age Verification
- Platform requires users to be 21+ years old
- Age verification during registration
- Legal compliance with local regulations

## Contact

For security concerns, please contact:
- **Email**: security@witchcityrope.com
- **Response Time**: Within 48 hours

For general support:
- **Email**: support@witchcityrope.com
- **Documentation**: See our security documentation in `/docs/security/`

---

*Last Updated: December 2024*
*Version: 1.0*