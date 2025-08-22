import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Rate, Trend } from 'k6/metrics';
import { randomString, randomItem } from 'https://jslib.k6.io/k6-utils/1.2.0/index.js';

// Custom metrics
const loginErrorRate = new Rate('login_errors');
const loginDuration = new Trend('login_duration');
const registrationErrorRate = new Rate('registration_errors');
const registrationDuration = new Trend('registration_duration');

// Test configuration
export const options = {
  scenarios: {
    // Login load test
    login_load: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 50 },   // Ramp up
        { duration: '2m', target: 100 },   // Stay at 100 users
        { duration: '30s', target: 200 },  // Spike
        { duration: '1m', target: 100 },   // Back to normal
        { duration: '30s', target: 0 },    // Ramp down
      ],
      gracefulRampDown: '30s',
      exec: 'loginScenario',
    },
    // Registration surge test
    registration_surge: {
      executor: 'ramping-arrival-rate',
      startRate: 10,
      timeUnit: '1s',
      preAllocatedVUs: 100,
      maxVUs: 500,
      stages: [
        { duration: '30s', target: 10 },   // Warm up
        { duration: '1m', target: 50 },    // Normal load
        { duration: '30s', target: 200 },  // Surge (flash sale)
        { duration: '2m', target: 200 },   // Sustained surge
        { duration: '1m', target: 50 },    // Cool down
      ],
      exec: 'registrationScenario',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<500', 'p(99)<1000'],
    http_req_failed: ['rate<0.1'],
    login_errors: ['rate<0.05'],
    registration_errors: ['rate<0.1'],
    login_duration: ['p(95)<300'],
    registration_duration: ['p(95)<500'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

// Pre-generated test users
const testUsers = Array.from({ length: 1000 }, (_, i) => ({
  email: `loadtest_${i}@test.com`,
  password: 'TestUser@123',
}));

export function setup() {
  // Setup code - could create test users here
  console.log(`Running tests against: ${BASE_URL}`);
  return { baseUrl: BASE_URL };
}

export function loginScenario() {
  group('Login Flow', () => {
    const user = randomItem(testUsers);
    const payload = JSON.stringify({
      email: user.email,
      password: user.password,
    });

    const params = {
      headers: {
        'Content-Type': 'application/json',
      },
      tags: { name: 'login' },
    };

    const startTime = new Date();
    const response = http.post(`${BASE_URL}/api/auth/login`, payload, params);
    const duration = new Date() - startTime;

    // Record custom metrics
    loginDuration.add(duration);
    loginErrorRate.add(response.status !== 200);

    // Checks
    const success = check(response, {
      'login status is 200': (r) => r.status === 200,
      'login has access token': (r) => {
        const body = JSON.parse(r.body);
        return body.accessToken !== undefined;
      },
      'login has refresh token': (r) => {
        const body = JSON.parse(r.body);
        return body.refreshToken !== undefined;
      },
      'login response time < 300ms': (r) => r.timings.duration < 300,
    });

    // If login successful, try to use the token
    if (success && response.status === 200) {
      const body = JSON.parse(response.body);
      const authParams = {
        headers: {
          'Authorization': `Bearer ${body.accessToken}`,
        },
      };

      // Make authenticated request
      const profileResponse = http.get(`${BASE_URL}/api/users/profile`, authParams);
      check(profileResponse, {
        'authenticated request succeeds': (r) => r.status === 200,
      });
    }

    sleep(randomIntBetween(1, 3));
  });
}

export function registrationScenario() {
  group('Registration Flow', () => {
    const uniqueId = `${Date.now()}_${randomString(8)}`;
    const payload = JSON.stringify({
      email: `newuser_${uniqueId}@test.com`,
      password: 'TestUser@123',
      confirmPassword: 'TestUser@123',
      sceneName: `User_${uniqueId}`,
      acceptedTerms: true,
    });

    const params = {
      headers: {
        'Content-Type': 'application/json',
      },
      tags: { name: 'registration' },
    };

    const startTime = new Date();
    const response = http.post(`${BASE_URL}/api/auth/register`, payload, params);
    const duration = new Date() - startTime;

    // Record custom metrics
    registrationDuration.add(duration);
    registrationErrorRate.add(response.status !== 200);

    // Checks
    check(response, {
      'registration status is 200': (r) => r.status === 200,
      'registration returns user id': (r) => {
        if (r.status !== 200) return false;
        const body = JSON.parse(r.body);
        return body.userId !== undefined;
      },
      'registration response time < 500ms': (r) => r.timings.duration < 500,
    });

    // Small delay between registrations
    sleep(randomIntBetween(0.5, 2));
  });
}

export function handleSummary(data) {
  return {
    'summary.html': htmlReport(data),
    'summary.json': JSON.stringify(data),
  };
}

// Helper function for random integers
function randomIntBetween(min, max) {
  return Math.floor(Math.random() * (max - min + 1) + min);
}

// Simple HTML report generator
function htmlReport(data) {
  return `
    <!DOCTYPE html>
    <html>
    <head>
        <title>Authentication Load Test Results</title>
        <style>
            body { font-family: Arial, sans-serif; margin: 20px; }
            .metric { margin: 10px 0; padding: 10px; background: #f0f0f0; }
            .pass { color: green; }
            .fail { color: red; }
        </style>
    </head>
    <body>
        <h1>Authentication Load Test Results</h1>
        <div class="metric">
            <h3>Summary</h3>
            <p>Total Requests: ${data.metrics.http_reqs.values.count}</p>
            <p>Failed Requests: ${data.metrics.http_req_failed.values.rate * 100}%</p>
            <p>Average Duration: ${data.metrics.http_req_duration.values.avg.toFixed(2)}ms</p>
        </div>
    </body>
    </html>
  `;
}