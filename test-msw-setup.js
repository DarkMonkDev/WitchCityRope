// Quick test script to verify MSW setup
// Run with: node test-msw-setup.js

const http = require('http');

async function testEndpoint(url, method = 'GET', data = null) {
  return new Promise((resolve, reject) => {
    const options = {
      hostname: 'localhost',
      port: 5173,
      path: url,
      method: method,
      headers: {
        'Content-Type': 'application/json',
      },
    };

    const req = http.request(options, (res) => {
      let body = '';
      res.on('data', (chunk) => {
        body += chunk;
      });
      res.on('end', () => {
        resolve({
          status: res.statusCode,
          headers: res.headers,
          body: body,
        });
      });
    });

    req.on('error', (err) => {
      reject(err);
    });

    if (data) {
      req.write(JSON.stringify(data));
    }
    req.end();
  });
}

async function runTests() {
  console.log('🧪 Testing MSW Setup...\n');

  try {
    // Test 1: Homepage loads (infinite loop check)
    console.log('1. Testing homepage load (infinite loop check)...');
    const homeResponse = await testEndpoint('/');
    console.log(`   Status: ${homeResponse.status}`);
    console.log(`   Content: ${homeResponse.body.includes('WitchCityRope') ? '✅ Contains expected content' : '❌ Missing expected content'}`);

    // Test 2: MSW Profile endpoint
    console.log('\n2. Testing MSW Protected profile endpoint...');
    const profileResponse = await testEndpoint('/api/Protected/profile');
    console.log(`   Status: ${profileResponse.status}`);
    if (profileResponse.body) {
      try {
        const data = JSON.parse(profileResponse.body);
        console.log(`   MSW Response: ${data.sceneName ? '✅ Mock data returned' : '❌ Unexpected response structure'}`);
        console.log(`   User: ${data.sceneName} (${data.email})`);
      } catch (e) {
        console.log(`   Body: ${profileResponse.body.substring(0, 100)}...`);
      }
    }

    // Test 3: MSW Login endpoint
    console.log('\n3. Testing MSW login endpoint...');
    const loginResponse = await testEndpoint('/api/Auth/login', 'POST', {
      email: 'admin@witchcityrope.com',
      password: 'Test123!'
    });
    console.log(`   Status: ${loginResponse.status}`);
    if (loginResponse.body) {
      try {
        const data = JSON.parse(loginResponse.body);
        console.log(`   Login Success: ${data.success ? '✅ Login successful' : '❌ Login failed'}`);
        console.log(`   User: ${data.user?.sceneName} (${data.user?.email})`);
      } catch (e) {
        console.log(`   Body: ${loginResponse.body.substring(0, 100)}...`);
      }
    }

    console.log('\n🎉 MSW tests completed!');
  } catch (error) {
    console.error('❌ Test failed:', error.message);
  }
}

runTests();