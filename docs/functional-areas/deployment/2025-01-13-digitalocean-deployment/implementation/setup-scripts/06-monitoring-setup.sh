#!/bin/bash
# Monitoring and Alerting Setup Script
# Sets up basic monitoring, logging, and alerting for WitchCityRope
# Run this script as the witchcity user
# Usage: ./06-monitoring-setup.sh

set -euo pipefail

echo "📊 Setting up monitoring and alerting for WitchCityRope..."
echo "📅 Started at: $(date)"

# Check if running as correct user
if [ "$USER" = "root" ]; then
    echo "❌ This script should not be run as root"
    echo "   Please run as witchcity user: ./06-monitoring-setup.sh"
    exit 1
fi

# Configuration
EMAIL=""
SLACK_WEBHOOK=""

# Function to prompt for notification settings
prompt_for_notifications() {
    echo "🔔 Configure notification settings:"
    echo ""

    # Prompt for email
    read -p "Enter email for alerts (optional): " EMAIL

    # Prompt for Slack webhook
    read -p "Enter Slack webhook URL (optional): " SLACK_WEBHOOK

    if [ -n "$EMAIL" ]; then
        echo "✅ Email notifications will be sent to: $EMAIL"
    fi

    if [ -n "$SLACK_WEBHOOK" ]; then
        echo "✅ Slack notifications configured"
    fi

    if [ -z "$EMAIL" ] && [ -z "$SLACK_WEBHOOK" ]; then
        echo "⚠️  No notifications configured - alerts will only be logged"
    fi
}

# Get notification preferences
prompt_for_notifications

# Create monitoring directories
echo "📁 Creating monitoring directories..."
sudo mkdir -p /var/log/witchcityrope/monitoring
mkdir -p /opt/witchcityrope/monitoring
mkdir -p /opt/witchcityrope/alerts

# Set proper permissions
sudo chown -R "$USER:$USER" /var/log/witchcityrope/monitoring
chown -R "$USER:$USER" /opt/witchcityrope/monitoring
chown -R "$USER:$USER" /opt/witchcityrope/alerts

echo "✅ Monitoring directories created"

# Install monitoring tools
echo "📊 Installing monitoring tools..."

# Install netdata (lightweight monitoring)
if ! command -v netdata &> /dev/null; then
    echo "Installing Netdata..."
    wget -O /tmp/netdata-kickstart.sh https://my-netdata.io/kickstart.sh
    sudo sh /tmp/netdata-kickstart.sh --non-interactive --stable-channel
    rm /tmp/netdata-kickstart.sh
    echo "✅ Netdata installed"
else
    echo "✅ Netdata already installed"
fi

# Configure Netdata
sudo tee /etc/netdata/netdata.conf > /dev/null << 'EOF'
[global]
    # Performance
    update every = 1
    memory mode = dbengine
    page cache size = 32
    dbengine disk space = 256

    # Security - only local access
    bind socket to IP = 127.0.0.1
    default port = 19999

    # Logging
    debug log = syslog
    error log = syslog
    access log = none

[web]
    # Security
    allow connections from = localhost 127.0.0.1 ::1
    enable gzip compression = yes
    gzip compression level = 3

[plugins]
    # Enable important plugins
    apps = yes
    cgroups = yes
    proc = yes
    diskspace = yes

    # Disable unnecessary plugins
    charts.d = no
    node.d = no
    python.d = no
EOF

# Restart and enable Netdata
sudo systemctl restart netdata
sudo systemctl enable netdata

# Create system monitoring script
echo "🔍 Creating system monitoring script..."
cat > /opt/witchcityrope/monitoring/system-monitor.sh << 'EOF'
#!/bin/bash
# System Monitoring Script for WitchCityRope
# Monitors system resources and sends alerts when thresholds are exceeded

set -euo pipefail

# Configuration
LOG_FILE="/var/log/witchcityrope/monitoring/system-monitor.log"
ALERT_FILE="/opt/witchcityrope/alerts/system-alerts.log"
EMAIL=""
SLACK_WEBHOOK=""

# Thresholds
CPU_THRESHOLD=80
MEMORY_THRESHOLD=85
DISK_THRESHOLD=90
LOAD_THRESHOLD=4.0

# Functions
log_message() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

send_alert() {
    local severity=$1
    local message=$2
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')

    # Log alert
    echo "[$timestamp] [$severity] $message" >> "$ALERT_FILE"
    log_message "ALERT [$severity]: $message"

    # Send email if configured
    if [ -n "$EMAIL" ] && command -v mail &> /dev/null; then
        echo "$message" | mail -s "WitchCityRope Alert [$severity]" "$EMAIL"
    fi

    # Send Slack notification if configured
    if [ -n "$SLACK_WEBHOOK" ]; then
        curl -X POST -H 'Content-type: application/json' \
            --data "{\"text\":\"🚨 WitchCityRope Alert [$severity]\\n$message\"}" \
            "$SLACK_WEBHOOK" > /dev/null 2>&1 || true
    fi
}

# Check CPU usage
check_cpu() {
    local cpu_usage=$(top -bn1 | grep "Cpu(s)" | sed "s/.*, *\([0-9.]*\)%* id.*/\1/" | awk '{print 100 - $1}')
    local cpu_int=${cpu_usage%.*}

    if [ "$cpu_int" -gt "$CPU_THRESHOLD" ]; then
        send_alert "WARNING" "High CPU usage: ${cpu_usage}% (threshold: ${CPU_THRESHOLD}%)"
    fi

    log_message "CPU Usage: ${cpu_usage}%"
}

# Check memory usage
check_memory() {
    local memory_usage=$(free | grep Mem | awk '{printf("%.0f", $3/$2 * 100.0)}')

    if [ "$memory_usage" -gt "$MEMORY_THRESHOLD" ]; then
        send_alert "WARNING" "High memory usage: ${memory_usage}% (threshold: ${MEMORY_THRESHOLD}%)"
    fi

    log_message "Memory Usage: ${memory_usage}%"
}

# Check disk usage
check_disk() {
    local disk_usage=$(df / | tail -1 | awk '{print $5}' | sed 's/%//')

    if [ "$disk_usage" -gt "$DISK_THRESHOLD" ]; then
        send_alert "CRITICAL" "High disk usage: ${disk_usage}% (threshold: ${DISK_THRESHOLD}%)"
    fi

    log_message "Disk Usage: ${disk_usage}%"
}

# Check system load
check_load() {
    local load_avg=$(uptime | awk -F'load average:' '{print $2}' | awk -F, '{print $1}' | xargs)
    local load_comparison=$(echo "$load_avg > $LOAD_THRESHOLD" | bc -l)

    if [ "$load_comparison" -eq 1 ]; then
        send_alert "WARNING" "High system load: $load_avg (threshold: $LOAD_THRESHOLD)"
    fi

    log_message "System Load: $load_avg"
}

# Check Docker containers
check_containers() {
    local unhealthy_containers=$(docker ps --filter "health=unhealthy" --format "{{.Names}}" | wc -l)
    local stopped_containers=$(docker ps -a --filter "name=witchcity-" --filter "status=exited" --format "{{.Names}}" | wc -l)

    if [ "$unhealthy_containers" -gt 0 ]; then
        local container_names=$(docker ps --filter "health=unhealthy" --format "{{.Names}}" | tr '\n' ', ')
        send_alert "CRITICAL" "Unhealthy containers detected: ${container_names%,}"
    fi

    if [ "$stopped_containers" -gt 0 ]; then
        local container_names=$(docker ps -a --filter "name=witchcity-" --filter "status=exited" --format "{{.Names}}" | tr '\n' ', ')
        send_alert "WARNING" "Stopped WitchCityRope containers: ${container_names%,}"
    fi

    log_message "Container Status: $(docker ps --filter 'name=witchcity-' --format '{{.Names}}' | wc -l) running, $stopped_containers stopped"
}

# Check application health
check_application() {
    local prod_api_status="DOWN"
    local staging_api_status="DOWN"

    if curl -f -s http://localhost:5001/health > /dev/null 2>&1; then
        prod_api_status="UP"
    else
        send_alert "CRITICAL" "Production API health check failed"
    fi

    if curl -f -s http://localhost:5002/health > /dev/null 2>&1; then
        staging_api_status="UP"
    else
        send_alert "WARNING" "Staging API health check failed"
    fi

    log_message "Application Status: Production API: $prod_api_status, Staging API: $staging_api_status"
}

# Check SSL certificates
check_ssl_certificates() {
    local cert_warnings=0

    # Check production SSL
    if [ -f "/etc/letsencrypt/live/$(hostname -f)/fullchain.pem" ]; then
        local expiry=$(openssl x509 -enddate -noout -in "/etc/letsencrypt/live/$(hostname -f)/fullchain.pem" | cut -d= -f2)
        local expiry_epoch=$(date -d "$expiry" +%s)
        local current_epoch=$(date +%s)
        local days_until_expiry=$(( (expiry_epoch - current_epoch) / 86400 ))

        if [ $days_until_expiry -lt 30 ]; then
            send_alert "WARNING" "SSL certificate expires in $days_until_expiry days"
            cert_warnings=1
        fi
    fi

    log_message "SSL Certificate Status: $([ $cert_warnings -eq 0 ] && echo "OK" || echo "WARNINGS")"
}

# Main monitoring function
main() {
    log_message "=== System Monitor Started ==="

    check_cpu
    check_memory
    check_disk
    check_load
    check_containers
    check_application
    check_ssl_certificates

    log_message "=== System Monitor Completed ==="
}

# Run monitoring
main
EOF

chmod +x /opt/witchcityrope/monitoring/system-monitor.sh

# Create log analysis script
echo "📋 Creating log analysis script..."
cat > /opt/witchcityrope/monitoring/analyze-logs.sh << 'EOF'
#!/bin/bash
# Log Analysis Script for WitchCityRope
# Analyzes application and system logs for patterns and issues

set -euo pipefail

LOG_FILE="/var/log/witchcityrope/monitoring/log-analysis.log"

log_message() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

analyze_nginx_logs() {
    log_message "=== Analyzing Nginx Logs ==="

    # Check for 4xx errors in last hour
    local errors_4xx=$(find /var/log/nginx/witchcityrope/ -name "*.log" -mmin -60 -exec grep -h " 4[0-9][0-9] " {} \; | wc -l)

    # Check for 5xx errors in last hour
    local errors_5xx=$(find /var/log/nginx/witchcityrope/ -name "*.log" -mmin -60 -exec grep -h " 5[0-9][0-9] " {} \; | wc -l)

    log_message "Nginx Errors (last hour): 4xx: $errors_4xx, 5xx: $errors_5xx"

    if [ "$errors_5xx" -gt 10 ]; then
        log_message "WARNING: High number of 5xx errors detected"
    fi
}

analyze_application_logs() {
    log_message "=== Analyzing Application Logs ==="

    # Check Docker container logs for errors
    for env in production staging; do
        local api_errors=$(docker logs "witchcity-api-$([ "$env" = "production" ] && echo "prod" || echo "staging")" --since="1h" 2>&1 | grep -i error | wc -l)
        local web_errors=$(docker logs "witchcity-web-$([ "$env" = "production" ] && echo "prod" || echo "staging")" --since="1h" 2>&1 | grep -i error | wc -l)

        log_message "$env Environment - API Errors: $api_errors, Web Errors: $web_errors"

        if [ "$api_errors" -gt 5 ] && [ "$env" = "production" ]; then
            log_message "WARNING: High number of production API errors"
        fi
    done
}

analyze_system_logs() {
    log_message "=== Analyzing System Logs ==="

    # Check for authentication failures
    local auth_failures=$(grep -c "authentication failure" /var/log/auth.log 2>/dev/null || echo 0)

    # Check for disk space warnings
    local disk_warnings=$(grep -c "No space left on device" /var/log/syslog 2>/dev/null || echo 0)

    log_message "System Issues - Auth Failures: $auth_failures, Disk Warnings: $disk_warnings"
}

# Main analysis function
main() {
    log_message "=== Log Analysis Started ==="

    analyze_nginx_logs
    analyze_application_logs
    analyze_system_logs

    log_message "=== Log Analysis Completed ==="
}

# Run analysis
main
EOF

chmod +x /opt/witchcityrope/monitoring/analyze-logs.sh

# Create uptime monitoring script
echo "⏱️ Creating uptime monitoring script..."
cat > /opt/witchcityrope/monitoring/uptime-monitor.sh << 'EOF'
#!/bin/bash
# Uptime Monitoring Script for WitchCityRope
# Monitors application uptime and response times

set -euo pipefail

LOG_FILE="/var/log/witchcityrope/monitoring/uptime-monitor.log"
ALERT_FILE="/opt/witchcityrope/alerts/uptime-alerts.log"

log_message() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

send_uptime_alert() {
    local message=$1
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')

    echo "[$timestamp] $message" >> "$ALERT_FILE"
    log_message "ALERT: $message"
}

check_endpoint() {
    local name=$1
    local url=$2
    local max_response_time=${3:-2000}  # milliseconds

    local start_time=$(date +%s%3N)
    local http_code=$(curl -o /dev/null -s -w "%{http_code}" --max-time 10 "$url" || echo "000")
    local end_time=$(date +%s%3N)
    local response_time=$((end_time - start_time))

    log_message "$name: HTTP $http_code, ${response_time}ms"

    if [ "$http_code" = "000" ]; then
        send_uptime_alert "$name is DOWN (connection failed)"
    elif [ "$http_code" != "200" ]; then
        send_uptime_alert "$name returned HTTP $http_code"
    elif [ "$response_time" -gt "$max_response_time" ]; then
        send_uptime_alert "$name slow response: ${response_time}ms (threshold: ${max_response_time}ms)"
    fi
}

# Main monitoring function
main() {
    log_message "=== Uptime Monitor Started ==="

    # Check internal endpoints
    check_endpoint "Production API Health" "http://localhost:5001/health" 1000
    check_endpoint "Staging API Health" "http://localhost:5002/health" 1000
    check_endpoint "Production Web" "http://localhost:3001" 2000
    check_endpoint "Staging Web" "http://localhost:3002" 2000

    # Check external endpoints if domains are configured
    if [ -f "/etc/nginx/sites-available/witchcityrope-production" ]; then
        local prod_domain=$(grep -h "server_name" /etc/nginx/sites-available/witchcityrope-production | head -1 | awk '{print $2}' | sed 's/;//g')
        if [ -n "$prod_domain" ] && [ "$prod_domain" != "_" ]; then
            check_endpoint "Production HTTPS" "https://$prod_domain/health" 3000
        fi
    fi

    if [ -f "/etc/nginx/sites-available/witchcityrope-staging" ]; then
        local staging_domain=$(grep -h "server_name" /etc/nginx/sites-available/witchcityrope-staging | head -1 | awk '{print $2}' | sed 's/;//g')
        if [ -n "$staging_domain" ] && [ "$staging_domain" != "_" ]; then
            check_endpoint "Staging HTTPS" "https://$staging_domain/health" 3000
        fi
    fi

    log_message "=== Uptime Monitor Completed ==="
}

# Run monitoring
main
EOF

chmod +x /opt/witchcityrope/monitoring/uptime-monitor.sh

# Create monitoring dashboard script
echo "📊 Creating monitoring dashboard script..."
cat > /opt/witchcityrope/monitoring/dashboard.sh << 'EOF'
#!/bin/bash
# Monitoring Dashboard for WitchCityRope
# Shows real-time system and application status

set -euo pipefail

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_header() {
    clear
    echo -e "${BLUE}================================${NC}"
    echo -e "${BLUE}  WitchCityRope Monitoring      ${NC}"
    echo -e "${BLUE}  $(date)                        ${NC}"
    echo -e "${BLUE}================================${NC}"
    echo ""
}

check_service_status() {
    local service=$1
    if systemctl is-active --quiet "$service"; then
        echo -e "   ${GREEN}✓${NC} $service: Running"
    else
        echo -e "   ${RED}✗${NC} $service: Stopped"
    fi
}

check_endpoint_status() {
    local name=$1
    local url=$2

    if curl -f -s --max-time 5 "$url" > /dev/null 2>&1; then
        echo -e "   ${GREEN}✓${NC} $name: Healthy"
    else
        echo -e "   ${RED}✗${NC} $name: Unhealthy"
    fi
}

show_system_info() {
    echo -e "${BLUE}System Information:${NC}"

    # System services
    check_service_status "docker"
    check_service_status "nginx"
    check_service_status "redis-server"
    check_service_status "netdata"

    echo ""

    # Resource usage
    local cpu_usage=$(top -bn1 | grep "Cpu(s)" | sed "s/.*, *\([0-9.]*\)%* id.*/\1/" | awk '{print 100 - $1}')
    local memory_usage=$(free | grep Mem | awk '{printf("%.1f", $3/$2 * 100.0)}')
    local disk_usage=$(df / | tail -1 | awk '{print $5}' | sed 's/%//')
    local load_avg=$(uptime | awk -F'load average:' '{print $2}' | awk -F, '{print $1}' | xargs)

    echo "   CPU Usage: ${cpu_usage%.*}%"
    echo "   Memory Usage: ${memory_usage}%"
    echo "   Disk Usage: ${disk_usage}%"
    echo "   Load Average: $load_avg"
    echo ""
}

show_container_status() {
    echo -e "${BLUE}Container Status:${NC}"

    local containers=$(docker ps --filter "name=witchcity-" --format "{{.Names}}")
    if [ -z "$containers" ]; then
        echo -e "   ${YELLOW}⚠${NC} No WitchCityRope containers running"
    else
        while read -r container; do
            local status=$(docker inspect --format '{{.State.Health.Status}}' "$container" 2>/dev/null || echo "unknown")
            case $status in
                "healthy")
                    echo -e "   ${GREEN}✓${NC} $container: Healthy"
                    ;;
                "unhealthy")
                    echo -e "   ${RED}✗${NC} $container: Unhealthy"
                    ;;
                *)
                    echo -e "   ${YELLOW}?${NC} $container: $status"
                    ;;
            esac
        done <<< "$containers"
    fi
    echo ""
}

show_application_health() {
    echo -e "${BLUE}Application Health:${NC}"

    check_endpoint_status "Production API" "http://localhost:5001/health"
    check_endpoint_status "Staging API" "http://localhost:5002/health"
    check_endpoint_status "Production Web" "http://localhost:3001"
    check_endpoint_status "Staging Web" "http://localhost:3002"

    echo ""
}

show_recent_alerts() {
    echo -e "${BLUE}Recent Alerts (Last 24h):${NC}"

    if [ -f "/opt/witchcityrope/alerts/system-alerts.log" ]; then
        local alert_count=$(find /opt/witchcityrope/alerts/ -name "*.log" -mtime -1 -exec cat {} \; | wc -l)
        if [ "$alert_count" -eq 0 ]; then
            echo -e "   ${GREEN}✓${NC} No alerts in last 24 hours"
        else
            echo -e "   ${YELLOW}⚠${NC} $alert_count alerts in last 24 hours"
            find /opt/witchcityrope/alerts/ -name "*.log" -mtime -1 -exec tail -5 {} \; | tail -3
        fi
    else
        echo -e "   ${GREEN}✓${NC} No alert history found"
    fi
    echo ""
}

# Main dashboard function
main() {
    # Check if running in watch mode
    if [ "${1:-}" = "--watch" ]; then
        while true; do
            print_header
            show_system_info
            show_container_status
            show_application_health
            show_recent_alerts

            echo "Press Ctrl+C to exit, or wait 30 seconds for refresh..."
            sleep 30
        done
    else
        print_header
        show_system_info
        show_container_status
        show_application_health
        show_recent_alerts

        echo "Run with --watch for continuous monitoring"
    fi
}

# Run dashboard
main "$@"
EOF

chmod +x /opt/witchcityrope/monitoring/dashboard.sh

# Set up cron jobs for monitoring
echo "⏰ Setting up monitoring cron jobs..."

# Update notification settings in monitoring scripts if provided
if [ -n "$EMAIL" ] || [ -n "$SLACK_WEBHOOK" ]; then
    sed -i "s/EMAIL=\"\"/EMAIL=\"$EMAIL\"/" /opt/witchcityrope/monitoring/system-monitor.sh
    sed -i "s|SLACK_WEBHOOK=\"\"|SLACK_WEBHOOK=\"$SLACK_WEBHOOK\"|" /opt/witchcityrope/monitoring/system-monitor.sh
fi

# Add monitoring jobs to cron
(crontab -l 2>/dev/null; cat << 'EOF'
# WitchCityRope Monitoring Jobs

# System monitoring every 5 minutes
*/5 * * * * /opt/witchcityrope/monitoring/system-monitor.sh >> /var/log/witchcityrope/monitoring/cron.log 2>&1

# Uptime monitoring every 2 minutes
*/2 * * * * /opt/witchcityrope/monitoring/uptime-monitor.sh >> /var/log/witchcityrope/monitoring/cron.log 2>&1

# Log analysis every hour
0 * * * * /opt/witchcityrope/monitoring/analyze-logs.sh >> /var/log/witchcityrope/monitoring/cron.log 2>&1

# Clean up old monitoring logs (keep 30 days)
0 1 * * * find /var/log/witchcityrope/monitoring -name "*.log" -mtime +30 -delete

# Clean up old alert logs (keep 90 days)
0 2 * * * find /opt/witchcityrope/alerts -name "*.log" -mtime +90 -delete
EOF
) | crontab -

echo "✅ Monitoring cron jobs configured"

# Configure log rotation for monitoring logs
echo "🗞️ Configuring log rotation for monitoring..."
sudo tee /etc/logrotate.d/witchcityrope-monitoring > /dev/null << 'EOF'
/var/log/witchcityrope/monitoring/*.log {
    daily
    rotate 30
    compress
    delaycompress
    missingok
    notifempty
    create 0644 witchcity witchcity
    postrotate
        # No action needed for monitoring logs
    endscript
}

/opt/witchcityrope/alerts/*.log {
    weekly
    rotate 12
    compress
    delaycompress
    missingok
    notifempty
    create 0644 witchcity witchcity
}
EOF

# Create monitoring summary report
echo "📄 Creating monitoring summary script..."
cat > /opt/witchcityrope/monitoring-report.sh << 'EOF'
#!/bin/bash
# Monitoring Summary Report for WitchCityRope
# Generates a comprehensive monitoring report

set -euo pipefail

REPORT_FILE="/opt/witchcityrope/monitoring/weekly-report-$(date +%Y%m%d).txt"

generate_report() {
    cat > "$REPORT_FILE" << EOL
=====================================
WitchCityRope Weekly Monitoring Report
Generated: $(date)
=====================================

SYSTEM OVERVIEW:
- Server Uptime: $(uptime -p)
- OS: $(cat /etc/os-release | grep PRETTY_NAME | cut -d'"' -f2)
- Kernel: $(uname -r)
- Load Average: $(uptime | awk -F'load average:' '{print $2}')

RESOURCE UTILIZATION (Current):
- CPU Usage: $(top -bn1 | grep "Cpu(s)" | sed "s/.*, *\([0-9.]*\)%* id.*/\1/" | awk '{print 100 - $1}')%
- Memory Usage: $(free | grep Mem | awk '{printf("%.1f", $3/$2 * 100.0)}')%
- Disk Usage: $(df / | tail -1 | awk '{print $5}')
- Available Space: $(df -h / | tail -1 | awk '{print $4}')

CONTAINER STATUS:
$(docker ps --filter "name=witchcity-" --format "- {{.Names}}: {{.Status}}")

APPLICATION HEALTH (Last Check):
$(tail -20 /var/log/witchcityrope/monitoring/uptime-monitor.log | grep -E "(Production|Staging)" | tail -4)

ALERTS SUMMARY (Last 7 Days):
$(find /opt/witchcityrope/alerts -name "*.log" -mtime -7 -exec cat {} \; | wc -l) total alerts
$(find /opt/witchcityrope/alerts -name "*.log" -mtime -7 -exec grep -l "CRITICAL" {} \; | wc -l) critical alerts
$(find /opt/witchcityrope/alerts -name "*.log" -mtime -7 -exec grep -l "WARNING" {} \; | wc -l) warning alerts

RECENT CRITICAL ISSUES:
$(find /opt/witchcityrope/alerts -name "*.log" -mtime -7 -exec grep "CRITICAL" {} \; | tail -5)

PERFORMANCE METRICS:
- Average API Response Time: $(grep "Health" /var/log/witchcityrope/monitoring/uptime-monitor.log | tail -20 | grep -o '[0-9]*ms' | sed 's/ms//' | awk '{sum+=$1; count++} END {if(count>0) print sum/count "ms"; else print "N/A"}')
- SSL Certificate Status: $([ -f /etc/letsencrypt/live/*/fullchain.pem ] && echo "Valid" || echo "Not configured")

RECOMMENDATIONS:
$(if [ "$(df / | tail -1 | awk '{print $5}' | sed 's/%//')" -gt 80 ]; then echo "- Consider disk cleanup or expansion"; fi)
$(if [ "$(free | grep Mem | awk '{printf("%.0f", $3/$2 * 100.0)}')" -gt 85 ]; then echo "- Monitor memory usage closely"; fi)
$(if [ "$(find /opt/witchcityrope/alerts -name "*.log" -mtime -1 -exec cat {} \; | wc -l)" -gt 5 ]; then echo "- Review recent alerts for recurring issues"; fi)

Report generated by WitchCityRope monitoring system.
=====================================
EOL

    echo "📊 Weekly report generated: $REPORT_FILE"

    # Email report if email is configured
    if [ -n "${EMAIL:-}" ] && command -v mail &> /dev/null; then
        mail -s "WitchCityRope Weekly Monitoring Report" "$EMAIL" < "$REPORT_FILE"
        echo "📧 Report emailed to $EMAIL"
    fi
}

generate_report
EOF

chmod +x /opt/witchcityrope/monitoring-report.sh

# Add weekly report to cron
(crontab -l 2>/dev/null; echo "0 9 * * 1 /opt/witchcityrope/monitoring-report.sh") | crontab -

# Test monitoring setup
echo "🧪 Testing monitoring setup..."
/opt/witchcityrope/monitoring/system-monitor.sh
echo "✅ System monitor test completed"

# Final summary
echo ""
echo "✅ Monitoring setup completed successfully!"
echo ""
echo "📋 Setup Summary:"
echo "   • Netdata monitoring: Installed and configured (port 19999)"
echo "   • System monitoring: Every 5 minutes"
echo "   • Uptime monitoring: Every 2 minutes"
echo "   • Log analysis: Every hour"
echo "   • Weekly reports: Every Monday at 9 AM"
echo "   • Log rotation: Configured for all monitoring logs"
echo "   • Alerts: $([ -n "$EMAIL" ] && echo "Email notifications configured" || echo "Local logging only")"
echo ""
echo "📁 Important Files:"
echo "   • System monitor: /opt/witchcityrope/monitoring/system-monitor.sh"
echo "   • Uptime monitor: /opt/witchcityrope/monitoring/uptime-monitor.sh"
echo "   • Log analyzer: /opt/witchcityrope/monitoring/analyze-logs.sh"
echo "   • Dashboard: /opt/witchcityrope/monitoring/dashboard.sh"
echo "   • Weekly report: /opt/witchcityrope/monitoring-report.sh"
echo ""
echo "🔧 Useful Commands:"
echo "   • View dashboard: /opt/witchcityrope/monitoring/dashboard.sh"
echo "   • Watch dashboard: /opt/witchcityrope/monitoring/dashboard.sh --watch"
echo "   • View monitoring logs: tail -f /var/log/witchcityrope/monitoring/system-monitor.log"
echo "   • View alerts: tail -f /opt/witchcityrope/alerts/system-alerts.log"
echo "   • Generate report: /opt/witchcityrope/monitoring-report.sh"
echo "   • Netdata UI: http://localhost:19999 (local access only)"
echo ""
echo "📊 Monitoring URLs:"
echo "   • Netdata Dashboard: http://localhost:19999"
echo "   • Application Dashboard: /opt/witchcityrope/monitoring/dashboard.sh"
echo ""
echo "🚨 NEXT STEPS:"
echo "   1. Access Netdata dashboard to verify system monitoring"
echo "   2. Test alert notifications (if configured)"
echo "   3. Run final script: 07-backup-setup.sh"
echo ""
echo "📅 Completed at: $(date)"