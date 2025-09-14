import { test, expect } from '@playwright/test';

// Detect if running in CI environment
const IS_CI = process.env.CI === 'true';
const API_URL = process.env.API_URL || 'http://localhost:5655';

test.describe('Payment Flow', () => {
  // Configure mocks for CI environment
  if (IS_CI) {
    test.beforeEach(async ({ page }) => {
      console.log('🤖 Running in CI mode - using mocked PayPal responses');
      
      // Mock PayPal order creation
      await page.route('**/api/payments/create-order', route => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            orderId: 'MOCK-ORDER-CI-123',
            status: 'CREATED',
            approveUrl: `${API_URL}/mock/paypal/approve?order=MOCK-ORDER-CI-123`
          })
        });
      });

      // Mock PayPal order capture
      await page.route('**/api/payments/capture/*', route => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            status: 'COMPLETED',
            paymentId: 'MOCK-PAYMENT-CI-123',
            captureId: 'MOCK-CAPTURE-CI-123',
            amount: { value: '50.00', currency: 'USD' }
          })
        });
      });

      // Mock webhook verification
      await page.route('**/api/webhooks/paypal', route => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ verified: true })
        });
      });
    });
  }

  test('should complete payment flow', async ({ page }) => {
    // Navigate to event payment page
    await page.goto('/events/test-event/payment');
    
    // Select sliding scale percentage
    await page.getByTestId('sliding-scale-25').click();
    
    // Verify price update
    await expect(page.getByTestId('payment-amount')).toContainText('37.50');
    
    if (!IS_CI) {
      // Real PayPal flow (development/staging)
      console.log('💳 Using real PayPal sandbox');
      
      // Click PayPal button
      await page.getByTestId('paypal-button').click();
      
      // Handle PayPal redirect (would need actual sandbox credentials)
      // This part would interact with real PayPal sandbox
    } else {
      // Mocked flow for CI
      console.log('🤖 Using mocked PayPal for CI');
      
      // Fill mock card details
      await page.fill('[data-testid="cardholder-name"]', 'CI Test User');
      await page.fill('[data-testid="card-number"]', '4111111111111111');
      await page.fill('[data-testid="expiry"]', '12/25');
      await page.fill('[data-testid="cvv"]', '123');
      
      // Click pay button
      await page.getByTestId('pay-button').click();
    }
    
    // Wait for success page
    await page.waitForURL('**/payment/success');
    
    // Verify success message
    await expect(page.getByTestId('payment-success')).toBeVisible();
    await expect(page.getByTestId('payment-success')).toContainText('Payment successful');
    
    // Verify confirmation details
    await expect(page.getByTestId('confirmation-number')).toBeVisible();
    
    if (IS_CI) {
      // Verify mock values in CI
      await expect(page.getByTestId('payment-id')).toContainText('MOCK-PAYMENT-CI-123');
    }
  });

  test('should handle payment failure gracefully', async ({ page }) => {
    if (IS_CI) {
      // Mock a failed payment
      await page.route('**/api/payments/capture/*', route => {
        route.fulfill({
          status: 400,
          contentType: 'application/json',
          body: JSON.stringify({
            error: 'PAYMENT_DECLINED',
            message: 'Payment was declined by processor'
          })
        });
      });
    }
    
    await page.goto('/events/test-event/payment');
    
    // Try to complete payment
    if (IS_CI) {
      await page.fill('[data-testid="cardholder-name"]', 'Declined Card');
      await page.getByTestId('pay-button').click();
    } else {
      // Real PayPal test for declined payment
      await page.getByTestId('paypal-button').click();
      // Would need to use test card that triggers decline
    }
    
    // Verify error message is shown
    await expect(page.getByTestId('payment-error')).toBeVisible();
    await expect(page.getByTestId('payment-error')).toContainText('Payment failed');
  });

  test('should validate sliding scale selection', async ({ page }) => {
    await page.goto('/events/test-event/payment');
    
    // Try to proceed without selecting sliding scale
    await page.getByTestId('continue-to-payment').click();
    
    // Should show validation error
    await expect(page.getByTestId('sliding-scale-error')).toBeVisible();
    await expect(page.getByTestId('sliding-scale-error')).toContainText('Please select a payment option');
    
    // Select an option
    await page.getByTestId('sliding-scale-0').click();
    
    // Error should disappear
    await expect(page.getByTestId('sliding-scale-error')).not.toBeVisible();
    
    // Should be able to continue
    await page.getByTestId('continue-to-payment').click();
    await expect(page.getByTestId('payment-form')).toBeVisible();
  });

  test('should handle webhook processing', async ({ page }) => {
    if (!IS_CI) {
      test.skip(); // Skip in real environments as webhooks are async
    }
    
    // Mock webhook processing
    await page.route('**/api/test/webhook-status/*', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          processed: true,
          status: 'COMPLETED',
          timestamp: new Date().toISOString()
        })
      });
    });
    
    await page.goto('/events/test-event/payment');
    
    // Complete mock payment
    await page.fill('[data-testid="cardholder-name"]', 'Webhook Test');
    await page.getByTestId('pay-button').click();
    
    // Wait for webhook confirmation
    await page.waitForSelector('[data-testid="webhook-confirmed"]', { timeout: 10000 });
    
    // Verify webhook was processed
    await expect(page.getByTestId('webhook-confirmed')).toContainText('Payment confirmed');
  });
});

// Helper function to load webhook fixtures
async function loadWebhookFixture(fixtureName: string) {
  const fs = require('fs').promises;
  const path = require('path');
  
  const fixturePath = path.join(
    __dirname,
    '..',
    'fixtures',
    'paypal-webhooks',
    `${fixtureName}.json`
  );
  
  const content = await fs.readFile(fixturePath, 'utf-8');
  return JSON.parse(content);
}

test.describe('Webhook Processing (CI Only)', () => {
  test.skip(() => !IS_CI, 'Webhook tests only run in CI');
  
  test('should process payment completed webhook', async ({ request }) => {
    const webhook = await loadWebhookFixture('payment-completed');
    
    const response = await request.post(`${API_URL}/api/webhooks/paypal`, {
      data: webhook,
      headers: {
        'Content-Type': 'application/json',
        'PAYPAL-TRANSMISSION-ID': 'TEST-TRANSMISSION-123',
        'PAYPAL-TRANSMISSION-TIME': new Date().toISOString(),
        'PAYPAL-TRANSMISSION-SIG': 'mock-signature'
      }
    });
    
    expect(response.status()).toBe(200);
    
    const body = await response.json();
    expect(body).toMatchObject({
      processed: true,
      eventType: 'PAYMENT.CAPTURE.COMPLETED'
    });
  });
  
  test('should process refund webhook', async ({ request }) => {
    const webhook = await loadWebhookFixture('payment-refunded');
    
    const response = await request.post(`${API_URL}/api/webhooks/paypal`, {
      data: webhook,
      headers: {
        'Content-Type': 'application/json',
        'PAYPAL-TRANSMISSION-ID': 'TEST-TRANSMISSION-456',
        'PAYPAL-TRANSMISSION-TIME': new Date().toISOString(),
        'PAYPAL-TRANSMISSION-SIG': 'mock-signature'
      }
    });
    
    expect(response.status()).toBe(200);
    
    const body = await response.json();
    expect(body).toMatchObject({
      processed: true,
      eventType: 'PAYMENT.CAPTURE.REFUNDED'
    });
  });
});