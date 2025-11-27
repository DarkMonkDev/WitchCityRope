import { APIRequestContext, Page } from '@playwright/test';

/**
 * Payment Test Helper - DATABASE-DRIVEN APPROACH
 *
 * Creates test payments by inserting TicketPurchase records directly into the database.
 * This provides TRUE test isolation - each test gets its own unique payment data.
 *
 * WHY THIS APPROACH:
 * - Creating payments via UI/API is complex (requires PayPal flow, events, tickets, etc.)
 * - Direct database insertion is faster and more reliable for E2E tests
 * - Each test gets a completely isolated payment - no shared state
 * - Tests can run in parallel without conflicts
 *
 * IMPLEMENTATION:
 * - Uses Playwright's request context to call a test-only admin endpoint
 * - Admin endpoint creates TicketPurchase with all required fields
 * - Supports PayPal, Cash, and Venmo payment methods
 * - Includes encrypted PayPal Capture IDs for refund testing
 *
 * Created: 2025-11-20
 * Related: Admin Variable Refund E2E tests
 */

// API base URL removed - use relative paths with Playwright's baseURL

export interface CreateTestPaymentOptions {
  /** Payment amount in USD */
  amount: number;
  /** Payment method */
  paymentMethod: 'PayPal' | 'Cash' | 'Venmo';
  /** Payment status (defaults to 'Completed') */
  paymentStatus?: 'Completed' | 'Pending' | 'Failed' | 'PartiallyRefunded';
  /** Optional notes for the payment */
  notes?: string;
  /** Unique event name for test isolation (defaults to auto-generated unique name) */
  eventName?: string;
}

export interface TestPaymentResult {
  /** TicketPurchase ID (primary key) */
  id: string;
  /** Transaction reference (PaymentReference field) - use this to find payment in UI */
  transactionId: string;
  /** Payment amount */
  amount: number;
  /** Payment method */
  paymentMethod: string;
  /** Payment status */
  paymentStatus: string;
  /** Event name for the payment */
  eventName: string;
}

/**
 * Payment Helper for creating isolated test payments
 */
export class PaymentHelper {
  /**
   * Create a test payment for use in E2E tests
   *
   * This creates a TicketPurchase record via a test admin endpoint.
   * The payment is fully isolated and won't conflict with other tests.
   *
   * USAGE:
   * ```typescript
   * const payment = await PaymentHelper.createTestPayment(page.request, {
   *   amount: 50.00,
   *   paymentMethod: 'PayPal',
   *   paymentStatus: 'Completed'
   * });
   *
   * // Use payment.transactionId to find it in the UI
   * const row = page.locator(`tr:has-text("${payment.transactionId}")`);
   * ```
   *
   * @param request - Playwright API request context (from page.request)
   * @param options - Payment creation options
   * @returns Payment details including ID and transaction reference
   */
  static async createTestPayment(
    request: APIRequestContext,
    options: CreateTestPaymentOptions
  ): Promise<TestPaymentResult> {
    const {
      amount,
      paymentMethod,
      paymentStatus = 'Completed',
      notes = `E2E Test Payment - ${new Date().toISOString()}`,
      eventName
    } = options;

    // Generate unique event name if not provided (prevents conflicts between tests)
    const uniqueEventName = eventName || `E2E-Test-Event-${Date.now()}-${Math.random().toString(36).substring(7)}`;

    // Create payment via test helper endpoint
    // The backend will generate IDs and handle encryption
    const payload = {
      totalPrice: amount,
      paymentMethod,
      paymentStatus,
      notes,
      quantity: 1,
      eventName: uniqueEventName,
      // Let backend handle PayPal capture ID encryption
      includePayPalCaptureId: paymentMethod === 'PayPal'
    };

    console.log(`📝 Creating test payment: $${amount} (${paymentMethod}) for event: ${uniqueEventName}`);

    try {
      // Try to create via test helper endpoint (using relative URL)
      const response = await request.post('/api/test-helpers/ticket-purchases', {
        data: payload,
        failOnStatusCode: false
      });

      if (response.ok()) {
        const result = await response.json();
        console.log(`✅ Test payment created: ${result.id}`);
        console.log(`   Transaction ID: ${result.paymentReference}`);
        console.log(`   Amount: $${result.totalPrice}`);
        console.log(`   Event: ${result.eventName || uniqueEventName}`);

        return {
          id: result.id,
          transactionId: result.paymentReference,
          amount: result.totalPrice,
          paymentMethod: result.paymentMethod,
          paymentStatus: result.paymentStatus,
          eventName: result.eventName || uniqueEventName
        };
      } else {
        const errorText = await response.text();
        throw new Error(`Test helper endpoint failed (${response.status()}): ${errorText}`);
      }
    } catch (error) {
      console.error(`❌ Failed to create test payment:`, error);
      throw error; // Re-throw instead of falling back - we need the real endpoint to work
    }
  }


  /**
   * Get auth token from page cookies
   *
   * Helper to extract JWT token from authenticated session.
   */
  static async getAuthToken(page: Page): Promise<string | null> {
    const cookies = await page.context().cookies();
    const authCookie = cookies.find(c => c.name === 'auth-token' || c.name === 'jwt' || c.name === '.AspNetCore.Cookies');

    if (authCookie) {
      return authCookie.value;
    }

    // Try to get from localStorage
    const token = await page.evaluate(() => {
      return localStorage.getItem('authToken') || sessionStorage.getItem('authToken');
    });

    return token;
  }

  /**
   * Verify payment exists and has correct data
   *
   * Useful for test assertions after creating a payment.
   */
  static async verifyPaymentExists(
    request: APIRequestContext,
    transactionId: string
  ): Promise<boolean> {
    try {
      const response = await request.get(`/api/admin/payments?search=${transactionId}`, {
        failOnStatusCode: false
      });

      if (!response.ok()) {
        return false;
      }

      const data = await response.json();
      const found = data.items?.some((p: any) =>
        p.transactionId === transactionId || p.paymentReference === transactionId
      );

      if (found) {
        console.log(`✅ Payment verified: ${transactionId}`);
      } else {
        console.log(`❌ Payment not found: ${transactionId}`);
      }

      return found;
    } catch (error) {
      console.error(`❌ Error verifying payment:`, error);
      return false;
    }
  }


  /**
   * Clean up test payment after test (optional)
   *
   * Use this in afterEach hooks if you want to remove test data.
   * Generally not needed since database is reset between test runs.
   */
  static async deleteTestPayment(
    request: APIRequestContext,
    ticketPurchaseId: string
  ): Promise<void> {
    try {
      const response = await request.delete(
        `/api/test-helpers/ticket-purchases/${ticketPurchaseId}`,
        { failOnStatusCode: false }
      );

      if (response.ok()) {
        console.log(`🧹 Deleted test payment: ${ticketPurchaseId}`);
      } else {
        console.warn(`⚠️ Could not delete test payment ${ticketPurchaseId}: ${response.status()}`);
      }
    } catch (error) {
      console.warn(`⚠️ Error deleting test payment:`, error);
    }
  }
}

/**
 * QUICK USAGE EXAMPLE:
 *
 * ```typescript
 * test('Refund a payment', async ({ page }) => {
 *   // 1. Login as admin
 *   await AuthHelper.loginAs(page, 'admin');
 *
 *   // 2. Create a test payment
 *   const payment = await PaymentHelper.createTestPayment(page.request, {
 *     amount: 50.00,
 *     paymentMethod: 'PayPal',
 *     paymentStatus: 'Completed'
 *   });
 *
 *   // 3. Navigate to payments page (using relative URL)
 *   await page.goto('/admin/analytics/payments');
 *
 *   // 4. Find the payment by transaction ID
 *   const row = page.locator(`tr:has-text("${payment.transactionId}")`);
 *   await expect(row).toBeVisible();
 *
 *   // 5. Click refund button
 *   await row.locator('button:has-text("Refund")').click();
 *
 *   // ... rest of test
 * });
 * ```
 */
