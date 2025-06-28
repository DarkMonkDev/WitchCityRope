# Setting Up Monitoring and Alerting

## Overview

This guide covers comprehensive monitoring setup for WitchCityRope, including application metrics, infrastructure monitoring, log aggregation, and alerting.

## Monitoring Stack Architecture

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Application   │────▶│   Prometheus    │────▶│     Grafana     │
│    Metrics      │     │  (Port 9090)    │     │   (Port 3000)   │
└─────────────────┘     └─────────────────┘     └─────────────────┘
                               │                          │
┌─────────────────┐            │                          ▼
│   Node Exporter │────────────┘                 ┌─────────────────┐
│   (Port 9100)   │                               │   Alert Manager │
└─────────────────┘                               │   (Port 9093)   │
                                                  └─────────────────┘
┌─────────────────┐     ┌─────────────────┐
│      Logs       │────▶│  Elasticsearch  │────▶┌─────────────────┐
│   (Filebeat)    │     │  (Port 9200)    │     │     Kibana      │
└─────────────────┘     └─────────────────┘     │   (Port 5601)   │
                                                 └─────────────────┘
```

## Docker Compose Monitoring Stack

```yaml
# docker-compose.monitoring.yml
version: '3.8'

services:
  prometheus:
    image: prom/prometheus:latest
    container_name: prometheus
    restart: always
    ports:
      - "9090:9090"
    volumes:
      - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - ./monitoring/alerts.yml:/etc/prometheus/alerts.yml:ro
      - prometheus-data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--storage.tsdb.retention.time=30d'
      - '--web.enable-lifecycle'
    networks:
      - monitoring

  grafana:
    image: grafana/grafana:latest
    container_name: grafana
    restart: always
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_USER=admin
      - GF_SECURITY_ADMIN_PASSWORD=${GRAFANA_PASSWORD}
      - GF_INSTALL_PLUGINS=grafana-clock-panel,grafana-simple-json-datasource
    volumes:
      - grafana-data:/var/lib/grafana
      - ./monitoring/grafana/provisioning:/etc/grafana/provisioning
      - ./monitoring/grafana/dashboards:/var/lib/grafana/dashboards
    depends_on:
      - prometheus
    networks:
      - monitoring

  alertmanager:
    image: prom/alertmanager:latest
    container_name: alertmanager
    restart: always
    ports:
      - "9093:9093"
    volumes:
      - ./monitoring/alertmanager.yml:/etc/alertmanager/alertmanager.yml:ro
      - alertmanager-data:/alertmanager
    command:
      - '--config.file=/etc/alertmanager/alertmanager.yml'
      - '--storage.path=/alertmanager'
    networks:
      - monitoring

  node-exporter:
    image: prom/node-exporter:latest
    container_name: node-exporter
    restart: always
    ports:
      - "9100:9100"
    volumes:
      - /proc:/host/proc:ro
      - /sys:/host/sys:ro
      - /:/rootfs:ro
    command:
      - '--path.procfs=/host/proc'
      - '--path.sysfs=/host/sys'
      - '--path.rootfs=/rootfs'
      - '--collector.filesystem.mount-points-exclude=^/(sys|proc|dev|host|etc)($$|/)'
    networks:
      - monitoring

  cadvisor:
    image: gcr.io/cadvisor/cadvisor:latest
    container_name: cadvisor
    restart: always
    ports:
      - "8080:8080"
    volumes:
      - /:/rootfs:ro
      - /var/run:/var/run:ro
      - /sys:/sys:ro
      - /var/lib/docker:/var/lib/docker:ro
      - /dev/disk:/dev/disk:ro
    devices:
      - /dev/kmsg
    networks:
      - monitoring

  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.11.0
    container_name: elasticsearch
    restart: always
    environment:
      - discovery.type=single-node
      - "ES_JAVA_OPTS=-Xms512m -Xmx512m"
      - xpack.security.enabled=false
    ports:
      - "9200:9200"
    volumes:
      - elasticsearch-data:/usr/share/elasticsearch/data
    networks:
      - monitoring

  kibana:
    image: docker.elastic.co/kibana/kibana:8.11.0
    container_name: kibana
    restart: always
    ports:
      - "5601:5601"
    environment:
      - ELASTICSEARCH_HOSTS=http://elasticsearch:9200
    depends_on:
      - elasticsearch
    networks:
      - monitoring

  filebeat:
    image: docker.elastic.co/beats/filebeat:8.11.0
    container_name: filebeat
    restart: always
    user: root
    volumes:
      - ./monitoring/filebeat.yml:/usr/share/filebeat/filebeat.yml:ro
      - /var/lib/docker/containers:/var/lib/docker/containers:ro
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - logs:/logs:ro
    command: filebeat -e -strict.perms=false
    depends_on:
      - elasticsearch
    networks:
      - monitoring

volumes:
  prometheus-data:
  grafana-data:
  alertmanager-data:
  elasticsearch-data:

networks:
  monitoring:
    external: true
```

## Application Metrics

### ASP.NET Core Metrics Configuration

```csharp
// Program.cs
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add metrics
builder.Services.AddSingleton<IMetrics, PrometheusMetrics>();
builder.Services.AddHostedService<MetricsReporter>();

var app = builder.Build();

// Use Prometheus middleware
app.UseHttpMetrics();
app.MapMetrics(); // Exposes /metrics endpoint

// Custom metrics
public class PrometheusMetrics : IMetrics
{
    private readonly Counter _httpRequestsTotal;
    private readonly Histogram _httpRequestDuration;
    private readonly Gauge _activeUsers;
    private readonly Counter _userRegistrations;
    private readonly Counter _eventCreations;
    private readonly Counter _paymentTransactions;
    private readonly Summary _databaseQueryDuration;

    public PrometheusMetrics()
    {
        _httpRequestsTotal = Metrics.CreateCounter(
            "http_requests_total",
            "Total HTTP requests",
            new CounterConfiguration
            {
                LabelNames = new[] { "method", "endpoint", "status" }
            });

        _httpRequestDuration = Metrics.CreateHistogram(
            "http_request_duration_seconds",
            "HTTP request duration in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "method", "endpoint" },
                Buckets = Histogram.LinearBuckets(0.001, 0.001, 100)
            });

        _activeUsers = Metrics.CreateGauge(
            "active_users",
            "Number of active users");

        _userRegistrations = Metrics.CreateCounter(
            "user_registrations_total",
            "Total user registrations");

        _eventCreations = Metrics.CreateCounter(
            "event_creations_total",
            "Total events created");

        _paymentTransactions = Metrics.CreateCounter(
            "payment_transactions_total",
            "Total payment transactions",
            new CounterConfiguration
            {
                LabelNames = new[] { "status", "type" }
            });

        _databaseQueryDuration = Metrics.CreateSummary(
            "database_query_duration_seconds",
            "Database query duration in seconds",
            new SummaryConfiguration
            {
                LabelNames = new[] { "operation" }
            });
    }

    public void RecordHttpRequest(string method, string endpoint, int status, double duration)
    {
        _httpRequestsTotal.WithLabels(method, endpoint, status.ToString()).Inc();
        _httpRequestDuration.WithLabels(method, endpoint).Observe(duration);
    }

    public void SetActiveUsers(int count) => _activeUsers.Set(count);
    public void IncrementUserRegistrations() => _userRegistrations.Inc();
    public void IncrementEventCreations() => _eventCreations.Inc();
    
    public void RecordPaymentTransaction(string status, string type) => 
        _paymentTransactions.WithLabels(status, type).Inc();
    
    public void RecordDatabaseQuery(string operation, double duration) => 
        _databaseQueryDuration.WithLabels(operation).Observe(duration);
}
```

### Health Checks

```csharp
// HealthChecks.cs
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database")
    .AddRedis("redis")
    .AddUrlGroup(new Uri("https://api.stripe.com"), "stripe")
    .AddCheck<StorageHealthCheck>("storage")
    .AddCheck<EmailHealthCheck>("email");

// Custom health check
public class StorageHealthCheck : IHealthCheck
{
    private readonly IStorageService _storageService;

    public StorageHealthCheck(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _storageService.CheckConnectivityAsync();
            return HealthCheckResult.Healthy("Storage is accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Storage is not accessible", ex);
        }
    }
}

// Map health endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
```

## Prometheus Configuration

```yaml
# monitoring/prometheus.yml
global:
  scrape_interval: 15s
  evaluation_interval: 15s
  external_labels:
    environment: 'production'
    service: 'witchcityrope'

alerting:
  alertmanagers:
    - static_configs:
        - targets:
            - alertmanager:9093

rule_files:
  - "alerts.yml"

scrape_configs:
  - job_name: 'witchcityrope-app'
    static_configs:
      - targets: ['web:5000']
    metrics_path: '/metrics'
    scrape_interval: 5s

  - job_name: 'node'
    static_configs:
      - targets: ['node-exporter:9100']

  - job_name: 'cadvisor'
    static_configs:
      - targets: ['cadvisor:8080']

  - job_name: 'postgres'
    static_configs:
      - targets: ['postgres-exporter:9187']

  - job_name: 'redis'
    static_configs:
      - targets: ['redis-exporter:9121']

  - job_name: 'nginx'
    static_configs:
      - targets: ['nginx-exporter:9113']
```

## Alert Rules

```yaml
# monitoring/alerts.yml
groups:
  - name: application
    interval: 30s
    rules:
      - alert: HighErrorRate
        expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.05
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: High error rate detected
          description: "Error rate is {{ $value }} errors per second"

      - alert: SlowResponses
        expr: histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 1
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: Slow response times
          description: "95th percentile response time is {{ $value }} seconds"

      - alert: LowActiveUsers
        expr: active_users < 10
        for: 30m
        labels:
          severity: info
        annotations:
          summary: Low number of active users
          description: "Only {{ $value }} active users"

  - name: infrastructure
    interval: 30s
    rules:
      - alert: HighCPUUsage
        expr: 100 - (avg(rate(node_cpu_seconds_total{mode="idle"}[5m])) * 100) > 80
        for: 10m
        labels:
          severity: warning
        annotations:
          summary: High CPU usage
          description: "CPU usage is {{ $value }}%"

      - alert: HighMemoryUsage
        expr: (1 - (node_memory_MemAvailable_bytes / node_memory_MemTotal_bytes)) * 100 > 85
        for: 10m
        labels:
          severity: warning
        annotations:
          summary: High memory usage
          description: "Memory usage is {{ $value }}%"

      - alert: DiskSpaceLow
        expr: (node_filesystem_avail_bytes{mountpoint="/"} / node_filesystem_size_bytes{mountpoint="/"}) * 100 < 15
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: Low disk space
          description: "Only {{ $value }}% disk space remaining"

  - name: database
    interval: 30s
    rules:
      - alert: DatabaseDown
        expr: up{job="postgres"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: Database is down
          description: "PostgreSQL database is not responding"

      - alert: DatabaseConnectionsHigh
        expr: pg_stat_database_numbackends{datname="WitchCityRope"} > 80
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: High database connections
          description: "{{ $value }} active database connections"

      - alert: DatabaseSlowQueries
        expr: rate(database_query_duration_seconds_sum[5m]) / rate(database_query_duration_seconds_count[5m]) > 1
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: Slow database queries
          description: "Average query time is {{ $value }} seconds"
```

## Alert Manager Configuration

```yaml
# monitoring/alertmanager.yml
global:
  resolve_timeout: 5m
  slack_api_url: 'YOUR_SLACK_WEBHOOK_URL'

route:
  group_by: ['alertname', 'severity']
  group_wait: 10s
  group_interval: 10s
  repeat_interval: 1h
  receiver: 'default'
  routes:
    - match:
        severity: critical
      receiver: 'critical'
      continue: true
    - match:
        severity: warning
      receiver: 'warning'

receivers:
  - name: 'default'
    slack_configs:
      - channel: '#alerts'
        title: 'WitchCityRope Alert'
        text: '{{ range .Alerts }}{{ .Annotations.summary }}\n{{ .Annotations.description }}{{ end }}'

  - name: 'critical'
    slack_configs:
      - channel: '#alerts-critical'
        title: '🚨 CRITICAL: WitchCityRope Alert'
        text: '{{ range .Alerts }}{{ .Annotations.summary }}\n{{ .Annotations.description }}{{ end }}'
    pagerduty_configs:
      - service_key: 'YOUR_PAGERDUTY_SERVICE_KEY'
    email_configs:
      - to: 'oncall@witchcityrope.com'
        from: 'alerts@witchcityrope.com'
        smarthost: 'smtp.gmail.com:587'
        auth_username: 'alerts@witchcityrope.com'
        auth_password: 'YOUR_EMAIL_PASSWORD'

  - name: 'warning'
    slack_configs:
      - channel: '#alerts-warning'
        title: '⚠️ WARNING: WitchCityRope Alert'
        text: '{{ range .Alerts }}{{ .Annotations.summary }}\n{{ .Annotations.description }}{{ end }}'
```

## Grafana Dashboards

### Application Dashboard

```json
{
  "dashboard": {
    "title": "WitchCityRope Application Metrics",
    "panels": [
      {
        "title": "Request Rate",
        "targets": [
          {
            "expr": "sum(rate(http_requests_total[5m])) by (method)"
          }
        ]
      },
      {
        "title": "Error Rate",
        "targets": [
          {
            "expr": "sum(rate(http_requests_total{status=~\"5..\"}[5m]))"
          }
        ]
      },
      {
        "title": "Response Time (p95)",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, sum(rate(http_request_duration_seconds_bucket[5m])) by (le))"
          }
        ]
      },
      {
        "title": "Active Users",
        "targets": [
          {
            "expr": "active_users"
          }
        ]
      },
      {
        "title": "User Registrations",
        "targets": [
          {
            "expr": "rate(user_registrations_total[1h])"
          }
        ]
      },
      {
        "title": "Database Query Time",
        "targets": [
          {
            "expr": "rate(database_query_duration_seconds_sum[5m]) / rate(database_query_duration_seconds_count[5m])"
          }
        ]
      }
    ]
  }
}
```

## Log Aggregation

### Filebeat Configuration

```yaml
# monitoring/filebeat.yml
filebeat.inputs:
  - type: container
    paths:
      - '/var/lib/docker/containers/*/*.log'
    processors:
      - add_docker_metadata:
          host: "unix:///var/run/docker.sock"
      - decode_json_fields:
          fields: ["message"]
          target: "json"
          overwrite_keys: true

  - type: log
    paths:
      - '/logs/*.log'
    multiline.pattern: '^\d{4}-\d{2}-\d{2}'
    multiline.negate: true
    multiline.match: after

processors:
  - add_host_metadata:
      when.not.contains.tags: forwarded
  - add_docker_metadata: ~
  - add_fields:
      target: ''
      fields:
        environment: production
        service: witchcityrope

output.elasticsearch:
  hosts: ["elasticsearch:9200"]
  index: "witchcityrope-%{+yyyy.MM.dd}"
  template.enabled: true
  template.path: "filebeat.template.json"
  template.overwrite: false

logging.level: info
logging.to_files: true
logging.files:
  path: /var/log/filebeat
  name: filebeat
  keepfiles: 7
  permissions: 0644
```

### Application Logging

```csharp
// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("Environment", environment)
    .Enrich.WithProperty("Application", "WitchCityRope")
    .WriteTo.Console(new ElasticsearchJsonFormatter())
    .WriteTo.File(
        new ElasticsearchJsonFormatter(),
        "/logs/witchcityrope-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

// Structured logging
_logger.LogInformation("User registered", new
{
    UserId = user.Id,
    Email = user.Email,
    Timestamp = DateTime.UtcNow
});
```

## Monitoring Scripts

### Health Check Monitor

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/monitor-health.sh

HEALTH_ENDPOINT="http://localhost:5000/health"
SLACK_WEBHOOK="YOUR_SLACK_WEBHOOK"

# Check health
RESPONSE=$(curl -s -w "\n%{http_code}" $HEALTH_ENDPOINT)
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | head -n-1)

if [ "$HTTP_CODE" != "200" ]; then
    curl -X POST $SLACK_WEBHOOK \
         -H 'Content-type: application/json' \
         -d "{\"text\":\"🚨 Health check failed! Status: $HTTP_CODE\"}"
fi

# Check specific components
echo "$BODY" | jq -r '.results | to_entries[] | select(.value.status != "Healthy") | "\(.key): \(.value.status)"'
```

### Performance Monitor

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/monitor-performance.sh

# Query Prometheus
curl -s http://localhost:9090/api/v1/query?query=histogram_quantile\(0.95,rate\(http_request_duration_seconds_bucket[5m]\)\) | \
jq -r '.data.result[0].value[1]' | \
awk '{if ($1 > 1) print "Warning: p95 response time is " $1 " seconds"}'
```

## Custom Dashboards

### Database Performance Dashboard

```sql
-- Create monitoring views
CREATE VIEW database_metrics AS
SELECT
    'active_connections' as metric,
    count(*) as value
FROM pg_stat_activity
WHERE state = 'active'
UNION ALL
SELECT
    'total_connections' as metric,
    count(*) as value
FROM pg_stat_activity
UNION ALL
SELECT
    'cache_hit_ratio' as metric,
    ROUND(sum(heap_blks_hit) / (sum(heap_blks_hit) + sum(heap_blks_read))::numeric, 4) as value
FROM pg_statio_user_tables;
```

### Business Metrics Dashboard

```csharp
// Custom metrics endpoint
app.MapGet("/metrics/business", async (IBusinessMetricsService metrics) =>
{
    return new
    {
        DailyActiveUsers = await metrics.GetDailyActiveUsers(),
        MonthlyRevenue = await metrics.GetMonthlyRevenue(),
        EventsThisWeek = await metrics.GetWeeklyEventCount(),
        AverageEventAttendance = await metrics.GetAverageEventAttendance(),
        UserRetentionRate = await metrics.GetUserRetentionRate()
    };
});
```

## Alerting Best Practices

1. **Alert Fatigue Prevention**
   - Only alert on actionable issues
   - Use appropriate thresholds
   - Group related alerts

2. **Escalation Policy**
   - Info → Slack channel
   - Warning → Slack + Email
   - Critical → Slack + Email + PagerDuty

3. **Documentation**
   - Include runbooks in alerts
   - Clear resolution steps
   - Contact information

4. **Testing**
   - Regular alert testing
   - Disaster recovery drills
   - On-call rotation