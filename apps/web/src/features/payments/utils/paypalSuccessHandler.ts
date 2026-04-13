/**
 * PayPal success handler — extracted from EventPaymentPage for unit testability.
 *
 * History (2026-04-12): the inline version of this handler inside EventPaymentPage.tsx
 * accidentally had its `setCurrentStep(2)` call dropped during the BE-15 edit. The bug
 * shipped to staging and was caught only when SafetyFirst clicked through a real PayPal
 * sandbox payment and reported the checkout page being stuck despite the payment
 * succeeding on the backend. Extracting the handler here lets us write a focused unit
 * test that asserts each required side-effect fires — specifically the navigation to
 * the Confirmation step, which the regression silently removed.
 *
 * See BE-15 and T-8 in /docs/technical-debt.md for the trail.
 */

import type { PayPalCheckoutResult } from '../components/PayPalButton';

/**
 * Shape of the `completedPayment` state used by the Confirmation step renderer.
 * Mirrors the response contract that the credit-card path populates, so the
 * Confirmation screen can render from either payment method without branching.
 */
export interface CompletedPaymentState {
    id: string;
    transactionId: string;
    amount: number;
    currency: string;
    status: string;
    paymentMethod: string;
    paymentMethodType: number;
    confirmationNumber?: string;
    slidingScalePercentage: number;
    createdAt: string;
}

/**
 * Dependencies injected by the component. Using an explicit deps object (instead of
 * importing from React or component context) makes this trivially unit-testable with
 * `vi.fn()` spies.
 */
export interface PayPalSuccessDeps {
    setCompletedPayment: (state: CompletedPaymentState) => void;
    setIdempotencyKey: (key: string) => void;
    showNotification: (notification: {
        title: string;
        message: string;
        color: string;
        autoClose: number;
    }) => void;
    setCurrentStep: (step: number) => void;
    /**
     * Idempotency-key factory. Injected so tests can produce a deterministic key
     * without pulling in `crypto.randomUUID`. Defaults to the same generator the
     * component uses in production.
     */
    generateIdempotencyKey?: () => string;
}

function defaultGenerateIdempotencyKey(): string {
    return `WCR-${crypto.randomUUID().replace(/-/g, '').substring(0, 32)}`;
}

/**
 * Handle a successful PayPal checkout capture.
 *
 * Required side-effects, all of which are covered by the unit test:
 *   1. Persist the completed-payment data (drives the Confirmation screen content)
 *   2. Regenerate the idempotency key (a fresh key for any future attempt on this page)
 *   3. Show a success toast
 *   4. Advance the checkout stepper to Step 2 (Confirmation)  ← the regressed line
 *
 * If any of these stops firing, the Confirmation screen won't appear correctly and
 * the user will believe their payment failed. The unit test guards against all four.
 */
export function handlePayPalSuccess(
    result: PayPalCheckoutResult,
    deps: PayPalSuccessDeps,
): void {
    const generate = deps.generateIdempotencyKey ?? defaultGenerateIdempotencyKey;

    // 1. Persist completed payment data for the Confirmation screen.
    deps.setCompletedPayment({
        id: result.captureId,
        transactionId: result.captureId,
        amount: parseFloat(result.amount),
        currency: result.currency,
        status: 'completed',
        paymentMethod: 'paypal',
        paymentMethodType: 2, // PayPal
        confirmationNumber: result.confirmationNumber,
        slidingScalePercentage: 0,
        createdAt: new Date().toISOString(),
    });

    // 2. Regenerate the idempotency key so the old one isn't reused.
    deps.setIdempotencyKey(generate());

    // 3. Show success notification.
    deps.showNotification({
        title: 'Ticket Purchased Successfully!',
        message: result.confirmationNumber
            ? `Confirmation: ${result.confirmationNumber}`
            : 'Your PayPal payment has been processed.',
        color: 'green',
        autoClose: 8000,
    });

    // 4. Advance to the Confirmation step.
    //
    // IMPORTANT: do NOT remove this line without updating the corresponding unit test
    // in tests/unit/web/features/payments/paypalSuccessHandler.test.ts. The whole
    // reason this function exists in its own file is to guard against silent removal
    // of this line (BE-15 regression, 2026-04-12).
    deps.setCurrentStep(2);
}
