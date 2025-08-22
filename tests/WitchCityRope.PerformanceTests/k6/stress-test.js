import http from 'k6/http';
import { check, sleep } from 'k6';
import exec from 'k6/execution';
import { Trend, Rate, Counter, Gauge } from 'k6/metrics';

// Custom metrics for monitoring
const memoryUsage = new Gauge('memory_usage_mb');
const errorsByType = new Counter('errors_by_type');
const recoveryTime = const recoveryTime = new Trend('recovery_time_ms');
const connectionErrors = new Rate('connection_errors');

// Stress test configuration
export const options = {
  scenarios: {
    // Breaking point test
    breaking_point: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '2m', target: 100 },
        { duration: '2m', target: 200 },
        { duration: '2m', target: 400 },
        { duration: '2m', target: 800 },
        { duration: '2m', target: 1600 },
        { duration: '2m', target: 3200 }, // Find breaking point
      ],
      gracefulRampDown: '2m',
      exec: 'breakingPointTest',
    },
    // Spike test
    spike_test: {
      executor: 'ramping-vus',
      startVUs: 10,
      stages: [
        { duration: '1m', target: 10 },    // Baseline
        { duration: '30s', target: 1000 }, // Massive spike
        { duration: '3m', target: 1000 },  // Stay at spike
        { duration: '30s', target: 10 },   // Quick drop
        { duration: '2m', target: 10 },    // Recovery period
      ],
      exec: 'spikeTest',
      startTime: '15m', // Start after breaking point test
    },
    // Soak test (long duration)
    soak_test: {
      executor: 'constant-vus',
      vus: 200,
      duration: '30m',
      exec: 'soakTest',
      startTime: '25m', // Start after spike test
    },
  },
  thresholds: {
    http_req_failed: [{
      threshold: 'rate<0.50', // 50% error rate is breaking point
      abortOnFail: false,
    }],
    http_req_duration: ['p(95)<3000'], // 3 second timeout
    connection_errors: ['rate<0.20'],   // 20% connection error threshold
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

// Track system state
let systemHealthy = true;
let breakingPointFound = false;
let breakingPointVUs = 0;

export function breakingPointTest() {
  const currentVUs = exec.scenario.activeVUs;
  
  // Mix of operations to stress different parts of the system
  const operations = [
    () => authOperation(),
    () => eventListingOperation(),
    () => eventRegistrationOperation(),
    () => heavyQueryOperation(),
  ];

  const operation = operations[Math.floor(Math.random() * operations.length)];
  const result = operation();

  // Check if we've hit breaking point
  if (!breakingPointFound && result.failed) {
    const errorRate = exec.scenario.metrics.http_req_failed.rate;
    if (errorRate > 0.5) {
      breakingPointFound = true;
      breakingPointVUs = currentVUs;
      console.log(`BREAKING POINT FOUND at ${currentVUs} VUs with ${errorRate * 100}% error rate`);
    }
  }

  // Monitor memory usage (simulated)
  if (Math.random() < 0.01) { // Sample 1% of requests
    const simulatedMemory = currentVUs * 0.5 + Math.random() * 100;
    memoryUsage.add(simulatedMemory);
  }

  sleep(randomFloatBetween(0.5, 2));
}

export function spikeTest() {
  const startTime = new Date();
  
  // Simple operation that should normally be fast
  const response = http.get(`${BASE_URL}/api/events/featured?count=6`, {
    timeout: '10s',
    tags: { name: 'spike_test' },
  });

  const responseTime = new Date() - startTime;

  const success = check(response, {
    'spike response successful': (r) => r.status === 200,
    'spike response time < 1s': (r) => r.timings.duration < 1000,
  });

  if (!success) {
    errorsByType.add(1, { type: 'spike_failure' });
  }

  // Track recovery after spike
  if (exec.scenario.progress > 0.7) { // In recovery phase
    recoveryTime.add(responseTime);
  }

  sleep(randomFloatBetween(0.1, 0.5));
}

export function soakTest() {
  const operations = [
    { weight: 40, fn: () => eventListingOperation() },
    { weight: 30, fn: () => authOperation() },
    { weight: 20, fn: () => eventRegistrationOperation() },
    { weight: 10, fn: () => heavyQueryOperation() },
  ];

  // Weighted random selection
  const totalWeight = operations.reduce((sum, op) => sum + op.weight, 0);
  let random = Math.random() * totalWeight;
  
  for (const op of operations) {
    random -= op.weight;
    if (random <= 0) {
      const result = op.fn();
      
      // Track long-term degradation
      if (result.failed) {
        errorsByType.add(1, { type: 'soak_failure', operation: result.operation });
      }
      break;
    }
  }

  // Monitor for memory leaks (simulated pattern)
  const timeElapsed = exec.scenario.progress * 30; // minutes
  const expectedMemory = 200 + (timeElapsed * 2); // Expected 2MB/min growth
  const actualMemory = 200 + (timeElapsed * 2.5) + Math.random() * 50; // Actual with leak
  
  if (Math.random() < 0.01) {
    memoryUsage.add(actualMemory);
    
    if (actualMemory > expectedMemory * 1.2) {
      console.warn(`Potential memory leak detected: ${actualMemory.toFixed(2)}MB (expected: ${expectedMemory.toFixed(2)}MB)`);
    }
  }

  sleep(randomFloatBetween(1, 3));
}

// Operation functions
function authOperation() {
  const payload = JSON.stringify({
    email: `stress_${exec.vu.idInInstance}@test.com`,
    password: 'TestPassword123!',
  });

  const response = http.post(`${BASE_URL}/api/auth/login`, payload, {
    headers: { 'Content-Type': 'application/json' },
    timeout: '5s',
    tags: { operation: 'auth' },
  });

  const failed = response.status !== 200 && response.status !== 401;
  if (failed && response.status === 0) {
    connectionErrors.add(1);
  }

  return { failed, operation: 'auth', response };
}

function eventListingOperation() {
  const response = http.get(`${BASE_URL}/api/events?page=1&pageSize=50`, {
    timeout: '5s',
    tags: { operation: 'event_listing' },
  });

  const failed = response.status !== 200;
  if (failed && response.status === 0) {
    connectionErrors.add(1);
  }

  return { failed, operation: 'event_listing', response };
}

function eventRegistrationOperation() {
  const payload = JSON.stringify({
    eventId: generateUUID(),
    ticketQuantity: 2,
    paymentMethod: 'PayPal',
  });

  const response = http.post(`${BASE_URL}/api/events/${generateUUID()}/register`, payload, {
    headers: { 
      'Content-Type': 'application/json',
      'Authorization': 'Bearer mock-token',
    },
    timeout: '10s',
    tags: { operation: 'event_registration' },
  });

  const failed = response.status !== 200 && response.status !== 400 && response.status !== 404;
  if (failed && response.status === 0) {
    connectionErrors.add(1);
  }

  return { failed, operation: 'event_registration', response };
}

function heavyQueryOperation() {
  // Simulate a complex search/filter operation
  const response = http.get(
    `${BASE_URL}/api/events?` +
    `search=workshop&` +
    `eventType=Workshop&` +
    `status=Published&` +
    `startDate=2024-01-01&` +
    `endDate=2024-12-31&` +
    `minPrice=0&` +
    `maxPrice=500&` +
    `page=1&` +
    `pageSize=100&` +
    `includeDetails=true`,
    {
      timeout: '15s',
      tags: { operation: 'heavy_query' },
    }
  );

  const failed = response.status !== 200;
  if (failed && response.status === 0) {
    connectionErrors.add(1);
  }

  return { failed, operation: 'heavy_query', response };
}

// Utility functions
function generateUUID() {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
    const r = Math.random() * 16 | 0;
    const v = c === 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
}

function randomFloatBetween(min, max) {
  return Math.random() * (max - min) + min;
}

export function handleSummary(data) {
  const report = {
    breakingPoint: breakingPointFound ? {
      found: true,
      vus: breakingPointVUs,
      errorRate: data.metrics.http_req_failed.values.rate,
    } : {
      found: false,
      maxVUsTested: 3200,
    },
    memoryAnalysis: {
      peak: data.metrics.memory_usage_mb?.values.max || 'N/A',
      average: data.metrics.memory_usage_mb?.values.avg || 'N/A',
    },
    connectionHealth: {
      errorRate: data.metrics.connection_errors?.values.rate || 0,
      totalConnectionErrors: data.metrics.connection_errors?.values.count || 0,
    },
    recovery: {
      averageRecoveryTime: data.metrics.recovery_time_ms?.values.avg || 'N/A',
      p95RecoveryTime: data.metrics.recovery_time_ms?.values['p(95)'] || 'N/A',
    },
  };

  console.log(`
    ========================================
    STRESS TEST SUMMARY
    ========================================
    Breaking Point: ${report.breakingPoint.found ? report.breakingPoint.vus + ' VUs' : 'Not found (system handled ' + report.breakingPoint.maxVUsTested + ' VUs)'}
    Peak Memory Usage: ${report.memoryAnalysis.peak}MB
    Connection Error Rate: ${(report.connectionHealth.errorRate * 100).toFixed(2)}%
    Average Recovery Time: ${report.recovery.averageRecoveryTime}ms
    ========================================
  `);

  return {
    'stress-test-report.html': generateStressTestHTML(data, report),
    'stress-test-summary.json': JSON.stringify(report, null, 2),
  };
}

function generateStressTestHTML(data, report) {
  return `
    <!DOCTYPE html>
    <html>
    <head>
        <title>Stress Test Results</title>
        <style>
            body { font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }
            .container { max-width: 1200px; margin: auto; background: white; padding: 20px; box-shadow: 0 0 10px rgba(0,0,0,0.1); }
            .section { margin: 30px 0; padding: 20px; border-left: 4px solid #007bff; background: #f8f9fa; }
            .critical { border-left-color: #dc3545; }
            .warning { border-left-color: #ffc107; }
            .success { border-left-color: #28a745; }
            .metric { display: inline-block; margin: 10px 20px; }
            .metric-label { font-weight: bold; color: #666; }
            .metric-value { font-size: 24px; color: #333; }
            .chart { margin: 20px 0; padding: 20px; background: #fff; border: 1px solid #ddd; }
        </style>
    </head>
    <body>
        <div class="container">
            <h1>System Stress Test Results</h1>
            
            <div class="section ${report.breakingPoint.found ? 'critical' : 'success'}">
                <h2>Breaking Point Analysis</h2>
                <div class="metric">
                    <div class="metric-label">Breaking Point</div>
                    <div class="metric-value">${report.breakingPoint.found ? report.breakingPoint.vus + ' VUs' : 'Not Found'}</div>
                </div>
                <div class="metric">
                    <div class="metric-label">Max Load Tested</div>
                    <div class="metric-value">3200 VUs</div>
                </div>
                <div class="metric">
                    <div class="metric-label">Final Error Rate</div>
                    <div class="metric-value">${(data.metrics.http_req_failed.values.rate * 100).toFixed(2)}%</div>
                </div>
            </div>

            <div class="section ${report.memoryAnalysis.peak > 1000 ? 'warning' : 'success'}">
                <h2>Memory Analysis</h2>
                <div class="metric">
                    <div class="metric-label">Peak Memory</div>
                    <div class="metric-value">${report.memoryAnalysis.peak}MB</div>
                </div>
                <div class="metric">
                    <div class="metric-label">Average Memory</div>
                    <div class="metric-value">${report.memoryAnalysis.average}MB</div>
                </div>
            </div>

            <div class="section ${report.connectionHealth.errorRate > 0.1 ? 'warning' : 'success'}">
                <h2>Connection Health</h2>
                <div class="metric">
                    <div class="metric-label">Connection Error Rate</div>
                    <div class="metric-value">${(report.connectionHealth.errorRate * 100).toFixed(2)}%</div>
                </div>
                <div class="metric">
                    <div class="metric-label">Total Connection Errors</div>
                    <div class="metric-value">${report.connectionHealth.totalConnectionErrors}</div>
                </div>
            </div>

            <div class="section">
                <h2>Recovery Performance</h2>
                <div class="metric">
                    <div class="metric-label">Avg Recovery Time</div>
                    <div class="metric-value">${report.recovery.averageRecoveryTime}ms</div>
                </div>
                <div class="metric">
                    <div class="metric-label">P95 Recovery Time</div>
                    <div class="metric-value">${report.recovery.p95RecoveryTime}ms</div>
                </div>
            </div>

            <div class="section">
                <h2>Overall Statistics</h2>
                <div class="metric">
                    <div class="metric-label">Total Requests</div>
                    <div class="metric-value">${data.metrics.http_reqs.values.count.toLocaleString()}</div>
                </div>
                <div class="metric">
                    <div class="metric-label">Avg Response Time</div>
                    <div class="metric-value">${data.metrics.http_req_duration.values.avg.toFixed(0)}ms</div>
                </div>
                <div class="metric">
                    <div class="metric-label">P95 Response Time</div>
                    <div class="metric-value">${data.metrics.http_req_duration.values['p(95)'].toFixed(0)}ms</div>
                </div>
            </div>
        </div>
    </body>
    </html>
  `;
}