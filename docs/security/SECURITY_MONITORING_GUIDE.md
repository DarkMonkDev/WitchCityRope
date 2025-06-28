# Security Monitoring and Incident Response Guide

This guide outlines the security monitoring procedures and incident response protocols for the WitchCityRope platform.

## Table of Contents
1. [Security Monitoring](#security-monitoring)
2. [Threat Detection](#threat-detection)
3. [Incident Response Plan](#incident-response-plan)
4. [Post-Incident Procedures](#post-incident-procedures)
5. [Security Metrics](#security-metrics)

## Security Monitoring

### Real-Time Monitoring

#### Authentication Monitoring
```csharp
public class AuthenticationMonitor
{
    private readonly ILogger<AuthenticationMonitor> _logger;
    private readonly IMemoryCache _cache;
    private readonly INotificationService _notificationService;
    
    public async Task MonitorLoginAttemptAsync(string email, string ipAddress, bool success)
    {
        // Log the attempt
        _logger.LogInformation("Login attempt: {Email} from {IP} - Success: {Success}", 
            email, ipAddress, success);
        
        // Track failed attempts
        if (!success)
        {
            var key = $"failed_login:{email}";
            var attempts = _cache.Get<int>(key) + 1;
            _cache.Set(key, attempts, TimeSpan.FromHours(1));
            
            // Alert on suspicious activity
            if (attempts >= 3)
            {
                await _notificationService.SendSecurityAlertAsync(
                    $"Multiple failed login attempts for {email} from {ipAddress}");
            }
        }
        
        // Check for unusual patterns
        await CheckUnusualLoginPatternAsync(email, ipAddress);
    }
    
    private async Task CheckUnusualLoginPatternAsync(string email, string ipAddress)
    {
        // Check for geographic anomalies
        var lastLocation = await GetLastKnownLocationAsync(email);
        var currentLocation = await GetLocationFromIpAsync(ipAddress);
        
        if (lastLocation != null && currentLocation != null)
        {
            var distance = CalculateDistance(lastLocation, currentLocation);
            var timeDiff = DateTime.UtcNow - lastLocation.Timestamp;
            
            // Impossible travel detection
            if (distance > 1000 && timeDiff.TotalHours < 2)
            {
                await _notificationService.SendSecurityAlertAsync(
                    $"Impossible travel detected for {email}: {distance}km in {timeDiff.TotalMinutes} minutes");
            }
        }
    }
}
```

#### API Monitoring
```csharp
public class ApiMonitor
{
    private readonly ILogger<ApiMonitor> _logger;
    private readonly IMetricsCollector _metrics;
    
    public async Task RecordApiCallAsync(HttpContext context, TimeSpan duration)
    {
        var endpoint = $"{context.Request.Method} {context.Request.Path}";
        var statusCode = context.Response.StatusCode;
        var userId = context.User?.GetUserIdOrNull();
        
        // Record metrics
        await _metrics.RecordAsync(new ApiMetric
        {
            Endpoint = endpoint,
            StatusCode = statusCode,
            Duration = duration,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        });
        
        // Log slow requests
        if (duration.TotalMilliseconds > 1000)
        {
            _logger.LogWarning("Slow API call: {Endpoint} took {Duration}ms", 
                endpoint, duration.TotalMilliseconds);
        }
        
        // Log errors
        if (statusCode >= 400)
        {
            _logger.LogError("API error: {Endpoint} returned {StatusCode} for user {UserId}", 
                endpoint, statusCode, userId);
        }
    }
}
```

### Security Event Logging

#### Structured Logging Configuration
```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.File", "Serilog.Sinks.Elasticsearch"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "WitchCityRope.Security": "Debug"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/security-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "Elasticsearch",
        "Args": {
          "nodeUris": "http://localhost:9200",
          "indexFormat": "security-logs-{0:yyyy.MM}",
          "autoRegisterTemplate": true
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

#### Security Event Types
```csharp
public enum SecurityEventType
{
    // Authentication
    LoginSuccess,
    LoginFailure,
    AccountLocked,
    PasswordChanged,
    TwoFactorEnabled,
    TwoFactorDisabled,
    
    // Authorization
    UnauthorizedAccess,
    PrivilegeEscalation,
    RoleChanged,
    
    // Data Access
    SensitiveDataAccess,
    BulkDataExport,
    DataModification,
    
    // System
    ConfigurationChanged,
    SecurityPolicyUpdated,
    AnomalousActivity
}

public class SecurityEventLogger
{
    private readonly ILogger<SecurityEventLogger> _logger;
    
    public void LogSecurityEvent(SecurityEventType eventType, string userId, 
        string details, Dictionary<string, object> additionalData = null)
    {
        var logData = new
        {
            EventType = eventType.ToString(),
            UserId = userId,
            Details = details,
            Timestamp = DateTime.UtcNow,
            AdditionalData = additionalData
        };
        
        _logger.LogInformation("SECURITY_EVENT: {@SecurityEvent}", logData);
    }
}
```

## Threat Detection

### Automated Threat Detection Rules

```csharp
public class ThreatDetectionService
{
    private readonly ILogger<ThreatDetectionService> _logger;
    private readonly INotificationService _notificationService;
    
    public async Task<ThreatLevel> AnalyzeActivityAsync(UserActivity activity)
    {
        var threatIndicators = new List<ThreatIndicator>();
        
        // Check for brute force attacks
        if (activity.FailedLoginAttempts > 5)
        {
            threatIndicators.Add(new ThreatIndicator
            {
                Type = "BruteForce",
                Severity = ThreatSeverity.High,
                Description = $"Multiple failed login attempts: {activity.FailedLoginAttempts}"
            });
        }
        
        // Check for unusual data access patterns
        if (activity.DataAccessRate > 100) // per hour
        {
            threatIndicators.Add(new ThreatIndicator
            {
                Type = "DataExfiltration",
                Severity = ThreatSeverity.Critical,
                Description = $"Excessive data access: {activity.DataAccessRate} requests/hour"
            });
        }
        
        // Check for privilege escalation attempts
        if (activity.UnauthorizedAccessAttempts > 0)
        {
            threatIndicators.Add(new ThreatIndicator
            {
                Type = "PrivilegeEscalation",
                Severity = ThreatSeverity.High,
                Description = $"Unauthorized access attempts: {activity.UnauthorizedAccessAttempts}"
            });
        }
        
        // Calculate overall threat level
        var threatLevel = CalculateThreatLevel(threatIndicators);
        
        if (threatLevel >= ThreatLevel.High)
        {
            await _notificationService.SendSecurityAlertAsync(
                $"High threat detected for user {activity.UserId}: {string.Join(", ", threatIndicators.Select(t => t.Type))}");
        }
        
        return threatLevel;
    }
}
```

### Anomaly Detection

```csharp
public class AnomalyDetectionService
{
    private readonly IMLModel _mlModel;
    private readonly ILogger<AnomalyDetectionService> _logger;
    
    public async Task<AnomalyScore> DetectAnomaliesAsync(UserBehavior behavior)
    {
        // Prepare features for ML model
        var features = new[]
        {
            behavior.LoginFrequency,
            behavior.AverageSessionDuration,
            behavior.DataAccessPatterns,
            behavior.GeographicLocations.Count,
            behavior.DeviceFingerprints.Count,
            behavior.ApiCallPatterns
        };
        
        // Run through ML model
        var anomalyScore = await _mlModel.PredictAnomalyScoreAsync(features);
        
        if (anomalyScore.Value > 0.8) // High anomaly
        {
            _logger.LogWarning("High anomaly score detected for user {UserId}: {Score}", 
                behavior.UserId, anomalyScore.Value);
            
            // Trigger additional verification
            await TriggerAdditionalVerificationAsync(behavior.UserId);
        }
        
        return anomalyScore;
    }
}
```

## Incident Response Plan

### Incident Classification

| Severity | Response Time | Examples |
|----------|--------------|----------|
| Critical | < 15 minutes | Data breach, system compromise, ransomware |
| High | < 1 hour | Multiple account compromises, DDoS attack |
| Medium | < 4 hours | Single account compromise, suspicious activity |
| Low | < 24 hours | Policy violations, minor security issues |

### Incident Response Workflow

```csharp
public class IncidentResponseCoordinator
{
    private readonly IIncidentRepository _repository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<IncidentResponseCoordinator> _logger;
    
    public async Task<Incident> CreateIncidentAsync(IncidentReport report)
    {
        // Create incident record
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            Title = report.Title,
            Description = report.Description,
            Severity = DetermineSeverity(report),
            Status = IncidentStatus.New,
            CreatedAt = DateTime.UtcNow,
            Timeline = new List<IncidentTimelineEntry>()
        };
        
        // Add initial timeline entry
        incident.AddTimelineEntry("Incident created", report.ReportedBy);
        
        // Save incident
        await _repository.CreateAsync(incident);
        
        // Notify response team
        await NotifyResponseTeamAsync(incident);
        
        // Start automated response if applicable
        await StartAutomatedResponseAsync(incident);
        
        return incident;
    }
    
    private async Task StartAutomatedResponseAsync(Incident incident)
    {
        switch (incident.Type)
        {
            case IncidentType.BruteForceAttack:
                await BlockAttackingIpAddressesAsync(incident);
                break;
                
            case IncidentType.AccountCompromise:
                await LockCompromisedAccountsAsync(incident);
                await ForcePasswordResetAsync(incident);
                break;
                
            case IncidentType.DataBreach:
                await IsolateAffectedSystemsAsync(incident);
                await PreserveForesicEvidenceAsync(incident);
                break;
        }
    }
}
```

### Incident Response Checklist

```markdown
## CRITICAL INCIDENT RESPONSE CHECKLIST

### Immediate Actions (0-15 minutes)
- [ ] Identify and classify the incident
- [ ] Activate incident response team
- [ ] Begin incident documentation
- [ ] Isolate affected systems if necessary
- [ ] Preserve evidence

### Containment (15-60 minutes)
- [ ] Stop the spread of the incident
- [ ] Identify affected users/systems
- [ ] Implement temporary fixes
- [ ] Document all actions taken
- [ ] Communicate with stakeholders

### Investigation (1-4 hours)
- [ ] Determine root cause
- [ ] Assess full impact
- [ ] Collect forensic evidence
- [ ] Review logs and audit trails
- [ ] Interview involved parties

### Remediation (4-24 hours)
- [ ] Remove threat actors
- [ ] Patch vulnerabilities
- [ ] Reset compromised credentials
- [ ] Restore from clean backups if needed
- [ ] Implement additional controls

### Recovery (24-72 hours)
- [ ] Restore normal operations
- [ ] Monitor for reoccurrence
- [ ] Verify all systems secure
- [ ] Update security measures
- [ ] Document lessons learned
```

## Post-Incident Procedures

### Post-Incident Review Template

```csharp
public class PostIncidentReview
{
    public Guid IncidentId { get; set; }
    public DateTime ReviewDate { get; set; }
    public List<string> Participants { get; set; }
    
    // What happened?
    public string IncidentSummary { get; set; }
    public DateTime IncidentStartTime { get; set; }
    public DateTime IncidentEndTime { get; set; }
    public TimeSpan TotalDowntime { get; set; }
    
    // Impact assessment
    public int AffectedUsers { get; set; }
    public decimal EstimatedFinancialImpact { get; set; }
    public string ReputationalImpact { get; set; }
    
    // Response evaluation
    public TimeSpan TimeToDetect { get; set; }
    public TimeSpan TimeToRespond { get; set; }
    public TimeSpan TimeToResolve { get; set; }
    
    // Root cause analysis
    public string RootCause { get; set; }
    public List<string> ContributingFactors { get; set; }
    
    // Lessons learned
    public List<string> WhatWentWell { get; set; }
    public List<string> WhatCouldBeImproved { get; set; }
    
    // Action items
    public List<ActionItem> ActionItems { get; set; }
}

public class ActionItem
{
    public string Description { get; set; }
    public string AssignedTo { get; set; }
    public DateTime DueDate { get; set; }
    public Priority Priority { get; set; }
    public ActionItemStatus Status { get; set; }
}
```

### Incident Communication Plan

```csharp
public class IncidentCommunicationService
{
    public async Task SendIncidentNotificationAsync(Incident incident, 
        CommunicationAudience audience)
    {
        var template = GetTemplateForAudience(audience);
        var message = FormatMessage(incident, template);
        
        switch (audience)
        {
            case CommunicationAudience.InternalTeam:
                await SendToInternalTeamAsync(message);
                break;
                
            case CommunicationAudience.ExecutiveTeam:
                await SendToExecutivesAsync(SummarizeForExecutives(message));
                break;
                
            case CommunicationAudience.AffectedUsers:
                await SendToAffectedUsersAsync(SanitizeForUsers(message));
                break;
                
            case CommunicationAudience.PublicDisclosure:
                await PublishPublicStatementAsync(PreparePublicStatement(message));
                break;
        }
    }
}
```

## Security Metrics

### Key Performance Indicators (KPIs)

```csharp
public class SecurityMetricsService
{
    public async Task<SecurityDashboard> GenerateDashboardAsync(DateTime startDate, 
        DateTime endDate)
    {
        return new SecurityDashboard
        {
            // Incident metrics
            TotalIncidents = await GetIncidentCountAsync(startDate, endDate),
            MeanTimeToDetect = await CalculateMTTDAsync(startDate, endDate),
            MeanTimeToRespond = await CalculateMTTRAsync(startDate, endDate),
            
            // Authentication metrics
            FailedLoginAttempts = await GetFailedLoginCountAsync(startDate, endDate),
            AccountLockouts = await GetAccountLockoutCountAsync(startDate, endDate),
            PasswordResets = await GetPasswordResetCountAsync(startDate, endDate),
            
            // Threat metrics
            BlockedIpAddresses = await GetBlockedIpCountAsync(startDate, endDate),
            SuspiciousActivities = await GetSuspiciousActivityCountAsync(startDate, endDate),
            
            // Compliance metrics
            SecurityPatchCompliance = await CalculatePatchComplianceAsync(),
            UserTrainingCompliance = await CalculateTrainingComplianceAsync(),
            
            // Vulnerability metrics
            OpenVulnerabilities = await GetOpenVulnerabilityCountAsync(),
            VulnerabilityRemediationTime = await CalculateRemediationTimeAsync()
        };
    }
}
```

### Monthly Security Report Template

```markdown
# Monthly Security Report - [Month Year]

## Executive Summary
- Total incidents: X (↑/↓ Y% from last month)
- Critical incidents: X
- Average response time: X minutes
- System availability: X%

## Incident Analysis
### By Type
- Authentication attacks: X
- Data access violations: X
- System intrusions: X
- Policy violations: X

### By Severity
- Critical: X
- High: X
- Medium: X
- Low: X

## Threat Landscape
- Top attack vectors
- Emerging threats
- Blocked attacks

## Compliance Status
- Security training completion: X%
- Policy compliance: X%
- Audit findings: X

## Recommendations
1. [High priority recommendation]
2. [Medium priority recommendation]
3. [Low priority recommendation]

## Next Month's Focus
- [Priority 1]
- [Priority 2]
- [Priority 3]
```

---

*This monitoring and incident response guide should be reviewed and updated quarterly to ensure it remains effective against evolving threats.*