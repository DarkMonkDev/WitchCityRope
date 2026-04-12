#!/bin/bash
# WitchCityRope Production Server Health Check - Data Collection
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# SSHes into the production/staging server, collects logs, system stats,
# container health, and application metrics, then outputs everything for analysis.
#
# Usage:
#   bash .claude/skills/check-production-server/execute.sh [OPTIONS]
#
# Options:
#   --hours N         Look back N hours for logs (default: 24)
#   --env ENV         Environment: production (default) or staging
#   --skip-db-logs    Skip querying application_logs table (faster)
#   --output FILE     Write raw output to file (default: /tmp/wcr-server-check-<env>-<timestamp>.txt)

set -e

# ============================================
# CONFIGURATION
# ============================================

HOURS=24
ENVIRONMENT="production"
SKIP_DB_LOGS=false
OUTPUT_FILE=""

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --hours)
            HOURS="$2"
            shift 2
            ;;
        --env)
            ENVIRONMENT="$2"
            shift 2
            ;;
        --skip-db-logs)
            SKIP_DB_LOGS=true
            shift
            ;;
        --output)
            OUTPUT_FILE="$2"
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Set environment-specific values
if [ "$ENVIRONMENT" = "production" ]; then
    API_CONTAINER="witchcity-api-prod"
    WEB_CONTAINER="witchcity-web-prod"
    REDIS_CONTAINER="witchcity-redis-prod"
    APP_DIR="/opt/witchcityrope/production"
    COMPOSE_FILE="docker-compose.production.yml"
    SITE_URL="https://witchcityrope.com"
    INTERNAL_API_PORT=5001
    INTERNAL_WEB_PORT=3001
    # Actual deployed nginx log paths on the droplet (flat filenames under /var/log/nginx/,
    # not a witchcityrope/ subdirectory as an older setup script suggested). Verified 2026-04-12
    # against the live sites-enabled config and /var/log/nginx/ directory listing.
    NGINX_ACCESS_LOG="/var/log/nginx/witchcityrope-production-access.log"
    NGINX_ACCESS_LOG_PREV="/var/log/nginx/witchcityrope-production-access.log.1"
    NGINX_ERROR_LOG="/var/log/nginx/witchcityrope-production-error.log"
    VAULT_SECRET_PATH="secret/projects/witchcityrope/production"
    VAULT_DB_FIELD="PROD_DB_CONNECTION_STRING"
elif [ "$ENVIRONMENT" = "staging" ]; then
    API_CONTAINER="witchcity-api-staging"
    WEB_CONTAINER="witchcity-web-staging"
    REDIS_CONTAINER="witchcity-redis-staging"
    APP_DIR="/opt/witchcityrope/staging"
    COMPOSE_FILE="docker-compose.staging.yml"
    SITE_URL="https://staging.notfai.com"
    INTERNAL_API_PORT=5002
    INTERNAL_WEB_PORT=3002
    # Staging for WCR is served by the notfai-staging nginx site block (staging.witchcityrope.com
    # aliased with staging.notfai.com). That site block has `access_log off;`, so there's no
    # staging-specific access log on disk. Point at a path that will simply not-exist so the
    # graceful "access logging disabled" fallback kicks in. 2026-04-12
    NGINX_ACCESS_LOG="/var/log/nginx/witchcityrope-staging-access.log"
    NGINX_ACCESS_LOG_PREV="/var/log/nginx/witchcityrope-staging-access.log.1"
    NGINX_ERROR_LOG="/var/log/nginx/witchcityrope-staging-error.log"
    VAULT_SECRET_PATH="secret/projects/witchcityrope/staging"
    VAULT_DB_FIELD="STAGING_DB_CONNECTION_STRING"
else
    echo "Unknown environment: $ENVIRONMENT (use 'production' or 'staging')"
    exit 1
fi

SERVER="104.131.165.14"
USER="witchcity"

TIMESTAMP=$(date +%Y%m%d-%H%M%S)
if [ -z "$OUTPUT_FILE" ]; then
    OUTPUT_FILE="/tmp/wcr-server-check-${ENVIRONMENT}-${TIMESTAMP}.txt"
fi

# ============================================
# VAULT INTEGRATION
# ============================================

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../_shared/vault-helpers.sh"

# ============================================
# HELPER FUNCTIONS
# ============================================

log() {
    echo "$@" | tee -a "$OUTPUT_FILE"
}

section() {
    echo "" | tee -a "$OUTPUT_FILE"
    echo "================================================================" | tee -a "$OUTPUT_FILE"
    echo "  $1" | tee -a "$OUTPUT_FILE"
    echo "================================================================" | tee -a "$OUTPUT_FILE"
    echo "" | tee -a "$OUTPUT_FILE"
}

subsection() {
    echo "" | tee -a "$OUTPUT_FILE"
    echo "--- $1 ---" | tee -a "$OUTPUT_FILE"
    echo "" | tee -a "$OUTPUT_FILE"
}

ssh_cmd() {
    ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no -o ConnectTimeout=10 "$USER@$SERVER" "$@" 2>/dev/null
}

# psql helper - runs locally against managed DB
psql_query() {
    PGPASSWORD="$DB_PASSWORD" psql \
        -h "$DB_HOST" \
        -p "${DB_PORT:-25060}" \
        -U "$DB_USER" \
        -d "$DB_NAME" \
        --set=sslmode=require \
        -c "$1" 2>&1
}

# Clean up old check logs (keep last 7 days)
find /tmp -name "wcr-server-check-*.txt" -mtime +7 -delete 2>/dev/null || true

# ============================================
# PREREQUISITE CHECKS
# ============================================

echo "=========================================="
echo "  WitchCityRope Server Health Check"
echo "=========================================="
echo ""
echo "Environment: $ENVIRONMENT"
echo "Lookback:    $HOURS hours"
echo "Output:      $OUTPUT_FILE"
echo ""

# Initialize output file
cat > "$OUTPUT_FILE" << EOF
================================================================
  WITCHCITYROPE SERVER HEALTH CHECK REPORT
  Environment: $ENVIRONMENT
  Generated:   $(date -u +"%Y-%m-%d %H:%M:%S UTC")
  Lookback:    $HOURS hours
================================================================

EOF

# Vault init
echo "Initializing Vault..."
vault_init

# Get SSH key from vault
SSH_KEY_FILENAME=$(vault_get_field "secret/shared/digitalocean" "SSH_KEY_FILENAME")
SSH_KEY="$HOME/.ssh/$SSH_KEY_FILENAME"
if [ ! -f "$SSH_KEY" ]; then
    echo "FAIL: SSH key not found at $SSH_KEY"
    exit 1
fi
echo "SSH key: $SSH_KEY"

# Test SSH connection
if ! ssh_cmd "echo connected" > /dev/null 2>&1; then
    echo "FAIL: Cannot SSH to $USER@$SERVER"
    exit 1
fi
echo "SSH:   Connected to $USER@$SERVER"

# Pull DB connection string from Vault (used for DB-dependent sections)
DB_HOST=""
if [ "$SKIP_DB_LOGS" = false ]; then
    DB_CONNECTION=$(vault_get_field "$VAULT_SECRET_PATH" "$VAULT_DB_FIELD" 2>/dev/null || echo "")
    if [ -n "$DB_CONNECTION" ]; then
        DB_HOST=$(echo "$DB_CONNECTION" | grep -oP 'Host=\K[^;]+')
        DB_PORT=$(echo "$DB_CONNECTION" | grep -oP 'Port=\K[^;]+')
        DB_NAME=$(echo "$DB_CONNECTION" | grep -oP 'Database=\K[^;]+')
        DB_USER=$(echo "$DB_CONNECTION" | grep -oP 'Username=\K[^;]+')
        DB_PASSWORD=$(echo "$DB_CONNECTION" | grep -oP 'Password=\K[^;]+')
        if [ -n "$DB_HOST" ] && [ -n "$DB_NAME" ]; then
            echo "DB:    $DB_USER@$DB_HOST:$DB_PORT/$DB_NAME"
        else
            echo "DB:    connection string parse failed — DB sections will be skipped"
            DB_HOST=""
        fi
    else
        echo "DB:    $VAULT_DB_FIELD not in vault — DB sections will be skipped"
    fi
fi

# Check psql available locally
if [ -n "$DB_HOST" ] && ! command -v psql > /dev/null 2>&1; then
    echo "WARN: psql not installed locally — DB sections will be skipped"
    DB_HOST=""
fi

echo ""
echo "Collecting data..."
echo ""

# ============================================
# SECTION 1: SERVER SYSTEM HEALTH
# ============================================

section "1. SERVER SYSTEM HEALTH"

subsection "1.1 System Uptime & Load"
ssh_cmd "uptime" >> "$OUTPUT_FILE" 2>&1

subsection "1.2 Memory Usage"
ssh_cmd "free -h" >> "$OUTPUT_FILE" 2>&1

subsection "1.3 Disk Usage"
ssh_cmd "df -h / /opt" >> "$OUTPUT_FILE" 2>&1

subsection "1.4 CPU Usage (top 10 processes)"
ssh_cmd "ps aux --sort=-%cpu | head -12" >> "$OUTPUT_FILE" 2>&1

subsection "1.5 Memory Usage (top 10 processes)"
ssh_cmd "ps aux --sort=-%mem | head -12" >> "$OUTPUT_FILE" 2>&1

subsection "1.6 Docker Disk Usage"
ssh_cmd "docker system df" >> "$OUTPUT_FILE" 2>&1

subsection "1.7 Open File Descriptors / Connections"
ssh_cmd "ss -s" >> "$OUTPUT_FILE" 2>&1

echo "  [1/9] System health collected"

# ============================================
# SECTION 2: DOCKER CONTAINER STATUS
# ============================================

section "2. DOCKER CONTAINER STATUS"

subsection "2.1 All Running Containers on Server"
ssh_cmd "docker ps --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}\t{{.Image}}'" >> "$OUTPUT_FILE" 2>&1

subsection "2.2 WitchCityRope Container Details (API / Web / Redis)"
for CONTAINER in "$API_CONTAINER" "$WEB_CONTAINER" "$REDIS_CONTAINER"; do
    echo "" >> "$OUTPUT_FILE"
    echo "== $CONTAINER ==" >> "$OUTPUT_FILE"
    ssh_cmd "docker inspect $CONTAINER --format 'Status: {{.State.Status}} | Started: {{.State.StartedAt}} | Restarts: {{.RestartCount}} | Health: {{if .State.Health}}{{.State.Health.Status}}{{else}}n/a{{end}}'" >> "$OUTPUT_FILE" 2>&1 || echo "Container $CONTAINER not found" >> "$OUTPUT_FILE"
done

subsection "2.3 Container Resource Usage"
ssh_cmd "docker stats --no-stream --format 'table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}\t{{.NetIO}}\t{{.BlockIO}}' $API_CONTAINER $WEB_CONTAINER $REDIS_CONTAINER" >> "$OUTPUT_FILE" 2>&1 || echo "Could not get container stats" >> "$OUTPUT_FILE"

subsection "2.4 API Container Health-Check History (last 5)"
ssh_cmd "docker inspect $API_CONTAINER --format '{{range .State.Health.Log}}{{.End}} | {{.ExitCode}} | {{.Output}}{{end}}'" >> "$OUTPUT_FILE" 2>&1 || echo "No health check history for $API_CONTAINER" >> "$OUTPUT_FILE"

subsection "2.5 Web Container Health-Check History (last 5)"
ssh_cmd "docker inspect $WEB_CONTAINER --format '{{range .State.Health.Log}}{{.End}} | {{.ExitCode}} | {{.Output}}{{end}}'" >> "$OUTPUT_FILE" 2>&1 || echo "No health check history for $WEB_CONTAINER" >> "$OUTPUT_FILE"

echo "  [2/9] Container status collected"

# ============================================
# SECTION 3: APPLICATION DOCKER LOGS
# ============================================

section "3. APPLICATION DOCKER LOGS (last $HOURS hours)"

# The API container is where application logs live (Serilog stdout + DB sink).
# Serilog compact JSON format: "@l":"Error", "@m":"rendered message", "@mt":"template"

subsection "3.1 API Error / Fatal Logs"
ssh_cmd "docker logs $API_CONTAINER --since ${HOURS}h 2>&1 | grep -E '\"@l\":\"(Error|Fatal)\"|\"Level\":\"(Error|Fatal)\"|Exception|CRITICAL|FATAL' | tail -200" >> "$OUTPUT_FILE" 2>&1 || echo "No error logs found (or could not parse)" >> "$OUTPUT_FILE"

subsection "3.2 API Warning Logs"
ssh_cmd "docker logs $API_CONTAINER --since ${HOURS}h 2>&1 | grep -E '\"@l\":\"Warning\"|\"Level\":\"Warning\"' | tail -100" >> "$OUTPUT_FILE" 2>&1 || echo "No warning logs found" >> "$OUTPUT_FILE"

subsection "3.3 API Error Summary (grouped by message)"
ssh_cmd "docker logs $API_CONTAINER --since ${HOURS}h 2>&1 | grep -E '\"@l\":\"(Error|Fatal)\"|\"Level\":\"(Error|Fatal)\"' | grep -oP '\"@m\":\"[^\"]+\"|\"Message\":\"[^\"]+\"' | sort | uniq -c | sort -rn | head -30" >> "$OUTPUT_FILE" 2>&1 || echo "Could not summarize errors" >> "$OUTPUT_FILE"

subsection "3.4 HTTP 4xx/5xx Responses"
ssh_cmd "docker logs $API_CONTAINER --since ${HOURS}h 2>&1 | grep -oP '\"StatusCode\":\s*[45]\d\d|responded [45]\d\d' | sort | uniq -c | sort -rn | head -20" >> "$OUTPUT_FILE" 2>&1 || echo "Could not extract HTTP status codes" >> "$OUTPUT_FILE"

subsection "3.5 Slow Requests"
ssh_cmd "docker logs $API_CONTAINER --since ${HOURS}h 2>&1 | grep -iE 'Elapsed.*[0-9]{4,}|timeout|slow' | tail -50" >> "$OUTPUT_FILE" 2>&1 || echo "No slow request logs found" >> "$OUTPUT_FILE"

subsection "3.6 Payment-Related Logs (PayPal / payment processing)"
ssh_cmd "docker logs $API_CONTAINER --since ${HOURS}h 2>&1 | grep -iE 'payment|paypal|refund|webhook|ticket.?purchase' | tail -50" >> "$OUTPUT_FILE" 2>&1 || echo "No payment logs found" >> "$OUTPUT_FILE"

subsection "3.7 Event Registration / Attendance Logs"
ssh_cmd "docker logs $API_CONTAINER --since ${HOURS}h 2>&1 | grep -iE 'attendance|rsvp|registration|waitlist|session.?capacity' | tail -50" >> "$OUTPUT_FILE" 2>&1 || echo "No event registration logs found" >> "$OUTPUT_FILE"

subsection "3.8 Authentication / Session Logs"
ssh_cmd "docker logs $API_CONTAINER --since ${HOURS}h 2>&1 | grep -iE 'login|logout|auth|session|identity|refresh.?token|register.?user' | tail -50" >> "$OUTPUT_FILE" 2>&1 || echo "No auth logs found" >> "$OUTPUT_FILE"

subsection "3.9 Vetting Workflow Logs"
ssh_cmd "docker logs $API_CONTAINER --since ${HOURS}h 2>&1 | grep -iE 'vetting|workflowstatus|approve|reject|interview' | tail -30" >> "$OUTPUT_FILE" 2>&1 || echo "No vetting logs found" >> "$OUTPUT_FILE"

subsection "3.10 Database / Connection Issues"
ssh_cmd "docker logs $API_CONTAINER --since ${HOURS}h 2>&1 | grep -iE 'npgsql|connection.*(refused|timeout|reset|pool)|deadlock|database.*error' | tail -50" >> "$OUTPUT_FILE" 2>&1 || echo "No database connection issues found" >> "$OUTPUT_FILE"

subsection "3.11 Container Restart / Crash Signals"
ssh_cmd "docker logs $API_CONTAINER --since ${HOURS}h 2>&1 | grep -iE 'Starting WitchCity|Application is shutting down|Unhandled exception|Application startup exception|Host terminated' | tail -20" >> "$OUTPUT_FILE" 2>&1 || echo "No restart/crash signals found" >> "$OUTPUT_FILE"

subsection "3.12 Hangfire Job Logs"
ssh_cmd "docker logs $API_CONTAINER --since ${HOURS}h 2>&1 | grep -iE 'hangfire|recurring.?job|backup.?job|retention.?job|email.?scheduler' | tail -30" >> "$OUTPUT_FILE" 2>&1 || echo "No Hangfire logs found" >> "$OUTPUT_FILE"

subsection "3.13 Last 30 Lines of API Container Log"
ssh_cmd "docker logs $API_CONTAINER --tail 30 2>&1" >> "$OUTPUT_FILE" 2>&1

subsection "3.14 Last 15 Lines of Web Container Log"
ssh_cmd "docker logs $WEB_CONTAINER --tail 15 2>&1" >> "$OUTPUT_FILE" 2>&1

echo "  [3/9] Docker logs collected"

# ============================================
# SECTION 4: SERILOG DATABASE LOGS (logging.application_logs)
# ============================================

section "4. SERILOG DATABASE LOGS (logging.application_logs)"

if [ "$SKIP_DB_LOGS" = true ] || [ -z "$DB_HOST" ]; then
    echo "  Skipped (--skip-db-logs or DB unavailable)" >> "$OUTPUT_FILE"
    echo "  [4/9] Database logs skipped"
else
    # Serilog PostgreSQL sink level values: 0=Verbose, 1=Debug, 2=Information, 3=Warning, 4=Error, 5=Fatal
    # Schema: logging; Table: application_logs (snake_case columns)
    # Properties JSONB is FLAT (not nested under 'Properties') — query directly: properties->>'UserAgent'
    # RequestPath is a first-class column (not nested in properties)

    subsection "4.1 Total Log Count (last $HOURS hours)"
    psql_query "SELECT COUNT(*) FROM logging.application_logs WHERE timestamp >= NOW() - INTERVAL '$HOURS hours';" >> "$OUTPUT_FILE"

    subsection "4.2 Log Count by Level (last $HOURS hours)"
    psql_query "SELECT level, level_name, COUNT(*) as count FROM logging.application_logs WHERE timestamp >= NOW() - INTERVAL '$HOURS hours' GROUP BY level, level_name ORDER BY count DESC;" >> "$OUTPUT_FILE"

    subsection "4.3 Error Logs - Full Details (last $HOURS hours, newest 50)"
    psql_query "SELECT timestamp, LEFT(message, 300) as message, LEFT(exception, 500) as exception_preview FROM logging.application_logs WHERE timestamp >= NOW() - INTERVAL '$HOURS hours' AND level >= 4 ORDER BY timestamp DESC LIMIT 50;" >> "$OUTPUT_FILE"

    subsection "4.4 Error Log Grouping by Message Template (last $HOURS hours)"
    psql_query "SELECT LEFT(message_template, 200) as template, COUNT(*) as count, MAX(timestamp) as last_occurrence FROM logging.application_logs WHERE timestamp >= NOW() - INTERVAL '$HOURS hours' AND level >= 4 GROUP BY message_template ORDER BY count DESC LIMIT 30;" >> "$OUTPUT_FILE"

    subsection "4.5 Warning Logs Grouped by Template (last $HOURS hours)"
    psql_query "SELECT LEFT(message_template, 200) as template, COUNT(*) as count, MAX(timestamp) as last_occurrence FROM logging.application_logs WHERE timestamp >= NOW() - INTERVAL '$HOURS hours' AND level = 3 GROUP BY message_template ORDER BY count DESC LIMIT 20;" >> "$OUTPUT_FILE"

    subsection "4.6 Errors by Hour (last $HOURS hours)"
    psql_query "SELECT date_trunc('hour', timestamp) as hour, COUNT(*) as error_count FROM logging.application_logs WHERE timestamp >= NOW() - INTERVAL '$HOURS hours' AND level >= 4 GROUP BY hour ORDER BY hour DESC;" >> "$OUTPUT_FILE"

    subsection "4.7 Exceptions with Stack Traces (last $HOURS hours, newest 10)"
    psql_query "SELECT timestamp, LEFT(message, 200) as message, LEFT(exception, 600) as exception_preview FROM logging.application_logs WHERE timestamp >= NOW() - INTERVAL '$HOURS hours' AND exception IS NOT NULL AND exception != '' ORDER BY timestamp DESC LIMIT 10;" >> "$OUTPUT_FILE"

    subsection "4.8 Payment / Checkout Related Logs (last $HOURS hours)"
    psql_query "SELECT timestamp, level_name, LEFT(message, 250) as message FROM logging.application_logs WHERE timestamp >= NOW() - INTERVAL '$HOURS hours' AND level >= 3 AND (message ILIKE '%payment%' OR message ILIKE '%paypal%' OR message ILIKE '%refund%' OR message ILIKE '%ticketpurchase%' OR source_context ILIKE '%Payment%') ORDER BY timestamp DESC LIMIT 30;" >> "$OUTPUT_FILE"

    subsection "4.9 Database / Connection Errors (last $HOURS hours)"
    psql_query "SELECT timestamp, level_name, LEFT(message, 250) as message, LEFT(exception, 300) as exception_preview FROM logging.application_logs WHERE timestamp >= NOW() - INTERVAL '$HOURS hours' AND level >= 3 AND (message ILIKE '%connection%' OR message ILIKE '%timeout%' OR message ILIKE '%npgsql%' OR message ILIKE '%deadlock%' OR exception ILIKE '%PostgresException%' OR exception ILIKE '%NpgsqlException%' OR exception ILIKE '%timeout%') ORDER BY timestamp DESC LIMIT 20;" >> "$OUTPUT_FILE"

    subsection "4.10 Application Logs Table Size"
    psql_query "SELECT pg_size_pretty(pg_total_relation_size('logging.application_logs')) as total_size, (SELECT COUNT(*) FROM logging.application_logs) as total_rows;" >> "$OUTPUT_FILE"

    # --------------------------------------------------------
    # ERROR FORENSICS - ACTUAL EVIDENCE FOR ROOT-CAUSING
    # --------------------------------------------------------

    subsection "4.11 Error Forensics - Request Paths and User Agents (last $HOURS hours)"
    echo "# Shows WHICH pages caused errors and WHO requested them." >> "$OUTPUT_FILE"
    echo "# Use to distinguish scanner/bot errors from real app bugs." >> "$OUTPUT_FILE"
    psql_query "
        SELECT
            LEFT(request_path, 100) as request_path,
            LEFT(properties->>'UserAgent', 120) as user_agent,
            COUNT(*) as error_count
        FROM logging.application_logs
        WHERE level >= 4
            AND timestamp >= NOW() - INTERVAL '$HOURS hours'
            AND request_path IS NOT NULL
        GROUP BY request_path, user_agent
        ORDER BY error_count DESC
        LIMIT 40;
    " >> "$OUTPUT_FILE"

    subsection "4.12 Error Forensics - Malicious User Agent Detection (last $HOURS hours)"
    echo "# Detects XSS probes, SQL injection, and fuzzing in user agents." >> "$OUTPUT_FILE"
    echo "# If count > 0, errors were likely caused by a vulnerability scanner, NOT app bugs." >> "$OUTPUT_FILE"
    psql_query "
        SELECT
            LEFT(properties->>'UserAgent', 150) as suspicious_user_agent,
            COUNT(*) as hit_count,
            CASE
                WHEN properties->>'UserAgent' LIKE '%onEvent=%' THEN 'XSS_PROBE'
                WHEN properties->>'UserAgent' LIKE '%<script%' THEN 'XSS_PROBE'
                WHEN properties->>'UserAgent' LIKE '%><qss%' THEN 'XSS_PROBE'
                WHEN properties->>'UserAgent' LIKE '%UNION%SELECT%' THEN 'SQL_INJECTION'
                WHEN properties->>'UserAgent' LIKE '%USER_NAME()%' THEN 'SQL_INJECTION'
                WHEN properties->>'UserAgent' LIKE '%OR 1=1%' THEN 'SQL_INJECTION'
                WHEN properties->>'UserAgent' LIKE 'Mozilla(%' THEN 'FUZZING'
                WHEN properties->>'UserAgent' LIKE 'Mozilla/*' THEN 'FUZZING'
                ELSE 'OTHER_SUSPICIOUS'
            END as attack_type
        FROM logging.application_logs
        WHERE level >= 4
            AND timestamp >= NOW() - INTERVAL '$HOURS hours'
            AND (
                properties->>'UserAgent' LIKE '%onEvent=%'
                OR properties->>'UserAgent' LIKE '%<script%'
                OR properties->>'UserAgent' LIKE '%><qss%'
                OR properties->>'UserAgent' LIKE '%UNION%SELECT%'
                OR properties->>'UserAgent' LIKE '%USER_NAME()%'
                OR properties->>'UserAgent' LIKE '%OR 1=1%'
                OR properties->>'UserAgent' LIKE 'Mozilla(%'
                OR properties->>'UserAgent' LIKE 'Mozilla/*'
            )
        GROUP BY suspicious_user_agent, attack_type
        ORDER BY hit_count DESC
        LIMIT 20;
    " >> "$OUTPUT_FILE"

    subsection "4.13 Error Forensics - PostgreSQL Exception Details (last $HOURS hours)"
    echo "# Extracts PostgreSQL-specific error info (SqlState, Where clause)." >> "$OUTPUT_FILE"
    echo "# The 'Where' field is CRITICAL: 'parameter \$N' = bad input, NOT bad data." >> "$OUTPUT_FILE"
    psql_query "
        SELECT
            timestamp,
            CASE WHEN exception LIKE '%Where:%' THEN substring(exception FROM 'Where: [^\n]+') ELSE NULL END as pg_where_clause,
            CASE WHEN exception LIKE '%SqlState:%' THEN substring(exception FROM 'SqlState: [^\n]+') ELSE NULL END as pg_sqlstate,
            LEFT(message, 200) as message_preview
        FROM logging.application_logs
        WHERE level >= 4
            AND timestamp >= NOW() - INTERVAL '$HOURS hours'
            AND exception LIKE '%PostgresException%'
        ORDER BY timestamp DESC
        LIMIT 20;
    " >> "$OUTPUT_FILE"

    subsection "4.14 Error Forensics - Error Concentration by IP (last $HOURS hours)"
    echo "# Shows if errors are concentrated from specific IPs (scanner indicator)." >> "$OUTPUT_FILE"
    echo "# RemoteIpAddress in properties JSONB = real client IP (set by forwarded-for middleware)." >> "$OUTPUT_FILE"
    psql_query "
        SELECT
            properties->>'RemoteIpAddress' as ip,
            COUNT(*) as error_count,
            COUNT(DISTINCT request_path) as unique_paths,
            COUNT(DISTINCT properties->>'UserAgent') as unique_agents
        FROM logging.application_logs
        WHERE level >= 4
            AND timestamp >= NOW() - INTERVAL '$HOURS hours'
            AND properties->>'RemoteIpAddress' IS NOT NULL
        GROUP BY ip
        ORDER BY error_count DESC
        LIMIT 10;
    " >> "$OUTPUT_FILE"

    echo "  [4/9] Database logs collected"
fi

# ============================================
# SECTION 5: NGINX & NETWORK
# ============================================

section "5. NGINX & NETWORK"

# Access nginx logs via sudo /bin/cp to /dev/stdout (witchcity user has limited sudoers: nginx, certbot, systemctl, cp, ln)

subsection "5.1 Nginx Service Status"
ssh_cmd "sudo /bin/systemctl status nginx --no-pager -l" >> "$OUTPUT_FILE" 2>&1 || echo "Could not get nginx status" >> "$OUTPUT_FILE"

subsection "5.2 Nginx Error Log (last 50 lines)"
ssh_cmd "sudo /bin/cp $NGINX_ERROR_LOG /dev/stdout 2>/dev/null | tail -50" >> "$OUTPUT_FILE" 2>&1 || echo "Could not read $NGINX_ERROR_LOG (may not exist)" >> "$OUTPUT_FILE"

subsection "5.3 Nginx Access Log - 4xx/5xx (last 200 entries)"
ssh_cmd "sudo /bin/cp $NGINX_ACCESS_LOG /dev/stdout 2>/dev/null | awk '\$9 >= 400' | tail -200" >> "$OUTPUT_FILE" 2>&1 || echo "Could not read $NGINX_ACCESS_LOG (access logging may be disabled)" >> "$OUTPUT_FILE"

subsection "5.4 Nginx 4xx/5xx Summary by Status Code"
ssh_cmd "sudo /bin/cp $NGINX_ACCESS_LOG /dev/stdout 2>/dev/null | awk '\$9 >= 400 {count[\$9]++} END {for (c in count) printf \"%s: %d\\n\", c, count[c]}' | sort -t: -k2 -rn" >> "$OUTPUT_FILE" 2>&1 || echo "Could not summarize nginx status codes" >> "$OUTPUT_FILE"

subsection "5.5 SSL Certificate Status"
ssh_cmd "sudo /usr/bin/certbot certificates 2>/dev/null | grep -A4 'Certificate Name\|Expiry'" >> "$OUTPUT_FILE" 2>&1 || echo "Could not check SSL certificates" >> "$OUTPUT_FILE"

subsection "5.5b Certbot Renewal Service Status"
echo "# CRITICAL: If certbot.service shows 'failed', auto-renewal is BROKEN and certs will expire!" >> "$OUTPUT_FILE"
ssh_cmd "systemctl status certbot.service --no-pager 2>/dev/null | head -10; echo '---TIMER---'; systemctl list-timers certbot.timer --no-pager 2>/dev/null | head -5; echo '---RECENT FAILURES---'; sudo /bin/cp /var/log/letsencrypt/letsencrypt.log /dev/stdout 2>/dev/null | grep -iE 'failure|failed|error.*renew|could not be renewed' | tail -10" >> "$OUTPUT_FILE" 2>&1 || echo "Could not check certbot renewal status" >> "$OUTPUT_FILE"

subsection "5.6 Nginx Traffic Analysis - Today (request count, unique IPs)"
ssh_cmd "echo 'Total requests:' && sudo /bin/cp $NGINX_ACCESS_LOG /dev/stdout 2>/dev/null | wc -l && echo 'Unique IPs:' && sudo /bin/cp $NGINX_ACCESS_LOG /dev/stdout 2>/dev/null | awk '{print \$1}' | sort -u | wc -l" >> "$OUTPUT_FILE" 2>&1 || echo "Could not analyze today's traffic (access log missing?)" >> "$OUTPUT_FILE"

subsection "5.7 Nginx Traffic Analysis - Yesterday"
ssh_cmd "echo 'Total requests:' && sudo /bin/cp $NGINX_ACCESS_LOG_PREV /dev/stdout 2>/dev/null | wc -l && echo 'Unique IPs:' && sudo /bin/cp $NGINX_ACCESS_LOG_PREV /dev/stdout 2>/dev/null | awk '{print \$1}' | sort -u | wc -l" >> "$OUTPUT_FILE" 2>&1 || echo "Could not analyze yesterday's traffic (log may not exist)" >> "$OUTPUT_FILE"

subsection "5.8 Top 20 Client IPs (today)"
ssh_cmd "sudo /bin/cp $NGINX_ACCESS_LOG /dev/stdout 2>/dev/null | awk '{print \$1}' | sort | uniq -c | sort -rn | head -20" >> "$OUTPUT_FILE" 2>&1 || echo "Could not extract top IPs" >> "$OUTPUT_FILE"

subsection "5.9 Top 20 User Agents (today)"
ssh_cmd "sudo /bin/cp $NGINX_ACCESS_LOG /dev/stdout 2>/dev/null | awk -F'\"' '{print \$6}' | sort | uniq -c | sort -rn | head -20" >> "$OUTPUT_FILE" 2>&1 || echo "Could not extract user agents" >> "$OUTPUT_FILE"

subsection "5.10 Hourly Request Volume (today)"
ssh_cmd "sudo /bin/cp $NGINX_ACCESS_LOG /dev/stdout 2>/dev/null | awk -F'[:/]' '{print \$4}' | sort | uniq -c | sort -k2n" >> "$OUTPUT_FILE" 2>&1 || echo "Could not compute hourly volume" >> "$OUTPUT_FILE"

subsection "5.11 Bot vs Human Traffic (today, approximate)"
ssh_cmd "sudo /bin/cp $NGINX_ACCESS_LOG /dev/stdout 2>/dev/null | awk -F'\"' '{ua=\$6; if (ua ~ /bot|crawl|spider|slurp|Googlebot|Bingbot|AdsBot|facebookexternalhit|meta-externalagent|ClaudeBot|Amazonbot|Bytespider|GPTBot|CCBot|Applebot|YandexBot|SemrushBot|DotBot|AhrefsBot|MJ12bot|PetalBot/i) bots++; else humans++} END {printf \"Humans: %d\\nBots: %d\\nBot %%: %.1f%%\\n\", humans, bots, (bots/(humans+bots))*100}'" >> "$OUTPUT_FILE" 2>&1 || echo "Could not compute bot/human split" >> "$OUTPUT_FILE"

# --------------------------------------------------------
# SCANNER DETECTION: cross-reference nginx traffic with errors
# --------------------------------------------------------

subsection "5.12 Scanner Detection - Malicious User Agents (today + yesterday)"
echo "# Detects XSS probes, SQL injection, and fuzzing patterns in nginx user agents." >> "$OUTPUT_FILE"
echo "=== TODAY ===" >> "$OUTPUT_FILE"
ssh_cmd "sudo /bin/cp $NGINX_ACCESS_LOG /dev/stdout 2>/dev/null | grep -iE 'onEvent=|<script|><qss|UNION.*SELECT|USER_NAME\(\)|OR.1=1|\\\\x0|\\\\x22' | awk '{print \$1}' | sort | uniq -c | sort -rn | head -10" >> "$OUTPUT_FILE" 2>&1 || echo "No scanner traffic detected today" >> "$OUTPUT_FILE"
echo "=== YESTERDAY ===" >> "$OUTPUT_FILE"
ssh_cmd "sudo /bin/cp $NGINX_ACCESS_LOG_PREV /dev/stdout 2>/dev/null | grep -iE 'onEvent=|<script|><qss|UNION.*SELECT|USER_NAME\(\)|OR.1=1|\\\\x0|\\\\x22' | awk '{print \$1}' | sort | uniq -c | sort -rn | head -10" >> "$OUTPUT_FILE" 2>&1 || echo "No scanner traffic detected yesterday (or log missing)" >> "$OUTPUT_FILE"

subsection "5.13 Scanner Detection - Top IPs by 429 Rate-Limited (today + yesterday)"
echo "# IPs getting rate-limited are likely bots/scanners." >> "$OUTPUT_FILE"
echo "=== TODAY (429 responses) ===" >> "$OUTPUT_FILE"
ssh_cmd "sudo /bin/cp $NGINX_ACCESS_LOG /dev/stdout 2>/dev/null | awk '\$9 == 429 {print \$1}' | sort | uniq -c | sort -rn | head -10" >> "$OUTPUT_FILE" 2>&1 || echo "No 429 responses today" >> "$OUTPUT_FILE"
echo "=== YESTERDAY (429 responses) ===" >> "$OUTPUT_FILE"
ssh_cmd "sudo /bin/cp $NGINX_ACCESS_LOG_PREV /dev/stdout 2>/dev/null | awk '\$9 == 429 {print \$1}' | sort | uniq -c | sort -rn | head -10" >> "$OUTPUT_FILE" 2>&1 || echo "No 429 responses yesterday (or log missing)" >> "$OUTPUT_FILE"

subsection "5.14 Scanner Detection - Top IPs by 500 Errors (today + yesterday)"
echo "# IPs triggering 500 errors — if also rate-limited or using malicious UAs, they're scanners." >> "$OUTPUT_FILE"
echo "=== TODAY (500 responses) ===" >> "$OUTPUT_FILE"
ssh_cmd "sudo /bin/cp $NGINX_ACCESS_LOG /dev/stdout 2>/dev/null | awk '\$9 == 500 {print \$1}' | sort | uniq -c | sort -rn | head -10" >> "$OUTPUT_FILE" 2>&1 || echo "No 500 responses today" >> "$OUTPUT_FILE"
echo "=== YESTERDAY (500 responses) ===" >> "$OUTPUT_FILE"
ssh_cmd "sudo /bin/cp $NGINX_ACCESS_LOG_PREV /dev/stdout 2>/dev/null | awk '\$9 == 500 {print \$1}' | sort | uniq -c | sort -rn | head -10" >> "$OUTPUT_FILE" 2>&1 || echo "No 500 responses yesterday (or log missing)" >> "$OUTPUT_FILE"

subsection "5.15 Scanner Detection - Full Profile of Top Error-Causing IPs"
echo "# For each IP that caused 500 errors, show FULL activity breakdown." >> "$OUTPUT_FILE"
echo "=== YESTERDAY - Top 500-error IP profiles ===" >> "$OUTPUT_FILE"
TOP_500_IPS=$(ssh_cmd "sudo /bin/cp $NGINX_ACCESS_LOG_PREV /dev/stdout 2>/dev/null | awk '\$9 == 500 {print \$1}' | sort | uniq -c | sort -rn | head -5 | awk '{print \$2}'" 2>/dev/null)
if [ -n "$TOP_500_IPS" ]; then
    for IP in $TOP_500_IPS; do
        echo "" >> "$OUTPUT_FILE"
        echo "--- IP: $IP ---" >> "$OUTPUT_FILE"
        echo "Response code breakdown:" >> "$OUTPUT_FILE"
        ssh_cmd "sudo /bin/cp $NGINX_ACCESS_LOG_PREV /dev/stdout 2>/dev/null | grep '^$IP ' | awk '{print \$9}' | sort | uniq -c | sort -rn" >> "$OUTPUT_FILE" 2>&1
        echo "Sample user agents (first 5 unique):" >> "$OUTPUT_FILE"
        ssh_cmd "sudo /bin/cp $NGINX_ACCESS_LOG_PREV /dev/stdout 2>/dev/null | grep '^$IP ' | awk -F'\"' '{print \$6}' | sort -u | head -5" >> "$OUTPUT_FILE" 2>&1
        echo "Total requests:" >> "$OUTPUT_FILE"
        ssh_cmd "sudo /bin/cp $NGINX_ACCESS_LOG_PREV /dev/stdout 2>/dev/null | grep -c '^$IP '" >> "$OUTPUT_FILE" 2>&1
    done
else
    echo "No IPs with 500 errors found yesterday (or access logging disabled)" >> "$OUTPUT_FILE"
fi
echo "" >> "$OUTPUT_FILE"
echo "=== TODAY - Top 500-error IP profiles ===" >> "$OUTPUT_FILE"
TOP_500_IPS_TODAY=$(ssh_cmd "sudo /bin/cp $NGINX_ACCESS_LOG /dev/stdout 2>/dev/null | awk '\$9 == 500 {print \$1}' | sort | uniq -c | sort -rn | head -5 | awk '{print \$2}'" 2>/dev/null)
if [ -n "$TOP_500_IPS_TODAY" ]; then
    for IP in $TOP_500_IPS_TODAY; do
        echo "" >> "$OUTPUT_FILE"
        echo "--- IP: $IP ---" >> "$OUTPUT_FILE"
        echo "Response code breakdown:" >> "$OUTPUT_FILE"
        ssh_cmd "sudo /bin/cp $NGINX_ACCESS_LOG /dev/stdout 2>/dev/null | grep '^$IP ' | awk '{print \$9}' | sort | uniq -c | sort -rn" >> "$OUTPUT_FILE" 2>&1
        echo "Sample user agents (first 5 unique):" >> "$OUTPUT_FILE"
        ssh_cmd "sudo /bin/cp $NGINX_ACCESS_LOG /dev/stdout 2>/dev/null | grep '^$IP ' | awk -F'\"' '{print \$6}' | sort -u | head -5" >> "$OUTPUT_FILE" 2>&1
        echo "Total requests:" >> "$OUTPUT_FILE"
        ssh_cmd "sudo /bin/cp $NGINX_ACCESS_LOG /dev/stdout 2>/dev/null | grep -c '^$IP '" >> "$OUTPUT_FILE" 2>&1
    done
else
    echo "No IPs with 500 errors found today (or access logging disabled)" >> "$OUTPUT_FILE"
fi

echo "  [5/9] Nginx & network collected"

# ============================================
# SECTION 6: HANGFIRE BACKGROUND JOBS
# ============================================

section "6. HANGFIRE BACKGROUND JOBS"

if [ "$SKIP_DB_LOGS" = false ] && [ -n "$DB_HOST" ]; then
    # Hangfire PostgreSQL uses schema 'hangfire' with lowercase column names (no underscores)
    # Columns: id, stateid, statename, invocationdata (jsonb), arguments (jsonb), createdat, expireat, updatecount

    subsection "6.1 Failed Hangfire Jobs (last $HOURS hours)"
    psql_query "SELECT id, statename, createdat, invocationdata->>'Type' as job_type FROM hangfire.job WHERE statename = 'Failed' AND createdat >= NOW() - INTERVAL '$HOURS hours' ORDER BY createdat DESC LIMIT 20;" >> "$OUTPUT_FILE"

    subsection "6.2 Hangfire Job Summary by State"
    psql_query "SELECT statename, COUNT(*) FROM hangfire.job GROUP BY statename ORDER BY COUNT(*) DESC;" >> "$OUTPUT_FILE"

    subsection "6.3 Recurring Jobs Status"
    echo "# Expected WCR recurring jobs: daily-backup, daily-log-summary, log-retention-cleanup, refresh-token-cleanup, event-email-scheduler" >> "$OUTPUT_FILE"
    psql_query "SELECT key, field, value FROM hangfire.hash WHERE key LIKE 'recurring-job:%' AND field IN ('Cron', 'LastExecution', 'NextExecution', 'LastJobId', 'CreatedAt') ORDER BY key, field;" >> "$OUTPUT_FILE"

    echo "  [6/9] Hangfire jobs collected"
else
    echo "  Skipped (DB unavailable)" >> "$OUTPUT_FILE"
    echo "  [6/9] Hangfire jobs skipped"
fi

# ============================================
# SECTION 7: HEALTH ENDPOINTS
# ============================================

section "7. HEALTH ENDPOINTS"

subsection "7.1 Public /health (via site URL)"
PUB_HEALTH=$(curl -s -w "\nHTTP_CODE:%{http_code}\nTIME_TOTAL:%{time_total}" "$SITE_URL/health" 2>/dev/null || echo "FAILED TO CONNECT")
echo "$PUB_HEALTH" >> "$OUTPUT_FILE"

subsection "7.2 Public /api/health"
API_HEALTH=$(curl -s -w "\nHTTP_CODE:%{http_code}\nTIME_TOTAL:%{time_total}" "$SITE_URL/api/health" 2>/dev/null || echo "FAILED TO CONNECT")
echo "$API_HEALTH" >> "$OUTPUT_FILE"

subsection "7.3 Public /api/health/detailed"
API_DETAILED=$(curl -s -w "\nHTTP_CODE:%{http_code}\nTIME_TOTAL:%{time_total}" "$SITE_URL/api/health/detailed" 2>/dev/null || echo "FAILED TO CONNECT")
echo "$API_DETAILED" >> "$OUTPUT_FILE"

subsection "7.4 Internal API /health (bypasses nginx/maintenance mode)"
ssh_cmd "curl -s -w '\nHTTP_CODE:%{http_code}\nTIME_TOTAL:%{time_total}' http://localhost:$INTERNAL_API_PORT/health" >> "$OUTPUT_FILE" 2>&1 || echo "Could not reach internal API" >> "$OUTPUT_FILE"

subsection "7.5 Internal Web Container"
ssh_cmd "curl -s -o /dev/null -w 'HTTP_CODE:%{http_code}\nTIME_TOTAL:%{time_total}\n' http://localhost:$INTERNAL_WEB_PORT/" >> "$OUTPUT_FILE" 2>&1 || echo "Could not reach internal web" >> "$OUTPUT_FILE"

subsection "7.6 Key Page Response Times"
# Paths verified 2026-04-12:
#   /api/kiosk/payments/health        -> KioskPaymentEndpoints.cs (SSE/kiosk subsystem)
#   /api/webhooks/paypal/health       -> WebhookEndpoints.cs (PayPal webhook subsystem)
# The bare "/api/payments/health" and "/api/paypal/health" do NOT exist; earlier skill
# versions probed those and always saw 404.
for path in "/" "/events" "/login" "/register" "/api/health" "/api/kiosk/payments/health" "/api/webhooks/paypal/health"; do
    RESP=$(curl -s -o /dev/null -w "%{http_code} %{time_total}s" "$SITE_URL$path" 2>/dev/null || echo "ERR 0.000s")
    echo "  $path -> $RESP" >> "$OUTPUT_FILE"
done

echo "  [7/9] Health endpoints collected"

# ============================================
# SECTION 8: ENVIRONMENT & CONFIGURATION
# ============================================

section "8. ENVIRONMENT & CONFIGURATION"

subsection "8.1 Docker Image Info (API)"
ssh_cmd "docker inspect $API_CONTAINER --format 'Image: {{.Config.Image}}
Created: {{.Created}}
Platform: {{.Platform}}'" >> "$OUTPUT_FILE" 2>&1 || echo "Could not inspect $API_CONTAINER" >> "$OUTPUT_FILE"

subsection "8.2 Docker Image Info (Web)"
ssh_cmd "docker inspect $WEB_CONTAINER --format 'Image: {{.Config.Image}}
Created: {{.Created}}
Platform: {{.Platform}}'" >> "$OUTPUT_FILE" 2>&1 || echo "Could not inspect $WEB_CONTAINER" >> "$OUTPUT_FILE"

subsection "8.3 API Env Var Keys (no secret values)"
ssh_cmd "docker exec $API_CONTAINER env 2>/dev/null | sed 's/=.*/=***/' | sort" >> "$OUTPUT_FILE" 2>&1 || echo "Could not list env vars" >> "$OUTPUT_FILE"

subsection "8.4 .env File Keys on Server (no values)"
ssh_cmd "cat $APP_DIR/.env* 2>/dev/null | grep -v '^#' | grep '=' | sed 's/=.*//' | sort -u" >> "$OUTPUT_FILE" 2>&1 || echo "Could not read .env files" >> "$OUTPUT_FILE"

subsection "8.5 Docker Compose File Hash"
ssh_cmd "md5sum $APP_DIR/$COMPOSE_FILE 2>/dev/null" >> "$OUTPUT_FILE" 2>&1 || echo "No $COMPOSE_FILE found" >> "$OUTPUT_FILE"

subsection "8.6 Vault Token Status (local)"
$HOME/bin/vault token lookup -format=json 2>/dev/null | grep -E '"expire_time"|"ttl"' >> "$OUTPUT_FILE" || echo "Could not check Vault token" >> "$OUTPUT_FILE"

echo "  [8/9] Environment config collected"

# ============================================
# SECTION 9: DATA INTEGRITY AUDITS
# ============================================

section "9. DATA INTEGRITY AUDITS"

if [ "$SKIP_DB_LOGS" = false ] && [ -n "$DB_HOST" ]; then

    subsection "9.1 Event Capacity vs Active Attendees"
    # Active attendance status = 1. Events where live count exceeds declared Capacity indicate:
    #   - a race in registration logic that skipped the capacity check, OR
    #   - manual DB edits that bypassed the app, OR
    #   - capacity lowered after registrations already existed.
    # Both RSVPs (AttendanceType=1) and Tickets (AttendanceType=2) consume capacity.
    psql_query "
        SELECT e.\"Id\" as event_id, LEFT(e.\"Title\", 60) as title, e.\"StartDate\"::date as start_date, e.\"Capacity\",
               COUNT(*) FILTER (WHERE ea.\"Status\" = 1) as active_total,
               COUNT(*) FILTER (WHERE ea.\"Status\" = 1 AND ea.\"AttendanceType\" = 1) as active_rsvps,
               COUNT(*) FILTER (WHERE ea.\"Status\" = 1 AND ea.\"AttendanceType\" = 2) as active_tickets,
               COUNT(*) FILTER (WHERE ea.\"Status\" = 4) as waitlisted
        FROM public.\"Events\" e
        LEFT JOIN public.\"EventAttendances\" ea ON e.\"Id\" = ea.\"EventId\"
        WHERE e.\"StartDate\" >= NOW() - INTERVAL '180 days'
        GROUP BY e.\"Id\", e.\"Title\", e.\"StartDate\", e.\"Capacity\"
        HAVING COUNT(*) FILTER (WHERE ea.\"Status\" = 1) > e.\"Capacity\"
        ORDER BY (COUNT(*) FILTER (WHERE ea.\"Status\" = 1) - e.\"Capacity\") DESC
        LIMIT 20;
    " >> "$OUTPUT_FILE"

    subsection "9.2 Session Capacity vs Ticketed Registrations (multi-session events)"
    # For multi-session events, each Session has its own Capacity.
    # A ticket covers multiple sessions, so count DISTINCT TicketPurchaseIds per session.
    psql_query "
        SELECT s.\"Id\" as session_id, LEFT(s.\"Name\", 50) as session_name, e.\"Id\" as event_id,
               LEFT(e.\"Title\", 50) as event_title, s.\"StartTime\"::date as start_date, s.\"Capacity\",
               COUNT(DISTINCT ea.\"TicketPurchaseId\") FILTER (WHERE ea.\"Status\" = 1 AND ea.\"AttendanceType\" = 2) as active_tickets
        FROM public.\"Sessions\" s
        JOIN public.\"Events\" e ON s.\"EventId\" = e.\"Id\"
        LEFT JOIN public.\"EventAttendances\" ea ON s.\"Id\" = ea.\"SessionId\"
        WHERE s.\"StartTime\" >= NOW() - INTERVAL '180 days'
        GROUP BY s.\"Id\", s.\"Name\", e.\"Id\", e.\"Title\", s.\"StartTime\", s.\"Capacity\"
        HAVING COUNT(DISTINCT ea.\"TicketPurchaseId\") FILTER (WHERE ea.\"Status\" = 1 AND ea.\"AttendanceType\" = 2) > s.\"Capacity\"
        ORDER BY (COUNT(DISTINCT ea.\"TicketPurchaseId\") FILTER (WHERE ea.\"Status\" = 1 AND ea.\"AttendanceType\" = 2) - s.\"Capacity\") DESC
        LIMIT 20;
    " >> "$OUTPUT_FILE"

    subsection "9.3 Vetting Status Drift (VettingApplications vs Users)"
    # VettingApplication.WorkflowStatus and User.VettingStatus should stay in sync.
    # Enum: 0=UnderReview, 1=InterviewApproved, 2=FinalReview, 3=Approved, 4=Denied, 5=OnHold, 6=Withdrawn
    # Drift usually means a status transition didn't propagate to the User record.
    psql_query "
        SELECT v.\"Id\" as application_id, v.\"UserId\", v.\"WorkflowStatus\" as app_status,
               u.\"VettingStatus\" as user_status, v.\"SubmittedAt\"::date as submitted,
               LEFT(u.\"UserName\", 40) as username
        FROM public.\"VettingApplications\" v
        INNER JOIN public.\"Users\" u ON v.\"UserId\" = u.\"Id\"
        WHERE v.\"IsDeleted\" = false
            AND v.\"WorkflowStatus\" != u.\"VettingStatus\"
        ORDER BY v.\"SubmittedAt\" DESC
        LIMIT 20;
    " >> "$OUTPUT_FILE"

    subsection "9.4 Orphaned Completed Ticket Purchases (no active attendance)"
    # A completed/confirmed ticket purchase should produce at least one active EventAttendance.
    # Orphans = payment went through but no seat was actually reserved (user paid but no ticket).
    psql_query "
        SELECT tp.\"Id\" as purchase_id, tp.\"UserId\", tp.\"PaymentStatus\", tp.\"ProcessedAt\"::date as processed,
               tp.\"TotalPrice\", COUNT(ea.\"Id\") FILTER (WHERE ea.\"Status\" = 1) as active_attendances
        FROM public.\"TicketPurchases\" tp
        LEFT JOIN public.\"EventAttendances\" ea ON tp.\"Id\" = ea.\"TicketPurchaseId\"
        WHERE tp.\"PaymentStatus\" IN ('Completed', 'Confirmed', 'PartiallyRefunded')
            AND tp.\"ProcessedAt\" >= NOW() - INTERVAL '90 days'
        GROUP BY tp.\"Id\", tp.\"UserId\", tp.\"PaymentStatus\", tp.\"ProcessedAt\", tp.\"TotalPrice\"
        HAVING COUNT(ea.\"Id\") FILTER (WHERE ea.\"Status\" = 1) = 0
        ORDER BY tp.\"ProcessedAt\" DESC
        LIMIT 20;
    " >> "$OUTPUT_FILE"

    subsection "9.5 Active Attendance Without Completed Payment"
    # Inverse of 9.4: an active/registered attendance record with a Pending or Failed payment.
    # Indicates either a payment retry race, a manual override, or abandoned checkout not cleaned up.
    psql_query "
        SELECT ea.\"Id\" as attendance_id, ea.\"EventId\", ea.\"UserId\", ea.\"Status\",
               ea.\"TicketPurchaseId\", tp.\"PaymentStatus\", tp.\"ProcessedAt\"::date as purchase_date
        FROM public.\"EventAttendances\" ea
        INNER JOIN public.\"TicketPurchases\" tp ON ea.\"TicketPurchaseId\" = tp.\"Id\"
        WHERE ea.\"Status\" = 1
            AND tp.\"PaymentStatus\" IN ('Pending', 'Failed')
            AND ea.\"CreatedAt\" >= NOW() - INTERVAL '90 days'
        ORDER BY ea.\"CreatedAt\" DESC
        LIMIT 20;
    " >> "$OUTPUT_FILE"

    subsection "9.6 Completed Refunds With Stale Ticket Status"
    # RefundStatus: 0=Processing, 1=Completed, 2=Failed, 3=Cancelled
    # After a refund Completes, TicketPurchase.PaymentStatus should be 'Refunded' or 'PartiallyRefunded'.
    # If still 'Completed'/'Confirmed', the reconciliation step didn't fire.
    psql_query "
        SELECT pr.\"Id\" as refund_id, pr.\"TicketPurchaseId\", pr.\"RefundStatus\",
               pr.\"RefundAmountValue\", pr.\"ProcessedAt\"::date as refund_date,
               tp.\"PaymentStatus\" as purchase_status, tp.\"TotalPrice\"
        FROM public.\"PaymentRefunds\" pr
        INNER JOIN public.\"TicketPurchases\" tp ON pr.\"TicketPurchaseId\" = tp.\"Id\"
        WHERE pr.\"RefundStatus\" = 1
            AND tp.\"PaymentStatus\" NOT IN ('Refunded', 'PartiallyRefunded')
            AND pr.\"ProcessedAt\" >= NOW() - INTERVAL '180 days'
        ORDER BY pr.\"ProcessedAt\" DESC
        LIMIT 20;
    " >> "$OUTPUT_FILE"

    echo "  [9/9] Data integrity audits collected"
else
    log "Skipping data integrity audits (DB unavailable or --skip-db-logs)"
fi

# ============================================
# SUMMARY
# ============================================

section "DATA COLLECTION COMPLETE"

echo "" >> "$OUTPUT_FILE"
echo "Report saved to: $OUTPUT_FILE" >> "$OUTPUT_FILE"
echo "Generated: $(date -u +"%Y-%m-%d %H:%M:%S UTC")" >> "$OUTPUT_FILE"

echo ""
echo "========================================"
echo "  Data Collection Complete"
echo "========================================"
echo ""
echo "Report: $OUTPUT_FILE"
echo ""
echo "=== SKILL_RESULT ==="
cat <<EOF
{
  "skill": "check-production-server",
  "status": "success",
  "environment": "$ENVIRONMENT",
  "hours": $HOURS,
  "output_file": "$OUTPUT_FILE",
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "message": "Data collection complete. Report saved to $OUTPUT_FILE"
}
EOF
echo "=== END_SKILL_RESULT ==="
