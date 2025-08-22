#!/bin/bash
# test-browser-mcp.sh - Tests both Browser Tools and Stagehand functionality

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[TEST]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[✓]${NC} $1"
}

print_error() {
    echo -e "${RED}[✗]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[!]${NC} $1"
}

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to test PowerShell bridge
test_powershell_bridge() {
    print_status "Testing PowerShell bridge..."
    
    local ps_script="/mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/browser-bridge.ps1"
    
    if [ ! -f "$ps_script" ]; then
        print_error "PowerShell bridge script not found at: $ps_script"
        return 1
    fi
    
    # Test PowerShell availability
    if command_exists powershell.exe; then
        print_success "PowerShell is available"
        
        # Test browser-bridge.ps1 status command
        print_status "Testing browser status..."
        powershell.exe -ExecutionPolicy Bypass -File "$ps_script" -Command status
        
        return 0
    else
        print_error "PowerShell not available from WSL"
        return 1
    fi
}

# Function to test Chrome launch
test_chrome_launch() {
    print_status "Testing Chrome launch script..."
    
    local launch_script="./launch-chrome-wsl.sh"
    
    if [ ! -f "$launch_script" ]; then
        print_error "Chrome launch script not found"
        return 1
    fi
    
    # Make script executable
    chmod +x "$launch_script"
    
    # Test Chrome detection
    print_status "Checking for Chrome installation..."
    if bash "$launch_script" --version 2>&1 | grep -q "Google Chrome"; then
        print_success "Chrome detected"
    else
        print_warning "Chrome may not be properly detected"
    fi
    
    return 0
}

# Function to test MCP server connections
test_mcp_servers() {
    print_status "Testing MCP server availability..."
    
    # Check if MCP server config exists
    local mcp_config="/mnt/c/Users/chad/AppData/Roaming/Claude/claude_desktop_config.json"
    
    if [ -f "$mcp_config" ]; then
        print_success "MCP configuration found"
        
        # Check for browser-tools and stagehand entries
        if grep -q "browser-tools" "$mcp_config"; then
            print_success "browser-tools MCP server configured"
        else
            print_warning "browser-tools not found in MCP config"
        fi
        
        if grep -q "stagehand" "$mcp_config"; then
            print_success "stagehand MCP server configured"
        else
            print_warning "stagehand not found in MCP config"
        fi
    else
        print_error "MCP configuration not found at: $mcp_config"
    fi
}

# Function to test browser automation
test_browser_automation() {
    print_status "Testing browser automation capabilities..."
    
    # Launch Chrome if not running
    print_status "Checking if Chrome is running with debugging..."
    if ! curl -s http://localhost:9222/json/version > /dev/null 2>&1; then
        print_warning "Chrome not running with debugging. Attempting to launch..."
        
        # Try PowerShell method
        powershell.exe -ExecutionPolicy Bypass -File "/mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/browser-bridge.ps1" -Command launch
        
        sleep 3
    fi
    
    # Check if Chrome is now accessible
    if curl -s http://localhost:9222/json/version > /dev/null 2>&1; then
        print_success "Chrome DevTools Protocol is accessible"
        
        # Get Chrome version
        local version=$(curl -s http://localhost:9222/json/version | grep -o '"Browser": "[^"]*"' | cut -d'"' -f4)
        print_success "Chrome version: $version"
        
        # List tabs
        print_status "Getting open tabs..."
        local tabs=$(curl -s http://localhost:9222/json | jq -r '.[] | select(.type=="page") | .title' 2>/dev/null)
        if [ -n "$tabs" ]; then
            echo "$tabs" | while read -r tab; do
                print_success "Tab: $tab"
            done
        fi
    else
        print_error "Cannot connect to Chrome DevTools Protocol"
    fi
}

# Function to create test HTML file
create_test_page() {
    local test_file="/tmp/browser-test.html"
    
    cat > "$test_file" << 'EOF'
<!DOCTYPE html>
<html>
<head>
    <title>MCP Browser Test Page</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            max-width: 800px;
            margin: 50px auto;
            padding: 20px;
        }
        h1 { color: #333; }
        .test-button {
            background: #4CAF50;
            color: white;
            padding: 10px 20px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            margin: 10px;
        }
        .test-input {
            padding: 10px;
            margin: 10px;
            border: 1px solid #ddd;
            border-radius: 4px;
        }
        #output {
            background: #f4f4f4;
            padding: 10px;
            margin: 20px 0;
            border-radius: 4px;
            min-height: 50px;
        }
    </style>
</head>
<body>
    <h1>MCP Browser Test Page</h1>
    <p>This page tests browser automation capabilities.</p>
    
    <div>
        <input type="text" class="test-input" id="testInput" placeholder="Type something here">
        <button class="test-button" onclick="addOutput('Button clicked!')">Click Me</button>
        <button class="test-button" onclick="clearOutput()">Clear Output</button>
    </div>
    
    <div id="output">Output will appear here...</div>
    
    <script>
        function addOutput(text) {
            const output = document.getElementById('output');
            const timestamp = new Date().toLocaleTimeString();
            output.innerHTML += `<div>[${timestamp}] ${text}</div>`;
        }
        
        function clearOutput() {
            document.getElementById('output').innerHTML = 'Output cleared.';
        }
        
        // Log page load
        addOutput('Page loaded successfully!');
    </script>
</body>
</html>
EOF
    
    echo "$test_file"
}

# Main test execution
main() {
    echo "========================================="
    echo "MCP Browser Testing Suite"
    echo "========================================="
    echo
    
    # Test 1: PowerShell Bridge
    print_status "Step 1: PowerShell Bridge Test"
    test_powershell_bridge
    echo
    
    # Test 2: Chrome Launch
    print_status "Step 2: Chrome Launch Test"
    test_chrome_launch
    echo
    
    # Test 3: MCP Server Configuration
    print_status "Step 3: MCP Server Configuration Test"
    test_mcp_servers
    echo
    
    # Test 4: Browser Automation
    print_status "Step 4: Browser Automation Test"
    test_browser_automation
    echo
    
    # Test 5: Create and open test page
    print_status "Step 5: Creating test page..."
    local test_page=$(create_test_page)
    print_success "Test page created at: $test_page"
    
    # Convert to Windows path for PowerShell
    local windows_path=$(wslpath -w "$test_page")
    
    print_status "Opening test page in Chrome..."
    powershell.exe -ExecutionPolicy Bypass -File "/mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/browser-bridge.ps1" -Command navigate -Url "file:///$windows_path"
    
    echo
    echo "========================================="
    echo "Test Summary:"
    echo "========================================="
    echo
    echo "To use browser automation from Claude:"
    echo "1. Ensure Chrome is running with: ./launch-chrome-wsl.sh"
    echo "2. Use PowerShell bridge: powershell.exe -ExecutionPolicy Bypass -File browser-bridge.ps1 -Command <command>"
    echo "3. Available commands: status, launch, navigate, tabs, close"
    echo
    echo "For MCP servers (browser-tools, stagehand):"
    echo "- They should be configured in Claude Desktop"
    echo "- Use them through Claude's interface, not directly from scripts"
    echo
    
    print_success "Testing complete!"
}

# Run main function
main "$@"