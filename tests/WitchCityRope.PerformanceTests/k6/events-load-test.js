import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { SharedArray } from 'k6/data';
import { Rate, Trend, Counter } from 'k6/metrics';
import { randomItem } from 'https://jslib.k6.io/k6-utils/1.2.0/index.js';

// Custom metrics
const eventListingDuration = new Trend('event_listing_duration');
const eventRegistrationDuration = new Trend('event_registration_duration');
const eventRegistrationErrors = new Rate('event_registration_errors');
const successfulRegistrations = new Counter('successful_registrations');

// Test configuration
export const options = {
  scenarios: {
    // Regular event browsing
    event_browsing: {
      executor: 'constant-vus',
      vus: 100,
      duration: '5m',
      exec: 'browsingScenario',
    },
    // Featured events (homepage)
    featured_events: {
      executor: 'constant-arrival-rate',
      rate: 200,
      timeUnit: '1s',
      duration: '5m',
      preAllocatedVUs: 50,
      maxVUs: 100,
      exec: 'featuredEventsScenario',
    },
    // Flash sale simulation
    flash_sale: {
      executor: 'ramping-arrival-rate',
      startRate: 10,
      timeUnit: '1s',
      preAllocatedVUs: 200,
      maxVUs: 1000,
      stages: [
        { duration: '30s', target: 10 },    // Pre-sale
        { duration: '10s', target: 500 },   // Sale starts - massive spike
        { duration: '2m', target: 300 },    // Sustained high load
        { duration: '30s', target: 100 },   // Tapering off
        { duration: '30s', target: 10 },    // Back to normal
      ],
      exec: 'flashSaleScenario',
      startTime: '60s', // Start after 1 minute
    },
  },
  thresholds: {
    http_req_duration: {
      'p(95)<500': ['browsingScenario'],
      'p(95)<200': ['featuredEventsScenario'],
      'p(95)<1000': ['flashSaleScenario'],
    },
    http_req_failed: ['rate<0.05'],
    event_registration_errors: ['rate<0.1'],
    'http_req_duration{scenario:featured_events}': ['p(99)<300'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

// Shared test data
const eventIds = new SharedArray('events', function () {
  // In real test, these would be fetched from API
  return Array.from({ length: 50 }, () => ({
    id: generateUUID(),
    capacity: 100,
    price: Math.floor(Math.random() * 100) + 20,
  }));
});

const authTokens = new SharedArray('tokens', function () {
  // In real test, these would be obtained from login
  return Array.from({ length: 200 }, (_, i) => `Bearer test-token-${i}`);
});

export function browsingScenario() {
  group('Event Browsing', () => {
    // Different search patterns
    const searchPatterns = [
      '',
      '?page=1&pageSize=20',
      '?page=2&pageSize=20&eventType=Workshop',
      '?search=rope&page=1',
      '?startDate=2024-01-01&endDate=2024-12-31',
      '?eventType=Social&status=Published',
    ];

    const query = randomItem(searchPatterns);
    const startTime = new Date();
    
    const response = http.get(`${BASE_URL}/api/events${query}`, {
      tags: { name: 'event_listing' },
    });
    
    eventListingDuration.add(new Date() - startTime);

    check(response, {
      'listing status is 200': (r) => r.status === 200,
      'listing returns events array': (r) => {
        const body = JSON.parse(r.body);
        return Array.isArray(body.events);
      },
      'listing has pagination info': (r) => {
        const body = JSON.parse(r.body);
        return body.totalCount !== undefined && body.page !== undefined;
      },
    });

    // Simulate user reading event list
    sleep(randomFloatBetween(2, 5));

    // Sometimes view event details
    if (Math.random() < 0.3) {
      const event = randomItem(eventIds);
      const detailResponse = http.get(`${BASE_URL}/api/events/${event.id}`, {
        tags: { name: 'event_detail' },
      });

      check(detailResponse, {
        'detail status is 200': (r) => r.status === 200,
      });

      sleep(randomFloatBetween(3, 8));
    }
  });
}

export function featuredEventsScenario() {
  const response = http.get(`${BASE_URL}/api/events/featured?count=6`, {
    tags: { name: 'featured_events' },
  });

  check(response, {
    'featured events status is 200': (r) => r.status === 200,
    'featured events returns 6 items': (r) => {
      const body = JSON.parse(r.body);
      return body.events && body.events.length === 6;
    },
    'featured events response < 200ms': (r) => r.timings.duration < 200,
  });
}

export function flashSaleScenario() {
  group('Flash Sale Registration', () => {
    const event = randomItem(eventIds);
    const token = randomItem(authTokens);
    
    const payload = JSON.stringify({
      eventId: event.id,
      ticketQuantity: randomIntBetween(1, 2),
      paymentMethod: 'PayPal',
    });

    const params = {
      headers: {
        'Content-Type': 'application/json',
        'Authorization': token,
      },
      tags: { name: 'event_registration' },
    };

    const startTime = new Date();
    const response = http.post(
      `${BASE_URL}/api/events/${event.id}/register`,
      payload,
      params
    );
    const duration = new Date() - startTime;

    eventRegistrationDuration.add(duration);
    eventRegistrationErrors.add(response.status !== 200);

    const success = check(response, {
      'registration successful': (r) => r.status === 200,
      'registration returns confirmation': (r) => {
        if (r.status !== 200) return false;
        const body = JSON.parse(r.body);
        return body.registrationId !== undefined;
      },
    });

    if (success) {
      successfulRegistrations.add(1);
    }

    // Quick retry if failed due to capacity
    if (response.status === 409) { // Conflict - sold out
      sleep(randomFloatBetween(0.5, 1));
      
      // Try another event
      const alternativeEvent = randomItem(eventIds);
      const retryPayload = JSON.stringify({
        eventId: alternativeEvent.id,
        ticketQuantity: 1,
        paymentMethod: 'PayPal',
      });

      http.post(
        `${BASE_URL}/api/events/${alternativeEvent.id}/register`,
        retryPayload,
        params
      );
    }
  });
}

// Utility functions
function generateUUID() {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
    const r = Math.random() * 16 | 0;
    const v = c === 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
}

function randomIntBetween(min, max) {
  return Math.floor(Math.random() * (max - min + 1) + min);
}

function randomFloatBetween(min, max) {
  return Math.random() * (max - min) + min;
}

export function handleSummary(data) {
  const flashSaleStats = data.metrics.successful_registrations 
    ? {
        total: data.metrics.successful_registrations.values.count,
        rate: data.metrics.successful_registrations.values.rate,
      }
    : { total: 0, rate: 0 };

  console.log(`
    ====================================
    Flash Sale Results:
    Total Successful Registrations: ${flashSaleStats.total}
    Registration Rate: ${flashSaleStats.rate.toFixed(2)}/s
    ====================================
  `);

  return {
    'events-summary.html': generateHTMLReport(data),
    'events-summary.json': JSON.stringify(data, null, 2),
  };
}

function generateHTMLReport(data) {
  return `
    <!DOCTYPE html>
    <html>
    <head>
        <title>Events Load Test Results</title>
        <style>
            body { font-family: Arial, sans-serif; margin: 20px; }
            .scenario { margin: 20px 0; padding: 15px; border: 1px solid #ddd; }
            .metric { margin: 10px 0; }
            .pass { color: green; }
            .fail { color: red; }
            table { border-collapse: collapse; width: 100%; }
            th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
        </style>
    </head>
    <body>
        <h1>Events Performance Test Results</h1>
        
        <div class="scenario">
            <h2>Event Browsing</h2>
            <div class="metric">Average Duration: ${data.metrics.event_listing_duration?.values.avg.toFixed(2) || 'N/A'}ms</div>
            <div class="metric">P95 Duration: ${data.metrics.event_listing_duration?.values['p(95)'].toFixed(2) || 'N/A'}ms</div>
        </div>

        <div class="scenario">
            <h2>Flash Sale Performance</h2>
            <div class="metric">Successful Registrations: ${data.metrics.successful_registrations?.values.count || 0}</div>
            <div class="metric">Registration Error Rate: ${(data.metrics.event_registration_errors?.values.rate * 100 || 0).toFixed(2)}%</div>
            <div class="metric">Average Registration Time: ${data.metrics.event_registration_duration?.values.avg.toFixed(2) || 'N/A'}ms</div>
        </div>

        <div class="scenario">
            <h2>Overall Performance</h2>
            <div class="metric">Total HTTP Requests: ${data.metrics.http_reqs.values.count}</div>
            <div class="metric">Request Failure Rate: ${(data.metrics.http_req_failed.values.rate * 100).toFixed(2)}%</div>
            <div class="metric">Average Response Time: ${data.metrics.http_req_duration.values.avg.toFixed(2)}ms</div>
        </div>
    </body>
    </html>
  `;
}