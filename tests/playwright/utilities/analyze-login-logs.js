const fs = require('fs');
const path = require('path');

function analyzeLoginLogs() {
  const logsDir = path.join(__dirname, 'login-monitoring-results');
  
  if (!fs.existsSync(logsDir)) {
    console.log('❌ No login monitoring results found.');
    console.log('Please run test-login-with-monitoring.js first.');
    return;
  }

  // Find the most recent test summary
  const files = fs.readdirSync(logsDir);
  const summaryFiles = files.filter(f => f.startsWith('test-summary-'));
  
  if (summaryFiles.length === 0) {
    console.log('❌ No test summary files found.');
    return;
  }

  // Get the most recent summary
  summaryFiles.sort();
  const latestSummary = summaryFiles[summaryFiles.length - 1];
  
  console.log(`\n📊 Analyzing: ${latestSummary}\n`);
  
  const summaryPath = path.join(logsDir, latestSummary);
  const summary = JSON.parse(fs.readFileSync(summaryPath, 'utf8'));
  
  console.log('🔍 LOGIN TEST ANALYSIS REPORT');
  console.log('================================\n');
  
  // Basic results
  console.log('📋 Test Results:');
  console.log(`   • Initial URL: ${summary.testResults.initialUrl}`);
  console.log(`   • Final URL: ${summary.testResults.finalUrl}`);
  console.log(`   • URL Changed: ${summary.testResults.urlChanged ? '✅ Yes' : '❌ No'}`);
  console.log(`   • Login Successful: ${summary.testResults.loginSuccessful ? '✅ Yes' : '❌ No'}`);
  console.log('');
  
  // API Calls Analysis
  console.log('🌐 API Calls Analysis:');
  console.log(`   • Total API calls: ${summary.apiCalls.length}`);
  
  if (summary.apiCalls.length > 0) {
    console.log('   • Endpoints called:');
    summary.apiCalls.forEach((call, index) => {
      console.log(`     ${index + 1}. ${call.method} ${call.url}`);
      if (call.postData) {
        console.log(`        Body: ${call.postData.substring(0, 100)}...`);
      }
    });
  }
  console.log('');
  
  // Docker Logs Analysis
  console.log('📝 Docker Logs Analysis:');
  console.log(`   • Total captured logs: ${summary.dockerLogs.length}`);
  
  // Check for specific patterns
  const patterns = {
    'LoginAsync method called': /LoginAsync/i,
    'AuthenticationService accessed': /AuthenticationService/i,
    'JWT token operations': /JWT|Token/i,
    'Errors or exceptions': /ERROR|Exception|fail/i,
    'HTTP client requests': /HttpClient/i,
    'Authentication attempts': /Authentication|auth/i,
    'Redirects': /redirect/i
  };
  
  const foundPatterns = {};
  Object.keys(patterns).forEach(key => {
    foundPatterns[key] = summary.dockerLogs.filter(log => 
      patterns[key].test(log.log)
    ).length;
  });
  
  console.log('   • Pattern matches:');
  Object.entries(foundPatterns).forEach(([pattern, count]) => {
    const status = count > 0 ? '✅' : '❌';
    console.log(`     ${status} ${pattern}: ${count} occurrences`);
  });
  console.log('');
  
  // Authentication Data Analysis
  console.log('🔐 Authentication Data:');
  const authData = summary.testResults.authData;
  
  console.log('   • localStorage items:', Object.keys(authData.localStorage).length);
  if (Object.keys(authData.localStorage).length > 0) {
    Object.entries(authData.localStorage).forEach(([key, value]) => {
      const displayValue = value && value.length > 50 ? value.substring(0, 50) + '...' : value;
      console.log(`     - ${key}: ${displayValue}`);
    });
  }
  
  console.log('   • sessionStorage items:', Object.keys(authData.sessionStorage).length);
  console.log(`   • Cookies present: ${authData.cookies ? 'Yes' : 'No'}`);
  console.log(`   • Has logout button: ${authData.authIndicators.hasLogoutButton ? '✅ Yes' : '❌ No'}`);
  console.log(`   • Shows user info: ${authData.authIndicators.hasUserInfo ? '✅ Yes' : '❌ No'}`);
  
  if (authData.authIndicators.errorMessages.length > 0) {
    console.log('   • ❌ Error messages found:');
    authData.authIndicators.errorMessages.forEach(msg => {
      console.log(`     - ${msg}`);
    });
  }
  console.log('');
  
  // Critical Issues
  console.log('🚨 Critical Findings:');
  const issues = [];
  
  if (!summary.testResults.urlChanged) {
    issues.push('URL did not change after login attempt');
  }
  
  if (foundPatterns['LoginAsync method called'] === 0) {
    issues.push('LoginAsync method was never called - login request may not be reaching the API');
  }
  
  if (foundPatterns['Errors or exceptions'] > 0) {
    issues.push(`Found ${foundPatterns['Errors or exceptions']} error/exception logs`);
  }
  
  if (summary.apiCalls.length === 0) {
    issues.push('No API calls were detected during login');
  }
  
  if (!authData.authIndicators.hasLogoutButton && !authData.authIndicators.hasUserInfo) {
    issues.push('No authentication indicators found after login');
  }
  
  if (issues.length === 0) {
    console.log('   ✅ No critical issues found');
  } else {
    issues.forEach((issue, index) => {
      console.log(`   ${index + 1}. ❌ ${issue}`);
    });
  }
  console.log('');
  
  // Recommendations
  console.log('💡 Recommendations:');
  
  if (foundPatterns['LoginAsync method called'] === 0) {
    console.log('   1. Check if the login form is properly submitting to the correct endpoint');
    console.log('   2. Verify the API routing configuration in the Web project');
    console.log('   3. Check for JavaScript errors preventing form submission');
  }
  
  if (summary.apiCalls.length === 0) {
    console.log('   1. Check if the API base URL is correctly configured');
    console.log('   2. Verify CORS settings between Web and API projects');
    console.log('   3. Check network connectivity between containers');
  }
  
  if (foundPatterns['Errors or exceptions'] > 0) {
    console.log('   1. Review the error logs for specific error messages');
    console.log('   2. Check API logs for detailed exception information');
    console.log('   3. Verify database connectivity and user seed data');
  }
  
  console.log('\n📁 Full results available in:', logsDir);
  
  // Show actual error logs if any
  const errorLogs = summary.dockerLogs.filter(log => 
    /ERROR|Exception|fail/i.test(log.log)
  );
  
  if (errorLogs.length > 0) {
    console.log('\n❌ Error Logs:');
    errorLogs.forEach((log, index) => {
      console.log(`${index + 1}. [${log.timestamp}] ${log.log}`);
    });
  }
}

// Run the analysis
analyzeLoginLogs();