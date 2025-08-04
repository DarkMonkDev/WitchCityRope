#!/bin/bash
# test-puppeteer-ubuntu.sh - Tests Puppeteer functionality on Ubuntu

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

# Function to test Chrome connection
test_chrome_connection() {
    print_status "Testing Chrome connection..."
    
    # Test Chrome availability
    if command_exists google-chrome; then
        print_success "Google Chrome is installed"
        
        # Get Chrome version
        local installed_version=$(google-chrome --version 2>/dev/null | cut -d' ' -f3)
        print_status "Installed Chrome version: $installed_version"
    else
        print_error "Google Chrome is not installed"
        print_status "Install with: sudo apt install google-chrome-stable"
        return 1
    fi
    
    # Check if Chrome is running with debug port
    if curl -s http://localhost:9222/json/version > /dev/null 2>&1; then
        print_success "Chrome is running with debugging on port 9222"
        
        # Get Chrome debug version
        local chrome_version=$(curl -s http://localhost:9222/json/version | grep -o '"Browser":"[^"]*"' | cut -d'"' -f4)
        print_status "Debug Chrome version: $chrome_version"
        
        return 0
    else
        print_warning "Chrome is not running with debugging port"
        print_status "Start Chrome with: google-chrome --remote-debugging-port=9222"
        return 1
    fi
}

# Function to test Node.js and Puppeteer
test_nodejs_puppeteer() {
    print_status "Testing Node.js and Puppeteer environment..."
    
    if command_exists node; then
        local node_version=$(node --version)
        print_success "Node.js installed: $node_version"
        
        # Check for npm
        if command_exists npm; then
            print_success "npm is available"
            
            # Check for Puppeteer in current project
            if [ -f "package.json" ] && grep -q "puppeteer" package.json; then
                print_success "Puppeteer is installed in this project"
            else
                print_warning "Puppeteer not found in package.json"
                print_status "Install with: npm install puppeteer"
            fi
        else
            print_error "npm is not installed"
        fi
    else
        print_error "Node.js is not installed"
        print_status "Install with: sudo apt install nodejs npm"
    fi
}

# Function to create Puppeteer test script
create_puppeteer_test() {
    local test_file="test-puppeteer-simple.js"
    
    cat > "$test_file" << 'EOF'
const puppeteer = require('puppeteer');

(async () => {
  console.log('Starting Puppeteer test...');
  
  try {
    // Launch browser
    const browser = await puppeteer.launch({
      headless: false,
      args: [
        '--no-sandbox',
        '--disable-setuid-sandbox',
        '--disable-dev-shm-usage'
      ]
    });
    
    // Create new page
    const page = await browser.newPage();
    console.log('✓ Browser launched successfully');
    
    // Navigate to example.com
    await page.goto('https://example.com');
    console.log('✓ Navigated to example.com');
    
    // Take screenshot
    await page.screenshot({ path: 'example.png' });
    console.log('✓ Screenshot saved as example.png');
    
    // Get page title
    const title = await page.title();
    console.log(`✓ Page title: ${title}`);
    
    // Wait a bit so user can see the browser
    await new Promise(resolve => setTimeout(resolve, 3000));
    
    // Close browser
    await browser.close();
    console.log('✓ Browser closed successfully');
    console.log('Puppeteer test completed!');
    
  } catch (error) {
    console.error('Error during Puppeteer test:', error);
    process.exit(1);
  }
})();
EOF
    
    echo "$test_file"
}

# Function to run Puppeteer test
run_puppeteer_test() {
    print_status "Running Puppeteer test..."
    
    # Check if Puppeteer is installed
    if [ ! -d "node_modules/puppeteer" ]; then
        print_error "Puppeteer not installed"
        print_status "Installing Puppeteer..."
        npm install puppeteer
    fi
    
    # Create test script
    local test_script=$(create_puppeteer_test)
    print_success "Created test script: $test_script"
    
    # Run the test
    print_status "Executing Puppeteer test..."
    node "$test_script"
    
    # Check if screenshot was created
    if [ -f "example.png" ]; then
        print_success "Screenshot created successfully"
        print_status "Screenshot saved at: $(pwd)/example.png"
    fi
    
    # Clean up
    rm -f "$test_script"
}

# Main test execution
main() {
    echo "========================================="
    echo "Puppeteer Testing Suite for Ubuntu"
    echo "========================================="
    echo
    
    # Test 1: Node.js and Puppeteer
    print_status "Step 1: Node.js and Puppeteer Test"
    test_nodejs_puppeteer
    echo
    
    # Test 2: Chrome Connection
    print_status "Step 2: Chrome Connection Test"
    test_chrome_connection
    echo
    
    # Test 3: Run Puppeteer Test
    print_status "Step 3: Puppeteer Functionality Test"
    run_puppeteer_test
    echo
    
    echo "========================================="
    echo "Test Summary:"
    echo "========================================="
    echo
    echo "To use Puppeteer in your project:"
    echo "1. Install Puppeteer: npm install puppeteer"
    echo "2. Use the Puppeteer API directly in your JavaScript code"
    echo "3. No MCP server configuration needed"
    echo
    echo "For debugging with Chrome DevTools:"
    echo "- Connect to existing Chrome: puppeteer.connect({ browserURL: 'http://localhost:9222' })"
    echo "- Or let Puppeteer launch its own Chrome instance"
    echo
    echo "Ubuntu-specific tips:"
    echo "- Install Chrome: sudo apt install google-chrome-stable"
    echo "- For headless mode: browser = await puppeteer.launch({ headless: true })"
    echo "- Use sandbox args if running as root or in Docker"
    echo
    
    print_success "Testing complete!"
}

# Run main function
main "$@"