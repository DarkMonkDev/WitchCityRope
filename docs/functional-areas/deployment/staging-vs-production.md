# Staging vs Production Environment Differences

## Overview

This document outlines the key differences between the staging and production environments for WitchCityRope. Understanding these differences is crucial for proper deployment and testing procedures.

## Configuration Differences

### 1. URLs and Domains

| Component | Staging | Production |
|-----------|---------|------------|
| Main URL | `https://staging.witchcityrope.com` | `https://witchcityrope.com` |
| API URL | `https://api.staging.witchcityrope.com` | `https://api.witchcityrope.com` |
| Admin URL | `https://staging.witchcityrope.com/admin` | `https://witchcityrope.com/admin` |
| Cookie Domain | `.staging.witchcityrope.com` | `.witchcityrope.com` |

### 2. Security Settings

| Setting | Staging | Production |
|---------|---------|------------|
| HSTS Max Age | 86400 (1 day) | 63072000 (2 years) |
| HSTS Preload | false | true |
| Debug Info | Enabled | Disabled |
| Error Details | Detailed | Generic |
| CORS Origins | Includes localhost | Production domains only |
| API Keys | Test keys | Production keys |
| SSL Certificates | Staging certificates | Production certificates |

### 3. Application Settings

```json
// Staging
{
  "Application": {
    "Environment": "Staging",
    "Version": "1.0.0-staging",
    "EnableDebugEndpoints": true,
    "EnableSwagger": true
  }
}

// Production
{
  "Application": {
    "Environment": "Production",
    "Version": "1.0.0",
    "EnableDebugEndpoints": false,
    "EnableSwagger": false
  }
}
```

### 4. Logging Configuration

| Aspect | Staging | Production |
|--------|---------|------------|
| Log Level | Information | Warning |
| Sensitive Data Logging | false | false |
| Console Output | Detailed | Minimal |
| Log Retention | 30 days | 90 days |
| External Logging | Seq (detailed) | Application Insights |
| Performance Logging | Enabled | Selective |

### 5. Email Configuration

| Setting | Staging | Production |
|---------|---------|------------|
| SendGrid Sandbox Mode | true | false |
| From Address | `noreply@staging.witchcityrope.com` | `noreply@witchcityrope.com` |
| Email Subject Prefix | "[STAGING]" | None |
| Test Recipients | Allowed | Restricted |
| Email Rate Limiting | Relaxed | Strict |

### 6. Payment Processing

| Provider | Staging | Production |
|----------|---------|------------|
| PayPal Mode | Sandbox | Live |
| PayPal Credentials | Sandbox credentials | Live credentials |
| Stripe Mode | Test mode | Live mode |
| Webhook URLs | Staging endpoints | Production endpoints |
| Transaction Logging | Verbose | Standard |

### 7. Database Configuration

| Aspect | Staging | Production |
|--------|---------|------------|
| Database Name | `witchcityrope_staging.db` | `witchcityrope.db` |
| Connection Pooling | Standard | Optimized |
| Query Timeout | 60 seconds | 30 seconds |
| Backup Schedule | Daily | Every 4 hours |
| Test Data | Included | None |

### 8. Performance Settings

| Setting | Staging | Production |
|---------|---------|------------|
| Request Rate Limit | 200/minute | 100/minute |
| Cache Duration | 5 minutes | 15 minutes |
| Session Timeout | 30 minutes | 20 minutes |
| Max Upload Size | 10 MB | 10 MB |
| Connection Timeout | 60 seconds | 30 seconds |

## Feature Flags

```csharp
// Staging Features
{
  "Features": {
    "EnableRegistration": true,
    "RequireEmailVerification": true,
    "EnableTwoFactor": true,
    "EnablePayments": true,
    "EnableVetting": true,
    "EnableDebugEndpoints": true,    // Staging only
    "EnableTestAccounts": true,       // Staging only
    "ShowEnvironmentBanner": true     // Staging only
  }
}

// Production Features
{
  "Features": {
    "EnableRegistration": true,
    "RequireEmailVerification": true,
    "EnableTwoFactor": true,
    "EnablePayments": true,
    "EnableVetting": true,
    "EnableDebugEndpoints": false,
    "EnableTestAccounts": false,
    "ShowEnvironmentBanner": false
  }
}
```

## Infrastructure Differences

### 1. Server Specifications

| Resource | Staging | Production |
|----------|---------|------------|
| CPU | 2 vCPUs | 4+ vCPUs |
| RAM | 4 GB | 8+ GB |
| Storage | 20 GB SSD | 100 GB SSD |
| Bandwidth | Standard | Premium |
| Backup Storage | 10 GB | 500 GB |

### 2. Docker Configuration

```yaml
# Staging
services:
  api:
    resources:
      limits:
        cpus: '1.0'
        memory: 1G
      reservations:
        cpus: '0.5'
        memory: 512M

# Production
services:
  api:
    resources:
      limits:
        cpus: '2.0'
        memory: 2G
      reservations:
        cpus: '1.0'
        memory: 1G
```

### 3. Monitoring and Alerts

| Component | Staging | Production |
|-----------|---------|------------|
| Health Check Interval | 60 seconds | 30 seconds |
| Alert Threshold | Relaxed | Strict |
| On-Call Rotation | Business hours | 24/7 |
| Incident Response SLA | Next business day | 1 hour |
| Monitoring Retention | 7 days | 30 days |

## Data Management

### 1. Test Data

**Staging Environment:**
- Pre-populated test users
- Sample events and registrations
- Test payment transactions
- Mock vetting applications

**Production Environment:**
- Real user data only
- No test accounts
- Live transactions
- Actual vetting data

### 2. Data Privacy

| Aspect | Staging | Production |
|--------|---------|------------|
| PII Handling | Synthetic data | Encrypted |
| Data Retention | 30 days | Per policy |
| Access Control | Development team | Restricted |
| Audit Logging | Basic | Comprehensive |
| GDPR Compliance | Testing mode | Full compliance |

## Development Workflows

### 1. Deployment Process

**Staging:**
```bash
# Automated deployment on merge to staging branch
git push origin feature-branch
# Create PR to staging
# Auto-deploy on merge
```

**Production:**
```bash
# Manual approval required
git push origin staging
# Create PR to main
# Manual review and approval
# Scheduled deployment window
```

### 2. Access Control

| Role | Staging Access | Production Access |
|------|----------------|-------------------|
| Developers | Full | Read-only |
| QA Team | Full | Read-only |
| DevOps | Full | Full |
| Admin Users | Test accounts | Restricted accounts |

## Testing Differences

### 1. Automated Testing

| Test Type | Staging | Production |
|-----------|---------|------------|
| Unit Tests | All | N/A |
| Integration Tests | All | Smoke tests only |
| E2E Tests | Full suite | Critical paths |
| Load Tests | Regular | Pre-deployment |
| Security Scans | Weekly | Daily |

### 2. Manual Testing

**Staging:**
- Full regression testing
- New feature testing
- User acceptance testing
- Performance testing
- Security testing

**Production:**
- Smoke testing only
- Monitoring verification
- Critical path validation

## Rollback Procedures

### Staging Rollback

```bash
# Quick rollback (last 5 versions available)
./rollback-staging.sh v1.0.0-staging-previous

# Database included in rollback
# 5-minute downtime acceptable
```

### Production Rollback

```bash
# Careful rollback with approval
./rollback-production.sh v1.0.0-previous --require-approval

# Database migration verification
# Zero-downtime required
# Notification to all stakeholders
```

## Cost Differences

| Component | Staging (Monthly) | Production (Monthly) |
|-----------|-------------------|----------------------|
| Server Hosting | $20-50 | $100-200 |
| SSL Certificates | Free (Let's Encrypt) | Free or EV ($200/year) |
| Monitoring | Free tier | $50-100 |
| Backup Storage | $5 | $20-50 |
| CDN | None | $20-50 |
| Total Estimate | $25-55 | $190-400 |

## Environment-Specific Code

### Staging-Only Features

```csharp
if (Environment.IsStaging())
{
    // Enable debug endpoints
    app.MapGet("/debug/config", () => configuration.AsEnumerable());
    app.MapGet("/debug/health", () => new { status = "healthy", env = "staging" });
    
    // Test data endpoints
    app.MapPost("/api/test/reset-data", TestDataController.ResetData);
    app.MapPost("/api/test/create-users", TestDataController.CreateTestUsers);
}
```

### Production-Only Features

```csharp
if (Environment.IsProduction())
{
    // Enhanced security
    app.UseSecurityHeaders();
    app.UseRateLimiting();
    
    // Production monitoring
    app.UseApplicationInsights();
    app.UseCustomExceptionHandler();
}
```

## Migration Path

### Promoting from Staging to Production

1. **Code Promotion:**
   ```bash
   git checkout main
   git merge staging --no-ff
   git tag -a v1.0.0 -m "Release version 1.0.0"
   git push origin main --tags
   ```

2. **Configuration Updates:**
   - Update environment variables
   - Switch payment providers to live mode
   - Update DNS records
   - Configure production monitoring

3. **Data Migration:**
   - Do NOT migrate staging data
   - Run production-specific seeds
   - Verify data integrity

4. **Validation:**
   - Run production smoke tests
   - Verify SSL certificates
   - Check payment processing
   - Monitor error rates

## Summary Checklist

### Before Deploying to Staging:
- [ ] All tests passing
- [ ] Environment variables configured
- [ ] Test data scripts ready
- [ ] SSL certificates valid
- [ ] Monitoring configured

### Before Promoting to Production:
- [ ] Staging testing complete
- [ ] Production secrets configured
- [ ] Backup procedures tested
- [ ] Rollback plan documented
- [ ] Stakeholders notified
- [ ] Maintenance window scheduled

### Key Differences to Remember:
1. Staging uses test payment credentials
2. Staging has debug endpoints enabled
3. Staging uses shorter security timeouts
4. Staging includes test data
5. Production has stricter rate limits
6. Production requires manual deployment approval
7. Production uses enhanced monitoring