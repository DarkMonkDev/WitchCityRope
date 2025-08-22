const http = require('http');
const fs = require('fs');
const path = require('path');

// Simple script to fetch and analyze the page
console.log('Fetching page content from http://localhost:5651/...\n');

const options = {
  hostname: 'localhost',
  port: 5651,
  path: '/',
  method: 'GET'
};

const req = http.request(options, (res) => {
  console.log(`Status Code: ${res.statusCode}`);
  console.log(`Headers: ${JSON.stringify(res.headers, null, 2)}\n`);

  let data = '';

  res.on('data', (chunk) => {
    data += chunk;
  });

  res.on('end', () => {
    // Save the HTML content
    const htmlPath = path.join(__dirname, 'homepage-content.html');
    fs.writeFileSync(htmlPath, data);
    console.log(`HTML content saved to: ${htmlPath}`);
    console.log(`Content length: ${data.length} bytes\n`);

    // Analyze the content
    const titleMatch = data.match(/<title>(.*?)<\/title>/i);
    if (titleMatch) {
      console.log(`Page Title: ${titleMatch[1]}`);
    }

    // Look for common elements
    const hasNavbar = data.includes('nav') || data.includes('navbar');
    const hasMain = data.includes('<main') || data.includes('id="app"');
    const hasFooter = data.includes('<footer');
    const hasScripts = (data.match(/<script/g) || []).length;
    const hasStyles = (data.match(/<link.*?css/g) || []).length;

    console.log('\nPage Analysis:');
    console.log(`- Has Navigation: ${hasNavbar}`);
    console.log(`- Has Main Content: ${hasMain}`);
    console.log(`- Has Footer: ${hasFooter}`);
    console.log(`- Number of Scripts: ${hasScripts}`);
    console.log(`- Number of Stylesheets: ${hasStyles}`);

    // Check for Blazor
    if (data.includes('blazor') || data.includes('_framework')) {
      console.log('\nThis appears to be a Blazor application.');
    }

    // Look for error indicators
    const errorIndicators = [
      'error',
      'exception',
      'failed',
      'cannot',
      'unable'
    ];

    console.log('\nChecking for potential issues...');
    let issuesFound = false;
    errorIndicators.forEach(indicator => {
      const regex = new RegExp(indicator, 'gi');
      const matches = data.match(regex);
      if (matches && matches.length > 0) {
        console.log(`- Found "${indicator}" ${matches.length} times`);
        issuesFound = true;
      }
    });

    if (!issuesFound) {
      console.log('No obvious error indicators found in the HTML.');
    }

    console.log('\n---');
    console.log('Since Puppeteer requires additional system dependencies in WSL,');
    console.log('please use one of these methods to take a screenshot:\n');
    console.log('1. Open http://localhost:5651/ in your Windows browser');
    console.log('2. Press F12 to open Developer Tools');
    console.log('3. Press Ctrl+Shift+P and type "screenshot"');
    console.log('4. Select "Capture full size screenshot"\n');
    console.log('The HTML content has been saved to homepage-content.html for review.');
  });
});

req.on('error', (error) => {
  console.error('Error connecting to server:', error.message);
  console.error('\nPlease ensure the application is running on http://localhost:5651/');
});

req.end();