#!/bin/bash

# Bash script to check for vulnerabilities in WitchCityRope project
# Run this script periodically to check for security updates

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

echo -e "${GREEN}WitchCityRope Security Vulnerability Check${NC}"
echo -e "${GREEN}=========================================${NC}"
echo ""

# Check if dotnet CLI is available
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}Error: .NET CLI is not installed or not in PATH${NC}"
    exit 1
fi

echo -e "${YELLOW}Checking .NET version...${NC}"
dotnet --version
echo ""

# Function to check vulnerabilities for a project
check_project_vulnerabilities() {
    local project_path=$1
    local project_name=$2
    
    echo -e "${CYAN}Checking $project_name...${NC}"
    
    # Check for vulnerable packages
    echo -e "${GRAY}  - Scanning for vulnerabilities...${NC}"
    vulnerable_output=$(dotnet list "$project_path" package --vulnerable --include-transitive 2>&1)
    
    if [ $? -eq 0 ]; then
        if echo "$vulnerable_output" | grep -q "No vulnerable packages"; then
            echo -e "  ${GREEN}✓ No vulnerable packages found${NC}"
        else
            echo -e "  ${RED}⚠ Vulnerable packages found:${NC}"
            echo "$vulnerable_output"
        fi
    else
        echo -e "  ${RED}✗ Error checking vulnerabilities${NC}"
        echo "$vulnerable_output"
    fi
    
    # Check for outdated packages
    echo -e "${GRAY}  - Checking for outdated packages...${NC}"
    outdated_output=$(dotnet list "$project_path" package --outdated 2>&1)
    
    if [ $? -eq 0 ]; then
        if echo "$outdated_output" | grep -q "No outdated packages"; then
            echo -e "  ${GREEN}✓ All packages are up to date${NC}"
        else
            echo -e "  ${YELLOW}ℹ Outdated packages found (review for security updates):${NC}"
            echo "$outdated_output"
        fi
    else
        echo -e "  ${RED}✗ Error checking outdated packages${NC}"
    fi
    
    echo ""
}

# Main execution
root_path=$(pwd)

# Check if we're in the right directory
if [ ! -f "WitchCityRope.sln" ]; then
    echo -e "${RED}Error: WitchCityRope.sln not found. Please run this script from the solution root.${NC}"
    exit 1
fi

# Restore packages first
echo -e "${YELLOW}Restoring NuGet packages...${NC}"
dotnet restore --verbosity quiet
echo ""

# Define projects to check
declare -a projects=(
    "src/WitchCityRope.Api/WitchCityRope.Api.csproj:API Project"
    "src/WitchCityRope.Core/WitchCityRope.Core.csproj:Core Project"
    "src/WitchCityRope.Infrastructure/WitchCityRope.Infrastructure.csproj:Infrastructure Project"
    "src/WitchCityRope.Web/WitchCityRope.Web.csproj:Web Project"
)

# Check each project
for project in "${projects[@]}"; do
    IFS=':' read -r path name <<< "$project"
    if [ -f "$path" ]; then
        check_project_vulnerabilities "$path" "$name"
    else
        echo -e "${YELLOW}Warning: Project not found - $path${NC}"
    fi
done

# Additional security checks
echo -e "${GREEN}Additional Security Recommendations:${NC}"
echo -e "${GREEN}===================================${NC}"
echo ""

# Check for specific packages that need attention
echo -e "${YELLOW}Checking for legacy packages...${NC}"

legacy_packages=("System.IdentityModel.Tokens.Jwt")

for package in "${legacy_packages[@]}"; do
    if grep -q "$package" src/**/*.csproj 2>/dev/null; then
        echo -e "${YELLOW}⚠ Legacy package found: $package${NC}"
        echo -e "${GRAY}  Consider migrating to: Microsoft.IdentityModel.JsonWebTokens${NC}"
    fi
done

echo ""
echo -e "${GREEN}Security Check Complete!${NC}"
echo ""
echo -e "${CYAN}Next Steps:${NC}"
echo -e "${GRAY}1. Review the SECURITY_VULNERABILITY_REPORT.md for detailed findings${NC}"
echo -e "${GRAY}2. Update any vulnerable packages immediately${NC}"
echo -e "${GRAY}3. Consider updating outdated packages after testing${NC}"
echo -e "${GRAY}4. Set up automated security scanning in your CI/CD pipeline${NC}"
echo ""

# Generate timestamp
timestamp=$(date "+%Y-%m-%d %H:%M:%S")
echo -e "${GRAY}Report generated at: $timestamp${NC}"