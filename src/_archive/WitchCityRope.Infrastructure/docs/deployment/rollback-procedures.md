# How to Rollback Deployments Safely

## Overview

This guide provides comprehensive procedures for safely rolling back deployments when issues arise in production. Quick and safe rollback capabilities are essential for maintaining service availability.

## Rollback Strategy

### Types of Rollbacks

1. **Application Code Rollback** - Revert to previous application version
2. **Database Schema Rollback** - Undo database migrations
3. **Configuration Rollback** - Restore previous settings
4. **Full System Rollback** - Complete restoration to previous state
5. **Partial/Feature Rollback** - Disable specific features

### Decision Matrix

| Issue Type | Rollback Type | Timeframe | Risk Level |
|------------|---------------|-----------|------------|
| Application crash | Code rollback | < 5 min | Low |
| Data corruption | Full rollback + restore | < 30 min | High |
| Performance degradation | Code or config rollback | < 10 min | Medium |
| Feature bug | Feature flag disable | < 2 min | Low |
| Security vulnerability | Immediate code rollback | < 5 min | Critical |

## Pre-Deployment Preparation

### Version Tagging

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/tag-release.sh

VERSION=$1
COMMIT_HASH=$(git rev-parse HEAD)

# Create git tag
git tag -a "v$VERSION" -m "Release version $VERSION"
git push origin "v$VERSION"

# Tag Docker images
docker tag witchcityrope:latest witchcityrope:v$VERSION
docker tag witchcityrope:latest witchcityrope:previous
docker push witchcityrope:v$VERSION

# Backup current deployment info
cat > /opt/witchcityrope/deployments/v$VERSION.json << EOF
{
  "version": "$VERSION",
  "commit": "$COMMIT_HASH",
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "database_version": "$(docker exec witchcityrope-db psql -U witchcity -d WitchCityRope -t -c 'SELECT version FROM schema_versions ORDER BY applied_at DESC LIMIT 1')"
}
EOF
```

### Deployment Snapshot

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/create-deployment-snapshot.sh

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
SNAPSHOT_DIR="/backups/deployments/$TIMESTAMP"

mkdir -p $SNAPSHOT_DIR

# Save current state
docker-compose -f docker-compose.prod.yml config > $SNAPSHOT_DIR/docker-compose.yml
cp .env.production $SNAPSHOT_DIR/
cp -r nginx/conf.d $SNAPSHOT_DIR/nginx-conf
docker inspect witchcityrope-web > $SNAPSHOT_DIR/container-state.json

# Database snapshot
docker exec witchcityrope-db pg_dump -U witchcity WitchCityRope | gzip > $SNAPSHOT_DIR/database.sql.gz

echo "Deployment snapshot created: $SNAPSHOT_DIR"
```

## Quick Rollback Procedures

### 1. Application Code Rollback (Blue-Green)

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/rollback-application.sh

set -e

echo "Starting application rollback..."

# Get previous version
PREVIOUS_VERSION=$(docker images witchcityrope --format "{{.Tag}}" | grep -E "^v[0-9]" | sort -V | tail -2 | head -1)

if [ -z "$PREVIOUS_VERSION" ]; then
    echo "ERROR: No previous version found"
    exit 1
fi

echo "Rolling back to version: $PREVIOUS_VERSION"

# Update docker-compose to use previous version
sed -i "s|image: witchcityrope:.*|image: witchcityrope:$PREVIOUS_VERSION|g" docker-compose.prod.yml

# Deploy previous version
docker-compose -f docker-compose.prod.yml up -d --no-deps web

# Wait for health check
sleep 30
if curl -f http://localhost:5000/health; then
    echo "Rollback successful"
    
    # Update load balancer
    docker exec nginx nginx -s reload
    
    # Notify team
    curl -X POST https://hooks.slack.com/services/YOUR/WEBHOOK/URL \
         -H 'Content-type: application/json' \
         -d "{\"text\":\"✅ Successfully rolled back to version $PREVIOUS_VERSION\"}"
else
    echo "ERROR: Health check failed after rollback"
    exit 1
fi
```

### 2. Zero-Downtime Rollback

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/zero-downtime-rollback.sh

set -e

PREVIOUS_VERSION=$1

# Start previous version on different port
docker run -d \
    --name witchcityrope-rollback \
    --network witchcity-network \
    -p 5001:5000 \
    -v /opt/witchcityrope/.env.production:/app/.env:ro \
    witchcityrope:$PREVIOUS_VERSION

# Wait for new instance to be healthy
echo "Waiting for rollback instance to be healthy..."
for i in {1..30}; do
    if curl -f http://localhost:5001/health; then
        echo "Rollback instance is healthy"
        break
    fi
    sleep 2
done

# Update nginx to point to rollback instance
cat > /tmp/nginx-rollback.conf << EOF
upstream witchcity_backend {
    server localhost:5001;
}
EOF

docker cp /tmp/nginx-rollback.conf nginx:/etc/nginx/conf.d/upstream.conf
docker exec nginx nginx -s reload

# Verify traffic is flowing
sleep 5
if curl -f https://yourdomain.com/health; then
    echo "Traffic successfully redirected"
    
    # Stop current version
    docker-compose -f docker-compose.prod.yml stop web
    
    # Replace with rollback version
    docker-compose -f docker-compose.prod.yml rm -f web
    docker rename witchcityrope-rollback witchcityrope-web
    
    # Update compose file
    sed -i "s|image: witchcityrope:.*|image: witchcityrope:$PREVIOUS_VERSION|g" docker-compose.prod.yml
else
    echo "ERROR: Traffic redirection failed"
    exit 1
fi
```

### 3. Database Migration Rollback

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/rollback-database.sh

set -e

MIGRATION_TO_REVERT=$1

echo "WARNING: Database rollback can cause data loss!"
read -p "Are you sure? (yes/no): " CONFIRM

if [ "$CONFIRM" != "yes" ]; then
    echo "Rollback cancelled"
    exit 0
fi

# Stop application
docker-compose -f docker-compose.prod.yml stop web

# Create backup before rollback
BACKUP_FILE="/backups/pre-rollback-$(date +%Y%m%d_%H%M%S).sql"
docker exec witchcityrope-db pg_dump -U witchcity WitchCityRope > $BACKUP_FILE

# Revert migration
docker-compose -f docker-compose.prod.yml run --rm web \
    dotnet ef database update $MIGRATION_TO_REVERT

# Verify database state
docker exec witchcityrope-db psql -U witchcity -d WitchCityRope -c "\dt"

# Restart application with compatible version
docker-compose -f docker-compose.prod.yml up -d web

echo "Database rollback completed"
```

### 4. Configuration Rollback

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/rollback-config.sh

set -e

BACKUP_TIMESTAMP=$1

if [ -z "$BACKUP_TIMESTAMP" ]; then
    # Find latest backup
    BACKUP_TIMESTAMP=$(ls -t /backups/deployments/ | head -1)
fi

BACKUP_DIR="/backups/deployments/$BACKUP_TIMESTAMP"

if [ ! -d "$BACKUP_DIR" ]; then
    echo "ERROR: Backup not found: $BACKUP_DIR"
    exit 1
fi

echo "Rolling back configuration from: $BACKUP_DIR"

# Backup current config
cp .env.production .env.production.rollback

# Restore configuration
cp $BACKUP_DIR/.env.production .
cp -r $BACKUP_DIR/nginx-conf/* nginx/conf.d/

# Restart services with new config
docker-compose -f docker-compose.prod.yml restart

echo "Configuration rollback completed"
```

## Feature Flag Rollback

### Quick Feature Disable

```csharp
// Feature flag service
public class FeatureFlags
{
    public static async Task DisableFeature(string featureName)
    {
        var redis = ConnectionMultiplexer.Connect("localhost:6379");
        var db = redis.GetDatabase();
        
        await db.StringSetAsync($"feature:{featureName}", "false");
        
        // Broadcast to all instances
        var pub = redis.GetSubscriber();
        await pub.PublishAsync("feature-updates", $"{featureName}:disabled");
    }
}
```

### Emergency Feature Kill Switch

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/kill-feature.sh

FEATURE=$1

# Disable in Redis
docker exec witchcityrope-redis redis-cli SET "feature:$FEATURE" "false"

# Update environment variable
sed -i "s/FEATURE_${FEATURE^^}_ENABLED=true/FEATURE_${FEATURE^^}_ENABLED=false/g" .env.production

# Soft restart (reload config without downtime)
docker-compose -f docker-compose.prod.yml kill -s HUP web

echo "Feature $FEATURE disabled"
```

## Automated Rollback

### Health Check Based Rollback

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/auto-rollback.sh

set -e

# Configuration
MAX_FAILURES=3
CHECK_INTERVAL=10
HEALTH_ENDPOINT="http://localhost:5000/health"

failures=0

echo "Monitoring deployment health..."

for i in {1..30}; do
    if curl -f $HEALTH_ENDPOINT > /dev/null 2>&1; then
        echo "Health check passed"
        failures=0
    else
        ((failures++))
        echo "Health check failed ($failures/$MAX_FAILURES)"
        
        if [ $failures -ge $MAX_FAILURES ]; then
            echo "CRITICAL: Maximum failures reached, initiating rollback"
            /opt/witchcityrope/scripts/rollback-application.sh
            exit 1
        fi
    fi
    
    sleep $CHECK_INTERVAL
done

echo "Deployment monitoring completed successfully"
```

### Canary Rollback

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/canary-rollback.sh

# Monitor canary metrics
ERROR_RATE=$(curl -s http://localhost:9090/api/v1/query?query=rate\(http_requests_total{status=~\"5..\",instance=\"canary\"}\[5m\] | jq -r '.data.result[0].value[1]')

if (( $(echo "$ERROR_RATE > 0.05" | bc -l) )); then
    echo "High error rate detected in canary: $ERROR_RATE"
    
    # Remove canary from load balancer
    docker exec nginx sed -i '/server canary:5000/d' /etc/nginx/conf.d/upstream.conf
    docker exec nginx nginx -s reload
    
    # Stop canary deployment
    docker stop witchcityrope-canary
    
    echo "Canary rollback completed"
fi
```

## Rollback Verification

### Post-Rollback Checks

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/verify-rollback.sh

echo "Verifying rollback..."

# Check version
CURRENT_VERSION=$(docker inspect witchcityrope-web | jq -r '.[0].Config.Labels.version')
echo "Current version: $CURRENT_VERSION"

# Health checks
curl -f http://localhost:5000/health || exit 1
curl -f http://localhost:5000/health/ready || exit 1

# Database connectivity
docker exec witchcityrope-web dotnet WitchCityRope.dll test-db || exit 1

# Critical features
CRITICAL_ENDPOINTS=(
    "/api/auth/login"
    "/api/events"
    "/api/users/profile"
)

for endpoint in "${CRITICAL_ENDPOINTS[@]}"; do
    if curl -f "http://localhost:5000$endpoint" > /dev/null 2>&1; then
        echo "✓ $endpoint is accessible"
    else
        echo "✗ $endpoint is not accessible"
        exit 1
    fi
done

echo "Rollback verification completed successfully"
```

### Rollback Metrics

```csharp
// Track rollback metrics
public class RollbackMetrics
{
    private static readonly Counter RollbackCounter = Metrics
        .CreateCounter("deployment_rollbacks_total", "Total number of rollbacks",
            new CounterConfiguration
            {
                LabelNames = new[] { "reason", "from_version", "to_version" }
            });

    public static void RecordRollback(string reason, string fromVersion, string toVersion)
    {
        RollbackCounter.WithLabels(reason, fromVersion, toVersion).Inc();
        
        // Log for audit
        Log.Warning("Deployment rollback executed", new
        {
            Reason = reason,
            FromVersion = fromVersion,
            ToVersion = toVersion,
            Timestamp = DateTime.UtcNow
        });
    }
}
```

## Rollback Communication

### Stakeholder Notification

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/notify-rollback.sh

REASON=$1
FROM_VERSION=$2
TO_VERSION=$3

# Slack notification
curl -X POST https://hooks.slack.com/services/YOUR/WEBHOOK/URL \
     -H 'Content-type: application/json' \
     -d @- << EOF
{
  "text": "🔄 Deployment Rollback Executed",
  "attachments": [
    {
      "color": "warning",
      "fields": [
        {"title": "Reason", "value": "$REASON", "short": false},
        {"title": "From Version", "value": "$FROM_VERSION", "short": true},
        {"title": "To Version", "value": "$TO_VERSION", "short": true},
        {"title": "Time", "value": "$(date)", "short": true},
        {"title": "Initiated By", "value": "$USER", "short": true}
      ]
    }
  ]
}
EOF

# Email notification
mail -s "WitchCityRope Rollback: $REASON" team@witchcityrope.com << EOF
A deployment rollback has been executed.

Reason: $REASON
From Version: $FROM_VERSION
To Version: $TO_VERSION
Time: $(date)
Initiated By: $USER

Please review the deployment logs and address the issues before the next deployment.
EOF

# Update status page
curl -X POST https://api.statuspage.io/v1/pages/YOUR_PAGE_ID/incidents \
     -H "Authorization: OAuth YOUR_API_KEY" \
     -H "Content-Type: application/json" \
     -d "{
       \"incident\": {
         \"name\": \"Deployment rollback due to $REASON\",
         \"status\": \"investigating\",
         \"impact_override\": \"minor\"
       }
     }"
```

## Rollback Recovery

### Post-Rollback Actions

1. **Root Cause Analysis**
```bash
# Collect logs from failed deployment
docker logs witchcityrope-web > /tmp/failed-deployment.log
docker exec witchcityrope-db pg_dump -U witchcity WitchCityRope > /tmp/failed-deployment-db.sql

# Create incident report
cat > /opt/witchcityrope/incidents/$(date +%Y%m%d_%H%M%S).md << EOF
# Deployment Rollback Incident

**Date**: $(date)
**Failed Version**: $FAILED_VERSION
**Rollback To**: $ROLLBACK_VERSION
**Duration**: $DOWNTIME minutes

## Summary
$REASON

## Impact
- Affected users: $AFFECTED_USERS
- Lost transactions: $LOST_TRANSACTIONS

## Root Cause
[To be determined]

## Action Items
- [ ] Fix identified issues
- [ ] Add tests to prevent recurrence
- [ ] Update deployment procedures
EOF
```

2. **Fix Forward Plan**
```bash
# Create fix branch
git checkout -b hotfix/rollback-issues
# Apply fixes
# Test thoroughly
# Deploy with extra monitoring
```

## Best Practices

### 1. Rollback Readiness
- Always tag releases
- Maintain version compatibility
- Test rollback procedures regularly
- Keep rollback scripts updated

### 2. Database Safety
- Use backward-compatible migrations
- Avoid destructive changes
- Always backup before schema changes
- Test migrations in staging

### 3. Monitoring
- Watch metrics after rollback
- Verify all features work
- Monitor user complaints
- Check for data inconsistencies

### 4. Documentation
- Document all rollbacks
- Track rollback reasons
- Update runbooks
- Share lessons learned