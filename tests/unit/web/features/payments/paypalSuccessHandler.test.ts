/**
 * Unit tests for handlePayPalSuccess (apps/web/src/features/payments/utils/paypalSuccessHandler.ts).
 *
 * Regression guard for BE-15 (2026-04-12): an earlier edit of EventPaymentPage.tsx
 * accidentally deleted the `setCurrentStep(2)` line from the inline PayPal-success
 * handler. PayPal captures succeeded but the checkout page stayed stuck on the
 * Payment step. These tests lock the required side-effect set so the same regression
 * can't silently ship again.
 *
 * Tests for:
 *   1. Completed-payment state is populated from the PayPal capture result
 *   2. Idempotency key is regenerated (via the injected generator)
 *   3. Success notification is fired
 *   4. currentStep is advanced to 2 (the regressed line)
 *   5. setCurrentStep is called AFTER completedPayment and idempotencyKey are set
 *      (order matters — the Confirmation step renders from completedPayment, so
 *      stepping forward before setting it would render an empty screen for a frame)
 *
 * See also: tech-debt T-8 (PayPal checkout component tests).
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';
import {
    handlePayPalSuccess,
    type PayPalSuccessDeps,
} from '@/features/payments/utils/paypalSuccessHandler';
import type { PayPalCheckoutResult } from '@/features/payments/components/PayPalButton';

describe('handlePayPalSuccess', () => {
    let deps: PayPalSuccessDeps;
    let callOrder: string[];

    beforeEach(() => {
        callOrder = [];
        deps = {
            setCompletedPayment: vi.fn(() => void callOrder.push('setCompletedPayment')),
            setIdempotencyKey: vi.fn(() => void callOrder.push('setIdempotencyKey')),
            showNotification: vi.fn(() => void callOrder.push('showNotification')),
            setCurrentStep: vi.fn(() => void callOrder.push('setCurrentStep')),
            generateIdempotencyKey: vi.fn(() => 'WCR-test-key-abc123'),
        };
    });

    const sampleResult: PayPalCheckoutResult = {
        captureId: 'CAPTURE-ABC-123',
        status: 'COMPLETED',
        amount: '40.00',
        currency: 'USD',
        payerId: 'PAYER-XYZ-789',
        confirmationNumber: 'WCR-CONFIRM-42',
        ticketPurchaseIds: ['purchase-1', 'purchase-2'],
    };

    it('populates completed-payment state from the capture result', () => {
        handlePayPalSuccess(sampleResult, deps);

        expect(deps.setCompletedPayment).toHaveBeenCalledOnce();
        expect(deps.setCompletedPayment).toHaveBeenCalledWith(
            expect.objectContaining({
                id: 'CAPTURE-ABC-123',
                transactionId: 'CAPTURE-ABC-123',
                amount: 40.0,
                currency: 'USD',
                status: 'completed',
                paymentMethod: 'paypal',
                paymentMethodType: 2,
                confirmationNumber: 'WCR-CONFIRM-42',
                slidingScalePercentage: 0,
            }),
        );
    });

    it('regenerates the idempotency key', () => {
        handlePayPalSuccess(sampleResult, deps);

        expect(deps.generateIdempotencyKey).toHaveBeenCalledOnce();
        expect(deps.setIdempotencyKey).toHaveBeenCalledOnce();
        expect(deps.setIdempotencyKey).toHaveBeenCalledWith('WCR-test-key-abc123');
    });

    it('shows a success notification with the confirmation number', () => {
        handlePayPalSuccess(sampleResult, deps);

        expect(deps.showNotification).toHaveBeenCalledOnce();
        expect(deps.showNotification).toHaveBeenCalledWith(
            expect.objectContaining({
                title: 'Ticket Purchased Successfully!',
                message: 'Confirmation: WCR-CONFIRM-42',
                color: 'green',
                autoClose: 8000,
            }),
        );
    });

    it('shows a generic success notification when confirmationNumber is absent', () => {
        const resultWithoutNumber = { ...sampleResult, confirmationNumber: undefined };
        handlePayPalSuccess(resultWithoutNumber, deps);

        expect(deps.showNotification).toHaveBeenCalledWith(
            expect.objectContaining({
                message: 'Your PayPal payment has been processed.',
            }),
        );
    });

    // ================================================================================
    // THE REGRESSION GUARD
    // ================================================================================
    // The 2026-04-12 bug: setCurrentStep(2) silently dropped during a refactor.
    // PayPal capture succeeded, completed-payment data was populated, notification
    // fired — everything LOOKED correct from the console — but the Confirmation step
    // never rendered because the stepper stayed at index 1.
    //
    // If this test fails, someone removed the setCurrentStep(2) line. DO NOT SILENCE
    // this test — restore the line in paypalSuccessHandler.ts.
    // ================================================================================
    it('advances the stepper to step 2 (Confirmation)', () => {
        handlePayPalSuccess(sampleResult, deps);

        expect(deps.setCurrentStep).toHaveBeenCalledOnce();
        expect(deps.setCurrentStep).toHaveBeenCalledWith(2);
    });

    it('calls setCurrentStep AFTER populating completed-payment state', () => {
        // Ordering matters — Confirmation renders from completedPayment, so
        // advancing the stepper before setCompletedPayment would flash an empty screen.
        handlePayPalSuccess(sampleResult, deps);

        const setCompletedIdx = callOrder.indexOf('setCompletedPayment');
        const setStepIdx = callOrder.indexOf('setCurrentStep');

        expect(setCompletedIdx).toBeGreaterThanOrEqual(0);
        expect(setStepIdx).toBeGreaterThanOrEqual(0);
        expect(setStepIdx).toBeGreaterThan(setCompletedIdx);
    });

    it('fires all four required side-effects exactly once', () => {
        handlePayPalSuccess(sampleResult, deps);

        // All four effects must fire. This is the overall contract — if you add a new
        // side-effect, add an assertion; if you remove one, make sure that's intentional.
        expect(deps.setCompletedPayment).toHaveBeenCalledOnce();
        expect(deps.setIdempotencyKey).toHaveBeenCalledOnce();
        expect(deps.showNotification).toHaveBeenCalledOnce();
        expect(deps.setCurrentStep).toHaveBeenCalledOnce();
    });
});
