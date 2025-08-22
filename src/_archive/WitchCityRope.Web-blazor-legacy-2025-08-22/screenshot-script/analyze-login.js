const http = require('http');
const fs = require('fs');
const path = require('path');

function fetchLoginPage() {
  const options = {
    hostname: 'localhost',
    port: 5651,
    path: '/auth/login',
    method: 'GET'
  };

  const req = http.request(options, (res) => {
    console.log(`Status Code: ${res.statusCode}`);
    console.log(`Headers:`, res.headers);
    
    let data = '';
    
    res.on('data', (chunk) => {
      data += chunk;
    });
    
    res.on('end', () => {
      // Save the HTML
      const timestamp = new Date().toISOString().replace(/[:]/g, '-').split('.')[0];
      const filename = `login-page-${timestamp}.html`;
      fs.writeFileSync(filename, data);
      console.log(`HTML saved to: ${filename}`);
      
      // Analyze the HTML
      console.log('\n=== Page Analysis ===');
      
      // Check for forms
      const formMatches = data.match(/<form[^>]*>/gi) || [];
      console.log(`Forms found: ${formMatches.length}`);
      
      // Check for input fields
      const inputMatches = data.match(/<input[^>]*>/gi) || [];
      console.log(`Input fields found: ${inputMatches.length}`);
      
      // Check for password fields
      const passwordFields = inputMatches.filter(input => input.includes('type="password"') || input.includes("type='password'"));
      console.log(`Password fields found: ${passwordFields.length}`);
      
      // Check for submit buttons
      const submitButtons = data.match(/<button[^>]*type="submit"[^>]*>|<input[^>]*type="submit"[^>]*>/gi) || [];
      console.log(`Submit buttons found: ${submitButtons.length}`);
      
      // Check for error indicators
      const errorIndicators = data.match(/class="[^"]*error[^"]*"|class='[^']*error[^']*'|<div[^>]*error[^>]*>/gi) || [];
      console.log(`Error indicators found: ${errorIndicators.length}`);
      
      // Check page title
      const titleMatch = data.match(/<title>([^<]*)<\/title>/i);
      if (titleMatch) {
        console.log(`Page title: ${titleMatch[1]}`);
      }
      
      // Check for Blazor indicators
      const blazorIndicators = data.match(/blazor\.|_framework\/blazor|<app>|<component>/gi) || [];
      console.log(`Blazor indicators found: ${blazorIndicators.length}`);
      
      // Extract visible text content (rough estimate)
      const textContent = data
        .replace(/<script[^>]*>[\s\S]*?<\/script>/gi, '')
        .replace(/<style[^>]*>[\s\S]*?<\/style>/gi, '')
        .replace(/<[^>]+>/g, ' ')
        .replace(/\s+/g, ' ')
        .trim();
      
      console.log(`\nFirst 500 characters of visible text:`);
      console.log(textContent.substring(0, 500) + '...');
    });
  });
  
  req.on('error', (error) => {
    console.error('Error fetching page:', error);
  });
  
  req.end();
}

fetchLoginPage();