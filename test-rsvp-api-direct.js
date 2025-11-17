/**
 * Direct API Test for RSVP Persistence Bug Investigation
 *
 * This script calls the RSVP API directly to trigger the diagnostic logging
 * added by backend-developer, bypassing the UI navigation issues.
 */

const https = require('http');

// Test configuration
const API_BASE = 'http://localhost:5655';
const WEB_BASE = 'http://localhost:5173';
const TEST_USER = {
  email: 'vetted@witchcityrope.com',
  password: 'Test123!'
};

// We'll need to get a real event ID from the database
const EVENT_ID = 'ad3d61af-a5bb-46d3-81e0-3af6fd5ebd05'; // From previous test output

async function makeRequest(url, options = {}) {
  return new Promise((resolve, reject) => {
    const urlObj = new URL(url);
    const reqOptions = {
      hostname: urlObj.hostname,
      port: urlObj.port,
      path: urlObj.pathname + urlObj.search,
      method: options.method || 'GET',
      headers: {
        'Content-Type': 'application/json',
        ...options.headers
      }
    };

    const req = https.request(reqOptions, (res) => {
      let data = '';
      res.on('data', (chunk) => data += chunk);
      res.on('end', () => {
        const cookies = res.headers['set-cookie'];
        resolve({
          status: res.statusCode,
          headers: res.headers,
          data: data,
          cookies: cookies
        });
      });
    });

    req.on('error', reject);

    if (options.body) {
      req.write(JSON.stringify(options.body));
    }

    req.end();
  });
}

async function login() {
  console.log('🔐 Logging in as', TEST_USER.email);

  const response = await makeRequest(`${API_BASE}/api/auth/login`, {
    method: 'POST',
    body: {
      emailOrScenename: TEST_USER.email,
      password: TEST_USER.password
    }
  });

  if (response.status !== 200) {
    throw new Error(`Login failed: ${response.status} ${response.data}`);
  }

  const authCookie = response.cookies?.find(c => c.startsWith('.AspNetCore.'));
  if (!authCookie) {
    throw new Error('No authentication cookie received');
  }

  console.log('✅ Login successful');
  return authCookie.split(';')[0]; // Get just the cookie value
}

async function createRSVP(authCookie, eventId) {
  console.log('\n📝 Creating RSVP for event:', eventId);

  const response = await makeRequest(`${API_BASE}/api/events/${eventId}/rsvp`, {
    method: 'POST',
    headers: {
      'Cookie': authCookie
    },
    body: {
      eventId: eventId,
      notes: 'API test for diagnostic logging'
    }
  });

  console.log(`\n📊 API Response: ${response.status}`);
  console.log('Response data:', response.data);

  return response;
}

async function main() {
  try {
    console.log('='.repeat(60));
    console.log('RSVP PERSISTENCE BUG - DIRECT API TEST');
    console.log('='.repeat(60));
    console.log('');
    console.log('This test will:');
    console.log('1. Login as vetted user');
    console.log('2. Call POST /api/events/{id}/rsvp');
    console.log('3. Trigger diagnostic logging in API container');
    console.log('');
    console.log('After this runs, check API logs with:');
    console.log('  docker logs witchcityrope-api-1 --tail=50');
    console.log('');
    console.log('='.repeat(60));
    console.log('');

    // Step 1: Login
    const authCookie = await login();

    // Step 2: Create RSVP
    await createRSVP(authCookie, EVENT_ID);

    console.log('\n✅ Test complete!');
    console.log('\n📋 Next steps:');
    console.log('  1. Check API logs: docker logs witchcityrope-api-1 --tail=50');
    console.log('  2. Look for "DIAGNOSTIC:" log lines');
    console.log('  3. Check if SaveChangesAsync succeeded');
    console.log('  4. Check database for new RSVP record');
    console.log('');

  } catch (error) {
    console.error('\n❌ Test failed:', error.message);
    process.exit(1);
  }
}

main();
