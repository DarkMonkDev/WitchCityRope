#!/usr/bin/env node

const https = require('https');

// GitHub token from the config (we'll update this with instructions)
const GITHUB_TOKEN = process.env.GITHUB_TOKEN || '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';

console.log('Testing GitHub API access...\n');

// Test basic API access
const options = {
  hostname: 'api.github.com',
  path: '/user',
  method: 'GET',
  headers: {
    'Authorization': `token ${GITHUB_TOKEN}`,
    'User-Agent': 'MCP-GitHub-Test',
    'Accept': 'application/vnd.github.v3+json'
  }
};

const req = https.request(options, (res) => {
  let data = '';
  
  res.on('data', (chunk) => {
    data += chunk;
  });
  
  res.on('end', () => {
    console.log(`Status Code: ${res.statusCode}`);
    console.log(`Headers: ${JSON.stringify(res.headers, null, 2)}\n`);
    
    try {
      const parsed = JSON.parse(data);
      
      if (res.statusCode === 200) {
        console.log('✅ GitHub API access successful!');
        console.log(`Authenticated as: ${parsed.login}`);
        console.log(`Name: ${parsed.name || 'Not set'}`);
        console.log(`Public repos: ${parsed.public_repos}`);
        console.log(`Created: ${parsed.created_at}`);
        
        // Check rate limit
        console.log('\nRate Limit Info:');
        console.log(`Limit: ${res.headers['x-ratelimit-limit']}`);
        console.log(`Remaining: ${res.headers['x-ratelimit-remaining']}`);
        console.log(`Reset: ${new Date(res.headers['x-ratelimit-reset'] * 1000).toLocaleString()}`);
      } else if (res.statusCode === 401) {
        console.log('❌ Authentication failed!');
        console.log('The GitHub token is invalid or expired.');
        console.log('\nTo fix this:');
        console.log('1. Go to https://github.com/settings/tokens');
        console.log('2. Generate a new personal access token');
        console.log('3. Update the token in claude_desktop_config.json');
        console.log(`\nError message: ${parsed.message}`);
      } else {
        console.log(`❌ Unexpected status code: ${res.statusCode}`);
        console.log(`Response: ${JSON.stringify(parsed, null, 2)}`);
      }
    } catch (e) {
      console.log('❌ Failed to parse response');
      console.log(`Raw response: ${data}`);
    }
  });
});

req.on('error', (e) => {
  console.error(`❌ Request failed: ${e.message}`);
});

req.end();