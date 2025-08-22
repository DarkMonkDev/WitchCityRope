#!/bin/bash

# WitchCityRope Pre-Deployment Validation Script
# This script validates the environment and configuration before deployment
# Compatible with both Linux and WSL environments

set -euo pipefail

# Script configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VALIDATION_LOG="pre-deployment-validation-$(date +%Y%m%d-%H%M%S).log"
ERRORS_FOUND=0
WARNINGS_FOUND=0

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Icons
PASS="✓"
FAIL="✗"
WARN="⚠"
INFO="ℹ"

# Logging functions
log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$VALIDATION_LOG"
}

print_header() {
    echo -e "\n${BLUE}=== $1 ===${NC}" | tee -a "$VALIDATION_LOG"
}

print_pass() {
    echo -e "${GREEN}${PASS}${NC} $1" | tee -a "$VALIDATION_LOG"
}

print_fail() {
    echo -e "${RED}${FAIL}${NC} $1" | tee -a "$VALIDATION_LOG"
    ((ERRORS_FOUND++))
}

print_warn() {
    echo -e "${YELLOW}${WARN}${NC} $1" | tee -a "$VALIDATION_LOG"
    ((WARNINGS_FOUND++))
}

print_info() {
    echo -e "${BLUE}${INFO}${NC} $1" | tee -a "$VALIDATION_LOG"
}

# Validation functions
check_os() {
    print_header "Operating System Check"
    
    if [[ -f /etc/os-release ]]; then
        . /etc/os-release
        print_info "OS: $NAME $VERSION"
        
        case "$ID" in
            ubuntu|debian)
                if [[ "${VERSION_ID%%.*}" -ge 20 ]] || [[ "${VERSION_ID%%.*}" -ge 11 ]]; then
                    print_pass "Supported Linux distribution"
                else
                    print_warn "Older OS version detected, recommend upgrade"
                fi
                ;;
            *)
                print_warn "Non-standard Linux distribution: $ID"
                ;;
        esac
    elif [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "cygwin" ]]; then
        print_pass "Windows environment detected"
    else
        print_warn "Unknown operating system: $OSTYPE"
    fi
    
    # Check if running in WSL
    if grep -qi microsoft /proc/version 2>/dev/null; then
        print_info "Running in WSL environment"
    fi
}

check_hardware() {
    print_header "Hardware Requirements"
    
    # CPU cores
    local cpu_cores=$(nproc 2>/dev/null || sysctl -n hw.ncpu 2>/dev/null || echo "unknown")
    if [[ "$cpu_cores" != "unknown" ]]; then
        if [[ "$cpu_cores" -ge 2 ]]; then
            print_pass "CPU cores: $cpu_cores"
        else
            print_fail "Insufficient CPU cores: $cpu_cores (minimum 2 required)"
        fi
    else
        print_warn "Unable to determine CPU count"
    fi
    
    # Memory
    local total_mem=$(free -m 2>/dev/null | awk '/^Mem:/{print $2}' || echo "unknown")
    if [[ "$total_mem" != "unknown" ]]; then
        if [[ "$total_mem" -ge 4096 ]]; then
            print_pass "Memory: ${total_mem}MB"
        elif [[ "$total_mem" -ge 2048 ]]; then
            print_warn "Low memory: ${total_mem}MB (4096MB recommended)"
        else
            print_fail "Insufficient memory: ${total_mem}MB (minimum 2048MB required)"
        fi
    else
        print_warn "Unable to determine memory size"
    fi
    
    # Disk space
    local disk_space=$(df -BG . | awk 'NR==2 {print $4}' | sed 's/G//' || echo "unknown")
    if [[ "$disk_space" != "unknown" ]]; then
        if [[ "$disk_space" -ge 20 ]]; then
            print_pass "Available disk space: ${disk_space}GB"
        elif [[ "$disk_space" -ge 10 ]]; then
            print_warn "Low disk space: ${disk_space}GB (20GB recommended)"
        else
            print_fail "Insufficient disk space: ${disk_space}GB (minimum 10GB required)"
        fi
    else
        print_warn "Unable to determine disk space"
    fi
}

check_prerequisites() {
    print_header "Software Prerequisites"
    
    # Define required and optional tools
    local required_tools=(
        "git:Git version control"
        "curl:HTTP client"
        "jq:JSON processor"
    )
    
    local optional_tools=(
        "docker:Docker container runtime"
        "docker-compose:Docker Compose"
        "nginx:Nginx web server"
        "dotnet:.NET runtime"
        "certbot:Let's Encrypt client"
    )
    
    # Check required tools
    print_info "Checking required tools..."
    for tool_desc in "${required_tools[@]}"; do
        IFS=':' read -r tool description <<< "$tool_desc"
        if command -v "$tool" &> /dev/null; then
            local version=$($tool --version 2>&1 | head -1)
            print_pass "$description: $version"
        else
            print_fail "$description: NOT FOUND"
        fi
    done
    
    # Check optional tools
    print_info "Checking optional tools..."
    for tool_desc in "${optional_tools[@]}"; do
        IFS=':' read -r tool description <<< "$tool_desc"
        if command -v "$tool" &> /dev/null; then
            local version=$($tool --version 2>&1 | head -1)
            print_pass "$description: $version"
        else
            print_warn "$description: NOT FOUND (may be required depending on deployment method)"
        fi
    done
    
    # Check .NET version if available
    if command -v dotnet &> /dev/null; then
        local dotnet_version=$(dotnet --version)
        if [[ "$dotnet_version" =~ ^8\. ]]; then
            print_pass ".NET SDK version: $dotnet_version"
        else
            print_warn ".NET SDK version: $dotnet_version (version 8.x recommended)"
        fi
    fi
}

check_network() {
    print_header "Network Configuration"
    
    # Check internet connectivity
    print_info "Checking internet connectivity..."
    if ping -c 1 -W 2 8.8.8.8 &> /dev/null; then
        print_pass "Internet connectivity verified"
    else
        print_fail "No internet connectivity detected"
    fi
    
    # Check DNS resolution
    if host github.com &> /dev/null || nslookup github.com &> /dev/null; then
        print_pass "DNS resolution working"
    else
        print_fail "DNS resolution not working"
    fi
    
    # Check required ports
    print_info "Checking port availability..."
    local ports=("80:HTTP" "443:HTTPS" "5651:Web App" "5653:API")
    
    for port_desc in "${ports[@]}"; do
        IFS=':' read -r port description <<< "$port_desc"
        if ! netstat -tuln 2>/dev/null | grep -q ":$port " && ! ss -tuln 2>/dev/null | grep -q ":$port "; then
            print_pass "Port $port ($description) is available"
        else
            print_warn "Port $port ($description) is already in use"
        fi
    done
}

check_docker() {
    print_header "Docker Environment"
    
    if ! command -v docker &> /dev/null; then
        print_warn "Docker not installed, skipping Docker checks"
        return
    fi
    
    # Check Docker daemon
    if docker info &> /dev/null; then
        print_pass "Docker daemon is running"
        
        # Check Docker version
        local docker_version=$(docker version --format '{{.Server.Version}}' 2>/dev/null)
        print_info "Docker version: $docker_version"
        
        # Check Docker Compose
        if command -v docker-compose &> /dev/null; then
            local compose_version=$(docker-compose version --short 2>/dev/null)
            print_pass "Docker Compose version: $compose_version"
        elif docker compose version &> /dev/null; then
            local compose_version=$(docker compose version --short 2>/dev/null)
            print_pass "Docker Compose (plugin) version: $compose_version"
        else
            print_fail "Docker Compose not found"
        fi
        
        # Check disk space for Docker
        local docker_root=$(docker info --format '{{.DockerRootDir}}' 2>/dev/null)
        if [[ -n "$docker_root" ]]; then
            local docker_space=$(df -BG "$docker_root" | awk 'NR==2 {print $4}' | sed 's/G//')
            if [[ "$docker_space" -ge 10 ]]; then
                print_pass "Docker storage space: ${docker_space}GB available"
            else
                print_warn "Low Docker storage space: ${docker_space}GB"
            fi
        fi
    else
        print_fail "Docker daemon is not running"
    fi
}

check_configuration() {
    print_header "Configuration Files"
    
    # Check for deployment configuration
    if [[ -f "$SCRIPT_DIR/deployment-config.json" ]]; then
        print_pass "deployment-config.json found"
        
        # Validate JSON
        if jq empty "$SCRIPT_DIR/deployment-config.json" 2>/dev/null; then
            print_pass "deployment-config.json is valid JSON"
            
            # Check required fields
            local required_fields=("AppName" "ApiPort" "WebPort" "SqliteDbPath")
            for field in "${required_fields[@]}"; do
                if jq -e ".$field" "$SCRIPT_DIR/deployment-config.json" &> /dev/null; then
                    local value=$(jq -r ".$field" "$SCRIPT_DIR/deployment-config.json")
                    print_pass "Config field '$field': $value"
                else
                    print_fail "Missing required config field: $field"
                fi
            done
        else
            print_fail "deployment-config.json is not valid JSON"
        fi
    else
        print_fail "deployment-config.json not found"
    fi
    
    # Check for environment file
    if [[ -f "$SCRIPT_DIR/../.env" ]]; then
        print_pass ".env file found"
        
        # Check for required environment variables
        local required_vars=(
            "JWT_SECRET"
            "SENDGRID_API_KEY"
            "PAYPAL_CLIENT_ID"
            "PAYPAL_CLIENT_SECRET"
        )
        
        for var in "${required_vars[@]}"; do
            if grep -q "^${var}=" "$SCRIPT_DIR/../.env"; then
                print_pass "Environment variable $var is set"
            else
                print_fail "Missing environment variable: $var"
            fi
        done
    else
        print_warn ".env file not found (will need to be created)"
    fi
    
    # Check for Docker Compose files
    if [[ -f "$SCRIPT_DIR/../docker-compose.yml" ]]; then
        print_pass "docker-compose.yml found"
        
        # Validate Docker Compose file
        if command -v docker-compose &> /dev/null; then
            if docker-compose -f "$SCRIPT_DIR/../docker-compose.yml" config &> /dev/null; then
                print_pass "docker-compose.yml is valid"
            else
                print_fail "docker-compose.yml validation failed"
            fi
        fi
    else
        print_warn "docker-compose.yml not found"
    fi
}

check_ssl() {
    print_header "SSL/TLS Configuration"
    
    # Check for existing certificates
    if [[ -d "/etc/letsencrypt/live" ]]; then
        print_info "Let's Encrypt directory found"
        local certs=$(find /etc/letsencrypt/live -name "fullchain.pem" 2>/dev/null | wc -l)
        if [[ "$certs" -gt 0 ]]; then
            print_pass "SSL certificates found: $certs domain(s)"
        else
            print_warn "No SSL certificates found in Let's Encrypt directory"
        fi
    else
        print_warn "Let's Encrypt directory not found"
    fi
    
    # Check Certbot
    if command -v certbot &> /dev/null; then
        local certbot_version=$(certbot --version 2>&1 | grep -oP 'certbot \K[0-9.]+')
        print_pass "Certbot installed: version $certbot_version"
    else
        print_warn "Certbot not installed (required for automatic SSL)"
    fi
}

check_permissions() {
    print_header "File Permissions"
    
    # Check script permissions
    local scripts=(
        "$SCRIPT_DIR/deploy-linux.sh"
        "$SCRIPT_DIR/deploy-windows.ps1"
        "$SCRIPT_DIR/post-deployment-health-check.sh"
    )
    
    for script in "${scripts[@]}"; do
        if [[ -f "$script" ]]; then
            if [[ -x "$script" ]]; then
                print_pass "$(basename "$script") is executable"
            else
                print_warn "$(basename "$script") is not executable"
                print_info "Fix with: chmod +x $script"
            fi
        fi
    done
    
    # Check directory permissions
    local required_dirs=(
        "$SCRIPT_DIR/../data"
        "$SCRIPT_DIR/../logs"
        "$SCRIPT_DIR/../uploads"
    )
    
    for dir in "${required_dirs[@]}"; do
        if [[ -d "$dir" ]]; then
            if [[ -w "$dir" ]]; then
                print_pass "$(basename "$dir") directory is writable"
            else
                print_fail "$(basename "$dir") directory is not writable"
            fi
        else
            print_info "$(basename "$dir") directory will be created during deployment"
        fi
    done
}

check_security() {
    print_header "Security Configuration"
    
    # Check for default credentials in config
    if [[ -f "$SCRIPT_DIR/../.env" ]]; then
        # Check for weak passwords
        if grep -qE '(password|secret|key)=.{1,8}$' "$SCRIPT_DIR/../.env"; then
            print_warn "Potentially weak passwords detected in .env file"
        else
            print_pass "No obviously weak passwords detected"
        fi
        
        # Check for default values
        if grep -qE '(YOUR_|CHANGE_ME|REPLACE_|DEFAULT_)' "$SCRIPT_DIR/../.env"; then
            print_fail "Default placeholder values found in .env file"
        else
            print_pass "No default placeholder values found"
        fi
    fi
    
    # Check firewall status
    if command -v ufw &> /dev/null; then
        if sudo ufw status &> /dev/null; then
            local ufw_status=$(sudo ufw status | grep -oP 'Status: \K\w+')
            if [[ "$ufw_status" == "active" ]]; then
                print_pass "UFW firewall is active"
            else
                print_warn "UFW firewall is not active"
            fi
        fi
    elif command -v firewall-cmd &> /dev/null; then
        if sudo firewall-cmd --state &> /dev/null; then
            print_pass "Firewalld is running"
        else
            print_warn "Firewalld is not running"
        fi
    else
        print_info "No supported firewall detected"
    fi
}

generate_summary() {
    print_header "Validation Summary"
    
    local total_checks=$((ERRORS_FOUND + WARNINGS_FOUND))
    
    if [[ "$ERRORS_FOUND" -eq 0 ]]; then
        if [[ "$WARNINGS_FOUND" -eq 0 ]]; then
            echo -e "${GREEN}All checks passed successfully!${NC}"
            echo "The environment is ready for deployment."
        else
            echo -e "${YELLOW}Validation completed with $WARNINGS_FOUND warning(s).${NC}"
            echo "Review warnings before proceeding with deployment."
        fi
    else
        echo -e "${RED}Validation failed with $ERRORS_FOUND error(s) and $WARNINGS_FOUND warning(s).${NC}"
        echo "Address all errors before attempting deployment."
    fi
    
    echo ""
    echo "Validation log saved to: $VALIDATION_LOG"
    
    # Generate recommendations
    if [[ "$ERRORS_FOUND" -gt 0 ]] || [[ "$WARNINGS_FOUND" -gt 0 ]]; then
        echo -e "\n${BLUE}Recommendations:${NC}"
        
        if grep -q "Docker not installed" "$VALIDATION_LOG"; then
            echo "- Install Docker: https://docs.docker.com/engine/install/"
        fi
        
        if grep -q "Certbot not installed" "$VALIDATION_LOG"; then
            echo "- Install Certbot for SSL: sudo apt-get install certbot"
        fi
        
        if grep -q ".env file not found" "$VALIDATION_LOG"; then
            echo "- Create .env file from template: cp .env.example .env"
        fi
        
        if grep -q "is not executable" "$VALIDATION_LOG"; then
            echo "- Make scripts executable: chmod +x deployment/*.sh"
        fi
    fi
}

# Main execution
main() {
    echo -e "${BLUE}WitchCityRope Pre-Deployment Validation${NC}"
    echo "========================================="
    echo "Timestamp: $(date)"
    echo "Running from: $SCRIPT_DIR"
    echo ""
    
    # Run all checks
    check_os
    check_hardware
    check_prerequisites
    check_network
    check_docker
    check_configuration
    check_ssl
    check_permissions
    check_security
    
    # Generate summary
    generate_summary
    
    # Exit with appropriate code
    if [[ "$ERRORS_FOUND" -gt 0 ]]; then
        exit 1
    elif [[ "$WARNINGS_FOUND" -gt 0 ]]; then
        exit 0
    else
        exit 0
    fi
}

# Run main function
main "$@"