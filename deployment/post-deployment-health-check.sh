#!/bin/bash

# WitchCityRope Post-Deployment Health Check Script
# This script performs comprehensive health checks after deployment
# Compatible with both Linux and Windows (WSL) environments

set -euo pipefail

# Script configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HEALTH_LOG="health-check-$(date +%Y%m%d-%H%M%S).log"
CHECKS_PASSED=0
CHECKS_FAILED=0
CHECKS_WARNING=0

# Default configuration
API_BASE_URL="${API_BASE_URL:-http://localhost:5653}"
WEB_BASE_URL="${WEB_BASE_URL:-http://localhost:5651}"
TIMEOUT="${TIMEOUT:-30}"
RETRY_COUNT="${RETRY_COUNT:-3}"
RETRY_DELAY="${RETRY_DELAY:-5}"

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

# Load configuration if available
if [[ -f "$SCRIPT_DIR/deployment-config.json" ]]; then
    if command -v jq &> /dev/null; then
        API_PORT=$(jq -r '.ApiPort // 5653' "$SCRIPT_DIR/deployment-config.json")
        WEB_PORT=$(jq -r '.WebPort // 5651' "$SCRIPT_DIR/deployment-config.json")
        API_BASE_URL="http://localhost:$API_PORT"
        WEB_BASE_URL="http://localhost:$WEB_PORT"
    fi
fi

# Logging functions
log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$HEALTH_LOG"
}

print_header() {
    echo -e "\n${BLUE}=== $1 ===${NC}" | tee -a "$HEALTH_LOG"
}

print_pass() {
    echo -e "${GREEN}✓${NC} $1" | tee -a "$HEALTH_LOG"
    ((CHECKS_PASSED++))
}

print_fail() {
    echo -e "${RED}✗${NC} $1" | tee -a "$HEALTH_LOG"
    ((CHECKS_FAILED++))
}

print_warn() {
    echo -e "${YELLOW}⚠${NC} $1" | tee -a "$HEALTH_LOG"
    ((CHECKS_WARNING++))
}

print_info() {
    echo -e "${CYAN}ℹ${NC} $1" | tee -a "$HEALTH_LOG"
}

# HTTP request with retry logic
http_request() {
    local url="$1"
    local method="${2:-GET}"
    local data="${3:-}"
    local expected_status="${4:-200}"
    local retry_count="$RETRY_COUNT"
    
    while [[ $retry_count -gt 0 ]]; do
        local response
        local status_code
        
        if [[ "$method" == "GET" ]]; then
            response=$(curl -s -w "\n%{http_code}" -X GET "$url" --max-time "$TIMEOUT" 2>/dev/null || echo "000")
        else
            response=$(curl -s -w "\n%{http_code}" -X "$method" -H "Content-Type: application/json" -d "$data" "$url" --max-time "$TIMEOUT" 2>/dev/null || echo "000")
        fi
        
        status_code=$(echo "$response" | tail -n1)
        
        if [[ "$status_code" == "$expected_status" ]]; then
            echo "$response" | head -n -1
            return 0
        elif [[ "$status_code" == "000" ]]; then
            ((retry_count--))
            if [[ $retry_count -gt 0 ]]; then
                sleep "$RETRY_DELAY"
            fi
        else
            echo "HTTP $status_code"
            return 1
        fi
    done
    
    echo "Connection timeout"
    return 1
}

# Check service availability
check_service_availability() {
    print_header "Service Availability"
    
    # Check API availability
    print_info "Checking API service at $API_BASE_URL"
    if response=$(http_request "$API_BASE_URL/health"); then
        print_pass "API service is accessible"
        
        # Parse health response if JSON
        if echo "$response" | jq -e '.status' &> /dev/null; then
            local status=$(echo "$response" | jq -r '.status')
            if [[ "$status" == "Healthy" ]]; then
                print_pass "API health status: Healthy"
            else
                print_warn "API health status: $status"
            fi
        fi
    else
        print_fail "API service is not accessible: $response"
    fi
    
    # Check Web availability
    print_info "Checking Web service at $WEB_BASE_URL"
    if response=$(http_request "$WEB_BASE_URL" "GET" "" "200"); then
        print_pass "Web service is accessible"
    else
        print_fail "Web service is not accessible: $response"
    fi
    
    # Check Swagger/API documentation
    if response=$(http_request "$API_BASE_URL/swagger" "GET" "" "301\|302\|200"); then
        print_pass "API documentation (Swagger) is accessible"
    else
        print_warn "API documentation not accessible: $response"
    fi
}

# Check API endpoints
check_api_endpoints() {
    print_header "API Endpoints"
    
    local endpoints=(
        "/api/auth/login:POST:{\"email\":\"test@example.com\",\"password\":\"Test123!\"}:400"
        "/api/events:GET::200"
        "/api/events/upcoming:GET::200"
    )
    
    for endpoint_config in "${endpoints[@]}"; do
        IFS=':' read -r endpoint method data expected_status <<< "$endpoint_config"
        
        print_info "Testing $method $endpoint"
        if response=$(http_request "$API_BASE_URL$endpoint" "$method" "$data" "$expected_status"); then
            print_pass "$endpoint responded correctly"
        else
            print_fail "$endpoint failed: $response"
        fi
    done
}

# Check database connectivity
check_database() {
    print_header "Database Connectivity"
    
    # Check if database file exists (for SQLite)
    local db_paths=(
        "/opt/witchcityrope/data/witchcityrope.db"
        "./data/witchcityrope.db"
        "../data/witchcityrope.db"
    )
    
    local db_found=false
    for db_path in "${db_paths[@]}"; do
        if [[ -f "$db_path" ]]; then
            db_found=true
            local db_size=$(du -h "$db_path" | cut -f1)
            print_pass "Database file found: $db_path (Size: $db_size)"
            
            # Check if database is locked
            if lsof "$db_path" &> /dev/null || fuser "$db_path" &> /dev/null; then
                print_pass "Database is in use (good - application connected)"
            else
                print_warn "Database is not in use (application may not be connected)"
            fi
            break
        fi
    done
    
    if [[ "$db_found" == "false" ]]; then
        print_warn "SQLite database file not found in expected locations"
    fi
    
    # Test database through API
    if response=$(http_request "$API_BASE_URL/api/health/db" "GET" "" "200\|404"); then
        if [[ "$response" != *"404"* ]]; then
            print_pass "Database connectivity verified through API"
        else
            print_info "Database health endpoint not implemented"
        fi
    fi
}

# Check external services
check_external_services() {
    print_header "External Services"
    
    # Check SendGrid (email service)
    print_info "Checking email service configuration..."
    if response=$(http_request "$API_BASE_URL/api/health/email" "GET" "" "200\|404"); then
        if [[ "$response" != *"404"* ]]; then
            print_pass "Email service configured"
        else
            print_info "Email health endpoint not implemented"
        fi
    fi
    
    # Check PayPal integration
    print_info "Checking payment service configuration..."
    if response=$(http_request "$API_BASE_URL/api/health/payment" "GET" "" "200\|404"); then
        if [[ "$response" != *"404"* ]]; then
            print_pass "Payment service configured"
        else
            print_info "Payment health endpoint not implemented"
        fi
    fi
}

# Check Docker containers (if applicable)
check_docker_containers() {
    if ! command -v docker &> /dev/null; then
        return
    fi
    
    print_header "Docker Container Health"
    
    local containers=(
        "witchcity-api"
        "witchcity-web"
        "witchcity-nginx"
        "witchcity-redis"
    )
    
    for container in "${containers[@]}"; do
        if docker ps --format "table {{.Names}}" | grep -q "^$container$"; then
            local status=$(docker inspect -f '{{.State.Status}}' "$container" 2>/dev/null)
            local health=$(docker inspect -f '{{.State.Health.Status}}' "$container" 2>/dev/null || echo "none")
            
            if [[ "$status" == "running" ]]; then
                if [[ "$health" == "healthy" ]] || [[ "$health" == "none" ]]; then
                    print_pass "$container: running$([ "$health" != "none" ] && echo " (health: $health)")"
                else
                    print_warn "$container: running (health: $health)"
                fi
            else
                print_fail "$container: $status"
            fi
        else
            print_info "$container: not deployed"
        fi
    done
    
    # Check Docker resource usage
    print_info "Docker resource usage:"
    docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}" | grep witchcity || true
}

# Check logs for errors
check_logs() {
    print_header "Application Logs"
    
    local log_paths=(
        "/opt/witchcityrope/logs"
        "./logs"
        "../logs"
    )
    
    local logs_found=false
    for log_path in "${log_paths[@]}"; do
        if [[ -d "$log_path" ]]; then
            logs_found=true
            print_info "Checking logs in: $log_path"
            
            # Check for recent errors in API logs
            local api_errors=$(find "$log_path" -name "*api*.log" -type f -mmin -5 -exec grep -i "error\|exception\|fatal" {} \; 2>/dev/null | wc -l)
            if [[ "$api_errors" -eq 0 ]]; then
                print_pass "No recent errors in API logs"
            else
                print_warn "Found $api_errors error(s) in recent API logs"
            fi
            
            # Check for recent errors in Web logs
            local web_errors=$(find "$log_path" -name "*web*.log" -type f -mmin -5 -exec grep -i "error\|exception\|fatal" {} \; 2>/dev/null | wc -l)
            if [[ "$web_errors" -eq 0 ]]; then
                print_pass "No recent errors in Web logs"
            else
                print_warn "Found $web_errors error(s) in recent Web logs"
            fi
            
            break
        fi
    done
    
    if [[ "$logs_found" == "false" ]]; then
        print_warn "Log directory not found in expected locations"
    fi
}

# Check SSL/TLS configuration
check_ssl() {
    print_header "SSL/TLS Configuration"
    
    # Check HTTPS endpoints
    local https_endpoints=(
        "https://localhost:5654"
        "https://localhost:5652"
    )
    
    for endpoint in "${https_endpoints[@]}"; do
        print_info "Checking SSL at $endpoint"
        if curl -k -s -o /dev/null -w "%{http_code}" "$endpoint" --max-time 5 | grep -q "200\|301\|302"; then
            print_pass "HTTPS endpoint accessible: $endpoint"
            
            # Check certificate details
            if cert_info=$(echo | openssl s_client -connect "${endpoint#https://}" -servername localhost 2>/dev/null | openssl x509 -noout -dates 2>/dev/null); then
                print_info "Certificate info: $cert_info"
            fi
        else
            print_info "HTTPS not configured for $endpoint"
        fi
    done
}

# Performance checks
check_performance() {
    print_header "Performance Metrics"
    
    # Check response times
    print_info "Measuring response times..."
    
    # API response time
    local api_time=$(curl -o /dev/null -s -w "%{time_total}" "$API_BASE_URL/api/events" 2>/dev/null || echo "N/A")
    if [[ "$api_time" != "N/A" ]]; then
        if (( $(echo "$api_time < 1.0" | bc -l) )); then
            print_pass "API response time: ${api_time}s"
        else
            print_warn "API response time: ${api_time}s (>1s)"
        fi
    fi
    
    # Web response time
    local web_time=$(curl -o /dev/null -s -w "%{time_total}" "$WEB_BASE_URL" 2>/dev/null || echo "N/A")
    if [[ "$web_time" != "N/A" ]]; then
        if (( $(echo "$web_time < 2.0" | bc -l) )); then
            print_pass "Web response time: ${web_time}s"
        else
            print_warn "Web response time: ${web_time}s (>2s)"
        fi
    fi
    
    # Check system resources
    print_info "System resource usage:"
    echo "CPU Load: $(uptime | awk -F'load average:' '{print $2}')"
    echo "Memory: $(free -h | awk '/^Mem:/ {print $3 " / " $2 " (" int($3/$2 * 100) "%)"}')"
    echo "Disk: $(df -h . | awk 'NR==2 {print $3 " / " $2 " (" $5 ")"}')"
}

# Security checks
check_security() {
    print_header "Security Configuration"
    
    # Check if debug mode is disabled
    if response=$(curl -s "$API_BASE_URL" 2>/dev/null); then
        if echo "$response" | grep -qi "debug\|stack.*trace\|exception.*details"; then
            print_fail "Debug information exposed in responses"
        else
            print_pass "No debug information exposed"
        fi
    fi
    
    # Check security headers
    print_info "Checking security headers..."
    local headers=$(curl -s -I "$WEB_BASE_URL" 2>/dev/null)
    
    local security_headers=(
        "X-Content-Type-Options: nosniff"
        "X-Frame-Options:"
        "X-XSS-Protection:"
        "Strict-Transport-Security:"
    )
    
    for header in "${security_headers[@]}"; do
        if echo "$headers" | grep -qi "$header"; then
            print_pass "Security header present: $header"
        else
            print_warn "Security header missing: $header"
        fi
    done
    
    # Check for default credentials
    local default_login='{"email":"admin@example.com","password":"admin"}'
    if response=$(http_request "$API_BASE_URL/api/auth/login" "POST" "$default_login" "400\|401"); then
        print_pass "Default credentials rejected"
    else
        print_fail "Default credentials might be active"
    fi
}

# Generate health report
generate_report() {
    print_header "Health Check Summary"
    
    local total_checks=$((CHECKS_PASSED + CHECKS_FAILED + CHECKS_WARNING))
    local health_score=$((CHECKS_PASSED * 100 / total_checks))
    
    echo -e "\nHealth Score: ${health_score}%"
    echo -e "${GREEN}Passed:${NC} $CHECKS_PASSED"
    echo -e "${YELLOW}Warnings:${NC} $CHECKS_WARNING"
    echo -e "${RED}Failed:${NC} $CHECKS_FAILED"
    
    # Determine overall status
    if [[ "$CHECKS_FAILED" -eq 0 ]]; then
        if [[ "$CHECKS_WARNING" -eq 0 ]]; then
            echo -e "\n${GREEN}✓ Deployment is fully healthy!${NC}"
            local exit_code=0
        else
            echo -e "\n${YELLOW}⚠ Deployment is operational with warnings${NC}"
            local exit_code=0
        fi
    else
        echo -e "\n${RED}✗ Deployment has critical issues${NC}"
        local exit_code=1
    fi
    
    # Generate recommendations
    if [[ "$CHECKS_FAILED" -gt 0 ]] || [[ "$CHECKS_WARNING" -gt 0 ]]; then
        echo -e "\n${BLUE}Recommendations:${NC}"
        
        if grep -q "not accessible" "$HEALTH_LOG"; then
            echo "- Check if all services are running"
            echo "- Verify firewall rules and port configurations"
        fi
        
        if grep -q "error.*in.*logs" "$HEALTH_LOG"; then
            echo "- Review application logs for error details"
            echo "- Check log rotation settings"
        fi
        
        if grep -q "Security header missing" "$HEALTH_LOG"; then
            echo "- Configure security headers in web server"
            echo "- Review security best practices"
        fi
        
        if grep -q "response time.*>.*s" "$HEALTH_LOG"; then
            echo "- Investigate performance bottlenecks"
            echo "- Consider scaling resources"
        fi
    fi
    
    echo -e "\nDetailed results saved to: $HEALTH_LOG"
    
    # Create JSON report
    cat > "health-report-$(date +%Y%m%d-%H%M%S).json" << EOF
{
    "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
    "health_score": $health_score,
    "checks": {
        "passed": $CHECKS_PASSED,
        "warning": $CHECKS_WARNING,
        "failed": $CHECKS_FAILED,
        "total": $total_checks
    },
    "status": "$([ $exit_code -eq 0 ] && echo "healthy" || echo "unhealthy")",
    "api_url": "$API_BASE_URL",
    "web_url": "$WEB_BASE_URL"
}
EOF
    
    return $exit_code
}

# Usage function
usage() {
    cat << EOF
Usage: $0 [OPTIONS]

Perform health checks on WitchCityRope deployment

OPTIONS:
    -a, --api-url URL       API base URL (default: http://localhost:5653)
    -w, --web-url URL       Web base URL (default: http://localhost:5651)
    -t, --timeout SECONDS   Request timeout in seconds (default: 30)
    -r, --retries COUNT     Number of retries for failed requests (default: 3)
    -d, --delay SECONDS     Delay between retries (default: 5)
    -h, --help             Show this help message

EXAMPLES:
    $0
    $0 --api-url https://api.witchcityrope.com --web-url https://witchcityrope.com
    $0 -t 60 -r 5

The script performs comprehensive health checks including:
- Service availability
- API endpoint functionality
- Database connectivity
- External service integration
- Container health (if using Docker)
- Log analysis
- SSL/TLS configuration
- Performance metrics
- Security configuration
EOF
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -a|--api-url)
            API_BASE_URL="$2"
            shift 2
            ;;
        -w|--web-url)
            WEB_BASE_URL="$2"
            shift 2
            ;;
        -t|--timeout)
            TIMEOUT="$2"
            shift 2
            ;;
        -r|--retries)
            RETRY_COUNT="$2"
            shift 2
            ;;
        -d|--delay)
            RETRY_DELAY="$2"
            shift 2
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            usage
            exit 1
            ;;
    esac
done

# Main execution
main() {
    echo -e "${BLUE}WitchCityRope Post-Deployment Health Check${NC}"
    echo "==========================================="
    echo "Timestamp: $(date)"
    echo "API URL: $API_BASE_URL"
    echo "Web URL: $WEB_BASE_URL"
    echo ""
    
    # Run all health checks
    check_service_availability
    check_api_endpoints
    check_database
    check_external_services
    check_docker_containers
    check_logs
    check_ssl
    check_performance
    check_security
    
    # Generate report and exit
    generate_report
}

# Run main function
main