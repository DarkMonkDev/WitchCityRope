import { test, expect } from '@playwright/test';
import { setTimeout } from 'timers/promises';

// Configuration
const IS_CI = process.env.CI === 'true';
const API_URL = process.env.API_URL || 'http://localhost:5655';
const USE_MOCK_SERVICE = process.env.USE_MOCK_PAYMENT_SERVICE === 'true' || IS_CI;

test.describe('PayPal Integration Tests', () => {
  test.beforeEach(async ({ page }) => {
    // Setup mock responses for CI/automated environments
    if (USE_MOCK_SERVICE) {
      console.log('🤖 Using PayPal mock service for testing');
      
      // Mock PayPal order creation endpoint
      await page.route('**/api/payments/create-order', route => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            success: true,
            orderId: 'MOCK-ORDER-E2E-12345',
            status: 'CREATED',
            approveUrl: `${API_URL}/mock/paypal/approve?order=MOCK-ORDER-E2E-12345`,
            links: [
              { rel: 'approve', href: `${API_URL}/mock/paypal/approve?order=MOCK-ORDER-E2E-12345`, method: 'GET' },
              { rel: 'capture', href: `${API_URL}/api/payments/orders/MOCK-ORDER-E2E-12345/capture`, method: 'POST' }
            ]
          })
        });
      });

      // Mock PayPal order capture endpoint
      await page.route('**/api/payments/orders/*/capture', route => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            success: true,
            captureId: 'MOCK-CAPTURE-E2E-67890',
            orderId: 'MOCK-ORDER-E2E-12345',
            status: 'COMPLETED',
            amount: { value: '50.00', currency: 'USD' },
            payerEmail: 'test@example.com',
            payerName: 'E2E Test User',
            captureTime: new Date().toISOString()
          })
        });
      });

      // Mock webhook endpoint for testing
      await page.route('**/api/webhooks/paypal', route => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            received: true,
            eventType: 'PAYMENT.CAPTURE.COMPLETED',
            processed: true
          })
        });
      });

      // Mock webhook health check
      await page.route('**/api/webhooks/paypal/health', route => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            status: 'healthy',
            service: 'paypal-webhooks',
            timestamp: new Date().toISOString()
          })
        });
      });
    } else {
      console.log('💳 Using real PayPal sandbox for testing');
    }
  });

  test('should create PayPal order successfully', async ({ request }) => {
    // Test the order creation API directly
    const response = await request.post(`${API_URL}/api/payments/create-order`, {
      data: {
        amount: {
          value: '50.00',
          currency: 'USD'
        },
        customerId: 'e2e-test-customer-123',
        slidingScalePercentage: 25,
        metadata: {
          eventId: 'test-event-456',
          testType: 'e2e-integration'
        }
      },
      headers: {
        'Content-Type': 'application/json'
      }
    });

    expect(response.status()).toBe(200);
    
    const data = await response.json();
    expect(data).toHaveProperty('success', true);
    expect(data).toHaveProperty('orderId');
    expect(data).toHaveProperty('status', 'CREATED');
    expect(data).toHaveProperty('approveUrl');

    if (USE_MOCK_SERVICE) {
      expect(data.orderId).toMatch(/^MOCK-ORDER-/);
    } else {
      expect(data.orderId).toMatch(/^[A-Z0-9]{15,20}$/);
    }

    console.log(`✅ Order created: ${data.orderId}`);
  });

  test('should capture PayPal order successfully', async ({ request }) => {
    // First create an order
    const createResponse = await request.post(`${API_URL}/api/payments/create-order`, {
      data: {
        amount: { value: '25.00', currency: 'USD' },
        customerId: 'e2e-test-customer-789',
        slidingScalePercentage: 0
      }
    });

    expect(createResponse.status()).toBe(200);
    const createData = await createResponse.json();
    const orderId = createData.orderId;

    // Then capture the order
    const captureResponse = await request.post(`${API_URL}/api/payments/orders/${orderId}/capture`, {
      headers: {
        'Content-Type': 'application/json'
      }
    });

    expect(captureResponse.status()).toBe(200);
    
    const captureData = await captureResponse.json();
    expect(captureData).toHaveProperty('success', true);
    expect(captureData).toHaveProperty('captureId');
    expect(captureData).toHaveProperty('status', 'COMPLETED');
    expect(captureData.orderId).toBe(orderId);

    console.log(`✅ Order captured: ${captureData.captureId}`);
  });

  test('should process webhook successfully', async ({ request }) => {
    // Simulate a PayPal webhook payload
    const webhookPayload = {
      id: 'WH-E2E-TEST-12345',
      event_type: 'PAYMENT.CAPTURE.COMPLETED',
      create_time: new Date().toISOString(),
      resource: {
        id: 'CAPTURE-E2E-67890',
        status: 'COMPLETED',
        amount: {
          currency_code: 'USD',
          value: '50.00'
        },
        seller_receivable_breakdown: {
          gross_amount: { currency_code: 'USD', value: '50.00' },
          paypal_fee: { currency_code: 'USD', value: '1.75' },
          net_amount: { currency_code: 'USD', value: '48.25' }
        }
      }
    };

    const response = await request.post(`${API_URL}/api/webhooks/paypal`, {
      data: webhookPayload,
      headers: {
        'Content-Type': 'application/json',
        'PAYPAL-TRANSMISSION-ID': 'E2E-TEST-TRANSMISSION-123',
        'PAYPAL-TRANSMISSION-SIG': 'mock-signature-for-e2e-test',
        'PAYPAL-TRANSMISSION-TIME': new Date().toISOString(),
        'PAYPAL-CERT-ID': 'e2e-test-cert-id'
      }
    });

    expect(response.status()).toBe(200);
    
    const data = await response.json();
    expect(data).toHaveProperty('received', true);
    expect(data).toHaveProperty('eventType', 'PAYMENT.CAPTURE.COMPLETED');

    console.log('✅ Webhook processed successfully');
  });

  test('should handle webhook health check', async ({ request }) => {
    const response = await request.get(`${API_URL}/api/webhooks/paypal/health`);

    expect(response.status()).toBe(200);
    
    const data = await response.json();
    expect(data).toHaveProperty('status', 'healthy');
    expect(data).toHaveProperty('service', 'paypal-webhooks');
    expect(data).toHaveProperty('timestamp');

    console.log('✅ Webhook health check passed');
  });

  test('should handle payment errors gracefully', async ({ request }) => {
    // Test order creation with invalid data
    const response = await request.post(`${API_URL}/api/payments/create-order`, {
      data: {
        // Missing required fields to trigger error
        amount: null,
        customerId: '',
        slidingScalePercentage: 'invalid'
      }
    });

    // Should return an error but not crash
    expect([400, 422, 500]).toContain(response.status());
    
    const data = await response.json();
    expect(data).toHaveProperty('success', false);
    expect(data).toHaveProperty('error');

    console.log('✅ Payment error handled gracefully');
  });

  test('should handle webhook validation errors', async ({ request }) => {
    // Send webhook without required headers
    const response = await request.post(`${API_URL}/api/webhooks/paypal`, {
      data: { event_type: 'TEST_EVENT' },
      headers: {
        'Content-Type': 'application/json'
        // Missing PayPal signature headers
      }
    });

    expect(response.status()).toBe(400);
    
    const data = await response.json();
    expect(data).toHaveProperty('error');
    expect(data.error).toMatch(/signature/i);

    console.log('✅ Webhook validation error handled correctly');
  });

  test('should handle concurrent payment requests', async ({ request }) => {
    const concurrentRequests = 5;
    const promises = Array.from({ length: concurrentRequests }, (_, i) =>
      request.post(`${API_URL}/api/payments/create-order`, {
        data: {
          amount: { value: (10 + i).toString() + '.00', currency: 'USD' },
          customerId: `concurrent-test-${i}`,
          slidingScalePercentage: i * 10
        }
      })
    );

    const responses = await Promise.all(promises);
    
    // All requests should succeed
    for (const response of responses) {
      expect(response.status()).toBe(200);
      const data = await response.json();
      expect(data).toHaveProperty('success', true);
      expect(data).toHaveProperty('orderId');
    }

    console.log(`✅ ${concurrentRequests} concurrent payment requests handled successfully`);
  });

  test('should maintain performance under load', async ({ request }) => {
    const startTime = Date.now();
    const requestCount = USE_MOCK_SERVICE ? 20 : 5; // Fewer requests for real PayPal
    
    const promises = Array.from({ length: requestCount }, (_, i) =>
      request.post(`${API_URL}/api/payments/create-order`, {
        data: {
          amount: { value: '25.00', currency: 'USD' },
          customerId: `perf-test-${i}`,
          slidingScalePercentage: 25
        }
      })
    );

    const responses = await Promise.all(promises);
    const duration = Date.now() - startTime;
    
    // All requests should succeed
    for (const response of responses) {
      expect(response.status()).toBe(200);
    }

    // Performance assertion
    const maxDuration = USE_MOCK_SERVICE ? 5000 : 30000; // 5s for mock, 30s for real PayPal
    expect(duration).toBeLessThan(maxDuration);

    console.log(`✅ Performance test: ${requestCount} requests in ${duration}ms (under ${maxDuration}ms limit)`);
  });

  test('should handle different sliding scale percentages', async ({ request }) => {
    const slidingScales = [0, 25, 50, 75, 100];
    const baseAmount = 100.00;

    for (const scale of slidingScales) {
      const response = await request.post(`${API_URL}/api/payments/create-order`, {
        data: {
          amount: { 
            value: (baseAmount * (1 - scale / 100)).toFixed(2), 
            currency: 'USD' 
          },
          customerId: `sliding-scale-test-${scale}`,
          slidingScalePercentage: scale,
          metadata: {
            originalAmount: baseAmount.toString(),
            discountPercentage: scale.toString()
          }
        }
      });

      expect(response.status()).toBe(200);
      const data = await response.json();
      expect(data).toHaveProperty('success', true);
      expect(data).toHaveProperty('orderId');
    }

    console.log(`✅ All sliding scale percentages (${slidingScales.join(', ')}%) processed successfully`);
  });

  test('should validate environment configuration', async ({ request }) => {
    // Test that the service is properly configured for the current environment
    const testEndpoints = [
      { path: '/api/webhooks/paypal/health', expectedStatus: 200 },
    ];

    for (const endpoint of testEndpoints) {
      const response = await request.get(`${API_URL}${endpoint.path}`);
      expect(response.status()).toBe(endpoint.expectedStatus);
    }

    // Test environment detection
    if (USE_MOCK_SERVICE) {
      // In mock mode, orders should have MOCK prefix
      const response = await request.post(`${API_URL}/api/payments/create-order`, {
        data: {
          amount: { value: '10.00', currency: 'USD' },
          customerId: 'env-test-customer',
          slidingScalePercentage: 0
        }
      });

      expect(response.status()).toBe(200);
      const data = await response.json();
      expect(data.orderId).toMatch(/^MOCK-ORDER-/);
      
      console.log('✅ Environment correctly configured for mock service');
    } else {
      console.log('✅ Environment configured for real PayPal sandbox');
    }
  });

  test('should handle webhook retry scenarios', async ({ request }) => {
    const webhookPayload = {
      id: 'WH-RETRY-TEST-12345',
      event_type: 'PAYMENT.CAPTURE.COMPLETED',
      create_time: new Date().toISOString(),
      resource: {
        id: 'CAPTURE-RETRY-67890',
        status: 'COMPLETED',
        amount: { currency_code: 'USD', value: '30.00' }
      }
    };

    // Send the same webhook multiple times (simulating PayPal retries)
    for (let attempt = 1; attempt <= 3; attempt++) {
      const response = await request.post(`${API_URL}/api/webhooks/paypal`, {
        data: webhookPayload,
        headers: {
          'Content-Type': 'application/json',
          'PAYPAL-TRANSMISSION-ID': `RETRY-TEST-TRANSMISSION-${attempt}`,
          'PAYPAL-TRANSMISSION-SIG': `retry-signature-${attempt}`,
          'PAYPAL-TRANSMISSION-TIME': new Date().toISOString()
        }
      });

      expect(response.status()).toBe(200);
      
      const data = await response.json();
      expect(data).toHaveProperty('received', true);
      
      // Small delay between retries
      await setTimeout(100);
    }

    console.log('✅ Webhook retry scenarios handled successfully');
  });
});

test.describe('PayPal Error Scenarios', () => {
  test('should handle service unavailable gracefully', async ({ request }) => {
    // This test would be more relevant with real PayPal, but we can simulate it
    const response = await request.post(`${API_URL}/api/payments/create-order`, {
      data: {
        amount: { value: 'invalid-amount', currency: 'INVALID' },
        customerId: 'error-test-customer',
        slidingScalePercentage: -1 // Invalid percentage
      }
    });

    // Should handle the error gracefully without crashing
    expect([400, 422, 500]).toContain(response.status());
    
    if (response.status() !== 500) {
      const data = await response.json();
      expect(data).toHaveProperty('success', false);
    }

    console.log('✅ Service errors handled gracefully');
  });

  test('should validate request data properly', async ({ request }) => {
    const invalidRequests = [
      {
        description: 'negative amount',
        data: {
          amount: { value: '-50.00', currency: 'USD' },
          customerId: 'test-customer',
          slidingScalePercentage: 0
        }
      },
      {
        description: 'invalid currency',
        data: {
          amount: { value: '50.00', currency: 'XXX' },
          customerId: 'test-customer',
          slidingScalePercentage: 0
        }
      },
      {
        description: 'invalid sliding scale',
        data: {
          amount: { value: '50.00', currency: 'USD' },
          customerId: 'test-customer',
          slidingScalePercentage: 150 // Over 100%
        }
      }
    ];

    for (const { description, data } of invalidRequests) {
      const response = await request.post(`${API_URL}/api/payments/create-order`, { data });
      
      expect([400, 422]).toContain(response.status());
      console.log(`✅ ${description} - validation error handled correctly`);
    }
  });
});