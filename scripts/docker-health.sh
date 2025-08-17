#!/bin/bash
# WitchCityRope - Docker Health Checker
# Description: Comprehensive health check for all services

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Helper functions
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[✅ PASS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[⚠️  WARN]${NC} $1"
}

print_error() {
    echo -e "${RED}[❌ FAIL]${NC} $1"
}

print_test() {
    echo -e "${CYAN}[🔍 TEST]${NC} $1"
}

show_help() {
    echo "WitchCityRope Docker Health Checker"
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -h, --help        Show this help message"
    echo "  -v, --verbose     Show detailed output"
    echo "  -q, --quiet       Show only pass/fail status"
    echo "  -f, --fix         Attempt to fix common issues"
    echo "  --api-only        Test API health only"
    echo "  --db-only         Test database health only"
    echo "  --web-only        Test React app health only"
    echo "  --auth-only       Test authentication flow only"
    echo "  --quick           Quick health check (basic connectivity)"
    echo "  --full            Full comprehensive health check"
    echo ""
    echo "Examples:"
    echo "  $0                # Standard health check"
    echo "  $0 --verbose      # Detailed health check"
    echo "  $0 --api-only     # Test API only"
    echo "  $0 --quick        # Quick connectivity check"
    echo "  $0 --full --fix   # Full check with auto-fix"
}

check_docker() {
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed or not in PATH"
        return 1
    fi

    if ! docker info &> /dev/null; then
        print_error "Docker daemon is not running"
        return 1
    fi

    if ! command -v docker-compose &> /dev/null; then
        print_error "Docker Compose is not installed or not in PATH"
        return 1
    fi
    
    return 0
}

test_container_status() {
    print_test "Checking container status..."
    
    local containers=$(docker-compose ps -q 2>/dev/null)
    if [ -z "$containers" ]; then
        print_error "No containers are running"
        if [ "$attempt_fix" = true ]; then
            print_status "Attempting to start containers..."
            ./scripts/docker-dev.sh
            sleep 10
            containers=$(docker-compose ps -q 2>/dev/null)
        fi
        
        if [ -z "$containers" ]; then
            return 1
        fi
    fi
    
    # Check each service
    local all_healthy=true
    
    # Check web container
    if docker-compose ps web | grep -q "Up"; then
        print_success "React app container is running"
    else
        print_error "React app container is not running"
        all_healthy=false
    fi
    
    # Check API container
    if docker-compose ps api | grep -q "Up"; then
        print_success "API container is running"
    else
        print_error "API container is not running"
        all_healthy=false
    fi
    
    # Check postgres container
    if docker-compose ps postgres | grep -q "Up"; then
        print_success "PostgreSQL container is running"
    else
        print_error "PostgreSQL container is not running"
        all_healthy=false
    fi
    
    if [ "$verbose" = true ]; then
        echo ""
        print_status "Container details:"
        docker-compose ps
    fi
    
    [ "$all_healthy" = true ]
}

test_database_connectivity() {
    print_test "Testing database connectivity..."
    
    # Test from host
    if docker-compose exec -T postgres pg_isready -U postgres -d witchcityrope_dev &> /dev/null; then
        print_success "Database is accepting connections"
    else
        print_error "Database is not accepting connections"
        return 1
    fi
    
    # Test basic query
    if docker-compose exec -T postgres psql -U postgres -d witchcityrope_dev -c "SELECT 1;" &> /dev/null; then
        print_success "Database queries are working"
    else
        print_error "Database queries are failing"
        return 1
    fi
    
    # Check if tables exist
    local table_count=$(docker-compose exec -T postgres psql -U postgres -d witchcityrope_dev -t -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';" 2>/dev/null | tr -d ' \n')
    
    if [ "$table_count" -gt 0 ]; then
        print_success "Database has $table_count tables"
    else
        print_warning "Database has no tables (migrations may need to be run)"
        if [ "$attempt_fix" = true ]; then
            print_status "Attempting to run migrations..."
            docker-compose exec -T api dotnet ef database update
        fi
    fi
    
    if [ "$verbose" = true ]; then
        echo ""
        print_status "Database version:"
        docker-compose exec -T postgres psql -U postgres -d witchcityrope_dev -c "SELECT version();" 2>/dev/null || echo "  (Unable to get version)"
    fi
    
    return 0
}

test_api_health() {
    print_test "Testing API health..."
    
    # Basic health check
    local health_response=$(curl -s -w "%{http_code}" -o /dev/null http://localhost:5655/health 2>/dev/null)
    
    if [ "$health_response" = "200" ]; then
        print_success "API health endpoint responding (HTTP 200)"
    else
        print_error "API health endpoint failed (HTTP $health_response)"
        return 1
    fi
    
    # Check API logs for errors
    local error_count=$(docker-compose logs api --tail=50 | grep -i error | wc -l)
    if [ "$error_count" -eq 0 ]; then
        print_success "No recent errors in API logs"
    else
        print_warning "Found $error_count recent errors in API logs"
        if [ "$verbose" = true ]; then
            echo ""
            print_status "Recent API errors:"
            docker-compose logs api --tail=10 | grep -i error || echo "  (No errors found)"
        fi
    fi
    
    # Test database connectivity from API
    if docker-compose exec -T api pg_isready -h postgres -p 5432 -U postgres -d witchcityrope_dev &> /dev/null; then
        print_success "API can connect to database"
    else
        print_error "API cannot connect to database"
        return 1
    fi
    
    if [ "$verbose" = true ]; then
        echo ""
        print_status "API environment:"
        docker-compose exec -T api env | grep -E "(ASPNETCORE|ConnectionStrings)" | head -3
    fi
    
    return 0
}

test_authentication_endpoints() {
    print_test "Testing authentication endpoints..."
    
    # Test auth health endpoint
    local auth_health=$(curl -s -w "%{http_code}" -o /dev/null http://localhost:5655/api/auth/health 2>/dev/null)
    
    if [ "$auth_health" = "200" ]; then
        print_success "Authentication health endpoint responding"
    else
        print_error "Authentication health endpoint failed (HTTP $auth_health)"
        return 1
    fi
    
    # Test registration endpoint (just connectivity, not actual registration)
    local reg_response=$(curl -s -w "%{http_code}" -o /dev/null -X POST http://localhost:5655/api/auth/register \
        -H "Content-Type: application/json" \
        -d '{}' 2>/dev/null)
    
    # We expect a 400 (Bad Request) because we're not sending valid data
    if [ "$reg_response" = "400" ]; then
        print_success "Registration endpoint is accessible"
    else
        print_warning "Registration endpoint returned HTTP $reg_response (expected 400)"
    fi
    
    # Test login endpoint (just connectivity)
    local login_response=$(curl -s -w "%{http_code}" -o /dev/null -X POST http://localhost:5655/api/auth/login \
        -H "Content-Type: application/json" \
        -d '{}' 2>/dev/null)
    
    # We expect a 400 (Bad Request) because we're not sending valid data
    if [ "$login_response" = "400" ]; then
        print_success "Login endpoint is accessible"
    else
        print_warning "Login endpoint returned HTTP $login_response (expected 400)"
    fi
    
    return 0
}

test_react_app() {
    print_test "Testing React app..."
    
    # Test React app responds
    local react_response=$(curl -s -w "%{http_code}" -o /dev/null http://localhost:5173 2>/dev/null)
    
    if [ "$react_response" = "200" ]; then
        print_success "React app is responding (HTTP 200)"
    else
        print_error "React app failed (HTTP $react_response)"
        return 1
    fi
    
    # Check for build errors in logs
    local build_errors=$(docker-compose logs web --tail=20 | grep -i -E "(error|failed|fatal)" | wc -l)
    if [ "$build_errors" -eq 0 ]; then
        print_success "No build errors in React app"
    else
        print_warning "Found $build_errors potential build issues"
        if [ "$verbose" = true ]; then
            echo ""
            print_status "Recent React app issues:"
            docker-compose logs web --tail=5 | grep -i -E "(error|failed|fatal)" || echo "  (No errors found)"
        fi
    fi
    
    # Test if React can reach API
    if docker-compose exec -T web curl -s -f http://api:8080/health &> /dev/null; then
        print_success "React app can reach API"
    else
        print_warning "React app cannot reach API (container-to-container)"
    fi
    
    return 0
}

test_service_communication() {
    print_test "Testing service-to-service communication..."
    
    # Test React -> API communication
    if curl -s -H "Origin: http://localhost:5173" http://localhost:5655/health &> /dev/null; then
        print_success "React -> API communication working"
    else
        print_error "React -> API communication failed"
        return 1
    fi
    
    # Test API -> Database communication
    if docker-compose exec -T api pg_isready -h postgres -p 5432 -U postgres &> /dev/null; then
        print_success "API -> Database communication working"
    else
        print_error "API -> Database communication failed"
        return 1
    fi
    
    # Test CORS
    local cors_response=$(curl -s -I -H "Origin: http://localhost:5173" \
        -H "Access-Control-Request-Method: POST" \
        -H "Access-Control-Request-Headers: Content-Type" \
        -X OPTIONS \
        http://localhost:5655/api/auth/login 2>/dev/null | grep -i "access-control-allow-origin")
    
    if [ -n "$cors_response" ]; then
        print_success "CORS configuration is working"
    else
        print_warning "CORS may not be configured correctly"
    fi
    
    return 0
}

test_performance() {
    print_test "Testing performance..."
    
    # Test API response time
    local api_time=$(curl -w "%{time_total}" -s -o /dev/null http://localhost:5655/health 2>/dev/null)
    local api_time_ms=$(echo "$api_time * 1000" | bc 2>/dev/null || echo "unknown")
    
    if (( $(echo "$api_time < 1.0" | bc -l 2>/dev/null) )); then
        print_success "API response time: ${api_time_ms}ms"
    else
        print_warning "API response time slow: ${api_time_ms}ms"
    fi
    
    # Test React app response time
    local react_time=$(curl -w "%{time_total}" -s -o /dev/null http://localhost:5173 2>/dev/null)
    local react_time_ms=$(echo "$react_time * 1000" | bc 2>/dev/null || echo "unknown")
    
    if (( $(echo "$react_time < 2.0" | bc -l 2>/dev/null) )); then
        print_success "React app response time: ${react_time_ms}ms"
    else
        print_warning "React app response time slow: ${react_time_ms}ms"
    fi
    
    # Check container resource usage
    if [ "$verbose" = true ]; then
        echo ""
        print_status "Container resource usage:"
        docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}" $(docker-compose ps -q) 2>/dev/null || echo "  (Unable to get stats)"
    fi
    
    return 0
}

run_health_checks() {
    local test_count=0
    local pass_count=0
    local fail_count=0
    
    echo ""
    print_status "🏥 Starting health checks..."
    echo ""
    
    # Container status check
    if [ "$test_containers" = true ]; then
        test_count=$((test_count + 1))
        if test_container_status; then
            pass_count=$((pass_count + 1))
        else
            fail_count=$((fail_count + 1))
        fi
        echo ""
    fi
    
    # Database checks
    if [ "$test_database" = true ]; then
        test_count=$((test_count + 1))
        if test_database_connectivity; then
            pass_count=$((pass_count + 1))
        else
            fail_count=$((fail_count + 1))
        fi
        echo ""
    fi
    
    # API checks
    if [ "$test_api" = true ]; then
        test_count=$((test_count + 1))
        if test_api_health; then
            pass_count=$((pass_count + 1))
        else
            fail_count=$((fail_count + 1))
        fi
        echo ""
    fi
    
    # Authentication checks
    if [ "$test_auth" = true ]; then
        test_count=$((test_count + 1))
        if test_authentication_endpoints; then
            pass_count=$((pass_count + 1))
        else
            fail_count=$((fail_count + 1))
        fi
        echo ""
    fi
    
    # React app checks
    if [ "$test_web" = true ]; then
        test_count=$((test_count + 1))
        if test_react_app; then
            pass_count=$((pass_count + 1))
        else
            fail_count=$((fail_count + 1))
        fi
        echo ""
    fi
    
    # Service communication checks
    if [ "$test_communication" = true ]; then
        test_count=$((test_count + 1))
        if test_service_communication; then
            pass_count=$((pass_count + 1))
        else
            fail_count=$((fail_count + 1))
        fi
        echo ""
    fi
    
    # Performance checks
    if [ "$test_performance" = true ]; then
        test_count=$((test_count + 1))
        if test_performance; then
            pass_count=$((pass_count + 1))
        else
            fail_count=$((fail_count + 1))
        fi
        echo ""
    fi
    
    # Summary
    echo "=========================================="
    print_status "Health Check Summary:"
    echo "  Total Tests: $test_count"
    print_success "  Passed: $pass_count"
    if [ $fail_count -gt 0 ]; then
        print_error "  Failed: $fail_count"
    else
        print_success "  Failed: $fail_count"
    fi
    echo ""
    
    if [ $fail_count -eq 0 ]; then
        print_success "🎉 All health checks passed!"
        return 0
    else
        print_error "❌ Some health checks failed"
        echo ""
        print_status "Troubleshooting suggestions:"
        echo "  1. Check logs: ./scripts/docker-logs.sh --errors"
        echo "  2. Restart services: ./scripts/docker-stop.sh && ./scripts/docker-dev.sh"
        echo "  3. Clean rebuild: ./scripts/docker-clean.sh --containers && ./scripts/docker-dev.sh --build"
        return 1
    fi
}

# Parse command line arguments
verbose=false
quiet=false
attempt_fix=false
test_containers=true
test_database=true
test_api=true
test_auth=true
test_web=true
test_communication=true
test_performance=true

while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            show_help
            exit 0
            ;;
        -v|--verbose)
            verbose=true
            shift
            ;;
        -q|--quiet)
            quiet=true
            shift
            ;;
        -f|--fix)
            attempt_fix=true
            shift
            ;;
        --api-only)
            test_containers=false
            test_database=false
            test_api=true
            test_auth=false
            test_web=false
            test_communication=false
            test_performance=false
            shift
            ;;
        --db-only)
            test_containers=false
            test_database=true
            test_api=false
            test_auth=false
            test_web=false
            test_communication=false
            test_performance=false
            shift
            ;;
        --web-only)
            test_containers=false
            test_database=false
            test_api=false
            test_auth=false
            test_web=true
            test_communication=false
            test_performance=false
            shift
            ;;
        --auth-only)
            test_containers=false
            test_database=false
            test_api=false
            test_auth=true
            test_web=false
            test_communication=false
            test_performance=false
            shift
            ;;
        --quick)
            test_containers=true
            test_database=true
            test_api=true
            test_auth=false
            test_web=true
            test_communication=false
            test_performance=false
            shift
            ;;
        --full)
            # All tests enabled by default
            shift
            ;;
        *)
            print_error "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# Main execution
if [ "$quiet" = false ]; then
    print_status "WitchCityRope Docker Health Checker"
fi

# Pre-flight checks
if ! check_docker; then
    exit 1
fi

# Change to project root
cd "$(dirname "$0")/.."

# Run health checks
if run_health_checks; then
    exit 0
else
    exit 1
fi