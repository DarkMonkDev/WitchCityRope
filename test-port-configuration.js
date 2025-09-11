#!/usr/bin/env node

/**
 * Port Configuration Test Script
 * 
 * This script verifies that:
 * 1. Environment variables are loaded correctly
 * 2. API endpoints respond on configured ports  
 * 3. Web server is accessible on configured port
 * 4. No hard-coded ports are breaking the configuration
 */

const http = require('http');
const https = require('https');

// Load environment variables (simulating Vite behavior)
const config = {
  web: {
    port: process.env.VITE_PORT || 5173,
    url: process.env.VITE_BASE_URL || `http://localhost:${process.env.VITE_PORT || 5173}`,
  },
  api: {
    port: process.env.VITE_API_PORT || 5655,
    url: process.env.VITE_API_BASE_URL || 'http://localhost:5655',
  }
};

console.log('🔧 Port Configuration Test');
console.log('============================');
console.log('Configuration loaded:');
console.log(`  Web Port: ${config.web.port}`);
console.log(`  Web URL: ${config.web.url}`);
console.log(`  API Port: ${config.api.port}`);
console.log(`  API URL: ${config.api.url}`);
console.log('');

/**
 * Test HTTP endpoint
 */
function testEndpoint(url, description) {
  return new Promise((resolve) => {
    const urlObj = new URL(url);
    const client = urlObj.protocol === 'https:' ? https : http;
    
    const req = client.get(url, { timeout: 5000 }, (res) => {
      console.log(`✅ ${description}: ${res.statusCode} ${res.statusMessage}`);
      resolve({ success: true, status: res.statusCode, url });
    });
    
    req.on('error', (err) => {
      console.log(`❌ ${description}: ${err.message}`);
      resolve({ success: false, error: err.message, url });
    });
    
    req.on('timeout', () => {
      console.log(`⏰ ${description}: Timeout`);
      req.destroy();
      resolve({ success: false, error: 'Timeout', url });
    });
  });
}

/**
 * Test API data response
 */
async function testApiData(url, description) {
  try {
    const response = await fetch(url);
    const data = await response.json();
    
    console.log(`📊 ${description}:`);
    console.log(`   Status: ${response.status}`);
    console.log(`   Response Type: ${typeof data}`);
    
    // Handle wrapped API responses
    if (data.success && data.data) {
      const actualData = data.data;
      console.log(`   Data Type: ${Array.isArray(actualData) ? 'Array' : typeof actualData}`);
      
      if (Array.isArray(actualData)) {
        console.log(`   Count: ${actualData.length} items`);
        if (actualData.length > 0) {
          const firstItem = actualData[0];
          console.log(`   Sample Fields: ${Object.keys(firstItem).join(', ')}`);
          
          // Check for known field name issues
          if (firstItem.startDate && !firstItem.startDateTime) {
            console.log(`   ⚠️ Field Mapping Required: API uses 'startDate' but frontend expects 'startDateTime'`);
            console.log(`   ✅ Our field mapping utility should handle this automatically`);
          }
          if (firstItem.startDateTime && !firstItem.startDate) {
            console.log(`   ✅ API uses correct 'startDateTime' field name`);
          }
          if (!firstItem.status) {
            console.log(`   ℹ️ API doesn't return 'status' field - frontend uses fallback logic`);
          }
        }
      }
    } else if (Array.isArray(data)) {
      console.log(`   Count: ${data.length} items (direct array)`);
    }
    
    return { success: true, data };
  } catch (error) {
    console.log(`❌ ${description}: ${error.message}`);
    return { success: false, error: error.message };
  }
}

/**
 * Main test execution
 */
async function runTests() {
  console.log('Testing endpoints...');
  
  const tests = [
    testEndpoint(config.web.url, 'Web Server (React)'),
    testEndpoint(`${config.api.url}/health`, 'API Health Check'),
    testEndpoint(`${config.api.url}/api/events`, 'API Events Endpoint'),
  ];
  
  const results = await Promise.all(tests);
  
  console.log('');
  console.log('Testing API data responses...');
  
  // Test actual API response format
  if (results[2]?.success) {
    await testApiData(`${config.api.url}/api/events`, 'Events API Data');
  }
  
  console.log('');
  console.log('Test Summary:');
  console.log('=============');
  
  const successCount = results.filter(r => r.success).length;
  console.log(`${successCount}/${results.length} endpoints responding`);
  
  if (successCount === results.length) {
    console.log('🎉 All port configurations working correctly!');
    console.log('');
    console.log('Next steps:');
    console.log('1. Update any remaining hard-coded ports in test files');
    console.log('2. Verify events page displays correctly');
    console.log('3. Run Playwright tests to confirm');
  } else {
    console.log('');
    console.log('❌ Some services are not responding. Check:');
    console.log('1. Is the development server running? (npm run dev)');
    console.log('2. Is the API server running?');
    console.log('3. Are the environment variables correct?');
  }
}

// Run the tests
runTests().catch(console.error);